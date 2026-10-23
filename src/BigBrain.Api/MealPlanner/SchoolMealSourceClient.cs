using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace BigBrain.Api.MealPlanner;

public sealed record SchoolMealSourceResult(string Status, string? Content = null, string? ETag = null,
    DateTimeOffset? Modified = null, DateTimeOffset? RetryAt = null, bool AllowStale = true);

// Fixed sources only. No credentials, user URLs, redirects, cookies or executable content.
public sealed class SchoolMealSourceClient(HttpClient client, TimeProvider clock)
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
            // Do not silently violate a stricter upstream policy or compensate with more requests.
            if (cache?.NoStore == true || cache?.NoCache == true ||
                cache?.MaxAge is { } maxAge && maxAge - age < TimeSpan.FromHours(6) ||
                cache?.MaxAge is null && response.Content.Headers.Expires is { } expires && expires - clock.GetUtcNow() < TimeSpan.FromHours(6))
                return new("cachePolicy", AllowStale: false);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retry = response.Headers.RetryAfter;
                var now = clock.GetUtcNow();
                var retryAt = retry?.Date;
                if (retry?.Delta is { } delta && delta > TimeSpan.Zero)
                    retryAt = delta < DateTimeOffset.MaxValue - now ? now + delta : DateTimeOffset.MaxValue;
                return new("unavailable", RetryAt: retryAt);
            }
            if (response.StatusCode == HttpStatusCode.NotModified)
                return new("notModified", AllowStale: cache?.MustRevalidate != true);
            if (response.StatusCode != HttpStatusCode.OK) return new("unavailable");
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
}
