namespace BigBrain.Modules;

public static class SystemModule
{
    public static ModuleDefinition Definition { get; } = new(
        Id: "system",
        Name: "System",
        Description: "Core system health and platform information.",
        Route: "/",
        DashboardWidgets:
        [
            new DashboardWidgetDefinition(
                Id: "system-health",
                Title: "System health",
                Kind: "health",
                DataEndpoint: "/api/v1/system/health")
        ],
        Capabilities: ["system.health.read"]);
}

