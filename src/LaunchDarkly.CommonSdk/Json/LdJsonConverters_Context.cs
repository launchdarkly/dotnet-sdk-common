using LaunchDarkly.JsonStream;

namespace LaunchDarkly.Sdk.Json
{
    public static partial class LdJsonConverters
    {
#pragma warning disable CS1591 // don't bother with XML comments for these low-level helpers
        /// <summary>
        /// The JSON converter for <see cref="Context"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Applications should not need to use this class directly. It is used automatically in
        /// <c>System.Text.Json</c> conversion, or if you call <see cref="LdJsonSerialization.SerializeObject{T}(T)"/>
        /// or <see cref="LdJsonSerialization.DeserializeObject{T}(string)"/>
        /// </para>
        /// <para>
        /// LaunchDarkly's JSON schema for contexts is standardized across SDKs. There are two serialization
        /// formats, depending on whether it is a single-kind context or a multi-kind context. There is also
        /// a third format corresponding to how users were represented in JSON in older LaunchDarkly SDKs;
        /// this format is recognized automatically and supported for deserialization, but is not supported
        /// for serialization.
        /// </para>
        /// </remarks>
        public sealed class ContextConverter : IJsonStreamConverter
        {
            private const string AttrKind = "kind";
            private const string AttrKey = "key";
            private const string AttrName = "name";
            private const string AttrAnonymous = "anonymous";
            private const string JsonPropMeta = "_meta";
            private const string JsonPropSecondary = "secondary";
            private const string JsonPropPrivateAttributes = "privateAttributes";
            private const string OldJsonPropCustom = "custom";
            private const string OldJsonPropPrivateAttributeNames = "privateAttributeNames";

            public object ReadJson(ref JReader reader)
            {
                // The implementation here of unmarshaling a context/user is that we first unmarshal the
                // whole JSON object into an LdValue (as a convenient way to represent arbitrary JSON data
                // structures), and then decide how to translate that object into a Context. This is
                // somewhat inefficient because we're producing dictionary-like structures that we don't
                // intend to keep-- but a straight-ahead approach, where we would parse JSON tokens as a
                // stream, is not really possible due to the details of the context JSON schema (we can't
                // know what schema we're using until we see the "kind" property). And, unmarshaling context
                // data from JSON is not a task applications are likely to be doing frequently enough for
                // it to be performance-critical.
                var objValue = LdValueConverter.ReadJsonValue(ref reader);
                if (objValue.Dictionary.TryGetValue(AttrKind, out var kindValue))
                {
                    if (!kindValue.IsString)
                    {
                        throw WrongType(kindValue, AttrKind);
                    }
                    if (kindValue.AsString == ContextKind.Multi.Value)
                    {
                        return ReadJsonMulti(objValue);
                    }
                    return ReadJsonSingle(objValue, null);
                }
                return ReadJsonOldUser(objValue);
            }

            public void WriteJson(object value, IValueWriter writer)
            {
                var c = (Context)value;

                if (!(c.Error is null))
                {
                    throw new JsonException(Errors.JsonSerializeInvalidContext(c.Error));
                }
                if (c.Multiple)
                {
                    var obj = writer.Object();
                    obj.Name(AttrKind).String(ContextKind.Multi.Value);
                    foreach (var mc in c._multiContexts)
                    {
                        WriteJsonSingle(mc, obj.Name(mc.Kind.Value), false);
                    }
                    obj.End();
                }
                else
                {
                    WriteJsonSingle(c, writer, true);
                }
            }

            private Context ReadJsonSingle(LdValue objValue, string knownKind)
            {
                var builder = Context.Builder("").Kind(knownKind);
                var kind = knownKind;
                foreach (var kv in objValue.Dictionary)
                {
                    if (kv.Key == JsonPropMeta)
                    {
                        RequireType(kv.Value, LdValueType.Object, true, JsonPropMeta);
                        var meta = kv.Value.Dictionary;
                        if (meta.TryGetValue(JsonPropSecondary, out var secondary))
                        {
                            RequireType(secondary, LdValueType.String, true, "{0}.{1}", JsonPropMeta, JsonPropSecondary);
                            builder.Secondary(secondary.AsString);
                        }
                        if (meta.TryGetValue(JsonPropPrivateAttributes, out var privateAttrs))
                        {
                            RequireType(privateAttrs, LdValueType.Array, true, "{0}.{1}", JsonPropMeta, JsonPropPrivateAttributes);
                            for (int i = 0; i < privateAttrs.List.Count; i++)
                            {
                                var value = privateAttrs.List[i];
                                RequireType(value, LdValueType.String, false, "{0}.{1}[{2}]", JsonPropMeta, JsonPropPrivateAttributes, i);
                                builder.Private(value.AsString);
                            }
                        }
                    }
                    else
                    {
                        if (!builder.TrySet(kv.Key, kv.Value))
                        {
                            throw WrongType(kv.Value, kv.Key);
                        }
                        if (kv.Key == AttrKind)
                        {
                            kind = kv.Value.AsString;
                        }
                    }
                }
                if (kind is null)
                {
                    throw new JsonException(Errors.JsonContextMissingProperty(AttrKind));
                }
                if (kind == "")
                {
                    throw new JsonException(Errors.JsonContextEmptyKind);
                }
                return Validate(builder.Build());
            }

            private Context Validate(Context c) =>
                c.Error is null ? c : throw new JsonException(c.Error);

            private Context ReadJsonMulti(LdValue objValue)
            {
                var builder = Context.MultiBuilder();
                foreach (var kv in objValue.Dictionary)
                {
                    if (kv.Key != "kind")
                    {
                        builder.Add(ReadJsonSingle(kv.Value, kv.Key));
                    }
                }
                return Validate(builder.Build());
            }

            private Context ReadJsonOldUser(LdValue objValue)
            {
                var builder = Context.Builder("");
                builder.AllowEmptyKey(true);
                var hasKey = false;

                foreach (var kv in objValue.Dictionary)
                {
                    switch (kv.Key)
                    {
                        case AttrAnonymous:
                            RequireType(kv.Value, LdValueType.Bool, true, AttrAnonymous);
                            builder.Anonymous(kv.Value.AsBool);
                            break;

                        case OldJsonPropCustom:
                            RequireType(kv.Value, LdValueType.Object, true, OldJsonPropCustom);
                            foreach (var kv1 in kv.Value.Dictionary)
                            {
                                switch (kv1.Key)
                                {
                                    // can't allow an old-style custom attribute to overwrite a top-level one with the same name
                                    case AttrKind:
                                    case AttrKey:
                                    case AttrName:
                                    case AttrAnonymous:
                                    case JsonPropMeta:
                                        break;
                                    default:
                                        builder.Set(kv1.Key, kv1.Value);
                                        break;
                                }
                            }
                            break;

                        case OldJsonPropPrivateAttributeNames:
                            RequireType(kv.Value, LdValueType.Array, true, OldJsonPropPrivateAttributeNames);
                            for (int i = 0; i < kv.Value.List.Count; i++)
                            {
                                var value = kv.Value.List[i];
                                RequireType(value, LdValueType.String, false, "{0}[{1}]", OldJsonPropPrivateAttributeNames, i);
                                builder.Private(AttributeRef.FromLiteral(value.AsString));
                            }
                            break;

                        case JsonPropSecondary:
                            RequireType(kv.Value, LdValueType.String, true, JsonPropSecondary);
                            builder.Secondary(kv.Value.AsString);
                            break;

                        case AttrName:
                        case "firstName":
                        case "lastName":
                        case "email":
                        case "country":
                        case "ip":
                        case "avatar":
                            if (!kv.Value.IsString && !kv.Value.IsNull)
                            {
                                throw WrongType(kv.Value, kv.Key);
                            }
                            builder.Set(kv.Key, kv.Value);
                            break;

                        default:
                            if (!builder.TrySet(kv.Key, kv.Value))
                            {
                                throw WrongType(kv.Value, kv.Key);
                            }
                            if (kv.Key == AttrKey)
                            {
                                hasKey = true;
                            }
                            break;
                    }
                }

                if (!hasKey)
                {
                    throw new JsonException(Errors.JsonContextMissingProperty(AttrKey));
                }
                return Validate(builder.Build());
            }

            private static void RequireType(LdValue value, LdValueType type, bool nullable,
                string propNameFormat, params object[] propNameArgs)
            {
                if (value.Type != type && !(value.IsNull && nullable))
                {
                    throw WrongType(value, string.Format(propNameFormat, propNameArgs));
                }
            }

            private static JsonException WrongType(LdValue value, string name) =>
                new JsonException(Errors.JsonContextWrongType(name, value.Type));

            private void WriteJsonSingle(in Context c, IValueWriter writer, bool includeKind)
            {
                var obj = writer.Object();
                obj.MaybeName(AttrKind, includeKind).String(c.Kind.Value);
                obj.Name(AttrKey).String(c.Key);
                obj.MaybeName(AttrName, c.Name != null).String(c.Name);
                obj.MaybeName(AttrAnonymous, c.Anonymous).Bool(c.Anonymous);
                if (!(c._attributes is null))
                {
                    foreach (var kv in c._attributes)
                    {
                        LdValueConverter.WriteJsonValue(kv.Value, obj.Name(kv.Key));
                    }
                }
                if (!(c.Secondary is null) || !(c._privateAttributes is null))
                {
                    var meta = obj.Name(JsonPropMeta).Object();
                    meta.MaybeName(JsonPropSecondary, c.Secondary != null).String(c.Secondary);
                    if (!(c._privateAttributes is null))
                    {
                        var privateArr = meta.Name(JsonPropPrivateAttributes).Array();
                        foreach (var pa in c._privateAttributes)
                        {
                            privateArr.String(pa.ToString());
                        }
                        privateArr.End();
                    }
                    meta.End();
                }
                obj.End();
            }
        }
    }
#pragma warning restore CS1591
}
