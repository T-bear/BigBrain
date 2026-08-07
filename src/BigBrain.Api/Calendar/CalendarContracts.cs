using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BigBrain.Api.Calendar;

public sealed record CalendarOptions
{
    public const string SectionName = "Calendar";
    public string DatabasePath { get; init; } = "data/calendar.db";
    [Range(1, 20_000_000)] public int MaximumFileBytes { get; init; } = 5 * 1024 * 1024;
    [Range(1, 10)] public int MaximumFilesPerRequest { get; init; } = 6;
    [Range(1, 30)] public int PreviewLifetimeMinutes { get; init; } = 10;
    public int MaximumSheets { get; init; } = 12;
    public int MaximumRowsPerSheet { get; init; } = 500;
    public int MaximumEventsPerFile { get; init; } = 500;
}

[JsonConverter(typeof(JsonStringEnumConverter<CalendarEventType>))]
public enum CalendarEventType { Work, Education, Collaboration, Vacation, Other }
[JsonConverter(typeof(JsonStringEnumConverter<CalendarVisualClassification>))]
public enum CalendarVisualClassification { Day, Evening, Education, Collaboration, Vacation, Other, Unknown }
[JsonConverter(typeof(JsonStringEnumConverter<CalendarImportStrategy>))]
public enum CalendarImportStrategy { Add, Replace, Merge, Cancel }

public sealed record CalendarEvent(
    string Id, DateOnly Date, TimeOnly? StartTime, TimeOnly? EndTime,
    CalendarEventType EventType, CalendarVisualClassification VisualClassification,
    string Title, string Source, string SourceImportId, string? SourceLabel,
    bool IsAllDay, bool EndsNextDay, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CalendarImport(
    string ImportId, string OriginalFileName, string FileHash, DateTimeOffset ImportedAt,
    int Year, int Month, int NumberOfRows, int ImportedEvents, int SkippedRows,
    int WarningCount, string ParserVersion, string Status);

public sealed record CalendarMonthResponse(int Year, int Month, IReadOnlyList<CalendarEvent> Events);
public sealed record CalendarWeekResponse(DateOnly From, DateOnly To, IReadOnlyList<CalendarEvent> Events);

public sealed record CalendarPreviewCounts(int Total, int Day, int Evening, int Education, int Collaboration, int Vacation, int Other);
public sealed record CalendarImportPreview(
    string PreviewId, string FileName, int Year, int Month, CalendarPreviewCounts Counts,
    int SkippedRows, int WarningCount, bool MonthExists, bool ExactDuplicate,
    int ExistingEventCount, int ConflictCount, DateTimeOffset ExpiresAt);
public sealed record CalendarPreviewFileResult(string FileName, CalendarImportPreview? Preview, string? ErrorCode, string? Message);
public sealed record CalendarPreviewResponse(IReadOnlyList<CalendarPreviewFileResult> Files);
public sealed record ConfirmCalendarImportRequest(string Strategy);
public sealed record ConfirmCalendarImportResponse(string Status, CalendarImport? Import, int ImportedEvents, int SkippedDuplicates, int Conflicts);

public static class CalendarErrorCodes
{
    public const string UnsupportedFile = "calendarImportUnsupportedFile";
    public const string InvalidStructure = "calendarImportInvalidStructure";
    public const string Empty = "calendarImportEmpty";
    public const string TooLarge = "calendarImportTooLarge";
    public const string NoEventsFound = "calendarImportNoEventsFound";
    public const string Duplicate = "calendarImportDuplicate";
    public const string Conflict = "calendarImportConflict";
    public const string ExpiredPreview = "calendarImportExpiredPreview";
    public const string PersistenceFailure = "calendarImportPersistenceFailure";
    public const string InvalidRequest = "calendarImportInvalidRequest";
}

public sealed class CalendarException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}
