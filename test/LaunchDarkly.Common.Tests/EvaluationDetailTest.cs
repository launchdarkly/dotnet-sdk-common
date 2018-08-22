using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Common.Tests
{
    public class EvaluationDetailTest
    {
        [Fact]
        public void TestSerializeOffReason()
        {
            EvaluationReason reason = new EvaluationReason
            {
                Kind = EvaluationReasonKind.OFF
            };
            var json = @"{""kind"":""OFF""}";
            AssertJsonEqual(json, JsonConvert.SerializeObject(reason));
            Assert.Equal(reason, JsonConvert.DeserializeObject<EvaluationReason>(json));
            Assert.Equal("OFF", reason.ToString());
        }
        
        [Fact]
        public void TestSerializeTargetMatchReason()
        {
            EvaluationReason reason = new EvaluationReason
            {
                Kind = EvaluationReasonKind.TARGET_MATCH
            };
            var json = @"{""kind"":""TARGET_MATCH""}";
            AssertJsonEqual(json, JsonConvert.SerializeObject(reason));
            Assert.Equal(reason, JsonConvert.DeserializeObject<EvaluationReason>(json));
            Assert.Equal("TARGET_MATCH", reason.ToString());
        }

        [Fact]
        public void TestSerializeRuleMatchReason()
        {
            EvaluationReason reason = new EvaluationReason
            {
                Kind = EvaluationReasonKind.RULE_MATCH,
                RuleIndex = 1,
                RuleId = "id"
            };
            var json = @"{""kind"":""RULE_MATCH"",""ruleIndex"":1,""ruleId"":""id""}";
            AssertJsonEqual(json, JsonConvert.SerializeObject(reason));
            Assert.Equal(reason, JsonConvert.DeserializeObject<EvaluationReason>(json));
            Assert.Equal("RULE_MATCH(1,id)", reason.ToString());
        }

        [Fact]
        public void TestSerializePrerequisitesFailedReason()
        {
            EvaluationReason reason = new EvaluationReason
            {
                Kind = EvaluationReasonKind.PREREQUISITES_FAILED,
                PrerequisiteKeys = new List<string> { "key1", "key2" }
            };
            var json = @"{""kind"":""PREREQUISITES_FAILED"",""prerequisiteKeys"":[""key1"",""key2""]}";
            AssertJsonEqual(json, JsonConvert.SerializeObject(reason));
            Assert.Equal(reason, JsonConvert.DeserializeObject<EvaluationReason>(json));
            Assert.Equal("PREREQUISITES_FAILED(key1,key2)", reason.ToString());
        }

        [Fact]
        public void TestSerializeFallthroughReason()
        {
            EvaluationReason reason = new EvaluationReason
            {
                Kind = EvaluationReasonKind.FALLTHROUGH
            };
            var json = @"{""kind"":""FALLTHROUGH""}";
            AssertJsonEqual(json, JsonConvert.SerializeObject(reason));
            Assert.Equal(reason, JsonConvert.DeserializeObject<EvaluationReason>(json));
            Assert.Equal("FALLTHROUGH", reason.ToString());
        }

        [Fact]
        public void TestSerializeErrorReason()
        {
            EvaluationReason reason = new EvaluationReason
            {
                Kind = EvaluationReasonKind.ERROR,
                ErrorKind = EvaluationErrorKind.EXCEPTION
            };
            var json = @"{""kind"":""ERROR"",""errorKind"":""EXCEPTION""}";
            AssertJsonEqual(json, JsonConvert.SerializeObject(reason));
            Assert.Equal(reason, JsonConvert.DeserializeObject<EvaluationReason>(json));
            Assert.Equal("ERROR(EXCEPTION)", reason.ToString());
        }

        private void AssertJsonEqual(string expectedString, string actualString)
        {
            JToken expected = JsonConvert.DeserializeObject<JToken>(expectedString);
            JToken actual = JsonConvert.DeserializeObject<JToken>(actualString);
            if (!JToken.DeepEquals(expected, actual))
            {
                Assert.True(false, "JSON did not match: expected " + expectedString + ", got " + actualString);
            }
        }
    }
}
