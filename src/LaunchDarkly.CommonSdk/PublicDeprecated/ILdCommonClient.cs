using System;

namespace LaunchDarkly.Common
{
    /// <summary>
    /// Common interface for the Launchdarkly .NET and Xamarin clients.
    /// </summary>
    /// <remarks>
    /// This interface will be removed in a future version, since .NET and Xamarin may
    /// not always have equivalent APIs. Use the specific interfaces for each SDK instead.
    /// </remarks>
    [Obsolete("This interface will be removed in a future version, since .NET and Xamarin may not always have equivalent APIs. Use the specific interfaces for each SDK instead.")]
    public interface ILdCommonClient : IDisposable
    {
        /// <summary>
        /// Tests whether the client is configured to be offline.
        /// </summary>
        /// <returns>true if using an offline configuration</returns>
        bool IsOffline();

        /// <summary>
        /// Tells the client that all pending analytics events should be delivered as soon as possible.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When the LaunchDarkly client generates analytics events (from flag evaluations, or from
        /// the <c>Identify</c> or <c>Track</c> methods), they are queued on a worker thread. The event
        /// thread normally sends all queued events to LaunchDarkly at regular intervals, controlled by
        /// the <see cref="IBaseConfiguration.EventFlushInterval"/> option. Calling <see cref="Flush"/>
        /// triggers a send without waiting for the next interval.
        /// </para>
        /// <para>
        /// Flushing is asynchronous, so this method will return before it is complete. However, if you
        /// shut down the client with <see cref="IDisposable.Dispose()"/>, events are guaranteed to be
        /// sent before that method returns.
        /// </para>
        /// </remarks>
        void Flush();

        /// <summary>
        /// The current version string of the SDK.
        /// </summary>
        Version Version { get; }
    }
}
