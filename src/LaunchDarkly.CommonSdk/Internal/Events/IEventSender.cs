using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using LaunchDarkly.Sdk.Interfaces;
using LaunchDarkly.Sdk.Internal.Helpers;

namespace LaunchDarkly.Sdk.Internal.Events
{
    internal interface IEventSender : IDisposable
    {
        Task<EventSenderResult> SendEventDataAsync(EventDataKind kind, string data, int eventCount);
    }

    internal enum EventDataKind
    {
        AnalyticsEvents,
        DiagnosticEvent
    };

    internal enum DeliveryStatus
    {
        Succeeded,
        Failed,
        FailedAndMustShutDown
    };

    internal struct EventSenderResult
    {
        internal DeliveryStatus Status { get; private set; }
        internal DateTime? TimeFromServer { get; private set; }

        internal EventSenderResult(DeliveryStatus status, DateTime? timeFromServer)
        {
            Status = status;
            TimeFromServer = timeFromServer;
        }
    }
}
