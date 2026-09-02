using System;
using UnityEngine;

public readonly struct DirectCalendarData
{
    public DirectCalendarData(int year, int month, int day, int dayOfYear, int gameDayIndex)
    {
        Year = year;
        Month = month;
        Day = day;
        DayOfYear = dayOfYear;
        GameDayIndex = gameDayIndex;
    }

    public int Year { get; }
    public int Month { get; }
    public int Day { get; }
    public int DayOfYear { get; }
    public int GameDayIndex { get; }
}

public sealed class DirectCalendar : MonoBehaviour
{
    public DateTime GetCurrentUtc()
    {
        return DateTime.UtcNow;
    }

    public DirectCalendarData GetCurrentCalendar()
    {
        return Calculate(DateTime.UtcNow);
    }

    public DirectCalendarData Calculate(DateTime utc)
    {
        double elapsedSeconds = Math.Max(0d, (utc - DirectAccessConstants.EpochUtc).TotalSeconds);
        int gameDayIndex = (int)Math.Floor(elapsedSeconds / DirectAccessConstants.RealSecondsPerGameDay);
        int dayOfYearIndex = gameDayIndex % DirectAccessConstants.DaysPerYear;

        return new DirectCalendarData(
            gameDayIndex / DirectAccessConstants.DaysPerYear + 1,
            dayOfYearIndex / DirectAccessConstants.DaysPerMonth + 1,
            dayOfYearIndex % DirectAccessConstants.DaysPerMonth + 1,
            dayOfYearIndex + 1,
            gameDayIndex);
    }
}
