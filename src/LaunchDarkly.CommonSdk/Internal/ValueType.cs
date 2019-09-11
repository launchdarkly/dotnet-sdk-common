using System;
using LaunchDarkly.Client;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Common
{
    internal sealed class ValueTypeException : Exception
    {
        public ValueTypeException() : base("The value cannot be converted to the desired type") { }
    }

    /// <summary>
    /// A type-safe standardized mechanism for converting between JSON and all of the value types
    /// supported by LaunchDarkly SDKs. Use the predefined <see cref="ValueTypes"/> instances. Unlike
    /// the type conversions in <see cref="LdValue"/> which never throw exceptions, these
    /// converters will throw a <see cref="ValueTypeException"/> if an incompatible type is requested,
    /// because the SDK evaluation methods need to be able to detect this condition.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This allows the SDK <c>Variation</c> methods to be implemented more simply and without casting.
    /// </para>
    /// <para>
    /// All of these types require an exact match with the JSON type except as follows:
    /// </para>
    /// <para>
    /// 1. <c>Json</c> allows any type.
    /// </para>
    /// <para>
    /// 2. <c>Int</c> and <c>Float</c> are transparently convertible to each other (this is necessary
    /// because JSON only really has one numeric type).
    /// </para>
    /// <para>
    /// 3. <c>String</c> and <c>Json</c> can be converted from a null value.
    /// </para>
    /// <para>
    /// This is a struct rather than a class so that in any context where we expect a <c>ValueType</c>,
    /// we do not have to worry about it being null.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">the desired type</typeparam>
    internal struct ValueType<T>
    {
        /// <summary>
        /// Function for converting a JSON value to the desired type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the JSON value is not of a compatible type, this function will throw a
        /// <see cref="ValueTypeException"/>.
        /// </para>
        /// <para>
        /// If we ever drop compatibility with older .NET frameworks that do not support <c>ValueTuple</c>,
        /// then it would be preferable to use <c>ValueTuple</c> to avoid the overhead of exceptions.
        /// However, these exceptions should not happen often.
        /// </para>
        /// </remarks>
        public Func<LdValue, T> ValueFromJson { get; }

        /// <summary>
        /// Function for converting the desired type to a JSON value.
        /// </summary>
        public Func<T, LdValue> ValueToJson { get; }

        internal ValueType(Func<LdValue, T> valueFromJson, Func<T, LdValue> valueToJson)
        {
            ValueFromJson = valueFromJson;
            ValueToJson = valueToJson;
        }
    }

    internal static class ValueTypes
    {
        public static readonly ValueType<bool> Bool = new ValueType<bool>(
            json =>
            {
                if (json.InnerValue is null || json.InnerValue.Type != JTokenType.Boolean)
                {
                    throw new ValueTypeException();
                }
                return json.AsBool;
            },
            value => LdValue.Of(value)
        );

        // Note that Int uses ImmutableJsonValue's rounding behavior for floating-point values
        // (round toward zero), rather than using Newtonsoft.Json's default behavior (round to
        // nearest). That is inconsistent with .NET SDK 2.x, but consistent with most or all of
        // our other strongly-typed SDKs.
        public static readonly ValueType<int> Int = new ValueType<int>(
            json => json.IsNumber ? json.AsInt : throw new ValueTypeException(),
            value => LdValue.Of(value)
        );

        public static readonly ValueType<float> Float = new ValueType<float>(
            json => json.IsNumber ? json.AsFloat : throw new ValueTypeException(),
            value => LdValue.Of(value)
        );

        public static readonly ValueType<string> String = new ValueType<string>(
            json =>
            {
                if (json.IsNull)
                {
                    return null; // strings are always nullable
                }
                if (json.Type != JsonValueType.String)
                {
                    throw new ValueTypeException();
                }
                return json.AsString;
            },
            value => LdValue.Of(value)
        );
        
        internal static readonly ValueType<LdValue> Json = new ValueType<LdValue>(
            json => json,
            value => value
        );
    }
}
