using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LaunchDarkly.Client
{
    /// <summary>
    /// An object returned by the "variation detail" methods of the client, combining the result
    /// of a flag evaluation with an explanation of how it was calculated.
    /// </summary>
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
        /// or <c>null</c> if the default value was returned.
        /// </summary>
        public int? VariationIndex => _variationIndex;

        /// <summary>
        /// An object describing the main factor that influenced the flag evaluation value.
        /// </summary>
        public EvaluationReason Reason => _reason;

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
    }

    /// <summary>
    /// Describes the reason that a flag evaluation produced a particular value.
    /// </summary>
    public class EvaluationReason
    {
        /// <summary>
        /// An enum indicating the general category of the reason.
        /// </summary>
        [JsonProperty(PropertyName = "kind")]
        public EvaluationReasonKind Kind { get; internal set; }

        /// <summary>
        /// If <c>Kind</c> is <c>ERROR</c>, this is an enum indicating the nature of the error.
        /// It is null otherwise.
        /// </summary>
        [JsonProperty(PropertyName = "errorKind", NullValueHandling = NullValueHandling.Ignore)]
        public EvaluationErrorKind? ErrorKind { get; internal set; }

        /// <summary>
        /// If <c>Kind</c> is <c>RULE_MATCH</c>, this is the positional index of the matched rule
        /// (0 for the first). It is null otherwise.
        /// </summary>
        [JsonProperty(PropertyName = "ruleIndex", NullValueHandling = NullValueHandling.Ignore)]
        public int? RuleIndex { get; internal set; }

        /// <summary>
        /// If <c>Kind</c> is <c>RULE_MATCH</c>, this is the unique identifier of the matched rule.
        /// It is null otherwise.
        /// </summary>
        [JsonProperty(PropertyName = "ruleId", NullValueHandling = NullValueHandling.Ignore)]
        public string RuleId { get; internal set; }

        /// <summary>
        /// If <c>Kind</c> is <c>PREREQUISITES_FAILED</c>, this is a list of the keys of the failed
        /// prerequisite flags. It is null otherwise.
        /// </summary>
        [JsonProperty(PropertyName = "prerequisiteKeys", NullValueHandling = NullValueHandling.Ignore)]
        public IList<string> PrerequisiteKeys { get; internal set; }

        /// <summary>
        /// Convenience method for constructing an error reason.
        /// </summary>
        /// <param name="kind">the type of error</param>
        /// <returns>an EvaluationReason</returns>
        public static EvaluationReason Error(EvaluationErrorKind kind)
        {
            return new EvaluationReason
            {
                Kind = EvaluationReasonKind.ERROR,
                ErrorKind = kind
            };
        }

        /// <see cref="object.ToString()"/>
        public override string ToString()
        {
            switch (Kind)
            {
                case EvaluationReasonKind.RULE_MATCH:
                    return Kind + "(" + RuleIndex + "," + RuleId + ")";
                case EvaluationReasonKind.PREREQUISITES_FAILED:
                    return Kind + "(" + ((PrerequisiteKeys == null) ? "" : string.Join(",", PrerequisiteKeys)) + ")";
                case EvaluationReasonKind.ERROR:
                    return Kind + "(" + ErrorKind + ")";
                default:
                    return Kind.ToString();
            }
        }

        /// <see cref="object.Equals(object)"/>
        public override bool Equals(object obj)
        {
            if (obj is EvaluationReason o)
            {
                return Kind == o.Kind &&
                    ErrorKind == o.ErrorKind &&
                    RuleIndex == o.RuleIndex &&
                    RuleId == o.RuleId &&
                    (PrerequisiteKeys == null) ? (o.PrerequisiteKeys == null) : PrerequisiteKeys.SequenceEqual(o.PrerequisiteKeys);
            }
            return false;
        }

        /// <see cref="object.GetHashCode()"/>
        public override int GetHashCode()
        {
            return (((Kind.GetHashCode() * 17 +
                ErrorKind.GetHashCode()) * 17 +
                RuleIndex.GetHashCode()) * 17 +
                (RuleId == null ? 0 : RuleId.GetHashCode())) * 17 +
                (PrerequisiteKeys == null ? 0 : PrerequisiteKeys.GetHashCode());
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
        PREREQUISITES_FAILED,
        /// <summary>
        /// Indicates that the flag was on but the user did not match any targets or rules.
        /// </summary>
        FALLTHROUGH,
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
        /// Indicates that the caller tried to evaluate a flag before the client had successfully initialized.
        /// </summary>
        CLIENT_NOT_READY,
        /// <summary>
        /// Indicates that the caller provided a flag key that did not match any known flag.
        /// </summary>
        FLAG_NOT_FOUND,
        /// <summary>
        /// Indicates that the caller passed <c>null</c> for the user parameter, or the user lacked a key.
        /// </summary>
        USER_NOT_SPECIFIED,
        /// <summary>
        /// Indicates that there was an internal inconsistency in the flag data, e.g. a rule specified a nonexistent
        /// variation. An error message will always be logged in this case.
        /// </summary>
        MALFORMED_FLAG,
        /// <summary>
        /// Indicates that the result value was not of the requested type, e.g. you requested a <c>bool</c>
        /// but the value was an <c>int</c>.
        /// </summary>
        WRONG_TYPE,
        /// <summary>
        /// Indicates that an unexpected exception stopped flag evaluation; check the log for details.
        /// </summary>
        EXCEPTION
    }
}
