using Serilog.Sinks.GrafanaLoki.Formatters;
using Shouldly;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.FormattersTests;

public class BatchFormatterTests
{
    [Fact]
    public void Format_ThrowsOnNullLogEvents()
    {
        var formatter = new BatchFormatter();

        Should.Throw<ArgumentNullException>(() =>
            formatter.Format(null!, new StringWriter()));
    }

    [Fact]
    public void Format_ThrowsOnNullOutput()
    {
        var formatter = new BatchFormatter();

        Should.Throw<ArgumentNullException>(() =>
            formatter.Format(Array.Empty<LogEventEntry>(), null!));
    }

    [Fact]
    public void Format_WritesNothingForEmptyCollection()
    {
        var formatter = new BatchFormatter();
        using var writer = new StringWriter();

        formatter.Format(Array.Empty<LogEventEntry>(), writer);

        writer.ToString().ShouldBeEmpty();
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

        writer.ToString().ShouldBeEmpty();
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
        json.ShouldNotBeNullOrEmpty();
        json.ShouldStartWith("{\"streams\":[");
        json.ShouldContain("\"level\":\"error\"");
        json.ShouldContain("Test message");
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
        json.ShouldContain("\"app\":\"myapp\"");
        json.ShouldContain("\"env\":\"production\"");
        json.ShouldContain("\"level\":\"info\"");
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

        errorCount.ShouldBe(1); // One stream for error group
        warningCount.ShouldBe(1); // One stream for warning group
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
        json.ShouldContain("Message 1");
        json.ShouldContain("Message 2");
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
        json.ShouldContain("\"level\":\"override");
        // AddOrAppend appends, so original gets appended after override
        json.ShouldContain("overrideoriginal");
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
