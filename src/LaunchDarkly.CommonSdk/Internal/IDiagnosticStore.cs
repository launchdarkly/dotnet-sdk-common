using System;
using System.Collections.Generic;

namespace LaunchDarkly.Common
{
    internal interface IDiagnosticStore {
        // Needed to schedule the first periodic diagnostic event delay
        DateTime DataSince { get; }
        IReadOnlyDictionary<string, object> InitEvent { get; }
        // Saved periodic diagnostic event, used by mobile platforms
        IReadOnlyDictionary<string, object> LastStats { get; }
        void IncrementDeduplicatedUsers();
        void IncrementDroppedEvents();
        void AddStreamInit(long timestamp, int durationMs, bool failed);
        IReadOnlyDictionary<string, object> CreateEventAndReset(long eventsInQueue);
    }
}
