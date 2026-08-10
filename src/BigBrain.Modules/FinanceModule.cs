namespace BigBrain.Modules;

public static class FinanceModule
{
    public static ModuleDefinition Definition { get; } = new(
        Id: "finance",
        Name: "Finance",
        Description: "Forsknings- och evidensgrund för framtida policy-governed paper trading.",
        Route: "/#finance",
        Status: "Research",
        DashboardWidgets: [],
        Capabilities: ["finance.research.read"]);
}
