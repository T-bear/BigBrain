using System.Collections.Immutable;
using System.Globalization;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

internal sealed record HistoricalResearchRow(string Provider, string Product, string Policy, string Instrument,
    string Symbol, string ProviderSymbol, string Mic, DateOnly Date, decimal Open, decimal High, decimal Low,
    decimal Close, string AdjustedClose, long Volume, DateTimeOffset AcquiredUtc);
internal sealed record HistoricalResearchSource(string Commitment, string Artifact, string Product,
    ImmutableArray<HistoricalResearchRow> Rows);

internal sealed partial class EodhdMarketMemory
{
    // Read-only preparation returns commitments, never creates authority or changes source data.
    internal static HistoricalResearchSource InspectHistoricalResearchSource(string database, DateTimeOffset now)
    {
        using var c = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = database, Mode = SqliteOpenMode.ReadOnly }.ToString());
        c.Open(); using var tx = c.BeginTransaction(deferred: true);
        return ReadHistoricalResearchSource(c, null, now);
    }

    private static HistoricalResearchSource ReadHistoricalResearchSource(SqliteConnection c,
        RecurringResearchAuthorization? grant, DateTimeOffset knownBy)
    {
        FinanceTime.RequireUtc(knownBy, nameof(knownBy));
        using var metadata = c.CreateCommand();
        metadata.CommandText = """
            SELECT d.source,d.state,d.license_class,d.provenance_result,d.promotion_result,d.artifact_sha256,
              d.manifest_json,d.validation_json,r.checksum,r.created_utc,r.observation_count,
              p.raw_capability,p.adjusted_capability,p.audit_version,p.evidence,p.audited_utc
            FROM dataset_candidates d JOIN revisions r ON r.revision_id=d.canonical_revision_id
            JOIN revision_price_capabilities p ON p.revision_id=r.revision_id
            WHERE d.candidate_id=$candidate AND r.revision_id=$revision
            """;
        metadata.Parameters.AddWithValue("$candidate", HistoricalResearchAdmission.Candidate);
        metadata.Parameters.AddWithValue("$revision", HistoricalResearchAdmission.Revision);
        using var m = metadata.ExecuteReader();
        if (!m.Read() || m.GetString(0) != "NASDAQ-WIKI" || m.GetString(1) != "Promoted" ||
            m.GetString(2) != "PublicDomain" || m.GetString(3) != "Pass" || m.GetString(4) != "Pass" ||
            m.IsDBNull(6) || m.IsDBNull(7) || m.GetString(11) != "RAW_ONLY_VALID")
            throw new InvalidDataException("Historical source rights/promotion/raw capability unavailable.");
        var meta = Enumerable.Range(0, m.FieldCount).Select(i => Convert.ToString(m.GetValue(i), CultureInfo.InvariantCulture)!).ToArray();
        foreach (var i in new[] { 9, 15 })
        {
            var at = DateTimeOffset.Parse(meta[i], CultureInfo.InvariantCulture);
            FinanceTime.RequireUtc(at, "historical evidence time");
            if (at > knownBy) throw new InvalidDataException("Historical evidence not known at admission.");
        }
        var artifact = meta[5];
        if (m.Read()) throw new InvalidDataException("Ambiguous historical admission.");
        m.Close();
        using var q = c.CreateCommand();
        q.CommandText = """
            SELECT provider,product,policy,instrument_id,symbol,provider_symbol,mic,session_date,
              open,high,low,close,adjusted_close,volume,acquired_utc FROM observations
            WHERE revision_id=$revision AND symbol='AAPL' ORDER BY session_date,provider,product
            """;
        q.Parameters.AddWithValue("$revision", HistoricalResearchAdmission.Revision);
        using var r = q.ExecuteReader(); var rows = ImmutableArray.CreateBuilder<HistoricalResearchRow>();
        while (r.Read())
        {
            if (rows.Count >= 748) throw new InvalidDataException("Historical scope exceeds bound.");
            var row = new HistoricalResearchRow(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4),
                r.GetString(5), r.GetString(6), DateOnly.ParseExact(r.GetString(7), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                Price(8), Price(9), Price(10), Price(11), r.GetString(12), r.GetInt64(13),
                DateTimeOffset.Parse(r.GetString(14), CultureInfo.InvariantCulture));
            FinanceTime.RequireUtc(row.AcquiredUtc, nameof(row.AcquiredUtc));
            if (row.Provider != "NASDAQ-WIKI" || row.Instrument != "US:XNAS:AAPL" || row.ProviderSymbol != "AAPL" || row.Mic != "XNAS" ||
                row.AcquiredUtc > knownBy || row.AcquiredUtc < DailyMarketEvidence.DayStart(row.Date) ||
                row.Open <= 0 || row.Low <= 0 || row.Close <= 0 || row.Volume < 0 ||
                row.High < Math.Max(row.Open, row.Close) || row.Low > Math.Min(row.Open, row.Close))
                throw new InvalidDataException("Historical row identity/temporal/OHLCV validation failed.");
            rows.Add(row);
            decimal Price(int i) => decimal.Parse(r.GetString(i), NumberStyles.Number, CultureInfo.InvariantCulture);
        }
        var all = rows.ToImmutable();
        if (all.Length != 748 || !all.Select(x => x.Date).SequenceEqual(HistoricalResearchAdmission.Sessions()) ||
            all.Select(x => (x.Product, x.Policy)).Distinct().Count() != 1)
            throw new InvalidDataException("Exact historical coverage required.");
        var commitment = LearningIdentity.Hash(new { Contract = HistoricalResearchAdmission.Version, Metadata = meta, Rows = all });
        if (grant is not null && (grant.Historical!.SnapshotChecksum != commitment || grant.Historical.ArtifactChecksum != artifact ||
            grant.Mapping.ProviderDataset.Value != all[0].Product))
            throw new InvalidDataException("Historical authorized source commitment changed.");
        return new(commitment, artifact, all[0].Product, all);
    }

    private static RecurringResearchScope ProjectHistoricalResearch(SqliteConnection c, RecurringResearchProgram state,
        int number, DateTimeOffset cutoff)
    {
        if (number != 1 || cutoff < state.RegisteredAtUtc) throw new InvalidDataException("Historical opportunity mismatch.");
        var source = ReadHistoricalResearchSource(c, state.Authorization, state.RegisteredAtUtc);
        var rows = source.Rows.Take(500).ToArray(); // fixed before any model/evaluation; remaining 248 are NOT searched
        var id = state.Authorization.Instrument.Id;
        // Simulated retrospective event clock ONLY. True acquisition timestamps remain in the full admission commitment.
        static DateTimeOffset Simulated(DateOnly day) => new(day.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        var features = DeterministicDailyFeatureEngine.Build(rows.Select(x => new DailyFeatureObservation(id,
            x.Date, x.Open, x.High, x.Low, x.Close, x.Volume, Simulated(x.Date), new(source.Commitment))));
        var bars = rows.Select(x => new BacktestMarketBar(id, source.Commitment, x.Date, x.Open, x.Close,
            Simulated(x.Date), x.Volume)).ToImmutableArray();
        var values = features.Values.Where(x => x.DefinitionId is "momentum.5" or "momentum.10" or "momentum.20")
            .Select(x => new BacktestFeatureValue(id, x.SessionDate, x.DefinitionId, x.Value, x.KnowledgeTimeUtc, features.DeterministicChecksum)).ToImmutableArray();
        return BuildResearchScope(state, number, cutoff, [], source.Commitment, features.DeterministicChecksum,
            bars, values, rows.Select(x => x.Date).ToArray());
    }
}
