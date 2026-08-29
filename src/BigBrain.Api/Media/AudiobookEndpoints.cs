namespace BigBrain.Api.Media;

public static class AudiobookEndpoints
{
    public static IEndpointRouteBuilder MapAudiobookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/modules/media/audiobooks");
        group.MapGet("/overview", async (IAudiobookshelfClient client, CancellationToken token) =>
            Results.Ok(await client.GetOverviewAsync(token)));
        group.MapGet("/playback/availability", (AudiobookPlaybackService playback, CancellationToken token) =>
            Playback(async () => Results.Ok(await playback.VerifyAsync(token))));
        group.MapPost("/{id}/playback", (string id, AudiobookPlaybackService playback, CancellationToken token) =>
            Playback(async () => Results.Ok(await playback.StartAsync(id, token))));
        group.MapPost("/playback/sessions/{id}/sync", (string id, AudiobookPlaybackProgress input, AudiobookPlaybackService playback, CancellationToken token) =>
            Playback(async () => { await playback.SyncAsync(id, input, false, token); return Results.NoContent(); }));
        group.MapPost("/playback/sessions/{id}/close", (string id, AudiobookPlaybackProgress input, AudiobookPlaybackService playback, CancellationToken token) =>
            Playback(async () => { await playback.SyncAsync(id, input, true, token); return Results.NoContent(); }));
        group.MapGet("/playback/sessions/{id}/tracks/{trackIndex:int}", async (string id, int trackIndex, AudiobookPlaybackService playback, HttpContext context, CancellationToken token) =>
        {
            try { await playback.StreamAsync(id, trackIndex, context, token); }
            catch (AudiobookPlaybackException exception) when (!context.Response.HasStarted)
            {
                context.Response.StatusCode = exception.StatusCode;
                await Results.Problem(statusCode: exception.StatusCode, title: "Audiobook playback could not be completed", detail: exception.SafeMessage,
                    extensions: new Dictionary<string, object?> { ["code"] = exception.Code }).ExecuteAsync(context);
            }
        });
        group.MapGet("/library", async (int? page, int? limit, string? query, string? language, IAudiobookshelfClient client, CancellationToken token) =>
        {
            var normalizedPage = Math.Max(0, page ?? 0);
            var normalizedLimit = Math.Clamp(limit ?? 24, 1, 50);
            if (query?.Length > 120) return Problem("queryTooLong", "Sökningen får vara högst 120 tecken.");
            return Results.Ok(await client.GetLibraryAsync(normalizedPage, normalizedLimit, query, language, token));
        });
        group.MapGet("/search", async (string? query, string? author, string? language, AudiobookUniversalSearchService search, CancellationToken token) =>
        {
            var value = query?.Trim() ?? string.Empty;
            if (value.Length is < 2 or > 120) return Problem("invalidQuery", "Sökningen måste vara 2–120 tecken.");
            if (author?.Trim().Length > 120) return Problem("invalidAuthor", "Författaren får vara högst 120 tecken.");
            return Results.Ok(await search.SearchAsync(value, author, language, token));
        });
        group.MapGet("/metadata/covers/{id}", async (string id, IAudiobookMetadataProvider metadata, HttpContext context, CancellationToken token) =>
        {
            var cover = await metadata.GetCoverAsync(id, token);
            if (cover is null) return Results.NotFound();
            context.Response.Headers.CacheControl = "public,max-age=86400";
            return Results.File(cover.Value.Bytes, cover.Value.ContentType);
        });
        group.MapGet("/acquisition/provider-status", async (AudiobookAcquisitionService acquisition, CancellationToken token) =>
            Results.Ok(await acquisition.StatusAsync(token)));
        group.MapPost("/acquisition/search", (AudiobookAcquisitionSearchInput input, AudiobookAcquisitionService acquisition, CancellationToken token) =>
            Execute(async () => Results.Ok(await acquisition.SearchAsync(input.Query ?? "", input.Author, input.Language ?? AudiobookLanguages.Unknown, token))));
        group.MapPost("/acquisition/jobs", (AudiobookAcquisitionCandidate input, AudiobookAcquisitionService acquisition, CancellationToken token) =>
            Execute(async () =>
            {
                var job = await acquisition.RequestAsync(input, token);
                return Results.Created($"/api/v1/modules/media/audiobooks/acquisition/jobs/{job.Id}", job);
            }));
        group.MapGet("/acquisition/jobs", (int? offset, int? limit, AudiobookAcquisitionService acquisition, CancellationToken token) =>
            Execute(async () => Results.Ok(await acquisition.ListAsync(Math.Max(0, offset ?? 0), Math.Clamp(limit ?? 25, 1, 50), token))));
        group.MapGet("/acquisition/jobs/{id}", (string id, AudiobookAcquisitionService acquisition, CancellationToken token) =>
            SafeId(id) ? Execute(async () => Results.Ok(await acquisition.GetAsync(id, token))) : Task.FromResult(Problem("invalidJobId", "Jobb-ID är ogiltigt.")));
        group.MapPost("/acquisition/jobs/{id}/cancel", (string id, AudiobookAcquisitionService acquisition, CancellationToken token) =>
            SafeId(id) ? Execute(async () => Results.Ok(await acquisition.CancelAsync(id, token))) : Task.FromResult(Problem("invalidJobId", "Jobb-ID är ogiltigt.")));
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

    private static async Task<IResult> Execute(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (AudiobookAcquisitionException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: "Audiobook acquisition could not be completed",
                detail: exception.SafeMessage, extensions: new Dictionary<string, object?> { ["code"] = exception.Code });
        }
    }
    private static async Task<IResult> Playback(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (AudiobookPlaybackException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: "Audiobook playback could not be completed",
                detail: exception.SafeMessage, extensions: new Dictionary<string, object?> { ["code"] = exception.Code });
        }
    }
    private static bool SafeId(string value) => value.Length is > 0 and <= 64 && value.All(char.IsLetterOrDigit);
}

public sealed record AudiobookAcquisitionSearchInput(string? Query, string? Author, string? Language);
