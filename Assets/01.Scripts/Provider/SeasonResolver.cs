using UnityEngine;

public sealed class SeasonResolver : MonoBehaviour
{
    public ProviderSeason Resolve(ProviderCalendarData calendar)
    {
        return (ProviderSeason)((calendar.Month - 1) / ProviderConstants.MonthsPerSeason);
    }

    public int GetSeasonDay(ProviderCalendarData calendar)
    {
        return (calendar.Month - 1) % ProviderConstants.MonthsPerSeason *
            ProviderConstants.DaysPerMonth + calendar.Day;
    }
}
