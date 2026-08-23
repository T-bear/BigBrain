using System.Net;
using System.Text;
using BigBrain.Api.Media;

namespace BigBrain.Api.Tests;

public sealed class LibrarrAudiobookAcquisitionProviderTests
{
    [Fact]
    public async Task StatusRequiresConfigurationAndAllDependencies()
    {
        var none = Provider(_ => Json(HttpStatusCode.OK, "{}"), configured: false);
        Assert.Equal("notConfigured", (await none.GetStatusAsync(TestContext.Current.CancellationToken)).State);

        var healthy = Provider(request =>
        {
            Assert.Equal("/api/admin/health", request.RequestUri!.AbsolutePath);
            return Json(HttpStatusCode.OK, """{"healthy":true,"checks":[{"service":"prowlarr","status":"ok"},{"service":"qbittorrent","status":"ok"},{"service":"audiobookshelf","status":"ok"}]}""");
        });
        var status = await healthy.GetStatusAsync(TestContext.Current.CancellationToken);
        Assert.Equal("configuredHealthy", status.State);
        Assert.True(status.CanSearch);
        Assert.True(status.CanRequest);
        Assert.False(status.CanCancel);
    }

    [Fact]
    public async Task StatusFailsClosedForAuthAndDependencyFailure()
    {
        Assert.Equal("configuredUnavailable", (await Provider(_ => Json(HttpStatusCode.Unauthorized, "{}")).GetStatusAsync(TestContext.Current.CancellationToken)).State);
        var provider = Provider(_ => Json(HttpStatusCode.OK, """{"healthy":false,"checks":[{"service":"prowlarr","status":"ok"},{"service":"qbittorrent","status":"error"}]}"""));
        var status = await provider.GetStatusAsync(TestContext.Current.CancellationToken);
        Assert.Equal("configuredUnavailable", status.State);
        Assert.Contains("qbittorrent", status.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchMapsOnlyTrackableProwlarrReleasesAndKeepsEditionsDistinct()
    {
        var provider = Provider(_ => Json(HttpStatusCode.OK, """
            {"results":[
              {"source":"prowlarr_audiobooks","title":"Boken.Svenska.M4B","author":"A","info_hash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","indexer":"one","format":"m4b","size":1000000},
              {"source":"prowlarr_audiobooks","title":"Boken.English.MP3","author":"A","info_hash":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","indexer":"two","format":"mp3","size":2000000},
              {"source":"audiobook","title":"external","info_hash":"cccccccccccccccccccccccccccccccccccccccc"},
              {"source":"prowlarr_audiobooks","title":"untrackable"}
            ]}
            """));
        var values = await provider.SearchAsync("Boken", "A", "sv", TestContext.Current.CancellationToken);
        Assert.Equal(2, values.Count);
        Assert.Equal(2, values.Select(x => x.EditionId).Distinct().Count());
        Assert.All(values, value => { Assert.Equal("librarr", value.Source); Assert.Null(value.Narrator); });
        Assert.Equal("sv", values.Single(x => x.Title.Contains("Svenska")).Language);
        Assert.Equal("probable", values.Single(x => x.Title.Contains("Svenska")).LanguageConfidence);
        Assert.Equal("en", values.Single(x => x.Title.Contains("English")).Language);
    }

    [Fact]
    public async Task SearchPreservesExplicitUnknownLanguage()
    {
        var provider = Provider(_ => Json(HttpStatusCode.OK, """{"results":[{"source":"prowlarr_audiobooks","title":"Boken","info_hash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]}"""));
        var candidate = Assert.Single(await provider.SearchAsync("Boken", null, "sv", TestContext.Current.CancellationToken));
        Assert.Equal("und", candidate.Language);
        Assert.Equal("unknown", candidate.LanguageConfidence);
    }

    [Fact]
    public async Task RequestUsesServerCachedCandidateAndNeverNeedsRawUrlFromWeb()
    {
        var calls = 0;
        var provider = Provider(request =>
        {
            calls++;
            if (request.Method == HttpMethod.Get)
                return Json(HttpStatusCode.OK, """{"results":[{"source":"prowlarr_audiobooks","title":"Boken","info_hash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","download_url":"https://provider.invalid/private"}]}""");
            var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.Contains("provider.invalid", body);
            Assert.DoesNotContain("edition:", body);
            return Json(HttpStatusCode.OK, """{"success":true,"title":"Boken"}""");
        });
        var candidate = Assert.Single(await provider.SearchAsync("Boken", null, "sv", TestContext.Current.CancellationToken));
        var job = await provider.RequestAsync(new(candidate.EditionId, candidate.Source, candidate.Language), TestContext.Current.CancellationToken);
        Assert.Equal("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", job.ProviderJobId);
        Assert.Equal("queued", job.Status);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task RequestRejectsUnknownOrReplayedCandidate()
    {
        var provider = Provider(_ => Json(HttpStatusCode.OK, """{"results":[]}"""));
        var exception = await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => provider.RequestAsync(new("edition:missing", "librarr", "sv"), TestContext.Current.CancellationToken));
        Assert.Equal("candidateExpired", exception.Code);
    }

    [Theory]
    [InlineData("queued", "queued")]
    [InlineData("downloading", "downloading")]
    [InlineData("completed", "importing")]
    [InlineData("importing", "importing")]
    [InlineData("error", "failed")]
    [InlineData("unexpected", "failed")]
    public void JobStatesMapFailClosed(string providerState, string expected) =>
        Assert.Equal(expected, LibrarrAudiobookAcquisitionProvider.MapState(providerState));

    [Fact]
    public async Task JobStatusRequiresMatchingHashAndCancelIsDisabled()
    {
        var provider = Provider(_ => Json(HttpStatusCode.OK, """{"downloads":[{"source":"audiobook","title":"Boken","status":"downloading","progress":25,"hash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]}"""));
        var status = await provider.GetJobStatusAsync("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", TestContext.Current.CancellationToken);
        Assert.Equal("downloading", status!.Status);
        var exception = await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => provider.CancelAsync(status.ProviderJobId, TestContext.Current.CancellationToken));
        Assert.Equal("cancelUnsupported", exception.Code);
    }

    private static LibrarrAudiobookAcquisitionProvider Provider(Func<HttpRequestMessage, HttpResponseMessage> response, bool configured = true)
    {
        var client = new HttpClient(new StubHandler(response)) { BaseAddress = new("http://librarr:5050/") };
        return new(client, new MediaOptions { Librarr = new() { ApiKey = configured ? "test-key" : null } }, TimeProvider.System);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response(request));
    }
}
