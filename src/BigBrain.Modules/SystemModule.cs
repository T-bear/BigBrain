namespace BigBrain.Modules;

public static class SystemModule
{
    public static ModuleDefinition Definition { get; } = new(
        Id: "system",
        Name: "System",
        Description: "Core system health and platform information.",
        Route: "/",
        Status: "Unavailable",
        DashboardWidgets:
        [
            new DashboardWidgetDefinition(
                Id: "system-overview",
                Title: "System overview",
                Kind: "system-overview",
                DataEndpoint: "/api/v1/system/overview")
        ],
        Capabilities: ["system.health.read", "system.overview.read"]);
}
