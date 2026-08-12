using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

internal sealed record FinanceFeatureBuildEvidence(
    string RevisionId, IReadOnlyList<string> SourceMarketRevisions, int ValueCount,
    int AvailableCount, int WarmupCount, int QualityIssueCount, string Checksum,
    long BuildElapsedMilliseconds, bool Idempotent);

internal sealed partial class EodhdMarketMemory
{
    private static void InitializeFeatureStorage(SqliteConnection connection)
    {
        using var command = connection.CreateCommand(); command.CommandText = """
            CREATE TABLE IF NOT EXISTS feature_definitions(
              definition_id TEXT NOT NULL, version TEXT NOT NULL, fingerprint TEXT NOT NULL,
              definition_json TEXT NOT NULL, PRIMARY KEY(definition_id,version,fingerprint));
            CREATE TABLE IF NOT EXISTS feature_revisions(
              revision_id TEXT PRIMARY KEY, feature_set_id TEXT NOT NULL, feature_set_fingerprint TEXT NOT NULL,
              engine_version TEXT NOT NULL, source_revisions_json TEXT NOT NULL, coverage_from TEXT,
              coverage_to TEXT, value_count INTEGER NOT NULL, available_count INTEGER NOT NULL,
              warmup_count INTEGER NOT NULL, quality_issue_count INTEGER NOT NULL, checksum TEXT NOT NULL,
              created_utc TEXT NOT NULL, elapsed_ms INTEGER NOT NULL, price_basis TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS feature_values(
              revision_id TEXT NOT NULL, instrument_id TEXT NOT NULL, definition_id TEXT NOT NULL,
              definition_version TEXT NOT NULL, definition_fingerprint TEXT NOT NULL,
              session_date TEXT NOT NULL, value TEXT, source_revision_id TEXT NOT NULL,
              source_from TEXT NOT NULL, source_to TEXT NOT NULL, knowledge_utc TEXT NOT NULL,
              state TEXT NOT NULL, quality TEXT NOT NULL, engine_version TEXT NOT NULL,
              PRIMARY KEY(revision_id,instrument_id,definition_id,session_date));
            CREATE INDEX IF NOT EXISTS ix_feature_values_read
              ON feature_values(revision_id,instrument_id,definition_id,session_date);
            CREATE TABLE IF NOT EXISTS feature_deletion_receipts(
              receipt_id TEXT PRIMARY KEY, feature_values INTEGER NOT NULL, feature_revisions INTEGER NOT NULL);
            """; command.ExecuteNonQuery();
    }

    internal FinanceFeatureBuildEvidence BuildFeatures()
    {
        var watch = Stopwatch.StartNew();
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        var observations = ReadFeatureInputs(connection);
        if (observations.Count == 0) throw new InvalidOperationException("No canonical market observations are available for feature computation.");
        var build = DeterministicDailyFeatureEngine.Build(observations);
        var revisionId = $"feature-{build.DeterministicChecksum[7..23]}";
        var sourceRevisions = build.SourceRevisions.Select(value => value.Value).ToArray();
        var existing = ReadFeatureBuildEvidence(connection, revisionId);
        if (existing is not null) return existing with { Idempotent = true };

        var available = build.Values.Count(value => value.State == FeatureValueState.Available);
        var warmup = build.Values.Count(value => value.State == FeatureValueState.Warmup);
        var quality = build.Values.Count(value => value.Quality != FeatureQualityState.Good);
        var created = DateTimeOffset.UtcNow; watch.Stop();
        using var transaction = connection.BeginTransaction();
        foreach (var definition in build.Definitions)
            Execute(connection, transaction, """
                INSERT OR IGNORE INTO feature_definitions VALUES($id,$version,$fingerprint,$json)
                """, ("$id", definition.Id), ("$version", definition.Version),
                ("$fingerprint", definition.Fingerprint), ("$json", JsonSerializer.Serialize(definition)));
        Execute(connection, transaction, """
            INSERT INTO feature_revisions VALUES($id,$set,$setFingerprint,$engine,$sources,$from,$to,$values,$available,$warmup,$quality,$checksum,$created,$elapsed,$basis)
            """, ("$id", revisionId), ("$set", build.FeatureSetId), ("$setFingerprint", build.FeatureSetFingerprint),
            ("$engine", build.EngineVersion), ("$sources", JsonSerializer.Serialize(sourceRevisions)),
            ("$from", build.CoverageFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? (object)DBNull.Value),
            ("$to", build.CoverageTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? (object)DBNull.Value),
            ("$values", build.Values.Length), ("$available", available), ("$warmup", warmup), ("$quality", quality),
            ("$checksum", build.DeterministicChecksum), ("$created", created.ToString("O")),
            ("$elapsed", watch.ElapsedMilliseconds), ("$basis", "raw close/OHLC; provider volume classification; no adjusted-price mixing"));
        foreach (var value in build.Values)
            Execute(connection, transaction, """
                INSERT INTO feature_values VALUES($revision,$instrument,$definition,$version,$fingerprint,$date,$value,$sourceRevision,$sourceFrom,$sourceTo,$knowledge,$state,$quality,$engine)
                """, ("$revision", revisionId), ("$instrument", value.InstrumentId.Value),
                ("$definition", value.DefinitionId), ("$version", value.DefinitionVersion),
                ("$fingerprint", value.DefinitionFingerprint), ("$date", value.SessionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("$value", value.Value?.ToString(CultureInfo.InvariantCulture) ?? (object)DBNull.Value),
                ("$sourceRevision", value.SourceRevisionId.Value),
                ("$sourceFrom", value.SourceFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("$sourceTo", value.SourceTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("$knowledge", value.KnowledgeTimeUtc.ToString("O")), ("$state", value.State.ToString()),
                ("$quality", value.Quality.ToString()), ("$engine", value.EngineVersion));
        transaction.Commit();
        return new(revisionId, sourceRevisions, build.Values.Length, available, warmup, quality,
            build.DeterministicChecksum, watch.ElapsedMilliseconds, false);
    }

    internal FinanceFeatureSnapshot FeatureSnapshot(string? instrumentId, string? featureId,
        DateOnly? from, DateOnly? to, DateTimeOffset? knowledgeAsOfUtc, int limit)
    {
        if (knowledgeAsOfUtc is { } supplied && supplied.Offset != TimeSpan.Zero)
            throw new ArgumentException("Feature knowledge boundary must be UTC.", nameof(knowledgeAsOfUtc));
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        var revision = LatestFeatureRevision(connection, knowledgeAsOfUtc);
        var chosenInstrument = string.IsNullOrWhiteSpace(instrumentId) ? EodhdCatalog.Watchlist[0].InstrumentId :
            EodhdCatalog.Watchlist.Any(value => value.InstrumentId == instrumentId) ? instrumentId :
            throw new ArgumentException("Unknown Finance instrument ID.", nameof(instrumentId));
        var chosenFeature = string.IsNullOrWhiteSpace(featureId) ? "sma.20" :
            CoreDailyFeatureSet.Definitions.Any(value => value.Id == featureId) ? featureId :
            throw new ArgumentException("Unknown Finance feature definition ID.", nameof(featureId));
        if (to < from) throw new ArgumentException("Feature range end cannot precede start.", nameof(to));
        var boundedLimit = Math.Clamp(limit, 1, 500);
        if (revision is null)
            return new(DateTimeOffset.UtcNow, "research", CoreDailyFeatureSet.Id, chosenInstrument, CoreDailyFeatureSet.Definitions,
                null, [], chosenFeature, []);

        var latest = new List<FinanceFeatureLatestValue>();
        foreach (var definition in CoreDailyFeatureSet.Definitions)
        {
            using var command = connection.CreateCommand(); command.CommandText = """
                SELECT session_date,value,state,quality,knowledge_utc FROM feature_values
                WHERE revision_id=$revision AND instrument_id=$instrument AND definition_id=$definition
                  AND ($asOf IS NULL OR knowledge_utc <= $asOf)
                ORDER BY session_date DESC LIMIT 1
                """;
            command.Parameters.AddWithValue("$revision", revision.RevisionId);
            command.Parameters.AddWithValue("$instrument", chosenInstrument);
            command.Parameters.AddWithValue("$definition", definition.Id);
            command.Parameters.AddWithValue("$asOf", knowledgeAsOfUtc?.ToString("O") ?? (object)DBNull.Value);
            using var reader = command.ExecuteReader();
            if (!reader.Read()) continue;
            latest.Add(new(definition.Id, definition.Name, definition.Period,
                reader.IsDBNull(1) ? null : decimal.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                DateOnly.Parse(reader.GetString(0), CultureInfo.InvariantCulture),
                Enum.Parse<FeatureValueState>(reader.GetString(2)), Enum.Parse<FeatureQualityState>(reader.GetString(3)),
                DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture)));
        }

        var history = new List<FinanceFeatureHistoryPoint>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT session_date,value,state,quality,knowledge_utc FROM feature_values
                WHERE revision_id=$revision AND instrument_id=$instrument AND definition_id=$definition
                  AND ($from IS NULL OR session_date >= $from) AND ($to IS NULL OR session_date <= $to)
                  AND ($asOf IS NULL OR knowledge_utc <= $asOf)
                ORDER BY session_date DESC LIMIT $limit
                """;
            command.Parameters.AddWithValue("$revision", revision.RevisionId);
            command.Parameters.AddWithValue("$instrument", chosenInstrument);
            command.Parameters.AddWithValue("$definition", chosenFeature);
            command.Parameters.AddWithValue("$from", from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$to", to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$asOf", knowledgeAsOfUtc?.ToString("O") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$limit", boundedLimit);
            using var reader = command.ExecuteReader();
            while (reader.Read()) history.Add(new(DateOnly.Parse(reader.GetString(0), CultureInfo.InvariantCulture),
                reader.IsDBNull(1) ? null : decimal.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                Enum.Parse<FeatureValueState>(reader.GetString(2)), Enum.Parse<FeatureQualityState>(reader.GetString(3)),
                DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture)));
        }
        history.Reverse();
        return new(DateTimeOffset.UtcNow, "research", CoreDailyFeatureSet.Id, chosenInstrument, CoreDailyFeatureSet.Definitions,
            revision, latest, chosenFeature, history);
    }

    private static List<DailyFeatureObservation> ReadFeatureInputs(SqliteConnection connection)
    {
        using var command = connection.CreateCommand(); command.CommandText = """
            SELECT instrument_id,session_date,open,high,low,close,volume,acquired_utc,revision_id FROM (
              SELECT instrument_id,session_date,open,high,low,close,volume,acquired_utc,revision_id,
                ROW_NUMBER() OVER(PARTITION BY instrument_id,session_date ORDER BY acquired_utc DESC,revision_id DESC) AS rank
              FROM observations) WHERE rank=1 ORDER BY instrument_id,session_date
            """;
        using var reader = command.ExecuteReader(); var values = new List<DailyFeatureObservation>();
        while (reader.Read()) values.Add(new(new InstrumentId(reader.GetString(0)),
            DateOnly.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
            decimal.Parse(reader.GetString(2), CultureInfo.InvariantCulture), decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
            decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture), decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
            reader.IsDBNull(6) ? null : reader.GetInt64(6), DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
            new DatasetRevisionId(reader.GetString(8))));
        return values;
    }

    private static FinanceFeatureRevisionSummary? LatestFeatureRevision(SqliteConnection connection, DateTimeOffset? knowledgeAsOfUtc)
    {
        using var command = connection.CreateCommand(); command.CommandText = """
            SELECT revision_id,feature_set_id,feature_set_fingerprint,engine_version,source_revisions_json,
              coverage_from,coverage_to,value_count,available_count,warmup_count,quality_issue_count,
              checksum,created_utc,elapsed_ms,price_basis FROM feature_revisions
              WHERE ($asOf IS NULL OR created_utc <= $asOf) ORDER BY created_utc DESC,revision_id DESC LIMIT 1
            """;
        command.Parameters.AddWithValue("$asOf", knowledgeAsOfUtc?.ToString("O") ?? (object)DBNull.Value);
        using var reader = command.ExecuteReader(); if (!reader.Read()) return null;
        return new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            JsonSerializer.Deserialize<string[]>(reader.GetString(4)) ?? [],
            reader.IsDBNull(5) ? null : DateOnly.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
            reader.IsDBNull(6) ? null : DateOnly.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
            reader.GetInt32(7), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetString(11),
            DateTimeOffset.Parse(reader.GetString(12), CultureInfo.InvariantCulture), reader.GetInt64(13), reader.GetString(14), "durable");
    }

    private static FinanceFeatureBuildEvidence? ReadFeatureBuildEvidence(SqliteConnection connection, string revisionId)
    {
        using var command = connection.CreateCommand(); command.CommandText = """
            SELECT source_revisions_json,value_count,available_count,warmup_count,quality_issue_count,checksum,elapsed_ms
            FROM feature_revisions WHERE revision_id=$id
            """; command.Parameters.AddWithValue("$id", revisionId);
        using var reader = command.ExecuteReader(); if (!reader.Read()) return null;
        return new(revisionId, JsonSerializer.Deserialize<string[]>(reader.GetString(0)) ?? [], reader.GetInt32(1),
            reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetString(5), reader.GetInt64(6), true);
    }
}

internal sealed class EodhdFinanceFeatureReader(EodhdMarketMemory memory) : IFinanceFeatureReader
{
    public FinanceFeatureSnapshot GetSnapshot(string? instrumentId, string? featureId,
        DateOnly? from, DateOnly? to, DateTimeOffset? knowledgeAsOfUtc, int limit) =>
        memory.FeatureSnapshot(instrumentId, featureId, from, to, knowledgeAsOfUtc, limit);
}

internal sealed class FinanceFeatureBuildWorker(EodhdFinanceOptions options, EodhdMarketMemory memory, BigBrain.Api.SystemRecovery.SystemRecoveryCoordinator recovery) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await recovery.WaitUntilRecoveredAsync(stoppingToken);
        if (!options.Enabled || !options.AccountActive || options.EntitlementEndsAtUtc is { } end && end <= DateTimeOffset.UtcNow) return;
        await Task.Delay(TimeSpan.FromSeconds(12), stoppingToken);
        try { memory.BuildFeatures(); }
        catch (InvalidOperationException) { /* No local market revision yet; fail closed until the next restart/build. */ }
    }
}
