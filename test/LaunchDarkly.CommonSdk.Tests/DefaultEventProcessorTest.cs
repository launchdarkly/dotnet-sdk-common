using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WireMock;
using WireMock.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Moq;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class DefaultEventProcessorTest : IDisposable
    {
        private const String HttpDateFormat = "ddd, dd MMM yyyy HH:mm:ss 'GMT'";
        private const string EventsUriPath = "/post-events-here";
        private const string DiagnosticUriPath = "/post-diagnostic-here";

        private SimpleConfiguration _config = new SimpleConfiguration();
        private DefaultEventProcessor _ep;
        private FluentMockServer _server;
        private readonly User _user = User.Builder("userKey").Name("Red").Build();
        private readonly JToken _userJson = JToken.Parse("{\"key\":\"userKey\",\"name\":\"Red\"}");
        private readonly JToken _scrubbedUserJson = JToken.Parse("{\"key\":\"userKey\",\"privateAttrs\":[\"name\"]}");

        public DefaultEventProcessorTest()
        {
            _server = FluentMockServer.Start();
            _config.EventsUri = new Uri(new Uri(_server.Urls[0]), EventsUriPath);
            _config.EventFlushInterval = TimeSpan.FromMilliseconds(-1);
            _config.DiagnosticUri = new Uri(new Uri(_server.Urls[0]), DiagnosticUriPath);
            _config.DiagnosticRecordingInterval = TimeSpan.FromMinutes(5);
        }

        void IDisposable.Dispose()
        {
            _server.Stop();
            if (_ep != null)
            {
                _ep.Dispose();
            }
        }

        private DefaultEventProcessor MakeProcessor(SimpleConfiguration config)
        {
            return MakeProcessor(config, null, null, null);
        }
    
        private DefaultEventProcessor MakeProcessor(SimpleConfiguration config, IDiagnosticStore diagnosticStore, IDiagnosticDisabler diagnosticDisabler, CountdownEvent diagnosticCountdown)
        {
            return new DefaultEventProcessor(config, new TestUserDeduplicator(),
                Util.MakeHttpClient(config, SimpleClientEnvironment.Instance), diagnosticStore, diagnosticDisabler, () => { diagnosticCountdown.Signal(); });
        }

        [Fact]
        public void IdentifyEventIsQueued()
        {
            _ep = MakeProcessor(_config);
            IdentifyEvent e = EventFactory.Default.NewIdentifyEvent(_user);
            _ep.SendEvent(e);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckIdentifyEvent(item, e, _userJson));
        }
        
        [Fact]
        public void UserDetailsAreScrubbedInIdentifyEvent()
        {
            _config.AllAttributesPrivate = true;
            _ep = MakeProcessor(_config);
            IdentifyEvent e = EventFactory.Default.NewIdentifyEvent(_user);
            _ep.SendEvent(e);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckIdentifyEvent(item, e, _scrubbedUserJson));
        }

        [Fact]
        public void IdentifyEventCanHaveNullUser()
        {
            _ep = MakeProcessor(_config);
            IdentifyEvent e = EventFactory.Default.NewIdentifyEvent(null);
            _ep.SendEvent(e);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckIdentifyEvent(item, e, null));
        }

        [Fact]
        public void IndividualFeatureEventIsQueuedWithIndexEvent()
        {
            _ep = MakeProcessor(_config);
            IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
            FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, null), LdValue.Null);
            _ep.SendEvent(fe);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckIndexEvent(item, fe, _userJson),
                item => CheckFeatureEvent(item, fe, flag, false, null),
                item => CheckSummaryEvent(item));
        }

        [Fact]
        public void UserDetailsAreScrubbedInIndexEvent()
        {
            _config.AllAttributesPrivate = true;
            _ep = MakeProcessor(_config);
            IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
            FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, null), LdValue.Null);
            _ep.SendEvent(fe);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckIndexEvent(item, fe, _scrubbedUserJson),
                item => CheckFeatureEvent(item, fe, flag, false, null),
                item => CheckSummaryEvent(item));
        }

        [Fact]
        public void FeatureEventCanContainInlineUser()
        {
            _config.InlineUsersInEvents = true;
            _ep = MakeProcessor(_config);
            IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
            FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, null), LdValue.Null);
            _ep.SendEvent(fe);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckFeatureEvent(item, fe, flag, false, _userJson),
                item => CheckSummaryEvent(item));
        }

        [Fact]
        public void FeatureEventCanHaveReason()
        {
            _config.InlineUsersInEvents = true;
            _ep = MakeProcessor(_config);
            IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
            var reasons = new EvaluationReason[]
            {
                EvaluationReason.OffReason,
                EvaluationReason.FallthroughReason,
                EvaluationReason.TargetMatchReason,
                EvaluationReason.RuleMatchReason(1, "id"),
                EvaluationReason.PrerequisiteFailedReason("key"),
                EvaluationReason.ErrorReason(EvaluationErrorKind.WRONG_TYPE)
            };
            foreach (var reason in reasons)
            {
                FeatureRequestEvent fe = EventFactory.DefaultWithReasons.NewFeatureRequestEvent(flag, _user,
                     new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, reason), LdValue.Null);
                _ep.SendEvent(fe);

                JArray output = FlushAndGetEvents(OkResponse());
                Assert.Collection(output,
                    item => CheckFeatureEvent(item, fe, flag, false, _userJson, reason),
                    item => CheckSummaryEvent(item));
            }
        }

        [Fact]
        public void UserDetailsAreScrubbedInFeatureEvent()
        {
            _config.AllAttributesPrivate = true;
            _config.InlineUsersInEvents = true;
            _ep = MakeProcessor(_config);
            IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
            FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, null), LdValue.Null);
            _ep.SendEvent(fe);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckFeatureEvent(item, fe, flag, false, _scrubbedUserJson),
                item => CheckSummaryEvent(item));
        }

        [Fact]
        public void FeatureEventCanHaveNullUser()
        {
             _ep = MakeProcessor(_config);
            IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
            FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, null,
                new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, null), LdValue.Null);
            _ep.SendEvent(fe);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckFeatureEvent(item, fe, flag, false, null),
                item => CheckSummaryEvent(item));
        }

        [Fact]
        public void IndexEventIsStillGeneratedIfInlineUsersIsTrueButFeatureEventIsNotTracked()
        {
            _config.InlineUsersInEvents = true;
            _ep = MakeProcessor(_config);
            IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(false).Build();
            FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, null), LdValue.Null);
            _ep.SendEvent(fe);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckIndexEvent(item, fe, _userJson),
                item => CheckSummaryEvent(item));
        }

        [Fact]
        public void EventKindIsDebugIfFlagIsTemporarilyInDebugMode()
        {
            _ep = MakeProcessor(_config);
            long futureTime = Util.GetUnixTimestampMillis(DateTime.Now) + 1000000;
            IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).DebugEventsUntilDate(futureTime).Build();
            FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, null), LdValue.Null);
            _ep.SendEvent(fe);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckIndexEvent(item, fe, _userJson),
                item => CheckFeatureEvent(item, fe, flag, true, _userJson),
                item => CheckSummaryEvent(item));
        }

        [Fact]
        public void EventCanBeBothTrackedAndDebugged()
        {
            _ep = MakeProcessor(_config);
            long futureTime = Util.GetUnixTimestampMillis(DateTime.Now) + 1000000;
            IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true)
                .DebugEventsUntilDate(futureTime).Build();
            FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, null), LdValue.Null);
            _ep.SendEvent(fe);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckIndexEvent(item, fe, _userJson),
                item => CheckFeatureEvent(item, fe, flag, false, null),
                item => CheckFeatureEvent(item, fe, flag, true, _userJson),
                item => CheckSummaryEvent(item));
        }

        [Fact]
        public void DebugModeExpiresBasedOnClientTimeIfClientTimeIsLaterThanServerTime()
        {
            _ep = MakeProcessor(_config);

            // Pick a server time that is somewhat behind the client time
            long serverTime = Util.GetUnixTimestampMillis(DateTime.Now) - 20000;

            // Send and flush an event we don't care about, just to set the last server time
            _ep.SendEvent(EventFactory.Default.NewIdentifyEvent(User.WithKey("otherUser")));
            FlushAndGetEvents(AddDateHeader(OkResponse(), serverTime));

            // Now send an event with debug mode on, with a "debug until" time that is further in
            // the future than the server time, but in the past compared to the client.
            long debugUntil = serverTime + 1000;
            IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).DebugEventsUntilDate(debugUntil).Build();
            FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, null), LdValue.Null);
            _ep.SendEvent(fe);

            // Should get a summary event only, not a full feature event
            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckIndexEvent(item, fe, _userJson),
                item => CheckSummaryEvent(item));
        }

        [Fact]
        public void DebugModeExpiresBasedOnServerTimeIfServerTimeIsLaterThanClientTime()
        {
            _ep = MakeProcessor(_config);

            // Pick a server time that is somewhat ahead of the client time
            long serverTime = Util.GetUnixTimestampMillis(DateTime.Now) + 20000;

            // Send and flush an event we don't care about, just to set the last server time
            _ep.SendEvent(EventFactory.Default.NewIdentifyEvent(User.WithKey("otherUser")));
            FlushAndGetEvents(AddDateHeader(OkResponse(), serverTime));

            // Now send an event with debug mode on, with a "debug until" time that is further in
            // the future than the client time, but in the past compared to the server.
            long debugUntil = serverTime - 1000;
            IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).DebugEventsUntilDate(debugUntil).Build();
            FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, null), LdValue.Null);
            _ep.SendEvent(fe);

            // Should get a summary event only, not a full feature event
            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckIndexEvent(item, fe, _userJson),
                item => CheckSummaryEvent(item));
        }

        [Fact]
        public void TwoFeatureEventsForSameUserGenerateOnlyOneIndexEvent()
        {
            _ep = MakeProcessor(_config);
            IFlagEventProperties flag1 = new FlagEventPropertiesBuilder("flagkey1").Version(11).TrackEvents(true).Build();
            IFlagEventProperties flag2 = new FlagEventPropertiesBuilder("flagkey2").Version(22).TrackEvents(true).Build();
            var value = LdValue.Of("value");
            FeatureRequestEvent fe1 = EventFactory.Default.NewFeatureRequestEvent(flag1, _user,
                new EvaluationDetail<LdValue>(value, 1, null), LdValue.Null);
            FeatureRequestEvent fe2 = EventFactory.Default.NewFeatureRequestEvent(flag2, _user,
                new EvaluationDetail<LdValue>(value, 1, null), LdValue.Null);
            _ep.SendEvent(fe1);
            _ep.SendEvent(fe2);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckIndexEvent(item, fe1, _userJson),
                item => CheckFeatureEvent(item, fe1, flag1, false, null),
                item => CheckFeatureEvent(item, fe2, flag2, false, null),
                item => CheckSummaryEvent(item));
        }

        [Fact]
        public void NonTrackedEventsAreSummarized()
        {
            _ep = MakeProcessor(_config);
            IFlagEventProperties flag1 = new FlagEventPropertiesBuilder("flagkey1").Version(11).Build();
            IFlagEventProperties flag2 = new FlagEventPropertiesBuilder("flagkey2").Version(22).Build();
            var value1 = LdValue.Of("value1");
            var value2 = LdValue.Of("value2");
            var default1 = LdValue.Of("default1");
            var default2 = LdValue.Of("default2");
            FeatureRequestEvent fe1a = EventFactory.Default.NewFeatureRequestEvent(flag1, _user,
                new EvaluationDetail<LdValue>(value1, 1, null), default1);
            FeatureRequestEvent fe1b = EventFactory.Default.NewFeatureRequestEvent(flag1, _user,
                new EvaluationDetail<LdValue>(value1, 1, null), default1);
            FeatureRequestEvent fe1c = EventFactory.Default.NewFeatureRequestEvent(flag1, _user,
                new EvaluationDetail<LdValue>(value2, 2, null), default1);
            FeatureRequestEvent fe2 = EventFactory.Default.NewFeatureRequestEvent(flag2, _user,
                new EvaluationDetail<LdValue>(value2, 2, null), default2);
            _ep.SendEvent(fe1a);
            _ep.SendEvent(fe1b);
            _ep.SendEvent(fe1c);
            _ep.SendEvent(fe2);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckIndexEvent(item, fe1a, _userJson),
                item => CheckSummaryEventDetails(item,
                    fe1a.CreationDate,
                    fe2.CreationDate,
                    MustHaveFlagSummary(flag1.Key, default1,
                        MustHaveFlagSummaryCounter(value1, 1, flag1.EventVersion, 2),
                        MustHaveFlagSummaryCounter(value2, 2, flag1.EventVersion, 1)
                    ),
                    MustHaveFlagSummary(flag2.Key, default2,
                        MustHaveFlagSummaryCounter(value2, 2, flag2.EventVersion, 1)
                    )
                )
            );
        }

        [Fact]
        public void CustomEventIsQueuedWithUser()
        {
            _ep = MakeProcessor(_config);
            CustomEvent e = EventFactory.Default.NewCustomEvent("eventkey", _user, LdValue.Of(3), 1.5);
            _ep.SendEvent(e);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckIndexEvent(item, e, _userJson),
                item => CheckCustomEvent(item, e, null));
        }
        
        [Fact]
        public void CustomEventCanContainInlineUser()
        {
            _config.InlineUsersInEvents = true;
            _ep = MakeProcessor(_config);
            CustomEvent e = EventFactory.Default.NewCustomEvent("eventkey", _user, LdValue.Of(3), null);
            _ep.SendEvent(e);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckCustomEvent(item, e, _userJson));
        }

        [Fact]
        public void UserDetailsAreScrubbedInCustomEvent()
        {
            _config.AllAttributesPrivate = true;
            _config.InlineUsersInEvents = true;
            _ep = MakeProcessor(_config);
            CustomEvent e = EventFactory.Default.NewCustomEvent("eventkey", _user, LdValue.Of(3), null);
            _ep.SendEvent(e);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckCustomEvent(item, e, _scrubbedUserJson));
        }

        [Fact]
        public void CustomEventCanHaveNullUser()
        {
            _ep = MakeProcessor(_config);
            CustomEvent e = EventFactory.Default.NewCustomEvent("eventkey", null, LdValue.Of("data"), null);
            _ep.SendEvent(e);

            JArray output = FlushAndGetEvents(OkResponse());
            Assert.Collection(output,
                item => CheckCustomEvent(item, e, null));
        }

        [Fact]
        public void FinalFlushIsDoneOnDispose()
        {
            _ep = MakeProcessor(_config);
            IdentifyEvent e = EventFactory.Default.NewIdentifyEvent(_user);
            _ep.SendEvent(e);

            PrepareEventResponse(OkResponse());
            _ep.Dispose();

            JArray output = GetLastRequest().BodyAsJson as JArray;
            Assert.Collection(output,
                item => CheckIdentifyEvent(item, e, _userJson));
        }

        [Fact]
        public void FlushDoesNothingIfThereAreNoEvents()
        {
            _ep = MakeProcessor(_config);
            _ep.Flush();

            foreach (LogEntry le in _server.LogEntries)
            {
                Assert.True(false, "Should not have sent an HTTP request");
            }
        }

        [Fact]
        public void SdkKeyIsSent()
        {
            _ep = MakeProcessor(_config);
            Event e = EventFactory.Default.NewIdentifyEvent(_user);
            _ep.SendEvent(e);

            RequestMessage r = FlushAndGetRequest(OkResponse());

            Assert.Equal("SDK_KEY", r.Headers["Authorization"][0]);
        }

        [Fact]
        public void SchemaHeaderIsSent()
        {
            _ep = MakeProcessor(_config);
            Event e = EventFactory.Default.NewIdentifyEvent(_user);
            _ep.SendEvent(e);

            RequestMessage r = FlushAndGetRequest(OkResponse());

            Assert.Equal("3", r.Headers["X-LaunchDarkly-Event-Schema"][0]);
        }

        [Fact]
        public void SendIsRetriedOnRecoverableFailure()
        {
            _ep = MakeProcessor(_config);

            _server.Given(EventingRequest())
                .InScenario("Send Retry")
                .WillSetStateTo("Retry")
                .RespondWith(Response.Create().WithStatusCode(429));

            _server.Given(EventingRequest())
                .InScenario("Send Retry")
                .WhenStateIs("Retry")
                .RespondWith(OkResponse());

            Event e = EventFactory.Default.NewIdentifyEvent(_user);
            _ep.SendEvent(e);
            _ep.Flush();
            ((DefaultEventProcessor)_ep).WaitUntilInactive();

            var logEntries = _server.LogEntries.ToList();
            Assert.Equal(
                logEntries[0].RequestMessage.BodyAsJson,
                logEntries[1].RequestMessage.BodyAsJson);
        }

        [Fact]
        public void EventPayloadIdIsSent()
        {
            _ep = MakeProcessor(_config);
            Event e = EventFactory.Default.NewIdentifyEvent(_user);
            _ep.SendEvent(e);

            RequestMessage r = FlushAndGetRequest(OkResponse());

            string payloadHeaderValue = r.Headers["X-LaunchDarkly-Payload-ID"][0];
            // Throws on null value or invalid format
            new Guid(payloadHeaderValue);
        }

        [Fact]
        private void EventPayloadIdReusedOnRetry()
        {
            _ep = MakeProcessor(_config);

            _server.Given(EventingRequest())
                .InScenario("Payload ID Retry")
                .WillSetStateTo("Retry")
                .RespondWith(Response.Create().WithStatusCode(429));

            _server.Given(EventingRequest())
                .InScenario("Payload ID Retry")
                .WhenStateIs("Retry")
                .RespondWith(OkResponse());

            Event e = EventFactory.Default.NewIdentifyEvent(_user);
            _ep.SendEvent(e);
            _ep.Flush();
            // Necessary to ensure the retry occurs before the second request for test assertion ordering
            ((DefaultEventProcessor)_ep).WaitUntilInactive();
            _ep.SendEvent(e);
            _ep.Flush();
            ((DefaultEventProcessor)_ep).WaitUntilInactive();

            var logEntries = _server.LogEntries.ToList();

            string payloadId = logEntries[0].RequestMessage.Headers["X-LaunchDarkly-Payload-ID"][0];
            string retryId = logEntries[1].RequestMessage.Headers["X-LaunchDarkly-Payload-ID"][0];
            Assert.Equal(payloadId, retryId);
            payloadId = logEntries[2].RequestMessage.Headers["X-LaunchDarkly-Payload-ID"][0];
            Assert.NotEqual(payloadId, retryId);
        }

        [Fact]
        public void EventsAreStillPostedAfterReceiving400Error()
        {
            VerifyRecoverableHttpError(400);
        }

        [Fact]
        public void NoMoreEventsArePostedAfterReceiving401Error()
        {
            VerifyUnrecoverableHttpError(401);
        }

        [Fact]
        public void NoMoreEventsArePostedAfterReceiving403Error()
        {
            VerifyUnrecoverableHttpError(403);
        }

        [Fact]
        public void EventsAreStillPostedAfterReceiving408Error()
        {
            VerifyRecoverableHttpError(408);
        }

        [Fact]
        public void EventsAreStillPostedAfterReceiving429Error()
        {
            VerifyRecoverableHttpError(429);
        }

        [Fact]
        public void EventsAreStillPostedAfterReceiving500Error()
        {
            VerifyRecoverableHttpError(500);
        }

        [Fact]
        public void EventsInBatchRecorded()
        {
            var mockDiagnosticStore = new Mock<IDiagnosticStore>(MockBehavior.Strict);
            mockDiagnosticStore.Setup(diagStore => diagStore.PersistedUnsentEvent).Returns((DiagnosticEvent?)null);
            mockDiagnosticStore.Setup(diagStore => diagStore.InitEvent).Returns((DiagnosticEvent?)null);
            mockDiagnosticStore.Setup(diagStore => diagStore.DataSince).Returns((DateTime)DateTime.Now);
            mockDiagnosticStore.Setup(diagStore => diagStore.RecordEventsInBatch(It.IsAny<long>()));
            mockDiagnosticStore.Setup(diagStore => diagStore.CreateEventAndReset()).Returns(new DiagnosticEvent(LdValue.Null));

            CountdownEvent diagnosticCountdown = new CountdownEvent(1);
            _ep = MakeProcessor(_config, mockDiagnosticStore.Object, null, diagnosticCountdown);

            var flag1 = new FlagEventPropertiesBuilder("flagkey1").Version(11).TrackEvents(true).Build();
            var value = LdValue.Of("value");
            var fe1 = EventFactory.Default.NewFeatureRequestEvent(flag1, _user,
                new EvaluationDetail<LdValue>(value, 1, null), LdValue.Null);
            _ep.SendEvent(fe1);

            FlushAndGetEvents(OkResponse());

            mockDiagnosticStore.Verify(diagStore => diagStore.RecordEventsInBatch(2), Times.Once(), "Diagnostic store's RecordEventsInBatch should be called with the number of events in last flush");

            _ep.DoDiagnosticSend(null);
            diagnosticCountdown.Wait();
            mockDiagnosticStore.Verify(diagStore => diagStore.CreateEventAndReset(), Times.Once());
        }

        [Fact]
        public void DiagnosticStorePersistedUnsentEventSentToDiagnosticUri()
        {
            var expected = LdValue.BuildObject().Add("testKey", "testValue").Build();

            var mockDiagnosticStore = new Mock<IDiagnosticStore>(MockBehavior.Strict);
            mockDiagnosticStore.Setup(diagStore => diagStore.PersistedUnsentEvent).Returns(new DiagnosticEvent(expected));
            mockDiagnosticStore.Setup(diagStore => diagStore.InitEvent).Returns((DiagnosticEvent?)null);
            mockDiagnosticStore.Setup(diagStore => diagStore.DataSince).Returns((DateTime)DateTime.Now);

            PrepareDiagnosticResponse(OkResponse());
            var diagnosticCountdown = new CountdownEvent(1);
            _ep = MakeProcessor(_config, mockDiagnosticStore.Object, null, diagnosticCountdown);

            diagnosticCountdown.Wait();
            var retrieved = GetLastDiagnostic();
            
            Assert.Equal(expected, retrieved);
        }

        [Fact]
        public void DiagnosticStoreInitEventSentToDiagnosticUri()
        {
            var expected = LdValue.BuildObject().Add("testKey", "testValue").Build();

            var mockDiagnosticStore = new Mock<IDiagnosticStore>(MockBehavior.Strict);
            mockDiagnosticStore.Setup(diagStore => diagStore.PersistedUnsentEvent).Returns((DiagnosticEvent?)null);
            mockDiagnosticStore.Setup(diagStore => diagStore.InitEvent).Returns(new DiagnosticEvent(expected));
            mockDiagnosticStore.Setup(diagStore => diagStore.DataSince).Returns((DateTime)DateTime.Now);

            PrepareDiagnosticResponse(OkResponse());
            var diagnosticCountdown = new CountdownEvent(1);
            _ep = MakeProcessor(_config, mockDiagnosticStore.Object, null, diagnosticCountdown);

            diagnosticCountdown.Wait();
            var retrieved = GetLastDiagnostic();

            Assert.Equal(expected, retrieved);
        }

        [Fact]
        public void DiagnosticDisablerDisablesInitialDiagnostics()
        {
            var testDiagnostic = LdValue.BuildObject().Add("testKey", "testValue").Build();

            var mockDiagnosticStore = new Mock<IDiagnosticStore>(MockBehavior.Strict);
            mockDiagnosticStore.Setup(diagStore => diagStore.PersistedUnsentEvent).Returns(new DiagnosticEvent(testDiagnostic));
            mockDiagnosticStore.Setup(diagStore => diagStore.InitEvent).Returns(new DiagnosticEvent(testDiagnostic));
            mockDiagnosticStore.Setup(diagStore => diagStore.DataSince).Returns((DateTime)DateTime.Now);

            var mockDiagnosticDisabler = new Mock<IDiagnosticDisabler>(MockBehavior.Strict);
            mockDiagnosticDisabler.Setup(diagDisabler => diagDisabler.Disabled).Returns(true);

            _ep = MakeProcessor(_config, mockDiagnosticStore.Object, mockDiagnosticDisabler.Object, null);
            mockDiagnosticStore.Verify(diagStore => diagStore.InitEvent, Times.Never());
            mockDiagnosticStore.Verify(diagStore => diagStore.PersistedUnsentEvent, Times.Never());
        }

        [Fact]
        public void DiagnosticDisablerEnabledInitialDiagnostics()
        {
            var expectedStats = LdValue.BuildObject().Add("stats", "testValue").Build();
            var expectedInit = LdValue.BuildObject().Add("init", "testValue").Build();

            var mockDiagnosticStore = new Mock<IDiagnosticStore>(MockBehavior.Strict);
            mockDiagnosticStore.Setup(diagStore => diagStore.PersistedUnsentEvent).Returns(new DiagnosticEvent(expectedStats));
            mockDiagnosticStore.Setup(diagStore => diagStore.InitEvent).Returns(new DiagnosticEvent(expectedInit));
            mockDiagnosticStore.Setup(diagStore => diagStore.DataSince).Returns((DateTime)DateTime.Now);

            var mockDiagnosticDisabler = new Mock<IDiagnosticDisabler>(MockBehavior.Strict);
            mockDiagnosticDisabler.Setup(diagDisabler => diagDisabler.Disabled).Returns(false);

            PrepareDiagnosticResponse(OkResponse());
            var diagnosticCountdown = new CountdownEvent(2);
            _ep = MakeProcessor(_config, mockDiagnosticStore.Object, mockDiagnosticDisabler.Object, diagnosticCountdown);

            diagnosticCountdown.Wait();

            var retrieved = new List<LdValue>();
            foreach (LogEntry le in _server.LogEntries)
            {
                Assert.Equal(DiagnosticUriPath, le.RequestMessage.Path);
                retrieved.Add(RequestAsLdValue(le.RequestMessage));
            }

            Assert.Equal(2, retrieved.Count);
            Assert.Contains(expectedInit, retrieved);
            Assert.Contains(expectedStats, retrieved);
        }

        private void VerifyUnrecoverableHttpError(int status)
        {
            _ep = MakeProcessor(_config);
            Event e = EventFactory.Default.NewIdentifyEvent(_user);
            _ep.SendEvent(e);
            FlushAndGetEvents(Response.Create().WithStatusCode(status));
            Assert.Equal(1, _server.LogEntries.Count()); // did not retry the request
            _server.ResetLogEntries();

            _ep.SendEvent(e);
            _ep.Flush();
            _ep.WaitUntilInactive();
            foreach (LogEntry le in _server.LogEntries)
            {
                Assert.True(false, "Should not have sent an HTTP request");
            }
        }

        private void VerifyRecoverableHttpError(int status)
        {
            _ep = MakeProcessor(_config);
            Event e = EventFactory.Default.NewIdentifyEvent(_user);
            _ep.SendEvent(e);
            FlushAndGetEvents(Response.Create().WithStatusCode(status));
            Assert.Equal(2, _server.LogEntries.Count()); // did retry the request
            _server.ResetLogEntries();

            _ep.SendEvent(e);
            Assert.NotNull(FlushAndGetRequest(OkResponse()));
        }

        private JObject MakeUserJson(User user)
        {
            return JObject.FromObject(EventUser.FromUser(user, _config));
        }

        private void CheckIdentifyEvent(JToken t, IdentifyEvent ie, JToken userJson)
        {
            JObject o = t as JObject;
            Assert.Equal("identify", (string)o["kind"]);
            Assert.Equal(ie.CreationDate, (long)o["creationDate"]);
            TestUtil.AssertJsonEquals(userJson, o["user"]);
        }

        private void CheckIndexEvent(JToken t, Event sourceEvent, JToken userJson)
        {
            JObject o = t as JObject;
            Assert.Equal("index", (string)o["kind"]);
            Assert.Equal(sourceEvent.CreationDate, (long)o["creationDate"]);
            TestUtil.AssertJsonEquals(userJson, o["user"]);
        }

        private void CheckFeatureEvent(JToken t, FeatureRequestEvent fe, IFlagEventProperties flag, bool debug, JToken userJson, EvaluationReason reason = null)
        {
            JObject o = t as JObject;
            Assert.Equal(debug ? "debug" : "feature", (string)o["kind"]);
            Assert.Equal(fe.CreationDate, (long)o["creationDate"]);
            Assert.Equal(flag.Key, (string)o["key"]);
            Assert.Equal(flag.EventVersion, (int)o["version"]);
            if (fe.Variation == null)
            {
                Assert.Null(o["variation"]);
            }
            else
            {
                Assert.Equal(fe.Variation, (int)o["variation"]);
            }
            TestUtil.AssertJsonEquals(fe.LdValue.InnerValue, o["value"]);
            CheckEventUserOrKey(o, fe, userJson);
            Assert.Equal(reason, fe.Reason);
        }

        private void CheckCustomEvent(JToken t, CustomEvent e, JToken userJson)
        {
            JObject o = t as JObject;
            Assert.Equal("custom", (string)o["kind"]);
            Assert.Equal(e.Key, (string)o["key"]);
            TestUtil.AssertJsonEquals(e.LdValueData.InnerValue, o["data"]);
            CheckEventUserOrKey(o, e, userJson);
            if (e.MetricValue.HasValue)
            {
                Assert.Equal(e.MetricValue, (double)o["metricValue"]);
            }
            else
            {
                Assert.Null(o["metricValue"]);
            }
        }

        private void CheckEventUserOrKey(JObject o, Event e, JToken userJson)
        {
            if (userJson != null)
            {
                TestUtil.AssertJsonEquals(userJson, o["user"]);
                Assert.Null(o["userKey"]);
            }
            else
            {
                Assert.Null(o["user"]);
                if (e.User == null)
                {
                    Assert.Null(o["userKey"]);
                }
                else
                {
                    Assert.Equal(e.User.Key, (string)o["userKey"]);
                }
            }
        }
        private void CheckSummaryEvent(JToken t)
        {
            JObject o = t as JObject;
            Assert.Equal("summary", (string)o["kind"]);
        }

        private void CheckSummaryEventDetails(JToken t, long startDate, long endDate, params Action<JObject>[] flagChecks)
        {
            CheckSummaryEvent(t);
            JObject o = t as JObject;
            Assert.Equal(startDate, (long)o["startDate"]);
            Assert.Equal(endDate, (long)o["endDate"]);
            JObject features = o["features"] as JObject;
            Assert.Equal(flagChecks.Length, features.Count);
            foreach (var flagCheck in flagChecks)
            {
                flagCheck(features);
            }
        }

        private Action<JObject> MustHaveFlagSummary(string flagKey, LdValue defaultVal, params Action<string, IEnumerable<JToken>>[] counterChecks)
        {
            return o =>
            {
                JObject fo = o[flagKey] as JObject;
                if (fo is null)
                {
                    Assert.True(false, "could not find flag '" + flagKey + "' in: " + o.ToString());
                }
                JArray cs = fo["counters"] as JArray;
                Assert.True(JToken.DeepEquals(defaultVal.InnerValue, fo["default"]),
                    "default should be " + defaultVal + " in " + fo);
                if (cs is null || counterChecks.Length != cs.Count)
                {
                    Assert.True(false, "number of counters should be " + counterChecks.Length + " in " + fo + " for flag " + flagKey);
                }
                foreach (var counterCheck in counterChecks)
                {
                    counterCheck(flagKey, cs);
                }
            };
        }

        private Action<string, IEnumerable<JToken>> MustHaveFlagSummaryCounter(LdValue value, int? variation, int? version, int count)
        {
            return (flagKey, items) =>
            {
                if (!items.Any(item =>
                {
                    var o = item as JObject;
                    return JToken.DeepEquals(o["value"], value.InnerValue)
                        && ((int?)o["version"]) == version
                        && ((int?)o["variation"]) == variation
                        && ((int)o["count"]) == count;
                }))
                {
                    Assert.True(false, "could not find counter for (" + value + ", " + version + ", " + variation + ", " + count
                        + ") in: " + items.ToString() + " for flag " + flagKey);
                }
            };
        }

        private IResponseBuilder OkResponse()
        {
            return Response.Create().WithStatusCode(200);
        }

        private IRequestBuilder EventingRequest()
        {
            return Request.Create().WithPath(EventsUriPath).UsingPost();
        }

        private IResponseBuilder AddDateHeader(IResponseBuilder resp, long timestamp)
        {
            DateTime dt = Util.UnixEpoch.AddMilliseconds(timestamp);
            return resp.WithHeader("Date", dt.ToString(HttpDateFormat));
        }

        private void PrepareResponse(string path, IResponseBuilder resp)
        {
            _server.Given(EventingRequest()).RespondWith(resp);
            _server.ResetLogEntries();
        }

        private void PrepareEventResponse(IResponseBuilder resp)
        {
            PrepareResponse(EventsUriPath, resp);
        }

        private void PrepareDiagnosticResponse(IResponseBuilder resp)
        {
            PrepareResponse(DiagnosticUriPath, resp);
        }

        private RequestMessage FlushAndGetRequest(IResponseBuilder resp)
        {
            PrepareEventResponse(resp);
            _ep.Flush();
            _ep.WaitUntilInactive();
            return GetLastRequest();
        }

        private RequestMessage GetLastRequest()
        {
            foreach (LogEntry le in _server.LogEntries)
            {
                return le.RequestMessage;
            }
            Assert.True(false, "Did not receive a post request");
            return null;
        }

        private LdValue RequestAsLdValue(RequestMessage r)
        {
            return LdValue.Parse(JsonConvert.SerializeObject(r.BodyAsJson));
        }

        private JArray FlushAndGetEvents(IResponseBuilder resp)
        {
            return FlushAndGetRequest(resp).BodyAsJson as JArray;
        }

        private LdValue GetLastDiagnostic() => RequestAsLdValue(GetLastRequest());
    }

    class TestUserDeduplicator : IUserDeduplicator
    {
        private HashSet<string> _userKeys = new HashSet<string>();
        public TimeSpan? FlushInterval => null;

        public void Flush()
        {
        }

        public bool ProcessUser(User user)
        {
            if (!_userKeys.Contains(user.Key))
            {
                _userKeys.Add(user.Key);
                return true;
            }
            return false;
        }
    }
}
