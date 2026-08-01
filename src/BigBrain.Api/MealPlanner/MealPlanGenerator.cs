namespace BigBrain.Api.MealPlanner;

public interface IMealSelectionRandom
{
    int ChooseIndex(int maximum);
}

public interface IMealSelectionRandomFactory
{
    IMealSelectionRandom Create(int seed);
}

public sealed class SeededMealSelectionRandomFactory : IMealSelectionRandomFactory
{
    public IMealSelectionRandom Create(int seed) => new SeededMealSelectionRandom(seed);

    private sealed class SeededMealSelectionRandom(int seed) : IMealSelectionRandom
    {
        private readonly Random random = new(seed);
        public int ChooseIndex(int maximum) => random.Next(maximum);
    }
}

public sealed class MealPlanGenerator(IFamilySchedule familySchedule, IMealSelectionRandomFactory randomFactory)
{
    public IReadOnlyList<ScheduleDay> Generate(
        IReadOnlyList<Meal> meals,
        IReadOnlyList<MealTag> tags,
        DateOnly startDate,
        int weekCount,
        int seed)
    {
        if (meals.Count == 0)
            throw new MealPlannerException(MealPlannerErrorCodes.NoMeals, "Add at least one meal before generating a schedule.", StatusCodes.Status400BadRequest);

        var random = randomFactory.Create(seed);
        var days = new List<ScheduleDay>(weekCount * 9);
        var recentMeals = new Queue<string>();
        for (var offset = 0; offset < weekCount * 7; offset++)
        {
            var date = startDate.AddDays(offset);
            var usedThisWeek = days.Where(day => day.Date >= date.AddDays(-(offset % 7))).Select(day => day.MealId).ToHashSet(StringComparer.Ordinal);
            var mealTypes = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                ? new[] { MealPlannerMealTypes.Lunch, MealPlannerMealTypes.Dinner }
                : new[] { MealPlannerMealTypes.Dinner };
            foreach (var mealType in mealTypes)
            {
                var selected = SelectMeal(meals, tags, date, mealType, usedThisWeek, recentMeals, null, random);
                days.Add(ToDay(date, mealType, familySchedule.GetPeopleCount(date), selected, tags, false));
                usedThisWeek.Add(selected.Id);
                recentMeals.Enqueue(selected.Id);
                while (recentMeals.Count > 9) recentMeals.Dequeue();
            }
        }
        return days;
    }

    public Meal SelectReplacement(
        IReadOnlyList<Meal> meals,
        IReadOnlyList<MealTag> tags,
        DateOnly date,
        string mealType,
        IReadOnlySet<string> usedThisWeek,
        string currentMealId,
        int seed) =>
        SelectMeal(meals, tags, date, mealType, usedThisWeek, [], currentMealId, randomFactory.Create(seed));

    public static ScheduleDay ToDay(DateOnly date, string mealType, int peopleCount, Meal meal, IReadOnlyList<MealTag> tags, bool manual)
    {
        var names = tags.Where(tag => meal.TagIds.Contains(tag.Id, StringComparer.Ordinal)).Select(tag => tag.Name).Order().ToArray();
        return new(date, mealType, date.DayOfWeek.ToString(), peopleCount, meal.Id, meal.Name, names, manual);
    }

    private Meal SelectMeal(
        IReadOnlyList<Meal> meals,
        IReadOnlyList<MealTag> tags,
        DateOnly date,
        string mealType,
        IReadOnlySet<string> usedThisWeek,
        IEnumerable<string> recentMeals,
        string? currentMealId,
        IMealSelectionRandom random)
    {
        var tagById = tags.ToDictionary(tag => tag.Id, StringComparer.Ordinal);
        var peopleCount = familySchedule.GetPeopleCount(date);
        var ranked = meals
            .Select(meal => new { Meal = meal, Rank = Rank(meal, tagById, date, mealType, peopleCount) })
            .OrderBy(item => item.Rank)
            .ToArray();
        var bestRank = ranked[0].Rank;
        var candidates = ranked.Where(item => item.Rank == bestRank).Select(item => item.Meal).ToArray();
        var recent = recentMeals.ToHashSet(StringComparer.Ordinal);

        candidates = Prefer(candidates, meal => meal.Id != currentMealId);
        candidates = Prefer(candidates, meal => !usedThisWeek.Contains(meal.Id));
        candidates = Prefer(candidates, meal => !recent.Contains(meal.Id));
        return candidates[random.ChooseIndex(candidates.Length)];
    }

    private static Meal[] Prefer(Meal[] meals, Func<Meal, bool> predicate)
    {
        var preferred = meals.Where(predicate).ToArray();
        return preferred.Length > 0 ? preferred : meals;
    }

    private static int Rank(Meal meal, Dictionary<string, MealTag> tags, DateOnly date, string mealType, int peopleCount)
    {
        var mealTags = meal.TagIds.Where(tags.ContainsKey).Select(id => tags[id]).ToArray();
        if (mealTags.Length == 0) return 5;
        var portionTags = mealTags.Where(tag => tag.Category == MealPlannerTagCategories.Portion).ToArray();
        var exactPortion = portionTags.Any(tag => PortionMatches(tag.Name, peopleCount));
        var wrongPortion = portionTags.Length > 0 && !exactPortion;
        var relevantOccasion = mealType == MealPlannerMealTypes.Lunch
            ? mealTags.Any(tag => tag.Category == MealPlannerTagCategories.MealType && tag.Name == "Lunch")
            : mealTags.Any(tag => tag.Category == MealPlannerTagCategories.Occasion && OccasionMatches(tag.Name, date.DayOfWeek));
        if (wrongPortion) return 6;
        if (exactPortion && relevantOccasion) return 1;
        if (exactPortion) return 2;
        if (portionTags.Length == 0 && relevantOccasion) return 3;
        return 4;
    }

    private static bool PortionMatches(string name, int peopleCount) =>
        name == "6 personer" ? peopleCount == 6 : name == "3–4 personer" && peopleCount is 3 or 4;

    private static bool OccasionMatches(string name, DayOfWeek dayOfWeek) =>
        dayOfWeek == DayOfWeek.Friday ? name == "Fredagsmat"
        : dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? name == "Helgmat"
        : name == "Lättlagat";
}
