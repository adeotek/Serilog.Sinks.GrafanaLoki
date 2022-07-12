using System.Collections.Generic;

namespace Serilog.Sinks.GrafanaLoki.Internal;

internal class LogEventsBatch
{
    public List<LogEventEntry> LogEvents { get; } = new();
    public bool HasReachedLimit { get; set; }
}