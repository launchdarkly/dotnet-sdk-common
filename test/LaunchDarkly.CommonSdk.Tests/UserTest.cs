using LaunchDarkly.Client;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class UserTest
    {
        private const string key = "UserKey";
        
        public static readonly User UserToCopy = User.Builder("userkey")
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
        public void UserWithKeySetsKey()
        {
            var user = User.WithKey(key);
            Assert.Equal(key, user.Key);
        }
        
        [Fact]
        public void TestPropertyDefaults()
        {
            var user = User.WithKey(key);
            Assert.Null(user.SecondaryKey);
            Assert.Null(user.IpAddress);
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
        public void DeprecatedIpSetsIP()
        {
            var ip = "1.2.3.4";
            var user = User.WithKey(key);
            user.IpAddress = ip;
            Assert.Equal(ip, user.IPAddress);
        }

        [Fact]
        public void TestUserSelfEquality()
        {
            Assert.True(UserToCopy.Equals(UserToCopy));
        }
        
        [Fact]
        public void TestUserEqualityWithCopyConstructor()
        {
            User copy = new User(UserToCopy);
            Assert.NotSame(UserToCopy, copy);
            Assert.True(copy.Equals(UserToCopy));
            Assert.True(UserToCopy.Equals(copy));
            Assert.Equal(UserToCopy.GetHashCode(), copy.GetHashCode());
        }
    }
}
