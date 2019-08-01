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
        private readonly JToken _value;

        /// <summary>
        /// Convenience property for an <c>ImmutableJsonValue</c> that wraps a null value.
        /// </summary>
        public static ImmutableJsonValue Null => new ImmutableJsonValue(null);

        private ImmutableJsonValue(JToken value)
        {
            if (!(value is null) && value.Type == JTokenType.Null)
            {
                // Newtonsoft.Json sometimes gives us real nulls and sometimes gives us "nully" objects.
                // Normalize these to null.
                _value = null;
            }
            else
            { 
                _value = value;
            }
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
        /// </remarks>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        internal static ImmutableJsonValue FromSafeValue(JToken value)
        {
            return new ImmutableJsonValue(value);
        }

        /// <summary>
        /// Initializes an <c>ImmutableJsonValue</c> from an arbitrary JSON value.
        /// </summary>
        /// <remarks>
        /// If the value is of a mutable type (object or array), it is copied.
        /// 
        /// Since <c>JToken</c> has implicit conversion operators for primitive types, you can simplify
        /// expressions like <c>ImmutableJsonValue.Of(new JValue(3))</c> to just
        /// <c>ImmutableJsonValue.Of(3)</c>.
        /// </remarks>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static ImmutableJsonValue Of(JToken value)
        {
            return new ImmutableJsonValue(CloneIfNonPrimitive(value));
        }

        /// <summary>
        /// True if the wrapped value is null.
        /// </summary>
        public bool IsNull => _value is null;

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
        public int AsInt => IsNumeric(_value) ? _value.Value<int>() : 0;

        /// <summary>
        /// Converts the value to a float.
        /// </summary>
        /// <remarks>
        /// If the value is null or is not numeric, this returns zero. It will never throw an exception.
        /// </remarks>
        public float AsFloat => IsNumeric(_value) ? _value.Value<float>() : 0;

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
        public JToken AsJToken() => CloneIfNonPrimitive(_value);

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
            return _value is null ? 0 : _value.GetHashCode();
        }

        private static JToken CloneIfNonPrimitive(JToken t)
        {
            if (t is JArray || t is JObject)
            {
                return t.DeepClone();
            }
            return t;
        }

        private static bool IsNumeric(JToken t)
        {
            return !(t is null) && (t.Type == JTokenType.Integer || t.Type == JTokenType.Float);
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
