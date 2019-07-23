using System;
using System.Collections.Generic;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class UserBuilderTest
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
                new StringTest("ip", b => b.IPAddress, u => u.IPAddress),
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
                new StringTest("ip", b => b.PrivateIPAddress, u => u.IPAddress),
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
        public void CustomDefaultsToEmptyDictionary()
        {
            var user = User.Builder(key).Build();
            Assert.NotNull(user.Custom);
            Assert.Equal(0, user.Custom.Count);
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
        
        [Fact]
        public void TestUserEqualityWithBuilderFromUser()
        {
            User copy = User.Builder(UserTest.UserToCopy).Build();
            Assert.NotSame(UserTest.UserToCopy, copy);
            Assert.True(copy.Equals(UserTest.UserToCopy));
            Assert.True(UserTest.UserToCopy.Equals(copy));
            Assert.Equal(UserTest.UserToCopy.GetHashCode(), copy.GetHashCode());
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
                User modUser = mod(User.Builder(UserTest.UserToCopy)).Build();
                Assert.False(UserTest.UserToCopy.Equals(modUser),
                    JsonConvert.SerializeObject(modUser) + " should not equal " +
                    JsonConvert.SerializeObject(UserTest.UserToCopy));
                Assert.False(UserTest.UserToCopy.GetHashCode() == modUser.GetHashCode(),
                    JsonConvert.SerializeObject(modUser) + " should not have same hashCode as " +
                    JsonConvert.SerializeObject(UserTest.UserToCopy));
            }
        }
    }
}
