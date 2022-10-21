using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LaunchDarkly.Sdk.Json;
using Xunit;

namespace LaunchDarkly.Sdk
{
    public class UserBuilderTestBase
    {
        public static readonly Context UserToCopy = User.Builder("userkey")
                .IPAddress("1")
                .Country("US")
                .FirstName("f")
                .LastName("l")
                .Name("n")
                .Avatar("a")
                .Email("e")
                .Custom("c1", "v1")
                .Custom("c2", "v2").AsPrivateAttribute()
                .Build();

        public struct StringPropertyDesc
        {
            public string Name;
            public Func<IUserBuilder, Func<string, IUserBuilder>> Setter;
            public Func<Context, string> Getter;

            public StringPropertyDesc(string name, Func<IUserBuilder, Func<string, IUserBuilder>> setter, Func<Context, string> getter)
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
            public Func<Context, string> Getter;

            public StringPropertyCanBePrivateDesc(string name, Func<IUserBuilder, Func<string, IUserBuilderCanMakeAttributePrivate>> setter, Func<Context, string> getter)
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
            new StringPropertyDesc("ip", b => b.IPAddress, u => u.GetValue("ip").AsString),
            new StringPropertyDesc("country", b => b.Country, u => u.GetValue("country").AsString),
            new StringPropertyDesc("firstName", b => b.FirstName, u => u.GetValue("firstName").AsString),
            new StringPropertyDesc("lastName", b => b.LastName, u => u.GetValue("lastName").AsString),
            new StringPropertyDesc("name", b => b.Name, u => u.Name),
            new StringPropertyDesc("avatar", b => b.Avatar, u => u.GetValue("avatar").AsString),
            new StringPropertyDesc("email", b => b.Email, u => u.GetValue("email").AsString)
        );

        public static IEnumerable<object[]> PrivateStringProperties = MakeParams(
            new StringPropertyCanBePrivateDesc("ip", b => b.IPAddress, u => u.GetValue("ip").AsString),
            new StringPropertyCanBePrivateDesc("country", b => b.Country, u => u.GetValue("country").AsString),
            new StringPropertyCanBePrivateDesc("firstName", b => b.FirstName, u => u.GetValue("firstName").AsString),
            new StringPropertyCanBePrivateDesc("lastName", b => b.LastName, u => u.GetValue("lastName").AsString),
            new StringPropertyCanBePrivateDesc("name", b => b.Name, u => u.Name),
            new StringPropertyCanBePrivateDesc("avatar", b => b.Avatar, u => u.GetValue("avatar").AsString),
            new StringPropertyCanBePrivateDesc("email", b => b.Email, u => u.GetValue("email").AsString)
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
            Assert.Empty(user.PrivateAttributes);
        }

        [Theory]
        [MemberData(nameof(PrivateStringProperties))]
        public void BuilderCanSetPrivateStringProperty(StringPropertyCanBePrivateDesc p)
        {
            var expectedValue = p.Name + " value";
            var user = p.Setter(User.Builder(key))(expectedValue).AsPrivateAttribute().Build();
            Assert.Equal(expectedValue, p.Getter(user));
            Assert.Equal(ImmutableList.Create(AttributeRef.FromLiteral(p.Name)), user.PrivateAttributes);
        }

        [Fact]
        public void AnonymousDefaultsToFalse()
        {
            var user = User.Builder(key).Build();
            Assert.False(user.Anonymous);
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
            var user = User.Builder(key).Anonymous(true).Anonymous(false).Build();
            Assert.False(user.Anonymous);
        }

        private void TestCustomAttribute<T>(T value,
            Func<IUserBuilder, string, T, IUserBuilderCanMakeAttributePrivate> setter, LdValue.Converter<T> converter)
        {
            var user0 = setter(User.Builder(key), "foo", value).Build();
            Assert.Equal(value, converter.ToType(user0.GetValue("foo")));
            Assert.Empty(user0.PrivateAttributes);

            var user1 = setter(User.Builder(key), "bar", value).AsPrivateAttribute().Build();
            Assert.Equal(value, converter.ToType(user1.GetValue("bar")));
            Assert.Equal(ImmutableList.Create(AttributeRef.FromLiteral("bar")), user1.PrivateAttributes);
        }

        [Fact]
        public void BuilderCanSetJsonCustomAttribute()
        {
            var value = LdValue.Convert.Int.ArrayOf(1, 2);
            var user0 = User.Builder(key).Custom("foo", value).Build();
            Assert.Equal(value, user0.GetValue("foo"));
            Assert.Empty(user0.PrivateAttributes);

            var user1 = User.Builder(key).Custom("bar", value).AsPrivateAttribute().Build();
            Assert.Equal(value, user1.GetValue("bar"));
            Assert.Equal(ImmutableList.Create(AttributeRef.FromLiteral("bar")), user1.PrivateAttributes);
        }

        [Fact]
        public void BuilderCanSetBoolCustomAttribute()
        {
            TestCustomAttribute<bool>(true, (b, n, v) => b.Custom(n, v), LdValue.Convert.Bool);
        }

        [Fact]
        public void BuilderCanSetStringCustomAttribute()
        {
            TestCustomAttribute<string>("x", (b, n, v) => b.Custom(n, v), LdValue.Convert.String);
        }

        [Fact]
        public void BuilderCanSetIntCustomAttribute()
        {
            TestCustomAttribute<int>(3, (b, n, v) => b.Custom(n, v), LdValue.Convert.Int);
        }

        [Fact]
        public void BuilderCanSetLongCustomAttribute()
        {
            TestCustomAttribute<long>(1634661422123L, (b, n, v) => b.Custom(n, v), LdValue.Convert.Long);
        }

        [Fact]
        public void BuilderCanSetFloatCustomAttribute()
        {
            TestCustomAttribute<float>(1.5f, (b, n, v) => b.Custom(n, v), LdValue.Convert.Float);
        }

        [Fact]
        public void BuilderCanSetDoubleCustomAttribute()
        {
            TestCustomAttribute<double>(double.MaxValue, (b, n, v) => b.Custom(n, v), LdValue.Convert.Double);
        }
        
        [Fact]
        public void TestUserEqualityWithBuilderFromUser()
        {
            Context copy = User.Builder(UserToCopy).Build();
            Assert.True(copy.Equals(UserTest.UserToCopy));
            Assert.True(UserTest.UserToCopy.Equals(copy));
            Assert.Equal(UserTest.UserToCopy.GetHashCode(), copy.GetHashCode());
        }

        [Fact]
        public void TestUserInequalityWithModifiedBuilder()
        {
            Func<IUserBuilder, IUserBuilder>[] mods = {
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
                Context modUser = mod(User.Builder(UserToCopy)).Build();
                Assert.False(UserTest.UserToCopy.Equals(modUser),
                    LdJsonSerialization.SerializeObject(modUser) + " should not equal " +
                    LdJsonSerialization.SerializeObject(UserToCopy));
                Assert.False(UserTest.UserToCopy.GetHashCode() == modUser.GetHashCode(),
                    LdJsonSerialization.SerializeObject(modUser) + " should not have same hashCode as " +
                    LdJsonSerialization.SerializeObject(UserToCopy));
            }
        }
    }
}
