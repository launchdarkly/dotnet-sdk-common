using Xunit;

namespace LaunchDarkly.Sdk
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
            Assert.NotNull(user.PrivateAttributeNames);
            Assert.Equal(0, user.PrivateAttributeNames.Count);
        }
        
        [Fact]
        public void TestEmptyImmutableCollectionsAreReused()
        {
            var user0 = User.WithKey("a");
            var user1 = User.WithKey("b");
            Assert.Same(user0.Custom, user1.Custom);
            Assert.Same(user0.PrivateAttributeNames, user1.PrivateAttributeNames);
        }

        [Fact]
        public void SettingDeprecatedSecondaryKeySetsSecondary()
        {
            var s = "1.2.3.4";
#pragma warning disable 618
            var user = User.Builder(key).SecondaryKey(s).Build();
#pragma warning restore 618
            Assert.Equal(s, user.Secondary);
        }

        [Fact]
        public void TestUserSelfEquality()
        {
            Assert.True(UserToCopy.Equals(UserToCopy));
        }
    }
}
