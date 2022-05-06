using LaunchDarkly.JsonStream;

namespace LaunchDarkly.Sdk.Json
{
    public static partial class LdJsonConverters
    {
#pragma warning disable CS1591 // don't bother with XML comments for these low-level helpers

        /// <summary>
        /// The JSON converter for <see cref="AttributeRef"/>.
        /// </summary>
        public sealed class AttributeRefConverter : IJsonStreamConverter
        {
            public object ReadJson(ref JReader reader)
            {
                var maybeString = reader.StringOrNull();
                if (maybeString is null)
                {
                    return new AttributeRef();
                }
                return AttributeRef.FromPath(maybeString);
            }

            public void WriteJson(object value, IValueWriter writer)
            {
                var a = (AttributeRef)value;
                if (a.Defined)
                {
                    writer.String(a.ToString());
                }
                else
                {
                    writer.Null();
                }
            }
        }
    }

#pragma warning restore CS1591
}
