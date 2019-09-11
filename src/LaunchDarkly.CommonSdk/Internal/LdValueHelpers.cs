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
                        case JsonValueType.Null:
                            writer.WriteNull();
                            break;
                        case JsonValueType.Bool:
                            writer.WriteValue(jv.AsBool);
                            break;
                        case JsonValueType.Number:
                            if (jv.IsInt)
                            {
                                writer.WriteValue(jv.AsInt);
                            }
                            else
                            {
                                writer.WriteValue(jv.AsFloat);
                            }
                            break;
                        case JsonValueType.String:
                            writer.WriteValue(jv.AsString);
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
    internal struct LdValueArrayConverter<T> : IReadOnlyList<T>
    {
        private readonly JArray _array;
        private readonly Func<LdValue, T> _converter;

        internal LdValueArrayConverter(JArray array, Func<LdValue, T> converter)
        {
            _array = array;
            _converter = converter;
        }

        public T this[int index]
        {
            get
            {
                if (_array is null || index < 0 || index >= _array.Count)
                {
                    throw new IndexOutOfRangeException();
                }
                return _converter(LdValue.FromSafeValue(_array[index]));
            }
        }

        public int Count => _array is null ? 0 : _array.Count;

        public IEnumerator<T> GetEnumerator()
        {
            if (_array is null)
            {
                return Enumerable.Empty<T>().GetEnumerator();
            }
            var conv = _converter;
            return _array.Select<JToken, T>(v => conv(LdValue.FromSafeValue(v))).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    // This struct wraps an existing JObject and makes it behave as an IReadOnlyDictionary, with
    // transparent value conversion.
    internal struct LdValueObjectConverter<T> : IReadOnlyDictionary<string, T>
    {
        private readonly JObject _object;
        private readonly Func<LdValue, T> _converter;

        internal LdValueObjectConverter(JObject o, Func<LdValue, T> converter)
        {
            _object = o;
            _converter = converter;
        }

        public T this[string key]
        {
            get
            {
                // Note that JObject[key] does *not* throw a KeyNotFoundException, but we should
                if (_object is null || !_object.TryGetValue(key, out var v))
                {
                    throw new KeyNotFoundException();
                }
                return _converter(LdValue.FromSafeValue(v));
            }
        }

        public IEnumerable<string> Keys =>
            _object is null ? Enumerable.Empty<string>() :
            _object.Properties().Select(p => p.Name);

        public IEnumerable<T> Values
        {
            get
            {
                if (_object is null)
                {
                    return Enumerable.Empty<T>();
                }
                var conv = _converter; // lambda can't use instance field
                return _object.Properties().Select(p => conv(LdValue.FromSafeValue(p.Value)));
            }
        }

        public int Count => _object is null ? 0 : _object.Count;

        public bool ContainsKey(string key) =>
            !(_object is null) && _object.TryGetValue(key, out var ignore);

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            if (_object is null)
            {
                return Enumerable.Empty<KeyValuePair<string, T>>().GetEnumerator();
            }
            var conv = _converter; // lambda can't use instance field
            return _object.Properties().Select<JProperty, KeyValuePair<string, T>>(
                p => new KeyValuePair<string, T>(p.Name, conv(LdValue.FromSafeValue(p.Value)))
                ).GetEnumerator();
        }

        public bool TryGetValue(string key, out T value)
        {
            if (!(_object is null) && _object.TryGetValue(key, out var v))
            {
                value = _converter(LdValue.FromSafeValue(v));
                return true;
            }
            value = default(T);
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
