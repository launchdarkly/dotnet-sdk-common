using System;
using Newtonsoft.Json;
using Xunit;

using static LaunchDarkly.Sdk.UnixMillisecondTime;

namespace LaunchDarkly.Sdk
{
    public class UnixMillisecondTimeTest
    {
        private const long someTime = 1605311688609;

        [Fact]
        public void MillisValue()
        {
            var t = OfMillis(someTime);
            Assert.Equal(someTime, t.Value);
        }

        [Fact]
        public void PlusMillis()
        {
            var t = OfMillis(someTime);
            Assert.Equal(someTime + 444, t.PlusMillis(444).Value);
        }

        [Fact]
        public void FromAndToDateTime()
        {
            var dt = new DateTime(1989, 11, 9, 17, 53, 00);
            var t = FromDateTime(dt);
            Assert.Equal(626637180000, t.Value);
            Assert.Equal(dt, t.AsDateTime);
        }

        [Fact]
        public void Comparisons()
        {
            for (var a = 1; a < 3; a++)
            {
                for (var b = 1; b < 3; b++)
                {
                    Assert.Equal(a == b, OfMillis(a).Equals(OfMillis(b)));
                    Assert.Equal(a == b, OfMillis(a) == OfMillis(b));
                    Assert.Equal(a != b, OfMillis(a) != OfMillis(b));
                    Assert.Equal(a < b, OfMillis(a) < OfMillis(b));
                    Assert.Equal(a <= b, OfMillis(a) <= OfMillis(b));
                    Assert.Equal(a > b, OfMillis(a) > OfMillis(b));
                    Assert.Equal(a >= b, OfMillis(a) >= OfMillis(b));
                    Assert.Equal(a.CompareTo(b), OfMillis(a).CompareTo(OfMillis(b)));
                }
                Assert.Equal(a.GetHashCode(), OfMillis(a).GetHashCode());
            }
        }

        [Fact]
        public void JsonSerialize()
        {
            var t = OfMillis(someTime);
            Assert.Equal(someTime.ToString(), JsonConvert.SerializeObject(t));
        }

        [Fact]
        public void JsonDeserialize()
        {
            Assert.Equal(OfMillis(someTime),
                JsonConvert.DeserializeObject<UnixMillisecondTime>(someTime.ToString()));
        }

        [Fact]
        public void JsonDeserializeNullableWithValue()
        {
            Assert.Equal((UnixMillisecondTime?)OfMillis(someTime),
                JsonConvert.DeserializeObject<UnixMillisecondTime?>(someTime.ToString()));
        }

        [Fact]
        public void JsonDeserializeNullableAsNull()
        {
            Assert.Null(JsonConvert.DeserializeObject<UnixMillisecondTime?>("null"));
        }
    }
}
