using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BigBrain.Api.Media;

public sealed class LibrarrAudiobookAcquisitionProvider(HttpClient http, MediaOptions options, TimeProvider clock)
    : IAudiobookAcquisitionProvider
{
    private static readonly Regex Swedish = new(@"(?:^|[\s.\-_\[(])(swedish|svenska|swe)(?:$|[\s.\-_\])])", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private static readonly Regex English = new(@"(?:^|[\s.\-_\[(])(english|eng)(?:$|[\s.\-_\])])", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private static readonly Regex MetadataSeparators = new(@"[^\p{L}\p{N}]+", RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private readonly ConcurrentDictionary<string, CachedCandidate> candidates = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> observedStates = new(StringComparer.OrdinalIgnoreCase);
    private bool Configured => !string.IsNullOrWhiteSpace(options.Librarr.ApiKey);
    private static readonly HashSet<string> ApprovedSources = new(StringComparer.Ordinal) { "prowlarr_audiobooks", "audiobookbay" };

    public async Task<AudiobookAcquisitionProviderStatus> GetStatusAsync(CancellationToken token)
    {
        if (!Configured)
            return new(AudiobookIntegrationStates.NotConfigured, "librarr", false, false, false, "Librarr är inte konfigurerat.");
        try
        {
            using var statusTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            statusTimeout.CancelAfter(TimeSpan.FromSeconds(10));
            using var response = await http.GetAsync("api/admin/health", statusTimeout.Token);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return Unavailable("Librarr avvisade autentiseringen.");
            if (!response.IsSuccessStatusCode) return Unavailable("Librarr kunde inte nås.");
            using var document = await ParseBoundedAsync(response, token);
            if (!document.RootElement.TryGetProperty("healthy", out var healthy) || healthy.ValueKind != JsonValueKind.True)
                return Unavailable(DependencyMessage(document.RootElement));
            var checks = document.RootElement.TryGetProperty("checks", out var value) && value.ValueKind == JsonValueKind.Array
                ? value.EnumerateArray().ToArray() : [];
            foreach (var required in new[] { "prowlarr", "qbittorrent", "audiobookshelf" })
                if (!checks.Any(check => Text(check, "service") == required && Text(check, "status") == "ok"))
                    return Unavailable($"Librarrs beroende {required} är inte tillgängligt.");
            return new(AudiobookIntegrationStates.ConfiguredHealthy, "librarr", true, true, false, null);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or IOException)
        {
            return Unavailable("Librarr kunde inte nås.");
        }
    }

    public async Task<IReadOnlyList<AudiobookAcquisitionCandidate>> SearchAsync(string query, string? author, string language, CancellationToken token)
    {
        PruneCandidates();
        var uri = $"api/search/audiobooks?q={Uri.EscapeDataString(query)}";
        if (!string.IsNullOrWhiteSpace(author)) uri += $"&author={Uri.EscapeDataString(author)}";
        using var response = await http.GetAsync(uri, token);
        EnsureProviderSuccess(response);
        using var document = await ParseBoundedAsync(response, token);
        if (!document.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array) return [];
        var mapped = new List<AudiobookAcquisitionCandidate>();
        var byRelease = new Dictionary<string, AudiobookAcquisitionCandidate>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in results.EnumerateArray().Take(50))
        {
            var source = Text(item, "source");
            var title = WebUtility.HtmlDecode(Text(item, "title"))?.Trim();
            var infoHash = Text(item, "info_hash")?.ToLowerInvariant();
            var abbUrl = Text(item, "abb_url");
            if (source is null || !ApprovedSources.Contains(source) || string.IsNullOrWhiteSpace(title)) continue;
            if (source == "prowlarr_audiobooks" && !SafeInfoHash(infoHash)) continue;
            if (source == "audiobookbay" && !SafeAbbPath(abbUrl)) continue;
            var raw = new LibrarrCandidate(
                title, NullIfWhiteSpace(WebUtility.HtmlDecode(Text(item, "author"))?.Trim()), source, Text(item, "source_id"),
                Text(item, "download_url"), Text(item, "magnet_url"), infoHash, Text(item, "guid"), abbUrl,
                Text(item, "download_protocol"), Text(item, "format"), Text(item, "indexer"), Number(item, "size"));
            var releaseKey = SafeInfoHash(infoHash) ? $"hash:{infoHash}" : $"source:{source}:{abbUrl}";
            var editionId = Id("edition", raw.Source, raw.InfoHash, raw.Guid, raw.DownloadUrl, raw.AbbUrl);
            candidates[editionId] = new(raw, clock.GetUtcNow().AddMinutes(options.Librarr.CandidateLifetimeMinutes));
            var (candidateLanguage, confidence) = Language(item, title);
            var candidate = new AudiobookAcquisitionCandidate(
                Id("work", title, raw.Author ?? author ?? string.Empty), editionId, title,
                raw.Author ?? author, null, candidateLanguage, AudiobookLanguages.DisplayName(candidateLanguage),
                Edition(raw), null, Year(item), null, "librarr", "available", confidence, Provenance(source));
            if (!byRelease.TryGetValue(releaseKey, out var existing) || Prefer(candidate, existing))
                byRelease[releaseKey] = candidate;
        }
        mapped.AddRange(byRelease.Values);
        return mapped;
    }

    public async Task<AudiobookProviderJob> RequestAsync(AudiobookAcquisitionRequest request, CancellationToken token)
    {
        if (request.Source != "librarr" || !candidates.TryRemove(request.EditionId, out var cached) || cached.ExpiresAtUtc <= clock.GetUtcNow())
            throw new AudiobookAcquisitionException("candidateExpired", "Sökresultatet har gått ut. Sök igen före Lägg till.", StatusCodes.Status409Conflict);
        var value = cached.Value;
        using var response = await http.PostAsJsonAsync("api/download/audiobook", new
        {
            source = value.Source,
            title = value.Title,
            author = value.Author,
            source_id = value.SourceId,
            download_url = value.DownloadUrl,
            magnet_url = value.MagnetUrl,
            info_hash = value.InfoHash,
            guid = value.Guid,
            abb_url = value.AbbUrl,
            download_protocol = value.DownloadProtocol,
            media_type = "audiobook",
            force = false
        }, token);
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new AudiobookAcquisitionException("acquisitionConflict", "Ljudboken eller hämtningen finns redan.", StatusCodes.Status409Conflict);
        EnsureProviderSuccess(response);
        using var document = await ParseBoundedAsync(response, token);
        if (!document.RootElement.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True)
            throw new AudiobookAcquisitionException("providerRejected", "Librarr avvisade hämtningen.", StatusCodes.Status502BadGateway);
        var providerJobId = Text(document.RootElement, "info_hash")?.ToLowerInvariant() ?? value.InfoHash;
        if (!SafeInfoHash(providerJobId))
            throw new AudiobookAcquisitionException("providerRejected", "Librarr returnerade ingen spårbar hämtning.", StatusCodes.Status502BadGateway);
        var trackedJobId = providerJobId!;
        observedStates[trackedJobId] = AudiobookAcquisitionStatuses.Queued;
        return new(trackedJobId, AudiobookAcquisitionStatuses.Queued, null);
    }

    public async Task<AudiobookProviderJob?> GetJobStatusAsync(string providerJobId, CancellationToken token)
    {
        if (!SafeInfoHash(providerJobId)) return null;
        using var response = await http.GetAsync("api/downloads", token);
        EnsureProviderSuccess(response);
        using var document = await ParseBoundedAsync(response, token);
        var downloads = document.RootElement.TryGetProperty("downloads", out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Take(100) : [];
        foreach (var item in downloads)
        {
            if (!string.Equals(Text(item, "hash"), providerJobId, StringComparison.OrdinalIgnoreCase)) continue;
            var mapped = MapState(Text(item, "status"));
            observedStates[providerJobId] = mapped;
            return new(providerJobId, mapped, SafeProviderMessage(mapped, Text(item, "error"), Text(item, "detail")));
        }
        var outcome = await GetImportOutcomeAsync(providerJobId, token);
        if (outcome.Status is not null)
        {
            observedStates[providerJobId] = outcome.Status;
            return new(providerJobId, outcome.Status, outcome.Message);
        }
        if (observedStates.TryGetValue(providerJobId, out var prior) && prior is AudiobookAcquisitionStatuses.Importing or AudiobookAcquisitionStatuses.Indexing)
            return new(providerJobId, prior, "Importen bearbetas fortfarande.");
        return null;
    }

    public Task<AudiobookProviderJob> CancelAsync(string providerJobId, CancellationToken token) =>
        throw new AudiobookAcquisitionException("cancelUnsupported", "Librarr stöder inte en tillräckligt säker avbrytning.", StatusCodes.Status409Conflict);

    internal static string MapState(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "queued" or "paused" or "checking" or "retry_wait" => AudiobookAcquisitionStatuses.Queued,
        "downloading" or "stalled" => AudiobookAcquisitionStatuses.Downloading,
        "completed" or "uploading" or "seeding" => AudiobookAcquisitionStatuses.Importing,
        "importing" => AudiobookAcquisitionStatuses.Importing,
        "error" or "failed" or "dead_letter" or "missingfiles" => AudiobookAcquisitionStatuses.Failed,
        "cancelled" => AudiobookAcquisitionStatuses.Cancelled,
        _ => AudiobookAcquisitionStatuses.Failed
    };

    private async Task<(string? Status, string? Message)> GetImportOutcomeAsync(string providerJobId, CancellationToken token)
    {
        using var activityResponse = await http.GetAsync("api/activity?limit=100&offset=0", token);
        EnsureProviderSuccess(activityResponse);
        using var activity = await ParseBoundedAsync(activityResponse, token);
        var events = activity.RootElement.TryGetProperty("events", out var eventValues) && eventValues.ValueKind == JsonValueKind.Array
            ? eventValues.EnumerateArray() : [];
        var latest = events.FirstOrDefault(item => string.Equals(Text(item, "job_id"), providerJobId, StringComparison.OrdinalIgnoreCase)
            && Text(item, "event_type") is "torrent_import" or "torrent_import_failed");
        var eventType = Text(latest, "event_type");
        if (eventType == "torrent_import_failed")
            return (AudiobookAcquisitionStatuses.Failed, "Importen stoppades och kräver åtgärd. Befintliga filer har bevarats.");
        if (eventType != "torrent_import") return (null, null);

        using var localResponse = await http.GetAsync("api/library?type=audiobook&limit=100&offset=0", token);
        EnsureProviderSuccess(localResponse);
        using var local = await ParseBoundedAsync(localResponse, token);
        var items = local.RootElement.TryGetProperty("items", out var localItems) && localItems.ValueKind == JsonValueKind.Array
            ? localItems.EnumerateArray() : [];
        var imported = items.FirstOrDefault(item => string.Equals(Text(item, "source_id"), providerJobId, StringComparison.OrdinalIgnoreCase));
        var title = Text(imported, "title");
        if (string.IsNullOrWhiteSpace(title))
            return (AudiobookAcquisitionStatuses.Importing, "Importen är registrerad men biblioteksposten kunde ännu inte bekräftas.");

        using var absResponse = await http.GetAsync($"api/library/audiobooks?q={Uri.EscapeDataString(title)}", token);
        EnsureProviderSuccess(absResponse);
        using var abs = await ParseBoundedAsync(absResponse, token);
        var indexedItems = abs.RootElement.TryGetProperty("items", out var absItems) && absItems.ValueKind == JsonValueKind.Array
            ? absItems.EnumerateArray() : [];
        var author = NullIfUnknown(Text(imported, "author"));
        var indexedMatches = indexedItems.Count(item => TitlesIdentifySameImportedWork(title, Text(item, "title"))
            && (author is null || string.Equals(Text(item, "author")?.Trim(), author, StringComparison.OrdinalIgnoreCase)));
        var indexed = indexedMatches == 1;
        return indexed
            ? (AudiobookAcquisitionStatuses.Completed, null)
            : (AudiobookAcquisitionStatuses.Indexing, "Importerad; väntar på Audiobookshelf-indexering.");
    }

    private static (string Language, string Confidence) Language(JsonElement item, string title)
    {
        var explicitLanguage = Text(item, "language");
        var normalized = AudiobookLanguages.Normalize(explicitLanguage);
        if (normalized != AudiobookLanguages.Unknown) return (normalized, "verified");
        if (Swedish.IsMatch(title)) return ("sv", "probable");
        if (English.IsMatch(title)) return ("en", "probable");
        return (AudiobookLanguages.Unknown, "unknown");
    }

    private static string Edition(LibrarrCandidate value)
    {
        var parts = new[] { value.Format, value.Size is > 0 ? FormatSize(value.Size.Value) : null }.Where(x => !string.IsNullOrWhiteSpace(x));
        var text = string.Join(" · ", parts);
        return text.Length == 0 ? "Release" : text.Length <= 160 ? text : text[..160];
    }
    private static string FormatSize(long bytes) => bytes >= 1_073_741_824 ? $"{bytes / 1_073_741_824d:0.0} GB" : $"{bytes / 1_048_576d:0} MB";
    private static int? Year(JsonElement item) => int.TryParse(Text(item, "year"), NumberStyles.None, CultureInfo.InvariantCulture, out var year) && year is >= 1000 and <= 9999 ? year : null;
    private static long? Number(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.TryGetInt64(out var number) ? number : null;
    private static string? Text(JsonElement item, string name) => item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private static string? NullIfUnknown(string? value) => string.IsNullOrWhiteSpace(value) || value.Equals("unknown", StringComparison.OrdinalIgnoreCase) ? null : value.Trim();
    private static bool TitlesIdentifySameImportedWork(string importedTitle, string? indexedTitle)
    {
        var imported = NormalizeMetadata(importedTitle);
        var indexed = NormalizeMetadata(indexedTitle);
        return imported == indexed || indexed.Length >= 8 && imported.Contains(indexed, StringComparison.Ordinal);
    }
    private static string NormalizeMetadata(string? value) => MetadataSeparators.Replace(value?.Normalize(NormalizationForm.FormKC).ToLowerInvariant() ?? string.Empty, " ").Trim();
    private static bool SafeInfoHash(string? value) => value is { Length: 40 or 64 } && value.All(Uri.IsHexDigit);
    private static bool SafeAbbPath(string? value) => value is { Length: > 1 and <= 500 }
        && value[0] == '/' && !value.StartsWith("//", StringComparison.Ordinal) && !value.Contains("..", StringComparison.Ordinal);
    private static string Provenance(string source) => source == "prowlarr_audiobooks" ? "Prowlarr" : "AudioBookBay";
    private static bool Prefer(AudiobookAcquisitionCandidate candidate, AudiobookAcquisitionCandidate existing) =>
        candidate.Provenance == "Prowlarr" && existing.Provenance != "Prowlarr";
    private static string Id(string prefix, params string?[] values) => $"{prefix}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\u001f', values.Select(value => value ?? string.Empty))))).ToLowerInvariant()}";
    private void PruneCandidates()
    {
        var now = clock.GetUtcNow();
        foreach (var item in candidates) if (item.Value.ExpiresAtUtc <= now) candidates.TryRemove(item.Key, out _);
    }
    private static AudiobookAcquisitionProviderStatus Unavailable(string message) => new(AudiobookIntegrationStates.ConfiguredUnavailable, "librarr", false, false, false, message);
    private static string DependencyMessage(JsonElement root) => root.TryGetProperty("checks", out var checks) && checks.ValueKind == JsonValueKind.Array
        ? checks.EnumerateArray().Select(x => new { Service = Text(x, "service"), Status = Text(x, "status") }).FirstOrDefault(x => x.Status != "ok") is { Service: { } service }
            ? $"Librarrs beroende {service} är inte tillgängligt." : "Librarrs beroenden är inte tillgängliga."
        : "Librarrs beroenden är inte tillgängliga.";
    private static void EnsureProviderSuccess(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new AudiobookAcquisitionException("providerAuthenticationFailure", "Librarr avvisade autentiseringen.", StatusCodes.Status503ServiceUnavailable);
        if (!response.IsSuccessStatusCode)
            throw new AudiobookAcquisitionException("providerUnavailable", "Librarr kunde inte nås.", StatusCodes.Status503ServiceUnavailable);
    }
    private static async Task<JsonDocument> ParseBoundedAsync(HttpResponseMessage response, CancellationToken token)
    {
        if (response.Content.Headers.ContentLength is > 2_000_000) throw new JsonException("Provider response exceeds limit.");
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var bounded = new LimitedReadStream(stream, 2_000_000);
        return await JsonDocument.ParseAsync(bounded, new JsonDocumentOptions { MaxDepth = 32 }, token);
    }
    private static string? SafeProviderMessage(string status, string? error, string? detail) => status == AudiobookAcquisitionStatuses.Failed
        ? error?.Contains("conflict", StringComparison.OrdinalIgnoreCase) == true || detail?.Contains("destination", StringComparison.OrdinalIgnoreCase) == true
            ? "Importen stoppades eftersom destinationen redan innehåller data."
            : "Librarr kunde inte slutföra hämtningen eller importen."
        : null;

    private sealed record CachedCandidate(LibrarrCandidate Value, DateTimeOffset ExpiresAtUtc);
    private sealed record LibrarrCandidate(string Title, string? Author, string Source, string? SourceId, string? DownloadUrl, string? MagnetUrl, string? InfoHash, string? Guid, string? AbbUrl, string? DownloadProtocol, string? Format, string? Indexer, long? Size);
}

internal sealed class LimitedReadStream(Stream inner, long maximumBytes) : Stream
{
    private long read;
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => read; set => throw new NotSupportedException(); }
    public override void Flush() => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) { var value = inner.Read(buffer, offset, count); Count(value); return value; }
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) { var value = await inner.ReadAsync(buffer, cancellationToken); Count(value); return value; }
    private void Count(int value) { read += value; if (read > maximumBytes) throw new IOException("Provider response exceeds limit."); }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
}
