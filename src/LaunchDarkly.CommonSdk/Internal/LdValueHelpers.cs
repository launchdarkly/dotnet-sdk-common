using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Common
{
    internal class LdValueSerializer : JsonConverter
    {
        // For values of primitive types that were not created from an existing JToken, this logic will
        // serialize them directly to JSON without ever allocating a JToken.
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is LdValue jv)
            {
                if (jv.HasWrappedJToken)
                {
                    jv.InnerValue.WriteTo(writer);
                }
                else
                {
                    switch (jv.Type)
                    {
                        case LdValueType.Null:
                            writer.WriteNull();
                            break;
                        case LdValueType.Bool:
                            writer.WriteValue(jv.AsBool);
                            break;
                        case LdValueType.Number:
                            if (jv.IsInt)
                            {
                                writer.WriteValue(jv.AsInt);
                            }
                            else
                            {
                                writer.WriteValue(jv.AsFloat);
                            }
                            break;
                        case LdValueType.String:
                            writer.WriteValue(jv.AsString);
                            break;
                        case LdValueType.Array:
                            writer.WriteStartArray();
                            foreach (var v in jv.AsList(LdValue.Convert.Json))
                            {
                                WriteJson(writer, v, serializer);
                            }
                            writer.WriteEndArray();
                            break;
                        case LdValueType.Object:
                            writer.WriteStartObject();
                            foreach (var kv in jv.AsDictionary(LdValue.Convert.Json))
                            {
                                writer.WritePropertyName(kv.Key);
                                WriteJson(writer, kv.Value, serializer);
                            }
                            writer.WriteEndObject();
                            break;
                        default:
                            // this shouldn't happen since all non-primitive types should have a JToken
                            writer.WriteNull();
                            break;
                    }
                }
            }
        }

        // Currently we always preserve the value in a JToken when parsing JSON. As long as JToken is exposed
        // in the public API, there would be no point in using our own primitive value mechanism because we
        // might need to get it as a JToken later at which point we'd be recreating that object. Once we are
        // completely avoiding JToken conversions, we can change this method to discard the JToken for
        // primitive types.
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return LdValue.FromSafeValue(JToken.Load(reader));
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(LdValue);
        }
    }

    // This struct wraps an existing JArray and makes it behave as an IReadOnlyList, with
    // transparent value conversion.
    internal struct LdValueListConverter<T, U> : IReadOnlyList<U>
    {
        private readonly IList<T> _source;
        private readonly Func<T, U> _converter;

        internal LdValueListConverter(IList<T> source, Func<T, U> converter)
        {
            _source = source;
            _converter = converter;
        }

        public U this[int index]
        {
            get
            {
                if (_source is null || index < 0 || index >= _source.Count)
                {
                    throw new IndexOutOfRangeException();
                }
                return _converter(_source[index]);
            }
        }

        public int Count => _source is null ? 0 : _source.Count;

        public IEnumerator<U> GetEnumerator() =>
            _source is null ? Enumerable.Empty<U>().GetEnumerator() :
                _source.Select<T, U>(_converter).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return "[" + string.Join(",", this) + "]";
        }
    }

    // This struct wraps an existing JObject and makes it behave as an IReadOnlyDictionary, with
    // transparent value conversion.
    internal struct LdValueObjectConverter<T, U> : IReadOnlyDictionary<string, U>
    {
        private readonly IDictionary<string, T> _source;
        private readonly Func<T, U> _converter;

        internal LdValueObjectConverter(IDictionary<string, T> source, Func<T, U> converter)
        {
            _source = source;
            _converter = converter;
        }

        public U this[string key]
        {
            get
            {
                // Note that JObject[key] does *not* throw a KeyNotFoundException, but we should
                if (_source is null || !_source.TryGetValue(key, out var v))
                {
                    throw new KeyNotFoundException();
                }
                return _converter(v);
            }
        }

        public IEnumerable<string> Keys =>
            _source is null ? Enumerable.Empty<string>() : _source.Keys;

        public IEnumerable<U> Values =>
            _source is null ? Enumerable.Empty<U>() :
                _source.Values.Select(_converter);

        public int Count => _source is null ? 0 : _source.Count;

        public bool ContainsKey(string key) =>
            !(_source is null) && _source.TryGetValue(key, out var ignore);

        public IEnumerator<KeyValuePair<string, U>> GetEnumerator()
        {
            if (_source is null)
            {
                return Enumerable.Empty<KeyValuePair<string, U>>().GetEnumerator();
            }
            var conv = _converter; // lambda can't use instance field
            return _source.Select<KeyValuePair<string, T>, KeyValuePair<string, U>>(
                p => new KeyValuePair<string, U>(p.Key, conv(p.Value))
                ).GetEnumerator();
        }

        public bool TryGetValue(string key, out U value)
        {
            if (!(_source is null) && _source.TryGetValue(key, out var v))
            {
                value = _converter(v);
                return true;
            }
            value = default(U);
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return "{" +
                string.Join(",", this.Select(kv => "\"" + kv.Key + "\":" + kv.Value)) +
                "}";
        }
    }
}
