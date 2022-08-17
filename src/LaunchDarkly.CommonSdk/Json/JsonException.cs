using System;

namespace LaunchDarkly.Sdk.Json
{
    /// <summary>
    /// An exception that indicates a problem in processing of JSON data.
    /// </summary>
    /// <remarks>
    /// LaunchDarkly SDK methods that involve JSON parsing will throw this exception if the
    /// input is not valid as JSON, or if it is valid as JSON but does not conform to the expected
    /// schema. The exception message may provide details as to what was wrong with the input, but
    /// the exact format of the message (and the type of the inner exception, if any) may vary due
    /// to platform-specific differences in how JSON is processed.
    /// </remarks>
    public sealed class JsonException : Exception
    {
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="message">the exception message</param>
        public JsonException(string message) : base(message) { }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="message">the exception message</param>
        /// <param name="position">the offset where a parsing error occurred</param>
        public JsonException(string message, long position) : base(message + " at position " + position) { }

        /// <summary>
        /// Constructs a new instance based on another exception.
        /// </summary>
        /// <param name="innerException">the original exception</param>
        public JsonException(Exception innerException) : base("Error in JSON parsing", innerException) { }

        /// <summary>
        /// Constructs a new instance based on another exception.
        /// </summary>
        /// <param name="innerException">the original exception</param>
        /// <param name="position">the offset where a parsing error occurred</param>
        public JsonException(Exception innerException, long position) : base("Error in JSON parsing at position " + position, innerException) { }
    }
}
