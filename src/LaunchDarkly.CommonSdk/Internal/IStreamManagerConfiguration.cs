using System;
using System.Net.Http;

namespace LaunchDarkly.Common
{
    /// <summary>
    /// Used internally to configure <see cref="StreamManager"/>.
    /// </summary>
    internal interface IStreamManagerConfiguration : IHttpRequestConfiguration
    {
        TimeSpan ReadTimeout { get; }
        TimeSpan ReconnectTime { get; }
        TimeSpan HttpClientTimeout { get; }
    }
}
