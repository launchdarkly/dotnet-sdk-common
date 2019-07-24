using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using LaunchDarkly.Client;

namespace LaunchDarkly.Common.Tests
{
    public class EventUserTest
    {
        static readonly IBaseConfiguration _baseConfig = new SimpleConfiguration();

        static readonly IBaseConfiguration _configWithAllAttrsPrivate = new SimpleConfiguration
        {
            AllAttributesPrivate = true
        };

        static readonly IBaseConfiguration _configWithSomeAttrsPrivate = new SimpleConfiguration
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

        static readonly JObject _userWithAllAttributesJson = JObject.Parse(@"
            { ""key"": ""abc"",
              ""secondary"": ""xyz"",
              ""firstName"": ""Sue"",
              ""lastName"": ""Storm"",
              ""name"": ""Susan"",
              ""country"": ""us"",
              ""avatar"": ""http://avatar"",
              ""ip"": ""1.2.3.4"",
              ""email"": ""test@example.com"",
              ""custom"": { ""bizzle"": ""def"", ""dizzle"": ""ghi"" }
            } ");

        static readonly JObject _userWithAllAttributesPrivateJson = JObject.Parse(@"
            { ""key"": ""abc"",
              ""secondary"": ""xyz"",
              ""privateAttrs"": [ ""avatar"", ""bizzle"", ""country"", ""dizzle"", ""email"",
              ""firstName"", ""ip"", ""lastName"", ""name"" ]
            } ");

        static readonly JObject _userWithSomeAttributesPrivateJson = JObject.Parse(@"
            { ""key"": ""abc"",
              ""secondary"": ""xyz"",
              ""lastName"": ""Storm"",
              ""name"": ""Susan"",
              ""country"": ""us"",
              ""avatar"": ""http://avatar"",
              ""ip"": ""1.2.3.4"",
              ""email"": ""test@example.com"",
              ""custom"": { ""dizzle"": ""ghi"" },
              ""privateAttrs"": [ ""bizzle"", ""firstName"" ]
            } ");

        static readonly JObject _anonUserWithAllAttributesPrivateJson = JObject.Parse(@"
            { ""key"": ""abc"",
              ""anonymous"": true,
              ""privateAttrs"": [ ""bizzle"", ""dizzle"" ]
            } ");
        
        [Fact]
        public void SerializingAUserWithNoAnonymousSetYieldsNoAnonymous()
        {
            var user = User.WithKey("foo@bar.com");
            var eu = EventUser.FromUser(user, _baseConfig);
            var json = JsonConvert.SerializeObject(eu);
            Assert.False(json.Contains("anonymous"));
        }

        [Fact]
        public void AllUserAttributesAreIncludedByDefault()
        {
            EventUser eu = EventUser.FromUser(_baseUser, _baseConfig);
            CheckJsonSerialization(eu, _userWithAllAttributesJson);
        }

        [Fact]
        public void CanHideAllAttributesExceptKeyForNonAnonUser()
        {
            EventUser eu = EventUser.FromUser(_baseUser, _configWithAllAttrsPrivate);
            CheckJsonSerialization(eu, _userWithAllAttributesPrivateJson);
        }

        [Fact]
        public void CanHideAllAttributesExceptKeyAndAnonymousForAnonUser()
        {
            EventUser eu = EventUser.FromUser(_anonUser, _configWithAllAttrsPrivate);
            CheckJsonSerialization(eu, _anonUserWithAllAttributesPrivateJson);
        }

        [Fact]
        public void CanHideSomeAttributesWithGlobalSet()
        {
            EventUser eu = EventUser.FromUser(_baseUser, _configWithSomeAttrsPrivate);
            CheckJsonSerialization(eu, _userWithSomeAttributesPrivateJson);
        }

        [Fact]
        public void CanHideSomeAttributesPerUser()
        {
            EventUser eu = EventUser.FromUser(_userSpecifyingOwnPrivateAttrs, _baseConfig);
            CheckJsonSerialization(eu, _userWithSomeAttributesPrivateJson);
        }

        [Fact]
        public void CustomIsNullIfUserHasNoCustomAttributes()
        {
            var u = User.WithKey("key");
            var eu = EventUser.FromUser(u, _baseConfig);
            Assert.Null(eu.Custom);
        }

        [Fact]
        public void PrivateAttrsIsNullIfUserHasNoPrivateAttributes()
        {
            var u = User.WithKey("key");
            var eu = EventUser.FromUser(u, _baseConfig);
            Assert.Null(eu.PrivateAttrs);
        }

        [Fact]
        public void CustomAttributesDictionaryIsReusedIfNoneArePrivate()
        {
            EventUser eu = EventUser.FromUser(_baseUser, _baseConfig);
            Assert.Same(_baseUser.Custom, eu.Custom);
        }

        private void CheckJsonSerialization(object o, JObject shouldBe)
        {
            string json = JsonConvert.SerializeObject(o);
            JObject parsed = JObject.Parse(json);
            TestUtil.AssertJsonEquals(shouldBe, parsed);
        }
    }
}
