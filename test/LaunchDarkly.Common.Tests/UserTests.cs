using System;
using System.Collections.Generic;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class UserTests
    {
        [Fact]
        public void WhenCreatingAUser_AKeyMustBeProvided()
        {
            var user = User.WithKey("AnyUniqueKey");
            Assert.Equal("AnyUniqueKey", user.Key);
        }

        [Fact]
        public void WhenCreatingAUser_AnOptionalSecondaryKeyCanBeProvided()
        {
            var user = User.WithKey("AnyUniqueKey")
                .AndSecondaryKey("AnySecondaryKey");

            Assert.Equal("AnyUniqueKey", user.Key);
            Assert.Equal("AnySecondaryKey", user.SecondaryKey);
        }

        [Fact]
        public void WhenCreatingAUser_AnOptionalIpAddressCanBeProvided()
        {
            var user = User.WithKey("AnyUniqueKey")
                .AndIpAddress("1.2.3.4");

            Assert.Equal("AnyUniqueKey", user.Key);
            Assert.Equal("1.2.3.4", user.IpAddress);
        }

        [Fact]
        public void WhenCreatingAUser_AnOptionalCountryAddressCanBeProvided()
        {
            var user = User.WithKey("AnyUniqueKey")
                .AndCountry("US");

            Assert.Equal("AnyUniqueKey", user.Key);
            Assert.Equal("US", user.Country);
        }

        [Fact]
        public void IfCountryIsSpecified_ItMustBeA2CharacterCode()
        {
            var user = User.WithKey("AnyUniqueKey");

            Assert.Throws<ArgumentException>(() => user.AndCountry(""));
            Assert.Throws<ArgumentException>(() => user.AndCountry("A"));
            Assert.Throws<ArgumentException>(() => user.AndCountry("ABC"));
        }

        [Fact]
        public void WhenCreatingAUser_AnOptionalCustomAttributeCanBeAdded()
        {
            var user = User.WithKey("AnyUniqueKey")
                .AndCustomAttribute("AnyAttributeName", "AnyValue");

            Assert.Equal("AnyUniqueKey", user.Key);
            Assert.Equal("AnyValue", (string) user.Custom["AnyAttributeName"]);
        }

        [Fact]
        public void WhenCreatingACustomAttribute_AnAttributeNameMustBeProvided()
        {
            var user = User.WithKey("AnyUniqueKey");
            Assert.Throws<ArgumentException>(() => user.AndCustomAttribute("", "AnyValue"));
        }

        [Fact]
        public void WhenCreatingACustomAttribute_AttributeNameMustBeUnique()
        {
            var user = User.WithKey("AnyUniqueKey")
                .AndCustomAttribute("DuplicatedAttributeName", "AnyValue");

            Assert.Throws<ArgumentException>(() => user.AndCustomAttribute("DuplicatedAttributeName", "AnyValue"));
        }

        [Fact]
        public void WhenCreatingAUser_MultipleCustomAttributeCanBeAdded()
        {
            var user = User.WithKey("AnyUniqueKey")
                .AndCustomAttribute("AnyAttributeName", "AnyValue")
                .AndCustomAttribute("AnyOtherAttributeName", "AnyOtherValue");

            Assert.Equal("AnyUniqueKey", user.Key);
            Assert.Equal("AnyValue", (string) user.Custom["AnyAttributeName"]);
            Assert.Equal("AnyOtherValue", (string) user.Custom["AnyOtherAttributeName"]);
        }


        [Fact]
        public void WhenCreatingAUser_AllOptionalPropertiesCanBeSetTogether()
        {
            var user = User.WithKey("AnyUniqueKey")
                .AndIpAddress("1.2.3.4")
                .AndCountry("US")
                .AndCustomAttribute("AnyAttributeName", "AnyValue")
                .AndCustomAttribute("AnyOtherAttributeName", "AnyOtherValue");

            Assert.Equal("AnyUniqueKey", user.Key);
            Assert.Equal("1.2.3.4", user.IpAddress);
            Assert.Equal("US", user.Country);
            Assert.Equal("AnyValue", (string) user.Custom["AnyAttributeName"]);
            Assert.Equal("AnyOtherValue", (string) user.Custom["AnyOtherAttributeName"]);
        }

        [Fact]
        public void SettingCustomAttrToListOfIntsCreatesJsonArray()
        {
            var user = User.WithKey("key")
                .AndCustomAttribute("foo", new List<int>() { 1, 2 });
            var expected = new JArray(new List<JToken>() { new JValue(1), new JValue(2) });
            Assert.Equal(expected, user.Custom["foo"]);
        }

        [Fact]
        public void SettingCustomAttrToListOfStringsCreatesJsonArray()
        {
            var user = User.WithKey("key")
                .AndCustomAttribute("foo", new List<string>() { "a", "b" });
            var expected = new JArray(new List<JToken>() { new JValue("a"), new JValue("b") });
            Assert.Equal(expected, user.Custom["foo"]);
        }

        [Fact]
        public void CanSetCustomAttrToJsonValue()
        {
            var value = new JArray(new List<JToken>() { new JValue(true), new JValue(1.5) });
            var user = User.WithKey("key").AndCustomAttribute("foo", value);
            Assert.Equal(value, user.Custom["foo"]);
        }

        [Fact]
        public void CanSetPrivateCustomAttrToJsonValue()
        {
            var value = new JArray(new List<JToken>() { new JValue(true), new JValue(1.5) });
            var user = User.WithKey("key").AndPrivateCustomAttribute("foo", value);
            Assert.Equal(value, user.Custom["foo"]);
            Assert.True(user.PrivateAttributeNames.Contains("foo"));
        }

        [Fact]
        public void SettingPrivateIpSetsIp()
        {
            var user = User.WithKey("key").AndPrivateIpAddress("x");
            Assert.Equal("x", user.IpAddress);
        }

        [Fact]
        public void SettingPrivateIpMarksIpAsPrivate()
        {
            var user = User.WithKey("key").AndPrivateIpAddress("x");
            Assert.True(user.PrivateAttributeNames.Contains("ip"));
        }

        [Fact]
        public void SettingPrivateEmailSetsEmail()
        {
            var user = User.WithKey("key").AndPrivateEmail("x");
            Assert.Equal("x", user.Email);
        }

        [Fact]
        public void SettingPrivateEmailMarksEmailAsPrivate()
        {
            var user = User.WithKey("key").AndPrivateEmail("x");
            Assert.True(user.PrivateAttributeNames.Contains("email"));
        }

        [Fact]
        public void SettingPrivateAvatarSetsAvatar()
        {
            var user = User.WithKey("key").AndPrivateAvatar("x");
            Assert.Equal("x", user.Avatar);
        }

        [Fact]
        public void SettingPrivateAvatarMarksAvatarAsPrivate()
        {
            var user = User.WithKey("key").AndPrivateAvatar("x");
            Assert.True(user.PrivateAttributeNames.Contains("avatar"));
        }

        [Fact]
        public void SettingPrivateFirstNameSetsFirstName()
        {
            var user = User.WithKey("key").AndPrivateFirstName("x");
            Assert.Equal("x", user.FirstName);
        }

        [Fact]
        public void SettingPrivateFirstNameMarksFirstNameAsPrivate()
        {
            var user = User.WithKey("key").AndPrivateFirstName("x");
            Assert.True(user.PrivateAttributeNames.Contains("firstName"));
        }

        [Fact]
        public void SettingPrivateLastNameSetsLastName()
        {
            var user = User.WithKey("key").AndPrivateLastName("x");
            Assert.Equal("x", user.LastName);
        }

        [Fact]
        public void SettingPrivateLastNameMarksLastNameAsPrivate()
        {
            var user = User.WithKey("key").AndPrivateLastName("x");
            Assert.True(user.PrivateAttributeNames.Contains("lastName"));
        }

        [Fact]
        public void SettingPrivateNameSetsName()
        {
            var user = User.WithKey("key").AndPrivateName("x");
            Assert.Equal("x", user.Name);
        }

        [Fact]
        public void SettingPrivateNameMarksNameAsPrivate()
        {
            var user = User.WithKey("key").AndPrivateName("x");
            Assert.True(user.PrivateAttributeNames.Contains("name"));
        }

        [Fact]
        public void SettingPrivateCountrySetsCountry()
        {
            var user = User.WithKey("key").AndPrivateCountry("us");
            Assert.Equal("us", user.Country);
        }

        [Fact]
        public void SettingPrivateCountryMarksCountryAsPrivate()
        {
            var user = User.WithKey("key").AndPrivateCountry("us");
            Assert.True(user.PrivateAttributeNames.Contains("country"));
        }

        // Note that we never deserialize users from JSON in the client code; however, our integration tests
        // do require that User be deserializable.

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
            var json = "{\"key\":\"user@test.com\", \"custom\": {\"bizzle\":\"cripps\"}}";
            var user = JsonConvert.DeserializeObject<User>(json);
            Assert.Equal("cripps", (string) user.Custom["bizzle"]);
        }

        [Fact]
        public void SerializingAndDeserializingAUserWithCustomAttributesIsIdempotent()
        {
            var user = User.WithKey("foo@bar.com").AndCustomAttribute("bizzle", "cripps");
            var json = JsonConvert.SerializeObject(user);
            var newUser = JsonConvert.DeserializeObject<User>(json);
            Assert.Equal("cripps", (string) user.Custom["bizzle"]);
            Assert.Equal("foo@bar.com", user.Key);
        }

        [Fact]
        public void SerializingAUserWithNoAnonymousSetYieldsNoAnonymous()
        {
            var user = User.WithKey("foo@bar.com");
            var json = JsonConvert.SerializeObject(user);
            Assert.False(json.Contains("anonymous"));
        }

        [Fact]
        public void TestUserEqualityAndCopyConstructor()
        {
            User user = User.WithKey("userkey")
                .AndSecondaryKey("s")
                .AndIpAddress("1")
                .AndCountry("US")
                .AndFirstName("f")
                .AndLastName("l")
                .AndName("n")
                .AndAvatar("a")
                .AndEmail("e")
                .AndCustomAttribute("c1", "v1")
                .AndPrivateCustomAttribute("c2", "v2");
            User copy = new User(user);
            Assert.True(user.Equals(user));
            Assert.True(user.Equals(copy));
            Assert.Equal(user.GetHashCode(), copy.GetHashCode());
            Func<User, User>[] mods = {
                u => u.AndSecondaryKey("x"),
                u => u.AndSecondaryKey(null),
                u => u.AndIpAddress("x"),
                u => u.AndIpAddress(null),
                u => u.AndCountry("FR"),
                u => u.AndCountry(null),
                u => u.AndFirstName("x"),
                u => u.AndFirstName(null),
                u => u.AndLastName("x"),
                u => u.AndLastName(null),
                u => u.AndName("x"),
                u => u.AndName(null),
                u => u.AndAvatar("x"),
                u => u.AndAvatar(null),
                u => u.AndEmail("x"),
                u => u.AndEmail(null),
                u => u.AndAnonymous(true),
                u => u.AndAnonymous(false),
                u => u.AndCustomAttribute("c3", "v3"),
                u => u.AndPrivateName("x")
            };
            foreach (var mod in mods)
            {
                User modUser = mod.Invoke(new User(user));
                Assert.False(user.Equals(modUser));
            }
        }
    }
}