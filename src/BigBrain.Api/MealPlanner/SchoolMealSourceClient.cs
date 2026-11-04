using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace BigBrain.Api.MealPlanner;

public sealed record SchoolMealCachePolicy(TimeSpan? MaxAge, TimeSpan? SharedMaxAge, DateTimeOffset? Expires);

public sealed record SchoolMealSourceResult(string Status, string? Content = null, string? ETag = null,
    DateTimeOffset? Modified = null, DateTimeOffset? RetryAt = null, bool HeadersVerified = false,
    DateTimeOffset? FreshUntil = null, SchoolMealCachePolicy? CachePolicy = null);

// Fixed sources only. No credentials, user URLs, redirects, cookies or executable content.
public sealed partial class SchoolMealSourceClient(HttpClient client, TimeProvider clock, ILogger<SchoolMealSourceClient> logger)
{
    public const int MaximumBytes = 512 * 1024;
    public static SocketsHttpHandler CreateHandler() => new()
    {
        PooledConnectionLifetime = TimeSpan.FromHours(6),
        AllowAutoRedirect = false,
        UseCookies = false,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    };

    public async Task<SchoolMealSourceResult> GetAsync(string school, string? etag, DateTimeOffset? modified, CancellationToken token,
        SchoolMealCachePolicy? previousPolicy = null, bool acceptApplicationData = false)
    {
        var url = school switch
        {
            SchoolMeals.Rosenfeldt => $"https://www.skolmat.info/api/public/matsedlar/{SchoolMeals.FacilityId}/rss?period=current-week",
            SchoolMeals.Musikugglan => "https://www.tantgron.nu/matsedel/",
            _ => throw new ArgumentException("Unknown school.", nameof(school))
        };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5), clock);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, timeout.Token);
        var headersVerified = false;
        DateTimeOffset? retryAt = null;
        DateTimeOffset? freshUntil = null;
        SchoolMealCachePolicy? policy = null;
        SchoolMealSourceResult Result(string status, string? content = null, string? responseEtag = null,
            DateTimeOffset? responseModified = null) =>
            new(status, content, responseEtag, responseModified, retryAt, headersVerified, freshUntil, policy);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (etag is not null) request.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Parse(etag));
            if (modified is not null) request.Headers.IfModifiedSince = modified;
            var requestedAt = clock.GetUtcNow();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linked.Token);
            var retry = response.Headers.RetryAfter;
            headersVerified = !response.Headers.Contains("Retry-After") || retry is not null;
            retryAt = retry?.Date;
            if (retry?.Delta is { } delta && delta > TimeSpan.Zero)
            {
                var receivedAt = clock.GetUtcNow();
                retryAt = delta < DateTimeOffset.MaxValue - receivedAt ? receivedAt + delta : DateTimeOffset.MaxValue;
            }
            var duplicateFreshness = HasDuplicateFreshness(response);
            var cache = response.Headers.CacheControl;
            var now = clock.GetUtcNow();
            // Source-call cooldown is independent of HTTP freshness. We never serve stale.
            // A 304 may inherit omitted policy fields, but must validate an existing response.
            var notModified = response.StatusCode == HttpStatusCode.NotModified;
            var inherited = notModified ? previousPolicy : null;
            var hasCacheControl = response.Headers.Contains("Cache-Control");
            policy = new(hasCacheControl ? cache?.MaxAge : inherited?.MaxAge,
                hasCacheControl ? cache?.SharedMaxAge : inherited?.SharedMaxAge,
                response.Content.Headers.Contains("Expires") ? response.Content.Headers.Expires : inherited?.Expires);
            var reason = response.StatusCode == HttpStatusCode.TooManyRequests ? "rateLimited"
                : response.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.NotModified) ? "httpError"
                : cache?.NoStore == true ? "noStore"
                : cache?.NoCache == true ? "noCache"
                : cache?.Private == true ? "private"
                : hasCacheControl && (cache is null || duplicateFreshness)
                    || response.Headers.Contains("Age") && response.Headers.Age is null
                    || response.Headers.Contains("Date") && response.Headers.Date is null ? "invalidCacheHeaders"
                : response.Headers.Vary.Contains("*") ? "varyStar"
                : !hasCacheControl && response.Headers.Pragma.Any(x => x.Name.Equals("no-cache", StringComparison.OrdinalIgnoreCase)) ? "noCache"
                : "cacheAccepted";
            if (reason == "cacheAccepted")
            {
                var date = response.Headers.Date ?? now;
                var lifetime = policy.SharedMaxAge ?? policy.MaxAge ?? (policy.Expires - date) ?? TimeSpan.Zero;
                // RFC 9111 age: max(apparent age, Age + response delay). Body transfer time
                // is accounted for by checking this absolute deadline again before serving.
                var apparentAge = now > date ? now - date : TimeSpan.Zero;
                var delay = now > requestedAt ? now - requestedAt : TimeSpan.Zero;
                var age = response.Headers.Age ?? TimeSpan.Zero;
                var correctedAge = age > TimeSpan.MaxValue - delay ? TimeSpan.MaxValue : age + delay;
                var currentAge = apparentAge > correctedAge ? apparentAge : correctedAge;
                var remaining = lifetime > currentAge ? lifetime - currentAge : TimeSpan.Zero;
                // Keep the existing 24-hour local retention cap even if the source permits more.
                remaining = remaining < TimeSpan.FromHours(24) ? remaining : TimeSpan.FromHours(24);
                freshUntil = remaining < DateTimeOffset.MaxValue - now ? now + remaining : DateTimeOffset.MaxValue;
                if (remaining == TimeSpan.Zero) reason = "noFreshness";
            }
            LogResponse(school, response, cache, now, reason);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return Result("unavailable");
            }
            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NotModified)
                return Result("unavailable");
            if (notModified && (previousPolicy is null || !MatchesStoredValidator(response, etag, modified)))
                return Result("invalid");
            // A complete 200 body may supply separately permitted
            // application data even when HTTP cache reuse is forbidden
            // by Vary:* or exhausted freshness.
            if (reason != "cacheAccepted" &&
                !(acceptApplicationData &&
                  response.StatusCode == HttpStatusCode.OK &&
                  (reason is "varyStar" or "noFreshness")))
                return Result("cachePolicy");
            if (notModified) return Result("notModified", responseEtag: response.Headers.ETag?.ToString() ?? etag,
                responseModified: response.Content.Headers.LastModified ?? modified);
            var type = response.Content.Headers.ContentType?.MediaType;
            var supported = school == SchoolMeals.Rosenfeldt
                ? type is "application/rss+xml" or "application/xml" or "text/xml"
                : type is "text/html";
            if (!supported || response.Content.Headers.ContentLength > MaximumBytes) return Result("invalid");
            using var stream = await response.Content.ReadAsStreamAsync(linked.Token);
            using var output = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(buffer, linked.Token)) != 0)
            {
                if (output.Length + read > MaximumBytes) return Result("invalid");
                output.Write(buffer, 0, read);
            }
            var charset = response.Content.Headers.ContentType?.CharSet?.Trim('"');
            if (charset is not null && !charset.Equals("utf-8", StringComparison.OrdinalIgnoreCase)) return Result("invalid");
            var text = new UTF8Encoding(false, true).GetString(output.ToArray()).TrimStart('\uFEFF');
            return Result("ok", text, response.Headers.ETag?.ToString(), response.Content.Headers.LastModified);
        }
        catch (Exception error) when (error is HttpRequestException or IOException or OperationCanceledException or DecoderFallbackException or FormatException)
        {
            return Result("unavailable");
        }
    }

    private static bool MatchesStoredValidator(HttpResponseMessage response, string? etag, DateTimeOffset? modified)
    {
        var receivedTag = response.Headers.ETag;
        var receivedModified = response.Content.Headers.LastModified;
        if (response.Headers.Contains("ETag") && (receivedTag is null || receivedTag.Tag == "*")
            || response.Content.Headers.Contains("Last-Modified") && receivedModified is null) return false;
        var storedTag = etag is null ? null : EntityTagHeaderValue.Parse(etag);
        // RFC 9111 §4.3.4: a new strong ETag cannot identify a stored weak tag or
        // a Last-Modified-only representation. Weak comparison ignores only W/.
        if (receivedTag is { IsWeak: false })
            return storedTag is { IsWeak: false } && storedTag.Tag == receivedTag.Tag;
        if (receivedTag is not null && storedTag is not null)
            return storedTag.Tag == receivedTag.Tag;
        // We do not infer strong Last-Modified validators from unverified source clocks.
        // With no conflicting ETag, an equal Last-Modified identifies our sole stored menu.
        // A validator-free 304 cannot identify a menu whose validator we sent.
        return modified is not null && receivedModified == modified;
    }

    private static bool HasDuplicateFreshness(HttpResponseMessage response)
    {
        // Duplicate lifetime directives are ambiguous even when the typed parser keeps one.
        // Conservatively reject ambiguous fields rather than choosing the longer lifetime.
        if (!response.Headers.NonValidated.TryGetValues("Cache-Control", out var values)) return false;
        var names = values.SelectMany(value => value.Split(','))
            .Select(value => value.Split('=')[0].Trim()).ToArray();
        return names.Count(name => name.Equals("max-age", StringComparison.OrdinalIgnoreCase)) > 1
            || names.Count(name => name.Equals("s-maxage", StringComparison.OrdinalIgnoreCase)) > 1;
    }

    private void LogResponse(string school, HttpResponseMessage response, CacheControlHeaderValue? cache,
        DateTimeOffset now, string reason)
    {
        if (!logger.IsEnabled(LogLevel.Information)) return;
        // Never format raw headers, extension names/values, field-name lists, URI, body or exceptions.
        // Fixed vocabulary + typed numbers/dates bounds every value independently of upstream input.
        var policy = cache is null ? HeaderState(response.Headers, "Cache-Control", null)
            : FormattableString.Invariant($"no-store={cache.NoStore};no-cache={cache.NoCache};must-revalidate={cache.MustRevalidate};proxy-revalidate={cache.ProxyRevalidate};private={cache.Private};public={cache.Public};max-age={Seconds(cache.MaxAge)};s-maxage={Seconds(cache.SharedMaxAge)};extensions={cache.Extensions.Count > 0}");
        var checkedAt = now.ToString("O", CultureInfo.InvariantCulture);
        var age = HeaderState(response.Headers, "Age", Seconds(response.Headers.Age));
        var date = HeaderState(response.Headers, "Date", response.Headers.Date?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        // .NET maps malformed Expires (including the common "0") to MinValue/expired.
        // Preserve that policy behavior, but identify the sentinel as invalid in diagnostics.
        var expiresValue = response.Content.Headers.Expires;
        var expires = HeaderState(response.Content.Headers, "Expires", expiresValue == DateTimeOffset.MinValue
            ? null : expiresValue?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        LogHeaders(logger, school, checkedAt, (int)response.StatusCode, policy, age, date, expires, reason);
    }

    [LoggerMessage(EventId = 1, EventName = "SchoolMealSourceResponse", Level = LogLevel.Information, SkipEnabledCheck = true,
        Message = "School meal source {School} at {CheckedAtUtc}: HTTP {HttpStatus}; Cache-Control={CacheControl}; Age={Age}; Date={Date}; Expires={Expires}; decision={PolicyReason}")]
    private static partial void LogHeaders(ILogger logger, string school, string checkedAtUtc, int httpStatus,
        string cacheControl, string age, string date, string expires, string policyReason);

    private static string? Seconds(TimeSpan? value) => value?.TotalSeconds.ToString("0.#######", CultureInfo.InvariantCulture);

    private static string HeaderState(HttpHeaders headers, string name, string? parsed) =>
        parsed ?? (headers.Contains(name) ? "invalid" : "missing");
}
