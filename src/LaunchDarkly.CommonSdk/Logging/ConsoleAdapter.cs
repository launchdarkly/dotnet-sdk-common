using System;
using System.Text;
using Common.Logging;
using Common.Logging.Configuration;
using Common.Logging.Simple;

namespace LaunchDarkly.Sdk.Logging
{
    /// <summary>
    /// An adapter that causes <c>Common.Logging</c> to log to standard output.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is equivalent to the class Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter. That class is provided
    /// in some target frameworks of the Common.Logging package, but not in all of them, so for maximum compatibility
    /// you can use this class instead. The only differences from the Common.Logging class are that it adds a
    /// constructor overload for specifing only the log level, and that it supports a <c>useStandardError</c> parameter
    /// for using the standard error stream instead of standard output.
    /// </para>
    /// <para>
    /// The LaunchDarkly .NET SDK uses Common.Logging as a logging abstraction because it supports many logging
    /// frameworks, allowing the SDK log output to be integrated into application logs if the application uses any
    /// framework supported by Common.Logging. However, if you are not using such a framework or if you simply want
    /// to set up an application quickly without worrying about logging, this adapter may be more convenient.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    ///     // To programmatically configure console logging, at INFO level:
    ///     Common.Logging.LogManager.Adapter = new ConsoleAdapter(Common.Logging.LogLevel.Info);
    ///     
    ///     // To configure console logging in app.config (ASP.NET):
    ///     &lt;configSections&gt;
    ///       &lt;sectionGroup name="common"&gt;
    ///         &lt;section name="logging"
    ///           type="Common.Logging.ConfigurationSectionHandler, Common.Logging"
    ///           requirePermission="false"/&gt;
    ///       &lt;/sectionGroup&gt;
    ///     &lt;/configSection&gt;
    ///     &lt;common&gt;
    ///       &lt;logging&gt;
    ///         &lt;factoryAdapter type="LaunchDarkly.Logging.ConsoleAdapter, LaunchDarkly.CommonSdk.StrongName"&gt;
    ///           &lt;arg key="level" value="Info"/&gt;
    ///           &lt;arg key="dateTimeFormat" value="yyyy/MM/dd HH:mm:ss.fff"/&gt;
    ///         &lt;/factoryAdapter&gt;
    ///       &lt;/logging&gt;
    ///     &lt;/common&gt;
    /// 
    ///     // To configure console logging in appsettings.json (ASP.NET Core):
    ///     {
    ///       "LogConfiguration": {
    ///         "factoryAdapter": {
    ///           "type": "LaunchDarkly.Logging.ConsoleAdapter, LaunchDarkly.CommonSdk.StrongName",
    ///           "arguments": {
    ///             "level": "Info",
    ///             "dateTimeFormat": "yyyy/MM/dd HH:mm:ss.fff"
    ///           }
    ///         }
    ///       }
    ///     }
    ///     
    ///     // Note, in the configuration file examples, if you are using the Xamarin SDK you must
    ///     // change "LaunchDarkly.CommonSdk.StrongName" to "LaunchDarkly.CommonSdk".
    /// </code>
    /// </example>
    public class ConsoleAdapter : AbstractSimpleLoggerFactoryAdapter
    {
        private readonly bool _useStandardError;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ConsoleAdapter() : base(null) { }
        
        /// <summary>
        /// Constructor that takes a <see cref="NameValueCollection"/>.
        /// </summary>
        /// <remarks>
        /// This is normally used for file-based configuration. The supported named properties are <c>level</c>,
        /// <c>showDateTime</c>, <c>showLogName</c>, and <c>dateTimeFormat</c>.
        /// </remarks>
        /// <param name="properties">collection of named properties</param>
        public ConsoleAdapter(NameValueCollection properties) : base(properties)
        {
            if (properties.TryGetValue("useStandardError", out var val))
            {
                if (bool.TryParse(val, out var b))
                {
                    _useStandardError = b;
                }
            }
        }

        /// <summary>
        /// Constructor that specifies only the log level.
        /// </summary>
        public ConsoleAdapter(LogLevel level)
            : base(level, true, true, true, null) { }

        /// <summary>
        /// Constructor that specifies all the properties supported by ConsoleOutLoggerFactoryAdapter.
        /// </summary>
        public ConsoleAdapter(LogLevel level, bool showDateTime, bool showLogName, bool showLevel, string dateTimeFormat)
            : base(level, showDateTime, showLogName, showLevel, dateTimeFormat) { }

        /// <summary>
        /// Constructor that specifies all properties.
        /// </summary>
        public ConsoleAdapter(LogLevel level, bool showDateTime, bool showLogName, bool showLevel, string dateTimeFormat, bool useStandardError)
            : base(level, showDateTime, showLogName, showLevel, dateTimeFormat)
        {
            _useStandardError = useStandardError;
        }

        /// <summary>
        /// Creates a logger with the specified configuration.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level"></param>
        /// <param name="showLevel"></param>
        /// <param name="showDateTime"></param>
        /// <param name="showLogName"></param>
        /// <param name="dateTimeFormat"></param>
        /// <returns></returns>
        protected override ILog CreateLogger(string name, LogLevel level, bool showLevel, bool showDateTime, bool showLogName, string dateTimeFormat)
        {
            return new ConsoleLogger(name, level, showLevel, showDateTime, showLogName, dateTimeFormat, _useStandardError);
        }
    }

    internal class ConsoleLogger : AbstractSimpleLogger
    {
        private readonly bool _useStandardError;

        public ConsoleLogger(string logName, LogLevel logLevel, bool showLevel, bool showDateTime, bool showLogName, string dateTimeFormat,
            bool useStandardError)
            : base(logName, logLevel, showLevel, showDateTime, showLogName, dateTimeFormat)
        {
            _useStandardError = useStandardError;
        }

        protected override void WriteInternal(LogLevel level, object message, Exception e)
        {
            var sb = new StringBuilder();
            FormatOutput(sb, level, message, e);
            var s = sb.ToString();
            if (_useStandardError)
            {
                Console.Error.WriteLine(s);
            }
            else
            {
                Console.Out.WriteLine(s);
            }
        }
    }
}
