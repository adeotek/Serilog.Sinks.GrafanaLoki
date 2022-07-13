using Serilog.Events;

namespace Serilog.Sinks.GrafanaLoki.Internal;

internal static class LogEventLevelExtensions
{
    internal static string ToGrafanaString(this LogEventLevel level) =>
        level switch
        {
            LogEventLevel.Verbose => "trace",
            LogEventLevel.Debug => "debug",
            LogEventLevel.Information => "info",
            LogEventLevel.Warning => "warning",
            LogEventLevel.Error => "error",
            LogEventLevel.Fatal => "fatal",
            _ => "info"
        };
}