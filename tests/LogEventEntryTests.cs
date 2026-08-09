using Serilog.Sinks.GrafanaLoki.Common;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests;

public class LogEventEntryTests
{
    [Fact]
    public void GetByteSize_ReturnsCorrectValue_ForLabelLessEntry()
    {
        var entry = new LogEventEntry("Test message");

        var size = entry.GetByteSize();

        // Size should be sizeof(long) + UTF8 byte count of the message (not -1)
        var expectedSize = sizeof(long) + ByteSize.From("Test message");
        Assert.Equal(expectedSize, size);
    }

    [Fact]
    public void GetByteSize_Recalculates_WhenLabelsAdded()
    {
        var entry = new LogEventEntry("Test message");

        var sizeBefore = entry.GetByteSize();
        entry.Labels.Add("key", "value");
        var sizeAfter = entry.GetByteSize();

        Assert.True(sizeAfter > sizeBefore, "Size should increase when labels are added");
    }

    [Fact]
    public void GetByteSize_ReturnsCorrectValue_WithLabels()
    {
        var entry = new LogEventEntry("Test message");
        entry.Labels.Add("level", "error");
        entry.Labels.Add("exception_type", "System.Exception");

        var size = entry.GetByteSize();

        var expectedSize = sizeof(long)
            + ByteSize.From("Test message")
            + ByteSize.From("level") + ByteSize.From("error")
            + ByteSize.From("exception_type") + ByteSize.From("System.Exception");
        Assert.Equal(expectedSize, size);
    }

    [Fact]
    public void GetByteSize_CachesValue_WhenLabelsUnchanged()
    {
        var entry = new LogEventEntry("Test message");
        entry.Labels.Add("key", "value");

        var size1 = entry.GetByteSize();
        var size2 = entry.GetByteSize();

        Assert.Equal(size1, size2);
        Assert.True(size1 > 0, "Cached size should be positive, not the -1 sentinel");
    }
}
