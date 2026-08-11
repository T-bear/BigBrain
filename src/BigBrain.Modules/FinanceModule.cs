namespace BigBrain.Modules;

public static class FinanceModule
{
    public static ModuleDefinition Definition { get; } = new(
        Id: "finance",
        Name: "Finance",
        Description: "Forsknings- och evidensgrund för framtida policy-governed paper trading.",
        Route: "/#finance",
        Status: "Research",
        DashboardWidgets:
        [
            new DashboardWidgetDefinition(
                "finance-observation",
                "Finance observation",
                "finance.read-only-observation",
                "/api/v1/modules/finance/observation")
        ],
        Capabilities: ["finance.research.read"]);
}
