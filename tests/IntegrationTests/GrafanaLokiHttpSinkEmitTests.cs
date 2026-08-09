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

    [Fact]
    public void UseStructuredMetadata_SendsPropertiesAsMetadata_NotLabels()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client,
                useStructuredMetadata: true)
            .CreateLogger();

        log.ForContext("RequestId", "abc123")
           .ForContext("UserId", "user42")
           .Information("Test message");
        log.Dispose();

        var content = JsonDocument.Parse(client.Content);
        var stream = content.RootElement.GetProperty("streams")[0].GetProperty("stream");
        Assert.Equal("info", stream.GetProperty("level").GetString());
        Assert.False(stream.TryGetProperty("RequestId", out _));
        Assert.False(stream.TryGetProperty("UserId", out _));
        var values = content.RootElement.GetProperty("streams")[0].GetProperty("values")[0];
        Assert.Equal(3, values.GetArrayLength());
        var metadata = values[2];
        Assert.Equal("`abc123`", metadata.GetProperty("RequestId").GetString());
        Assert.Equal("`user42`", metadata.GetProperty("UserId").GetString());
    }

    [Fact]
    public void UseStructuredMetadata_False_SendsPropertiesAsLabels()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client,
                useStructuredMetadata: false)
            .CreateLogger();
        log.ForContext("RequestId", "abc123").Information("Test message");
        log.Dispose();
        var content = JsonDocument.Parse(client.Content);
        var stream = content.RootElement.GetProperty("streams")[0].GetProperty("stream");
        Assert.Equal("`abc123`", stream.GetProperty("RequestId").GetString());
        var values = content.RootElement.GetProperty("streams")[0].GetProperty("values")[0];
        Assert.Equal(2, values.GetArrayLength());
    }

    [Fact]
    public void MaxLabelCount_DropsExcessLabels()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client, maxLabelCount: 3)
            .CreateLogger();
        log.ForContext("Prop1", "val1").ForContext("Prop2", "val2").ForContext("Prop3", "val3").ForContext("Prop4", "val4")
           .Information("Test message");
        log.Dispose();
        var content = JsonDocument.Parse(client.Content);
        var stream = content.RootElement.GetProperty("streams")[0].GetProperty("stream");
        var labelCount = stream.EnumerateObject().Count();
        Assert.True(labelCount <= 3, $"Expected at most 3 labels, got {labelCount}");
        Assert.Equal("info", stream.GetProperty("level").GetString());
    }

    [Fact]
    public void MaxLabelCount_RespectsExceptionLabel_WhenExceptionTypeAsLabel()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client, maxLabelCount: 3)
            .CreateLogger();

        try { throw new InvalidOperationException("Boom"); }
        catch (Exception ex) { log.Error(ex, "Error occurred"); }
        log.Dispose();

        var content = JsonDocument.Parse(client.Content);
        var stream = content.RootElement.GetProperty("streams")[0].GetProperty("stream");
        var labelCount = stream.EnumerateObject().Count();
        Assert.True(labelCount <= 3, string.Format("Expected at most 3 labels, got {0}", labelCount));
        Assert.Equal("error", stream.GetProperty("level").GetString());
        Assert.Equal("System.InvalidOperationException", stream.GetProperty("exception_type").GetString());
    }

    [Fact]
    public void MaxLabelCount_Null_AllowsAllLabels()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client, maxLabelCount: null)
            .CreateLogger();
        log.ForContext("Prop1", "val1").ForContext("Prop2", "val2").ForContext("Prop3", "val3").ForContext("Prop4", "val4")
           .Information("Test message");
        log.Dispose();
        var content = JsonDocument.Parse(client.Content);
        var stream = content.RootElement.GetProperty("streams")[0].GetProperty("stream");
        Assert.Equal("`val1`", stream.GetProperty("Prop1").GetString());
        Assert.Equal("`val4`", stream.GetProperty("Prop4").GetString());
    }

    [Fact]
    public void MaxLabelCount_WithStructuredMetadata_MovesExcessLabelsToMetadata()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client, useStructuredMetadata: true, maxLabelCount: 2)
            .CreateLogger();
        log.ForContext("Prop1", "val1").ForContext("Prop2", "val2").Information("Test message");
        log.Dispose();
        var content = JsonDocument.Parse(client.Content);
        var stream = content.RootElement.GetProperty("streams")[0].GetProperty("stream");
        Assert.Equal("info", stream.GetProperty("level").GetString());
        Assert.False(stream.TryGetProperty("Prop2", out _));
        var values = content.RootElement.GetProperty("streams")[0].GetProperty("values")[0];
        Assert.Equal(3, values.GetArrayLength());
        var metadata = values[2];
        Assert.Equal("`val2`", metadata.GetProperty("Prop2").GetString());
    }
}
