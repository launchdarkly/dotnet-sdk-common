using System;

namespace LaunchDarkly.Sdk.Interfaces
{
    /// <summary>
    /// Interface for an object that can send or store analytics events.
    /// </summary>
    /// <remarks>
    /// By default, the SDK uses its own default implementation that sends events to LaunchDarkly.
    /// You should not need to implement this interface except for testing purposes, or if you are
    /// implementing a custom mechanism for event processing.
    /// </remarks>
    public interface IEventProcessor : IDisposable
    {
        /// <summary>
        /// Sets whether the event processor should avoid trying to use the network.
        /// </summary>
        /// <remarks>
        /// In offline mode, a flush (either an automatic one, or calling <see cref="Flush"/> explicitly)
        /// is a no-op: any events currently in the queue will stay there. Events can continue to be added
        /// to the queue, up to its configured capacity.
        /// </remarks>
        /// <param name="offline">true if we should be offline</param>
        void SetOffline(bool offline);

        /// <summary>
        /// Processes an event. This method is asynchronous; the event may be sent later in the background
        /// at an interval set by the configuration property <c>EventFlushInterval</c>, or due to a call to
        /// <see cref="Flush"/>.
        /// </summary>
        /// <param name="evt">the event</param>
        void SendEvent(Event evt);

        /// <summary>
        /// Specifies that any buffered events should be sent as soon as possible, rather than waiting for
        /// the next flush interval. This method is asynchronous, so events may still not be sent until a
        /// later time. However, calling <see cref="IDisposable.Dispose"/> will synchronously deliver any
        /// events that were not yet delivered prior to shutting down.
        /// </summary>
        void Flush();
    }

    internal sealed class NullEventProcessor : IEventProcessor
    {
        void IEventProcessor.SetOffline(bool offline)
        { }

        void IEventProcessor.SendEvent(Event eventToLog)
        { }

        void IEventProcessor.Flush()
        { }

        void IDisposable.Dispose()
        { }
    }
}