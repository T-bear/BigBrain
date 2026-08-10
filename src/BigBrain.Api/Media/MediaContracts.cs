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
public sealed record MediaCalendarItem(string Title, DateTimeOffset? AirDateUtc);
public sealed record RecentlyAddedMedia(string Name, string MediaType, DateTimeOffset? DateCreatedUtc);
public sealed record MediaHealthWarning(string Source, string Message);
public sealed record TorrentItem(string Name, double ProgressPercent, string State, string? Category, long? EtaSeconds);

public sealed record QBittorrentOverview(
    MediaServiceStatus Service,
    int ActiveCount,
    int PausedCount,
    int CompletedCount,
    long DownloadSpeedBytesPerSecond,
    long UploadSpeedBytesPerSecond,
    long? EtaSeconds,
    double? AverageRatio,
    long TotalDownloadedBytes,
    long TotalUploadedBytes,
    long? FreeSpaceBytes,
    IReadOnlyList<TorrentItem> Torrents);

public sealed record SonarrOverview(
    MediaServiceStatus Service,
    int SeriesCount,
    int MonitoredSeriesCount,
    int MissingMonitoredEpisodes,
    int QueueCount,
    IReadOnlyList<MediaQueueItem> Queue,
    IReadOnlyList<MediaCalendarItem> Calendar,
    IReadOnlyList<MediaHistoryItem> RecentHistory,
    IReadOnlyList<MediaHealthWarning> HealthWarnings);

public sealed record RadarrOverview(
    MediaServiceStatus Service,
    int MovieCount,
    int MonitoredMovieCount,
    int MissingMovieCount,
    int QualityUpgradeCount,
    int QueueCount,
    IReadOnlyList<MediaQueueItem> Queue,
    IReadOnlyList<MediaHistoryItem> RecentHistory,
    IReadOnlyList<MediaHealthWarning> HealthWarnings);

public sealed record ProwlarrOverview(
    MediaServiceStatus Service,
    int IndexerCount,
    int EnabledIndexerCount,
    int OnlineIndexerCount,
    int RssEnabledIndexerCount,
    IReadOnlyList<string> IndexerStatuses,
    IReadOnlyList<string> ConnectedApplications,
    IReadOnlyList<MediaHistoryItem> RecentFailures,
    IReadOnlyList<MediaHealthWarning> HealthWarnings);

public sealed record JellyfinOverview(
    MediaServiceStatus Service,
    int LibraryCount,
    int MovieCount,
    int SeriesCount,
    int EpisodeCount,
    int ActiveUserCount,
    int ActiveStreamCount,
    IReadOnlyList<RecentlyAddedMedia> RecentlyAdded);

public sealed record MediaInsight(string Severity, string Title, string Message);

public sealed record MediaOverview(
    string Status,
    int HealthScore,
    string HealthSummary,
    string HealthStatusLevel,
    DateTimeOffset CollectedAtUtc,
    IReadOnlyList<MediaInsight> Insights,
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

internal interface IQBittorrentQueueClient
{
    Task<IReadOnlyList<QBittorrentQueueItem>> GetQueueAsync(CancellationToken cancellationToken);
    Task RemoveAsync(string hash, bool deleteFiles, CancellationToken cancellationToken);
    Task StopAsync(string hash, CancellationToken cancellationToken);
    Task StartAsync(string hash, CancellationToken cancellationToken);
    Task ReannounceAsync(string hash, CancellationToken cancellationToken);
}

public interface IMediaService
{
    Task<MediaOverview> GetOverviewAsync(CancellationToken cancellationToken);
}
