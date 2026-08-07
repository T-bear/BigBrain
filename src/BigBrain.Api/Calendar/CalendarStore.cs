using System.Globalization;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Calendar;

internal sealed class CalendarStore : IDisposable
{
    private readonly string connectionString;
    private readonly SemaphoreSlim gate = new(1, 1);
    public bool IsAvailable { get; private set; }

    public CalendarStore(CalendarOptions options)
    {
        try
        {
            var path = Path.GetFullPath(options.DatabasePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            connectionString = new SqliteConnectionStringBuilder { DataSource = path, ForeignKeys = true }.ToString();
            Initialize();
            IsAvailable = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SqliteException or InvalidDataException)
        {
            connectionString = string.Empty;
            IsAvailable = false;
        }
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(DateOnly from, DateOnly to, CancellationToken token)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id,EventDate,StartTime,EndTime,EventType,VisualClassification,Title,Source,SourceImportId,SourceLabel,IsAllDay,EndsNextDay,CreatedAtUtc,UpdatedAtUtc FROM CalendarEvents WHERE EventDate BETWEEN $from AND $to ORDER BY EventDate,COALESCE(StartTime,'00:00'),Id";
        command.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        var result = new List<CalendarEvent>();
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) result.Add(ReadEvent(reader));
        return result;
    }

    public async Task<IReadOnlyList<CalendarImport>> GetImportsAsync(CancellationToken token)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT ImportId,OriginalFileName,FileHash,ImportedAtUtc,DetectedYear,DetectedMonth,NumberOfRows,ImportedEvents,SkippedRows,WarningCount,ParserVersion,Status FROM CalendarImports ORDER BY ImportedAtUtc DESC LIMIT 100";
        var result = new List<CalendarImport>();
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) result.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), ParseOffset(reader.GetString(3)), reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6), reader.GetInt32(7), reader.GetInt32(8), reader.GetInt32(9), reader.GetString(10), reader.GetString(11)));
        return result;
    }

    public async Task<(bool Exact, int ExistingCount, HashSet<string> Identities)> InspectAsync(string hash, int year, int month, CancellationToken token)
    {
        await using var connection = await OpenAsync(token);
        await using var duplicate = connection.CreateCommand();
        duplicate.CommandText = "SELECT COUNT(*) FROM CalendarImports WHERE FileHash=$hash AND ParserVersion=$parser AND Status='completed'";
        duplicate.Parameters.AddWithValue("$hash", hash); duplicate.Parameters.AddWithValue("$parser", HeromaScheduleParser.ParserVersion);
        var exact = Convert.ToInt32(await duplicate.ExecuteScalarAsync(token), CultureInfo.InvariantCulture) > 0;
        await using var events = connection.CreateCommand();
        events.CommandText = "SELECT EventDate,StartTime,EndTime,EventType,COALESCE(SourceLabel,'') FROM CalendarEvents WHERE Source='heroma' AND substr(EventDate,1,7)=$month";
        events.Parameters.AddWithValue("$month", $"{year:D4}-{month:D2}");
        var identities = new HashSet<string>(StringComparer.Ordinal);
        await using var reader = await events.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) identities.Add($"{reader.GetString(0)}|{(reader.IsDBNull(1) ? string.Empty : reader.GetString(1))}|{(reader.IsDBNull(2) ? string.Empty : reader.GetString(2))}|{reader.GetString(3)}|{reader.GetString(4)}");
        return (exact, identities.Count, identities);
    }

    public async Task<ConfirmCalendarImportResponse> ConfirmAsync(StoredCalendarPreview preview, CalendarImportStrategy strategy, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            await using var connection = await OpenAsync(token);
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(token);
            if (await ExactExistsAsync(connection, transaction, preview.FileHash, token))
                throw new CalendarException(CalendarErrorCodes.Duplicate, "Filen har redan importerats.", StatusCodes.Status409Conflict);

            var inspection = await InspectWithinAsync(connection, transaction, preview.Parse.Year, preview.Parse.Month, token);
            if (strategy == CalendarImportStrategy.Add && inspection.Count > 0)
                throw new CalendarException(CalendarErrorCodes.Conflict, "Månaden finns redan. Välj Ersätt eller Slå ihop.", StatusCodes.Status409Conflict);
            if (strategy == CalendarImportStrategy.Cancel)
                return new("cancelled", null, 0, 0, 0);
            if (strategy == CalendarImportStrategy.Replace)
            {
                await using var remove = connection.CreateCommand(); remove.Transaction = transaction;
                remove.CommandText = "DELETE FROM CalendarEvents WHERE Source='heroma' AND substr(EventDate,1,7)=$month";
                remove.Parameters.AddWithValue("$month", $"{preview.Parse.Year:D4}-{preview.Parse.Month:D2}");
                await remove.ExecuteNonQueryAsync(token);
                inspection.Clear();
            }

            var conflicts = CountConflicts(preview.Parse.Events, inspection);
            if (strategy == CalendarImportStrategy.Merge && conflicts > 0)
                throw new CalendarException(CalendarErrorCodes.Conflict, "Olika arbetspass finns på samma datum. Konflikten måste lösas innan sammanslagning.", StatusCodes.Status409Conflict);

            var importId = Guid.NewGuid().ToString("N");
            var now = DateTimeOffset.UtcNow;
            var imported = 0;
            var skipped = 0;
            foreach (var parsed in preview.Parse.Events)
            {
                if (inspection.Contains(parsed.Identity)) { skipped++; continue; }
                await InsertEventAsync(connection, transaction, parsed, importId, now, token);
                inspection.Add(parsed.Identity);
                imported++;
            }
            var import = new CalendarImport(importId, preview.FileName, preview.FileHash, now, preview.Parse.Year, preview.Parse.Month, preview.Parse.Rows, imported, preview.Parse.SkippedRows + skipped, preview.Parse.WarningCount, HeromaScheduleParser.ParserVersion, "completed");
            await InsertImportAsync(connection, transaction, import, token);
            await transaction.CommitAsync(token);
            return new("completed", import, imported, skipped, conflicts);
        }
        catch (SqliteException)
        {
            throw new CalendarException(CalendarErrorCodes.PersistenceFailure, "Kalenderimporten kunde inte sparas.", StatusCodes.Status503ServiceUnavailable);
        }
        finally { gate.Release(); }
    }

    private static int CountConflicts(IReadOnlyList<ParsedCalendarEvent> proposed, HashSet<string> existing)
    {
        var existingWorkDates = existing.Where(value => value.Contains("|Work|", StringComparison.Ordinal)).Select(value => value[..10]).ToHashSet();
        return proposed.Where(value => value.EventType == CalendarEventType.Work && existingWorkDates.Contains(value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) && !existing.Contains(value.Identity)).Select(value => value.Date).Distinct().Count();
    }

    private static async Task InsertEventAsync(SqliteConnection connection, SqliteTransaction transaction, ParsedCalendarEvent value, string importId, DateTimeOffset now, CancellationToken token)
    {
        await using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "INSERT INTO CalendarEvents VALUES($id,$date,$start,$end,$type,$visual,$title,'heroma',$import,$label,$allDay,$nextDay,$created,$updated)";
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
        command.Parameters.AddWithValue("$date", value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$start", value.StartTime?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$end", value.EndTime?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$type", value.EventType.ToString()); command.Parameters.AddWithValue("$visual", value.VisualClassification.ToString());
        command.Parameters.AddWithValue("$title", value.Title); command.Parameters.AddWithValue("$import", importId);
        command.Parameters.AddWithValue("$label", value.SourceLabel ?? (object)DBNull.Value); command.Parameters.AddWithValue("$allDay", value.IsAllDay ? 1 : 0); command.Parameters.AddWithValue("$nextDay", value.EndsNextDay ? 1 : 0);
        command.Parameters.AddWithValue("$created", now.ToString("O")); command.Parameters.AddWithValue("$updated", now.ToString("O"));
        await command.ExecuteNonQueryAsync(token);
    }

    private static async Task InsertImportAsync(SqliteConnection connection, SqliteTransaction transaction, CalendarImport value, CancellationToken token)
    {
        await using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "INSERT INTO CalendarImports VALUES($id,$name,$hash,$at,$year,$month,$rows,$events,$skipped,$warnings,$parser,$status)";
        command.Parameters.AddWithValue("$id", value.ImportId); command.Parameters.AddWithValue("$name", value.OriginalFileName); command.Parameters.AddWithValue("$hash", value.FileHash); command.Parameters.AddWithValue("$at", value.ImportedAt.ToString("O")); command.Parameters.AddWithValue("$year", value.Year); command.Parameters.AddWithValue("$month", value.Month); command.Parameters.AddWithValue("$rows", value.NumberOfRows); command.Parameters.AddWithValue("$events", value.ImportedEvents); command.Parameters.AddWithValue("$skipped", value.SkippedRows); command.Parameters.AddWithValue("$warnings", value.WarningCount); command.Parameters.AddWithValue("$parser", value.ParserVersion); command.Parameters.AddWithValue("$status", value.Status);
        await command.ExecuteNonQueryAsync(token);
    }

    private static async Task<bool> ExactExistsAsync(SqliteConnection connection, SqliteTransaction transaction, string hash, CancellationToken token)
    {
        await using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = "SELECT COUNT(*) FROM CalendarImports WHERE FileHash=$hash AND ParserVersion=$parser AND Status='completed'"; command.Parameters.AddWithValue("$hash", hash); command.Parameters.AddWithValue("$parser", HeromaScheduleParser.ParserVersion); return Convert.ToInt32(await command.ExecuteScalarAsync(token), CultureInfo.InvariantCulture) > 0;
    }

    private static async Task<HashSet<string>> InspectWithinAsync(SqliteConnection connection, SqliteTransaction transaction, int year, int month, CancellationToken token)
    {
        await using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = "SELECT EventDate,StartTime,EndTime,EventType,COALESCE(SourceLabel,'') FROM CalendarEvents WHERE Source='heroma' AND substr(EventDate,1,7)=$month"; command.Parameters.AddWithValue("$month", $"{year:D4}-{month:D2}"); var result = new HashSet<string>(StringComparer.Ordinal); await using var reader = await command.ExecuteReaderAsync(token); while (await reader.ReadAsync(token)) result.Add($"{reader.GetString(0)}|{(reader.IsDBNull(1) ? string.Empty : reader.GetString(1))}|{(reader.IsDBNull(2) ? string.Empty : reader.GetString(2))}|{reader.GetString(3)}|{reader.GetString(4)}"); return result;
    }

    private void Initialize()
    {
        using var connection = new SqliteConnection(connectionString); connection.Open(); using var command = connection.CreateCommand(); command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS CalendarSchema(Version INTEGER NOT NULL);
            INSERT INTO CalendarSchema(Version) SELECT 1 WHERE NOT EXISTS(SELECT 1 FROM CalendarSchema);
            CREATE TABLE IF NOT EXISTS CalendarImports(ImportId TEXT PRIMARY KEY,OriginalFileName TEXT NOT NULL,FileHash TEXT NOT NULL,ImportedAtUtc TEXT NOT NULL,DetectedYear INTEGER NOT NULL,DetectedMonth INTEGER NOT NULL,NumberOfRows INTEGER NOT NULL,ImportedEvents INTEGER NOT NULL,SkippedRows INTEGER NOT NULL,WarningCount INTEGER NOT NULL,ParserVersion TEXT NOT NULL,Status TEXT NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS UX_CalendarImports_HashParser ON CalendarImports(FileHash,ParserVersion) WHERE Status='completed';
            CREATE TABLE IF NOT EXISTS CalendarEvents(Id TEXT PRIMARY KEY,EventDate TEXT NOT NULL,StartTime TEXT NULL,EndTime TEXT NULL,EventType TEXT NOT NULL,VisualClassification TEXT NOT NULL,Title TEXT NOT NULL,Source TEXT NOT NULL,SourceImportId TEXT NOT NULL REFERENCES CalendarImports(ImportId) DEFERRABLE INITIALLY DEFERRED,SourceLabel TEXT NULL,IsAllDay INTEGER NOT NULL,EndsNextDay INTEGER NOT NULL,CreatedAtUtc TEXT NOT NULL,UpdatedAtUtc TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS IX_CalendarEvents_Date ON CalendarEvents(EventDate);
            """; command.ExecuteNonQuery();
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken token) { if (!IsAvailable) throw new CalendarException(CalendarErrorCodes.PersistenceFailure, "Kalenderlagringen är inte tillgänglig.", 503); var connection = new SqliteConnection(connectionString); await connection.OpenAsync(token); return connection; }
    private static CalendarEvent ReadEvent(SqliteDataReader reader) => new(reader.GetString(0), DateOnly.ParseExact(reader.GetString(1), "yyyy-MM-dd", CultureInfo.InvariantCulture), reader.IsDBNull(2) ? null : TimeOnly.Parse(reader.GetString(2), CultureInfo.InvariantCulture), reader.IsDBNull(3) ? null : TimeOnly.Parse(reader.GetString(3), CultureInfo.InvariantCulture), Enum.Parse<CalendarEventType>(reader.GetString(4)), Enum.Parse<CalendarVisualClassification>(reader.GetString(5)), reader.GetString(6), reader.GetString(7), reader.GetString(8), reader.IsDBNull(9) ? null : reader.GetString(9), reader.GetInt32(10) == 1, reader.GetInt32(11) == 1, ParseOffset(reader.GetString(12)), ParseOffset(reader.GetString(13)));
    private static DateTimeOffset ParseOffset(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    public void Dispose() => gate.Dispose();
}
