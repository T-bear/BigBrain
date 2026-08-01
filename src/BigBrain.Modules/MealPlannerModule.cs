namespace BigBrain.Modules;

public static class MealPlannerModule
{
    public static ModuleDefinition Definition { get; } = new(
        Id: "meal-planner",
        Name: "Matlista",
        Description: "Familjens maträtter och sparade veckomatsedlar.",
        Route: "/#meal-planner",
        Status: "Available",
        DashboardWidgets:
        [
            new DashboardWidgetDefinition(
                Id: "meal-planner-overview",
                Title: "Matlista",
                Kind: "meal-planner",
                DataEndpoint: "/api/v1/modules/meal-planner/schedules")
        ],
        Capabilities:
        [
            "meal-planner.meals.manage",
            "meal-planner.tags.manage",
            "meal-planner.schedules.manage"
        ]);
}
