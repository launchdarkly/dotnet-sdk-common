using System;

namespace LaunchDarkly.Common
{
    /// <summary>
    /// Common interface for the Launchdarkly .NET and Xamarin clients. Most client methods are defined
    /// in the platform-specific client classes.
    /// </summary>
    [Obsolete("This interface will be removed in a future version, since .NET and Xamarin may not always have equivalent APIs. Use the specific interfaces for each SDK instead.")]
    public interface ILdCommonClient : IDisposable
    {
        /// <summary>
        /// Tests whether the client is being used in offline mode.
        /// </summary>
        /// <returns>true if the client is offline</returns>
        bool IsOffline();

        /// <summary>
        /// Flushes all pending events.
        /// </summary>
        void Flush();

        /// <summary>
        /// Returns the current version number of the LaunchDarkly client.
        /// </summary>
        Version Version { get; }
    }
}
