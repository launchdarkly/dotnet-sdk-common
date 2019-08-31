using System;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Common
{
    internal sealed class ValueTypeException : Exception
    {
        public ValueTypeException() : base("The value cannot be converted to the desired type") { }
    }

    /// <summary>
    /// A type-safe standardized mechanism for converting between JSON and all of the value types
    /// supported by LaunchDarkly SDKs. Use the predefined <see cref="ValueTypes"/> instances.
    /// </summary>
    /// <remarks>
    /// This allows the SDK <c>Variation</c> methods to be implemented more simply and without casting.
    /// 
    /// All of these types require an exact match with the JSON type except as follows:
    /// 
    /// 1. <c>Json</c> allows any type.
    /// 
    /// 2. <c>Int</c> and <c>Float</c> are transparently convertible to each other (this is necessary
    /// because JSON only really has one numeric type). Note that conversion from float to int uses
    /// the behavior defined by Newtonsoft.Json, which rounds to the nearest integer rather than
    /// rounding down. This behavior is preserved for backward compatibility with the .NET SDK.
    /// 
    /// 3. <c>String</c> and <c>Json</c> can be converted from either an actual <see langword="null"/> or a
    /// <see cref="JToken"/> of type <see cref="JTokenType.Null"/>.
    /// 
    /// This is a struct rather than a class so that in any context where we expect a <c>ValueType</c>,
    /// we do not have to worry about it being null.
    /// </remarks>
    /// <typeparam name="T">the desired type</typeparam>
    internal struct ValueType<T>
    {
        /// <summary>
        /// Function for converting a JSON value to the desired type.
        /// </summary>
        /// <remarks>
        /// If the JSON value is not of a compatible type, this function will throw a
        /// <see cref="ValueTypeException"/>.
        /// 
        /// If we ever drop compatibility with older .NET frameworks that do not support <c>ValueTuple</c>,
        /// then it would be preferable to use <c>ValueTuple</c> to avoid the overhead of exceptions.
        /// However, these exceptions should not happen often.
        /// </remarks>
        public Func<JToken, T> ValueFromJson { get; }

        /// <summary>
        /// Function for converting the desired type to a JSON value.
        /// </summary>
        public Func<T, JToken> ValueToJson { get; }

        internal ValueType(Func<JToken, T> valueFromJson, Func<T, JToken> valueToJson)
        {
            ValueFromJson = valueFromJson;
            ValueToJson = valueToJson;
        }
    }
    
    internal static class ValueTypes
    {
        // Slight optimization to avoid creating lots of JValue boolean instances
        private static readonly JToken JBoolFalse = new JValue(false);
        private static readonly JToken JBoolTrue = new JValue(true);

        public static readonly ValueType<bool> Bool = new ValueType<bool>(
            json =>
            {
                if (json is null || json.Type != JTokenType.Boolean)
                {
                    throw new ValueTypeException();
                }
                return json.Value<bool>();
            },
            value => value ? JBoolTrue : JBoolFalse
        );

        public static readonly ValueType<int> Int = new ValueType<int>(
            json => IsNumeric(json) ? json.Value<int>() : throw new ValueTypeException(),
            value => new JValue(value)
        );

        public static readonly ValueType<float> Float = new ValueType<float>(
            json => IsNumeric(json) ? json.Value<float>() : throw new ValueTypeException(),
            value => new JValue(value)
        );

        public static readonly ValueType<string> String = new ValueType<string>(
            json =>
            {
                if (json == null || json.Type == JTokenType.Null)
                {
                    return null; // strings are always nullable
                }
                if (json.Type != JTokenType.String)
                {
                    throw new ValueTypeException();
                }
                return json.Value<string>();
            },
            value => value == null ? null : new JValue(value)
        );

        internal static readonly ValueType<JToken> Json = new ValueType<JToken>(
            json => json,
            value => value
        );

        internal static bool IsNumeric(JToken json) => !(json is null) &&
            (json.Type == JTokenType.Integer || json.Type == JTokenType.Float);
    }
}
