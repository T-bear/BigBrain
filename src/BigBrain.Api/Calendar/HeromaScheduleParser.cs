using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace BigBrain.Api.Calendar;

internal sealed record ParsedCalendarEvent(
    DateOnly Date, TimeOnly? StartTime, TimeOnly? EndTime, CalendarEventType EventType,
    CalendarVisualClassification VisualClassification, string Title, string? SourceLabel,
    bool IsAllDay, bool EndsNextDay)
{
    public string Identity => $"{Date:yyyy-MM-dd}|{StartTime:HH:mm}|{EndTime:HH:mm}|{EventType}|{SourceLabel}";
}

internal sealed record HeromaParseResult(int Year, int Month, int Rows, int SkippedRows, int WarningCount, IReadOnlyList<ParsedCalendarEvent> Events);

internal sealed partial class HeromaScheduleParser(CalendarOptions options)
{
    public const string ParserVersion = "heroma-calendar-grid-v1";
    private static readonly CultureInfo Swedish = CultureInfo.GetCultureInfo("sv-SE");
    private static readonly Dictionary<string, int> Months = new(StringComparer.OrdinalIgnoreCase)
    {
        ["januari"] = 1, ["februari"] = 2, ["mars"] = 3, ["april"] = 4,
        ["maj"] = 5, ["juni"] = 6, ["juli"] = 7, ["augusti"] = 8,
        ["september"] = 9, ["oktober"] = 10, ["november"] = 11, ["december"] = 12
    };

    public HeromaParseResult Parse(Stream stream)
    {
        if (stream.Length == 0) throw Error(CalendarErrorCodes.Empty, "Filen är tom.");
        try
        {
            using var workbook = new XLWorkbook(stream);
            if (workbook.Worksheets.Count is < 1 || workbook.Worksheets.Count > 12 || workbook.Worksheets.Count > options.MaximumSheets)
                throw Error(CalendarErrorCodes.InvalidStructure, "Filen har ett oväntat antal sheets.");

            var parsed = new List<HeromaParseResult>();
            foreach (var sheet in workbook.Worksheets)
            {
                if (!TryMonth(sheet.Name, out var year, out var month)) continue;
                var used = sheet.RangeUsed();
                if (used is null || used.RowCount() > options.MaximumRowsPerSheet || used.ColumnCount() < 8)
                    throw Error(CalendarErrorCodes.InvalidStructure, "Kalenderstrukturen är ogiltig.");
                ValidateWeekdays(sheet);

                var events = new List<ParsedCalendarEvent>();
                var skipped = 0;
                var warnings = 0;
                for (var row = 3; row <= used.LastRow().RowNumber(); row++)
                {
                    for (var column = 2; column <= 8; column++)
                    {
                        var text = Normalize(sheet.Cell(row, column).GetFormattedString());
                        if (string.IsNullOrWhiteSpace(text)) continue;
                        var match = DayCell().Match(text);
                        if (!match.Success || !int.TryParse(match.Groups["day"].Value, out var day) || day > DateTime.DaysInMonth(year, month))
                        {
                            skipped++;
                            continue;
                        }
                        var date = new DateOnly(year, month, day);
                        if ((int)date.DayOfWeek != (column - 1) % 7)
                        {
                            // Calendar exports include adjacent-month days in the first/last week.
                            continue;
                        }
                        var body = match.Groups["body"].Value.Trim();
                        var added = ParseDay(date, body).ToArray();
                        if (added.Length == 0)
                        {
                            if (!IsFree(body) && body.Length > 0) warnings++;
                            continue;
                        }
                        events.AddRange(added);
                    }
                }
                if (events.Count > options.MaximumEventsPerFile)
                    throw Error(CalendarErrorCodes.TooLarge, "Filen innehåller för många kalenderposter.");
                parsed.Add(new(year, month, used.RowCount(), skipped, warnings, events));
            }
            if (parsed.Count != 1)
                throw Error(CalendarErrorCodes.InvalidStructure, "Filen måste innehålla exakt en identifierbar Heroma-månad.");
            if (parsed[0].Events.Count == 0)
                throw Error(CalendarErrorCodes.NoEventsFound, "Inga schemaposter kunde hittas.");
            return parsed[0];
        }
        catch (CalendarException) { throw; }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw Error(CalendarErrorCodes.UnsupportedFile, "Filen kunde inte läsas som ett säkert Heroma-schema.");
        }
    }

    private static IEnumerable<ParsedCalendarEvent> ParseDay(DateOnly date, string body)
    {
        var type = ClassifyType(body);
        var times = TimeRange().Matches(body);
        if (times.Count == 0)
        {
            if (type is CalendarEventType.Vacation or CalendarEventType.Education or CalendarEventType.Collaboration or CalendarEventType.Other && !IsFree(body))
            {
                var visual = Visual(type, null);
                yield return new(date, null, null, type, visual, Title(type, visual), SafeLabel(body), true, false);
            }
            yield break;
        }

        foreach (Match match in times)
        {
            if (!TimeOnly.TryParseExact(match.Groups["start"].Value, "H:mm", Swedish, DateTimeStyles.None, out var start) &&
                !TimeOnly.TryParseExact(match.Groups["start"].Value, "HH:mm", Swedish, DateTimeStyles.None, out start)) continue;
            if (!TimeOnly.TryParseExact(match.Groups["end"].Value, "H:mm", Swedish, DateTimeStyles.None, out var end) &&
                !TimeOnly.TryParseExact(match.Groups["end"].Value, "HH:mm", Swedish, DateTimeStyles.None, out end)) continue;
            var line = body.Split('\n').FirstOrDefault(value => value.Contains(match.Value, StringComparison.Ordinal)) ?? body;
            var lineType = ClassifyType(line);
            if (lineType == CalendarEventType.Other) lineType = type == CalendarEventType.Other ? CalendarEventType.Work : type;
            var visual = Visual(lineType, start);
            var label = SafeLabel(TimeRange().Replace(line, string.Empty));
            yield return new(date, start, end, lineType, visual, Title(lineType, visual), label, false, end <= start);
        }
    }

    private static CalendarEventType ClassifyType(string value)
    {
        var normalized = Normalize(value).ToLower(Swedish);
        if (Regex.IsMatch(normalized, @"\butbild\w*|\bkurs\w*|\bstudie\w*")) return CalendarEventType.Education;
        if (Regex.IsMatch(normalized, @"\bsamverk\w*")) return CalendarEventType.Collaboration;
        if (Regex.IsMatch(normalized, @"\bsemester\w*|\bsem\b")) return CalendarEventType.Vacation;
        return TimeRange().IsMatch(normalized) ? CalendarEventType.Work : CalendarEventType.Other;
    }

    private static CalendarVisualClassification Visual(CalendarEventType type, TimeOnly? start) => type switch
    {
        CalendarEventType.Education => CalendarVisualClassification.Education,
        CalendarEventType.Collaboration => CalendarVisualClassification.Collaboration,
        CalendarEventType.Vacation => CalendarVisualClassification.Vacation,
        CalendarEventType.Work when start is not null && start < new TimeOnly(12, 0) => CalendarVisualClassification.Day,
        CalendarEventType.Work when start is not null => CalendarVisualClassification.Evening,
        CalendarEventType.Other => CalendarVisualClassification.Other,
        _ => CalendarVisualClassification.Unknown
    };

    private static string Title(CalendarEventType type, CalendarVisualClassification visual) => type switch
    {
        CalendarEventType.Education => "Utbildning",
        CalendarEventType.Collaboration => "Samverkan",
        CalendarEventType.Vacation => "Semester",
        CalendarEventType.Work when visual == CalendarVisualClassification.Day => "Dagpass",
        CalendarEventType.Work when visual == CalendarVisualClassification.Evening => "Kvällspass",
        _ => "Annan schemapost"
    };

    private static bool IsFree(string value) => Regex.IsMatch(Normalize(value), @"(?i)\bledig\w*|\bfridag\w*|\barbetsfri\w*|^\s*$");
    private static string Normalize(string value) => Regex.Replace(value.Replace('\r', '\n').Trim(), @"[ \t]+", " ");
    private static string? SafeLabel(string value)
    {
        var label = Normalize(value).Trim(' ', '-', '–', ':');
        if (label.Length == 0) return null;
        return label.Length <= 120 ? label : label[..120];
    }

    private static bool TryMonth(string value, out int year, out int month)
    {
        var match = MonthName().Match(value.Trim());
        year = match.Success && int.TryParse(match.Groups["year"].Value, out var parsedYear) ? parsedYear : 0;
        month = match.Success && Months.TryGetValue(match.Groups["month"].Value, out var parsedMonth) ? parsedMonth : 0;
        return year is >= 2000 and <= 2100 && month > 0;
    }

    private static void ValidateWeekdays(IXLWorksheet sheet)
    {
        var expected = new[] { "mån", "tis", "ons", "tor", "fre", "lör", "sön" };
        for (var column = 2; column <= 8; column++)
        {
            var value = Normalize(sheet.Cell(2, column).GetFormattedString()).ToLower(Swedish);
            if (!value.StartsWith(expected[column - 2], StringComparison.Ordinal))
                throw Error(CalendarErrorCodes.InvalidStructure, "Veckodagsrubrikerna saknas eller är ogiltiga.");
        }
    }

    private static CalendarException Error(string code, string message) => new(code, message, StatusCodes.Status400BadRequest);

    [GeneratedRegex(@"^(?<month>[A-Za-zÅÄÖåäö]+)\s+(?<year>\d{4})$")]
    private static partial Regex MonthName();
    [GeneratedRegex(@"^\s*(?<day>3[01]|[12]\d|[1-9])(?:\s+[A-Za-zÅÄÖåäö]+)?(?:\s*\n|\s+)?(?<body>[\s\S]*)$")]
    private static partial Regex DayCell();
    [GeneratedRegex(@"(?<start>(?:[01]?\d|2[0-3]):[0-5]\d)\s*[-–]\s*(?<end>(?:[01]?\d|2[0-3]):[0-5]\d)")]
    private static partial Regex TimeRange();
}
