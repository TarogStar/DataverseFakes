using Microsoft.Xrm.Sdk;
using System;
using System.Text;

namespace FakeXrmEasy
{
    /// <summary>
    /// Provides a fake implementation of <see cref="ITracingService"/> for unit testing Dynamics 365 plugins and custom workflow activities.
    /// This class captures trace messages that would normally be written to the CRM trace log,
    /// allowing tests to verify that plugins are logging the expected information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In production, the ITracingService writes messages to the plug-in trace log in Dynamics 365.
    /// This fake implementation captures those messages in memory, allowing unit tests to:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Verify that expected trace messages are written</description></item>
    /// <item><description>Debug plugin execution by examining trace output</description></item>
    /// <item><description>Test error handling paths that log diagnostic information</description></item>
    /// </list>
    /// <para>
    /// Trace messages are also written to the console during test execution for debugging purposes.
    /// </para>
    /// </remarks>
    public class XrmFakedTracingService : ITracingService
    {
        /// <summary>
        /// Gets or sets the internal StringBuilder used to accumulate trace messages.
        /// </summary>
        protected StringBuilder _trace { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XrmFakedTracingService"/> class.
        /// Creates an empty trace buffer ready to capture trace messages.
        /// </summary>
        public XrmFakedTracingService()
        {
            _trace = new StringBuilder();
        }

        /// <summary>
        /// Writes a trace message to the trace buffer, using composite formatting.
        /// This method mimics the behavior of the CRM ITracingService.Trace method.
        /// </summary>
        /// <param name="format">A composite format string that contains text intermixed with zero or more format items,
        /// which correspond to objects in the args array. Uses the same format as <see cref="string.Format(string, object[])"/>.</param>
        /// <param name="args">An object array that contains zero or more objects to format.
        /// If no arguments are provided, the format string is written as-is.</param>
        /// <remarks>
        /// <para>
        /// The trace message is both written to the console (for immediate debugging visibility during test execution)
        /// and appended to an internal buffer that can be retrieved using <see cref="DumpTrace"/>.
        /// </para>
        /// <para>
        /// When called with no arguments, the method treats the format string as a literal string
        /// to avoid format string parsing errors when the string contains braces.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// tracingService.Trace("Processing entity: {0}", entityName);
        /// tracingService.Trace("Operation completed successfully");
        /// </code>
        /// </example>
        public void Trace(string format, params object[] args)
        {
            if (args.Length == 0)
            {
                Trace("{0}", format);
            }
            else
            {
                Console.WriteLine(format, args);

                _trace.AppendLine(string.Format(format, args));
            };
        }

        /// <summary>
        /// Retrieves all trace messages that have been captured since the service was created.
        /// </summary>
        /// <returns>A string containing all accumulated trace messages, with each message on a separate line.</returns>
        /// <remarks>
        /// This method is useful in unit tests to verify that a plugin wrote the expected trace messages.
        /// The returned string can be searched or compared against expected values.
        /// </remarks>
        /// <example>
        /// <code>
        /// var tracingService = new XrmFakedTracingService();
        /// // ... execute plugin that uses tracingService ...
        /// string traceOutput = tracingService.DumpTrace();
        /// Assert.Contains("Expected message", traceOutput);
        /// </code>
        /// </example>
        public string DumpTrace()
        {
            return _trace.ToString();
        }
    }
}
