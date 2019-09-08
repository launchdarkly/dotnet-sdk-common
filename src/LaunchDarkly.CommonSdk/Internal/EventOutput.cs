using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LaunchDarkly.Client;

namespace LaunchDarkly.Common
{
    // Base class for data structures that we send in an event payload, which are somewhat
    // different in shape from the originating events.  Also defines all of its own subclasses
    // and the class that constructs them.  These are implementation details used only by
    // DefaultEventProcessor and related classes, so they are all internal.
    internal abstract class EventOutput
    {
        [JsonProperty(PropertyName = "kind")]
        internal string Kind { get; set; }
    }

    internal sealed class FeatureRequestEventOutput : EventOutput
    {
        [JsonProperty(PropertyName = "creationDate")]
        internal long CreationDate { get; set; }
        [JsonProperty(PropertyName = "key")]
        internal string Key { get; set; }
        [JsonProperty(PropertyName = "user", NullValueHandling = NullValueHandling.Ignore)]
        internal EventUser User { get; set; }
        [JsonProperty(PropertyName = "userKey", NullValueHandling = NullValueHandling.Ignore)]
        internal string UserKey { get; set; }
        [JsonProperty(PropertyName = "variation", NullValueHandling = NullValueHandling.Ignore)]
        internal int? Variation { get; set; }
        [JsonProperty(PropertyName = "version", NullValueHandling = NullValueHandling.Ignore)]
        internal int? Version { get; set; }
        [JsonProperty(PropertyName = "value")]
        internal ImmutableJsonValue Value { get; set; }
        [JsonProperty(PropertyName = "default", NullValueHandling = NullValueHandling.Ignore)]
        internal ImmutableJsonValue? Default { get; set; }
        [JsonProperty(PropertyName = "prereqOf", NullValueHandling = NullValueHandling.Ignore)]
        internal string PrereqOf { get; set; }
        [JsonProperty(PropertyName = "reason", NullValueHandling = NullValueHandling.Ignore)]
        internal EvaluationReason Reason { get; set; }
    }

    internal sealed class IdentifyEventOutput : EventOutput
    {
        [JsonProperty(PropertyName = "creationDate")]
        internal long CreationDate { get; set; }
        [JsonProperty(PropertyName = "key")]
        internal string Key { get; set; }
        [JsonProperty(PropertyName = "user", NullValueHandling = NullValueHandling.Ignore)]
        internal EventUser User { get; set; }
    }

    internal sealed class CustomEventOutput : EventOutput
    {
        [JsonProperty(PropertyName = "creationDate")]
        internal long CreationDate { get; set; }
        [JsonProperty(PropertyName = "key")]
        internal string Key { get; set; }
        [JsonProperty(PropertyName = "user", NullValueHandling = NullValueHandling.Ignore)]
        internal EventUser User { get; set; }
        [JsonProperty(PropertyName = "userKey", NullValueHandling = NullValueHandling.Ignore)]
        internal string UserKey { get; set; }
        [JsonProperty(PropertyName = "data", NullValueHandling = NullValueHandling.Ignore)]
        internal ImmutableJsonValue? Data { get; set; }
        [JsonProperty(PropertyName = "metricValue", NullValueHandling = NullValueHandling.Ignore)]
        internal double? MetricValue { get; set; }
    }

    internal sealed class IndexEventOutput : EventOutput
    {
        [JsonProperty(PropertyName = "creationDate")]
        internal long CreationDate { get; set; }
        [JsonProperty(PropertyName = "user", NullValueHandling = NullValueHandling.Ignore)]
        internal EventUser User { get; set; }
    }

    internal sealed class SummaryEventOutput : EventOutput
    {
        [JsonProperty(PropertyName = "startDate")]
        internal long StartDate { get; set; }
        [JsonProperty(PropertyName = "endDate")]
        internal long EndDate { get; set; }
        [JsonProperty(PropertyName = "features")]
        internal Dictionary<string, EventSummaryFlag> Features;
    }

    internal sealed class EventSummaryFlag
    {
        [JsonProperty(PropertyName = "default")]
        internal ImmutableJsonValue Default { get; set; }
        [JsonProperty(PropertyName = "counters")]
        internal List<EventSummaryCounter> Counters { get; set; }
    }

    internal sealed class EventSummaryCounter
    {
        [JsonProperty(PropertyName = "variation", NullValueHandling = NullValueHandling.Ignore)]
        internal int? Variation { get; set; }
        [JsonProperty(PropertyName = "value")]
        internal ImmutableJsonValue Value { get; private set; }
        [JsonProperty(PropertyName = "version", NullValueHandling = NullValueHandling.Ignore)]
        internal int? Version { get; private set; }
        [JsonProperty(PropertyName = "count")]
        internal int Count { get; private set; }
        [JsonProperty(PropertyName = "unknown", NullValueHandling = NullValueHandling.Ignore)]
        internal bool? Unknown { get; private set; }

        internal EventSummaryCounter(int? variation, ImmutableJsonValue value, int? version, int count)
        {
            Variation = variation;
            Value = value;
            Version = version;
            Count = count;
            if (version == null)
            {
                Unknown = true;
            }
        }
    }

    internal sealed class EventOutputFormatter
    {
        private readonly IEventProcessorConfiguration _config;

        internal EventOutputFormatter(IEventProcessorConfiguration config)
        {
            _config = config;
        }

        internal List<EventOutput> MakeOutputEvents(Event[] events, EventSummary summary)
        {
            List<EventOutput> eventsOut = new List<EventOutput>(events.Length + 1);
            foreach (Event e in events)
            {
                EventOutput eo = MakeOutputEvent(e);
                if (eo != null)
                {
                    eventsOut.Add(eo);
                }
            }
            if (summary.Counters.Count > 0)
            {
                eventsOut.Add(MakeSummaryEvent(summary));
            }
            return eventsOut;
        }

        private EventUser MaybeInlineUser(User user, bool inline)
        {
            if (inline)
            {
                return user == null ? null : EventUser.FromUser(user, _config);
            }
            return null;
        }

        private string MaybeUserKey(User user, bool inline)
        {
            if (inline)
            {
                return null;
            }
            return user?.Key;
        }

        private EventOutput MakeOutputEvent(Event e)
        {
            switch (e)
            {
                case FeatureRequestEvent fe:
                    bool inlineUser = _config.InlineUsersInEvents || fe.Debug;
                    return new FeatureRequestEventOutput
                    {
                        Kind = fe.Debug ? "debug" : "feature",
                        CreationDate = fe.CreationDate,
                        Key = fe.Key,
                        User = MaybeInlineUser(fe.User, inlineUser),
                        UserKey = MaybeUserKey(fe.User, inlineUser),
                        Version = fe.Version,
                        Variation = fe.Variation,
                        Value = fe.ImmutableJsonValue,
                        // Default is nullable to save a little bandwidth if it's null
                        Default = fe.ImmutableJsonDefault.IsNull ? null : (ImmutableJsonValue?)fe.ImmutableJsonDefault,
                        PrereqOf = fe.PrereqOf,
                        Reason = fe.Reason
                    };
                case IdentifyEvent ie:
                    return new IdentifyEventOutput
                    {
                        Kind = "identify",
                        CreationDate = e.CreationDate,
                        Key = e.User?.Key,
                        User = e.User == null ? null : EventUser.FromUser(e.User, _config)
                    };
                case CustomEvent ce:
                    return new CustomEventOutput
                    {
                        Kind = "custom",
                        CreationDate = ce.CreationDate,
                        Key = ce.Key,
                        User = MaybeInlineUser(ce.User, _config.InlineUsersInEvents),
                        UserKey = MaybeUserKey(ce.User, _config.InlineUsersInEvents),
                        // Data is nullable to save a little bandwidth if it's null
                        Data = ce.ImmutableJsonData.IsNull ? null : (ImmutableJsonValue?)ce.ImmutableJsonData,
                        MetricValue = ce.MetricValue
                    };
                case IndexEvent ie:
                    return new IndexEventOutput
                    {
                        Kind = "index",
                        CreationDate = e.CreationDate,
                        User = EventUser.FromUser(e.User, _config)
                    };
            }
            return null;
        }

        // Transform the summary data into the format used in event sending.
        private SummaryEventOutput MakeSummaryEvent(EventSummary summary)
        {
            Dictionary<string, EventSummaryFlag> flagsOut = new Dictionary<string, EventSummaryFlag>();
            foreach (KeyValuePair<EventsCounterKey, EventsCounterValue> entry in summary.Counters)
            {
                EventSummaryFlag flag;
                if (!flagsOut.TryGetValue(entry.Key.Key, out flag))
                {
                    flag = new EventSummaryFlag
                    {
                        Default = entry.Value.Default,
                        Counters = new List<EventSummaryCounter>()
                    };
                    flagsOut[entry.Key.Key] = flag;
                }
                flag.Counters.Add(new EventSummaryCounter(entry.Key.Variation, entry.Value.FlagValue,
                    entry.Key.Version, entry.Value.Count));
            }
            return new SummaryEventOutput
            {
                Kind = "summary",
                StartDate = summary.StartDate,
                EndDate = summary.EndDate,
                Features = flagsOut
            };
        }
    }
}
