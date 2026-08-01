using BigBrain.Api.MealPlanner;

namespace BigBrain.Api.Tests;

public sealed class FamilyScheduleTests
{
    private readonly TwoWeekFamilySchedule schedule = new();

    [Theory]
    [InlineData(2026, 8, 1, 3)]
    [InlineData(2026, 8, 2, 3)]
    [InlineData(2026, 8, 3, 4)]
    [InlineData(2026, 8, 4, 4)]
    [InlineData(2026, 8, 5, 6)]
    [InlineData(2026, 8, 10, 6)]
    [InlineData(2026, 8, 12, 3)]
    public void ReturnsConfiguredPeopleCount(int year, int month, int day, int expected) =>
        Assert.Equal(expected, schedule.GetPeopleCount(new DateOnly(year, month, day)));

    [Fact]
    public void RepeatsAfterFourteenDays()
    {
        for (var offset = -14; offset <= 14; offset++)
            Assert.Equal(
                schedule.GetPeopleCount(TwoWeekFamilySchedule.AnchorDate.AddDays(offset)),
                schedule.GetPeopleCount(TwoWeekFamilySchedule.AnchorDate.AddDays(offset + 14)));
    }
}
