#if !NET452

using System.Text.Json;
using Xunit;

namespace LaunchDarkly.Sdk
{
    public class SystemTextJsonInteroperabilityTest
    {
        // These tests verify that all of our LaunchDarkly.Sdk types that have a custom JSON conversion
        // behave the same with System.Text.Json (on supported platforms, i.e. everything except .NET
        // Framework 4.5.x) as they do with LdJsonSerialization. We get this for free due to how
        // LaunchDarkly.JsonStream's [JsonStreamConverter] annotation works.

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
        public void EvaluationReasonConversion() =>
            VerifySerializationAndDeserialization(ExpectedEvaluationReason, ExpectedEvaluationReasonJson);

        [Fact]
        public void LdValueConversion() =>
            VerifySerializationAndDeserialization(ExpectedValue, ExpectedValueJson);

        [Fact]
        public void UnixMillisecondTimeConversion() =>
            VerifySerializationAndDeserialization(ExpectedUnixTime, ExpectedUnixTimeJson);

        [Fact]
        public void UserConversion() =>
            VerifySerializationAndDeserialization(ExpectedUser, ExpectedUserJson);

        [Fact]
        public void NullableValueTypes()
        {
            // We don't need to check this for User because that's a class, so nullability doesn't affect the type.
            Assert.Equal(@"{""reason"":" + ExpectedEvaluationReasonJson + "}",
                JsonSerializer.Serialize(new ObjectWithNullableReason { reason = ExpectedEvaluationReason }));
            Assert.Equal(@"{""reason"":null}",
                JsonSerializer.Serialize(new ObjectWithNullableReason { reason = null }));
            Assert.Equal(@"{""time"":" + ExpectedUnixTimeJson + "}",
                JsonSerializer.Serialize(new ObjectWithNullableTime { time = ExpectedUnixTime }));
            Assert.Equal(@"{""time"":null}",
                JsonSerializer.Serialize(new ObjectWithNullableTime { time = null }));
            Assert.Equal(@"{""value"":" + ExpectedValueJson + "}",
                JsonSerializer.Serialize(new ObjectWithNullableValue { value = ExpectedValue }));
            Assert.Equal(@"{""value"":null}",
                JsonSerializer.Serialize(new ObjectWithNullableValue { value = null }));
        }

        private void VerifySerializationAndDeserialization<T>(T value, string expectedJson)
        {
            Assert.Equal(expectedJson, JsonSerializer.Serialize(value));
            Assert.Equal(value, JsonSerializer.Deserialize<T>(expectedJson));
        }
    }
}

#endif
