using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using LaunchDarkly.EventSource;
using Moq;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class StreamManagerTest
    {
        private static string SdkKey = "sdk_key";
        private static Uri StreamUri = new Uri("http://test");

        Mock<IEventSource> _mockEventSource;
        IEventSource _eventSource;
        StubEventSourceCreator _eventSourceCreator;
        Mock<IStreamProcessor> _mockStreamProcessor;
        IStreamProcessor _streamProcessor;
        StreamProperties _streamProperties;
        SimpleConfiguration _config;

        public StreamManagerTest()
        {
            _mockEventSource = new Mock<IEventSource>();
            _mockEventSource.Setup(es => es.StartAsync()).Returns(Task.CompletedTask);
            _eventSource = _mockEventSource.Object;
            _eventSourceCreator = new StubEventSourceCreator(_eventSource);
            _config = new SimpleConfiguration
            {
                SdkKey = SdkKey
            };
            _mockStreamProcessor = new Mock<IStreamProcessor>();
            _streamProcessor = _mockStreamProcessor.Object;
            _streamProperties = new StreamProperties(StreamUri, HttpMethod.Get, null);
        }

        private StreamManager CreateManager()
        {
            return new StreamManager(_streamProcessor, _streamProperties, _config,
                SimpleClientEnvironment.Instance, _eventSourceCreator.Create);
        }

        [Fact]
        public void StreamPropertiesArePassedToEventSourceFactory()
        {
            using (StreamManager sm = CreateManager())
            {
                sm.Start();
                Assert.Equal(_streamProperties, _eventSourceCreator.ReceivedProperties);
            }
        }

        [Fact]
        public void HeadersHaveAuthorization()
        {
            using (StreamManager sm = CreateManager())
            {
                sm.Start();
                Assert.Equal(SdkKey, _eventSourceCreator.ReceivedHeaders["Authorization"]);
            }
        }

        [Fact]
        public void HeadersHaveUserAgent()
        {
            using (StreamManager sm = CreateManager())
            {
                sm.Start();
                Assert.Equal(SimpleClientEnvironment.Instance.UserAgentType + "/" +
                    SimpleClientEnvironment.Instance.VersionString, _eventSourceCreator.ReceivedHeaders["User-Agent"]);
            }
        }

        [Fact]
        public void HeadersHaveAccept()
        {
            using (StreamManager sm = CreateManager())
            {
                sm.Start();
                Assert.Equal("text/event-stream", _eventSourceCreator.ReceivedHeaders["Accept"]);
            }
        }

        [Fact]
        public void EventIsPassedToStreamProcessor()
        {
            string eventType = "put";
            string eventData = "{}";

            using (StreamManager sm = CreateManager())
            {
                sm.Start();
                MessageReceivedEventArgs e = new MessageReceivedEventArgs(new MessageEvent(eventData, null), eventType);
                _mockEventSource.Raise(es => es.MessageReceived += null, e);

                _mockStreamProcessor.Verify(sp => sp.HandleMessage(sm, eventType, eventData));
            }
        }

        [Fact]
        public void TaskIsNotCompletedByDefault()
        {
            using (StreamManager sm = CreateManager())
            {
                Task<bool> task = sm.Start();
                Assert.False(task.IsCompleted);
            }
        }

        [Fact]
        public void InitializedIsFalseByDefault()
        {
            using (StreamManager sm = CreateManager())
            {
                sm.Start();
                Assert.False(sm.Initialized);
            }
        }

        [Fact]
        public void SettingInitializedCausesTaskToBeCompleted()
        {
            using (StreamManager sm = CreateManager())
            {
                Task<bool> task = sm.Start();
                sm.Initialized = true;
                Assert.True(task.IsCompleted);
                Assert.False(task.IsFaulted);
            }
        }

        [Fact]
        public void GeneralExceptionDoesNotStopStream()
        {
            using (StreamManager sm = CreateManager())
            {
                sm.Start();
                ExceptionEventArgs e = new ExceptionEventArgs(new Exception("whatever"));
                _mockEventSource.Raise(es => es.Error += null, e);

                _mockEventSource.Verify(es => es.Close(), Times.Never());
            }
        }
        
        [Fact]
        public void Http401ErrorShutsDownStream()
        {
            VerifyUnrecoverableHttpError(401);
        }

        [Fact]
        public void Http403ErrorShutsDownStream()
        {
            VerifyUnrecoverableHttpError(403);
        }

        [Fact]
        public void Http408ErrorDoesNotShutDownStream()
        {
            VerifyRecoverableHttpError(408);
        }

        [Fact]
        public void Http429ErrorDoesNotShutDownStream()
        {
            VerifyRecoverableHttpError(429);
        }

        [Fact]
        public void Http500ErrorDoesNotShutDownStream()
        {
            VerifyRecoverableHttpError(500);
        }

        private void VerifyUnrecoverableHttpError(int status)
        {
            using (StreamManager sm = CreateManager())
            {
                Task<bool> initTask = sm.Start();
                Exception e = new EventSourceServiceUnsuccessfulResponseException("", status);
                ExceptionEventArgs eea = new ExceptionEventArgs(e);
                _mockEventSource.Raise(es => es.Error += null, eea);

                _mockEventSource.Verify(es => es.Close());
                Assert.True(initTask.IsCompleted);
                Assert.True(initTask.IsFaulted);
                Assert.Equal(e, initTask.Exception.InnerException);
                Assert.False(sm.Initialized);
            }
        }

        private void VerifyRecoverableHttpError(int status)
        {
            using (StreamManager sm = CreateManager())
            {
                Task<bool> initTask = sm.Start();
                ExceptionEventArgs e = new ExceptionEventArgs(new EventSourceServiceUnsuccessfulResponseException("", 500));
                _mockEventSource.Raise(es => es.Error += null, e);

                _mockEventSource.Verify(es => es.Close(), Times.Never());
                Assert.False(initTask.IsCompleted);
            }
        }
    }

    internal class StubEventSourceCreator
    {
        public StreamProperties ReceivedProperties { get; private set; }
        public IDictionary<string, string> ReceivedHeaders { get; private set; }
        private IEventSource _eventSource;

        public StubEventSourceCreator(IEventSource es)
        {
            _eventSource = es;
        }

        public IEventSource Create(StreamProperties sp, IDictionary<string, string> headers)
        {
            ReceivedProperties = sp;
            ReceivedHeaders = headers;
            return _eventSource;
        }
    }
}
