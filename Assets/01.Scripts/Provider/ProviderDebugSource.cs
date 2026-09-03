using System;
using UnityEngine;

public sealed class ProviderDebugSource : MonoBehaviour,
    ITimeArchitectureDebugSource, ITimeArchitectureDebugCommand, IManualTimeDebugCommand
{
    [Tooltip("현재 시간 공급자 선택")]
    [SerializeField] private ProviderRuntimeController providerRuntime;
    [Tooltip("UTC를 게임 날짜로 변환")]
    [SerializeField] private CalendarService calendar;
    [Tooltip("게임 날짜의 계절 해석")]
    [SerializeField] private SeasonResolver season;
    [Tooltip("UTC 기반 무역 상태")]
    [SerializeField] private TradeService trade;

    public TimeArchitectureDebugSnapshot GetSnapshot()
    {
        // 표시 기준 UTC는 한 번만 읽고 도메인 계산은 각 Service에 맡깁니다.
        DateTime utc = providerRuntime.UtcNow;
        ProviderCalendarData date = calendar.Calculate(utc);
        double? remaining = trade.GetRemainingSeconds(utc);
        return new TimeArchitectureDebugSnapshot(
            "Time Provider + Domain Services",
            providerRuntime.IsManualTime ? nameof(ManualTimeProvider) : nameof(SystemUtcTimeProvider),
            utc, calendar.CalculateElapsedSeconds(utc), Time.time, Time.timeScale,
            date.Year, date.Month, date.Day, date.DayOfYear, date.GameDayIndex,
            season.Resolve(date).ToString(), season.GetSeasonDay(date),
            trade.State.ToString(), trade.StartUtc, trade.EndUtc, remaining,
            true, providerRuntime.IsManualTime);
    }

    public void StartTrade() => trade.StartTrade();
    public void ResetTrade() => trade.ResetTrade();
    public void UseSystemTime() => providerRuntime.UseSystemTime();
    public void UseManualTime() => providerRuntime.UseManualTime();
    public void AddGameDays(int days) => providerRuntime.AddGameDays(days);
    public void ResetManualTime() => providerRuntime.ResetManualTime();

    public void SetTimeScale(float value)
    {
        if (value != 0f && value != 1f && value != 2f && value != 5f)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Supported time scales: 0, 1, 2, 5.");
        }
        Time.timeScale = value;
    }
}
