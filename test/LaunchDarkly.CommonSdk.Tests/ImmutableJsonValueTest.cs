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
        static readonly ImmutableJsonValue anArrayValue = ImmutableJsonValue.Of(someArray);
        static readonly ImmutableJsonValue anObjectValue = ImmutableJsonValue.Of(someObject);

        [Fact]
        public void CanGetValueAsBool()
        {
            Assert.True(aTrueBoolValue.AsBool);
        }

        [Fact]
        public void NonBooleanValueAsBoolIsFalse()
        {
            Assert.False(ImmutableJsonValue.Null.AsBool);
            Assert.False(aStringValue.AsBool);
            Assert.False(anIntValue.AsBool);
            Assert.False(aFloatValue.AsBool);
            Assert.False(anArrayValue.AsBool);
            Assert.False(anObjectValue.AsBool);
        }
        
        [Fact]
        public void CanGetValueAsString()
        {
            Assert.Equal(someString, aStringValue.AsString);
        }

        [Fact]
        public void NullValueAsStringIsNull()
        {
            Assert.Null(ImmutableJsonValue.Null.AsString);
        }

        [Fact]
        public void NonStringValuesAreStringified()
        {
            Assert.Equal(true.ToString(), aTrueBoolValue.AsString);
            Assert.Equal(someInt.ToString(), anIntValue.AsString);
            Assert.Equal(someFloat.ToString(), aFloatValue.AsString);
            Assert.Equal("[3]", anArrayValue.AsString);
            Assert.Equal("{\"a\":\"b\"}", anObjectValue.AsString);
        }

        [Fact]
        public void CanGetValueAsInt()
        {
            Assert.Equal(someInt, anIntValue.AsInt);
        }
        
        [Fact]
        public void NonNumericValueAsIntIsZero()
        {
            Assert.Equal(0, ImmutableJsonValue.Null.AsInt);
            Assert.Equal(0, aTrueBoolValue.AsInt);
            Assert.Equal(0, aStringValue.AsInt);
            Assert.Equal(0, aNumericLookingStringValue.AsInt);
            Assert.Equal(0, anArrayValue.AsInt);
        }

        [Fact]
        public void CanGetValueAsFloat()
        {
            Assert.Equal(someFloat, aFloatValue.AsFloat);
        }

        [Fact]
        public void CanGetIntValueAsFloat()
        {
            Assert.Equal((float)someInt, anIntValue.AsFloat);
        }

        [Fact]
        public void NonNumericValueAsFloatIsZero()
        {
            Assert.Equal(0.0f, ImmutableJsonValue.Null.AsFloat);
            Assert.Equal(0.0f, aTrueBoolValue.AsFloat);
            Assert.Equal(0.0f, aStringValue.AsFloat);
            Assert.Equal(0.0f, aNumericLookingStringValue.AsFloat);
            Assert.Equal(0.0f, anArrayValue.AsFloat);
        }
        
        [Fact]
        public void CanGetFloatValueAsInt()
        {
            Assert.Equal((int)someFloat, aFloatValue.AsInt);
        }

        [Fact]
        public void CanGetValueAsJArray()
        {
            var a1 = anArrayValue.AsJArray();
            TestUtil.AssertJsonEquals(someArray, a1);
            Assert.NotSame(someArray, a1); // it's been deep-copied
        }

        [Fact]
        public void CanGetValueAsJObject()
        {
            var o1 = anObjectValue.AsJObject();
            TestUtil.AssertJsonEquals(someObject, o1);
            Assert.NotSame(someObject, o1);
        }

        [Fact]
        public void ValueAsJTokenUsesSameObjectForPrimitiveType()
        {
            var simpleValue0 = new JValue(3);
            var v = ImmutableJsonValue.Of(simpleValue0);
            var simpleValue1 = v.AsJToken();
            Assert.Same(simpleValue0, simpleValue1);
        }

        [Fact]
        public void ValueAsJTokenCopiesValueForArray()
        {
            var a0 = new JArray() { new JValue(3) };
            var v = ImmutableJsonValue.Of(a0);
            var a1 = v.AsJToken();
            TestUtil.AssertJsonEquals(a0, a1);
            Assert.NotSame(a0, a1);
        }

        [Fact]
        public void ValueAsJTokenCopiesValueForObject()
        {
            var o0 = new JObject() { { "a", new JValue("b") } };
            var v = ImmutableJsonValue.Of(o0);
            var o1 = v.AsJToken();
            TestUtil.AssertJsonEquals(o0, o1);
            Assert.NotSame(o0, o1);
        }

        [Fact]
        public void EqualityUsesDeepEqual()
        {
            var o0 = new JObject() { { "a", new JValue("b") } };
            var o1 = new JObject() { { "a", new JValue("b") } };
            Assert.Equal(ImmutableJsonValue.Of(o0), ImmutableJsonValue.Of(o1));
        }

        [Fact]
        public void TestJsonSerialization()
        {
            var o = new JObject() { { "a", new JValue("b") } };
            var v = ImmutableJsonValue.Of(o);
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
