using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace LaunchDarkly.Sdk.Internal.Stream
{
    // Interface for platform-specific implementations of the streaming connection,
    // called from StreamManager.
    internal interface IStreamProcessor
    {
        /// <summary>
        /// Handle a message from the stream. Implementations of this method should be async.
        /// </summary>
        /// <param name="streamManager">the StreamManager instance; this is passed so
        /// that you can set its Initialized property or call Restart if necessary</param>
        /// <param name="messageType">the SSE event type</param>
        /// <param name="messageData">the event data, as a string</param>
        /// <returns>nothing; implementations should be "async void"</returns>
        Task HandleMessage(StreamManager streamManager, string messageType, string messageData);
    }
}
