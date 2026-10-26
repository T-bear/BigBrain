using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace BigBrain.Api.MealPlanner;

public sealed record SchoolMealSourceResult(string Status, string? Content = null, string? ETag = null,
    DateTimeOffset? Modified = null, DateTimeOffset? RetryAt = null, bool AllowStale = true);

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

    public async Task<SchoolMealSourceResult> GetAsync(string school, string? etag, DateTimeOffset? modified, CancellationToken token)
    {
        var url = school switch
        {
            SchoolMeals.Rosenfeldt => $"https://www.skolmat.info/api/public/matsedlar/{SchoolMeals.FacilityId}/rss?period=current-week",
            SchoolMeals.Musikugglan => "https://www.tantgron.nu/matsedel/",
            _ => throw new ArgumentException("Unknown school.", nameof(school))
        };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5), clock);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, timeout.Token);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (etag is not null) request.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Parse(etag));
            if (modified is not null) request.Headers.IfModifiedSince = modified;
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linked.Token);
            var cache = response.Headers.CacheControl;
            var age = response.Headers.Age ?? TimeSpan.Zero;
            var now = clock.GetUtcNow();
            // HTTP errors are not menu responses; preserve their classification and Retry-After.
            // For 200/304, keep the existing upstream cache restrictions exactly as before.
            var reason = response.StatusCode == HttpStatusCode.TooManyRequests ? "rateLimited"
                : response.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.NotModified) ? "httpError"
                : cache?.NoStore == true ? "noStore"
                : cache?.NoCache == true ? "noCache"
                : cache?.MaxAge is { } maxAge && maxAge - age < TimeSpan.FromHours(6) ? "insufficientFreshness"
                : cache?.MaxAge is null && response.Content.Headers.Expires is { } expires && expires - now < TimeSpan.FromHours(6) ? "expiresTooSoon"
                : "cacheAccepted";
            LogResponse(school, response, cache, now, reason);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retry = response.Headers.RetryAfter;
                var retryAt = retry?.Date;
                if (retry?.Delta is { } delta && delta > TimeSpan.Zero)
                    retryAt = delta < DateTimeOffset.MaxValue - now ? now + delta : DateTimeOffset.MaxValue;
                return new("unavailable", RetryAt: retryAt);
            }
            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NotModified)
                return new("unavailable");
            if (reason != "cacheAccepted") return new("cachePolicy", AllowStale: false);
            if (response.StatusCode == HttpStatusCode.NotModified)
                return new("notModified", AllowStale: cache?.MustRevalidate != true);
            var type = response.Content.Headers.ContentType?.MediaType;
            var supported = school == SchoolMeals.Rosenfeldt
                ? type is "application/rss+xml" or "application/xml" or "text/xml"
                : type is "text/html";
            if (!supported || response.Content.Headers.ContentLength > MaximumBytes) return new("invalid");
            using var stream = await response.Content.ReadAsStreamAsync(linked.Token);
            using var output = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(buffer, linked.Token)) != 0)
            {
                if (output.Length + read > MaximumBytes) return new("invalid");
                output.Write(buffer, 0, read);
            }
            var charset = response.Content.Headers.ContentType?.CharSet?.Trim('"');
            if (charset is not null && !charset.Equals("utf-8", StringComparison.OrdinalIgnoreCase)) return new("invalid");
            var text = new UTF8Encoding(false, true).GetString(output.ToArray()).TrimStart('\uFEFF');
            return new("ok", text, response.Headers.ETag?.ToString(), response.Content.Headers.LastModified,
                AllowStale: cache?.MustRevalidate != true);
        }
        catch (Exception error) when (error is HttpRequestException or IOException or OperationCanceledException or DecoderFallbackException)
        {
            return new("unavailable");
        }
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
