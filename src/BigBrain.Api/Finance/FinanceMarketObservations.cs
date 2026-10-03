using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

// Same Finance SQLite owner. No legacy history import, scheduler, endpoint or scientific evaluator.
internal sealed partial class EodhdMarketMemory
{
    internal const string MarketObservationMigration = """
        CREATE TABLE market_observation_receipts(
          id TEXT PRIMARY KEY,logical_identity TEXT NOT NULL,ingested_ticks INTEGER NOT NULL,
          checksum TEXT NOT NULL,receipt_json TEXT NOT NULL);
        CREATE INDEX ix_market_observation_knowledge ON market_observation_receipts(ingested_ticks,id);
        CREATE INDEX ix_market_observation_logical ON market_observation_receipts(logical_identity,ingested_ticks);
        CREATE TABLE market_observation_clock(singleton INTEGER PRIMARY KEY CHECK(singleton=1),watermark_ticks INTEGER NOT NULL);
        INSERT INTO market_observation_clock VALUES(1,0);
        """;
    private static readonly JsonSerializerOptions ObservationJson = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNameCaseInsensitive = false,
        Converters = {
            new ObservationText<InstrumentId>(x => x.Value, x => new(x)),
            new ObservationText<Currency>(x => x.Code, x => new(x)),
            new ObservationText<MarketDataProvider>(x => x.Value, x => new(x)),
            new ObservationText<ProviderDataset>(x => x.Value, x => new(x)),
            new ObservationText<EvidenceReference>(x => x.Value, x => new(x)),
            new ObservationText<PolicyId>(x => x.Value, x => new(x)),
            new ObservationText<PolicyVersion>(x => x.Value, x => new(x))
        }
    };
    // Local to the new receipt envelope; existing scientific serializers/identities are unchanged.
    private sealed class ObservationText<T>(Func<T, string> value, Func<string, T> create) : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = reader.GetString() ?? throw new JsonException("Missing observation identity.");
            var result = create(text);
            if (value(result) != text) throw new JsonException("Noncanonical observation identity.");
            return result;
        }
        public override void Write(Utf8JsonWriter writer, T item, JsonSerializerOptions options) => writer.WriteStringValue(value(item));
    }
    internal Action<string>? ObservationTestHook { get; set; }

    internal async Task<MarketObservationReceipt> AcquireMarketObservationAsync(IMarketObservationSource source,
        CanonicalInstrument instrument, ProviderInstrumentMapping mapping, MarketDataEntitlementPolicy policy,
        TimeProvider clock, CancellationToken cancellationToken, string? correctsId = null)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(clock);
        if (source.Provider != mapping.Provider || source.Dataset != mapping.ProviderDataset ||
            !ValidObservationSource(source.ContractVersion, source.Origin))
            throw new InvalidDataException("Observation source scope is invalid.");
        MarketObservationIntegrity.Token(source.AdapterVersion);
        var started = clock.GetUtcNow();
        RequireReceiptRights(source.ContractVersion, policy, mapping, started);
        cancellationToken.ThrowIfCancellationRequested();
        var value = await source.ReadAsync(mapping, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var acquired = clock.GetUtcNow();
        if (acquired < started) throw new InvalidDataException("Observation acquisition clock regressed.");
        if ((value.Daily is null) != (source.ContractVersion == MarketObservationReceipt.Contract))
            throw new InvalidDataException("Source contract mismatch.");
        if (value.Daily is not null) DailyMarketEvidence.Validate(value, mapping, started);
        MarketObservationIntegrity.Validate(value, instrument, mapping, acquired);
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireObservationSchema(connection);
        var ingested = clock.GetUtcNow(); FinanceTime.RequireUtc(ingested, nameof(clock));
        RequireReceiptRights(source.ContractVersion, policy, mapping, ingested);
        if (ingested < acquired) throw new InvalidDataException("Observation clock regressed.");
        var logical = MarketObservationIntegrity.LogicalIdentity(instrument, source.Provider, source.Dataset, source.Origin, value);
        var content = MarketObservationIntegrity.ContentChecksum(value);
        _ = ObservationWatermark(connection);
        var existing = ReadObservationReceipts(connection, "WHERE logical_identity=$key ORDER BY ingested_ticks,id LIMIT 1001", ("$key", logical));
        if (existing.Length > 1000) throw new InvalidDataException("Observation lineage exceeds bounded capacity.");
        var same = existing.FirstOrDefault(x => x.ContentChecksum == content && x.CorrectsId == correctsId);
        if (same is not null)
        {
            if (same.Instrument != instrument || same.Mapping != mapping || same.AdapterVersion != source.AdapterVersion ||
                same.Policy != policy.Reference || same.PolicyEvidence != policy.Evidence.Value)
                throw new InvalidDataException("Observation duplicate provenance conflicts.");
            return same; // Preserve first receipt/time; do not refresh known-at history.
        }
        var last = existing.LastOrDefault();
        if (last is null ? correctsId is not null : correctsId != last.Id)
            throw new InvalidDataException("Observation conflict requires exact preceding receipt lineage.");
        var watermark = ObservationWatermark(connection);
        if (ingested.UtcTicks <= watermark)
            throw new InvalidDataException("Observation cannot enter a sealed or regressed knowledge boundary.");
        var id = MarketObservationIntegrity.Hash(new[] { logical, content, correctsId ?? "NONE" });
        var receipt = new MarketObservationReceipt(source.ContractVersion, id, logical, content, "",
            instrument, mapping, source.Provider, source.Dataset, source.AdapterVersion, source.Origin, value, policy.Reference,
            policy.Evidence.Value, acquired, ingested, correctsId);
        receipt = receipt with { Checksum = MarketObservationIntegrity.Checksum(receipt) };
        Execute(connection, transaction, "INSERT INTO market_observation_receipts VALUES($id,$key,$ticks,$hash,$json)",
            ("$id", id), ("$key", logical), ("$ticks", ingested.UtcTicks), ("$hash", receipt.Checksum),
            ("$json", JsonSerializer.Serialize(receipt, ObservationJson)));
        Execute(connection, transaction, "UPDATE market_observation_clock SET watermark_ticks=$ticks WHERE singleton=1", ("$ticks", ingested.UtcTicks));
        ObservationTestHook?.Invoke("before-observation-commit");
        cancellationToken.ThrowIfCancellationRequested(); transaction.Commit();
        ObservationTestHook?.Invoke("after-observation-commit");
        return receipt;
    }

    internal MarketKnowledgeProjection MarketKnowledgeAt(DateTimeOffset cutoff, TimeProvider clock, MarketDataEntitlementPolicy? dailyPolicy = null)
    {
        FinanceTime.RequireUtc(cutoff, nameof(cutoff));
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireObservationSchema(connection);
        var now = clock.GetUtcNow(); FinanceTime.RequireUtc(now, nameof(clock));
        if (cutoff == default || cutoff > now) throw new InvalidDataException("Cannot seal future knowledge.");
        var rows = ReadObservationReceipts(connection, "WHERE ingested_ticks<=$ticks ORDER BY ingested_ticks,id LIMIT 1001", ("$ticks", cutoff.UtcTicks));
        if (rows.Any(x => x.Value.Daily is not null))
            AlpacaDailyOwnerDecision.Require(dailyPolicy ?? throw new InvalidDataException("Daily use policy required."), now);
        if (rows.Length > 1000) throw new InvalidDataException("Knowledge projection exceeds bounded capacity.");
        var watermark = ObservationWatermark(connection);
        Execute(connection, transaction, "UPDATE market_observation_clock SET watermark_ticks=$ticks WHERE singleton=1", ("$ticks", Math.Max(watermark, cutoff.UtcTicks)));
        transaction.Commit();
        const string version = "finance-market-knowledge-v1";
        return new(version, cutoff, rows, MarketObservationIntegrity.Hash(new[] { version, MarketObservationIntegrity.Utc(cutoff) }.Concat(rows.Select(x => x.Checksum))));
    }

    private static bool ValidObservationSource(string version, MarketObservationOrigin origin) =>
        version == MarketObservationReceipt.Contract && origin is MarketObservationOrigin.AcquiredProviderSnapshot or MarketObservationOrigin.DeterministicFixture ||
        version == DailyMarketEvidence.Contract && origin is MarketObservationOrigin.AcquiredProviderDaily or MarketObservationOrigin.DeterministicFixture;

    private static void RequireReceiptRights(string version, MarketDataEntitlementPolicy policy, ProviderInstrumentMapping mapping, DateTimeOffset now)
    {
        if (version == DailyMarketEvidence.Contract)
        {
            AlpacaDailyOwnerDecision.Require(policy, now);
            if (mapping.Provider != policy.Provider || mapping.ProviderDataset != policy.ProviderDataset)
                throw new InvalidDataException("Daily policy/source mismatch.");
        }
        else RequireObservationRights(policy, mapping, now);
    }

    private static void RequireObservationRights(MarketDataEntitlementPolicy policy, ProviderInstrumentMapping mapping, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(policy); FinanceTime.RequireUtc(now, nameof(now));
        // Bounded foundation cannot promise subscription/deadline deletion for a new source.
        // No provider gets a fabricated default grant; Basic rights remain unresolved/blocked.
        if (policy.MonetaryCostSek != 0 || policy.Retention != RetentionClassification.LongTerm ||
            policy.Deletion != DeletionRequirement.None || policy.EvidenceClass is not
                (EntitlementEvidenceClass.ExplicitProviderGrant or EntitlementEvidenceClass.OwnerAcceptedPersonalResearch))
            throw new InvalidDataException("Observation retention rights are not supported.");
        MarketObservationIntegrity.Token(policy.Evidence.Value);
        MarketObservationIntegrity.Token(policy.Id.Value); MarketObservationIntegrity.Token(policy.Version.Value);
        if (policy.ReviewedAtUtc > now) throw new InvalidDataException("Observation policy is not yet known.");
        var context = new MarketDataEntitlementContext(now, true, false, MarketDataClassification.Raw,
            mapping.Provider, mapping.ProviderDataset);
        foreach (var use in new[] { MarketDataUse.HistoricalAnalysis, MarketDataUse.LongTermStorage })
            if (!MarketDataEntitlementEvaluator.Evaluate(policy, use, context).IsAllowed)
                throw new InvalidDataException("Observation entitlement denied.");
    }

    private static long ObservationWatermark(SqliteConnection connection)
    {
        using var command = connection.CreateCommand(); command.CommandText = "SELECT watermark_ticks FROM market_observation_clock WHERE singleton=1 AND watermark_ticks>=(SELECT COALESCE(MAX(ingested_ticks),0) FROM market_observation_receipts)";
        return command.ExecuteScalar() is long value && value >= 0 ? value : throw new InvalidDataException("Observation clock state missing.");
    }

    private static void RequireObservationSchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand(); command.CommandText = "SELECT MAX(version) FROM finance_schema_migrations";
        if (Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) != FinanceSchemaMigrator.LatestVersion)
            throw new InvalidDataException("Observation schema version unsupported.");
    }

    private static ImmutableArray<MarketObservationReceipt> ReadObservationReceipts(SqliteConnection connection,
        string suffix, params (string Name, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,logical_identity,ingested_ticks,checksum,receipt_json FROM market_observation_receipts " + suffix;
        foreach (var p in parameters) command.Parameters.AddWithValue(p.Name, p.Value);
        using var reader = command.ExecuteReader(); var rows = ImmutableArray.CreateBuilder<MarketObservationReceipt>();
        var preceding = new Dictionary<string, string>(StringComparer.Ordinal);
        while (reader.Read())
        {
            var json = reader.GetString(4);
            if (json.Length > 16384) throw new InvalidDataException("Oversized observation record.");
            var r = JsonSerializer.Deserialize<MarketObservationReceipt>(json, ObservationJson)
                ?? throw new InvalidDataException("Observation record missing.");
            MarketObservationIntegrity.Validate(r.Value, r.Instrument, r.Mapping, r.AcquiredAtUtc);
            if (r.Provider != r.Mapping.Provider || r.Dataset != r.Mapping.ProviderDataset ||
                !ValidObservationSource(r.Version, r.Origin) ||
                (r.Value.Daily is null) != (r.Version == MarketObservationReceipt.Contract) || r.Id != reader.GetString(0) || r.LogicalIdentity != reader.GetString(1) ||
                r.IngestedAtUtc.UtcTicks != reader.GetInt64(2) || r.Checksum != reader.GetString(3) ||
                r.Checksum != MarketObservationIntegrity.Checksum(r) || r.ContentChecksum != MarketObservationIntegrity.ContentChecksum(r.Value) ||
                r.LogicalIdentity != MarketObservationIntegrity.LogicalIdentity(r.Instrument, r.Provider, r.Dataset, r.Origin, r.Value) ||
                r.Id != MarketObservationIntegrity.Hash(new[] { r.LogicalIdentity, r.ContentChecksum, r.CorrectsId ?? "NONE" }) ||
                r.AcquiredAtUtc.Offset != TimeSpan.Zero || r.IngestedAtUtc.Offset != TimeSpan.Zero ||
                r.AcquiredAtUtc > r.IngestedAtUtc || r.Value.EventTimeUtc > r.AcquiredAtUtc)
                throw new InvalidDataException("Observation identity/integrity mismatch.");
            if (r.CorrectsId != preceding.GetValueOrDefault(r.LogicalIdentity))
                throw new InvalidDataException("Observation revision lineage mismatch.");
            preceding[r.LogicalIdentity] = r.Id;
            rows.Add(r);
        }
        return rows.ToImmutable();
    }
}
