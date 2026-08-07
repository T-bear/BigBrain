namespace BigBrain.Api.Settings;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/settings");
        group.MapGet("/theme", (SettingsStore store, CancellationToken token) => store.GetThemeAsync(token));
        group.MapPut("/theme", async (ThemeSetting request, SettingsStore store, CancellationToken token) =>
        {
            try { return Results.Ok(await store.SetThemeAsync(request.Theme, token)); }
            catch (ArgumentException exception)
            {
                return Results.Problem(statusCode: 400, title: "Temat kunde inte sparas", detail: exception.Message,
                    extensions: new Dictionary<string, object?> { ["code"] = "settingsInvalidTheme" });
            }
        });
        return app;
    }
}
