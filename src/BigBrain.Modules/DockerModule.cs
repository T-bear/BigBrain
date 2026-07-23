namespace BigBrain.Modules;

public static class DockerModule
{
    public static ModuleDefinition Definition { get; } = new(
        Id: "docker",
        Name: "Docker",
        Description: "Read-only Docker container inventory.",
        Route: "/#docker",
        Status: "Unavailable",
        DashboardWidgets:
        [
            new DashboardWidgetDefinition(
                Id: "docker-overview",
                Title: "Docker overview",
                Kind: "docker-overview",
                DataEndpoint: "/api/v1/docker/containers")
        ],
        Capabilities: ["docker.containers.read"]);
}
