using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Client
{
    /// <summary>
    /// An immutable instance of any data type that is allowed in JSON.
    /// </summary>
    /// <remarks>
    /// While the LaunchDarkly SDK uses Newtonsoft.Json types to represent JSON values,
    /// some of those types (object and array) are mutable. In contexts where it is
    /// important for data to remain immutable after it is created, these values are
    /// represented with <c>ImmutableJsonValue</c> instead. It is easily convertible
    /// to primitive types and also to Newtonsoft.Json types.
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
        /// Convenience property for an <c>ImmutableJsonValue</c> that wraps a null value.
        /// </summary>
        public static ImmutableJsonValue Null => new ImmutableJsonValue(null);

        private ImmutableJsonValue(JToken value)
        {
            _value = NormalizePrimitives(value);
        }

        /// <summary>
        /// For internal use only. Initializes an <c>ImmutableJsonValue</c> from an arbitrary JSON
        /// value that we know will not be modified.
        /// </summary>
        /// <remarks>
        /// This method is to be used internally when the SDK has a JToken instance that has not been
        /// exposed to any application code, and the SDK code is never going to call any mutative
        /// methods on that value. In that case, we do not need to perform a deep copy on the value
        /// just to wrap it in an <c>ImmutableJsonValue</c>; a deep copy will be performed anyway
        /// if the application tries to access the JToken.
        /// 
        /// It also performs minor optimizations by using our static JToken instances for true, 0, etc.
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
                        return value.Value<bool>() ? _jsonTrue : _jsonFalse;
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
        /// Initializes an <c>ImmutableJsonValue</c> from an arbitrary JSON value.
        /// </summary>
        /// <remarks>
        /// If the value is of a mutable type (object or array), it is copied.
        /// 
        /// For primitive value types, it is simpler to call the <c>Of</c> methods. This method is only
        /// useful if you already have a <c>JToken</c>.
        /// </remarks>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static ImmutableJsonValue FromJToken(JToken value)
        {
            return new ImmutableJsonValue(value is JContainer ? value.DeepClone() :
                NormalizePrimitives(value));
        }

        /// <summary>
        /// Initializes an <c>ImmutableJsonValue</c> from a boolean value.
        /// </summary>
        /// <remarks>
        /// This method reuses static instances for <c>true</c> and <c>false</c>, so it will never create new objects.
        /// </remarks>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static ImmutableJsonValue Of(bool value)
        {
            return new ImmutableJsonValue(value ? _jsonTrue : _jsonFalse);
        }
        
        /// <summary>
        /// Initializes an <c>ImmutableJsonValue</c> from an integer value.
        /// </summary>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static ImmutableJsonValue Of(int value)
        {
            return new ImmutableJsonValue(value == 0 ? _jsonIntZero : new JValue(value));
        }

        /// <summary>
        /// Initializes an <c>ImmutableJsonValue</c> from a float value.
        /// </summary>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static ImmutableJsonValue Of(float value)
        {
            return new ImmutableJsonValue(value == 0 ? _jsonFloatZero : new JValue(value));
        }

        /// <summary>
        /// Initializes an <c>ImmutableJsonValue</c> from a string value.
        /// </summary>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static ImmutableJsonValue Of(string value)
        {
            return new ImmutableJsonValue(value is null ? null :
                (value.Length == 0 ? _jsonStringEmpty : new JValue(value)));
        }

        /// <summary>
        /// True if the wrapped value is null.
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
        /// If the value is null or is not a boolean, this returns false. It will never throw an exception.
        /// </remarks>
        public bool AsBool => (_value is null || _value.Type != JTokenType.Boolean) ? false : _value.Value<bool>();

        /// <summary>
        /// Converts the value to a string.
        /// </summary>
        /// <remarks>
        /// If the value is null, this returns null. If the value is of a non-string type, it is
        /// converted to a string. It will never throw an exception.
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
        /// If the value is null or is not numeric, this returns zero. It will never throw an exception.
        /// </remarks>
        public int AsInt => IsNumber ? _value.Value<int>() : 0;

        /// <summary>
        /// Converts the value to a float.
        /// </summary>
        /// <remarks>
        /// If the value is null or is not numeric, this returns zero. It will never throw an exception.
        /// </remarks>
        public float AsFloat => IsNumber ? _value.Value<float>() : 0;

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
        /// Returns the value as a <c>JArray</c>, deep-copying it so the original value
        /// cannot be changed.
        /// </summary>
        /// <returns>the array</returns>
        public JArray AsJArray()
        {
            if (_value is JArray)
            {
                return _value.DeepClone() as JArray;
            }
            throw new ArgumentException();
        }

        /// <summary>
        /// Returns the value as a <c>JObject</c>, deep-copying it so the original value
        /// cannot be changed.
        /// </summary>
        /// <returns>the object</returns>
        public JObject AsJObject()
        {
            if (_value is JObject)
            {
                return _value.DeepClone() as JObject;
            }
            throw new ArgumentException();
        }

        /// <summary>
        /// Returns the value as a <c>JToken</c>, deep-copying any mutable values so the
        /// original value cannot be changed.
        /// </summary>
        /// <returns>the value</returns>
        public JToken AsJToken() => _value is JContainer ? _value.DeepClone() : _value;

        /// <summary>
        /// Converts the value to the desired type, deep-copying any mutable values so the
        /// original value cannot be changed.
        /// </summary>
        /// <remarks>
        /// This is identical to <c>AsJToken().Value&lt;T&gt;()</c>.
        /// </remarks>
        /// <typeparam name="T">the desired type</typeparam>
        /// <returns>the value</returns>
        public T Value<T>() => AsJToken().Value<T>();

        /// <summary>
        /// Performs a deep-equality comparison using <c>JToken.DeepEquals</c>.
        /// </summary>
        public override bool Equals(object o) => (o is ImmutableJsonValue v) && Equals(v);

        /// <summary>
        /// Performs a deep-equality comparison using <c>JToken.DeepEquals</c>.
        /// </summary>
        public bool Equals(ImmutableJsonValue o)
        {
            return JToken.DeepEquals(_value, o._value);
        }

        /// <see cref="Object.GetHashCode"/>
        public override int GetHashCode()
        {
            return IsNull ? 0 : _value.GetHashCode();
        }

        /// <see cref="Object.ToString"/>
        public override string ToString()
        {
            return IsNull ? "null" : JsonConvert.SerializeObject(_value);
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
            // Note that we use the constructor directly here instead of calling FromJToken,
            // because we do not need to do a deep copy of the newly-parsed value.
            return ImmutableJsonValue.FromSafeValue(JToken.Load(reader));
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ImmutableJsonValue);
        }
    }
}
