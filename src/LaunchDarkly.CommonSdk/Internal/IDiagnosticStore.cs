using System;
using System.Collections.Generic;
using LaunchDarkly.Client;

namespace LaunchDarkly.Common
{
    /// <summary>
    /// This interface is for providing to the DefaultEventProcessor and StreamManager. It is
    /// responsible for providing diagnostic data specific to the platform implementation for the
    /// DefaultEventManager to send to LaunchDarkly. Periodic diagnostic events include data
    /// collected during operation (stream initializations and counters) that the IDiagnosticStore
    /// implementation stores until CreateEventAndReset is called to retrieve a full event including
    /// the collected data and diagnostic session identifier.
    /// </summary>
    internal interface IDiagnosticStore
    {
        /// <summary>
        /// The last time the periodic data was reset for the current diagnosticId. This may be from
        /// before the SDK initialized if periodic diagnostic data has been persisted from a
        /// previous initialization, but the session was not considered ended (meaning the same
        /// diagnostic id) when the current initialization occurred. For the server SDK this will be
        /// the time of initialization.
        /// </summary>
        DateTime DataSince { get; }
        /// <summary>
        /// An event to be sent for a new diagnostic id, if the initialization represented a new
        /// diagnostic id.
        /// </summary>
        LdValue InitEvent { get; }
        /// <summary>
        /// Persisted periodic diagnostic data from a previous initialization. This should be set
        /// with the data from the previous diagnostic id if the initialization caused a switch of
        /// diagnostic id and there is periodic diagnostics data available for the previous id.
        /// </summary>
        LdValue PersistedUnsentEvent { get; }
        /// <summary>
        /// Called when the user deduplicator prevents a user from being indexed.
        /// </summary>
        void IncrementDeduplicatedUsers();
        /// <summary>
        /// Called when an event is dropped due to a full event buffer.
        /// </summary>
        void IncrementDroppedEvents();
        /// <summary>
        /// Called when a stream init completes
        /// </summary>
        /// <param name="timestamp">The time at which the stream began attempted initialization. </param>
        /// <param name="duration">The duration of the stream initialization attempt. </param>
        /// <param name="failed">True if the initialization failed, false otherwise. </param>
        void AddStreamInit(DateTime timestamp, TimeSpan duration, bool failed);
        /// <summary>
        /// Called to generate a periodic diagnostic event, resetting the store counts and stream
        /// initializations.
        /// </summary>
        /// <param name="eventsInQueue">The current number of events in the event buffer</param>
        /// <returns>A dictionary representing the periodic diagnostic event</returns>
        LdValue CreateEventAndReset(long eventsInQueue);
    }
}
