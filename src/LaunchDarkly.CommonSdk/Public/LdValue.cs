using System;
using System.Collections.Generic;
using System.Linq;
using LaunchDarkly.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Client
{
    // Note, internal classes used here are in LdValueHelpers.cs

    /// <summary>
    /// Describes the type of a JSON value.
    /// </summary>
    public enum LdValueType
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
    /// represented with <see cref="LdValue"/> instead. It is easily convertible
    /// to primitive types and array/dictionary structures.
    /// </para>
    /// <para>
    /// Note that this is a <see langword="struct"/>, not a class, so it is always passed by value
    /// and is not nullable; JSON nulls are represented by the constant <see cref="Null"/> and can
    /// be detected with <see cref="IsNull"/>. Whenever possible, <see cref="LdValue"/>
    /// stores primitive types within the struct rather than allocating an object on the heap.
    /// </para>
    /// <para>
    /// There are several ways to create an <see cref="LdValue"/>. For primitive types,
    /// use the various overloads of "Of" such as <see cref="Of(bool)"/>; these are very efficient
    /// since they do not allocate any objects on the heap. For arrays and objects (dictionaries),
    /// use <see cref="ArrayFrom(IEnumerable{LdValue})"/>, <see cref="ArrayOf(LdValue[])"/>,
    /// <see cref="ObjectFrom(IReadOnlyDictionary{string, LdValue})"/>, or the corresponding
    /// methods in the type-specific <see cref="Convert"/> instances.
    /// </para>
    /// <para>
    /// To convert to other types, there are the "As" properties such as 
    /// use the various overloads of "Of" such as <see cref="Of(bool)"/>; these are very efficient
    /// since they do not allocate any objects on the heap. For arrays and objects (dictionaries),
    /// use <see cref="AsList{T}(LdValue.Converter{T})"/> or <see cref="AsDictionary{T}(LdValue.Converter{T})"/>.
    /// </para>
    /// <para>
    /// Currently, there is also the option of converting to or from the <see cref="Newtonsoft.Json"/>
    /// type <see cref="JToken"/>. However, those methods may be removed in the future in order to avoid
    /// this third-party API dependency.
    /// </para>
    /// </remarks>
    [JsonConverter(typeof(LdValueSerializer))]
    public struct LdValue : IEquatable<LdValue>
    {
        #region Private fields

        private static readonly LdValue _nullInstance = new LdValue(LdValueType.Null, null);
        private static readonly JToken _jsonFalse = new JValue(false);
        private static readonly JToken _jsonTrue = new JValue(true);
        private static readonly JToken _jsonIntZero = new JValue(0);
        private static readonly JToken _jsonDoubleZero = new JValue((double)0);
        private static readonly JToken _jsonStringEmpty = new JValue("");

        // Often, LdValue wraps an existing JToken. In that case, it will be in _wrappedJTokenValue,
        // and _type will be set to one of our type constants as appropriate. However, when creating a value of
        // a primitive type, we'd like to be able to access that value without having to create a JToken on the
        // heap. In that case, _type will indicate the type, and the value will be in _boolValue, _intValue,
        // etc. If we ever need to convert these primitives to a JToken, InnerValue will lazily create this and
        // keep it in _synthesizedJTokenValue (which is only used by InnerValue).
        private readonly LdValueType _type;
        private readonly JToken _wrappedJTokenValue; // is never null unless _type is Null
        private readonly bool _boolValue;
        private readonly double _doubleValue; // all numbers are stored as double
        private readonly string _stringValue;
        private readonly IList<LdValue> _arrayValue; // will be IImmutableList in the future, but we don't have System.Collections.Immutables yet
        private readonly IDictionary<string, LdValue> _objectValue; // same
        private volatile JToken _synthesizedJTokenValue; // see InnerValue

        #endregion

        #region Public static properties

        /// <summary>
        /// Convenience property for an <see cref="LdValue"/> that wraps a <see langword="null"/> value.
        /// </summary>
        public static LdValue Null => _nullInstance;

        #endregion

        #region Internal/private constructors, factory, and properties

        // Constructor from an existing JToken
        private LdValue(LdValueType type, JToken value)
        {
            _type = type;
            _wrappedJTokenValue = value;
            _boolValue = false;
            _doubleValue = 0;
            _stringValue = null;
            _arrayValue = null;
            _objectValue = null;
            _synthesizedJTokenValue = null;
        }

        // Constructor from a primitive type
        private LdValue(LdValueType type, bool boolValue, double doubleValue, string stringValue)
        {
            _type = type;
            _wrappedJTokenValue = null;
            _boolValue = boolValue;
            _doubleValue = doubleValue;
            _stringValue = stringValue;
            _arrayValue = null;
            _objectValue = null;
            _synthesizedJTokenValue = null;
        }

        // Constructor from a read-only list
        private LdValue(IList<LdValue> list)
        {
            _type = LdValueType.Array;
            _arrayValue = list;
            _wrappedJTokenValue = null;
            _boolValue = false;
            _doubleValue = 0;
            _stringValue = null;
            _objectValue = null;
            _synthesizedJTokenValue = null;
        }

        // Constructor from a read-only dictionary
        private LdValue(IDictionary<string, LdValue> dict)
        {
            _type = LdValueType.Object;
            _objectValue = dict;
            _wrappedJTokenValue = null;
            _boolValue = false;
            _doubleValue = 0;
            _stringValue = null;
            _arrayValue = null;
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
                if (Type == LdValueType.Null)
                {
                    return null;
                }
                // Were we created to wrap a JToken in the first place?
                if (!(_wrappedJTokenValue is null))
                {
                    return _wrappedJTokenValue;
                }
                // Perhaps we already converted our value to a JToken?
                if (!(_synthesizedJTokenValue is null))
                {
                    return _synthesizedJTokenValue;
                }
                // No - create one now as appropriate.
                JToken value = null;
                switch (Type)
                {
                    case LdValueType.Bool:
                        value = _boolValue ? _jsonTrue : _jsonFalse;
                        break;
                    case LdValueType.Number:
                        if (IsInt)
                        {
                            value = _doubleValue == 0 ? _jsonIntZero : new JValue((int)_doubleValue);
                        }
                        else
                        {
                            value = _doubleValue == 0 ? _jsonDoubleZero : new JValue(_doubleValue);
                        }
                        break;
                    case LdValueType.String:
                        // _stringValue should never be null in this case because we would have
                        // stored the type as Null
                        value = _stringValue.Length == 0 ? _jsonStringEmpty : new JValue(_stringValue);
                        break;
                    case LdValueType.Array:
                        var a = new JArray();
                        foreach (var item in _arrayValue)
                        {
#pragma warning disable 0618
                            a.Add(item.AsJToken());
#pragma warning restore 0618
                        }
                        value = a;
                        break;
                    case LdValueType.Object:
                        var o = new JObject();
                        foreach (var e in _objectValue)
                        {
#pragma warning disable 0618
                            o.Add(e.Key, e.Value.AsJToken());
#pragma warning restore 0618
                        }
                        value = 0;
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
        /// For internal use only. Initializes an <see cref="LdValue"/> from an arbitrary JSON
        /// value that we know will not be modified.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is to be used internally when the SDK has a JToken instance that has not been
        /// exposed to any application code, and the SDK code is never going to call any mutative
        /// methods on that value. In that case, we do not need to perform a deep copy on the value
        /// just to wrap it in an <see cref="LdValue"/>; a deep copy will be performed anyway
        /// if the application tries to access the JToken.
        /// </para>
        /// <para>
        /// It also performs minor optimizations by using our static JToken instances for true, 0, etc.
        /// </para>
        /// </remarks>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        internal static LdValue FromSafeValue(JToken value)
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
                    return new LdValue(LdValueType.Number, value);
                case JTokenType.String:
                    // JToken can unfortunately claim that the type is string but actually have a null in it.
                    var s = value.Value<string>();
                    return s is null ? _nullInstance : new LdValue(LdValueType.String, value);
                case JTokenType.Array:
                    return new LdValue(LdValueType.Array, value);
                case JTokenType.Object:
                    return new LdValue(LdValueType.Object, value);
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
        /// Initializes an <see cref="LdValue"/> from a <see cref="JToken"/>.
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
        /// <returns>a corresponding <see cref="LdValue"/></returns>
        [Obsolete("This method will be removed in the future; use non-JToken factory methods instead")]
        public static LdValue FromJToken(JToken value)
        {
            if (value is JArray || value is JObject)
            {
                return FromSafeValue(value.DeepClone());
            }
            return FromSafeValue(value);
        }

        /// <summary>
        /// Initializes an <see cref="LdValue"/> from a boolean value.
        /// </summary>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static LdValue Of(bool value) =>
            new LdValue(LdValueType.Bool, value, 0, null);

        /// <summary>
        /// Initializes an <see cref="LdValue"/> from an <see langword="int"/> value.
        /// </summary>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static LdValue Of(int value) =>
            new LdValue(LdValueType.Number, false, value, null);

        /// <summary>
        /// Initializes an <see cref="LdValue"/> from a <see langword="long"/> value.
        /// </summary>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static LdValue Of(long value) =>
            new LdValue(LdValueType.Number, false, value, null);

        /// <summary>
        /// Initializes an <see cref="LdValue"/> from a <see langword="float"/> value.
        /// </summary>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static LdValue Of(float value) =>
            new LdValue(LdValueType.Number, false, value, null);

        /// <summary>
        /// Initializes an <see cref="LdValue"/> from a <see langword="double"/> value.
        /// </summary>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static LdValue Of(double value) =>
            new LdValue(LdValueType.Number, false, value, null);

        /// <summary>
        /// Initializes an <see cref="LdValue"/> from a string value.
        /// </summary>
        /// <remarks>
        /// A null string reference will be stored as <see cref="Null"/> rather than as a string.
        /// </remarks>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static LdValue Of(string value) =>
            value is null ? Null : new LdValue(LdValueType.String, false, 0, value);

        /// <summary>
        /// Initializes an <see cref="LdValue"/> as an array, from a sequence of JSON values.
        /// </summary>
        /// <remarks>
        /// To create an array from values of some other type, use <see cref="Converter{T}.ArrayFrom(IEnumerable{T})"/>
        /// </remarks>
        /// <example>
        /// <code>
        ///     var listOfValues = new List&lt;LdValue&gt; { LdValue.Of(1), LdValue.Of("x") };
        ///     var arrayValue = LdValue.ArrayFrom(listOfValues);
        /// </code>
        /// </example>
        /// <param name="values">a sequence of values</param>
        /// <returns>a struct representing a JSON array, or <see cref="Null"/> if the parameter was null</returns>
        public static LdValue ArrayFrom(IEnumerable<LdValue> values) =>
            Convert.Json.ArrayFrom(values);

        /// <summary>
        /// Initializes an <see cref="LdValue"/> as an array, from a sequence of JSON values.
        /// </summary>
        /// <remarks>
        /// To create an array from values of some other type, use <see cref="Converter{T}.ArrayOf(T[])"/>
        /// </remarks>
        /// <example>
        /// <code>
        ///     var arrayValue = LdValue.ArrayFrom(LdValue.Of("a"), LdValue.Of("b"));
        /// </code>
        /// </example>
        /// <param name="values">any number of values</param>
        /// <returns>a struct representing a JSON array</returns>
        public static LdValue ArrayOf(params LdValue[] values) =>
            Convert.Json.ArrayOf(values);

        /// <summary>
        /// Initializes an <see cref="LdValue"/> as a JSON object, from a dictionary.
        /// </summary>
        /// <remarks>
        /// To use a dictionary with values of some other type, use <see cref="Converter{T}.ObjectFrom"/>.
        /// </remarks>
        /// <param name="dictionary">a dictionary with string keys and values of the specified type</param>
        /// <returns>a struct representing a JSON object, or <see cref="Null"/> if the parameter was null</returns>
        public static LdValue ObjectFrom(IReadOnlyDictionary<string, LdValue> dictionary) =>
            Convert.Json.ObjectFrom(dictionary);

        #endregion

        #region Public properties

        /// <summary>
        /// The type of the JSON value.
        /// </summary>
        public LdValueType Type => _type;

        /// <summary>
        /// True if the wrapped value is <see langword="null"/>.
        /// </summary>
        public bool IsNull => Type == LdValueType.Null;

        /// <summary>
        /// True if the wrapped value is numeric.
        /// </summary>
        public bool IsNumber => Type == LdValueType.Number;

        /// <summary>
        /// True if the wrapped value is an integer.
        /// </summary>
        /// <remarks>
        /// JSON does not have separate types for integer and floating-point values; they are both just
        /// numbers. <see cref="IsInt"/> returns true if and only if the actual numeric value has no
        /// fractional component, so <c>LdValue(2).IsInt</c> and <c>LdValue(2.0f).IsInt</c>
        /// are both true.
        /// </remarks>
        public bool IsInt => IsNumber && (AsFloat == (float)AsInt);

        /// <summary>
        /// True if the wrapped value is a string.
        /// </summary>
        public bool IsString => Type == LdValueType.String;

        /// <summary>
        /// Gets the boolean value if this is a boolean.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the value is <see langword="null"/> or is not a boolean, this returns <see langword="false"/>.
        /// It will never throw an exception.
        /// </para>
        /// <para>
        /// This is equivalent to calling <see cref="Converter{T}.ToType(LdValue)"/> on
        /// <see cref="LdValue.Convert.Bool"/>.
        /// </para>
        /// </remarks>
        public bool AsBool => Type == LdValueType.Bool &&
            (_wrappedJTokenValue is null ? _boolValue : _wrappedJTokenValue.Value<bool>());

        /// <summary>
        /// Gets the string value if this is a string.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the value is <see langword="null"/> or is not a string, this returns <see langword="null"/>.
        /// It will never throw an exception. To get a JSON representation of the value as a string, use
        /// <see cref="ToJsonString"/> instead.
        /// </para>
        /// <para>
        /// This is equivalent to calling <see cref="Converter{T}.ToType(LdValue)"/> on
        /// <see cref="LdValue.Convert.String"/>.
        /// </para>
        /// </remarks>
        public string AsString => Type == LdValueType.String ?
            (_wrappedJTokenValue is null ? _stringValue : _wrappedJTokenValue.Value<string>()) : null;

        /// <summary>
        /// Gets the value as an <see langword="int"/> if it is numeric.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the value is <see langword="null"/> or is not numeric, this returns zero. It will
        /// never throw an exception.
        /// </para>
        /// <para>
        /// If the value is a number but not an integer, it will be rounded to the nearest integer.
        /// This is consistent with the behavior of <c>IntVariation</c> in .NET SDK 5.x, and rounding in
        /// <see cref="Newtonsoft.Json"/>, but it is different from C# casting behavior and the behavior
        /// of other LaunchDarkly SDKs, which round toward zero. This will be changed in a future version
        /// to always round toward zero. If in doubt, call <see cref="AsFloat"/> or <see cref="AsDouble"/>
        /// and do the rounding yourself.
        /// </para>
        /// <para>
        /// This is equivalent to calling <see cref="Converter{T}.ToType(LdValue)"/> on
        /// <see cref="LdValue.Convert.Int"/>.
        /// </para>
        /// </remarks>
        public int AsInt => (int)Math.Round(AsDouble, MidpointRounding.ToEven);

        /// <summary>
        /// Gets the value as an <see langword="long"/> if it is numeric.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the value is <see langword="null"/> or is not numeric, this returns zero. It will
        /// never throw an exception.
        /// </para>
        /// <para>
        /// If the value is a number but not an integer, it will be rounded to the nearest integer.
        /// This is consistent with the behavior of <c>IntVariation</c> in .NET SDK 5.x, and rounding in
        /// <see cref="Newtonsoft.Json"/>, but it is different from C# casting behavior and the behavior
        /// of other LaunchDarkly SDKs, which round toward zero. This will be changed in a future version
        /// to always round toward zero. If in doubt, call <see cref="AsFloat"/> or <see cref="AsDouble"/>
        /// and do the rounding yourself.
        /// </para>
        /// <para>
        /// This is equivalent to calling <see cref="Converter{T}.ToType(LdValue)"/> on
        /// <see cref="LdValue.Convert.Long"/>.
        /// </para>
        /// </remarks>
        public long AsLong => (long)Math.Round(AsDouble, MidpointRounding.ToEven);

        /// <summary>
        /// Gets the value as an <see langword="float"/> if it is numeric.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the value is <see langword="null"/> or is not numeric, this returns zero. It will never
        /// throw an exception.
        /// </para>
        /// <para>
        /// This is equivalent to calling <see cref="Converter{T}.ToType(LdValue)"/> on
        /// <see cref="LdValue.Convert.Float"/>.
        /// </para>
        /// </remarks>
        public float AsFloat => (float)AsDouble;

        /// <summary>
        /// Gets the value as an <see langword="double"/> if it is numeric.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the value is <see langword="null"/> or is not numeric, this returns zero. It will never
        /// throw an exception.
        /// </para>
        /// <para>
        /// This is equivalent to calling <see cref="Converter{T}.ToType(LdValue)"/> on
        /// <see cref="LdValue.Convert.Double"/>.
        /// </para>
        /// </remarks>
        public double AsDouble
        {
            get
            {
                if (Type == LdValueType.Number)
                {
                    return _wrappedJTokenValue is null ? _doubleValue : _wrappedJTokenValue.Value<double>();
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
        /// The first parameter is one of the type converters from <see cref="Convert"/>, or your own
        /// implementation of <see cref="Converter{T}"/> for some type.
        /// </para>
        /// <para>
        /// If the value is not a JSON array at all, an empty list is returned. This method will
        /// never throw an exception.
        /// </para>
        /// <para>
        /// This is an efficient method because it does not copy values to a new list, but returns
        /// a read-only view into the existing array.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">the element type</typeparam>
        /// <returns>an array of elements of the specified type</returns>
        public IReadOnlyList<T> AsList<T>(Converter<T> desiredType)
        {
            if (_type == LdValueType.Array)
            {
                if (!(_arrayValue is null))
                {
                    return new LdValueListConverter<LdValue, T>(_arrayValue, desiredType.ToType);
                }
                else if (_wrappedJTokenValue is JArray a)
                {
                    return new LdValueListConverter<JToken, T>(a, v => desiredType.ToType(LdValue.FromSafeValue(v)));
                }
            }
            return new LdValueListConverter<T, T>(null, null);
        }

        /// <summary>
        /// Converts the value to a read-only dictionary.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The first parameter is one of the type converters from <see cref="Convert"/>, or your own
        /// implementation of <see cref="Converter{T}"/> for some type.
        /// </para>
        /// <para>
        /// This is an efficient method because it does not copy values to a new dictionary, but returns
        /// a read-only view into the existing object.
        /// </para>
        /// </remarks>
        /// <returns>a read-only dictionary</returns>
        public IReadOnlyDictionary<string, T> AsDictionary<T>(Converter<T> desiredType)
        {
            if (_type == LdValueType.Object)
            {
                if (!(_objectValue is null))
                {
                    return new LdValueObjectConverter<LdValue, T>(_objectValue, desiredType.ToType);
                }
                else if (_wrappedJTokenValue is JObject o)
                {
                    return new LdValueObjectConverter<JToken, T>(o, v => desiredType.ToType(LdValue.FromSafeValue(v)));
                }
            }
            return new LdValueObjectConverter<T, T>(null, null);
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
        /// Converts the value to its JSON encoding.
        /// </summary>
        /// <remarks>
        /// For instance, <c>LdValue.Of(1).ToJsonString()</c> returns <c>"1"</c>;
        /// <c>LdValue.Of("x").ToJsonString()</c> returns <c>"\"x\""</c>; and
        /// <c>LdValue.Null.ToJsonString()</c> returns <c>"null"</c>.
        /// </remarks>
        /// <returns>the JSON encoding of the value</returns>
        public string ToJsonString()
        {
            return IsNull ? "null" : JsonConvert.SerializeObject(InnerValue);
        }

        /// <summary>
        /// Performs a deep-equality comparison using <see cref="JToken.DeepEquals(JToken)"/>.
        /// </summary>
        public override bool Equals(object o) => (o is LdValue v) && Equals(v);

        /// <summary>
        /// Performs a deep-equality comparison using <see cref="JToken.DeepEquals(JToken)"/>.
        /// </summary>
        public bool Equals(LdValue o)
        {
            if (Type != o.Type)
            {
                return false;
            }
            switch (Type)
            {
                case LdValueType.Null:
                    return true;
                case LdValueType.Bool:
                    return AsBool == o.AsBool;
                case LdValueType.Number:
                    return AsDouble == o.AsDouble; // don't worry about ints because you can't lose precision going from int to double
                case LdValueType.String:
                    return AsString.Equals(o.AsString);
                case LdValueType.Array:
                    return AsList(Convert.Json).SequenceEqual(o.AsList(Convert.Json));
                case LdValueType.Object:
                    var d0 = AsDictionary(Convert.Json);
                    var d1 = AsDictionary(Convert.Json);
                    return d0.Count == d1.Count && d0.All(kv => kv.Value.Equals(d1[kv.Key]));
                default:
                    return false;
            }
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            switch (Type)
            {
                case LdValueType.Null:
                    return 0;
                case LdValueType.Bool:
                    return AsBool.GetHashCode();
                case LdValueType.Number:
                    return AsFloat.GetHashCode();
                case LdValueType.String:
                    return AsString.GetHashCode();
                case LdValueType.Array:
                    int ah = 0;
                    foreach (var item in AsList(Convert.Json))
                    {
                        ah = ah * 23 + item.GetHashCode();
                    }
                    return ah;
                case LdValueType.Object:
                    int oh = 0;
                    foreach (var kv in AsDictionary(Convert.Json))
                    {
                        oh = (oh * 23 + kv.Key.GetHashCode()) * 23 + kv.Value.GetHashCode();
                    }
                    return oh;
                default:
                    return 0;
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

        #region Inner types

        /// <summary>
        /// Defines a conversion between <see cref="LdValue"/> and some other type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Besides converting individual values, <see cref="Converter{T}"/> provides factory methods
        /// like <see cref="ArrayOf"/> which transform a collection of the specified type to the
        /// corresponding <see cref="LdValue"/> complex type.
        /// </para>
        /// <para>
        /// There are type-specific instances of this class for commonly used types in
        /// <see cref="LdValue.Convert"/>, but you can also implement your own.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">the type to convert from/to</typeparam>
        public abstract class Converter<T>
        {
            /// <summary>
            /// Converts a value of the specified type to an <see cref="LdValue"/>.
            /// </summary>
            /// <remarks>
            /// This method should never throw an exception; if for some reason the value is invalid,
            /// it should return <see cref="LdValue.Null"/>.
            /// </remarks>
            /// <param name="valueOfType">a value of this type</param>
            /// <returns>an <see cref="LdValue"/></returns>
            abstract public LdValue FromType(T valueOfType);

            /// <summary>
            /// Converts an <see cref="LdValue"/> to a value of the specified type.
            /// </summary>
            /// <remarks>
            /// This method should never throw an exception; if the conversion cannot be done, it
            /// should return <c>default(T)</c>.
            /// </remarks>
            /// <param name="jsonValue">an <see cref="LdValue"/></param>
            /// <returns>a value of this type</returns>
            abstract public T ToType(LdValue jsonValue);

            /// <summary>
            /// Initializes an <see cref="LdValue"/> as an array, from a sequence of this type.
            /// </summary>
            /// <remarks>
            /// Values are copied, so subsequent changes to the source values do not affect the array.
            /// </remarks>
            /// <example>
            /// <code>
            ///     var listOfInts = new List&lt;int&gt; { 1, 2, 3 };
            ///     var arrayValue = LdValue.Convert.Int.ArrayFrom(arrayOfInts);
            /// </code>
            /// </example>
            /// <param name="values">a sequence of elements of the specified type</param>
            /// <returns>a struct representing a JSON array, or <see cref="LdValue.Null"/> if the
            /// parameter was null</returns>
            public LdValue ArrayFrom(IEnumerable<T> values)
            {
                if (values is null)
                {
                    return Null;
                }
                var list = new List<LdValue>(values.Count());
                foreach (var value in values)
                {
                    list.Add(FromType(value));
                }
                return new LdValue(list);
            }

            /// <summary>
            /// Initializes an <see cref="LdValue"/> as an array, from a sequence of this type.
            /// </summary>
            /// <remarks>
            /// Values are copied, so subsequent changes to the source values do not affect the array.
            /// </remarks>
            /// <example>
            /// <code>
            ///     var arrayValue = LdValue.Convert.Int.ArrayOf(1, 2, 3);
            /// </code>
            /// </example>
            /// <param name="values">any number of elements of the specified type</param>
            /// <returns>a struct representing a JSON array</returns>
            public LdValue ArrayOf(params T[] values)
            {
                return ArrayFrom(values);
            }

            /// <summary>
            /// Initializes an <see cref="LdValue"/> as a JSON object, from a dictionary containing
            /// values of this type.
            /// </summary>
            /// <remarks>
            /// Values are copied, so subsequent changes to the source values do not affect the array.
            /// </remarks>
            /// <example>
            /// <code>
            ///     var dictionaryOfInts = new Dictionary&lt;string, int&gt; { { "a", 1 }, { "b", 2 } };
            ///     var objectValue = LdValue.Convert.Int.ObjectFrom(dictionaryOfInts);
            /// </code>
            /// </example>
            /// <param name="dictionary">a dictionary with string keys and values of the specified type</param>
            /// <returns>a struct representing a JSON object, or <see cref="LdValue.Null"/> if the
            /// parameter was null</returns>
            public LdValue ObjectFrom(IReadOnlyDictionary<string, T> dictionary)
            {
                if (dictionary is null)
                {
                    return Null;
                }
                var d = new Dictionary<string, LdValue>(dictionary.Count);
                foreach (var e in dictionary)
                {
                    d[e.Key] = FromType(e.Value);
                }
                return new LdValue(d);
            }
        }

        /// <summary>
        /// Predefined instances of <see cref="Converter{T}"/> for commonly used types.
        /// </summary>
        /// <remarks>
        /// These are mostly useful for methods that convert <see cref="LdValue"/> to or from a
        /// collection of some type, such as <see cref="Converter{T}.ArrayOf(T[])"/> and
        /// <see cref="LdValue.AsList{T}(Converter{T})"/>.
        /// </remarks>
        public static class Convert
        {
            /// <summary>
            /// A <see cref="Converter{T}"/> for the <see langword="bool"/> type.
            /// </summary>
            /// <remarks>
            /// Its behavior is consistent with <see cref="LdValue.Of(bool)"/> and
            /// <see cref="LdValue.AsBool"/>.
            /// </remarks>
            public static readonly Converter<bool> Bool = new ConverterImpl<bool>(
                v => LdValue.Of(v),
                j => j.AsBool
            );

            /// <summary>
            /// A <see cref="Converter{T}"/> for the <see langword="int"/> type.
            /// </summary>
            /// <remarks>
            /// Its behavior is consistent with <see cref="LdValue.Of(int)"/> and
            /// <see cref="LdValue.AsInt"/>.
            /// </remarks>
            public static readonly Converter<int> Int = new ConverterImpl<int>(
                v => LdValue.Of(v),
                j => j.AsInt
            );

            /// <summary>
            /// A <see cref="Converter{T}"/> for the <see langword="long"/> type.
            /// </summary>
            /// <remarks>
            /// Its behavior is consistent with <see cref="LdValue.Of(long)"/> and
            /// <see cref="LdValue.AsLong"/>.
            /// </remarks>
            public static readonly Converter<long> Long = new ConverterImpl<long>(
                v => LdValue.Of(v),
                j => j.AsInt
            );

            /// <summary>
            /// A <see cref="Converter{T}"/> for the <see langword="float"/> type.
            /// </summary>
            /// <remarks>
            /// Its behavior is consistent with <see cref="LdValue.Of(float)"/> and
            /// <see cref="LdValue.AsFloat"/>.
            /// </remarks>
            public static readonly Converter<float> Float = new ConverterImpl<float>(
                v => LdValue.Of(v),
                j => j.AsFloat
            );

            /// <summary>
            /// A <see cref="Converter{T}"/> for the <see langword="double"/> type.
            /// </summary>
            /// <remarks>
            /// Its behavior is consistent with <see cref="LdValue.Of(double)"/> and
            /// <see cref="LdValue.AsDouble"/>.
            /// </remarks>
            public static readonly Converter<double> Double = new ConverterImpl<double>(
                v => LdValue.Of(v),
                j => j.AsDouble
            );

            /// <summary>
            /// A <see cref="Converter{T}"/> for the <see cref="string"/> type.
            /// </summary>
            /// <remarks>
            /// Its behavior is consistent with <see cref="LdValue.Of(string)"/> and
            /// <see cref="LdValue.AsString"/>.
            /// </remarks>
            public static readonly Converter<string> String = new ConverterImpl<string>(
                v => LdValue.Of(v),
                j => j.AsString
            );

            /// <summary>
            /// A <see cref="Converter{T}"/> that indicates the value is an <see cref="LdValue"/>
            /// and does not need to be converted.
            /// </summary>
            public static readonly Converter<LdValue> Json = new ConverterImpl<LdValue>(
                v => v,
                j => j
            );

            /// <summary>
            /// Used internally by SDK methods that have to deal with JToken values. Not exposed to applications.
            /// </summary>
            internal static readonly Converter<JToken> UnsafeJToken = new ConverterImpl<JToken>(
                LdValue.FromSafeValue,
                j => j.InnerValue
            );
        }

        private sealed class ConverterImpl<T> : Converter<T>
        {
            private readonly Func<T, LdValue> _fromTypeFn;
            private readonly Func<LdValue, T> _toTypeFn;

            internal ConverterImpl(Func<T, LdValue> fromTypeFn,
                Func<LdValue, T> toTypeFn)
            {
                _fromTypeFn = fromTypeFn;
                _toTypeFn = toTypeFn;
            }

            public override LdValue FromType(T valueOfType) => _fromTypeFn(valueOfType);
            public override T ToType(LdValue jsonValue) => _toTypeFn(jsonValue);
        }

        #endregion
    }
}
