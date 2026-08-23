namespace BigBrain.Api.Media;

public static class AudiobookEndpoints
{
    public static IEndpointRouteBuilder MapAudiobookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/modules/media/audiobooks");
        group.MapGet("/overview", async (IAudiobookshelfClient client, CancellationToken token) =>
            Results.Ok(await client.GetOverviewAsync(token)));
        group.MapGet("/library", async (int? page, int? limit, string? query, string? language, IAudiobookshelfClient client, CancellationToken token) =>
        {
            var normalizedPage = Math.Max(0, page ?? 0);
            var normalizedLimit = Math.Clamp(limit ?? 24, 1, 50);
            if (query?.Length > 120) return Problem("queryTooLong", "Sökningen får vara högst 120 tecken.");
            return Results.Ok(await client.GetLibraryAsync(normalizedPage, normalizedLimit, query, language, token));
        });
        group.MapGet("/search", async (string? query, string? language, IAudiobookshelfClient client, IAudiobookAcquisitionProvider provider, CancellationToken token) =>
        {
            var value = query?.Trim() ?? string.Empty;
            if (value.Length is < 2 or > 120) return Problem("invalidQuery", "Sökningen måste vara 2–120 tecken.");
            var normalizedLanguage = string.IsNullOrWhiteSpace(language) || language.Equals("all", StringComparison.OrdinalIgnoreCase) ? null : AudiobookLanguages.Normalize(language);
            var local = await client.GetLibraryAsync(0, 25, value, normalizedLanguage, token);
            var capabilities = await provider.GetCapabilitiesAsync(token);
            var discovery = capabilities.CanSearch ? await provider.SearchAsync(value, normalizedLanguage ?? AudiobookLanguages.Unknown, token) : [];
            return Results.Ok(new { library = local.Items, discovery = AudiobookRanking.Rank(discovery), acquisition = capabilities });
        });
        group.MapGet("/{id}", async (string id, IAudiobookshelfClient client, CancellationToken token) =>
            await client.GetItemAsync(id, token) is { } item ? Results.Ok(item) : Results.NotFound());
        group.MapGet("/{id}/cover", async (string id, IAudiobookshelfClient client, HttpContext context, CancellationToken token) =>
        {
            var cover = await client.GetCoverAsync(id, token);
            if (cover is null) return Results.NotFound();
            context.Response.Headers.CacheControl = "private,max-age=3600";
            return Results.File(cover.Value.Bytes, cover.Value.ContentType);
        });
        return app;
    }

    private static IResult Problem(string code, string detail) => Results.Problem(
        statusCode: StatusCodes.Status400BadRequest,
        title: "Audiobook request could not be completed",
        detail: detail,
        extensions: new Dictionary<string, object?> { ["code"] = code });
}
