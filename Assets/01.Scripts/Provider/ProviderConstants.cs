using System;

public static class ProviderConstants
{
    public static readonly DateTime EpochUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public const double RealSecondsPerGameDay = 2d;
    public const int DaysPerMonth = 30;
    public const int MonthsPerYear = 12;
    public const int DaysPerYear = 360;
    public const int MonthsPerSeason = 3;
    public const int DaysPerSeason = 90;
    public const double TradeDurationSeconds = 60d;
}
