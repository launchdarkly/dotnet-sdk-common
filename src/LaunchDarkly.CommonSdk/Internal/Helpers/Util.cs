using System;
using System.Collections.Generic;
using System.Net.Http;

namespace LaunchDarkly.Sdk.Internal.Helpers
{
    internal static class Util
    {
        internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        internal static Dictionary<string, string> GetRequestHeaders(IHttpRequestConfiguration config,
            ClientEnvironment env)
        {
            Dictionary<string, string> headers =  new Dictionary<string, string> {
                { "Authorization", config.HttpAuthorizationKey },
                { "User-Agent", env.UserAgentType + "/" + env.VersionString }
            };

            if (config.WrapperName != null)
            {
                string wrapperVersion = "";
                if (config.WrapperVersion != null)
                {
                    wrapperVersion = "/" + config.WrapperVersion;
                }
                headers.Add("X-LaunchDarkly-Wrapper", config.WrapperName + wrapperVersion);
            }

            return headers;
        }

        internal static HttpClient MakeHttpClient(IHttpRequestConfiguration config, ClientEnvironment env)
        {
            var httpClient = config.HttpMessageHandler is null ?
                new HttpClient() :
                new HttpClient(config.HttpMessageHandler, false);
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

        internal static T Clamp<T>(T value, T min, T max)
            where T : IComparable
        {
            if (value.CompareTo(min) < 0) return min;
            if (value.CompareTo(max) > 0) return max;
            return value;
        }

        // Differs from e.ToString() by not including the stacktrace
        internal static string DescribeException(Exception e)
        {
            return (e.Message is null && e.InnerException is null) ? e.GetType().Name :
                (e.GetType().Name + " " + ExceptionMessage(e));
        }

        internal static string ExceptionMessage(Exception e)
        {
            var msg = e.Message;
            if (e.InnerException != null)
            {
                return msg + " (caused by: " + e.InnerException.Message + ")";
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
            return string.Format("{0} for {1} - {2}",
                HttpErrorMessageBase(status),
                context,
                IsHttpErrorRecoverable(status) ? recoverableMessage : "giving up permanently"
                );
        }

        internal static string HttpErrorMessageBase(int status)
        {
            return string.Format("HTTP error {0}{1}",
                status,
               (status == 401 || status == 403) ? " (invalid SDK key)" : "");
        }

        internal static HashCodeBuilder Hash()
        {
            return new HashCodeBuilder();
        }
    }

    internal struct HashCodeBuilder
    {
        private readonly int _value;
        public int Value => _value;
        
        internal HashCodeBuilder(int value)
        {
            _value = value;
        }

        public HashCodeBuilder With(object o)
        {
            return new HashCodeBuilder(_value * 17 + (o == null ? 0 : o.GetHashCode()));
        }
    }
}
