using System.Collections;
using System.Collections.Generic;
using Xunit;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Common.Tests
{
    public class EvaluationDetailTest
    {
        [Fact]
        public void TestIsDefaultValueTrue()
        {
            var detail = new EvaluationDetail<string>("default", null, EvaluationReason.Off.Instance);
            Assert.True(detail.IsDefaultValue);
        }

        [Fact]
        public void TestIsDefaultValueFalse()
        {
            var detail = new EvaluationDetail<string>("default", 0, EvaluationReason.Off.Instance);
            Assert.False(detail.IsDefaultValue);
        }

        [Theory]
        [MemberData(nameof(ReasonTestData))]
        public void TestReasonSerializationDeserialization(EvaluationReason reason,
            string jsonString, string expectedShortString)
        {
            AssertJsonEqual(jsonString, JsonConvert.SerializeObject(reason));
            Assert.Equal(reason, JsonConvert.DeserializeObject<EvaluationReason>(jsonString));
            Assert.Equal(expectedShortString, reason.ToString());
        }

        public static IEnumerable ReasonTestData => new List<object[]>
        {
            new object[] { EvaluationReason.Off.Instance, @"{""kind"":""OFF""}", "OFF" },
            new object[] { EvaluationReason.Fallthrough.Instance, @"{""kind"":""FALLTHROUGH""}", "FALLTHROUGH" },
            new object[] { EvaluationReason.TargetMatch.Instance, @"{""kind"":""TARGET_MATCH""}", "TARGET_MATCH" },
            new object[] { new EvaluationReason.RuleMatch(1, "id"),
                @"{""kind"":""RULE_MATCH"",""ruleIndex"":1,""ruleId"":""id""}",
                "RULE_MATCH(1,id)"
            },
            new object[] { new EvaluationReason.PrerequisiteFailed("key"),
                @"{""kind"":""PREREQUISITE_FAILED"",""prerequisiteKey"":""key""}",
                "PREREQUISITE_FAILED(key)"
            },
            new object[] { new EvaluationReason.Error(EvaluationErrorKind.EXCEPTION),
                @"{""kind"":""ERROR"",""errorKind"":""EXCEPTION""}",
                "ERROR(EXCEPTION)"
            }
        };
        
        [Fact]
        public void TestDeserializeNullReason()
        {
            var reason = JsonConvert.DeserializeObject<EvaluationReason>("null");
            Assert.Null(reason);
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
