namespace BigBrain.Api.Media;

public sealed class MediaOptions
{
    public const string SectionName = "Media";

    public MediaApiKeyOptions Jellyfin { get; init; } = new("http://jellyfin:8096");
    public MediaApiKeyOptions Sonarr { get; init; } = new("http://sonarr:8989");
    public MediaApiKeyOptions Radarr { get; init; } = new("http://radarr:7878");
    public MediaApiKeyOptions Prowlarr { get; init; } = new("http://prowlarr:9696");
    public QBittorrentOptions QBittorrent { get; init; } = new();
    public MediaRequestOptions Requests { get; init; } = new();
    public SmartShuffleOptions SmartShuffle { get; init; } = new();
    public MediaServiceLinksOptions ServiceLinks { get; init; } = new();
    public int TimeoutSeconds { get; init; } = 3;

    public static bool IsValid(MediaOptions options) =>
        options.TimeoutSeconds is >= 1 and <= 15
        && IsHttpUrl(options.Jellyfin.BaseUrl)
        && IsHttpUrl(options.Sonarr.BaseUrl)
        && IsHttpUrl(options.Radarr.BaseUrl)
        && IsHttpUrl(options.Prowlarr.BaseUrl)
        && IsHttpUrl(options.QBittorrent.BaseUrl)
        && options.ServiceLinks.All.All(IsValidServiceLink)
        && options.Requests.PreviewTokenLifetimeMinutes is >= 1 and <= 15
        && options.Requests.MaximumConcurrentRequests is >= 1 and <= 4
        && (!options.SmartShuffle.Enabled || !string.IsNullOrWhiteSpace(options.Jellyfin.UserId));

    public bool IsAnyServiceConfigured =>
        !string.IsNullOrWhiteSpace(Jellyfin.ApiKey)
        || !string.IsNullOrWhiteSpace(Sonarr.ApiKey)
        || !string.IsNullOrWhiteSpace(Radarr.ApiKey)
        || !string.IsNullOrWhiteSpace(Prowlarr.ApiKey)
        || !string.IsNullOrWhiteSpace(QBittorrent.ApiKey);

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && string.IsNullOrEmpty(uri.Query)
        && string.IsNullOrEmpty(uri.Fragment);

    private static bool IsValidServiceLink(MediaServiceLinkOptions link) =>
        !link.Enabled || IsHttpUrl(link.Url);
}

public sealed class MediaRequestOptions
{
    public bool Enabled { get; init; } = true;
    public bool DefaultSearchAfterAdd { get; init; }
    public int PreviewTokenLifetimeMinutes { get; init; } = 5;
    public int MaximumConcurrentRequests { get; init; } = 1;
}

public sealed class MediaApiKeyOptions(string baseUrl)
{
    public string BaseUrl { get; init; } = baseUrl;
    public string? ApiKey { get; init; }
    public string? UserId { get; init; }
}

public sealed class SmartShuffleOptions
{
    public bool Enabled { get; init; }
}

public sealed class QBittorrentOptions
{
    public string BaseUrl { get; init; } = "http://qbittorrent:8080";
    public string? ApiKey { get; init; }
}

public sealed class MediaServiceLinksOptions
{
    public MediaServiceLinkOptions Jellyfin { get; init; } = new();
    public MediaServiceLinkOptions Radarr { get; init; } = new();
    public MediaServiceLinkOptions Sonarr { get; init; } = new();
    public MediaServiceLinkOptions Prowlarr { get; init; } = new();
    public MediaServiceLinkOptions QBittorrent { get; init; } = new();

    internal IReadOnlyList<MediaServiceLinkOptions> All =>
        [Jellyfin, Radarr, Sonarr, Prowlarr, QBittorrent];
}

public sealed class MediaServiceLinkOptions
{
    public string Url { get; init; } = string.Empty;
    public bool Enabled { get; init; }
}

public sealed record MediaServiceLink(string Id, string DisplayName, string Url, bool Enabled);

public static class MediaServiceLinks
{
    public static IReadOnlyList<MediaServiceLink> From(MediaOptions options) =>
    [
        Create("jellyfin", "Jellyfin", options.ServiceLinks.Jellyfin),
        Create("radarr", "Radarr", options.ServiceLinks.Radarr),
        Create("sonarr", "Sonarr", options.ServiceLinks.Sonarr),
        Create("prowlarr", "Prowlarr", options.ServiceLinks.Prowlarr),
        Create("qbittorrent", "qBittorrent", options.ServiceLinks.QBittorrent)
    ];

    private static MediaServiceLink Create(string id, string name, MediaServiceLinkOptions options) =>
        new(id, name, options.Enabled ? options.Url : string.Empty, options.Enabled);
}
