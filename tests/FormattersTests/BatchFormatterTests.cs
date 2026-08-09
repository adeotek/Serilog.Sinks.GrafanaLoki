using Serilog.Sinks.GrafanaLoki.Formatters;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.FormattersTests;

public class BatchFormatterTests
{
    [Fact]
    public void Format_ThrowsOnNullLogEvents()
    {
        var formatter = new BatchFormatter();

        Assert.Throws<ArgumentNullException>(() =>
            formatter.Format(null!, new StringWriter()));
    }

    [Fact]
    public void Format_ThrowsOnNullOutput()
    {
        var formatter = new BatchFormatter();

        Assert.Throws<ArgumentNullException>(() =>
            formatter.Format(Array.Empty<LogEventEntry>(), null!));
    }

    [Fact]
    public void Format_WritesNothingForEmptyCollection()
    {
        var formatter = new BatchFormatter();
        using var writer = new StringWriter();

        formatter.Format(Array.Empty<LogEventEntry>(), writer);

        Assert.Empty(writer.ToString());
    }

    [Fact]
    public void Format_WritesNothingWhenAllMessagesAreWhitespace()
    {
        var formatter = new BatchFormatter();
        using var writer = new StringWriter();
        var entries = new[]
        {
            new LogEventEntry("   "),
            new LogEventEntry(""),
            new LogEventEntry(null!)
        };

        formatter.Format(entries, writer);

        Assert.Empty(writer.ToString());
    }

    [Fact]
    public void Format_ProducesValidJsonForSingleEvent()
    {
        var formatter = new BatchFormatter();
        using var writer = new StringWriter();
        var entry = new LogEventEntry("Test message");
        entry.Labels.Add("level", "error");

        formatter.Format(new[] { entry }, writer);

        var json = writer.ToString();
        Assert.False(string.IsNullOrEmpty(json));
        Assert.StartsWith("{\"streams\":[", json);
        Assert.Contains("\"level\":\"error\"", json);
        Assert.Contains("Test message", json);
    }

    [Fact]
    public void Format_IncludesGlobalLabels()
    {
        var globalLabels = new Dictionary<string, string>
        {
            { "app", "myapp" },
            { "env", "production" }
        };
        var formatter = new BatchFormatter(globalLabels);
        using var writer = new StringWriter();
        var entry = new LogEventEntry("Test message");
        entry.Labels.Add("level", "info");

        formatter.Format(new[] { entry }, writer);

        var json = writer.ToString();
        Assert.Contains("\"app\":\"myapp\"", json);
        Assert.Contains("\"env\":\"production\"", json);
        Assert.Contains("\"level\":\"info\"", json);
    }

    [Fact]
    public void Format_GroupsEventsByLabels()
    {
        var formatter = new BatchFormatter();
        using var writer = new StringWriter();

        var entry1 = new LogEventEntry("Error 1");
        entry1.Labels.Add("level", "error");

        var entry2 = new LogEventEntry("Error 2");
        entry2.Labels.Add("level", "error");

        var entry3 = new LogEventEntry("Warning 1");
        entry3.Labels.Add("level", "warning");

        formatter.Format(new[] { entry1, entry2, entry3 }, writer);

        var json = writer.ToString();
        // Should have two streams: one for error, one for warning
        var errorCount = CountOccurrences(json, "\"level\":\"error\"");
        var warningCount = CountOccurrences(json, "\"level\":\"warning\"");

        Assert.Equal(1, errorCount); // One stream for error group
        Assert.Equal(1, warningCount); // One stream for warning group
    }

    [Fact]
    public void Format_MultipleEventsInSameGroupHaveMultipleValues()
    {
        var formatter = new BatchFormatter();
        using var writer = new StringWriter();

        var entry1 = new LogEventEntry("Message 1");
        entry1.Labels.Add("level", "info");

        var entry2 = new LogEventEntry("Message 2");
        entry2.Labels.Add("level", "info");

        formatter.Format(new[] { entry1, entry2 }, writer);

        var json = writer.ToString();
        Assert.Contains("Message 1", json);
        Assert.Contains("Message 2", json);
    }

    [Fact]
    public void Format_GlobalLabelsOverrideEventLabelsWhenSameKey()
    {
        var globalLabels = new Dictionary<string, string>
        {
            { "level", "override" }
        };
        var formatter = new BatchFormatter(globalLabels);
        using var writer = new StringWriter();

        var entry = new LogEventEntry("Test");
        entry.Labels.Add("level", "original");

        formatter.Format(new[] { entry }, writer);

        var json = writer.ToString();
        Assert.Contains("\"level\":\"override", json);
        // AddOrAppend appends, so original gets appended after override
        Assert.Contains("overrideoriginal", json);
    }

    [Fact]
    public void Format_IncludesStructuredMetadata_WhenEntryHasMetadata()
    {
        var formatter = new BatchFormatter();
        using var writer = new StringWriter();
        var entry = new LogEventEntry("Test message");
        entry.Labels.Add("level", "info");
        entry.Metadata.Add("trace_id", "abc123");

        formatter.Format(new[] { entry }, writer);

        var json = writer.ToString();
        Assert.Contains("\"trace_id\":\"abc123\"", json);
    }

    [Fact]
    public void Format_OmitsMetadataObject_WhenEntryHasNoMetadata()
    {
        var formatter = new BatchFormatter();
        using var writer = new StringWriter();
        var entry = new LogEventEntry("Test message");
        entry.Labels.Add("level", "info");

        formatter.Format(new[] { entry }, writer);

        var json = writer.ToString();
        // Without metadata, the values entry should be [["ts","msg"]]
        // With metadata, it would be [["ts","msg",{"key":"val"}]]
        // So "msg" should be immediately followed by "] without a comma + brace
        Assert.DoesNotContain("\"Test message\",{", json);
    }

    private static int CountOccurrences(string text, string search)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(search, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += search.Length;
        }
        return count;
    }
}
