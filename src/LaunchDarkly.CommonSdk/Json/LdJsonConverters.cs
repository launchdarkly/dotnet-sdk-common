using System;
using LaunchDarkly.JsonStream;

namespace LaunchDarkly.Sdk.Json
{
    /// <summary>
    /// Low-level JSON custom serializations for SDK types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Applications normally will not need to reference these types; they are used automatically
    /// when you call <see cref="LdJsonSerialization"/> methods (or <c>System.Text.Json</c>
    /// methods, if that API is available). They are included here for use by other LaunchDarkly
    /// library code.
    /// </para>
    /// <para>
    /// These conversions use the <c>LaunchDarkly.JsonStream</c> library
    /// (https://github.com/launchdarkly/dotnet-jsonstream), which internally uses
    /// <c>System.Text.Json</c> on platforms where that API is available or a custom implementation
    /// otherwise. If an error occurs, they may throw a lower-level exception type such as
    /// <c>LaunchDarkly.JsonStream.JsonReadException</c> or <c>System.Text.Json.JsonException</c>
    /// rather than <c>LaunchDarkly.Sdk.Json.JsonException</c>.
    /// </para>
    /// <para>
    /// Some of these converters also have <c>ReadJsonValue</c> and <c>WriteJsonValue</c> methods.
    /// The reason for this is that the <c>object</c> type used by the regular converter methods
    /// causes boxing/unboxing conversions if the target type is a <c>struct</c>, and if the
    /// overhead of these is a concern it is more efficient to call a strongly typed method.
    /// </para>
    /// </remarks>
    /// <seealso cref="LdJsonSerialization"/>
    public static class LdJsonConverters
    {
#pragma warning disable CS1591 // don't bother with XML comments for these low-level helpers
        public sealed class EvaluationReasonConverter : IJsonStreamConverter
        {
            private static readonly string[] _requiredProperties = new string[] { "kind" };

            public object ReadJson(ref JReader reader) => ReadJsonValue(ref reader);

            public static EvaluationReason ReadJsonValue(ref JReader reader) =>
                ReadJsonInternal(ref reader, false).Value;

            public static EvaluationReason? ReadJsonNullableValue(ref JReader reader) =>
                ReadJsonInternal(ref reader, true);

            private static EvaluationReason? ReadJsonInternal(ref JReader reader, bool nullable)
            {
                var obj = (nullable ? reader.ObjectOrNull() : reader.Object())
                    .WithRequiredProperties(_requiredProperties);
                if (!obj.IsDefined)
                {
                    return null;
                }
                try
                {
                    EvaluationReasonKind kind = EvaluationReasonKind.Error;
                    int? ruleIndex = null;
                    string ruleId = null;
                    string prerequisiteKey = null;
                    EvaluationErrorKind? errorKind = null;
                    bool inExperiment = false;
                    BigSegmentsStatus? bigSegmentsStatus = null;

                    while (obj.Next(ref reader))
                    {
                        var name = obj.Name;
                        if (name == "kind")
                        {
                            try
                            {
                                kind = EvaluationReasonKindConverter.FromIdentifier(reader.String());
                            }
                            catch (ArgumentException)
                            {
                                throw new SyntaxException("unsupported value for \"kind\"", 0);
                            }
                        }
                        else if (name == "ruleIndex")
                        {
                            ruleIndex = reader.Int();
                        }
                        else if (name == "ruleId")
                        {
                            ruleId = reader.String();
                        }
                        else if (name == "prerequisiteKey")
                        {
                            prerequisiteKey = reader.String();
                        }
                        else if (name == "errorKind")
                        {
                            try
                            {
                                errorKind = EvaluationErrorKindConverter.FromIdentifier(reader.String());
                            }
                            catch (ArgumentException)
                            {
                                throw new SyntaxException("unsupported value for \"errorKind\"", 0);
                            }
                        }
                        else if (name == "inExperiment")
                        {
                            inExperiment = reader.Bool();
                        }
                        else if (name == "bigSegmentsStatus")
                        {
                            try
                            {
                                bigSegmentsStatus = BigSegmentsStatusConverter.FromIdentifier(reader.String());
                            }
                            catch (ArgumentException)
                            {
                                throw new SyntaxException("unsupported value for \"bigSegmentsStatus\"", 0);
                            }
                        }
                    }

                    EvaluationReason reason;
                    switch (kind) // it's guaranteed to have a value, otherwise there'd be a required property error above
                    {
                        case EvaluationReasonKind.Off:
                            reason = EvaluationReason.OffReason;
                            break;
                        case EvaluationReasonKind.Fallthrough:
                            reason = EvaluationReason.FallthroughReason;
                            break;
                        case EvaluationReasonKind.TargetMatch:
                            reason = EvaluationReason.TargetMatchReason;
                            break;
                        case EvaluationReasonKind.RuleMatch:
                            reason = EvaluationReason.RuleMatchReason(ruleIndex ?? 0, ruleId);
                            break;
                        case EvaluationReasonKind.PrerequisiteFailed:
                            reason = EvaluationReason.PrerequisiteFailedReason(prerequisiteKey);
                            break;
                        case EvaluationReasonKind.Error:
                            reason = EvaluationReason.ErrorReason(errorKind ?? EvaluationErrorKind.Exception);
                            break;
                        default:
                            return null;
                    }
                    if (inExperiment)
                    {
                        reason = reason.WithInExperiment(true);
                    }
                    if (bigSegmentsStatus.HasValue)
                    {
                        reason = reason.WithBigSegmentsStatus(bigSegmentsStatus);
                    }
                    return reason;
                }
                catch (Exception e)
                {
                    throw reader.TranslateException(e);
                }
            }

            public void WriteJson(object value, IValueWriter writer) =>
                WriteJsonValue((EvaluationReason)value, writer);

            public static void WriteJsonValue(EvaluationReason value, IValueWriter writer)
            {
                var obj = writer.Object();
                obj.Name("kind").String(EvaluationReasonKindConverter.ToIdentifier(value.Kind));
                switch (value.Kind)
                {
                    case EvaluationReasonKind.RuleMatch:
                        obj.Name("ruleIndex").Int(value.RuleIndex ?? 0);
                        obj.Name("ruleId").String(value.RuleId);
                        break;
                    case EvaluationReasonKind.PrerequisiteFailed:
                        obj.Name("prerequisiteKey").String(value.PrerequisiteKey);
                        break;
                    case EvaluationReasonKind.Error:
                        obj.Name("errorKind").String(EvaluationErrorKindConverter.ToIdentifier(value.ErrorKind.Value));
                        break;
                }
                if (value.InExperiment)
                {
                    obj.Name("inExperiment").Bool(true); // omit property if false
                }
                if (value.BigSegmentsStatus.HasValue)
                {
                    obj.Name("bigSegmentsStatus").String(
                        BigSegmentsStatusConverter.ToIdentifier(value.BigSegmentsStatus.Value));
                }
                obj.End();
            }
        }

        public sealed class BigSegmentsStatusConverter : IJsonStreamConverter
        {
            public object ReadJson(ref JReader reader) => ReadJsonValue(ref reader);

            public void WriteJson(object instance, IValueWriter writer) =>
                WriteJsonValue((EvaluationErrorKind)instance, writer);

            public static BigSegmentsStatus ReadJsonValue(ref JReader reader) =>
                FromIdentifier(reader.String());

            public static void WriteJsonValue(EvaluationErrorKind instance, IValueWriter writer) =>
                writer.String(ToIdentifier((BigSegmentsStatus)instance));

            internal static BigSegmentsStatus FromIdentifier(string value)
            {
                foreach (BigSegmentsStatus k in Enum.GetValues(typeof(BigSegmentsStatus)))
                {
                    if (ToIdentifier(k) == value)
                    {
                        return k;
                    }
                }
                throw new ArgumentException("invalid BigSegmentsStatus");
            }

            internal static string ToIdentifier(BigSegmentsStatus value)
            {
                switch (value)
                {
                    case BigSegmentsStatus.Healthy:
                        return "HEALTHY";
                    case BigSegmentsStatus.Stale:
                        return "STALE";
                    case BigSegmentsStatus.NotConfigured:
                        return "NOT_CONFIGURED";
                    case BigSegmentsStatus.StoreError:
                        return "STORE_ERROR";
                    default:
                        throw new ArgumentException();
                }
            }
        }

        /// <summary>
        /// The JSON converter for <see cref="EvaluationErrorKind"/>.
        /// </summary>
        public sealed class EvaluationErrorKindConverter : IJsonStreamConverter
        {
            public object ReadJson(ref JReader reader) => ReadJsonValue(ref reader);

            public void WriteJson(object instance, IValueWriter writer) =>
                WriteJsonValue((EvaluationErrorKind)instance, writer);

            public static EvaluationErrorKind ReadJsonValue(ref JReader reader) =>
                FromIdentifier(reader.String());

            public static void WriteJsonValue(EvaluationErrorKind instance, IValueWriter writer) =>
                writer.String(ToIdentifier((EvaluationErrorKind)instance));

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

        /// <summary>
        /// The JSON converter for <see cref="EvaluationReasonKind"/>.
        /// </summary>
        public sealed class EvaluationReasonKindConverter : IJsonStreamConverter
        {
            public object ReadJson(ref JReader reader) => ReadJsonValue(ref reader);

            public void WriteJson(object instance, IValueWriter writer) =>
                WriteJsonValue((EvaluationReasonKind)instance, writer);

            public EvaluationReasonKind ReadJsonValue(ref JReader reader) =>
                FromIdentifier(reader.String());

            public void WriteJsonValue(EvaluationReasonKind instance, IValueWriter writer) =>
                writer.String(ToIdentifier((EvaluationReasonKind)instance));

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

        /// <summary>
        /// The JSON converter for <see cref="LdValue"/>.
        /// </summary>
        public sealed class LdValueConverter : IJsonStreamConverter
        {
            public object ReadJson(ref JReader reader)
            {
                try
                {
                    return ReadJsonValue(ref reader);
                }
                catch (Exception e)
                {
                    throw reader.TranslateException(e);
                }
            }

            public void WriteJson(object value, IValueWriter writer) =>
                WriteJsonValue((LdValue)value, writer);

            public static void WriteJsonValue(LdValue value, IValueWriter writer)
            {
                switch (value.Type)
                {
                    case LdValueType.Null:
                        writer.Null();
                        break;
                    case LdValueType.Bool:
                        writer.Bool(value.AsBool);
                        break;
                    case LdValueType.Number:
                        var asInt = value.AsInt;
                        var asDouble = value.AsDouble;
                        if ((double)asInt == asDouble)
                        {
                            writer.Int(asInt);
                        }
                        else
                        {
                            writer.Double(asDouble);
                        }
                        break;
                    case LdValueType.String:
                        writer.String(value.AsString);
                        break;
                    case LdValueType.Array:
                        var arr = writer.Array();
                        foreach (var v in value.List)
                        {
                            WriteJsonValue(v, arr);
                        }
                        arr.End();
                        break;
                    case LdValueType.Object:
                        var obj = writer.Object();
                        foreach (var kv in value.Dictionary)
                        {
                            WriteJsonValue(kv.Value, obj.Name(kv.Key));
                        }
                        obj.End();
                        break;
                }
            }

            public static LdValue ReadJsonValue(ref JReader reader)
            {
                var value = reader.Any();
                switch (value.Type)
                {
                    case JsonStream.ValueType.Bool:
                        return LdValue.Of(value.BoolValue);
                    case JsonStream.ValueType.Number:
                        return LdValue.Of(value.NumberValue);
                    case JsonStream.ValueType.String:
                        return LdValue.Of(value.StringValue.ToString());
                    case JsonStream.ValueType.Array:
                        var arrayBuilder = LdValue.BuildArray();
                        for (var arr = value.ArrayValue; arr.Next(ref reader);)
                        {
                            arrayBuilder.Add(ReadJsonValue(ref reader));
                        }
                        return arrayBuilder.Build();
                    case JsonStream.ValueType.Object:
                        var objBuilder = LdValue.BuildObject();
                        for (var obj = value.ObjectValue; obj.Next(ref reader);)
                        {
                            objBuilder.Add(obj.Name.ToString(), ReadJsonValue(ref reader));
                        }
                        return objBuilder.Build();
                    default:
                        return LdValue.Null;
                }
            }
        }

        /// <summary>
        /// The JSON converter for <see cref="User"/>.
        /// </summary>
        public sealed class UserConverter : IJsonStreamConverter
        {
            private static readonly string[] _requiredProperties = new string[] { "key" };

            public object ReadJson(ref JReader reader)
            {
                var obj = reader.ObjectOrNull().WithRequiredProperties(_requiredProperties);
                if (!obj.IsDefined)
                {
                    return null;
                }
                var builder = User.Builder("");
                try
                {
                    while (obj.Next(ref reader))
                    {
                        switch (obj.Name.ToString())
                        {
                            case "key":
                                builder.Key(reader.String());
                                break;
                            case "secondary":
                                builder.Secondary(reader.StringOrNull());
                                break;
                            case "ip":
                                builder.IPAddress(reader.StringOrNull());
                                break;
                            case "country":
                                builder.Country(reader.StringOrNull());
                                break;
                            case "firstName":
                                builder.FirstName(reader.StringOrNull());
                                break;
                            case "lastName":
                                builder.LastName(reader.StringOrNull());
                                break;
                            case "name":
                                builder.Name(reader.StringOrNull());
                                break;
                            case "avatar":
                                builder.Avatar(reader.StringOrNull());
                                break;
                            case "email":
                                builder.Email(reader.StringOrNull());
                                break;
                            case "anonymous":
                                builder.AnonymousOptional(reader.BoolOrNull());
                                break;
                            case "custom":
                                for (var customObj = reader.ObjectOrNull(); customObj.Next(ref reader);)
                                {
                                    builder.Custom(customObj.Name.ToString(),
                                        LdValueConverter.ReadJsonValue(ref reader));
                                }
                                break;
                            case "privateAttributeNames":
                                var internalBuilder = builder as UserBuilder;
                                for (var arr = reader.ArrayOrNull(); arr.Next(ref reader);)
                                {
                                    internalBuilder.AddPrivateAttribute(reader.String());
                                }
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw reader.TranslateException(e);
                }
                return builder.Build();
            }

            public void WriteJson(object value, IValueWriter writer)
            {
                var user = (User)value;
                if (user is null)
                {
                    writer.Null();
                    return;
                }
                var obj = writer.Object();
                obj.Name("key").String(user.Key);
                obj.MaybeName("secondary", user.Secondary != null).String(user.Secondary);
                obj.MaybeName("ip", user.IPAddress != null).String(user.IPAddress);
                obj.MaybeName("country", user.Country != null).String(user.Country);
                obj.MaybeName("firstName", user.FirstName != null).String(user.FirstName);
                obj.MaybeName("lastName", user.LastName != null).String(user.LastName);
                obj.MaybeName("name", user.Name != null).String(user.Name);
                obj.MaybeName("avatar", user.Avatar != null).String(user.Avatar);
                obj.MaybeName("email", user.Email != null).String(user.Email);
                if (user.AnonymousOptional.HasValue)
                {
                    obj.Name("anonymous").Bool(user.Anonymous);
                }
                if (user.Custom.Count > 0)
                {
                    var customObj = obj.Name("custom").Object();
                    foreach (var kv in user.Custom)
                    {
                        LdValueConverter.WriteJsonValue(kv.Value, customObj.Name(kv.Key));
                    }
                    customObj.End();
                }
                if (user.PrivateAttributeNames.Count > 0)
                {
                    var attrsArr = obj.Name("privateAttributeNames").Array();
                    foreach (var n in user.PrivateAttributeNames)
                    {
                        attrsArr.String(n);
                    }
                    attrsArr.End();
                }
                obj.End();
            }
        }
    }
#pragma warning restore CS1591
}
