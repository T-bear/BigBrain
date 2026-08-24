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
    public async Task StatusFailsClosedForTransportFailure()
    {
        var provider = Provider(_ => throw new HttpRequestException("unreachable"));
        var status = await provider.GetStatusAsync(TestContext.Current.CancellationToken);
        Assert.Equal("configuredUnavailable", status.State);
        Assert.False(status.CanSearch);
        Assert.False(status.CanRequest);
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
        Assert.All(values, value => Assert.Equal("Prowlarr", value.Provenance));
        Assert.DoesNotContain(values, value => value.Edition?.Contains("one", StringComparison.OrdinalIgnoreCase) == true || value.Edition?.Contains("two", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Equal("sv", values.Single(x => x.Title.Contains("Svenska")).Language);
        Assert.Equal("probable", values.Single(x => x.Title.Contains("Svenska")).LanguageConfidence);
        Assert.Equal("en", values.Single(x => x.Title.Contains("English")).Language);
    }

    [Fact]
    public async Task SearchMapsApprovedAudioBookBayCandidateWithoutExposingItsPath()
    {
        var calls = 0;
        var provider = Provider(request =>
        {
            calls++;
            if (request.Method == HttpMethod.Get)
                return Json(HttpStatusCode.OK, """{"results":[{"source":"audiobookbay","title":"Boken English","abb_url":"/abss/boken/","indexer":"AudioBookBay"}]}""");
            var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.Contains("/abss/boken/", body);
            return Json(HttpStatusCode.OK, """{"success":true,"title":"Boken","info_hash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}""");
        });

        var candidate = Assert.Single(await provider.SearchAsync("Boken", null, "sv", TestContext.Current.CancellationToken));
        Assert.Equal("AudioBookBay", candidate.Provenance);
        Assert.DoesNotContain("abss", candidate.EditionId);
        Assert.Equal("probable", candidate.LanguageConfidence);
        var job = await provider.RequestAsync(new(candidate.EditionId, candidate.Source, candidate.Language), TestContext.Current.CancellationToken);
        Assert.Equal("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", job.ProviderJobId);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task SearchRejectsUnapprovedOrUnsafeNativeCandidates()
    {
        var provider = Provider(_ => Json(HttpStatusCode.OK, """
            {"results":[
              {"source":"librivox","title":"Direct","download_url":"https://example.invalid/book.zip"},
              {"source":"audiobookbay","title":"Unsafe","abb_url":"https://example.invalid/private"}
            ]}
            """));
        Assert.Empty(await provider.SearchAsync("Boken", null, "sv", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SearchDeduplicatesOnlyTheSameExactInfoHash()
    {
        var provider = Provider(_ => Json(HttpStatusCode.OK, """
            {"results":[
              {"source":"prowlarr_audiobooks","title":"Boken Release A","info_hash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","indexer":"one"},
              {"source":"prowlarr_audiobooks","title":"Boken Release B","info_hash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","indexer":"two"},
              {"source":"prowlarr_audiobooks","title":"Boken annan upplaga","info_hash":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","indexer":"one"}
            ]}
            """));
        var candidates = await provider.SearchAsync("Boken", null, "sv", TestContext.Current.CancellationToken);
        Assert.Equal(2, candidates.Count);
        Assert.Equal(2, candidates.Select(candidate => candidate.EditionId).Distinct().Count());
    }

    [Fact]
    public async Task SearchPropagatesCallerCancellation()
    {
        var client = new HttpClient(new DelayedHandler()) { BaseAddress = new("http://librarr:5050/"), Timeout = TimeSpan.FromSeconds(30) };
        var provider = new LibrarrAudiobookAcquisitionProvider(client, new MediaOptions { Librarr = new() { ApiKey = "test-key", SearchTimeoutSeconds = 30 } }, TimeProvider.System);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(25));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.SearchAsync("Boken", null, "sv", cancellation.Token));
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
    public async Task SearchDecodesDisplayMetadataWithoutInventingMissingAuthor()
    {
        var provider = Provider(_ => Json(HttpStatusCode.OK, """{"results":[{"source":"prowlarr_audiobooks","title":"Boken &amp; Berättelsen","author":"","info_hash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]}"""));
        var candidate = Assert.Single(await provider.SearchAsync("Boken", null, "sv", TestContext.Current.CancellationToken));
        Assert.Equal("Boken & Berättelsen", candidate.Title);
        Assert.Null(candidate.Author);
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
        var replay = await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => provider.RequestAsync(new(candidate.EditionId, candidate.Source, candidate.Language), TestContext.Current.CancellationToken));
        Assert.Equal("candidateExpired", replay.Code);
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

    [Fact]
    public async Task MissingDownloadNeverBecomesCompletedWithoutDurableImportEvidence()
    {
        var calls = 0;
        var provider = Provider(request =>
        {
            calls++;
            return request.RequestUri!.AbsolutePath switch
            {
                "/api/downloads" when calls == 1 => Json(HttpStatusCode.OK, """{"downloads":[{"status":"completed","hash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]}"""),
                "/api/downloads" => Json(HttpStatusCode.OK, """{"downloads":[]}"""),
                "/api/activity" => Json(HttpStatusCode.OK, """{"events":[]}"""),
                _ => throw new InvalidOperationException(request.RequestUri.AbsolutePath)
            };
        });
        var first = await provider.GetJobStatusAsync("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", TestContext.Current.CancellationToken);
        var second = await provider.GetJobStatusAsync("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", TestContext.Current.CancellationToken);
        Assert.Equal("importing", first!.Status);
        Assert.Equal("importing", second!.Status);
        Assert.NotEqual("completed", second.Status);
    }

    [Fact]
    public async Task DurableImportFailureMapsToSafeTerminalState()
    {
        var provider = Provider(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/downloads" => Json(HttpStatusCode.OK, """{"downloads":[]}"""),
            "/api/activity" => Json(HttpStatusCode.OK, """{"events":[{"event_type":"torrent_import_failed","job_id":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]}"""),
            _ => throw new InvalidOperationException(request.RequestUri.AbsolutePath)
        });
        var status = await provider.GetJobStatusAsync("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", TestContext.Current.CancellationToken);
        Assert.Equal("failed", status!.Status);
        Assert.Contains("bevarats", status.Message);
    }

    [Theory]
    [InlineData(false, "indexing")]
    [InlineData(true, "completed")]
    public async Task CompletedImportWaitsForExactAudiobookshelfIndexEvidence(bool indexed, string expected)
    {
        var provider = Provider(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/downloads" => Json(HttpStatusCode.OK, """{"downloads":[]}"""),
            "/api/activity" => Json(HttpStatusCode.OK, """{"events":[{"event_type":"torrent_import","job_id":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]}"""),
            "/api/library" => Json(HttpStatusCode.OK, """{"items":[{"title":"Boken","author":"Författaren","source_id":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]}"""),
            "/api/library/audiobooks" when indexed => Json(HttpStatusCode.OK, """{"items":[{"title":"Boken","author":"Författaren"}]}"""),
            "/api/library/audiobooks" => Json(HttpStatusCode.OK, """{"items":[]}"""),
            _ => throw new InvalidOperationException(request.RequestUri.AbsolutePath)
        });
        var status = await provider.GetJobStatusAsync("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", TestContext.Current.CancellationToken);
        Assert.Equal(expected, status!.Status);
    }

    [Fact]
    public async Task CompletedImportAcceptsOneCanonicalAudiobookshelfTitleForTheExactImportedHash()
    {
        var provider = Provider(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/downloads" => Json(HttpStatusCode.OK, """{"downloads":[]}"""),
            "/api/activity" => Json(HttpStatusCode.OK, """{"events":[{"event_type":"torrent_import","job_id":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]}"""),
            "/api/library" => Json(HttpStatusCode.OK, """{"items":[{"title":"Narnia.1. Min morbror trollkarlen.swe.sagablanc-reseeded","author":"Unknown","source_id":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]}"""),
            "/api/library/audiobooks" => Json(HttpStatusCode.OK, """{"items":[{"title":"Min Morbror Trollkarlen","author":"C.S. Lewis"}]}"""),
            _ => throw new InvalidOperationException(request.RequestUri.AbsolutePath)
        });
        var status = await provider.GetJobStatusAsync("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", TestContext.Current.CancellationToken);
        Assert.Equal("completed", status!.Status);
    }

    [Fact]
    public async Task AmbiguousCanonicalAudiobookshelfTitlesRemainIndexing()
    {
        var provider = Provider(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/downloads" => Json(HttpStatusCode.OK, """{"downloads":[]}"""),
            "/api/activity" => Json(HttpStatusCode.OK, """{"events":[{"event_type":"torrent_import","job_id":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]}"""),
            "/api/library" => Json(HttpStatusCode.OK, """{"items":[{"title":"Narnia.1. Min morbror trollkarlen.swe.sagablanc-reseeded","author":"Unknown","source_id":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}]}"""),
            "/api/library/audiobooks" => Json(HttpStatusCode.OK, """{"items":[{"title":"Min Morbror Trollkarlen","author":"C.S. Lewis"},{"title":"Min Morbror Trollkarlen","author":"Annan"}]}"""),
            _ => throw new InvalidOperationException(request.RequestUri.AbsolutePath)
        });
        var status = await provider.GetJobStatusAsync("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", TestContext.Current.CancellationToken);
        Assert.Equal("indexing", status!.Status);
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

    private sealed class DelayedHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("unreachable");
        }
    }
}
