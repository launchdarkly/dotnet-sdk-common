using System;
using System.Collections.Immutable;

namespace LaunchDarkly.Sdk.Internal.Events
{
    /// <summary>
    /// Used internally to configure <see cref="DefaultEventProcessor"/>.
    /// </summary>
    internal interface IEventProcessorConfiguration
    {
        bool AllAttributesPrivate { get; }
        int EventCapacity { get; }
        TimeSpan EventFlushInterval { get; }
        Uri EventsUri { get; }
        TimeSpan HttpClientTimeout { get; }
        bool InlineUsersInEvents { get; }
        IImmutableSet<string> PrivateAttributeNames { get; }
        TimeSpan ReadTimeout { get; }
        TimeSpan ReconnectTime { get; }
        int UserKeysCapacity { get; }
        TimeSpan UserKeysFlushInterval { get; }
    }
}
