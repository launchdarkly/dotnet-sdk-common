using System;
using System.Collections.Generic;
using System.Linq;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class UserBuilderTestBase
    {
        public struct StringPropertyDesc
        {
            public string Name;
            public Func<IUserBuilder, Func<string, IUserBuilder>> Setter;
            public Func<User, string> Getter;

            public StringPropertyDesc(string name, Func<IUserBuilder, Func<string, IUserBuilder>> setter, Func<User, string> getter)
            {
                Name = name;
                Setter = setter;
                Getter = getter;
            }

            public override string ToString() => Name;
        }

        public struct StringPropertyCanBePrivateDesc
        {
            public string Name;
            public Func<IUserBuilder, Func<string, IUserBuilderCanMakeAttributePrivate>> Setter;
            public Func<User, string> Getter;

            public StringPropertyCanBePrivateDesc(string name, Func<IUserBuilder, Func<string, IUserBuilderCanMakeAttributePrivate>> setter, Func<User, string> getter)
            {
                Name = name;
                Setter = setter;
                Getter = getter;
            }

            public override string ToString() => Name;
        }

        private static IEnumerable<object[]> MakeParams(params object[] ps)
        {
            return ps.Select(p => new object[] { p });
        }

        public static IEnumerable<object[]> AllStringProperties => MakeParams(
            new StringPropertyDesc("key", b => b.Key, u => u.Key),
            new StringPropertyDesc("secondary", b => b.SecondaryKey, u => u.SecondaryKey),
            new StringPropertyDesc("ip", b => b.IPAddress, u => u.IPAddress),
            new StringPropertyDesc("country", b => b.Country, u => u.Country),
            new StringPropertyDesc("firstName", b => b.FirstName, u => u.FirstName),
            new StringPropertyDesc("lastName", b => b.LastName, u => u.LastName),
            new StringPropertyDesc("name", b => b.Name, u => u.Name),
            new StringPropertyDesc("avatar", b => b.Avatar, u => u.Avatar),
            new StringPropertyDesc("email", b => b.Email, u => u.Email)
        );

        public static IEnumerable<object[]> PrivateStringProperties = MakeParams(
            new StringPropertyCanBePrivateDesc("ip", b => b.IPAddress, u => u.IPAddress),
            new StringPropertyCanBePrivateDesc("country", b => b.Country, u => u.Country),
            new StringPropertyCanBePrivateDesc("firstName", b => b.FirstName, u => u.FirstName),
            new StringPropertyCanBePrivateDesc("lastName", b => b.LastName, u => u.LastName),
            new StringPropertyCanBePrivateDesc("name", b => b.Name, u => u.Name),
            new StringPropertyCanBePrivateDesc("avatar", b => b.Avatar, u => u.Avatar),
            new StringPropertyCanBePrivateDesc("email", b => b.Email, u => u.Email)
        );
    }

    public class UserBuilderTest : UserBuilderTestBase
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

        [Theory]
        [MemberData(nameof(AllStringProperties))]
        public void StringPropertyIsNullByDefault(StringPropertyDesc p)
        {
            var user = User.Builder(key).Build();
            var value = p.Getter(user);
            if (p.Name == "key")
            {
                Assert.Equal(key, value); 
            }
            else
            {
                Assert.Null(value);
            }
        }

        [Theory]
        [MemberData(nameof(AllStringProperties))]
        public void BuilderCanSetStringProperty(StringPropertyDesc p)
        {
            var expectedValue = "x";
            var user = p.Setter(User.Builder(key))(expectedValue).Build();
            Assert.Equal(expectedValue, p.Getter(user));
            Assert.Null(user.PrivateAttributeNames);
        }

        [Theory]
        [MemberData(nameof(PrivateStringProperties))]
        public void BuilderCanSetPrivateStringProperty(StringPropertyCanBePrivateDesc p)
        {
            var expectedValue = p.Name + " value";
            var user = p.Setter(User.Builder(key))(expectedValue).AsPrivateAttribute().Build();
            Assert.Equal(expectedValue, p.Getter(user));
            Assert.Equal(new HashSet<string> { p.Name }, user.PrivateAttributeNames);
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
            Func<IUserBuilder, string, T, IUserBuilderCanMakeAttributePrivate> setter)
        {
            var user0 = setter(User.Builder(key), "foo", value).Build();
            Assert.Equal<object>(value, user0.Custom["foo"].Value<T>());
            Assert.Null(user0.PrivateAttributeNames);

            var user1 = setter(User.Builder(key), "bar", value).AsPrivateAttribute().Build();
            Assert.Equal<object>(value, user1.Custom["bar"].Value<T>());
            Assert.Equal(new HashSet<string> { "bar" }, user1.PrivateAttributeNames);
        }

        [Fact]
        public void BuilderCanSetJsonCustomAttribute()
        {
            var value = new JArray(new List<JToken>() { new JValue(true), new JValue(1.5) });
            TestCustomAttribute<JToken>(value, (b, n, v) => b.Custom(n, v));
        }

        [Fact]
        public void BuilderCanSetImmutableJsonCustomAttribute()
        {
            var value = ImmutableJsonValue.FromValues(new int[] { 1, 2 });
            var user0 = User.Builder(key).Custom("foo", value).Build();
            Assert.Equal(value, ImmutableJsonValue.FromJToken(user0.Custom["foo"]));
            Assert.Null(user0.PrivateAttributeNames);

            var user1 = User.Builder(key).Custom("bar", value).AsPrivateAttribute().Build();
            Assert.Equal(value, ImmutableJsonValue.FromJToken(user1.Custom["bar"]));
            Assert.Equal(new HashSet<string> { "bar" }, user1.PrivateAttributeNames);
        }

        [Fact]
        public void BuilderCanSetBoolCustomAttribute()
        {
            TestCustomAttribute<bool>(true, (b, n, v) => b.Custom(n, v));
        }

        [Fact]
        public void BuilderCanSetStringCustomAttribute()
        {
            TestCustomAttribute<string>("x", (b, n, v) => b.Custom(n, v));
        }

        [Fact]
        public void BuilderCanSetIntCustomAttribute()
        {
            TestCustomAttribute<int>(3, (b, n, v) => b.Custom(n, v));
        }

        [Fact]
        public void BuilderCanSetFloatCustomAttribute()
        {
            TestCustomAttribute<float>(1.5f, (b, n, v) => b.Custom(n, v));
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
            Func<IUserBuilder, IUserBuilder>[] mods = {
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
                b => b.Name("n").AsPrivateAttribute(),
                b => b.Name("o").AsPrivateAttribute()
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
