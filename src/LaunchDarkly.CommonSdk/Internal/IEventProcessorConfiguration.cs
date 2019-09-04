using System;
using System.Collections.Generic;

namespace LaunchDarkly.Common
{
    /// <summary>
    /// Used internally to configure <see cref="DefaultEventProcessor"/>.
    /// </summary>
    internal interface IEventProcessorConfiguration
    {
        bool AllAttributesPrivate { get; }
        int EventCapacity { get; }
        TimeSpan EventFlushInterval { get; }
        int EventSamplingInterval { get; }
        Uri EventsUri { get; }
        TimeSpan HttpClientTimeout { get; }
        bool InlineUsersInEvents { get; }
        ISet<string> PrivateAttributeNames { get; }
        TimeSpan ReadTimeout { get; }
        TimeSpan ReconnectTime { get; }
        int UserKeysCapacity { get; }
        TimeSpan UserKeysFlushInterval { get; }
    }
}
