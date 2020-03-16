using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LaunchDarkly.Sdk.Interfaces;
using LaunchDarkly.Sdk.Internal.Helpers;
using Newtonsoft.Json;
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

        private Mock<IEventSender> MakeMockSender()
        {
            var mockSender = new Mock<IEventSender>(MockBehavior.Strict);
            mockSender.Setup(s => s.Dispose());
            return mockSender;
        }

        private Mock<IDiagnosticStore> MakeDiagnosticStore(
            DiagnosticEvent? persistedUnsentEvent,
            DiagnosticEvent? initEvent,
            DiagnosticEvent statsEvent
        )
        {
            var mockDiagnosticStore = new Mock<IDiagnosticStore>(MockBehavior.Strict);
            mockDiagnosticStore.Setup(diagStore => diagStore.PersistedUnsentEvent).Returns(persistedUnsentEvent);
            mockDiagnosticStore.Setup(diagStore => diagStore.InitEvent).Returns(initEvent);
            mockDiagnosticStore.Setup(diagStore => diagStore.DataSince).Returns(DateTime.Now);
            mockDiagnosticStore.Setup(diagStore => diagStore.RecordEventsInBatch(It.IsAny<long>()));
            mockDiagnosticStore.Setup(diagStore => diagStore.CreateEventAndReset()).Returns(statsEvent);
            return mockDiagnosticStore;
        }

        private DefaultEventProcessor MakeProcessor(SimpleConfiguration config, Mock<IEventSender> mockSender)
        {
            return MakeProcessor(config, mockSender, null, null, null);
        }

        private DefaultEventProcessor MakeProcessor(SimpleConfiguration config, Mock<IEventSender> mockSender,
            IDiagnosticStore diagnosticStore, IDiagnosticDisabler diagnosticDisabler, CountdownEvent diagnosticCountdown)
        {
            return new DefaultEventProcessor(config, mockSender.Object, new TestUserDeduplicator(),
                diagnosticStore, diagnosticDisabler, () => { diagnosticCountdown.Signal(); });
        }

        private void FlushAndWait(DefaultEventProcessor ep)
        {
            ep.Flush();
            ep.WaitUntilInactive();
        }

        [Fact]
        public void IdentifyEventIsQueued()
        {
            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                IdentifyEvent e = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(e);
                FlushAndWait(ep);

                Assert.Collection(captured.Events,
                    item => CheckIdentifyEvent(item, e, _userJson));
            }
        }
        
        [Fact]
        public void UserDetailsAreScrubbedInIdentifyEvent()
        {
            _config.AllAttributesPrivate = true;

            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                IdentifyEvent e = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(e);
                FlushAndWait(ep);

                Assert.Collection(captured.Events,
                    item => CheckIdentifyEvent(item, e, _scrubbedUserJson));
            }
        }

        [Fact]
        public void IdentifyEventCanHaveNullUser()
        {
            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                IdentifyEvent e = EventFactory.Default.NewIdentifyEvent(null);
                ep.SendEvent(e);
                FlushAndWait(ep);

                Assert.Collection(captured.Events,
                    item => CheckIdentifyEvent(item, e, LdValue.Null));
            }
        }

        [Fact]
        public void IndividualFeatureEventIsQueuedWithIndexEvent()
        {
            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);
                FlushAndWait(ep);

                Assert.Collection(captured.Events,
                    item => CheckIndexEvent(item, fe, _userJson),
                    item => CheckFeatureEvent(item, fe, flag, false, LdValue.Null),
                    item => CheckSummaryEvent(item));
            }
        }

        [Fact]
        public void UserDetailsAreScrubbedInIndexEvent()
        {
            _config.AllAttributesPrivate = true;

            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);
                FlushAndWait(ep);

                Assert.Collection(captured.Events,
                    item => CheckIndexEvent(item, fe, _scrubbedUserJson),
                    item => CheckFeatureEvent(item, fe, flag, false, LdValue.Null),
                    item => CheckSummaryEvent(item));
            }
        }

        [Fact]
        public void FeatureEventCanContainInlineUser()
        {
            _config.InlineUsersInEvents = true;

            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);
                FlushAndWait(ep);

                Assert.Collection(captured.Events,
                    item => CheckFeatureEvent(item, fe, flag, false, _userJson),
                    item => CheckSummaryEvent(item));
            }
        }

        [Fact]
        public void FeatureEventCanHaveReason()
        {
            _config.InlineUsersInEvents = true;

            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
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
                    captured.Events.Clear();

                    FeatureRequestEvent fe = EventFactory.DefaultWithReasons.NewFeatureRequestEvent(flag, _user,
                         new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, reason), LdValue.Null);
                    ep.SendEvent(fe);
                    FlushAndWait(ep);

                    Assert.Collection(captured.Events,
                        item => CheckFeatureEvent(item, fe, flag, false, _userJson, reason),
                        item => CheckSummaryEvent(item));
                }
            }
        }

        [Fact]
        public void UserDetailsAreScrubbedInFeatureEvent()
        {
            _config.AllAttributesPrivate = true;
            _config.InlineUsersInEvents = true;

            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);
                FlushAndWait(ep);

                Assert.Collection(captured.Events,
                    item => CheckFeatureEvent(item, fe, flag, false, _scrubbedUserJson),
                    item => CheckSummaryEvent(item));
            }
        }

        [Fact]
        public void FeatureEventCanHaveNullUser()
        {
            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, null,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);
                FlushAndWait(ep);

                Assert.Collection(captured.Events,
                    item => CheckFeatureEvent(item, fe, flag, false, LdValue.Null),
                    item => CheckSummaryEvent(item));
            }
        }

        [Fact]
        public void IndexEventIsStillGeneratedIfInlineUsersIsTrueButFeatureEventIsNotTracked()
        {
            _config.InlineUsersInEvents = true;

            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(false).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);
                FlushAndWait(ep);

                Assert.Collection(captured.Events,
                    item => CheckIndexEvent(item, fe, _userJson),
                    item => CheckSummaryEvent(item));
            }
        }

        [Fact]
        public void EventKindIsDebugIfFlagIsTemporarilyInDebugMode()
        {
            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                long futureTime = Util.GetUnixTimestampMillis(DateTime.Now) + 1000000;
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).DebugEventsUntilDate(futureTime).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);
                FlushAndWait(ep);

                Assert.Collection(captured.Events,
                    item => CheckIndexEvent(item, fe, _userJson),
                    item => CheckFeatureEvent(item, fe, flag, true, _userJson),
                    item => CheckSummaryEvent(item));
            }
        }

        [Fact]
        public void EventCanBeBothTrackedAndDebugged()
        {
            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                long futureTime = Util.GetUnixTimestampMillis(DateTime.Now) + 1000000;
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).TrackEvents(true)
                    .DebugEventsUntilDate(futureTime).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);
                FlushAndWait(ep);

                Assert.Collection(captured.Events,
                    item => CheckIndexEvent(item, fe, _userJson),
                    item => CheckFeatureEvent(item, fe, flag, false, LdValue.Null),
                    item => CheckFeatureEvent(item, fe, flag, true, _userJson),
                    item => CheckSummaryEvent(item));
            }
        }

        [Fact]
        public void DebugModeExpiresBasedOnClientTimeIfClientTimeIsLaterThanServerTime()
        {
            // Pick a server time that is somewhat behind the client time
            var serverTime = DateTime.Now.Subtract(TimeSpan.FromSeconds(20));

            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender,
                new EventSenderResult(DeliveryStatus.Succeeded, serverTime));

            using (var ep = MakeProcessor(_config, mockSender))
            {
                // Send and flush an event we don't care about, just to set the last server time
                ep.SendEvent(EventFactory.Default.NewIdentifyEvent(User.WithKey("otherUser")));
                FlushAndWait(ep);
                captured.Events.Clear();

                // Now send an event with debug mode on, with a "debug until" time that is further in
                // the future than the server time, but in the past compared to the client.
                long debugUntil = Util.GetUnixTimestampMillis(serverTime) + 1000;
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).DebugEventsUntilDate(debugUntil).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);
                FlushAndWait(ep);

                // Should get a summary event only, not a full feature event
                Assert.Collection(captured.Events,
                    item => CheckIndexEvent(item, fe, _userJson),
                    item => CheckSummaryEvent(item));
            }
        }

        [Fact]
        public void DebugModeExpiresBasedOnServerTimeIfServerTimeIsLaterThanClientTime()
        {
            // Pick a server time that is somewhat ahead of the client time
            var serverTime = DateTime.Now.Add(TimeSpan.FromSeconds(20));

            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender,
                new EventSenderResult(DeliveryStatus.Succeeded, serverTime));

            using (var ep = MakeProcessor(_config, mockSender))
            {
                // Send and flush an event we don't care about, just to set the last server time
                ep.SendEvent(EventFactory.Default.NewIdentifyEvent(User.WithKey("otherUser")));
                FlushAndWait(ep);
                captured.Events.Clear();

                // Now send an event with debug mode on, with a "debug until" time that is further in
                // the future than the client time, but in the past compared to the server.
                long debugUntil = Util.GetUnixTimestampMillis(serverTime) - 1000;
                IFlagEventProperties flag = new FlagEventPropertiesBuilder("flagkey").Version(11).DebugEventsUntilDate(debugUntil).Build();
                FeatureRequestEvent fe = EventFactory.Default.NewFeatureRequestEvent(flag, _user,
                    new EvaluationDetail<LdValue>(LdValue.Of("value"), 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe);
                FlushAndWait(ep);

                // Should get a summary event only, not a full feature event
                Assert.Collection(captured.Events,
                    item => CheckIndexEvent(item, fe, _userJson),
                    item => CheckSummaryEvent(item));
            }
        }

        [Fact]
        public void TwoFeatureEventsForSameUserGenerateOnlyOneIndexEvent()
        {
            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
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
                ep.Flush();
                ep.WaitUntilInactive();

                Assert.Collection(captured.Events,
                    item => CheckIndexEvent(item, fe1, _userJson),
                    item => CheckFeatureEvent(item, fe1, flag1, false, LdValue.Null),
                    item => CheckFeatureEvent(item, fe2, flag2, false, LdValue.Null),
                    item => CheckSummaryEvent(item));
            }
        }

        [Fact]
        public void NonTrackedEventsAreSummarized()
        {
            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
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
                ep.Flush();
                ep.WaitUntilInactive();

                Assert.Collection(captured.Events,
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
        }

        [Fact]
        public void CustomEventIsQueuedWithUser()
        {
            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                CustomEvent e = EventFactory.Default.NewCustomEvent("eventkey", _user, LdValue.Of(3), 1.5);
                ep.SendEvent(e);
                ep.Flush();
                ep.WaitUntilInactive();

                Assert.Collection(captured.Events,
                    item => CheckIndexEvent(item, e, _userJson),
                    item => CheckCustomEvent(item, e, LdValue.Null));
            }
        }
        
        [Fact]
        public void CustomEventCanContainInlineUser()
        {
            _config.InlineUsersInEvents = true;

            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                CustomEvent e = EventFactory.Default.NewCustomEvent("eventkey", _user, LdValue.Of(3), null);
                ep.SendEvent(e);
                ep.Flush();
                ep.WaitUntilInactive();

                Assert.Collection(captured.Events,
                    item => CheckCustomEvent(item, e, _userJson));
            }
        }

        [Fact]
        public void UserDetailsAreScrubbedInCustomEvent()
        {
            _config.AllAttributesPrivate = true;
            _config.InlineUsersInEvents = true;

            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                CustomEvent e = EventFactory.Default.NewCustomEvent("eventkey", _user, LdValue.Of("data"), null);
                ep.SendEvent(e);
                ep.Flush();
                ep.WaitUntilInactive();

                Assert.Collection(captured.Events,
                    item => CheckCustomEvent(item, e, _scrubbedUserJson));
            }
        }

        [Fact]
        public void CustomEventCanHaveNullUser()
        {
            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                CustomEvent e = EventFactory.Default.NewCustomEvent("eventkey", null, LdValue.Of("data"), null);
                ep.SendEvent(e);
                ep.Flush();
                ep.WaitUntilInactive();

                Assert.Collection(captured.Events,
                    item => CheckCustomEvent(item, e, LdValue.Null));
            }
        }

        [Fact]
        public void FinalFlushIsDoneOnDispose()
        {
            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                IdentifyEvent e = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(e);

                ep.Dispose();

                Assert.Collection(captured.Events,
                    item => CheckIdentifyEvent(item, e, _userJson));
                mockSender.Verify(s => s.Dispose(), Times.Once());
            }
        }

        [Fact]
        public void FlushDoesNothingIfThereAreNoEvents()
        {
            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                ep.Flush();
                ep.WaitUntilInactive();

                Assert.Empty(captured.Events);
            }
        }

        [Fact]
        public void FlushDoesNothingWhenOffline()
        {
            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender);

            using (var ep = MakeProcessor(_config, mockSender))
            {
                ep.SetOffline(true);
                IdentifyEvent e = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(e);
                ep.Flush();
                ep.WaitUntilInactive();

                Assert.Empty(captured.Events);

                // We should have still held on to that event, so if we go online again and flush, it is sent.
                ep.SetOffline(false);
                ep.Flush();
                ep.WaitUntilInactive();

                Assert.Collection(captured.Events,
                    item => CheckIdentifyEvent(item, e, _userJson));
            };
        }

        [Fact]
        public void EventsAreStillPostedAfterRecoverableFailure()
        {
            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender,
                new EventSenderResult(DeliveryStatus.Failed, null));
            
            using (var ep = MakeProcessor(_config, mockSender))
            {
                IdentifyEvent ie = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(ie);
                ep.Flush();
                ep.WaitUntilInactive();

                CustomEvent ce = EventFactory.Default.NewCustomEvent("custom", _user, LdValue.Null, null);
                ep.SendEvent(ce);
                ep.Flush();
                ep.WaitUntilInactive();

                Assert.Collection(captured.Events,
                    item => CheckIdentifyEvent(item, ie, _userJson),
                    item => CheckCustomEvent(item, ce, LdValue.Null));
            }
        }

        [Fact]
        public void EventsAreNotPostedAfterUnrecoverableFailure()
        {
            var mockSender = MakeMockSender();
            var captured = EventCapture.From(mockSender,
                new EventSenderResult(DeliveryStatus.FailedAndMustShutDown, null));

            using (var ep = MakeProcessor(_config, mockSender))
            {
                IdentifyEvent ie = EventFactory.Default.NewIdentifyEvent(_user);
                ep.SendEvent(ie);
                ep.Flush();
                ep.WaitUntilInactive();

                CustomEvent ce = EventFactory.Default.NewCustomEvent("custom", _user, LdValue.Null, null);
                ep.SendEvent(ce);
                ep.Flush();
                ep.WaitUntilInactive();

                Assert.Collection(captured.Events,
                    item => CheckIdentifyEvent(item, ie, _userJson));
            }
        }

        [Fact]
        public void EventsInBatchRecorded()
        {
            var expectedStats = LdValue.BuildObject().Add("stats", "testValue").Build();
            var mockDiagnosticStore = MakeDiagnosticStore(null, null, new DiagnosticEvent(expectedStats));

            var mockSender = MakeMockSender();
            var eventCapture = EventCapture.From(mockSender);
            var diagnosticCapture = EventCapture.DiagnosticsFrom(mockSender);
            CountdownEvent diagnosticCountdown = new CountdownEvent(1);

            using (var ep = MakeProcessor(_config, mockSender, mockDiagnosticStore.Object, null, diagnosticCountdown))
            {
                var flag1 = new FlagEventPropertiesBuilder("flagkey1").Version(11).TrackEvents(true).Build();
                var value = LdValue.Of("value");
                var fe1 = EventFactory.Default.NewFeatureRequestEvent(flag1, _user,
                    new EvaluationDetail<LdValue>(value, 1, _irrelevantReason), LdValue.Null);
                ep.SendEvent(fe1);
                FlushAndWait(ep);

                mockDiagnosticStore.Verify(diagStore => diagStore.RecordEventsInBatch(2), Times.Once(), "Diagnostic store's RecordEventsInBatch should be called with the number of events in last flush");

                ep.DoDiagnosticSend(null);
                diagnosticCountdown.Wait();
                mockDiagnosticStore.Verify(diagStore => diagStore.CreateEventAndReset(), Times.Once());

                Assert.Equal(expectedStats, diagnosticCapture.EventsQueue.Take());
            }
        }

        [Fact]
        public void DiagnosticStorePersistedUnsentEventSentToDiagnosticUri()
        {
            var expected = LdValue.BuildObject().Add("testKey", "testValue").Build();
            var mockDiagnosticStore = MakeDiagnosticStore(new DiagnosticEvent(expected), null,
                new DiagnosticEvent(LdValue.Null));

            var mockSender = MakeMockSender();
            var eventCapture = EventCapture.From(mockSender);
            var diagnosticCapture = EventCapture.DiagnosticsFrom(mockSender);
            var diagnosticCountdown = new CountdownEvent(1);

            using (var ep = MakeProcessor(_config, mockSender, mockDiagnosticStore.Object, null, diagnosticCountdown))
            {
                diagnosticCountdown.Wait();

                Assert.Equal(expected, diagnosticCapture.EventsQueue.Take());
            }
        }

        [Fact]
        public void DiagnosticStoreInitEventSentToDiagnosticUri()
        {
            var expected = LdValue.BuildObject().Add("testKey", "testValue").Build();
            var mockDiagnosticStore = MakeDiagnosticStore(null, new DiagnosticEvent(expected),
                new DiagnosticEvent(LdValue.Null));

            var mockSender = MakeMockSender();
            var eventCapture = EventCapture.From(mockSender);
            var diagnosticCapture = EventCapture.DiagnosticsFrom(mockSender);
            var diagnosticCountdown = new CountdownEvent(1);

            using (var ep = MakeProcessor(_config, mockSender, mockDiagnosticStore.Object, null, diagnosticCountdown))
            {
                diagnosticCountdown.Wait();

                Assert.Equal(expected, diagnosticCapture.EventsQueue.Take());
            }
        }

        [Fact]
        public void DiagnosticDisablerDisablesInitialDiagnostics()
        {
            var testDiagnostic = LdValue.BuildObject().Add("testKey", "testValue").Build();
            var mockDiagnosticStore = MakeDiagnosticStore(new DiagnosticEvent(testDiagnostic),
                new DiagnosticEvent(testDiagnostic), new DiagnosticEvent(LdValue.Null));

            var mockDiagnosticDisabler = new Mock<IDiagnosticDisabler>(MockBehavior.Strict);
            mockDiagnosticDisabler.Setup(diagDisabler => diagDisabler.Disabled).Returns(true);

            var mockSender = MakeMockSender();
            var eventCapture = EventCapture.From(mockSender);
            var diagnosticCapture = EventCapture.DiagnosticsFrom(mockSender);

            using (var ep = MakeProcessor(_config, mockSender, mockDiagnosticStore.Object, mockDiagnosticDisabler.Object, null))
            {
            }
            mockDiagnosticStore.Verify(diagStore => diagStore.InitEvent, Times.Never());
            mockDiagnosticStore.Verify(diagStore => diagStore.PersistedUnsentEvent, Times.Never());
            Assert.Empty(diagnosticCapture.Events);
        }

        [Fact]
        public void DiagnosticDisablerEnabledInitialDiagnostics()
        {
            var expectedStats = LdValue.BuildObject().Add("stats", "testValue").Build();
            var expectedInit = LdValue.BuildObject().Add("init", "testValue").Build();
            var mockDiagnosticStore = MakeDiagnosticStore(new DiagnosticEvent(expectedStats),
                new DiagnosticEvent(expectedInit), new DiagnosticEvent(LdValue.Null));

            var mockDiagnosticDisabler = new Mock<IDiagnosticDisabler>(MockBehavior.Strict);
            mockDiagnosticDisabler.Setup(diagDisabler => diagDisabler.Disabled).Returns(false);

            var mockSender = MakeMockSender();
            var eventCapture = EventCapture.From(mockSender);
            var diagnosticCapture = EventCapture.DiagnosticsFrom(mockSender);
            var diagnosticCountdown = new CountdownEvent(1);

            using (var ep = MakeProcessor(_config, mockSender, mockDiagnosticStore.Object, mockDiagnosticDisabler.Object, diagnosticCountdown))
            {
                diagnosticCountdown.Wait();

                Assert.Equal(expectedStats, diagnosticCapture.EventsQueue.Take());
                Assert.Equal(expectedInit, diagnosticCapture.EventsQueue.Take());
            }
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
    }

    class EventCapture
    {
        public readonly List<LdValue> Events = new List<LdValue>();
        public readonly BlockingCollection<LdValue> EventsQueue = new BlockingCollection<LdValue>();

        internal static EventCapture From(Mock<IEventSender> mockSender) =>
            From(mockSender, new EventSenderResult(DeliveryStatus.Succeeded, null));

        internal static EventCapture From(Mock<IEventSender> mockSender, EventSenderResult result) =>
            From(mockSender, EventDataKind.AnalyticsEvents, result);

        internal static EventCapture DiagnosticsFrom(Mock<IEventSender> mockSender) =>
            From(mockSender, EventDataKind.DiagnosticEvent, new EventSenderResult(DeliveryStatus.Succeeded, null));

        internal static EventCapture From(Mock<IEventSender> mockSender, EventDataKind forKind, EventSenderResult result)
        {
            var ec = new EventCapture();
            mockSender.Setup(
                s => s.SendEventDataAsync(forKind, It.IsAny<string>(), It.IsAny<int>())
            ).Callback<EventDataKind, string, int>((kind, data, count) =>
            {
                var parsed = LdValue.Parse(data);
                var events = kind == EventDataKind.DiagnosticEvent ? new List<LdValue> { parsed } :
                    parsed.AsList(LdValue.Convert.Json);
                ec.Events.AddRange(events);
                foreach (var e in events)
                {
                    ec.EventsQueue.Add(e);
                }
            }).Returns(Task.FromResult(result));
            return ec;
        }
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
