using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

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

        /// <see cref="object.Equals(object)"/>
        public override bool Equals(object obj)
        {
            if (obj is EvaluationDetail<T> o)
            {
                return (Value == null ? o.Value == null : Value.Equals(o.Value))
                    && VariationIndex == o.VariationIndex && Reason.Equals(o.Reason);
            }
            return false;
        }

        /// <see cref="object.GetHashCode()"/>
        public override int GetHashCode()
        {
            return ((Value == null ? 0 : Value.GetHashCode()) * 17 +
                VariationIndex.GetHashCode()) * 17 +
                (Reason == null ? 0 : Reason.GetHashCode());
        }
    }

    /// <summary>
    /// Describes the reason that a flag evaluation produced a particular value. Subclasses of
    /// EvaluationReason describe specific reasons.
    /// </summary>
    [JsonConverter(typeof(EvaluationReasonConverter))]
    public abstract class EvaluationReason
    {
        private readonly EvaluationReasonKind _kind;

        /// <summary>
        /// An enum indicating the general category of the reason.
        /// </summary>
        [JsonProperty(PropertyName = "kind")]
        public EvaluationReasonKind Kind => _kind;

        internal EvaluationReason(EvaluationReasonKind kind)
        {
            _kind = kind;
        }

        /// <see cref="object.ToString()"/>
        public override string ToString()
        {
            return Kind.ToString();
        }

        /// <summary>
        /// Indicates that the flag was off and therefore returned its configured off value.
        /// </summary>
        public class Off : EvaluationReason
        {
            private static readonly Off _instance = new Off();

            /// <summary>
            /// The singleton instance of Off.
            /// </summary>
            public static Off Instance => _instance;

            private Off() : base(EvaluationReasonKind.OFF) { }
        }

        /// <summary>
        /// Indicates that the flag was on but the user did not match any targets or rules.
        /// </summary>
        public class Fallthrough : EvaluationReason
        {
            private static readonly Fallthrough _instance = new Fallthrough();

            /// <summary>
            /// The singleton instance of Fallthrough.
            /// </summary>
            public static Fallthrough Instance => _instance;

            private Fallthrough() : base(EvaluationReasonKind.FALLTHROUGH) { }
        }

        /// <summary>
        /// Indicates that the user key was specifically targeted for this flag.
        /// </summary>
        public class TargetMatch : EvaluationReason
        {
            private static readonly TargetMatch _instance = new TargetMatch();

            /// <summary>
            /// The singleton instance of TargetMatch.
            /// </summary>
            public static TargetMatch Instance => _instance;

            private TargetMatch() : base(EvaluationReasonKind.TARGET_MATCH) { }
        }

        /// <summary>
        /// Indicates that the flag was considered off because it had at least one prerequisite flag
        /// that either was off or did not return the desired variation.
        /// </summary>
        public class RuleMatch : EvaluationReason
        {
            private readonly int _ruleIndex;
            private readonly string _ruleId;

            /// <summary>
            /// The index of the rule that was matched (0 for the first).
            /// </summary>
            [JsonProperty(PropertyName = "ruleIndex")]
            public int RuleIndex => _ruleIndex;

            /// <summary>
            /// The unique identifier of the rule that was matched.
            /// </summary>
            [JsonProperty(PropertyName = "ruleId")]
            public string RuleId => _ruleId;

            /// <summary>
            /// Constructs a new RuleMatch instance.
            /// </summary>
            /// <param name="index">the rule index</param>
            /// <param name="id">the rule ID</param>
            [JsonConstructor]
            public RuleMatch(int index, string id) : base(EvaluationReasonKind.RULE_MATCH)
            {
                _ruleIndex = index;
                _ruleId = id;
            }

            /// <see cref="object.Equals(object)"/>
            public override bool Equals(object obj)
            {
                if (obj is RuleMatch o)
                {
                    return _ruleIndex == o._ruleIndex && _ruleId == o._ruleId;
                }
                return false;
            }

            /// <see cref="object.GetHashCode()"/>
            public override int GetHashCode()
            {
                return RuleIndex * 17 + RuleId.GetHashCode();
            }

            /// <see cref="object.ToString()"/>
            public override string ToString()
            {
                return Kind + "(" + RuleIndex + "," + RuleId + ")";
            }
        }

        /// <summary>
        /// Indicates that the flag was considered off because it had at least one prerequisite flag
        /// that either was off or did not return the desired variation.
        /// </summary>
        public class PrerequisiteFailed : EvaluationReason
        {
            private readonly string _prerequisiteKey;

            /// <summary>
            /// The key of the prerequisite flag that failed.
            /// </summary>
            [JsonProperty(PropertyName = "prerequisiteKey")]
            public string PrerequisiteKey => _prerequisiteKey;

            /// <summary>
            /// Constructs a new PrerequisitesFailed instance.
            /// </summary>
            /// <param name="key">the key of the failed prerequisite</param>
            [JsonConstructor]
            public PrerequisiteFailed(string key) : base(EvaluationReasonKind.PREREQUISITE_FAILED)
            {
                _prerequisiteKey = key;
            }

            /// <see cref="object.Equals(object)"/>
            public override bool Equals(object obj)
            {
                if (obj is PrerequisiteFailed o)
                {
                    return PrerequisiteKey == o.PrerequisiteKey;
                }
                return false;
            }

            /// <see cref="object.GetHashCode()"/>
            public override int GetHashCode()
            {
                return PrerequisiteKey.GetHashCode();
            }

            /// <see cref="object.ToString()"/>
            public override string ToString()
            {
                return Kind + "(" + PrerequisiteKey + ")";
            }
        }

        /// <summary>
        /// Indicates that the flag could not be evaluated, e.g. because it does not exist or due to an unexpected
        /// error. In this case the result value will be the default value that the caller passed to the client.
        /// </summary>
        public class Error : EvaluationReason
        {
            private readonly EvaluationErrorKind _errorKind;

            /// <summary>
            /// Describes the type of error.
            /// </summary>
            [JsonProperty(PropertyName = "errorKind")]
            public EvaluationErrorKind ErrorKind => _errorKind;

            /// <summary>
            /// Constructs a new Error instance.
            /// </summary>
            /// <param name="errorKind">the type of error</param>
            public Error(EvaluationErrorKind errorKind) : base(EvaluationReasonKind.ERROR)
            {
                _errorKind = errorKind;
            }

            /// <see cref="object.Equals(object)"/>
            public override bool Equals(object obj)
            {
                if (obj is Error o)
                {
                    return ErrorKind == o.ErrorKind;
                }
                return false;
            }

            /// <see cref="object.GetHashCode()"/>
            public override int GetHashCode()
            {
                return ErrorKind.GetHashCode();
            }

            /// <see cref="object.ToString()"/>
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
    /// Enumerated type defining the possible values of <see cref="EvaluationReason.Error.ErrorKind"/>.
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

    // Note that while the default serialization will work fine for the reason classes, we also need
    // to be able to deserialize from JSON. This is because the Xamarin client may receive JSONified
    // reason objects from LaunchDarkly.
    internal class EvaluationReasonConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject o = serializer.Deserialize<JObject>(reader);
            if (o == null)
            {
                return null;
            }
            EvaluationReasonKind kind = o.GetValue("kind").ToObject<EvaluationReasonKind>();
            switch (kind)
            {
                case EvaluationReasonKind.OFF:
                    return EvaluationReason.Off.Instance;
                case EvaluationReasonKind.FALLTHROUGH:
                    return EvaluationReason.Fallthrough.Instance;
                case EvaluationReasonKind.TARGET_MATCH:
                    return EvaluationReason.TargetMatch.Instance;
                case EvaluationReasonKind.RULE_MATCH:
                    var index = (int)o.GetValue("ruleIndex");
                    var id = (string)o.GetValue("ruleId");
                    return new EvaluationReason.RuleMatch(index, id);
                case EvaluationReasonKind.PREREQUISITE_FAILED:
                    var key = (string)o.GetValue("prerequisiteKey");
                    return new EvaluationReason.PrerequisiteFailed(key);
                case EvaluationReasonKind.ERROR:
                    var errorKind = o.GetValue("errorKind").ToObject<EvaluationErrorKind>();
                    return new EvaluationReason.Error(errorKind);
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
