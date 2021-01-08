using System;
using Newtonsoft.Json;

namespace LaunchDarkly.Sdk.Internal.Helpers
{
    internal sealed class EvaluationReasonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(EvaluationReason);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is EvaluationReason r))
            {
                throw new ArgumentException();
            }
            writer.WriteStartObject();
            writer.WritePropertyName("kind");
            writer.WriteValue(EvaluationReasonKindJsonConverter.ToIdentifier(r.Kind));
            switch (r.Kind)
            {
                case EvaluationReasonKind.RuleMatch:
                    writer.WritePropertyName("ruleIndex");
                    writer.WriteValue(r.RuleIndex);
                    writer.WritePropertyName("ruleId");
                    writer.WriteValue(r.RuleId);
                    break;
                case EvaluationReasonKind.PrerequisiteFailed:
                    writer.WritePropertyName("prerequisiteKey");
                    writer.WriteValue(r.PrerequisiteKey);
                    break;
                case EvaluationReasonKind.Error:
                    writer.WritePropertyName("errorKind");
                    writer.WriteValue(EvaluationErrorKindJsonConverter.ToIdentifier(r.ErrorKind.Value));
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
            EvaluationReasonKind kind = EvaluationReasonKindJsonConverter.FromIdentifier(o.Get("kind").AsString);
            switch (kind)
            {
                case EvaluationReasonKind.Off:
                    return EvaluationReason.OffReason;
                case EvaluationReasonKind.Fallthrough:
                    return EvaluationReason.FallthroughReason;
                case EvaluationReasonKind.TargetMatch:
                    return EvaluationReason.TargetMatchReason;
                case EvaluationReasonKind.RuleMatch:
                    var index = o.Get("ruleIndex").AsInt;
                    var id = o.Get("ruleId").AsString;
                    return EvaluationReason.RuleMatchReason(index, id);
                case EvaluationReasonKind.PrerequisiteFailed:
                    var key = o.Get("prerequisiteKey").AsString;
                    return EvaluationReason.PrerequisiteFailedReason(key);
                case EvaluationReasonKind.Error:
                    var errorKind = EvaluationErrorKindJsonConverter.FromIdentifier(o.Get("errorKind").AsString);
                    return EvaluationReason.ErrorReason(errorKind);
            }
            throw new ArgumentException();
        }
    }

    internal sealed class EvaluationReasonKindJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(EvaluationReasonKind);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
            FromIdentifier(reader.ReadAsString());

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is EvaluationReasonKind k))
            {
                throw new ArgumentException();
            }
            writer.WriteValue(ToIdentifier(k));
        }

        internal static EvaluationReasonKind FromIdentifier(string value)
        {
            foreach (EvaluationReasonKind k in Enum.GetValues(typeof(EvaluationErrorKind)))
            {
                if (ToIdentifier(k) == value)
                {
                    return k;
                }
            }
            throw new ArgumentException("invalid EvaluationReasonKind");
        }

        internal static string ToIdentifier(EvaluationReasonKind value)
        {
            switch (value)
            {
                case EvaluationReasonKind.Off:
                    return "OFF";
                case EvaluationReasonKind.Fallthrough:
                    return "FALLTHROUGH";
                case EvaluationReasonKind.TargetMatch:
                    return "TARGET_MATCH";
                case EvaluationReasonKind.RuleMatch:
                    return "RULE_MATCH";
                case EvaluationReasonKind.PrerequisiteFailed:
                    return "PREREQUISITE_FAILED";
                case EvaluationReasonKind.Error:
                    return "ERROR";
                default:
                    throw new ArgumentException();
            }
        }
    }

    internal sealed class EvaluationErrorKindJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(EvaluationReasonKind);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
            FromIdentifier(reader.ReadAsString());

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is EvaluationErrorKind k))
            {
                throw new ArgumentException();
            }
            writer.WriteValue(ToIdentifier(k));
        }

        internal static EvaluationErrorKind FromIdentifier(string value)
        {
            foreach (EvaluationErrorKind k in Enum.GetValues(typeof(EvaluationErrorKind)))
            {
                if (ToIdentifier(k) == value)
                {
                    return k;
                }
            }
            throw new ArgumentException("invalid EvaluationErrorKind");
        }

        internal static string ToIdentifier(EvaluationErrorKind value)
        {
            switch (value)
            {
                case EvaluationErrorKind.ClientNotReady:
                    return "CLIENT_NOT_READY";
                case EvaluationErrorKind.FlagNotFound:
                    return "FLAG_NOT_FOUND";
                case EvaluationErrorKind.UserNotSpecified:
                    return "USER_NOT_SPECIFIED";
                case EvaluationErrorKind.MalformedFlag:
                    return "MALFORMED_FLAG";
                case EvaluationErrorKind.WrongType:
                    return "WRONG_TYPE";
                case EvaluationErrorKind.Exception:
                    return "EXCEPTION";
                default:
                    throw new ArgumentException();
            }
        }
    }
}
