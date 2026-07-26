using System.Security.Cryptography;
using System.Text;
using System.Net;
using BigBrain.Api.Media;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace BigBrain.Api.Tests;

public sealed class MediaLookupRequestTests
{
    [Theory]
    [InlineData(MediaLookupTypes.Series, 1)]
    [InlineData(MediaLookupTypes.Movie, 1)]
    [InlineData(MediaLookupTypes.All, 2)]
    public async Task LookupSelectsExpectedProviders(string mediaType, int expectedProviders)
    {
        IMediaLookupProvider[] providers =
        [
            new LookupStub("Sonarr", MediaLookupTypes.Series),
            new LookupStub("Radarr", MediaLookupTypes.Movie)
        ];
        var service = new MediaLookupService(providers, new MediaOptions(), NullLogger<MediaLookupService>.Instance);

        var response = await service.LookupAsync("title", mediaType, TestContext.Current.CancellationToken);

        Assert.Equal(expectedProviders, response.Providers.Count);
        Assert.Equal(MediaSearchStatuses.Complete, response.Status);
        Assert.All(response.Providers, provider => Assert.True(provider.Results.Count <= 10));
    }

    [Theory]
    [InlineData(MediaLookupTypes.Series, 1, 0)]
    [InlineData(MediaLookupTypes.Movie, 0, 1)]
    [InlineData(MediaLookupTypes.All, 1, 1)]
    public async Task LookupNeverCallsAnUnselectedProvider(
        string mediaType,
        int expectedSonarrCalls,
        int expectedRadarrCalls)
    {
        var sonarr = new LookupStub("Sonarr", MediaLookupTypes.Series);
        var radarr = new LookupStub("Radarr", MediaLookupTypes.Movie);
        var service = new MediaLookupService([sonarr, radarr], new MediaOptions(), NullLogger<MediaLookupService>.Instance);

        await service.LookupAsync("title", mediaType, TestContext.Current.CancellationToken);

        Assert.Equal(expectedSonarrCalls, sonarr.Calls);
        Assert.Equal(expectedRadarrCalls, radarr.Calls);
    }

    [Fact]
    public async Task LookupMapsProviderTimeoutToStableErrorCode()
    {
        var provider = new LookupStub("Sonarr", MediaLookupTypes.Series, timeout: true);
        var service = new MediaLookupService([provider], new MediaOptions(), NullLogger<MediaLookupService>.Instance);

        var response = await service.LookupAsync("title", MediaLookupTypes.Series, TestContext.Current.CancellationToken);

        var failure = Assert.Single(response.Providers);
        Assert.Equal(MediaProviderErrorCodes.Timeout, failure.ErrorCode);
        Assert.Equal(MediaStatuses.Unavailable, failure.Status);
        Assert.DoesNotContain("exception", failure.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupIsPartialWhenOneProviderFailsAndSanitizesException()
    {
        IMediaLookupProvider[] providers =
        [
            new LookupStub("Sonarr", MediaLookupTypes.Series),
            new LookupStub("Radarr", MediaLookupTypes.Movie, throwRaw: true)
        ];
        var service = new MediaLookupService(providers, new MediaOptions(), NullLogger<MediaLookupService>.Instance);

        var response = await service.LookupAsync("title", MediaLookupTypes.All, TestContext.Current.CancellationToken);
        var failed = response.Providers.Single(provider => provider.Provider == "Radarr");

        Assert.Equal(MediaSearchStatuses.Partial, response.Status);
        Assert.Equal("The provider lookup failed.", failed.Error);
        Assert.DoesNotContain("/srv/private", System.Text.Json.JsonSerializer.Serialize(response), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LookupCancellationPropagates()
    {
        using var source = new CancellationTokenSource();
        var provider = new LookupStub("Sonarr", MediaLookupTypes.Series, waitForCancellation: true);
        var service = new MediaLookupService([provider], new MediaOptions(), NullLogger<MediaLookupService>.Instance);

        var task = service.LookupAsync("title", MediaLookupTypes.Series, source.Token);
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.True(provider.CancellationObserved);
    }

    [Fact]
    public void PosterUrlAcceptsOnlySafePublicHttpsImages()
    {
        using var safe = System.Text.Json.JsonDocument.Parse(
            """{"images":[{"coverType":"poster","remoteUrl":"https://image.tmdb.org/t/p/w500/poster.jpg"}]}""");
        using var internalUrl = System.Text.Json.JsonDocument.Parse(
            """{"images":[{"coverType":"poster","remoteUrl":"http://sonarr:8989/MediaCover/1/poster.jpg"}]}""");
        using var secretUrl = System.Text.Json.JsonDocument.Parse(
            """{"images":[{"coverType":"poster","remoteUrl":"https://user:secret@example.test/poster.jpg"}]}""");

        Assert.Equal("https://image.tmdb.org/t/p/w500/poster.jpg", ArrPosterUrl.Get(safe.RootElement));
        Assert.Null(ArrPosterUrl.Get(internalUrl.RootElement));
        Assert.Null(ArrPosterUrl.Get(secretUrl.RootElement));
    }

    [Fact]
    public async Task PosterProxyReturnsAllowedImageWithoutExposingSourceOrSecrets()
    {
        const string source = "https://image.tmdb.org/t/p/w500/poster.jpg";
        var route = MediaPosterToken.Create(source);
        var handler = new PosterHandler(HttpStatusCode.OK, "image/jpeg", [1, 2, 3]);
        var service = new MediaPosterService(
            new PosterClientFactory(handler),
            NullLogger<MediaPosterService>.Instance);

        var poster = await service.GetAsync(
            Assert.IsType<string>(route).Split('/').Last(),
            TestContext.Current.CancellationToken);

        Assert.NotNull(poster);
        Assert.Equal("image/jpeg", poster.Value.ContentType);
        Assert.Equal([1, 2, 3], poster.Value.Bytes);
        Assert.Equal(source, Assert.Single(handler.RequestUris));
        Assert.DoesNotContain(source, route, StringComparison.Ordinal);
        Assert.DoesNotContain("apiKey", route, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("http://image.tmdb.org/poster.jpg")]
    [InlineData("https://127.0.0.1/poster.jpg")]
    [InlineData("https://user:secret@image.tmdb.org/poster.jpg")]
    [InlineData("https://untrusted.example.test/poster.jpg")]
    public void PosterProxyRejectsUnsafeSources(string source)
    {
        Assert.Null(MediaPosterToken.Create(source));
    }

    [Fact]
    public void ServiceLinksExposeOnlyAllowlistedPublicFields()
    {
        var options = new MediaOptions
        {
            Jellyfin = new MediaApiKeyOptions("http://jellyfin:8096") { ApiKey = "secret-key" },
            ServiceLinks = new MediaServiceLinksOptions
            {
                Jellyfin = new MediaServiceLinkOptions
                {
                    Enabled = true,
                    Url = "https://media.example.test/jellyfin"
                }
            }
        };

        var serialized = System.Text.Json.JsonSerializer.Serialize(MediaServiceLinks.From(options));

        Assert.Contains("https://media.example.test/jellyfin", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-key", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("ApiKey", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BaseUrl", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddOptionsUseOpaqueIdsAndNeverExposeRootValue()
    {
        var provider = new RequestProviderStub();
        var service = new MediaAddOptionsService([provider], new MediaOpaqueIdProtector(), Options());

        var response = await service.GetAsync(MediaLookupTypes.Series, TestContext.Current.CancellationToken);
        var serialized = System.Text.Json.JsonSerializer.Serialize(response);

        Assert.DoesNotContain("/srv/media/tv", serialized, StringComparison.Ordinal);
        Assert.NotEqual("42", response.RootFolders[0].Id);
        Assert.Equal(64, response.RootFolders[0].Id.Length);
        Assert.Equal("TV Library", response.RootFolders[0].DisplayName);
    }

    [Fact]
    public async Task PreviewDoesNotWriteAndConfirmIsIdempotent()
    {
        var provider = new RequestProviderStub();
        var protector = new MediaOpaqueIdProtector();
        var options = Options();
        var optionService = new MediaAddOptionsService([provider], protector, options);
        var available = await optionService.GetAsync(MediaLookupTypes.Series, TestContext.Current.CancellationToken);
        var service = new MediaRequestService(
            [provider],
            [provider],
            protector,
            new MediaRequestStore(),
            options,
            NullLogger<MediaRequestService>.Instance);

        var preview = await service.PreviewAsync(new(
            "Sonarr",
            MediaLookupTypes.Series,
            "123",
            available.RootFolders[0].Id,
            available.QualityProfiles[0].Id,
            "all",
            "standard",
            false), TestContext.Current.CancellationToken);

        Assert.Equal(0, provider.AddCalls);
        Assert.True(preview.ExpiresAtUtc <= DateTimeOffset.UtcNow.AddMinutes(6));
        Assert.DoesNotContain("/srv/", System.Text.Json.JsonSerializer.Serialize(preview), StringComparison.Ordinal);

        var first = await service.ConfirmAsync(
            new(preview.RequestToken, "request-1"),
            TestContext.Current.CancellationToken);
        var repeated = await service.ConfirmAsync(
            new(preview.RequestToken, "request-1"),
            TestContext.Current.CancellationToken);

        Assert.Equal(MediaRequestStatuses.Created, first.Status);
        Assert.Equal(first, repeated);
        Assert.Equal(1, provider.AddCalls);
        Assert.True(provider.DuplicateChecks >= 2);
    }

    [Fact]
    public async Task MoviePreviewIsValidAndDoesNotWrite()
    {
        var provider = new RequestProviderStub("Radarr", MediaLookupTypes.Movie);
        var protector = new MediaOpaqueIdProtector();
        var options = Options();
        var available = await new MediaAddOptionsService([provider], protector, options)
            .GetAsync(MediaLookupTypes.Movie, TestContext.Current.CancellationToken);
        var service = new MediaRequestService(
            [provider], [provider], protector, new MediaRequestStore(), options, NullLogger<MediaRequestService>.Instance);

        var preview = await service.PreviewAsync(new(
            "Radarr",
            MediaLookupTypes.Movie,
            "123",
            available.RootFolders[0].Id,
            available.QualityProfiles[0].Id,
            "movieOnly",
            null,
            false), TestContext.Current.CancellationToken);

        Assert.Equal(MediaRequestStatuses.PreviewReady, preview.Status);
        Assert.Equal(MediaLookupTypes.Movie, preview.Summary.MediaType);
        Assert.Equal(0, provider.AddCalls);
    }

    [Fact]
    public async Task ConfirmClassifiesRejectedProviderCredentials()
    {
        var provider = new RequestProviderStub { AddException = new MediaAuthenticationException() };
        var protector = new MediaOpaqueIdProtector();
        var options = Options();
        var available = await new MediaAddOptionsService([provider], protector, options)
            .GetAsync(MediaLookupTypes.Series, TestContext.Current.CancellationToken);
        var service = new MediaRequestService(
            [provider], [provider], protector, new MediaRequestStore(), options, NullLogger<MediaRequestService>.Instance);
        var preview = await service.PreviewAsync(new(
            "Sonarr", MediaLookupTypes.Series, "123",
            available.RootFolders[0].Id, available.QualityProfiles[0].Id,
            "all", "standard", false), TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<MediaRequestException>(() =>
            service.ConfirmAsync(
                new(preview.RequestToken, "request-configuration-error"),
                TestContext.Current.CancellationToken));

        Assert.Equal(MediaRequestErrors.ProviderConfigurationInvalid, exception.Code);
        Assert.Equal(StatusCodes.Status502BadGateway, exception.StatusCode);
    }

    [Fact]
    public async Task PreviewRejectsAlreadyRegisteredAndInvalidSelections()
    {
        var provider = new RequestProviderStub { Registered = true };
        var protector = new MediaOpaqueIdProtector();
        var service = new MediaRequestService(
            [provider],
            [provider],
            protector,
            new MediaRequestStore(),
            Options(),
            NullLogger<MediaRequestService>.Instance);

        var exception = await Assert.ThrowsAsync<MediaRequestException>(() => service.PreviewAsync(new(
            "Sonarr", "series", "123", "invalid", "invalid", "invalid", "standard", false),
            TestContext.Current.CancellationToken));

        Assert.Equal(MediaRequestErrors.AlreadyRegistered, exception.Code);
        Assert.Equal(0, provider.AddCalls);
    }

    [Fact]
    public async Task ExpiredAndManipulatedTokensAreRejected()
    {
        var store = new MediaRequestStore();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("expired"))).ToLowerInvariant();
        store.Add(new(
            hash,
            DateTimeOffset.UtcNow.AddSeconds(-1),
            "Sonarr",
            "series",
            "123",
            "root",
            "quality",
            "all",
            "standard",
            false,
            new("Title", 2020, "Sonarr", "series", "TV Library", "HD", "All", "Standard", false)));

        var expired = Assert.Throws<MediaRequestException>(() => store.Acquire(hash, "one", DateTimeOffset.UtcNow));
        var manipulated = Assert.Throws<MediaRequestException>(() => store.Acquire("bad", "two", DateTimeOffset.UtcNow));

        Assert.Equal(MediaRequestErrors.RequestExpired, expired.Code);
        Assert.Equal(MediaRequestErrors.RequestExpired, manipulated.Code);
    }

    [Fact]
    public async Task DisabledRequestsBlockOptionsPreviewAndConfirm()
    {
        var provider = new RequestProviderStub();
        var options = Options(enabled: false);
        var protector = new MediaOpaqueIdProtector();
        var requestService = new MediaRequestService(
            [provider], [provider], protector, new MediaRequestStore(), options, NullLogger<MediaRequestService>.Instance);

        var optionsFailure = await Assert.ThrowsAsync<MediaRequestException>(() =>
            new MediaAddOptionsService([provider], protector, options)
                .GetAsync("series", TestContext.Current.CancellationToken));
        var previewFailure = await Assert.ThrowsAsync<MediaRequestException>(() =>
            requestService.PreviewAsync(new("Sonarr", "series", "123", "a", "b", "all", "standard", false),
                TestContext.Current.CancellationToken));
        var confirmFailure = await Assert.ThrowsAsync<MediaRequestException>(() =>
            requestService.ConfirmAsync(new("token", "key"), TestContext.Current.CancellationToken));

        Assert.All([optionsFailure, previewFailure, confirmFailure],
            exception => Assert.Equal(MediaRequestErrors.RequestsDisabled, exception.Code));
    }

    [Theory]
    [InlineData("Sonarr", "http://sonarr/", "api/v3/series")]
    [InlineData("Radarr", "http://radarr/", "api/v3/movie")]
    public async Task AddAdaptersUseExactlyOnePostToTheAllowedEndpoint(
        string providerName,
        string baseAddress,
        string expectedEndpoint)
    {
        var handler = new RecordingHandler("""{"id":91,"title":"Requested title"}""");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
        var mediaOptions = new MediaOptions
        {
            Sonarr = new MediaApiKeyOptions("http://sonarr") { ApiKey = "sonarr-key" },
            Radarr = new MediaApiKeyOptions("http://radarr") { ApiKey = "radarr-key" }
        };
        IMediaAddProvider provider = providerName == "Sonarr"
            ? new SonarrClient(httpClient, mediaOptions)
            : new RadarrClient(httpClient, mediaOptions);

        var result = await provider.AddAsync(new(
            "123",
            "Requested title",
            2020,
            7,
            "/provider/internal/value",
            providerName == "Sonarr" ? "all" : "movieOnly",
            providerName == "Sonarr" ? "standard" : null,
            false), TestContext.Current.CancellationToken);

        Assert.Equal("91", result.SourceId);
        Assert.Equal(HttpMethod.Post, Assert.Single(handler.Methods));
        Assert.EndsWith(expectedEndpoint, Assert.Single(handler.RequestUris), StringComparison.Ordinal);
        Assert.DoesNotContain(handler.Methods, method =>
            method == HttpMethod.Put || method == HttpMethod.Patch || method == HttpMethod.Delete);
    }

    private static MediaOptions Options(bool enabled = true) => new()
    {
        Requests = new MediaRequestOptions { Enabled = enabled }
    };

    private sealed class LookupStub(
        string provider,
        string mediaType,
        bool throwRaw = false,
        bool waitForCancellation = false,
        bool timeout = false) : IMediaLookupProvider
    {
        public bool CancellationObserved { get; private set; }
        public int Calls { get; private set; }
        public string ProviderName => provider;
        public string SupportedMediaType => mediaType;

        public async Task<MediaLookupProviderResult> LookupAsync(
            string query,
            int limit,
            CancellationToken cancellationToken)
        {
            Calls++;
            if (throwRaw) throw new InvalidOperationException("raw secret /srv/private");
            if (timeout) throw new TaskCanceledException("provider timeout");
            if (waitForCancellation)
            {
                using var registration = cancellationToken.Register(() => CancellationObserved = true);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            var results = Enumerable.Range(0, 15).Select(index =>
                new MediaLookupResult(provider, index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    $"Title {index}", null, 2020, null, null, null, null, mediaType,
                    MediaLookupStates.External, false, false, null)).ToArray();
            return new(provider, MediaStatuses.Online, null, results);
        }
    }

    private sealed class RequestProviderStub(
        string providerName = "Sonarr",
        string mediaType = MediaLookupTypes.Series) : IMediaRequestProvider, IMediaAddProvider
    {
        public string ProviderName => providerName;
        public string SupportedMediaType => mediaType;
        public int AddCalls { get; private set; }
        public int DuplicateChecks { get; private set; }
        public bool Registered { get; set; }
        public Exception? AddException { get; set; }

        public Task<ProviderAddOptions> GetAddOptionsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new ProviderAddOptions(
                ProviderName,
                SupportedMediaType,
                [new(42, "/srv/media/tv", "TV Library", 1000)],
                [new(7, "7", "HD 1080p")],
                mediaType == MediaLookupTypes.Series ? ["all", "none"] : ["movieOnly", "none"],
                mediaType == MediaLookupTypes.Series ? ["standard"] : []));

        public Task<MediaLookupResult?> GetLookupItemAsync(string foreignId, CancellationToken cancellationToken) =>
            Task.FromResult<MediaLookupResult?>(new(
                ProviderName, foreignId, "The Expanse", null, 2015, "Overview", "Syfy", 45, "ended",
                SupportedMediaType, MediaLookupStates.External, false, false, null));

        public Task<bool> IsRegisteredAsync(
            string foreignId,
            string title,
            int? year,
            CancellationToken cancellationToken)
        {
            DuplicateChecks++;
            return Task.FromResult(Registered);
        }

        public Task<ProviderAddResult> AddAsync(ProviderAddCommand command, CancellationToken cancellationToken)
        {
            AddCalls++;
            if (AddException is not null) throw AddException;
            return Task.FromResult(new ProviderAddResult("99", command.Title));
        }
    }

    private sealed class RecordingHandler(string responseBody) : HttpMessageHandler
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
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class PosterClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class PosterHandler(
        HttpStatusCode status,
        string contentType,
        byte[] content) : HttpMessageHandler
    {
        public List<string> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri?.AbsoluteUri ?? string.Empty);
            var response = new HttpResponseMessage(status) { Content = new ByteArrayContent(content) };
            response.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            return Task.FromResult(response);
        }
    }
}
