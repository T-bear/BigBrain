namespace BigBrain.Api.Media;

public static class MediaStatuses
{
    public const string Online = "online";
    public const string Degraded = "degraded";
    public const string Unavailable = "unavailable";
    public const string NotConfigured = "notConfigured";
}

public sealed record MediaServiceStatus(
    string ServiceName,
    string Status,
    string? Version,
    long? ResponseTimeMs,
    DateTimeOffset CheckedAtUtc,
    string? SanitizedMessage,
    bool IsConfigured);

public sealed record MediaQueueItem(string Title, string Status, double? ProgressPercent);
public sealed record MediaHistoryItem(string Title, string EventType, DateTimeOffset? DateUtc);
public sealed record MediaHealthWarning(string Source, string Message);
public sealed record TorrentItem(string Name, double ProgressPercent, string State, string? Category, long? EtaSeconds);

public sealed record QBittorrentOverview(
    MediaServiceStatus Service,
    int ActiveCount,
    int PausedCount,
    int CompletedCount,
    long DownloadSpeedBytesPerSecond,
    long UploadSpeedBytesPerSecond,
    IReadOnlyList<TorrentItem> Torrents);

public sealed record SonarrOverview(
    MediaServiceStatus Service,
    int SeriesCount,
    int MonitoredSeriesCount,
    int MissingMonitoredEpisodes,
    int QueueCount,
    IReadOnlyList<MediaQueueItem> Queue,
    IReadOnlyList<MediaHistoryItem> RecentHistory,
    IReadOnlyList<MediaHealthWarning> HealthWarnings);

public sealed record RadarrOverview(
    MediaServiceStatus Service,
    int MovieCount,
    int MonitoredMovieCount,
    int MissingMovieCount,
    int QueueCount,
    IReadOnlyList<MediaQueueItem> Queue,
    IReadOnlyList<MediaHistoryItem> RecentHistory,
    IReadOnlyList<MediaHealthWarning> HealthWarnings);

public sealed record ProwlarrOverview(
    MediaServiceStatus Service,
    int IndexerCount,
    int EnabledIndexerCount,
    IReadOnlyList<string> IndexerStatuses,
    IReadOnlyList<string> ConnectedApplications,
    IReadOnlyList<MediaHealthWarning> HealthWarnings);

public sealed record JellyfinOverview(
    MediaServiceStatus Service,
    int LibraryCount,
    int MovieCount,
    int SeriesCount,
    int ActiveSessionCount);

public sealed record MediaOverview(
    string Status,
    DateTimeOffset CollectedAtUtc,
    IReadOnlyList<MediaServiceStatus> Services,
    QBittorrentOverview QBittorrent,
    SonarrOverview Sonarr,
    RadarrOverview Radarr,
    ProwlarrOverview Prowlarr,
    JellyfinOverview Jellyfin);

public interface IJellyfinClient
{
    Task<JellyfinOverview> GetOverviewAsync(CancellationToken cancellationToken);
}

public interface ISonarrClient
{
    Task<SonarrOverview> GetOverviewAsync(CancellationToken cancellationToken);
}

public interface IRadarrClient
{
    Task<RadarrOverview> GetOverviewAsync(CancellationToken cancellationToken);
}

public interface IProwlarrClient
{
    Task<ProwlarrOverview> GetOverviewAsync(CancellationToken cancellationToken);
}

public interface IQBittorrentClient
{
    Task<QBittorrentOverview> GetOverviewAsync(CancellationToken cancellationToken);
}

public interface IMediaService
{
    Task<MediaOverview> GetOverviewAsync(CancellationToken cancellationToken);
}
