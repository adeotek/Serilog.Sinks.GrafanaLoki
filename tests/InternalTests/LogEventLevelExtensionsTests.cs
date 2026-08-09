using Serilog.Events;
using Serilog.Sinks.GrafanaLoki.Internal;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.InternalTests;

public class LogEventLevelExtensionsTests
{
    [Theory]
    [InlineData(LogEventLevel.Verbose, "trace")]
    [InlineData(LogEventLevel.Debug, "debug")]
    [InlineData(LogEventLevel.Information, "info")]
    [InlineData(LogEventLevel.Warning, "warning")]
    [InlineData(LogEventLevel.Error, "error")]
    [InlineData(LogEventLevel.Fatal, "fatal")]
    public void ToGrafanaString_MapsCorrectly(LogEventLevel level, string expected)
    {
        var result = level.ToGrafanaString();

        Assert.Equal(expected, result);
    }
}
