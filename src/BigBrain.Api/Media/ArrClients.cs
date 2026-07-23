using System.Diagnostics;
using System.Text.Json;

namespace BigBrain.Api.Media;

public sealed class SonarrClient(HttpClient httpClient, MediaOptions options)
    : MediaClientBase(httpClient, "Sonarr"), ISonarrClient
{
    public async Task<SonarrOverview> GetOverviewAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Sonarr.ApiKey))
        {
            return Empty(NotConfigured());
        }

        var timer = Stopwatch.StartNew();
        try
        {
            using var status = await GetJsonAsync("api/v3/system/status", options.Sonarr.ApiKey, cancellationToken);
            using var series = await GetJsonAsync("api/v3/series", options.Sonarr.ApiKey, cancellationToken);
            using var missing = await GetJsonAsync("api/v3/wanted/missing?page=1&pageSize=1", options.Sonarr.ApiKey, cancellationToken);
            using var queue = await GetJsonAsync("api/v3/queue?page=1&pageSize=25", options.Sonarr.ApiKey, cancellationToken);
            using var history = await GetJsonAsync("api/v3/history?page=1&pageSize=10&sortKey=date&sortDirection=descending", options.Sonarr.ApiKey, cancellationToken);
            using var health = await GetJsonAsync("api/v3/health", options.Sonarr.ApiKey, cancellationToken);

            var seriesItems = series.RootElement.EnumerateArray().ToArray();
            return new SonarrOverview(
                Online(GetString(status.RootElement, "version"), timer),
                seriesItems.Length,
                seriesItems.Count(item => Boolean(item, "monitored")),
                GetInt32(missing.RootElement, "totalRecords"),
                GetInt32(queue.RootElement, "totalRecords"),
                MapQueue(Records(queue.RootElement)),
                MapHistory(Records(history.RootElement)),
                HealthWarnings(health.RootElement, "Sonarr"));
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Empty(Failure(exception, timer));
        }
    }

    private static SonarrOverview Empty(MediaServiceStatus status) => new(status, 0, 0, 0, 0, [], [], []);

    private static MediaQueueItem[] MapQueue(JsonElement records) =>
        records.ValueKind != JsonValueKind.Array
            ? []
            : records.EnumerateArray().Take(25).Select(item =>
            {
                var total = GetDouble(item, "size");
                var left = GetDouble(item, "sizeleft");
                return new MediaQueueItem(
                    GetString(item, "title") ?? "Untitled",
                    GetString(item, "status") ?? "unknown",
                    total > 0 ? ClampPercent(100 * (total - left) / total) : null);
            }).ToArray();

    private static MediaHistoryItem[] MapHistory(JsonElement records) =>
        records.ValueKind != JsonValueKind.Array
            ? []
            : records.EnumerateArray().Take(10).Select(item => new MediaHistoryItem(
                GetString(item, "sourceTitle") ?? "Untitled",
                GetString(item, "eventType") ?? "unknown",
                DateTimeOffset.TryParse(GetString(item, "date"), out var date) ? date : null)).ToArray();
}

public sealed class RadarrClient(HttpClient httpClient, MediaOptions options)
    : MediaClientBase(httpClient, "Radarr"), IRadarrClient
{
    public async Task<RadarrOverview> GetOverviewAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Radarr.ApiKey))
        {
            return Empty(NotConfigured());
        }

        var timer = Stopwatch.StartNew();
        try
        {
            using var status = await GetJsonAsync("api/v3/system/status", options.Radarr.ApiKey, cancellationToken);
            using var movies = await GetJsonAsync("api/v3/movie", options.Radarr.ApiKey, cancellationToken);
            using var missing = await GetJsonAsync("api/v3/wanted/missing?page=1&pageSize=1", options.Radarr.ApiKey, cancellationToken);
            using var queue = await GetJsonAsync("api/v3/queue?page=1&pageSize=25", options.Radarr.ApiKey, cancellationToken);
            using var history = await GetJsonAsync("api/v3/history?page=1&pageSize=10&sortKey=date&sortDirection=descending", options.Radarr.ApiKey, cancellationToken);
            using var health = await GetJsonAsync("api/v3/health", options.Radarr.ApiKey, cancellationToken);

            var movieItems = movies.RootElement.EnumerateArray().ToArray();
            return new RadarrOverview(
                Online(GetString(status.RootElement, "version"), timer),
                movieItems.Length,
                movieItems.Count(item => Boolean(item, "monitored")),
                GetInt32(missing.RootElement, "totalRecords"),
                GetInt32(queue.RootElement, "totalRecords"),
                MapQueue(Records(queue.RootElement)),
                MapHistory(Records(history.RootElement)),
                HealthWarnings(health.RootElement, "Radarr"));
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Empty(Failure(exception, timer));
        }
    }

    private static RadarrOverview Empty(MediaServiceStatus status) => new(status, 0, 0, 0, 0, [], [], []);

    private static MediaQueueItem[] MapQueue(JsonElement records) =>
        records.ValueKind != JsonValueKind.Array
            ? []
            : records.EnumerateArray().Take(25).Select(item =>
            {
                var total = GetDouble(item, "size");
                var left = GetDouble(item, "sizeleft");
                return new MediaQueueItem(
                    GetString(item, "title") ?? "Untitled",
                    GetString(item, "status") ?? "unknown",
                    total > 0 ? ClampPercent(100 * (total - left) / total) : null);
            }).ToArray();

    private static MediaHistoryItem[] MapHistory(JsonElement records) =>
        records.ValueKind != JsonValueKind.Array
            ? []
            : records.EnumerateArray().Take(10).Select(item => new MediaHistoryItem(
                GetString(item, "sourceTitle") ?? "Untitled",
                GetString(item, "eventType") ?? "unknown",
                DateTimeOffset.TryParse(GetString(item, "date"), out var date) ? date : null)).ToArray();
}
