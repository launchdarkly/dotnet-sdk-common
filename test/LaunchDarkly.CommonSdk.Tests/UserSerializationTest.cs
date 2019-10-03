using LaunchDarkly.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class UserSerializationTests : UserBuilderTestBase
    {
        private const string key = "UserKey";

        // Note that users are never deserialized from JSON in the .NET SDK, but they are in the Xamarin SDK.

        [Fact]
        public void DeserializeBasicUserAsJson()
        {
            var json = $"{{\"key\":\"{key}\"}}";
            var user = JsonConvert.DeserializeObject<User>(json);
            Assert.Equal(key, user.Key);
        }

        [Fact]
        public void DeserializeUserWithCustomAsJson()
        {
            var json = $"{{\"key\":\"{key}\", \"custom\": {{\"a\":\"b\"}}}}";
            var user = JsonConvert.DeserializeObject<User>(json);
            Assert.Equal("b", user.Custom["a"].AsString);
        }

        [Fact]
        public void SerializingAndDeserializingAUserWithCustomAttributesIsIdempotent()
        {
            var user = User.Builder(key).Custom("a", "b").Build();
            var json = JsonConvert.SerializeObject(user);
            var newUser = JsonConvert.DeserializeObject<User>(json);
            Assert.Equal("b", user.Custom["a"].AsString);
            Assert.Equal(key, user.Key);
        }

        [Fact]
        public void SerializingAUserWithNoAnonymousSetYieldsAnonymousNull()
        {
            var user = User.WithKey(key);
            var json = JObject.FromObject(user);
            Assert.Null(json["anonymous"]);
        }

        [Fact]
        public void SerializingAUserWithAnonymousSetYieldsAnonymousTrue()
        {
            var user = User.Builder(key).Anonymous(true).Build();
            var json = JObject.FromObject(user);
            Assert.Equal(new JValue(true), json["anonymous"]);
        }

        [Fact]
        public void SerializingAUserWithAnonymousSetToFalseYieldsAnonymousFalse()
        {
            var user = User.Builder(key).Anonymous(false).Build();
            var json = JObject.FromObject(user);
            Assert.Equal(new JValue(false), json["anonymous"]);
        }

        [Theory]
        [MemberData(nameof(AllStringProperties))]
        public void CanSerializeStringProperty(StringPropertyDesc p)
        {
            var value = "x";
            var user = p.Setter(User.Builder(key))(value).Build();
            var json = JObject.FromObject(user);
            Assert.Equal(new JValue(value), json[p.Name]);
        }
    }
}
