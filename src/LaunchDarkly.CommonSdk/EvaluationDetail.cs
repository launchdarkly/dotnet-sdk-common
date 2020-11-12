using System;
using LaunchDarkly.Sdk.Internal.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LaunchDarkly.Sdk
{
    /// <summary>
    /// An object returned by the "variation detail" methods of the client, combining the result
    /// of a flag evaluation with an explanation of how it was calculated.
    /// </summary>
    /// <typeparam name="T">the type of the flag value</typeparam>
    public struct EvaluationDetail<T>
    {
        private readonly T _value;
        private readonly int? _variationIndex;
        private readonly EvaluationReason _reason;

        /// <summary>
        /// The result of the flag evaluation. This will be either one of the flag's variations or the default
        /// value that was specified when the flag was evaluated.
        /// </summary>
        public T Value => _value;

        /// <summary>
        /// The index of the returned value within the flag's list of variations, e.g. 0 for the first variation -
        /// or <see langword="null"/> if the default value was returned.
        /// </summary>
        public int? VariationIndex => _variationIndex;

        /// <summary>
        /// An object describing the main factor that influenced the flag evaluation value.
        /// </summary>
        public EvaluationReason Reason => _reason;

        /// <summary>
        /// True if the flag evaluated to the default value, rather than one of its variations.
        /// </summary>
        public bool IsDefaultValue => _variationIndex == null;

        /// <summary>
        /// Constructs a new EvaluationDetail insetance.
        /// </summary>
        /// <param name="value">the flag value</param>
        /// <param name="variationIndex">the variation index</param>
        /// <param name="reason">the evaluation reason</param>
        public EvaluationDetail(T value, int? variationIndex, EvaluationReason reason)
        {
            _value = value;
            _variationIndex = variationIndex;
            _reason = reason;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is EvaluationDetail<T> o)
            {
                return (Value == null ? o.Value == null : Value.Equals(o.Value))
                    && VariationIndex == o.VariationIndex && Reason.Equals(o.Reason);
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return new HashCodeBuilder().With(Value).With(VariationIndex).With(Reason).Value;
        }
    }

    /// <summary>
    /// Describes the reason that a flag evaluation produced a particular value. Subclasses of
    /// <see cref="EvaluationReason"/> describe specific reasons.
    /// </summary>
    [JsonConverter(typeof(EvaluationReasonConverter))]
    public struct EvaluationReason
    {
        private static readonly EvaluationReason _offInstance =
            new EvaluationReason(EvaluationReasonKind.OFF, -1, null, null, EvaluationErrorKind.NONE);
        private static readonly EvaluationReason _fallthroughInstance =
            new EvaluationReason(EvaluationReasonKind.FALLTHROUGH, -1, null, null, EvaluationErrorKind.NONE);
        private static readonly EvaluationReason _targetMatchInstance =
            new EvaluationReason(EvaluationReasonKind.TARGET_MATCH, -1, null, null, EvaluationErrorKind.NONE);

        private readonly EvaluationReasonKind _kind;
        private readonly int _ruleIndex;
        private readonly string _ruleId;
        private readonly string _prerequisiteKey;
        private readonly EvaluationErrorKind _errorKind;

        // Note that the JsonProperty annotations in this class are used only if application code decides to
        // serialize an EvaluationReason instance. When generating event output, the SDK instead uses the
        // direct stream-writing logic in EventOutputFormatter.

        /// <summary>
        /// An enum indicating the general category of the reason.
        /// </summary>
        public EvaluationReasonKind Kind => _kind;

        /// <summary>
        /// The index of the rule that was matched (0 for the first), or -1 if this is not a rule match.
        /// </summary>
        public int RuleIndex => _ruleIndex;

        /// <summary>
        /// The unique identifier of the rule that was matched, or <see langword="null"/> if this is not a rule match.
        /// </summary>
        public string RuleId => _ruleId;

        /// <summary>
        /// The key of the prerequisite flag that failed, if <see cref="Kind"/> is <see cref="EvaluationReasonKind.PREREQUISITE_FAILED"/>,
        /// otherwise <see langword="null"/>.
        /// </summary>
        public string PrerequisiteKey => _prerequisiteKey;

        /// <summary>
        /// Describes the type of error, if <see cref="Kind"/> is <see cref="EvaluationReasonKind.ERROR"/>,
        /// otherwise <see cref="EvaluationErrorKind.NONE"/>.
        /// </summary>
        public EvaluationErrorKind ErrorKind => _errorKind;

        internal EvaluationReason(EvaluationReasonKind kind, int ruleIndex, string ruleId, string prereqKey, EvaluationErrorKind errorKind)
        {
            _kind = kind;
            _ruleIndex = ruleIndex;
            _ruleId = ruleId;
            _prerequisiteKey = prereqKey;
            _errorKind = errorKind;
        }
        
#pragma warning disable 0618
        /// <summary>
        /// Returns an EvaluationReason of the kind <see cref="EvaluationReasonKind.OFF"/>.
        /// </summary>
        public static EvaluationReason OffReason => _offInstance;

        /// <summary>
        /// Returns an EvaluationReason of the kind <see cref="EvaluationReasonKind.FALLTHROUGH"/>.
        /// </summary>
        public static EvaluationReason FallthroughReason => _fallthroughInstance;

        /// <summary>
        /// Returns an EvaluationReason of the kind <see cref="EvaluationReasonKind.TARGET_MATCH"/>.
        /// </summary>
        public static EvaluationReason TargetMatchReason => _targetMatchInstance;

        /// <summary>
        /// Returns an EvaluationReason of the kind <see cref="EvaluationReasonKind.RULE_MATCH"/>.
        /// </summary>
        /// <param name="ruleIndex">the rule index</param>
        /// <param name="ruleId">the unique rule ID</param>
        /// <returns>a reason descriptor</returns>
        public static EvaluationReason RuleMatchReason(int ruleIndex, string ruleId) =>
            new EvaluationReason(EvaluationReasonKind.RULE_MATCH, ruleIndex, ruleId, null, EvaluationErrorKind.NONE);

        /// <summary>
        /// Returns an EvaluationReason of the kind <see cref="EvaluationReasonKind.PREREQUISITE_FAILED"/>.
        /// </summary>
        /// <param name="key">the key of the prerequisite flag</param>
        /// <returns>a reason descriptor</returns>
        public static EvaluationReason PrerequisiteFailedReason(string key) =>
            new EvaluationReason(EvaluationReasonKind.PREREQUISITE_FAILED, -1, null, key, EvaluationErrorKind.NONE);

        /// <summary>
        /// Returns an EvaluationReason of the kind <see cref="EvaluationReasonKind.ERROR"/>.
        /// </summary>
        /// <param name="errorKind"></param>
        /// <returns></returns>
        public static EvaluationReason ErrorReason(EvaluationErrorKind errorKind) =>
            new EvaluationReason(EvaluationReasonKind.ERROR, -1, null, null, errorKind);

        /// <summary>
        /// Returns the implementation of custom JSON serialization for this type.
        /// </summary>
        public static JsonConverter JsonConverter { get; } = new EvaluationReasonConverter();

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is EvaluationReason o)
            {
                return _kind == o._kind && _ruleId == o._ruleId && _ruleIndex == o._ruleIndex &&
                    _prerequisiteKey == o._prerequisiteKey && _errorKind == o._errorKind;
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return new HashCodeBuilder().With(_kind).With(_ruleIndex).With(_ruleId).With(_prerequisiteKey).With(_errorKind).Value;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            switch (_kind)
            {
                case EvaluationReasonKind.RULE_MATCH:
                    return _kind.ToString() + "(" + _ruleIndex + "," + _ruleId + ")";
                case EvaluationReasonKind.PREREQUISITE_FAILED:
                    return _kind.ToString() + "(" + _prerequisiteKey + ")";
                case EvaluationReasonKind.ERROR:
                    return _kind.ToString() + "(" + _errorKind.ToString() + ")";
            }
            return Kind.ToString();
        }
    }

    /// <summary>
    /// Enumerated type defining the possible values of <see cref="EvaluationReason.Kind"/>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EvaluationReasonKind
    {
        // Note that these do not follow standard C# capitalization style, because their names
        // need to correspond to the values used within the LaunchDarkly application, which are
        // in all caps.

        /// <summary>
        /// Indicates that the flag was off and therefore returned its configured off value.
        /// </summary>
        OFF,
        /// <summary>
        /// Indicates that the flag was on but the user did not match any targets or rules.
        /// </summary>
        FALLTHROUGH,
        /// <summary>
        /// Indicates that the user key was specifically targeted for this flag.
        /// </summary>
        TARGET_MATCH,
        /// <summary>
        /// Indicates that the user matched one of the flag's rules.
        /// </summary>
        RULE_MATCH,
        /// <summary>
        /// Indicates that the flag was considered off because it had at least one prerequisite flag
        /// that either was off or did not return the desired variation.
        /// </summary>
        PREREQUISITE_FAILED,
        /// <summary>
        /// Indicates that the flag could not be evaluated, e.g. because it does not exist or due to an unexpected
        /// error. In this case the result value will be the default value that the caller passed to the client.
        /// </summary>
        ERROR
    }

    /// <summary>
    /// Enumerated type defining the possible values of <see cref="EvaluationReason.ErrorKind"/>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EvaluationErrorKind
    {
        // Note that these do not follow standard C# capitalization style, because their names
        // need to correspond to the values used within the LaunchDarkly application, which are
        // in all caps.

        /// <summary>
        /// Used when the evaluation reason is not an error.
        /// </summary>
        NONE,
        /// <summary>
        /// Indicates that the caller tried to evaluate a flag before the client had successfully initialized.
        /// </summary>
        CLIENT_NOT_READY,
        /// <summary>
        /// Indicates that the caller provided a flag key that did not match any known flag.
        /// </summary>
        FLAG_NOT_FOUND,
        /// <summary>
        /// Indicates that the caller passed <see langword="null"/> for the user parameter, or the user lacked a key.
        /// </summary>
        USER_NOT_SPECIFIED,
        /// <summary>
        /// Indicates that there was an internal inconsistency in the flag data, e.g. a rule specified a nonexistent
        /// variation. An error message will always be logged in this case.
        /// </summary>
        MALFORMED_FLAG,
        /// <summary>
        /// Indicates that the result value was not of the requested type, e.g. you requested a <see langword="bool"/>
        /// but the value was an <see langword="int"/>.
        /// </summary>
        WRONG_TYPE,
        /// <summary>
        /// Indicates that an unexpected exception stopped flag evaluation; check the log for details.
        /// </summary>
        EXCEPTION
    }

    internal sealed class EvaluationReasonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is EvaluationReason r))
            {
                throw new ArgumentException();
            }
            writer.WriteStartObject();
            writer.WritePropertyName("kind");
            writer.WriteValue(r.Kind.ToString());
            switch (r.Kind)
            {
                case EvaluationReasonKind.RULE_MATCH:
                    writer.WritePropertyName("ruleIndex");
                    writer.WriteValue(r.RuleIndex);
                    writer.WritePropertyName("ruleId");
                    writer.WriteValue(r.RuleId);
                    break;
                case EvaluationReasonKind.PREREQUISITE_FAILED:
                    writer.WritePropertyName("prerequisiteKey");
                    writer.WriteValue(r.PrerequisiteKey);
                    break;
                case EvaluationReasonKind.ERROR:
                    writer.WritePropertyName("errorKind");
                    writer.WriteValue(r.ErrorKind.ToString());
                    break;
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            LdValue o = (LdValue)LdValue.JsonConverter.ReadJson(reader, typeof(LdValue), LdValue.Null, serializer);
            if (o.IsNull)
            {
                return null;
            }
            EvaluationReasonKind kind = (EvaluationReasonKind)Enum.Parse(typeof(EvaluationReasonKind), o.Get("kind").AsString);
            switch (kind)
            {
                case EvaluationReasonKind.OFF:
                    return EvaluationReason.OffReason;
                case EvaluationReasonKind.FALLTHROUGH:
                    return EvaluationReason.FallthroughReason;
                case EvaluationReasonKind.TARGET_MATCH:
                    return EvaluationReason.TargetMatchReason;
                case EvaluationReasonKind.RULE_MATCH:
                    var index = o.Get("ruleIndex").AsInt;
                    var id = o.Get("ruleId").AsString;
                    return EvaluationReason.RuleMatchReason(index, id);
                case EvaluationReasonKind.PREREQUISITE_FAILED:
                    var key = o.Get("prerequisiteKey").AsString;
                    return EvaluationReason.PrerequisiteFailedReason(key);
                case EvaluationReasonKind.ERROR:
                    var errorKind = (EvaluationErrorKind)Enum.Parse(typeof(EvaluationErrorKind), o.Get("errorKind").AsString);
                    return EvaluationReason.ErrorReason(errorKind);
            }
            throw new ArgumentException();
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
            // It would be more correct to check typeof(EvaluationReason).IsAssignableFrom(objectType),
            // but you can't do that in .NET Standard 1.6. We won't be called for other types anyway.
        }
    }
}
