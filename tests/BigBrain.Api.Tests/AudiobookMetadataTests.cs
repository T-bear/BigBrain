using System.Net;
using System.Text;
using BigBrain.Api.Media;

namespace BigBrain.Api.Tests;

public sealed class AudiobookMetadataTests
{
    [Theory]
    [InlineData("0-306-40615-2", "0306406152", "isbn10")]
    [InlineData("978-0-306-40615-7", "9780306406157", "isbn13")]
    public void IsbnIsNormalizedAndValidated(string input, string normalized, string kind)
    {
        var result = AudiobookMetadataInput.Classify(input);
        Assert.Equal(normalized, result.Normalized);
        Assert.Equal(kind, result.Kind);
    }

    [Theory]
    [InlineData("0306406153")]
    [InlineData("9780306406158")]
    public void MalformedIsbnShapedInputRemainsSafeFreeText(string input) =>
        Assert.Equal(AudiobookMetadataInputKinds.FreeText, AudiobookMetadataInput.Classify(input).Kind);

    [Fact]
    public async Task OpenLibraryFixtureMapsCanonicalMetadataWithoutInventingNarratorOrAsin()
    {
        const string fixture = """
            {"docs":[{"key":"/works/OL123W","edition_key":["OL1M"],"title":"The Wandering Inn",
            "alternative_title":["Wandering Inn"],"author_name":["pirateaba"],"first_publish_year":2017,
            "isbn":["0306406152","9780306406157"],"language":["eng"],"series":["The Wandering Inn"],"cover_i":42}]}
            """;
        var provider = Provider(_ => Json(fixture));
        var values = await provider.ResolveAsync(AudiobookMetadataInput.Classify("9780306406157"), CancellationToken.None);
        var work = Assert.Single(values);
        Assert.Equal("OL123W", work.WorkId);
        Assert.Equal("The Wandering Inn", work.CanonicalTitle);
        Assert.Equal("pirateaba", Assert.Single(work.Authors));
        Assert.Equal("The Wandering Inn", work.Series);
        Assert.Equal("en", work.Language);
        Assert.Equal("0306406152", work.Isbn10);
        Assert.Equal("9780306406157", work.Isbn13);
        Assert.Empty(work.Narrators);
        Assert.Null(work.Asin);
        Assert.Equal("/api/v1/modules/media/audiobooks/metadata/covers/42", work.CoverUrl);
    }

    [Fact]
    public async Task MissingMetadataIsAnEmptySuccessfulResult()
    {
        var values = await Provider(_ => Json("""{"numFound":0,"docs":[]}"""))
            .ResolveAsync(AudiobookMetadataInput.Classify("Unknown work"), CancellationToken.None);
        Assert.Empty(values);
    }

    [Fact]
    public void WanderingInnFixtureProducesBoundedCanonicalAuthorAndSeriesVariants()
    {
        var work = Work(series: "The Wandering Inn", alternate: ["Wandering Inn"]);
        var plan = AudiobookDiscoveryPlanner.Plan(AudiobookMetadataInput.Classify("The Wandering Inn"), [work], null);
        Assert.InRange(plan.Count, 1, AudiobookDiscoveryPlanner.MaximumProviderSearches);
        Assert.Equal("The Wandering Inn", plan[0].Query);
        Assert.Equal("pirateaba", plan[0].Author);
        Assert.DoesNotContain(plan.GroupBy(value => (value.Query.ToLowerInvariant(), value.Author?.ToLowerInvariant())).Select(group => group.Count()), count => count > 1);
        Assert.Equal(6, AudiobookDiscoveryPlanner.MaximumUpstreamQueries);
    }

    [Fact]
    public void AuthorOnlySearchPreservesLiteralQueryAndAddsOneResolvedWork()
    {
        var plan = AudiobookDiscoveryPlanner.Plan(AudiobookMetadataInput.Classify("pirateaba"),
            [Work(), Work("The Wandering Inn: Fae and Fare")], null);
        Assert.Equal(2, plan.Count);
        Assert.Equal("pirateaba", plan[0].Query);
        Assert.Null(plan[0].Author);
        Assert.Equal("literal", plan[0].MatchEvidence);
        Assert.Equal("The Wandering Inn", plan[1].Query);
        Assert.Equal("pirateaba", plan[1].Author);
        Assert.Equal("authorWork", plan[1].MatchEvidence);
    }

    [Fact]
    public void SeriesAndAlternateQueriesAreDeduplicated()
    {
        var work = Work(series: "Wandering Inn", alternate: ["The Wandering Inn", "Wandering Inn"]);
        var plan = AudiobookDiscoveryPlanner.Plan(AudiobookMetadataInput.Classify("The Wandering Inn"), [work], null);
        Assert.Equal(2, plan.Count);
        Assert.Equal("Wandering Inn", plan[1].Query);
        Assert.Equal("series", plan[1].MatchEvidence);
    }

    [Fact]
    public async Task ProviderTimeoutPropagatesAsControlledMetadataFailure()
    {
        var client = new HttpClient(new DelayedHandler()) { BaseAddress = new("https://openlibrary.org/"), Timeout = TimeSpan.FromMilliseconds(20) };
        var provider = new OpenLibraryAudiobookMetadataProvider(client, new MediaOptions());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.ResolveAsync(AudiobookMetadataInput.Classify("The Wandering Inn"), CancellationToken.None));
    }

    [Fact]
    public void NarratorCapabilityIsExplicitlyAbsent()
    {
        var work = Work();
        Assert.Empty(work.Narrators);
        Assert.False(new AudiobookMetadataResolution(AudiobookMetadataInput.Classify("Andrea Parsneau"), "notFound", [], false, null).NarratorSearchSupported);
    }

    private static AudiobookMetadataWork Work(
        string title = "The Wandering Inn",
        string? series = null,
        IReadOnlyList<string>? alternate = null) =>
        new("OL1W", ["OL1M"], title, alternate ?? [], ["pirateaba"], series, null, [], null, null, null, "en", 2017, null, "openLibrary");

    private static OpenLibraryAudiobookMetadataProvider Provider(Func<HttpRequestMessage, HttpResponseMessage> response) =>
        new(new HttpClient(new StubHandler(response)) { BaseAddress = new("https://openlibrary.org/"), Timeout = TimeSpan.FromSeconds(2) },
            new MediaOptions());
    private static HttpResponseMessage Json(string value) =>
        new(HttpStatusCode.OK) { Content = new StringContent(value, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response(request));
    }
    private sealed class DelayedHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return Json("{}");
        }
    }
}
