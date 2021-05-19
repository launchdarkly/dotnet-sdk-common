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

        public struct ReasonTestCase
        {
            public EvaluationReason Reason { get; set; }
            public string JsonString { get; set; }
            public string ExpectedShortString { get; set; }
        }

        [Fact]
        public void TestReasonSerializationDeserialization()
        {
            foreach (var test in new ReasonTestCase[]
            {
                new ReasonTestCase { Reason = EvaluationReason.OffReason,
                    JsonString = @"{""kind"":""OFF""}", ExpectedShortString = "OFF" },
                new ReasonTestCase { Reason = EvaluationReason.FallthroughReason,
                    JsonString = @"{""kind"":""FALLTHROUGH""}", ExpectedShortString = "FALLTHROUGH" },
                new ReasonTestCase {
                    Reason = EvaluationReason.FallthroughReason.WithInExperiment(true),
                    JsonString = @"{""kind"":""FALLTHROUGH"",""inExperiment"":true}",
                    ExpectedShortString = "FALLTHROUGH" },
                new ReasonTestCase { Reason = EvaluationReason.TargetMatchReason,
                    JsonString = @"{""kind"":""TARGET_MATCH""}", ExpectedShortString = "TARGET_MATCH" },
                new ReasonTestCase { Reason = EvaluationReason.RuleMatchReason(1, "id"),
                    JsonString = @"{""kind"":""RULE_MATCH"",""ruleIndex"":1,""ruleId"":""id""}",
                    ExpectedShortString = "RULE_MATCH(1,id)"
                },
                new ReasonTestCase { Reason = EvaluationReason.RuleMatchReason(1, "id").WithInExperiment(true),
                    JsonString = @"{""kind"":""RULE_MATCH"",""ruleIndex"":1,""ruleId"":""id"",""inExperiment"":true}",
                    ExpectedShortString = "RULE_MATCH(1,id)"
                },
                new ReasonTestCase { Reason = EvaluationReason.PrerequisiteFailedReason("key"),
                    JsonString = @"{""kind"":""PREREQUISITE_FAILED"",""prerequisiteKey"":""key""}",
                    ExpectedShortString = "PREREQUISITE_FAILED(key)"
                },
                new ReasonTestCase { Reason = EvaluationReason.ErrorReason(EvaluationErrorKind.Exception),
                    JsonString = @"{""kind"":""ERROR"",""errorKind"":""EXCEPTION""}",
                    ExpectedShortString = "ERROR(EXCEPTION)"
                }
            })
            {
                AssertJsonEqual(test.JsonString, LdJsonSerialization.SerializeObject(test.Reason));
                Assert.Equal(test.Reason, LdJsonSerialization.DeserializeObject<EvaluationReason>(test.JsonString));
                Assert.Equal(test.ExpectedShortString, test.Reason.ToString());
            }
        }

        [Fact]
        public void TestEqualityAndHashCode()
        {
            // For parameterless (singleton) reasons, object.Equals and object.HashCode() already do what
            // we want. Test our implementations for the parameterized reasons. The two parameters for
            // each call should construct values that are *not* equal to each other.
            VerifyEqualityAndHashCode(() => EvaluationReason.FallthroughReason,
                () => EvaluationReason.FallthroughReason.WithInExperiment(true));
            VerifyEqualityAndHashCode(() => EvaluationReason.RuleMatchReason(0, "rule1"),
                () => EvaluationReason.RuleMatchReason(1, "rule2"));
            VerifyEqualityAndHashCode(() => EvaluationReason.RuleMatchReason(0, "rule1"),
                () => EvaluationReason.RuleMatchReason(0, "rule1").WithInExperiment(true));
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
