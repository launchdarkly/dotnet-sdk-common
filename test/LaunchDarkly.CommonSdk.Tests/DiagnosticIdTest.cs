using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class DiagnosticIdTest
    {

        [Fact]
        public void DiagnosticIdTakesKeySuffix()
        {
            DiagnosticId id = new DiagnosticId("suffix-of-sdkkey", Guid.NewGuid());
            Assert.Equal("sdkkey", id._sdkKeySuffix);
        }

        [Fact]
        public void DiagnosticIdTakesKeySuffixOfShortKey()
        {
            DiagnosticId id = new DiagnosticId("abc", Guid.NewGuid());
            Assert.Equal("abc", id._sdkKeySuffix);
        }

        [Fact]
        public void DiagnosticIdTakesKeySuffixOfEmptyKey()
        {
            DiagnosticId id = new DiagnosticId("", Guid.NewGuid());
            Assert.Equal("", id._sdkKeySuffix);
        }

        [Fact]
        public void DiagnosticIdDoesNotCrashWithNullKey()
        {
            DiagnosticId id = new DiagnosticId(null, Guid.NewGuid());
            Assert.Equal(null, id._sdkKeySuffix);
        }

        static readonly JObject _testSerialized = JObject.Parse(@"
            { ""diagnosticId"": ""80de2f3e-5bf8-4ec3-96bf-979318fc7dd4"",
              ""sdkKeySuffix"": ""sdkkey""
            } ");

        [Fact]
        public void DiagnosticIdSerializationHasRequiredFields()
        {
            DiagnosticId id = new DiagnosticId("suffix-of-sdkkey", Guid.Parse("80de2f3e-5bf8-4ec3-96bf-979318fc7dd4"));
            string json = JsonConvert.SerializeObject(id);
            JObject parsed = JObject.Parse(json);
            Assert.True(JToken.DeepEquals(_testSerialized, parsed));
        }

    }

}