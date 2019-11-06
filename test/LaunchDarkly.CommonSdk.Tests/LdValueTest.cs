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
        const long someLong = 3;
        const float someFloat = 3.25f;
        const double someDouble = 3.25d;
        const string someString = "hi";
        static readonly JArray someArray = new JArray() { new JValue(3) };
        static readonly JObject someObject = new JObject() { { "1", new JValue("x") } };

        static readonly LdValue aTrueBoolValue = LdValue.Of(true);
        static readonly LdValue anIntValue = LdValue.Of(someInt);
        static readonly LdValue aLongValue = LdValue.Of(someLong);
        static readonly LdValue aFloatValue = LdValue.Of(someFloat);
        static readonly LdValue aDoubleValue = LdValue.Of(someDouble);
        static readonly LdValue aStringValue = LdValue.Of(someString);
        static readonly LdValue aNumericLookingStringValue = LdValue.Of("3");
        static readonly LdValue anArrayValue = LdValue.Convert.Int.ArrayOf(3);
        static readonly LdValue anObjectValue =
            LdValue.Convert.String.ObjectFrom(MakeDictionary("x"));
#pragma warning disable 0618
        static readonly LdValue aTrueBoolValueFromJToken = LdValue.FromJToken(new JValue(true));
        static readonly LdValue anIntValueFromJToken = LdValue.FromJToken(new JValue(someInt));
        static readonly LdValue aLongValueFromJToken = LdValue.FromJToken(new JValue(someLong));
        static readonly LdValue aFloatValueFromJToken = LdValue.FromJToken(new JValue(someFloat));
        static readonly LdValue aDoubleValueFromJToken = LdValue.FromJToken(new JValue(someDouble));
        static readonly LdValue aStringValueFromJToken = LdValue.FromJToken(new JValue(someString));
        static readonly LdValue anArrayValueFromJToken = LdValue.FromJToken(
            new JArray() { new JValue(3) });
        static readonly LdValue anObjectValueFromJToken = LdValue.FromJToken(
            new JObject() { { "1", new JValue("x") } });
#pragma warning restore 0618

        [Fact]
        public void ValuesCreatedFromPrimitivesDoNotHaveJToken()
        {
            Assert.False(aTrueBoolValue.HasWrappedJToken);
            Assert.False(anIntValue.HasWrappedJToken);
            Assert.False(aLongValue.HasWrappedJToken);
            Assert.False(aFloatValue.HasWrappedJToken);
            Assert.False(aDoubleValue.HasWrappedJToken);
            Assert.False(aStringValue.HasWrappedJToken);
            Assert.False(anArrayValue.HasWrappedJToken);
            Assert.False(anObjectValue.HasWrappedJToken);

            Assert.True(anIntValueFromJToken.HasWrappedJToken);
            Assert.True(aLongValueFromJToken.HasWrappedJToken);
            Assert.True(aFloatValueFromJToken.HasWrappedJToken);
            Assert.True(aDoubleValueFromJToken.HasWrappedJToken);
            Assert.True(aStringValueFromJToken.HasWrappedJToken);
            Assert.True(anArrayValueFromJToken.HasWrappedJToken);
            Assert.True(anObjectValueFromJToken.HasWrappedJToken);

            // Boolean is a special case where we never create a token because we reuse two static ones
            Assert.False(aTrueBoolValueFromJToken.HasWrappedJToken);
        }

        [Fact]
        public void DefaultValueJTokensAreReused()
        {
            TestValuesUseSameJTokenInstance(() => LdValue.Of(true));
            TestValuesUseSameJTokenInstance(() => LdValue.Of(false));
            TestValuesUseSameJTokenInstance(() => LdValue.Of((int)0));
            TestValuesUseSameJTokenInstance(() => LdValue.Of((long)0));
            TestValuesUseSameJTokenInstance(() => LdValue.Of((float)0));
            TestValuesUseSameJTokenInstance(() => LdValue.Of((double)0));
            TestValuesUseSameJTokenInstance(() => LdValue.Of(""));
        }

        private void TestValuesUseSameJTokenInstance(Func<LdValue> constructor)
        {
            var value1 = constructor();
            var value2 = constructor();
            Assert.Same(value1.InnerValue, value2.InnerValue);
        }

        [Fact]
        public void CanGetValueAsBool()
        {
            Assert.Equal(LdValueType.Bool, aTrueBoolValue.Type);
            Assert.True(aTrueBoolValue.AsBool);
            Assert.True(LdValue.Convert.Bool.ToType(aTrueBoolValue));
            Assert.Equal(LdValueType.Bool, aTrueBoolValueFromJToken.Type);
            Assert.True(aTrueBoolValueFromJToken.AsBool);
            Assert.True(LdValue.Convert.Bool.ToType(aTrueBoolValueFromJToken));
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
                aLongValue,
                aLongValueFromJToken,
                aFloatValue,
                aFloatValueFromJToken,
                aDoubleValue,
                aDoubleValueFromJToken,
                anArrayValue,
                anArrayValueFromJToken,
                anObjectValue,
                anObjectValueFromJToken
            };
            foreach (var value in values)
            {
                Assert.False(value.AsBool);
                Assert.False(LdValue.Convert.Bool.ToType(value));
            }
        }
        
        [Fact]
        public void CanGetValueAsString()
        {
            Assert.Equal(LdValueType.String, aStringValue.Type);
            Assert.Equal(someString, aStringValue.AsString);
            Assert.Equal(someString, LdValue.Convert.String.ToType(aStringValue));
            Assert.Equal(LdValueType.String, aStringValueFromJToken.Type);
            Assert.Equal(someString, aStringValueFromJToken.AsString);
            Assert.Equal(someString, LdValue.Convert.String.ToType(aStringValueFromJToken));
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
                anArrayValueFromJToken,
                anObjectValue,
                anObjectValueFromJToken
            };
            foreach (var value in values)
            {
                Assert.NotEqual(LdValueType.String, value.Type);
                Assert.Null(value.AsString);
                Assert.Null(LdValue.Convert.String.ToType(value));
            }
        }
        
        [Fact]
        public void CanGetIntegerValueOfAnyNumericType()
        {
            TestConvertIntegerToNumericType(LdValue.Convert.Int, v => v.AsInt);
            TestConvertIntegerToNumericType(LdValue.Convert.Long, v => v.AsLong);
            TestConvertIntegerToNumericType(LdValue.Convert.Float, v => v.AsFloat);
            TestConvertIntegerToNumericType(LdValue.Convert.Double, v => v.AsDouble);
            Assert.Equal(LdValueType.Number, anIntValue.Type);
            Assert.Equal(LdValueType.Number, anIntValueFromJToken.Type);
            Assert.Equal(LdValueType.Number, aLongValue.Type);
            Assert.Equal(LdValueType.Number, aLongValueFromJToken.Type);
        }

        private void TestConvertIntegerToNumericType<T>(LdValue.Converter<T> converter, Func<LdValue, T> getter)
        {
            var t_2 = (T)Convert.ChangeType(2, typeof(T));
            TestTypeConversion((int)2, t_2, n => LdValue.Of(n), converter, getter);
            TestTypeConversion((long)2, t_2, n => LdValue.Of(n), converter, getter);
            TestTypeConversion((float)2, t_2, n => LdValue.Of(n), converter, getter);
            TestTypeConversion((double)2, t_2, n => LdValue.Of(n), converter, getter);
        }

        [Fact]
        public void NonIntegerValueAsIntegerRoundsToNearest()
        {
            TestConvertNonIntegerToIntegerType(LdValue.Convert.Int, v => v.AsInt);
            TestConvertNonIntegerToIntegerType(LdValue.Convert.Long, v => v.AsLong);
        }

        private void TestConvertNonIntegerToIntegerType<T>(LdValue.Converter<T> converter, Func<LdValue, T> getter)
        {
            var t_2 = (T)Convert.ChangeType(2, typeof(T));
            var t_3 = (T)Convert.ChangeType(3, typeof(T));
            var t_minus_2 = (T)Convert.ChangeType(-2, typeof(T));
            var t_minus_3 = (T)Convert.ChangeType(-3, typeof(T));
            TestTypeConversion((float)2.25, t_2, n => LdValue.Of(n), converter, getter);
            TestTypeConversion((double)2.25, t_2, n => LdValue.Of(n), converter, getter);
            TestTypeConversion((float)2.75, t_3, n => LdValue.Of(n), converter, getter);
            TestTypeConversion((double)2.75, t_3, n => LdValue.Of(n), converter, getter);
            TestTypeConversion((float)-2.25, t_minus_2, n => LdValue.Of(n), converter, getter);
            TestTypeConversion((double)-2.25, t_minus_2, n => LdValue.Of(n), converter, getter);
            TestTypeConversion((float)-2.75, t_minus_3, n => LdValue.Of(n), converter, getter);
            TestTypeConversion((double)-2.75, t_minus_3, n => LdValue.Of(n), converter, getter);
        }
        
        [Fact]
        public void CanGetNonIntegerValueAsFloatingPoint()
        {
            TestTypeConversion(2.5f, 2.5f, n => LdValue.Of(n), LdValue.Convert.Float, v => v.AsFloat);
            TestTypeConversion(2.5d, 2.5f, n => LdValue.Of(n), LdValue.Convert.Float, v => v.AsFloat);
            TestTypeConversion(2.5d, 2.5d, n => LdValue.Of(n), LdValue.Convert.Double, v => v.AsDouble);
            TestTypeConversion(2.5d, 2.5d, n => LdValue.Of(n), LdValue.Convert.Double, v => v.AsDouble);
            Assert.Equal(LdValueType.Number, aFloatValue.Type);
            Assert.Equal(LdValueType.Number, aFloatValueFromJToken.Type);
            Assert.Equal(LdValueType.Number, aDoubleValue.Type);
            Assert.Equal(LdValueType.Number, aDoubleValueFromJToken.Type);
        }
        
        [Fact]
        public void NonNumericValueAsNumberIsZero()
        {
            TestNonNumericValueAsNumericTypeIsZero(LdValue.Convert.Int, v => v.AsInt, 0);
            TestNonNumericValueAsNumericTypeIsZero(LdValue.Convert.Long, v => v.AsLong, 0);
            TestNonNumericValueAsNumericTypeIsZero(LdValue.Convert.Float, v => v.AsFloat, 0);
            TestNonNumericValueAsNumericTypeIsZero(LdValue.Convert.Double, v => v.AsDouble, 0);
        }
        
        private void TestNonNumericValueAsNumericTypeIsZero<T>(LdValue.Converter<T> converter,
            Func<LdValue, T> getter, T zero)
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
                anArrayValueFromJToken,
                anObjectValue,
                anObjectValueFromJToken
            };
            foreach (var value in values)
            {
                Assert.Equal(zero, getter(value));
                Assert.Equal(zero, converter.ToType(value));
            }
        }

        private void TestTypeConversion<T, U>(T fromValue, U toValue, Func<T, LdValue> constructor,
            LdValue.Converter<U> converter, Func<LdValue, U> getter)
        {
            var value = constructor(fromValue);
            Assert.Equal(toValue, getter(value));
            Assert.Equal(toValue, converter.ToType(value));
        }
        
        [Fact]
        public void CanGetValuesAsList()
        {
            Assert.Equal(new bool[] { true, false }, LdValue.Convert.Bool.ArrayFrom(new bool[] { true, false }).AsList(LdValue.Convert.Bool));
            Assert.Equal(new bool[] { true, false }, LdValue.Convert.Bool.ArrayOf(true, false).AsList(LdValue.Convert.Bool));
            Assert.Equal(new bool[] { true, false }, LdValue.BuildArray().Add(true).Add(false).Build().AsList(LdValue.Convert.Bool));
            Assert.Equal(new int[] { 1, 2 }, LdValue.Convert.Int.ArrayFrom(new int[] { 1, 2 }).AsList(LdValue.Convert.Int));
            Assert.Equal(new int[] { 1, 2 }, LdValue.Convert.Int.ArrayOf(1, 2).AsList(LdValue.Convert.Int));
            Assert.Equal(new int[] { 1, 2 }, LdValue.BuildArray().Add(1).Add(2).Build().AsList(LdValue.Convert.Int));
            Assert.Equal(new long[] { 1, 2 }, LdValue.Convert.Long.ArrayFrom(new long[] { 1, 2 }).AsList(LdValue.Convert.Long));
            Assert.Equal(new long[] { 1, 2 }, LdValue.Convert.Long.ArrayOf(1, 2).AsList(LdValue.Convert.Long));
            Assert.Equal(new long[] { 1, 2 }, LdValue.BuildArray().Add(1).Add(2).Build().AsList(LdValue.Convert.Long));
            Assert.Equal(new float[] { 1.0f, 2.0f }, LdValue.Convert.Float.ArrayFrom(new float[] { 1.0f, 2.0f }).AsList(LdValue.Convert.Float));
            Assert.Equal(new float[] { 1.0f, 2.0f }, LdValue.Convert.Float.ArrayOf(1.0f, 2.0f).AsList(LdValue.Convert.Float));
            Assert.Equal(new float[] { 1.0f, 2.0f }, LdValue.BuildArray().Add(1.0f).Add(2.0f).Build().AsList(LdValue.Convert.Float));
            Assert.Equal(new double[] { 1.0d, 2.0d }, LdValue.Convert.Double.ArrayFrom(new double[] { 1.0d, 2.0d }).AsList(LdValue.Convert.Double));
            Assert.Equal(new double[] { 1.0d, 2.0d }, LdValue.Convert.Double.ArrayOf(1.0d, 2.0d).AsList(LdValue.Convert.Double));
            Assert.Equal(new double[] { 1.0f, 2.0f }, LdValue.BuildArray().Add(1.0d).Add(2.0d).Build().AsList(LdValue.Convert.Double));
            Assert.Equal(new string[] { "a", "b" }, LdValue.Convert.String.ArrayFrom(new string[] { "a", "b" }).AsList(LdValue.Convert.String));
            Assert.Equal(new string[] { "a", "b" }, LdValue.Convert.String.ArrayOf("a", "b").AsList(LdValue.Convert.String));
            Assert.Equal(new string[] { "a", "b" }, LdValue.BuildArray().Add("a").Add("b").Build().AsList(LdValue.Convert.String));
            Assert.Equal(new LdValue[] { anIntValue, aStringValue },
                LdValue.ArrayFrom(new LdValue[] { anIntValue, aStringValue }).AsList(LdValue.Convert.Json));
            Assert.Equal(new LdValue[] { anIntValue, aStringValue },
                LdValue.ArrayOf(anIntValue, aStringValue).AsList(LdValue.Convert.Json));
            Assert.Equal(new LdValue[] { anIntValue, aStringValue },
                LdValue.BuildArray().Add(anIntValue).Add(aStringValue).Build().AsList(LdValue.Convert.Json));

#pragma warning disable 0618
            Assert.Equal(new bool[] { true, false }, LdValue.FromJToken(new JArray { new JValue(true), new JValue(false) }).AsList(LdValue.Convert.Bool));
            Assert.Equal(new int[] { 1, 2 }, LdValue.FromJToken(new JArray { new JValue(1), new JValue(2) }).AsList(LdValue.Convert.Int));
            Assert.Equal(new long[] { 1, 2 }, LdValue.FromJToken(new JArray { new JValue(1L), new JValue(2L) }).AsList(LdValue.Convert.Long));
            Assert.Equal(new float[] { 1.0f, 2.0f }, LdValue.FromJToken(new JArray { new JValue(1.0f), new JValue(2.0f) }).AsList(LdValue.Convert.Float));
            Assert.Equal(new double[] { 1.0d, 2.0d }, LdValue.FromJToken(new JArray { new JValue(1.0d), new JValue(2.0d) }).AsList(LdValue.Convert.Double));
            Assert.Equal(new string[] { "a", "b" }, LdValue.FromJToken(new JArray { new JValue("a"), new JValue("b") }).AsList(LdValue.Convert.String));
            Assert.Equal(new LdValue[] { anIntValue, aStringValue }, LdValue.FromJToken(new JArray { new JValue(someInt), new JValue(someString) }).AsList(LdValue.Convert.Json));
#pragma warning restore 0618

            Assert.Equal(LdValue.Null, LdValue.Convert.Int.ArrayFrom((IEnumerable<int>)null));
        }
        
        [Fact]
        public void ListCanGetItemByIndex()
        {
            var v = LdValue.Convert.Int.ArrayOf(1, 2, 3);

            Assert.Equal(3, v.Count);
            Assert.Equal(LdValue.Of(2), v.Get(1));
            Assert.Equal(LdValue.Null, v.Get(-1));
            Assert.Equal(LdValue.Null, v.Get(3));

            var list = v.AsList(LdValue.Convert.Int);
            Assert.Equal(2, list[1]);
            Assert.Throws<IndexOutOfRangeException>(() => list[-1]);
            Assert.Throws<IndexOutOfRangeException>(() => list[3]);

#pragma warning disable 0618
            var v1 = LdValue.FromJToken(new JArray { new JValue(1), new JValue(2), new JValue(3) });
            Assert.Equal(3, v1.Count);
            Assert.Equal(LdValue.Of(2), v1.Get(1));
            Assert.Equal(LdValue.Null, v1.Get(-1));
            Assert.Equal(LdValue.Null, v1.Get(3));
#pragma warning restore 0618
        }

        [Fact]
        public void ListCanBeEnumerated()
        {
            var v = LdValue.Convert.Int.ArrayOf(1, 2, 3);
            var list = v.AsList(LdValue.Convert.Int);
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
                var emptyList = value.AsList(LdValue.Convert.Bool);
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
                LdValue.Convert.Bool.ObjectFrom(MakeDictionary(true, false)).AsDictionary(LdValue.Convert.Bool));
            AssertDictsEqual(MakeDictionary(true, false),
                LdValue.BuildObject().Add("1", true).Add("2", false).Build().AsDictionary(LdValue.Convert.Bool));
            AssertDictsEqual(MakeDictionary(1, 2),
                LdValue.Convert.Int.ObjectFrom(MakeDictionary(1, 2)).AsDictionary(LdValue.Convert.Int));
            AssertDictsEqual(MakeDictionary(1, 2),
               LdValue.BuildObject().Add("1", 1).Add("2", 2).Build().AsDictionary(LdValue.Convert.Int));
            AssertDictsEqual(MakeDictionary(1L, 2L),
                LdValue.Convert.Long.ObjectFrom(MakeDictionary(1L, 2L)).AsDictionary(LdValue.Convert.Long));
            AssertDictsEqual(MakeDictionary(1L, 2L),
               LdValue.BuildObject().Add("1", 1L).Add("2", 2L).Build().AsDictionary(LdValue.Convert.Long));
            AssertDictsEqual(MakeDictionary(1.0f, 2.0f),
                LdValue.Convert.Float.ObjectFrom(MakeDictionary(1.0f, 2.0f)).AsDictionary(LdValue.Convert.Float));
            AssertDictsEqual(MakeDictionary(1.0f, 2.0f),
                LdValue.BuildObject().Add("1", 1.0f).Add("2", 2.0f).Build().AsDictionary(LdValue.Convert.Float));
            AssertDictsEqual(MakeDictionary(1.0d, 2.0d),
                LdValue.Convert.Double.ObjectFrom(MakeDictionary(1.0d, 2.0d)).AsDictionary(LdValue.Convert.Double));
            AssertDictsEqual(MakeDictionary(1.0d, 2.0d),
               LdValue.BuildObject().Add("1", 1.0d).Add("2", 2.0d).Build().AsDictionary(LdValue.Convert.Double));
            AssertDictsEqual(MakeDictionary("a", "b"),
               LdValue.BuildObject().Add("1", "a").Add("2", "b").Build().AsDictionary(LdValue.Convert.String));
            AssertDictsEqual(MakeDictionary("a", "b"),
                LdValue.Convert.String.ObjectFrom(MakeDictionary("a", "b")).AsDictionary(LdValue.Convert.String));
            AssertDictsEqual(MakeDictionary(anIntValue, aStringValue),
                LdValue.Convert.Json.ObjectFrom(MakeDictionary(anIntValue, aStringValue)).AsDictionary(LdValue.Convert.Json));
            AssertDictsEqual(MakeDictionary(anIntValue, aStringValue),
                LdValue.BuildObject().Add("1", anIntValue).Add("2", aStringValue).Build().AsDictionary(LdValue.Convert.Json));
            Assert.Equal(LdValue.Null, LdValue.Convert.String.ObjectFrom((IReadOnlyDictionary<string, string>)null));
        }

        [Fact]
        public void DictionaryCanGetValueByKey()
        {
            var v = LdValue.BuildObject().Add("a", 100).Add("b", 200).Add("c", 300).Build();

            Assert.Equal(3, v.Count);
            Assert.Equal(LdValue.Of(200), v.Get("b"));
            Assert.NotEqual(LdValue.Null, v.Get(1));
            Assert.Equal(LdValue.Null, v.Get("x"));
            Assert.Equal(LdValue.Null, v.Get(-1));
            Assert.Equal(LdValue.Null, v.Get(3));

            var d = v.AsDictionary(LdValue.Convert.Int);
            Assert.True(d.ContainsKey("b"));
            Assert.Equal(200, d["b"]);
            Assert.True(d.TryGetValue("a", out var n));
            Assert.Equal(100, n);
            Assert.False(d.ContainsKey("x"));
            Assert.Throws<KeyNotFoundException>(() => d["x"]);
            Assert.False(d.TryGetValue("x", out n));

#pragma warning disable 0618
            var v1 = LdValue.FromJToken(new JObject() { { "a", new JValue(100) }, { "b", new JValue(200) }, { "c", new JValue(300) } });
            Assert.Equal(3, v1.Count);
            Assert.Equal(LdValue.Of(200), v1.Get("b"));
            Assert.NotEqual(LdValue.Null, v1.Get(1));
            Assert.Equal(LdValue.Null, v1.Get("x"));
            Assert.Equal(LdValue.Null, v1.Get(-1));
            Assert.Equal(LdValue.Null, v1.Get(3));
#pragma warning restore 0618
        }

        [Fact]
        public void DictionaryCanBeEnumerated()
        {
            var v = LdValue.Convert.Int.ObjectFrom(MakeDictionary(100, 200, 300));
            var d = v.AsDictionary(LdValue.Convert.Int);
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
                var emptyDict = value.AsDictionary(LdValue.Convert.Bool);
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
#pragma warning disable 0618
            Assert.Equal(LdValue.FromJToken(new JValue(2)),
                LdValue.FromJToken(new JValue(2.0f)));
#pragma warning restore 0618
        }

        [Fact]
        public void ComplexTypeEqualityUsesDeepEqual()
        {
            var a0 = LdValue.ArrayOf(LdValue.Of("a"), LdValue.ArrayOf(LdValue.Of("b")));
            var a1 = LdValue.FromSafeValue(new JArray() { new JValue("a"),
                new JArray() { new JValue("b") } });
            Assert.Equal(a0, a1);
            Assert.Equal(a0.GetHashCode(), a1.GetHashCode());
            var o0 = LdValue.Convert.String.ObjectFrom(new Dictionary<string, string> { { "a", "b" } });
            var o1 = LdValue.FromSafeValue(new JObject { { "a", new JValue("b") } });
            Assert.Equal(o0, o1);
            Assert.Equal(o0.GetHashCode(), o1.GetHashCode());
        }

        [Fact]
        public void CanUseLongTypeForNumberGreaterThanMaxInt()
        {
            long n = (long)int.MaxValue + 1;
            Assert.Equal(n, LdValue.Of(n).AsLong);
            Assert.Equal(n, LdValue.Convert.Long.ToType(LdValue.Of(n)));
            Assert.Equal(n, LdValue.Convert.Long.FromType(n).AsLong);
        }

        [Fact]
        public void CanUseDoubleTypeForNumberGreaterThanMaxFloat()
        {
            double n = (double)float.MaxValue + 1;
            Assert.Equal(n, LdValue.Of(n).AsDouble);
            Assert.Equal(n, LdValue.Convert.Double.ToType(LdValue.Of(n)));
            Assert.Equal(n, LdValue.Convert.Double.FromType(n).AsDouble);
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
            Assert.Equal("[3]", JsonConvert.SerializeObject(anArrayValueFromJToken));
            Assert.Equal("{\"1\":\"x\"}", JsonConvert.SerializeObject(anObjectValue));
            Assert.Equal("{\"1\":\"x\"}", JsonConvert.SerializeObject(anObjectValueFromJToken));
        }
        
        [Fact]
        public void TestJsonDeserialization()
        {
            var json = "{\"a\":\"b\"}";
            var actual = JsonConvert.DeserializeObject<LdValue>(json);
            var expected = LdValue.Convert.String.ObjectFrom(new Dictionary<string, string> { { "a", "b" } });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestValueToJToken()
        {
#pragma warning disable 0618
            Assert.Null(LdValue.Null.AsJToken());
            Assert.Equal(new JValue(true), LdValue.Of(true).AsJToken());
            Assert.Equal(new JValue(1), LdValue.Of(1).AsJToken());
            Assert.Equal(new JValue(1L), LdValue.Of(1L).AsJToken());
            Assert.Equal(new JValue(1.0f), LdValue.Of(1.0f).AsJToken());
            Assert.Equal(new JValue(1.0d), LdValue.Of(1.0d).AsJToken());
            Assert.Equal(new JValue("x"), LdValue.Of("x").AsJToken());
            Assert.True(JToken.DeepEquals(new JArray { new JValue(1), new JValue(2) },
                LdValue.Convert.Int.ArrayOf(1, 2).AsJToken()));
            Assert.True(JToken.DeepEquals(new JObject { { "a", new JValue(1) } },
                LdValue.Convert.Int.ObjectFrom(new Dictionary<string, int> { { "a", 1 } }).AsJToken()));
#pragma warning restore 0618
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
