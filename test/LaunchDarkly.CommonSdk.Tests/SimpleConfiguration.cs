using System;
using System.Collections.Generic;
using System.Net.Http;

namespace LaunchDarkly.Common.Tests
{
    // Used in unit tests of common code - a minimal implementation of our configuration interfaces.
    public class SimpleConfiguration :
        IEventProcessorConfiguration, IHttpRequestConfiguration, IStreamManagerConfiguration
    {
        public string SdkKey { get; set; } = "SDK_KEY";
        public string HttpAuthorizationKey => SdkKey;
        public Uri BaseUri { get; set; }
        public Uri EventsUri { get; set; }
        public Uri StreamUri { get; set; }
        public bool Offline { get; set; }
        public TimeSpan ReadTimeout { get; set; }
        public TimeSpan ReconnectTime { get; set;  }
        public int EventCapacity { get; set; } = 1000;
        public TimeSpan EventFlushInterval { get; set; }
        public int EventSamplingInterval { get; set; }
        public bool AllAttributesPrivate { get; set; }
        public ISet<string> PrivateAttributeNames { get; set; } = new HashSet<string>();
        public int UserKeysCapacity { get; set; } = 1000;
        public TimeSpan UserKeysFlushInterval { get; set; }
        public bool InlineUsersInEvents { get; set; }
        public TimeSpan HttpClientTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public HttpClientHandler HttpClientHandler { get; set; } = new HttpClientHandler();
        public TimeSpan DiagnosticRecordingInterval { get; set; }
        public bool DiagnosticOptOut { get; set; }
        public Uri DiagnosticUri { get; set; }
        public string WrapperName { get; set; }
        public string WrapperVersion { get; set; }
   }
}
