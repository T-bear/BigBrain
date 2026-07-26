using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BigBrain.Api.Media;

public static class MediaJobStatuses
{
    public const string Requested = "requested";
    public const string Searching = "searching";
    public const string Queued = "queued";
    public const string Downloading = "downloading";
    public const string Stalled = "stalled";
    public const string Completed = "completed";
    public const string Importing = "importing";
    public const string Available = "available";
    public const string Failed = "failed";
    public const string Unknown = "unknown";
}

public sealed record MediaJobProviderStatus(string Provider, string Status, string? UserMessage);

public sealed record MediaJobDetail(
    string Provider,
    string Status,
    double? ProgressPercent,
    string? Subtitle,
    string? UserMessage);

public sealed record MediaJob(
    string Id,
    string MediaType,
    string Title,
    string? Subtitle,
    string Provider,
    string Status,
    double? ProgressPercent,
    long? SizeBytes,
    long? DownloadSpeedBytesPerSecond,
    long? UploadSpeedBytesPerSecond,
    long? EtaSeconds,
    int? EpisodeCount,
    int? CompletedEpisodeCount,
    DateTimeOffset? RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? AvailableAt,
    string? ErrorCode,
    string? UserMessage,
    string? PlayItemId,
    bool CanPlay,
    string? Artwork,
    IReadOnlyList<MediaJobDetail> Details);

public sealed record MediaJobsResponse(
    DateTimeOffset CollectedAtUtc,
    string Status,
    IReadOnlyList<MediaJobProviderStatus> Providers,
    IReadOnlyList<MediaJob> Jobs);

public sealed record MediaLibraryStatusResponse(
    string Provider,
    string ForeignId,
    string MediaType,
    bool Requested,
    bool Searching,
    bool Queued,
    bool Downloading,
    bool Importing,
    bool ExistsInJellyfin,
    bool Available,
    bool Failed,
    bool Missing);

public sealed record MediaPlayResponse(
    string JellyfinItemId,
    string Title,
    string MediaType,
    string? Artwork,
    string PlayUrl,
    bool CanPlay);

public sealed record MediaJobsQuery(
    string? Status,
    string? MediaType,
    string? Provider,
    bool IncludeCompleted,
    int Limit);

internal sealed record ProviderMediaJob(
    string ForeignId,
    string GroupKey,
    string Title,
    string? Subtitle,
    string MediaType,
    string Status,
    double? ProgressPercent,
    long? SizeBytes,
    long? DownloadSpeedBytesPerSecond,
    long? UploadSpeedBytesPerSecond,
    long? EtaSeconds,
    int? EpisodeNumber,
    DateTimeOffset? RequestedAt,
    DateTimeOffset? StartedAt,
    string? ErrorCode,
    string? UserMessage,
    string? DetailLabel);

internal sealed record ProviderLibraryItem(
    string ForeignId,
    string Title,
    string MediaType,
    bool HasFile);

internal sealed record MediaJobsProviderSnapshot(
    string Provider,
    IReadOnlyList<ProviderMediaJob> Jobs,
    IReadOnlyList<ProviderLibraryItem> Library);

internal sealed record JellyfinCatalogItem(
    string ItemId,
    string Title,
    string MediaType,
    string? TvdbId,
    string? TmdbId,
    DateTimeOffset? AddedAtUtc,
    bool ArtworkAvailable);

internal interface IMediaJobsProvider
{
    string ProviderName { get; }
    string MediaType { get; }
    Task<MediaJobsProviderSnapshot> GetJobsSnapshotAsync(CancellationToken cancellationToken);
}

internal interface IMediaLibraryCatalog
{
    Task<JellyfinCatalogItem?> FindByForeignIdAsync(
        string provider,
        string foreignId,
        string mediaType,
        CancellationToken cancellationToken);

    Task<JellyfinCatalogItem?> GetPlayItemAsync(string itemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<JellyfinCatalogItem>> GetAvailableCatalogAsync(CancellationToken cancellationToken);
}

public interface IMediaJobsService
{
    Task<MediaJobsResponse> GetJobsAsync(MediaJobsQuery query, CancellationToken cancellationToken);
    Task<MediaJob?> GetJobAsync(string id, CancellationToken cancellationToken);
    Task<MediaLibraryStatusResponse> GetLibraryStatusAsync(
        string provider,
        string foreignId,
        string mediaType,
        CancellationToken cancellationToken);
    Task<MediaPlayResponse?> GetPlayAsync(string itemId, CancellationToken cancellationToken);
    IAsyncEnumerable<MediaJobsResponse> StreamJobsAsync(CancellationToken cancellationToken);
}

internal sealed class MediaJobsService(
    IEnumerable<IMediaJobsProvider> providers,
    IMediaLibraryCatalog jellyfinCatalog) : IMediaJobsService, IDisposable
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RecentlyAvailableWindow = TimeSpan.FromHours(24);
    private static readonly MediaJobsQuery StreamQuery = new(null, null, null, false, 100);
    private readonly IMediaJobsProvider[] providers = providers.ToArray();
    private readonly SemaphoreSlim refreshLock = new(1, 1);
    private MediaJobsResponse? cached;
    private Dictionary<string, string> playItemsByOpaqueId =
        new Dictionary<string, string>(StringComparer.Ordinal);
    private DateTimeOffset cacheExpiresAtUtc;

    public async Task<MediaJobsResponse> GetJobsAsync(
        MediaJobsQuery query,
        CancellationToken cancellationToken)
    {
        Validate(query);
        var snapshot = await GetSnapshotAsync(cancellationToken);
        IEnumerable<MediaJob> jobs = snapshot.Jobs;
        if (!string.IsNullOrWhiteSpace(query.Status))
            jobs = jobs.Where(job =>
                job.Status.Equals(query.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query.MediaType))
            jobs = jobs.Where(job =>
                job.MediaType.Equals(query.MediaType.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query.Provider))
            jobs = jobs.Where(job => job.Details.Any(detail =>
                string.Equals(detail.Provider, query.Provider.Trim(), StringComparison.OrdinalIgnoreCase)));
        if (!query.IncludeCompleted)
            jobs = jobs.Where(job => job.Status != MediaJobStatuses.Completed);
        return snapshot with { Jobs = jobs.Take(query.Limit).ToArray() };
    }

    public async Task<MediaJob?> GetJobAsync(string id, CancellationToken cancellationToken)
    {
        ValidateOpaqueId(id);
        var snapshot = await GetSnapshotAsync(cancellationToken);
        return snapshot.Jobs.SingleOrDefault(job => job.Id == id);
    }

    public async Task<MediaLibraryStatusResponse> GetLibraryStatusAsync(
        string provider,
        string foreignId,
        string mediaType,
        CancellationToken cancellationToken)
    {
        var normalizedProvider = provider.Trim();
        var normalizedType = mediaType.Trim().ToLowerInvariant();
        var selected = providers.SingleOrDefault(candidate =>
            string.Equals(candidate.ProviderName, normalizedProvider, StringComparison.OrdinalIgnoreCase)
            && candidate.MediaType == normalizedType)
            ?? throw Invalid("invalidProvider", "The provider and media type combination is invalid.");
        if (!int.TryParse(foreignId, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out _))
            throw Invalid("invalidForeignId", "The foreign identifier is invalid.");

        try
        {
            var snapshotTask = selected.GetJobsSnapshotAsync(cancellationToken);
            var jellyfinTask = jellyfinCatalog.FindByForeignIdAsync(
                selected.ProviderName, foreignId, normalizedType, cancellationToken);
            await Task.WhenAll(snapshotTask, jellyfinTask);
            var snapshot = await snapshotTask;
            var jellyfin = await jellyfinTask;
            var job = snapshot.Jobs.FirstOrDefault(item => item.ForeignId == foreignId);
            var registered = snapshot.Library.FirstOrDefault(item => item.ForeignId == foreignId);
            var available = jellyfin is not null;
            return new(
                selected.ProviderName,
                foreignId,
                normalizedType,
                registered is not null,
                job?.Status == MediaJobStatuses.Searching,
                job?.Status == MediaJobStatuses.Queued,
                job?.Status is MediaJobStatuses.Downloading or MediaJobStatuses.Stalled,
                job?.Status == MediaJobStatuses.Importing,
                available,
                available,
                job?.Status == MediaJobStatuses.Failed,
                !available && job is null && registered is not null && !registered.HasFile);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (MediaJobsException)
        {
            throw;
        }
        catch
        {
            throw new MediaJobsException(
                "providerUnavailable",
                "The media status provider is unavailable.",
                StatusCodes.Status503ServiceUnavailable);
        }
    }

    public async Task<MediaPlayResponse?> GetPlayAsync(string itemId, CancellationToken cancellationToken)
    {
        ValidateOpaqueId(itemId);
        try
        {
            var snapshot = await GetSnapshotAsync(cancellationToken);
            if (!snapshot.Jobs.Any(job => job.CanPlay && job.PlayItemId == itemId)
                || !playItemsByOpaqueId.TryGetValue(itemId, out var jellyfinItemId))
                return null;
            var item = await jellyfinCatalog.GetPlayItemAsync(jellyfinItemId, cancellationToken);
            return item is null ? null : new(
                itemId,
                item.Title,
                item.MediaType,
                null,
                $"/jellyfin/web/index.html#!/details?id={Uri.EscapeDataString(item.ItemId)}",
                true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (MediaJobsException)
        {
            throw;
        }
        catch
        {
            throw new MediaJobsException(
                "providerUnavailable",
                "Jellyfin is unavailable.",
                StatusCodes.Status503ServiceUnavailable);
        }
    }

    public async IAsyncEnumerable<MediaJobsResponse> StreamJobsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? previous = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            var response = await GetJobsAsync(StreamQuery, cancellationToken);
            var fingerprint = JsonSerializer.Serialize(response);
            if (!string.Equals(previous, fingerprint, StringComparison.Ordinal))
            {
                previous = fingerprint;
                yield return response;
            }
            await Task.Delay(RefreshInterval, cancellationToken);
        }
    }

    private async Task<MediaJobsResponse> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        if (cached is not null && cacheExpiresAtUtc > DateTimeOffset.UtcNow) return cached;
        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (cached is not null && cacheExpiresAtUtc > DateTimeOffset.UtcNow) return cached;
            cached = await CollectAsync(cancellationToken);
            cacheExpiresAtUtc = DateTimeOffset.UtcNow + CacheTtl;
            return cached;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    private async Task<MediaJobsResponse> CollectAsync(CancellationToken cancellationToken)
    {
        var providerResults = await Task.WhenAll(providers.Select(provider =>
            GetProviderSnapshotAsync(provider, cancellationToken)));
        var catalogResult = await SafeGetCatalogAsync(cancellationToken);
        var providerStatuses = providerResults.Select(result => new MediaJobProviderStatus(
            result.Provider,
            result.Snapshot is null ? MediaStatuses.Unavailable : MediaStatuses.Online,
            result.Snapshot is null ? "Provider data is temporarily unavailable." : null)).Append(
                new MediaJobProviderStatus(
                    "Jellyfin",
                    catalogResult.Succeeded ? MediaStatuses.Online : MediaStatuses.Unavailable,
                    catalogResult.Succeeded ? null : "Jellyfin is currently unavailable. Download and import status is still shown."))
            .ToArray();
        var raw = providerResults
            .Where(result => result.Snapshot is not null)
            .SelectMany(result => result.Snapshot!.Jobs.Select(job => (result.Provider, Job: job)))
            .ToArray();
        var arr = raw.Where(item => item.Provider is "Sonarr" or "Radarr").ToArray();
        var availableByGroup = arr
            .Where(item => item.Job.MediaType is MediaLookupTypes.Series or MediaLookupTypes.Movie)
            .Select(item => (
                item.Job.GroupKey,
                Available: FindAvailable(catalogResult.Items, item.Provider, item.Job.ForeignId)))
            .Where(item => item.Available is not null)
            .GroupBy(item => item.GroupKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Available!, StringComparer.Ordinal);

        var grouped = MediaJobAggregator.Aggregate(raw, availableByGroup);
        var keys = grouped.Select(job => job.Id).ToHashSet(StringComparer.Ordinal);
        var cutoff = DateTimeOffset.UtcNow - RecentlyAvailableWindow;
        foreach (var item in catalogResult.Items
            .Where(item => item.AddedAtUtc >= cutoff))
        {
            var foreignId = item.MediaType == MediaLookupTypes.Series ? item.TvdbId : item.TmdbId;
            if (string.IsNullOrWhiteSpace(foreignId)) continue;
            var provider = item.MediaType == MediaLookupTypes.Series ? "Sonarr" : "Radarr";
            var id = OpaqueId($"{provider}:{item.MediaType}:{foreignId}");
            if (!keys.Add(id)) continue;
            grouped.Add(AvailableJob(id, provider, foreignId, item));
        }

        var publicJobs = grouped.Select(job =>
        {
            if (!job.CanPlay || string.IsNullOrWhiteSpace(job.PlayItemId))
                return job with { PlayItemId = null, CanPlay = false };
            var opaqueId = OpaqueId($"Jellyfin:{job.PlayItemId}");
            return job with { PlayItemId = opaqueId };
        }).ToArray();
        playItemsByOpaqueId = grouped
            .Where(job => job.CanPlay && !string.IsNullOrWhiteSpace(job.PlayItemId))
            .ToDictionary(
                job => OpaqueId($"Jellyfin:{job.PlayItemId}"),
                job => job.PlayItemId!,
                StringComparer.Ordinal);

        return new(
            DateTimeOffset.UtcNow,
            providerStatuses.All(status => status.Status == MediaStatuses.Online) ? "complete" : "degraded",
            providerStatuses,
            publicJobs.OrderBy(job => StatusOrder(job.Status))
                .ThenBy(job => job.Title, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static async Task<(string Provider, MediaJobsProviderSnapshot? Snapshot)> GetProviderSnapshotAsync(
        IMediaJobsProvider provider,
        CancellationToken cancellationToken)
    {
        try
        {
            return (provider.ProviderName, await provider.GetJobsSnapshotAsync(cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return (provider.ProviderName, null);
        }
    }

    private static JellyfinCatalogItem? FindAvailable(
        IReadOnlyList<JellyfinCatalogItem> catalog,
        string provider,
        string foreignId) =>
        catalog.FirstOrDefault(item => string.Equals(
            provider,
            "Sonarr",
            StringComparison.OrdinalIgnoreCase) ? item.TvdbId == foreignId : item.TmdbId == foreignId);

    private async Task<(bool Succeeded, IReadOnlyList<JellyfinCatalogItem> Items)> SafeGetCatalogAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            return (true, await jellyfinCatalog.GetAvailableCatalogAsync(cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return (false, []);
        }
    }

    private static MediaJob AvailableJob(
        string id,
        string provider,
        string foreignId,
        JellyfinCatalogItem item) =>
        new(
            id,
            item.MediaType,
            item.Title,
            null,
            provider,
            MediaJobStatuses.Available,
            100,
            null, null, null, null, null, null,
            item.AddedAtUtc,
            item.AddedAtUtc,
            item.AddedAtUtc ?? DateTimeOffset.UtcNow,
            item.AddedAtUtc,
            null,
            null,
            item.ItemId,
            true,
            null,
            [new(provider, MediaJobStatuses.Available, 100, foreignId, null)]);

    private static void Validate(MediaJobsQuery query)
    {
        if (query.Limit is < 1 or > 100)
            throw Invalid("invalidLimit", "Limit must be between 1 and 100.");
        if (!string.IsNullOrWhiteSpace(query.Status)
            && !MediaJobAggregator.ValidStatuses.Contains(query.Status.Trim().ToLowerInvariant()))
            throw Invalid("invalidStatus", "The requested media job status is invalid.");
        if (!string.IsNullOrWhiteSpace(query.MediaType)
            && query.MediaType.Trim().ToLowerInvariant() is not ("movie" or "series" or "season" or "episode" or "unknown"))
            throw Invalid("invalidMediaType", "The requested media type is invalid.");
        if (!string.IsNullOrWhiteSpace(query.Provider)
            && query.Provider.Trim().ToLowerInvariant() is not ("sonarr" or "radarr" or "qbittorrent"))
            throw Invalid("invalidProvider", "The requested provider is invalid.");
    }

    private static void ValidateOpaqueId(string value)
    {
        if (value.Length is < 1 or > 64 || value.Any(character => !char.IsAsciiLetterOrDigit(character)))
            throw Invalid("invalidItemId", "The item identifier is invalid.");
    }

    private static MediaJobsException Invalid(string code, string message) =>
        new(code, message, StatusCodes.Status400BadRequest);

    private static string OpaqueId(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..24];

    private static int StatusOrder(string status) => status switch
    {
        MediaJobStatuses.Failed => 0,
        MediaJobStatuses.Stalled => 1,
        MediaJobStatuses.Importing => 2,
        MediaJobStatuses.Downloading => 3,
        MediaJobStatuses.Queued => 4,
        MediaJobStatuses.Searching => 5,
        MediaJobStatuses.Requested => 6,
        MediaJobStatuses.Completed => 7,
        MediaJobStatuses.Available => 8,
        _ => 9
    };

    public void Dispose() => refreshLock.Dispose();

    internal static string CreateOpaqueId(string value) => OpaqueId(value);
}

internal static partial class MediaJobAggregator
{
    private static readonly string[] Precedence =
    [
        MediaJobStatuses.Available, MediaJobStatuses.Failed, MediaJobStatuses.Stalled,
        MediaJobStatuses.Importing, MediaJobStatuses.Downloading, MediaJobStatuses.Queued,
        MediaJobStatuses.Searching, MediaJobStatuses.Requested, MediaJobStatuses.Completed,
        MediaJobStatuses.Unknown
    ];
    public static readonly HashSet<string> ValidStatuses = Precedence.ToHashSet(StringComparer.Ordinal);

    [GeneratedRegex(@"(?i)\bS(?<season>\d{1,2})(?:E(?<episode>\d{1,3}))?")]
    private static partial Regex SeasonEpisodeRegex();

    [GeneratedRegex(@"(?i)[^a-z0-9]+")]
    private static partial Regex NonAlphaNumericRegex();

    public static List<MediaJob> Aggregate(
        IReadOnlyList<(string Provider, ProviderMediaJob Job)> raw,
        IReadOnlyDictionary<string, JellyfinCatalogItem> availableByGroup)
    {
        var arrGroups = raw.Where(item => item.Provider is "Sonarr" or "Radarr")
            .GroupBy(item => item.Job.GroupKey, StringComparer.Ordinal)
            .ToList();
        var torrent = raw.Where(item => item.Provider == "qBittorrent").ToList();
        var result = new List<MediaJob>();
        foreach (var group in arrGroups)
        {
            var items = group.ToList();
            var representative = items[0].Job;
            var matchingTorrents = torrent.Where(candidate =>
                Matches(representative, candidate.Job)).ToArray();
            foreach (var match in matchingTorrents) torrent.Remove(match);
            items.AddRange(matchingTorrents);
            availableByGroup.TryGetValue(group.Key, out var available);
            result.Add(ToPublic(group.Key, items, available));
        }
        result.AddRange(torrent.GroupBy(item => item.Job.GroupKey, StringComparer.Ordinal)
            .Select(group => ToPublic(group.Key, group.ToList(), null)));
        return result;
    }

    public static string CanonicalTitle(string value)
    {
        var seasonMatch = SeasonEpisodeRegex().Match(value);
        var title = seasonMatch.Success ? value[..seasonMatch.Index] : value;
        return NonAlphaNumericRegex().Replace(title.ToLowerInvariant(), string.Empty);
    }

    public static (int? Season, int? Episode) ParseEpisode(string value)
    {
        var match = SeasonEpisodeRegex().Match(value);
        return match.Success
            ? (int.Parse(match.Groups["season"].Value, System.Globalization.CultureInfo.InvariantCulture),
                match.Groups["episode"].Success
                    ? int.Parse(match.Groups["episode"].Value, System.Globalization.CultureInfo.InvariantCulture)
                    : null)
            : (null, null);
    }

    private static bool Matches(ProviderMediaJob arr, ProviderMediaJob torrent) =>
        torrent.GroupKey == arr.GroupKey
        || (CanonicalTitle(torrent.Title) == CanonicalTitle(arr.Title)
            && ParseEpisode(torrent.DetailLabel ?? torrent.Title).Season
                == ParseEpisode(arr.DetailLabel ?? arr.Subtitle ?? string.Empty).Season);

    private static MediaJob ToPublic(
        string groupKey,
        List<(string Provider, ProviderMediaJob Job)> items,
        JellyfinCatalogItem? available)
    {
        var primary = items.FirstOrDefault(item => item.Provider is "Sonarr" or "Radarr");
        if (primary == default) primary = items[0];
        var statuses = items.Select(item => item.Job.Status).ToList();
        if (available is not null) statuses.Add(MediaJobStatuses.Available);
        var status = Precedence.First(candidate => statuses.Contains(candidate, StringComparer.Ordinal));
        var episodes = items.Select(item => item.Job.EpisodeNumber).Where(number => number is not null).Distinct().Count();
        var completed = items.Where(item => item.Job.Status is
                MediaJobStatuses.Completed or MediaJobStatuses.Importing or MediaJobStatuses.Available)
            .Select(item => item.Job.EpisodeNumber).Where(number => number is not null).Distinct().Count();
        var details = items.Select(item => new MediaJobDetail(
            item.Provider,
            item.Job.Status,
            item.Job.ProgressPercent,
            item.Job.DetailLabel,
            item.Job.UserMessage)).ToArray();
        var progressValues = items.Select(item => item.Job.ProgressPercent).Where(value => value is not null).Cast<double>().ToArray();
        return new(
            MediaJobsService.CreateOpaqueId(groupKey),
            primary.Job.MediaType,
            available?.Title ?? primary.Job.Title,
            primary.Job.Subtitle,
            primary.Provider,
            status,
            status == MediaJobStatuses.Available ? 100 : progressValues.Length == 0 ? null : progressValues.Average(),
            Sum(items.Select(item => item.Job.SizeBytes)),
            Sum(items.Select(item => item.Job.DownloadSpeedBytesPerSecond)),
            Sum(items.Select(item => item.Job.UploadSpeedBytesPerSecond)),
            items.Select(item => item.Job.EtaSeconds).Where(value => value is >= 0).Min(),
            episodes == 0 ? null : episodes,
            episodes == 0 ? null : completed,
            items.Select(item => item.Job.RequestedAt).Where(value => value is not null).Min(),
            items.Select(item => item.Job.StartedAt).Where(value => value is not null).Min(),
            DateTimeOffset.UtcNow,
            available?.AddedAtUtc,
            items.Select(item => item.Job.ErrorCode).FirstOrDefault(value => value is not null),
            status == MediaJobStatuses.Failed ? "This media job needs attention." : null,
            available?.ItemId,
            available is not null,
            null,
            details);
    }

    private static long? Sum(IEnumerable<long?> values)
    {
        var materialized = values.Where(value => value is not null).Cast<long>().ToArray();
        return materialized.Length == 0 ? null : materialized.Sum();
    }
}

internal sealed class MediaJobsException(string code, string safeMessage, int statusCode) : Exception
{
    public string Code { get; } = code;
    public string SafeMessage { get; } = safeMessage;
    public int StatusCode { get; } = statusCode;
}

internal static class ArrJobMapper
{
    public static MediaJobsProviderSnapshot Map(
        string provider,
        string mediaType,
        JsonElement libraryRoot,
        JsonElement queueRoot,
        string queueItemIdProperty,
        string foreignIdProperty,
        Func<JsonElement, bool> hasFile)
    {
        var libraryElements = libraryRoot.ValueKind == JsonValueKind.Array
            ? libraryRoot.EnumerateArray().ToArray() : [];
        var library = libraryElements
            .Where(item => Int32(item, "id") > 0 && Int32(item, foreignIdProperty) > 0)
            .Select(item => new ProviderLibraryItem(
                Int32(item, foreignIdProperty).ToString(System.Globalization.CultureInfo.InvariantCulture),
                String(item, "title") ?? "Untitled",
                mediaType,
                hasFile(item))).ToArray();
        var byInternalId = libraryElements.Where(item => Int32(item, "id") > 0)
            .ToDictionary(item => Int32(item, "id"));
        var records = queueRoot.TryGetProperty("records", out var values)
            && values.ValueKind == JsonValueKind.Array ? values.EnumerateArray() : [];
        var jobs = records.Take(50).Select(item =>
        {
            byInternalId.TryGetValue(Int32(item, queueItemIdProperty), out var registered);
            var total = Int64(item, "size");
            var left = Int64(item, "sizeleft");
            double? progress = total > 0 ? Math.Clamp(100d * (total - left) / total, 0, 100) : null;
            var releaseTitle = String(item, "title") ?? "Untitled";
            var parsed = MediaJobAggregator.ParseEpisode(releaseTitle);
            var statusMessages = item.TryGetProperty("statusMessages", out var messages)
                && messages.ValueKind == JsonValueKind.Array && messages.GetArrayLength() > 0;
            var status = NormalizeArrStatus(
                String(item, "status"), String(item, "trackedDownloadStatus"),
                String(item, "trackedDownloadState"), statusMessages);
            var foreignId = Int32(registered, foreignIdProperty)
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
            var title = String(registered, "title") ?? "Untitled";
            var groupKey = parsed.Season is null
                ? $"{provider}:{mediaType}:{foreignId}"
                : $"{provider}:{mediaType}:{foreignId}:s{parsed.Season}";
            return new ProviderMediaJob(
                foreignId,
                groupKey,
                title,
                parsed.Season is null ? null : $"Season {parsed.Season}",
                parsed.Season is null ? mediaType : "season",
                status,
                progress,
                total > 0 ? total : null,
                null, null,
                Seconds(item, "timeleft"),
                parsed.Episode,
                Date(item, "added"),
                Date(item, "added"),
                status == MediaJobStatuses.Failed ? "providerJobFailed" : null,
                status == MediaJobStatuses.Failed ? "The provider reported a job failure." : null,
                parsed.Episode is null ? null : $"Episode {parsed.Episode}");
        }).Where(item => item.ForeignId != "0").ToArray();
        return new(provider, jobs, library);
    }

    internal static string NormalizeArrStatus(
        string? status,
        string? trackedStatus,
        string? trackedState,
        bool hasError)
    {
        if (hasError || Contains(trackedStatus, "error", "warning") || Contains(trackedState, "failed"))
            return MediaJobStatuses.Failed;
        if (Contains(status, "import") || Contains(trackedStatus, "import") || Contains(trackedState, "import"))
            return MediaJobStatuses.Importing;
        if (Contains(status, "search") || Contains(trackedStatus, "search") || Contains(trackedState, "search"))
            return MediaJobStatuses.Searching;
        if (Contains(status, "queue", "delay", "pending", "paused"))
            return MediaJobStatuses.Queued;
        if (Contains(status, "complete") || Contains(trackedState, "completed"))
            return MediaJobStatuses.Completed;
        if (Contains(status, "stall") || Contains(trackedState, "stall"))
            return MediaJobStatuses.Stalled;
        if (Contains(status, "download") || Contains(trackedStatus, "download") || Contains(trackedState, "download"))
            return MediaJobStatuses.Downloading;
        return MediaJobStatuses.Unknown;
    }

    private static bool Contains(string? value, params string[] candidates) =>
        value is not null && candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));

    private static string? String(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static int Int32(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value)
        && value.TryGetInt32(out var result) ? result : 0;
    private static long Int64(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value)
        && value.TryGetInt64(out var result) ? result : 0;
    private static DateTimeOffset? Date(JsonElement element, string property) =>
        DateTimeOffset.TryParse(String(element, property), out var result) ? result : null;
    private static long? Seconds(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetInt64(out var result)
        && result is >= 0 and < 8_640_000 ? result : null;
}
