using System.Net;
using System.Text;
using System.Text.Json;
using BigBrain.Api.Media;

namespace BigBrain.Api.Tests;

public sealed class MediaClientTests
{
    [Fact]
    public async Task JellyfinMapsSuccessfulReadOnlyResponses()
    {
        var client = new JellyfinClient(CreateClient(request => Json(request.RequestUri!.AbsolutePath switch
        {
            "/System/Info" => """{"Version":"10.10.7"}""",
            "/Library/VirtualFolders" => """[{"Name":"Movies"},{"Name":"TV"}]""",
            "/Items/Counts" => """{"MovieCount":12,"SeriesCount":4,"EpisodeCount":80}""",
            "/Sessions" => """[{"UserId":"one","NowPlayingItem":{"Name":"Private title"}},{"UserId":"two","UserName":"must not leak"}]""",
            "/Items/Latest" => """[{"Name":"New movie","Type":"Movie","DateCreated":"2026-07-23T10:00:00Z"}]""",
            _ => throw new InvalidOperationException()
        })), Options());

        var result = await client.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal(MediaStatuses.Online, result.Service.Status);
        Assert.Equal("10.10.7", result.Service.Version);
        Assert.Equal(2, result.LibraryCount);
        Assert.Equal(12, result.MovieCount);
        Assert.Equal(4, result.SeriesCount);
        Assert.Equal(80, result.EpisodeCount);
        Assert.Equal(2, result.ActiveUserCount);
        Assert.Equal(1, result.ActiveStreamCount);
        Assert.Single(result.RecentlyAdded);
        Assert.DoesNotContain("Private", JsonSerializer.Serialize(result), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SonarrMapsSuccessfulReadOnlyResponses()
    {
        var client = new SonarrClient(CreateClient(request => Json(ArrResponse(request.RequestUri!, true))), Options());

        var result = await client.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal(MediaStatuses.Online, result.Service.Status);
        Assert.Equal(2, result.SeriesCount);
        Assert.Equal(1, result.MonitoredSeriesCount);
        Assert.Equal(3, result.MissingMonitoredEpisodes);
        Assert.Single(result.Calendar);
        Assert.Equal(1, result.QueueCount);
        Assert.Single(result.Queue);
        Assert.Single(result.RecentHistory);
        Assert.Single(result.HealthWarnings);
    }

    [Fact]
    public async Task RadarrMapsSuccessfulReadOnlyResponses()
    {
        var client = new RadarrClient(CreateClient(request => Json(ArrResponse(request.RequestUri!, false))), Options());

        var result = await client.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal(MediaStatuses.Online, result.Service.Status);
        Assert.Equal(2, result.MovieCount);
        Assert.Equal(1, result.MonitoredMovieCount);
        Assert.Equal(3, result.MissingMovieCount);
        Assert.Equal(1, result.QualityUpgradeCount);
        Assert.Single(result.Queue);
    }

    [Fact]
    public async Task ProwlarrMapsSuccessfulReadOnlyResponses()
    {
        var client = new ProwlarrClient(CreateClient(request => Json(request.RequestUri!.AbsolutePath switch
        {
            "/api/v1/system/status" => """{"version":"1.35.1"}""",
            "/api/v1/indexer" => """[{"name":"Public","enable":true},{"name":"Disabled","enable":false}]""",
            "/api/v1/indexerstatus" => """[]""",
            "/api/v1/health" => """[{"message":"Indexer check failed"}]""",
            "/api/v1/applications" => """[{"name":"Sonarr"},{"name":"Radarr"},{"name":"Other"}]""",
            "/api/v1/history" => """{"records":[{"sourceTitle":"Failed query","eventType":"indexerQueryFailure","date":"2026-07-23T10:00:00Z"}]}""",
            _ => throw new InvalidOperationException()
        })), Options());

        var result = await client.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal(MediaStatuses.Online, result.Service.Status);
        Assert.Equal(2, result.IndexerCount);
        Assert.Equal(1, result.EnabledIndexerCount);
        Assert.Equal(1, result.OnlineIndexerCount);
        Assert.Single(result.RecentFailures);
        Assert.Equal(["Sonarr", "Radarr"], result.ConnectedApplications);
        Assert.Single(result.HealthWarnings);
    }

    [Fact]
    public async Task QBittorrentMapsAndLimitsSuccessfulReadOnlyResponses()
    {
        var torrents = Enumerable.Range(1, 30).Select(index => new
        {
            name = $"Torrent {index}",
            progress = index == 1 ? 1 : 0.5,
            state = index == 1 ? "pausedUP" : "downloading",
            category = "media",
            eta = 120
        });
        var client = new QBittorrentClient(CreateClient(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("test-api-key", request.Headers.Authorization?.Parameter);
            return request.RequestUri!.AbsolutePath switch
            {
                "/api/v2/app/version" => Text("v5.2.3"),
                "/api/v2/torrents/info" => Json(JsonSerializer.Serialize(torrents)),
                "/api/v2/transfer/info" => Json("""{"dl_info_speed":2048,"up_info_speed":1024}"""),
                _ => throw new InvalidOperationException()
            };
        }, "test-api-key"), Options());

        var result = await client.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal(MediaStatuses.Online, result.Service.Status);
        Assert.Equal("v5.2.3", result.Service.Version);
        Assert.Equal(25, result.Torrents.Count);
        Assert.Equal(24, result.ActiveCount);
        Assert.Equal(1, result.PausedCount);
        Assert.Equal(2048, result.DownloadSpeedBytesPerSecond);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task AuthenticationFailuresAreSanitized(HttpStatusCode statusCode)
    {
        var client = new SonarrClient(CreateClient(_ => new HttpResponseMessage(statusCode)), Options("super-secret"));

        var result = await client.GetOverviewAsync(TestContext.Current.CancellationToken);
        var serialized = JsonSerializer.Serialize(result);

        Assert.Equal(MediaStatuses.Degraded, result.Service.Status);
        Assert.Equal("Authentication was rejected by the service.", result.Service.SanitizedMessage);
        Assert.DoesNotContain("super-secret", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TimeoutIsUnavailableAndSanitized()
    {
        var client = new SonarrClient(CreateClient(_ => throw new TaskCanceledException("secret URL")), Options());

        var result = await client.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal(MediaStatuses.Unavailable, result.Service.Status);
        Assert.Equal("The service timed out.", result.Service.SanitizedMessage);
    }

    [Fact]
    public async Task UnreachableServiceIsUnavailableAndSanitized()
    {
        var client = new SonarrClient(CreateClient(_ => throw new HttpRequestException("http://secret-host")), Options());

        var result = await client.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal(MediaStatuses.Unavailable, result.Service.Status);
        Assert.Equal("The service could not be reached.", result.Service.SanitizedMessage);
    }

    [Fact]
    public async Task MalformedJsonIsDegraded()
    {
        var client = new SonarrClient(CreateClient(_ => Json("{not-json")), Options());

        var result = await client.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal(MediaStatuses.Degraded, result.Service.Status);
        Assert.Equal("The service returned an invalid response.", result.Service.SanitizedMessage);
    }

    [Fact]
    public async Task AggregatorKeepsPartialResults()
    {
        var service = new MediaService(
            new StubJellyfin(Online("Jellyfin")),
            new StubSonarr(Unavailable("Sonarr")),
            new StubRadarr(Online("Radarr")),
            new StubProwlarr(Online("Prowlarr")),
            new StubQBittorrent(Online("qBittorrent")),
            new MediaHealthEngine());

        var result = await service.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal(MediaStatuses.Degraded, result.Status);
        Assert.InRange(result.HealthScore, 0, 99);
        Assert.Equal(MediaStatuses.Online, result.Jellyfin.Service.Status);
        Assert.Equal(MediaStatuses.Unavailable, result.Sonarr.Service.Status);
        Assert.Equal("critical", result.Insights[0].Severity);
        Assert.Equal("Services unavailable", result.Insights[0].Title);
        Assert.DoesNotContain(result.Insights, insight => insight.Title == "All services healthy");
    }

    [Fact]
    public async Task AggregatorReturnsUnavailableWhenEveryServiceIsOffline()
    {
        var service = new MediaService(
            new StubJellyfin(Unavailable("Jellyfin")),
            new StubSonarr(Unavailable("Sonarr")),
            new StubRadarr(Unavailable("Radarr")),
            new StubProwlarr(Unavailable("Prowlarr")),
            new StubQBittorrent(Unavailable("qBittorrent")),
            new MediaHealthEngine());

        var result = await service.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal(MediaStatuses.Unavailable, result.Status);
        Assert.Equal(0, result.HealthScore);
        Assert.Equal("Services unavailable", result.Insights[0].Title);
        Assert.All(result.Services, status => Assert.Equal(MediaStatuses.Unavailable, status.Status));
    }

    [Fact]
    public async Task MissingCredentialsReturnNotConfiguredWithoutRequests()
    {
        var requestCount = 0;
        var options = Options();
        options = new MediaOptions
        {
            Sonarr = new MediaApiKeyOptions(options.Sonarr.BaseUrl)
        };
        var client = new SonarrClient(CreateClient(_ =>
        {
            requestCount++;
            return Json("{}");
        }), options);

        var result = await client.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal(MediaStatuses.NotConfigured, result.Service.Status);
        Assert.False(result.Service.IsConfigured);
        Assert.Equal(0, requestCount);
    }

    private static MediaOptions Options(string apiKey = "test-api-key") => new()
    {
        Jellyfin = new MediaApiKeyOptions("http://test/") { ApiKey = apiKey },
        Sonarr = new MediaApiKeyOptions("http://test/") { ApiKey = apiKey },
        Radarr = new MediaApiKeyOptions("http://test/") { ApiKey = apiKey },
        Prowlarr = new MediaApiKeyOptions("http://test/") { ApiKey = apiKey },
        QBittorrent = new QBittorrentOptions { BaseUrl = "http://test/", ApiKey = apiKey }
    };

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        string? bearerToken = null)
    {
        var client = new HttpClient(new StubHandler(responder))
        {
            BaseAddress = new Uri("http://test/"),
            Timeout = TimeSpan.FromSeconds(1)
        };
        if (bearerToken is not null)
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return client;
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Text(string text) =>
        new(HttpStatusCode.OK) { Content = new StringContent(text, Encoding.UTF8, "text/plain") };

    private static string ArrResponse(Uri uri, bool series) => uri.AbsolutePath switch
    {
        "/api/v3/system/status" => """{"version":"4.0.0"}""",
        "/api/v3/series" => """[{"monitored":true},{"monitored":false}]""",
        "/api/v3/movie" => """[{"monitored":true,"movieFile":{"qualityCutoffNotMet":true}},{"monitored":false}]""",
        "/api/v3/calendar" => """[{"title":"Next episode","airDateUtc":"2026-07-25T10:00:00Z"}]""",
        "/api/v3/wanted/missing" => """{"totalRecords":3,"records":[]}""",
        "/api/v3/queue" => """{"totalRecords":1,"records":[{"title":"Safe title","status":"downloading","size":100,"sizeleft":25}]}""",
        "/api/v3/history" => """{"records":[{"sourceTitle":"Safe history","eventType":"downloadFolderImported","date":"2026-07-23T10:00:00Z"}]}""",
        "/api/v3/health" => """[{"message":"Read-only warning"}]""",
        _ => throw new InvalidOperationException($"{uri.AbsolutePath} is not expected for {(series ? "Sonarr" : "Radarr")}.")
    };

    private static MediaServiceStatus Online(string name) =>
        new(name, MediaStatuses.Online, "1.0", 1, DateTimeOffset.UtcNow, null, true);

    private static MediaServiceStatus Unavailable(string name) =>
        new(name, MediaStatuses.Unavailable, null, 1, DateTimeOffset.UtcNow, "Unavailable.", true);

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }

    private sealed class StubJellyfin(MediaServiceStatus status) : IJellyfinClient
    {
        public Task<JellyfinOverview> GetOverviewAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new JellyfinOverview(status, 0, 0, 0, 0, 0, 0, []));
    }

    private sealed class StubSonarr(MediaServiceStatus status) : ISonarrClient
    {
        public Task<SonarrOverview> GetOverviewAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new SonarrOverview(status, 0, 0, 0, 0, [], [], [], []));
    }

    private sealed class StubRadarr(MediaServiceStatus status) : IRadarrClient
    {
        public Task<RadarrOverview> GetOverviewAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new RadarrOverview(status, 0, 0, 0, 0, 0, [], [], []));
    }

    private sealed class StubProwlarr(MediaServiceStatus status) : IProwlarrClient
    {
        public Task<ProwlarrOverview> GetOverviewAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new ProwlarrOverview(status, 0, 0, 0, 0, [], [], [], []));
    }

    private sealed class StubQBittorrent(MediaServiceStatus status) : IQBittorrentClient
    {
        public Task<QBittorrentOverview> GetOverviewAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new QBittorrentOverview(status, 0, 0, 0, 0, 0, null, null, 0, 0, null, []));
    }
}
