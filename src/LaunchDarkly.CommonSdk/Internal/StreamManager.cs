using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using LaunchDarkly.EventSource;
using Newtonsoft.Json;

namespace LaunchDarkly.Common
{
    // Internal base implementation of the LaunchDarkly streaming connection. This class
    // manages an EventSource instance, and is responsible for restarting the connection if
    // necessary. It delegates all platform-specific logic to an implementation of
    // IStreamProcessor. The IStreamProcessor does not interact with the EventSource API directly.
    internal sealed class StreamManager : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StreamManager));
        private static int UNINITIALIZED = 0;
        private static int INITIALIZED = 1;

        internal delegate IEventSource EventSourceCreator(StreamProperties streamProperties, IDictionary<string, string> headers);

        private readonly IStreamProcessor _streamProcessor;
        private readonly StreamProperties _streamProperties;
        private readonly IStreamManagerConfiguration _config;
        private readonly ClientEnvironment _clientEnvironment;
        private readonly TaskCompletionSource<bool> _initTask;
        private readonly EventSourceCreator _esCreator;
        private readonly EventSource.ExponentialBackoffWithDecorrelation _backOff;
        private IEventSource _es;
        private int _initialized = UNINITIALIZED;

        /// <summary>
        /// Constructs a StreamManager instance.
        /// </summary>
        /// <param name="streamProcessor">A platform-specific implementation of IStreamProcessor.</param>
        /// <param name="streamProperties">HTTP request properties for the stream.</param>
        /// <param name="config">An implementation of IBaseConfiguration.</param>
        /// <param name="clientEnvironment">A subclass of ClientEnvironment.</param>
        /// <param name="eventSourceCreator">Null in normal usage; pass a non-null delegate if you
        /// are in a unit test and want to mock out the event source.</param>
        public StreamManager(IStreamProcessor streamProcessor, StreamProperties streamProperties,
            IStreamManagerConfiguration config, ClientEnvironment clientEnvironment,
            EventSourceCreator eventSourceCreator)
        {
            _streamProcessor = streamProcessor;
            _streamProperties = streamProperties;
            _config = config;
            _clientEnvironment = clientEnvironment;
            _esCreator = eventSourceCreator ?? DefaultEventSourceCreator;
            _initTask = new TaskCompletionSource<bool>();
            _backOff = new EventSource.ExponentialBackoffWithDecorrelation(_config.ReconnectTime, TimeSpan.FromMilliseconds(30000));
        }

        // Stream processors should set this property to true as soon as they have received their
        // first complete set of feature data. Setting it to true causes the Task created by Start
        // to be completed.
        public bool Initialized
        {
            get
            {
                return _initialized == INITIALIZED;
            }
            set
            {
                var newState = value ? INITIALIZED : UNINITIALIZED;
                if (Interlocked.Exchange(ref _initialized, newState) == UNINITIALIZED && value)
                {
                    _initTask.SetResult(true);
                    Log.Info("Initialized LaunchDarkly Stream Processor.");
                }
            }
        }

        // Attempts to start the stream connection asynchronously. The resulting Task will be
        // marked as completed as soon as the subclass implementation sets Initialized to true. 
        public Task<bool> Start()
        {
            Dictionary<string, string> headers = Util.GetRequestHeaders(_config, _clientEnvironment);
            headers.Add("Accept", "text/event-stream");

            _es = _esCreator(_streamProperties, headers);

            _es.CommentReceived += OnComment;
            _es.MessageReceived += OnMessage;
            _es.Error += OnError;
            _es.Opened += OnOpen;
            _es.Closed += OnClose;

            try
            {
                Task.Run(() => _es.StartAsync());
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("General Exception: {0}", ex, Util.ExceptionMessage(ex));

                _initTask.SetException(ex);
            }
            return _initTask.Task;
        }

        // Closes and restarts the connection (using the same stream URI).
        public async void Restart()
        {
            TimeSpan sleepTime = _backOff.GetNextBackOff();
            if (sleepTime != TimeSpan.Zero)
            {
                Log.InfoFormat("Stopping LaunchDarkly StreamProcessor. Waiting {0} milliseconds before reconnecting...",
                    sleepTime.TotalMilliseconds);
            }
            _es.Close();
            await Task.Delay(sleepTime);
            try
            {
                await _es.StartAsync();
                _backOff.ResetReconnectAttemptCount();
                Log.Info("Reconnected to LaunchDarkly StreamProcessor");
            }
            catch (Exception exc)
            {
                Log.ErrorFormat("General Exception: {0}",
                    exc, Util.ExceptionMessage(exc));
            }
        }

        private IEventSource DefaultEventSourceCreator(StreamProperties streamProperties, IDictionary<string, string> headers)
        {
            EventSource.Configuration config = EventSource.Configuration.Builder(streamProperties.StreamUri)
                .Method(streamProperties.Method)
                .RequestBodyFactory(() => streamProperties.RequestBody)
                .MessageHandler(_config.HttpClientHandler)
                .ConnectionTimeout(_config.HttpClientTimeout)
                .DelayRetryDuration(_config.ReconnectTime)
                .ReadTimeout(_config.ReadTimeout)
                .RequestHeaders(headers)
                .Logger(LogManager.GetLogger(typeof(EventSource.EventSource)))
                .Build();
            return new EventSource.EventSource(config);
        }

        private async void OnMessage(object sender, EventSource.MessageReceivedEventArgs e)
        {
            try
            {
                await _streamProcessor.HandleMessage(this, e.EventName, e.Message.Data);
            }
            catch (JsonReaderException ex)
            {
                Log.DebugFormat("Failed to deserialize JSON in {0} message:\n{1}",
                    ex, e.EventName, e.Message.Data);

                Log.ErrorFormat("Encountered an error reading feature data: {0}",
                    ex, Util.ExceptionMessage(ex));

                Restart();
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Encountered an unexpected error: {0}",
                    ex, Util.ExceptionMessage(ex));

                Restart();
            }
        }

        private void OnOpen(object sender, EventSource.StateChangedEventArgs e)
        {
            Log.Debug("Eventsource Opened");
        }

        private void OnClose(object sender, EventSource.StateChangedEventArgs e)
        {
            Log.Debug("Eventsource Closed");
        }

        private void OnComment(object sender, EventSource.CommentReceivedEventArgs e)
        {
            Log.Debug("Received a heartbeat.");
        }

        private void OnError(object sender, EventSource.ExceptionEventArgs e)
        {
            Log.ErrorFormat("Encountered EventSource error: {0}",
                e.Exception,
                Util.ExceptionMessage(e.Exception));
            if (e.Exception is EventSource.EventSourceServiceUnsuccessfulResponseException)
            {
                int status = ((EventSource.EventSourceServiceUnsuccessfulResponseException)e.Exception).StatusCode;
                Log.Error(Util.HttpErrorMessage(status, "streaming connection", "will retry"));
                if (!Util.IsHttpErrorRecoverable(status))
                {
                    _initTask.TrySetException(e.Exception); // sends this exception to the client if we haven't already started up
                    ((IDisposable)this).Dispose();
                }
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Log.Info("Stopping LaunchDarkly StreamProcessor");
                if (_es != null)
                {
                    _es.Close();
                }
            }
        }
    }
}
