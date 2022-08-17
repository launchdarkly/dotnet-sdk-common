using System;
using System.Text.Json;

namespace LaunchDarkly.Sdk.Json
{
    /// <summary>
    /// Helper methods for JSON serialization of SDK classes.
    /// </summary>
    /// <remarks>
    /// These methods can be used with any SDK type that has the <see cref="IJsonSerializable"/>
    /// marker interface.
    /// </remarks>
    public static class LdJsonSerialization
    {
        /// <summary>
        /// Converts an object to its JSON representation.
        /// </summary>
        /// <typeparam name="T">type of the object being serialized</typeparam>
        /// <param name="instance">the instance to serialize</param>
        /// <returns>the object's JSON encoding as a string</returns>
        public static string SerializeObject<T>(T instance) where T : IJsonSerializable =>
            JsonSerializer.Serialize(instance);

        /// <summary>
        /// Converts an object to its JSON representation as a UTF-8 byte array.
        /// </summary>
        /// <typeparam name="T">type of the object being serialized</typeparam>
        /// <param name="instance">the instance to serialize</param>
        /// <returns>the object's JSON encoding as a byte array</returns>
        public static byte[] SerializeObjectToUtf8Bytes<T>(T instance) where T : IJsonSerializable =>
            JsonSerializer.SerializeToUtf8Bytes(instance);

        /// <summary>
        /// Parses an object from its JSON representation.
        /// </summary>
        /// <typeparam name="T">type of the object being deserialized</typeparam>
        /// <param name="json">the object's JSON encoding as a string</param>
        /// <returns>the deserialized instance</returns>
        /// <exception cref="JsonException">if the JSON encoding was invalid</exception>
        public static T DeserializeObject<T>(string json) where T : IJsonSerializable
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception e)
            {
                throw new JsonException(e);
            }
        }

        internal static void RequireTokenType(ref Utf8JsonReader reader, JsonTokenType expectedType)
        {
            if (reader.TokenType != expectedType)
            {
                throw new JsonException("Expected " + expectedType + ", got " + reader.TokenType, reader.TokenStartIndex);
            }
        }

        internal static JsonException MissingRequiredProperty(ref Utf8JsonReader reader, string name) =>
            new JsonException("Missing required property \"" + name + "\"", reader.TokenStartIndex);

        internal static bool NextProperty(ref Utf8JsonReader reader, out string name)
        {
            if (!reader.Read() || reader.TokenType == JsonTokenType.EndObject)
            {
                name = null;
                return false;
            }
            name = reader.GetString();
            reader.Read();
            return true;
        }
    }
}
