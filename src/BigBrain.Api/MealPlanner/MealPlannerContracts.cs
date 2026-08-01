namespace BigBrain.Api.MealPlanner;

public static class MealPlannerTagCategories
{
    public const string Portion = "portion";
    public const string Occasion = "occasion";
    public const string MealType = "mealType";
    public const string Custom = "custom";

    public static bool IsValid(string value) => value is Portion or Occasion or MealType or Custom;
}

public static class MealPlannerMealTypes
{
    public const string Lunch = "lunch";
    public const string Dinner = "dinner";

    public static bool IsValid(string value) => value is Lunch or Dinner;
}

public static class MealPlannerErrorCodes
{
    public const string InvalidRequest = "mealPlannerInvalidRequest";
    public const string NoMeals = "mealPlannerNoMeals";
    public const string MealNotFound = "mealPlannerMealNotFound";
    public const string TagNotFound = "mealPlannerTagNotFound";
    public const string TagAlreadyExists = "mealPlannerTagAlreadyExists";
    public const string ScheduleNotFound = "mealPlannerScheduleNotFound";
    public const string ProtectedTag = "mealPlannerProtectedTag";
    public const string InvalidMealType = "mealPlannerInvalidMealType";
    public const string Unavailable = "mealPlannerUnavailable";
}

public sealed record MealPlannerOptions
{
    public const string SectionName = "MealPlanner";
    public string DatabasePath { get; init; } = "data/meal-planner.db";
}

public sealed record Meal(string Id, string Name, IReadOnlyList<string> TagIds, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
public sealed record MealTag(string Id, string Name, string Category, DateTimeOffset CreatedAtUtc, bool IsProtected);
public sealed record ScheduleDay(
    DateOnly Date,
    string MealType,
    string DayOfWeek,
    int PeopleCount,
    string MealId,
    string MealName,
    IReadOnlyList<string> TagSummary,
    bool IsManuallyReplaced);
public sealed record MealSchedule(
    string Id,
    DateOnly StartDate,
    DateOnly EndDate,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<ScheduleDay> Days,
    string? Title,
    int GenerationVersion);

public sealed record CreateMealRequest(string? Name, IReadOnlyList<string>? TagIds);
public sealed record UpdateMealRequest(string? Name, IReadOnlyList<string>? TagIds);
public sealed record CreateTagRequest(string? Name, string? Category);
public sealed record GenerateSchedulesRequest(DateOnly StartDate, int WeekCount, string? Title, int? Seed);
public sealed record ReplaceScheduleDayRequest(int? Seed);
public sealed record SetScheduleDayMealRequest(string? MealId);
public sealed record SeedExampleMealsResponse(int CreatedCount, int IgnoredCount);
public sealed record ExampleMealDefinition(string Name, IReadOnlyList<string> TagIds);

public sealed class MealPlannerException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed class MealPlannerUnavailableException() : Exception("Meal planner storage is unavailable.");
