using System;
using System.Collections.Generic;
using System.Linq;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class LdValueTest
    {
        const int someInt = 3;
        const float someFloat = 3.25f;
        const string someString = "hi";
        static readonly JArray someArray = new JArray() { new JValue(3) };
        static readonly JObject someObject = new JObject() { { "1", new JValue("x") } };

        static readonly LdValue aTrueBoolValue = LdValue.Of(true);
        static readonly LdValue anIntValue = LdValue.Of(someInt);
        static readonly LdValue aFloatValue = LdValue.Of(someFloat);
        static readonly LdValue aStringValue = LdValue.Of(someString);
        static readonly LdValue aTrueBoolValueFromJToken = LdValue.FromSafeValue(new JValue(true));
        static readonly LdValue anIntValueFromJToken = LdValue.FromSafeValue(new JValue(someInt));
        static readonly LdValue aFloatValueFromJToken = LdValue.FromSafeValue(new JValue(someFloat));
        static readonly LdValue aStringValueFromJToken = LdValue.FromSafeValue(new JValue(someString));
        static readonly LdValue aNumericLookingStringValue = LdValue.Of("3");
        static readonly LdValue anArrayValue =
            LdValue.FromValues(new int[] { 3 });
        static readonly LdValue anObjectValue =
            LdValue.FromDictionary(MakeDictionary("x"));

        [Fact]
        public void ValuesCreatedFromPrimitivesDoNotHaveJToken()
        {
            Assert.False(aTrueBoolValue.HasWrappedJToken);
            Assert.False(anIntValue.HasWrappedJToken);
            Assert.False(aFloatValue.HasWrappedJToken);
            Assert.False(aStringValue.HasWrappedJToken);

            Assert.True(anIntValueFromJToken.HasWrappedJToken);
            Assert.True(aFloatValueFromJToken.HasWrappedJToken);
            Assert.True(aStringValueFromJToken.HasWrappedJToken);
            Assert.True(anArrayValue.HasWrappedJToken);
            Assert.True(anObjectValue.HasWrappedJToken);

            // Boolean is a special case where we never create a token because we reuse two static ones
            Assert.False(aTrueBoolValueFromJToken.HasWrappedJToken);
        }

        [Fact]
        public void CanGetValueAsBool()
        {
            Assert.Equal(JsonValueType.Bool, aTrueBoolValue.Type);
            Assert.True(aTrueBoolValue.AsBool);
            Assert.True(aTrueBoolValue.Value<bool>());
            Assert.Equal(JsonValueType.Bool, aTrueBoolValueFromJToken.Type);
            Assert.True(aTrueBoolValueFromJToken.AsBool);
            Assert.True(aTrueBoolValueFromJToken.Value<bool>());
        }

        [Fact]
        public void NonBooleanValueAsBoolIsFalse()
        {
            var values = new LdValue[]
            {
                LdValue.Null,
                aStringValue,
                aStringValueFromJToken,
                anIntValue,
                anIntValueFromJToken,
                aFloatValue,
                aFloatValueFromJToken,
                anArrayValue,
                anObjectValue
            };
            foreach (var value in values)
            {
                Assert.NotEqual(JsonValueType.Bool, value.Type);
                Assert.False(value.AsBool);
                Assert.False(value.Value<bool>());
            }
        }

        [Fact]
        public void BoolValuesUseSameInstances()
        {
            Assert.Same(LdValue.Of(true).InnerValue, LdValue.Of(true).InnerValue);
            Assert.Same(LdValue.Of(false).InnerValue, LdValue.Of(false).InnerValue);
            Assert.Same(LdValue.Of(true).InnerValue, LdValue.FromSafeValue(new JValue(true)).InnerValue);
            Assert.Same(LdValue.Of(false).InnerValue, LdValue.FromSafeValue(new JValue(false)).InnerValue);
        }

        [Fact]
        public void CanGetValueAsString()
        {
            Assert.Equal(JsonValueType.String, aStringValue.Type);
            Assert.Equal(someString, aStringValue.AsString);
            Assert.Equal(someString, aStringValue.Value<string>());
            Assert.Equal(JsonValueType.String, aStringValueFromJToken.Type);
            Assert.Equal(someString, aStringValueFromJToken.AsString);
            Assert.Equal(someString, aStringValueFromJToken.Value<string>());
        }

        [Fact]
        public void NonStringValueAsStringIsNull()
        {
            var values = new LdValue[]
            {
                LdValue.Null,
                aTrueBoolValue,
                aTrueBoolValueFromJToken,
                anIntValue,
                anIntValueFromJToken,
                aFloatValue,
                aFloatValueFromJToken,
                anArrayValue,
                anObjectValue
            };
            foreach (var value in values)
            {
                Assert.NotEqual(JsonValueType.String, value.Type);
                Assert.Null(value.AsString);
                Assert.Null(value.Value<string>());
            }
        }

        [Fact]
        public void EmptyStringValuesUseSameInstance()
        {
            Assert.Same(LdValue.Of("").AsString, LdValue.Of("").AsString);
            Assert.Same(LdValue.Of("").InnerValue, LdValue.Of("").InnerValue);
        }

        [Fact]
        public void CanGetValueAsInt()
        {
            Assert.Equal(JsonValueType.Number, anIntValue.Type);
            Assert.Equal(someInt, anIntValue.AsInt);
            Assert.Equal(someInt, anIntValue.Value<int>());
            Assert.Equal(JsonValueType.Number, anIntValueFromJToken.Type);
            Assert.Equal(someInt, anIntValueFromJToken.AsInt);
            Assert.Equal(someInt, anIntValueFromJToken.Value<int>());
        }

        [Fact]
        public void NonNumericValueAsIntIsZero()
        {
            var values = new LdValue[]
            {
                LdValue.Null,
                aTrueBoolValue,
                aTrueBoolValueFromJToken,
                aStringValue,
                aStringValueFromJToken,
                aNumericLookingStringValue,
                anArrayValue,
                anObjectValue
            };
            foreach (var value in values)
            {
                Assert.Equal(0, value.AsInt);
                Assert.Equal(0, value.Value<int>());
            }
        }

        [Fact]
        public void ZeroIntValuesUseSameInstance()
        {
            Assert.Same(LdValue.Of(0).InnerValue, LdValue.Of(0).InnerValue);
        }

        [Fact]
        public void CanGetValueAsFloat()
        {
            Assert.Equal(JsonValueType.Number, aFloatValue.Type);
            Assert.Equal(someFloat, aFloatValue.AsFloat);
            Assert.Equal(someFloat, aFloatValue.Value<float>());
            Assert.Equal(JsonValueType.Number, aFloatValueFromJToken.Type);
            Assert.Equal(someFloat, aFloatValueFromJToken.AsFloat);
            Assert.Equal(someFloat, aFloatValueFromJToken.Value<float>());
        }

        [Fact]
        public void CanGetIntValueAsFloat()
        {
            Assert.Equal((float)someInt, anIntValue.AsFloat);
            Assert.Equal((float)someInt, anIntValue.Value<float>());
            Assert.Equal((float)someInt, anIntValueFromJToken.AsFloat);
            Assert.Equal((float)someInt, anIntValueFromJToken.Value<float>());
        }

        [Fact]
        public void NonNumericValueAsFloatIsZero()
        {
            var values = new LdValue[]
            {
                LdValue.Null,
                aTrueBoolValue,
                aTrueBoolValueFromJToken,
                aStringValue,
                aStringValueFromJToken,
                aNumericLookingStringValue,
                anArrayValue,
                anObjectValue
            };
            foreach (var value in values)
            {
                Assert.Equal(0, value.AsFloat);
                Assert.Equal(0, value.Value<float>());
            }
        }

        [Fact]
        public void CanGetFloatValueAsInt()
        {
            Assert.Equal((int)someFloat, aFloatValue.AsInt);
            Assert.Equal((int)someFloat, aFloatValue.Value<int>());
            Assert.Equal((int)someFloat, aFloatValueFromJToken.AsInt);
            Assert.Equal((int)someFloat, aFloatValueFromJToken.Value<int>());
        }
        
        [Fact]
        public void FloatValueAsIntRoundsTowardZero()
        {
            Assert.Equal(2, LdValue.Of(2.25f).AsInt);
            Assert.Equal(2, LdValue.Of(2.75f).AsInt);
            Assert.Equal(-2, LdValue.Of(-2.25f).AsInt);
            Assert.Equal(-2, LdValue.Of(-2.75f).AsInt);
            Assert.Equal(2, LdValue.Of(2.25f).Value<int>());
            Assert.Equal(2, LdValue.Of(2.75f).Value<int>());
            Assert.Equal(-2, LdValue.Of(-2.25f).Value<int>());
            Assert.Equal(-2, LdValue.Of(-2.75f).Value<int>());
        }

        [Fact]
        public void ZeroFloatValuesUseSameInstance()
        {
            Assert.Same(LdValue.Of(0f).InnerValue, LdValue.Of(0f).InnerValue);
        }

        [Fact]
        public void CanGetValuesAsList()
        {
            Assert.Equal(new bool[] { true, false }, LdValue.FromValues(new bool[] { true, false }).AsList<bool>());
            Assert.Equal(new int[] { 1, 2 }, LdValue.FromValues(new int[] { 1, 2 }).AsList<int>());
            Assert.Equal(new float[] { 1.0f, 2.0f }, LdValue.FromValues(new float[] { 1.0f, 2.0f }).AsList<float>());
            Assert.Equal(new string[] { "a", "b" }, LdValue.FromValues(new string[] { "a", "b" }).AsList<string>());
            Assert.Equal(new LdValue[] { anIntValue, aStringValue },
                LdValue.FromValues(new LdValue[] { anIntValue, aStringValue }).AsList<LdValue>());
            Assert.Equal(LdValue.Null, LdValue.FromValues((IEnumerable<int>)null));
        }

        [Fact]
        public void TypesAreConvertedInList()
        {
            Assert.Equal(new float[] { 1.0f, 2.0f }, LdValue.FromValues(new int[] { 1, 2 }).AsList<float>());
        }

        [Fact]
        public void ListCanGetItemByIndex()
        {
            var v = LdValue.FromValues(new int[] { 1, 2, 3 });
            var list = v.AsList<int>();
            Assert.Equal(2, list[1]);
            Assert.Throws<IndexOutOfRangeException>(() => list[-1]);
            Assert.Throws<IndexOutOfRangeException>(() => list[3]);
        }

        [Fact]
        public void ListCanBeEnumerated()
        {
            var v = LdValue.FromValues(new int[] { 1, 2, 3 });
            var list = v.AsList<int>();
            var listOut = new List<int>();
            Assert.Equal(3, list.Count);
            foreach (var n in list)
            {
                listOut.Add(n);
            }
            Assert.Equal(listOut, list);
        }

        [Fact]
        public void NonArrayValuesReturnEmptyOrList()
        {
            var values = new LdValue[]
            {
                LdValue.Null,
                aTrueBoolValue,
                anIntValue,
                aFloatValue,
                aStringValue,
                anObjectValue
            };
            var emptyArray = new JArray();
            foreach (var value in values)
            {
                var emptyList = value.AsList<bool>();
                Assert.Equal(0, emptyList.Count);
                Assert.Throws<IndexOutOfRangeException>(() => emptyList[0]);
                foreach (var n in emptyList)
                {
                    Assert.True(false, "should not have enumerated an element");
                }
            }
        }

        [Fact]
        public void CanGetValueAsDictionary()
        {
            AssertDictsEqual(MakeDictionary(true, false),
                LdValue.FromDictionary(MakeDictionary(true, false)).AsDictionary<bool>());
            AssertDictsEqual(MakeDictionary(1, 2),
                LdValue.FromDictionary(MakeDictionary(1, 2)).AsDictionary<int>());
            AssertDictsEqual(MakeDictionary(1.0f, 2.0f),
                LdValue.FromDictionary(MakeDictionary(1.0f, 2.0f)).AsDictionary<float>());
            AssertDictsEqual(MakeDictionary(anIntValue, aStringValue),
                LdValue.FromDictionary(MakeDictionary(anIntValue, aStringValue)).AsDictionary<LdValue>());
            Assert.Equal(LdValue.Null, LdValue.FromDictionary((IReadOnlyDictionary<string, string>)null));
        }

        [Fact]
        public void DictionaryCanGetValueByKey()
        {
            var v = LdValue.FromDictionary(MakeDictionary(100, 200, 300));
            var d = v.AsDictionary<int>();
            Assert.True(d.ContainsKey("2"));
            Assert.Equal(200, d["2"]);
            Assert.True(d.TryGetValue("1", out var n));
            Assert.Equal(100, n);
            Assert.False(d.ContainsKey("000"));
            Assert.Throws<KeyNotFoundException>(() => d["000"]);
            Assert.False(d.TryGetValue("000", out n));
        }

        [Fact]
        public void DictionaryCanBeEnumerated()
        {
            var v = LdValue.FromDictionary(MakeDictionary(100, 200, 300));
            var d = v.AsDictionary<int>();
            Assert.Equal(3, d.Count);
            Assert.Equal(new string[] { "1", "2", "3" }, new List<string>(d.Keys).OrderBy(s => s));
            Assert.Equal(new int[] { 100, 200, 300 }, new List<int>(d.Values).OrderBy(n => n));
            Assert.Equal(new KeyValuePair<string, int>[]
            {
                new KeyValuePair<string, int>("1", 100),
                new KeyValuePair<string, int>("2", 200),
                new KeyValuePair<string, int>("3", 300),
            }, d.OrderBy(e => e.Key));
        }

        [Fact]
        public void NonObjectValuesReturnEmptyDictionary()
        {
            var values = new LdValue[]
            {
                LdValue.Null,
                aTrueBoolValue,
                anIntValue,
                aFloatValue,
                aStringValue,
                anArrayValue
            };
            var emptyObject = new JObject();
            foreach (var value in values)
            {
                var emptyDict = value.AsDictionary<bool>();
                Assert.Equal(0, emptyDict.Count);
                Assert.False(emptyDict.ContainsKey("1"));
                Assert.Throws<KeyNotFoundException>(() => emptyDict["1"]);
                Assert.False(emptyDict.TryGetValue("1", out var b));
                foreach (var e in emptyDict)
                {
                    Assert.True(false, "should not have enumerated an element");
                }
                Assert.Equal(new string[0], emptyDict.Keys);
                Assert.Equal(new bool[0], emptyDict.Values);
            }
        }

        [Fact]
        public void SamePrimitivesWithOrWithoutJTokenAreEqual()
        {
            Assert.Equal(aTrueBoolValue, aTrueBoolValueFromJToken);
            Assert.Equal(anIntValue, anIntValueFromJToken);
            Assert.Equal(aFloatValue, aFloatValueFromJToken);
            Assert.Equal(aStringValue, aStringValueFromJToken);
        }

        [Fact]
        public void SamePrimitivesWithOrWithoutJTokenHaveSameHashCode()
        {
            Assert.Equal(aTrueBoolValue.GetHashCode(), aTrueBoolValueFromJToken.GetHashCode());
            Assert.Equal(anIntValue.GetHashCode(), anIntValueFromJToken.GetHashCode());
            Assert.Equal(aFloatValue.GetHashCode(), aFloatValueFromJToken.GetHashCode());
            Assert.Equal(aStringValue.GetHashCode(), aStringValueFromJToken.GetHashCode());
        }

        [Fact]
        public void IntAndFloatJTokensWithSameValueAreEqual()
        {
            Assert.Equal(LdValue.FromSafeValue(new JValue(2)),
                LdValue.FromSafeValue(new JValue(2.0f)));
        }

        [Fact]
        public void ComplexTypeEqualityUsesDeepEqual()
        {
            var o0 = LdValue.FromDictionary(new Dictionary<string, string> { { "a", "b" } });
            var o1 = LdValue.FromDictionary(new Dictionary<string, string> { { "a", "b" } });
            Assert.Equal(o0, o1);
        }

        [Fact]
        public void TestJsonSerialization()
        {
            Assert.Equal("null", JsonConvert.SerializeObject(LdValue.Null));
            Assert.Equal("true", JsonConvert.SerializeObject(aTrueBoolValue));
            Assert.Equal("true", JsonConvert.SerializeObject(aTrueBoolValueFromJToken));
            Assert.Equal("false", JsonConvert.SerializeObject(LdValue.Of(false)));
            Assert.Equal(someInt.ToString(), JsonConvert.SerializeObject(anIntValue));
            Assert.Equal(someInt.ToString(), JsonConvert.SerializeObject(anIntValueFromJToken));
            Assert.Equal(someFloat.ToString(), JsonConvert.SerializeObject(aFloatValue));
            Assert.Equal(someFloat.ToString(), JsonConvert.SerializeObject(aFloatValueFromJToken));
            Assert.Equal("[3]", JsonConvert.SerializeObject(anArrayValue));
            Assert.Equal("{\"1\":\"x\"}", JsonConvert.SerializeObject(anObjectValue));
        }
        
        [Fact]
        public void TestJsonDeserialization()
        {
            var json = "{\"a\":\"b\"}";
            var actual = JsonConvert.DeserializeObject<LdValue>(json);
            var expected = LdValue.FromDictionary(new Dictionary<string, string> { { "a", "b" } });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestJsonDeserializationOfNull()
        {
            var v = JsonConvert.DeserializeObject<LdValue>("null");
            Assert.Null(v.InnerValue);
        }

        [Fact]
        public void TestNullStringConstructorIsEquivalentToNullInstance()
        {
            Assert.Equal(LdValue.Null, LdValue.Of(null));
        }

        private static Dictionary<string, T> MakeDictionary<T>(params T[] values)
        {
            var ret = new Dictionary<string, T>();
            var i = 0;
            foreach (var v in values)
            {
                ret[(++i).ToString()] = v;
            }
            return ret;
        }

        private void AssertDictsEqual<T>(IReadOnlyDictionary<string, T> expected, IReadOnlyDictionary<string, T> actual)
        {
            if (expected.Count != actual.Count || expected.Except(actual).Any())
            {
                Assert.False(true, string.Format("expected: {0}, actual: {0}",
                    string.Join(", ", expected.Select(e => e.Key + "=" + e.Value)),
                    string.Join(", ", actual.Select(e => e.Key + "=" + e.Value))));
            }
        }

    }
}
