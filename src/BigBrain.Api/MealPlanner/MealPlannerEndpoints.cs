namespace BigBrain.Api.MealPlanner;

public static class MealPlannerEndpoints
{
    public static IEndpointRouteBuilder MapMealPlannerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/modules/meal-planner");

        group.MapGet("/meals", async (string? tags, MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => Results.Ok(await service.GetMealsAsync(ParseTags(tags), token))));
        group.MapPost("/meals", async (CreateMealRequest request, MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => Results.Created($"/api/v1/modules/meal-planner/meals", await service.CreateMealAsync(request, token))));
        group.MapPost("/meals/seed-examples", async (MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => Results.Ok(await service.SeedExampleMealsAsync(token))));
        group.MapPut("/meals/{id}", async (string id, UpdateMealRequest request, MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => Results.Ok(await service.UpdateMealAsync(id, request, token))));
        group.MapDelete("/meals/{id}", async (string id, MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => { await service.DeleteMealAsync(id, token); return Results.NoContent(); }));

        group.MapGet("/tags", async (MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => Results.Ok(await service.GetTagsAsync(token))));
        group.MapPost("/tags", async (CreateTagRequest request, MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => Results.Created("/api/v1/modules/meal-planner/tags", await service.CreateTagAsync(request, token))));
        group.MapDelete("/tags/{id}", async (string id, MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => { await service.DeleteTagAsync(id, token); return Results.NoContent(); }));

        group.MapGet("/schedules", async (MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => Results.Ok(await service.GetSchedulesAsync(token))));
        group.MapGet("/schedules/{id}", async (string id, MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => Results.Ok(await service.GetScheduleAsync(id, token))));
        group.MapPost("/schedules/generate", async (GenerateSchedulesRequest request, MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => Results.Created($"/api/v1/modules/meal-planner/schedules", await service.GenerateAsync(request, token))));
        group.MapPut("/schedules/{scheduleId}/days/{date}/replace", async (string scheduleId, DateOnly date, ReplaceScheduleDayRequest request, MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => Results.Ok(await service.ReplaceAsync(scheduleId, date, MealPlannerMealTypes.Dinner, request, token))));
        group.MapPut("/schedules/{scheduleId}/days/{date}/meal", async (string scheduleId, DateOnly date, SetScheduleDayMealRequest request, MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => Results.Ok(await service.SetMealAsync(scheduleId, date, MealPlannerMealTypes.Dinner, request, token))));
        group.MapPut("/schedules/{scheduleId}/days/{date}/{mealType}/replace", async (string scheduleId, DateOnly date, string mealType, ReplaceScheduleDayRequest request, MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => Results.Ok(await service.ReplaceAsync(scheduleId, date, mealType, request, token))));
        group.MapPut("/schedules/{scheduleId}/days/{date}/{mealType}/meal", async (string scheduleId, DateOnly date, string mealType, SetScheduleDayMealRequest request, MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => Results.Ok(await service.SetMealAsync(scheduleId, date, mealType, request, token))));
        group.MapDelete("/schedules/{id}", async (string id, MealPlannerService service, CancellationToken token) =>
            await ExecuteAsync(async () => { await service.DeleteScheduleAsync(id, token); return Results.NoContent(); }));
        return app;
    }

    private static string[] ParseTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags) ? [] : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.Ordinal).ToArray();

    private static async Task<IResult> ExecuteAsync(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (MealPlannerException exception)
        {
            return Problem(exception.Code, exception.Message, exception.StatusCode);
        }
        catch (MealPlannerUnavailableException)
        {
            return Problem(MealPlannerErrorCodes.Unavailable, "Meal planner storage is unavailable.", StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static IResult Problem(string code, string detail, int statusCode) =>
        Results.Problem(statusCode: statusCode, title: "Meal planner request failed", detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });
}
