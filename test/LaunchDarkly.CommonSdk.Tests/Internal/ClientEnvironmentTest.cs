using System;
using Xunit;

namespace LaunchDarkly.Sdk.Internal
{
    public class ClientEnvironmentTest
    {
        [Fact]
        public void CanGetVersionString()
        {
            var ce = new SimpleClientEnvironment();
            Assert.Equal("1.0.0", ce.VersionString);
            // Note, this is the version string of the *test* assembly (since that's where
            // SimpleClientEnvironment is defined).
        }

        [Fact]
        public void CanGetVersionObject()
        {
            var ce = new SimpleClientEnvironment();
            var expectedVersion = new Version("1.0.0.0");
            Assert.Equal(expectedVersion, ce.Version);
        }
    }
}
