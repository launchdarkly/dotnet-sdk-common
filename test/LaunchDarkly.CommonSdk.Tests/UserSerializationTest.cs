using System.Collections.Generic;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class UserSerializationTests : UserBuilderTestBase
    {
        private const string key = "UserKey";

        // We test user deserialization just because a developer who sees Newtonsoft.Json attributes used in
        // the User class could reasonably expect both serialization and deserialization to work correctly.
        // We never deserialize users in the .NET or Xamarin SDK.

        [Fact]
        public void DeserializeBasicUserAsJson()
        {
            var json = $"{{\"key\":\"{key}\"}}";
            var user = JsonConvert.DeserializeObject<User>(json);
            Assert.Equal(key, user.Key);
            Assert.Equal(0, user.Custom.Count);
            Assert.Equal(0, user.PrivateAttributeNames.Count);
        }

        [Theory]
        [MemberData(nameof(AllStringProperties))]
        public void CanDeserializeStringProperty(StringPropertyDesc p)
        {
            var value = "x";
            var jsonObject = new JObject();
            jsonObject.Add(p.Name, value);
            var user = JsonConvert.DeserializeObject<User>(JsonConvert.SerializeObject(jsonObject));
            Assert.Equal(value, p.Getter(user));
        }

        [Fact]
        public void CanDeserializeCustomAttribute()
        {
            var json = $"{{\"key\":\"{key}\",\"custom\": {{\"a\":\"b\"}}}}";
            var user = JsonConvert.DeserializeObject<User>(json);
            Assert.Equal("b", user.Custom["a"].AsString);
        }
        
        [Fact]
        public void CanDeserializePrivateAttribute()
        {
            var json = $"{{\"key\":\"{key}\",\"name\":\"Lucy\",\"privateAttributeNames\":[\"name\"]}}";
            var user = JsonConvert.DeserializeObject<User>(json);
            Assert.Equal(new List<string> { "name" }, user.PrivateAttributeNames);
        }

        [Fact]
        public void CustomAttributesAndPrivateAttributesOmittedIfEmpty()
        {
            var user = User.WithKey(key);
            var expected = $"{{\"key\":\"{key}\"}}";
            TestUtil.AssertJsonEquals(expected, JsonConvert.SerializeObject(user));
        }

        [Fact]
        public void AnonymousTrueIsSerializedAsTrue()
        {
            var user = User.Builder(key).Anonymous(true).Build();
            var expected = $"{{\"key\":\"{key}\",\"anonymous\":true}}";
            TestUtil.AssertJsonEquals(expected, JsonConvert.SerializeObject(user));
        }

        [Fact]
        public void AnonymousFalseIsSerializedAsFalse()
        {
            var user = User.Builder(key).Anonymous(false).Build();
            var expected = $"{{\"key\":\"{key}\",\"anonymous\":false}}";
            TestUtil.AssertJsonEquals(expected, JsonConvert.SerializeObject(user));
        }

        [Theory]
        [MemberData(nameof(AllStringProperties))]
        public void CanSerializeStringProperty(StringPropertyDesc p)
        {
            var value = "x";
            var user = p.Setter(User.Builder(key))(value).Build();
            var json = JObject.Parse(JsonConvert.SerializeObject(user));
            Assert.Equal(new JValue(value), json[p.Name]);
        }

        [Fact]
        public void CustomAttributesAreSerialized()
        {
            LdValue value1 = LdValue.Of("hi");
            LdValue value2 = LdValue.Of(2);
            var user = User.Builder(key).Custom("name1", value1).Custom("name2", value2).Build();
            var json = JObject.Parse(JsonConvert.SerializeObject(user));
            Assert.Equal(new JValue("hi"), json["custom"]["name1"]);
            Assert.Equal(new JValue(2), json["custom"]["name2"]);
        }

        [Fact]
        public void PrivateAttributeNamesAreSerialized()
        {
            var user = User.Builder(key)
                .Name("user-name").AsPrivateAttribute()
                .Email("test@example.com").AsPrivateAttribute()
                .Build();
            var json = JObject.Parse(JsonConvert.SerializeObject(user));
            var names = new List<string>((json["privateAttributeNames"] as JArray).Values<string>());
            names.Sort();
            Assert.Equal(new List<string> { "email", "name" }, names);
        }
    }
}
