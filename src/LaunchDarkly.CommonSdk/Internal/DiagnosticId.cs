using System;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Common
{
    class DiagnosticId
    {
        internal readonly Guid diagnosticId = System.Guid.NewGuid();
        internal readonly string SdkKeySuffix;

        internal DiagnosticId(string sdkKey) {
            SdkKeySuffix = sdkKey.Substring(Math.Max(0, sdkKey.Length - 6));
        }
    }
}
