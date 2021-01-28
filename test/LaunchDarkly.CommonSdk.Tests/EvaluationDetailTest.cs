using System;
using System.Collections.Generic;
using LaunchDarkly.Sdk.Json;
using Xunit;

namespace LaunchDarkly.Sdk
{
    public class EvaluationDetailTest
    {
        [Fact]
        public void TestIsDefaultValueTrue()
        {
            var detail = new EvaluationDetail<string>("default", null, EvaluationReason.OffReason);
            Assert.True(detail.IsDefaultValue);
        }

        [Fact]
        public void TestIsDefaultValueFalse()
        {
            var detail = new EvaluationDetail<string>("default", 0, EvaluationReason.OffReason);
            Assert.False(detail.IsDefaultValue);
        }

        [Theory]
        [MemberData(nameof(ReasonTestData))]
        public void TestReasonSerializationDeserialization(EvaluationReason reason,
            string jsonString, string expectedShortString)
        {
            AssertJsonEqual(jsonString, LdJsonSerialization.SerializeObject(reason));
            Assert.Equal(reason, LdJsonSerialization.DeserializeObject<EvaluationReason>(jsonString));
            Assert.Equal(expectedShortString, reason.ToString());
        }

        public static IEnumerable<object[]> ReasonTestData => new List<object[]>
        {
            new object[] { EvaluationReason.OffReason, @"{""kind"":""OFF""}", "OFF" },
            new object[] { EvaluationReason.FallthroughReason, @"{""kind"":""FALLTHROUGH""}", "FALLTHROUGH" },
            new object[] { EvaluationReason.TargetMatchReason, @"{""kind"":""TARGET_MATCH""}", "TARGET_MATCH" },
            new object[] { EvaluationReason.RuleMatchReason(1, "id"),
                @"{""kind"":""RULE_MATCH"",""ruleIndex"":1,""ruleId"":""id""}",
                "RULE_MATCH(1,id)"
            },
            new object[] { EvaluationReason.PrerequisiteFailedReason("key"),
                @"{""kind"":""PREREQUISITE_FAILED"",""prerequisiteKey"":""key""}",
                "PREREQUISITE_FAILED(key)"
            },
            new object[] { EvaluationReason.ErrorReason(EvaluationErrorKind.Exception),
                @"{""kind"":""ERROR"",""errorKind"":""EXCEPTION""}",
                "ERROR(EXCEPTION)"
            }
        };
                
        [Fact]
        public void TestEqualityAndHashCode()
        {
            // For parameterless (singleton) reasons, object.Equals and object.HashCode() already do what
            // we want. Test our implementations for the parameterized reasons.
            VerifyEqualityAndHashCode(() => EvaluationReason.RuleMatchReason(0, "rule1"),
                () => EvaluationReason.RuleMatchReason(1, "rule2"));
            VerifyEqualityAndHashCode(() => EvaluationReason.PrerequisiteFailedReason("a"),
                () => EvaluationReason.PrerequisiteFailedReason("b"));
            VerifyEqualityAndHashCode(() => EvaluationReason.ErrorReason(EvaluationErrorKind.FlagNotFound),
                () => EvaluationReason.ErrorReason(EvaluationErrorKind.Exception));
        }

        private void VerifyEqualityAndHashCode(Func<EvaluationReason> createA, Func<EvaluationReason> createB)
        {
            Assert.Equal(createA(), createA());
            Assert.NotEqual(createA(), createB());
            Assert.Equal(createA().GetHashCode(), createA().GetHashCode());
            Assert.NotEqual(createA().GetHashCode(), createB().GetHashCode());
        }

        private void AssertJsonEqual(string expectedString, string actualString)
        {
            Assert.Equal(LdValue.Parse(expectedString), LdValue.Parse(actualString));
        }
    }
}
