namespace BigBrain.Modules.Finance;

public sealed record UsEquitySession(DateOnly Date, DateTimeOffset OpenUtc, DateTimeOffset CloseUtc, string CalendarVersion);

public static class UsMarketCalendar
{
    public const string Version = "us-equities-ny-v1";
    private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public static UsEquitySession? Session(DateOnly date)
    {
        if (!IsSession(date)) return null;
        return new(date, ToUtc(date, new(9, 30)), ToUtc(date, new(16, 0)), Version);
    }

    public static bool IsSession(DateOnly date) => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday && !Holidays(date.Year).Contains(date);

    public static int CompletedSessionsAfter(DateOnly observation, DateOnly through)
    {
        var count = 0;
        for (var day = observation.AddDays(1); day <= through; day = day.AddDays(1)) if (IsSession(day)) count++;
        return count;
    }

    private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified), NewYork);

    private static HashSet<DateOnly> Holidays(int year)
    {
        var days = new HashSet<DateOnly>
        {
            Observed(new(year,1,1)), Nth(year,1,DayOfWeek.Monday,3), Nth(year,2,DayOfWeek.Monday,3),
            Easter(year).AddDays(-2), Last(year,5,DayOfWeek.Monday), Observed(new(year,7,4)),
            Nth(year,9,DayOfWeek.Monday,1), Nth(year,11,DayOfWeek.Thursday,4), Observed(new(year,12,25))
        };
        if (year >= 2022) days.Add(Observed(new(year,6,19)));
        // Bounded exceptional full-day closures relevant to the repository's historical range.
        if (year == 2001) foreach (var d in new[] { 11, 12, 13, 14 }) days.Add(new(2001,9,d));
        if (year == 2012) { days.Add(new(2012,10,29)); days.Add(new(2012,10,30)); }
        if (year == 2018) days.Add(new(2018,12,5));
        return days;
    }

    private static DateOnly Observed(DateOnly date) => date.DayOfWeek switch { DayOfWeek.Saturday => date.AddDays(-1), DayOfWeek.Sunday => date.AddDays(1), _ => date };
    private static DateOnly Nth(int year,int month,DayOfWeek weekday,int n){var d=new DateOnly(year,month,1);while(d.DayOfWeek!=weekday)d=d.AddDays(1);return d.AddDays(7*(n-1));}
    private static DateOnly Last(int year,int month,DayOfWeek weekday){var d=new DateOnly(year,month,DateTime.DaysInMonth(year,month));while(d.DayOfWeek!=weekday)d=d.AddDays(-1);return d;}
    private static DateOnly Easter(int year){var a=year%19;var b=year/100;var c=year%100;var d=b/4;var e=b%4;var f=(b+8)/25;var g=(b-f+1)/3;var h=(19*a+b-d-g+15)%30;var i=c/4;var k=c%4;var l=(32+2*e+2*i-h-k)%7;var m=(a+11*h+22*l)/451;var month=(h+l-7*m+114)/31;var day=(h+l-7*m+114)%31+1;return new(year,month,day);}
}
