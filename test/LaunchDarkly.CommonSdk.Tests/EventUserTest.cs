using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Xunit;
using LaunchDarkly.Client;

namespace LaunchDarkly.Common.Tests
{
    public class EventUserTest
    {
        static readonly SimpleConfiguration _baseConfig = new SimpleConfiguration();

        static readonly SimpleConfiguration _configWithAllAttrsPrivate = new SimpleConfiguration
        {
            AllAttributesPrivate = true
        };

        static readonly SimpleConfiguration _configWithSomeAttrsPrivate = new SimpleConfiguration
        {
            PrivateAttributeNames = new HashSet<string>(new string[] { "firstName", "bizzle" })
        };

        static readonly User _baseUser = User.Builder("abc")
            .SecondaryKey("xyz")
            .FirstName("Sue")
            .LastName("Storm")
            .Name("Susan")
            .Country("us")
            .Avatar("http://avatar")
            .IPAddress("1.2.3.4")
            .Email("test@example.com")
            .Custom("bizzle", "def")
            .Custom("dizzle", "ghi")
            .Build();

        static readonly User _userSpecifyingOwnPrivateAttrs = User.Builder("abc")
            .SecondaryKey("xyz")
            .FirstName("Sue").AsPrivateAttribute()
            .LastName("Storm")
            .Name("Susan")
            .Country("us")
            .Avatar("http://avatar")
            .IPAddress("1.2.3.4")
            .Email("test@example.com")
            .Custom("bizzle", "def").AsPrivateAttribute()
            .Custom("dizzle", "ghi")
            .Build();

        static readonly User _anonUser = User.Builder("abc")
            .Anonymous(true)
            .Custom("bizzle", "def")
            .Custom("dizzle", "ghi")
            .Build();
        
        [Fact]
        public void AllUserAttributesAreIncludedByDefault()
        {
            EventUser eu = EventUser.FromUser(_baseUser, _baseConfig);
            Assert.Equal(_baseUser.Key, eu.Key);
            Assert.Equal(_baseUser.SecondaryKey, eu.SecondaryKey);
            Assert.Equal(_baseUser.FirstName, eu.FirstName);
            Assert.Equal(_baseUser.LastName, eu.LastName);
            Assert.Equal(_baseUser.Name, eu.Name);
            Assert.Equal(_baseUser.Avatar, eu.Avatar);
            Assert.Equal(_baseUser.IPAddress, eu.IpAddress);
            Assert.Equal(_baseUser.Email, eu.Email);
            Assert.Null(eu.Anonymous);
            Assert.Equal(_baseUser.Custom, eu.Custom);
            Assert.Null(eu.PrivateAttrs);
        }

        [Fact]
        public void CanHideAllAttributesExceptKeyForNonAnonUser()
        {
            EventUser eu = EventUser.FromUser(_baseUser, _configWithAllAttrsPrivate);
            Assert.Equal(_baseUser.Key, eu.Key);
            Assert.Equal(_baseUser.SecondaryKey, eu.SecondaryKey);
            Assert.Null(eu.FirstName);
            Assert.Null(eu.LastName);
            Assert.Null(eu.Name);
            Assert.Null(eu.Avatar);
            Assert.Null(eu.IpAddress);
            Assert.Null(eu.Email);
            Assert.Null(eu.Anonymous);
            Assert.Null(eu.Custom);
            Assert.Equal(new List<string> { "avatar", "bizzle", "country", "dizzle", "email", "firstName", "ip", "lastName", "name" },
                eu.PrivateAttrs);
        }

        [Fact]
        public void CanHideAllAttributesExceptKeyAndAnonymousForAnonUser()
        {
            EventUser eu = EventUser.FromUser(_anonUser, _configWithAllAttrsPrivate);
            Assert.Equal(_anonUser.Key, eu.Key);
            Assert.Equal(_anonUser.SecondaryKey, eu.SecondaryKey);
            Assert.Null(eu.FirstName);
            Assert.Null(eu.LastName);
            Assert.Null(eu.Name);
            Assert.Null(eu.Avatar);
            Assert.Null(eu.IpAddress);
            Assert.Null(eu.Email);
            Assert.True(eu.Anonymous);
            Assert.Null(eu.Custom);
            Assert.Equal(new List<string> { "bizzle", "dizzle" }, eu.PrivateAttrs);
        }

        [Fact]
        public void CanHideSomeAttributesWithGlobalSet()
        {
            EventUser eu = EventUser.FromUser(_baseUser, _configWithSomeAttrsPrivate);
            Assert.Equal(_baseUser.Key, eu.Key);
            Assert.Equal(_baseUser.SecondaryKey, eu.SecondaryKey);
            Assert.Null(eu.FirstName);
            Assert.Equal(_baseUser.LastName, eu.LastName);
            Assert.Equal(_baseUser.Name, eu.Name);
            Assert.Equal(_baseUser.Avatar, eu.Avatar);
            Assert.Equal(_baseUser.IPAddress, eu.IpAddress);
            Assert.Equal(_baseUser.Email, eu.Email);
            Assert.Null(eu.Anonymous);
            Assert.Equal(new Dictionary<string, JToken> { { "dizzle", new JValue("ghi") } }, eu.Custom);
            Assert.Equal(new List<string> { "bizzle", "firstName" }, eu.PrivateAttrs);
        }

        [Fact]
        public void CanHideSomeAttributesPerUser()
        {
            EventUser eu = EventUser.FromUser(_userSpecifyingOwnPrivateAttrs, _baseConfig);
            Assert.Equal(_baseUser.Key, eu.Key);
            Assert.Equal(_baseUser.SecondaryKey, eu.SecondaryKey);
            Assert.Null(eu.FirstName);
            Assert.Equal(_baseUser.LastName, eu.LastName);
            Assert.Equal(_baseUser.Name, eu.Name);
            Assert.Equal(_baseUser.Avatar, eu.Avatar);
            Assert.Equal(_baseUser.IPAddress, eu.IpAddress);
            Assert.Equal(_baseUser.Email, eu.Email);
            Assert.Null(eu.Anonymous);
            Assert.Equal(new Dictionary<string, JToken> { { "dizzle", new JValue("ghi") } }, eu.Custom);
            Assert.Equal(new List<string> { "bizzle", "firstName" }, eu.PrivateAttrs);
        }
    }
}
