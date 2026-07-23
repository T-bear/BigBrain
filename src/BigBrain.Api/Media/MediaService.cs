namespace BigBrain.Api.Media;

public sealed class MediaService(
    IJellyfinClient jellyfinClient,
    ISonarrClient sonarrClient,
    IRadarrClient radarrClient,
    IProwlarrClient prowlarrClient,
    IQBittorrentClient qBittorrentClient) : IMediaService
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

        return new MediaOverview(
            AggregateStatus(services),
            DateTimeOffset.UtcNow,
            services,
            qBittorrent,
            sonarr,
            radarr,
            prowlarr,
            jellyfin);
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
