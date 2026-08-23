using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BigBrain.Api.Media;

public static class AudiobookLanguages
{
    public const string Unknown = "und";
    public static string Normalize(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "sv" or "swe" or "svenska" or "swedish" => "sv",
        "en" or "eng" or "engelska" or "english" => "en",
        "de" or "deu" or "ger" or "tyska" or "german" => "de",
        _ => Unknown
    };
    public static string DisplayName(string code) => Normalize(code) switch
    {
        "sv" => "Svenska", "en" => "Engelska", "de" => "Tyska", _ => "Språk okänt"
    };
}

public static class AudiobookIntegrationStates
{
    public const string ConfiguredHealthy = "configuredHealthy";
    public const string ConfiguredUnavailable = "configuredUnavailable";
    public const string NotConfigured = "notConfigured";
}

public sealed record AudiobookItem(
    string Id, string Title, string? Author, string? Series, string? Narrator,
    string Language, string LanguageLabel, double? DurationSeconds, double? ProgressPercent,
    string? Description, string? CoverUrl, string? PublishedYear, bool? IsAbridged, string? PlaybackUrl);
public sealed record AudiobookOverview(
    string State, string? Message, AudiobookItem? ContinueListening,
    IReadOnlyList<AudiobookItem> Library, IReadOnlyList<AudiobookItem> Recent,
    AudiobookAcquisitionCapabilities Acquisition);
public sealed record AudiobookLibraryPage(IReadOnlyList<AudiobookItem> Items, int Page, int PageSize, int Total);
public sealed record AudiobookAcquisitionCapabilities(string State, bool CanSearch, bool CanRequest, string? Message);
public sealed record AudiobookDiscoveryResult(
    string WorkId, string EditionId, string Title, string? Author, string? Narrator,
    string Language, string LanguageLabel, double? DurationSeconds, int? PublicationYear,
    string? CoverUrl, string Source, string Availability, string LanguageConfidence);

public interface IAudiobookAcquisitionProvider
{
    Task<AudiobookAcquisitionCapabilities> GetCapabilitiesAsync(CancellationToken token);
    Task<IReadOnlyList<AudiobookDiscoveryResult>> SearchAsync(string query, string language, CancellationToken token);
}

public sealed class NoAudiobookAcquisitionProvider : IAudiobookAcquisitionProvider
{
    private static readonly AudiobookAcquisitionCapabilities Capabilities =
        new("notConfigured", false, false, "Ingen granskad anskaffningsleverantör är konfigurerad.");
    public Task<AudiobookAcquisitionCapabilities> GetCapabilitiesAsync(CancellationToken token) => Task.FromResult(Capabilities);
    public Task<IReadOnlyList<AudiobookDiscoveryResult>> SearchAsync(string query, string language, CancellationToken token) =>
        Task.FromResult<IReadOnlyList<AudiobookDiscoveryResult>>([]);
}

public interface IAudiobookshelfClient
{
    Task<AudiobookOverview> GetOverviewAsync(CancellationToken token);
    Task<AudiobookLibraryPage> GetLibraryAsync(int page, int limit, string? query, string? language, CancellationToken token);
    Task<AudiobookItem?> GetItemAsync(string id, CancellationToken token);
    Task<(byte[] Bytes, string ContentType)?> GetCoverAsync(string id, CancellationToken token);
}

public sealed class AudiobookshelfClient(HttpClient http, MediaOptions options, IAudiobookAcquisitionProvider acquisition)
    : IAudiobookshelfClient
{
    private static readonly Regex Html = new("<[^>]+>", RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));

    public async Task<AudiobookOverview> GetOverviewAsync(CancellationToken token)
    {
        var capabilities = await acquisition.GetCapabilitiesAsync(token);
        if (!Configured) return new(AudiobookIntegrationStates.NotConfigured, "Audiobookshelf är inte konfigurerat.", null, [], [], capabilities);
        try
        {
            var page = await GetLibraryCoreAsync(0, options.Audiobookshelf.PageSize, null, token);
            var listening = page.Items.Where(x => x.ProgressPercent is > 0 and < 100).OrderByDescending(x => x.ProgressPercent).FirstOrDefault();
            return new(AudiobookIntegrationStates.ConfiguredHealthy, null, listening, page.Items, page.Items.Take(8).ToArray(), capabilities);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or MediaAuthenticationException)
        {
            return new(AudiobookIntegrationStates.ConfiguredUnavailable, SafeMessage(exception), null, [], [], capabilities);
        }
    }

    public async Task<AudiobookLibraryPage> GetLibraryAsync(int page, int limit, string? query, string? language, CancellationToken token)
    {
        if (!Configured) return new([], page, limit, 0);
        var result = await GetLibraryCoreAsync(page, limit, query, token);
        var normalized = string.IsNullOrWhiteSpace(language) ? null : AudiobookLanguages.Normalize(language);
        var items = result.Items.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(query)) items = items.Where(x => x.Title.Contains(query.Trim(), StringComparison.CurrentCultureIgnoreCase) || (x.Author?.Contains(query.Trim(), StringComparison.CurrentCultureIgnoreCase) ?? false));
        if (normalized is not null) items = items.Where(x => x.Language == normalized);
        var filtered = items.ToArray();
        return result with { Items = filtered, Total = filtered.Length };
    }

    public async Task<AudiobookItem?> GetItemAsync(string id, CancellationToken token)
    {
        if (!Configured || !SafeId(id)) return null;
        using var document = await GetAsync($"api/items/{Uri.EscapeDataString(id)}?expanded=1&include=progress", token);
        return MapItem(document.RootElement);
    }

    public async Task<(byte[] Bytes, string ContentType)?> GetCoverAsync(string id, CancellationToken token)
    {
        if (!Configured || !SafeId(id)) return null;
        using var request = Request(HttpMethod.Get, $"api/items/{Uri.EscapeDataString(id)}/cover");
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        EnsureSuccess(response);
        var length = response.Content.Headers.ContentLength;
        if (length is > 5_000_000) return null;
        var bytes = await response.Content.ReadAsByteArrayAsync(token);
        if (bytes.Length > 5_000_000) return null;
        return (bytes, response.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
    }

    private bool Configured => !string.IsNullOrWhiteSpace(options.Audiobookshelf.ApiKey) && !string.IsNullOrWhiteSpace(options.Audiobookshelf.LibraryId);
    private async Task<AudiobookLibraryPage> GetLibraryCoreAsync(int page, int limit, string? query, CancellationToken token)
    {
        var uri = $"api/libraries/{Uri.EscapeDataString(options.Audiobookshelf.LibraryId!)}/items?page={page}&limit={limit}&sort=addedAt&desc=1&include=progress";
        using var document = await GetAsync(uri, token);
        var root = document.RootElement;
        var items = root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array
            ? results.EnumerateArray().Select(MapItem).Where(x => x is not null).Cast<AudiobookItem>().ToArray() : [];
        var total = root.TryGetProperty("total", out var totalValue) && totalValue.TryGetInt32(out var count) ? count : items.Length;
        return new(items, page, limit, total);
    }

    private async Task<JsonDocument> GetAsync(string uri, CancellationToken token)
    {
        using var request = Request(HttpMethod.Get, uri);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        EnsureSuccess(response);
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        return await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { MaxDepth = 32 }, token);
    }
    private HttpRequestMessage Request(HttpMethod method, string uri)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new("Bearer", options.Audiobookshelf.ApiKey);
        return request;
    }
    private static void EnsureSuccess(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) throw new MediaAuthenticationException();
        response.EnsureSuccessStatusCode();
    }
    private static bool SafeId(string id) => id.Length is > 0 and <= 128 && id.All(c => char.IsLetterOrDigit(c) || c is '-' or '_');
    private static string SafeMessage(Exception exception) => exception switch
    {
        MediaAuthenticationException => "Audiobookshelf avvisade autentiseringen.",
        TaskCanceledException => "Audiobookshelf svarade inte i tid.",
        JsonException => "Audiobookshelf returnerade ett ogiltigt svar.",
        _ => "Audiobookshelf kunde inte nås."
    };
    private AudiobookItem? MapItem(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object) return null;
        var id = Text(item, "id");
        if (string.IsNullOrWhiteSpace(id)) return null;
        var media = item.TryGetProperty("media", out var mediaValue) ? mediaValue : item;
        var metadata = media.TryGetProperty("metadata", out var metadataValue) ? metadataValue : media;
        var title = Text(metadata, "title") ?? "Namnlös ljudbok";
        var language = AudiobookLanguages.Normalize(Text(metadata, "language"));
        var progress = item.TryGetProperty("userMediaProgress", out var p) && p.ValueKind == JsonValueKind.Object ? Number(p, "progress") * 100 : null;
        var duration = Number(media, "duration");
        var description = Text(metadata, "description");
        if (description is not null) description = WebUtility.HtmlDecode(Html.Replace(description, " ")).Trim();
        return new(id, title, Text(metadata, "authorName"), Text(metadata, "seriesName"), Text(metadata, "narratorName"),
            language, AudiobookLanguages.DisplayName(language), duration, progress, description?.Length > 2000 ? description[..2000] : description,
            $"/api/v1/modules/media/audiobooks/{Uri.EscapeDataString(id)}/cover", Text(metadata, "publishedYear"), null,
            string.IsNullOrWhiteSpace(options.Audiobookshelf.PublicUrl) ? null : $"{options.Audiobookshelf.PublicUrl.TrimEnd('/')}/item/{Uri.EscapeDataString(id)}");
    }
    private static string? Text(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static double? Number(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.TryGetDouble(out var n) ? n : null;
}

public static class AudiobookRanking
{
    public static IReadOnlyList<AudiobookDiscoveryResult> Rank(IEnumerable<AudiobookDiscoveryResult> values, string preferred = "sv", string fallback = "en") =>
        values.OrderBy(x => Score(x, preferred, fallback)).ThenBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.EditionId, StringComparer.Ordinal).ToArray();
    private static int Score(AudiobookDiscoveryResult value, string preferred, string fallback) =>
        value.Language == AudiobookLanguages.Normalize(preferred) && value.LanguageConfidence == "verified" ? 0 :
        value.Language == AudiobookLanguages.Normalize(fallback) && value.LanguageConfidence == "verified" ? 1 :
        value.Language != AudiobookLanguages.Unknown ? 2 : 3;
}
