namespace BigBrain.Modules;

public static class MediaModule
{
    public static ModuleDefinition Definition { get; } = new(
        Id: "media",
        Name: "Media",
        Description: "Read-only status and activity from configured media services.",
        Route: "/#media",
        Status: "NotConfigured",
        DashboardWidgets:
        [
            new DashboardWidgetDefinition(
                Id: "media-overview",
                Title: "Media overview",
                Kind: "media-overview",
                DataEndpoint: "/api/v1/modules/media"),
            new DashboardWidgetDefinition(
                Id: "media-jobs",
                Title: "Media Jobs",
                Kind: "media-jobs",
                DataEndpoint: "/api/v1/modules/media/jobs")
        ],
        Capabilities: ["media.overview.read", "media.jobs.read", "media.play-metadata.read"]);
}
