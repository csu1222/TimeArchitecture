using System;
using UnityEditor;
using UnityEngine;

public static class ProviderValidator
{
    [MenuItem("Tools/Time Architecture/Validate Provider Architecture")]
    public static void Validate()
    {
        GameObject root = new GameObject("ProviderValidation");
        root.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            SystemUtcTimeProvider system = root.AddComponent<SystemUtcTimeProvider>();
            ManualTimeProvider manual = root.AddComponent<ManualTimeProvider>();
            ProviderRuntimeController runtime = root.AddComponent<ProviderRuntimeController>();
            CalendarService calendar = root.AddComponent<CalendarService>();
            SeasonResolver season = root.AddComponent<SeasonResolver>();
            TradeService trade = root.AddComponent<TradeService>();
            ProviderDebugSource source = root.AddComponent<ProviderDebugSource>();
            ProviderSceneBuilder.SetReference(manual, "systemProvider", system);
            ProviderSceneBuilder.SetReference(runtime, "systemProvider", system);
            ProviderSceneBuilder.SetReference(runtime, "manualProvider", manual);
            ProviderSceneBuilder.SetReference(calendar, "timeProviderBehaviour", runtime);
            ProviderSceneBuilder.SetReference(trade, "timeProviderBehaviour", runtime);
            ProviderSceneBuilder.SetReference(source, "providerRuntime", runtime);
            ProviderSceneBuilder.SetReference(source, "calendar", calendar);
            ProviderSceneBuilder.SetReference(source, "season", season);
            ProviderSceneBuilder.SetReference(source, "trade", trade);

            source.UseManualTime();
            manual.SetUtc(ProviderConstants.EpochUtc.AddTicks(-1));
            Require(calendar.GetCurrentCalendar().GameDayIndex == 0 &&
                source.GetSnapshot().ElapsedFromEpochSeconds == 0, "Before epoch clamp.");
            // 입력은 계산 함수의 인자가 아니라 실제 공급자입니다.
            for (int index = 0; index <= 720; index++)
            {
                manual.SetUtc(ProviderConstants.EpochUtc.AddSeconds(index * 2d));
                TimeArchitectureDebugSnapshot actual = source.GetSnapshot();
                ProviderCalendarData date = calendar.GetCurrentCalendar();
                int day = index % 360;
                Require(actual.GameDayIndex == index && actual.Year == index / 360 + 1 &&
                    actual.Month == day / 30 + 1 && actual.Day == day % 30 + 1 &&
                    actual.DayOfYear == day + 1 && date.GameDayIndex == index &&
                    actual.Season == ((ProviderSeason)(day / 90)).ToString() &&
                    actual.SeasonDay == day % 90 + 1, "Manual runtime calendar day " + index);
                manual.SetUtc(manual.UtcNow.AddTicks(-1));
                Require(calendar.GetCurrentCalendar().GameDayIndex == Math.Max(0, index - 1),
                    "Before day boundary " + index);
            }

            manual.SetUtc(ProviderConstants.EpochUtc);
            source.StartTrade();
            DateTime start = trade.StartUtc.Value;
            Require(trade.State == ProviderTradeState.Traveling &&
                trade.GetRemainingSeconds() == 60d && trade.EndUtc == start.AddSeconds(60), "Trade start.");
            source.StartTrade();
            Require(trade.StartUtc == start, "Repeated start ignored.");
            manual.SetUtc(start.AddSeconds(60).AddTicks(-1));
            Require(source.GetSnapshot().TradeState == "Traveling", "Not completed before EndUtc.");
            manual.SetUtc(start.AddSeconds(60));
            Require(source.GetSnapshot().TradeState == "Completed" && trade.GetRemainingSeconds() == 0d,
                "Completion at EndUtc through provider.");
            manual.SetUtc(start);
            Require(source.GetSnapshot().TradeState == "Completed" && trade.GetRemainingSeconds() == 0d,
                "Completed trade stays completed after clock rewind.");
            source.ResetTrade();
            Require(!trade.StartUtc.HasValue && !trade.EndUtc.HasValue &&
                trade.State == ProviderTradeState.Idle && !trade.GetRemainingSeconds().HasValue, "Reset.");
            source.StartTrade();
            source.AddGameDays(30);
            Require(source.GetSnapshot().TradeState == "Completed", "+1 Month completes trade.");
            source.StartTrade();
            Require(trade.State == ProviderTradeState.Traveling, "Restart completed trade.");
            Debug.Log("Provider architecture validation: PASS (721 manual runtime dates, tick boundaries, trade boundary, rewind, reset, restart)");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    internal static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Provider validation: " + message);
        }
    }
}
