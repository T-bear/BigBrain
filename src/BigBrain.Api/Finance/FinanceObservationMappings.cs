using System.Collections.Immutable;
using System.Text.Json;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

// Trusted operator assertions, not provider-discovered identity or a scientific continuity grant.
internal sealed class ObservationMappingVersionOptions
{
    public ObservationRuntimeInstrument Snapshot { get; set; } = new();
    public string VerificationEvidence { get; set; } = "";
    public DateTimeOffset VerifiedAtUtc { get; set; }
    public DateTimeOffset RevalidateByUtc { get; set; }
}

internal sealed record ObservationMappingVersion(CanonicalInstrument Instrument, ProviderInstrumentMapping Mapping,
    string VerificationEvidence, DateTimeOffset VerifiedAtUtc, DateTimeOffset RevalidateByUtc)
{
    // Expiry ends acquisition authority, not the validity of retained assertion evidence.
    internal void RequireHistorical(DateTimeOffset recorded)
    {
        FinanceTime.RequireUtc(recorded, nameof(recorded));
        MarketObservationIntegrity.Token(VerificationEvidence);
        if (VerifiedAtUtc == default || VerifiedAtUtc.Offset != TimeSpan.Zero || RevalidateByUtc.Offset != TimeSpan.Zero ||
            VerifiedAtUtc > recorded || RevalidateByUtc <= VerifiedAtUtc || RevalidateByUtc - VerifiedAtUtc > TimeSpan.FromDays(7))
            throw new InvalidDataException("Mapping assertion chronology is invalid.");
    }

    internal void RequireCurrent(DateTimeOffset now)
    {
        RequireHistorical(now);
        if (now >= RevalidateByUtc) throw new InvalidDataException("Mapping verification is not current.");
    }
}

internal sealed record ObservationMappingManifest(string Version, string InstrumentId, ImmutableArray<ObservationMappingVersion> Versions)
{
    internal const string Contract = "finance-observation-mapping-v1";
}

internal sealed class ObservationInstrumentPlan
{
    internal string InstrumentId { get; }
    internal ObservationMappingManifest? Manifest { get; }
    internal ImmutableArray<(CanonicalInstrument Instrument, ProviderInstrumentMapping Mapping)> Snapshots { get; }
    private readonly InstrumentMappingCatalog _catalog;

    internal ObservationInstrumentPlan(ObservationRuntimeInstrument entry, DateTimeOffset now)
    {
        if (entry.MappingVersions is null || entry.MappingVersions.Length > 4) throw new InvalidDataException();
        if (entry.MappingVersions.Length == 0)
            Snapshots = [entry.Canonical()]; // Preserve the accepted single-entry contract, without inventing verification dates.
        else
        {
            if (entry.DisplayName != "" || entry.ProviderSymbol != "" || entry.Mic != "" || entry.VenueCode != "" ||
                entry.VenueName != "" || entry.ValidFrom != default || entry.ValidTo is not null || entry.MappingEvidence != "")
                throw new InvalidDataException("Legacy and versioned mapping forms cannot be mixed.");
            var versions = entry.MappingVersions.Select(x =>
            {
                if (x?.Snapshot is null || x.Snapshot.MappingVersions is not { Length: 0 }) throw new InvalidDataException();
                var snapshot = x.Snapshot.Canonical();
                var version = new ObservationMappingVersion(snapshot.Instrument, snapshot.Mapping, x.VerificationEvidence,
                    x.VerifiedAtUtc, x.RevalidateByUtc);
                version.RequireHistorical(now);
                if (snapshot.Instrument.Id.Value != entry.InstrumentId) throw new InvalidDataException();
                return version;
            }).OrderBy(x => x.Mapping.ValidFrom).ToImmutableArray();
            Manifest = new(ObservationMappingManifest.Contract, entry.InstrumentId, versions);
            Snapshots = versions.Select(x => (x.Instrument, x.Mapping)).ToImmutableArray();
        }
        InstrumentId = Snapshots[0].Instrument.Id.Value;
        var first = Snapshots[0];
        foreach (var item in Snapshots)
        {
            FinanceObservationRuntime.ValidateSnapshot(item.Instrument, item.Mapping);
            // H permits reviewed date/provenance transitions, not unknown renames/venue or material identity changes.
            if (item.Instrument.Id != first.Instrument.Id || item.Instrument.Type != first.Instrument.Type ||
                item.Instrument.Currency != first.Instrument.Currency || item.Instrument.Mic != first.Instrument.Mic ||
                item.Instrument.Venue != first.Instrument.Venue || item.Instrument.DisplayName != first.Instrument.DisplayName ||
                item.Mapping.ProviderReference != first.Mapping.ProviderReference)
                throw new InvalidDataException("Mapping transition has unapproved identity semantics.");
        }
        _catalog = new([first.Instrument], Snapshots.Select(x => x.Mapping));
    }

    internal (CanonicalInstrument Instrument, ProviderInstrumentMapping Mapping) Resolve(DateOnly day, DateTimeOffset now)
    {
        var selected = ResolveHistorical(day);
        Manifest?.Versions.Single(x => x.Mapping == selected.Mapping).RequireCurrent(now);
        return selected;
    }

    // Provenance comparison only. This resolution cannot authorize a provider request.
    internal (CanonicalInstrument Instrument, ProviderInstrumentMapping Mapping) ResolveHistorical(DateOnly day)
    {
        var first = Snapshots[0];
        var mapping = _catalog.ResolveProviderReference(first.Instrument.Id, first.Mapping.Provider,
            first.Mapping.ProviderDataset, first.Mapping.Mic, day);
        var selected = Snapshots.Single(x => x.Mapping == mapping);
        if (day < selected.Instrument.ValidFrom || day > selected.Instrument.ValidTo) throw new InvalidDataException();
        return selected;
    }
}

internal sealed partial class EodhdMarketMemory
{
    // Same Finance owner. Append-only authorization evidence; no changes to old receipts or their hashes.
    internal const string ObservationMappingMigration = """
        CREATE TABLE observation_mapping_manifests(id TEXT PRIMARY KEY,recorded_ticks INTEGER NOT NULL,
          checksum TEXT NOT NULL,manifest_json TEXT NOT NULL);
        CREATE TABLE observation_mapping_receipts(receipt_id TEXT PRIMARY KEY REFERENCES market_observation_receipts(id),
          manifest_id TEXT NOT NULL REFERENCES observation_mapping_manifests(id));
        """;

    internal string? PrepareObservationMapping(ObservationInstrumentPlan plan, DateOnly day, DateTimeOffset now)
    {
        var selected = plan.Resolve(day, now);
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireObservationSchema(connection);
        if (now.UtcTicks < ObservationWatermark(connection)) throw new ObservationClockException();
        var manifests = ReadMappingManifests(connection, now);
        var prior = manifests.Where(x => x.Manifest.InstrumentId == plan.InstrumentId).ToArray();
        if (plan.Manifest is null && prior.Length != 0) throw new InvalidDataException("Versioned mapping cannot downgrade to legacy configuration.");
        // Preserve adopted snapshots even when a failed request created no market receipt.
        foreach (var old in prior.SelectMany(x => x.Manifest.Versions))
            if (!plan.Snapshots.Any(x => x.Instrument == old.Instrument && x.Mapping == old.Mapping))
                throw new InvalidDataException("Adopted mapping snapshots cannot be replaced or removed.");
        var receipts = ReadObservationReceipts(connection, "ORDER BY ingested_ticks,id LIMIT 1001");
        if (receipts.Length > 1000) throw new InvalidDataException();
        foreach (var receipt in receipts.Where(x => x.Value.Daily is not null && x.Instrument.Id.Value == plan.InstrumentId))
        {
            // No current config can reinterpret an already persisted daily identity before making a request.
            var snapshot = plan.ResolveHistorical(receipt.Value.Daily!.SourceDate);
            if (snapshot.Instrument != receipt.Instrument || snapshot.Mapping != receipt.Mapping)
                throw new InvalidDataException("Retained receipt mapping conflicts with configuration.");
        }
        if (plan.Manifest is null) return null;
        var json = JsonSerializer.Serialize(plan.Manifest, ObservationJson);
        var id = MappingManifestId(json);
        if (!manifests.Any(x => x.Id == id))
        {
            if (manifests.Length >= 1000) throw new InvalidDataException("Mapping manifest capacity exceeded.");
            Execute(connection, transaction, "INSERT INTO observation_mapping_manifests VALUES($id,$ticks,$hash,$json)",
                ("$id", id), ("$ticks", now.UtcTicks), ("$hash", MappingManifestChecksum(id, now)), ("$json", json));
        }
        RequireMappingAuthorization(connection, id, selected.Instrument, selected.Mapping, now);
        transaction.Commit();
        return id;
    }

    private static string MappingManifestId(string json) => MarketObservationIntegrity.Hash(new[] { ObservationMappingManifest.Contract, json });
    private static string MappingManifestChecksum(string id, DateTimeOffset recorded) =>
        MarketObservationIntegrity.Hash(new[] { id, MarketObservationIntegrity.Utc(recorded) });

    private static ImmutableArray<(string Id, ObservationMappingManifest Manifest, DateTimeOffset Recorded)> ReadMappingManifests(SqliteConnection connection, DateTimeOffset? now)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,recorded_ticks,checksum,manifest_json FROM observation_mapping_manifests ORDER BY recorded_ticks,id LIMIT 1001";
        using var reader = command.ExecuteReader();
        var rows = ImmutableArray.CreateBuilder<(string, ObservationMappingManifest, DateTimeOffset)>();
        while (reader.Read())
        {
            var json = reader.GetString(3);
            if (json.Length > 32768 || rows.Count >= 1000) throw new InvalidDataException();
            var recorded = new DateTimeOffset(reader.GetInt64(1), TimeSpan.Zero);
            if (now is { } current && recorded > current) throw new ObservationClockException();
            var m = JsonSerializer.Deserialize<ObservationMappingManifest>(json, ObservationJson) ?? throw new InvalidDataException();
            if (recorded == default || m.Version != ObservationMappingManifest.Contract || m.Versions.IsDefaultOrEmpty || m.Versions.Length > 4 ||
                MappingManifestId(json) != reader.GetString(0) || MappingManifestChecksum(reader.GetString(0), recorded) != reader.GetString(2))
                throw new InvalidDataException("Mapping manifest integrity mismatch.");
            foreach (var version in m.Versions)
            {
                version.RequireHistorical(recorded); // Validate original recording chronology, never today's time.
                FinanceObservationRuntime.ValidateSnapshot(version.Instrument, version.Mapping);
                if (version.Instrument.Id.Value != m.InstrumentId) throw new InvalidDataException();
            }
            _ = new InstrumentMappingCatalog([m.Versions[0].Instrument], m.Versions.Select(x => x.Mapping));
            rows.Add((reader.GetString(0), m, recorded));
        }
        return rows.ToImmutable();
    }

    private static void RequireLegacyObservationMapping(SqliteConnection connection, CanonicalInstrument instrument, DateTimeOffset now)
    {
        if (ReadMappingManifests(connection, now).Any(x => x.Manifest.InstrumentId == instrument.Id.Value))
            throw new InvalidDataException("Legacy acquisition cannot bypass adopted versioned authority.");
    }

    private static void RequireMappingAuthorization(SqliteConnection connection, string id, CanonicalInstrument instrument,
        ProviderInstrumentMapping mapping, DateTimeOffset now)
    {
        RequireMappingAuthorization(ReadMappingManifests(connection, now), id, instrument, mapping, now);
    }

    private static void RequireMappingAuthorization(
        ImmutableArray<(string Id, ObservationMappingManifest Manifest, DateTimeOffset Recorded)> manifests,
        string id, CanonicalInstrument instrument, ProviderInstrumentMapping mapping, DateTimeOffset now)
    {
        var row = manifests.SingleOrDefault(x => x.Id == id);
        if (row.Manifest is null || row.Recorded > now) throw new InvalidDataException("Mapping authorization missing or not yet recorded.");
        var selected = row.Manifest.Versions.SingleOrDefault(x => x.Instrument == instrument && x.Mapping == mapping)
            ?? throw new InvalidDataException("Mapping authorization mismatch.");
        selected.RequireCurrent(now);
    }
}
