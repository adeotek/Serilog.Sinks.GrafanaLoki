using Serilog.Debugging;

namespace Serilog.Sinks.GrafanaLoki.Internal;

internal static class LogEventsQueueReader
{
    public static LogEventsBatch Read(LogEventsQueue queue, int? logEventsInBatchLimit, long? batchSizeLimitBytes)
    {
        var batch = new LogEventsBatch();
        var remainingBatchSizeBytes = batchSizeLimitBytes;

        while (true)
        {
            var result = queue.TryDequeue(remainingBatchSizeBytes, out var logEvent);
            if (result == LogEventsQueue.DequeueResult.Ok && logEvent != null)
            {
                batch.LogEvents.Add(logEvent.Value);
                remainingBatchSizeBytes -= logEvent.Value.GetByteSize();

                // Respect batch posting limit
                if (batch.LogEvents.Count == logEventsInBatchLimit)
                {
                    batch.HasReachedLimit = true;
                    break;
                }
            }
            else if (result == LogEventsQueue.DequeueResult.MaxSizeViolation)
            {
                if (batch.LogEvents.Count == 0)
                {
                    // This single log event exceeds the batch size limit, let's drop it
                    queue.TryDequeue(long.MaxValue, out var logEventToDrop);

                    SelfLog.WriteLine(
                        "Event exceeds the batch size limit of {0} bytes set for this sink and will be dropped; data: {1}",
                        batchSizeLimitBytes,
                        logEventToDrop);
                }
                else
                {
                    batch.HasReachedLimit = true;
                    break;
                }
            }
            else if (result == LogEventsQueue.DequeueResult.QueueEmpty)
            {
                break;
            }
        }

        return batch;
    }
}