using LaunchDarkly.Client;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class UserTest
    {
        private const string key = "UserKey";
        
        public static readonly User UserToCopy = User.Builder("userkey")
                .Secondary("s")
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

        [Fact]
        public void ConstructorSetsKey()
        {
#pragma warning disable 618
            var user = new User(key);
#pragma warning restore 618
            Assert.Equal(key, user.Key);
        }

        [Fact]
        public void UserWithKeySetsKey()
        {
            var user = User.WithKey(key);
            Assert.Equal(key, user.Key);
        }
        
        [Fact]
        public void TestPropertyDefaults()
        {
            var user = User.WithKey(key);
            Assert.Null(user.Secondary);
            Assert.Null(user.IPAddress);
            Assert.Null(user.Country);
            Assert.Null(user.FirstName);
            Assert.Null(user.LastName);
            Assert.Null(user.Name);
            Assert.Null(user.Avatar);
            Assert.Null(user.Email);
            Assert.NotNull(user.Custom);
            Assert.Equal(0, user.Custom.Count);
            Assert.Null(user.PrivateAttributeNames);
        }

        [Fact]
        public void SettingDeprecatedIpSetsIP()
        {
            var ip = "1.2.3.4";
            var user = User.WithKey(key);
#pragma warning disable 618
            user.IpAddress = ip;
#pragma warning restore 618
            Assert.Equal(ip, user.IPAddress);
        }

        [Fact]
        public void GettingDeprecatedIpGetsIP()
        {
            var ip = "1.2.3.4";
            var user = User.WithKey(key);
            user.IPAddress = ip;
#pragma warning disable 618
            Assert.Equal(ip, user.IpAddress);
#pragma warning restore 618
        }

        [Fact]
        public void SettingDeprecatedSecondaryKeySetsSecondary()
        {
            var s = "1.2.3.4";
            var user = User.WithKey(key);
#pragma warning disable 618
            user.SecondaryKey = s;
#pragma warning restore 618
            Assert.Equal(s, user.Secondary);
        }

        [Fact]
        public void GettingDeprecatedSecondaryKeyGetsSecondary()
        {
            var s = "1.2.3.4";
            var user = User.WithKey(key);
            user.Secondary = s;
#pragma warning disable 618
            Assert.Equal(s, user.SecondaryKey);
#pragma warning restore 618
        }

        [Fact]
        public void TestUserSelfEquality()
        {
            Assert.True(UserToCopy.Equals(UserToCopy));
        }
        
        [Fact]
        public void TestUserEqualityWithCopyConstructor()
        {
#pragma warning disable 618
            User copy = new User(UserToCopy);
#pragma warning restore 618
            Assert.NotSame(UserToCopy, copy);
            Assert.True(copy.Equals(UserToCopy));
            Assert.True(UserToCopy.Equals(copy));
            Assert.Equal(UserToCopy.GetHashCode(), copy.GetHashCode());
        }
    }
}
