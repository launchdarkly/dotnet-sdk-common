using Xunit;

namespace LaunchDarkly.Sdk
{
    public class UserTest
    {
        private const string key = "UserKey";
        
        public static readonly Context UserToCopy = User.Builder("userkey")
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
        public void UserWithKeyOnly()
        {
            var user = User.WithKey(key);
            Assert.Equal(Context.New(key), user);
        }
    }
}
