using System.Net;
using System.Text;
using BigBrain.Api.Media;

namespace BigBrain.Api.Tests;

public sealed class MediaSearchTests
{
    [Fact]
    public async Task AllProvidersSucceedAndResultsAreLimited()
    {
        var providers = new[]
        {
            Provider("Jellyfin", MediaStatuses.Online, 15),
            Provider("Sonarr", MediaStatuses.Online, 15),
            Provider("Radarr", MediaStatuses.Online, 15)
        };

        var response = await new MediaSearchService(providers)
            .SearchAsync("  Family Guy  ", TestContext.Current.CancellationToken);

        Assert.Equal("Family Guy", response.Query);
        Assert.Equal(MediaSearchStatuses.Complete, response.Status);
        Assert.All(response.Providers, provider => Assert.Equal(10, provider.Results.Count));
    }

    [Theory]
    [InlineData(1, MediaSearchStatuses.Partial)]
    [InlineData(2, MediaSearchStatuses.Partial)]
    [InlineData(3, MediaSearchStatuses.Unavailable)]
    public async Task ProviderFailuresRemainIsolated(int failureCount, string expectedStatus)
    {
        var providers = Enumerable.Range(0, 3)
            .Select(index => index < failureCount
                ? new StubProvider($"Provider {index}", (_, _) => throw new InvalidOperationException(
                    "secret https://internal/path /srv/media"))
                : Provider($"Provider {index}", MediaStatuses.Online, 1))
            .ToArray();

        var response = await new MediaSearchService(providers)
            .SearchAsync("title", TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatus, response.Status);
        Assert.Equal(3 - failureCount, response.Providers.Count(result => result.Status == MediaStatuses.Online));
        Assert.All(response.Providers.Where(result => result.Error is not null), result =>
        {
            Assert.DoesNotContain("secret", result.Error, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/srv/", result.Error, StringComparison.Ordinal);
            Assert.DoesNotContain("http", result.Error, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task NotConfiguredProviderProducesPartialResult()
    {
        var response = await new MediaSearchService([
            Provider("Jellyfin", MediaStatuses.Online, 1),
            Provider("Sonarr", MediaStatuses.NotConfigured, 0),
            Provider("Radarr", MediaStatuses.Online, 0)])
            .SearchAsync("title", TestContext.Current.CancellationToken);

        Assert.Equal(MediaSearchStatuses.Partial, response.Status);
        Assert.Equal(MediaStatuses.NotConfigured, response.Providers[1].Status);
    }

    [Fact]
    public async Task CancellationIsPropagatedToEveryProvider()
    {
        using var source = new CancellationTokenSource();
        var observed = new bool[3];
        var providers = Enumerable.Range(0, 3).Select(index =>
            new StubProvider($"Provider {index}", async (_, token) =>
            {
                using var registration = token.Register(() => observed[index] = true);
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return ProviderResult($"Provider {index}", MediaStatuses.Online, 0);
            })).ToArray();

        var task = new MediaSearchService(providers).SearchAsync("title", source.Token);
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.All(observed, Assert.True);
    }

    [Fact]
    public async Task ArrSearchUsesOnlyGetAndDoesNotExposePathsOrCredentials()
    {
        const string payload = """
            [
              {
                "id": 7,
                "title": "Family Guy",
                "year": 1999,
                "monitored": true,
                "path": "/srv/media/tv/Family Guy",
                "apiKey": "top-secret",
                "seasons": [{}, {}],
                "statistics": { "episodeCount": 12, "episodeFileCount": 10 }
              }
            ]
            """;
        var handler = new RecordingHandler(payload);
        var options = Options();
        var client = new SonarrClient(new HttpClient(handler) { BaseAddress = new Uri("http://sonarr/") }, options);

        var response = await client.SearchAsync("family", 10, TestContext.Current.CancellationToken);
        var serialized = System.Text.Json.JsonSerializer.Serialize(response);

        Assert.Single(handler.Methods);
        Assert.All(handler.Methods, method => Assert.Equal(HttpMethod.Get, method));
        Assert.DoesNotContain("/srv/media", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("top-secret", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(options.Sonarr.ApiKey!, serialized, StringComparison.Ordinal);
    }

    [Fact]
    public async Task JellyfinSearchUsesBoundedTextSearch()
    {
        var handler = new RecordingHandler("""{"Items":[],"TotalRecordCount":0}""");
        var options = Options();
        var client = new JellyfinClient(new HttpClient(handler) { BaseAddress = new Uri("http://jellyfin/") }, options);

        await client.SearchAsync("Family Guy", 10, TestContext.Current.CancellationToken);

        var request = Assert.Single(handler.RequestUris);
        Assert.Contains("SearchTerm=Family Guy", Uri.UnescapeDataString(request), StringComparison.Ordinal);
        Assert.Contains("Limit=10", request, StringComparison.Ordinal);
        Assert.Equal(HttpMethod.Get, Assert.Single(handler.Methods));
    }

    private static StubProvider Provider(string name, string status, int requestedResults) =>
        new(name, (_, _) => Task.FromResult(ProviderResult(name, status, requestedResults)));

    private static MediaSearchProviderResult ProviderResult(string name, string status, int resultCount) =>
        new(name, status, status == MediaStatuses.Online ? null : "Provider unavailable.",
            Enumerable.Range(0, resultCount).Select(index =>
                new MediaSearchResult(index.ToString(System.Globalization.CultureInfo.InvariantCulture), $"Title {index}", 2000, MediaTypes.Movie,
                    MediaSearchStates.Available, null, new MediaSearchMetadata())).ToArray());

    private static MediaOptions Options() => new()
    {
        Jellyfin = new MediaApiKeyOptions("http://jellyfin") { ApiKey = "jellyfin-key" },
        Sonarr = new MediaApiKeyOptions("http://sonarr") { ApiKey = "sonarr-key" },
        Radarr = new MediaApiKeyOptions("http://radarr") { ApiKey = "radarr-key" }
    };

    private sealed class StubProvider(
        string name,
        Func<int, CancellationToken, Task<MediaSearchProviderResult>> search) : IMediaSearchProvider
    {
        public string ProviderName => name;
        public Task<MediaSearchProviderResult> SearchAsync(string query, int limit, CancellationToken cancellationToken) =>
            search(limit, cancellationToken);
    }

    private sealed class RecordingHandler(string content) : HttpMessageHandler
    {
        public List<HttpMethod> Methods { get; } = [];
        public List<string> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Methods.Add(request.Method);
            RequestUris.Add(request.RequestUri?.ToString() ?? string.Empty);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
        }
    }
}
