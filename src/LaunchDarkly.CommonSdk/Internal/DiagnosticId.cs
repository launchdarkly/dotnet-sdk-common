using System;
using Newtonsoft.Json;

namespace LaunchDarkly.Common
{
    class DiagnosticId
    {
        [JsonProperty(PropertyName = "diagnosticId", NullValueHandling = NullValueHandling.Ignore)]
        internal readonly Guid _diagnosticId;
        [JsonProperty(PropertyName = "sdkKeySuffix", NullValueHandling = NullValueHandling.Ignore)]
        internal readonly string _sdkKeySuffix;

        internal DiagnosticId(string sdkKey, Guid diagnosticId)
        {
            if (sdkKey != null)
            {
                _sdkKeySuffix = sdkKey.Substring(Math.Max(0, sdkKey.Length - 6));
            }
            _diagnosticId = diagnosticId;
        }
    }
}
