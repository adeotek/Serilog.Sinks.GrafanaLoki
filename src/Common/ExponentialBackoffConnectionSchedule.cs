using System;

namespace Serilog.Sinks.GrafanaLoki.Common;

public class ExponentialBackoffConnectionSchedule
{
    /// <summary>
    /// The minimum backoff period.
    /// </summary>
    public static readonly TimeSpan MinimumBackoffPeriod = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The maximum, i.e. capped, backoff period.
    /// </summary>
    public static readonly TimeSpan MaximumBackoffInterval = TimeSpan.FromMinutes(10);

    private readonly TimeSpan period;

    private int _failuresSinceSuccessfulConnection;

    public ExponentialBackoffConnectionSchedule(TimeSpan period)
    {
        if (period < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(period), "The connection retry period must be a positive timespan");

        this.period = period;
    }

    public void MarkSuccess()
    {
        _failuresSinceSuccessfulConnection = 0;
    }

    public void MarkFailure()
    {
        _failuresSinceSuccessfulConnection++;
    }

    public TimeSpan NextInterval
    {
        get
        {
            try
            {
                // Available, and first failure, just try the batch interval
                if (_failuresSinceSuccessfulConnection <= 1)
                    return period;

                // Second failure, start ramping up the interval - first 2x, then 4x, ...
                var backoffFactor = Math.Pow(2, _failuresSinceSuccessfulConnection - 1);

                // If the period is ridiculously short, give it a boost so we get some
                // visible backoff
                var backoffPeriod = Math.Max(period.Ticks, MinimumBackoffPeriod.Ticks);

                // The "ideal" interval
                var backedOff = checked((long)(backoffPeriod * backoffFactor));

                // Capped to the maximum interval
                var cappedBackoff = Math.Min(MaximumBackoffInterval.Ticks, backedOff);

                // Unless that's shorter than the base interval, in which case we'll just apply the period
                var actual = Math.Max(period.Ticks, cappedBackoff);

                return TimeSpan.FromTicks(actual);
            }
            catch (OverflowException)
            {
                return MaximumBackoffInterval;
            }
        }
    }
}