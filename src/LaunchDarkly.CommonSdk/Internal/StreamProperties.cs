using System;
using System.Net.Http;

namespace LaunchDarkly.Common
{
    // Used by StreamManager to specify how to make a streaming connection.
    internal struct StreamProperties
    {
        /// <summary>
        /// The URI for the streaming connection. This is normally constructed by adding some
        /// URL path to the base StreamUri from the configuration. Note that this cannot
        /// change during the lifetime of a stream; if you want to reconnect to a different
        /// stream URI than before, you should dispose of the current StreamManager and
        /// IStreamProcessor and create new ones.
        /// </summary>
        public Uri StreamUri { get; }

        /// <summary>
        /// The HTTP method to use for the streaming connection.
        /// NOTE: Currently only GET is supported.
        /// </summary>
        public HttpMethod Method { get; }

        /// <summary>
        /// The request body to send if the method is not GET; null for no body.
        /// NOTE: Currently not used, since only GET is supported.
        /// </summary>
        public HttpContent RequestBody { get; }

        public StreamProperties(Uri uri, HttpMethod method, HttpContent body)
        {
            StreamUri = uri;
            Method = method;
            RequestBody = body;
        }
    }
}
