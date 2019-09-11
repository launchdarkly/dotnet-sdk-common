using LaunchDarkly.Client;
using System.Collections.Generic;
using Xunit;

namespace LaunchDarkly.Common.Tests
{
    public class ValueTypeTest
    {
        private const int jsonIntValue = 3;
        private const float jsonFloatValue = 3.25f;
        private const string jsonStringValue = "hi";

        private static readonly LdValue jsonBoolTrue = LdValue.Of(true);
        private static readonly LdValue jsonInt = LdValue.Of(jsonIntValue);
        private static readonly LdValue jsonFloat = LdValue.Of(jsonFloatValue);
        private static readonly LdValue jsonString = LdValue.Of(jsonStringValue);
        private static readonly LdValue jsonArray = LdValue.FromValues(new string[] { "item" });
        private static readonly LdValue jsonObject = LdValue.FromDictionary(new Dictionary<string, string> { { "a", "b" } });

        [Fact]
        public void BoolFromJson()
        {
            Assert.True(ValueTypes.Bool.ValueFromJson(LdValue.Of(true)));
        }

        [Fact]
        public void BoolFromNonBoolValueIsError()
        {
            VerifyConversionError(ValueTypes.Bool, new LdValue[] {
                LdValue.Null, jsonInt, jsonFloat, jsonString, jsonArray, jsonObject
            });
        }

        [Fact]
        public void BoolToJson()
        {
            Assert.Equal(jsonBoolTrue, ValueTypes.Bool.ValueToJson(true));
        }

        [Fact]
        public void IntFromJsonInt()
        {
            Assert.Equal(jsonIntValue, ValueTypes.Int.ValueFromJson(jsonInt));
        }

        [Fact]
        public void IntFromJsonFloatRoundsToNearestInt()
        {
            Assert.Equal(2, ValueTypes.Int.ValueFromJson(LdValue.Of(2.25f)));
            Assert.Equal(3, ValueTypes.Int.ValueFromJson(LdValue.Of(2.75f)));
            Assert.Equal(-2, ValueTypes.Int.ValueFromJson(LdValue.Of(-2.25f)));
            Assert.Equal(-3, ValueTypes.Int.ValueFromJson(LdValue.Of(-2.75f)));
        }

        [Fact]
        public void IntFromNonNumericValueIsError()
        {
            VerifyConversionError(ValueTypes.Int, new LdValue[] {
                LdValue.Null, jsonBoolTrue, jsonString, jsonArray, jsonObject
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
            VerifyConversionError(ValueTypes.Float, new LdValue[] {
                LdValue.Null, LdValue.Of(true), jsonString, jsonArray, jsonObject
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
            Assert.Null(ValueTypes.String.ValueFromJson(LdValue.Null));
        }

        [Fact]
        public void StringFromJsonNull()
        {
            Assert.Null(ValueTypes.String.ValueFromJson(LdValue.Null));
        }

        [Fact]
        public void StringFromNonStringValueIsError()
        {
            VerifyConversionError(ValueTypes.String, new LdValue[] {
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
            Assert.Same(jsonObject.InnerValue, ValueTypes.Json.ValueFromJson(jsonObject).InnerValue);
        }

        [Fact]
        public void JsonFromNull()
        {
            Assert.Equal(LdValue.Null, ValueTypes.Json.ValueFromJson(LdValue.Null));
        }

        [Fact]
        public void JsonToJson()
        {
            Assert.Same(jsonObject.InnerValue, ValueTypes.Json.ValueToJson(jsonObject).InnerValue);
        }

        [Fact]
        public void JsonFromMutableJson()
        {
            Assert.Same(jsonObject.InnerValue, ValueTypes.MutableJson.ValueToJson(jsonObject.InnerValue).InnerValue);
        }

        [Fact]
        public void JsonToMutableJson()
        {
            Assert.Same(jsonObject.InnerValue, ValueTypes.MutableJson.ValueFromJson(jsonObject));
        }
        
        private void VerifyConversionError<T>(ValueType<T> type, LdValue[] badValues)
        {
            foreach (var v in badValues)
            {
                try
                {
                    type.ValueFromJson(v);
                    Assert.True(false, "converting from " + v.Type + " should throw exception");
                }
                catch (ValueTypeException) { }
            }
        }
    }
}