using Serilog.Sinks.GrafanaLoki.Common;
using Shouldly;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.CommonTests;

public class ExponentialBackoffConnectionScheduleTests
{
    [Fact]
    public void Constructor_AcceptsValidPeriod()
    {
        var period = TimeSpan.FromSeconds(2);

        var schedule = new ExponentialBackoffConnectionSchedule(period);

        schedule.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ThrowsOnNegativePeriod()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new ExponentialBackoffConnectionSchedule(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void Constructor_AcceptsZeroPeriod()
    {
        var schedule = new ExponentialBackoffConnectionSchedule(TimeSpan.Zero);

        schedule.ShouldNotBeNull();
    }

    [Fact]
    public void NextInterval_ReturnsPeriodOnFirstFailure()
    {
        var period = TimeSpan.FromSeconds(2);
        var schedule = new ExponentialBackoffConnectionSchedule(period);

        schedule.MarkFailure();

        schedule.NextInterval.ShouldBe(period);
    }

    [Fact]
    public void NextInterval_ReturnsPeriodAfterSuccess()
    {
        var period = TimeSpan.FromSeconds(2);
        var schedule = new ExponentialBackoffConnectionSchedule(period);
        schedule.MarkFailure();
        schedule.MarkFailure();

        schedule.MarkSuccess();

        schedule.NextInterval.ShouldBe(period);
    }

    [Fact]
    public void NextInterval_IncreasesExponentiallyWithFailures()
    {
        var period = TimeSpan.FromSeconds(2);
        var schedule = new ExponentialBackoffConnectionSchedule(period);

        // 1 failure: use period
        schedule.MarkFailure();
        var interval1 = schedule.NextInterval;
        interval1.ShouldBe(period);

        // 2 failures: 2x
        schedule.MarkFailure();
        var interval2 = schedule.NextInterval;
        interval2.ShouldBeGreaterThan(interval1);

        // 3 failures: 4x
        schedule.MarkFailure();
        var interval3 = schedule.NextInterval;
        interval3.ShouldBeGreaterThan(interval2);
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

        schedule.NextInterval.ShouldBeLessThanOrEqualTo(ExponentialBackoffConnectionSchedule.MaximumBackoffInterval);
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

        schedule.NextInterval.ShouldBeGreaterThanOrEqualTo(ExponentialBackoffConnectionSchedule.MinimumBackoffPeriod);
    }

    [Fact]
    public void MarkSuccess_ResetsFailureCount()
    {
        var period = TimeSpan.FromSeconds(2);
        var schedule = new ExponentialBackoffConnectionSchedule(period);

        schedule.MarkFailure();
        schedule.MarkFailure();
        schedule.MarkSuccess();

        schedule.NextInterval.ShouldBe(period);
    }
}
