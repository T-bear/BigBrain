using System.Net;
using System.Net.Http.Headers;
using System.Text;
using BigBrain.Api.MealPlanner;
using Microsoft.Extensions.Logging.Abstractions;

namespace BigBrain.Api.Tests;

public sealed class SchoolMealLiveTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Handler : HttpMessageHandler
    {
        public int Calls;
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Reply = (_, _) => Task.FromResult(Rss());
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref Calls);
            Assert.Equal("https", request.RequestUri!.Scheme);
            Assert.True(request.RequestUri.Host is "www.skolmat.info" or "www.tantgron.nu");
            return Reply(request, token);
        }
    }
    private static HttpResponseMessage Rss(string? text = null) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(text ?? SchoolMeals.ExampleRss, Encoding.UTF8, "application/rss+xml"),
        Headers = { CacheControl = new() { MaxAge = TimeSpan.FromHours(6) } }
    };
    private static MealPlannerOptions Enabled => new()
    {
        SchoolMeals = new() { Enabled = true, RosenfeldtEnabled = true, MusikugglanEnabled = true }
    };
    private static SchoolMealService Service(Handler handler, Clock clock, MealPlannerOptions? options = null) =>
        new(options ?? Enabled, new SchoolMealSourceClient(new HttpClient(handler), clock, NullLogger<SchoolMealSourceClient>.Instance), clock);
    private static Task<SchoolMealWeek> Get(SchoolMealService service) => service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken);

    [Fact]
    public async Task DefaultOffAndIndividualOffNeverSendHttp()
    {
        var handler = new Handler();
        using var off = Service(handler, new Clock(), new());
        Assert.Equal("offline", (await Get(off)).Status);
        using var individual = Service(handler, new Clock(), new() { SchoolMeals = new() { Enabled = true } });
        Assert.Equal("offline", (await Get(individual)).Status);
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public async Task BothSchoolsRespectGlobalAndIndividualFlags(bool enabled, bool rss, bool html)
    {
        var handler = new Handler();
        using var service = Service(handler, new Clock(), new()
        {
            SchoolMeals = new() { Enabled = enabled, RosenfeldtEnabled = rss, MusikugglanEnabled = html }
        });
        foreach (var school in new[] { SchoolMeals.Rosenfeldt, SchoolMeals.Musikugglan })
        {
            var permitted = enabled && (school == SchoolMeals.Rosenfeldt ? rss : html);
            var before = handler.Calls;
            var result = await service.GetAsync(school, 2026, 43, TestContext.Current.CancellationToken);
            Assert.Equal(permitted ? 1 : 0, handler.Calls - before);
            if (!permitted)
            {
                Assert.Equal("offline", result.Status);
                Assert.All(result.Days, day => Assert.Empty(day.Dishes));
            }
        }
    }

    [Fact]
    public async Task SourceModifiedAndFetchTimesStayDistinctOnCacheHitAndExpiredFailure()
    {
        var clock = new Clock();
        var modified = clock.Now.AddDays(-1);
        var handler = new Handler
        {
            Reply = (_, _) =>
            {
                var response = Rss();
                response.Content.Headers.LastModified = modified;
                return Task.FromResult(response);
            }
        };
        using var service = Service(handler, clock);
        var first = await Get(service);
        Assert.Equal(modified, first.SourceUpdatedAtUtc);
        Assert.Equal(clock.Now, first.FetchedAtUtc);
        clock.Now = clock.Now.AddHours(1);
        var cached = await Get(service);
        Assert.Equal(first, cached);
        Assert.Equal(1, handler.Calls);
        clock.Now = clock.Now.AddHours(5);
        handler.Reply = (_, _) => throw new HttpRequestException();
        var stale = await Get(service);
        Assert.False(stale.IsStale);
        Assert.Equal("unavailable", stale.Status);
        Assert.All(stale.Days, day => Assert.Empty(day.Dishes));
        Assert.Null(stale.SourceUpdatedAtUtc);
        Assert.Null(stale.FetchedAtUtc);
        Assert.Equal(clock.Now, stale.LastCheckedAtUtc);
    }

    [Fact]
    public async Task ConcurrentRequestsShareOneFetchAndSixHourInterval()
    {
        var gate = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new Handler { Reply = (_, _) => gate.Task };
        var clock = new Clock();
        using var service = Service(handler, clock);
        var requests = Enumerable.Range(0, 10).Select(_ => Get(service)).ToArray();
        Assert.Equal(1, handler.Calls);
        gate.SetResult(Rss());
        var results = await Task.WhenAll(requests);
        Assert.All(results, result => { Assert.Equal("published", result.Status); Assert.False(result.IsFixture); });
        clock.Now = clock.Now.AddHours(5.99);
        await Get(service);
        Assert.Equal(1, handler.Calls);
        handler.Reply = (_, _) => Task.FromResult(Rss());
        clock.Now = clock.Now.AddMinutes(1);
        await Get(service);
        Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData(500, "unavailable")]
    [InlineData(302, "unavailable")]
    [InlineData(206, "unavailable")]
    [InlineData(304, "invalid")]
    public async Task HttpFailuresAreHonestAndRateLimited(int code, string status)
    {
        var handler = new Handler { Reply = (_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)code)) };
        using var service = Service(handler, new Clock());
        Assert.Equal(status, (await Get(service)).Status);
        Assert.Equal(status, (await Get(service)).Status);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task FailedRefreshNeverServesExpiredContent()
    {
        var handler = new Handler();
        var clock = new Clock();
        using var service = Service(handler, clock);
        var first = await Get(service);
        handler.Reply = (_, _) => throw new HttpRequestException("not exposed");
        clock.Now = clock.Now.AddHours(6);
        var stale = await Get(service);
        Assert.False(stale.IsStale);
        Assert.Equal("unavailable", stale.Status);
        Assert.All(stale.Days, day => Assert.Empty(day.Dishes));
        Assert.Null(stale.FetchedAtUtc);
        Assert.Equal("unavailable", stale.SourceStatus);
        Assert.Equal(clock.Now, stale.LastCheckedAtUtc);
        clock.Now = clock.Now.AddHours(19);
        var expired = await Get(service);
        Assert.False(expired.IsStale);
        Assert.Equal("unavailable", expired.Status);
        Assert.All(expired.Days, d => Assert.Empty(d.Dishes));
        clock.Now = clock.Now.AddDays(7);
        Assert.Equal("notPublished", (await Get(service)).Status);
    }

    [Fact]
    public async Task InvalidRefreshHidesExpiredContentAndEmptyMenuReplacesIt()
    {
        var handler = new Handler();
        var clock = new Clock();
        using var service = Service(handler, clock);
        await Get(service);
        handler.Reply = (_, _) => Task.FromResult(Rss("<rss>"));
        clock.Now = clock.Now.AddHours(6);
        var invalid = await Get(service);
        Assert.False(invalid.IsStale);
        Assert.All(invalid.Days, day => Assert.Empty(day.Dishes));
        Assert.Equal("invalid", invalid.SourceStatus);
        handler.Reply = (_, _) => Task.FromResult(Rss("<rss version='2.0'><channel><title>Skolmat - Rosenfeldtskolan</title></channel></rss>"));
        clock.Now = clock.Now.AddHours(6);
        var empty = await Get(service);
        Assert.Equal("notPublished", empty.Status);
        Assert.False(empty.IsStale);
        Assert.All(empty.Days, d => Assert.Empty(d.Dishes));
    }

    [Fact]
    public async Task Conditional304RequiresMatchingCachedMenuAndKeepsFetchTime()
    {
        var handler = new Handler
        {
            Reply = (_, _) =>
        {
            var response = Rss();
            response.Headers.ETag = new EntityTagHeaderValue("\"menu-v1\"");
            return Task.FromResult(response);
        }
        };
        var clock = new Clock();
        using var service = Service(handler, clock);
        var first = await Get(service);
        handler.Reply = (request, _) =>
        {
            Assert.Equal("\"menu-v1\"", Assert.Single(request.Headers.IfNoneMatch).ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotModified)
            { Headers = { ETag = new("\"menu-v1\"") } });
        };
        clock.Now = clock.Now.AddHours(6);
        var unchanged = await Get(service);
        Assert.Equal("published", unchanged.Status);
        Assert.Equal(first.FetchedAtUtc, unchanged.FetchedAtUtc);
        Assert.Equal(clock.Now, unchanged.LastCheckedAtUtc);
    }

    [Theory]
    [InlineData(SchoolMeals.Rosenfeldt)]
    [InlineData(SchoolMeals.Musikugglan)]
    public async Task ExpiredMenuCannotBeRevivedBy304AfterLongRetryAfter(string school)
    {
        var html = school == SchoolMeals.Musikugglan;
        var week = html ? 44 : 43;
        var clock = new Clock { Now = new(2026, 10, html ? 26 : 19, 10, 0, 0, TimeSpan.Zero) };
        var started = clock.Now;
        var handler = new Handler
        {
            Reply = (_, _) =>
            {
                var response = html
                    ? new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(MusikugglanSchoolMeals.ExampleHtml, Encoding.UTF8, "text/html"),
                        Headers = { CacheControl = new() { MaxAge = TimeSpan.FromHours(6) } }
                    }
                    : Rss();
                response.Headers.ETag = new("\"original-menu\"");
                response.Content.Headers.LastModified = started.AddHours(-1);
                return Task.FromResult(response);
            }
        };
        using var service = Service(handler, clock);
        Task<SchoolMealWeek> Read() => service.GetAsync(school, 2026, week, TestContext.Current.CancellationToken);
        Assert.Equal("published", (await Read()).Status);
        clock.Now = started.AddHours(6);
        handler.Reply = (_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new(TimeSpan.FromHours(48));
            return Task.FromResult(response);
        };
        Assert.Equal("unavailable", (await Read()).Status);
        clock.Now = started.AddHours(24);
        Assert.Equal("unavailable", (await Read()).Status); // Retention never authorizes stale reuse.
        clock.Now = clock.Now.AddSeconds(1);
        var expired = await Read();
        Assert.Equal("unavailable", expired.Status);
        Assert.False(expired.IsStale);
        Assert.All(expired.Days, day => Assert.Empty(day.Dishes));
        Assert.Equal(2, handler.Calls); // Eviction never shortens Retry-After or triggers a fetch.
        clock.Now = started.AddHours(54);
        handler.Reply = (request, _) =>
        {
            Assert.Empty(request.Headers.IfNoneMatch);
            Assert.Null(request.Headers.IfModifiedSince);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotModified));
        };
        var unvalidated = await Read();
        Assert.Equal("invalid", unvalidated.Status);
        Assert.False(unvalidated.IsStale);
        Assert.All(unvalidated.Days, day => Assert.Empty(day.Dishes));
        Assert.Equal(3, handler.Calls);
        await Read();
        Assert.Equal(3, handler.Calls);
    }

    [Theory]
    [InlineData(SchoolMeals.Rosenfeldt)]
    [InlineData(SchoolMeals.Musikugglan)]
    public async Task Unsolicited304DoesNotValidateMenuWithoutRequestValidators(string school)
    {
        var html = school == SchoolMeals.Musikugglan;
        var week = html ? 44 : 43;
        var clock = new Clock { Now = new(2026, 10, html ? 26 : 19, 10, 0, 0, TimeSpan.Zero) };
        var handler = new Handler
        {
            Reply = (_, _) => Task.FromResult(html
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(MusikugglanSchoolMeals.ExampleHtml, Encoding.UTF8, "text/html"),
                    Headers = { CacheControl = new() { MaxAge = TimeSpan.FromHours(6) } }
                }
                : Rss())
        };
        using var service = Service(handler, clock);
        Task<SchoolMealWeek> Read() => service.GetAsync(school, 2026, week, TestContext.Current.CancellationToken);
        var first = await Read();
        Assert.Equal("published", first.Status);
        handler.Reply = (request, _) =>
        {
            Assert.Empty(request.Headers.IfNoneMatch);
            Assert.Null(request.Headers.IfModifiedSince);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotModified));
        };
        clock.Now = clock.Now.AddHours(6);
        var failed = await Read();
        Assert.Equal("invalid", failed.SourceStatus);
        Assert.False(failed.IsStale);
        Assert.All(failed.Days, day => Assert.Empty(day.Dishes));
        Assert.Null(failed.FetchedAtUtc);
        Assert.False(failed.IsFixture);
        await Read();
        Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData("no-store")]
    [InlineData("no-cache")]
    public async Task StricterCachePolicyClearsPreviousMenu(string policy)
    {
        var handler = new Handler();
        var clock = new Clock();
        using var service = Service(handler, clock);
        await Get(service);
        handler.Reply = (_, _) =>
        {
            var response = Rss();
            response.Headers.CacheControl = CacheControlHeaderValue.Parse(policy);
            return Task.FromResult(response);
        };
        clock.Now = clock.Now.AddHours(6);
        var blocked = await Get(service);
        Assert.Equal("cachePolicy", blocked.Status);
        Assert.All(blocked.Days, day => Assert.Empty(day.Dishes));
        Assert.False(blocked.IsStale);
    }

    [Fact]
    public async Task MustRevalidateDoesNotServeStaleOnError()
    {
        var handler = new Handler
        {
            Reply = (_, _) =>
        {
            var response = Rss();
            response.Headers.CacheControl = new() { MustRevalidate = true, MaxAge = TimeSpan.FromSeconds(60) };
            return Task.FromResult(response);
        }
        };
        var clock = new Clock();
        using var service = Service(handler, clock);
        await Get(service);
        handler.Reply = (_, _) => throw new HttpRequestException();
        clock.Now = clock.Now.AddHours(6);
        Assert.Equal("unavailable", (await Get(service)).Status);
    }

    [Fact]
    public async Task RetryAfterExtendsCooldownWithoutRetryLoop()
    {
        var clock = new Clock();
        var handler = new Handler
        {
            Reply = (_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromHours(12));
            return Task.FromResult(response);
        }
        };
        using var service = Service(handler, clock);
        await Get(service);
        clock.Now = clock.Now.AddHours(7);
        await Get(service);
        Assert.Equal(1, handler.Calls);
        clock.Now = clock.Now.AddHours(5);
        await Get(service);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task HtmlOnlyDerivesCurrentYearAndDoesNotFetchOtherWeeks()
    {
        var clock = new Clock { Now = new(2026, 10, 26, 10, 0, 0, TimeSpan.Zero) };
        var handler = new Handler
        {
            Reply = (request, _) =>
        {
            Assert.Equal("https://www.tantgron.nu/matsedel/", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(MusikugglanSchoolMeals.ExampleHtml, Encoding.UTF8, "text/html"),
                Headers = { CacheControl = new() { MaxAge = TimeSpan.FromHours(6) } }
            });
        }
        };
        using var service = Service(handler, clock);
        var menu = await service.GetAsync(SchoolMeals.Musikugglan, 2026, 44, TestContext.Current.CancellationToken);
        Assert.Equal("htmlWeekRequestedYear", menu.DateBasis);
        Assert.Equal(5, menu.Days.Count(d => d.Status == "published"));
        Assert.Equal("notPublished", (await service.GetAsync(SchoolMeals.Musikugglan, 2026, 45, TestContext.Current.CancellationToken)).Status);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task WeekRolloverAndIsoYearNeverRelabelPreviousMenu()
    {
        var clock = new Clock { Now = new(2026, 10, 25, 22, 30, 0, TimeSpan.Zero) };
        var handler = new Handler();
        using var service = Service(handler, clock);
        Assert.Equal((2026, 43), service.CurrentWeek());
        await Get(service);
        clock.Now = clock.Now.AddHours(1);
        Assert.Equal((2026, 44), service.CurrentWeek());
        var next = await service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 44, TestContext.Current.CancellationToken);
        Assert.Equal("notPublished", next.Status);
        Assert.All(next.Days, day => Assert.Empty(day.Dishes));
        Assert.Equal(1, handler.Calls);
        clock.Now = new(2027, 1, 1, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal((2026, 53), service.CurrentWeek());
    }

    [Fact]
    public async Task RestartDropsMenuAndCooldownAndSourcesRemainIndependent()
    {
        var handler = new Handler();
        var clock = new Clock();
        var options = Enabled;
        using (var first = Service(handler, clock, options)) await Get(first);
        using var restarted = Service(handler, clock, options);
        Assert.Equal("published", (await Get(restarted)).Status);
        Assert.Equal(2, handler.Calls);
        Assert.Equal("invalid", (await restarted.GetAsync(SchoolMeals.Musikugglan, 2026, 43, TestContext.Current.CancellationToken)).Status);
        Assert.Equal(3, handler.Calls);
        clock.Now = clock.Now.AddHours(6);
        Assert.Equal("published", (await Get(restarted)).Status);
        Assert.Equal(4, handler.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OversizedBodyIsRejectedWithOrWithoutContentLength(bool knownLength)
    {
        var bytes = new byte[SchoolMealSourceClient.MaximumBytes + 1];
        var content = knownLength ? (HttpContent)new ByteArrayContent(bytes) : new StreamContent(new NonSeekableStream(bytes));
        content.Headers.ContentType = new("application/rss+xml");
        var handler = new Handler { Reply = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content, Headers = { CacheControl = new() { MaxAge = TimeSpan.FromHours(6) } } }) };
        using var service = Service(handler, new Clock());
        Assert.Equal("invalid", (await Get(service)).Status);
    }

    private sealed class NonSeekableStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeadlineCoversHeadersAndBody(bool slowBody)
    {
        var handler = new Handler
        {
            Reply = async (_, token) =>
        {
            if (!slowBody) await Task.Delay(Timeout.Infinite, token);
            var content = new StreamContent(new SlowStream());
            content.Headers.ContentType = new("application/rss+xml");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content, Headers = { CacheControl = new() { MaxAge = TimeSpan.FromHours(6) } } };
        }
        };
        using var service = Service(handler, new Clock());
        Assert.Equal("unavailable", (await Get(service)).Status);
        Assert.Equal(1, handler.Calls);
    }
    private sealed class SlowStream : MemoryStream
    {
        public override bool CanSeek => false;
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }
    }

    [Fact]
    public async Task CancellationReleasesGateButKeepsCooldown()
    {
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new Handler
        {
            Reply = async (_, token) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, token);
            return Rss();
        }
        };
        using var service = Service(handler, new Clock());
        var pending = service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, cancellation.Token);
        await started.Task;
        cancellation.Cancel();
        Assert.Equal("unavailable", (await pending).Status);
        Assert.Equal("unavailable", (await Get(service)).Status);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData("text/plain", "utf-8")]
    [InlineData("application/rss+xml", "utf-16")]
    public async Task UnexpectedContentTypeOrEncodingIsRejected(string type, string charset)
    {
        var handler = new Handler
        {
            Reply = (_, _) =>
        {
            var response = Rss();
            response.Content.Headers.ContentType = new(type) { CharSet = charset };
            return Task.FromResult(response);
        }
        };
        using var service = Service(handler, new Clock());
        Assert.Equal("invalid", (await Get(service)).Status);
    }

    [Fact]
    public async Task ExpiredHttpContentIsNotCachedAsFresh()
    {
        var clock = new Clock();
        var handler = new Handler
        {
            Reply = (_, _) =>
        {
            var response = Rss();
            response.Headers.CacheControl = null;
            response.Content.Headers.Expires = clock.Now.AddMinutes(-1);
            return Task.FromResult(response);
        }
        };
        using var service = Service(handler, clock);
        Assert.Equal("cachePolicy", (await Get(service)).Status);
    }

    [Fact]
    public async Task CooldownStartsAfterSourceCompletion()
    {
        var clock = new Clock();
        var start = clock.Now;
        var handler = new Handler { Reply = (_, _) => { clock.Now = start.AddHours(2); return Task.FromResult(Rss()); } };
        using var service = Service(handler, clock);
        Assert.Equal("published", (await Get(service)).Status);
        clock.Now = start.AddHours(8).AddTicks(-1);
        await Get(service);
        Assert.Equal(1, handler.Calls);
        handler.Reply = (_, _) => Task.FromResult(Rss());
        clock.Now = start.AddHours(8);
        await Get(service);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task SlowSchoolDoesNotBlockOtherSchool()
    {
        var waiting = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new Handler
        {
            Reply = (request, _) => request.RequestUri!.Host == "www.skolmat.info"
            ? waiting.Task : Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
        };
        using var service = Service(handler, new Clock());
        var slow = Get(service);
        var other = await service.GetAsync(SchoolMeals.Musikugglan, 2026, 43, TestContext.Current.CancellationToken);
        Assert.Equal("unavailable", other.Status);
        Assert.False(slow.IsCompleted);
        Assert.Equal(2, handler.Calls);
        waiting.SetResult(Rss());
        Assert.Equal("published", (await slow).Status);
        await Get(service);
        Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UncertainHeadersBlockOnlyThisSchoolUntilProcessRestart(bool malformedRetry)
    {
        var clock = new Clock();
        var handler = new Handler
        {
            Reply = (_, _) =>
        {
            if (!malformedRetry) throw new HttpRequestException();
            var response = Rss();
            response.Headers.TryAddWithoutValidation("Retry-After", "not-a-date-or-delay");
            return Task.FromResult(response);
        }
        };
        using var service = Service(handler, clock);
        Assert.Equal("unavailable", (await Get(service)).Status);
        clock.Now = clock.Now.AddDays(2);
        await Get(service);
        Assert.Equal(1, handler.Calls);
        await service.GetAsync(SchoolMeals.Musikugglan, 2026, 43, TestContext.Current.CancellationToken);
        Assert.Equal(2, handler.Calls);
        handler.Reply = (_, _) => Task.FromResult(Rss());
        using var restarted = Service(handler, clock);
        Assert.Equal("published", (await Get(restarted)).Status);
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public async Task FetchDoesNotOpenOrCreateMealPlannerDatabase()
    {
        var directory = Directory.CreateTempSubdirectory("school-memory-");
        try
        {
            // A directory is not a usable SQLite file; school fetching must not try to open it.
            var handler = new Handler();
            using var service = Service(handler, new Clock(), Enabled with { DatabasePath = directory.FullName });
            Assert.Equal("published", (await Get(service)).Status);
            Assert.Empty(directory.EnumerateFileSystemInfos());
        }
        finally { directory.Delete(true); }
    }

    [Fact]
    public async Task FetchLeavesExistingHouseholdDatabaseAndHistoricalGateUntouched()
    {
        var directory = Directory.CreateTempSubdirectory("school-household-");
        try
        {
            var path = Path.Combine(directory.FullName, "meal-planner.db");
            var options = Enabled with { DatabasePath = path };
            using var household = new MealPlannerStore(options);
            Assert.True(household.IsAvailable);
            using (var db = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
            {
                db.Open();
                using var command = db.CreateCommand();
                // Historical or malformed gate data must be ignored, not migrated or deleted.
                command.CommandText = "CREATE TABLE SchoolMealFetchGate (HistoricalState TEXT); INSERT INTO SchoolMealFetchGate VALUES ('untouched');";
                command.ExecuteNonQuery();
            }
            Dictionary<string, byte[]> Snapshot() => directory.EnumerateFiles().ToDictionary(file => file.Name, file => File.ReadAllBytes(file.FullName));
            var before = Snapshot();
            var handler = new Handler();
            using var service = Service(handler, new Clock(), options);
            Assert.Equal("published", (await Get(service)).Status);
            await service.GetAsync(SchoolMeals.Musikugglan, 2026, 43, TestContext.Current.CancellationToken);
            var after = Snapshot();
            Assert.Equal(before.Keys.Order(), after.Keys.Order());
            foreach (var file in before) Assert.Equal(file.Value, after[file.Key]);
        }
        finally { directory.Delete(true); }
    }

    [Fact]
    public async Task LastModifiedAloneCanValidate304WithoutChangingFetchProvenance()
    {
        var clock = new Clock();
        var modified = clock.Now.AddHours(-1);
        var handler = new Handler
        {
            Reply = (_, _) =>
        {
            var response = Rss();
            response.Content.Headers.LastModified = modified;
            return Task.FromResult(response);
        }
        };
        using var service = Service(handler, clock);
        var first = await Get(service);
        clock.Now = clock.Now.AddHours(6);
        handler.Reply = (request, _) =>
        {
            Assert.Empty(request.Headers.IfNoneMatch);
            Assert.Equal(modified, request.Headers.IfModifiedSince);
            var response = new HttpResponseMessage(HttpStatusCode.NotModified);
            response.Content.Headers.LastModified = modified;
            return Task.FromResult(response);
        };
        var validated = await Get(service);
        Assert.Equal("published", validated.Status);
        Assert.Equal(first.FetchedAtUtc, validated.FetchedAtUtc);
        Assert.Equal(modified, validated.SourceUpdatedAtUtc);
    }

    [Fact]
    public void TransportRejectsRedirectsAndCookies()
    {
        using var handler = SchoolMealSourceClient.CreateHandler();
        Assert.False(handler.AllowAutoRedirect);
        Assert.False(handler.UseCookies);
        Assert.Null(handler.Credentials);
    }
}
