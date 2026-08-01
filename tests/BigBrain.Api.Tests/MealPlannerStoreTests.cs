using BigBrain.Api.MealPlanner;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Tests;

public sealed class MealPlannerStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"bigbrain-meals-{Guid.NewGuid():N}");
    private string DatabasePath => Path.Combine(directory, "meal-planner.db");

    [Fact]
    public async Task SeedsDefaultTagsAndSupportsMealCrudAndFiltering()
    {
        using var store = CreateStore();
        var tags = await store.GetTagsAsync(TestContext.Current.CancellationToken);
        Assert.Equal(6, tags.Count);
        Assert.All(tags, tag => Assert.True(tag.IsProtected));
        Assert.Contains(tags, tag => tag.Id == "meal-type-lunch" && tag.Category == MealPlannerTagCategories.MealType);

        var custom = await store.CreateTagAsync("Vegetariskt", MealPlannerTagCategories.Custom, TestContext.Current.CancellationToken);
        var meal = await store.CreateMealAsync("Pasta", [custom.Id, "occasion-easy"], TestContext.Current.CancellationToken);
        Assert.Equal(2, meal.TagIds.Count);
        Assert.Single(await store.GetMealsAsync([custom.Id], TestContext.Current.CancellationToken));

        var updated = await store.UpdateMealAsync(meal.Id, "Pasta pesto", [], TestContext.Current.CancellationToken);
        Assert.Empty(updated.TagIds);
        await store.DeleteMealAsync(meal.Id, TestContext.Current.CancellationToken);
        Assert.Empty(await store.GetMealsAsync([], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeletingCustomTagSafelyUnlinksMealsAndProtectedTagIsRejected()
    {
        using var store = CreateStore();
        var custom = await store.CreateTagAsync("Barnfavorit", MealPlannerTagCategories.Custom, TestContext.Current.CancellationToken);
        var meal = await store.CreateMealAsync("Tacos", [custom.Id], TestContext.Current.CancellationToken);

        await store.DeleteTagAsync(custom.Id, TestContext.Current.CancellationToken);
        Assert.Empty((await store.GetMealsAsync([], TestContext.Current.CancellationToken)).Single(item => item.Id == meal.Id).TagIds);
        var error = await Assert.ThrowsAsync<MealPlannerException>(() => store.DeleteTagAsync("portion-6", TestContext.Current.CancellationToken));
        Assert.Equal(MealPlannerErrorCodes.ProtectedTag, error.Code);
    }

    [Fact]
    public async Task ScheduleSurvivesNewStoreInstanceAndCanBeDeleted()
    {
        var now = DateTimeOffset.UtcNow;
        var schedule = new MealSchedule("schedule", new(2026, 8, 3), new(2026, 8, 9), now, now,
            [new(new(2026, 8, 3), MealPlannerMealTypes.Dinner, "Monday", 4, "meal", "Pasta", [], false)], "Vecka", 2);
        using (var first = CreateStore()) await first.SaveScheduleAsync(schedule, TestContext.Current.CancellationToken);
        using (var second = CreateStore())
        {
            var loaded = await second.GetScheduleAsync(schedule.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Pasta", Assert.Single(loaded.Days).MealName);
            Assert.Equal(MealPlannerMealTypes.Dinner, Assert.Single(loaded.Days).MealType);
            await second.DeleteScheduleAsync(schedule.Id, TestContext.Current.CancellationToken);
            Assert.Empty(await second.GetSchedulesAsync(TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task InvalidStorageTargetMakesOnlyStoreUnavailable()
    {
        Directory.CreateDirectory(directory);
        using var store = new MealPlannerStore(new MealPlannerOptions { DatabasePath = directory });
        Assert.False(store.IsAvailable);
        await Assert.ThrowsAsync<MealPlannerUnavailableException>(() => store.GetTagsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task VersionOneScheduleMigratesToDinnerIdempotentlyWithoutDataLoss()
    {
        Directory.CreateDirectory(directory);
        await using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE Tags (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Category TEXT NOT NULL, CreatedAtUtc TEXT NOT NULL, IsProtected INTEGER NOT NULL);
                CREATE TABLE Meals (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, TagIdsJson TEXT NOT NULL, CreatedAtUtc TEXT NOT NULL, UpdatedAtUtc TEXT NOT NULL);
                CREATE TABLE Schedules (Id TEXT PRIMARY KEY, StartDate TEXT NOT NULL, EndDate TEXT NOT NULL, CreatedAtUtc TEXT NOT NULL, UpdatedAtUtc TEXT NOT NULL, Title TEXT NULL, GenerationVersion INTEGER NOT NULL, DaysJson TEXT NOT NULL);
                INSERT INTO Meals VALUES ('meal','Bevarad rätt','[]','2026-08-01T00:00:00Z','2026-08-01T00:00:00Z');
                INSERT INTO Schedules VALUES ('legacy','2026-08-03','2026-08-03','2026-08-01T00:00:00Z','2026-08-01T00:00:00Z','Äldre',1,'[{"date":"2026-08-03","dayOfWeek":"Monday","peopleCount":4,"mealId":"meal","mealName":"Bevarad rätt","tagSummary":[],"isManuallyReplaced":false}]');
                PRAGMA user_version=1;
                """;
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        using (var migrated = CreateStore())
        {
            Assert.Equal("Bevarad rätt", Assert.Single(await migrated.GetMealsAsync([], TestContext.Current.CancellationToken)).Name);
            Assert.Equal(MealPlannerMealTypes.Dinner, Assert.Single((await migrated.GetScheduleAsync("legacy", TestContext.Current.CancellationToken)).Days).MealType);
        }
        using (var reopened = CreateStore())
            Assert.Equal(MealPlannerMealTypes.Dinner, Assert.Single((await reopened.GetScheduleAsync("legacy", TestContext.Current.CancellationToken)).Days).MealType);

        await using var verify = new SqliteConnection($"Data Source={DatabasePath}");
        await verify.OpenAsync(TestContext.Current.CancellationToken);
        await using var version = verify.CreateCommand();
        version.CommandText = "PRAGMA user_version";
        Assert.Equal(2L, (long)(await version.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);
    }

    private MealPlannerStore CreateStore() => new(new MealPlannerOptions { DatabasePath = DatabasePath });

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
