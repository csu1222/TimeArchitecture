using System;
using UnityEngine;

// Option 2는 UTC 접근과 Calendar / Season / Trade / TimeScale 책임을 의도적으로 모읍니다.
public sealed class CentralTimeManager : MonoBehaviour
{
    private CentralTradeState state = CentralTradeState.Idle;
    private DateTime? startUtc;
    private DateTime? endUtc;

    public CentralTradeState State => state;
    public DateTime? StartUtc => startUtc;
    public DateTime? EndUtc => endUtc;

    public DateTime GetCurrentUtc() => DateTime.UtcNow;

    public double CalculateElapsedSeconds(DateTime utc)
    {
        return Math.Max(0d, (utc - CentralManagerConstants.EpochUtc).TotalSeconds);
    }

    public CentralCalendarData GetCurrentCalendar() => CalculateCalendar(GetCurrentUtc());

    public CentralCalendarData CalculateCalendar(DateTime utc)
    {
        int gameDayIndex = (int)Math.Floor(
            CalculateElapsedSeconds(utc) / CentralManagerConstants.RealSecondsPerGameDay);
        int dayOfYearIndex = gameDayIndex % CentralManagerConstants.DaysPerYear;
        return new CentralCalendarData(
            gameDayIndex / CentralManagerConstants.DaysPerYear + 1,
            dayOfYearIndex / CentralManagerConstants.DaysPerMonth + 1,
            dayOfYearIndex % CentralManagerConstants.DaysPerMonth + 1,
            dayOfYearIndex + 1,
            gameDayIndex);
    }

    public CentralSeason CalculateSeason(CentralCalendarData calendar)
    {
        return (CentralSeason)((calendar.Month - 1) / CentralManagerConstants.MonthsPerSeason);
    }

    public int CalculateSeasonDay(CentralCalendarData calendar)
    {
        return (calendar.Month - 1) % CentralManagerConstants.MonthsPerSeason *
            CentralManagerConstants.DaysPerMonth + calendar.Day;
    }

    private void Update()
    {
        EvaluateTrade(GetCurrentUtc());
    }

    public void StartTrade()
    {
        if (state == CentralTradeState.Traveling)
        {
            return;
        }

        DateTime utc = GetCurrentUtc();
        startUtc = utc;
        endUtc = utc.AddSeconds(CentralManagerConstants.TradeDurationSeconds);
        state = CentralTradeState.Traveling;
    }

    // Snapshot도 같은 UTC로 완료 상태를 갱신하여 상태와 Remaining 표시를 맞춥니다.
    public void EvaluateTrade(DateTime utc)
    {
        if (state == CentralTradeState.Traveling && utc >= endUtc)
        {
            state = CentralTradeState.Completed;
        }
    }

    public double? GetTradeRemainingSeconds() => CalculateTradeRemaining(GetCurrentUtc());

    public double? CalculateTradeRemaining(DateTime utc)
    {
        return endUtc.HasValue ? Math.Max(0d, (endUtc.Value - utc).TotalSeconds) : (double?)null;
    }

    public void ResetTrade()
    {
        state = CentralTradeState.Idle;
        startUtc = null;
        endUtc = null;
    }

    public void SetTimeScale(float value)
    {
        if (value != 0f && value != 1f && value != 2f && value != 5f)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Supported time scales: 0, 1, 2, 5.");
        }

        Time.timeScale = value;
    }
}
