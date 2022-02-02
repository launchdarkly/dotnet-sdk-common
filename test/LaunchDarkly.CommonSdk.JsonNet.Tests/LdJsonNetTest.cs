using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace LaunchDarkly.Sdk.Json
{
    public class LdJsonNetTest
    {
        private static readonly User ExpectedUser = User.WithKey("user-key");
        private const string ExpectedUserJson = @"{""key"":""user-key""}";
        private static readonly EvaluationReason ExpectedEvaluationReason = EvaluationReason.OffReason;
        private const string ExpectedEvaluationReasonJson = @"{""kind"":""OFF""}";

        private sealed class ObjectWithNullableReason
        {
            public EvaluationReason? reason { get; set; }
        }

        [Fact]
        public void SerializeWithExplicitConverter()
        {
            Assert.Equal(ExpectedUserJson, JsonConvert.SerializeObject(ExpectedUser, LdJsonNet.Converter));
            Assert.Equal(ExpectedEvaluationReasonJson, JsonConvert.SerializeObject(ExpectedEvaluationReason, LdJsonNet.Converter));
        }

        [Fact]
        public void SerializeWithConverterInSettings()
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { LdJsonNet.Converter }
            };
            Assert.Equal(ExpectedUserJson, JsonConvert.SerializeObject(ExpectedUser, settings));
            Assert.Equal(ExpectedEvaluationReasonJson, JsonConvert.SerializeObject(ExpectedEvaluationReason, settings));
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

        [Fact]
        public void NullableValueTypeIsSerializedCorrectly()
        {

            Assert.Equal(@"{""reason"":" + ExpectedEvaluationReasonJson + "}",
                JsonConvert.SerializeObject(new ObjectWithNullableReason { reason = ExpectedEvaluationReason }, LdJsonNet.Converter));
            Assert.Equal(@"{""reason"":null}",
                JsonConvert.SerializeObject(new ObjectWithNullableReason { reason = null }, LdJsonNet.Converter));
        }

        [Fact]
        public void NullableValueTypeIsDeserializedCorrectly()
        {

            Assert.Equal(ExpectedEvaluationReason,
                JsonConvert.DeserializeObject<ObjectWithNullableReason>(@"{""reason"":" + ExpectedEvaluationReasonJson + "}").reason);
            Assert.Null(JsonConvert.DeserializeObject<ObjectWithNullableReason>(@"{""reason"":null}").reason);
        }
    }
}
