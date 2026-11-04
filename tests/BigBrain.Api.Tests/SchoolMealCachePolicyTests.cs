using System.Net;
using System.Net.Http.Headers;
using System.Text;
using BigBrain.Api.MealPlanner;
using Microsoft.Extensions.Logging.Abstractions;

namespace BigBrain.Api.Tests;

public sealed class SchoolMealCachePolicyTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            return Task.FromResult(reply(request));
        }
    }

    private static HttpResponseMessage Response(string policy, string school = SchoolMeals.Rosenfeldt) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(school == SchoolMeals.Rosenfeldt ? SchoolMeals.ExampleRss : MusikugglanSchoolMeals.ExampleHtml,
            Encoding.UTF8, school == SchoolMeals.Rosenfeldt ? "application/rss+xml" : "text/html"),
        Headers = { CacheControl = CacheControlHeaderValue.Parse(policy) }
    };

    private static SchoolMealService Service(HttpClient http, Clock clock, bool daily = false) => new(
        new() { SchoolMeals = new() { Enabled = true, RosenfeldtEnabled = true, MusikugglanEnabled = true, DailySnapshot = daily } },
        new SchoolMealSourceClient(http, clock, NullLogger<SchoolMealSourceClient>.Instance), clock);

    [Fact]
    public async Task DailySnapshotRetainsWeeklyMenuAfterFailedCheck()
    {
        var clock = new Clock();
        var start = clock.Now;

        using var handler = new Handler(_ =>
        {
            if (clock.Now > start)
                return new HttpResponseMessage(
                    HttpStatusCode.ServiceUnavailable);

            var response = Response("max-age=60, must-revalidate");
            response.Headers.Date = clock.Now;
            return response;
        });

        using var http = new HttpClient(handler);
        using var service = Service(http, clock, daily: true);

        Task<SchoolMealWeek> Read() => service.GetAsync(
            SchoolMeals.Rosenfeldt, 2026, 43,
            TestContext.Current.CancellationToken);

        var first = await Read();
        Assert.Equal("published", first.Status);

        clock.Now = start.AddHours(6);
        Assert.Equal("published", (await Read()).Status);
        Assert.Equal(1, handler.Calls);

        clock.Now = start.AddHours(24);
        var retained = await Read();

        Assert.Equal("published", retained.Status);
        Assert.True(retained.IsStale);
        Assert.Equal("unavailable", retained.SourceStatus);
        Assert.Equal(first.Days[0].Dishes.ToArray(),
                     retained.Days[0].Dishes.ToArray());
        Assert.Equal(2, handler.Calls);

        await Read();
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task DailySnapshotAcceptsVaryStar()
    {
        var clock = new Clock();
        clock.Now = clock.Now.AddDays(7);

        using var handler = new Handler(_ =>
        {
            var response = Response(
                "max-age=3600", SchoolMeals.Musikugglan);
            response.Headers.Vary.Add("*");
            return response;
        });

        using var http = new HttpClient(handler);
        using var service = Service(http, clock, daily: true);

        Task<SchoolMealWeek> Read() => service.GetAsync(
            SchoolMeals.Musikugglan, 2026, 44,
            TestContext.Current.CancellationToken);

        Assert.Equal("published", (await Read()).Status);

        clock.Now = clock.Now.AddHours(3);
        Assert.Equal("published", (await Read()).Status);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task DailySnapshotAcceptsFull200WithExpiredHttpFreshness()
    {
        var clock = new Clock();

        using var handler = new Handler(_ =>
        {
            var response = Response("max-age=60, must-revalidate");
            response.Headers.Date = clock.Now.AddHours(-4);
            response.Headers.Age = TimeSpan.FromHours(4);
            return response;
        });

        using var http = new HttpClient(handler);
        using var service = Service(http, clock, daily: true);

        var menu = await service.GetAsync(
            SchoolMeals.Rosenfeldt, 2026, 43,
            TestContext.Current.CancellationToken);

        Assert.Equal("published", menu.Status);
    }

    [Theory]
    [InlineData("no-store, max-age=60")]
    [InlineData("no-cache, max-age=60")]
    public async Task DailySnapshotKeepsExplicitCacheProhibitions(string policy)
    {
        var clock = new Clock();

        using var handler = new Handler(_ => Response(policy));
        using var http = new HttpClient(handler);
        using var service = Service(http, clock, daily: true);

        Task<SchoolMealWeek> Read() => service.GetAsync(
            SchoolMeals.Rosenfeldt, 2026, 43,
            TestContext.Current.CancellationToken);

        Assert.Equal("cachePolicy", (await Read()).Status);

        clock.Now = clock.Now.AddSeconds(61);
        await Read();
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(SchoolMeals.Rosenfeldt, 60, true, 43)]
    [InlineData(SchoolMeals.Musikugglan, 3600, false, 44)]
    public async Task ProductionPoliciesReuseFreshMenusAndFetchOnDemandAtExpiry(string school, int seconds, bool revalidate, int week)
    {
        var clock = new Clock();
        if (week == 44) clock.Now = clock.Now.AddDays(7);
        var start = clock.Now;
        using var handler = new Handler(_ => Response($"max-age={seconds}" + (revalidate ? ", must-revalidate" : ""), school));
        using var http = new HttpClient(handler);
        using var service = Service(http, clock);
        Task<SchoolMealWeek> Read() => service.GetAsync(school, 2026, week, TestContext.Current.CancellationToken);
        var first = await Read();
        Assert.Equal("published", first.Status);
        Assert.False(first.IsFixture);
        Assert.Equal(start.AddSeconds(seconds), first.ValidUntilUtc);
        var fixture = SchoolMeals.Preview(school, 2026, week);
        Assert.Equal(fixture.DateBasis, first.DateBasis);
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(fixture.Days[i].Date, first.Days[i].Date);
            Assert.Equal(fixture.Days[i].Dishes, first.Days[i].Dishes);
        }
        clock.Now = start.AddSeconds(59);
        Assert.Equal(first, await Read());
        if (seconds > 60)
        {
            clock.Now = start.AddSeconds(60);
            Assert.Equal(first, await Read()); // Cooldown expiry does not invalidate a fresh hour-long menu.
            Assert.Equal(1, handler.Calls);
        }
        clock.Now = start.AddSeconds(seconds).AddTicks(-1);
        Assert.Equal("published", (await Read()).Status);
        clock.Now = start.AddSeconds(seconds);
        var concurrent = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Read()));
        Assert.All(concurrent, menu =>
        {
            Assert.Equal("published", menu.Status);
            Assert.False(menu.IsStale);
            Assert.Equal(clock.Now.AddSeconds(seconds), menu.ValidUntilUtc);
        });
        Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExpiredMenuWaitsForFloorThenAccepts200Or304(bool notModified)
    {
        var clock = new Clock();
        var start = clock.Now;
        var calls = 0;
        using var handler = new Handler(request =>
        {
            calls++;
            if (calls > 1) Assert.Equal("\"menu\"", Assert.Single(request.Headers.IfNoneMatch).Tag);
            var response = calls > 1 && notModified ? new HttpResponseMessage(HttpStatusCode.NotModified) : Response("max-age=30");
            response.Headers.ETag = new("\"menu\"");
            return response;
        });
        using var http = new HttpClient(handler);
        using var service = Service(http, clock);
        Task<SchoolMealWeek> Read() => service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken);
        var first = await Read();
        clock.Now = start.AddSeconds(59);
        var blocked = await Read();
        Assert.Equal("unavailable", blocked.Status);
        Assert.All(blocked.Days, day => Assert.Empty(day.Dishes));
        Assert.Equal(1, handler.Calls);
        clock.Now = start.AddSeconds(60);
        var refreshed = await Read();
        Assert.Equal("published", refreshed.Status);
        Assert.Equal(clock.Now.AddSeconds(30), refreshed.ValidUntilUtc);
        Assert.Equal(notModified ? first.FetchedAtUtc : clock.Now, refreshed.FetchedAtUtc);
        Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData("max-age=60", 30, 0, 0, 30)]
    [InlineData("max-age=60", 0, 45, 0, 15)]
    [InlineData("max-age=60", 10, 0, 5, 45)]
    [InlineData("max-age=60", 10, 30, 5, 30)]
    [InlineData("max-age=3600, s-maxage=60", 0, 0, 0, 60)]
    [InlineData("max-age=60, s-maxage=3600", 0, 0, 0, 3600)]
    [InlineData("max-age=60, must-revalidate, proxy-revalidate", 0, 0, 0, 60)]
    [InlineData("public", 10, 30, 0, 30)]
    public async Task FreshnessAccountsForAgeDateDelaySharedCacheAndExpires(string policy, int age, int apparentAge, int delay, int remaining)
    {
        var clock = new Clock();
        using var handler = new Handler(_ =>
        {
            clock.Now = clock.Now.AddSeconds(delay);
            var response = Response(policy);
            response.Headers.Date = clock.Now.AddSeconds(-apparentAge);
            response.Headers.Age = TimeSpan.FromSeconds(age);
            response.Headers.RetryAfter = new(TimeSpan.FromHours(2));
            response.Content.Headers.Expires = response.Headers.Date.Value.AddSeconds(60);
            return response;
        });
        using var http = new HttpClient(handler);
        using var service = Service(http, clock);
        var menu = await service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken);
        Assert.Equal("published", menu.Status);
        Assert.Equal(clock.Now.AddSeconds(remaining), menu.ValidUntilUtc);
        clock.Now = clock.Now.AddSeconds(remaining);
        Assert.Equal("unavailable", (await service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken)).Status);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData("no-store, max-age=60", null, null)]
    [InlineData("no-cache, max-age=60", null, null)]
    [InlineData("no-cache=\"Set-Cookie\", max-age=60", null, null)]
    [InlineData("private, max-age=60", null, null)]
    [InlineData("max-age=0", null, null)]
    [InlineData("max-age=60", "Age", "60")]
    [InlineData("max-age=60", "Age", "bad")]
    [InlineData("max-age=60", "Date", "bad")]
    [InlineData("max-age=60", "Vary", "*")]

    public async Task RestrictionsNeverPublishOrShortenCooldown(string policy, string? header, string? value)
    {
        var clock = new Clock();
        using var handler = new Handler(_ =>
        {
            var response = Response(policy);
            if (header is not null) response.Headers.TryAddWithoutValidation(header, value);
            return response;
        });
        using var http = new HttpClient(handler);
        using var service = Service(http, clock);
        var menu = await service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken);
        Assert.Equal("cachePolicy", menu.Status);
        Assert.All(menu.Days, day => Assert.Empty(day.Dishes));
        clock.Now = clock.Now.AddSeconds(59);
        await service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(null, false, "published", 60)]
    [InlineData("max-age=120", false, "published", 120)]
    [InlineData("max-age=120, no-cache", false, "cachePolicy", 0)]
    [InlineData("max-age=120, no-store", false, "cachePolicy", 0)]
    [InlineData("max-age=120", true, "invalid", 0)]
    public async Task Conditional304InheritsOrReplacesFreshnessButCannotBypassRestrictions(string? update, bool wrongTag, string status, int seconds)
    {
        var clock = new Clock();
        var refresh = false;
        using var handler = new Handler(request =>
        {
            var response = refresh ? new HttpResponseMessage(HttpStatusCode.NotModified) : Response("max-age=60, must-revalidate");
            response.Headers.ETag = new(wrongTag && refresh ? "\"other\"" : "\"menu\"");
            if (refresh)
            {
                response.Headers.RetryAfter = new(TimeSpan.FromHours(1));
                Assert.Equal("\"menu\"", Assert.Single(request.Headers.IfNoneMatch).Tag);
                if (update is not null) response.Headers.CacheControl = CacheControlHeaderValue.Parse(update);
            }
            return response;
        });
        using var http = new HttpClient(handler);
        using var service = Service(http, clock);
        Task<SchoolMealWeek> Read() => service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken);
        var first = await Read();
        clock.Now = clock.Now.AddHours(6);
        refresh = true;
        var next = await Read();
        Assert.Equal(status, next.Status);
        if (seconds > 0)
        {
            Assert.Equal(first.FetchedAtUtc, next.FetchedAtUtc);
            Assert.Equal(clock.Now.AddSeconds(seconds), next.ValidUntilUtc);
            clock.Now = clock.Now.AddSeconds(seconds);
            Assert.Equal("unavailable", (await Read()).Status);
        }
        else Assert.All(next.Days, day => Assert.Empty(day.Dishes));
        Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData(null, true, "\"new\"", "same", false)]
    [InlineData(null, true, null, "missing", false)]
    [InlineData("\"menu\"", false, "\"menu\"", "missing", true)]
    [InlineData("\"menu\"", false, "\"other\"", "missing", false)]
    [InlineData("W/\"menu\"", false, "\"menu\"", "missing", false)]
    [InlineData("\"menu\"", false, "W/\"menu\"", "missing", true)]
    [InlineData("W/\"menu\"", false, "W/\"menu\"", "missing", true)]
    [InlineData("W/\"menu\"", false, "W/\"other\"", "missing", false)]
    [InlineData("\"menu\"", true, "\"other\"", "same", false)]
    [InlineData("\"menu\"", true, "W/\"other\"", "same", false)]
    [InlineData("\"menu\"", false, null, "missing", false)]
    [InlineData(null, true, null, "same", true)]
    [InlineData(null, true, null, "different", false)]
    [InlineData("\"menu\"", true, null, "same", true)]
    [InlineData(null, true, "W/\"new\"", "same", true)]
    [InlineData(null, true, "bad", "same", false)]
    [InlineData("\"menu\"", true, "\"menu\"", "bad", false)]
    public async Task RevalidationRequiresCorrespondingValidatorAndPreservesCooldown(
        string? storedTag, bool storedModified, string? receivedTag, string receivedModified, bool accepted)
    {
        var clock = new Clock();
        var modified = clock.Now.AddHours(-1);
        var attempt = 0;
        using var handler = new Handler(request =>
        {
            attempt++;
            if (attempt == 1)
            {
                var original = Response("max-age=60");
                if (storedTag is not null) original.Headers.ETag = EntityTagHeaderValue.Parse(storedTag);
                if (storedModified) original.Content.Headers.LastModified = modified;
                return original;
            }
            if (attempt == 4)
            {
                // Failed validation must not mutate validators or extend 24-hour retention.
                Assert.Empty(request.Headers.IfNoneMatch);
                Assert.Null(request.Headers.IfModifiedSince);
                return new(HttpStatusCode.NotModified);
            }
            Assert.Equal(storedTag, request.Headers.IfNoneMatch.SingleOrDefault()?.ToString());
            Assert.Equal(storedModified ? modified : (DateTimeOffset?)null, request.Headers.IfModifiedSince);
            if (attempt == 3) return new(HttpStatusCode.NotModified);
            var response = new HttpResponseMessage(HttpStatusCode.NotModified);
            response.Headers.CacheControl = new() { MaxAge = TimeSpan.FromHours(24) };
            response.Headers.RetryAfter = new(TimeSpan.FromHours(12));
            if (receivedTag is not null) response.Headers.TryAddWithoutValidation("ETag", receivedTag);
            if (receivedModified != "missing") response.Content.Headers.TryAddWithoutValidation("Last-Modified",
                receivedModified == "bad" ? "bad" : (receivedModified == "same" ? modified : modified.AddMinutes(1)).ToString("R"));
            return response;
        });
        using var http = new HttpClient(handler);
        using var service = Service(http, clock);
        Task<SchoolMealWeek> Read() => service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken);
        Assert.Equal("published", (await Read()).Status);
        clock.Now = clock.Now.AddHours(6);
        var result = await Read();
        Assert.Equal(accepted ? "published" : "invalid", result.Status);
        if (!accepted)
        {
            Assert.Null(result.ValidUntilUtc);
            Assert.All(result.Days, day => Assert.Empty(day.Dishes));
        }
        clock.Now = clock.Now.AddHours(6);
        await Read();
        Assert.Equal(2, handler.Calls); // 304 Retry-After still extends the 60-second floor.
        if (!accepted)
        {
            clock.Now = clock.Now.AddHours(6); // Retry-After ends; original validators must still be sent.
            Assert.Equal("invalid", (await Read()).Status);
            Assert.Equal(3, handler.Calls);
            clock.Now = modified.AddHours(26); // 25 hours after original storage, 19 after invalid 304.
            Assert.Equal("invalid", (await Read()).Status);
            Assert.Equal(4, handler.Calls);
        }
    }

    [Fact]
    public async Task Valid304UpdatesValidatorsAndPolicyAndPreservesOmittedFields()
    {
        var clock = new Clock();
        var modified = clock.Now.AddHours(-1);
        var updated = modified.AddMinutes(1);
        var attempt = 0;
        using var handler = new Handler(request =>
        {
            attempt++;
            if (attempt == 1)
            {
                var original = Response("max-age=60, s-maxage=60");
                original.Headers.ETag = new("\"menu\"");
                original.Content.Headers.LastModified = modified;
                original.Content.Headers.Expires = clock.Now.AddHours(24);
                return original;
            }
            var response = new HttpResponseMessage(HttpStatusCode.NotModified);
            response.Headers.Date = clock.Now;
            if (attempt == 2)
            {
                Assert.Equal("\"menu\"", Assert.Single(request.Headers.IfNoneMatch).ToString());
                Assert.Equal(modified, request.Headers.IfModifiedSince);
                response.Headers.ETag = EntityTagHeaderValue.Parse("W/\"menu\"");
                response.Content.Headers.LastModified = updated;
                response.Headers.CacheControl = new() { MaxAge = TimeSpan.FromSeconds(120) };
                response.Content.Headers.Expires = clock.Now.AddHours(18);
            }
            else
            {
                Assert.Equal("W/\"menu\"", Assert.Single(request.Headers.IfNoneMatch).ToString());
                Assert.Equal(updated, request.Headers.IfModifiedSince);
                response.Content.Headers.LastModified = updated; // ETag and Expires omitted: retain them.
                if (attempt == 4) response.Headers.CacheControl = new() { Public = true };
            }
            return response;
        });
        using var http = new HttpClient(handler);
        using var service = Service(http, clock);
        Task<SchoolMealWeek> Read() => service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken);
        var original = await Read();
        for (var step = 1; step <= 3; step++)
        {
            clock.Now = clock.Now.AddHours(6);
            var validated = await Read();
            Assert.Equal("published", validated.Status);
            Assert.Equal(original.FetchedAtUtc, validated.FetchedAtUtc);
            Assert.Equal(updated, validated.SourceUpdatedAtUtc);
            // Replacing Cache-Control removes old s-maxage; omitted Cache-Control is retained.
            // At step 3, max-age is removed and the retained absolute Expires becomes effective.
            Assert.Equal(step < 3 ? clock.Now.AddSeconds(120) : clock.Now.AddHours(6), validated.ValidUntilUtc);
        }
        Assert.Equal(4, handler.Calls);
    }

    [Fact]
    public async Task BodyTransferCannotResetFreshnessDeadline()
    {
        var clock = new Clock();
        using var handler = new Handler(_ =>
        {
            var response = Response("max-age=60");
            response.Content = new StreamContent(new AdvancingStream(Encoding.UTF8.GetBytes(SchoolMeals.ExampleRss), clock));
            response.Content.Headers.ContentType = new("application/rss+xml");
            return response;
        });
        using var http = new HttpClient(handler);
        using var service = Service(http, clock);
        var menu = await service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken);
        Assert.Equal("unavailable", menu.Status);
        Assert.All(menu.Days, day => Assert.Empty(day.Dishes));
        await service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken);
        Assert.Equal(1, handler.Calls);
    }

    private sealed class AdvancingStream(byte[] bytes, Clock clock) : MemoryStream(bytes)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            clock.Now = clock.Now.AddMinutes(1);
            return base.ReadAsync(buffer, cancellationToken);
        }
    }

    [Theory]
    [InlineData("max-age=60, max-age=3600")]
    [InlineData("s-maxage=60, s-maxage=3600")]
    [InlineData("max-age=invalid")]
    public async Task MalformedOrDuplicateLifetimesFailClosed(string header)
    {
        var clock = new Clock();
        using var handler = new Handler(_ =>
        {
            var response = Response("max-age=60");
            response.Headers.Remove("Cache-Control");
            response.Headers.TryAddWithoutValidation("Cache-Control", header);
            return response;
        });
        using var http = new HttpClient(handler);
        using var service = Service(http, clock);
        Assert.Equal("cachePolicy", (await service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task SuccessWithRetryAfterDoesNotExtendFreshness()
    {
        var clock = new Clock();
        using var handler = new Handler(_ =>
        {
            var response = Response("max-age=60");
            response.Headers.RetryAfter = new(TimeSpan.FromHours(12));
            return response;
        });
        using var http = new HttpClient(handler);
        using var service = Service(http, clock);
        Task<SchoolMealWeek> Read() => service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken);
        Assert.Equal("published", (await Read()).Status);
        clock.Now = clock.Now.AddHours(6);
        Assert.Equal("unavailable", (await Read()).Status);
        Assert.Equal(1, handler.Calls);
        clock.Now = clock.Now.AddHours(6);
        Assert.Equal("published", (await Read()).Status);
        Assert.Equal(2, handler.Calls);
    }
}
