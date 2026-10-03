using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

internal sealed record ProspectiveDailyCandidate(string Version, string Id, string Checksum, string ResearchRunId,
    string ResearchChecksum, string ResearchPayloadChecksum, string InstrumentId, string Provider, string Dataset,
    MarketObservationOrigin Origin, string MappingChecksum, string StrategyId, string StrategyVersion,
    int MomentumPeriod, DateTimeOffset FrozenAtUtc, DateTimeOffset KnowledgeCutoffUtc, string ProjectionChecksum,
    string FeatureChecksum, ImmutableArray<string> SourceReceiptIds, string ReferenceReceiptId,
    DateOnly ReferenceSession, decimal ReferenceClose, ResearchIntentKind Signal, string Horizon);
internal sealed record ProspectiveDailyResult(string Version, string Id, string Checksum, string CandidateId,
    string CandidateChecksum, DateTimeOffset CutoffUtc, string ProjectionChecksum, string State,
    string? OutcomeReceiptId, DateOnly? OutcomeSession, DateTimeOffset? OutcomeKnowledgeUtc,
    decimal? ObservedCloseReturn, string DirectionResult);

internal sealed partial class EodhdMarketMemory
{
    internal const string ProspectiveDailyMigration = """
        CREATE TABLE prospective_daily_candidates(id TEXT PRIMARY KEY,checksum TEXT NOT NULL,candidate_json TEXT NOT NULL);
        CREATE TABLE prospective_daily_results(id TEXT PRIMARY KEY,candidate_id TEXT NOT NULL REFERENCES prospective_daily_candidates(id),
          cutoff_ticks INTEGER NOT NULL,checksum TEXT NOT NULL,result_json TEXT NOT NULL,UNIQUE(candidate_id,cutoff_ticks));
        """;
    private const string DailyShadowVersion = "finance-prospective-daily-v1";
    private static readonly JsonSerializerOptions DailyShadowJson = new(JsonSerializerDefaults.Web)
    { UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow, PropertyNameCaseInsensitive = false };
    internal Action<string>? DailyShadowTestHook { get; set; }

    // No caller freeze time, supplied feature values, strategy code, grant, or simulation request.
    internal ProspectiveDailyCandidate FreezeDailyShadow(string researchRunId, ProviderInstrumentMapping mapping,
        MarketDataEntitlementPolicy policy, TimeProvider clock)
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireObservationSchema(connection);
        var now = clock.GetUtcNow(); AlpacaDailyOwnerDecision.Require(policy, now);
        var mappingHash = DailyHash(mapping);
        var id = MarketObservationIntegrity.Hash(new[] { DailyShadowVersion, researchRunId, mapping.InstrumentId.Value,
            mapping.Provider.Value, mapping.ProviderDataset.Value });
        var existing = ReadDailyCandidate(connection, id);
        if (existing is not null)
        {
            if (existing.MappingChecksum != mappingHash) throw new InvalidDataException("Frozen mapping conflict.");
            ValidateDailyCandidate(connection, existing, now);
            return existing; // One immutable freeze per research/source/instrument; never refresh a losing hypothesis.
        }
        var research = ReadDailyResearch(connection, researchRunId, now);
        if (research.Result.Configuration.Strategy != new StrategyIdentity("momentum", "v1") ||
            !research.Result.Configuration.Universe.Contains(mapping.InstrumentId.Value, StringComparer.Ordinal) ||
            research.Result.Configuration.StrategyParameters.Count != 1 ||
            !research.Result.Configuration.StrategyParameters.TryGetValue("period", out var period) || period is not (5 or 10 or 20))
            throw new InvalidDataException("Unsupported persisted research identity/parameters.");
        var rows = DailyRows(connection, now, mapping.InstrumentId.Value, mapping.Provider.Value, mapping.ProviderDataset.Value);
        if (rows.Length == 0 || rows.Any(x => x.Mapping != mapping) || rows.Select(x => x.Origin).Distinct().Count() != 1)
            throw new InvalidDataException("Daily freeze source/mapping is unavailable or mixed.");
        var selected = LatestDailyAtCutoff(rows);
        var features = DeterministicDailyFeatureEngine.Build(selected.Select((x, index) => new DailyFeatureObservation(
            x.Instrument.Id, x.Value.Daily!.SourceDate, x.Value.Open, x.Value.High, x.Value.Low, x.Value.Close, x.Value.Volume,
            x.KnowledgeTimeUtc, new(x.Id), index > 0 && HasDailyGap(selected[index - 1].Value.Daily!.SourceDate, x.Value.Daily.SourceDate))));
        var reference = selected[^1];
        var feature = features.Values.Single(x => x.SessionDate == reference.Value.Daily!.SourceDate && x.DefinitionId == $"momentum.{period}");
        if (feature.State != FeatureValueState.Available || feature.Quality != FeatureQualityState.Good || feature.Value is null)
            throw new InvalidDataException("Frozen daily research feature is not eligible.");
        var strategy = new MomentumResearchStrategy((int)period);
        var signal = strategy.Evaluate(new(mapping.InstrumentId, reference.Value.Daily!.SourceDate, now, reference.Value.Close,
            new Dictionary<string, decimal> { [feature.DefinitionId] = feature.Value.Value }, new(0, 0, reference.Value.Close, 0))).Kind;
        if (now.UtcTicks < ObservationWatermark(connection)) throw new InvalidDataException("Freeze clock regressed.");
        var candidate = new ProspectiveDailyCandidate(DailyShadowVersion, id, "", researchRunId, research.Result.Checksum,
            research.PayloadHash, mapping.InstrumentId.Value, mapping.Provider.Value, mapping.ProviderDataset.Value,
            reference.Origin, mappingHash, strategy.Identity.Id, strategy.Identity.Version, (int)period, now, now,
            DailyProjectionHash(rows), features.DeterministicChecksum, selected.Select(x => x.Id).ToImmutableArray(),
            reference.Id, reference.Value.Daily.SourceDate, reference.Value.Close, signal, FinanceShadowIdentity.Horizon);
        candidate = candidate with { Checksum = DailyHash(candidate) };
        Execute(connection, transaction, "INSERT INTO prospective_daily_candidates VALUES($id,$hash,$json)",
            ("$id", id), ("$hash", candidate.Checksum), ("$json", JsonSerializer.Serialize(candidate, DailyShadowJson)));
        SealDailyCutoff(connection, transaction, now);
        DailyShadowTestHook?.Invoke("before-freeze-commit"); transaction.Commit();
        return candidate;
    }

    internal ProspectiveDailyResult EvaluateDailyShadow(string candidateId, DateTimeOffset cutoff,
        MarketDataEntitlementPolicy policy, TimeProvider clock)
    {
        FinanceTime.RequireUtc(cutoff, nameof(cutoff));
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireObservationSchema(connection);
        var now = clock.GetUtcNow(); AlpacaDailyOwnerDecision.Require(policy, now);
        var candidate = ReadDailyCandidate(connection, candidateId) ?? throw new InvalidDataException("Missing frozen candidate.");
        if (cutoff < candidate.FrozenAtUtc || cutoff > now) throw new InvalidDataException("Invalid prospective cutoff.");
        ValidateDailyCandidate(connection, candidate, now);
        var known = DailyRows(connection, cutoff, candidate.InstrumentId, candidate.Provider, candidate.Dataset);
        if (known.Any(x => x.Origin != candidate.Origin || DailyHash(x.Mapping) != candidate.MappingChecksum))
            throw new InvalidDataException("Prospective source provenance mismatch.");
        // Reference-day corrections cannot rewrite the frozen reference. Never compare an earlier
        // session as a forward outcome. Late acquisition retains its own later knowledge time.
        var eligible = known.Where(x => x.KnowledgeTimeUtc > candidate.FrozenAtUtc &&
            x.Value.Daily!.SourceDate > candidate.ReferenceSession).ToImmutableArray();
        var selected = LatestDailyAtCutoff(eligible);
        var outcome = selected.FirstOrDefault();
        decimal? observedReturn = outcome is null ? null : outcome.Value.Close / candidate.ReferenceClose - 1m;
        var direction = outcome is null ? "NOT_AVAILABLE" :
            candidate.Signal == ResearchIntentKind.TargetLong ? (observedReturn > 0 ? "CORRECT" : "INCORRECT") :
            candidate.Signal == ResearchIntentKind.TargetFlat ? (observedReturn <= 0 ? "CORRECT" : "INCORRECT") : "NOT_APPLICABLE";
        var id = MarketObservationIntegrity.Hash(new[] { DailyShadowVersion, candidate.Id, MarketObservationIntegrity.Utc(cutoff) });
        var result = new ProspectiveDailyResult(DailyShadowVersion, id, "", candidate.Id, candidate.Checksum, cutoff,
            DailyProjectionHash(eligible), outcome is null ? "INSUFFICIENT_DATA" : "OBSERVED", outcome?.Id,
            outcome?.Value.Daily?.SourceDate, outcome?.KnowledgeTimeUtc, observedReturn, direction);
        result = result with { Checksum = DailyHash(result) };
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT checksum,result_json FROM prospective_daily_results WHERE id=$id";
        query.Parameters.AddWithValue("$id", id);
        using var reader = query.ExecuteReader();
        if (reader.Read())
        {
            if (reader.GetString(0) != result.Checksum || reader.GetString(1) != JsonSerializer.Serialize(result, DailyShadowJson))
                throw new InvalidDataException("Immutable prospective result conflict.");
            return result;
        }
        reader.Close();
        Execute(connection, transaction, "INSERT INTO prospective_daily_results VALUES($id,$candidate,$ticks,$hash,$json)",
            ("$id", id), ("$candidate", candidate.Id), ("$ticks", cutoff.UtcTicks), ("$hash", result.Checksum),
            ("$json", JsonSerializer.Serialize(result, DailyShadowJson)));
        SealDailyCutoff(connection, transaction, cutoff);
        DailyShadowTestHook?.Invoke("before-result-commit"); transaction.Commit();
        return result;
    }

    private static bool HasDailyGap(DateOnly previous, DateOnly current) =>
        UsMarketCalendar.CompletedSessionsAfter(previous, current) != 1;
    private static void SealDailyCutoff(SqliteConnection c, SqliteTransaction tx, DateTimeOffset cutoff) =>
        Execute(c, tx, "UPDATE market_observation_clock SET watermark_ticks=$t WHERE singleton=1", ("$t", Math.Max(ObservationWatermark(c), cutoff.UtcTicks)));
    private static string DailyHash<T>(T value) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value, DailyShadowJson)));
    private static string DailyProjectionHash(IEnumerable<MarketObservationReceipt> rows) =>
        MarketObservationIntegrity.Hash(new[] { DailyShadowVersion }.Concat(rows.Select(x => x.Checksum)));
    private static ImmutableArray<MarketObservationReceipt> LatestDailyAtCutoff(ImmutableArray<MarketObservationReceipt> rows) =>
        rows.GroupBy(x => x.LogicalIdentity).Select(g => g.Last()).OrderBy(x => x.Value.Daily!.SourceDate).ToImmutableArray();
    private static ImmutableArray<MarketObservationReceipt> DailyRows(SqliteConnection c, DateTimeOffset cutoff, string instrument, string provider, string dataset)
    {
        _ = ObservationWatermark(c);
        var rows = ReadObservationReceipts(c, "WHERE ingested_ticks<=$t ORDER BY ingested_ticks,id LIMIT 1001", ("$t", cutoff.UtcTicks));
        if (rows.Length > 1000) throw new InvalidDataException("Daily projection exceeds bounded capacity.");
        return rows.Where(x => x.Version == DailyMarketEvidence.Contract && x.Instrument.Id.Value == instrument &&
            x.Provider.Value == provider && x.Dataset.Value == dataset).ToImmutableArray();
    }
    private static ProspectiveDailyCandidate? ReadDailyCandidate(SqliteConnection c, string id)
    {
        using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT checksum,candidate_json FROM prospective_daily_candidates WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id); using var r = cmd.ExecuteReader(); if (!r.Read()) return null;
        if (r.GetString(1).Length > 131072) throw new InvalidDataException("Oversized candidate.");
        var value = JsonSerializer.Deserialize<ProspectiveDailyCandidate>(r.GetString(1), DailyShadowJson) ?? throw new InvalidDataException("Missing candidate.");
        if (value.Id != id || value.Id != MarketObservationIntegrity.Hash(new[] { DailyShadowVersion, value.ResearchRunId, value.InstrumentId, value.Provider, value.Dataset }) ||
            value.FrozenAtUtc.Offset != TimeSpan.Zero || value.Checksum != r.GetString(0) || value.Checksum != DailyHash(value with { Checksum = "" }) ||
            value.Version != DailyShadowVersion || value.Horizon != FinanceShadowIdentity.Horizon || value.KnowledgeCutoffUtc != value.FrozenAtUtc)
            throw new InvalidDataException("Candidate identity/provenance corrupt.");
        return value;
    }
    private static void ValidateDailyCandidate(SqliteConnection c, ProspectiveDailyCandidate value, DateTimeOffset now)
    {
        if (value.FrozenAtUtc > now) throw new InvalidDataException("Candidate clock regressed.");
        var research = ReadDailyResearch(c, value.ResearchRunId, value.FrozenAtUtc);
        if (research.Result.Checksum != value.ResearchChecksum || research.PayloadHash != value.ResearchPayloadChecksum)
            throw new InvalidDataException("Supporting research identity changed.");
        var rows = DailyRows(c, value.KnowledgeCutoffUtc, value.InstrumentId, value.Provider, value.Dataset);
        if (DailyProjectionHash(rows) != value.ProjectionChecksum ||
            !LatestDailyAtCutoff(rows).Select(x => x.Id).SequenceEqual(value.SourceReceiptIds))
            throw new InvalidDataException("Frozen evidence missing or inconsistent.");
    }
    private static (BacktestResult Result, string PayloadHash) ReadDailyResearch(SqliteConnection c, string id, DateTimeOffset now)
    {
        using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT checksum,result_json,created_utc FROM backtest_runs WHERE run_id=$id";
        cmd.Parameters.AddWithValue("$id", id); using var r = cmd.ExecuteReader();
        if (!r.Read() || !DateTimeOffset.TryParse(r.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.None, out var known) ||
            known.Offset != TimeSpan.Zero || known > now) throw new InvalidDataException("Supporting science was not known at freeze.");
        var json = r.GetString(1); if (json.Length > 16_777_216) throw new InvalidDataException("Oversized supporting science.");
        var result = JsonSerializer.Deserialize<BacktestResult>(json, BacktestJson) ?? throw new InvalidDataException("Missing science.");
        // Same canonical checksum as the existing deterministic engine; no evaluator is rerun.
        if (result.RunId != id || result.Checksum != r.GetString(0) || result.Checksum != DailyHash(result with { Checksum = "" }) || result.Status != "RESEARCH")
            throw new InvalidDataException("Supporting science integrity mismatch.");
        return (result, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
    }
}
