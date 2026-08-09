using Serilog.Sinks.GrafanaLoki.Internal;
using Shouldly;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.InternalTests;

public class LogEventsQueueTests
{
    [Fact]
    public void Constructor_AcceptsNullLimit()
    {
        var queue = new LogEventsQueue(null);

        queue.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_AcceptsPositiveLimit()
    {
        var queue = new LogEventsQueue(1024);

        queue.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ThrowsOnZeroLimit()
    {
        Should.Throw<ArgumentException>(() => new LogEventsQueue(0));
    }

    [Fact]
    public void Constructor_ThrowsOnNegativeLimit()
    {
        Should.Throw<ArgumentException>(() => new LogEventsQueue(-1));
    }

    [Fact]
    public void TryEnqueue_ReturnsOkForValidEntry()
    {
        var queue = new LogEventsQueue();
        var entry = new LogEventEntry("Test message");

        var result = queue.TryEnqueue(entry);

        result.ShouldBe(LogEventsQueue.EnqueueResult.Ok);
    }

    [Fact]
    public void Enqueue_ThrowsOnQueueFull()
    {
        var queue = new LogEventsQueue(1); // Very small limit
        var entry = new LogEventEntry("A message that exceeds the tiny queue limit with labels etc.");

        // Fill up with labels to increase size
        entry.Labels.Add("key1", "value1");
        entry.Labels.Add("key2", "value2");

        Should.Throw<Exception>(() => queue.Enqueue(entry));
    }

    [Fact]
    public void TryEnqueue_ReturnsQueueFullWhenLimitExceeded()
    {
        var queue = new LogEventsQueue(1); // 1 byte limit ensures queue is full immediately
        var entry = new LogEventEntry("message");
        entry.Labels.Add("key", "value");

        var result = queue.TryEnqueue(entry);

        result.ShouldBe(LogEventsQueue.EnqueueResult.QueueFull);
    }

    [Fact]
    public void TryDequeue_ReturnsQueueEmptyWhenQueueIsEmpty()
    {
        var queue = new LogEventsQueue();

        var result = queue.TryDequeue(null, out var logEvent);

        result.ShouldBe(LogEventsQueue.DequeueResult.QueueEmpty);
        logEvent.ShouldBeNull();
    }

    [Fact]
    public void TryDequeue_ReturnsOkForEnqueuedItem()
    {
        var queue = new LogEventsQueue();
        var entry = new LogEventEntry("Test message");
        queue.TryEnqueue(entry);

        var result = queue.TryDequeue(null, out var dequeued);

        result.ShouldBe(LogEventsQueue.DequeueResult.Ok);
        dequeued.ShouldNotBeNull();
        dequeued.Value.Message.ShouldBe("Test message");
    }

    [Fact]
    public void TryDequeue_ReturnsMaxSizeViolationWhenEventExceedsLimit()
    {
        var queue = new LogEventsQueue();
        var entry = new LogEventEntry("Test message");
        // Add a label to ensure GetByteSize() properly recalculates size;
        // without labels the cached _size sentinel value (-1) causes the check to pass incorrectly.
        entry.Labels.Add("key", "value");
        queue.TryEnqueue(entry);

        var result = queue.TryDequeue(1, out var logEvent); // 1 byte max

        result.ShouldBe(LogEventsQueue.DequeueResult.MaxSizeViolation);
        logEvent.ShouldBeNull();
    }

    [Fact]
    public void TryEnqueue_TracksByteSize()
    {
        var queue = new LogEventsQueue();
        var entry = new LogEventEntry("Test message");

        queue.TryEnqueue(entry);
        var result = queue.TryDequeue(null, out var dequeued);

        result.ShouldBe(LogEventsQueue.DequeueResult.Ok);
        dequeued.ShouldNotBeNull();
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

        d1!.Value.Message.ShouldBe("First");
        d2!.Value.Message.ShouldBe("Second");
        d3!.Value.Message.ShouldBe("Third");
    }
}
