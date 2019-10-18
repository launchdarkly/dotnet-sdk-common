using System;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Common
{
    class DiagnosticId
    {
        internal readonly Guid diagnosticId;
        internal readonly string sdkKeySuffix;

        internal DiagnosticId(string sdkKey, Guid diagnosticId)
        {
            this.sdkKeySuffix = sdkKey.Substring(Math.Max(0, sdkKey.Length - 6));
            this.diagnosticId = diagnosticId;
        }
    }
}
