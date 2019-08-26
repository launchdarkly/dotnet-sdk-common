using System;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Client
{
    abstract class DiagnosticEvent
    {
        internal readonly string Kind;
        internal readonly long CreationDate;
        internal readonly DiagnosticId Id;

        internal DiagnosticEvent(string kind, long creationDate, DiagnosticId diagnosticId)
        {
            Kind = kind;
            CreationDate = creationDate;
            Id = diagnosticId;
        }
    }

    class Statistics : DiagnosticEvent
    {
        internal readonly long DataSinceDate;
        internal readonly long DroppedEvents;
        internal readonly long DeduplicatedUsers;
        internal readonly long EventsInQueue;

        internal Statistics(long creationDate, DiagnosticId diagnosticId,
                            long dataSinceDate, long droppedEvents,
                            long deduplicatedUsers, long eventsInQueue)
            : base("diagnostic", creationDate, diagnosticId)
        {
            DataSinceDate = dataSinceDate;
            DroppedEvents = droppedEvents;
            DeduplicatedUsers = deduplicatedUsers;
            EventsInQueue = eventsInQueue;
        }
    }

    class Init : DiagnosticEvent
    {
        internal readonly DiagnosticSdk Sdk;
        internal readonly DiagnosticConfiguration Configuration;
        internal readonly DiagnosticPlatform Platform = new DiagnosticPlatform();

        internal Init(long creationDate, DiagnosticId diagnosticId)
            : base("diagnostic-init", creationDate, diagnosticId)
        {
            Sdk = new DiagnosticSdk();
            Configuration = new DiagnosticConfiguration();
        }
    }

    class DiagnosticConfiguration {
        internal readonly Uri BaseURI;
        internal readonly Uri EventsURI;
        internal readonly Uri StreamURI;
        internal readonly int EventsCapacity;
        internal readonly int ConnectTimeoutMillis;
        internal readonly int SocketTimeoutMillis;
        internal readonly long EventsFlushIntervalMillis;
        internal readonly bool UsingProxy;
        internal readonly bool UsingProxyAuthenticator;
        internal readonly bool StreamingDisabled;
        internal readonly bool UsingRelayDaemon;
        internal readonly bool offline;
        internal readonly bool AllAttributesPrivate;
        internal readonly bool EventReportingDisabled;
        internal readonly long PollingIntervalMillis;
        internal readonly long StartWaitMillis;
        internal readonly int SamplingInterval;
        internal readonly long ReconnectTimeMillis;
        internal readonly int UserKeysCapacity;
        internal readonly long UserKeysFlushIntervalMillis;
        internal readonly bool InlineUsersInEvents;
        internal readonly int DiagnosticRecordingIntervalMillis;
        internal readonly string FeatureStore;
        
        internal DiagnosticConfiguration()
        {

        }
    }

    class DiagnosticSdk
    {
        internal static readonly string Name = "dotnet-server-sdk";

        internal DiagnosticSdk()
        {

        }
    }
    
    class DiagnosticPlatform
    {
        internal static readonly string Name = "DotNet";

        internal DiagnosticPlatform()
        {

        }
    }
}
