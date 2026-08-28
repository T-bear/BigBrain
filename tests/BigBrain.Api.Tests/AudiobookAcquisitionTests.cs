using System.Collections.Concurrent;
using BigBrain.Api.Media;

namespace BigBrain.Api.Tests;

public sealed class AudiobookAcquisitionTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"bb-audiobook-{Guid.NewGuid():N}");

    [Fact]
    public async Task ProviderNoneIsTruthfulAndDoesNotCreateFakeJob()
    {
        using var store = Store();
        var service = new AudiobookAcquisitionService(new NoAudiobookAcquisitionProvider(), store, TimeProvider.System);
        var status = await service.StatusAsync(CancellationToken.None);
        Assert.Equal("notConfigured", status.State); Assert.False(status.CanRequest);
        var exception = await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => service.RequestAsync(Candidate("sv", "Röst"), CancellationToken.None));
        Assert.Equal("providerNotConfigured", exception.Code);
        Assert.Empty((await service.ListAsync(0, 25, CancellationToken.None)).Items);
    }

    [Fact]
    public async Task SearchRanksVerifiedSwedishThenEnglishThenUnknownAndKeepsEditions()
    {
        using var store = Store();
        var provider = new FakeProvider { Results = [Candidate("und", "Okänd", "u"), Candidate("en", "Voice", "e"), Candidate("sv", "Röst", "s"), Candidate("sv", "Annan röst", "s2")] };
        var values = await new AudiobookAcquisitionService(provider, store, TimeProvider.System).SearchAsync("Boken", null, "sv", CancellationToken.None);
        Assert.Equal(["s", "s2", "e", "u"], values.Select(x => x.EditionId));
        Assert.Equal(2, values.Where(x => x.Language == "sv").Select(x => x.Narrator).Distinct().Count());
    }

    [Fact]
    public async Task ProviderCandidatesAreBoundedNormalizedAndDoNotExposeRemoteCoverUrls()
    {
        using var store = Store();
        var candidates = Enumerable.Range(0, 60).Select(index => Candidate(index == 0 ? "zz" : "sv", "Röst", $"e{index}") with
        {
            CoverUrl = index == 1 ? "/api/v1/modules/media/audiobooks/item/cover" : "https://untrusted.example/cover.jpg",
            LanguageConfidence = index == 0 ? "invented" : "verified"
        }).ToArray();
        var values = await new AudiobookAcquisitionService(new FakeProvider { Results = candidates }, store, TimeProvider.System)
            .SearchAsync("Boken", null, "sv", CancellationToken.None);
        Assert.Equal(50, values.Count);
        Assert.DoesNotContain(values, x => x.CoverUrl?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true);
        var unknown = Assert.Single(values, x => x.EditionId == "e0");
        Assert.Equal("und", unknown.Language); Assert.Equal("unknown", unknown.LanguageConfidence);
    }

    [Fact]
    public async Task MetadataVariantsAreBoundedDeduplicatedAndKeepEnglishAndUnknownWithSwedishPreference()
    {
        using var store = Store();
        var provider = new FakeProvider
        {
            Results = [Candidate("en", "Voice", "same"), Candidate("und", "Okänd", "unknown")]
        };
        var seeds = new[]
        {
            new AudiobookDiscoverySeed("The Wandering Inn", "pirateaba", "OL1W", "canonicalTitleAuthor"),
            new AudiobookDiscoverySeed("Wandering Inn", null, "OL1W", "alternateTitle")
        };
        var values = await new AudiobookAcquisitionService(provider, store, TimeProvider.System)
            .SearchVariantsAsync(seeds, "sv", CancellationToken.None);
        Assert.Equal(2, values.Count);
        Assert.Contains(values, value => value.Language == "en");
        Assert.Contains(values, value => value.Language == "und");
        Assert.All(values, value => Assert.Equal("OL1W", value.MetadataWorkId));
        Assert.Equal(2, provider.Searches.Count);
        Assert.Equal(0, provider.RequestCount);
    }

    [Fact]
    public async Task EnglishPreferenceRanksEnglishFirstAndAllLanguagesAddsNoPreference()
    {
        using var store = Store();
        var provider = new FakeProvider
        {
            Results = [Candidate("sv", "Röst", "sv"), Candidate("und", "Okänd", "und"), Candidate("en", "Voice", "en")]
        };
        var service = new AudiobookAcquisitionService(provider, store, TimeProvider.System);

        var english = await service.SearchAsync("Book", null, "en", CancellationToken.None);
        Assert.Equal("en", english[0].EditionId);
        Assert.Contains(english, value => value.Language == "und");

        var all = await service.SearchAsync("Book", null, "und", CancellationToken.None);
        Assert.Equal(["en", "sv", "und"], all.Select(value => value.EditionId));
        Assert.Equal(0, provider.RequestCount);
    }

    [Fact]
    public async Task MetadataVariantPartialFailureKeepsSuccessfulCandidates()
    {
        using var store = Store();
        var provider = new FakeProvider
        {
            Results = [Candidate("en", "Voice", "edition")],
            FailingQuery = "broken"
        };
        var values = await new AudiobookAcquisitionService(provider, store, TimeProvider.System).SearchVariantsAsync(
            [new("broken", null, null, "literal"), new("working", null, null, "literal")], "en", CancellationToken.None);
        Assert.Single(values);
        Assert.Equal("edition", values[0].EditionId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("x")]
    public async Task SearchValidationIsBounded(string query)
    {
        using var store = Store();
        var service = new AudiobookAcquisitionService(new FakeProvider(), store, TimeProvider.System);
        var exception = await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => service.SearchAsync(query, null, "sv", CancellationToken.None));
        Assert.Equal("invalidQuery", exception.Code);
    }

    [Fact]
    public async Task JobHasStableBigBrainIdMapsProviderStateAndCancelsSafely()
    {
        using var store = Store(); var provider = new FakeProvider(); var service = new AudiobookAcquisitionService(provider, store, TimeProvider.System);
        var requested = await service.RequestAsync(Candidate("sv", "Röst"), CancellationToken.None);
        Assert.Equal(32, requested.Id.Length); Assert.Equal(AudiobookAcquisitionStatuses.Queued, requested.Status);
        provider.JobStatus = new("provider-job", AudiobookAcquisitionStatuses.Downloading, null);
        Assert.Equal(AudiobookAcquisitionStatuses.Downloading, (await service.GetAsync(requested.Id, CancellationToken.None)).Status);
        Assert.Equal(AudiobookAcquisitionStatuses.Cancelled, (await service.CancelAsync(requested.Id, CancellationToken.None)).Status);
    }

    [Fact]
    public async Task MissingProviderJobFailsClosedAfterGraceInsteadOfRemainingDownloadingForever()
    {
        using var store = Store();
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 8, 28, 8, 0, 0, TimeSpan.Zero));
        var old = new AudiobookAcquisitionJob(
            new string('b', 32), "provider-job", Candidate("en", "Narrator"),
            AudiobookAcquisitionStatuses.Downloading, clock.GetUtcNow().AddHours(-1),
            clock.GetUtcNow().AddMinutes(-6), null);
        await store.AddAsync(old, CancellationToken.None);

        var result = await new AudiobookAcquisitionService(new FakeProvider(), store, clock)
            .GetAsync(old.Id, CancellationToken.None);

        Assert.Equal(AudiobookAcquisitionStatuses.Failed, result.Status);
        Assert.Contains("inte längre", result.Message);
        Assert.Equal(AudiobookAcquisitionStatuses.Failed, (await store.GetAsync(old.Id, CancellationToken.None))!.Status);
    }

    [Fact]
    public async Task MissingNewProviderJobRetainsActiveStateDuringRegistrationGrace()
    {
        using var store = Store();
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 8, 28, 8, 0, 0, TimeSpan.Zero));
        var recent = new AudiobookAcquisitionJob(
            new string('c', 32), "provider-job", Candidate("en", "Narrator"),
            AudiobookAcquisitionStatuses.Queued, clock.GetUtcNow(), clock.GetUtcNow(), null);
        await store.AddAsync(recent, CancellationToken.None);

        var result = await new AudiobookAcquisitionService(new FakeProvider(), store, clock)
            .GetAsync(recent.Id, CancellationToken.None);

        Assert.Equal(AudiobookAcquisitionStatuses.Queued, result.Status);
    }

    [Fact]
    public async Task MissingJobAndProviderFailureAreControlled()
    {
        using var store = Store(); var service = new AudiobookAcquisitionService(new FakeProvider { Fail = true }, store, TimeProvider.System);
        var missing = await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => service.GetAsync(new string('a',32), CancellationToken.None));
        Assert.Equal("jobNotFound", missing.Code); Assert.Equal(404, missing.StatusCode);
        Assert.Equal("providerUnavailable", (await Assert.ThrowsAsync<AudiobookAcquisitionException>(() => service.StatusAsync(CancellationToken.None))).Code);
    }

    [Fact]
    public void ImportPathCannotEscapeServerControlledRoot()
    {
        var root = Path.Combine(directory, "library");
        Assert.StartsWith(Path.GetFullPath(root), AudiobookImportPolicy.ResolveUnderRoot(root, "Author/Book"));
        Assert.Throws<ArgumentException>(() => AudiobookImportPolicy.ResolveUnderRoot(root, "../outside"));
        Assert.Throws<ArgumentException>(() => AudiobookImportPolicy.ResolveUnderRoot(root, "/tmp/outside"));
        Directory.CreateDirectory(Path.Combine(root, "Author", "Existing"));
        Assert.Throws<IOException>(() => AudiobookImportPolicy.ResolveNewDestination(root, "Author/Existing"));
        Assert.EndsWith(Path.Combine("Author", "New"), AudiobookImportPolicy.ResolveNewDestination(root, "Author/New"));
    }

    private AudiobookAcquisitionStore Store() => new(new MediaOptions { Audiobookshelf = new() { AcquisitionDatabasePath = Path.Combine(directory, "jobs.db") } });
    private static AudiobookAcquisitionCandidate Candidate(string language, string narrator, string id="edition") => new("work",id,"Boken","Författaren",narrator,language,AudiobookLanguages.DisplayName(language),"Oavkortad",100,2025,null,"fixture","available",language=="und"?"unknown":"verified");
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}

    private sealed class FakeProvider : IAudiobookAcquisitionProvider
    {
        public IReadOnlyList<AudiobookAcquisitionCandidate> Results { get; init; } = [];
        public bool Fail { get; init; }
        public string? FailingQuery { get; init; }
        public ConcurrentBag<string> Searches { get; } = [];
        public int RequestCount { get; private set; }
        public AudiobookProviderJob? JobStatus { get; set; }
        public Task<AudiobookAcquisitionProviderStatus> GetStatusAsync(CancellationToken token) => Fail?throw new HttpRequestException():Task.FromResult(new AudiobookAcquisitionProviderStatus("configuredHealthy","fixture",true,true,true,null));
        public Task<IReadOnlyList<AudiobookAcquisitionCandidate>> SearchAsync(string query,string? author,string language,CancellationToken token){Searches.Add(query);return query==FailingQuery?throw new HttpRequestException():Task.FromResult(Results);}
        public Task<AudiobookProviderJob> RequestAsync(AudiobookAcquisitionRequest request,CancellationToken token){RequestCount++;return Task.FromResult(new AudiobookProviderJob("provider-job",AudiobookAcquisitionStatuses.Queued,null));}
        public Task<AudiobookProviderJob?> GetJobStatusAsync(string providerJobId,CancellationToken token)=>Task.FromResult(JobStatus);
        public Task<AudiobookProviderJob> CancelAsync(string providerJobId,CancellationToken token)=>Task.FromResult(new AudiobookProviderJob(providerJobId,AudiobookAcquisitionStatuses.Cancelled,null));
    }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
