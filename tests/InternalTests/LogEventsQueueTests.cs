using Serilog.Sinks.GrafanaLoki.Internal;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.InternalTests;

public class LogEventsQueueTests
{
    [Fact]
    public void Constructor_AcceptsNullLimit()
    {
        var queue = new LogEventsQueue(null);

        Assert.NotNull(queue);
    }

    [Fact]
    public void Constructor_AcceptsPositiveLimit()
    {
        var queue = new LogEventsQueue(1024);

        Assert.NotNull(queue);
    }

    [Fact]
    public void Constructor_ThrowsOnZeroLimit()
    {
        Assert.Throws<ArgumentException>(() => new LogEventsQueue(0));
    }

    [Fact]
    public void Constructor_ThrowsOnNegativeLimit()
    {
        Assert.Throws<ArgumentException>(() => new LogEventsQueue(-1));
    }

    [Fact]
    public void TryEnqueue_ReturnsOkForValidEntry()
    {
        var queue = new LogEventsQueue();
        var entry = new LogEventEntry("Test message");

        var result = queue.TryEnqueue(entry);

        Assert.Equal(LogEventsQueue.EnqueueResult.Ok, result);
    }

    [Fact]
    public void Enqueue_ThrowsOnQueueFull()
    {
        var queue = new LogEventsQueue(1); // Very small limit
        var entry = new LogEventEntry("A message that exceeds the tiny queue limit with labels etc.");

        // Fill up with labels to increase size
        entry.Labels.Add("key1", "value1");
        entry.Labels.Add("key2", "value2");

        Assert.Throws<InvalidOperationException>(() => queue.Enqueue(entry));
    }

    [Fact]
    public void TryEnqueue_ReturnsQueueFullWhenLimitExceeded()
    {
        var queue = new LogEventsQueue(1); // 1 byte limit ensures queue is full immediately
        var entry = new LogEventEntry("message");
        entry.Labels.Add("key", "value");

        var result = queue.TryEnqueue(entry);

        Assert.Equal(LogEventsQueue.EnqueueResult.QueueFull, result);
    }

    [Fact]
    public void TryDequeue_ReturnsQueueEmptyWhenQueueIsEmpty()
    {
        var queue = new LogEventsQueue();

        var result = queue.TryDequeue(null, out var logEvent);

        Assert.Equal(LogEventsQueue.DequeueResult.QueueEmpty, result);
        Assert.Null(logEvent);
    }

    [Fact]
    public void TryDequeue_ReturnsOkForEnqueuedItem()
    {
        var queue = new LogEventsQueue();
        var entry = new LogEventEntry("Test message");
        queue.TryEnqueue(entry);

        var result = queue.TryDequeue(null, out var dequeued);

        Assert.Equal(LogEventsQueue.DequeueResult.Ok, result);
        Assert.NotNull(dequeued);
        Assert.Equal("Test message", dequeued.Value.Message);
    }

    [Fact]
    public void TryDequeue_ReturnsMaxSizeViolationWhenEventExceedsLimit()
    {
        var queue = new LogEventsQueue();
        var entry = new LogEventEntry("Test message");
        queue.TryEnqueue(entry);

        var result = queue.TryDequeue(1, out var logEvent); // 1 byte max

        Assert.Equal(LogEventsQueue.DequeueResult.MaxSizeViolation, result);
        Assert.Null(logEvent);
    }

    [Fact]
    public void TryEnqueue_TracksByteSize()
    {
        var queue = new LogEventsQueue();
        var entry = new LogEventEntry("Test message");

        queue.TryEnqueue(entry);
        var result = queue.TryDequeue(null, out var dequeued);

        Assert.Equal(LogEventsQueue.DequeueResult.Ok, result);
        Assert.NotNull(dequeued);
    }

    [Fact]
    public void Queue_MaintainsFifoOrder()
    {
        var queue = new LogEventsQueue();
        var entry1 = new LogEventEntry("First");
        var entry2 = new LogEventEntry("Second");
        var entry3 = new LogEventEntry("Third");

        queue.TryEnqueue(entry1);
        queue.TryEnqueue(entry2);
        queue.TryEnqueue(entry3);

        queue.TryDequeue(null, out var d1);
        queue.TryDequeue(null, out var d2);
        queue.TryDequeue(null, out var d3);

        Assert.Equal("First", d1!.Value.Message);
        Assert.Equal("Second", d2!.Value.Message);
        Assert.Equal("Third", d3!.Value.Message);
    }
}
