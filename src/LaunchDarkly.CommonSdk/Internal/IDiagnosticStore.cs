using System;
using System.Collections.Generic;

namespace LaunchDarkly.Common
{
    internal interface IDiagnosticStore {
        // Needed to schedule the first periodic diagnostic event delay
        DateTimeOffset DataSince { get; }
        Dictionary<string, Object> InitEvent { get; }
        // Saved periodic diagnostic event, used by mobile platforms
        Dictionary<string, Object> LastStats { get; }
        void IncrementDeduplicatedUsers();
        void IncrementDroppedEvents();
        Dictionary<string, Object> GetStatsAndReset(long eventsInQueue);
    }
}
