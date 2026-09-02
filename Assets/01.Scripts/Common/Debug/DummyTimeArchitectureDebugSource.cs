using System;
using UnityEngine;

/// <summary>
/// 공통 Panel의 표시와 Command 전달만 확인하기 위한 임시 Debug 구현입니다.
/// </summary>
public sealed class DummyTimeArchitectureDebugSource : MonoBehaviour,
    ITimeArchitectureDebugSource,
    ITimeArchitectureDebugCommand,
    IManualTimeDebugCommand
{
    private static readonly DateTime EpochUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const double TradeDurationSeconds = 60d;

    private DateTime currentUtc = new DateTime(2026, 3, 30, 12, 0, 0, DateTimeKind.Utc);
    private DateTime? tradeStartUtc;
    private DateTime? tradeEndUtc;
    private bool isManualTime;
    private int addedGameDays;

    private void Update()
    {
        if (!isManualTime)
        {
            currentUtc = currentUtc.AddSeconds(Time.unscaledDeltaTime);
        }
    }

    public TimeArchitectureDebugSnapshot GetSnapshot()
    {
        string tradeState = GetTradeState();
        double? remaining = tradeEndUtc.HasValue && tradeState == "Traveling"
            ? Math.Max(0d, (tradeEndUtc.Value - currentUtc).TotalSeconds)
            : (double?)null;

        return new TimeArchitectureDebugSnapshot(
            "Debug Dummy",
            isManualTime ? "Dummy Manual Source" : "Dummy Source",
            currentUtc,
            (currentUtc - EpochUtc).TotalSeconds,
            Time.time,
            Time.timeScale,
            1,
            3,
            30 + addedGameDays,
            90 + addedGameDays,
            89 + addedGameDays,
            "Spring",
            90 + addedGameDays,
            tradeState,
            tradeStartUtc,
            tradeEndUtc,
            remaining,
            true,
            isManualTime);
    }

    public void StartTrade()
    {
        tradeStartUtc = currentUtc;
        tradeEndUtc = currentUtc.AddSeconds(TradeDurationSeconds);
    }

    public void ResetTrade()
    {
        tradeStartUtc = null;
        tradeEndUtc = null;
    }

    public void SetTimeScale(float value)
    {
        Time.timeScale = value;
    }

    public void UseSystemTime()
    {
        isManualTime = false;
    }

    public void UseManualTime()
    {
        isManualTime = true;
    }

    public void AddGameDays(int days)
    {
        currentUtc = currentUtc.AddSeconds(days * 2d);
        addedGameDays += days;
    }

    public void ResetManualTime()
    {
        currentUtc = new DateTime(2026, 3, 30, 12, 0, 0, DateTimeKind.Utc);
        addedGameDays = 0;
    }

    private string GetTradeState()
    {
        if (!tradeEndUtc.HasValue)
        {
            return "Idle";
        }

        return currentUtc >= tradeEndUtc.Value ? "Completed" : "Traveling";
    }
}
