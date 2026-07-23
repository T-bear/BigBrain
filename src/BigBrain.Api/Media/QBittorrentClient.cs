using System.Diagnostics;
using System.Text.Json;

namespace BigBrain.Api.Media;

public sealed class QBittorrentClient(HttpClient httpClient, MediaOptions options)
    : MediaClientBase(httpClient, "qBittorrent"), IQBittorrentClient
{
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
                mapped);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Empty(Failure(exception, timer));
        }
    }

    private static bool IsActive(string? state) =>
        state is not null
        && (state.Contains("downloading", StringComparison.OrdinalIgnoreCase)
            || state.Contains("uploading", StringComparison.OrdinalIgnoreCase)
            || state.Contains("stalled", StringComparison.OrdinalIgnoreCase));

    private static bool IsPaused(string? state) =>
        state is not null
        && (state.Contains("paused", StringComparison.OrdinalIgnoreCase)
            || state.Contains("stopped", StringComparison.OrdinalIgnoreCase));

    private static QBittorrentOverview Empty(MediaServiceStatus status) => new(status, 0, 0, 0, 0, 0, []);
}
