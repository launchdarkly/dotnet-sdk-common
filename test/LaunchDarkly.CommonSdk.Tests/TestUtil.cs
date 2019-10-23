using Newtonsoft.Json.Linq;
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

        public static void AssertJsonEquals(string expected, string actual)
        {
            AssertJsonEquals(JObject.Parse(expected), JObject.Parse(actual));
        }
    }
}
