using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace BigBrain.Api.Media;

public sealed record AudiobookPlaybackAvailability(string State, string? Message, bool SeparateIdentity, bool HasProgress);
public sealed record AudiobookPlaybackTrack(int Index, double StartOffset, double Duration, string? Title, string MimeType, string StreamUrl);
public sealed record AudiobookPlaybackSession(string Id, string ItemId, double CurrentTime, double Duration, IReadOnlyList<AudiobookPlaybackTrack> Tracks, DateTimeOffset ExpiresAtUtc);
public sealed record AudiobookPlaybackProgress(double CurrentTime, double Duration, double TimeListened);

public sealed class AudiobookPlaybackException(string code, string safeMessage, int statusCode) : Exception(safeMessage)
{
    public string Code { get; } = code;
    public string SafeMessage { get; } = safeMessage;
    public int StatusCode { get; } = statusCode;
}

public sealed class AudiobookPlaybackService(IHttpClientFactory clients, MediaOptions options, TimeProvider clock)
{
    private const long MaximumRangeBytes = 8 * 1024 * 1024;
    private readonly ConcurrentDictionary<string, SessionState> sessions = new(StringComparer.Ordinal);
    private HttpClient Http => clients.CreateClient("AudiobookPlayback");
    public bool Configured => !string.IsNullOrWhiteSpace(options.Audiobookshelf.PlaybackApiKey);

    public async Task<AudiobookItem?> GetContinueListeningAsync(CancellationToken token)
    {
        if (!Configured || string.IsNullOrWhiteSpace(options.Audiobookshelf.LibraryId)) return null;
        using var response = await SendAsync(HttpMethod.Get, "api/me/items-in-progress?limit=25", null, token);
        EnsureSuccess(response);
        using var document = await ParseAsync(response, token);
        if (!document.RootElement.TryGetProperty("libraryItems", out var results) || results.ValueKind != JsonValueKind.Array) return null;
        using var progressResponse = await SendAsync(HttpMethod.Get, "api/me/progress", null, token);
        EnsureSuccess(progressResponse);
        using var progressDocument = await ParseAsync(progressResponse, token);
        var progressRows = progressDocument.RootElement.TryGetProperty("mediaProgress", out var rows) && rows.ValueKind == JsonValueKind.Array ? rows : default;
        foreach (var value in results.EnumerateArray())
        {
            var id = Text(value, "id");
            if (string.IsNullOrWhiteSpace(id)) continue;
            var row = progressRows.ValueKind == JsonValueKind.Array ? progressRows.EnumerateArray().FirstOrDefault(x => Text(x, "libraryItemId") == id) : default;
            var item = MapProgressItem(value, row.ValueKind == JsonValueKind.Object ? Number(row, "progress") * 100 : null);
            if (item?.ProgressPercent is > 0 and < 100) return item;
        }
        return null;
    }

    public async Task<AudiobookPlaybackAvailability> VerifyAsync(CancellationToken token)
    {
        if (!Configured) return new("notConfigured", "Separat playback-identitet är inte konfigurerad.", false, false);
        using var me = await SendAsync(HttpMethod.Get, "api/me", null, token);
        if (!me.IsSuccessStatusCode) return Unavailable(me.StatusCode);
        using var meJson = await ParseAsync(me, token);
        var active = !Bool(meJson.RootElement, "isActive", true).Equals(false);
        var root = Bool(meJson.RootElement, "isRoot", false) || Bool(meJson.RootElement, "isAdmin", false);
        if (!active || root) return new("rejected", "Playback-identiteten måste vara en aktiv begränsad användare.", false, false);

        using var progress = await SendAsync(HttpMethod.Get, "api/me/progress?limit=1", null, token);
        if (!progress.IsSuccessStatusCode) return Unavailable(progress.StatusCode);
        using var progressJson = await ParseAsync(progress, token);
        var hasProgress = ArrayCount(progressJson.RootElement, "mediaProgress") > 0 || ArrayCount(progressJson.RootElement, "results") > 0
            || progressJson.RootElement.ValueKind == JsonValueKind.Array && progressJson.RootElement.GetArrayLength() > 0;
        var separate = !CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(options.Audiobookshelf.PlaybackApiKey!)),
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(options.Audiobookshelf.ApiKey ?? string.Empty)));
        return separate
            ? new("configuredHealthy", null, true, hasProgress)
            : new("rejected", "Playback-identiteten får inte vara integrationsidentiteten.", false, hasProgress);
    }

    public async Task<AudiobookPlaybackSession> StartAsync(string itemId, CancellationToken token)
    {
        SafeId(itemId, "itemId");
        var verified = await VerifyAsync(token);
        if (verified.State != "configuredHealthy") throw new AudiobookPlaybackException("playbackIdentityUnavailable", verified.Message ?? "Playback-identiteten kunde inte verifieras.", StatusCodes.Status503ServiceUnavailable);
        var payload = new
        {
            forceDirectPlay = true,
            mediaPlayer = "html5",
            supportedMimeTypes = new[] { "audio/mpeg", "audio/mp4", "audio/x-m4b", "audio/aac", "audio/ogg", "audio/flac", "audio/wav", "audio/webm" },
            deviceInfo = new { clientName = "BigBrain", clientVersion = "1.0", deviceName = "BigBrain Web", deviceId = "bigbrain-web" }
        };
        using var response = await SendAsync(HttpMethod.Post, $"api/items/{Uri.EscapeDataString(itemId)}/play", JsonContent.Create(payload), token);
        if (response.StatusCode == HttpStatusCode.NotFound) throw new AudiobookPlaybackException("itemNotPlayable", "Ljudboken kunde inte spelas.", StatusCodes.Status404NotFound);
        EnsureSuccess(response);
        using var document = await ParseAsync(response, token);
        var root = document.RootElement;
        var upstreamId = Text(root, "id") ?? throw InvalidUpstream();
        var upstreamItemId = Text(root, "libraryItemId") ?? throw InvalidUpstream();
        if (!string.Equals(itemId, upstreamItemId, StringComparison.Ordinal)) throw InvalidUpstream();
        var duration = Number(root, "duration") ?? 0;
        var current = Number(root, "currentTime") ?? Number(root, "startTime") ?? 0;
        var opaqueId = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var expires = clock.GetUtcNow().AddMinutes(options.Audiobookshelf.PlaybackSessionLifetimeMinutes);
        var tracks = new List<AudiobookPlaybackTrack>();
        if (root.TryGetProperty("audioTracks", out var values) && values.ValueKind == JsonValueKind.Array)
        {
            foreach (var value in values.EnumerateArray().Take(100))
            {
                var index = Int(value, "index") ?? tracks.Count + 1;
                tracks.Add(new(index, Number(value, "startOffset") ?? 0, Number(value, "duration") ?? 0,
                    Text(value, "title"), Text(value, "mimeType") ?? "application/octet-stream",
                    $"/api/v1/modules/media/audiobooks/playback/sessions/{opaqueId}/tracks/{index}"));
            }
        }
        if (tracks.Count == 0) throw InvalidUpstream();
        sessions[opaqueId] = new(upstreamId, itemId, tracks.Select(x => x.Index).ToHashSet(), expires);
        Prune();
        return new(opaqueId, itemId, current, duration, tracks, expires);
    }

    public async Task SyncAsync(string id, AudiobookPlaybackProgress progress, bool close, CancellationToken token)
    {
        var state = Get(id);
        if (!double.IsFinite(progress.CurrentTime) || !double.IsFinite(progress.Duration) || !double.IsFinite(progress.TimeListened)
            || progress.CurrentTime < 0 || progress.Duration <= 0 || progress.CurrentTime > progress.Duration + 5 || progress.TimeListened is < 0 or > 300)
            throw new AudiobookPlaybackException("invalidProgress", "Playback-positionen är ogiltig.", StatusCodes.Status400BadRequest);
        using var response = await SendAsync(HttpMethod.Post, $"api/session/{Uri.EscapeDataString(state.UpstreamId)}/{(close ? "close" : "sync")}",
            JsonContent.Create(new { currentTime = progress.CurrentTime, duration = progress.Duration, timeListened = progress.TimeListened }), token);
        EnsureSuccess(response);
        if (close) sessions.TryRemove(id, out _);
    }

    public async Task StreamAsync(string id, int trackIndex, HttpContext context, CancellationToken token)
    {
        var state = Get(id);
        if (!state.TrackIndexes.Contains(trackIndex)) throw new AudiobookPlaybackException("trackMismatch", "Spåret tillhör inte playback-sessionen.", StatusCodes.Status404NotFound);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"public/session/{Uri.EscapeDataString(state.UpstreamId)}/track/{trackIndex}");
        if (context.Request.Headers.Range.Count > 0)
        {
            if (!TryBoundRange(context.Request.Headers.Range.ToString(), out var range))
            {
                context.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
                return;
            }
            request.Headers.Range = range;
        }
        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        if (response.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.PartialContent or HttpStatusCode.RequestedRangeNotSatisfiable))
            throw new AudiobookPlaybackException("upstreamStreamUnavailable", "Ljudströmmen kunde inte hämtas.", StatusCodes.Status502BadGateway);
        context.Response.StatusCode = (int)response.StatusCode;
        Copy(response.Content.Headers.ContentType, value => context.Response.ContentType = value.ToString());
        Copy(response.Content.Headers.ContentLength, value => context.Response.ContentLength = value);
        Copy(response.Content.Headers.ContentRange, value => context.Response.Headers.ContentRange = value.ToString());
        Copy(response.Headers.AcceptRanges.FirstOrDefault(), value => context.Response.Headers.AcceptRanges = value);
        if (response.StatusCode != HttpStatusCode.RequestedRangeNotSatisfiable)
            await response.Content.CopyToAsync(context.Response.Body, token);
    }

    private SessionState Get(string id)
    {
        SafeId(id, "sessionId");
        if (!sessions.TryGetValue(id, out var state) || state.ExpiresAtUtc <= clock.GetUtcNow())
        {
            sessions.TryRemove(id, out _);
            throw new AudiobookPlaybackException("sessionNotFound", "Playback-sessionen finns inte eller har löpt ut.", StatusCodes.Status404NotFound);
        }
        return state;
    }
    private void Prune() { foreach (var item in sessions) if (item.Value.ExpiresAtUtc <= clock.GetUtcNow()) sessions.TryRemove(item.Key, out _); }
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, HttpContent? content, CancellationToken token)
    {
        var request = new HttpRequestMessage(method, uri) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Audiobookshelf.PlaybackApiKey);
        try { return await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch { throw new AudiobookPlaybackException("upstreamUnavailable", "Audiobookshelf kunde inte nås.", StatusCodes.Status503ServiceUnavailable); }
    }
    private static async Task<JsonDocument> ParseAsync(HttpResponseMessage response, CancellationToken token)
    {
        if (response.Content.Headers.ContentLength is > 2_000_000) throw InvalidUpstream();
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        return await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { MaxDepth = 32 }, token);
    }
    private static void EnsureSuccess(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) throw new AudiobookPlaybackException("playbackAuthenticationFailure", "Audiobookshelf avvisade playback-identiteten.", StatusCodes.Status503ServiceUnavailable);
        if (!response.IsSuccessStatusCode) throw new AudiobookPlaybackException("upstreamPlaybackFailure", "Audiobookshelf kunde inte slutföra playback-anropet.", StatusCodes.Status502BadGateway);
    }
    private static AudiobookPlaybackAvailability Unavailable(HttpStatusCode status) => status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
        ? new("rejected", "Audiobookshelf avvisade playback-identiteten.", false, false)
        : new("configuredUnavailable", "Playback-identiteten kunde inte verifieras.", false, false);
    private static AudiobookPlaybackException InvalidUpstream() => new("invalidUpstreamResponse", "Audiobookshelf returnerade ett ogiltigt playback-svar.", StatusCodes.Status502BadGateway);
    private static bool TryBoundRange(string value, out RangeHeaderValue range)
    {
        range = null!;
        if (!RangeHeaderValue.TryParse(value, out var parsed) || parsed.Unit != "bytes" || parsed.Ranges.Count != 1) return false;
        var item = parsed.Ranges.Single();
        if (!item.From.HasValue)
        {
            if (!item.To.HasValue || item.To <= 0) return false;
            range = new RangeHeaderValue(null, Math.Min(item.To.Value, MaximumRangeBytes));
            return true;
        }
        if (item.From < 0 || item.To < item.From) return false;
        var end = item.To.HasValue ? Math.Min(item.To.Value, item.From.Value + MaximumRangeBytes - 1) : item.From.Value + MaximumRangeBytes - 1;
        range = new RangeHeaderValue(item.From, end);
        return true;
    }
    private static void Copy<T>(T? value, Action<T> set) { if (value is not null) set(value); }
    private static void SafeId(string value, string field) { if (value.Length is < 1 or > 128 || value.Any(c => !char.IsLetterOrDigit(c) && c is not '-' and not '_')) throw new AudiobookPlaybackException("invalid" + field, "Playback-identifieraren är ogiltig.", StatusCodes.Status400BadRequest); }
    private static string? Text(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static double? Number(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.TryGetDouble(out var n) ? n : null;
    private static int? Int(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.TryGetInt32(out var n) ? n : null;
    private static bool Bool(JsonElement e, string name, bool fallback) => e.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : fallback;
    private static int ArrayCount(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array ? v.GetArrayLength() : 0;
    private static AudiobookItem? MapProgressItem(JsonElement item, double? progress)
    {
        var id = Text(item, "id");
        if (string.IsNullOrWhiteSpace(id)) return null;
        var media = item.TryGetProperty("media", out var mediaValue) ? mediaValue : item;
        var metadata = media.TryGetProperty("metadata", out var metadataValue) ? metadataValue : media;
        var language = AudiobookLanguages.Normalize(Text(metadata, "language"));
        return new(id, Text(metadata, "title") ?? "Namnlös ljudbok", Text(metadata, "authorName"), Text(metadata, "seriesName"), Text(metadata, "narratorName"),
            language, AudiobookLanguages.DisplayName(language), Number(media, "duration"), progress, null,
            $"/api/v1/modules/media/audiobooks/{Uri.EscapeDataString(id)}/cover", Text(metadata, "publishedYear"), null, null);
    }
    private sealed record SessionState(string UpstreamId, string ItemId, HashSet<int> TrackIndexes, DateTimeOffset ExpiresAtUtc);
}
