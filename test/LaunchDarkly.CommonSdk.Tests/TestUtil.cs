using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WireMock.Logging;
using WireMock.Server;
using WireMock.Settings;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class TestUtil
    {
        public static void AssertJsonEquals(JToken expected, JToken actual)
        {
            Assert.True(JToken.DeepEquals(expected, actual), actual is null ? "null" : actual.ToString() +
                " should be " + expected is null ? "null" : expected.ToString());
        }
        
        public static void AssertContainsInAnyOrder<T>(IEnumerable<T> items, params T[] expectedItems)
        {
            Assert.Equal(expectedItems.Length, items.Count());
            foreach (var e in expectedItems)
            {
                Assert.Contains(e, items);
            }
        }

        public static WireMockServer NewServer()
        {
            return WireMockServer.Start(new WireMockServerSettings
            {
                Logger = new WireMockNullLogger(),
                AllowAnyHttpStatusCodeInResponse = true // without this setting, WireMock will silently change errors like 429 to 200
            });
        }

        public static void WithServer(Action<WireMockServer> a)
        {
            var server = NewServer();
            try
            {
                a(server);
            }
            finally
            {
                server.Stop();
            }
        }
    }
}
