using LaunchDarkly.Client;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class TestUtil
    {
        public static void AssertJsonEquals(string expected, string actual)
        {
            Assert.Equal(LdValue.Parse(expected), LdValue.Parse(actual));
        }
    }
}
