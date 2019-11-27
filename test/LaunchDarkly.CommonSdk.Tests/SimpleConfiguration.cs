using System;
using System.Collections.Immutable;
using System.Net.Http;
using LaunchDarkly.Sdk.Internal;
using LaunchDarkly.Sdk.Internal.Events;
using LaunchDarkly.Sdk.Internal.Stream;

namespace LaunchDarkly.Sdk
{
    // Used in unit tests of common code - a minimal implementation of our configuration interfaces.
    public class SimpleConfiguration :
        IEventProcessorConfiguration, IHttpRequestConfiguration, IStreamManagerConfiguration
    {
        public string SdkKey { get; set; } = "SDK_KEY";
        public string HttpAuthorizationKey => SdkKey;
        public Uri BaseUri { get; set; }
        public Uri EventsUri { get; set; }
        public Uri StreamUri { get; set; }
        public bool Offline { get; set; }
        public TimeSpan ReadTimeout { get; set; }
        public TimeSpan ReconnectTime { get; set;  }
        public int EventCapacity { get; set; } = 1000;
        public TimeSpan EventFlushInterval { get; set; }
        public TimeSpan EventQueueFrequency => EventFlushInterval;
        public bool AllAttributesPrivate { get; set; }
        public IImmutableSet<string> PrivateAttributeNames { get; set; } = ImmutableHashSet.Create<string>();
        public int UserKeysCapacity { get; set; } = 1000;
        public TimeSpan UserKeysFlushInterval { get; set; }
        public bool InlineUsersInEvents { get; set; }
        public TimeSpan HttpClientTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public HttpMessageHandler HttpMessageHandler { get; set; }
        public HttpClientHandler HttpClientHandler { get; set; } = new HttpClientHandler();
        public TimeSpan DiagnosticRecordingInterval { get; set; }
        public Uri DiagnosticUri { get; set; }
        public string WrapperName { get; set; }
        public string WrapperVersion { get; set; }

        public Exception TranslateHttpException(Exception e) =>
            TranslateHttpExceptionFn is null ? e : TranslateHttpExceptionFn(e);

        public Func<Exception, Exception> TranslateHttpExceptionFn { get; set; }
   }
}
