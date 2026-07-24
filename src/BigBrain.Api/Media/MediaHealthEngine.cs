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
        var score = configured.Length == 0
            ? 0
            : configured.Sum(ServiceScore) / configured.Length;

        score -= Math.Min(
            20,
            (sonarr.HealthWarnings.Count + radarr.HealthWarnings.Count + prowlarr.HealthWarnings.Count) * 4);
        score -= prowlarr.EnabledIndexerCount > prowlarr.OnlineIndexerCount ? 10 : 0;
        score -= qBittorrent.ActiveCount > 0 && qBittorrent.DownloadSpeedBytesPerSecond == 0 ? 10 : 0;
        score -= qBittorrent.FreeSpaceBytes is > 0 and < 10L * 1024 * 1024 * 1024 ? 10 : 0;

        score = Math.Clamp(score, 0, 100);
        return score switch
        {
            >= 85 => new(score, "Everything looks great", "healthy"),
            >= 60 => new(score, "Some attention needed", "attention"),
            _ => new(score, "Action recommended", "critical")
        };
    }

    private static int ServiceScore(MediaServiceStatus service) => service.Status switch
    {
        MediaStatuses.Online => 100,
        MediaStatuses.Degraded => 50,
        _ => 0
    };
}
