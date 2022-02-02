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
        private static readonly UnixMillisecondTime ExpectedUnixTime = UnixMillisecondTime.OfMillis(123456789);
        private const string ExpectedUnixTimeJson = "123456789";
        private static readonly LdValue ExpectedValue = LdValue.Of(true);
        private const string ExpectedValueJson = "true";

        private sealed class ObjectWithNullableReason
        {
            // The reason we use an enclosing class here to test the serialization of a nullable EvaluationReason?
            // is that if we pass an "EvaluationReason?" value to SerializeObject, the type of the parameter in
            // that method is actually "object" and so what is really passed is either an EvaluationReason or a
            // plain old null-- it doesn't really see an "EvaluationReason?". But if it's in a property like this,
            // it really will detect the type.
            public EvaluationReason? reason { get; set; }
        }

        private sealed class ObjectWithNullableTime
        {
            public UnixMillisecondTime? time { get; set; } // see above
        }

        private sealed class ObjectWithNullableValue
        {
            public LdValue? value { get; set; } // see above
            // "LdValue?" is a bit pointless, since an LdValue can already encode null, but it is a struct so this should work
        }

        [Fact]
        public void SerializeWithExplicitConverter()
        {
            Assert.Equal(ExpectedUserJson, JsonConvert.SerializeObject(ExpectedUser, LdJsonNet.Converter));
            Assert.Equal(ExpectedEvaluationReasonJson, JsonConvert.SerializeObject(ExpectedEvaluationReason, LdJsonNet.Converter));
            Assert.Equal(ExpectedUnixTimeJson, JsonConvert.SerializeObject(ExpectedUnixTime, LdJsonNet.Converter));
            Assert.Equal(ExpectedValueJson, JsonConvert.SerializeObject(ExpectedValue, LdJsonNet.Converter));
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
            Assert.Equal(ExpectedUnixTimeJson, JsonConvert.SerializeObject(ExpectedUnixTime, settings));
            Assert.Equal(ExpectedValueJson, JsonConvert.SerializeObject(ExpectedValue, settings));
        }

        [Fact]
        public void DeserializeWithExplicitConverter()
        {
            Assert.Equal(ExpectedUser, JsonConvert.DeserializeObject<User>(ExpectedUserJson, LdJsonNet.Converter));
            Assert.Equal(ExpectedEvaluationReason,
                JsonConvert.DeserializeObject<EvaluationReason>(ExpectedEvaluationReasonJson, LdJsonNet.Converter));
            Assert.Equal(ExpectedUnixTime, JsonConvert.DeserializeObject<UnixMillisecondTime>(ExpectedUnixTimeJson, LdJsonNet.Converter));
            Assert.Equal(ExpectedValue, JsonConvert.DeserializeObject<LdValue>(ExpectedValueJson, LdJsonNet.Converter));
        }

        [Fact]
        public void DeserializeWithConverterInSettings()
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { LdJsonNet.Converter }
            };
            Assert.Equal(ExpectedUser, JsonConvert.DeserializeObject<User>(ExpectedUserJson, settings));
            Assert.Equal(ExpectedEvaluationReason,
                JsonConvert.DeserializeObject<EvaluationReason>(ExpectedEvaluationReasonJson, settings));
            Assert.Equal(ExpectedUnixTime, JsonConvert.DeserializeObject<UnixMillisecondTime>(ExpectedUnixTimeJson, settings));
            Assert.Equal(ExpectedValue, JsonConvert.DeserializeObject<LdValue>(ExpectedValueJson, settings));
        }

        [Fact]
        public void NullableValueTypeIsSerializedCorrectly()
        {
            // We don't need to check this for User because that's a class, so nullability doesn't affect the type.
            Assert.Equal(@"{""reason"":" + ExpectedEvaluationReasonJson + "}",
                JsonConvert.SerializeObject(new ObjectWithNullableReason { reason = ExpectedEvaluationReason }, LdJsonNet.Converter));
            Assert.Equal(@"{""reason"":null}",
                JsonConvert.SerializeObject(new ObjectWithNullableReason { reason = null }, LdJsonNet.Converter));
            Assert.Equal(@"{""time"":" + ExpectedUnixTimeJson + "}",
                JsonConvert.SerializeObject(new ObjectWithNullableTime { time = ExpectedUnixTime }, LdJsonNet.Converter));
            Assert.Equal(@"{""time"":null}",
                JsonConvert.SerializeObject(new ObjectWithNullableTime { time = null }, LdJsonNet.Converter));
            Assert.Equal(@"{""value"":" + ExpectedValueJson + "}",
                JsonConvert.SerializeObject(new ObjectWithNullableValue { value = ExpectedValue }, LdJsonNet.Converter));
            Assert.Equal(@"{""value"":null}",
                JsonConvert.SerializeObject(new ObjectWithNullableValue { value = null }, LdJsonNet.Converter));
        }

        [Fact]
        public void NullableValueTypeIsDeserializedCorrectly()
        {
            Assert.Equal(ExpectedEvaluationReason,
                JsonConvert.DeserializeObject<ObjectWithNullableReason>(
                    @"{""reason"":" + ExpectedEvaluationReasonJson + "}",
                    LdJsonNet.Converter).reason);
            Assert.Null(JsonConvert.DeserializeObject<ObjectWithNullableReason>(
                @"{""reason"":null}",
                LdJsonNet.Converter).reason);
            Assert.Equal(ExpectedUnixTime,
                JsonConvert.DeserializeObject<ObjectWithNullableTime>(
                    @"{""time"":" + ExpectedUnixTimeJson + "}",
                    LdJsonNet.Converter).time);
            Assert.Null(JsonConvert.DeserializeObject<ObjectWithNullableTime>(
                @"{""time"":null}",
                LdJsonNet.Converter).time);
        }
    }
}
