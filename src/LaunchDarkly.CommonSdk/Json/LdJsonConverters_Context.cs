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
                if (objValue.Dictionary.TryGetValue("kind", out var kindValue))
                {
                    if (!kindValue.IsString)
                    {
                        throw WrongType(kindValue, "kind");
                    }
                    if (kindValue.AsString == "multi")
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
                    obj.Name("kind").String("multi");
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
                    if (kv.Key == "_meta")
                    {
                        if (kv.Value.Type != LdValueType.Object && !kv.Value.IsNull)
                        {
                            throw WrongType(kv.Value, "_meta");
                        }
                        var meta = kv.Value.Dictionary;
                        if (meta.TryGetValue("secondary", out var secondary))
                        {
                            if (!secondary.IsString && !secondary.IsNull)
                            {
                                throw WrongType(secondary, "_meta.secondary");
                            }
                            builder.Secondary(secondary.AsString);
                        }
                        if (meta.TryGetValue("privateAttributes", out var privateAttrs))
                        {
                            if (privateAttrs.Type != LdValueType.Array && !privateAttrs.IsNull)
                            {
                                throw WrongType(privateAttrs, "_meta.privateAttributes");
                            }
                            for (int i = 0; i < privateAttrs.List.Count; i++)
                            {
                                var value = privateAttrs.List[i];
                                if (!value.IsString)
                                {
                                    throw WrongType(value, string.Format("_meta.privateAttributes[{0}]", i));
                                }
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
                        if (kv.Key == "kind")
                        {
                            kind = kv.Value.AsString;
                        }
                    }
                }
                if (kind is null)
                {
                    throw new JsonException(Errors.JsonContextMissingProperty("kind"));
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
                        case "anonymous":
                            if (kv.Value.Type != LdValueType.Bool && !kv.Value.IsNull)
                            {
                                throw WrongType(kv.Value, "anonymous");
                            }
                            builder.Anonymous(kv.Value.AsBool);
                            break;

                        case "custom":
                            if (kv.Value.Type != LdValueType.Object && !kv.Value.IsNull)
                            {
                                throw WrongType(kv.Value, "custom");
                            }
                            foreach (var kv1 in kv.Value.Dictionary)
                            {
                                switch (kv1.Key)
                                {
                                    // can't allow an old-style custom attribute to overwrite a top-level one with the same name
                                    case "kind":
                                    case "key":
                                    case "name":
                                    case "anonymous":
                                    case "_meta":
                                        break;
                                    default:
                                        builder.Set(kv1.Key, kv1.Value);
                                        break;
                                }
                            }
                            break;

                        case "privateAttributeNames":
                            if (kv.Value.Type != LdValueType.Array && !kv.Value.IsNull)
                            {
                                throw WrongType(kv.Value, "privateAttributeNames");
                            }
                            for (int i = 0; i < kv.Value.List.Count; i++)
                            {
                                var value = kv.Value.List[i];
                                if (!value.IsString)
                                {
                                    throw WrongType(value, string.Format("privateAttributeNames[{0}]", i));
                                }
                                builder.Private(AttributeRef.FromLiteral(value.AsString));
                            }
                            break;

                        case "secondary":
                            if (!kv.Value.IsString && !kv.Value.IsNull)
                            {
                                throw WrongType(kv.Value, "secondary");
                            }
                            builder.Secondary(kv.Value.AsString);
                            break;

                        case "name":
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
                            if (kv.Key == "key")
                            {
                                hasKey = true;
                            }
                            break;
                    }
                }

                if (!hasKey)
                {
                    throw new JsonException(Errors.JsonContextMissingProperty("key"));
                }
                return Validate(builder.Build());
            }

            private static JsonException WrongType(LdValue value, string name) =>
                new JsonException(Errors.JsonContextWrongType(name, value.Type));

            private void WriteJsonSingle(in Context c, IValueWriter writer, bool includeKind)
            {
                var obj = writer.Object();
                obj.MaybeName("kind", includeKind).String(c.Kind.Value);
                obj.Name("key").String(c.Key);
                obj.MaybeName("name", c.Name != null).String(c.Name);
                obj.MaybeName("anonymous", c.Anonymous).Bool(c.Anonymous);
                if (!(c._attributes is null))
                {
                    foreach (var kv in c._attributes)
                    {
                        LdValueConverter.WriteJsonValue(kv.Value, obj.Name(kv.Key));
                    }
                }
                if (!(c.Secondary is null) || !(c._privateAttributes is null))
                {
                    var meta = obj.Name("_meta").Object();
                    meta.MaybeName("secondary", c.Secondary != null).String(c.Secondary);
                    if (!(c._privateAttributes is null))
                    {
                        var privateArr = meta.Name("privateAttributes").Array();
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
