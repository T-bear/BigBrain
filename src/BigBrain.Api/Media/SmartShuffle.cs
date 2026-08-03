using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace BigBrain.Api.Media;

public sealed record SmartShuffleSeriesOption(string Id, string Name, bool HasPlayableEpisode);
public sealed record SmartShuffleDevice(string Id, string DisplayName, string ClientType, bool Available, bool IsPlaying);
public sealed record SmartShuffleEpisode(string Id, string SeriesId, string SeriesName, string Title, int SeasonNumber, int EpisodeNumber, long? PlaybackPositionTicks);
public sealed record SmartShuffleOptionsResponse(bool Enabled, IReadOnlyList<SmartShuffleSeriesOption> Series);
public sealed record CreateSmartShuffleSession(IReadOnlyList<string> SeriesIds, string DeviceId);
public sealed record SmartShuffleSessionResponse(string Id, string Status, SmartShuffleEpisode? NowPlaying, IReadOnlyList<string> RecentSeries, int RemainingSeries, string DeviceName, DateTimeOffset StartedAtUtc, string? ErrorCode);

internal sealed record JellyfinRemoteSession(string SessionId, string UserId, string DeviceName, string Client, bool SupportsRemoteControl, bool IsPlaying);
internal sealed record JellyfinPlaybackStatus(bool SessionAvailable, string? NowPlayingItemId, bool IsPaused);

internal interface IJellyfinPlaybackClient
{
    Task<IReadOnlyList<SmartShuffleSeriesOption>> GetSeriesAsync(string userId, CancellationToken cancellationToken);
    Task<SmartShuffleEpisode?> GetNextEpisodeAsync(string seriesId, string userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<JellyfinRemoteSession>> GetRemoteSessionsAsync(CancellationToken cancellationToken);
    Task PlayNowAsync(string sessionId, string itemId, long? startPositionTicks, CancellationToken cancellationToken);
    Task<JellyfinPlaybackStatus> GetPlaybackStatusAsync(string sessionId, CancellationToken cancellationToken);
}

internal interface ISmartShuffleRandom { double NextDouble(); }
internal sealed class SmartShuffleRandom : ISmartShuffleRandom { public double NextDouble() => Random.Shared.NextDouble(); }

internal sealed record SmartShuffleCandidate(string SeriesId, bool Playable, int Selections, int TurnsSinceSelected);

internal sealed class SmartShuffleSelector(ISmartShuffleRandom random)
{
    public string? Select(IReadOnlyList<SmartShuffleCandidate> source, string? previousSeriesId)
    {
        var playable = source.Where(candidate => candidate.Playable).ToArray();
        if (playable.Length == 0) return null;
        var candidates = playable.Length > 1
            ? playable.Where(candidate => candidate.SeriesId != previousSeriesId).ToArray()
            : playable;
        if (candidates.Length == 0) candidates = playable;

        // A playable series may wait at most twice the active pool size. At the
        // threshold, starvation candidates are selected before normal weighting.
        var starvationLimit = Math.Max(2, playable.Length * 2);
        var starved = candidates.Where(candidate => candidate.TurnsSinceSelected >= starvationLimit).ToArray();
        if (starved.Length > 0) candidates = starved;

        var weighted = candidates.Select(candidate => new
        {
            candidate.SeriesId,
            Weight = 1d + candidate.TurnsSinceSelected * 1.5d + 1d / (candidate.Selections + 1d) + random.NextDouble()
        }).ToArray();
        var target = random.NextDouble() * weighted.Sum(item => item.Weight);
        foreach (var item in weighted)
        {
            target -= item.Weight;
            if (target <= 0) return item.SeriesId;
        }
        return weighted[^1].SeriesId;
    }
}

internal sealed class SmartShuffleState
{
    public required string Id { get; init; }
    public required string RawDeviceSessionId { get; init; }
    public required string DeviceName { get; init; }
    public required DateTimeOffset StartedAtUtc { get; init; }
    public required Dictionary<string, SmartShuffleCandidate> Candidates { get; init; }
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public List<string> History { get; } = [];
    public SmartShuffleEpisode? CurrentEpisode { get; set; }
    public DateTimeOffset LastCommandAtUtc { get; set; }
    public string Status { get; set; } = "active";
    public string? ErrorCode { get; set; }
}

internal interface ISmartShuffleStore
{
    SmartShuffleState? Active { get; set; }
    SmartShuffleState? Find(string id);
}

internal sealed class SmartShuffleStore : ISmartShuffleStore
{
    private readonly object sync = new();
    private SmartShuffleState? active;
    public SmartShuffleState? Active { get { lock (sync) return active; } set { lock (sync) active = value; } }
    public SmartShuffleState? Find(string id) { lock (sync) return active?.Id == id ? active : null; }
}

internal sealed class SmartShuffleException(string code, string message, int statusCode = 400) : Exception(message)
{
    public string Code { get; } = code;
    public string SafeMessage { get; } = message;
    public int StatusCode { get; } = statusCode;
}

internal interface ISmartShuffleService
{
    Task<SmartShuffleOptionsResponse> GetOptionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SmartShuffleDevice>> GetDevicesAsync(CancellationToken cancellationToken);
    Task<SmartShuffleSessionResponse> CreateAsync(CreateSmartShuffleSession input, CancellationToken cancellationToken);
    SmartShuffleSessionResponse? Get(string id);
    Task<SmartShuffleSessionResponse> SkipAsync(string id, CancellationToken cancellationToken);
    SmartShuffleSessionResponse Stop(string id);
    Task PollAsync(CancellationToken cancellationToken);
}

internal sealed class SmartShuffleService(
    MediaOptions options,
    IJellyfinPlaybackClient jellyfin,
    ISmartShuffleStore store,
    SmartShuffleSelector selector,
    ILogger<SmartShuffleService> logger) : ISmartShuffleService
{
    private const int MaximumSeries = 20;
    private static readonly Action<ILogger, string, string, Exception?> SessionStarted =
        LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(2401, nameof(SessionStarted)), "Smart Shuffle started session={Session} device={Device}");
    private static readonly Action<ILogger, string, Exception?> SessionSkipped =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2402, nameof(SessionSkipped)), "Smart Shuffle skipped session={Session}");
    private static readonly Action<ILogger, string, Exception?> SessionStopped =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2403, nameof(SessionStopped)), "Smart Shuffle stopped session={Session}");
    private static readonly Action<ILogger, string, Exception?> PollFailed =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2404, nameof(PollFailed)), "Smart Shuffle poll failed session={Session} category=provider");

    public async Task<SmartShuffleOptionsResponse> GetOptionsAsync(CancellationToken cancellationToken)
    {
        if (!Configured) return new(false, []);
        try { return new(true, await jellyfin.GetSeriesAsync(options.Jellyfin.UserId!, cancellationToken)); }
        catch (TaskCanceledException) { throw new SmartShuffleException("timeout", "Jellyfin svarade inte i tid.", 503); }
        catch (MediaAuthenticationException) { throw new SmartShuffleException("authenticationFailure", "Jellyfin-autentiseringen misslyckades.", 503); }
        catch (HttpRequestException) { throw new SmartShuffleException("providerUnavailable", "Jellyfin är inte tillgängligt.", 503); }
    }

    public async Task<IReadOnlyList<SmartShuffleDevice>> GetDevicesAsync(CancellationToken cancellationToken)
    {
        if (!Configured) return [];
        var sessions = await jellyfin.GetRemoteSessionsAsync(cancellationToken);
        return sessions.Where(IsTargetable).Select(session => new SmartShuffleDevice(
            DeviceId(session.SessionId), session.DeviceName, session.Client, true, session.IsPlaying)).ToArray();
    }

    public async Task<SmartShuffleSessionResponse> CreateAsync(CreateSmartShuffleSession input, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var selected = input.SeriesIds.Distinct(StringComparer.Ordinal).ToArray();
        if (selected.Length is < 2 or > MaximumSeries) throw new SmartShuffleException("invalidSeriesSelection", "Välj mellan två och tjugo unika serier.");
        if (store.Active is { Status: "active" }) throw new SmartShuffleException("sessionAlreadyActive", "En Smart Shuffle-session är redan aktiv.", 409);

        var validSeries = await jellyfin.GetSeriesAsync(options.Jellyfin.UserId!, cancellationToken);
        if (selected.Any(id => validSeries.All(series => series.Id != id))) throw new SmartShuffleException("invalidSeries", "En vald serie är inte längre tillgänglig.");
        var sessions = (await jellyfin.GetRemoteSessionsAsync(cancellationToken)).Where(IsTargetable).ToArray();
        var target = sessions.SingleOrDefault(session => DeviceId(session.SessionId) == input.DeviceId)
            ?? throw new SmartShuffleException("deviceUnavailable", "Den valda TV:n är inte längre tillgänglig.", 409);
        var candidates = new Dictionary<string, SmartShuffleCandidate>(StringComparer.Ordinal);
        foreach (var id in selected)
        {
            var episode = await jellyfin.GetNextEpisodeAsync(id, options.Jellyfin.UserId!, cancellationToken);
            candidates[id] = new(id, episode is not null, 0, 0);
        }
        if (candidates.Values.Count(candidate => candidate.Playable) < 2) throw new SmartShuffleException("tooFewPlayableSeries", "Minst två valda serier måste ha osedda avsnitt.");

        var state = new SmartShuffleState
        {
            Id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant(),
            RawDeviceSessionId = target.SessionId,
            DeviceName = target.DeviceName,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Candidates = candidates
        };
        store.Active = state;
        await PlayNextAsync(state, cancellationToken);
        SessionStarted(logger, state.Id, target.DeviceName, null);
        return Map(state);
    }

    public SmartShuffleSessionResponse? Get(string id) => store.Find(id) is { } state ? Map(state) : null;

    public async Task<SmartShuffleSessionResponse> SkipAsync(string id, CancellationToken cancellationToken)
    {
        var state = store.Find(id) ?? throw new SmartShuffleException("sessionNotFound", "Shuffle-sessionen hittades inte.", 404);
        if (DateTimeOffset.UtcNow - state.LastCommandAtUtc < TimeSpan.FromSeconds(2))
            throw new SmartShuffleException("rateLimited", "Vänta ett ögonblick innan nästa byte.", 429);
        await state.Gate.WaitAsync(cancellationToken);
        try { await PlayNextCoreAsync(state, cancellationToken); }
        finally { state.Gate.Release(); }
        SessionSkipped(logger, state.Id, null);
        return Map(state);
    }

    public SmartShuffleSessionResponse Stop(string id)
    {
        var state = store.Find(id) ?? throw new SmartShuffleException("sessionNotFound", "Shuffle-sessionen hittades inte.", 404);
        state.Status = "stopped";
        SessionStopped(logger, state.Id, null);
        return Map(state);
    }

    public async Task PollAsync(CancellationToken cancellationToken)
    {
        var state = store.Active;
        if (state is not { Status: "active", CurrentEpisode: not null }) return;
        if (DateTimeOffset.UtcNow - state.LastCommandAtUtc < TimeSpan.FromSeconds(15)) return;
        if (!await state.Gate.WaitAsync(0, cancellationToken)) return;
        try
        {
            var playback = await jellyfin.GetPlaybackStatusAsync(state.RawDeviceSessionId, cancellationToken);
            if (!playback.SessionAvailable) { state.Status = "stopped"; state.ErrorCode = "deviceDisconnected"; return; }
            if (playback.NowPlayingItemId is null) await PlayNextCoreAsync(state, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            state.ErrorCode = "jellyfinUnavailable";
            PollFailed(logger, state.Id, null);
        }
        finally { state.Gate.Release(); }
    }

    private async Task PlayNextAsync(SmartShuffleState state, CancellationToken cancellationToken)
    {
        await state.Gate.WaitAsync(cancellationToken);
        try { await PlayNextCoreAsync(state, cancellationToken); }
        finally { state.Gate.Release(); }
    }

    private async Task PlayNextCoreAsync(SmartShuffleState state, CancellationToken cancellationToken)
    {
        var previous = state.History.LastOrDefault();
        while (true)
        {
            var seriesId = selector.Select(state.Candidates.Values.ToArray(), previous);
            if (seriesId is null) { state.Status = "completed"; state.CurrentEpisode = null; return; }
            var episode = await jellyfin.GetNextEpisodeAsync(seriesId, options.Jellyfin.UserId!, cancellationToken);
            if (episode is null) { state.Candidates[seriesId] = state.Candidates[seriesId] with { Playable = false }; continue; }
            await jellyfin.PlayNowAsync(state.RawDeviceSessionId, episode.Id, episode.PlaybackPositionTicks, cancellationToken);
            foreach (var key in state.Candidates.Keys.ToArray())
            {
                var current = state.Candidates[key];
                state.Candidates[key] = current with
                {
                    Selections = current.Selections + (key == seriesId ? 1 : 0),
                    TurnsSinceSelected = key == seriesId ? 0 : current.TurnsSinceSelected + 1
                };
            }
            state.History.Add(seriesId);
            if (state.History.Count > 12) state.History.RemoveAt(0);
            state.CurrentEpisode = episode;
            state.LastCommandAtUtc = DateTimeOffset.UtcNow;
            state.ErrorCode = null;
            return;
        }
    }

    private bool Configured => options.SmartShuffle.Enabled && !string.IsNullOrWhiteSpace(options.Jellyfin.UserId) && !string.IsNullOrWhiteSpace(options.Jellyfin.ApiKey);
    private void EnsureConfigured() { if (!Configured) throw new SmartShuffleException("smartShuffleDisabled", "Smart Shuffle är inte konfigurerat.", 503); }
    private bool IsTargetable(JellyfinRemoteSession session) => session.SupportsRemoteControl && session.UserId == options.Jellyfin.UserId;
    private static string DeviceId(string raw) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw))).ToLowerInvariant()[..24];
    private static SmartShuffleSessionResponse Map(SmartShuffleState state) => new(
        state.Id, state.Status, state.CurrentEpisode, state.History.TakeLast(6).ToArray(),
        state.Candidates.Values.Count(candidate => candidate.Playable), state.DeviceName,
        state.StartedAtUtc, state.ErrorCode);
}

internal sealed class SmartShuffleCoordinator(IServiceScopeFactory scopeFactory, MediaOptions options, ILogger<SmartShuffleCoordinator> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> CycleFailed =
        LoggerMessage.Define(LogLevel.Warning, new EventId(2405, nameof(CycleFailed)), "Smart Shuffle coordinator cycle failed category=internal");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.SmartShuffle.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<ISmartShuffleService>().PollAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception) { CycleFailed(logger, null); }
        }
    }
}
