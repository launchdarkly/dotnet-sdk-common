﻿using System;
using System.Collections.Generic;
using System.Linq;
using LaunchDarkly.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class ImmutableJsonValueTest
    {
        const int someInt = 3;
        const float someFloat = 3.25f;
        const string someString = "hi";
        static readonly JArray someArray = new JArray() { new JValue(3) };
        static readonly JObject someObject = new JObject() { { "1", new JValue("x") } };

        static readonly ImmutableJsonValue aTrueBoolValue = ImmutableJsonValue.Of(true);
        static readonly ImmutableJsonValue anIntValue = ImmutableJsonValue.Of(someInt);
        static readonly ImmutableJsonValue aFloatValue = ImmutableJsonValue.Of(someFloat);
        static readonly ImmutableJsonValue aStringValue = ImmutableJsonValue.Of(someString);
        static readonly ImmutableJsonValue aNumericLookingStringValue = ImmutableJsonValue.Of("3");
        static readonly ImmutableJsonValue anArrayValue =
            ImmutableJsonValue.FromValues(new int[] { 3 });
        static readonly ImmutableJsonValue anObjectValue =
            ImmutableJsonValue.FromDictionary(MakeDictionary("x"));

        [Fact]
        public void CanGetValueAsBool()
        {
            Assert.True(aTrueBoolValue.AsBool);
            Assert.True(aTrueBoolValue.Value<bool>());
        }

        [Fact]
        public void NonBooleanValueAsBoolIsFalse()
        {
            var values = new ImmutableJsonValue[]
            {
                ImmutableJsonValue.Null,
                aStringValue,
                anIntValue,
                aFloatValue,
                anArrayValue,
                anObjectValue
            };
            foreach (var value in values)
            {
                Assert.False(value.AsBool);
                Assert.False(value.Value<bool>());
            }
        }

        [Fact]
        public void BoolValuesUseSameInstances()
        {
            Assert.Same(ImmutableJsonValue.Of(true).InnerValue, ImmutableJsonValue.Of(true).InnerValue);
            Assert.Same(ImmutableJsonValue.Of(false).InnerValue, ImmutableJsonValue.Of(false).InnerValue);
        }

        [Fact]
        public void CanGetValueAsString()
        {
            Assert.Equal(someString, aStringValue.AsString);
            Assert.Equal(someString, aStringValue.Value<string>());
        }

        [Fact]
        public void NullValueAsStringIsNull()
        {
            Assert.Null(ImmutableJsonValue.Null.AsString);
            Assert.Null(ImmutableJsonValue.Null.Value<string>());
        }

        [Fact]
        public void NonStringValuesAreStringified()
        {
            Assert.Equal(true.ToString(), aTrueBoolValue.AsString);
            Assert.Equal(true.ToString(), aTrueBoolValue.Value<string>());
            Assert.Equal(someInt.ToString(), anIntValue.AsString);
            Assert.Equal(someInt.ToString(), anIntValue.Value<string>());
            Assert.Equal(someFloat.ToString(), aFloatValue.AsString);
            Assert.Equal(someFloat.ToString(), aFloatValue.Value<string>());
            Assert.Equal("[3]", anArrayValue.AsString);
            Assert.Equal("[3]", anArrayValue.Value<string>());
            Assert.Equal("{\"1\":\"x\"}", anObjectValue.AsString);
            Assert.Equal("{\"1\":\"x\"}", anObjectValue.Value<string>());
        }

        [Fact]
        public void EmptyStringValuesUseSameInstance()
        {
            Assert.Same(ImmutableJsonValue.Of("").InnerValue, ImmutableJsonValue.Of("").InnerValue);
        }

        [Fact]
        public void CanGetValueAsInt()
        {
            Assert.Equal(someInt, anIntValue.AsInt);
            Assert.Equal(someInt, anIntValue.Value<int>());
        }

        [Fact]
        public void NonNumericValueAsIntIsZero()
        {
            var values = new ImmutableJsonValue[]
            {
                ImmutableJsonValue.Null,
                aTrueBoolValue,
                aStringValue,
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
            Assert.Same(ImmutableJsonValue.Of(0).InnerValue, ImmutableJsonValue.Of(0).InnerValue);
        }

        [Fact]
        public void CanGetValueAsFloat()
        {
            Assert.Equal(someFloat, aFloatValue.AsFloat);
            Assert.Equal(someFloat, aFloatValue.Value<float>());
        }

        [Fact]
        public void CanGetIntValueAsFloat()
        {
            Assert.Equal((float)someInt, anIntValue.AsFloat);
            Assert.Equal((float)someInt, anIntValue.Value<float>());
        }

        [Fact]
        public void NonNumericValueAsFloatIsZero()
        {
            var values = new ImmutableJsonValue[]
            {
                ImmutableJsonValue.Null,
                aTrueBoolValue,
                aStringValue,
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
        }

        [Fact]
        public void IntFromFloatRoundsToNearest()
        {
            Assert.Equal(2, ImmutableJsonValue.Of(2.25f).AsInt);
            Assert.Equal(2, ImmutableJsonValue.Of(2.25f).Value<int>());
            Assert.Equal(2, ImmutableJsonValue.Of(2.75f).AsInt);
            Assert.Equal(2, ImmutableJsonValue.Of(2.75f).Value<int>());
            Assert.Equal(-2, ImmutableJsonValue.Of(-2.25f).AsInt);
            Assert.Equal(-2, ImmutableJsonValue.Of(-2.25f).Value<int>());
            Assert.Equal(-2, ImmutableJsonValue.Of(-2.75f).AsInt);
            Assert.Equal(-2, ImmutableJsonValue.Of(-2.75f).Value<int>());
        }

        [Fact]
        public void FloatValueAsIntRoundsTowardZero()
        {
            Assert.Equal(2, ImmutableJsonValue.Of(2.25f).AsInt);
            Assert.Equal(2, ImmutableJsonValue.Of(2.75f).AsInt);
            Assert.Equal(-2, ImmutableJsonValue.Of(-2.25f).AsInt);
            Assert.Equal(-2, ImmutableJsonValue.Of(-2.75f).AsInt);
            Assert.Equal(2, ImmutableJsonValue.Of(2.25f).Value<int>());
            Assert.Equal(2, ImmutableJsonValue.Of(2.75f).Value<int>());
            Assert.Equal(-2, ImmutableJsonValue.Of(-2.25f).Value<int>());
            Assert.Equal(-2, ImmutableJsonValue.Of(-2.75f).Value<int>());
        }

        [Fact]
        public void ZeroFloatValuesUseSameInstance()
        {
            Assert.Same(ImmutableJsonValue.Of(0f).InnerValue, ImmutableJsonValue.Of(0f).InnerValue);
        }
        
        [Fact]
        public void CanGetValuesAsList()
        {
            Assert.Equal(new bool[] { true, false }, ImmutableJsonValue.FromValues(new bool[] { true, false }).AsList<bool>());
            Assert.Equal(new int[] { 1, 2 }, ImmutableJsonValue.FromValues(new int[] { 1, 2 }).AsList<int>());
            Assert.Equal(new float[] { 1.0f, 2.0f }, ImmutableJsonValue.FromValues(new float[] { 1.0f, 2.0f }).AsList<float>());
            Assert.Equal(new string[] { "a", "b" }, ImmutableJsonValue.FromValues(new string[] { "a", "b" }).AsList<string>());
            Assert.Equal(new ImmutableJsonValue[] { anIntValue, aStringValue },
                ImmutableJsonValue.FromValues(new ImmutableJsonValue[] { anIntValue, aStringValue }).AsList<ImmutableJsonValue>());
        }

        [Fact]
        public void TypesAreConvertedInList()
        {
            Assert.Equal(new float[] { 1.0f, 2.0f }, ImmutableJsonValue.FromValues(new int[] { 1, 2 }).AsList<float>());
        }

        [Fact]
        public void ListCanGetItemByIndex()
        {
            var v = ImmutableJsonValue.FromValues(new int[] { 1, 2, 3 });
            var list = v.AsList<int>();
            Assert.Equal(2, list[1]);
            Assert.Throws<IndexOutOfRangeException>(() => list[-1]);
            Assert.Throws<IndexOutOfRangeException>(() => list[3]);
        }

        [Fact]
        public void ListCanBeEnumerated()
        {
            var v = ImmutableJsonValue.FromValues(new int[] { 1, 2, 3 });
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
            var values = new ImmutableJsonValue[]
            {
                ImmutableJsonValue.Null,
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
                ImmutableJsonValue.FromDictionary(MakeDictionary(true, false)).AsDictionary<bool>());
            AssertDictsEqual(MakeDictionary(1, 2),
                ImmutableJsonValue.FromDictionary(MakeDictionary(1, 2)).AsDictionary<int>());
            AssertDictsEqual(MakeDictionary(1.0f, 2.0f),
                ImmutableJsonValue.FromDictionary(MakeDictionary(1.0f, 2.0f)).AsDictionary<float>());
            AssertDictsEqual(MakeDictionary(anIntValue, aStringValue),
                ImmutableJsonValue.FromDictionary(MakeDictionary(anIntValue, aStringValue)).AsDictionary<ImmutableJsonValue>());
        }

        [Fact]
        public void DictionaryCanGetValueByKey()
        {
            var v = ImmutableJsonValue.FromDictionary(MakeDictionary(100, 200, 300));
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
            var v = ImmutableJsonValue.FromDictionary(MakeDictionary(100, 200, 300));
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
            var values = new ImmutableJsonValue[]
            {
                ImmutableJsonValue.Null,
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
        public void EqualityUsesDeepEqual()
        {
            var o0 = ImmutableJsonValue.FromDictionary(new Dictionary<string, string> { { "a", "b" } });
            var o1 = ImmutableJsonValue.FromDictionary(new Dictionary<string, string> { { "a", "b" } });
            Assert.Equal(o0, o1);
        }

        [Fact]
        public void TestJsonSerialization()
        {
            var v = ImmutableJsonValue.FromDictionary(new Dictionary<string, string> { { "a", "b" } });
            var json = JsonConvert.SerializeObject(v);
            Assert.Equal("{\"a\":\"b\"}", json);
        }

        [Fact]
        public void TestJsonSerializationOfNull()
        {
            Assert.Equal("null", JsonConvert.SerializeObject(ImmutableJsonValue.Null));
        }

        [Fact]
        public void TestJsonDeserialization()
        {
            var json = "{\"a\":\"b\"}";
            var v = JsonConvert.DeserializeObject<ImmutableJsonValue>(json);
            var o = new JObject() { { "a", new JValue("b") } };
            TestUtil.AssertJsonEquals(o, v.InnerValue);
        }

        [Fact]
        public void TestJsonDeserializationOfNull()
        {
            var v = JsonConvert.DeserializeObject<ImmutableJsonValue>("null");
            Assert.Null(v.InnerValue);
        }

        [Fact]
        public void TestNullConstructorIsEquivalentToNullInstance()
        {
            Assert.Equal(ImmutableJsonValue.Null, ImmutableJsonValue.Of(null));
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
