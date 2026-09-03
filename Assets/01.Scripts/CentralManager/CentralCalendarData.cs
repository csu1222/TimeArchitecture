public readonly struct CentralCalendarData
{
    public CentralCalendarData(int year, int month, int day, int dayOfYear, int gameDayIndex)
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
