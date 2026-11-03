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

    private static SchoolMealService Service(HttpClient http, Clock clock) => new(
        new() { SchoolMeals = new() { Enabled = true, RosenfeldtEnabled = true, MusikugglanEnabled = true } },
        new SchoolMealSourceClient(http, clock, NullLogger<SchoolMealSourceClient>.Instance), clock);

    [Theory]
    [InlineData(SchoolMeals.Rosenfeldt, 60, true, 43)]
    [InlineData(SchoolMeals.Musikugglan, 3600, false, 44)]
    public async Task ProductionPoliciesParseAllowedFixturesAndExpireWithoutAnotherSourceCall(string school, int seconds, bool revalidate, int week)
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
        clock.Now = start.AddSeconds(seconds).AddTicks(-1);
        Assert.Equal("published", (await Read()).Status);
        clock.Now = start.AddSeconds(seconds);
        var concurrent = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Read()));
        Assert.All(concurrent, menu =>
        {
            Assert.Equal("unavailable", menu.Status);
            Assert.False(menu.IsStale);
            Assert.Null(menu.ValidUntilUtc);
            Assert.All(menu.Days, day => Assert.Empty(day.Dishes));
        });
        clock.Now = start.AddHours(6).AddTicks(-1);
        await Read();
        Assert.Equal(1, handler.Calls);
        clock.Now = start.AddHours(6);
        Assert.Equal("published", (await Read()).Status);
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
        clock.Now = clock.Now.AddHours(5);
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
