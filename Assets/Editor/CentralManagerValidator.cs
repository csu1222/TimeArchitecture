using System;
using UnityEditor;
using UnityEngine;

public static class CentralManagerValidator
{
    [MenuItem("Tools/Time Architecture/Validate Central Time Manager")]
    public static void Validate()
    {
        GameObject runtime = new GameObject("CentralManagerValidation");
        runtime.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            CentralTimeManager manager = runtime.AddComponent<CentralTimeManager>();
            ValidateCalendar(manager);
            ValidateTrade(manager);
            CentralManagerDebugSource source = runtime.AddComponent<CentralManagerDebugSource>();
            CentralManagerSceneBuilder.SetReference(source, "manager", manager);
            Require(!typeof(IManualTimeDebugCommand).IsAssignableFrom(source.GetType()),
                "Manual time must not be implemented.");
            TimeArchitectureDebugSnapshot snapshot = source.GetSnapshot();
            Require(!snapshot.SupportsManualTime && !snapshot.IsManualTime, "Manual time flags.");
            CentralCalendarData calendar = manager.CalculateCalendar(snapshot.CurrentUtc);
            Require(snapshot.GameDayIndex == calendar.GameDayIndex &&
                snapshot.Season == manager.CalculateSeason(calendar).ToString() &&
                snapshot.SeasonDay == manager.CalculateSeasonDay(calendar) &&
                snapshot.ElapsedFromEpochSeconds == manager.CalculateElapsedSeconds(snapshot.CurrentUtc),
                "Snapshot must use one UTC.");
            Debug.Log("Central Manager calculation validation: PASS (calendar, all season boundaries, trade, reset, snapshot, manual unsupported)");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(runtime);
        }
    }

    private static void ValidateCalendar(CentralTimeManager manager)
    {
        DateTime epoch = CentralManagerConstants.EpochUtc;
        Require(manager.CalculateCalendar(epoch.AddTicks(-1)).GameDayIndex == 0, "Before epoch clamp.");
        Require(manager.CalculateElapsedSeconds(epoch.AddSeconds(-1)) == 0, "Elapsed clamp.");
        // 2년의 각 날짜와 직전 시각을 검사하여 월·계절·연도 경계를 검증합니다.
        for (int index = 0; index <= 720; index++)
        {
            DateTime utc = epoch.AddSeconds(index * 2d);
            CentralCalendarData actual = manager.CalculateCalendar(utc);
            int dayOfYearIndex = index % 360;
            Require(actual.GameDayIndex == index && actual.Year == index / 360 + 1 &&
                actual.Month == dayOfYearIndex / 30 + 1 && actual.Day == dayOfYearIndex % 30 + 1 &&
                actual.DayOfYear == dayOfYearIndex + 1, $"Calendar at day {index}.");
            Require((int)manager.CalculateSeason(actual) == dayOfYearIndex / 90 &&
                manager.CalculateSeasonDay(actual) == dayOfYearIndex % 90 + 1,
                $"Season at day {index}.");
            Require(manager.CalculateCalendar(utc.AddTicks(-1)).GameDayIndex == Math.Max(0, index - 1),
                $"Calendar before day {index}.");
        }
    }

    private static void ValidateTrade(CentralTimeManager manager)
    {
        Require(manager.State == CentralTradeState.Idle, "Initial Idle.");
        manager.StartTrade();
        DateTime start = manager.StartUtc.Value;
        DateTime end = manager.EndUtc.Value;
        Require(manager.State == CentralTradeState.Traveling && end == start.AddSeconds(60), "Trade start/end.");
        manager.StartTrade();
        Require(manager.StartUtc == start && manager.EndUtc == end, "Repeated start must be ignored.");
        Require(manager.CalculateTradeRemaining(start) == 60d, "Initial remaining.");
        manager.EvaluateTrade(end.AddTicks(-1));
        Require(manager.State == CentralTradeState.Traveling, "Early completion.");
        manager.EvaluateTrade(end);
        Require(manager.State == CentralTradeState.Completed, "Completion at EndUtc.");
        Require(manager.CalculateTradeRemaining(end.AddSeconds(1)) == 0d, "Remaining clamp.");
        ValidateReset(manager);
        manager.StartTrade();
        ValidateReset(manager);
        manager.StartTrade();
        manager.EvaluateTrade(manager.EndUtc.Value);
        manager.StartTrade();
        Require(manager.State == CentralTradeState.Traveling, "Restart after completion.");
    }

    private static void ValidateReset(CentralTimeManager manager)
    {
        manager.ResetTrade();
        Require(manager.State == CentralTradeState.Idle && !manager.StartUtc.HasValue &&
            !manager.EndUtc.HasValue && !manager.GetTradeRemainingSeconds().HasValue, "Reset timestamps/state.");
    }

    internal static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Central Manager validation: " + message);
        }
    }
}
