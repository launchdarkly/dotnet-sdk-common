using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Common
{
    internal class ImmutableJsonValueSerializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is ImmutableJsonValue jv)
            {
                if (jv.InnerValue is null)
                {
                    writer.WriteNull();
                }
                else
                {
                    jv.InnerValue.WriteTo(writer);
                }
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return ImmutableJsonValue.FromSafeValue(JToken.Load(reader));
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ImmutableJsonValue);
        }
    }

    // This struct wraps an existing JArray and makes it behave as an IReadOnlyList, with
    // transparent value conversion.
    internal struct ImmutableJsonArrayConverter<T> : IReadOnlyList<T>
    {
        private readonly JArray _array;
        private readonly Func<ImmutableJsonValue, T> _converter;

        internal ImmutableJsonArrayConverter(JArray array, Func<ImmutableJsonValue, T> converter)
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
                return _converter(ImmutableJsonValue.FromSafeValue(_array[index]));
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
            return _array.Select<JToken, T>(v => conv(ImmutableJsonValue.FromSafeValue(v))).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    // This struct wraps an existing JObject and makes it behave as an IReadOnlyDictionary, with
    // transparent value conversion.
    internal struct ImmutableJsonObjectConverter<T> : IReadOnlyDictionary<string, T>
    {
        private readonly JObject _object;
        private readonly Func<ImmutableJsonValue, T> _converter;

        internal ImmutableJsonObjectConverter(JObject o, Func<ImmutableJsonValue, T> converter)
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
                return _converter(ImmutableJsonValue.FromSafeValue(v));
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
                return _object.Properties().Select(p => conv(ImmutableJsonValue.FromSafeValue(p.Value)));
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
                p => new KeyValuePair<string, T>(p.Name, conv(ImmutableJsonValue.FromSafeValue(p.Value)))
                ).GetEnumerator();
        }

        public bool TryGetValue(string key, out T value)
        {
            if (!(_object is null) && _object.TryGetValue(key, out var v))
            {
                value = _converter(ImmutableJsonValue.FromSafeValue(v));
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
