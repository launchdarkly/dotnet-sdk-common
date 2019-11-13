using System.Collections.Generic;
using System.Linq;
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
