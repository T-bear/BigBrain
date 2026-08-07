using BigBrain.Api.Calendar;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace BigBrain.Api.Tests;

public sealed class CalendarParserTests
{
    [Fact]
    public void ParsesSanitizedCalendarGridAndClassifiesEvents()
    {
        using var stream = CalendarFixture.Create("Augusti 2026", new Dictionary<int, string>
        {
            [3] = "3 Mån\nArbete 07:00-15:00",
            [4] = "4 Tis\nArbete 13:00-21:15",
            [5] = "5 Ons\nSamverkan 14:00-21:15",
            [6] = "6 Tor\nUTBILDNING 08:30-17:00",
            [7] = "7 Fre\n  semester  ",
            [10] = "10 Mån\nArbete 07:00-11:00\nArbete 12:00-16:00",
            [11] = "11 Tis\nMystery",
            [12] = "12 Ons\nLedig",
            [13] = "13 Tor\nArbete 22:00-06:00"
        });
        var result = new HeromaScheduleParser(new CalendarOptions()).Parse(stream);

        Assert.Equal((2026, 8), (result.Year, result.Month));
        Assert.Contains(result.Events, value => value.Date.Day == 3 && value.VisualClassification == CalendarVisualClassification.Day);
        Assert.Contains(result.Events, value => value.Date.Day == 4 && value.VisualClassification == CalendarVisualClassification.Evening);
        Assert.Contains(result.Events, value => value.EventType == CalendarEventType.Collaboration);
        Assert.Contains(result.Events, value => value.EventType == CalendarEventType.Education);
        Assert.Contains(result.Events, value => value.EventType == CalendarEventType.Vacation && value.IsAllDay);
        Assert.Equal(2, result.Events.Count(value => value.Date.Day == 10));
        Assert.Contains(result.Events, value => value.Date.Day == 11 && value.EventType == CalendarEventType.Other);
        Assert.DoesNotContain(result.Events, value => value.Date.Day == 12);
        Assert.Contains(result.Events, value => value.Date.Day == 13 && value.EndsNextDay);
    }

    [Fact]
    public void RejectsWrongSheetAndMissingWeekdays()
    {
        using var stream = CalendarFixture.Create("Schema", new Dictionary<int, string> { [3] = "3 Mån\n07:00-15:00" });
        var error = Assert.Throws<CalendarException>(() => new HeromaScheduleParser(new CalendarOptions()).Parse(stream));
        Assert.Equal(CalendarErrorCodes.InvalidStructure, error.Code);
    }

    [Fact]
    public void RejectsEmptyWorkbookAndNoEvents()
    {
        using var stream = CalendarFixture.Create("Augusti 2026", new Dictionary<int, string> { [3] = "3 Mån\nLedig" });
        var error = Assert.Throws<CalendarException>(() => new HeromaScheduleParser(new CalendarOptions()).Parse(stream));
        Assert.Equal(CalendarErrorCodes.NoEventsFound, error.Code);
    }
}

public sealed class CalendarStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"bigbrain-calendar-{Guid.NewGuid():N}");
    private readonly CalendarStore store;

    public CalendarStoreTests()
    {
        Directory.CreateDirectory(directory);
        store = new CalendarStore(new CalendarOptions { DatabasePath = Path.Combine(directory, "calendar.db") });
    }

    [Fact]
    public async Task ConfirmPersistsAndExactHashIsRejected()
    {
        var preview = Preview("hash-one", Event(3, new TimeOnly(7, 0), new TimeOnly(15, 0)));
        var result = await store.ConfirmAsync(preview, CalendarImportStrategy.Add, TestContext.Current.CancellationToken);
        Assert.Equal(1, result.ImportedEvents);
        Assert.Single(await store.GetEventsAsync(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), TestContext.Current.CancellationToken));
        var error = await Assert.ThrowsAsync<CalendarException>(() => store.ConfirmAsync(preview, CalendarImportStrategy.Add, TestContext.Current.CancellationToken));
        Assert.Equal(CalendarErrorCodes.Duplicate, error.Code);
    }

    [Fact]
    public async Task ReplaceOnlyChangesHeromaMonthAndMergeRejectsWorkConflict()
    {
        await store.ConfirmAsync(Preview("hash-a", Event(3, new TimeOnly(7, 0), new TimeOnly(15, 0))), CalendarImportStrategy.Add, TestContext.Current.CancellationToken);
        var conflict = Preview("hash-b", Event(3, new TimeOnly(13, 0), new TimeOnly(21, 0)));
        var error = await Assert.ThrowsAsync<CalendarException>(() => store.ConfirmAsync(conflict, CalendarImportStrategy.Merge, TestContext.Current.CancellationToken));
        Assert.Equal(CalendarErrorCodes.Conflict, error.Code);
        var replaced = await store.ConfirmAsync(conflict, CalendarImportStrategy.Replace, TestContext.Current.CancellationToken);
        Assert.Equal(1, replaced.ImportedEvents);
        var events = await store.GetEventsAsync(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), TestContext.Current.CancellationToken);
        Assert.Single(events);
        Assert.Equal(new TimeOnly(13, 0), events[0].StartTime);
    }

    private static StoredCalendarPreview Preview(string hash, params ParsedCalendarEvent[] events) => new("opaque", "sanitized.xlsx", hash, new HeromaParseResult(2026, 8, 8, 0, 0, events), DateTimeOffset.UtcNow.AddMinutes(5));
    private static ParsedCalendarEvent Event(int day, TimeOnly start, TimeOnly end) => new(new DateOnly(2026, 8, day), start, end, CalendarEventType.Work, start.Hour < 12 ? CalendarVisualClassification.Day : CalendarVisualClassification.Evening, "Arbetspass", "Arbete", false, end <= start);
    public void Dispose() { store.Dispose(); Directory.Delete(directory, true); }
}

public sealed class CalendarImportServiceTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"bigbrain-calendar-import-{Guid.NewGuid():N}");
    private readonly CalendarStore store;
    private readonly CalendarImportService service;

    public CalendarImportServiceTests()
    {
        Directory.CreateDirectory(directory);
        var options = new CalendarOptions { DatabasePath = Path.Combine(directory, "calendar.db") };
        store = new CalendarStore(options);
        service = new CalendarImportService(options, new HeromaScheduleParser(options), store, NullLogger<CalendarImportService>.Instance);
    }

    [Fact]
    public async Task PreviewDoesNotMutateAndConfirmPersists()
    {
        using var workbook = CalendarFixture.Create("Augusti 2026", new Dictionary<int, string> { [3] = "3 Mån\nArbete 07:00-15:00" });
        var preview = await service.PreviewAsync(Form(workbook, "synthetic.xlsx"), TestContext.Current.CancellationToken);
        Assert.NotNull(preview.Preview);
        Assert.Empty(await store.GetEventsAsync(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), TestContext.Current.CancellationToken));
        var result = await service.ConfirmAsync(preview.Preview!.PreviewId, CalendarImportStrategy.Add, TestContext.Current.CancellationToken);
        Assert.Equal(1, result.ImportedEvents);
        Assert.Single(await store.GetEventsAsync(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), TestContext.Current.CancellationToken));
        var second = await Assert.ThrowsAsync<CalendarException>(() => service.ConfirmAsync(preview.Preview.PreviewId, CalendarImportStrategy.Add, TestContext.Current.CancellationToken));
        Assert.Equal(CalendarErrorCodes.ExpiredPreview, second.Code);
    }

    [Fact]
    public async Task MultiplePreviewsAreIndependentAndInvalidFileIsSanitized()
    {
        using var first = CalendarFixture.Create("Augusti 2026", new Dictionary<int, string> { [3] = "3 Mån\n07:00-15:00" });
        using var second = CalendarFixture.Create("September 2026", new Dictionary<int, string> { [1] = "1 Tis\n13:00-21:00" }, 2026, 9);
        var results = await Task.WhenAll(service.PreviewAsync(Form(first, "one.xlsx"), TestContext.Current.CancellationToken), service.PreviewAsync(Form(second, "two.xlsx"), TestContext.Current.CancellationToken));
        Assert.All(results, value => Assert.NotNull(value.Preview));
        using var invalid = new MemoryStream("not a workbook"u8.ToArray());
        var rejected = await service.PreviewAsync(Form(invalid, "../../unsafe.xlsx"), TestContext.Current.CancellationToken);
        Assert.Equal("unsafe.xlsx", rejected.FileName);
        Assert.Equal(CalendarErrorCodes.UnsupportedFile, rejected.ErrorCode);
    }

    private static FormFile Form(MemoryStream stream, string name) { stream.Position = 0; return new FormFile(stream, 0, stream.Length, "files", name) { Headers = new HeaderDictionary(), ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }; }
    public void Dispose() { store.Dispose(); Directory.Delete(directory, true); }
}

internal static class CalendarFixture
{
    public static MemoryStream Create(string sheetName, IReadOnlyDictionary<int, string> days, int year = 2026, int month = 8)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(sheetName);
        sheet.Cell(1, 1).Value = sheetName;
        sheet.Range("A1:H1").Merge();
        var weekdays = new[] { "Mån", "Tis", "Ons", "Tor", "Fre", "Lör", "Sön" };
        for (var index = 0; index < weekdays.Length; index++) sheet.Cell(2, index + 2).Value = weekdays[index];
        foreach (var (day, value) in days)
        {
            var first = new DateOnly(year, month, 1);
            var offset = ((int)first.DayOfWeek + 6) % 7;
            var cellIndex = offset + day - 1;
            sheet.Cell(3 + cellIndex / 7, 2 + cellIndex % 7).Value = value;
        }
        var stream = new MemoryStream(); workbook.SaveAs(stream); stream.Position = 0; return stream;
    }
}
