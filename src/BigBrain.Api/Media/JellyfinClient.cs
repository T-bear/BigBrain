using System.Diagnostics;

namespace BigBrain.Api.Media;

public sealed class JellyfinClient(HttpClient httpClient, MediaOptions options)
    : MediaClientBase(httpClient, "Jellyfin"), IJellyfinClient, IMediaSearchProvider, IMediaLibraryCatalog, IJellyfinPlaybackClient
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
                "Items?Recursive=true&Limit=8&Fields=DateCreated&IncludeItemTypes=Movie,Series,Episode"
                    + "&SortBy=DateCreated&SortOrder=Descending",
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
            var recentlyAdded = recent is not null
                ? Items(recent.RootElement).Take(8).Select(item => new RecentlyAddedMedia(
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

    async Task<JellyfinCatalogItem?> IMediaLibraryCatalog.FindByForeignIdAsync(
        string provider,
        string foreignId,
        string mediaType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Jellyfin.ApiKey))
        {
            return null;
        }

        var providerKey = string.Equals(provider, "Sonarr", StringComparison.OrdinalIgnoreCase)
            ? "tvdb"
            : string.Equals(provider, "Radarr", StringComparison.OrdinalIgnoreCase) ? "tmdb" : null;
        if (providerKey is null)
        {
            return null;
        }

        var itemType = mediaType == MediaLookupTypes.Series ? "Series" : "Movie";
        using var response = await GetJellyfinJsonAsync(
            $"Items?Recursive=true&IncludeItemTypes={itemType}"
            + $"&AnyProviderIdEquals={providerKey}.{Uri.EscapeDataString(foreignId)}"
            + "&Fields=ProviderIds,DateCreated,ImageTags&Limit=1",
            options.Jellyfin.ApiKey,
            cancellationToken);
        return Items(response.RootElement).Select(MapCatalogItem).FirstOrDefault();
    }

    async Task<JellyfinCatalogItem?> IMediaLibraryCatalog.GetPlayItemAsync(
        string itemId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Jellyfin.ApiKey))
        {
            return null;
        }

        try
        {
            using var response = await GetJellyfinJsonAsync(
                $"Items?Ids={Uri.EscapeDataString(itemId)}&Fields=ProviderIds,DateCreated,ImageTags&Limit=1",
                options.Jellyfin.ApiKey,
                cancellationToken);
            var item = Items(response.RootElement).FirstOrDefault();
            var mediaType = item.ValueKind == System.Text.Json.JsonValueKind.Object
                ? NormalizeMediaType(GetString(item, "Type"))
                : MediaTypes.Unknown;
            return mediaType is MediaLookupTypes.Series or MediaLookupTypes.Movie
                ? MapCatalogItem(item)
                : null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    async Task<IReadOnlyList<JellyfinCatalogItem>> IMediaLibraryCatalog.GetAvailableCatalogAsync(
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Jellyfin.ApiKey))
        {
            return [];
        }

        using var response = await GetJellyfinJsonAsync(
            "Items?Recursive=true&Limit=500&Fields=ProviderIds,DateCreated,ImageTags&IncludeItemTypes=Movie,Series",
            options.Jellyfin.ApiKey,
            cancellationToken);
        return Items(response.RootElement)
            .Select(MapCatalogItem)
            .Where(item => item.MediaType is MediaLookupTypes.Series or MediaLookupTypes.Movie)
            .ToArray();
    }

    async Task<IReadOnlyList<SmartShuffleSeriesOption>> IJellyfinPlaybackClient.GetSeriesAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        using var response = await GetJellyfinJsonAsync(
            $"Items?UserId={Uri.EscapeDataString(userId)}&Recursive=true&IncludeItemTypes=Series&Limit=200",
            options.Jellyfin.ApiKey!, cancellationToken);
        return Items(response.RootElement).Select(item => new SmartShuffleSeriesOption(
            GetString(item, "Id") ?? string.Empty,
            GetString(item, "Name") ?? "Namnlös serie",
            true)).Where(item => item.Id.Length > 0).ToArray();
    }

    async Task<SmartShuffleEpisode?> IJellyfinPlaybackClient.GetNextEpisodeAsync(
        string seriesId,
        string userId,
        CancellationToken cancellationToken)
    {
        using var response = await GetJellyfinJsonAsync(
            $"Shows/{Uri.EscapeDataString(seriesId)}/Episodes?UserId={Uri.EscapeDataString(userId)}"
            + "&Fields=UserData,MediaSources&EnableUserData=true&Limit=1000",
            options.Jellyfin.ApiKey!, cancellationToken);
        var episodes = Items(response.RootElement);
        if (episodes.Any(item => !item.TryGetProperty("UserData", out var data)
            || data.ValueKind != System.Text.Json.JsonValueKind.Object))
            throw new SmartShuffleException("userDataUnavailable", "Jellyfin saknar tillförlitlig användarstatus för serien.", 503);
        return episodes
            .Where(item => GetNullableInt32(item, "ParentIndexNumber") is > 0)
            .Where(item => item.TryGetProperty("UserData", out var data)
                && data.ValueKind == System.Text.Json.JsonValueKind.Object
                && !Boolean(data, "Played"))
            .Where(item => item.TryGetProperty("MediaSources", out var sources)
                && sources.ValueKind == System.Text.Json.JsonValueKind.Array
                && sources.GetArrayLength() > 0)
            .OrderBy(item => GetNullableInt32(item, "ParentIndexNumber") ?? int.MaxValue)
            .ThenBy(item => GetNullableInt32(item, "IndexNumber") ?? int.MaxValue)
            .ThenBy(item => GetString(item, "Id"), StringComparer.Ordinal)
            .Select(item =>
            {
                var userData = item.GetProperty("UserData");
                var position = GetInt64(userData, "PlaybackPositionTicks");
                return new SmartShuffleEpisode(
                    GetString(item, "Id") ?? string.Empty,
                    seriesId,
                    GetString(item, "SeriesName") ?? "Namnlös serie",
                    GetString(item, "Name") ?? "Namnlöst avsnitt",
                    GetNullableInt32(item, "ParentIndexNumber") ?? 0,
                    GetNullableInt32(item, "IndexNumber") ?? 0,
                    position > 0 ? position : null);
            })
            .FirstOrDefault(episode => episode.Id.Length > 0);
    }

    async Task<IReadOnlyList<JellyfinRemoteSession>> IJellyfinPlaybackClient.GetRemoteSessionsAsync(CancellationToken cancellationToken)
    {
        using var response = await GetJellyfinJsonAsync("Sessions", options.Jellyfin.ApiKey!, cancellationToken);
        return response.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array ? [] : response.RootElement.EnumerateArray()
            .Select(item => new JellyfinRemoteSession(
                GetString(item, "Id") ?? string.Empty,
                GetString(item, "UserId") ?? string.Empty,
                GetString(item, "DeviceName") ?? "Jellyfin-enhet",
                GetString(item, "Client") ?? "Jellyfin",
                Boolean(item, "SupportsRemoteControl"),
                item.TryGetProperty("NowPlayingItem", out var playing) && playing.ValueKind == System.Text.Json.JsonValueKind.Object))
            .Where(item => item.SessionId.Length > 0).ToArray();
    }

    async Task IJellyfinPlaybackClient.PlayNowAsync(string sessionId, string itemId, long? startPositionTicks, CancellationToken cancellationToken)
    {
        var path = $"Sessions/{Uri.EscapeDataString(sessionId)}/Playing?playCommand=PlayNow&itemIds={Uri.EscapeDataString(itemId)}";
        if (startPositionTicks is > 0) path += $"&startPositionTicks={startPositionTicks.Value}";
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.TryAddWithoutValidation("X-Emby-Token", options.Jellyfin.ApiKey!);
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode) return;
        var status = (int)response.StatusCode;
        throw response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden =>
                new JellyfinPlaybackException("playbackAuthenticationFailure", "Jellyfin-autentiseringen för uppspelning misslyckades.", 503),
            System.Net.HttpStatusCode.NotFound =>
                new JellyfinPlaybackException("playbackTargetUnavailable", "TV-sessionen eller avsnittet finns inte längre.", 409),
            System.Net.HttpStatusCode.BadRequest =>
                new JellyfinPlaybackException("playbackRejected", "Jellyfin avvisade uppspelningskommandot.", 502),
            _ => new JellyfinPlaybackException("playbackProviderFailure", "Jellyfin kunde inte starta avsnittet.", status >= 500 ? 503 : 502)
        };
    }

    async Task<JellyfinPlaybackStatus> IJellyfinPlaybackClient.GetPlaybackStatusAsync(string sessionId, CancellationToken cancellationToken)
    {
        using var response = await GetJellyfinJsonAsync("Sessions", options.Jellyfin.ApiKey!, cancellationToken);
        var session = response.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array
            ? response.RootElement.EnumerateArray().FirstOrDefault(item => GetString(item, "Id") == sessionId)
            : default;
        if (session.ValueKind != System.Text.Json.JsonValueKind.Object) return new(false, null, false);
        var itemId = session.TryGetProperty("NowPlayingItem", out var playing) && playing.ValueKind == System.Text.Json.JsonValueKind.Object
            ? GetString(playing, "Id") : null;
        var paused = session.TryGetProperty("PlayState", out var state) && state.ValueKind == System.Text.Json.JsonValueKind.Object && Boolean(state, "IsPaused");
        return new(true, itemId, paused);
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

    private static System.Text.Json.JsonElement[] Items(System.Text.Json.JsonElement root)
    {
        if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            return root.EnumerateArray().ToArray();
        }
        return root.TryGetProperty("Items", out var items)
            && items.ValueKind == System.Text.Json.JsonValueKind.Array
            ? items.EnumerateArray().ToArray()
            : [];
    }

    private static JellyfinCatalogItem MapCatalogItem(System.Text.Json.JsonElement item)
    {
        var providerIds = item.TryGetProperty("ProviderIds", out var values)
            && values.ValueKind == System.Text.Json.JsonValueKind.Object ? values : default;
        return new(
            GetString(item, "Id") ?? string.Empty,
            GetString(item, "Name") ?? "Untitled",
            NormalizeMediaType(GetString(item, "Type")),
            GetString(providerIds, "Tvdb"),
            GetString(providerIds, "Tmdb"),
            DateTimeOffset.TryParse(GetString(item, "DateCreated"), out var added) ? added : null,
            HasPrimaryImage(item));
    }
}
