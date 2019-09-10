using System;
using System.Collections.Generic;

namespace LaunchDarkly.Common
{
    internal interface IDiagnosticStore {
        DiagnosticId DiagnosticId { get; }
        bool SendInitEvent { get; }
        Dictionary<string, Object> LastStats { get; }
        DateTime DataSince { get; }
        void IncrementDeduplicatedUsers();
        void IncrementDroppedEvents();
        Dictionary<string, Object> GetStatsAndReset(long eventsInQueue);
    }
}
