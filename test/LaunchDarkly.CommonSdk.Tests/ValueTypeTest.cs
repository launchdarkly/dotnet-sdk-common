using Newtonsoft.Json.Linq;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class ValueTypeTest
    {
        private const int jsonIntValue = 3;
        private const float jsonFloatValue = 3.25f;
        private const string jsonStringValue = "hi";

        private static readonly JToken jsonNull = JValue.CreateNull();
        private static readonly JToken jsonBoolTrue = new JValue(true);
        private static readonly JToken jsonInt = new JValue(jsonIntValue);
        private static readonly JToken jsonFloat = new JValue(jsonFloatValue);
        private static readonly JToken jsonString = new JValue(jsonStringValue);
        private static readonly JToken jsonArray = new JArray { new JValue("item") };
        private static readonly JToken jsonObject = new JObject { { "a", new JValue("b") } };

        [Fact]
        public void BoolFromJson()
        {
            Assert.True(ValueTypes.Bool.ValueFromJson(jsonBoolTrue));
        }

        [Fact]
        public void BoolFromNonBoolValueIsError()
        {
            VerifyConversionError(ValueTypes.Bool, new JToken[] {
                null, jsonNull, jsonInt, jsonFloat, jsonString, jsonArray, jsonObject
            });
        }

        [Fact]
        public void BoolToJson()
        {
            Assert.Equal(jsonBoolTrue, ValueTypes.Bool.ValueToJson(true));
        }

        [Fact]
        public void BoolToJsonUsesSameInstances()
        {
            var jt0 = ValueTypes.Bool.ValueToJson(true);
            var jt1 = ValueTypes.Bool.ValueToJson(true);
            var jf0 = ValueTypes.Bool.ValueToJson(false);
            var jf1 = ValueTypes.Bool.ValueToJson(false);
            Assert.Same(jt0, jt1);
            Assert.Same(jf0, jf1);
        }

        [Fact]
        public void IntFromJsonInt()
        {
            Assert.Equal(jsonIntValue, ValueTypes.Int.ValueFromJson(jsonInt));
        }

        [Fact]
        public void IntFromJsonFloatRoundsToNearest()
        {
            // This behavior is defined by the Newtonsoft.Json conversion operator that we have been
            // relying on in the .NET SDK, so we must preserve it until the next major version.
            Assert.Equal(2, ValueTypes.Int.ValueFromJson(new JValue(2.25f)));
            Assert.Equal(3, ValueTypes.Int.ValueFromJson(new JValue(2.75f)));
        }

        [Fact]
        public void IntFromNonNumericValueIsError()
        {
            VerifyConversionError(ValueTypes.Int, new JToken[] {
                null, jsonNull, jsonBoolTrue, jsonString, jsonArray, jsonObject
            });
        }

        [Fact]
        public void IntToJson()
        {
            Assert.Equal(jsonInt, ValueTypes.Int.ValueToJson(jsonIntValue));
        }
        
        [Fact]
        public void FloatFromJsonFloat()
        {
            Assert.Equal(jsonFloatValue, ValueTypes.Float.ValueFromJson(jsonFloat));
        }

        [Fact]
        public void FloatFromJsonInt()
        {
            Assert.Equal((float)jsonIntValue, ValueTypes.Float.ValueFromJson(jsonInt));
        }

        [Fact]
        public void FloatFromNonNumericValueIsError()
        {
            VerifyConversionError(ValueTypes.Float, new JToken[] {
                null, jsonNull, jsonBoolTrue, jsonString, jsonArray, jsonObject
            });
        }

        [Fact]
        public void FloatToJson()
        {
            Assert.Equal(jsonFloat, ValueTypes.Float.ValueToJson(jsonFloatValue));
        }

        [Fact]
        public void StringFromJson()
        {
            Assert.Equal(jsonStringValue, ValueTypes.String.ValueFromJson(jsonString));
        }

        [Fact]
        public void StringFromNull()
        {
            Assert.Null(ValueTypes.String.ValueFromJson(null));
        }

        [Fact]
        public void StringFromJsonNull()
        {
            Assert.Null(ValueTypes.String.ValueFromJson(jsonNull));
        }

        [Fact]
        public void StringFromNonStringValueIsError()
        {
            VerifyConversionError(ValueTypes.String, new JToken[] {
                jsonBoolTrue, jsonInt, jsonFloat, jsonArray, jsonObject
            });
        }

        [Fact]
        public void StringToJson()
        {
            Assert.Equal(jsonString, ValueTypes.String.ValueToJson(jsonStringValue));
        }

        [Fact]
        public void JsonFromJson()
        {
            Assert.Same(jsonObject, ValueTypes.Json.ValueFromJson(jsonObject));
        }

        [Fact]
        public void JsonFromNull()
        {
            Assert.Null(ValueTypes.Json.ValueFromJson(null));
        }

        [Fact]
        public void JsonToJson()
        {
            Assert.Same(jsonObject, ValueTypes.Json.ValueToJson(jsonObject));
        }

        private void VerifyConversionError<T>(ValueType<T> type, JToken[] badValues)
        {
            foreach (var v in badValues)
            {
                try
                {
                    type.ValueFromJson(v);
                    Assert.True(false, "converting from " + (v is null ? "null" : v.Type.ToString()) +
                        " should throw exception");
                }
                catch (ValueTypeException) { }
            }
        }
    }
}
