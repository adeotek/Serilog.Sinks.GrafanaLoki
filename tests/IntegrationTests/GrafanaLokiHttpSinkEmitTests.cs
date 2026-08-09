using System.Text.Json;
using Serilog.Sinks.GrafanaLoki.Tests.Infrastructure;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.IntegrationTests;

public class GrafanaLokiHttpSinkEmitTests
{
    // ── C2: size check after labels ─────────────────────────────────

    [Fact]
    public void LogEventLimitBytes_DropsEventsExceedingLimit_WithLabelsCounted()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client,
                logEventLimitBytes: 10) // 10 bytes — message alone (<10) passes, but with labels (>10) fails
            .CreateLogger();

        log.Information("X");
        log.Dispose();

        // Event should have been dropped by the size check — nothing was sent.
        Assert.True(string.IsNullOrEmpty(client.Content));
    }

    // ── H3: reserved label protection ───────────────────────────────

    [Fact]
    public void ReservedLabels_NotOverwrittenByEventProperties()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client)
            .CreateLogger();

        // Log with a property named "level" that differs from the actual log level.
        log.ForContext("level", "custom")
           .Information("Test message");
        log.Dispose();

        var content = JsonDocument.Parse(client.Content);
        var stream = content.RootElement.GetProperty("streams")[0].GetProperty("stream");

        // The level label should be the actual log level ("info"), not "custom".
        Assert.Equal("info", stream.GetProperty("level").GetString());
    }

    [Fact]
    public void ReservedExceptionLabels_NotOverwrittenByEventProperties()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client)
            .CreateLogger();

        // Log an error with properties that try to overwrite exception labels.
        try { throw new InvalidOperationException("Boom"); }
        catch (Exception ex)
        {
            log.ForContext("exception_type", "Fake.Type")
               .ForContext("exception", "Fake exception")
               .Error(ex, "An error occurred");
        }
        log.Dispose();

        var content = JsonDocument.Parse(client.Content);
        var stream = content.RootElement.GetProperty("streams")[0].GetProperty("stream");

        // The exception_type label should be the real exception type, not "Fake.Type".
        Assert.Equal("System.InvalidOperationException", stream.GetProperty("exception_type").GetString());
    }

    // ── L1: disposed check ──────────────────────────────────────────

    [Fact]
    public void Emit_AfterDispose_DoesNotEnqueue()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client)
            .CreateLogger();

        log.Dispose();

        // After dispose, calling Emit (via log methods) may cause Serilog to throw.
        // We verify that no event was enqueued/sent regardless.
        try { log.Information("After dispose"); } catch { /* expected from Serilog */ }

        Assert.True(string.IsNullOrEmpty(client.Content));
    }
}
