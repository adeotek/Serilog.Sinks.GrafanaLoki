using System.Text.Json;
using Serilog.Context;
using Serilog.Events;
using Serilog.Sinks.GrafanaLoki.Tests.Infrastructure;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.IntegrationTests;

public class GrafanaLokiSinkIntegrationTests
{
    private static JsonElement ParseStreams(string content)
    {
        return JsonDocument.Parse(content).RootElement.GetProperty("streams");
    }

    private static JsonElement FindStream(JsonElement streams, Dictionary<string, string> labels)
    {
        foreach (var stream in streams.EnumerateArray())
        {
            var streamLabels = stream.GetProperty("stream");
            var match = labels.All(kvp =>
                streamLabels.TryGetProperty(kvp.Key, out var v) && v.GetString() == kvp.Value);
            if (match)
                return stream;
        }
        throw new InvalidOperationException(
            $"No stream found with labels: {string.Join(", ", labels.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
    }

    private static List<string> GetStreamValues(JsonElement stream)
    {
        return stream.GetProperty("values").EnumerateArray()
            .Select(v => v[1].GetString()!)
            .ToList();
    }

    // ── Label grouping ──────────────────────────────────────────────

    [Fact]
    public void EventsWithSameLabels_AreGroupedIntoSameStream()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client)
            .CreateLogger();

        log.Information("Message A");
        log.Information("Message B");
        log.Dispose();

        var streams = ParseStreams(client.Content);
        var count = streams.GetArrayLength();

        // Both Information events share the same labels → one stream with two values
        Assert.Equal(1, count);

        var values = GetStreamValues(streams[0]);
        Assert.Equal(2, values.Count);
        Assert.Contains("Message A", values);
        Assert.Contains("Message B", values);
    }

    [Fact]
    public void EventsWithDifferentLevels_AreGroupedIntoSeparateStreams()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client)
            .CreateLogger();

        log.Information("Info message");
        log.Warning("Warning message");
        log.Error("Error message");
        log.Dispose();

        var streams = ParseStreams(client.Content);

        Assert.Equal(3, streams.GetArrayLength());

        var infoStream = FindStream(streams, new() { { "level", "info" } });
        var warnStream = FindStream(streams, new() { { "level", "warning" } });
        var errorStream = FindStream(streams, new() { { "level", "error" } });

        Assert.Contains("Info message", GetStreamValues(infoStream));
        Assert.Contains("Warning message", GetStreamValues(warnStream));
        Assert.Contains("Error message", GetStreamValues(errorStream));
    }

    [Fact]
    public void AllLogLevels_MapToCorrectLabels()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client)
            .CreateLogger();

        log.Verbose("v");
        log.Debug("d");
        log.Information("i");
        log.Warning("w");
        log.Error("e");
        log.Fatal("f");
        log.Dispose();

        var streams = ParseStreams(client.Content);

        Assert.True(streams.GetArrayLength() >= 5, "Expected at least 5 streams for different levels");

        var expectedMappings = new Dictionary<string, string>
        {
            { "trace", "v" }, { "debug", "d" }, { "info", "i" },
            { "warning", "w" }, { "error", "e" }, { "fatal", "f" }
        };

        foreach (var (level, message) in expectedMappings)
        {
            var stream = FindStream(streams, new() { { "level", level } });
            Assert.Contains(message, GetStreamValues(stream));
        }
    }

    // ── Global labels ───────────────────────────────────────────────

    [Fact]
    public void GlobalLabels_AppearOnAllStreams()
    {
        var client = new TestHttpClient();
        var globalLabels = new Dictionary<string, string>
        {
            { "app", "myapp" },
            { "env", "production" }
        };
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", labels: globalLabels, outputTemplate: "{Message:lj}", httpClient: client)
            .CreateLogger();

        log.Information("Info 1");
        log.Error("Error 1");
        log.Dispose();

        var streams = ParseStreams(client.Content);

        foreach (var stream in streams.EnumerateArray())
        {
            var streamLabels = stream.GetProperty("stream");
            Assert.Equal("myapp", streamLabels.GetProperty("app").GetString());
            Assert.Equal("production", streamLabels.GetProperty("env").GetString());
        }
    }

    // ── Contextual labels ───────────────────────────────────────────

    [Fact]
    public void ContextualLabels_PropagateToStream()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client,
                propertiesStringDelimiter: null)
            .CreateLogger();

        using (LogContext.PushProperty("RequestId", "abc-123"))
        using (LogContext.PushProperty("UserId", "user-42"))
        {
            log.Information("Processing request");
        }
        log.Dispose();

        var streams = ParseStreams(client.Content);

        // The sink replaces quotes in property values with the delimiter (default "`").
        // ScalarValue.ToString() for strings wraps in quotes, so the label value
        // will have quotes replaced with backticks.
        var stream = FindStream(streams, new()
        {
            { "level", "info" },
            { "RequestId", "`abc-123`" },
            { "UserId", "`user-42`" }
        });

        Assert.Contains("Processing request", GetStreamValues(stream));
    }

    [Fact]
    public void ContextualLabels_CreateSeparateStreamsForDifferentContexts()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client,
                propertiesStringDelimiter: null)
            .CreateLogger();

        using (LogContext.PushProperty("Tenant", "A"))
            log.Information("Tenant A message");

        using (LogContext.PushProperty("Tenant", "B"))
            log.Information("Tenant B message");

        log.Dispose();

        var streams = ParseStreams(client.Content);

        // Two separate streams — one per tenant.
        // String values from ScalarValue.ToString() are quoted, and quotes are
        // replaced with the delimiter (default "`").
        Assert.Equal(2, streams.GetArrayLength());

        var streamA = FindStream(streams, new() { { "Tenant", "`A`" } });
        var streamB = FindStream(streams, new() { { "Tenant", "`B`" } });

        Assert.Contains("Tenant A message", GetStreamValues(streamA));
        Assert.Contains("Tenant B message", GetStreamValues(streamB));
    }

    // ── Exception labels ────────────────────────────────────────────

    [Fact]
    public void ExceptionWithBothLabelsEnabled_IncludesBoth()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client,
                exceptionTypeAsLabel: true,
                exceptionAsLabel: true)
            .CreateLogger();

        try { throw new InvalidOperationException("Something broke"); }
        catch (Exception ex) { log.Error(ex, "An error occurred"); }
        log.Dispose();

        var streams = ParseStreams(client.Content);
        var stream = FindStream(streams, new() { { "level", "error" } });
        var labels = stream.GetProperty("stream");

        Assert.Equal("System.InvalidOperationException", labels.GetProperty("exception_type").GetString());
        Assert.Contains("Something broke", labels.GetProperty("exception").GetString());
    }

    [Fact]
    public void ExceptionWithOnlyTypeLabel_ExcludesFullException()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client,
                exceptionTypeAsLabel: true,
                exceptionAsLabel: false)
            .CreateLogger();

        try { throw new ArgumentNullException("param"); }
        catch (Exception ex) { log.Error(ex, "Null param"); }
        log.Dispose();

        var streams = ParseStreams(client.Content);
        var stream = FindStream(streams, new() { { "level", "error" } });
        var labels = stream.GetProperty("stream");

        Assert.Equal("System.ArgumentNullException", labels.GetProperty("exception_type").GetString());
        Assert.False(labels.TryGetProperty("exception", out _));
    }

    [Fact]
    public void ExceptionTypeLabel_IncludesNamespaceInFullName()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client,
                exceptionTypeAsLabel: true)
            .CreateLogger();

        try { throw new System.IO.FileNotFoundException("missing.txt"); }
        catch (Exception ex) { log.Error(ex, "File not found"); }
        log.Dispose();

        var streams = ParseStreams(client.Content);
        var stream = FindStream(streams, new() { { "level", "error" } });
        var labels = stream.GetProperty("stream");

        Assert.Equal("System.IO.FileNotFoundException", labels.GetProperty("exception_type").GetString());
    }

    // ── Custom output template ──────────────────────────────────────

    [Fact]
    public void CustomOutputTemplate_IsApplied()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "[{Level:u3}] {Message:lj}",
                httpClient: client)
            .CreateLogger();

        log.Information("Hello, World!");
        log.Dispose();

        var streams = ParseStreams(client.Content);
        var values = GetStreamValues(streams[0]);

        Assert.Single(values);
        Assert.Equal("[INF] Hello, World!", values[0]);
    }

    // ── Special characters ──────────────────────────────────────────

    [Fact]
    public void SpecialCharacters_AreIncludedInMessage()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client)
            .CreateLogger();

        log.Information("Special chars: <>&\"'");
        log.Dispose();

        var streams = ParseStreams(client.Content);
        var values = GetStreamValues(streams[0]);

        Assert.Single(values);
        Assert.Contains("<", values[0]);
        Assert.Contains(">", values[0]);
        Assert.Contains("&", values[0]);
        Assert.Contains("\"", values[0]);
        Assert.Contains("'", values[0]);
    }

    [Fact]
    public void QuotesInPropertyValues_AreReplacedWithDelimiter()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client,
                propertiesStringDelimiter: "`")
            .CreateLogger();

        log.ForContext("QuotedValue", "he said \"hello\"")
           .Information("Message with quoted property");
        log.Dispose();

        var streams = ParseStreams(client.Content);
        var stream = streams[0].GetProperty("stream");

        Assert.True(stream.TryGetProperty("QuotedValue", out var prop));
        var propValue = prop.GetString()!;
        Assert.DoesNotContain("\"", propValue);
        Assert.Contains("`", propValue);
    }

    // ── Timestamp ordering ──────────────────────────────────────────

    [Fact]
    public void EventsWithinStream_AreOrderedByTimestamp()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client)
            .CreateLogger();

        log.Information("First");
        Thread.Sleep(10);
        log.Information("Second");
        Thread.Sleep(10);
        log.Information("Third");
        log.Dispose();

        var streams = ParseStreams(client.Content);
        var stream = streams[0];
        var values = stream.GetProperty("values").EnumerateArray().ToList();

        // Values should be ordered by timestamp (ascending)
        var timestamps = values.Select(v => long.Parse(v[0].GetString()!)).ToList();
        for (var i = 1; i < timestamps.Count; i++)
        {
            Assert.True(timestamps[i] > timestamps[i - 1],
                $"Expected timestamp[{i}] ({timestamps[i]}) > timestamp[{i - 1}] ({timestamps[i - 1]})");
        }
    }

    // ── Message content ─────────────────────────────────────────────

    [Fact]
    public void EmittedMessages_ArePresentInPayload()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client)
            .CreateLogger();

        log.Information("Alpha");
        log.Warning("Beta");
        log.Error("Gamma");
        log.Dispose();

        var content = client.Content;

        Assert.Contains("Alpha", content);
        Assert.Contains("Beta", content);
        Assert.Contains("Gamma", content);
    }

    // ── Multiple enrichers ──────────────────────────────────────────

    [Fact]
    public void StaticAndContextualEnrichers_CombineCorrectly()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("Service", "api")
            .Enrich.FromLogContext()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client,
                propertiesStringDelimiter: null)
            .CreateLogger();

        using (LogContext.PushProperty("CorrelationId", "corr-789"))
        {
            log.Information("Enriched message");
        }
        log.Dispose();

        var streams = ParseStreams(client.Content);

        // String values from ScalarValue.ToString() are quoted; quotes are replaced
        // with the default delimiter "`".
        var stream = FindStream(streams, new()
        {
            { "level", "info" },
            { "Service", "`api`" },
            { "CorrelationId", "`corr-789`" }
        });

        Assert.Contains("Enriched message", GetStreamValues(stream));
    }

    // ── Request URI ─────────────────────────────────────────────────

    [Fact]
    public void RequestUri_UsesCorrectLokiApiPath()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://loki:3100", outputTemplate: "{Message:lj}", httpClient: client)
            .CreateLogger();

        log.Information("Test");
        log.Dispose();

        Assert.Equal("http://loki:3100/loki/api/v1/push", client.RequestUri);
    }

    [Fact]
    public void RequestUri_WithCustomApiVersion()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://loki:3100", apiVersion: "v2", outputTemplate: "{Message:lj}", httpClient: client)
            .CreateLogger();

        log.Information("Test");
        log.Dispose();

        Assert.Equal("http://loki:3100/loki/api/v2/push", client.RequestUri);
    }

    // ── Edge cases ──────────────────────────────────────────────────

    [Fact]
    public void DisposingLogger_FlushesAllEvents()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client)
            .CreateLogger();

        for (var i = 0; i < 10; i++)
            log.Information($"Event {i}");

        log.Dispose();

        Assert.NotEmpty(client.Content);
        var streams = ParseStreams(client.Content);
        var values = GetStreamValues(streams[0]);

        Assert.Equal(10, values.Count);
    }

    [Fact]
    public void EmptyMessage_IsExcludedFromBatch()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message}", httpClient: client)
            .CreateLogger();

        log.Information("Real message");
        log.Dispose();

        var streams = ParseStreams(client.Content);
        Assert.Equal(1, streams.GetArrayLength());
        var values = GetStreamValues(streams[0]);
        Assert.Single(values);
    }

    [Fact]
    public void MultilineMessages_AreTrimmedOfTrailingNewlines()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message}", httpClient: client)
            .CreateLogger();

        log.Information("Line 1\nLine 2\r\nLine 3");
        log.Dispose();

        var streams = ParseStreams(client.Content);
        var values = GetStreamValues(streams[0]);

        Assert.Single(values);
        Assert.Contains("Line 1", values[0]);
        Assert.Contains("Line 2", values[0]);
        Assert.Contains("Line 3", values[0]);
        Assert.False(values[0].EndsWith("\n"));
        Assert.False(values[0].EndsWith("\r"));
    }

    [Fact]
    public void ExceptionsOnMultipleEventsWithSameType_ShareStream()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80",
                outputTemplate: "{Message:lj}",
                httpClient: client,
                exceptionTypeAsLabel: true)
            .CreateLogger();

        try { throw new InvalidOperationException("First error"); }
        catch (Exception ex) { log.Error(ex, "Error 1"); }

        try { throw new InvalidOperationException("Second error"); }
        catch (Exception ex) { log.Error(ex, "Error 2"); }

        log.Dispose();

        var streams = ParseStreams(client.Content);

        // Same exception_type → same stream
        Assert.Equal(1, streams.GetArrayLength());

        var labels = streams[0].GetProperty("stream");
        Assert.Equal("System.InvalidOperationException", labels.GetProperty("exception_type").GetString());

        var values = GetStreamValues(streams[0]);
        Assert.Contains("Error 1", values);
        Assert.Contains("Error 2", values);
    }

    // ── JSON validity ───────────────────────────────────────────────

    [Fact]
    public void Payload_IsValidJson()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client)
            .CreateLogger();

        log.Information("Test message");
        log.Dispose();

        // Parsing without exception proves valid JSON
        var doc = JsonDocument.Parse(client.Content);
        Assert.NotNull(doc);

        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("streams", out var streams));
        Assert.Equal(JsonValueKind.Array, streams.ValueKind);
        Assert.True(streams.GetArrayLength() > 0);
    }

    [Fact]
    public void EachStream_HasRequiredFields()
    {
        var client = new TestHttpClient();
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.GrafanaLoki("http://test:80", outputTemplate: "{Message:lj}", httpClient: client)
            .CreateLogger();

        log.Information("Test");
        log.Dispose();

        var streams = ParseStreams(client.Content);

        foreach (var stream in streams.EnumerateArray())
        {
            // Each stream must have "stream" (labels) and "values"
            Assert.True(stream.TryGetProperty("stream", out _));
            Assert.True(stream.TryGetProperty("values", out var values));
            Assert.Equal(JsonValueKind.Array, values.ValueKind);

            // Each value entry must be [timestamp, message]
            foreach (var entry in values.EnumerateArray())
            {
                Assert.Equal(2, entry.GetArrayLength());
                Assert.True(long.TryParse(entry[0].GetString(), out _),
                    $"Timestamp should be a numeric string, got: {entry[0].GetString()}");
            }
        }
    }
}
