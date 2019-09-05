using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Common
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

        internal class Statistics : DiagnosticEvent
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

        internal class Init : DiagnosticEvent
        {
            internal readonly DiagnosticSdk Sdk;
            internal readonly Dictionary<String, Object> Configuration;
            internal readonly DiagnosticPlatform Platform = new DiagnosticPlatform();

            internal Init(long creationDate, DiagnosticId diagnosticId, Dictionary<String, Object> Configuration)
                : base("diagnostic-init", creationDate, diagnosticId)
            {
                Sdk = new DiagnosticSdk();
                this.Configuration = Configuration;
            }
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
