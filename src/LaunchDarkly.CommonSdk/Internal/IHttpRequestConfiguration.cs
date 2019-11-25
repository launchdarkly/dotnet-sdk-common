using System.Net.Http;

namespace LaunchDarkly.Sdk.Internal
{
    /// <summary>
    /// Used internally to describe all configuration properties that affect HTTP requests in general.
    /// </summary>
    internal interface IHttpRequestConfiguration
    {
        string HttpAuthorizationKey { get; }
        HttpMessageHandler HttpMessageHandler { get; }
    }
}
