using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using LaunchDarkly.Common;

namespace LaunchDarkly.Client
{
    /// <summary>
    /// An object returned by the "variation detail" methods of the client, combining the result
    /// of a flag evaluation with an explanation of how it was calculated.
    /// </summary>
    /// <remarks>
    /// In future versions of the SDK, this may change from a class to a struct; avoid relying on it
    /// being a class.
    /// </remarks>
    /// <typeparam name="T">the type of the flag value</typeparam>
    public class EvaluationDetail<T>
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
            return Util.Hash().With(Value).With(VariationIndex).With(Reason).Value;
        }
    }

    /// <summary>
    /// Describes the reason that a flag evaluation produced a particular value. Subclasses of
    /// <see cref="EvaluationReason"/> describe specific reasons.
    /// </summary>
    /// <remarks>
    /// Note that this is currently a base class with subclasses corresponding to each <see cref="EvaluationReasonKind"/>.
    /// In a future version of the SDK, it will instead be a value type (struct). Therefore, instead of checking for the
    /// subclasses, use the <see cref="Kind"/> property to distinguish between reasons, and instead of constructing the
    /// subclasses directly, use factory methods/properties such as <see cref="RuleMatchReason(int, string)"/> or
    /// <see cref="FallthroughReason"/>.
    /// </remarks>
    [JsonConverter(typeof(EvaluationReasonConverter))]
    public abstract class EvaluationReason
    {
#pragma warning disable 0618
        private static readonly EvaluationReason ErrorClientNotReady = new Error(EvaluationErrorKind.CLIENT_NOT_READY);
        private static readonly EvaluationReason ErrorFlagNotFound = new Error(EvaluationErrorKind.FLAG_NOT_FOUND);
        private static readonly EvaluationReason ErrorUserNotSpecified = new Error(EvaluationErrorKind.USER_NOT_SPECIFIED);
        private static readonly EvaluationReason ErrorMalformedFlag = new Error(EvaluationErrorKind.MALFORMED_FLAG);
        private static readonly EvaluationReason ErrorWrongType = new Error(EvaluationErrorKind.WRONG_TYPE);
        private static readonly EvaluationReason ErrorException = new Error(EvaluationErrorKind.EXCEPTION);
#pragma warning restore 0618

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
        public static EvaluationReason OffReason => Off.Instance;

        /// <summary>
        /// Returns an EvaluationReason of the kind <see cref="EvaluationReasonKind.FALLTHROUGH"/>.
        /// </summary>
        public static EvaluationReason FallthroughReason => Fallthrough.Instance;

        /// <summary>
        /// Returns an EvaluationReason of the kind <see cref="EvaluationReasonKind.TARGET_MATCH"/>.
        /// </summary>
        public static EvaluationReason TargetMatchReason => TargetMatch.Instance;

        /// <summary>
        /// Returns an EvaluationReason of the kind <see cref="EvaluationReasonKind.RULE_MATCH"/>.
        /// </summary>
        /// <param name="ruleIndex">the rule index</param>
        /// <param name="ruleId">the unique rule ID</param>
        /// <returns>a reason descriptor</returns>
        public static EvaluationReason RuleMatchReason(int ruleIndex, string ruleId) => new RuleMatch(ruleIndex, ruleId);

        /// <summary>
        /// Returns an EvaluationReason of the kind <see cref="EvaluationReasonKind.PREREQUISITE_FAILED"/>.
        /// </summary>
        /// <param name="key">the key of the prerequisite flag</param>
        /// <returns>a reason descriptor</returns>
        public static EvaluationReason PrerequisiteFailedReason(string key) => new PrerequisiteFailed(key);

        /// <summary>
        /// Returns an EvaluationReason of the kind <see cref="EvaluationReasonKind.ERROR"/>.
        /// </summary>
        /// <param name="errorKind"></param>
        /// <returns></returns>
        public static EvaluationReason ErrorReason(EvaluationErrorKind errorKind)
        {
            switch (errorKind)
            {
                case EvaluationErrorKind.CLIENT_NOT_READY: return ErrorClientNotReady;
                case EvaluationErrorKind.FLAG_NOT_FOUND: return ErrorFlagNotFound;
                case EvaluationErrorKind.USER_NOT_SPECIFIED: return ErrorUserNotSpecified;
                case EvaluationErrorKind.MALFORMED_FLAG: return ErrorMalformedFlag;
                case EvaluationErrorKind.WRONG_TYPE: return ErrorWrongType;
                case EvaluationErrorKind.EXCEPTION: return ErrorException;
            }
            return new Error(errorKind);
        }
#pragma warning restore 0618

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
            return Util.Hash().With(_kind).With(_ruleIndex).With(_ruleId).With(_prerequisiteKey).With(_errorKind).Value;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Kind.ToString();
        }

        /// <summary>
        /// Indicates that the flag was off and therefore returned its configured off value.
        /// </summary>
        [Obsolete("EvaluationReason will become a struct and will no longer have subclasses; use its factory methods and Kind")]
        public class Off : EvaluationReason
        {
            private static readonly Off _instance = new Off();

            /// <summary>
            /// The singleton instance of Off.
            /// </summary>
            public static Off Instance => _instance;

            private Off() : base(EvaluationReasonKind.OFF, -1, null, null, EvaluationErrorKind.NONE) { }
        }

        /// <summary>
        /// Indicates that the flag was on but the user did not match any targets or rules.
        /// </summary>
        [Obsolete("EvaluationReason will become a struct and will no longer have subclasses; use its factory methods and Kind")]
        public class Fallthrough : EvaluationReason
        {
            private static readonly Fallthrough _instance = new Fallthrough();

            /// <summary>
            /// The singleton instance of Fallthrough.
            /// </summary>
            public static Fallthrough Instance => _instance;

            private Fallthrough() : base(EvaluationReasonKind.FALLTHROUGH, -1, null, null, EvaluationErrorKind.NONE) { }
        }

        /// <summary>
        /// Indicates that the user key was specifically targeted for this flag.
        /// </summary>
        [Obsolete("EvaluationReason will become a struct and will no longer have subclasses; use its factory methods and Kind")]
        public class TargetMatch : EvaluationReason
        {
            private static readonly TargetMatch _instance = new TargetMatch();

            /// <summary>
            /// The singleton instance of TargetMatch.
            /// </summary>
            public static TargetMatch Instance => _instance;

            private TargetMatch() : base(EvaluationReasonKind.TARGET_MATCH, -1, null, null, EvaluationErrorKind.NONE) { }
        }

        /// <summary>
        /// Indicates that the flag was considered off because it had at least one prerequisite flag
        /// that either was off or did not return the desired variation.
        /// </summary>
        [Obsolete("EvaluationReason will become a struct and will no longer have subclasses; use its factory methods and Kind")]
        public class RuleMatch : EvaluationReason
        {
            /// <summary>
            /// Constructs a new RuleMatch instance.
            /// </summary>
            /// <param name="index">the rule index</param>
            /// <param name="id">the rule ID</param>
            [JsonConstructor]
            public RuleMatch(int index, string id) : base(EvaluationReasonKind.RULE_MATCH, index, id, null, EvaluationErrorKind.NONE) { }
            
            /// <inheritdoc/>
            public override string ToString()
            {
                return Kind + "(" + RuleIndex + "," + RuleId + ")";
            }
        }

        /// <summary>
        /// Indicates that the flag was considered off because it had at least one prerequisite flag
        /// that either was off or did not return the desired variation.
        /// </summary>
        [Obsolete("EvaluationReason will become a struct and will no longer have subclasses; use its factory methods and Kind")]
        public class PrerequisiteFailed : EvaluationReason
        {
            /// <summary>
            /// Constructs a new PrerequisitesFailed instance.
            /// </summary>
            /// <param name="key">the key of the failed prerequisite</param>
            [JsonConstructor]
            public PrerequisiteFailed(string key) : base(EvaluationReasonKind.PREREQUISITE_FAILED, -1, null, key, EvaluationErrorKind.NONE) { }

            /// <inheritdoc/>
            public override string ToString()
            {
                return Kind + "(" + PrerequisiteKey + ")";
            }
        }

        /// <summary>
        /// Indicates that the flag could not be evaluated, e.g. because it does not exist or due to an unexpected
        /// error. In this case the result value will be the default value that the caller passed to the client.
        /// </summary>
        [Obsolete("EvaluationReason will become a struct and will no longer have subclasses; use its factory methods and Kind")]
        public class Error : EvaluationReason
        {
            /// <summary>
            /// Constructs a new Error instance.
            /// </summary>
            /// <param name="errorKind">the type of error</param>
            public Error(EvaluationErrorKind errorKind) : base(EvaluationReasonKind.ERROR, -1, null, null, errorKind) { }

            /// <inheritdoc/>
            public override string ToString()
            {
                return Kind + "(" + ErrorKind + ")";
            }
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
        internal static readonly EvaluationReasonConverter Instance = new EvaluationReasonConverter();

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
            LdValue o = (LdValue)LdValueSerializer.Instance.ReadJson(reader, typeof(LdValue), LdValue.Null, serializer);
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
