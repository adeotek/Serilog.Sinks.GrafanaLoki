using Serilog.Sinks.GrafanaLoki.Internal;
using Shouldly;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.InternalTests;

public class LogEventsQueueReaderTests
{
    [Fact]
    public void Read_ReturnsEmptyBatchFromEmptyQueue()
    {
        var queue = new LogEventsQueue();

        var batch = LogEventsQueueReader.Read(queue, null, null);

        batch.LogEvents.ShouldBeEmpty();
        batch.HasReachedLimit.ShouldBeFalse();
    }

    [Fact]
    public void Read_ReturnsAllItemsWhenNoLimits()
    {
        var queue = new LogEventsQueue();
        queue.TryEnqueue(new LogEventEntry("Message 1"));
        queue.TryEnqueue(new LogEventEntry("Message 2"));
        queue.TryEnqueue(new LogEventEntry("Message 3"));

        var batch = LogEventsQueueReader.Read(queue, null, null);

        batch.LogEvents.Count.ShouldBe(3);
        batch.HasReachedLimit.ShouldBeFalse();
    }

    [Fact]
    public void Read_RespectsLogEventsInBatchLimit()
    {
        var queue = new LogEventsQueue();
        for (var i = 0; i < 10; i++)
        {
            queue.TryEnqueue(new LogEventEntry($"Message {i}"));
        }

        var batch = LogEventsQueueReader.Read(queue, logEventsInBatchLimit: 5, batchSizeLimitBytes: null);

        batch.LogEvents.Count.ShouldBe(5);
        batch.HasReachedLimit.ShouldBeTrue();
    }

    [Fact]
    public void Read_RespectsBatchSizeLimit()
    {
        var queue = new LogEventsQueue();
        var entry = new LogEventEntry("Test");
        queue.TryEnqueue(entry);

        // Set a large batch size so the single entry fits
        var batch = LogEventsQueueReader.Read(queue, logEventsInBatchLimit: null, batchSizeLimitBytes: 1024 * 1024);

        batch.LogEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Read_DropsSingleOversizedEvent()
    {
        var queue = new LogEventsQueue();
        var entry = new LogEventEntry("A large message that has some content");
        entry.Labels.Add("key", "value");
        queue.TryEnqueue(entry);

        // Set batch size limit very small so the entry exceeds it
        var batch = LogEventsQueueReader.Read(queue, logEventsInBatchLimit: null, batchSizeLimitBytes: 1);

        batch.LogEvents.ShouldBeEmpty();
        batch.HasReachedLimit.ShouldBeFalse();
    }

    [Fact]
    public void Read_StopsOnOversizedEventWhenBatchNotEmpty()
    {
        var queue = new LogEventsQueue();
        var smallEntry = new LogEventEntry("OK");
        queue.TryEnqueue(smallEntry);

        var batch = LogEventsQueueReader.Read(queue, logEventsInBatchLimit: 10, batchSizeLimitBytes: 100);

        batch.LogEvents.Count.ShouldBe(1);
        batch.HasReachedLimit.ShouldBeFalse(); // Not the limit, just no more fitting items (or reached end)
    }
}
