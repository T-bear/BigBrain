using System.Diagnostics;

namespace BigBrain.Api.Media;

public sealed class JellyfinClient(HttpClient httpClient, MediaOptions options)
    : MediaClientBase(httpClient, "Jellyfin"), IJellyfinClient, IMediaSearchProvider
{
    public async Task<MediaSearchProviderResult> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Jellyfin.ApiKey))
        {
            return new(ServiceName, MediaStatuses.NotConfigured, "Provider credentials are not configured.", []);
        }

        try
        {
            var requestUri = $"Items?Recursive=true&SearchTerm={Uri.EscapeDataString(query)}"
                + $"&Limit={limit}&IncludeItemTypes=Movie,Series,Season,Episode"
                + "&Fields=ProductionYear,ChildCount,RecursiveItemCount,ImageTags";
            using var response = await GetJellyfinJsonAsync(requestUri, options.Jellyfin.ApiKey, cancellationToken);
            var root = response.RootElement;
            var items = root.TryGetProperty("Items", out var values)
                && values.ValueKind == System.Text.Json.JsonValueKind.Array
                ? values.EnumerateArray()
                    .Take(limit)
                    .Select(item => new MediaSearchResult(
                        GetString(item, "Id") ?? string.Empty,
                        GetString(item, "Name") ?? "Untitled",
                        item.TryGetProperty("ProductionYear", out var year) && year.TryGetInt32(out var yearValue)
                            ? yearValue
                            : null,
                        NormalizeMediaType(GetString(item, "Type")),
                        MediaSearchStates.Available,
                        null,
                        new MediaSearchMetadata(
                            SeasonCount: GetNullableInt32(item, "ChildCount"),
                            EpisodeCount: GetNullableInt32(item, "RecursiveItemCount"),
                            AvailableInLibrary: true,
                            ImageAvailable: HasPrimaryImage(item))))
                    .ToArray()
                : [];
            return new(ServiceName, MediaStatuses.Online, null, items);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return SearchFailure(exception);
        }
    }

    public async Task<JellyfinOverview> GetOverviewAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Jellyfin.ApiKey))
        {
            return Empty(NotConfigured());
        }

        var timer = Stopwatch.StartNew();
        try
        {
            using var status = await GetJellyfinJsonAsync("System/Info", options.Jellyfin.ApiKey, cancellationToken);
            var version = GetString(status.RootElement, "Version");
            var librariesResult = await TryGetSupplementalJsonAsync(
                "Library/VirtualFolders",
                options.Jellyfin.ApiKey,
                cancellationToken);
            using var libraries = librariesResult.Document;
            var countsResult = await TryGetSupplementalJsonAsync(
                "Items/Counts",
                options.Jellyfin.ApiKey,
                cancellationToken);
            using var counts = countsResult.Document;
            var sessionsResult = await TryGetSupplementalJsonAsync(
                "Sessions",
                options.Jellyfin.ApiKey,
                cancellationToken);
            using var sessions = sessionsResult.Document;
            var recentResult = await TryGetSupplementalJsonAsync(
                "Items/Latest?Limit=8&Fields=DateCreated&IncludeItemTypes=Movie,Series,Episode",
                options.Jellyfin.ApiKey,
                cancellationToken);
            using var recent = recentResult.Document;

            var sessionItems = sessions?.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array
                ? sessions.RootElement.EnumerateArray().ToArray()
                : [];
            var activeStreams = sessionItems.Count(item =>
                    item.TryGetProperty("NowPlayingItem", out var playing)
                    && playing.ValueKind == System.Text.Json.JsonValueKind.Object);
            var activeUsers = sessionItems
                .Where(item => item.TryGetProperty("UserId", out var userId) && userId.ValueKind == System.Text.Json.JsonValueKind.String)
                .Select(item => GetString(item, "UserId"))
                .Where(userId => userId is not null)
                .Distinct(StringComparer.Ordinal)
                .Count();
            var recentlyAdded = recent?.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array
                ? recent.RootElement.EnumerateArray().Take(8).Select(item => new RecentlyAddedMedia(
                    GetString(item, "Name") ?? "Untitled",
                    GetString(item, "Type") ?? "Unknown",
                    DateTimeOffset.TryParse(GetString(item, "DateCreated"), out var date) ? date : null)).ToArray()
                : [];
            var supplementalDataComplete = librariesResult.Succeeded
                && countsResult.Succeeded
                && sessionsResult.Succeeded
                && recentResult.Succeeded;
            var serviceStatus = supplementalDataComplete
                ? Online(version, timer)
                : new MediaServiceStatus(
                    ServiceName,
                    MediaStatuses.Degraded,
                    version,
                    timer.ElapsedMilliseconds,
                    DateTimeOffset.UtcNow,
                    "Some Jellyfin dashboard data could not be loaded.",
                    true);
            return new JellyfinOverview(
                serviceStatus,
                libraries is null ? 0 : ArrayLength(libraries.RootElement),
                counts is null ? 0 : GetInt32(counts.RootElement, "MovieCount"),
                counts is null ? 0 : GetInt32(counts.RootElement, "SeriesCount"),
                counts is null ? 0 : GetInt32(counts.RootElement, "EpisodeCount"),
                activeUsers,
                activeStreams,
                recentlyAdded);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Empty(Failure(exception, timer));
        }
    }

    private async Task<(System.Text.Json.JsonDocument? Document, bool Succeeded)> TryGetSupplementalJsonAsync(
        string path,
        string apiKey,
        CancellationToken cancellationToken)
    {
        try
        {
            return (await GetJellyfinJsonAsync(path, apiKey, cancellationToken), true);
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException
            || !cancellationToken.IsCancellationRequested)
        {
            return (null, false);
        }
    }

    private async Task<System.Text.Json.JsonDocument> GetJellyfinJsonAsync(
        string path,
        string apiKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("X-Emby-Token", apiKey);
        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        EnsureSuccess(response);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await System.Text.Json.JsonDocument.ParseAsync(
            stream,
            new System.Text.Json.JsonDocumentOptions { MaxDepth = 32 },
            cancellationToken);
    }

    private static JellyfinOverview Empty(MediaServiceStatus status) => new(status, 0, 0, 0, 0, 0, 0, []);

    private static string NormalizeMediaType(string? type) => type?.ToLowerInvariant() switch
    {
        "movie" => MediaTypes.Movie,
        "series" => MediaTypes.Series,
        "season" => MediaTypes.Season,
        "episode" => MediaTypes.Episode,
        _ => MediaTypes.Unknown
    };

    private static int? GetNullableInt32(System.Text.Json.JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.TryGetInt32(out var result) ? result : null;

    private static bool HasPrimaryImage(System.Text.Json.JsonElement item) =>
        item.TryGetProperty("ImageTags", out var tags)
        && tags.ValueKind == System.Text.Json.JsonValueKind.Object
        && tags.TryGetProperty("Primary", out var primary)
        && primary.ValueKind == System.Text.Json.JsonValueKind.String;
}
