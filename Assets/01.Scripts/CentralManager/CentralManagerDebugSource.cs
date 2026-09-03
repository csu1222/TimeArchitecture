using System;
using UnityEngine;

public sealed class CentralManagerDebugSource : MonoBehaviour,
    ITimeArchitectureDebugSource,
    ITimeArchitectureDebugCommand
{
    [Tooltip("시간 조회와 도메인 계산을 담당하는 중앙 Manager")]
    [SerializeField] private CentralTimeManager manager;

    public TimeArchitectureDebugSnapshot GetSnapshot()
    {
        // 한 Snapshot의 모든 도메인 값은 Manager가 읽은 동일 UTC를 사용합니다.
        DateTime utc = manager.GetCurrentUtc();
        CentralCalendarData calendar = manager.CalculateCalendar(utc);
        manager.EvaluateTrade(utc);

        return new TimeArchitectureDebugSnapshot(
            "Central Time Manager",
            "CentralTimeManager",
            utc,
            manager.CalculateElapsedSeconds(utc),
            Time.time,
            Time.timeScale,
            calendar.Year,
            calendar.Month,
            calendar.Day,
            calendar.DayOfYear,
            calendar.GameDayIndex,
            manager.CalculateSeason(calendar).ToString(),
            manager.CalculateSeasonDay(calendar),
            manager.State.ToString(),
            manager.StartUtc,
            manager.EndUtc,
            manager.CalculateTradeRemaining(utc),
            false,
            false);
    }

    public void StartTrade() => manager.StartTrade();
    public void ResetTrade() => manager.ResetTrade();
    public void SetTimeScale(float value) => manager.SetTimeScale(value);
}
