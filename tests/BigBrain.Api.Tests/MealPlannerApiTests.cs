using System.Net;
using System.Net.Http.Json;
using BigBrain.Api;
using BigBrain.Api.MealPlanner;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging.Abstractions;

namespace BigBrain.Api.Tests;

public sealed class MealPlannerApiTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"bigbrain-meal-api-{Guid.NewGuid():N}");

    [Fact]
    public async Task HappyPathCreatesMealGeneratesAndReplacesOnlyOneDay()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var first = await Post<Meal>(client, "/api/v1/modules/meal-planner/meals", new CreateMealRequest("Pasta", []));
        var second = await Post<Meal>(client, "/api/v1/modules/meal-planner/meals", new CreateMealRequest("Soppa", []));
        var schedule = await Post<MealSchedule>(client, "/api/v1/modules/meal-planner/schedules/generate", new GenerateSchedulesRequest(new(2026, 8, 3), 1, "Vecka", 0));
        Assert.Equal(9, schedule.Days.Count);
        Assert.Equal(2, schedule.Days.Count(day => day.MealType == MealPlannerMealTypes.Lunch));

        var date = schedule.Days[0].Date;
        var manual = await Put<MealSchedule>(client, $"/api/v1/modules/meal-planner/schedules/{schedule.Id}/days/{date:yyyy-MM-dd}/dinner/meal", new SetScheduleDayMealRequest(second.Id));
        Assert.Equal(second.Id, manual.Days[0].MealId);
        Assert.Equal(schedule.Days.Skip(1).Select(day => day.MealId), manual.Days.Skip(1).Select(day => day.MealId));
        Assert.Equal(date, manual.Days[0].Date);
        Assert.Equal(4, manual.Days[0].PeopleCount);
        Assert.True(manual.Days[0].IsManuallyReplaced);
        Assert.NotEqual(first.Id, second.Id);

        var saturday = schedule.Days.First(day => day.DayOfWeek == nameof(DayOfWeek.Saturday) && day.MealType == MealPlannerMealTypes.Lunch);
        var lunch = await Put<MealSchedule>(client, $"/api/v1/modules/meal-planner/schedules/{schedule.Id}/days/{saturday.Date:yyyy-MM-dd}/lunch/meal", new SetScheduleDayMealRequest(first.Id));
        Assert.Equal(first.Id, lunch.Days.Single(day => day.Date == saturday.Date && day.MealType == MealPlannerMealTypes.Lunch).MealId);
        Assert.Equal(schedule.Days.Single(day => day.Date == saturday.Date && day.MealType == MealPlannerMealTypes.Dinner).MealId,
            lunch.Days.Single(day => day.Date == saturday.Date && day.MealType == MealPlannerMealTypes.Dinner).MealId);
    }

    [Fact]
    public async Task InvalidMealTypeUsesStableProblemDetailsCode()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync("/api/v1/modules/meal-planner/schedules/missing/days/2026-08-08/brunch/replace", new ReplaceScheduleDayRequest(0), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(MealPlannerErrorCodes.InvalidMealType, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidationAndNotFoundUseStableProblemDetailsWithoutPaths()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var invalid = await client.PostAsJsonAsync("/api/v1/modules/meal-planner/meals", new CreateMealRequest(" ", []), TestContext.Current.CancellationToken);
        var missing = await client.GetAsync("/api/v1/modules/meal-planner/schedules/missing", TestContext.Current.CancellationToken);
        var invalidBody = await invalid.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var missingBody = await missing.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains(MealPlannerErrorCodes.InvalidRequest, invalidBody, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Contains(MealPlannerErrorCodes.ScheduleNotFound, missingBody, StringComparison.Ordinal);
        Assert.DoesNotContain(directory, invalidBody + missingBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateWithoutMealsReturnsStableValidationError()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/modules/meal-planner/schedules/generate", new GenerateSchedulesRequest(new(2026, 8, 3), 1, null, 0), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(MealPlannerErrorCodes.NoMeals, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DuplicateTagReturnsStableConflictProblemDetails()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var request = new CreateTagRequest("Barnvänligt", MealPlannerTagCategories.Custom);
        (await client.PostAsJsonAsync("/api/v1/modules/meal-planner/tags", request, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var duplicate = await client.PostAsJsonAsync("/api/v1/modules/meal-planner/tags", request, TestContext.Current.CancellationToken);
        var body = await duplicate.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Contains(MealPlannerErrorCodes.TagAlreadyExists, body, StringComparison.Ordinal);
        Assert.DoesNotContain(directory, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MealTypeCategoryUsesStableSerializedValue()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var tag = await Post<MealTag>(client, "/api/v1/modules/meal-planner/tags", new CreateTagRequest("Mellanmål", "MEALTYPE"));
        Assert.Equal(MealPlannerTagCategories.MealType, tag.Category);
    }

    [Fact]
    public async Task SeedExamplesIsIdempotentAndPreservesExistingMeals()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var existing = await Post<Meal>(client, "/api/v1/modules/meal-planner/meals", new CreateMealRequest("Min egen rätt", ["occasion-easy"]));

        var first = await Post<SeedExampleMealsResponse>(client, "/api/v1/modules/meal-planner/meals/seed-examples", new { });
        var second = await Post<SeedExampleMealsResponse>(client, "/api/v1/modules/meal-planner/meals/seed-examples", new { });
        var meals = (await client.GetFromJsonAsync<Meal[]>("/api/v1/modules/meal-planner/meals", TestContext.Current.CancellationToken))!;

        Assert.Equal(24, first.CreatedCount);
        Assert.Equal(0, first.IgnoredCount);
        Assert.Equal(0, second.CreatedCount);
        Assert.Equal(24, second.IgnoredCount);
        Assert.Contains(meals, meal => meal.Id == existing.Id && meal.Name == existing.Name && meal.TagIds.SequenceEqual(existing.TagIds));
        Assert.Contains(meals, meal => meal.TagIds.Count == 0);
        Assert.Contains(meals, meal => meal.TagIds.Count >= 2);
        Assert.True(meals.Count(meal => meal.TagIds.Contains("meal-type-lunch")) >= 5);
        Assert.Equal(meals.Length, meals.Select(meal => meal.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Theory]
    [InlineData("Production", false)]
    [InlineData("Development", true)]
    public async Task SchoolMealsAreReadOnlyOfflineAndFixturesAreDevelopmentOnly(string environment, bool allowed)
    {
        await using var factory = Factory().WithWebHostBuilder(builder => builder.UseEnvironment(environment));
        using var client = factory.CreateClient();
        var before = await client.GetStringAsync("/api/v1/modules/meal-planner/meals", TestContext.Current.CancellationToken);
        var current = await client.GetFromJsonAsync<SchoolMealWeek>("/api/v1/modules/meal-planner/school-meals?schoolId=rosenfeldt", TestContext.Current.CancellationToken);
        Assert.Equal("offline", current!.Status);
        Assert.False(current.IsFixture);
        var offline = await client.GetFromJsonAsync<SchoolMealWeek>("/api/v1/modules/meal-planner/school-meals?schoolId=rosenfeldt&year=2026&week=43", TestContext.Current.CancellationToken);
        Assert.Equal("offline", offline!.Status);
        Assert.False(offline.IsFixture);
        var preview = await client.GetAsync("/api/v1/modules/meal-planner/school-meals?schoolId=rosenfeldt&year=2026&week=43&fixture=true", TestContext.Current.CancellationToken);
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, preview.StatusCode);
        var html = await client.GetAsync("/api/v1/modules/meal-planner/school-meals?schoolId=musikugglan&year=2026&week=44&fixture=true", TestContext.Current.CancellationToken);
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, html.StatusCode);
        if (allowed)
        {
            var menu = await html.Content.ReadFromJsonAsync<SchoolMealWeek>(TestContext.Current.CancellationToken);
            Assert.Equal("htmlWeekRequestedYear", menu!.DateBasis);
            Assert.Equal(5, menu.Days.Count(day => day.Status == "published"));
        }
        var invalid = await client.GetAsync("/api/v1/modules/meal-planner/school-meals?schoolId=unknown&year=2026&week=99", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains("schoolMeals.invalidRequest", await invalid.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
        Assert.Equal(before, await client.GetStringAsync("/api/v1/modules/meal-planner/meals", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SchoolMealsEndpointUsesServerWeekAndIsolatesSourceFailure()
    {
        var clock = new SchoolClock();
        using var transport = new SchoolHandler();
        using var data = new SchoolMealTestData();
        var options = data.Options(new() { SchoolMeals = new() { Enabled = true, RosenfeldtEnabled = true, MusikugglanEnabled = true } }, clock.GetUtcNow());
        await using var factory = Factory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<SchoolMealService>();
            services.AddSingleton(new SchoolMealService(options, new SchoolMealSourceClient(new HttpClient(transport), clock, NullLogger<SchoolMealSourceClient>.Instance), clock, data.Store(options)));
        }));
        using var client = factory.CreateClient();
        var before = await client.GetStringAsync("/api/v1/modules/meal-planner/meals", TestContext.Current.CancellationToken);
        var rss = await client.GetFromJsonAsync<SchoolMealWeek>("/api/v1/modules/meal-planner/school-meals?schoolId=rosenfeldt", TestContext.Current.CancellationToken);
        Assert.Equal("published", rss!.Status);
        Assert.False(rss.IsFixture);
        Assert.Equal(2026, rss.Year);
        Assert.Equal(43, rss.Week);
        var html = await client.GetFromJsonAsync<SchoolMealWeek>("/api/v1/modules/meal-planner/school-meals?schoolId=musikugglan", TestContext.Current.CancellationToken);
        Assert.Equal("unavailable", html!.Status);
        var partial = await client.GetAsync("/api/v1/modules/meal-planner/school-meals?schoolId=rosenfeldt&year=2026", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, partial.StatusCode);
        Assert.Equal(before, await client.GetStringAsync("/api/v1/modules/meal-planner/meals", TestContext.Current.CancellationToken));
        Assert.Equal(2, transport.Calls);
    }

    private sealed class SchoolClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class SchoolHandler : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            return Task.FromResult(request.RequestUri!.Host == "www.skolmat.info"
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SchoolMeals.ExampleRss, System.Text.Encoding.UTF8, "application/rss+xml") }
                : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }
    }

    private WebApplicationFactory<Program> Factory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<MealPlannerOptions>();
            services.RemoveAll<MealPlannerStore>();
            services.AddSingleton(new MealPlannerOptions { DatabasePath = Path.Combine(directory, "meal-planner.db") });
            services.AddSingleton<MealPlannerStore>();
        }));

    private static async Task<T> Post<T>(HttpClient client, string path, object body)
    {
        var response = await client.PostAsJsonAsync(path, body, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken))!;
    }

    private static async Task<T> Put<T>(HttpClient client, string path, object body)
    {
        var response = await client.PutAsJsonAsync(path, body, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken))!;
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
