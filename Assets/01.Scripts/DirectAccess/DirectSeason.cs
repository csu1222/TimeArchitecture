using UnityEngine;

public enum DemoSeason
{
    Spring,
    Summer,
    Autumn,
    Winter
}

public readonly struct DirectSeasonData
{
    public DirectSeasonData(DemoSeason season, int seasonDay)
    {
        Season = season;
        SeasonDay = seasonDay;
    }

    public DemoSeason Season { get; }
    public int SeasonDay { get; }
}

public sealed class DirectSeason : MonoBehaviour
{
    [SerializeField] private DirectCalendar calendar;

    public DirectSeasonData GetCurrentSeason()
    {
        DirectCalendarData currentCalendar = calendar.GetCurrentCalendar();
        return Calculate(currentCalendar.Month, currentCalendar.Day);
    }

    public DirectSeasonData Calculate(int month, int day)
    {
        int seasonIndex = (month - 1) / DirectAccessConstants.MonthsPerSeason;
        int seasonStartMonth = seasonIndex * DirectAccessConstants.MonthsPerSeason + 1;
        int seasonDay = (month - seasonStartMonth) * DirectAccessConstants.DaysPerMonth + day;
        return new DirectSeasonData((DemoSeason)seasonIndex, seasonDay);
    }
}
