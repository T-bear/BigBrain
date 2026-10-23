using System.Net;
using System.Net.Http.Headers;
using System.Text;
using BigBrain.Api.MealPlanner;

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
        Content = new StringContent(text ?? SchoolMeals.ExampleRss, Encoding.UTF8, "application/rss+xml")
    };
    private static MealPlannerOptions Enabled => new()
    {
        SchoolMeals = new() { Enabled = true, RosenfeldtEnabled = true, MusikugglanEnabled = true }
    };
    private static SchoolMealService Service(Handler handler, Clock clock, MealPlannerOptions? options = null) =>
        new(options ?? Enabled, new SchoolMealSourceClient(new HttpClient(handler), clock), clock);
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
    public async Task FailedRefreshIsStaleOnlyWithinSameWeekAndTwentyFourHours()
    {
        var handler = new Handler();
        var clock = new Clock();
        using var service = Service(handler, clock);
        var first = await Get(service);
        handler.Reply = (_, _) => throw new HttpRequestException("not exposed");
        clock.Now = clock.Now.AddHours(6);
        var stale = await Get(service);
        Assert.True(stale.IsStale);
        Assert.Equal(first.FetchedAtUtc, stale.FetchedAtUtc);
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
    public async Task InvalidRefreshPreservesStaleButEmptyMenuReplacesIt()
    {
        var handler = new Handler();
        var clock = new Clock();
        using var service = Service(handler, clock);
        await Get(service);
        handler.Reply = (_, _) => Task.FromResult(Rss("<rss>"));
        clock.Now = clock.Now.AddHours(6);
        var invalid = await Get(service);
        Assert.True(invalid.IsStale);
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
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotModified));
        };
        clock.Now = clock.Now.AddHours(6);
        var unchanged = await Get(service);
        Assert.Equal("published", unchanged.Status);
        Assert.Equal(first.FetchedAtUtc, unchanged.FetchedAtUtc);
        Assert.Equal(clock.Now, unchanged.LastCheckedAtUtc);
    }

    [Theory]
    [InlineData("no-store")]
    [InlineData("no-cache")]
    [InlineData("max-age=300")]
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
            response.Headers.CacheControl = new() { MustRevalidate = true };
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
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(MusikugglanSchoolMeals.ExampleHtml, Encoding.UTF8, "text/html") });
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
    public async Task RestartDropsCacheAndSourcesRemainIndependent()
    {
        var handler = new Handler();
        var clock = new Clock();
        using (var first = Service(handler, clock)) await Get(first);
        using var restarted = Service(handler, clock);
        Assert.Equal("published", (await Get(restarted)).Status);
        Assert.Equal(2, handler.Calls);
        Assert.Equal("invalid", (await restarted.GetAsync(SchoolMeals.Musikugglan, 2026, 43, TestContext.Current.CancellationToken)).Status);
        Assert.Equal("published", (await Get(restarted)).Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OversizedBodyIsRejectedWithOrWithoutContentLength(bool knownLength)
    {
        var bytes = new byte[SchoolMealSourceClient.MaximumBytes + 1];
        var content = knownLength ? (HttpContent)new ByteArrayContent(bytes) : new StreamContent(new NonSeekableStream(bytes));
        content.Headers.ContentType = new("application/rss+xml");
        var handler = new Handler { Reply = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content }) };
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
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
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
            response.Content.Headers.Expires = clock.Now.AddMinutes(-1);
            return Task.FromResult(response);
        }
        };
        using var service = Service(handler, clock);
        Assert.Equal("cachePolicy", (await Get(service)).Status);
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
