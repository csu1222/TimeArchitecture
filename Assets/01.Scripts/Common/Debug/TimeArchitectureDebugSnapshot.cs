using System;

/// <summary>
/// 각 시간 아키텍처가 Debug Panel에 한 번에 전달하는 읽기 전용 표시 데이터입니다.
/// </summary>
public readonly struct TimeArchitectureDebugSnapshot
{
    public TimeArchitectureDebugSnapshot(
        string strategyName,
        string timeSourceName,
        DateTime currentUtc,
        double elapsedFromEpochSeconds,
        float unityTime,
        float timeScale,
        int year,
        int month,
        int day,
        int dayOfYear,
        int gameDayIndex,
        string season,
        int seasonDay,
        string tradeState,
        DateTime? tradeStartUtc,
        DateTime? tradeEndUtc,
        double? tradeRemainingSeconds,
        bool supportsManualTime,
        bool isManualTime)
    {
        StrategyName = strategyName;
        TimeSourceName = timeSourceName;
        CurrentUtc = currentUtc;
        ElapsedFromEpochSeconds = elapsedFromEpochSeconds;
        UnityTime = unityTime;
        TimeScale = timeScale;
        Year = year;
        Month = month;
        Day = day;
        DayOfYear = dayOfYear;
        GameDayIndex = gameDayIndex;
        Season = season;
        SeasonDay = seasonDay;
        TradeState = tradeState;
        TradeStartUtc = tradeStartUtc;
        TradeEndUtc = tradeEndUtc;
        TradeRemainingSeconds = tradeRemainingSeconds;
        SupportsManualTime = supportsManualTime;
        IsManualTime = isManualTime;
    }

    public string StrategyName { get; }
    public string TimeSourceName { get; }
    public DateTime CurrentUtc { get; }
    public double ElapsedFromEpochSeconds { get; }
    public float UnityTime { get; }
    public float TimeScale { get; }
    public int Year { get; }
    public int Month { get; }
    public int Day { get; }
    public int DayOfYear { get; }
    public int GameDayIndex { get; }
    public string Season { get; }
    public int SeasonDay { get; }
    public string TradeState { get; }
    public DateTime? TradeStartUtc { get; }
    public DateTime? TradeEndUtc { get; }
    public double? TradeRemainingSeconds { get; }
    public bool SupportsManualTime { get; }
    public bool IsManualTime { get; }
}
