using System.Collections.Generic;
using System.IO;
using Serilog.Sinks.GrafanaLoki.Internal;

namespace Serilog.Sinks.GrafanaLoki.Formatters;

/// <summary>
/// Formats batches of log events into payloads that can be sent over the network.
/// </summary>
public interface IBatchFormatter
{
    /// <summary>
    /// Format the log events into a payload.
    /// </summary>
    /// <param name="logEvents">
    /// The events to format.
    /// </param>
    /// <param name="output">
    /// The payload to send over the network.
    /// </param>
    void Format(IEnumerable<LogEventEntry> logEvents, TextWriter output);
}