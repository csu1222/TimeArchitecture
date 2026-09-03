using System;
using UnityEngine;

public sealed class CalendarService : MonoBehaviour
{
    [Tooltip("ITimeProvider를 구현한 시간 접근점")]
    [SerializeField] private MonoBehaviour timeProviderBehaviour;

    private ITimeProvider TimeProvider => (ITimeProvider)timeProviderBehaviour;

    public ProviderCalendarData GetCurrentCalendar() => Calculate(TimeProvider.UtcNow);

    public double CalculateElapsedSeconds(DateTime utc)
    {
        return Math.Max(0d, (utc - ProviderConstants.EpochUtc).TotalSeconds);
    }

    public ProviderCalendarData Calculate(DateTime utc)
    {
        int gameDayIndex = checked((int)Math.Floor(
            CalculateElapsedSeconds(utc) / ProviderConstants.RealSecondsPerGameDay));
        int dayOfYearIndex = gameDayIndex % ProviderConstants.DaysPerYear;
        return new ProviderCalendarData(
            gameDayIndex / ProviderConstants.DaysPerYear + 1,
            dayOfYearIndex / ProviderConstants.DaysPerMonth + 1,
            dayOfYearIndex % ProviderConstants.DaysPerMonth + 1,
            dayOfYearIndex + 1,
            gameDayIndex);
    }
}
