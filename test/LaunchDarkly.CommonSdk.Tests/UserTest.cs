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
        private const string key = "UserKey";

        [Fact]
        public void UserWithKeySetsKey()
        {
            var user = User.WithKey(key);
            Assert.Equal(key, user.Key);
        }

        [Fact]
        public void BuilderCanSetKey()
        {
            var user = User.Builder(key).Build();
            Assert.Equal(key, user.Key);
        }

        struct StringTest
        {
            public string Name;
            public Func<UserBuilder, Func<string, UserBuilder>> Setter;
            public Func<User, string> Getter;

            public StringTest(string name, Func<UserBuilder, Func<string, UserBuilder>> setter, Func<User, string> getter)
            {
                Name = name;
                Setter = setter;
                Getter = getter;
            }
        }

        [Fact]
        public void TestStringProperties()
        {
            StringTest[] stringTests =
            {
                new StringTest("secondary", b => b.SecondaryKey, u => u.SecondaryKey),
                new StringTest("ip", b => b.IPAddress, u => u.IpAddress),
                new StringTest("country", b => b.Country, u => u.Country),
                new StringTest("firstName", b => b.FirstName, u => u.FirstName),
                new StringTest("lastName", b => b.LastName, u => u.LastName),
                new StringTest("name", b => b.Name, u => u.Name),
                new StringTest("avatar", b => b.Avatar, u => u.Avatar),
                new StringTest("email", b => b.Email, u => u.Email)
            };
            foreach (var t in stringTests)
            {
                var user = User.Builder(key).Build();
                Assert.True(t.Getter(user) == null, t.Name + " should default to null");
            }
            foreach (var t in stringTests)
            {
                var expectedValue = t.Name + " value";
                var user = t.Setter(User.Builder(key))(expectedValue).Build();
                Assert.Equal(expectedValue, t.Getter(user));
                Assert.Null(user.PrivateAttributeNames);
            }
        }

        [Fact]
        public void TestPrivateStringProperties()
        {
            StringTest[] stringTests =
            {
                new StringTest("ip", b => b.PrivateIPAddress, u => u.IpAddress),
                new StringTest("country", b => b.PrivateCountry, u => u.Country),
                new StringTest("firstName", b => b.PrivateFirstName, u => u.FirstName),
                new StringTest("lastName", b => b.PrivateLastName, u => u.LastName),
                new StringTest("name", b => b.PrivateName, u => u.Name),
                new StringTest("avatar", b => b.PrivateAvatar, u => u.Avatar),
                new StringTest("email", b => b.PrivateEmail, u => u.Email)
            };
            foreach (var t in stringTests)
            {
                var expectedValue = t.Name + " value";
                var user = t.Setter(User.Builder(key))(expectedValue).Build();
                Assert.Equal(expectedValue, t.Getter(user));
                Assert.Equal(new HashSet<string> { t.Name }, user.PrivateAttributeNames);
            }
        }

        [Fact]
        public void AnonymousDefaultsToNull()
        {
            var user = User.Builder(key).Build();
            Assert.Null(user.Anonymous);
        }

        [Fact]
        public void BuilderCanSetAnonymousTrue()
        {
            var user = User.Builder(key).Anonymous(true).Build();
            Assert.True(user.Anonymous);
        }

        [Fact]
        public void BuilderCanSetAnonymousFalse()
        {
            var user = User.Builder(key).Anonymous(false).Build();
            Assert.Null(user.Anonymous); // it's null rather than false so the JSON property won't appear
        }

        [Fact]
        public void CustomDefaultsToNull()
        {
            var user = User.Builder(key).Build();
            Assert.Null(user.Custom);
        }

        private void TestCustomAttribute<T>(T value,
            Func<UserBuilder, string, T, UserBuilder> setter,
            Func<UserBuilder, string, T, UserBuilder> privateSetter)
        {
            var user0 = setter(User.Builder(key), "foo", value).Build();
            Assert.Equal<object>(value, user0.Custom["foo"].Value<T>());
            Assert.Null(user0.PrivateAttributeNames);

            var user1 = privateSetter(User.Builder(key), "bar", value).Build();
            Assert.Equal<object>(value, user1.Custom["bar"].Value<T>());
            Assert.Equal(new HashSet<string> { "bar" }, user1.PrivateAttributeNames);
        }

        [Fact]
        public void BuilderCanSetJsonCustomAttribute()
        {
            var value = new JArray(new List<JToken>() { new JValue(true), new JValue(1.5) });
            TestCustomAttribute<JToken>(value, (b, n, v) => b.Custom(n, v), (b, n, v) => b.PrivateCustom(n, v));
        }
        
        [Fact]
        public void BuilderCanSetBoolCustomAttribute()
        {
            TestCustomAttribute<bool>(true, (b, n, v) => b.Custom(n, v), (b, n, v) => b.PrivateCustom(n, v));
        }
        
        [Fact]
        public void BuilderCanSetStringCustomAttribute()
        {
            TestCustomAttribute<string>("x", (b, n, v) => b.Custom(n, v), (b, n, v) => b.PrivateCustom(n, v));
        }

        [Fact]
        public void BuilderCanSetIntCustomAttribute()
        {
            TestCustomAttribute<int>(3, (b, n, v) => b.Custom(n, v), (b, n, v) => b.PrivateCustom(n, v));
        }

        [Fact]
        public void BuilderCanSetFloatCustomAttribute()
        {
            TestCustomAttribute<float>(1.5f, (b, n, v) => b.Custom(n, v), (b, n, v) => b.PrivateCustom(n, v));
        }

        private static readonly User userToCopy = User.Builder("userkey")
                .SecondaryKey("s")
                .IPAddress("1")
                .Country("US")
                .FirstName("f")
                .LastName("l")
                .Name("n")
                .Avatar("a")
                .Email("e")
                .Custom("c1", "v1")
                .PrivateCustom("c2", "v2")
                .Build();

        [Fact]
        public void TestUserSelfEquality()
        {
            Assert.True(userToCopy.Equals(userToCopy));
        }

        [Fact]
        public void TestUserEqualityWithCopyConstructor()
        {
            User copy = new User(userToCopy);
            Assert.NotSame(userToCopy, copy);
            Assert.True(copy.Equals(userToCopy));
            Assert.True(userToCopy.Equals(copy));
            Assert.Equal(userToCopy.GetHashCode(), copy.GetHashCode());
        }

        [Fact]
        public void TestUserEqualityWithBuilderFromUser()
        {
            User copy = User.Builder(userToCopy).Build();
            Assert.NotSame(userToCopy, copy);
            Assert.True(copy.Equals(userToCopy));
            Assert.True(userToCopy.Equals(copy));
            Assert.Equal(userToCopy.GetHashCode(), copy.GetHashCode());
        }

        [Fact]
        public void TestUserInequalityWithModifiedBuilder()
        {
            Func<UserBuilder, UserBuilder>[] mods = {
                b => b.SecondaryKey("x"),
                b => b.SecondaryKey(null),
                b => b.IPAddress("x"),
                b => b.IPAddress(null),
                b => b.Country("FR"),
                b => b.Country(null),
                b => b.FirstName("x"),
                b => b.FirstName(null),
                b => b.LastName("x"),
                b => b.LastName(null),
                b => b.Name("x"),
                b => b.Name(null),
                b => b.Avatar("x"),
                b => b.Avatar(null),
                b => b.Email("x"),
                b => b.Email(null),
                b => b.Anonymous(true),
                b => b.Custom("c3", "v3"),
                b => b.PrivateName("n"),
                b => b.PrivateName("o")
            };
            foreach (var mod in mods)
            {
                User modUser = mod(User.Builder(userToCopy)).Build();
                Assert.False(userToCopy.Equals(modUser),
                    JsonConvert.SerializeObject(modUser) + " should not equal " +
                    JsonConvert.SerializeObject(userToCopy));
                Assert.False(userToCopy.GetHashCode() == modUser.GetHashCode(),
                    JsonConvert.SerializeObject(modUser) + " should not have same hashCode as " +
                    JsonConvert.SerializeObject(userToCopy));
            }
        }
    }

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

    public class UserExtensionsTests
    {
        // suppress warnings for these obsolete methods
#pragma warning disable 618
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
            Assert.Equal("AnyValue", (string)user.Custom["AnyAttributeName"]);
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
            Assert.Equal("AnyValue", (string)user.Custom["AnyAttributeName"]);
            Assert.Equal("AnyOtherValue", (string)user.Custom["AnyOtherAttributeName"]);
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
            Assert.Equal("AnyValue", (string)user.Custom["AnyAttributeName"]);
            Assert.Equal("AnyOtherValue", (string)user.Custom["AnyOtherAttributeName"]);
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
#pragma warning restore 618
    }
}
