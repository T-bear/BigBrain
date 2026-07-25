using System.Diagnostics;
using System.Text.Json;

namespace BigBrain.Api.Media;

public sealed class SonarrClient(HttpClient httpClient, MediaOptions options)
    : MediaClientBase(httpClient, "Sonarr"), ISonarrClient, IMediaSearchProvider
{
    public async Task<MediaSearchProviderResult> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Sonarr.ApiKey))
        {
            return new(ServiceName, MediaStatuses.NotConfigured, "Provider credentials are not configured.", []);
        }

        try
        {
            using var series = await GetJsonAsync("api/v3/series", options.Sonarr.ApiKey, cancellationToken);
            var results = series.RootElement.ValueKind == JsonValueKind.Array
                ? series.RootElement.EnumerateArray()
                    .Where(item => (GetString(item, "title") ?? string.Empty)
                        .Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Take(limit)
                    .Select(item =>
                    {
                        var statistics = item.TryGetProperty("statistics", out var value)
                            && value.ValueKind == JsonValueKind.Object ? value : default;
                        return new MediaSearchResult(
                            GetInt32(item, "id").ToString(System.Globalization.CultureInfo.InvariantCulture),
                            GetString(item, "title") ?? "Untitled",
                            NullableInt32(item, "year"),
                            MediaTypes.Series,
                            Boolean(item, "monitored") ? MediaSearchStates.Monitored : MediaSearchStates.Unmonitored,
                            null,
                            new MediaSearchMetadata(
                                SeasonCount: ArrayLength(item.TryGetProperty("seasons", out var seasons) ? seasons : default),
                                EpisodeCount: NullableInt32(statistics, "episodeCount"),
                                EpisodeFileCount: NullableInt32(statistics, "episodeFileCount"),
                                ImageAvailable: HasPoster(item)));
                    })
                    .ToArray()
                : [];
            return new(ServiceName, MediaStatuses.Online, null, results);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return SearchFailure(exception);
        }
    }

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
            using var calendar = await GetJsonAsync(
                $"api/v3/calendar?start={Uri.EscapeDataString(DateTimeOffset.UtcNow.Date.ToString("O"))}&end={Uri.EscapeDataString(DateTimeOffset.UtcNow.Date.AddDays(7).ToString("O"))}",
                options.Sonarr.ApiKey,
                cancellationToken);
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
                MapCalendar(calendar.RootElement),
                MapHistory(Records(history.RootElement)),
                HealthWarnings(health.RootElement, "Sonarr"));
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Empty(Failure(exception, timer));
        }
    }

    private static SonarrOverview Empty(MediaServiceStatus status) => new(status, 0, 0, 0, 0, [], [], [], []);

    private static MediaCalendarItem[] MapCalendar(JsonElement items) =>
        items.ValueKind != JsonValueKind.Array ? [] : items.EnumerateArray().Take(14).Select(item =>
            new MediaCalendarItem(
                GetString(item, "title") ?? GetString(item, "seriesTitle") ?? "Untitled",
                DateTimeOffset.TryParse(GetString(item, "airDateUtc"), out var date) ? date : null)).ToArray();

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

    private static int? NullableInt32(JsonElement item, string property) =>
        item.ValueKind == JsonValueKind.Object
        && item.TryGetProperty(property, out var value)
        && value.TryGetInt32(out var result) ? result : null;

    private static bool HasPoster(JsonElement item) =>
        item.TryGetProperty("images", out var images)
        && images.ValueKind == JsonValueKind.Array
        && images.EnumerateArray().Any(image =>
            string.Equals(GetString(image, "coverType"), "poster", StringComparison.OrdinalIgnoreCase));
}

public sealed class RadarrClient(HttpClient httpClient, MediaOptions options)
    : MediaClientBase(httpClient, "Radarr"), IRadarrClient, IMediaSearchProvider
{
    public async Task<MediaSearchProviderResult> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Radarr.ApiKey))
        {
            return new(ServiceName, MediaStatuses.NotConfigured, "Provider credentials are not configured.", []);
        }

        try
        {
            using var movies = await GetJsonAsync("api/v3/movie", options.Radarr.ApiKey, cancellationToken);
            var results = movies.RootElement.ValueKind == JsonValueKind.Array
                ? movies.RootElement.EnumerateArray()
                    .Where(item => (GetString(item, "title") ?? string.Empty)
                        .Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Take(limit)
                    .Select(item =>
                    {
                        var hasFile = Boolean(item, "hasFile");
                        var monitored = Boolean(item, "monitored");
                        var state = hasFile
                            ? MediaSearchStates.Available
                            : monitored ? MediaSearchStates.Monitored : MediaSearchStates.Unmonitored;
                        return new MediaSearchResult(
                            GetInt32(item, "id").ToString(System.Globalization.CultureInfo.InvariantCulture),
                            GetString(item, "title") ?? "Untitled",
                            NullableInt32(item, "year"),
                            MediaTypes.Movie,
                            state,
                            null,
                            new MediaSearchMetadata(
                                HasFile: hasFile,
                                ImageAvailable: HasPoster(item)));
                    })
                    .ToArray()
                : [];
            return new(ServiceName, MediaStatuses.Online, null, results);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return SearchFailure(exception);
        }
    }

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
            var qualityUpgrades = movieItems.Count(item =>
                item.TryGetProperty("movieFile", out var movieFile)
                && movieFile.ValueKind == JsonValueKind.Object
                && Boolean(movieFile, "qualityCutoffNotMet"));
            return new RadarrOverview(
                Online(GetString(status.RootElement, "version"), timer),
                movieItems.Length,
                movieItems.Count(item => Boolean(item, "monitored")),
                GetInt32(missing.RootElement, "totalRecords"),
                qualityUpgrades,
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

    private static RadarrOverview Empty(MediaServiceStatus status) => new(status, 0, 0, 0, 0, 0, [], [], []);

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

    private static int? NullableInt32(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.TryGetInt32(out var result) ? result : null;

    private static bool HasPoster(JsonElement item) =>
        item.TryGetProperty("images", out var images)
        && images.ValueKind == JsonValueKind.Array
        && images.EnumerateArray().Any(image =>
            string.Equals(GetString(image, "coverType"), "poster", StringComparison.OrdinalIgnoreCase));
}
