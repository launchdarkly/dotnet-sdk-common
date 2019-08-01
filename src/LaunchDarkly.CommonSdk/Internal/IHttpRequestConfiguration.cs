using System.Net.Http;

namespace LaunchDarkly.Common
{
    /// <summary>
    /// Used internally to describe all configuration properties that affect HTTP requests in general.
    /// </summary>
    internal interface IHttpRequestConfiguration
    {
        string HttpAuthorizationKey { get; }
        HttpClientHandler HttpClientHandler { get; }
    }
}
