using Serilog.Sinks.GrafanaLoki.Common;
using Shouldly;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.CommonTests;

public class UnixTimestampTests
{
    [Fact]
    public void GetUnixNanoSeconds_ReturnsUnixEpochInNanoseconds()
    {
        var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var result = UnixTimestamp.GetUnixNanoSeconds(epoch);

        result.ShouldBe(0);
    }

    [Fact]
    public void GetUnixNanoSeconds_ReturnsCorrectValueForKnownTimestamp()
    {
        // 2020-01-01T00:00:00Z = 1577836800 seconds since epoch = 1577836800000000000 nanoseconds
        var dt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var result = UnixTimestamp.GetUnixNanoSeconds(dt);

        result.ShouldBe(1577836800000000000L);
    }

    [Fact]
    public void GetUnixNanoSeconds_HandlesPositiveOffset()
    {
        var dt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(3));

        var result = UnixTimestamp.GetUnixNanoSeconds(dt);

        // Should be the same as the UTC equivalent
        var utcExpected = UnixTimestamp.GetUnixNanoSeconds(new DateTimeOffset(2019, 12, 31, 21, 0, 0, TimeSpan.Zero));
        result.ShouldBe(utcExpected);
    }

    [Fact]
    public void GetUnixNanoSeconds_IsMonotonicallyIncreasing()
    {
        var t1 = UnixTimestamp.GetUnixNanoSeconds(new DateTimeOffset(2023, 6, 15, 10, 0, 0, TimeSpan.Zero));
        var t2 = UnixTimestamp.GetUnixNanoSeconds(new DateTimeOffset(2023, 6, 15, 10, 0, 1, TimeSpan.Zero));

        t2.ShouldBeGreaterThan(t1);
        (t2 - t1).ShouldBe(1_000_000_000L); // 1 second = 1e9 nanoseconds
    }

    [Fact]
    public void GetUnixTimestamp_ReturnsStringRepresentation()
    {
        // Must use Unspecified or Local kind — GetUnixTimestamp internally
        // constructs DateTimeOffset with local offset, which fails for Utc.
        var dt = new DateTime(2023, 6, 15, 10, 0, 0, DateTimeKind.Unspecified);

        var result = UnixTimestamp.GetUnixTimestamp(dt);

        result.ShouldNotBeNullOrEmpty();
        long.Parse(result).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GetUnixTimestamp_WithoutParameter_ReturnsCurrentTimestamp()
    {
        var result = UnixTimestamp.GetUnixTimestamp();

        result.ShouldNotBeNullOrEmpty();
        var nanoseconds = long.Parse(result);
        nanoseconds.ShouldBeGreaterThan(0);
    }
}
