using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Client
{
    /// <summary>
    /// Describes the type of a JSON value.
    /// </summary>
    public enum JsonValueType
    {
        /// <summary>
        /// The value is null.
        /// </summary>
        Null,
        /// <summary>
        /// The value is a boolean.
        /// </summary>
        Bool,
        /// <summary>
        /// The value is numeric. JSON does not have separate types for int and float,
        /// but you can convert to either.
        /// </summary>
        Number,
        /// <summary>
        /// The value is a string.
        /// </summary>
        String,
        /// <summary>
        /// The value is an array.
        /// </summary>
        Array,
        /// <summary>
        /// The value is an object (dictionary).
        /// </summary>
        Object
    }

    /// <summary>
    /// An immutable instance of any data type that is allowed in JSON.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is used as the return type of the client's JsonVariation method, and also as
    /// the type of custom attributes in <see cref="User"/> and <see cref="IUserBuilder"/>.
    /// </para>
    /// <para>
    /// While the LaunchDarkly SDK uses <see cref="Newtonsoft.Json"/> types to represent JSON values,
    /// some of those types (object and array) are mutable. In contexts where it is
    /// important for data to remain immutable after it is created, these values are
    /// represented with <see cref="ImmutableJsonValue"/> instead. It is easily convertible
    /// to primitive types and array/dictionary structures.
    /// </para>
    /// </remarks>
    [JsonConverter(typeof(ImmutableJsonValueSerializer))]
    public struct ImmutableJsonValue : IEquatable<ImmutableJsonValue>
    {
        private static readonly JToken _jsonFalse = new JValue(false);
        private static readonly JToken _jsonTrue = new JValue(true);
        private static readonly JToken _jsonIntZero = new JValue(0);
        private static readonly JToken _jsonFloatZero = new JValue(0);
        private static readonly JToken _jsonStringEmpty = new JValue("");

        private static readonly ImmutableJsonValue _falseInstance = new ImmutableJsonValue(new JValue(false));
        private static readonly ImmutableJsonValue _trueInstance = new ImmutableJsonValue(new JValue(true));
        private static readonly ImmutableJsonValue _intZeroInstance = new ImmutableJsonValue(new JValue(0));
        private static readonly ImmutableJsonValue _floatZeroInstance = new ImmutableJsonValue(new JValue(0f));
        private static readonly ImmutableJsonValue _stringEmptyInstance = new ImmutableJsonValue(new JValue(""));

        private readonly JToken _value;

        /// <summary>
        /// Convenience property for an <see cref="ImmutableJsonValue"/> that wraps a <see langword="null"/> value.
        /// </summary>
        public static ImmutableJsonValue Null => new ImmutableJsonValue(null);

        private ImmutableJsonValue(JToken value)
        {
            _value = NormalizePrimitives(value);
        }

        /// <summary>
        /// For internal use only. Directly accesses the wrapped value.
        /// </summary>
        /// <remarks>
        /// This internal method is used for efficiency only during flag evaluation or JSON serialization,
        /// where we know we will not be modifying any mutable objects or arrays and we will not be
        /// exposing the value to any external code.
        /// </remarks>
        internal JToken InnerValue => _value;

        /// <summary>
        /// For internal use only. Initializes an <see cref="ImmutableJsonValue"/> from an arbitrary JSON
        /// value that we know will not be modified.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is to be used internally when the SDK has a JToken instance that has not been
        /// exposed to any application code, and the SDK code is never going to call any mutative
        /// methods on that value. In that case, we do not need to perform a deep copy on the value
        /// just to wrap it in an <see cref="ImmutableJsonValue"/>; a deep copy will be performed anyway
        /// if the application tries to access the JToken.
        /// </para>
        /// <para>
        /// It also performs minor optimizations by using our static JToken instances for true, 0, etc.
        /// </para>
        /// </remarks>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        internal static ImmutableJsonValue FromSafeValue(JToken value) => new ImmutableJsonValue(value);

        private static JToken NormalizePrimitives(JToken value)
        {
            if (!(value is null))
            {
                switch (value.Type)
                {
                    case JTokenType.Boolean:
                        return JValueFromBool(value.Value<bool>());
                    case JTokenType.Integer when value.Value<int>() == 0:
                        return _jsonIntZero;
                    case JTokenType.Float when value.Value<float>() == 0f:
                        return _jsonFloatZero;
                    case JTokenType.String when value.Value<string>().Length == 0:
                        return _jsonStringEmpty;
                    case JTokenType.Null:
                        // Newtonsoft.Json sometimes gives us real nulls and sometimes gives us "nully" objects.
                        // Normalize these to null.
                        return null;
                }
            }
            return value;
        }
        
        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> from a boolean value.
        /// </summary>
        /// <remarks>
        /// This method reuses static instances for <see langword="true"/> and <see langword="false"/>,
        /// so it will never create new objects.
        /// </remarks>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static ImmutableJsonValue Of(bool value) => new ImmutableJsonValue(JValueFromBool(value));

        private static JToken JValueFromBool(bool value) => value ? _jsonTrue : _jsonFalse;

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> from an integer value.
        /// </summary>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static ImmutableJsonValue Of(int value) => new ImmutableJsonValue(JValueFromInt(value));

        private static JToken JValueFromInt(int value) => value == 0 ? _jsonIntZero : new JValue(value);

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> from a float value.
        /// </summary>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static ImmutableJsonValue Of(float value) => new ImmutableJsonValue(JValueFromFloat(value));

        private static JToken JValueFromFloat(float value) => value == 0 ? _jsonFloatZero : new JValue(value);

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> from a string value.
        /// </summary>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static ImmutableJsonValue Of(string value) => new ImmutableJsonValue(JValueFromString(value));

        private static JToken JValueFromString(string value) =>
            value is null ? null : (value.Length == 0 ? _jsonStringEmpty : new JValue(value));

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as an array, from a sequence of booleans.
        /// </summary>
        /// <param name="arrayValue">a sequence of booleans</param>
        /// <returns>a struct representing a JSON array</returns>
        public static ImmutableJsonValue FromValues(IEnumerable<bool> arrayValue) =>
            FromEnumerable(arrayValue, JValueFromBool);

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as an array, from a sequence of ints.
        /// </summary>
        /// <param name="arrayValue">a sequence of ints</param>
        /// <returns>a struct representing a JSON array</returns>
        public static ImmutableJsonValue FromValues(IEnumerable<int> arrayValue) =>
            FromEnumerable(arrayValue, JValueFromInt);

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as an array, from a sequence of floats.
        /// </summary>
        /// <param name="arrayValue">a sequence of floats</param>
        /// <returns>a struct representing a JSON array</returns>
        public static ImmutableJsonValue FromValues(IEnumerable<float> arrayValue) =>
            FromEnumerable(arrayValue, JValueFromFloat);

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> from a sequence of strings.
        /// </summary>
        /// <param name="arrayValue">a sequence of strings</param>
        /// <returns>a struct representing a JSON array</returns>
        public static ImmutableJsonValue FromValues(IEnumerable<string> arrayValue) =>
            FromEnumerable(arrayValue, JValueFromString);

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as an array, from a sequence of JSON values.
        /// </summary>
        /// <param name="arrayValue">a sequence of values</param>
        /// <returns>a struct representing a JSON array</returns>
        public static ImmutableJsonValue FromValues(IEnumerable<ImmutableJsonValue> arrayValue) =>
            FromEnumerable(arrayValue, v => v.InnerValue);

        private static ImmutableJsonValue FromEnumerable<T>(IEnumerable<T> values, Func<T, JToken> convert)
        {
            var a = new JArray();
            foreach (var item in values)
            {
                a.Add(convert(item));
            }
            return ImmutableJsonValue.FromSafeValue(a);
        }

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as a JSON object, from a dictionary
        /// containing booleans.
        /// </summary>
        /// <param name="dictionary">a dictionary of strings to booleans</param>
        /// <returns>a struct representing a JSON object</returns>
        public static ImmutableJsonValue FromDictionary(IReadOnlyDictionary<string, bool> dictionary) =>
            FromDictionaryInternal(dictionary, JValueFromBool);

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as a JSON object, from a dictionary
        /// containing ints.
        /// </summary>
        /// <param name="dictionary">a dictionary of strings to ints</param>
        /// <returns>a struct representing a JSON object</returns>
        public static ImmutableJsonValue FromDictionary(IReadOnlyDictionary<string, int> dictionary) =>
            FromDictionaryInternal(dictionary, JValueFromInt);

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as a JSON object, from a dictionary
        /// containing floats.
        /// </summary>
        /// <param name="dictionary">a dictionary of strings to floats</param>
        /// <returns>a struct representing a JSON object</returns>
        public static ImmutableJsonValue FromDictionary(IReadOnlyDictionary<string, float> dictionary) =>
            FromDictionaryInternal(dictionary, JValueFromFloat);

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as a JSON object, from a dictionary
        /// containing strings.
        /// </summary>
        /// <param name="dictionary">a dictionary of strings to strings</param>
        /// <returns>a struct representing a JSON object</returns>
        public static ImmutableJsonValue FromDictionary(IReadOnlyDictionary<string, string> dictionary) =>
            FromDictionaryInternal(dictionary, JValueFromString);

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as a JSON object, from a dictionary
        /// containing JSON values.
        /// </summary>
        /// <param name="dictionary">a dictionary of strings to JSON values</param>
        /// <returns>a struct representing a JSON object</returns>
        public static ImmutableJsonValue FromDictionary(IReadOnlyDictionary<string, ImmutableJsonValue> dictionary) =>
            FromDictionaryInternal(dictionary, v => v.InnerValue);

        private static ImmutableJsonValue FromDictionaryInternal<T>(IReadOnlyDictionary<string, T> dictionary,
            Func<T, JToken> convert)
        {
            var o = new JObject();
            foreach (var e in dictionary)
            {
                o.Add(e.Key, convert(e.Value));
            }
            return ImmutableJsonValue.FromSafeValue(o);
        }

        /// <summary>
        /// The type of the JSON value.
        /// </summary>
        public JsonValueType Type
        {
            get
            {
                if (!(_value is null))
                {
                    switch (_value.Type)
                    {
                        case JTokenType.Boolean:
                            return JsonValueType.Bool;
                        case JTokenType.Integer:
                        case JTokenType.Float:
                            return JsonValueType.Number;
                        case JTokenType.String:
                            return JsonValueType.String;
                        case JTokenType.Array:
                            return JsonValueType.Array;
                        case JTokenType.Object:
                            return JsonValueType.Object;
                    }
                }
                return JsonValueType.Null;
            }
        }

        /// <summary>
        /// True if the wrapped value is <see langword="null"/>.
        /// </summary>
        public bool IsNull => _value is null || _value.Type == JTokenType.Null;

        /// <summary>
        /// True if the wrapped value is numeric.
        /// </summary>
        public bool IsNumber => !(_value is null) && (_value.Type == JTokenType.Integer || _value.Type == JTokenType.Float);

        /// <summary>
        /// Converts the value to a boolean.
        /// </summary>
        /// <remarks>
        /// If the value is <see langword="null"/> or is not a boolean, this returns <see langword="false"/>.
        /// It will never throw an exception.
        /// </remarks>
        public bool AsBool => (_value is null || _value.Type != JTokenType.Boolean) ? false : _value.Value<bool>();

        /// <summary>
        /// Converts the value to a string.
        /// </summary>
        /// <remarks>
        /// If the value is <see langword="null"/>, this returns <see langword="null"/>. If the value is of a
        /// non-string type, it is converted to a string. It will never throw an exception.
        /// </remarks>
        public string AsString
        {
            get
            {
                if (_value is null)
                {
                    return null;
                }
                if (_value.Type == JTokenType.Array || _value.Type == JTokenType.Object)
                {
                    return JsonConvert.SerializeObject(_value);
                }
                return _value.Value<string>();
            }
        }

        /// <summary>
        /// Converts the value to an integer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the value is <see langword="null"/> or is not numeric, this returns zero. It will
        /// never throw an exception.
        /// </para>
        /// <para>
        /// If the value is a number but not an integer, it will be rounded toward zero (truncated).
        /// This is consistent with C# casting behavior, and with other LaunchDarkly SDKs that have
        /// strong typing, but it is different from the default behavior of <see cref="Newtonsoft.Json"/>
        /// which is to round to the nearest integer.
        /// </para>
        /// </remarks>
        public int AsInt
        {
            get
            {
                if (!(_value is null))
                {
                    if (_value.Type == JTokenType.Integer)
                    {
                        return _value.Value<int>();
                    }
                    if (_value.Type == JTokenType.Float)
                    {
                        return (int)_value.Value<float>();
                    }
                }
                return 0;
            }
        }

        /// <summary>
        /// Converts the value to a float.
        /// </summary>
        /// <remarks>
        /// If the value is <see langword="null"/> or is not numeric, this returns zero. It will never throw an exception.
        /// </remarks>
        public float AsFloat => IsNumber ? _value.Value<float>() : 0;

        /// <summary>
        /// Converts the value to a read-only list of elements of some type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The type parameter can be any of the types supported by <see cref="Value{T}"/>, and
        /// the conversion rules are the same: for instance, if it is <see langword="bool"/>,
        /// each array element will be converted with <see cref="AsBool"/>. If the value is not a
        /// JSON array at all, an empty list is returned. This method will never throw an exception.
        /// </para>
        /// <para>
        /// This is an efficient method because it does not copy values to a new list, but returns
        /// a read-only view into the existing array.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">the element type</typeparam>
        /// <returns>an array of elements of the specified type</returns>
        public IReadOnlyList<T> AsList<T>() =>
            (_value is JArray a) ?
                new ImmutableJsonArrayConverter<T>(a, v => v.Value<T>()) :
                new ImmutableJsonArrayConverter<T>(null, null);

        /// <summary>
        /// Converts the value to a read-only dictionary.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The type parameter can be any of the types supported by <see cref="Value{T}"/>, and
        /// the conversion rules are the same: for instance, if it is <see langword="bool"/>,
        /// the value of each key-value pair will be converted with <see cref="AsBool"/>. If this is not
        /// a JSON object at all, an empty dictionary is returned. This method will never throw an exception.
        /// </para>
        /// <para>
        /// This is an efficient method because it does not copy values to a new dictionary, but returns
        /// a read-only view into the existing object.
        /// </para>
        /// </remarks>
        /// <returns>a read-only dictionary</returns>
        public IReadOnlyDictionary<string, T> AsDictionary<T>()
        {
            if (_value is JObject o)
            {
                return new ImmutableJsonObjectConverter<T>(o, v => v.Value<T>());
            }
            return new ImmutableJsonObjectConverter<T>(null, null);
        }

        /// <summary>
        /// Converts the value to the desired type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method only works for primitive types: the type parameter can only be
        /// <see langword="bool"/>, <see langword="int"/>, <see langword="long"/>,
        /// <see langword="float"/>, <see langword="double"/>, or <see langword="string"/>
        /// (or <see cref="ImmutableJsonValue"/>, which returns the value unchanged. Type
        /// conversion behavior is consistent with the <see cref="ImmutableJsonValue"/>
        /// properties like <see cref="AsBool"/>, <see cref="AsInt"/>, etc. Any type that
        /// cannot be converted will return <c>default(T)</c> rather than throwing an exception.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">the desired type</typeparam>
        /// <returns>the value</returns>
        public T Value<T>()
        {
            if (typeof(T) == typeof(bool))
            {
                return (T)(object)AsBool; // odd double cast is necessary due to C# generics
            }
            else if (typeof(T) == typeof(int) || typeof(T) == typeof(long))
            {
                return (T)(object)AsInt;
            }
            else if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
            {
                return (T)(object)AsFloat;
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)AsString;
            }
            else if (typeof(T) == typeof(ImmutableJsonValue))
            {
                return (T)(object)this;
            }
            return default(T);
        }

        /// <summary>
        /// Converts the value to its JSON encoding.
        /// </summary>
        /// <remarks>
        /// For instance, <c>ImmutableValue.Of(1).ToJsonString()</c> returns <c>"1"</c>;
        /// <c>ImmutableValue.Of("x").ToJsonString()</c> returns <c>"\"x\""</c>; and
        /// <c>ImmutableValue.Null.ToJsonString()</c> returns <c>"null"</c>.
        /// </remarks>
        /// <returns>the JSON encoding of the value</returns>
        public string ToJsonString()
        {
            return IsNull ? "null" : JsonConvert.SerializeObject(_value);
        }

        /// <summary>
        /// Performs a deep-equality comparison using <see cref="JToken.DeepEquals(JToken)"/>.
        /// </summary>
        public override bool Equals(object o) => (o is ImmutableJsonValue v) && Equals(v);

        /// <summary>
        /// Performs a deep-equality comparison using <see cref="JToken.DeepEquals(JToken)"/>.
        /// </summary>
        public bool Equals(ImmutableJsonValue o)
        {
            return JToken.DeepEquals(_value, o._value);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return IsNull ? 0 : _value.GetHashCode();
        }

        /// <summary>
        /// Converts the value to its JSON encoding (same as <see cref="ToJsonString"/>).
        /// </summary>
        /// <returns>the JSON encoding of the value</returns>
        public override string ToString()
        {
            return ToJsonString();
        }
    }

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
                return ImmutableList.Create<T>().GetEnumerator();
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
            _object is null ? ImmutableList.Create<string>() :
            _object.Properties().Select(p => p.Name);

        public IEnumerable<T> Values
        {
            get
            {
                if (_object is null)
                {
                    return ImmutableList.Create<T>();
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
                return ImmutableDictionary.Create<string, T>().GetEnumerator();
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
