using System.Collections.Generic;

namespace Serilog.Sinks.GrafanaLoki.Internal;

internal class StreamEntry
{
    public string Timestamp { get; }
    public string Message { get; }
    public Dictionary<string, string>? Metadata { get; }

    public StreamEntry(string timestamp, string message, Dictionary<string, string>? metadata = null)
    {
        Timestamp = timestamp;
        Message = message;
        Metadata = metadata;
    }
}
