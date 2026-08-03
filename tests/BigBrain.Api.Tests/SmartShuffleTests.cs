using BigBrain.Api.Media;
using Microsoft.Extensions.Logging.Abstractions;

namespace BigBrain.Api.Tests;

public sealed class SmartShuffleTests
{
    private static readonly string[] RoundRobinPrefix = ["a", "b", "c", "a", "b", "c"];
    [Fact]
    public void SameSeedProducesSameNonRoundRobinSequenceWithoutImmediateRepeats()
    {
        var first = Sequence(42, 60);
        var second = Sequence(42, 60);
        var other = Sequence(43, 60);

        Assert.Equal(first, second);
        Assert.NotEqual(first, other);
        Assert.DoesNotContain(Enumerable.Range(1, first.Count - 1), index => first[index] == first[index - 1]);
        Assert.Equal(3, first.Distinct().Count());
        Assert.NotEqual(RoundRobinPrefix, first.Take(6));
    }

    [Fact]
    public void UnplayableSeriesIsNeverSelectedAndSingleSeriesMayRepeat()
    {
        var selector = new SmartShuffleSelector(new SeededRandom(1));
        var candidates = new[] { new SmartShuffleCandidate("done", false, 0, 99), new SmartShuffleCandidate("only", true, 0, 0) };
        Assert.Equal("only", selector.Select(candidates, "only"));
    }

    [Fact]
    public void StarvationThresholdIsHonoured()
    {
        var selector = new SmartShuffleSelector(new SeededRandom(9));
        var candidates = new[]
        {
            new SmartShuffleCandidate("starved", true, 0, 6),
            new SmartShuffleCandidate("recent", true, 0, 0),
            new SmartShuffleCandidate("other", true, 0, 1)
        };
        Assert.Equal("starved", selector.Select(candidates, null));
    }

    [Fact]
    public async Task CoordinatorPollDoesNotDoubleStart()
    {
        var fake = new FakePlayback();
        var store = new SmartShuffleStore();
        var options = Options();
        var service = new SmartShuffleService(options, fake, store, new SmartShuffleSelector(new SeededRandom(2)), NullLogger<SmartShuffleService>.Instance);
        var created = await service.CreateAsync(new(["a", "b"], SmartShuffleDeviceId(fake.SessionId)), TestContext.Current.CancellationToken);
        var state = store.Find(created.Id)!;
        state.LastCommandAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        fake.NowPlayingItemId = null;

        await Task.WhenAll(
            service.PollAsync(TestContext.Current.CancellationToken),
            service.PollAsync(TestContext.Current.CancellationToken));

        Assert.Equal(2, fake.PlayCount); // initial explicit start plus exactly one coordinator transition
    }

    [Fact]
    public async Task CreateRequiresTwoPlayableSeriesAndLiveDevice()
    {
        var fake = new FakePlayback { MissingSeries = "b" };
        var service = new SmartShuffleService(Options(), fake, new SmartShuffleStore(), new SmartShuffleSelector(new SeededRandom(2)), NullLogger<SmartShuffleService>.Instance);
        var error = await Assert.ThrowsAsync<SmartShuffleException>(() =>
            service.CreateAsync(new(["a", "b"], SmartShuffleDeviceId(fake.SessionId)), TestContext.Current.CancellationToken));
        Assert.Equal("tooFewPlayableSeries", error.Code);
        Assert.Equal(0, fake.PlayCount);
    }

    [Fact]
    public async Task CreateRejectsExpiredOpaqueDeviceWithoutPlayback()
    {
        var fake = new FakePlayback();
        var service = new SmartShuffleService(Options(), fake, new SmartShuffleStore(), new SmartShuffleSelector(new SeededRandom(2)), NullLogger<SmartShuffleService>.Instance);

        var error = await Assert.ThrowsAsync<SmartShuffleException>(() =>
            service.CreateAsync(new(["a", "b"], "expired-opaque-device"), TestContext.Current.CancellationToken));

        Assert.Equal("deviceUnavailable", error.Code);
        Assert.Equal(0, fake.PlayCount);
    }

    [Fact]
    public async Task OptionsSanitizesJellyfinAuthenticationFailure()
    {
        var fake = new FakePlayback { SeriesException = new MediaAuthenticationException() };
        var service = new SmartShuffleService(Options(), fake, new SmartShuffleStore(), new SmartShuffleSelector(new SeededRandom(2)), NullLogger<SmartShuffleService>.Instance);

        var error = await Assert.ThrowsAsync<SmartShuffleException>(() =>
            service.GetOptionsAsync(TestContext.Current.CancellationToken));

        Assert.Equal("authenticationFailure", error.Code);
        Assert.Equal(503, error.StatusCode);
        Assert.DoesNotContain("fake", error.SafeMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> Sequence(int seed, int count)
    {
        var selector = new SmartShuffleSelector(new SeededRandom(seed));
        var candidates = new Dictionary<string, SmartShuffleCandidate>
        {
            ["a"] = new("a", true, 0, 0), ["b"] = new("b", true, 0, 0), ["c"] = new("c", true, 0, 0)
        };
        var result = new List<string>();
        string? previous = null;
        for (var turn = 0; turn < count; turn++)
        {
            var selected = selector.Select(candidates.Values.ToArray(), previous)!;
            result.Add(selected);
            foreach (var key in candidates.Keys.ToArray())
            {
                var item = candidates[key];
                candidates[key] = item with { Selections = item.Selections + (key == selected ? 1 : 0), TurnsSinceSelected = key == selected ? 0 : item.TurnsSinceSelected + 1 };
            }
            previous = selected;
        }
        return result;
    }

    private static MediaOptions Options() => new()
    {
        Jellyfin = new MediaApiKeyOptions("http://jellyfin") { ApiKey = "fake", UserId = "user" },
        SmartShuffle = new SmartShuffleOptions { Enabled = true }
    };

    private static string SmartShuffleDeviceId(string raw) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw))).ToLowerInvariant()[..24];

    private sealed class SeededRandom(int seed) : ISmartShuffleRandom
    {
        private readonly Random random = new(seed);
        public double NextDouble() => random.NextDouble();
    }

    private sealed class FakePlayback : IJellyfinPlaybackClient
    {
        public string SessionId { get; } = "raw-session-id";
        public string? MissingSeries { get; init; }
        public Exception? SeriesException { get; init; }
        public int PlayCount { get; private set; }
        public string? NowPlayingItemId { get; set; } = "playing";
        public Task<IReadOnlyList<SmartShuffleSeriesOption>> GetSeriesAsync(string userId, CancellationToken cancellationToken) =>
            SeriesException is null
                ? Task.FromResult<IReadOnlyList<SmartShuffleSeriesOption>>([new("a", "A", true), new("b", "B", true)])
                : Task.FromException<IReadOnlyList<SmartShuffleSeriesOption>>(SeriesException);
        public Task<SmartShuffleEpisode?> GetNextEpisodeAsync(string seriesId, string userId, CancellationToken cancellationToken) =>
            Task.FromResult(seriesId == MissingSeries ? null : new SmartShuffleEpisode(seriesId + "-e1", seriesId, seriesId.ToUpperInvariant(), "Episode", 1, 1, seriesId == "a" ? 120L : null));
        public Task<IReadOnlyList<JellyfinRemoteSession>> GetRemoteSessionsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<JellyfinRemoteSession>>([new(SessionId, "user", "TV", "Tizen", true, false)]);
        public Task PlayNowAsync(string sessionId, string itemId, long? startPositionTicks, CancellationToken cancellationToken)
        {
            PlayCount++;
            NowPlayingItemId = itemId;
            return Task.CompletedTask;
        }
        public Task<JellyfinPlaybackStatus> GetPlaybackStatusAsync(string sessionId, CancellationToken cancellationToken) =>
            Task.FromResult(new JellyfinPlaybackStatus(true, NowPlayingItemId, false));
    }
}
