using System;
using UnityEditor;
using UnityEngine;

public static class DirectAccessValidator
{
    [MenuItem("Tools/Time Architecture/Validate Direct Access")]
    public static void Validate()
    {
        GameObject runtime = new GameObject("DirectAccessValidation");
        try
        {
            DirectCalendar calendar = runtime.AddComponent<DirectCalendar>();
            DirectSeason season = runtime.AddComponent<DirectSeason>();
            DirectTrade trade = runtime.AddComponent<DirectTrade>();

            ValidateCalendar(calendar);
            ValidateSeason(season);
            ValidateTrade(trade);
            Require(!(runtime.AddComponent<DirectAccessDebugSource>() is IManualTimeDebugCommand),
                "Direct Access must not implement IManualTimeDebugCommand.");

            Debug.Log("Direct Access calculation validation: PASS");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Direct Access calculation validation: FAIL\n{exception}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(runtime);
        }
    }

    private static void ValidateCalendar(DirectCalendar calendar)
    {
        RequireDate(calendar.Calculate(AtGameDay(0)), 1, 1, 1, 1, 0);
        RequireDate(calendar.Calculate(AtGameDay(29)), 1, 1, 30, 30, 29);
        RequireDate(calendar.Calculate(AtGameDay(30)), 1, 2, 1, 31, 30);
        RequireDate(calendar.Calculate(AtGameDay(359)), 1, 12, 30, 360, 359);
        RequireDate(calendar.Calculate(AtGameDay(360)), 2, 1, 1, 1, 360);
        RequireDate(calendar.Calculate(DirectAccessConstants.EpochUtc.AddSeconds(-1)), 1, 1, 1, 1, 0);
    }

    private static void ValidateSeason(DirectSeason season)
    {
        RequireSeason(season.Calculate(3, 30), DemoSeason.Spring, 90);
        RequireSeason(season.Calculate(4, 1), DemoSeason.Summer, 1);
        RequireSeason(season.Calculate(12, 30), DemoSeason.Winter, 90);
        RequireSeason(season.Calculate(1, 1), DemoSeason.Spring, 1);
    }

    private static void ValidateTrade(DirectTrade trade)
    {
        DateTime start = DirectAccessConstants.EpochUtc;
        trade.StartTradeAt(start);
        Require(trade.State == DemoTradeState.Traveling, "Trade did not enter Traveling.");
        Require(trade.EndUtc == start.AddSeconds(60d), "Trade end is not start + 60 seconds.");
        Require(Math.Abs(trade.GetRemainingSeconds(start).Value - 60d) < 0.001d,
            "Trade remaining did not start at 60 seconds.");
        trade.Evaluate(start.AddSeconds(60d));
        Require(trade.State == DemoTradeState.Completed, "Trade did not complete at EndUtc.");
        Require(trade.GetRemainingSeconds(start.AddSeconds(61d)) == 0d,
            "Completed trade remaining was not clamped to zero.");
        trade.ResetTrade();
        Require(trade.State == DemoTradeState.Idle, "Trade reset did not enter Idle.");
        Require(!trade.StartUtc.HasValue && !trade.EndUtc.HasValue,
            "Trade reset did not clear timestamps.");
        Require(!trade.GetRemainingSeconds(start).HasValue,
            "Trade reset did not clear remaining time.");
    }

    private static DateTime AtGameDay(int gameDayIndex)
    {
        return DirectAccessConstants.EpochUtc.AddSeconds(
            gameDayIndex * DirectAccessConstants.RealSecondsPerGameDay);
    }

    private static void RequireDate(
        DirectCalendarData actual,
        int year,
        int month,
        int day,
        int dayOfYear,
        int gameDayIndex)
    {
        Require(actual.Year == year && actual.Month == month && actual.Day == day &&
                actual.DayOfYear == dayOfYear && actual.GameDayIndex == gameDayIndex,
            $"Unexpected calendar: Y{actual.Year} M{actual.Month} D{actual.Day}, " +
            $"day {actual.DayOfYear}, index {actual.GameDayIndex}.");
    }

    private static void RequireSeason(
        DirectSeasonData actual,
        DemoSeason season,
        int seasonDay)
    {
        Require(actual.Season == season && actual.SeasonDay == seasonDay,
            $"Unexpected season: {actual.Season}, day {actual.SeasonDay}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
