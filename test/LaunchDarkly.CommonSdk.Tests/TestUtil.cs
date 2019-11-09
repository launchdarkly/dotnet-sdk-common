using System.Collections.Generic;
using System.Linq;
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
        
        public static void AssertContainsInAnyOrder<T>(IEnumerable<T> items, params T[] expectedItems)
        {
            Assert.Equal(expectedItems.Length, items.Count());
            foreach (var e in expectedItems)
            {
                Assert.Contains(e, items);
            }
        }
    }
}
