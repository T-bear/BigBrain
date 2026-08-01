namespace BigBrain.Api.MealPlanner;

public sealed class MealPlannerService(MealPlannerStore store, MealPlanGenerator generator, ILogger<MealPlannerService> logger)
{
    private static readonly ExampleMealDefinition[] ExampleMeals =
    [
        new("Spaghetti och köttfärssås", ["portion-6", "occasion-easy"]),
        new("Tacos", ["portion-6", "occasion-friday"]),
        new("Korv stroganoff", ["portion-3-4", "occasion-easy"]),
        new("Pannkakor", ["portion-3-4", "meal-type-lunch"]),
        new("Kycklinggryta", ["portion-6"]),
        new("Lasagne", ["portion-6", "occasion-weekend"]),
        new("Fiskpinnar och potatis", ["occasion-easy", "meal-type-lunch"]),
        new("Hamburgare", ["occasion-friday"]),
        new("Hemmagjord pizza", ["portion-6", "occasion-friday", "occasion-weekend"]),
        new("Köttbullar och makaroner", ["portion-3-4", "occasion-easy", "meal-type-lunch"]),
        new("Soppa med smörgås", ["meal-type-lunch"]),
        new("Ugnspannkaka", ["portion-6", "occasion-easy", "meal-type-lunch"]),
        new("Pulled chicken", ["portion-6", "occasion-weekend"]),
        new("Falukorv i ugn", ["portion-3-4"]),
        new("Vegetarisk pasta", []),
        new("Kyckling med ris", ["portion-6", "occasion-easy"]),
        new("Köttfärslimpa", ["occasion-weekend"]),
        new("Pytt i panna", ["occasion-easy", "meal-type-lunch"]),
        new("Fiskgratäng", ["portion-3-4"]),
        new("Wraps", ["occasion-friday", "occasion-easy", "meal-type-lunch"]),
        new("Grillad korv", []),
        new("Fläskfilé med potatisgratäng", ["portion-6", "occasion-weekend"]),
        new("Nudlar med kyckling", ["portion-3-4", "occasion-easy"]),
        new("Bakad potatis", ["meal-type-lunch"]),
    ];
    private static readonly Action<ILogger, string, string, Exception?> Deleted =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(3101, "MealPlannerDeleted"),
            "Meal planner entity deleted: entityType={EntityType} entityId={EntityId}");
    public Task<IReadOnlyList<MealTag>> GetTagsAsync(CancellationToken cancellationToken) => store.GetTagsAsync(cancellationToken);
    public Task<IReadOnlyList<Meal>> GetMealsAsync(IReadOnlyList<string> tagIds, CancellationToken cancellationToken) => store.GetMealsAsync(tagIds, cancellationToken);
    public Task<IReadOnlyList<MealSchedule>> GetSchedulesAsync(CancellationToken cancellationToken) => store.GetSchedulesAsync(cancellationToken);
    public Task<MealSchedule> GetScheduleAsync(string id, CancellationToken cancellationToken) => store.GetScheduleAsync(id, cancellationToken);

    public Task<MealTag> CreateTagAsync(CreateTagRequest request, CancellationToken cancellationToken)
    {
        var name = RequiredName(request.Name, "Tag name is required.");
        var category = request.Category?.Trim() switch
        {
            { } value when value.Equals(MealPlannerTagCategories.Portion, StringComparison.OrdinalIgnoreCase) => MealPlannerTagCategories.Portion,
            { } value when value.Equals(MealPlannerTagCategories.Occasion, StringComparison.OrdinalIgnoreCase) => MealPlannerTagCategories.Occasion,
            { } value when value.Equals(MealPlannerTagCategories.MealType, StringComparison.OrdinalIgnoreCase) => MealPlannerTagCategories.MealType,
            { } value when value.Equals(MealPlannerTagCategories.Custom, StringComparison.OrdinalIgnoreCase) => MealPlannerTagCategories.Custom,
            _ => string.Empty,
        };
        if (!MealPlannerTagCategories.IsValid(category))
            throw Invalid("Tag category must be portion, occasion, mealType or custom.");
        return store.CreateTagAsync(name, category, cancellationToken);
    }

    public async Task DeleteTagAsync(string id, CancellationToken cancellationToken)
    {
        await store.DeleteTagAsync(id, cancellationToken);
        Deleted(logger, "tag", id, null);
    }

    public Task<Meal> CreateMealAsync(CreateMealRequest request, CancellationToken cancellationToken) =>
        store.CreateMealAsync(RequiredName(request.Name, "Meal name is required."), request.TagIds ?? [], cancellationToken);

    public Task<Meal> UpdateMealAsync(string id, UpdateMealRequest request, CancellationToken cancellationToken) =>
        store.UpdateMealAsync(id, RequiredName(request.Name, "Meal name is required."), request.TagIds ?? [], cancellationToken);

    public Task<SeedExampleMealsResponse> SeedExampleMealsAsync(CancellationToken cancellationToken) =>
        store.SeedExampleMealsAsync(ExampleMeals, cancellationToken);

    public async Task DeleteMealAsync(string id, CancellationToken cancellationToken)
    {
        await store.DeleteMealAsync(id, cancellationToken);
        Deleted(logger, "meal", id, null);
    }

    public async Task<MealSchedule> GenerateAsync(GenerateSchedulesRequest request, CancellationToken cancellationToken)
    {
        if (request.WeekCount is < 1 or > 12) throw Invalid("Week count must be between 1 and 12.");
        var meals = await store.GetMealsAsync([], cancellationToken);
        var tags = await store.GetTagsAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var days = generator.Generate(meals, tags, request.StartDate, request.WeekCount, request.Seed ?? 0);
        var schedule = new MealSchedule(
            Guid.NewGuid().ToString("N"), request.StartDate, request.StartDate.AddDays(request.WeekCount * 7 - 1),
            now, now, days, NormalizeOptional(request.Title), 2);
        return await store.SaveScheduleAsync(schedule, cancellationToken);
    }

    public async Task<MealSchedule> ReplaceAsync(string scheduleId, DateOnly date, string mealType, ReplaceScheduleDayRequest request, CancellationToken cancellationToken)
    {
        mealType = ValidateMealType(mealType);
        var schedule = await store.GetScheduleAsync(scheduleId, cancellationToken);
        var index = FindDay(schedule, date, mealType);
        var meals = await store.GetMealsAsync([], cancellationToken);
        if (meals.Count == 0) throw new MealPlannerException(MealPlannerErrorCodes.NoMeals, "Add at least one meal before replacing a meal.", StatusCodes.Status400BadRequest);
        var tags = await store.GetTagsAsync(cancellationToken);
        var weekOffset = (date.DayNumber - schedule.StartDate.DayNumber) / 7;
        var weekStart = schedule.StartDate.AddDays(weekOffset * 7);
        var used = schedule.Days.Where(day => day.Date >= weekStart && day.Date < weekStart.AddDays(7) && !(day.Date == date && day.MealType == mealType)).Select(day => day.MealId).ToHashSet(StringComparer.Ordinal);
        var selected = generator.SelectReplacement(meals, tags, date, mealType, used, schedule.Days[index].MealId, request.Seed ?? 0);
        return await ReplaceDayAsync(schedule, index, MealPlanGenerator.ToDay(date, mealType, schedule.Days[index].PeopleCount, selected, tags, true), cancellationToken);
    }

    public async Task<MealSchedule> SetMealAsync(string scheduleId, DateOnly date, string mealType, SetScheduleDayMealRequest request, CancellationToken cancellationToken)
    {
        mealType = ValidateMealType(mealType);
        var mealId = request.MealId?.Trim() ?? string.Empty;
        var schedule = await store.GetScheduleAsync(scheduleId, cancellationToken);
        var index = FindDay(schedule, date, mealType);
        var meal = (await store.GetMealsAsync([], cancellationToken)).SingleOrDefault(value => value.Id == mealId)
            ?? throw new MealPlannerException(MealPlannerErrorCodes.MealNotFound, "Meal was not found.", StatusCodes.Status404NotFound);
        var tags = await store.GetTagsAsync(cancellationToken);
        return await ReplaceDayAsync(schedule, index, MealPlanGenerator.ToDay(date, mealType, schedule.Days[index].PeopleCount, meal, tags, true), cancellationToken);
    }

    public async Task DeleteScheduleAsync(string id, CancellationToken cancellationToken)
    {
        await store.DeleteScheduleAsync(id, cancellationToken);
        Deleted(logger, "schedule", id, null);
    }

    private async Task<MealSchedule> ReplaceDayAsync(MealSchedule schedule, int index, ScheduleDay day, CancellationToken cancellationToken)
    {
        var days = schedule.Days.ToArray();
        days[index] = day;
        return await store.UpdateScheduleAsync(schedule with { Days = days, UpdatedAtUtc = DateTimeOffset.UtcNow }, cancellationToken);
    }

    private static int FindDay(MealSchedule schedule, DateOnly date, string mealType)
    {
        var index = schedule.Days.ToList().FindIndex(day => day.Date == date && day.MealType == mealType);
        return index >= 0 ? index : throw new MealPlannerException(MealPlannerErrorCodes.InvalidRequest, "Date and meal type are not part of the schedule.", StatusCodes.Status400BadRequest);
    }

    private static string ValidateMealType(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (!MealPlannerMealTypes.IsValid(normalized))
            throw new MealPlannerException(MealPlannerErrorCodes.InvalidMealType, "Meal type must be lunch or dinner.", StatusCodes.Status400BadRequest);
        return normalized;
    }

    private static string RequiredName(string? value, string message)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0) throw Invalid(message);
        if (normalized.Length > 120) throw Invalid("Name must be 120 characters or fewer.");
        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        if (normalized?.Length > 120) throw Invalid("Title must be 120 characters or fewer.");
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static MealPlannerException Invalid(string message) =>
        new(MealPlannerErrorCodes.InvalidRequest, message, StatusCodes.Status400BadRequest);
}
