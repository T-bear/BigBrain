namespace BigBrain.Api.Media;

public sealed class MediaService(
    IJellyfinClient jellyfinClient,
    ISonarrClient sonarrClient,
    IRadarrClient radarrClient,
    IProwlarrClient prowlarrClient,
    IQBittorrentClient qBittorrentClient,
    IMediaHealthEngine healthEngine) : IMediaService
{
    public async Task<MediaOverview> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var jellyfinTask = jellyfinClient.GetOverviewAsync(cancellationToken);
        var sonarrTask = sonarrClient.GetOverviewAsync(cancellationToken);
        var radarrTask = radarrClient.GetOverviewAsync(cancellationToken);
        var prowlarrTask = prowlarrClient.GetOverviewAsync(cancellationToken);
        var qBittorrentTask = qBittorrentClient.GetOverviewAsync(cancellationToken);

        await Task.WhenAll(jellyfinTask, sonarrTask, radarrTask, prowlarrTask, qBittorrentTask);

        var jellyfin = await jellyfinTask;
        var sonarr = await sonarrTask;
        var radarr = await radarrTask;
        var prowlarr = await prowlarrTask;
        var qBittorrent = await qBittorrentTask;
        MediaServiceStatus[] services =
        [
            jellyfin.Service,
            sonarr.Service,
            radarr.Service,
            prowlarr.Service,
            qBittorrent.Service
        ];

        var health = healthEngine.Assess(services, sonarr, radarr, prowlarr, qBittorrent);
        return new MediaOverview(
            AggregateStatus(services),
            health.Score,
            health.Summary,
            health.StatusLevel,
            DateTimeOffset.UtcNow,
            BuildInsights(services, jellyfin, sonarr, radarr, prowlarr, qBittorrent),
            services,
            qBittorrent,
            sonarr,
            radarr,
            prowlarr,
            jellyfin);
    }

    private static MediaInsight[] BuildInsights(
        IReadOnlyList<MediaServiceStatus> services,
        JellyfinOverview jellyfin,
        SonarrOverview sonarr,
        RadarrOverview radarr,
        ProwlarrOverview prowlarr,
        QBittorrentOverview qBittorrent)
    {
        var insights = new List<MediaInsight>();
        var configuredServices = services.Where(service => service.IsConfigured).ToArray();
        if (configuredServices.Length > 0 && configuredServices.All(service => service.Status == MediaStatuses.Online))
            insights.Add(new("success", "All services healthy", "Every configured media service is responding."));
        if (qBittorrent.ActiveCount > 0 && qBittorrent.DownloadSpeedBytesPerSecond == 0)
            insights.Add(new("warning", "Downloads stalled", "Active torrents are reporting no download traffic."));
        if (prowlarr.EnabledIndexerCount > prowlarr.OnlineIndexerCount)
            insights.Add(new("critical", "Indexers offline", $"{prowlarr.EnabledIndexerCount - prowlarr.OnlineIndexerCount} enabled indexer(s) are unavailable."));
        if (jellyfin.RecentlyAdded.Count > 0)
            insights.Add(new("information", "New media added", $"{jellyfin.RecentlyAdded.Count} recent library item(s) are available."));
        if (qBittorrent.FreeSpaceBytes is > 0 and < 10L * 1024 * 1024 * 1024)
            insights.Add(new("warning", "Disk space low", "qBittorrent reports less than 10 GiB free."));
        if (sonarr.HealthWarnings.Count + radarr.HealthWarnings.Count + prowlarr.HealthWarnings.Count > 0)
            insights.Add(new("warning", "Service health warnings", "One or more media services require attention."));
        return [.. insights];
    }

    private static string AggregateStatus(IEnumerable<MediaServiceStatus> services)
    {
        var statuses = services.Select(service => service.Status).ToArray();
        if (statuses.All(status => status == MediaStatuses.NotConfigured))
        {
            return MediaStatuses.NotConfigured;
        }

        if (statuses.All(status => status == MediaStatuses.Unavailable))
        {
            return MediaStatuses.Unavailable;
        }

        return statuses.All(status => status == MediaStatuses.Online)
            ? MediaStatuses.Online
            : MediaStatuses.Degraded;
    }
}
