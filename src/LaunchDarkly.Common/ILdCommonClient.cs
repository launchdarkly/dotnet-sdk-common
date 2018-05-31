using System;
using LaunchDarkly.Client;

namespace LaunchDarkly.Common
{
    /// <summary>
    /// Common interface for the Launchdarkly .NET and Xamarin clients. Most client methods are defined
    /// in the platform-specific client classes.
    /// </summary>
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
        /// Registers the user.
        /// </summary>
        /// <param name="user">the user to register</param>
        void Identify(User user);

        /// <summary>
        /// Returns the current version number of the LaunchDarkly client.
        /// </summary>
        Version Version { get; }
    }
}
