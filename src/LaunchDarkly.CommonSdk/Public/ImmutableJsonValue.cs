using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LaunchDarkly.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Client
{
    // Note, internal classes used here are in ImmutableJsonValueHelpers.cs

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
        /// The value is an object (a.k.a. hash or dictionary).
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
    /// <para>
    /// Note that this is a <see langword="struct"/>, not a class, so it is always passed by value
    /// and is not nullable; JSON nulls are represented by the constant <see cref="Null"/> and can
    /// be detected with <see cref="IsNull"/>. Whenever possible, <see cref="ImmutableJsonValue"/>
    /// stores primitive types within the struct rather than allocating an object on the heap.
    /// </para>
    /// </remarks>
    [JsonConverter(typeof(ImmutableJsonValueSerializer))]
    public struct ImmutableJsonValue : IEquatable<ImmutableJsonValue>
    {
        #region Private fields

        private static readonly ImmutableJsonValue _nullInstance = new ImmutableJsonValue(JsonValueType.Null, null);
        private static readonly JToken _jsonFalse = new JValue(false);
        private static readonly JToken _jsonTrue = new JValue(true);
        private static readonly JToken _jsonIntZero = new JValue(0);
        private static readonly JToken _jsonFloatZero = new JValue(0);
        private static readonly JToken _jsonStringEmpty = new JValue("");

        // Often, ImmutableJsonValue wraps an existing JToken. In that case, it will be in _wrappedJTokenValue,
        // and _type will be set to one of our type constants as appropriate. However, when creating a value of
        // a primitive type, we'd like to be able to access that value without having to create a JToken on the
        // heap. In that case, _type will indicate the type, and the value will be in _boolValue, _intValue,
        // etc. If we ever need to convert these primitives to a JToken, InnerValue will lazily create this and
        // keep it in _synthesizedJTokenValue (which is only used by InnerValue).
        private readonly JsonValueType _type;
        private readonly JToken _wrappedJTokenValue; // is never null unless _type is Null
        private readonly bool _boolValue;
        private readonly int _intValue;
        private readonly float _floatValue;
        private readonly string _stringValue;
        private volatile JToken _synthesizedJTokenValue; // see InnerValue

        #endregion

        #region Public static properties

        /// <summary>
        /// Convenience property for an <see cref="ImmutableJsonValue"/> that wraps a <see langword="null"/> value.
        /// </summary>
        public static ImmutableJsonValue Null => _nullInstance;

        #endregion

        #region Internal/private constructors, factory, and properties

        // Constructor from an existing JToken
        private ImmutableJsonValue(JsonValueType type, JToken value)
        {
            _type = type;
            _wrappedJTokenValue = value;
            _boolValue = false;
            _intValue = 0;
            _floatValue = 0;
            _stringValue = null;
            _synthesizedJTokenValue = null;
        }

        // Constructor from a primitive type
        private ImmutableJsonValue(JsonValueType type, bool boolValue, int intValue, float floatValue, string stringValue)
        {
            _type = type;
            _wrappedJTokenValue = null;
            _boolValue = boolValue;
            _intValue = intValue;
            _floatValue = floatValue;
            _stringValue = stringValue;
            _synthesizedJTokenValue = null;
        }

        /// <summary>
        /// For internal use only. Accesses the wrapped value as a JToken.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This internal method is used for efficiency only during flag evaluation or JSON serialization,
        /// where we know we will not be modifying any mutable objects or arrays and we will not be
        /// exposing the value to any external code.
        /// </para>
        /// <para>
        /// For values that were initialized from primitive types and do not have a wrapped JToken,
        /// this lazily creates one if necessary. Note that for efficiency, no synchronization is used
        /// when doing this, so there is a race condition where we might do it twice but the result
        /// will be the same.
        /// </para>
        /// </remarks>
        internal JToken InnerValue
        {
            get
            {
                if (Type == JsonValueType.Null)
                {
                    return null;
                }
                if (!(_wrappedJTokenValue is null))
                {
                    return _wrappedJTokenValue;
                }
                // This is a primitive type; perhaps we already converted it to a JToken?
                if (!(_synthesizedJTokenValue is null))
                {
                    return _synthesizedJTokenValue;
                }
                // No - create one now as appropriate.
                JToken value = null;
                switch (Type)
                {
                    case JsonValueType.Bool:
                        value = _boolValue ? _jsonTrue : _jsonFalse;
                        break;
                    case JsonValueType.Number:
                        if (_intValue != 0)
                        {
                            value = new JValue(_intValue);
                        }
                        else if (_floatValue != 0)
                        {
                            value = new JValue(_floatValue);
                        }
                        else
                        {
                            value = _jsonIntZero;
                        }
                        break;
                    case JsonValueType.String:
                        // _stringValue should never be null in this case because we would have
                        // stored the type as Null
                        value = _stringValue.Length == 0 ? _jsonStringEmpty : new JValue(_stringValue);
                        break;
                }
                _synthesizedJTokenValue = value;
                return value;
            }
        }

        /// <summary>
        /// True if this value was created from an existing JToken. Used only in serialization.
        /// </summary>
        internal bool HasWrappedJToken => !(_wrappedJTokenValue is null);

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
        internal static ImmutableJsonValue FromSafeValue(JToken value)
        {
            if (value is null || value.Type == JTokenType.Null)
            {
                return _nullInstance;
            }
            switch (value.Type)
            {
                case JTokenType.Boolean:
                    return Of(value.Value<bool>()); // this uses static instances for true and false
                case JTokenType.Integer:
                case JTokenType.Float:
                    return new ImmutableJsonValue(JsonValueType.Number, value);
                case JTokenType.String:
                    // JToken can unfortunately claim that the type is string but actually have a null in it.
                    var s = value.Value<string>();
                    return s is null ? _nullInstance : new ImmutableJsonValue(JsonValueType.String, value);
                case JTokenType.Array:
                    return new ImmutableJsonValue(JsonValueType.Array, value);
                case JTokenType.Object:
                    return new ImmutableJsonValue(JsonValueType.Object, value);
                // JTokenType also defines a few nonstandard types like TimeSpan, which can only be created
                // programmatically - we will never see them in parsed input. These are meaningless in
                // LaunchDarkly logic, which only supports standard JSON types, so we will convert them
                // to strings except for dates, which we will encode as Unix milliseconds because that's
                // what our date logic uses in flag evaluations.
                case JTokenType.Date:
                    var t = value.Value<DateTime>().ToUniversalTime();
                    float millis = Util.GetUnixTimestampMillis(t);
                    return Of(millis);
                default:
                    return Of(value.Value<string>());
            }
        }

        #endregion

        #region Public factory methods

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> from a <see cref="JToken"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="Newtonsoft.Json"/> type <see cref="JToken"/> is being phased out of the SDK because
        /// it is mutable (so it is possible to unintentionally modify a JSON object or array that is being
        /// used elsewhere), and in order to reduce third-party dependencies. However, since parts of the current
        /// API still use this type, <see cref="FromJToken(JToken)"/> and <see cref="AsJToken()"/> provide a
        /// simple conversion.
        /// </para>
        /// <para>
        /// In order to avoid the mutability problem, this method performs a deep copy of any <see cref="JObject"/>
        /// or <see cref="JArray"/> value.
        /// </para>
        /// <para>
        /// <see cref="FromJToken(JToken)"/> and <see cref="AsJToken()"/> are immediately deprecated, to
        /// encourage developers to migrate away from using <see cref="JToken"/>-based SDK methods as soon as
        /// possible.
        /// </para>
        /// </remarks>
        /// <param name="value">a <see cref="JToken"/> value</param>
        /// <returns>a corresponding <see cref="ImmutableJsonValue"/></returns>
        [Obsolete("This method will be removed in the future; use non-JToken factory methods instead")]
        public static ImmutableJsonValue FromJToken(JToken value)
        {
            if (value is JArray || value is JObject)
            {
                return FromSafeValue(value.DeepClone());
            }
            return FromSafeValue(value);
        }

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> from a boolean value.
        /// </summary>
        /// <remarks>
        /// This method will not create any objects on the heap.
        /// </remarks>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static ImmutableJsonValue Of(bool value) =>
            new ImmutableJsonValue(JsonValueType.Bool, value, 0, 0, null);

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> from an integer value.
        /// </summary>
        /// <remarks>
        /// This method will not create any objects on the heap unless you later call <see cref="AsJToken"/>.
        /// </remarks>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static ImmutableJsonValue Of(int value) =>
            new ImmutableJsonValue(JsonValueType.Number, false, value, 0, null);

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> from a float value.
        /// </summary>
        /// <remarks>
        /// This method will not create any objects on the heap unless you later call <see cref="AsJToken"/>.
        /// </remarks>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static ImmutableJsonValue Of(float value) =>
            new ImmutableJsonValue(JsonValueType.Number, false, 0, value, null);

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> from a string value.
        /// </summary>
        /// <remarks>
        /// A null string reference will be stored as <see cref="Null"/> rather than as a string. For a
        /// non-null string, this method will not create any additional objects on the heap unless you
        /// later call <see cref="AsJToken"/>.
        /// </remarks>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static ImmutableJsonValue Of(string value) =>
            value is null ? Null : new ImmutableJsonValue(JsonValueType.String, false, 0, 0, value);

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as an array, from a sequence of booleans.
        /// </summary>
        /// <param name="arrayValue">a sequence of booleans</param>
        /// <returns>a struct representing a JSON array</returns>
        public static ImmutableJsonValue FromValues(IEnumerable<bool> arrayValue) =>
            FromEnumerable(arrayValue, (bool b) => Of(b));

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as an array, from a sequence of ints.
        /// </summary>
        /// <param name="arrayValue">a sequence of ints</param>
        /// <returns>a struct representing a JSON array</returns>
        public static ImmutableJsonValue FromValues(IEnumerable<int> arrayValue) =>
            FromEnumerable(arrayValue, (int n) => Of(n));

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as an array, from a sequence of floats.
        /// </summary>
        /// <param name="arrayValue">a sequence of floats</param>
        /// <returns>a struct representing a JSON array</returns>
        public static ImmutableJsonValue FromValues(IEnumerable<float> arrayValue) =>
            FromEnumerable(arrayValue, (float f) => Of(f));

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> from a sequence of strings.
        /// </summary>
        /// <param name="arrayValue">a sequence of strings</param>
        /// <returns>a struct representing a JSON array</returns>
        public static ImmutableJsonValue FromValues(IEnumerable<string> arrayValue) =>
            FromEnumerable(arrayValue, (string s) => Of(s));

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as an array, from a sequence of JSON values.
        /// </summary>
        /// <param name="arrayValue">a sequence of values</param>
        /// <returns>a struct representing a JSON array</returns>
        public static ImmutableJsonValue FromValues(IEnumerable<ImmutableJsonValue> arrayValue) =>
            FromEnumerable(arrayValue, v => v);

        private static ImmutableJsonValue FromEnumerable<T>(IEnumerable<T> values, Func<T, ImmutableJsonValue> convert)
        {
            if (values is null)
            {
                return Null;
            }
            var a = new JArray();
            foreach (var item in values)
            {
                a.Add(convert(item).InnerValue);
            }
            return FromSafeValue(a);
        }

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as a JSON object, from a dictionary
        /// containing booleans.
        /// </summary>
        /// <param name="dictionary">a dictionary of strings to booleans</param>
        /// <returns>a struct representing a JSON object</returns>
        public static ImmutableJsonValue FromDictionary(IReadOnlyDictionary<string, bool> dictionary) =>
            FromDictionaryInternal(dictionary, (bool b) => Of(b));

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as a JSON object, from a dictionary
        /// containing ints.
        /// </summary>
        /// <param name="dictionary">a dictionary of strings to ints</param>
        /// <returns>a struct representing a JSON object</returns>
        public static ImmutableJsonValue FromDictionary(IReadOnlyDictionary<string, int> dictionary) =>
            FromDictionaryInternal(dictionary, (int n) => Of(n));

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as a JSON object, from a dictionary
        /// containing floats.
        /// </summary>
        /// <param name="dictionary">a dictionary of strings to floats</param>
        /// <returns>a struct representing a JSON object</returns>
        public static ImmutableJsonValue FromDictionary(IReadOnlyDictionary<string, float> dictionary) =>
            FromDictionaryInternal(dictionary, (float f) => Of(f));

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as a JSON object, from a dictionary
        /// containing strings.
        /// </summary>
        /// <param name="dictionary">a dictionary of strings to strings</param>
        /// <returns>a struct representing a JSON object</returns>
        public static ImmutableJsonValue FromDictionary(IReadOnlyDictionary<string, string> dictionary) =>
            FromDictionaryInternal(dictionary, (string s) => Of(s));

        /// <summary>
        /// Initializes an <see cref="ImmutableJsonValue"/> as a JSON object, from a dictionary
        /// containing JSON values.
        /// </summary>
        /// <param name="dictionary">a dictionary of strings to JSON values</param>
        /// <returns>a struct representing a JSON object</returns>
        public static ImmutableJsonValue FromDictionary(IReadOnlyDictionary<string, ImmutableJsonValue> dictionary) =>
            FromDictionaryInternal(dictionary, v => v);

        private static ImmutableJsonValue FromDictionaryInternal<T>(IReadOnlyDictionary<string, T> dictionary,
            Func<T, ImmutableJsonValue> convert)
        {
            if (dictionary is null)
            {
                return Null;
            }
            var o = new JObject();
            foreach (var e in dictionary)
            {
                o.Add(e.Key, convert(e.Value).InnerValue);
            }
            return FromSafeValue(o);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// The type of the JSON value.
        /// </summary>
        public JsonValueType Type => _type;

        /// <summary>
        /// True if the wrapped value is <see langword="null"/>.
        /// </summary>
        public bool IsNull => Type == JsonValueType.Null;

        /// <summary>
        /// True if the wrapped value is numeric.
        /// </summary>
        public bool IsNumber => Type == JsonValueType.Number;

        /// <summary>
        /// True if the wrapped value is an integer.
        /// </summary>
        /// <remarks>
        /// JSON does not have separate types for integer and floating-point values; they are both just
        /// numbers. <see cref="IsInt"/> returns true if and only if the actual numeric value has no
        /// fractional component, so <c>ImmutableJsonValue(2).IsInt</c> and <c>ImmutableJsonValue(2.0f).IsInt</c>
        /// are both true.
        /// </remarks>
        public bool IsInt => IsNumber && (AsFloat == (float)AsInt);

        /// <summary>
        /// Converts the value to a boolean.
        /// </summary>
        /// <remarks>
        /// If the value is <see langword="null"/> or is not a boolean, this returns <see langword="false"/>.
        /// It will never throw an exception.
        /// </remarks>
        public bool AsBool => Type == JsonValueType.Bool &&
            (_wrappedJTokenValue is null ? _boolValue : _wrappedJTokenValue.Value<bool>());

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
                switch (Type)
                {
                    case JsonValueType.Null:
                        return null;
                    case JsonValueType.String:
                        return _wrappedJTokenValue is null ? _stringValue : _wrappedJTokenValue.Value<string>();
                    case JsonValueType.Bool:
                        return AsBool.ToString();
                    case JsonValueType.Number:
                        return AsFloat.ToString();
                    default:
                        return _wrappedJTokenValue is null ? null :
                            JsonConvert.SerializeObject(_wrappedJTokenValue);
                }
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
                if (Type == JsonValueType.Number)
                {
                    if (_wrappedJTokenValue is null)
                    {
                        // we stored a primitive int or float - it's whichever one is nonzero, if any
                        return _floatValue == 0 ? _intValue : (int)_floatValue;
                    }
                    return _wrappedJTokenValue.Type == JTokenType.Integer ?
                        _wrappedJTokenValue.Value<int>() : (int)_wrappedJTokenValue.Value<float>();
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
        public float AsFloat
        {
            get
            {
                if (Type == JsonValueType.Number)
                {
                    if (_wrappedJTokenValue is null)
                    {
                        // we stored a primitive int or float - it's whichever one is nonzero, if any
                        return _intValue == 0 ? _floatValue : (float)_intValue;
                    }
                    return _wrappedJTokenValue.Type == JTokenType.Float ?
                        _wrappedJTokenValue.Value<float>() : (float)_wrappedJTokenValue.Value<int>();
                }
                return 0;
            }
        }

        #endregion

        #region Public methods

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
            (_wrappedJTokenValue is JArray a) ?
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
            if (_wrappedJTokenValue is JObject o)
            {
                return new ImmutableJsonObjectConverter<T>(o, v => v.Value<T>());
            }
            return new ImmutableJsonObjectConverter<T>(null, null);
        }

        /// <summary>
        /// Converts the value to a <see cref="JToken"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="Newtonsoft.Json"/> type <see cref="JToken"/> is being phased out of the SDK because
        /// it is mutable (so it is possible to unintentionally modify a JSON object or array that is being
        /// used elsewhere), and in order to reduce third-party dependencies. However, since parts of the current
        /// API still use this type, <see cref="FromJToken(JToken)"/> and <see cref="AsJToken()"/> provide a
        /// simple conversion.
        /// </para>
        /// <para>
        /// In order to avoid the mutability problem, this method performs a deep copy of any <see cref="JObject"/>
        /// or <see cref="JArray"/> value.
        /// </para>
        /// <para>
        /// <see cref="FromJToken(JToken)"/> and <see cref="AsJToken"/> are immediately deprecated, to
        /// encourage developers to migrate away from using <see cref="JToken"/>-based SDK methods as soon as
        /// possible.
        /// </para>
        /// </remarks>
        /// <returns>a <see cref="JToken"/> representation of this value</returns>
        [Obsolete("This method will be removed in the future; use non-JToken-based methods and properties instead")]
        public JToken AsJToken()
        {
            if (_wrappedJTokenValue is JArray || _wrappedJTokenValue is JObject)
            {
                return _wrappedJTokenValue.DeepClone();
            }
            return InnerValue;
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
            return IsNull ? "null" : JsonConvert.SerializeObject(InnerValue);
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
            if (Type != o.Type)
            {
                return false;
            }
            switch (Type)
            {
                case JsonValueType.Null:
                    return true;
                case JsonValueType.Bool:
                    return AsBool == o.AsBool;
                case JsonValueType.Number:
                    return AsFloat == o.AsFloat; // don't worry about ints because you can't lose precision going from int to float
                case JsonValueType.String:
                    return AsString.Equals(o.AsString);
                default:
                    // array and object types always have a wrapped JToken
                    return JToken.DeepEquals(_wrappedJTokenValue, o._wrappedJTokenValue);
            }
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            switch (Type)
            {
                case JsonValueType.Null:
                    return 0;
                case JsonValueType.Bool:
                    return AsBool.GetHashCode();
                case JsonValueType.Number:
                    return AsFloat.GetHashCode();
                case JsonValueType.String:
                    return AsString.GetHashCode();
                default:
                    // array and object types always have a wrapped JToken
                    return _wrappedJTokenValue.GetHashCode();
            }
        }

        /// <summary>
        /// Converts the value to its JSON encoding (same as <see cref="ToJsonString"/>).
        /// </summary>
        /// <returns>the JSON encoding of the value</returns>
        public override string ToString()
        {
            return ToJsonString();
        }

        #endregion
    }
}
