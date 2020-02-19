using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using LaunchDarkly.Sdk.Interfaces;
using LaunchDarkly.Sdk.Internal.Helpers;
using Newtonsoft.Json;
using WireMock;
using WireMock.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Moq;
using Xunit;

using static LaunchDarkly.Sdk.TestUtil;

namespace LaunchDarkly.Sdk.Internal.Events
{
    public class DefaultEventProcessorTest
    {
        private const String HttpDateFormat = "ddd, dd MMM yyyy HH:mm:ss 'GMT'";
        private const string EventsUriPath = "/post-events-here";
        private const string DiagnosticUriPath = "/post-diagnostic-here";
        private static readonly EvaluationReason _irrelevantReason = EvaluationReason.OffReason;
        
        private SimpleConfiguration _config = new SimpleConfiguration();
        private readonly User _user = User.Builder("userKey").Name("Red").Build();
        private readonly LdValue _userJson = LdValue.Parse("{\"key\":\"userKey\",\"name\":\"Red\"}");
        private readonly LdValue _scrubbedUserJson = LdValue.Parse("{\"key\":\"userKey\",\"privateAttrs\":[\"name\"]}");

        public DefaultEventProcessorTest()
        {
            _config.EventFlushInterval = TimeSpan.FromMilliseconds(-1);
            _config.DiagnosticRecordingInterval = TimeSpan.FromMinutes(5);
        }

        private void WithServerAndProcessor(SimpleConfiguration config, Action<WireMockServer, DefaultEventProcessor> a)
        {
            WithServer(server =>
            {
                using (var ep = MakeProcessor(config, server))
                {
                    a(server, ep);
                }
            });
        }

        private DefaultEventProcessor MakeProcessor(SimpleConfiguration config, WireMockServer server)
        {
            return MakeProcessor(config, server, null, null, null);
        }
    
        private DefaultEventProcessor MakeProcessor(SimpleConfiguration config, WireMockServer server,
            IDiagnosticStore diagnosticStore, IDiagnosticDisabler diagnosticDisabler, CountdownEvent diagnosticCountdown)
        {
            if (server != null)
            {
                _config.EventsUri = new Uri(new Uri(server.Urls[0]), EventsUriPath);
                _config.DiagnosticUri = new Uri(new Uri(server.Urls[0]), DiagnosticUriPath);
            }
            return new DefaultEventProcessor(config, new TestUserDeduplicator(),
                Util.MakeHttpClient(config, SimpleClientEnvironment.Instance), diagnosticStore, diagnosticDisabler, () => { diagnosticCountdown.Signal(); });
        }

        [Fact]
        public void IdentifyEventIsQueued()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                IdentifyEvent e = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(e);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckIdentifyEvent(item, e, _userJson));
            });
        }
        
        [Fact]
        public void UserDetailsAreScrubbedInIdentifyEvent()
        {
            _config.AllAttributesPrivate = true;
            WithServerAndProcessor(_config, (server, ep) =>
            {
                IdentifyEvent e = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(e);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckIdentifyEvent(item, e, _scrubbedUserJson));
            });
        }

        [Fact]
        public void IdentifyEventCanHaveNullUser()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                IdentifyEvent e = EventFactory.Default.NewIdentifyEvent(null);
                ep.SendEvent(e);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckIdentifyEvent(item, e, LdValue.Null));
            });
        }

        [Fact]
        public void IndividualFeatureEventIsQueuedWithIndexEvent()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckIndexEvent(item, fe, _userJson),
                    item => CheckFeatureEvent(item, fe, flag, false, LdValue.Null),
                    item => CheckSummaryEvent(item));
            });
        }

        [Fact]
        public void UserDetailsAreScrubbedInIndexEvent()
        {
            _config.AllAttributesPrivate = true;
            WithServerAndProcessor(_config, (server, ep) =>
            {
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckIndexEvent(item, fe, _scrubbedUserJson),
                    item => CheckFeatureEvent(item, fe, flag, false, LdValue.Null),
                    item => CheckSummaryEvent(item));
            });
        }

        [Fact]
        public void FeatureEventCanContainInlineUser()
        {
            _config.InlineUsersInEvents = true;
            WithServerAndProcessor(_config, (server, ep) =>
            {
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckFeatureEvent(item, fe, flag, false, _userJson),
                    item => CheckSummaryEvent(item));
            });
        }

        [Fact]
        public void FeatureEventCanHaveReason()
        {
            _config.InlineUsersInEvents = true;
            WithServerAndProcessor(_config, (server, ep) =>
            {
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
                var reasons = new EvaluationReason[]
                {
                    _irrelevantReason,
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
                    ep.SendEvent(fe);

                    var output = FlushAndGetEvents(ep, server, OkResponse());
                    Assert.Collection(output,
                        item => CheckFeatureEvent(item, fe, flag, false, _userJson, reason),
                        item => CheckSummaryEvent(item));
                }
            });
        }

        [Fact]
        public void UserDetailsAreScrubbedInFeatureEvent()
        {
            _config.AllAttributesPrivate = true;
            _config.InlineUsersInEvents = true;
            WithServerAndProcessor(_config, (server, ep) =>
            {
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckFeatureEvent(item, fe, flag, false, _scrubbedUserJson),
                    item => CheckSummaryEvent(item));
            });
        }

        [Fact]
        public void FeatureEventCanHaveNullUser()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, null,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckFeatureEvent(item, fe, flag, false, LdValue.Null),
                    item => CheckSummaryEvent(item));
            });
        }

        [Fact]
        public void IndexEventIsStillGeneratedIfInlineUsersIsTrueButFeatureEventIsNotTracked()
        {
            _config.InlineUsersInEvents = true;
            WithServerAndProcessor(_config, (server, ep) =>
            {
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(false).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckIndexEvent(item, fe, _userJson),
                    item => CheckSummaryEvent(item));
            });
        }

        [Fact]
        public void EventKindIsDebugIfFlagIsTemporarilyInDebugMode()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                long futureTime = Util.GetUnixTimestampMillis(DateTime.Now) + 1000000;
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).DebugEventsUntilDate(futureTime).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckIndexEvent(item, fe, _userJson),
                    item => CheckFeatureEvent(item, fe, flag, true, _userJson),
                    item => CheckSummaryEvent(item));
            });
        }

        [Fact]
        public void EventCanBeBothTrackedAndDebugged()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                long futureTime = Util.GetUnixTimestampMillis(DateTime.Now) + 1000000;
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true)
                    .DebugEventsUntilDate(futureTime).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckIndexEvent(item, fe, _userJson),
                    item => CheckFeatureEvent(item, fe, flag, false, LdValue.Null),
                    item => CheckFeatureEvent(item, fe, flag, true, _userJson),
                    item => CheckSummaryEvent(item));
            });
        }

#if !NET46
// The following two tests are conditionally compiled because they cannot work with some versions of
// ASP.NET Core, which is used by WireMock.Net when running in .NET Core. Specifically, the HTTP server
// implementation in some platform versions will ignore any custom value that we set for the Date
// header, and always uses the current date/time instead. In other versions, it does respect our value
// for the Date header, but it's impractical to try to detect that precondition in the tests. It
// appears to always work correctly in .NET Framework.

        [Fact]
        public void DebugModeExpiresBasedOnClientTimeIfClientTimeIsLaterThanServerTime()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                // Pick a server time that is somewhat behind the client time
                long serverTime = Util.GetUnixTimestampMillis(DateTime.Now) - 20000;

                // Send and flush an event we don't care about, just to set the last server time
                ep.SendEvent(EventFactory.Default.NewIdentifyEvent(User.WithKey("otherUser")));
                FlushAndGetEvents(ep, server, AddDateHeader(OkResponse(), serverTime));

                // Now send an event with debug mode on, with a "debug until" time that is further in
                // the future than the server time, but in the past compared to the client.
                long debugUntil = serverTime + 1000;
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).DebugEventsUntilDate(debugUntil).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);

                // Should get a summary event only, not a full feature event
                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckIndexEvent(item, fe, _userJson),
                    item => CheckSummaryEvent(item));
            });
        }

        [Fact]
        public void DebugModeExpiresBasedOnServerTimeIfServerTimeIsLaterThanClientTime()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                // Pick a server time that is somewhat ahead of the client time
                long serverTime = Util.GetUnixTimestampMillis(DateTime.Now) + 20000;

                // Send and flush an event we don't care about, just to set the last server time
                ep.SendEvent(EventFactory.Default.NewIdentifyEvent(User.WithKey("otherUser")));
                FlushAndGetEvents(ep, server, AddDateHeader(OkResponse(), serverTime));

                // Now send an event with debug mode on, with a "debug until" time that is further in
                // the future than the client time, but in the past compared to the server.
                long debugUntil = serverTime - 1000;
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).DebugEventsUntilDate(debugUntil).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);

                // Should get a summary event only, not a full feature event
                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckIndexEvent(item, fe, _userJson),
                    item => CheckSummaryEvent(item));
            });
        }
#endif

        [Fact]
        public void TwoFeatureEventsForSameUserGenerateOnlyOneIndexEvent()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                IFlagEventProperties flag1 = new FlagEventPropertiesBuilder("flagkey1").Version(11).TrackEvents(true).Build();
                IFlagEventProperties flag2 = new FlagEventPropertiesBuilder("flagkey2").Version(22).TrackEvents(true).Build();
                var value = LdValue.Of("value");
                FeatureRequestEvent fe1 = EventFactory.Default.NewFeatureRequestEvent(flag1, _user,
                    new EvaluationDetail<LdValue>(value, 1, _irrelevantReason), LdValue.Null);
                FeatureRequestEvent fe2 = EventFactory.Default.NewFeatureRequestEvent(flag2, _user,
                    new EvaluationDetail<LdValue>(value, 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe1);
                ep.SendEvent(fe2);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckIndexEvent(item, fe1, _userJson),
                    item => CheckFeatureEvent(item, fe1, flag1, false, LdValue.Null),
                    item => CheckFeatureEvent(item, fe2, flag2, false, LdValue.Null),
                    item => CheckSummaryEvent(item));
            });
        }

        [Fact]
        public void NonTrackedEventsAreSummarized()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                IFlagEventProperties flag1 = new FlagEventPropertiesBuilder("flagkey1").Version(11).Build();
                IFlagEventProperties flag2 = new FlagEventPropertiesBuilder("flagkey2").Version(22).Build();
                var value1 = LdValue.Of("value1");
                var value2 = LdValue.Of("value2");
                var default1 = LdValue.Of("default1");
                var default2 = LdValue.Of("default2");
                FeatureRequestEvent fe1a = EventFactory.Default.NewFeatureRequestEvent(flag1, _user,
                    new EvaluationDetail<LdValue>(value1, 1, _irrelevantReason), default1);
                FeatureRequestEvent fe1b = EventFactory.Default.NewFeatureRequestEvent(flag1, _user,
                    new EvaluationDetail<LdValue>(value1, 1, _irrelevantReason), default1);
                FeatureRequestEvent fe1c = EventFactory.Default.NewFeatureRequestEvent(flag1, _user,
                    new EvaluationDetail<LdValue>(value2, 2, _irrelevantReason), default1);
                FeatureRequestEvent fe2 = EventFactory.Default.NewFeatureRequestEvent(flag2, _user,
                    new EvaluationDetail<LdValue>(value2, 2, _irrelevantReason), default2);
                ep.SendEvent(fe1a);
                ep.SendEvent(fe1b);
                ep.SendEvent(fe1c);
                ep.SendEvent(fe2);

                var output = FlushAndGetEvents(ep, server, OkResponse());
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
            });
        }

        [Fact]
        public void CustomEventIsQueuedWithUser()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                CustomEvent e = EventFactory.Default.NewCustomEvent("eventkey", _user, LdValue.Of(3), 1.5);
                ep.SendEvent(e);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckIndexEvent(item, e, _userJson),
                    item => CheckCustomEvent(item, e, LdValue.Null));
            });
        }
        
        [Fact]
        public void CustomEventCanContainInlineUser()
        {
            _config.InlineUsersInEvents = true;
            WithServerAndProcessor(_config, (server, ep) =>
            {
                CustomEvent e = EventFactory.Default.NewCustomEvent("eventkey", _user, LdValue.Of(3), null);
                ep.SendEvent(e);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckCustomEvent(item, e, _userJson));
            });
        }

        [Fact]
        public void UserDetailsAreScrubbedInCustomEvent()
        {
            _config.AllAttributesPrivate = true;
            _config.InlineUsersInEvents = true;
            WithServerAndProcessor(_config, (server, ep) =>
            {
                CustomEvent e = EventFactory.Default.NewCustomEvent("eventkey", _user, LdValue.Of(3), null);
                ep.SendEvent(e);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckCustomEvent(item, e, _scrubbedUserJson));
            });
        }

        [Fact]
        public void CustomEventCanHaveNullUser()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                CustomEvent e = EventFactory.Default.NewCustomEvent("eventkey", null, LdValue.Of("data"), null);
                ep.SendEvent(e);

                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckCustomEvent(item, e, LdValue.Null));
            });
        }

        [Fact]
        public void FinalFlushIsDoneOnDispose()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                IdentifyEvent e = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(e);

                PrepareEventResponse(server, OkResponse());
                ep.Dispose();

                var output = RequestAsLdValue(GetLastRequest(server)).AsList(LdValue.Convert.Json);
                Assert.Collection(output,
                    item => CheckIdentifyEvent(item, e, _userJson));
            });
        }

        [Fact]
        public void FlushDoesNothingIfThereAreNoEvents()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                ep.Flush();

                foreach (LogEntry le in server.LogEntries)
                {
                    Assert.True(false, "Should not have sent an HTTP request");
                }
            });
        }

        [Fact]
        public void FlushDoesNothingWhenOffline()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                ep.SetOffline(true);
                IdentifyEvent e = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(e);
                ep.Flush();

                // We can't prove a negative - there's no way to know when the event processor has definitely
                // decided *not* to do a flush, it is asynchronous, so this is just a best-guess delay.
                Thread.Sleep(TimeSpan.FromMilliseconds(500));

                Assert.Equal(0, server.LogEntries.Count());

                // We should have still held on to that event, so if we go online again and flush, it is sent.
                ep.SetOffline(false);
                var output = FlushAndGetEvents(ep, server, OkResponse());
                Assert.Collection(output,
                    item => CheckIdentifyEvent(item, e, _userJson));
            });
        }

        [Fact]
        public void SdkKeyIsSent()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                Event e = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(e);

                RequestMessage r = FlushAndGetRequest(ep, server, OkResponse());

                Assert.Equal("SDK_KEY", r.Headers["Authorization"][0]);
            });
        }

        [Fact]
        public void SchemaHeaderIsSent()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                Event e = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(e);

                RequestMessage r = FlushAndGetRequest(ep, server, OkResponse());

                Assert.Equal("3", r.Headers["X-LaunchDarkly-Event-Schema"][0]);
            });
        }

        [Fact]
        public void SendIsRetriedOnRecoverableFailure()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                server.Given(EventingRequest())
                    .InScenario("Send Retry")
                    .WillSetStateTo("Retry")
                    .RespondWith(Response.Create().WithStatusCode(429));

                server.Given(EventingRequest())
                    .InScenario("Send Retry")
                    .WhenStateIs("Retry")
                    .RespondWith(OkResponse());

                Event e = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(e);
                ep.Flush();
                ep.WaitUntilInactive();

                var logEntries = server.LogEntries.ToList();
                Assert.Equal(
                    logEntries[0].RequestMessage.BodyAsJson,
                    logEntries[1].RequestMessage.BodyAsJson);
            });
        }

        [Fact]
        public void EventPayloadIdIsSent()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                Event e = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(e);

                RequestMessage r = FlushAndGetRequest(ep, server, OkResponse());

                string payloadHeaderValue = r.Headers["X-LaunchDarkly-Payload-ID"][0];
                // Throws on null value or invalid format
                new Guid(payloadHeaderValue);
            });
        }

        [Fact]
        private void EventPayloadIdReusedOnRetry()
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                server.Given(EventingRequest())
                    .InScenario("Payload ID Retry")
                    .WillSetStateTo("Retry")
                    .RespondWith(Response.Create().WithStatusCode(429));

                server.Given(EventingRequest())
                    .InScenario("Payload ID Retry")
                    .WhenStateIs("Retry")
                    .RespondWith(OkResponse());

                Event e = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(e);
                ep.Flush();
                // Necessary to ensure the retry occurs before the second request for test assertion ordering
                ep.WaitUntilInactive();
                ep.SendEvent(e);
                ep.Flush();
                ep.WaitUntilInactive();

                var logEntries = server.LogEntries.ToList();

                string payloadId = logEntries[0].RequestMessage.Headers["X-LaunchDarkly-Payload-ID"][0];
                string retryId = logEntries[1].RequestMessage.Headers["X-LaunchDarkly-Payload-ID"][0];
                Assert.Equal(payloadId, retryId);
                payloadId = logEntries[2].RequestMessage.Headers["X-LaunchDarkly-Payload-ID"][0];
                Assert.NotEqual(payloadId, retryId);
            });
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
            WithServer(server =>
            {
                using (var ep = MakeProcessor(_config, server, mockDiagnosticStore.Object, null, diagnosticCountdown))
                {
                    var flag1 = new FlagEventPropertiesBuilder("flagkey1").Version(11).TrackEvents(true).Build();
                    var value = LdValue.Of("value");
                    var fe1 = EventFactory.Default.NewFeatureRequestEvent(flag1, _user,
                        new EvaluationDetail<LdValue>(value, 1, _irrelevantReason), LdValue.Null);
                    ep.SendEvent(fe1);

                    FlushAndGetEvents(ep, server, OkResponse());

                    mockDiagnosticStore.Verify(diagStore => diagStore.RecordEventsInBatch(2), Times.Once(), "Diagnostic store's RecordEventsInBatch should be called with the number of events in last flush");

                    ep.DoDiagnosticSend(null);
                    diagnosticCountdown.Wait();
                    mockDiagnosticStore.Verify(diagStore => diagStore.CreateEventAndReset(), Times.Once());
                }
            });
        }

        [Fact]
        public void DiagnosticStorePersistedUnsentEventSentToDiagnosticUri()
        {
            var expected = LdValue.BuildObject().Add("testKey", "testValue").Build();

            var mockDiagnosticStore = new Mock<IDiagnosticStore>(MockBehavior.Strict);
            mockDiagnosticStore.Setup(diagStore => diagStore.PersistedUnsentEvent).Returns(new DiagnosticEvent(expected));
            mockDiagnosticStore.Setup(diagStore => diagStore.InitEvent).Returns((DiagnosticEvent?)null);
            mockDiagnosticStore.Setup(diagStore => diagStore.DataSince).Returns((DateTime)DateTime.Now);

            WithServer(server =>
            {
                PrepareDiagnosticResponse(server, OkResponse());
                var diagnosticCountdown = new CountdownEvent(1);
                using (var ep = MakeProcessor(_config, server, mockDiagnosticStore.Object, null, diagnosticCountdown))
                {
                    diagnosticCountdown.Wait();
                    var retrieved = GetLastDiagnostic(server);
                    
                    Assert.Equal(expected, retrieved);
                }
            });
        }

        [Fact]
        public void DiagnosticStoreInitEventSentToDiagnosticUri()
        {
            var expected = LdValue.BuildObject().Add("testKey", "testValue").Build();

            var mockDiagnosticStore = new Mock<IDiagnosticStore>(MockBehavior.Strict);
            mockDiagnosticStore.Setup(diagStore => diagStore.PersistedUnsentEvent).Returns((DiagnosticEvent?)null);
            mockDiagnosticStore.Setup(diagStore => diagStore.InitEvent).Returns(new DiagnosticEvent(expected));
            mockDiagnosticStore.Setup(diagStore => diagStore.DataSince).Returns((DateTime)DateTime.Now);

            WithServer(server =>
            {
                PrepareDiagnosticResponse(server, OkResponse());
                var diagnosticCountdown = new CountdownEvent(1);
                using (var ep = MakeProcessor(_config, server, mockDiagnosticStore.Object, null, diagnosticCountdown))
                {
                    diagnosticCountdown.Wait();
                    var retrieved = GetLastDiagnostic(server);

                    Assert.Equal(expected, retrieved);
                }
            });
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

            using (var ep = MakeProcessor(_config, null, mockDiagnosticStore.Object, mockDiagnosticDisabler.Object, null))
            {
                mockDiagnosticStore.Verify(diagStore => diagStore.InitEvent, Times.Never());
                mockDiagnosticStore.Verify(diagStore => diagStore.PersistedUnsentEvent, Times.Never());
            }
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

            WithServer(server =>
            {
                PrepareDiagnosticResponse(server, OkResponse());
                var diagnosticCountdown = new CountdownEvent(2);
                using (var ep = MakeProcessor(_config, server, mockDiagnosticStore.Object, mockDiagnosticDisabler.Object, diagnosticCountdown))
                {
                    diagnosticCountdown.Wait();

                    var retrieved = new List<LdValue>();
                    foreach (LogEntry le in server.LogEntries)
                    {
                        Assert.Equal(DiagnosticUriPath, le.RequestMessage.Path);
                        retrieved.Add(RequestAsLdValue(le.RequestMessage));
                    }

                    Assert.Equal(2, retrieved.Count);
                    Assert.Contains(expectedInit, retrieved);
                    Assert.Contains(expectedStats, retrieved);
                }
            });
        }

        private void VerifyUnrecoverableHttpError(int status)
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                Event e = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(e);
                FlushAndGetEvents(ep, server, Response.Create().WithStatusCode(status));
                Assert.Equal(1, server.LogEntries.Count()); // did not retry the request
                server.ResetLogEntries();

                ep.SendEvent(e);
                ep.Flush();
                ep.WaitUntilInactive();
                foreach (LogEntry le in server.LogEntries)
                {
                    Assert.True(false, "Should not have sent an HTTP request");
                }
            });
        }

        private void VerifyRecoverableHttpError(int status)
        {
            WithServerAndProcessor(_config, (server, ep) =>
            {
                Event e = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(e);
                FlushAndGetEvents(ep, server, Response.Create().WithStatusCode(status));
                Assert.Equal(2, server.LogEntries.Count()); // did retry the request
                server.ResetLogEntries();

                ep.SendEvent(e);
                Assert.NotNull(FlushAndGetRequest(ep, server, OkResponse()));
            });
        }

        private LdValue MakeUserJson(User user)
        {
            return LdValue.Parse(JsonConvert.SerializeObject(EventUser.FromUser(user, _config)));
        }

        private void CheckIdentifyEvent(LdValue t, IdentifyEvent ie, LdValue userJson)
        {
            Assert.Equal(LdValue.Of("identify"), t.Get("kind"));
            Assert.Equal(LdValue.Of(ie.CreationDate), t.Get("creationDate"));
            Assert.Equal(userJson, t.Get("user"));
        }

        private void CheckIndexEvent(LdValue t, Event sourceEvent, LdValue userJson)
        {
            Assert.Equal(LdValue.Of("index"), t.Get("kind"));
            Assert.Equal(LdValue.Of(sourceEvent.CreationDate), t.Get("creationDate"));
            Assert.Equal(userJson, t.Get("user"));
        }

        private void CheckFeatureEvent(LdValue t, FeatureRequestEvent fe, IFlagEventProperties flag, bool debug, LdValue userJson, EvaluationReason? reason = null)
        {
            Assert.Equal(LdValue.Of(debug ? "debug" : "feature"), t.Get("kind"));
            Assert.Equal(LdValue.Of(fe.CreationDate), t.Get("creationDate"));
            Assert.Equal(LdValue.Of(flag.Key), t.Get("key"));
            Assert.Equal(LdValue.Of(flag.EventVersion), t.Get("version"));
            Assert.Equal(fe.Variation.HasValue ? LdValue.Of(fe.Variation.Value) : LdValue.Null, t.Get("variation"));
            Assert.Equal(fe.Value, t.Get("value"));
            CheckEventUserOrKey(t, fe, userJson);
            Assert.Equal(reason, fe.Reason);
        }

        private void CheckCustomEvent(LdValue t, CustomEvent e, LdValue userJson)
        {
            Assert.Equal(LdValue.Of("custom"), t.Get("kind"));
            Assert.Equal(LdValue.Of(e.Key), t.Get("key"));
            Assert.Equal(e.Data, t.Get("data"));
            CheckEventUserOrKey(t, e, userJson);
            Assert.Equal(e.MetricValue.HasValue ? LdValue.Of(e.MetricValue.Value) : LdValue.Null, t.Get("metricValue"));
        }

        private void CheckEventUserOrKey(LdValue o, Event e, LdValue userJson)
        {
            if (!userJson.IsNull)
            {
                Assert.Equal(userJson, o.Get("user"));
                Assert.Equal(LdValue.Null, o.Get("userKey"));
            }
            else
            {
                Assert.Equal(LdValue.Null, o.Get("user"));
                Assert.Equal(e.User is null ? LdValue.Null : LdValue.Of(e.User.Key), o.Get("userKey"));
            }
        }
        private void CheckSummaryEvent(LdValue t)
        {
            Assert.Equal(LdValue.Of("summary"), t.Get("kind"));
        }
        
        private void CheckSummaryEventDetails(LdValue o, long startDate, long endDate, params Action<LdValue>[] flagChecks)
        {
            CheckSummaryEvent(o);
            Assert.Equal(LdValue.Of(startDate), o.Get("startDate"));
            Assert.Equal(LdValue.Of(endDate), o.Get("endDate"));
            var features = o.Get("features");
            Assert.Equal(flagChecks.Length, features.Count);
            foreach (var flagCheck in flagChecks)
            {
                flagCheck(features);
            }
        }

        private Action<LdValue> MustHaveFlagSummary(string flagKey, LdValue defaultVal, params Action<string, LdValue>[] counterChecks)
        {
            return o =>
            {
                var fo = o.Get(flagKey);
                if (fo.IsNull)
                {
                    Assert.True(false, "could not find flag '" + flagKey + "' in: " + fo.ToString());
                }
                LdValue cs = fo.Get("counters");
                Assert.True(defaultVal.Equals(fo.Get("default")),
                    "default should be " + defaultVal + " in " + fo);
                if (counterChecks.Length != cs.Count)
                {
                    Assert.True(false, "number of counters should be " + counterChecks.Length + " in " + fo + " for flag " + flagKey);
                }
                foreach (var counterCheck in counterChecks)
                {
                    counterCheck(flagKey, cs);
                }
            };
        }

        private Action<string, LdValue> MustHaveFlagSummaryCounter(LdValue value, int? variation, int? version, int count)
        {
            return (flagKey, items) =>
            {
                if (!items.AsList(LdValue.Convert.Json).Any(o =>
                {
                    return o.Get("value").Equals(value)
                        && o.Get("version").Equals(version.HasValue ? LdValue.Of(version.Value) : LdValue.Null)
                        && o.Get("variation").Equals(variation.HasValue ? LdValue.Of(variation.Value) : LdValue.Null)
                        && o.Get("count").Equals(LdValue.Of(count));
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

        private void PrepareResponse(WireMockServer server, string path, IResponseBuilder resp)
        {
            server.Given(EventingRequest()).RespondWith(resp);
            server.ResetLogEntries();
        }

        private void PrepareEventResponse(WireMockServer server, IResponseBuilder resp)
        {
            PrepareResponse(server, EventsUriPath, resp);
        }

        private void PrepareDiagnosticResponse(WireMockServer server, IResponseBuilder resp)
        {
            PrepareResponse(server, DiagnosticUriPath, resp);
        }

        private RequestMessage FlushAndGetRequest(DefaultEventProcessor ep, WireMockServer server, IResponseBuilder resp)
        {
            PrepareEventResponse(server, resp);
            ep.Flush();
            ep.WaitUntilInactive();
            return GetLastRequest(server);
        }
        
        private RequestMessage GetLastRequest(WireMockServer server)
        {
            foreach (LogEntry le in server.LogEntries)
            {
                return le.RequestMessage;
            }
            Assert.True(false, "Did not receive a post request");
            return null;
        }

        private LdValue RequestAsLdValue(RequestMessage r)
        {
            // annoyingly, req.Body is not provided by WireMock, only req.BodyAsJson
            return LdValue.Parse(JsonConvert.SerializeObject(r.BodyAsJson));
        }

        private IReadOnlyList<LdValue> FlushAndGetEvents(DefaultEventProcessor ep, WireMockServer server, IResponseBuilder resp)
        {
            var req = FlushAndGetRequest(ep, server, resp);
            // annoyingly, req.Body is not provided by WireMock, only req.BodyAsJson
            var bodyStr = JsonConvert.SerializeObject(req.BodyAsJson);
            return RequestAsLdValue(req).AsList(LdValue.Convert.Json);
        }

        private LdValue GetLastDiagnostic(WireMockServer server) => RequestAsLdValue(GetLastRequest(server));
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
