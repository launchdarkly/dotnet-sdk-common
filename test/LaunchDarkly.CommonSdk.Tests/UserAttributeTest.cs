using System;
using Xunit;

namespace LaunchDarkly.Sdk
{
    public class UserAttributeTest
    {
        [Fact]
        public void TestBuiltIns()
        {
            TestBuiltInString(UserAttribute.Key, "key", (b, v) => b.Key(v));
            // TestBuiltInString(UserAttribute.Secondary, "secondary", (b, v) => b.Secondary(v)); // Secondary is no longer addressable like a regular attribute
            TestBuiltInString(UserAttribute.IPAddress, "ip", (b, v) => b.IPAddress(v));
            TestBuiltInString(UserAttribute.Email, "email", (b, v) => b.Email(v));
            TestBuiltInString(UserAttribute.Name, "name", (b, v) => b.Name(v));
            TestBuiltInString(UserAttribute.Avatar, "avatar", (b, v) => b.Avatar(v));
            TestBuiltInString(UserAttribute.FirstName, "firstName", (b, v) => b.FirstName(v));
            TestBuiltInString(UserAttribute.LastName, "lastName", (b, v) => b.LastName(v));
            TestBuiltInString(UserAttribute.Country, "country", (b, v) => b.Country(v));

            Assert.Equal("anonymous", UserAttribute.Anonymous.AttributeName);
            Assert.True(User.Builder(".").Anonymous(true).Build().Anonymous);
        }

        [Fact]
        public void TestCustom()
        {
            var a = UserAttribute.ForName("age");
            Assert.Equal(LdValue.Null, User.WithKey(".").GetValue(a.AttributeName));
            Assert.Equal(LdValue.Of(99), User.Builder(".").Custom("age", LdValue.Of(99)).Build()
                .GetValue(a.AttributeName));
        }

        [Fact]
        public void TestEquality()
        {
            Assert.True(UserAttribute.Key.Equals(UserAttribute.Key));
            Assert.True(UserAttribute.Key.Equals(UserAttribute.ForName("key")));
            Assert.False(UserAttribute.Key.Equals(UserAttribute.Email));

#pragma warning disable CS1718 // Comparison made to same variable
            Assert.True(UserAttribute.Key == UserAttribute.Key);
#pragma warning restore CS1718 // Comparison made to same variable
            Assert.True(UserAttribute.Key == UserAttribute.ForName("key"));
            Assert.True(UserAttribute.ForName("x") == UserAttribute.ForName("x"));
            Assert.False(UserAttribute.Key == UserAttribute.Email);

#pragma warning disable CS1718 // Comparison made to same variable
            Assert.False(UserAttribute.Key != UserAttribute.Key);
#pragma warning restore CS1718 // Comparison made to same variable
            Assert.False(UserAttribute.Key != UserAttribute.ForName("key"));
            Assert.False(UserAttribute.ForName("x") != UserAttribute.ForName("x"));
            Assert.True(UserAttribute.Key != UserAttribute.Email);
        }

        private void TestBuiltInString(UserAttribute a, string name,
            Action<IUserBuilder, string> setter)
        {
            Assert.Equal(name, a.AttributeName);

            var b = User.Builder(".");
            setter(b, "x");
            Assert.Equal(LdValue.Of("x"), b.Build().GetValue(a.AttributeName));
        }
    }
}
