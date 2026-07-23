namespace BigBrain.Modules;

public sealed record ModuleDefinition(
    string Id,
    string Name,
    string Description,
    string Route,
    string Status,
    IReadOnlyList<DashboardWidgetDefinition> DashboardWidgets,
    IReadOnlyList<string> Capabilities);

public sealed record DashboardWidgetDefinition(
    string Id,
    string Title,
    string Kind,
    string DataEndpoint);
