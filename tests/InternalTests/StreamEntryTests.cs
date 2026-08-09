using System.Collections.Generic;
using Serilog.Sinks.GrafanaLoki.Internal;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.InternalTests;

public class StreamEntryTests
{
    [Fact]
    public void Constructor_StoresTimestampAndMessage()
    {
        var entry = new StreamEntry("1234567890", "test message");

        Assert.Equal("1234567890", entry.Timestamp);
        Assert.Equal("test message", entry.Message);
        Assert.Null(entry.Metadata);
    }

    [Fact]
    public void Constructor_StoresMetadata_WhenProvided()
    {
        var metadata = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };

        var entry = new StreamEntry("1234567890", "test message", metadata);

        Assert.NotNull(entry.Metadata);
        Assert.Equal(2, entry.Metadata!.Count);
        Assert.Equal("value1", entry.Metadata["key1"]);
        Assert.Equal("value2", entry.Metadata["key2"]);
    }
}
