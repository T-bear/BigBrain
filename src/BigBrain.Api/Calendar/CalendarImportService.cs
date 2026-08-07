using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO.Compression;

namespace BigBrain.Api.Calendar;

internal sealed record StoredCalendarPreview(string PreviewId, string FileName, string FileHash, HeromaParseResult Parse, DateTimeOffset ExpiresAt);

internal sealed partial class CalendarImportService(CalendarOptions options, HeromaScheduleParser parser, CalendarStore store, ILogger<CalendarImportService> logger)
{
    private readonly ConcurrentDictionary<string, StoredCalendarPreview> previews = new(StringComparer.Ordinal);

    public async Task<CalendarPreviewFileResult> PreviewAsync(IFormFile file, CancellationToken token)
    {
        var name = SanitizeFileName(file.FileName);
        try
        {
            ValidateFile(file);
            await using var buffer = new MemoryStream((int)file.Length);
            await file.CopyToAsync(buffer, token);
            if (buffer.Length < 4 || !buffer.GetBuffer().AsSpan(0, 4).SequenceEqual(new byte[] { 0x50, 0x4b, 0x03, 0x04 }))
                throw new CalendarException(CalendarErrorCodes.UnsupportedFile, "Filen har inte ett giltigt Excel-format.", 400);
            ValidatePackage(buffer);
            var hash = Convert.ToHexString(SHA256.HashData(buffer.GetBuffer().AsSpan(0, (int)buffer.Length))).ToLowerInvariant();
            buffer.Position = 0;
            var parsed = parser.Parse(buffer);
            var inspection = await store.InspectAsync(hash, parsed.Year, parsed.Month, token);
            var conflictCount = CountConflicts(parsed.Events, inspection.Identities);
            var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
            var expires = DateTimeOffset.UtcNow.AddMinutes(options.PreviewLifetimeMinutes);
            var stored = new StoredCalendarPreview(id, name, hash, parsed, expires);
            previews[id] = stored;
            Prune();
            var counts = new CalendarPreviewCounts(
                parsed.Events.Count,
                parsed.Events.Count(value => value.VisualClassification == CalendarVisualClassification.Day),
                parsed.Events.Count(value => value.VisualClassification == CalendarVisualClassification.Evening),
                parsed.Events.Count(value => value.EventType == CalendarEventType.Education),
                parsed.Events.Count(value => value.EventType == CalendarEventType.Collaboration),
                parsed.Events.Count(value => value.EventType == CalendarEventType.Vacation),
                parsed.Events.Count(value => value.EventType == CalendarEventType.Other));
            PreviewCreated(logger, parsed.Year, parsed.Month, parsed.Events.Count, parsed.WarningCount, inspection.Exact);
            return new(name, new(id, name, parsed.Year, parsed.Month, counts, parsed.SkippedRows, parsed.WarningCount, inspection.ExistingCount > 0, inspection.Exact, inspection.ExistingCount, conflictCount, expires), null, null);
        }
        catch (CalendarException exception)
        {
            PreviewRejected(logger, exception.Code);
            return new(name, null, exception.Code, exception.Message);
        }
        catch (InvalidDataException)
        {
            PreviewRejected(logger, CalendarErrorCodes.UnsupportedFile);
            return new(name, null, CalendarErrorCodes.UnsupportedFile, "Filen kunde inte läsas som ett säkert Heroma-schema.");
        }
    }

    public async Task<ConfirmCalendarImportResponse> ConfirmAsync(string previewId, CalendarImportStrategy strategy, CancellationToken token)
    {
        if (!previews.TryGetValue(previewId, out var preview) || preview.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            previews.TryRemove(previewId, out _);
            throw new CalendarException(CalendarErrorCodes.ExpiredPreview, "Importförhandsgranskningen har gått ut. Försök igen.", 410);
        }
        if (strategy == CalendarImportStrategy.Cancel)
        {
            previews.TryRemove(previewId, out _);
            return new("cancelled", null, 0, 0, 0);
        }
        var result = await store.ConfirmAsync(preview, strategy, token);
        previews.TryRemove(previewId, out _);
        ImportConfirmed(logger, preview.Parse.Year, preview.Parse.Month, strategy, result.ImportedEvents);
        return result;
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length == 0) throw new CalendarException(CalendarErrorCodes.Empty, "Filen är tom.", 400);
        if (file.Length > options.MaximumFileBytes) throw new CalendarException(CalendarErrorCodes.TooLarge, "Filen är för stor.", 413);
        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase)) throw new CalendarException(CalendarErrorCodes.UnsupportedFile, "Endast .xlsx-filer stöds.", 415);
        var allowed = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/octet-stream" };
        if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase)) throw new CalendarException(CalendarErrorCodes.UnsupportedFile, "Filens MIME-typ stöds inte.", 415);
    }

    private static void ValidatePackage(MemoryStream buffer)
    {
        buffer.Position = 0;
        using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count is < 1 or > 200 || archive.Entries.Sum(entry => entry.Length) > 25 * 1024 * 1024)
            throw new CalendarException(CalendarErrorCodes.TooLarge, "Excel-filen expanderar till en orimlig storlek.", 413);
        if (archive.Entries.Any(entry => entry.FullName.Contains("vbaProject", StringComparison.OrdinalIgnoreCase) || entry.FullName.StartsWith("xl/externalLinks/", StringComparison.OrdinalIgnoreCase)))
            throw new CalendarException(CalendarErrorCodes.UnsupportedFile, "Makron eller externa workbooklänkar stöds inte.", 415);
        buffer.Position = 0;
    }

    public static string SanitizeFileName(string value)
    {
        var name = Path.GetFileName(value);
        name = UnsafeFileName().Replace(name, "_").Trim();
        if (name.Length == 0) name = "schema.xlsx";
        return name.Length <= 120 ? name : name[..115] + ".xlsx";
    }

    private static int CountConflicts(IReadOnlyList<ParsedCalendarEvent> proposed, HashSet<string> existing)
    {
        var dates = existing.Where(value => value.Contains("|Work|", StringComparison.Ordinal)).Select(value => value[..10]).ToHashSet();
        return proposed.Where(value => value.EventType == CalendarEventType.Work && dates.Contains(value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) && !existing.Contains(value.Identity)).Select(value => value.Date).Distinct().Count();
    }

    private void Prune()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in previews.Where(value => value.Value.ExpiresAt <= now)) previews.TryRemove(entry.Key, out _);
    }

    [GeneratedRegex(@"[^A-Za-z0-9ÅÄÖåäö._ -]")]
    private static partial Regex UnsafeFileName();

    [LoggerMessage(Level = LogLevel.Information, Message = "Calendar import preview created: month={Year}-{Month:D2}, events={Events}, warnings={Warnings}, duplicate={Duplicate}")]
    private static partial void PreviewCreated(ILogger logger, int year, int month, int events, int warnings, bool duplicate);
    [LoggerMessage(Level = LogLevel.Warning, Message = "Calendar import preview rejected: code={Code}")]
    private static partial void PreviewRejected(ILogger logger, string code);
    [LoggerMessage(Level = LogLevel.Information, Message = "Calendar import confirmed: month={Year}-{Month:D2}, strategy={Strategy}, events={Events}")]
    private static partial void ImportConfirmed(ILogger logger, int year, int month, CalendarImportStrategy strategy, int events);
}
