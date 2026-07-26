using BigBrain.Api.Media;
using System.Net;
using System.Text;

namespace BigBrain.Api.Tests;

public sealed class MediaJobsTests
{
    private static readonly MediaJobsQuery AllJobs = new(null, null, null, true, 100);

    [Theory]
    [InlineData("queued", null, null, false, MediaJobStatuses.Queued)]
    [InlineData("searching", null, null, false, MediaJobStatuses.Searching)]
    [InlineData("downloading", null, null, false, MediaJobStatuses.Downloading)]
    [InlineData("completed", "ok", "importPending", false, MediaJobStatuses.Importing)]
    [InlineData("downloading", "warning", null, false, MediaJobStatuses.Failed)]
    [InlineData("futureProviderState", null, null, false, MediaJobStatuses.Unknown)]
    public void NormalizesSonarrAndRadarrStates(
        string status,
        string? trackedStatus,
        string? trackedState,
        bool hasError,
        string expected)
    {
        Assert.Equal(
            expected,
            ArrJobMapper.NormalizeArrStatus(status, trackedStatus, trackedState, hasError));
    }

    [Theory]
    [InlineData("downloading", 0.68, MediaJobStatuses.Downloading)]
    [InlineData("stalledDL", 0.68, MediaJobStatuses.Stalled)]
    [InlineData("queuedDL", 0, MediaJobStatuses.Queued)]
    [InlineData("stoppedUP", 1, MediaJobStatuses.Completed)]
    [InlineData("uploading", 1, MediaJobStatuses.Completed)]
    [InlineData("futureState", 0.2, MediaJobStatuses.Unknown)]
    public void NormalizesQBittorrentStates(string state, double progress, string expected)
    {
        Assert.Equal(expected, QBittorrentClient.NormalizeJobStatus(state, progress));
    }

    [Fact]
    public async Task CompletedDownloadRemainsCompletedWhileWaitingForImport()
    {
        var service = Service(
            new StubJobsProvider("qBittorrent", "unknown", [Job(
                "torrent-1", "torrent:theexpanse:s1", "The Expanse",
                MediaJobStatuses.Completed, "season", "Season 1")]),
            new StubCatalog());

        var result = Assert.Single((await service.GetJobsAsync(AllJobs, Token)).Jobs);

        Assert.Equal(MediaJobStatuses.Completed, result.Status);
        Assert.False(result.CanPlay);
        Assert.Null(result.PlayItemId);
    }

    [Fact]
    public async Task AggregatesEpisodesInTheSameSeason()
    {
        var jobs = new[]
        {
            Job("280619", "Sonarr:series:280619:s10", "Doctor Who", MediaJobStatuses.Importing,
                "season", "Season 10", 1, 100),
            Job("280619", "Sonarr:series:280619:s10", "Doctor Who", MediaJobStatuses.Downloading,
                "season", "Season 10", 2, 50)
        };
        var service = Service(new StubJobsProvider("Sonarr", "series", jobs), new StubCatalog());

        var result = Assert.Single(
            (await service.GetJobsAsync(AllJobs, Token)).Jobs,
            job => job.MediaType == "season");

        Assert.Equal("Season 10", result.Subtitle);
        Assert.Equal(2, result.EpisodeCount);
        Assert.Equal(1, result.CompletedEpisodeCount);
        Assert.Equal(2, result.Details.Count);
    }

    [Fact]
    public async Task DeduplicatesQBittorrentAndSonarrDeterministically()
    {
        var sonarr = new StubJobsProvider("Sonarr", "series", [Job(
            "280619", "Sonarr:series:280619:s1", "The Expanse",
            MediaJobStatuses.Importing, "season", "Season 1", 1, 100)]);
        var qbittorrent = new StubJobsProvider("qBittorrent", "unknown", [Job(
            "torrent-1", "qbit:theexpanse:s1", "The Expanse",
            MediaJobStatuses.Completed, "season", "The.Expanse.S01E01", 1, 100)]);
        var service = Service([sonarr, qbittorrent], new StubCatalog());

        var result = Assert.Single((await service.GetJobsAsync(AllJobs, Token)).Jobs);

        Assert.Equal(MediaJobStatuses.Importing, result.Status);
        Assert.Equal(2, result.Details.Count);
        Assert.Contains(result.Details, detail => detail.Provider == "Sonarr");
        Assert.Contains(result.Details, detail => detail.Provider == "qBittorrent");
    }

    [Fact]
    public async Task StableJellyfinMatchTransitionsMovieToAvailableAndEnablesPlay()
    {
        var item = CatalogItem("abc123", "Inception", "movie", tmdbId: "27205");
        var service = Service(
            new StubJobsProvider("Radarr", "movie", [Job(
                "27205", "Radarr:movie:27205", "Inception", MediaJobStatuses.Importing, "movie")]),
            new StubCatalog([item]));

        var response = await service.GetJobsAsync(AllJobs, Token);
        var result = Assert.Single(response.Jobs);
        var play = await service.GetPlayAsync(result.PlayItemId!, Token);
        Assert.Equal(MediaJobStatuses.Available, result.Status);
        Assert.Equal(100, result.ProgressPercent);
        Assert.True(result.CanPlay);
        Assert.Matches("^[a-f0-9]{24}$", result.PlayItemId);
        Assert.NotEqual("abc123", result.PlayItemId);
        Assert.NotNull(play);
        Assert.Equal(result.PlayItemId, play.JellyfinItemId);
        Assert.Equal("/jellyfin/web/index.html#!/details?id=abc123", play.PlayUrl);
    }

    [Fact]
    public async Task InvalidOrStalePlayIdDoesNotResolve()
    {
        var service = Service(
            new StubJobsProvider("Radarr", "movie", [Job(
                "27205", "Radarr:movie:27205", "Inception", MediaJobStatuses.Importing, "movie")]),
            new StubCatalog([CatalogItem("abc123", "Inception", "movie", tmdbId: "27205")]));

        await service.GetJobsAsync(AllJobs, Token);

        Assert.Null(await service.GetPlayAsync("0123456789abcdef01234567", Token));
    }

    [Fact]
    public async Task SearchResultWithoutJellyfinMatchCannotPlay()
    {
        var service = Service(
            new StubJobsProvider(
                "Radarr",
                "movie",
                [Job("27205", "Radarr:movie:27205", "Inception", MediaJobStatuses.Completed, "movie")],
                [new("27205", "Inception", "movie", true)]),
            new StubCatalog());

        var result = Assert.Single((await service.GetJobsAsync(AllJobs, Token)).Jobs);

        Assert.False(result.CanPlay);
        Assert.Null(result.PlayItemId);
    }

    [Fact]
    public async Task SeasonIsNotMarkedAvailableFromSeriesLevelJellyfinMatch()
    {
        var item = CatalogItem("series123", "The Expanse", "series", tvdbId: "280619");
        var service = Service(
            new StubJobsProvider("Sonarr", "series", [Job(
                "280619", "Sonarr:series:280619:s1", "The Expanse",
                MediaJobStatuses.Importing, "season", "Season 1")]),
            new StubCatalog([item]));

        var result = Assert.Single(
            (await service.GetJobsAsync(AllJobs, Token)).Jobs,
            job => job.MediaType == "season");

        Assert.Equal(MediaJobStatuses.Importing, result.Status);
        Assert.False(result.CanPlay);
    }

    [Fact]
    public async Task JellyfinFailureProducesDegradedResponseWithoutRemovingProviderJobs()
    {
        var service = Service(
            new StubJobsProvider("Radarr", "movie", [Job(
                "27205", "Radarr:movie:27205", "Inception", MediaJobStatuses.Downloading, "movie")]),
            new StubCatalog(throwOnCatalog: true));

        var response = await service.GetJobsAsync(AllJobs, Token);

        Assert.Equal("degraded", response.Status);
        Assert.Single(response.Jobs);
        var jellyfin = Assert.Single(response.Providers, provider => provider.Provider == "Jellyfin");
        Assert.Equal(MediaStatuses.Unavailable, jellyfin.Status);
        Assert.DoesNotContain("http", jellyfin.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OneProviderFailureIsIsolated()
    {
        var service = Service(
            [
                new StubJobsProvider("Sonarr", "series", [Job(
                    "280619", "Sonarr:series:280619", "The Expanse", MediaJobStatuses.Queued, "series")]),
                new ThrowingJobsProvider("Radarr", "movie")
            ],
            new StubCatalog());

        var response = await service.GetJobsAsync(AllJobs, Token);

        Assert.Equal("degraded", response.Status);
        Assert.Single(response.Jobs);
        Assert.Equal(MediaStatuses.Unavailable,
            Assert.Single(response.Providers, provider => provider.Provider == "Radarr").Status);
    }

    [Fact]
    public async Task ProviderTimeoutIsSanitizedAndIsolated()
    {
        var service = Service(
            [
                new StubJobsProvider("Sonarr", "series", [Job(
                    "280619", "Sonarr:series:280619", "The Expanse", MediaJobStatuses.Queued, "series")]),
                new TimeoutJobsProvider()
            ],
            new StubCatalog());

        var response = await service.GetJobsAsync(AllJobs, Token);

        Assert.Equal("degraded", response.Status);
        Assert.Single(response.Jobs);
        var provider = Assert.Single(response.Providers, item => item.Provider == "Radarr");
        Assert.Equal(MediaStatuses.Unavailable, provider.Status);
        Assert.Equal("Provider data is temporarily unavailable.", provider.UserMessage);
    }

    [Fact]
    public async Task CachePreventsDuplicateProviderReadsWithinTtl()
    {
        var provider = new StubJobsProvider("Sonarr", "series", []);
        var catalog = new StubCatalog();
        var service = Service(provider, catalog);

        await service.GetJobsAsync(AllJobs, Token);
        await service.GetJobsAsync(AllJobs, Token);

        Assert.Equal(1, provider.CallCount);
        Assert.Equal(1, catalog.CatalogCallCount);
    }

    [Fact]
    public async Task FiltersLimitAndOpaqueDetailLookupAreApplied()
    {
        var provider = new StubJobsProvider("Sonarr", "series",
        [
            Job("1", "Sonarr:series:1", "One", MediaJobStatuses.Downloading, "series"),
            Job("2", "Sonarr:series:2", "Two", MediaJobStatuses.Failed, "series")
        ]);
        var service = Service(provider, new StubCatalog());

        var response = await service.GetJobsAsync(
            new(MediaJobStatuses.Downloading, "series", "sonarr", true, 1), Token);
        var result = Assert.Single(response.Jobs);
        var detail = await service.GetJobAsync(result.Id, Token);

        Assert.Matches("^[a-f0-9]{24}$", result.Id);
        Assert.Equal(result, detail);
        Assert.DoesNotContain("Sonarr:series", result.Id, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancellationIsPropagatedToProviders()
    {
        using var cancellation = new CancellationTokenSource();
        var provider = new CancellingJobsProvider();
        var service = Service(provider, new StubCatalog());

        var request = service.GetJobsAsync(AllJobs, cancellation.Token);
        await provider.Started.Task.WaitAsync(Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => request);

        Assert.Equal(cancellation.Token, provider.ObservedToken);
    }

    [Fact]
    public async Task SonarrRadarrAndQBittorrentAdaptersUseOnlyGet()
    {
        var sonarrHandler = new RecordingHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/v3/series" => Json("""[{"id":7,"tvdbId":280619,"title":"The Expanse","statistics":{"episodeFileCount":0}}]"""),
            "/api/v3/queue" => Json("""{"records":[{"seriesId":7,"title":"The.Expanse.S01E01","status":"downloading","size":100,"sizeleft":32,"added":"2026-07-25T10:00:00Z"}]}"""),
            _ => throw new InvalidOperationException()
        });
        var radarrHandler = new RecordingHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/v3/movie" => Json("""[{"id":9,"tmdbId":27205,"title":"Inception","hasFile":false}]"""),
            "/api/v3/queue" => Json("""{"records":[{"movieId":9,"status":"queued","size":100,"sizeleft":100}]}"""),
            _ => throw new InvalidOperationException()
        });
        var qbitHandler = new RecordingHandler(_ => Json(
            """[{"hash":"opaque-hash","name":"The.Expanse.S01E01","state":"downloading","progress":0.5,"size":100,"dlspeed":10,"upspeed":0,"eta":60,"added_on":1}]"""));
        var options = new MediaOptions
        {
            Sonarr = new MediaApiKeyOptions("http://sonarr.test/") { ApiKey = "test-key" },
            Radarr = new MediaApiKeyOptions("http://radarr.test/") { ApiKey = "test-key" },
            QBittorrent = new QBittorrentOptions
            {
                BaseUrl = "http://qbittorrent.test/",
                ApiKey = "test-key"
            }
        };
        var providers = new IMediaJobsProvider[]
        {
            new SonarrClient(Client(sonarrHandler, "http://sonarr.test/"), options),
            new RadarrClient(Client(radarrHandler, "http://radarr.test/"), options),
            new QBittorrentClient(Client(qbitHandler, "http://qbittorrent.test/"), options)
        };

        await Task.WhenAll(providers.Select(provider => provider.GetJobsSnapshotAsync(Token)));

        Assert.All(
            sonarrHandler.Methods.Concat(radarrHandler.Methods).Concat(qbitHandler.Methods),
            method => Assert.Equal(HttpMethod.Get, method));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static ProviderMediaJob Job(
        string foreignId,
        string groupKey,
        string title,
        string status,
        string mediaType,
        string? subtitle = null,
        int? episode = null,
        double? progress = null) =>
        new(
            foreignId, groupKey, title, subtitle, mediaType, status, progress,
            null, null, null, null, episode, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            status == MediaJobStatuses.Failed ? "providerJobFailed" : null,
            status == MediaJobStatuses.Failed ? "The provider reported a job failure." : null,
            episode is null ? subtitle : $"Episode {episode}");

    private static JellyfinCatalogItem CatalogItem(
        string id,
        string title,
        string mediaType,
        string? tvdbId = null,
        string? tmdbId = null) =>
        new(id, title, mediaType, tvdbId, tmdbId, DateTimeOffset.UtcNow, false);

    private static MediaJobsService Service(
        IMediaJobsProvider provider,
        IMediaLibraryCatalog catalog) =>
        new([provider], catalog);

    private static MediaJobsService Service(
        IEnumerable<IMediaJobsProvider> providers,
        IMediaLibraryCatalog catalog) =>
        new(providers, catalog);

    private sealed class StubJobsProvider(
        string providerName,
        string mediaType,
        IReadOnlyList<ProviderMediaJob> jobs,
        IReadOnlyList<ProviderLibraryItem>? library = null) : IMediaJobsProvider
    {
        public string ProviderName => providerName;
        public string MediaType => mediaType;
        public int CallCount { get; private set; }

        public Task<MediaJobsProviderSnapshot> GetJobsSnapshotAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new MediaJobsProviderSnapshot(providerName, jobs, library ?? []));
        }
    }

    private sealed class ThrowingJobsProvider(string providerName, string mediaType) : IMediaJobsProvider
    {
        public string ProviderName => providerName;
        public string MediaType => mediaType;

        public Task<MediaJobsProviderSnapshot> GetJobsSnapshotAsync(CancellationToken cancellationToken) =>
            throw new HttpRequestException("Raw provider error with http://internal:8989");
    }

    private sealed class TimeoutJobsProvider : IMediaJobsProvider
    {
        public string ProviderName => "Radarr";
        public string MediaType => "movie";

        public Task<MediaJobsProviderSnapshot> GetJobsSnapshotAsync(CancellationToken cancellationToken) =>
            throw new TaskCanceledException("Raw provider timeout");
    }

    private sealed class StubCatalog(
        IReadOnlyList<JellyfinCatalogItem>? items = null,
        bool throwOnCatalog = false) : IMediaLibraryCatalog
    {
        private readonly IReadOnlyList<JellyfinCatalogItem> items = items ?? [];
        public int CatalogCallCount { get; private set; }

        public Task<JellyfinCatalogItem?> FindByForeignIdAsync(
            string provider,
            string foreignId,
            string mediaType,
            CancellationToken cancellationToken) =>
            Task.FromResult(items.FirstOrDefault(item =>
                string.Equals(provider, "Sonarr", StringComparison.OrdinalIgnoreCase)
                    ? item.TvdbId == foreignId
                    : item.TmdbId == foreignId));

        public Task<JellyfinCatalogItem?> GetPlayItemAsync(
            string itemId,
            CancellationToken cancellationToken) =>
            Task.FromResult(items.FirstOrDefault(item => item.ItemId == itemId));

        public Task<IReadOnlyList<JellyfinCatalogItem>> GetAvailableCatalogAsync(
            CancellationToken cancellationToken)
        {
            CatalogCallCount++;
            return throwOnCatalog
                ? throw new HttpRequestException("Raw Jellyfin error")
                : Task.FromResult(items);
        }
    }

    private sealed class CancellingJobsProvider : IMediaJobsProvider
    {
        public string ProviderName => "Sonarr";
        public string MediaType => "series";
        public CancellationToken ObservedToken { get; private set; }
        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<MediaJobsProviderSnapshot> GetJobsSnapshotAsync(CancellationToken cancellationToken)
        {
            ObservedToken = cancellationToken;
            Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException();
        }
    }

    private static HttpClient Client(HttpMessageHandler handler, string baseUrl) =>
        new(handler) { BaseAddress = new Uri(baseUrl) };

    private static HttpResponseMessage Json(string value) =>
        new(HttpStatusCode.OK) { Content = new StringContent(value, Encoding.UTF8, "application/json") };

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpMethod> Methods { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Methods.Add(request.Method);
            return Task.FromResult(responder(request));
        }
    }
}
