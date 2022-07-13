using System;
using System.Collections.Generic;

namespace Serilog.Sinks.GrafanaLoki.Internal;

internal class LogEventsQueue
{
    private readonly Queue<LogEventEntry> _queue;
    private readonly long? _queueLimitBytes;
    private readonly object _syncRoot = new();

    private long _queueBytes;

    public LogEventsQueue(long? queueLimitBytes = null)
    {
        if (queueLimitBytes < 1)
        {
            throw new ArgumentException("queueLimitBytes must be either null or greater than 0", nameof(queueLimitBytes));
        }

        _queue = new Queue<LogEventEntry>();
        _queueLimitBytes = queueLimitBytes;

        _queueBytes = 0;
    }

    public void Enqueue(LogEventEntry logEvent)
    {
        var result = TryEnqueue(logEvent);
        if (result != EnqueueResult.Ok)
        {
            throw new Exception($"Enqueue log event failed: {result}");
        }
    }

    public EnqueueResult TryEnqueue(LogEventEntry logEvent)
    {
        lock (_syncRoot)
        {
            var logEventByteSize = logEvent.GetByteSize();
            if (_queueBytes + logEventByteSize > _queueLimitBytes)
            {
                return EnqueueResult.QueueFull;
            }

            _queueBytes += logEventByteSize;
            _queue.Enqueue(logEvent);
            return EnqueueResult.Ok;
        }
    }

    public DequeueResult TryDequeue(long? logEventMaxSize, out LogEventEntry? logEvent)
    {
        lock (_syncRoot)
        {
            if (_queue.Count == 0)
            {
                logEvent = null;
                return DequeueResult.QueueEmpty;
            }

            logEvent = _queue.Peek();
            var logEventByteSize = logEvent.Value.GetByteSize();

            if (logEventByteSize > logEventMaxSize)
            {
                logEvent = null;
                return DequeueResult.MaxSizeViolation;
            }

            _queueBytes -= logEventByteSize;
            _queue.Dequeue();
            return DequeueResult.Ok;
        }
    }

    public enum EnqueueResult
    {
        Ok,
        QueueFull
    }

    public enum DequeueResult
    {
        Ok,
        QueueEmpty,
        MaxSizeViolation
    }
}