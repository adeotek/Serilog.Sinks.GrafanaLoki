using Serilog.Sinks.GrafanaLoki.Common;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.CommonTests;

public class ExponentialBackoffConnectionScheduleTests
{
    [Fact]
    public void Constructor_AcceptsValidPeriod()
    {
        var period = TimeSpan.FromSeconds(2);

        var schedule = new ExponentialBackoffConnectionSchedule(period);

        Assert.NotNull(schedule);
    }

    [Fact]
    public void Constructor_ThrowsOnNegativePeriod()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExponentialBackoffConnectionSchedule(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void Constructor_AcceptsZeroPeriod()
    {
        var schedule = new ExponentialBackoffConnectionSchedule(TimeSpan.Zero);

        Assert.NotNull(schedule);
    }

    [Fact]
    public void NextInterval_ReturnsPeriodOnFirstFailure()
    {
        var period = TimeSpan.FromSeconds(2);
        var schedule = new ExponentialBackoffConnectionSchedule(period);

        schedule.MarkFailure();

        Assert.Equal(period, schedule.NextInterval);
    }

    [Fact]
    public void NextInterval_ReturnsPeriodAfterSuccess()
    {
        var period = TimeSpan.FromSeconds(2);
        var schedule = new ExponentialBackoffConnectionSchedule(period);
        schedule.MarkFailure();
        schedule.MarkFailure();

        schedule.MarkSuccess();

        Assert.Equal(period, schedule.NextInterval);
    }

    [Fact]
    public void NextInterval_IncreasesExponentiallyWithFailures()
    {
        var period = TimeSpan.FromSeconds(2);
        var schedule = new ExponentialBackoffConnectionSchedule(period);

        // 1 failure: use period
        schedule.MarkFailure();
        var interval1 = schedule.NextInterval;
        Assert.Equal(period, interval1);

        // 2 failures: 2x
        schedule.MarkFailure();
        var interval2 = schedule.NextInterval;
        Assert.True(interval2 > interval1);

        // 3 failures: 4x
        schedule.MarkFailure();
        var interval3 = schedule.NextInterval;
        Assert.True(interval3 > interval2);
    }

    [Fact]
    public void NextInterval_CapsAtMaximumBackoff()
    {
        var period = TimeSpan.FromSeconds(1);
        var schedule = new ExponentialBackoffConnectionSchedule(period);

        // Trigger many failures to hit the cap
        for (var i = 0; i < 20; i++)
        {
            schedule.MarkFailure();
        }

        Assert.True(schedule.NextInterval <= ExponentialBackoffConnectionSchedule.MaximumBackoffInterval);
    }

    [Fact]
    public void NextInterval_RespectsMinimumBackoffForShortPeriods()
    {
        var veryShortPeriod = TimeSpan.FromMilliseconds(100);
        var schedule = new ExponentialBackoffConnectionSchedule(veryShortPeriod);

        // After multiple failures, backoff should be at least the minimum
        schedule.MarkFailure();
        schedule.MarkFailure();
        schedule.MarkFailure();

        Assert.True(schedule.NextInterval >= ExponentialBackoffConnectionSchedule.MinimumBackoffPeriod);
    }

    [Fact]
    public void MarkSuccess_ResetsFailureCount()
    {
        var period = TimeSpan.FromSeconds(2);
        var schedule = new ExponentialBackoffConnectionSchedule(period);

        schedule.MarkFailure();
        schedule.MarkFailure();
        schedule.MarkSuccess();

        Assert.Equal(period, schedule.NextInterval);
    }
}
