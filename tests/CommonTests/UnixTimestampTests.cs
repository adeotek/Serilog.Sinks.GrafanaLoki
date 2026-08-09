using Serilog.Sinks.GrafanaLoki.Common;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.CommonTests;

public class UnixTimestampTests
{
    [Fact]
    public void GetUnixNanoSeconds_ReturnsUnixEpochInNanoseconds()
    {
        var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var result = UnixTimestamp.GetUnixNanoSeconds(epoch);

        Assert.Equal(0, result);
    }

    [Fact]
    public void GetUnixNanoSeconds_ReturnsCorrectValueForKnownTimestamp()
    {
        // 2020-01-01T00:00:00Z = 1577836800 seconds since epoch = 1577836800000000000 nanoseconds
        var dt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var result = UnixTimestamp.GetUnixNanoSeconds(dt);

        Assert.Equal(1577836800000000000L, result);
    }

    [Fact]
    public void GetUnixNanoSeconds_HandlesPositiveOffset()
    {
        var dt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(3));

        var result = UnixTimestamp.GetUnixNanoSeconds(dt);

        // Should be the same as the UTC equivalent
        var utcExpected = UnixTimestamp.GetUnixNanoSeconds(new DateTimeOffset(2019, 12, 31, 21, 0, 0, TimeSpan.Zero));
        Assert.Equal(utcExpected, result);
    }

    [Fact]
    public void GetUnixNanoSeconds_IsMonotonicallyIncreasing()
    {
        var t1 = UnixTimestamp.GetUnixNanoSeconds(new DateTimeOffset(2023, 6, 15, 10, 0, 0, TimeSpan.Zero));
        var t2 = UnixTimestamp.GetUnixNanoSeconds(new DateTimeOffset(2023, 6, 15, 10, 0, 1, TimeSpan.Zero));

        Assert.True(t2 > t1);
        Assert.Equal(1_000_000_000L, t2 - t1);
    }

    [Fact]
    public void GetUnixTimestamp_ReturnsStringRepresentation()
    {
        // Must use Unspecified or Local kind — GetUnixTimestamp internally
        // constructs DateTimeOffset with local offset, which fails for Utc.
        var dt = new DateTime(2023, 6, 15, 10, 0, 0, DateTimeKind.Unspecified);

        var result = UnixTimestamp.GetUnixTimestamp(dt);

        Assert.False(string.IsNullOrEmpty(result));
        Assert.True(long.Parse(result) > 0);
    }

    [Fact]
    public void GetUnixTimestamp_WithoutParameter_ReturnsCurrentTimestamp()
    {
        var result = UnixTimestamp.GetUnixTimestamp();

        Assert.False(string.IsNullOrEmpty(result));
        var nanoseconds = long.Parse(result);
        Assert.True(nanoseconds > 0);
    }
}
