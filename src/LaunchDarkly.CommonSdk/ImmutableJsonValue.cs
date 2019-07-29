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

        internal ImmutableJsonValue(JToken value)
        {
            _value = value;
        }

        /// <summary>
        /// Initializes an <c>ImmutableJsonValue</c> from an arbitrary JSON value.
        /// </summary>
        /// <remarks>
        /// If the value is of a mutable type (object or array), it is copied.
        /// </remarks>
        /// <param name="value">the initial value</param>
        /// <returns>a struct that wraps the value</returns>
        public static ImmutableJsonValue FromJToken(JToken value)
        {
            return new ImmutableJsonValue(CloneIfNonPrimitive(value));
        }

        /// <summary>
        /// Converts the value to a boolean.
        /// </summary>
        public bool AsBool => _value.Value<bool>();

        /// <summary>
        /// Converts the value to a string.
        /// </summary>
        public string AsString => _value.Value<string>();

        /// <summary>
        /// Converts the value to an integer.
        /// </summary>
        public int AsInt => _value.Value<int>();

        /// <summary>
        /// Converts the value to a float.
        /// </summary>
        public float AsFloat => _value.Value<float>();

        // This internal method is used only during flag evaluation or JSON serialization,
        // where we know we will not be modifying any mutable objects or arrays and we will
        // not be exposing the value to any external code.
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

        /// <see cref="Object.Equals(object)"/>
        public override bool Equals(object o) => (o is ImmutableJsonValue v) && Equals(v);

        /// <see cref="IEquatable{T}.Equals(T)"/>
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
    }

    internal class ImmutableJsonValueSerializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ((ImmutableJsonValue)value).InnerValue.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Note that we use the constructor directly here instead of calling FromJToken,
            // because we do not need to do a deep copy of the newly-parsed value.
            return new ImmutableJsonValue(JToken.Load(reader));
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ImmutableJsonValue);
        }
    }
}
