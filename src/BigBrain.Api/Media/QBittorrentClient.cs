using System.Diagnostics;
using System.Text.Json;

namespace BigBrain.Api.Media;

public sealed class QBittorrentClient(HttpClient httpClient, MediaOptions options)
    : MediaClientBase(httpClient, "qBittorrent"), IQBittorrentClient, IQBittorrentQueueClient, IMediaJobsProvider
{
    string IMediaJobsProvider.MediaType => MediaTypes.Unknown;

    public async Task<QBittorrentOverview> GetOverviewAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.QBittorrent.ApiKey))
        {
            return Empty(NotConfigured());
        }

        var timer = Stopwatch.StartNew();
        try
        {
            using var versionResponse = await HttpClient.GetAsync("api/v2/app/version", cancellationToken);
            EnsureSuccess(versionResponse);
            var version = await versionResponse.Content.ReadAsStringAsync(cancellationToken);
            using var torrents = await GetJsonAsync("api/v2/torrents/info?limit=25&sort=added_on&reverse=true", null, cancellationToken);
            using var transfer = await GetJsonAsync("api/v2/transfer/info", null, cancellationToken);
            var items = torrents.RootElement.EnumerateArray().Take(25).ToArray();
            var mapped = items.Select(item => new TorrentItem(
                GetString(item, "name") ?? "Untitled",
                ClampPercent(GetDouble(item, "progress") * 100),
                GetString(item, "state") ?? "unknown",
                GetString(item, "category"),
                GetInt64(item, "eta") is >= 0 and < 8_640_000 ? GetInt64(item, "eta") : null)).ToArray();

            return new QBittorrentOverview(
                Online(version.Trim(), timer),
                items.Count(item => IsActive(GetString(item, "state"))),
                items.Count(item => IsPaused(GetString(item, "state"))),
                items.Count(item => GetDouble(item, "progress") >= 1),
                GetInt64(transfer.RootElement, "dl_info_speed"),
                GetInt64(transfer.RootElement, "up_info_speed"),
                mapped.Where(item => IsActive(item.State) && item.EtaSeconds is not null)
                    .Select(item => item.EtaSeconds).Min(),
                mapped.Length == 0 ? null : items.Average(item => GetDouble(item, "ratio")),
                GetInt64(transfer.RootElement, "dl_info_data"),
                GetInt64(transfer.RootElement, "up_info_data"),
                GetInt64(transfer.RootElement, "free_space_on_disk"),
                mapped);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Empty(Failure(exception, timer));
        }
    }

    async Task<MediaJobsProviderSnapshot> IMediaJobsProvider.GetJobsSnapshotAsync(
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.QBittorrent.ApiKey))
        {
            throw new HttpRequestException("Provider is not configured.");
        }

        using var torrents = await GetJsonAsync(
            "api/v2/torrents/info?limit=50&sort=added_on&reverse=true",
            null,
            cancellationToken);
        var jobs = torrents.RootElement.ValueKind != JsonValueKind.Array
            ? []
            : torrents.RootElement.EnumerateArray().Take(50).Select(item =>
            {
                var rawTitle = GetString(item, "name") ?? "Untitled";
                var parsed = MediaJobAggregator.ParseEpisode(rawTitle);
                var canonicalTitle = MediaJobAggregator.CanonicalTitle(rawTitle);
                var groupKey = parsed.Season is null
                    ? $"qbittorrent:unknown:{canonicalTitle}"
                    : $"qbittorrent:series:{canonicalTitle}:s{parsed.Season}";
                var progress = ClampPercent(GetDouble(item, "progress") * 100);
                var status = NormalizeJobStatus(GetString(item, "state"), progress);
                var addedSeconds = GetInt64(item, "added_on");
                return new ProviderMediaJob(
                    string.Empty,
                    groupKey,
                    SafeDisplayTitle(rawTitle),
                    parsed.Season is null ? null : $"Season {parsed.Season}",
                    parsed.Season is null ? MediaTypes.Unknown : "season",
                    status,
                    progress,
                    Positive(GetInt64(item, "size")),
                    Positive(GetInt64(item, "dlspeed")),
                    Positive(GetInt64(item, "upspeed")),
                    ValidEta(GetInt64(item, "eta")),
                    parsed.Episode,
                    addedSeconds > 0 ? DateTimeOffset.FromUnixTimeSeconds(addedSeconds) : null,
                    addedSeconds > 0 ? DateTimeOffset.FromUnixTimeSeconds(addedSeconds) : null,
                    status == MediaJobStatuses.Failed ? "downloadFailed" : null,
                    status == MediaJobStatuses.Stalled ? "The download is currently stalled." : null,
                    parsed.Episode is null ? "Download detail" : $"Episode {parsed.Episode}");
            }).ToArray();
        return new(ServiceName, jobs, []);
    }

    internal static string NormalizeJobStatus(string? state, double progress)
    {
        if (string.IsNullOrWhiteSpace(state)) return MediaJobStatuses.Unknown;
        if (state.Contains("error", StringComparison.OrdinalIgnoreCase)
            || state.Contains("missingFiles", StringComparison.OrdinalIgnoreCase))
            return MediaJobStatuses.Failed;
        if (state.Contains("stalledDL", StringComparison.OrdinalIgnoreCase))
            return MediaJobStatuses.Stalled;
        if (progress >= 100
            || state.Contains("UP", StringComparison.OrdinalIgnoreCase)
            || state.Contains("upload", StringComparison.OrdinalIgnoreCase))
            return MediaJobStatuses.Completed;
        if (state.Contains("downloading", StringComparison.OrdinalIgnoreCase)
            || state.Contains("metaDL", StringComparison.OrdinalIgnoreCase)
            || state.Contains("forcedDL", StringComparison.OrdinalIgnoreCase))
            return MediaJobStatuses.Downloading;
        if (state.Contains("queued", StringComparison.OrdinalIgnoreCase)
            || state.Contains("checking", StringComparison.OrdinalIgnoreCase)
            || state.Contains("stoppedDL", StringComparison.OrdinalIgnoreCase))
            return MediaJobStatuses.Queued;
        return MediaJobStatuses.Unknown;
    }

    private static string SafeDisplayTitle(string rawTitle)
    {
        var parsed = MediaJobAggregator.ParseEpisode(rawTitle);
        var matchIndex = rawTitle.IndexOf(".S", StringComparison.OrdinalIgnoreCase);
        var title = matchIndex > 0 ? rawTitle[..matchIndex] : rawTitle;
        title = title.Replace('.', ' ').Replace('_', ' ').Trim();
        return string.IsNullOrWhiteSpace(title)
            ? parsed.Season is null ? "Media download" : "Series download"
            : title.Length <= 120 ? title : string.Concat(title.AsSpan(0, 117), "...");
    }

    private static long? Positive(long value) => value > 0 ? value : null;
    private static long? ValidEta(long value) => value is >= 0 and < 8_640_000 ? value : null;

    private static bool IsActive(string? state) =>
        state is not null
        && (state.Contains("downloading", StringComparison.OrdinalIgnoreCase)
            || state.Contains("uploading", StringComparison.OrdinalIgnoreCase)
            || state.Contains("stalled", StringComparison.OrdinalIgnoreCase));

    private static bool IsPaused(string? state) =>
        state is not null
        && (state.Contains("paused", StringComparison.OrdinalIgnoreCase)
            || state.Contains("stopped", StringComparison.OrdinalIgnoreCase));

    private static QBittorrentOverview Empty(MediaServiceStatus status) => new(status, 0, 0, 0, 0, 0, null, null, 0, 0, null, []);

    async Task<IReadOnlyList<QBittorrentQueueItem>> IQBittorrentQueueClient.GetQueueAsync(
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.QBittorrent.ApiKey))
            throw new DownloadControlException("providerUnavailable", "qBittorrent är inte konfigurerad.", StatusCodes.Status503ServiceUnavailable);

        using var torrents = await GetJsonAsync("api/v2/torrents/info", null, cancellationToken);
        if (torrents.RootElement.ValueKind != JsonValueKind.Array) return [];
        return torrents.RootElement.EnumerateArray().Select(item => new QBittorrentQueueItem(
            GetString(item, "hash") ?? string.Empty,
            SafeDisplayTitle(GetString(item, "name") ?? "Untitled"),
            GetString(item, "state") ?? "unknown",
            GetString(item, "category"),
            GetString(item, "save_path"),
            GetString(item, "content_path"),
            Math.Clamp(GetDouble(item, "progress"), 0, 1),
            Math.Max(0, GetInt64(item, "size")),
            Math.Max(0, GetInt64(item, "downloaded")),
            Math.Max(0, GetInt64(item, "dlspeed")),
            Math.Max(0, GetInt64(item, "upspeed")),
            GetInt32(item, "priority"))).Where(item => item.Hash.Length is 40 or 64).ToArray();
    }

    async Task IQBittorrentQueueClient.RemoveAsync(string hash, bool deleteFiles, CancellationToken cancellationToken)
    {
        if (hash.Length is not (40 or 64) || hash.Any(character => !Uri.IsHexDigit(character)))
            throw new DownloadControlException("downloadIdentityChanged", "Nedladdningens identitet är inte längre giltig.", StatusCodes.Status409Conflict);
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/torrents/delete")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["hashes"] = hash,
                ["deleteFiles"] = deleteFiles ? "true" : "false"
            })
        };
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            throw new DownloadControlException("providerAuthenticationFailure", "qBittorrent-autentiseringen misslyckades.", StatusCodes.Status503ServiceUnavailable);
        if (!response.IsSuccessStatusCode)
            throw new DownloadControlException("removalRejected", "qBittorrent avvisade borttagningen.", StatusCodes.Status502BadGateway);
    }
}
