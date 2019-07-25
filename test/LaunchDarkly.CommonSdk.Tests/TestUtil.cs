using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class TestUtil
    {
        public static void AssertJsonEquals(JToken expected, JToken actual)
        {
            Assert.True(JToken.DeepEquals(expected, actual), actual.ToString() + " should be " + expected.ToString());
        }
    }
}
