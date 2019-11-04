using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using LaunchDarkly.Client;

namespace LaunchDarkly.Common
{
    internal sealed class EventOutputFormatter
    {
        private readonly IEventProcessorConfiguration _config;

        internal EventOutputFormatter(IEventProcessorConfiguration config)
        {
            _config = config;
        }

        internal string SerializeOutputEvents(Event[] events, EventSummary summary, out int eventCountOut)
        {
            var stringWriter = new StringWriter();
            var scope = new EventOutputFormatterScope(_config, stringWriter, _config.InlineUsersInEvents);
            eventCountOut = scope.WriteOutputEvents(events, summary);
            return stringWriter.ToString();
        }
    }

    internal struct EventOutputFormatterScope
    {
        private readonly IEventProcessorConfiguration _config;
        private readonly JsonWriter _jsonWriter;
        private readonly JsonSerializer _jsonSerializer;

        private struct MutableKeyValuePair<A, B>
        {
            public A Key { get; set; }
            public B Value { get; set; }

            public static MutableKeyValuePair<A, B> FromKeyValue(KeyValuePair<A, B> kv) =>
                new MutableKeyValuePair<A, B> { Key = kv.Key, Value = kv.Value };
        }

        internal EventOutputFormatterScope(IEventProcessorConfiguration config, TextWriter tw, bool inlineUsers)
        {
            _config = config;
            _jsonWriter = new JsonTextWriter(tw);
            _jsonSerializer = new JsonSerializer();
        }

        internal int WriteOutputEvents(Event[] events, EventSummary summary)
        {
            var eventCount = 0;
            _jsonWriter.WriteStartArray();
            foreach (Event e in events)
            {
                if (WriteOutputEvent(e))
                {
                    eventCount++;
                }
            }
            if (summary.Counters.Count > 0)
            {
                WriteSummaryEvent(summary);
                eventCount++;
            }
            _jsonWriter.WriteEndArray();
            _jsonWriter.Flush();
            return eventCount;
        }

        private bool WriteOutputEvent(Event e)
        {
            switch (e)
            {
                case FeatureRequestEvent fe:
                    WithBaseObject(fe.Debug ? "debug" : "feature", fe.CreationDate, fe.Key, me =>
                    {
                        me.WriteUserOrKey(fe.User, fe.Debug);
                        if (fe.Version.HasValue)
                        {
                            me._jsonWriter.WritePropertyName("version");
                            me._jsonWriter.WriteValue(fe.Version.Value);
                        }
                        if (fe.Variation.HasValue)
                        {
                            me._jsonWriter.WritePropertyName("variation");
                            me._jsonWriter.WriteValue(fe.Variation.Value);
                        }
                        me._jsonWriter.WritePropertyName("value");
                        LdValueSerializer.Instance.WriteJson(me._jsonWriter, fe.Value, me._jsonSerializer);
                        if (!fe.Default.IsNull)
                        {
                            me._jsonWriter.WritePropertyName("default");
                            LdValueSerializer.Instance.WriteJson(me._jsonWriter, fe.Default, me._jsonSerializer);
                        }
                        me.MaybeWriteString("prereqOf", fe.PrereqOf);
                        me.WriteReason(fe.Reason);
                    });
                    break;
                case IdentifyEvent ie:
                    WithBaseObject("identify", ie.CreationDate, e.User?.Key, me =>
                    {
                        me.WriteUser(ie.User);
                    });
                    break;
                case CustomEvent ce:
                    WithBaseObject("custom", ce.CreationDate, ce.Key, me =>
                    {
                        me.WriteUserOrKey(ce.User, false);
                        if (!ce.Data.IsNull)
                        {
                            me._jsonWriter.WritePropertyName("data");
                            LdValueSerializer.Instance.WriteJson(me._jsonWriter, ce.Data, me._jsonSerializer);
                        }
                        if (ce.MetricValue.HasValue)
                        {
                            me._jsonWriter.WritePropertyName("metricValue");
                            me._jsonWriter.WriteValue(ce.MetricValue.Value);
                        }
                    });
                    break;
                case IndexEvent ie:
                    WithBaseObject("index", ie.CreationDate, null, me =>
                    {
                        me.WriteUserOrKey(ie.User, true);
                    });
                    break;
                default:
                    return false;
                }
            return true;
        }
        
        private void WriteSummaryEvent(EventSummary summary)
        {
            _jsonWriter.WriteStartObject();

            _jsonWriter.WritePropertyName("kind");
            _jsonWriter.WriteValue("summary");
            _jsonWriter.WritePropertyName("startDate");
            _jsonWriter.WriteValue(summary.StartDate);
            _jsonWriter.WritePropertyName("endDate");
            _jsonWriter.WriteValue(summary.EndDate);

            _jsonWriter.WritePropertyName("features");
            _jsonWriter.WriteStartObject();

            var unprocessedCounters = summary.Counters.Select(kv => MutableKeyValuePair<EventsCounterKey, EventsCounterValue>.FromKeyValue(kv)).ToArray();
            for (var i = 0; i < unprocessedCounters.Length; i++)
            {
                var firstEntry = unprocessedCounters[i];
                if (firstEntry.Value is null)
                { // already processed
                    continue;
                }
                var flagKey = firstEntry.Key.Key;
                var flagDefault = firstEntry.Value.Default;

                _jsonWriter.WritePropertyName(flagKey);
                _jsonWriter.WriteStartObject();
                _jsonWriter.WritePropertyName("default");
                LdValueSerializer.Instance.WriteJson(_jsonWriter, flagDefault, _jsonSerializer);
                _jsonWriter.WritePropertyName("counters");
                _jsonWriter.WriteStartArray();

                for (var j = i; j < unprocessedCounters.Length; j++)
                {
                    var entry = unprocessedCounters[j];
                    var key = entry.Key;
                    if (key.Key == flagKey && entry.Value != null)
                    {
                        var counter = entry.Value;
                        unprocessedCounters[j].Value = null; // mark as already processed

                        _jsonWriter.WriteStartObject();
                        if (key.Variation.HasValue)
                        {
                            _jsonWriter.WritePropertyName("variation");
                            _jsonWriter.WriteValue(key.Variation.Value);
                        }
                        else
                        {
                            _jsonWriter.WritePropertyName("unknown");
                            _jsonWriter.WriteValue(true);
                        }
                        _jsonWriter.WritePropertyName("value");
                        LdValueSerializer.Instance.WriteJson(_jsonWriter, counter.FlagValue, _jsonSerializer);
                        if (key.Version.HasValue)
                        {
                            _jsonWriter.WritePropertyName("version");
                            _jsonWriter.WriteValue(key.Version.Value);
                        }
                        _jsonWriter.WritePropertyName("count");
                        _jsonWriter.WriteValue(counter.Count);
                        _jsonWriter.WriteEndObject();
                    }
                }

                _jsonWriter.WriteEndArray();
                _jsonWriter.WriteEndObject();
            }

            _jsonWriter.WriteEndObject();

            _jsonWriter.WriteEndObject();
        }

        private void MaybeWriteString(string name, string value)
        {
            if (value != null)
            {
                _jsonWriter.WritePropertyName(name);
                _jsonWriter.WriteValue(value);
            }
        }

        private void WithBaseObject(string kind, long creationDate, string key, Action<EventOutputFormatterScope> moreActions)
        {
            _jsonWriter.WriteStartObject();
            _jsonWriter.WritePropertyName("kind");
            _jsonWriter.WriteValue(kind);
            _jsonWriter.WritePropertyName("creationDate");
            _jsonWriter.WriteValue(creationDate);
            MaybeWriteString("key", key);
            moreActions(this);
            _jsonWriter.WriteEndObject();
        }

        private void WriteUserOrKey(User user, bool forceInline)
        {
            if (forceInline || _config.InlineUsersInEvents)
            {
                WriteUser(user);
            }
            else if (user != null)
            {
                _jsonWriter.WritePropertyName("userKey");
                _jsonWriter.WriteValue(user.Key);
            }
        }

        private void WriteUser(User user)
        {
            if (user is null)
            {
                return;
            }
            var eu = EventUser.FromUser(user, _config);
            _jsonWriter.WritePropertyName("user");
            _jsonWriter.WriteStartObject();
            MaybeWriteString("key", eu.Key);
            MaybeWriteString("secondary", eu.SecondaryKey);
            MaybeWriteString("ip", eu.IPAddress);
            MaybeWriteString("country", eu.Country);
            MaybeWriteString("firstName", eu.FirstName);
            MaybeWriteString("lastName", eu.LastName);
            MaybeWriteString("name", eu.Name);
            MaybeWriteString("avatar", eu.Avatar);
            MaybeWriteString("email", eu.Email);
            if (eu.Anonymous.HasValue)
            {
                _jsonWriter.WritePropertyName("anonymous");
                _jsonWriter.WriteValue(eu.Anonymous.Value);
            }
            if (eu.Custom != null)
            {
                _jsonWriter.WritePropertyName("custom");
                _jsonWriter.WriteStartObject();
                foreach (var kv in eu.Custom)
                {
                    _jsonWriter.WritePropertyName(kv.Key);
                    LdValueSerializer.Instance.WriteJson(_jsonWriter, kv.Value, _jsonSerializer);
                }
                _jsonWriter.WriteEndObject();
            }
            if (eu.PrivateAttrs != null)
            {
                _jsonWriter.WritePropertyName("privateAttrs");
                _jsonWriter.WriteStartArray();
                foreach (var a in eu.PrivateAttrs)
                {
                    _jsonWriter.WriteValue(a);
                }
                _jsonWriter.WriteEndArray();
            }
            _jsonWriter.WriteEndObject();
        }

        private void WriteReason(EvaluationReason reason)
        {
            if (reason is null)
            {
                return;
            }
            _jsonWriter.WritePropertyName("reason");
            _jsonWriter.WriteStartObject();
            _jsonWriter.WritePropertyName("kind");
            _jsonWriter.WriteValue(reason.Kind.ToString());
            switch (reason)
            {
                case EvaluationReason.Error e:
                    _jsonWriter.WritePropertyName("errorKind");
                    _jsonWriter.WriteValue(e.ErrorKind.ToString());
                    break;
                case EvaluationReason.RuleMatch rm:
                    _jsonWriter.WritePropertyName("ruleIndex");
                    _jsonWriter.WriteValue(rm.RuleIndex);
                    break;
                case EvaluationReason.PrerequisiteFailed pf:
                    _jsonWriter.WritePropertyName("prerequisiteKey");
                    _jsonWriter.WriteValue(pf.PrerequisiteKey);
                    break;
                default:
                    break;
            }
            _jsonWriter.WriteEndObject();
        }
    }
}
