namespace BigBrain.Modules;

public static class CalendarModule
{
    public static ModuleDefinition Definition { get; } = new(
        Id: "calendar",
        Name: "Kalender",
        Description: "Central kalender med säker import av arbetsscheman.",
        Route: "/#calendar",
        Status: "Available",
        DashboardWidgets: [new("calendar", "Kalender", "calendar", "/api/v1/modules/calendar/week")],
        Capabilities: ["calendar.events.read", "calendar.heroma.import"]);
}
