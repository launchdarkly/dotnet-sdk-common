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
        bool DiagnosticOptOut { get; }
        TimeSpan DiagnosticRecordingInterval { get; }
        IDiagnosticStore DiagnosticStore { get; }
        string DiagnosticUriPath { get; }
        int EventCapacity { get; }
        TimeSpan EventFlushInterval { get; }
        int EventSamplingInterval { get; }
        Uri EventsUri { get; }
        string EventsUriPath { get; }
        TimeSpan HttpClientTimeout { get; }
        bool InlineUsersInEvents { get; }
        ISet<string> PrivateAttributeNames { get; }
        TimeSpan ReadTimeout { get; }
        TimeSpan ReconnectTime { get; }
        int UserKeysCapacity { get; }
        TimeSpan UserKeysFlushInterval { get; }
    }
}
