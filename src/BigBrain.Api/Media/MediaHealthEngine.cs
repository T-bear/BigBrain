namespace BigBrain.Api.Media;

public sealed record MediaHealthAssessment(int Score, string Summary, string StatusLevel);

public interface IMediaHealthEngine
{
    MediaHealthAssessment Assess(
        IReadOnlyList<MediaServiceStatus> services,
        SonarrOverview sonarr,
        RadarrOverview radarr,
        ProwlarrOverview prowlarr,
        QBittorrentOverview qBittorrent);
}

public sealed class MediaHealthEngine : IMediaHealthEngine
{
    public MediaHealthAssessment Assess(
        IReadOnlyList<MediaServiceStatus> services,
        SonarrOverview sonarr,
        RadarrOverview radarr,
        ProwlarrOverview prowlarr,
        QBittorrentOverview qBittorrent)
    {
        var configured = services.Where(service => service.IsConfigured).ToArray();
        if (configured.Length == 0)
        {
            return new(0, "Configure media services to calculate health.", "notConfigured");
        }

        var score = configured.Sum(ServiceScore) / configured.Length;
        score -= Math.Min(
            20,
            (sonarr.HealthWarnings.Count + radarr.HealthWarnings.Count + prowlarr.HealthWarnings.Count) * 4);
        score -= prowlarr.EnabledIndexerCount > prowlarr.OnlineIndexerCount ? 10 : 0;
        score -= qBittorrent.ActiveCount > 0 && qBittorrent.DownloadSpeedBytesPerSecond == 0 ? 10 : 0;
        score -= qBittorrent.FreeSpaceBytes is > 0 and < 10L * 1024 * 1024 * 1024 ? 10 : 0;

        score = Math.Clamp(score, 0, 100);
        return score switch
        {
            >= 90 => new(score, "Everything looks great", "excellent"),
            >= 75 => new(score, "Your media stack is in good shape", "good"),
            >= 50 => new(score, "Some services need attention", "actionRecommended"),
            _ => new(score, "Immediate attention is recommended", "critical")
        };
    }

    private static int ServiceScore(MediaServiceStatus service) => service.Status switch
    {
        MediaStatuses.Online => 100,
        MediaStatuses.Degraded => 50,
        _ => 0
    };
}
