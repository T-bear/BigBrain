using System.Security.Cryptography;
using System.Text;

namespace BigBrain.Api.Media;

internal static class MediaPosterToken
{
    private static readonly byte[] Key = RandomNumberGenerator.GetBytes(32);
    private static readonly string[] AllowedHostSuffixes =
        ["image.tmdb.org", "thetvdb.com", "fanart.tv"];

    public static string? Create(string? sourceUrl)
    {
        if (!TryValidateSource(sourceUrl, out var uri)) return null;
        var payload = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(uri.AbsoluteUri));
        var signature = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
            HMACSHA256.HashData(Key, Encoding.UTF8.GetBytes(payload)));
        return $"/api/v1/modules/media/posters/{payload}.{signature}";
    }

    public static bool TryRead(string token, out Uri uri)
    {
        uri = null!;
        if (token.Length is < 10 or > 4096) return false;
        var separator = token.LastIndexOf('.');
        if (separator <= 0 || separator == token.Length - 1) return false;
        var payload = token[..separator];
        byte[] supplied;
        try
        {
            supplied = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(token[(separator + 1)..]);
        }
        catch (FormatException)
        {
            return false;
        }
        var expected = HMACSHA256.HashData(Key, Encoding.UTF8.GetBytes(payload));
        if (!CryptographicOperations.FixedTimeEquals(expected, supplied)) return false;
        try
        {
            var source = Encoding.UTF8.GetString(
                Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(payload));
            return TryValidateSource(source, out uri);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    internal static bool TryValidateSource(string? sourceUrl, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var parsed)
            || parsed.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(parsed.UserInfo)
            || parsed.IsLoopback
            || string.IsNullOrWhiteSpace(parsed.Host)
            || System.Net.IPAddress.TryParse(parsed.Host, out _)
            || !string.IsNullOrEmpty(parsed.Fragment)
            || !AllowedHostSuffixes.Any(suffix =>
                parsed.Host.Equals(suffix, StringComparison.OrdinalIgnoreCase)
                || parsed.Host.EndsWith($".{suffix}", StringComparison.OrdinalIgnoreCase)))
            return false;
        uri = parsed;
        return true;
    }
}

internal sealed class MediaPosterService(
    IHttpClientFactory httpClientFactory,
    ILogger<MediaPosterService> logger)
{
    private const int MaximumBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };
    private static readonly Action<ILogger, string, string, Exception?> PosterRejected =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(2450, "MediaPosterRejected"),
            "Poster proxy rejected image from {Host}: {Reason}");

    public async Task<(byte[] Bytes, string ContentType)?> GetAsync(
        string token,
        CancellationToken cancellationToken)
    {
        if (!MediaPosterToken.TryRead(token, out var uri))
        {
            PosterRejected(logger, "unknown", "invalid token", null);
            return null;
        }
        try
        {
            using var response = await httpClientFactory.CreateClient("MediaPosters")
                .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (!response.IsSuccessStatusCode
                || contentType is null
                || !AllowedContentTypes.Contains(contentType)
                || response.Content.Headers.ContentLength > MaximumBytes)
            {
                PosterRejected(
                    logger,
                    uri.Host,
                    $"status {(int)response.StatusCode}, content type {contentType ?? "missing"}, length {response.Content.Headers.ContentLength}",
                    null);
                return null;
            }
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length is > 0 and <= MaximumBytes) return (bytes, contentType);
            PosterRejected(
                logger,
                uri.Host,
                $"body length {bytes.Length}",
                null);
            return null;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException
            && !cancellationToken.IsCancellationRequested)
        {
            PosterRejected(logger, uri.Host, "public image host could not be reached", exception);
            return null;
        }
    }
}
