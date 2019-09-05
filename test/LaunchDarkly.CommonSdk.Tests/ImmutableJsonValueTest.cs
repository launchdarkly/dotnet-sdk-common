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
        static readonly JObject someObject = new JObject() { { "a", new JValue("b") } };

        static readonly ImmutableJsonValue aTrueBoolValue = ImmutableJsonValue.Of(true);
        static readonly ImmutableJsonValue anIntValue = ImmutableJsonValue.Of(someInt);
        static readonly ImmutableJsonValue aFloatValue = ImmutableJsonValue.Of(someFloat);
        static readonly ImmutableJsonValue aStringValue = ImmutableJsonValue.Of(someString);
        static readonly ImmutableJsonValue aNumericLookingStringValue = ImmutableJsonValue.Of("3");
        static readonly ImmutableJsonValue anArrayValue = ImmutableJsonValue.FromJToken(someArray);
        static readonly ImmutableJsonValue anObjectValue = ImmutableJsonValue.FromJToken(someObject);

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
            Assert.Equal("{\"a\":\"b\"}", anObjectValue.AsString);
            Assert.Equal("{\"a\":\"b\"}", anObjectValue.Value<string>());
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
        public void CanGetValueAsJArray()
        {
            var a1 = anArrayValue.AsJArray();
            TestUtil.AssertJsonEquals(someArray, a1);
            Assert.NotSame(someArray, a1); // it's been deep-copied
        }

        [Fact]
        public void NonArrayValuesReturnEmptyJArray()
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
                TestUtil.AssertJsonEquals(emptyArray, value.AsJArray());
                TestUtil.AssertJsonEquals(emptyArray, value.Value<JArray>());
            }
        }

        [Fact]
        public void CanGetValueAsJObject()
        {
            var o1 = anObjectValue.AsJObject();
            TestUtil.AssertJsonEquals(someObject, o1);
            Assert.NotSame(someObject, o1);
        }

        [Fact]
        public void NonObjectValuesReturnEmptyJObject()
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
                TestUtil.AssertJsonEquals(emptyObject, value.AsJObject());
                TestUtil.AssertJsonEquals(emptyObject, value.Value<JObject>());
            }
        }

        [Fact]
        public void ValueAsJTokenUsesSameObjectForPrimitiveType()
        {
            var simpleValue0 = new JValue(3);
            var v = ImmutableJsonValue.FromJToken(simpleValue0);
            var simpleValue1 = v.AsJToken();
            Assert.Same(simpleValue0, simpleValue1);
        }

        [Fact]
        public void ValueAsJTokenCopiesValueForArray()
        {
            var a0 = new JArray() { new JValue(3) };
            var v = ImmutableJsonValue.FromJToken(a0);
            var a1 = v.AsJToken();
            TestUtil.AssertJsonEquals(a0, a1);
            Assert.NotSame(a0, a1);
        }

        [Fact]
        public void ValueAsJTokenCopiesValueForObject()
        {
            var o0 = new JObject() { { "a", new JValue("b") } };
            var v = ImmutableJsonValue.FromJToken(o0);
            var o1 = v.AsJToken();
            TestUtil.AssertJsonEquals(o0, o1);
            Assert.NotSame(o0, o1);
        }

        [Fact]
        public void EqualityUsesDeepEqual()
        {
            var o0 = new JObject() { { "a", new JValue("b") } };
            var o1 = new JObject() { { "a", new JValue("b") } };
            Assert.Equal(ImmutableJsonValue.FromJToken(o0), ImmutableJsonValue.FromJToken(o1));
        }

        [Fact]
        public void TestJsonSerialization()
        {
            var o = new JObject() { { "a", new JValue("b") } };
            var v = ImmutableJsonValue.FromJToken(o);
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
            TestUtil.AssertJsonEquals(o, v.AsJToken());
        }

        [Fact]
        public void TestJsonDeserializationOfNull()
        {
            var v = JsonConvert.DeserializeObject<ImmutableJsonValue>("null");
            Assert.Null(v.AsJToken());
        }

        [Fact]
        public void TestNullConstructorIsEquivalentToNullInstance()
        {
            Assert.Equal(ImmutableJsonValue.Null, ImmutableJsonValue.Of(null));
        }
    }
}
