using System;
using System.Linq;
using System.Threading.Tasks;
using LaunchDarkly.Sdk.Internal.Helpers;
using WireMock;
using WireMock.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

using static LaunchDarkly.Sdk.TestUtil;

namespace LaunchDarkly.Sdk.Internal.Events
{
    public class DefaultEventSenderTest
    {
        private const string HttpDateFormat = "ddd, dd MMM yyyy HH:mm:ss 'GMT'";
        private const string AuthKey = "fake-sdk-key";
        private const string EventsUriPath = "/post-events-here";
        private const string DiagnosticUriPath = "/post-diagnostic-here";
        private const string FakeData = "{\"things\":[]}";

        private async Task WithServerAndSender(Func<WireMockServer, DefaultEventSender, Task> a)
        {
            var server = NewServer();
            try
            {
                using (var es = MakeSender(server))
                {
                    await a(server, es);
                }
            }
            finally
            {
                server.Stop();
            }
        }

        private DefaultEventSender MakeSender(WireMockServer server)
        {
            var config = new SimpleConfiguration();
            config.SdkKey = AuthKey;
            if (server != null)
            {
                config.EventsUri = new Uri(new Uri(server.Urls[0]), EventsUriPath);
                config.DiagnosticUri = new Uri(new Uri(server.Urls[0]), DiagnosticUriPath);
            }
            var httpClient = Util.MakeHttpClient(config, SimpleClientEnvironment.Instance);
            return new DefaultEventSender(httpClient, config);
        }

        [Fact]
        public async void AnalyticsEventDataIsSentSuccessfully()
        {
            await WithServerAndSender(async (server, es) =>
            {
                server.Given(AnalyticsEventRequest()).RespondWith(OkResponse());

                var result = await es.SendEventDataAsync(EventDataKind.AnalyticsEvents, FakeData, 1);

                Assert.Equal(DeliveryStatus.Succeeded, result.Status);
                Assert.NotNull(result.TimeFromServer);
                // Note that we can't provide a specific value for the HTTP Date header and check it against
                // result.TimeFromServer, because on some platforms the underlying Owin implementation of
                // WireMock.Net doesn't allow customizing Date.

                var request = GetLastRequest(server);
                Assert.Equal(AuthKey, request.Headers["Authorization"][0]);
                Assert.NotNull(request.Headers["X-LaunchDarkly-Payload-ID"][0]);
                Assert.Equal("3", request.Headers["X-LaunchDarkly-Event-Schema"][0]);
            });
        }

        [Fact]
        public async void NewPayloadIdIsGeneratedForEachPayload()
        {
            await WithServerAndSender(async (server, es) =>
            {
                server.Given(AnalyticsEventRequest()).RespondWith(OkResponse());

                var result1 = await es.SendEventDataAsync(EventDataKind.AnalyticsEvents, FakeData, 1);
                var result2 = await es.SendEventDataAsync(EventDataKind.AnalyticsEvents, FakeData, 1);

                Assert.Equal(DeliveryStatus.Succeeded, result1.Status);
                Assert.Equal(DeliveryStatus.Succeeded, result2.Status);

                var logEntries = server.LogEntries.ToList();
                Assert.Equal(2, logEntries.Count);
                Assert.NotEqual(
                    logEntries[0].RequestMessage.Headers["X-LaunchDarkly-Payload-ID"][0],
                    logEntries[1].RequestMessage.Headers["X-LaunchDarkly-Payload-ID"][0]);
            });
        }

        [Fact]
        public async void DiagnosticEventDataIsSentSuccessfully()
        {
            await WithServerAndSender(async (server, es) =>
            {
                server.Given(DiagnosticEventRequest()).RespondWith(OkResponse());
                var result = await es.SendEventDataAsync(EventDataKind.DiagnosticEvent, FakeData, 1);

                Assert.Equal(DeliveryStatus.Succeeded, result.Status);
                Assert.NotNull(result.TimeFromServer);

                var request = GetLastRequest(server);
                Assert.Equal(AuthKey, request.Headers["Authorization"][0]);
                Assert.False(request.Headers.ContainsKey("X-LaunchDarkly-Payload-ID"));
                Assert.False(request.Headers.ContainsKey("X-LaunchDarkly-Event-Schema"));
            });
        }

        [Theory]
        [InlineData(400)]
        [InlineData(408)]
        [InlineData(429)]
        [InlineData(500)]
        private async void VerifyRecoverableHttpError(int status)
        {
            await WithServerAndSender(async (server, es) =>
            {
                server.Given(AnalyticsEventRequest())
                    .InScenario("Send Retry")
                    .WillSetStateTo("Retry")
                    .RespondWith(Response.Create().WithStatusCode(status));

                server.Given(AnalyticsEventRequest())
                    .InScenario("Send Retry")
                    .WhenStateIs("Retry")
                    .RespondWith(OkResponse());

                var result = await es.SendEventDataAsync(EventDataKind.AnalyticsEvents, FakeData, 1);
                Assert.Equal(DeliveryStatus.Succeeded, result.Status);
                Assert.NotNull(result.TimeFromServer);

                var logEntries = server.LogEntries.ToList();
                Assert.Equal(2, logEntries.Count);
                Assert.Equal(
                    logEntries[0].RequestMessage.BodyAsJson,
                    logEntries[1].RequestMessage.BodyAsJson);
                Assert.Equal(
                    logEntries[0].RequestMessage.Headers["X-LaunchDarkly-Payload-ID"][0],
                    logEntries[1].RequestMessage.Headers["X-LaunchDarkly-Payload-ID"][0]);
            });
        }

        [Theory]
        [InlineData(400)]
        [InlineData(408)]
        [InlineData(429)]
        [InlineData(500)]
        private async void VerifyRecoverableHttpErrorIsOnlyRetriedOnce(int status)
        {
            await WithServerAndSender(async (server, es) =>
            {
                server.Given(AnalyticsEventRequest())
                    .InScenario("Send Retry")
                    .WillSetStateTo("Retry1")
                    .RespondWith(Response.Create().WithStatusCode(status));

                server.Given(AnalyticsEventRequest())
                    .InScenario("Send Retry")
                    .WhenStateIs("Retry1")
                    .WillSetStateTo("Retry2")
                    .RespondWith(Response.Create().WithStatusCode(status));

                server.Given(AnalyticsEventRequest())
                    .InScenario("Send Retry")
                    .WhenStateIs("Retry2")
                    .RespondWith(OkResponse());

                var result = await es.SendEventDataAsync(EventDataKind.AnalyticsEvents, FakeData, 1);
                Assert.Equal(DeliveryStatus.Failed, result.Status);
                Assert.Null(result.TimeFromServer);

                var logEntries = server.LogEntries.ToList();
                Assert.Equal(2, logEntries.Count);
                Assert.Equal(
                    logEntries[0].RequestMessage.BodyAsJson,
                    logEntries[1].RequestMessage.BodyAsJson);
            });
        }

        [Theory]
        [InlineData(401)]
        [InlineData(403)]
        private async void VerifyUnrecoverableHttpError(int status)
        {
            await WithServerAndSender(async (server, es) =>
            {
                server.Given(AnalyticsEventRequest()).RespondWith(Response.Create().WithStatusCode(status));

                var result = await es.SendEventDataAsync(EventDataKind.AnalyticsEvents, FakeData, 1);
                Assert.Equal(DeliveryStatus.FailedAndMustShutDown, result.Status);
                Assert.Null(result.TimeFromServer);

                var logEntries = server.LogEntries.ToList();
                Assert.Equal(1, logEntries.Count);
            });
        }

        private IResponseBuilder OkResponse()
        {
            return Response.Create().WithStatusCode(202);
        }

        private IRequestBuilder AnalyticsEventRequest()
        {
            return Request.Create().WithPath(EventsUriPath).UsingPost();
        }

        private IRequestBuilder DiagnosticEventRequest()
        {
            return Request.Create().WithPath(DiagnosticUriPath).UsingPost();
        }

        private RequestMessage GetLastRequest(WireMockServer server)
        {
            foreach (LogEntry le in server.LogEntries)
            {
                return le.RequestMessage;
            }
            Assert.True(false, "Did not receive a post request");
            return null;
        }
    }
}
