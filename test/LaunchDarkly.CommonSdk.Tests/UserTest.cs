﻿using LaunchDarkly.Client;
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
            Assert.Null(user.SecondaryKey);
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
        public void TestUserSelfEquality()
        {
            Assert.True(UserToCopy.Equals(UserToCopy));
        }
    }
}
