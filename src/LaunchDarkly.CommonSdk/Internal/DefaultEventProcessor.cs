using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using LaunchDarkly.Client;

namespace LaunchDarkly.Common
{
    internal sealed class DefaultEventProcessor : IEventProcessor
    {
        internal static readonly ILog Log = LogManager.GetLogger(typeof(DefaultEventProcessor));
        internal static readonly string CurrentSchemaVersion = "3";

        private readonly BlockingCollection<IEventMessage> _messageQueue;
        private readonly EventDispatcher _dispatcher;
        private readonly IDiagnosticStore _diagnosticStore;
        private readonly Timer _flushTimer;
        private readonly Timer _flushUsersTimer;
        private readonly TimeSpan _diagnosticRecordingInterval;
        private readonly Object _diagnosticTimerLock = new Object();
        private Timer _diagnosticTimer;
        private AtomicBoolean _stopped;
        private AtomicBoolean _offline;
        private AtomicBoolean _sentInitialDiagnostics;
        private AtomicBoolean _inputCapacityExceeded;

        internal DefaultEventProcessor(IEventProcessorConfiguration config,
            IUserDeduplicator userDeduplicator,
            HttpClient httpClient,
            IDiagnosticStore diagnosticStore,
            IDiagnosticDisabler diagnosticDisabler,
            Action testActionOnDiagnosticSend)
        {
            _stopped = new AtomicBoolean(false);
            _offline = new AtomicBoolean(false);
            _sentInitialDiagnostics = new AtomicBoolean(false);
            _inputCapacityExceeded = new AtomicBoolean(false);
            _messageQueue = new BlockingCollection<IEventMessage>(config.EventCapacity);
            _dispatcher = new EventDispatcher(config, _messageQueue, userDeduplicator, httpClient, diagnosticStore, testActionOnDiagnosticSend);
            _flushTimer = new Timer(DoBackgroundFlush, null, config.EventFlushInterval,
                config.EventFlushInterval);
            _diagnosticStore = diagnosticStore;
            _diagnosticRecordingInterval = config.DiagnosticRecordingInterval;
            if (userDeduplicator != null && userDeduplicator.FlushInterval.HasValue)
            {
                _flushUsersTimer = new Timer(DoUserKeysFlush, null, userDeduplicator.FlushInterval.Value,
                    userDeduplicator.FlushInterval.Value);
            }
            else
            {
                _flushUsersTimer = null;
            }

            if (diagnosticStore != null)
            {
                SetupDiagnosticInit(diagnosticDisabler == null || !diagnosticDisabler.Disabled);

                if (diagnosticDisabler != null)
                {
                    diagnosticDisabler.DisabledChanged += ((sender, args) => SetupDiagnosticInit(!args.Disabled));
                }
            }
        }

        private void SetupDiagnosticInit(bool enabled)
        {
            lock (_diagnosticTimerLock) {
                _diagnosticTimer?.Dispose();
                _diagnosticTimer = null;
                if (enabled)
                {
                    TimeSpan initialDelay = _diagnosticRecordingInterval - (DateTime.Now - _diagnosticStore.DataSince);
                    TimeSpan safeDelay = Util.Clamp(initialDelay, TimeSpan.Zero, _diagnosticRecordingInterval);
                    _diagnosticTimer = new Timer(DoDiagnosticSend, null, safeDelay, _diagnosticRecordingInterval);
                }
            }
            // Send initial and persisted unsent event the first time diagnostics are started
            if (enabled && !_sentInitialDiagnostics.GetAndSet(true))
            {
                var unsent = _diagnosticStore.PersistedUnsentEvent;
                var init = _diagnosticStore.InitEvent;
                if (unsent.HasValue)
                {
                    Task.Run(() => _dispatcher.SendDiagnosticEventAsync(unsent.Value));
                }
                if (init.HasValue)
                {
                    Task.Run(() => _dispatcher.SendDiagnosticEventAsync(init.Value));
                }
            }
        }

        public void SetOffline(bool offline)
        {
            _offline.GetAndSet(offline);
            // Note that the offline state is known only to DefaultEventProcessor, not to EventDispatcher. We will
            // simply avoid sending any flush messages to EventDispatcher if we're offline. EventDispatcher will
            // never initiate a flush on its own.
        }

        public void SendEvent(Event eventToLog)
        {
            SubmitMessage(new EventMessage(eventToLog));
        }

        public void Flush()
        {
            if (!_offline.Get())
            {
                SubmitMessage(new FlushMessage());
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_stopped.GetAndSet(true))
                {
                    _flushTimer.Dispose();
                    if (_flushUsersTimer != null)
                    {
                        _flushUsersTimer.Dispose();
                    }
                    SubmitMessage(new FlushMessage());
                    ShutdownMessage message = new ShutdownMessage();
                    SubmitMessage(message);
                    message.WaitForCompletion();
                    ((IDisposable)_dispatcher).Dispose();
                    _messageQueue.CompleteAdding();
                    _messageQueue.Dispose();
                }
            }
        }

        private bool SubmitMessage(IEventMessage message)
        {
            try
            {
                if (_messageQueue.TryAdd(message))
                {
                    _inputCapacityExceeded.GetAndSet(false);
                }
                else
                {
                    // This doesn't mean that the output event buffer is full, but rather that the main thread is
                    // seriously backed up with not-yet-processed events. We shouldn't see this.
                    if (!_inputCapacityExceeded.GetAndSet(true))
                    {
                        Log.Warn("Events are being produced faster than they can be processed");
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // queue has been shut down
                return false;
            }
            return true;
        }

        // exposed for testing
        internal void WaitUntilInactive()
        {
            TestSyncMessage message = new TestSyncMessage();
            SubmitMessage(message);
            message.WaitForCompletion();
        }

        private void DoBackgroundFlush(object stateInfo)
        {
            if (!_offline.Get())
            {
                SubmitMessage(new FlushMessage());
            }
        }

        private void DoUserKeysFlush(object stateInfo)
        {
            SubmitMessage(new FlushUsersMessage());
        }

        // exposed for testing 
        internal void DoDiagnosticSend(object stateInfo)
        {
            SubmitMessage(new DiagnosticMessage());
        }
    }

    internal class AtomicBoolean
    {
        internal volatile int _value;

        internal AtomicBoolean(bool value)
        {
            _value = value ? 1 : 0;
        }

        internal bool Get()
        {
            return _value != 0;
        }

        internal bool GetAndSet(bool newValue)
        {
            int old = Interlocked.Exchange(ref _value, newValue ? 1 : 0);
            return old != 0;
        }
    }

    internal interface IEventMessage { }

    internal class EventMessage : IEventMessage
    {
        internal Event Event { get; private set; }

        internal EventMessage(Event e)
        {
            Event = e;
        }
    }

    internal class FlushMessage : IEventMessage { }

    internal class FlushUsersMessage : IEventMessage { }

    internal class DiagnosticMessage : IEventMessage { }

    internal class SynchronousMessage : IEventMessage
    {
        internal readonly Semaphore _reply;

        internal SynchronousMessage()
        {
            _reply = new Semaphore(0, 1);
        }

        internal void WaitForCompletion()
        {
            _reply.WaitOne();
        }

        internal void Completed()
        {
            _reply.Release();
        }
    }

    internal class TestSyncMessage : SynchronousMessage { }

    internal class ShutdownMessage : SynchronousMessage { }

    internal sealed class EventDispatcher : IDisposable
    {
        private static readonly int MaxFlushWorkers = 5;

        private readonly IEventProcessorConfiguration _config;
        private readonly IDiagnosticStore _diagnosticStore;
        private readonly IUserDeduplicator _userDeduplicator;
        private readonly CountdownEvent _flushWorkersCounter;
        private readonly Action _testActionOnDiagnosticSend;
        private readonly HttpClient _httpClient;
        private readonly Random _random;
        private long _lastKnownPastTime;
        private volatile bool _disabled;

        internal EventDispatcher(IEventProcessorConfiguration config,
            BlockingCollection<IEventMessage> messageQueue,
            IUserDeduplicator userDeduplicator,
            HttpClient httpClient,
            IDiagnosticStore diagnosticStore,
            Action testActionOnDiagnosticSend)
        {
            _config = config;
            _diagnosticStore = diagnosticStore;
            _userDeduplicator = userDeduplicator;
            _testActionOnDiagnosticSend = testActionOnDiagnosticSend;
            _flushWorkersCounter = new CountdownEvent(1);
            _httpClient = httpClient;
            _random = new Random();
            
            EventBuffer buffer = new EventBuffer(config.EventCapacity, _diagnosticStore);

            Task.Run(() => RunMainLoop(messageQueue, buffer));
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
                _httpClient.Dispose();
            }
        }

        private void RunMainLoop(BlockingCollection<IEventMessage> messageQueue, EventBuffer buffer)
        {
            bool running = true;
            while (running)
            {
                try
                {
                    IEventMessage message = messageQueue.Take();
                    switch (message)
                    {
                        case EventMessage em:
                            ProcessEvent(em.Event, buffer);
                            break;
                        case FlushMessage fm:
                            StartFlush(buffer);
                            break;
                        case FlushUsersMessage fm:
                            if (_userDeduplicator != null)
                            {
                                _userDeduplicator.Flush();
                            }
                            break;
                        case DiagnosticMessage dm:
                            SendAndResetDiagnostics(buffer);
                            break;
                        case TestSyncMessage tm:
                            WaitForFlushes();
                            tm.Completed();
                            break;
                        case ShutdownMessage sm:
                            WaitForFlushes();
                            running = false;
                            sm.Completed();
                            break;
                    }
                }
                catch (Exception e)
                {
                    DefaultEventProcessor.Log.ErrorFormat("Unexpected error in event dispatcher thread: {0}",
                        e, Util.ExceptionMessage(e));
                }
            }
        }

        private void SendAndResetDiagnostics(EventBuffer buffer)
        {
            if (_diagnosticStore != null)
            {
                Task.Run(() => SendDiagnosticEventAsync(_diagnosticStore.CreateEventAndReset()));
            }
        }

        private void WaitForFlushes()
        {
            // Our CountdownEvent was initialized with a count of 1, so that's the lowest it can be at this point.
            _flushWorkersCounter.Signal(); // Drop the count to zero if there are no active flush tasks.
            _flushWorkersCounter.Wait();   // Wait until it is zero.
            _flushWorkersCounter.Reset(1);
        }

        private void ProcessEvent(Event e, EventBuffer buffer)
        {
            if (_disabled)
            {
                return;
            }

            // Always record the event in the summarizer.
            buffer.AddToSummary(e);

            // Decide whether to add the event to the payload. Feature events may be added twice, once for
            // the event (if tracked) and once for debugging.
            bool willAddFullEvent;
            Event debugEvent = null;
            if (e is FeatureRequestEvent fe)
            {
                willAddFullEvent = fe.TrackEvents;
                if (ShouldDebugEvent(fe))
                {
                    debugEvent = EventFactory.Default.NewDebugEvent(fe);
                }
            }
            else
            {
                willAddFullEvent = true;
            }

            // Tell the user deduplicator, if any, about this user; this may produce an index event.
            // We only need to do this if there is *not* already going to be a full-fidelity event
            // containing an inline user.
            if (!(willAddFullEvent && _config.InlineUsersInEvents))
            {
                if (_userDeduplicator != null && e.User != null)
                {
                    bool needUserEvent = _userDeduplicator.ProcessUser(e.User);
                    if (needUserEvent && !(e is IdentifyEvent))
                    {
                        IndexEvent ie = new IndexEvent(e.CreationDate, e.User);
                        buffer.AddEvent(ie);
                    }
                    else if (!(e is IdentifyEvent))
                    {
                       _diagnosticStore?.IncrementDeduplicatedUsers();
                    }
                }
            }
            
            if (willAddFullEvent)
            {
                buffer.AddEvent(e);
            }
            if (debugEvent != null)
            {
                buffer.AddEvent(debugEvent);
            }
        }

        private bool ShouldDebugEvent(FeatureRequestEvent fe)
        {
            if (fe.DebugEventsUntilDate != null)
            {
                long lastPast = Interlocked.Read(ref _lastKnownPastTime);
                if (fe.DebugEventsUntilDate > lastPast &&
                    fe.DebugEventsUntilDate > Util.GetUnixTimestampMillis(DateTime.Now))
                {
                    return true;
                }
            }
            return false;
        }
        
        private bool ShouldTrackFullEvent(Event e)
        {
            if (e is FeatureRequestEvent fe)
            {
                if (fe.TrackEvents)
                {
                    return true;
                }
                if (fe.DebugEventsUntilDate != null)
                {
                    long lastPast = Interlocked.Read(ref _lastKnownPastTime);
                    if (fe.DebugEventsUntilDate > lastPast &&
                        fe.DebugEventsUntilDate > Util.GetUnixTimestampMillis(DateTime.Now))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        // Grabs a snapshot of the current internal state, and starts a new task to send it to the server.
        private void StartFlush(EventBuffer buffer)
        {
            if (_disabled)
            {
                return;
            }
            FlushPayload payload = buffer.GetPayload();
            if (_diagnosticStore != null)
            {
                _diagnosticStore.RecordEventsInBatch(payload.Events.Length);
            }
            if (payload.Events.Length > 0 || !payload.Summary.Empty)
            {
                lock (_flushWorkersCounter)
                {
                    // Note that this counter will be 1, not 0, when there are no active flush workers.
                    // This is because a .NET CountdownEvent can't be reused without explicitly resetting
                    // it once it has gone to zero.
                    if (_flushWorkersCounter.CurrentCount >= MaxFlushWorkers + 1)
                    {
                        // We already have too many workers, so just leave the events as is
                        return;
                    }
                    // We haven't hit the limit, we'll go ahead and start a flush task
                    _flushWorkersCounter.AddCount(1);
                }
                buffer.Clear();
                Task.Run(async () => {
                    try
                    {
                        await FlushEventsAsync(payload);
                    }
                    finally
                    {
                        _flushWorkersCounter.Signal();
                    }
                });
            }
        }

        private async Task FlushEventsAsync(FlushPayload payload)
        {
            EventOutputFormatter formatter = new EventOutputFormatter(_config);
            string jsonEvents;
            int eventCount;
            try
            {
                jsonEvents = formatter.SerializeOutputEvents(payload.Events, payload.Summary, out eventCount);
            }
            catch (Exception e)
            {
                DefaultEventProcessor.Log.ErrorFormat("Error preparing events, will not send: {0}",
                    e, Util.ExceptionMessage(e));
                return;
            }

            string description = string.Format("{0} event(s)", eventCount);
            await SendWithRetry(_config.EventsUri, jsonEvents, description, false, (response, duration) =>
            {
                if (response == null)
                {
                    return;
                }

                DefaultEventProcessor.Log.DebugFormat("Event delivery took {0} ms, response status {1}",
                    duration, response.StatusCode);
                if (response.IsSuccessStatusCode)
                {
                    DateTimeOffset? respDate = response.Headers.Date;
                    if (respDate.HasValue)
                    {
                        Interlocked.Exchange(ref _lastKnownPastTime,
                            Util.GetUnixTimestampMillis(respDate.Value.DateTime));
                    }
                }
            });
        }

        private async Task SendWithRetry(Uri uri, string content, string description, bool isDiagnostic, Action<HttpResponseMessage, long> onComplete)
        {
            const int maxAttempts = 2;
            string payloadId = isDiagnostic ? null : Guid.NewGuid().ToString();
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (attempt > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                DefaultEventProcessor.Log.DebugFormat("Submitting {0} to {1} with json: {2}",
                    description, uri.AbsoluteUri, content);

                using (var cts = new CancellationTokenSource(_config.HttpClientTimeout))
                {
                    string errorMessage = null;
                    bool canRetry = false;
                    try
                    {
                        await SendEventsAsync(uri, content, payloadId, cts.Token, onComplete);
                        return; // success
                    }
                    catch (TaskCanceledException e)
                    {
                        if (e.CancellationToken == cts.Token)
                        {
                            // Indicates the task was cancelled deliberately somehow; in this case don't retry
                            DefaultEventProcessor.Log.Warn("Event sending task was cancelled");
                            return;
                        }
                        else
                        {
                            // Otherwise this was a request timeout.
                            errorMessage = "Timed out";
                            canRetry = true;
                        }
                    }
                    catch (UnsuccessfulResponseException e)
                    {
                        errorMessage = Util.HttpErrorMessageBase(e.StatusCode);
                        if (Util.IsHttpErrorRecoverable(e.StatusCode))
                        {
                            canRetry = true;
                        }
                        else
                        {
                            _disabled = true; // for error 401, etc.
                        }
                    }
                    catch (Exception e)
                    {
                        errorMessage = string.Format("Error ({0})", Util.DescribeException(e));
                        canRetry = true;
                    }
                    string nextStepDesc = canRetry ?
                        (maxAttempts == maxAttempts - 1 ? "will not retry" : "will retry after one second") :
                        "giving up permanently";
                    DefaultEventProcessor.Log.WarnFormat(errorMessage + " sending {0}; {1}",
                        description,
                        nextStepDesc);
                    if (!canRetry)
                    {
                        return;
                    }
                }
            }
        }

        internal async Task SendDiagnosticEventAsync(DiagnosticEvent diagnostic)
        {
            var jsonDiagnostic = diagnostic.JsonValue.ToJsonString();
            await SendWithRetry(_config.DiagnosticUri, jsonDiagnostic, "diagnostic event", true, (response, duration) =>
            {
                _testActionOnDiagnosticSend?.Invoke();

                if (response == null)
                {
                    return;
                }

                DefaultEventProcessor.Log.DebugFormat("Diagnostic delivery took {0} ms, response status {1}",
                    duration, response.StatusCode);
            });
        }

        private async Task<bool> SendEventsAsync(Uri uri, String jsonEvents, String payloadId, CancellationToken token, Action<HttpResponseMessage, long> onComplete)
        {
            Stopwatch timer = new Stopwatch();

            using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
            using (var stringContent = new StringContent(jsonEvents, Encoding.UTF8, "application/json"))
            {
                request.Content = stringContent;
                if (payloadId != null)
                {
                    request.Headers.Add("X-LaunchDarkly-Payload-ID", payloadId);
                    request.Headers.Add("X-LaunchDarkly-Event-Schema", DefaultEventProcessor.CurrentSchemaVersion);
                }

                using (var response = await _httpClient.SendAsync(request, token))
                {
                    timer.Stop();
                    onComplete(response, timer.ElapsedMilliseconds);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new UnsuccessfulResponseException((int)response.StatusCode);
                    }
                }
            }
            return true;
        }
    }

    internal sealed class FlushPayload
    {
        internal Event[] Events { get; set; }
        internal EventSummary Summary { get; set; }
    }

    internal sealed class EventBuffer
    {
        private readonly List<Event> _events;
        private readonly EventSummarizer _summarizer;
        private readonly IDiagnosticStore _diagnosticStore;
        private readonly int _capacity;
        private bool _exceededCapacity;

        internal EventBuffer(int capacity, IDiagnosticStore diagnosticStore)
        {
            _capacity = capacity;
            _events = new List<Event>();
            _summarizer = new EventSummarizer();
            _diagnosticStore = diagnosticStore;
        }

        internal void AddEvent(Event e)
        {
            if (_events.Count >= _capacity)
            {
                _diagnosticStore?.IncrementDroppedEvents();
                if (!_exceededCapacity)
                {
                    DefaultEventProcessor.Log.Warn("Exceeded event queue capacity. Increase capacity to avoid dropping events.");
                    _exceededCapacity = true;
                }
            }
            else
            {
                _events.Add(e);
                _exceededCapacity = false;
            }
        }

        internal void AddToSummary(Event e)
        {
            _summarizer.SummarizeEvent(e);
        }

        internal FlushPayload GetPayload()
        {
            return new FlushPayload { Events = _events.ToArray(), Summary = _summarizer.Snapshot() };
        }

        internal void Clear()
        {
            _events.Clear();
            _summarizer.Clear();
        }
    }
}
