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
        group.MapGet("/search", async (string? query, string? author, string? language, IAudiobookshelfClient client, AudiobookAcquisitionService acquisition, CancellationToken token) =>
        {
            var value = query?.Trim() ?? string.Empty;
            if (value.Length is < 2 or > 120) return Problem("invalidQuery", "Sökningen måste vara 2–120 tecken.");
            var normalizedLanguage = string.IsNullOrWhiteSpace(language) || language.Equals("all", StringComparison.OrdinalIgnoreCase) ? null : AudiobookLanguages.Normalize(language);
            var local = await client.GetLibraryAsync(0, 25, value, normalizedLanguage, token);
            AudiobookAcquisitionProviderStatus status;
            IReadOnlyList<AudiobookAcquisitionCandidate> discovery;
            try
            {
                status = await acquisition.StatusAsync(token);
                discovery = await acquisition.SearchAsync(value, author, normalizedLanguage ?? AudiobookLanguages.Unknown, token);
            }
            catch (AudiobookAcquisitionException exception)
            {
                status = new("unavailable", "unknown", false, false, false, exception.SafeMessage);
                discovery = [];
            }
            return Results.Ok(new { library = local.Items, discovery, acquisition = status });
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
    private static bool SafeId(string value) => value.Length is > 0 and <= 64 && value.All(char.IsLetterOrDigit);
}

public sealed record AudiobookAcquisitionSearchInput(string? Query, string? Author, string? Language);
