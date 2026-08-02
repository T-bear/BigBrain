namespace BigBrain.Modules;

public static class ShoppingListModule
{
    public static ModuleDefinition Definition { get; } = new(
        Id: "shopping-list",
        Name: "Inköpslista",
        Description: "Familjens permanenta inköpslista med lokal butiksordning.",
        Route: "/#shopping-list",
        Status: "Available",
        DashboardWidgets: [new("shopping-list-overview", "Inköpslista", "shopping-list", "/api/v1/modules/shopping-list/items")],
        Capabilities: ["shopping-list.items.manage", "shopping-list.sessions.finish"]);
}
