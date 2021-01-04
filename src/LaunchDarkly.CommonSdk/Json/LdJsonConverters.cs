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
    /// when you call <see cref="LdJsonSerialization"/> methods. They are included here for use by
    /// other LaunchDarkly library code.
    /// </para>
    /// <para>
    /// These conversions use the <c>LaunchDarkly.JsonStream</c> library
    /// (https://github.com/launchdarkly/dotnet-jsonstream), which internally uses
    /// <c>System.Text.Json</c> on platforms where that API is available or a custom implementation
    /// otherwise.
    /// </para>
    /// </remarks>
    /// <seealso cref="LdJsonSerialization"/>
    public static class LdJsonConverters
    {
#pragma warning disable CS1591 // don't bother with XML comments for these low-level helpers
        public sealed class EvaluationReasonConverter : IJsonStreamConverter<EvaluationReason>
        {
            private static readonly string[] _requiredProperties = new string[] { "kind" };

            public EvaluationReason ReadJson(ref JReader reader) =>
                ReadJsonInternal(ref reader, false).Value;

            public EvaluationReason? ReadJsonNullable(ref JReader reader) =>
                ReadJsonInternal(ref reader, true);

            private EvaluationReason? ReadJsonInternal(ref JReader reader, bool nullable)
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
                    }

                    switch (kind) // it's guaranteed to have a value, otherwise there'd be a required property error above
                    {
                        case EvaluationReasonKind.Off:
                            return EvaluationReason.OffReason;
                        case EvaluationReasonKind.Fallthrough:
                            return EvaluationReason.FallthroughReason;
                        case EvaluationReasonKind.TargetMatch:
                            return EvaluationReason.TargetMatchReason;
                        case EvaluationReasonKind.RuleMatch:
                            return EvaluationReason.RuleMatchReason(ruleIndex ?? 0, ruleId);
                        case EvaluationReasonKind.PrerequisiteFailed:
                            return EvaluationReason.PrerequisiteFailedReason(prerequisiteKey);
                        case EvaluationReasonKind.Error:
                            return EvaluationReason.ErrorReason(errorKind ?? EvaluationErrorKind.Exception);
                        default:
                            return null;
                    }
                }
                catch (Exception e)
                {
                    throw reader.TranslateException(e);
                }
            }

            public void WriteJson(EvaluationReason value, IValueWriter writer)
            {
                var obj = writer.Object();
                obj.Property("kind").String(EvaluationReasonKindConverter.ToIdentifier(value.Kind));
                switch (value.Kind)
                {
                    case EvaluationReasonKind.RuleMatch:
                        obj.Property("ruleIndex").Int(value.RuleIndex ?? 0);
                        obj.Property("ruleId").String(value.RuleId);
                        break;
                    case EvaluationReasonKind.PrerequisiteFailed:
                        obj.Property("prerequisiteKey").String(value.PrerequisiteKey);
                        break;
                    case EvaluationReasonKind.Error:
                        obj.Property("errorKind").String(EvaluationErrorKindConverter.ToIdentifier(value.ErrorKind.Value));
                        break;
                }
                obj.End();
            }
        }

        /// <summary>
        /// The JSON converter for <see cref="EvaluationErrorKind"/>.
        /// </summary>
        public sealed class EvaluationErrorKindConverter : IJsonStreamConverter<EvaluationErrorKind>
        {
            public EvaluationErrorKind ReadJson(ref JReader reader) =>
                FromIdentifier(reader.String());

            public void WriteJson(EvaluationErrorKind instance, IValueWriter writer) =>
                writer.String(ToIdentifier(instance));

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
        public sealed class EvaluationReasonKindConverter : IJsonStreamConverter<EvaluationReasonKind>
        {
            public EvaluationReasonKind ReadJson(ref JReader reader) =>
                FromIdentifier(reader.String());

            public void WriteJson(EvaluationReasonKind instance, IValueWriter writer) =>
                writer.String(ToIdentifier(instance));

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
        public sealed class LdValueConverter : IJsonStreamConverter<LdValue>
        {
            public LdValue ReadJson(ref JReader reader)
            {
                try
                {
                    return ReadJsonInternal(ref reader);
                }
                catch (Exception e)
                {
                    throw reader.TranslateException(e);
                }
            }

            public void WriteJson(LdValue value, IValueWriter writer) =>
                WriteJsonInternal(value, writer);

            internal static void WriteJsonInternal(LdValue value, IValueWriter writer)
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
                            LdJsonConverters.LdValueConverter.WriteJsonInternal(v, arr);
                        }
                        arr.End();
                        break;
                    case LdValueType.Object:
                        var obj = writer.Object();
                        foreach (var kv in value.Dictionary)
                        {
                            LdJsonConverters.LdValueConverter.WriteJsonInternal(kv.Value, obj.Property(kv.Key));
                        }
                        obj.End();
                        break;
                }
            }

            internal static LdValue ReadJsonInternal(ref JReader reader)
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
                            arrayBuilder.Add(ReadJsonInternal(ref reader));
                        }
                        return arrayBuilder.Build();
                    case JsonStream.ValueType.Object:
                        var objBuilder = LdValue.BuildObject();
                        for (var obj = value.ObjectValue; obj.Next(ref reader);)
                        {
                            objBuilder.Add(obj.Name.ToString(), ReadJsonInternal(ref reader));
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
        public sealed class UserConverter : IJsonStreamConverter<User>
        {
            private static readonly string[] _requiredProperties = new string[] { "key" };

            public User ReadJson(ref JReader reader)
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
                                        LdValueConverter.ReadJsonInternal(ref reader));
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

            public void WriteJson(User user, IValueWriter writer)
            {
                var obj = writer.Object();
                obj.Property("key").String(user.Key);
                obj.MaybeProperty("secondary", user.Secondary != null).String(user.Secondary);
                obj.MaybeProperty("ip", user.IPAddress != null).String(user.IPAddress);
                obj.MaybeProperty("country", user.Country != null).String(user.Country);
                obj.MaybeProperty("firstName", user.FirstName != null).String(user.FirstName);
                obj.MaybeProperty("lastName", user.LastName != null).String(user.LastName);
                obj.MaybeProperty("name", user.Name != null).String(user.Name);
                obj.MaybeProperty("avatar", user.Avatar != null).String(user.Avatar);
                obj.MaybeProperty("email", user.Email != null).String(user.Email);
                if (user.AnonymousOptional.HasValue)
                {
                    obj.Property("anonymous").Bool(user.Anonymous);
                }
                if (user.Custom.Count > 0)
                {
                    var customObj = obj.Property("custom").Object();
                    foreach (var kv in user.Custom)
                    {
                        LdValueConverter.WriteJsonInternal(kv.Value, customObj.Property(kv.Key));
                    }
                    customObj.End();
                }
                if (user.PrivateAttributeNames.Count > 0)
                {
                    var attrsArr = obj.Property("privateAttributeNames").Array();
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
