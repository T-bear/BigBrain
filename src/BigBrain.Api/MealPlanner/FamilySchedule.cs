namespace BigBrain.Api.MealPlanner;

public interface IFamilySchedule
{
    int GetPeopleCount(DateOnly day);
}

public sealed class TwoWeekFamilySchedule : IFamilySchedule
{
    public static readonly DateOnly AnchorDate = new(2026, 8, 3);
    private static readonly int[] Counts = [4, 4, 6, 6, 6, 6, 6, 6, 6, 3, 3, 3, 3, 3];

    public int GetPeopleCount(DateOnly day)
    {
        var daysFromAnchor = day.DayNumber - AnchorDate.DayNumber;
        var cycleDay = ((daysFromAnchor % Counts.Length) + Counts.Length) % Counts.Length;
        return Counts[cycleDay];
    }
}
