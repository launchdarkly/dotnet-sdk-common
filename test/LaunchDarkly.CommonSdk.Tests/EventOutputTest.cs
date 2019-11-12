using System;
using System.Collections.Generic;
using LaunchDarkly.Client;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class EventOutputTest
    {
        [Fact]
        public void AllUserAttributesAreSerialized()
        {
            var f = new EventOutputFormatter(new SimpleConfiguration());
            var user = User.Builder("userkey")
                .Anonymous(true)
                .Avatar("http://avatar")
                .Country("US")
                .Custom("custom1", "value1")
                .Custom("custom2", "value2")
                .Email("test@example.com")
                .FirstName("first")
                .IPAddress("1.2.3.4")
                .LastName("last")
                .Name("me")
                .SecondaryKey("s")
                .Build();
            var userJson = LdValue.Parse(@"{
                ""key"":""userkey"",
                ""anonymous"":true,
                ""avatar"":""http://avatar"",
                ""country"":""US"",
                ""custom"":{""custom1"":""value1"",""custom2"":""value2""},
                ""email"":""test@example.com"",
                ""firstName"":""first"",
                ""ip"":""1.2.3.4"",
                ""lastName"":""last"",
                ""name"":""me"",
                ""secondary"":""s""
                }");
            TestInlineUserSerialization(user, userJson, new SimpleConfiguration());
        }

        [Fact]
        public void UnsetUserAttributesAreNotSerialized()
        {
            var f = new EventOutputFormatter(new SimpleConfiguration());
            var user = User.Builder("userkey")
                .Build();
            var userJson = LdValue.Parse(@"{
                ""key"":""userkey""
                }");
            TestInlineUserSerialization(user, userJson, new SimpleConfiguration());
        }

        [Fact]
        public void AllAttributesPrivateMakesAttributesPrivate()
        {
            var f = new EventOutputFormatter(new SimpleConfiguration());
            var user = User.Builder("userkey")
                .Anonymous(true)
                .Avatar("http://avatar")
                .Country("US")
                .Custom("custom1", "value1")
                .Custom("custom2", "value2")
                .Email("test@example.com")
                .FirstName("first")
                .IPAddress("1.2.3.4")
                .LastName("last")
                .Name("me")
                .SecondaryKey("s")
                .Build();
            var userJson = LdValue.Parse(@"{
                ""key"":""userkey"",
                ""anonymous"":true,
                ""secondary"":""s"",
                ""privateAttrs"":[
                    ""avatar"", ""country"", ""custom1"", ""custom2"", ""email"",
                    ""firstName"", ""ip"", ""lastName"", ""name""
                ]
                }");
            var config = new SimpleConfiguration() { AllAttributesPrivate = true };
            TestInlineUserSerialization(user, userJson, config);
        }
        
        [Fact]
        public void GlobalPrivateAttributeNamesMakeAttributesPrivate()
        {
            TestPrivateAttribute("avatar", true);
            TestPrivateAttribute("country", true);
            TestPrivateAttribute("custom1", true);
            TestPrivateAttribute("custom2", true);
            TestPrivateAttribute("email", true);
            TestPrivateAttribute("firstName", true);
            TestPrivateAttribute("ip", true);
            TestPrivateAttribute("lastName", true);
            TestPrivateAttribute("name", true);
        }
        
        [Fact]
        public void PerUserPrivateAttributesMakeAttributesPrivate()
        {
            TestPrivateAttribute("avatar", false);
            TestPrivateAttribute("country", false);
            TestPrivateAttribute("custom1", false);
            TestPrivateAttribute("custom2", false);
            TestPrivateAttribute("email", false);
            TestPrivateAttribute("firstName", false);
            TestPrivateAttribute("ip", false);
            TestPrivateAttribute("lastName", false);
            TestPrivateAttribute("name", false);
        }

        [Fact]
        public void UserKeyIsSetInsteadOfUserWhenNotInlined()
        {
            var user = User.Builder("userkey")
                .Name("me")
                .Build();
            var userJson = LdValue.Parse(@"{""key"":""userkey"",""name"":""me""}");
            var f = new EventOutputFormatter(new SimpleConfiguration());
            var emptySummary = new EventSummary();

            var featureEvent = EventFactory.Default.NewFeatureRequestEvent(
                new FlagEventPropertiesBuilder("flag").Build(),
                user,
                new EvaluationDetail<LdValue>(LdValue.Null, null, EvaluationReason.OffReason),
                LdValue.Null);
            var outputEvent = LdValue.Parse(f.SerializeOutputEvents(new Event[] { featureEvent }, emptySummary, out var count)).Get(0);
            Assert.Equal(1, count);
            Assert.Equal(LdValue.Null, outputEvent.Get("user"));
            Assert.Equal(user.Key, outputEvent.Get("userKey").AsString);

            var identifyEvent = EventFactory.Default.NewIdentifyEvent(user);
            outputEvent = LdValue.Parse(f.SerializeOutputEvents(new Event[] { featureEvent }, emptySummary, out count)).Get(0);
            Assert.Equal(1, count);
            Assert.Equal(LdValue.Null, outputEvent.Get("user"));
            Assert.Equal(user.Key, outputEvent.Get("userKey").AsString);

            var customEvent = EventFactory.Default.NewCustomEvent("custom", user, LdValue.Null);
            outputEvent = LdValue.Parse(f.SerializeOutputEvents(new Event[] { featureEvent }, emptySummary, out count)).Get(0);
            Assert.Equal(1, count);
            Assert.Equal(LdValue.Null, outputEvent.Get("user"));
            Assert.Equal(user.Key, outputEvent.Get("userKey").AsString);

            // user is always inlined in index event
            var indexEvent = new IndexEvent(0, user);
            outputEvent = LdValue.Parse(f.SerializeOutputEvents(new Event[] { indexEvent }, emptySummary, out count)).Get(0);
            Assert.Equal(1, count);
            Assert.Equal(LdValue.Null, outputEvent.Get("userKey"));
            Assert.Equal(userJson, outputEvent.Get("user"));
        }

        [Fact]
        public void FeatureEventIsSerialized()
        {
            var factory = new EventFactory(() => 100000, false);
            var factoryWithReason = new EventFactory(() => 100000, true);
            var flag = new FlagEventPropertiesBuilder("flag")
                .Version(11)
                .Build();
            var user = User.Builder("userkey").Name("me").Build();

            var feWithVariation = factory.NewFeatureRequestEvent(flag, user,
                new EvaluationDetail<LdValue>(LdValue.Of("flagvalue"), 1, EvaluationReason.OffReason),
                LdValue.Of("defaultvalue"));
            var feJson1 = LdValue.Parse(@"{
                ""kind"":""feature"",
                ""creationDate"":100000,
                ""key"":""flag"",
                ""version"":11,
                ""userKey"":""userkey"",
                ""value"":""flagvalue"",
                ""variation"":1,
                ""default"":""defaultvalue""
                }");
            TestEventSerialization(feWithVariation, feJson1);

            var feWithoutVariationOrDefault = factory.NewFeatureRequestEvent(flag, user,
                new EvaluationDetail<LdValue>(LdValue.Of("flagvalue"), null, EvaluationReason.OffReason),
                LdValue.Null);
            var feJson2 = LdValue.Parse(@"{
                ""kind"":""feature"",
                ""creationDate"":100000,
                ""key"":""flag"",
                ""version"":11,
                ""userKey"":""userkey"",
                ""value"":""flagvalue""
                }");
            TestEventSerialization(feWithoutVariationOrDefault, feJson2);

            var feWithReason = factoryWithReason.NewFeatureRequestEvent(flag, user,
                new EvaluationDetail<LdValue>(LdValue.Of("flagvalue"), 1, EvaluationReason.RuleMatchReason(1, "id")),
                LdValue.Of("defaultvalue"));
            var feJson3 = LdValue.Parse(@"{
                ""kind"":""feature"",
                ""creationDate"":100000,
                ""key"":""flag"",
                ""version"":11,
                ""userKey"":""userkey"",
                ""value"":""flagvalue"",
                ""variation"":1,
                ""default"":""defaultvalue"",
                ""reason"":{""kind"":""RULE_MATCH"",""ruleIndex"":1,""ruleId"":""id""}
                }");
            TestEventSerialization(feWithReason, feJson3);

            var feUnknownFlag = factoryWithReason.NewUnknownFeatureRequestEvent("flag", user,
                LdValue.Of("defaultvalue"), EvaluationErrorKind.FLAG_NOT_FOUND);
            var feJson4 = LdValue.Parse(@"{
                ""kind"":""feature"",
                ""creationDate"":100000,
                ""key"":""flag"",
                ""userKey"":""userkey"",
                ""value"":""defaultvalue"",
                ""default"":""defaultvalue"",
                ""reason"":{""kind"":""FLAG_NOT_FOUND""}
                }");
            TestEventSerialization(feWithReason, feJson3);

            var debugEvent = factory.NewDebugEvent(feWithVariation);
            var feJson5 = LdValue.Parse(@"{
                ""kind"":""debug"",
                ""creationDate"":100000,
                ""key"":""flag"",
                ""version"":11,
                ""user"":{""key"":""userkey"",""name"":""me""},
                ""value"":""flagvalue"",
                ""variation"":1,
                ""default"":""defaultvalue""
                }");
            TestEventSerialization(debugEvent, feJson5);
        }

        [Fact]
        public void IdentifyEventIsSerialized()
        {
            var factory = new EventFactory(() => 100000, false);
            var user = User.Builder("userkey").Name("me").Build();

            var ie = factory.NewIdentifyEvent(user);
            var ieJson = LdValue.Parse(@"{
                ""kind"":""identify"",
                ""creationDate"":100000,
                ""key"":""userkey"",
                ""user"":{""key"":""userkey"",""name"":""me""}
                }");
            TestEventSerialization(ie, ieJson);
        }

        [Fact]
        public void CustomEventIsSerialized()
        {
            var factory = new EventFactory(() => 100000, false);
            var user = User.Builder("userkey").Name("me").Build();

            var ceWithoutData = factory.NewCustomEvent("custom", user, LdValue.Null);
            var ceJson1 = LdValue.Parse(@"{
                ""kind"":""custom"",
                ""creationDate"":100000,
                ""key"":""custom"",
                ""userKey"":""userkey""
                }");
            TestEventSerialization(ceWithoutData, ceJson1);

            var ceWithData = factory.NewCustomEvent("custom", user, LdValue.Of("thing"));
            var ceJson2 = LdValue.Parse(@"{
                ""kind"":""custom"",
                ""creationDate"":100000,
                ""key"":""custom"",
                ""userKey"":""userkey"",
                ""data"":""thing""
                }");
            TestEventSerialization(ceWithData, ceJson2);

            var ceWithMetric = factory.NewCustomEvent("custom", user, LdValue.Null, 2.5);
            var ceJson3 = LdValue.Parse(@"{
                ""kind"":""custom"",
                ""creationDate"":100000,
                ""key"":""custom"",
                ""userKey"":""userkey"",
                ""metricValue"":2.5
                }");
            TestEventSerialization(ceWithMetric, ceJson3);

            var ceWithDataAndMetric = factory.NewCustomEvent("custom", user, LdValue.Of("thing"), 2.5);
            var ceJson4 = LdValue.Parse(@"{
                ""kind"":""custom"",
                ""creationDate"":100000,
                ""key"":""custom"",
                ""userKey"":""userkey"",
                ""data"":""thing"",
                ""metricValue"":2.5
                }");
            TestEventSerialization(ceWithDataAndMetric, ceJson4);
        }

        [Fact]
        public void SummaryEventIsSerialized()
        {
            var summary = new EventSummary();
            summary.NoteTimestamp(1001);

            summary.IncrementCounter("first", 1, 11, LdValue.Of("value1a"), LdValue.Of("default1"));

            summary.IncrementCounter("second", 1, 21, LdValue.Of("value2a"), LdValue.Of("default2"));

            summary.IncrementCounter("first", 1, 11, LdValue.Of("value1a"), LdValue.Of("default1"));
            summary.IncrementCounter("first", 1, 12, LdValue.Of("value1a"), LdValue.Of("default1"));

            summary.IncrementCounter("second", 2, 21, LdValue.Of("value2b"), LdValue.Of("default2"));
            summary.IncrementCounter("second", null, 21, LdValue.Of("default2"), LdValue.Of("default2")); // flag exists (has version), but eval failed (no variation)

            summary.IncrementCounter("third", null, null, LdValue.Of("default3"), LdValue.Of("default3")); // flag doesn't exist (no version)

            summary.NoteTimestamp(1000);
            summary.NoteTimestamp(1002);

            var f = new EventOutputFormatter(new SimpleConfiguration());
            var outputEvent = LdValue.Parse(f.SerializeOutputEvents(new Event[0], summary, out var count)).Get(0);
            Assert.Equal(1, count);

            Assert.Equal("summary", outputEvent.Get("kind").AsString);
            Assert.Equal(1000, outputEvent.Get("startDate").AsInt);
            Assert.Equal(1002, outputEvent.Get("endDate").AsInt);

            var featuresJson = outputEvent.Get("features");
            Assert.Equal(3, featuresJson.Count);

            var firstJson = featuresJson.Get("first");
            Assert.Equal("default1", firstJson.Get("default").AsString);
            TestUtil.AssertContainsInAnyOrder(firstJson.Get("counters").AsList(LdValue.Convert.Json),
                LdValue.Parse(@"{""value"":""value1a"",""variation"":1,""version"":11,""count"":2}"),
                LdValue.Parse(@"{""value"":""value1a"",""variation"":1,""version"":12,""count"":1}"));

            var secondJson = featuresJson.Get("second");
            Assert.Equal("default2", secondJson.Get("default").AsString);
            TestUtil.AssertContainsInAnyOrder(secondJson.Get("counters").AsList(LdValue.Convert.Json),
                LdValue.Parse(@"{""value"":""value2a"",""variation"":1,""version"":21,""count"":1}"),
                LdValue.Parse(@"{""value"":""value2b"",""variation"":2,""version"":21,""count"":1}"),
                LdValue.Parse(@"{""value"":""default2"",""version"":21,""count"":1}"));

            var thirdJson = featuresJson.Get("third");
            Assert.Equal("default3", thirdJson.Get("default").AsString);
            TestUtil.AssertContainsInAnyOrder(thirdJson.Get("counters").AsList(LdValue.Convert.Json),
                LdValue.Parse(@"{""unknown"":true,""value"":""default3"",""count"":1}"));
        }

        private void TestInlineUserSerialization(User user, LdValue expectedJsonValue, SimpleConfiguration config)
        {
            config.InlineUsersInEvents = true;
            var f = new EventOutputFormatter(config);
            var emptySummary = new EventSummary();

            var featureEvent = EventFactory.Default.NewFeatureRequestEvent(
                new FlagEventPropertiesBuilder("flag").Build(),
                user,
                new EvaluationDetail<LdValue>(LdValue.Null, null, EvaluationReason.OffReason),
                LdValue.Null);
            var outputEvent = LdValue.Parse(f.SerializeOutputEvents(new Event[] { featureEvent }, emptySummary, out var count)).Get(0);
            Assert.Equal(1, count);
            Assert.Equal(LdValue.Null, outputEvent.Get("userKey"));
            Assert.Equal(expectedJsonValue, outputEvent.Get("user"));

            var identifyEvent = EventFactory.Default.NewIdentifyEvent(user);
            outputEvent = LdValue.Parse(f.SerializeOutputEvents(new Event[] { featureEvent }, emptySummary, out count)).Get(0);
            Assert.Equal(1, count);
            Assert.Equal(LdValue.Null, outputEvent.Get("userKey"));
            Assert.Equal(expectedJsonValue, outputEvent.Get("user"));

            var customEvent = EventFactory.Default.NewCustomEvent("custom", user, LdValue.Null);
            outputEvent = LdValue.Parse(f.SerializeOutputEvents(new Event[] { featureEvent }, emptySummary, out count)).Get(0);
            Assert.Equal(1, count);
            Assert.Equal(LdValue.Null, outputEvent.Get("userKey"));
            Assert.Equal(expectedJsonValue, outputEvent.Get("user"));

            var indexEvent = new IndexEvent(0, user);
            outputEvent = LdValue.Parse(f.SerializeOutputEvents(new Event[] { indexEvent }, emptySummary, out count)).Get(0);
            Assert.Equal(1, count);
            Assert.Equal(LdValue.Null, outputEvent.Get("userKey"));
            Assert.Equal(expectedJsonValue, outputEvent.Get("user"));
        }

        private void TestPrivateAttribute(string privateAttrName, bool globallyPrivate)
        {
            var builder = User.Builder("userkey")
                .Anonymous(true)
                .SecondaryKey("s");
            var topJsonBuilder = LdValue.BuildObject()
                .Add("key", "userkey")
                .Add("anonymous", true)
                .Add("secondary", "s");
            var customJsonBuilder = LdValue.BuildObject();
            Action<string, Func<string, IUserBuilderCanMakeAttributePrivate>, string, LdValue.ObjectBuilder> setAttr =
                (attrName, setter, value, jsonBuilder) =>
                {
                    if (attrName == privateAttrName)
                    {
                        if (globallyPrivate)
                        {
                            setter(value);
                        }
                        else
                        {
                            setter(value).AsPrivateAttribute();
                        }
                    }
                    else
                    {
                        setter(value);
                        jsonBuilder.Add(attrName, value);
                    }
                };
            setAttr("avatar", builder.Avatar, "http://avatar", topJsonBuilder);
            setAttr("country", builder.Country, "US", topJsonBuilder);
            setAttr("custom1", v => builder.Custom("custom1", v), "value1", customJsonBuilder);
            setAttr("custom2", v => builder.Custom("custom2", v), "value2", customJsonBuilder);
            setAttr("email", builder.Email, "test@example.com", topJsonBuilder);
            setAttr("firstName", builder.FirstName, "first", topJsonBuilder);
            setAttr("ip", builder.IPAddress, "1.2.3.4", topJsonBuilder);
            setAttr("lastName", builder.LastName, "last", topJsonBuilder);
            setAttr("name", builder.Name, "me", topJsonBuilder);

            topJsonBuilder.Add("custom", customJsonBuilder.Build());
            topJsonBuilder.Add("privateAttrs", LdValue.ArrayOf(LdValue.Of(privateAttrName)));
            var userJson = topJsonBuilder.Build();
            var config = new SimpleConfiguration();
            if (globallyPrivate)
            {
                config.PrivateAttributeNames = new HashSet<string> { privateAttrName };
            };

            TestInlineUserSerialization(builder.Build(), userJson, config);
        }

        private void TestEventSerialization(Event e, LdValue expectedJsonValue)
        {
            var f = new EventOutputFormatter(new SimpleConfiguration());
            var emptySummary = new EventSummary();

            var outputEvent = LdValue.Parse(f.SerializeOutputEvents(new Event[] { e }, emptySummary, out var count)).Get(0);
            Assert.Equal(1, count);
            Assert.Equal(expectedJsonValue, outputEvent);
        }
    }
}
