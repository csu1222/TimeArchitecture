using System;
using UnityEngine;

public sealed class DirectAccessDebugSource : MonoBehaviour,
    ITimeArchitectureDebugSource,
    ITimeArchitectureDebugCommand
{
    [SerializeField] private DirectCalendar calendar;
    [SerializeField] private DirectSeason season;
    [SerializeField] private DirectTrade trade;

    public TimeArchitectureDebugSnapshot GetSnapshot()
    {
        // 한 프레임의 표시 값은 Calendar가 직접 읽은 동일 UTC를 기준으로 계산합니다.
        DateTime currentUtc = calendar.GetCurrentUtc();
        DirectCalendarData calendarData = calendar.Calculate(currentUtc);
        DirectSeasonData seasonData = season.Calculate(calendarData.Month, calendarData.Day);
        trade.Evaluate(currentUtc);

        return new TimeArchitectureDebugSnapshot(
            "Direct Access",
            "DateTime.UtcNow",
            currentUtc,
            Math.Max(0d, (currentUtc - DirectAccessConstants.EpochUtc).TotalSeconds),
            Time.time,
            Time.timeScale,
            calendarData.Year,
            calendarData.Month,
            calendarData.Day,
            calendarData.DayOfYear,
            calendarData.GameDayIndex,
            seasonData.Season.ToString(),
            seasonData.SeasonDay,
            trade.State.ToString(),
            trade.StartUtc,
            trade.EndUtc,
            trade.GetRemainingSeconds(currentUtc),
            false,
            false);
    }

    public void StartTrade() => trade.StartTrade();
    public void ResetTrade() => trade.ResetTrade();
    public void SetTimeScale(float value) => Time.timeScale = value;
}
