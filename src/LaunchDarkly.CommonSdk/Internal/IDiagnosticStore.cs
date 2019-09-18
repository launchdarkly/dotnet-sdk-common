using System;
using System.Collections.Generic;

namespace LaunchDarkly.Common
{
    internal interface IDiagnosticStore {
        // Needed to schedule the first periodic diagnostic event delay
        DateTime DataSince { get; }
        Dictionary<string, object> InitEvent { get; }
        // Saved periodic diagnostic event, used by mobile platforms
        Dictionary<string, object> LastStats { get; }
        void IncrementDeduplicatedUsers();
        void IncrementDroppedEvents();
        void IncrementStreamReconnections();
        Dictionary<string, object> CreateEventAndReset(long eventsInQueue);
    }
}
