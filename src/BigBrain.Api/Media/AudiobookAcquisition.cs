using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Text.Json;

namespace BigBrain.Api.Media;

public static class AudiobookAcquisitionStatuses
{
    public const string Requested = "requested";
    public const string Searching = "searching";
    public const string CandidateFound = "candidateFound";
    public const string AwaitingSelection = "awaitingSelection";
    public const string Queued = "queued";
    public const string Downloading = "downloading";
    public const string Importing = "importing";
    public const string Indexing = "indexing";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";

    public static bool IsKnown(string value) => value is Requested or Searching or CandidateFound or AwaitingSelection
        or Queued or Downloading or Importing or Indexing or Completed or Failed or Cancelled;
}

public sealed record AudiobookAcquisitionProviderStatus(
    string State, string Provider, bool CanSearch, bool CanRequest, bool CanCancel, string? Message);
public sealed record AudiobookAcquisitionCandidate(
    string WorkId, string EditionId, string Title, string? Author, string? Narrator,
    string Language, string LanguageLabel, string? Edition, double? DurationSeconds,
    int? PublicationYear, string? CoverUrl, string Source, string Availability, string LanguageConfidence,
    string? Provenance = null, string? MetadataWorkId = null, string? MatchEvidence = null);
public sealed record AudiobookAcquisitionRequest(string EditionId, string Source, string Language);
public sealed record AudiobookProviderJob(string ProviderJobId, string Status, string? Message);
public sealed record AudiobookAcquisitionJob(
    string Id, string? ProviderJobId, AudiobookAcquisitionCandidate Candidate, string Status,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, string? Message);
public sealed record AudiobookAcquisitionJobPage(IReadOnlyList<AudiobookAcquisitionJob> Items, int Offset, int Limit, int Total);

public sealed class AudiobookAcquisitionException(string code, string safeMessage, int statusCode) : Exception
{
    public string Code { get; } = code;
    public string SafeMessage { get; } = safeMessage;
    public int StatusCode { get; } = statusCode;
}

public sealed class AudiobookAcquisitionStore : IDisposable
{
    private readonly string connectionString;
    private readonly SemaphoreSlim gate = new(1, 1);

    public AudiobookAcquisitionStore(MediaOptions options)
    {
        var path = Path.GetFullPath(options.Audiobookshelf.AcquisitionDatabasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS AudiobookAcquisitionJobs(
                Id TEXT PRIMARY KEY,
                ProviderJobId TEXT NULL,
                CandidateJson TEXT NOT NULL,
                Status TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL,
                Message TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_AudiobookAcquisitionJobs_UpdatedAtUtc
                ON AudiobookAcquisitionJobs(UpdatedAtUtc DESC);
            """;
        command.ExecuteNonQuery();
    }

    public async Task<AudiobookAcquisitionJob> AddAsync(AudiobookAcquisitionJob job, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            await using var connection = await OpenAsync(token);
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO AudiobookAcquisitionJobs VALUES($id,$provider,$candidate,$status,$created,$updated,$message)";
            command.Parameters.AddWithValue("$id", job.Id);
            command.Parameters.AddWithValue("$provider", (object?)job.ProviderJobId ?? DBNull.Value);
            command.Parameters.AddWithValue("$candidate", JsonSerializer.Serialize(job.Candidate));
            command.Parameters.AddWithValue("$status", job.Status);
            command.Parameters.AddWithValue("$created", job.CreatedAtUtc.ToString("O"));
            command.Parameters.AddWithValue("$updated", job.UpdatedAtUtc.ToString("O"));
            command.Parameters.AddWithValue("$message", (object?)job.Message ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(token);
            return job;
        }
        finally { gate.Release(); }
    }

    public async Task<AudiobookAcquisitionJob?> GetAsync(string id, CancellationToken token)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM AudiobookAcquisitionJobs WHERE Id=$id";
        command.Parameters.AddWithValue("$id", id);
        await using var reader = await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token) ? Read(reader) : null;
    }

    public async Task<AudiobookAcquisitionJobPage> ListAsync(int offset, int limit, CancellationToken token)
    {
        await using var connection = await OpenAsync(token);
        await using var count = connection.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM AudiobookAcquisitionJobs";
        var total = Convert.ToInt32(await count.ExecuteScalarAsync(token), CultureInfo.InvariantCulture);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM AudiobookAcquisitionJobs ORDER BY UpdatedAtUtc DESC LIMIT $limit OFFSET $offset";
        command.Parameters.AddWithValue("$limit", limit);
        command.Parameters.AddWithValue("$offset", offset);
        await using var reader = await command.ExecuteReaderAsync(token);
        var items = new List<AudiobookAcquisitionJob>();
        while (await reader.ReadAsync(token)) items.Add(Read(reader));
        return new(items, offset, limit, total);
    }

    public async Task<AudiobookAcquisitionJob> UpdateAsync(AudiobookAcquisitionJob job, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            await using var connection = await OpenAsync(token);
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE AudiobookAcquisitionJobs SET ProviderJobId=$provider,Status=$status,UpdatedAtUtc=$updated,Message=$message WHERE Id=$id";
            command.Parameters.AddWithValue("$provider", (object?)job.ProviderJobId ?? DBNull.Value);
            command.Parameters.AddWithValue("$status", job.Status);
            command.Parameters.AddWithValue("$updated", job.UpdatedAtUtc.ToString("O"));
            command.Parameters.AddWithValue("$message", (object?)job.Message ?? DBNull.Value);
            command.Parameters.AddWithValue("$id", job.Id);
            await command.ExecuteNonQueryAsync(token);
            return job;
        }
        finally { gate.Release(); }
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken token)
    {
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(token);
        return connection;
    }
    private static AudiobookAcquisitionJob Read(SqliteDataReader reader) => new(
        reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1),
        JsonSerializer.Deserialize<AudiobookAcquisitionCandidate>(reader.GetString(2))!, reader.GetString(3),
        DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture), DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
        reader.IsDBNull(6) ? null : reader.GetString(6));
    public void Dispose() => gate.Dispose();
}

public sealed class AudiobookAcquisitionService(
    IAudiobookAcquisitionProvider provider,
    AudiobookAcquisitionStore store,
    TimeProvider clock)
{
    public async Task<AudiobookAcquisitionProviderStatus> StatusAsync(CancellationToken token)
    {
        try { return await provider.GetStatusAsync(token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch { throw ProviderUnavailable(); }
    }
    public Task<IReadOnlyList<AudiobookAcquisitionCandidate>> SearchAsync(string query, string? author, string language, CancellationToken token) =>
        SearchVariantsAsync([new(query, author, null, "literal")], language, token);

    public async Task<IReadOnlyList<AudiobookAcquisitionCandidate>> SearchVariantsAsync(
        IReadOnlyList<AudiobookDiscoverySeed> seeds, string language, CancellationToken token)
    {
        if (seeds.Count is < 1 or > AudiobookDiscoveryPlanner.MaximumProviderSearches)
            throw new AudiobookAcquisitionException("invalidQueryPlan", "Sökplanen är ogiltig.", StatusCodes.Status400BadRequest);
        foreach (var seed in seeds) ValidateSearch(seed.Query, seed.Author);
        var status = await StatusAsync(token);
        if (!status.CanSearch) return [];
        var searches = seeds.Select(async seed =>
        {
            try
            {
                var values = await provider.SearchAsync(seed.Query.Trim(), seed.Author?.Trim(), AudiobookLanguages.Normalize(language), token);
                return (Seed: seed, Values: values, Failed: false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch { return (Seed: seed, Values: (IReadOnlyList<AudiobookAcquisitionCandidate>)[], Failed: true); }
        }).ToArray();
        var completed = await Task.WhenAll(searches);
        if (completed.All(result => result.Failed)) throw ProviderUnavailable();
        var deduplicated = new Dictionary<string, AudiobookAcquisitionCandidate>(StringComparer.Ordinal);
        foreach (var result in completed.Where(result => !result.Failed))
        {
            foreach (var value in result.Values.Take(50))
            {
                var normalized = NormalizeProviderCandidate(value) with
                {
                    MetadataWorkId = result.Seed.MetadataWorkId,
                    MatchEvidence = result.Seed.MatchEvidence
                };
                if (!deduplicated.TryGetValue(normalized.EditionId, out var existing) || MatchScore(normalized.MatchEvidence) < MatchScore(existing.MatchEvidence))
                    deduplicated[normalized.EditionId] = normalized;
            }
        }
        var preferredLanguage = AudiobookLanguages.Normalize(language);
        return deduplicated.Values.Take(50)
            .OrderBy(x => MatchScore(x.MatchEvidence))
            .ThenBy(x => LanguagePreferenceScore(x, preferredLanguage))
            .ThenBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x.EditionId, StringComparer.Ordinal).ToArray();
    }
    public async Task<AudiobookAcquisitionJob> RequestAsync(AudiobookAcquisitionCandidate candidate, CancellationToken token)
    {
        ValidateCandidate(candidate);
        var status = await StatusAsync(token);
        if (!status.CanRequest) throw new AudiobookAcquisitionException("providerNotConfigured", status.Message ?? "Automatisk hämtning är inte konfigurerad.", StatusCodes.Status409Conflict);
        AudiobookProviderJob providerJob;
        try { providerJob = await provider.RequestAsync(new(candidate.EditionId, candidate.Source, candidate.Language), token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (AudiobookAcquisitionException) { throw; }
        catch { throw ProviderUnavailable(); }
        if (!AudiobookAcquisitionStatuses.IsKnown(providerJob.Status)) throw new AudiobookAcquisitionException("providerInvalidState", "Leverantören returnerade ett ogiltigt tillstånd.", StatusCodes.Status502BadGateway);
        var now = clock.GetUtcNow();
        return await store.AddAsync(new(Guid.NewGuid().ToString("N"), providerJob.ProviderJobId, candidate, providerJob.Status, now, now, providerJob.Message), token);
    }
    public Task<AudiobookAcquisitionJobPage> ListAsync(int offset, int limit, CancellationToken token) => store.ListAsync(offset, limit, token);
    public async Task<AudiobookAcquisitionJob> GetAsync(string id, CancellationToken token)
    {
        var job = await store.GetAsync(id, token) ?? throw new AudiobookAcquisitionException("jobNotFound", "Hämtningsjobbet hittades inte.", StatusCodes.Status404NotFound);
        if (string.IsNullOrWhiteSpace(job.ProviderJobId) || job.Status is AudiobookAcquisitionStatuses.Completed or AudiobookAcquisitionStatuses.Failed or AudiobookAcquisitionStatuses.Cancelled) return job;
        AudiobookProviderJob? status;
        try { status = await provider.GetJobStatusAsync(job.ProviderJobId, token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch { throw ProviderUnavailable(); }
        if (status is null || !AudiobookAcquisitionStatuses.IsKnown(status.Status)) return job;
        return await store.UpdateAsync(job with { Status = status.Status, UpdatedAtUtc = clock.GetUtcNow(), Message = status.Message }, token);
    }
    public async Task<AudiobookAcquisitionJob> CancelAsync(string id, CancellationToken token)
    {
        var job = await GetAsync(id, token);
        if (job.Status is AudiobookAcquisitionStatuses.Completed or AudiobookAcquisitionStatuses.Failed or AudiobookAcquisitionStatuses.Cancelled)
            throw new AudiobookAcquisitionException("jobNotCancellable", "Hämtningsjobbet kan inte avbrytas.", StatusCodes.Status409Conflict);
        var status = await StatusAsync(token);
        if (!status.CanCancel || string.IsNullOrWhiteSpace(job.ProviderJobId))
            throw new AudiobookAcquisitionException("cancelUnsupported", "Leverantören stöder inte säker avbrytning.", StatusCodes.Status409Conflict);
        AudiobookProviderJob result;
        try { result = await provider.CancelAsync(job.ProviderJobId, token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (AudiobookAcquisitionException) { throw; }
        catch { throw ProviderUnavailable(); }
        if (!AudiobookAcquisitionStatuses.IsKnown(result.Status))
            throw new AudiobookAcquisitionException("providerInvalidState", "Leverantören returnerade ett ogiltigt tillstånd.", StatusCodes.Status502BadGateway);
        var now = clock.GetUtcNow();
        return await store.UpdateAsync(job with { Status = result.Status, UpdatedAtUtc = now, Message = result.Message }, token);
    }
    private static void ValidateSearch(string query, string? author)
    {
        if (query.Trim().Length is < 2 or > 120) throw new AudiobookAcquisitionException("invalidQuery", "Sökningen måste vara 2–120 tecken.", StatusCodes.Status400BadRequest);
        if (author?.Trim().Length > 120) throw new AudiobookAcquisitionException("invalidAuthor", "Författaren får vara högst 120 tecken.", StatusCodes.Status400BadRequest);
    }
    private static void ValidateCandidate(AudiobookAcquisitionCandidate value)
    {
        static bool Opaque(string text) => text.Length is > 0 and <= 160 && text.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or ':');
        if (!Opaque(value.WorkId) || !Opaque(value.EditionId) || !Opaque(value.Source) || value.Title.Trim().Length is < 1 or > 300
            || value.Author?.Length > 300 || value.Narrator?.Length > 300 || value.Edition?.Length > 160
            || value.MetadataWorkId is not null && !Opaque(value.MetadataWorkId)
            || value.MatchEvidence is not null && !KnownMatchEvidence(value.MatchEvidence)
            || value.CoverUrl is not null && !SafeCoverUrl(value.CoverUrl))
            throw new AudiobookAcquisitionException("invalidCandidate", "Utgåvan är ogiltig.", StatusCodes.Status400BadRequest);
        if (AudiobookLanguages.Normalize(value.Language) == AudiobookLanguages.Unknown && value.Language != AudiobookLanguages.Unknown)
            throw new AudiobookAcquisitionException("invalidLanguage", "Språkkoden stöds inte.", StatusCodes.Status400BadRequest);
    }
    private static AudiobookAcquisitionException ProviderUnavailable() =>
        new("providerUnavailable", "Anskaffningsleverantören kunde inte nås.", StatusCodes.Status503ServiceUnavailable);

    private static AudiobookAcquisitionCandidate NormalizeProviderCandidate(AudiobookAcquisitionCandidate value)
    {
        var language = AudiobookLanguages.Normalize(value.Language);
        var confidence = value.LanguageConfidence is "verified" or "probable" ? value.LanguageConfidence : "unknown";
        var normalized = value with
        {
            Language = language,
            LanguageLabel = AudiobookLanguages.DisplayName(language),
            LanguageConfidence = confidence,
            CoverUrl = value.CoverUrl is not null && SafeCoverUrl(value.CoverUrl) ? value.CoverUrl : null
        };
        ValidateCandidate(normalized);
        return normalized;
    }

    private static int MatchScore(string? evidence) => evidence switch
    {
        "identifier" => 0,
        "canonicalTitleAuthor" => 1,
        "canonicalTitle" => 2,
        "alternateTitle" => 3,
        "series" => 4,
        "authorWork" => 5,
        _ => 6
    };
    private static int LanguagePreferenceScore(AudiobookAcquisitionCandidate candidate, string preferred) => preferred switch
    {
        "sv" => AudiobookRanking.Score(candidate.Language, candidate.LanguageConfidence, "sv", "en"),
        "en" => AudiobookRanking.Score(candidate.Language, candidate.LanguageConfidence, "en", "sv"),
        _ => 0
    };
    private static bool KnownMatchEvidence(string value) =>
        value is "identifier" or "canonicalTitleAuthor" or "canonicalTitle" or "alternateTitle" or "series" or "authorWork" or "literal";

    private static bool SafeCoverUrl(string value) =>
        value.StartsWith("/api/v1/modules/media/audiobooks/", StringComparison.Ordinal) && !value.Contains("..", StringComparison.Ordinal);
}

public static class AudiobookImportPolicy
{
    public static string ResolveUnderRoot(string root, string relativePath)
    {
        if (Path.IsPathRooted(relativePath) || string.IsNullOrWhiteSpace(relativePath)) throw new ArgumentException("Import path must be relative.");
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(fullRoot, relativePath));
        if (!full.StartsWith(fullRoot, StringComparison.Ordinal)) throw new ArgumentException("Import path escapes the configured root.");
        return full;
    }

    public static string ResolveNewDestination(string root, string relativePath)
    {
        var destination = ResolveUnderRoot(root, relativePath);
        if (File.Exists(destination) || Directory.Exists(destination))
            throw new IOException("Import destination already exists; existing audiobook data will not be overwritten.");
        return destination;
    }
}
