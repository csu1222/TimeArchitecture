using System;

public static class CentralManagerConstants
{
    public static readonly DateTime EpochUtc =
        new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public const double RealSecondsPerGameDay = 2.0;
    public const int DaysPerMonth = 30;
    public const int MonthsPerYear = 12;
    public const int DaysPerYear = DaysPerMonth * MonthsPerYear;
    public const int MonthsPerSeason = 3;
    public const int DaysPerSeason = DaysPerMonth * MonthsPerSeason;
    public const double TradeDurationSeconds = 60.0;
}
