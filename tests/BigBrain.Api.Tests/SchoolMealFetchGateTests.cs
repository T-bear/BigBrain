using System.Net;
using BigBrain.Api.MealPlanner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace BigBrain.Api.Tests;

// Known-empty synthetic history only. Production never receives these bootstrap values.
internal sealed class SchoolMealTestData : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("school-gate-").FullName;
    private readonly List<MealPlannerStore> stores = [];
    public MealPlannerOptions Options(MealPlannerOptions options, DateTimeOffset now) => options with
    {
        DatabasePath = options.DatabasePath == "data/meal-planner.db" ? Path.Combine(directory, Guid.NewGuid() + ".db") : options.DatabasePath,
        SchoolMeals = options.SchoolMeals with
        {
            RosenfeldtVerifiedInitialNotBeforeUtc = now,
            MusikugglanVerifiedInitialNotBeforeUtc = now
        }
    };
    public MealPlannerStore Store(MealPlannerOptions options)
    {
        var store = new MealPlannerStore(options);
        stores.Add(store);
        return store;
    }
    public void Dispose()
    {
        foreach (var store in stores) store.Dispose();
        SqliteConnection.ClearAllPools();
        Directory.Delete(directory, true);
    }
}

public sealed class SchoolMealFetchGateTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = SchoolMealFetchGateTests.Now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Handler : HttpMessageHandler
    {
        public int Calls;
        public Func<Task<HttpResponseMessage>> Reply = () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref Calls);
            return Reply();
        }
    }
    private static SchoolMealService Service(MealPlannerOptions options, MealPlannerStore store, Handler handler, Clock clock) =>
        new(options, new SchoolMealSourceClient(new HttpClient(handler), clock, NullLogger<SchoolMealSourceClient>.Instance), clock, store);
    private static Task<SchoolMealWeek> Read(SchoolMealService service, string school = SchoolMeals.Rosenfeldt) =>
        service.GetAsync(school, 2026, 43, TestContext.Current.CancellationToken);
    private static MealPlannerOptions Enabled => new() { SchoolMeals = new() { Enabled = true, RosenfeldtEnabled = true, MusikugglanEnabled = true } };

    [Fact]
    public async Task UnknownHistoryNeedsVerifiedBoundAndDoesNotChangeHousehold()
    {
        using var data = new SchoolMealTestData();
        var options = data.Options(Enabled, Now);
        var store = data.Store(options);
        var before = await store.GetTagsAsync(TestContext.Current.CancellationToken);
        Assert.Null(await store.TryBeginSchoolFetchAsync(SchoolMeals.Rosenfeldt, Now, null, TestContext.Current.CancellationToken));
        Assert.Null(await store.TryBeginSchoolFetchAsync(SchoolMeals.Rosenfeldt, Now.AddDays(30), null, TestContext.Current.CancellationToken));
        Assert.Null(await store.TryBeginSchoolFetchAsync(SchoolMeals.Rosenfeldt, Now, Now.AddHours(12), TestContext.Current.CancellationToken));
        Assert.NotNull(await store.TryBeginSchoolFetchAsync(SchoolMeals.Rosenfeldt, Now.AddHours(12), Now, TestContext.Current.CancellationToken));
        Assert.NotNull(await store.TryBeginSchoolFetchAsync(SchoolMeals.Musikugglan, Now, Now, TestContext.Current.CancellationToken));
        Assert.Equal(before, await store.GetTagsAsync(TestContext.Current.CancellationToken));
        Assert.True(store.IsAvailable);
        using var connection = new SqliteConnection($"Data Source={options.DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version";
        Assert.Equal(2L, command.ExecuteScalar()); // Old household schema remains readable.
    }

    [Fact]
    public async Task CrossInstanceAtomicReservationAndCrashRemainBlockedForOnlyOneSchool()
    {
        using var data = new SchoolMealTestData();
        var options = data.Options(Enabled, Now);
        var stores = Enumerable.Range(0, 8).Select(_ => data.Store(options)).ToArray();
        var attempts = await Task.WhenAll(stores.Select(s => Task.Run(() => s.TryBeginSchoolFetchAsync(SchoolMeals.Rosenfeldt, Now, Now, TestContext.Current.CancellationToken), TestContext.Current.CancellationToken)));
        var winner = Assert.Single(attempts, a => a is not null);
        var restarted = data.Store(options);
        Assert.Null(await restarted.TryBeginSchoolFetchAsync(SchoolMeals.Rosenfeldt, Now.AddDays(30), Now, TestContext.Current.CancellationToken));
        Assert.False(await restarted.CompleteSchoolFetchAsync(SchoolMeals.Rosenfeldt, "wrong", Now, TestContext.Current.CancellationToken));
        Assert.NotNull(await restarted.TryBeginSchoolFetchAsync(SchoolMeals.Musikugglan, Now, Now, TestContext.Current.CancellationToken));
        Assert.True(await restarted.CompleteSchoolFetchAsync(SchoolMeals.Rosenfeldt, winner!, Now.AddHours(12), TestContext.Current.CancellationToken));
        Assert.Null(await restarted.TryBeginSchoolFetchAsync(SchoolMeals.Rosenfeldt, Now.AddHours(6), Now, TestContext.Current.CancellationToken));
        Assert.NotNull(await restarted.TryBeginSchoolFetchAsync(SchoolMeals.Rosenfeldt, Now.AddHours(12), Now, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RetryAfterOn503SurvivesServiceAndStoreRestart()
    {
        using var data = new SchoolMealTestData();
        var options = data.Options(Enabled, Now);
        var handler = new Handler
        {
            Reply = () =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            response.Headers.RetryAfter = new(TimeSpan.FromHours(12));
            return Task.FromResult(response);
        }
        };
        var clock = new Clock();
        using (var first = Service(options, data.Store(options), handler, clock)) await Read(first);
        using var second = Service(options, data.Store(options), handler, clock);
        clock.Now = Now.AddHours(6);
        await Read(second);
        Assert.Equal(1, handler.Calls);
        clock.Now = Now.AddHours(12);
        await Read(second);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task UnseenResponseCannotLoseRetryAfterOnRestart()
    {
        using var data = new SchoolMealTestData();
        var options = data.Options(Enabled, Now);
        var handler = new Handler { Reply = () => throw new HttpRequestException() };
        var clock = new Clock();
        using (var first = Service(options, data.Store(options), handler, clock)) await Read(first);
        clock.Now = Now.AddHours(18);
        using var second = Service(options, data.Store(options), handler, clock);
        Assert.Equal("unavailable", (await Read(second)).Status);
        Assert.Equal(1, handler.Calls);
        await Read(second, SchoolMeals.Musikugglan);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task MalformedGateDoesNotDisableHouseholdOrSendHttp()
    {
        using var data = new SchoolMealTestData();
        var options = data.Options(Enabled, Now);
        var store = data.Store(options);
        using var connection = new SqliteConnection($"Data Source={options.DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE SchoolMealFetchGate (Unexpected TEXT)";
        command.ExecuteNonQuery();
        var handler = new Handler();
        using var service = Service(options, store, handler, new Clock());
        Assert.Equal("unavailable", (await Read(service)).Status);
        Assert.Equal(0, handler.Calls);
        Assert.True(store.IsAvailable);
        Assert.NotEmpty(await store.GetTagsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FailedCompletionLeavesPendingAndCannotAuthorizeAnotherRequest()
    {
        using var data = new SchoolMealTestData();
        var options = data.Options(Enabled, Now);
        var store = data.Store(options);
        var attempt = await store.TryBeginSchoolFetchAsync(SchoolMeals.Rosenfeldt, Now, Now, TestContext.Current.CancellationToken);
        using var connection = new SqliteConnection($"Data Source={options.DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TRIGGER fail_gate BEFORE UPDATE ON SchoolMealFetchGate BEGIN SELECT RAISE(FAIL, 'synthetic'); END";
        command.ExecuteNonQuery();
        Assert.False(await store.CompleteSchoolFetchAsync(SchoolMeals.Rosenfeldt, attempt!, Now.AddHours(6), TestContext.Current.CancellationToken));
        command.CommandText = "DROP TRIGGER fail_gate";
        command.ExecuteNonQuery();
        Assert.Null(await store.TryBeginSchoolFetchAsync(SchoolMeals.Rosenfeldt, Now.AddDays(30), Now, TestContext.Current.CancellationToken));
        Assert.NotEmpty(await store.GetTagsAsync(TestContext.Current.CancellationToken));
    }
    [Theory]
    [InlineData("no-store", false)]
    [InlineData("no-cache", false)]
    [InlineData("max-age=60", false)]
    [InlineData("no-store", true)]
    public async Task CacheProhibitionSurvivesPersistenceFailureAndUnknownRetry(string policy, bool invalidRetry)
    {
        using var data = new SchoolMealTestData();
        var options = data.Options(Enabled, Now);
        var store = data.Store(options);
        var handler = new Handler
        {
            Reply = () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(SchoolMeals.ExampleRss, System.Text.Encoding.UTF8, "application/rss+xml") })
        };
        var clock = new Clock();
        using var service = Service(options, store, handler, clock);
        Assert.Equal("published", (await Read(service)).Status);
        handler.Reply = () =>
        {
            // Fail only completion (reservation already committed), never household writes.
            using var connection = new SqliteConnection($"Data Source={options.DatabasePath}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TRIGGER fail_gate BEFORE UPDATE ON SchoolMealFetchGate WHEN NEW.Pending = 0 BEGIN SELECT RAISE(FAIL, 'synthetic'); END";
            command.ExecuteNonQuery();
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.CacheControl = System.Net.Http.Headers.CacheControlHeaderValue.Parse(policy);
            if (invalidRetry) response.Headers.TryAddWithoutValidation("Retry-After", "invalid");
            return Task.FromResult(response);
        };
        clock.Now = Now.AddHours(6);
        var failed = await Read(service);
        Assert.Equal("cachePolicy", failed.Status);
        Assert.False(failed.IsStale);
        Assert.All(failed.Days, day => Assert.Empty(day.Dishes));
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task ConcurrentServicesShareOneActualHttpRequest()
    {
        using var data = new SchoolMealTestData();
        var options = data.Options(Enabled, Now);
        var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new Handler { Reply = () => response.Task };
        var clock = new Clock();
        using var first = Service(options, data.Store(options), handler, clock);
        using var second = Service(options, data.Store(options), handler, clock);
        var pending = Read(first);
        Assert.Equal("unavailable", (await Read(second)).Status);
        Assert.Equal(1, handler.Calls);
        response.SetResult(new(HttpStatusCode.ServiceUnavailable));
        await pending;
        clock.Now = Now.AddHours(5);
        await Read(second);
        Assert.Equal(1, handler.Calls);
    }

}
