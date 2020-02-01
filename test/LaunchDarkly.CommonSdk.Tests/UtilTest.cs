using System;
using System.Collections.Generic;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class UtilTest
    {
        [Fact]
        public void CanConvertDateTimeToUnixMillis()
        {
            var dateTime = new DateTime(2000, 1, 1, 0, 0, 10, DateTimeKind.Utc);
            var dateTimeMillis = 946684810000;
            var actualEpochMillis = Util.GetUnixTimestampMillis(dateTime);
            Assert.Equal(dateTimeMillis, actualEpochMillis);
        }

        [Fact]
        public void CommonRequestHeadersHaveSdkKey()
        {
            var config = new SimpleConfiguration
            {
                SdkKey = "MyKey"
            };
            IDictionary<string, string> headers = Util.GetRequestHeaders(config, SimpleClientEnvironment.Instance);
            Assert.Equal("MyKey", headers["Authorization"]);
        }

        [Fact]
        public void CommonRequestHeadersHaveUserAgent()
        {
            var config = new SimpleConfiguration();
            IDictionary<string, string> headers = Util.GetRequestHeaders(config, SimpleClientEnvironment.Instance);
            Assert.Equal("CommonClient/1.0.0", headers["User-Agent"]);
        }

        [Fact]
        public void CommonRequestHeadersDontIncludeWrapperWhenNotSet()
        {
            var config = new SimpleConfiguration();
            IDictionary<string, string> headers = Util.GetRequestHeaders(config, SimpleClientEnvironment.Instance);
            Assert.False(headers.ContainsKey("X-LaunchDarkly-Wrapper"));
        }

        [Fact]
        public void CommonRequestHeadersIncludeWrapperNameWhenSet()
        {
            var config = new SimpleConfiguration
            {
                WrapperName = "Xamarin"
            };
            IDictionary<string, string> headers = Util.GetRequestHeaders(config, SimpleClientEnvironment.Instance);
            Assert.Equal("Xamarin", headers["X-LaunchDarkly-Wrapper"]);
        }

        [Fact]
        public void CommonRequestHeadersIgnoreWrapperVersionWithoutName()
        {
            var config = new SimpleConfiguration
            {
                WrapperVersion = "1.0.0"
            };
            IDictionary<string, string> headers = Util.GetRequestHeaders(config, SimpleClientEnvironment.Instance);
            Assert.False(headers.ContainsKey("X-LaunchDarkly-Wrapper"));
        }

        [Fact]
        public void CommonRequestHeadersIncludeWrapperNameAndVersionWhenSet()
        {
            var config = new SimpleConfiguration
            {
                WrapperName = "Xamarin",
                WrapperVersion = "1.0.0"
            };
            IDictionary<string, string> headers = Util.GetRequestHeaders(config, SimpleClientEnvironment.Instance);
            Assert.Equal("Xamarin/1.0.0", headers["X-LaunchDarkly-Wrapper"]);
        }
    }
}