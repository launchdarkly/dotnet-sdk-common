using System;
using System.Net.Http;

namespace LaunchDarkly.Sdk.Internal.Stream
{
    /// <summary>
    /// Used internally to configure <see cref="StreamManager"/>.
    /// </summary>
    internal interface IStreamManagerConfiguration : IHttpRequestConfiguration
    {
        TimeSpan ReadTimeout { get; }
        TimeSpan ReconnectTime { get; }
        TimeSpan HttpClientTimeout { get; }
        /// <summary>
        /// For mobile platforms where HTTP requests might throw platform-specific exceptions, this method should
        /// translate them to standard .NET exceptions. Otherwise it should just return the same exception.
        /// </summary>
        Exception TranslateHttpException(Exception e);
    }
}
