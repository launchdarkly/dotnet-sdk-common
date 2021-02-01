using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace LaunchDarkly.Sdk.Json
{
    public class LdJsonNetTest
    {
        private static readonly User ExpectedUser = User.WithKey("user-key");
        private const string ExpectedUserJson = @"{""key"":""user-key""}";

        [Fact]
        public void SerializeWithExplicitConverter()
        {
            Assert.Equal(ExpectedUserJson, JsonConvert.SerializeObject(ExpectedUser, LdJsonNet.Converter));
        }

        [Fact]
        public void SerializeWithConverterInSettings()
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { LdJsonNet.Converter }
            };
            Assert.Equal(ExpectedUserJson, JsonConvert.SerializeObject(ExpectedUser, settings));
        }

        [Fact]
        public void DeserializeWithExplicitConverter()
        {
            Assert.Equal(ExpectedUser, JsonConvert.DeserializeObject<User>(ExpectedUserJson, LdJsonNet.Converter));
        }

        [Fact]
        public void DeserializeWithConverterInSettings()
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { LdJsonNet.Converter }
            };
            Assert.Equal(ExpectedUser, JsonConvert.DeserializeObject<User>(ExpectedUserJson, settings));
        }
    }
}
