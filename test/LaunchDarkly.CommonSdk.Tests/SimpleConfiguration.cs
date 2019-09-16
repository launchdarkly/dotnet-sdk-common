using System;
using System.Collections.Generic;
using System.Net.Http;

namespace LaunchDarkly.Common.Tests
{
    // Used in unit tests of common code - a minimal implementation of our configuration interfaces.
    class SimpleConfiguration :
        IEventProcessorConfiguration, IHttpRequestConfiguration, IStreamManagerConfiguration
    {
        internal string SdkKey { get; set; } = "SDK_KEY";
        internal Uri BaseUri { get; set; }
        internal Uri EventsUri { get; set; }
        internal Uri StreamUri { get; set; }
        internal bool Offline { get; set; }
        internal TimeSpan ReadTimeout { get; set; }
        internal TimeSpan ReconnectTime { get; set;  }
        internal int EventCapacity { get; set; } = 1000;
        internal TimeSpan EventFlushInterval { get; set; }
        internal int EventSamplingInterval { get; set; }
        internal bool AllAttributesPrivate { get; set; }
        internal ISet<string> PrivateAttributeNames { get; set; } = new HashSet<string>();
        internal int UserKeysCapacity { get; set; } = 1000;
        internal TimeSpan UserKeysFlushInterval { get; set; }
        internal bool InlineUsersInEvents { get; set; }
        internal TimeSpan HttpClientTimeout { get; set; } = TimeSpan.FromSeconds(30);
        internal HttpClientHandler HttpClientHandler { get; set; } = new HttpClientHandler();
        internal TimeSpan DiagnosticRecordingInterval { get; set; }
        internal bool DiagnosticOptOut { get; set; }
        internal string WrapperName { get; set; }
        internal string WrapperVersion { get; set; }
        internal IDiagnosticStore DiagnosticStore { get; set; }

        string IHttpRequestConfiguration.HttpAuthorizationKey { get { return SdkKey; } }
        Uri IEventProcessorConfiguration.EventsUri => EventsUri;
        TimeSpan IStreamManagerConfiguration.ReadTimeout => ReadTimeout;
        TimeSpan IStreamManagerConfiguration.ReconnectTime => ReconnectTime;
        TimeSpan IEventProcessorConfiguration.ReadTimeout => ReadTimeout;
        TimeSpan IEventProcessorConfiguration.ReconnectTime => ReconnectTime;
        int IEventProcessorConfiguration.EventCapacity => EventCapacity;
        TimeSpan IEventProcessorConfiguration.EventFlushInterval => EventFlushInterval;
        int IEventProcessorConfiguration.EventSamplingInterval => EventSamplingInterval;
        bool IEventProcessorConfiguration.AllAttributesPrivate => AllAttributesPrivate;
        ISet<string> IEventProcessorConfiguration.PrivateAttributeNames => PrivateAttributeNames;
        int IEventProcessorConfiguration.UserKeysCapacity => UserKeysCapacity;
        TimeSpan IEventProcessorConfiguration.UserKeysFlushInterval => UserKeysFlushInterval;
        bool IEventProcessorConfiguration.InlineUsersInEvents => InlineUsersInEvents;
        TimeSpan IStreamManagerConfiguration.HttpClientTimeout => HttpClientTimeout;
        TimeSpan IEventProcessorConfiguration.HttpClientTimeout => HttpClientTimeout;
        HttpClientHandler IHttpRequestConfiguration.HttpClientHandler => HttpClientHandler;
        TimeSpan IEventProcessorConfiguration.DiagnosticRecordingInterval => DiagnosticRecordingInterval;
        bool IEventProcessorConfiguration.DiagnosticOptOut => DiagnosticOptOut;
        string IHttpRequestConfiguration.WrapperName => WrapperName;
        string IHttpRequestConfiguration.WrapperVersion => WrapperVersion;
        IDiagnosticStore IEventProcessorConfiguration.DiagnosticStore => DiagnosticStore;
   }
}
