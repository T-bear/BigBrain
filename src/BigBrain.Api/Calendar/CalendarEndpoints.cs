namespace BigBrain.Api.Calendar;

internal static class CalendarEndpoints
{
    public static IEndpointRouteBuilder MapCalendarEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/modules/calendar");
        group.MapGet("/week", async (DateOnly? date, CalendarStore store, CancellationToken token) => await Run(async () =>
        {
            var selected = date ?? StockholmToday();
            var offset = ((int)selected.DayOfWeek + 6) % 7;
            var from = selected.AddDays(-offset);
            var to = from.AddDays(6);
            return new CalendarWeekResponse(from, to, await store.GetEventsAsync(from, to, token));
        }));
        group.MapGet("/month", async (int year, int month, CalendarStore store, CancellationToken token) => await Run(async () =>
        {
            if (year is < 2000 or > 2100 || month is < 1 or > 12) throw new CalendarException(CalendarErrorCodes.InvalidRequest, "År eller månad är ogiltig.", 400);
            var from = new DateOnly(year, month, 1); var to = from.AddMonths(1).AddDays(-1);
            return new CalendarMonthResponse(year, month, await store.GetEventsAsync(from, to, token));
        }));
        group.MapGet("/imports", (CalendarStore store, CancellationToken token) => Run(() => store.GetImportsAsync(token)));
        group.MapPost("/import-preview", async (HttpRequest request, CalendarOptions options, CalendarImportService service, CancellationToken token) => await Run(async () =>
        {
            if (!request.HasFormContentType) throw new CalendarException(CalendarErrorCodes.UnsupportedFile, "Importen måste innehålla Excel-filer.", 415);
            var form = await request.ReadFormAsync(token);
            if (form.Files.Count is < 1) throw new CalendarException(CalendarErrorCodes.Empty, "Välj minst en fil.", 400);
            if (form.Files.Count > options.MaximumFilesPerRequest) throw new CalendarException(CalendarErrorCodes.TooLarge, "För många filer valdes.", 413);
            var files = new List<CalendarPreviewFileResult>();
            foreach (var file in form.Files) files.Add(await service.PreviewAsync(file, token));
            return new CalendarPreviewResponse(files);
        })).DisableAntiforgery();
        group.MapPost("/imports/{previewId}/confirm", async (string previewId, ConfirmCalendarImportRequest request, CalendarImportService service, CancellationToken token) => await Run(async () =>
        {
            if (!Enum.TryParse<CalendarImportStrategy>(request.Strategy, true, out var strategy)) throw new CalendarException(CalendarErrorCodes.InvalidRequest, "Importvalet är ogiltigt.", 400);
            return await service.ConfirmAsync(previewId, strategy, token);
        }));
        return app;
    }

    private static async Task<IResult> Run<T>(Func<Task<T>> action)
    {
        try { return Results.Ok(await action()); }
        catch (CalendarException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: "Kalenderimporten kunde inte slutföras.", detail: exception.Message, extensions: new Dictionary<string, object?> { ["code"] = exception.Code });
        }
    }

    private static DateOnly StockholmToday()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).DateTime);
    }
}
