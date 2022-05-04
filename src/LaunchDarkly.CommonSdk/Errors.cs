
namespace LaunchDarkly.Sdk
{
    internal static class Errors
    {
        internal const string AttrEmpty = "attribute reference cannot be empty";
        internal const string AttrExtraSlash = "attribute reference contained a double slash or a trailing slash";
        internal const string AttrInvalidEscape =
            "attribute reference contained an escape character (~) that was not followed by 0 or 1";
    }
}
