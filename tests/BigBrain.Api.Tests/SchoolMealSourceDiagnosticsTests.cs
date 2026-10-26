using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using BigBrain.Api.MealPlanner;
using Microsoft.Extensions.Logging;

namespace BigBrain.Api.Tests;

public sealed class SchoolMealSourceDiagnosticsTests
{
    private static readonly string[] ResponseHeaderNames = ["Cache-Control", "Age", "Date"];
    private static readonly string[] DiagnosticKeys = ["School", "CheckedAtUtc", "HttpStatus", "CacheControl", "Age", "Date", "Expires", "PolicyReason", "{OriginalFormat}"];
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Handler(Func<HttpResponseMessage> reply) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            return Task.FromResult(reply());
        }
    }

    private sealed class CaptureLogger : ILogger<SchoolMealSourceClient>
    {
        public readonly List<Dictionary<string, object?>> Entries = [];
        public readonly List<string> Messages = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Assert.Equal(LogLevel.Information, level);
            Assert.Equal("SchoolMealSourceResponse", id.Name);
            Assert.Null(exception);
            Entries.Add(((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary(x => x.Key, x => x.Value));
            Messages.Add(formatter(state, exception));
        }
    }

    private static HttpResponseMessage Response(int status = 200) => new((HttpStatusCode)status)
    {
        Content = new StringContent(SchoolMeals.ExampleRss, Encoding.UTF8, "application/rss+xml")
    };

    [Theory]
    [InlineData("no-store", 0, 0, "noStore")]
    [InlineData("no-cache", 0, 0, "noCache")]
    [InlineData("max-age=300", 0, 0, "insufficientFreshness")]
    [InlineData("max-age=21600", 1, 0, "insufficientFreshness")]
    [InlineData(null, 0, 21599, "expiresTooSoon")]
    [InlineData(null, 0, -1, "expiresTooSoon")]
    [InlineData(null, 0, 21600, "cacheAccepted")]
    [InlineData("max-age=21600", 0, -1, "cacheAccepted")]
    [InlineData("max-age=300, no-cache, no-store", 0, 0, "noStore")]
    public async Task CacheReasonsAndBoundariesArePreservedFor200And304(string? policy, int age, int expires, string reason)
    {
        foreach (var status in new[] { 200, 304 })
        {
            var clock = new Clock();
            var log = new CaptureLogger();
            using var handler = new Handler(() =>
            {
                var response = Response(status);
                if (policy is not null) response.Headers.CacheControl = CacheControlHeaderValue.Parse(policy);
                response.Headers.Age = TimeSpan.FromSeconds(age);
                response.Headers.Date = clock.Now;
                if (expires != 0) response.Content.Headers.Expires = clock.Now.AddSeconds(expires);
                return response;
            });
            using var http = new HttpClient(handler);
            var result = await new SchoolMealSourceClient(http, clock, log).GetAsync(SchoolMeals.Rosenfeldt, null, null, TestContext.Current.CancellationToken);
            Assert.Equal(reason == "cacheAccepted" ? status == 200 ? "ok" : "notModified" : "cachePolicy", result.Status);
            if (reason != "cacheAccepted") Assert.False(result.AllowStale);
            var entry = Assert.Single(log.Entries);
            Assert.Equal(reason, entry["PolicyReason"]);
            Assert.Equal(status, entry["HttpStatus"]);
            Assert.Equal(age.ToString(CultureInfo.InvariantCulture), entry["Age"]);
            Assert.Equal(clock.Now.ToString("O"), entry["Date"]);
            Assert.Equal(expires == 0 ? "missing" : clock.Now.AddSeconds(expires).ToString("O"), entry["Expires"]);
            Assert.Equal(1, handler.Calls);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingAndMalformedHeadersAreDistinguishedWithoutChangingExistingPolicy(bool malformed)
    {
        var log = new CaptureLogger();
        using var handler = new Handler(() =>
        {
            var response = Response();
            if (malformed)
            {
                foreach (var header in ResponseHeaderNames)
                    response.Headers.TryAddWithoutValidation(header, "@SENSITIVE_INVALID@");
                response.Content.Headers.TryAddWithoutValidation("Expires", "@SENSITIVE_INVALID@");
                Assert.Equal(DateTimeOffset.MinValue, response.Content.Headers.Expires);
            }
            return response;
        });
        using var http = new HttpClient(handler);
        var result = await new SchoolMealSourceClient(http, new Clock(), log).GetAsync(SchoolMeals.Rosenfeldt, null, null, TestContext.Current.CancellationToken);
        // Malformed Expires is already treated as expired by .NET and the existing policy.
        Assert.Equal(malformed ? "cachePolicy" : "ok", result.Status);
        var entry = Assert.Single(log.Entries);
        foreach (var key in new[] { "CacheControl", "Age", "Date", "Expires" })
            Assert.Equal(malformed ? "invalid" : "missing", entry[key]);
        Assert.DoesNotContain("SENSITIVE", Assert.Single(log.Messages));
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(301)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task HttpErrorsAreNotMisclassifiedAsCachePolicy(int status)
    {
        var log = new CaptureLogger();
        using var handler = new Handler(() =>
        {
            var response = Response(status);
            response.Headers.CacheControl = new() { NoStore = true, NoCache = true };
            return response;
        });
        using var http = new HttpClient(handler);
        var result = await new SchoolMealSourceClient(http, new Clock(), log).GetAsync(SchoolMeals.Rosenfeldt, null, null, TestContext.Current.CancellationToken);
        Assert.Equal("unavailable", result.Status);
        Assert.Null(result.Content);
        Assert.Equal("httpError", Assert.Single(log.Entries)["PolicyReason"]);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData("delta")]
    [InlineData("date")]
    [InlineData("invalid")]
    [InlineData("missing")]
    public async Task RateLimitPreservesRetryAfterAndCooldownDespiteRestrictiveCacheHeaders(string retry)
    {
        var clock = new Clock();
        var log = new CaptureLogger();
        using var handler = new Handler(() =>
        {
            var response = Response(429);
            response.Headers.CacheControl = new() { NoCache = true, NoStore = true };
            if (retry == "delta") response.Headers.RetryAfter = new(TimeSpan.FromHours(12));
            if (retry == "date") response.Headers.RetryAfter = new(clock.Now.AddHours(12));
            if (retry == "invalid") response.Headers.TryAddWithoutValidation("Retry-After", "@SENSITIVE_INVALID@");
            return response;
        });
        using var http = new HttpClient(handler);
        using var service = new SchoolMealService(new() { SchoolMeals = new() { Enabled = true, RosenfeldtEnabled = true } },
            new SchoolMealSourceClient(http, clock, log), clock);
        Assert.Equal("unavailable", (await service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken)).Status);
        Assert.Equal("rateLimited", Assert.Single(log.Entries)["PolicyReason"]);
        var interval = retry is "delta" or "date" ? 12 : 6;
        clock.Now = clock.Now.AddHours(interval).AddSeconds(-1);
        await service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken);
        Assert.Equal(1, handler.Calls);
        Assert.Single(log.Entries);
        clock.Now = clock.Now.AddSeconds(1);
        await service.GetAsync(SchoolMeals.Rosenfeldt, 2026, 43, TestContext.Current.CancellationToken);
        Assert.Equal(2, handler.Calls);
        Assert.Equal(2, log.Entries.Count);
    }

    [Theory]
    [InlineData(SchoolMeals.Rosenfeldt)]
    [InlineData(SchoolMeals.Musikugglan)]
    public async Task DiagnosticsAreBoundedAndExcludeUntrustedStrings(string school)
    {
        var log = new CaptureLogger();
        var secret = "SENSITIVE_CANARY_" + new string('x', 10000);
        using var handler = new Handler(() =>
        {
            var response = Response();
            response.ReasonPhrase = secret;
            response.Headers.CacheControl = CacheControlHeaderValue.Parse($"no-cache=\"{secret}\", private=\"{secret}\", x-secret=\"{secret}\", max-age=300");
            response.Headers.TryAddWithoutValidation("Set-Cookie", secret);
            response.Headers.TryAddWithoutValidation("X-Api-Key", secret);
            response.Headers.Location = new Uri("https://example.invalid/?token=" + secret);
            response.Content = new StringContent(secret);
            return response;
        });
        using var http = new HttpClient(handler);
        var result = await new SchoolMealSourceClient(http, new Clock(), log).GetAsync(school, null, null, TestContext.Current.CancellationToken);
        Assert.Equal("cachePolicy", result.Status);
        var entry = Assert.Single(log.Entries);
        Assert.Equal(school, entry["School"]);
        Assert.Contains("no-cache=True", (string)entry["CacheControl"]!);
        Assert.Contains("extensions=True", (string)entry["CacheControl"]!);
        Assert.Equal(DiagnosticKeys, entry.Keys);
        foreach (var value in entry.Values)
        {
            var text = value?.ToString() ?? "";
            Assert.True(text.Length <= 512);
            Assert.DoesNotContain("SENSITIVE", text);
            Assert.DoesNotContain('\r', text);
            Assert.DoesNotContain('\n', text);
        }
        Assert.True(Assert.Single(log.Messages).Length < 1024);
        Assert.Equal(1, handler.Calls);
    }
}
