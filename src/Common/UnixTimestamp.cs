using System;

namespace Serilog.Sinks.GrafanaLoki.Common;

public static class UnixTimestamp
{
    // Number of 100ns ticks per time unit
    private const long NanosecondsPerTick = 100;
    private const long TicksPerMillisecond = 10000;
    private const long TicksPerSecond = TicksPerMillisecond * 1000;
    private const long TicksPerMinute = TicksPerSecond * 60;
    private const long TicksPerHour = TicksPerMinute * 60;
    private const long TicksPerDay = TicksPerHour * 24;

    private const int DaysPerYear = 365;
    // Number of days in 4 years
    private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
    // Number of days in 100 years
    private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
    // Number of days in 400 years
    private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097

    // Number of days from 1/1/0001 to 12/31/1600
    private const int DaysTo1601 = DaysPer400Years * 4;          // 584388
    // Number of days from 1/1/0001 to 12/30/1899
    private const int DaysTo1899 = DaysPer400Years * 4 + DaysPer100Years * 3 - 367;
    // Number of days from 1/1/0001 to 12/31/1969
    private const int DaysTo1970 = DaysPer400Years * 4 + DaysPer100Years * 3 + DaysPer4Years * 17 + DaysPerYear; // 719,162

    private const long UnixEpochTicks = DaysTo1970 * TicksPerDay;

    public static long GetUnixNanoSeconds(DateTimeOffset timestamp) => (timestamp.Ticks - UnixEpochTicks) * NanosecondsPerTick;

    public static string GetUnixTimestamp(DateTime? dateTime = null)
    {
        var localTime = dateTime ?? DateTime.Now;
        return GetUnixNanoSeconds(new DateTimeOffset(localTime, TimeZoneInfo.Local.GetUtcOffset(localTime))).ToString();
    }
}