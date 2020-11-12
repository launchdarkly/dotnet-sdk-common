using System;
using Newtonsoft.Json;

namespace LaunchDarkly.Sdk.Internal.Helpers
{
    /// <summary>
    /// A non-reflection-based implementation of JSON serialization for the <see cref="User"/> class.
    /// </summary>
    /// <remarks>
    /// We don't expose this publicly, but SDKs should use it internally for efficiency. It avoids the
    /// overhead of reflection, and also allows for optimizations that aren't possible with the default
    /// converter, e.g. omitting <c>custom</c> if it is an empty set. It doesn't support
    /// deserialization because the SDK never unmarshals a user from JSON.
    /// </remarks>
    internal class UserJsonSerializer : JsonConverter
    {
        public override bool CanRead => false;
        public override bool CanWrite => true;
        
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(User);
        }

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is User u))
            {
                throw new ArgumentException();
            }
            writer.WriteStartObject();
            MaybeWriteString(writer, "key", u.Key);
            MaybeWriteString(writer, "secondary", u.Secondary);
            MaybeWriteString(writer, "ip", u.IPAddress);
            MaybeWriteString(writer, "country", u.Country);
            MaybeWriteString(writer, "firstName", u.FirstName);
            MaybeWriteString(writer, "lastName", u.LastName);
            MaybeWriteString(writer, "name", u.Name);
            MaybeWriteString(writer, "avatar", u.Avatar);
            MaybeWriteString(writer, "email", u.Email);
            if (u.AnonymousOptional.HasValue)
            {
                writer.WritePropertyName("anonymous");
                writer.WriteValue(u.AnonymousOptional.Value);
            }
            if (u.Custom.Count > 0)
            {
                writer.WritePropertyName("custom");
                writer.WriteStartObject();
                foreach (var kv in u.Custom)
                {
                    writer.WritePropertyName(kv.Key);
                    LdValue.JsonConverter.WriteJson(writer, kv.Value, serializer);
                }
                writer.WriteEndObject();
            }
            if (u.PrivateAttributeNames.Count > 0)
            {
                writer.WritePropertyName("privateAttributeNames");
                writer.WriteStartArray();
                foreach (var n in u.PrivateAttributeNames)
                {
                    writer.WriteValue(n);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }

        private static void MaybeWriteString(JsonWriter writer, string propertyName, string value)
        {
            if (value != null)
            {
                writer.WritePropertyName(propertyName);
                writer.WriteValue(value);
            }
        }
    }
}
