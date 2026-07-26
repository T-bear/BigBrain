using System.Net.Http.Json;
using System.Diagnostics;
using System.Text.Json;

namespace BigBrain.Api.Media;

public sealed class SonarrClient(HttpClient httpClient, MediaOptions options)
    : MediaClientBase(httpClient, "Sonarr"), ISonarrClient, IMediaSearchProvider,
      IMediaLookupProvider, IMediaRequestProvider, IMediaAddProvider, IMediaJobsProvider
{
    public string SupportedMediaType => MediaLookupTypes.Series;
    string IMediaJobsProvider.MediaType => MediaLookupTypes.Series;

    public async Task<MediaLookupProviderResult> LookupAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Sonarr.ApiKey))
            return new(ServiceName, MediaStatuses.NotConfigured, "Provider credentials are not configured.", []);
        try
        {
            using var lookup = await GetJsonAsync(
                $"api/v3/series/lookup?term={Uri.EscapeDataString(query)}",
                options.Sonarr.ApiKey,
                cancellationToken);
            using var registered = await GetJsonAsync("api/v3/series", options.Sonarr.ApiKey, cancellationToken);
            var registeredItems = registered.RootElement.ValueKind == JsonValueKind.Array
                ? registered.RootElement.EnumerateArray().ToArray()
                : [];
            var results = lookup.RootElement.ValueKind == JsonValueKind.Array
                ? lookup.RootElement.EnumerateArray().Take(limit)
                    .Select(item => MapLookup(item, registeredItems))
                    .ToArray()
                : [];
            return new(ServiceName, MediaStatuses.Online, null, results);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return LookupFailure(exception);
        }
    }

    async Task<ProviderAddOptions> IMediaRequestProvider.GetAddOptionsAsync(CancellationToken cancellationToken)
    {
        EnsureSonarrConfigured();
        using var roots = await GetJsonAsync("api/v3/rootfolder", options.Sonarr.ApiKey, cancellationToken);
        using var qualities = await GetJsonAsync("api/v3/qualityprofile", options.Sonarr.ApiKey, cancellationToken);
        return new(
            ServiceName,
            MediaLookupTypes.Series,
            MapProviderOptions(roots.RootElement, "TV Library", includeValue: true),
            MapProviderOptions(qualities.RootElement, "Quality profile", includeValue: false),
            ["all", "future", "missing", "existing", "firstSeason", "lastSeason", "latestSeason", "pilot", "recent", "none"],
            ["standard", "daily", "anime"]);
    }

    async Task<MediaLookupResult?> IMediaRequestProvider.GetLookupItemAsync(
        string foreignId,
        CancellationToken cancellationToken)
    {
        EnsureSonarrConfigured();
        if (!int.TryParse(foreignId, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var tvdbId))
            return null;
        using var lookup = await GetJsonAsync(
            $"api/v3/series/lookup?term=tvdb:{tvdbId}",
            options.Sonarr.ApiKey,
            cancellationToken);
        var item = lookup.RootElement.ValueKind == JsonValueKind.Array
            ? lookup.RootElement.EnumerateArray().FirstOrDefault(candidate => GetInt32(candidate, "tvdbId") == tvdbId)
            : default;
        return item.ValueKind == JsonValueKind.Object ? MapLookup(item, []) : null;
    }

    async Task<bool> IMediaRequestProvider.IsRegisteredAsync(
        string foreignId,
        string title,
        int? year,
        CancellationToken cancellationToken)
    {
        EnsureSonarrConfigured();
        using var registered = await GetJsonAsync("api/v3/series", options.Sonarr.ApiKey, cancellationToken);
        if (registered.RootElement.ValueKind != JsonValueKind.Array) return false;
        return registered.RootElement.EnumerateArray().Any(item =>
            int.TryParse(foreignId, out var tvdbId) && tvdbId > 0
                ? GetInt32(item, "tvdbId") == tvdbId
                : SameTitleAndYear(item, title, year));
    }

    async Task<ProviderAddResult> IMediaAddProvider.AddAsync(
        ProviderAddCommand command,
        CancellationToken cancellationToken)
    {
        EnsureSonarrConfigured();
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v3/series");
        request.Headers.TryAddWithoutValidation("X-Api-Key", options.Sonarr.ApiKey);
        request.Content = JsonContent.Create(new
        {
            tvdbId = int.Parse(command.ForeignId, System.Globalization.CultureInfo.InvariantCulture),
            command.Title,
            command.QualityProfileId,
            rootFolderPath = command.RootFolderValue,
            monitored = command.Monitor != "none",
            seriesType = command.SeriesType ?? "standard",
            seasonFolder = true,
            addOptions = new
            {
                monitor = command.Monitor,
                searchForMissingEpisodes = command.SearchAfterAdd,
                searchForCutoffUnmetEpisodes = false
            }
        });
        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        EnsureSuccess(response);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return new(
            GetInt32(document.RootElement, "id").ToString(System.Globalization.CultureInfo.InvariantCulture),
            GetString(document.RootElement, "title") ?? command.Title);
    }
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

    async Task<MediaJobsProviderSnapshot> IMediaJobsProvider.GetJobsSnapshotAsync(
        CancellationToken cancellationToken)
    {
        EnsureSonarrConfigured();
        using var series = await GetJsonAsync("api/v3/series", options.Sonarr.ApiKey, cancellationToken);
        using var queue = await GetJsonAsync(
            "api/v3/queue?page=1&pageSize=50",
            options.Sonarr.ApiKey,
            cancellationToken);
        return ArrJobMapper.Map(
            ServiceName,
            MediaLookupTypes.Series,
            series.RootElement,
            queue.RootElement,
            "seriesId",
            "tvdbId",
            item => item.TryGetProperty("statistics", out var statistics)
                && GetInt32(statistics, "episodeFileCount") > 0);
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

    private MediaLookupProviderResult LookupFailure(Exception exception)
    {
        var (code, message, status) = MediaProviderFailures.Map(exception);
        return new(ServiceName, status, message, [], code);
    }

    private static MediaLookupResult MapLookup(JsonElement item, IReadOnlyList<JsonElement> registered)
    {
        var tvdbId = GetInt32(item, "tvdbId");
        var title = GetString(item, "title") ?? "Untitled";
        var year = NullableInt32(item, "year");
        var existing = registered.FirstOrDefault(candidate =>
            tvdbId > 0 ? GetInt32(candidate, "tvdbId") == tvdbId : SameTitleAndYear(candidate, title, year));
        var alreadyRegistered = existing.ValueKind == JsonValueKind.Object;
        return new(
            "Sonarr",
            tvdbId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            title,
            null,
            year,
            LimitOverview(GetString(item, "overview")),
            GetString(item, "network"),
            NullableInt32(item, "runtime"),
            GetString(item, "status"),
            MediaLookupTypes.Series,
            alreadyRegistered ? MediaLookupStates.AlreadyRegistered : MediaLookupStates.External,
            HasPoster(item),
            alreadyRegistered,
            alreadyRegistered ? GetInt32(existing, "id").ToString(System.Globalization.CultureInfo.InvariantCulture) : null,
            tvdbId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            PosterUrl(item),
            alreadyRegistered ? Boolean(existing, "monitored") : null,
            !alreadyRegistered,
            alreadyRegistered ? MediaProviderErrorCodes.AlreadyExists : "canRequest");
    }

    private static ProviderOption[] MapProviderOptions(JsonElement root, string label, bool includeValue) =>
        root.ValueKind != JsonValueKind.Array ? [] : root.EnumerateArray()
            .Where(item => GetInt32(item, "id") > 0 && (!includeValue || Boolean(item, "accessible")))
            .Select((item, index) => new ProviderOption(
                GetInt32(item, "id"),
                includeValue ? GetString(item, "path") ?? string.Empty : GetInt32(item, "id").ToString(System.Globalization.CultureInfo.InvariantCulture),
                includeValue ? $"{label} {index + 1}" : GetString(item, "name") ?? $"{label} {index + 1}",
                includeValue && item.TryGetProperty("freeSpace", out var free) && free.TryGetInt64(out var bytes) ? bytes : null))
            .ToArray();

    private static bool SameTitleAndYear(JsonElement item, string title, int? year) =>
        string.Equals(GetString(item, "title"), title, StringComparison.OrdinalIgnoreCase)
        && (year is null || NullableInt32(item, "year") == year);

    private static string? LimitOverview(string? overview) =>
        overview is null || overview.Length <= 500 ? overview : string.Concat(overview.AsSpan(0, 497), "...");

    private static string? PosterUrl(JsonElement item) => MediaPosterToken.Create(ArrPosterUrl.Get(item));

    private void EnsureSonarrConfigured()
    {
        if (string.IsNullOrWhiteSpace(options.Sonarr.ApiKey))
            throw new MediaRequestException(
                MediaRequestErrors.ProviderUnavailable,
                "Sonarr is not configured.",
                StatusCodes.Status503ServiceUnavailable);
    }
}

public sealed class RadarrClient(HttpClient httpClient, MediaOptions options)
    : MediaClientBase(httpClient, "Radarr"), IRadarrClient, IMediaSearchProvider,
      IMediaLookupProvider, IMediaRequestProvider, IMediaAddProvider, IMediaJobsProvider
{
    public string SupportedMediaType => MediaLookupTypes.Movie;
    string IMediaJobsProvider.MediaType => MediaLookupTypes.Movie;

    public async Task<MediaLookupProviderResult> LookupAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Radarr.ApiKey))
            return new(ServiceName, MediaStatuses.NotConfigured, "Provider credentials are not configured.", []);
        try
        {
            using var lookup = await GetJsonAsync(
                $"api/v3/movie/lookup?term={Uri.EscapeDataString(query)}",
                options.Radarr.ApiKey,
                cancellationToken);
            using var registered = await GetJsonAsync("api/v3/movie", options.Radarr.ApiKey, cancellationToken);
            var registeredItems = registered.RootElement.ValueKind == JsonValueKind.Array
                ? registered.RootElement.EnumerateArray().ToArray()
                : [];
            var results = lookup.RootElement.ValueKind == JsonValueKind.Array
                ? lookup.RootElement.EnumerateArray().Take(limit)
                    .Select(item => MapLookup(item, registeredItems))
                    .ToArray()
                : [];
            return new(ServiceName, MediaStatuses.Online, null, results);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            var (code, message, status) = MediaProviderFailures.Map(exception);
            return new(ServiceName, status, message, [], code);
        }
    }

    async Task<ProviderAddOptions> IMediaRequestProvider.GetAddOptionsAsync(CancellationToken cancellationToken)
    {
        EnsureRadarrConfigured();
        using var roots = await GetJsonAsync("api/v3/rootfolder", options.Radarr.ApiKey, cancellationToken);
        using var qualities = await GetJsonAsync("api/v3/qualityprofile", options.Radarr.ApiKey, cancellationToken);
        return new(
            ServiceName,
            MediaLookupTypes.Movie,
            MapProviderOptions(roots.RootElement, "Movie Library", includeValue: true),
            MapProviderOptions(qualities.RootElement, "Quality profile", includeValue: false),
            ["movieOnly", "movieAndCollection", "none"],
            []);
    }

    async Task<MediaLookupResult?> IMediaRequestProvider.GetLookupItemAsync(
        string foreignId,
        CancellationToken cancellationToken)
    {
        EnsureRadarrConfigured();
        if (!int.TryParse(foreignId, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var tmdbId))
            return null;
        using var lookup = await GetJsonAsync(
            $"api/v3/movie/lookup?term=tmdb:{tmdbId}",
            options.Radarr.ApiKey,
            cancellationToken);
        var item = lookup.RootElement.ValueKind == JsonValueKind.Array
            ? lookup.RootElement.EnumerateArray().FirstOrDefault(candidate => GetInt32(candidate, "tmdbId") == tmdbId)
            : default;
        return item.ValueKind == JsonValueKind.Object ? MapLookup(item, []) : null;
    }

    async Task<bool> IMediaRequestProvider.IsRegisteredAsync(
        string foreignId,
        string title,
        int? year,
        CancellationToken cancellationToken)
    {
        EnsureRadarrConfigured();
        using var registered = await GetJsonAsync("api/v3/movie", options.Radarr.ApiKey, cancellationToken);
        if (registered.RootElement.ValueKind != JsonValueKind.Array) return false;
        return registered.RootElement.EnumerateArray().Any(item =>
            int.TryParse(foreignId, out var tmdbId) && tmdbId > 0
                ? GetInt32(item, "tmdbId") == tmdbId
                : SameTitleAndYear(item, title, year));
    }

    async Task<ProviderAddResult> IMediaAddProvider.AddAsync(
        ProviderAddCommand command,
        CancellationToken cancellationToken)
    {
        EnsureRadarrConfigured();
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v3/movie");
        request.Headers.TryAddWithoutValidation("X-Api-Key", options.Radarr.ApiKey);
        request.Content = JsonContent.Create(new
        {
            tmdbId = int.Parse(command.ForeignId, System.Globalization.CultureInfo.InvariantCulture),
            command.Title,
            command.QualityProfileId,
            rootFolderPath = command.RootFolderValue,
            monitored = command.Monitor != "none",
            minimumAvailability = "released",
            addOptions = new
            {
                monitor = command.Monitor,
                searchForMovie = command.SearchAfterAdd,
                addMethod = "manual"
            }
        });
        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        EnsureSuccess(response);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return new(
            GetInt32(document.RootElement, "id").ToString(System.Globalization.CultureInfo.InvariantCulture),
            GetString(document.RootElement, "title") ?? command.Title);
    }
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

    async Task<MediaJobsProviderSnapshot> IMediaJobsProvider.GetJobsSnapshotAsync(
        CancellationToken cancellationToken)
    {
        EnsureRadarrConfigured();
        using var movies = await GetJsonAsync("api/v3/movie", options.Radarr.ApiKey, cancellationToken);
        using var queue = await GetJsonAsync(
            "api/v3/queue?page=1&pageSize=50",
            options.Radarr.ApiKey,
            cancellationToken);
        return ArrJobMapper.Map(
            ServiceName,
            MediaLookupTypes.Movie,
            movies.RootElement,
            queue.RootElement,
            "movieId",
            "tmdbId",
            item => Boolean(item, "hasFile"));
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

    private static MediaLookupResult MapLookup(JsonElement item, IReadOnlyList<JsonElement> registered)
    {
        var tmdbId = GetInt32(item, "tmdbId");
        var title = GetString(item, "title") ?? "Untitled";
        var year = NullableInt32(item, "year");
        var existing = registered.FirstOrDefault(candidate =>
            tmdbId > 0 ? GetInt32(candidate, "tmdbId") == tmdbId : SameTitleAndYear(candidate, title, year));
        var alreadyRegistered = existing.ValueKind == JsonValueKind.Object;
        return new(
            "Radarr",
            tmdbId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            title,
            GetString(item, "originalTitle"),
            year,
            LimitOverview(GetString(item, "overview")),
            null,
            NullableInt32(item, "runtime"),
            GetString(item, "status"),
            MediaLookupTypes.Movie,
            alreadyRegistered ? MediaLookupStates.AlreadyRegistered : MediaLookupStates.External,
            HasPoster(item),
            alreadyRegistered,
            alreadyRegistered ? GetInt32(existing, "id").ToString(System.Globalization.CultureInfo.InvariantCulture) : null,
            tmdbId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            PosterUrl(item),
            alreadyRegistered ? Boolean(existing, "monitored") : null,
            !alreadyRegistered,
            alreadyRegistered ? MediaProviderErrorCodes.AlreadyExists : "canRequest");
    }

    private static ProviderOption[] MapProviderOptions(JsonElement root, string label, bool includeValue) =>
        root.ValueKind != JsonValueKind.Array ? [] : root.EnumerateArray()
            .Where(item => GetInt32(item, "id") > 0 && (!includeValue || Boolean(item, "accessible")))
            .Select((item, index) => new ProviderOption(
                GetInt32(item, "id"),
                includeValue ? GetString(item, "path") ?? string.Empty : GetInt32(item, "id").ToString(System.Globalization.CultureInfo.InvariantCulture),
                includeValue ? $"{label} {index + 1}" : GetString(item, "name") ?? $"{label} {index + 1}",
                includeValue && item.TryGetProperty("freeSpace", out var free) && free.TryGetInt64(out var bytes) ? bytes : null))
            .ToArray();

    private static bool SameTitleAndYear(JsonElement item, string title, int? year) =>
        string.Equals(GetString(item, "title"), title, StringComparison.OrdinalIgnoreCase)
        && (year is null || NullableInt32(item, "year") == year);

    private static string? LimitOverview(string? overview) =>
        overview is null || overview.Length <= 500 ? overview : string.Concat(overview.AsSpan(0, 497), "...");

    private static string? PosterUrl(JsonElement item) => MediaPosterToken.Create(ArrPosterUrl.Get(item));

    private void EnsureRadarrConfigured()
    {
        if (string.IsNullOrWhiteSpace(options.Radarr.ApiKey))
            throw new MediaRequestException(
                MediaRequestErrors.ProviderUnavailable,
                "Radarr is not configured.",
                StatusCodes.Status503ServiceUnavailable);
    }
}

internal static class ArrPosterUrl
{
    public static string? Get(JsonElement item)
    {
        if (!item.TryGetProperty("images", out var images) || images.ValueKind != JsonValueKind.Array)
            return null;
        var value = images.EnumerateArray()
            .Where(image => string.Equals(String(image, "coverType"), "poster", StringComparison.OrdinalIgnoreCase))
            .Select(image => String(image, "remoteUrl"))
            .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(uri.UserInfo)
            || uri.IsLoopback
            || string.IsNullOrWhiteSpace(uri.Host)
            || !string.IsNullOrEmpty(uri.Fragment)
            || System.Net.IPAddress.TryParse(uri.Host, out _)
            || uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            return null;
        return uri.AbsoluteUri;
    }

    private static string? String(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
