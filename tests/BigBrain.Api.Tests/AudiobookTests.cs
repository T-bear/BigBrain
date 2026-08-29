using System.Net;
using System.Text;
using BigBrain.Api.Media;

namespace BigBrain.Api.Tests;

public sealed class AudiobookTests
{
    private static readonly string[] ExpectedRanking = ["s", "e", "u"];
    [Theory]
    [InlineData("sv", "Svenska")]
    [InlineData("SWE", "Svenska")]
    [InlineData("en", "Engelska")]
    [InlineData("zz", "Språk okänt")]
    public void LanguageNormalizationIsExplicit(string value, string display) =>
        Assert.Equal(display, AudiobookLanguages.DisplayName(value));

    [Fact]
    public void RankingKeepsUnknownAfterVerifiedPreferredAndFallback()
    {
        var values = new[] { Result("u", "und", "unknown"), Result("e", "en", "verified"), Result("s", "sv", "verified") };
        Assert.Equal(ExpectedRanking, AudiobookRanking.Rank(values).Select(x => x.EditionId));
    }

    [Fact]
    public void EditionsWithSameWorkRemainDistinct()
    {
        var values = new[] { Result("sv-edition", "sv", "verified", "A"), Result("en-edition", "en", "verified", "B") };
        Assert.Equal(2, AudiobookRanking.Rank(values).Select(x => x.EditionId).Distinct().Count());
    }

    [Fact]
    public async Task ProviderNoneDoesNotPreventLibraryAndHtmlIsPlainText()
    {
        const string payload = """{"results":[{"id":"book-1","media":{"duration":3600,"metadata":{"title":"Boken","authorName":"Författaren","narratorName":"Rösten","language":"sv","description":"<p>Trygg &amp; text</p>"}},"userMediaProgress":{"progress":0.63}}],"total":1}""";
        var client = Client(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(payload, Encoding.UTF8, "application/json") });
        var overview = await client.GetOverviewAsync(CancellationToken.None);
        Assert.Equal(AudiobookIntegrationStates.ConfiguredHealthy, overview.State);
        Assert.Equal(63, overview.ContinueListening!.ProgressPercent);
        Assert.Equal("Trygg & text", overview.ContinueListening.Description);
        Assert.False(overview.Acquisition.CanRequest);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, AudiobookIntegrationStates.ConfiguredUnavailable)]
    [InlineData(HttpStatusCode.Forbidden, AudiobookIntegrationStates.ConfiguredUnavailable)]
    public async Task AuthenticationFailureDegradesSafely(HttpStatusCode status, string expected)
    {
        var overview = await Client(new HttpResponseMessage(status)).GetOverviewAsync(CancellationToken.None);
        Assert.Equal(expected, overview.State);
        Assert.Empty(overview.Library);
    }

    [Fact]
    public async Task MalformedPayloadDegradesSafely()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not-json") };
        Assert.Equal(AudiobookIntegrationStates.ConfiguredUnavailable, (await Client(response).GetOverviewAsync(CancellationToken.None)).State);
    }

    [Fact]
    public async Task MissingConfigurationIsTruthfulAndNetworkFree()
    {
        var options = new MediaOptions { Audiobookshelf = new AudiobookshelfOptions() };
        var client = new AudiobookshelfClient(new HttpClient(new StubHandler(_ => throw new InvalidOperationException("network used"))) { BaseAddress = new Uri("http://abs/") }, options, new NoAudiobookAcquisitionProvider());
        var overview = await client.GetOverviewAsync(CancellationToken.None);
        Assert.Equal(AudiobookIntegrationStates.NotConfigured, overview.State);
    }

    [Fact]
    public async Task MissingLibraryItemReturnsNull()
    {
        var item = await Client(new HttpResponseMessage(HttpStatusCode.NotFound)).GetItemAsync("missing-item", CancellationToken.None);
        Assert.Null(item);
    }

    [Fact]
    public async Task ValidOwnerArtworkRemainsAvailable()
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
        response.Content.Headers.ContentType = new("image/jpeg");
        var cover = await Client(response).GetCoverAsync("book-1", CancellationToken.None);
        Assert.NotNull(cover);
        Assert.Equal(bytes, cover.Value.Bytes);
    }

    [Fact]
    public void RecognizesOnlyTheVerifiedLegacyGenericArtworkHash()
    {
        Assert.True(AudiobookshelfClient.IsKnownGenericArtworkHash(Convert.FromHexString("4F0501EA0E79901895E6860E60F32DE273F6F18700636BAE86611BA5E149EFF0")));
        Assert.False(AudiobookshelfClient.IsKnownGenericArtworkHash(new byte[32]));
        Assert.False(AudiobookshelfClient.IsKnownGenericArtworkHash(new byte[31]));
    }

    private static AudiobookDiscoveryResult Result(string edition, string language, string confidence, string? narrator = null) =>
        new("work", edition, "Title", "Author", narrator, language, AudiobookLanguages.DisplayName(language), null, null, null, "test", "available", confidence);
    private static AudiobookshelfClient Client(HttpResponseMessage response)
    {
        var http = new HttpClient(new StubHandler(_ => response)) { BaseAddress = new Uri("http://abs/") };
        var options = new MediaOptions { Audiobookshelf = new AudiobookshelfOptions { ApiKey = "secret", LibraryId = "library" } };
        return new(http, options, new NoAudiobookAcquisitionProvider());
    }
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response(request));
    }
}
