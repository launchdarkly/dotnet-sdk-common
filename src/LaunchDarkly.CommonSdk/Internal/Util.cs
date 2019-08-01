using System;
using System.Collections.Generic;
using System.Net.Http;

namespace LaunchDarkly.Common
{
    internal static class Util
    {
        internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        internal static Dictionary<string, string> GetRequestHeaders(IHttpRequestConfiguration config,
            ClientEnvironment env)
        {
            return new Dictionary<string, string> {
                { "Authorization", config.HttpAuthorizationKey },
                { "User-Agent", env.UserAgentType + "/" + env.VersionString }
            };
        }

        internal static HttpClient MakeHttpClient(IHttpRequestConfiguration config, ClientEnvironment env)
        {
            var httpClient = new HttpClient(handler: config.HttpClientHandler, disposeHandler: false);
            foreach (var h in GetRequestHeaders(config, env))
            {
                httpClient.DefaultRequestHeaders.Add(h.Key, h.Value);
            }
            return httpClient;
        }

        internal static long GetUnixTimestampMillis(DateTime dateTime)
        {
            return (long) (dateTime - UnixEpoch).TotalMilliseconds;
        }

        internal static string ExceptionMessage(Exception e)
        {
            var msg = e.Message;
            if (e.InnerException != null)
            {
                return msg + " with inner exception: " + e.InnerException.Message;
            }
            return msg;
        }

        // Returns true if this type of error could be expected to eventually resolve itself,
        // or false if it indicates a configuration problem or client logic error such that the
        // client should give up on making any further requests.
        internal static bool IsHttpErrorRecoverable(int status)
        {
            if (status >= 400 && status <= 499)
            {
                return (status == 400) || (status == 408) || (status == 429);
            }
            return true;
        }

        internal static string HttpErrorMessage(int status, string context, string recoverableMessage)
        {
            return string.Format("HTTP error {0}{1} for {2} - {3}",
                status,
                (status == 401 || status == 403) ? " (invalid SDK key)" : "",
                context,
                IsHttpErrorRecoverable(status) ? recoverableMessage : "giving up permanently"
                );
        }

        internal static HashCodeBuilder Hash()
        {
            return new HashCodeBuilder();
        }
    }

    internal class HashCodeBuilder
    {
        private int value = 0;
        public int Value => value;

        public HashCodeBuilder With(object o)
        {
            value = value * 17 + (o == null ? 0 : o.GetHashCode());
            return this;
        }
    }
}