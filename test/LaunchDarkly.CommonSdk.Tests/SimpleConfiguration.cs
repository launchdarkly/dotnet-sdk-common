using System;
using System.Collections.Generic;
using System.Net.Http;

namespace LaunchDarkly.Common.Tests
{
    // Used in unit tests of common code - a minimal implementation of IBaseConfiguration.
    class SimpleConfiguration : IBaseConfiguration
    {
        public string SdkKey { get; set; } = "SDK_KEY";
        public Uri BaseUri { get; set; }
        public Uri EventsUri { get; set; }
        public Uri StreamUri { get; set; }
        public bool Offline { get; set; }
        public TimeSpan ReadTimeout { get; set; }
        public TimeSpan ReconnectTime { get; set;  }
        public int EventCapacity { get; set; } = 1000;
        public int EventQueueCapacity => EventCapacity;
        public TimeSpan EventFlushInterval { get; set; }
        public TimeSpan EventQueueFrequency => EventFlushInterval;
        public int EventSamplingInterval { get; set; }
        public bool AllAttributesPrivate { get; set; }
        public ISet<string> PrivateAttributeNames { get; set; } = new HashSet<string>();
        public int UserKeysCapacity { get; set; } = 1000;
        public TimeSpan UserKeysFlushInterval { get; set; }
        public bool InlineUsersInEvents { get; set; }
        public TimeSpan HttpClientTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public HttpClientHandler HttpClientHandler { get; set; } = new HttpClientHandler();
    }
}
