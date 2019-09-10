using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Xunit;
using LaunchDarkly.Client;

namespace LaunchDarkly.Common.Tests
{
    public class EventSummarizerTest
    {
        private static readonly User _user = User.WithKey("key");
        private static readonly EventFactory _eventFactory = new EventFactory(() => 1000, false);

        [Fact]
        public void SummarizeEventDoesNothingForIdentifyEvent()
        {
            EventSummarizer es = new EventSummarizer();
            EventSummary snapshot = es.Snapshot();
            es.SummarizeEvent(_eventFactory.NewIdentifyEvent(_user));
            EventSummary snapshot2 = es.Snapshot();
            Assert.Equal(snapshot.StartDate, snapshot2.StartDate);
            Assert.Equal(snapshot.EndDate, snapshot2.EndDate);
            Assert.Equal(snapshot.Counters, snapshot2.Counters);
        }

        [Fact]
        public void SummarizeEventDoesNothingForCustomEvent()
        {
            EventSummarizer es = new EventSummarizer();
            EventSummary snapshot = es.Snapshot();
            es.SummarizeEvent(_eventFactory.NewCustomEvent("whatever", _user, ImmutableJsonValue.Null, null));
            EventSummary snapshot2 = es.Snapshot();
            Assert.Equal(snapshot.StartDate, snapshot2.StartDate);
            Assert.Equal(snapshot.EndDate, snapshot2.EndDate);
            Assert.Equal(snapshot.Counters, snapshot2.Counters);
        }

        [Fact]
        public void SummarizeEventSetsStartAndEndDates()
        {
            EventSummarizer es = new EventSummarizer();
            IFlagEventProperties flag = new FlagEventPropertiesBuilder("key").Build();
            var nullResult = new EvaluationDetail<ImmutableJsonValue>(ImmutableJsonValue.Null, null, null);
            var factory1 = new EventFactory(() => 2000, false);
            Event event1 = factory1.NewFeatureRequestEvent(flag, _user, nullResult, ImmutableJsonValue.Null);
            var factory2 = new EventFactory(() => 1000, false);
            Event event2 = factory2.NewFeatureRequestEvent(flag, _user, nullResult, ImmutableJsonValue.Null);
            var factory3 = new EventFactory(() => 1500, false);
            Event event3 = factory3.NewFeatureRequestEvent(flag, _user, nullResult, ImmutableJsonValue.Null);
            es.SummarizeEvent(event1);
            es.SummarizeEvent(event2);
            es.SummarizeEvent(event3);
            EventSummary data = es.Snapshot();

            Assert.Equal(1000, data.StartDate);
            Assert.Equal(2000, data.EndDate);
        }

        [Fact]
        public void SummarizeEventIncrementsCounters()
        {
            EventSummarizer es = new EventSummarizer();
            IFlagEventProperties flag1 = new FlagEventPropertiesBuilder("key1").Build();
            IFlagEventProperties flag2 = new FlagEventPropertiesBuilder("key2").Build();
            string unknownFlagKey = "badkey";
            var default1 = ImmutableJsonValue.Of("default1");
            var default2 = ImmutableJsonValue.Of("default2");
            var default3 = ImmutableJsonValue.Of("default3");
            Event event1 = _eventFactory.NewFeatureRequestEvent(flag1, _user,
                new EvaluationDetail<ImmutableJsonValue>(ImmutableJsonValue.Of("value1"), 1, null), default1);
            Event event2 = _eventFactory.NewFeatureRequestEvent(flag1, _user,
                new EvaluationDetail<ImmutableJsonValue>(ImmutableJsonValue.Of("value2"), 2, null), default1);
            Event event3 = _eventFactory.NewFeatureRequestEvent(flag2, _user,
                new EvaluationDetail<ImmutableJsonValue>(ImmutableJsonValue.Of("value99"), 1, null), default2);
            Event event4 = _eventFactory.NewFeatureRequestEvent(flag1, _user,
                new EvaluationDetail<ImmutableJsonValue>(ImmutableJsonValue.Of("value1"), 1, null), default1);
            Event event5 = _eventFactory.NewUnknownFeatureRequestEvent(unknownFlagKey, _user, default3, EvaluationErrorKind.FLAG_NOT_FOUND);
            es.SummarizeEvent(event1);
            es.SummarizeEvent(event2);
            es.SummarizeEvent(event3);
            es.SummarizeEvent(event4);
            es.SummarizeEvent(event5);
            EventSummary data = es.Snapshot();

            Dictionary<EventsCounterKey, EventsCounterValue> expected = new Dictionary<EventsCounterKey, EventsCounterValue>();
            Assert.Equal(new EventsCounterValue(2, ImmutableJsonValue.Of("value1"), default1),
                data.Counters[new EventsCounterKey(flag1.Key, flag1.EventVersion, 1)]);
            Assert.Equal(new EventsCounterValue(1, ImmutableJsonValue.Of("value2"), default1),
                data.Counters[new EventsCounterKey(flag1.Key, flag1.EventVersion, 2)]);
            Assert.Equal(new EventsCounterValue(1, ImmutableJsonValue.Of("value99"), default2),
                data.Counters[new EventsCounterKey(flag2.Key, flag2.EventVersion, 1)]);
            Assert.Equal(new EventsCounterValue(1, default3, default3),
                data.Counters[new EventsCounterKey(unknownFlagKey, null, null)]);
        }
    }
}
