using LaunchDarkly.Client;
using Newtonsoft.Json;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class UserSerializationTests
    {
        // Note that users are never deserialized from JSON in the .NET SDK, but they are in the Xamarin SDK.

        [Fact]
        public void DeserializeBasicUserAsJson()
        {
            var json = "{\"key\":\"user@test.com\"}";
            var user = JsonConvert.DeserializeObject<User>(json);
            Assert.Equal("user@test.com", user.Key);
        }

        [Fact]
        public void DeserializeUserWithCustomAsJson()
        {
            var json = "{\"key\":\"user@test.com\", \"custom\": {\"a\":\"b\"}}";
            var user = JsonConvert.DeserializeObject<User>(json);
            Assert.Equal("b", (string)user.Custom["a"]);
        }

        [Fact]
        public void SerializingAndDeserializingAUserWithCustomAttributesIsIdempotent()
        {
            var user = User.Builder("foo@bar.com").Custom("a", "b").Build();
            var json = JsonConvert.SerializeObject(user);
            var newUser = JsonConvert.DeserializeObject<User>(json);
            Assert.Equal("b", (string)user.Custom["a"]);
            Assert.Equal("foo@bar.com", user.Key);
        }

        [Fact]
        public void SerializingAUserWithNoAnonymousSetYieldsNoAnonymous()
        {
            var user = User.WithKey("foo@bar.com");
            var json = JsonConvert.SerializeObject(user);
            Assert.DoesNotContain("anonymous", json);
        }

        [Fact]
        public void SerializingAUserWithAnonymousSetYieldsAnonymousTrue()
        {
            var user = User.Builder("foo@bar.com").Anonymous(true).Build();
            var json = JsonConvert.SerializeObject(user);
            Assert.Contains("\"anonymous\":true", json);
        }
    }
}
