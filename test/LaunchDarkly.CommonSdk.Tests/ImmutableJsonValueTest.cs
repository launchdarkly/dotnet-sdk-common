using LaunchDarkly.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class ImmutableJsonValueTest
    {
        [Fact]
        public void CanGetValueAsBool()
        {
            var v = ImmutableJsonValue.FromJToken(true);
            Assert.True(v.AsBool);
        }

        [Fact]
        public void CanGetValueAsString()
        {
            var v = ImmutableJsonValue.FromJToken("hi");
            Assert.Equal("hi", v.AsString);
        }

        [Fact]
        public void CanGetValueAsInt()
        {
            var v = ImmutableJsonValue.FromJToken(3);
            Assert.Equal(3, v.AsInt);
        }

        [Fact]
        public void CanGetValueAsFloat()
        {
            var v = ImmutableJsonValue.FromJToken(3.5f);
            Assert.Equal(3.5f, v.AsFloat);
        }

        [Fact]
        public void CanGetIntValueAsFloat()
        {
            var v = ImmutableJsonValue.FromJToken(3);
            Assert.Equal(3.0f, v.AsFloat);
        }

        [Fact]
        public void CanGetFloatValueAsInt()
        {
            var v = ImmutableJsonValue.FromJToken(3.0f);
            Assert.Equal(3, v.AsInt);
        }

        [Fact]
        public void CanGetValueAsJArray()
        {
            var a0 = new JArray() { new JValue(3) };
            var v = ImmutableJsonValue.FromJToken(a0);
            var a1 = v.AsJArray();
            TestUtil.AssertJsonEquals(a0, a1);
            Assert.NotSame(a0, a1);
        }

        [Fact]
        public void CanGetValueAsJObject()
        {
            var o0 = new JObject() { { "a", new JValue("b") } };
            var v = ImmutableJsonValue.FromJToken(o0);
            var o1 = v.AsJObject();
            TestUtil.AssertJsonEquals(o0, o1);
            Assert.NotSame(o0, o1);
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
            Assert.Equal(ImmutableJsonValue.Null, ImmutableJsonValue.FromJToken(null));
        }
    }
}
