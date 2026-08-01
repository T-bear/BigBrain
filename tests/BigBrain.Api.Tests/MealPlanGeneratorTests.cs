using BigBrain.Api.MealPlanner;

namespace BigBrain.Api.Tests;

public sealed class MealPlanGeneratorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 8, 0, 0, TimeSpan.Zero);
    private readonly MealTag[] tags =
    [
        Tag("portion-3-4", "3–4 personer", "portion"), Tag("portion-6", "6 personer", "portion"),
        Tag("friday", "Fredagsmat", "occasion"), Tag("easy", "Lättlagat", "occasion"), Tag("weekend", "Helgmat", "occasion"),
        Tag("meal-type-lunch", "Lunch", "mealType")
    ];

    [Fact]
    public void GeneratesRequestedWeeksWithPeopleCountsAndAvoidsDuplicatesWhenPossible()
    {
        var meals = Enumerable.Range(1, 14).Select(index => Meal($"m{index}", $"Rätt {index}", [])).ToArray();
        var days = Generator().Generate(meals, tags, new(2026, 8, 3), 2, 4);
        Assert.Equal(18, days.Count);
        var firstWeek = days.Where(day => day.Date < new DateOnly(2026, 8, 10)).ToArray();
        Assert.Equal(9, firstWeek.Length);
        Assert.Equal(7, firstWeek.Count(day => day.MealType == MealPlannerMealTypes.Dinner));
        Assert.Equal(2, firstWeek.Count(day => day.MealType == MealPlannerMealTypes.Lunch));
        Assert.Equal(9, firstWeek.Select(day => day.MealId).Distinct().Count());
        Assert.All(days.GroupBy(day => day.Date), group => Assert.Single(group.Select(day => day.PeopleCount).Distinct()));
    }

    [Theory]
    [InlineData(2026, 8, 7, "portion-6", "friday")]
    [InlineData(2026, 8, 8, "portion-6", "weekend")]
    [InlineData(2026, 8, 3, "portion-3-4", "easy")]
    public void PrioritizesRelevantOccasion(int year, int month, int day, string portionTag, string occasionTag)
    {
        var selected = Assert.Single(Generator().Generate(
            [Meal("relevant", "Relevant", [portionTag, occasionTag]), Meal("plain", "Plain", [portionTag])],
            tags, new(year, month, day), 1, 0), item => item.Date == new DateOnly(year, month, day) && item.MealType == MealPlannerMealTypes.Dinner);
        Assert.Equal("relevant", selected.MealId);
    }

    [Fact]
    public void ExactPortionWinsAndWrongPortionIsAvoided()
    {
        var day = Generator().Generate(
            [Meal("wrong", "Liten", ["portion-3-4"]), Meal("right", "Stor", ["portion-6"])],
            tags, new(2026, 8, 5), 1, 0)[0];
        Assert.Equal("right", day.MealId);
    }

    [Fact]
    public void WeekendLunchPrioritizesLunchTagButAllowsUntaggedFallback()
    {
        var generated = Generator().Generate(
            [Meal("lunch", "Lunchrätt", ["portion-6", "meal-type-lunch"]), Meal("dinner", "Helgmiddag", ["portion-6", "weekend"])],
            tags, new(2026, 8, 8), 1, 0);
        Assert.Equal("lunch", generated.Single(day => day.Date == new DateOnly(2026, 8, 8) && day.MealType == MealPlannerMealTypes.Lunch).MealId);
        Assert.Equal("dinner", generated.Single(day => day.Date == new DateOnly(2026, 8, 8) && day.MealType == MealPlannerMealTypes.Dinner).MealId);

        var fallback = Generator().Generate([Meal("plain", "Otaggad", [])], tags, new(2026, 8, 8), 1, 0);
        Assert.Equal("plain", fallback.Single(day => day.Date == new DateOnly(2026, 8, 8) && day.MealType == MealPlannerMealTypes.Lunch).MealId);
    }

    [Fact]
    public void UntaggedMealIsValidFallbackAndEmptyCollectionHasStableError()
    {
        Assert.Equal("plain", Generator().Generate([Meal("plain", "Soppa", [])], tags, new(2026, 8, 3), 1, 0)[0].MealId);
        var error = Assert.Throws<MealPlannerException>(() => Generator().Generate([], tags, new(2026, 8, 3), 1, 0));
        Assert.Equal(MealPlannerErrorCodes.NoMeals, error.Code);
    }

    [Fact]
    public void ReplacementAvoidsCurrentAndMealsAlreadyInWeek()
    {
        var selected = Generator().SelectReplacement(
            [Meal("current", "Nuvarande", []), Meal("used", "Använd", []), Meal("new", "Ny", [])], tags,
            new(2026, 8, 3), MealPlannerMealTypes.Dinner, new HashSet<string>(["used"]), "current", 0);
        Assert.Equal("new", selected.Id);
    }

    private static MealPlanGenerator Generator() => new(new TwoWeekFamilySchedule(), new SeededMealSelectionRandomFactory());
    private static MealTag Tag(string id, string name, string category) => new(id, name, category, Now, true);
    private static Meal Meal(string id, string name, string[] tagIds) => new(id, name, tagIds, Now, Now);
}
