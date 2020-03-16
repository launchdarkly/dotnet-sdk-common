using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using LaunchDarkly.Sdk.Interfaces;
using LaunchDarkly.Sdk.Internal.Helpers;

namespace LaunchDarkly.Sdk.Internal.Events
{
    internal sealed class DefaultEventSender : IEventSender
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DefaultEventSender));
        private const int MaxAttempts = 2;
        private const string CurrentSchemaVersion = "3";

        private readonly HttpClient _httpClient;
        private readonly Uri _eventsUri;
        private readonly Uri _diagnosticUri;
        private readonly TimeSpan _timeout;

        internal DefaultEventSender(HttpClient httpClient, IEventProcessorConfiguration config)
        {
            _httpClient = httpClient;
            _eventsUri = config.EventsUri;
            _diagnosticUri = config.DiagnosticUri;
            _timeout = config.HttpClientTimeout;
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }
        }

    	public async Task<EventSenderResult> SendEventDataAsync(EventDataKind kind, string data, int eventCount)
        {
            Uri uri;
            string description;
            string payloadId;

            if (kind == EventDataKind.DiagnosticEvent)
            {
                uri = _diagnosticUri;
                description = "diagnostic event";
                payloadId = null;
            }
            else
            {
                uri = _eventsUri;
                description = string.Format("{0} event(s)", eventCount);
                payloadId = Guid.NewGuid().ToString();
            }

            Log.DebugFormat("Submitting {0} to {1} with json: {2}", description, uri.AbsoluteUri, data);

            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                if (attempt > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                using (var cts = new CancellationTokenSource(_timeout))
                {
                    string errorMessage = null;
                    bool canRetry = false;
                    bool mustShutDown = false;

                    try
                    {
                        using (var request = PrepareRequest(uri, payloadId))
                        using (var stringContent = new StringContent(data, Encoding.UTF8, "application/json"))
                        {
                            request.Content = stringContent;
                            Stopwatch timer = new Stopwatch();
                            using (var response = await _httpClient.SendAsync(request, cts.Token))
                            {
                                timer.Stop();
                                Log.DebugFormat("Event delivery took {0} ms, response status {1}",
                                    timer.ElapsedMilliseconds, (int)response.StatusCode);
                                if (response.IsSuccessStatusCode)
                                {
                                    DateTimeOffset? respDate = response.Headers.Date;
                                    return new EventSenderResult(DeliveryStatus.Succeeded,
                                        respDate.HasValue ? (DateTime?)respDate.Value.DateTime : null);
                                }
                                else
                                {
                                    errorMessage = Util.HttpErrorMessageBase((int)response.StatusCode);
                                    canRetry = Util.IsHttpErrorRecoverable((int)response.StatusCode);
                                    mustShutDown = !canRetry;
                                }
                            }
                        }
                    }
                    catch (TaskCanceledException e)
                    {
                        if (e.CancellationToken == cts.Token)
                        {
                            // Indicates the task was cancelled deliberately somehow; in this case don't retry
                            Log.Warn("Event sending task was cancelled");
                            return new EventSenderResult(DeliveryStatus.Failed, null);
                        }
                        else
                        {
                            // Otherwise this was a request timeout.
                            errorMessage = "Timed out";
                            canRetry = true;
                        }
                    }
                    catch (Exception e)
                    {
                        errorMessage = string.Format("Error ({0})", Util.DescribeException(e));
                        canRetry = true;
                    }
                    string nextStepDesc = canRetry ?
                        (attempt == MaxAttempts - 1 ? "will not retry" : "will retry after one second") :
                        "giving up permanently";
                    Log.WarnFormat(errorMessage + " sending {0}; {1}", description, nextStepDesc);
                    if (mustShutDown)
                    {
                        return new EventSenderResult(DeliveryStatus.FailedAndMustShutDown, null);
                    }
                    if (!canRetry)
                    {
                        break;
                    }
                }
            }
            return new EventSenderResult(DeliveryStatus.Failed, null);
        }

        private HttpRequestMessage PrepareRequest(Uri uri, string payloadId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            if (payloadId != null) // payloadId is provided for regular analytics events payloads, not for diagnostic events
            {
                request.Headers.Add("X-LaunchDarkly-Payload-ID", payloadId);
                request.Headers.Add("X-LaunchDarkly-Event-Schema", CurrentSchemaVersion);
            }
            return request;
        }
    }
}
