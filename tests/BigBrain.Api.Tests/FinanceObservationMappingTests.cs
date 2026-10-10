using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Tests;

#pragma warning disable CA1861 // Bounded explicit model-free fixtures, never production mapping evidence.
public sealed class FinanceObservationMappingTests
{
    private static readonly DateTimeOffset At = new(2027, 2, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly OldDay = new(2027, 1, 29);
    private static readonly DateOnly NewDay = new(2027, 2, 1);
    private static readonly AlpacaDailyObservationOptions Transport = new() { Enabled = true, ApiKey = "fixturekey", ApiSecret = "fixturesecret" };
    private static MarketDataEntitlementPolicy Policy => AlpacaDailyOwnerDecision.Create(At.AddYears(-1));
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    private const string Instrument = "TEST:XNAS:ABC";

    private static ObservationRuntimeInstrument Snapshot(bool old) => new()
    {
        InstrumentId = Instrument,
        DisplayName = "Synthetic equity",
        ProviderSymbol = "ABC",
        Mic = "XNAS",
        VenueCode = "NASDAQ",
        VenueName = "Synthetic Nasdaq",
        ValidFrom = old ? new(2027, 1, 1) : NewDay,
        ValidTo = old ? OldDay : null,
        MappingEvidence = old ? "fixture:old-mapping" : "fixture:new-mapping"
    };
    private static ObservationRuntimeInstrument Versioned() => new()
    {
        InstrumentId = Instrument,
        MappingVersions = [Version(true), Version(false)]
    };
    private static ObservationMappingVersionOptions Version(bool old) => new()
    {
        Snapshot = Snapshot(old),
        VerificationEvidence = "fixture:verified-identity",
        VerifiedAtUtc = At.AddHours(-1),
        RevalidateByUtc = At.AddDays(2)
    };
    private static FinanceObservationRuntimeOptions Options(ObservationRuntimeInstrument entry) => new()
    {
        Enabled = true,
        CadenceMinutes = 60,
        LookbackDays = 4,
        OwnerAcceptanceVersion = AlpacaDailyOwnerDecision.Version,
        PolicyRecordedAtUtc = At.AddYears(-1),
        AffectedUseEnabled = true,
        Instruments = [entry]
    };

    [Fact]
    public void SourceDateResolutionAndNestedConfigurationAreBoundedAndIndependentOfVerificationTime()
    {
        var entry = Versioned();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Finance:ObservationRuntime:InstrumentsJson"] = JsonSerializer.Serialize(new[] { entry }) }).Build();
        var parsed = Assert.Single(FinanceObservationRuntimeOptions.FromConfiguration(config).Instruments);
        var plan = new ObservationInstrumentPlan(parsed, At);
        Assert.Equal(Snapshot(true).Canonical(), plan.Resolve(OldDay, At));
        Assert.Equal(Snapshot(false).Canonical(), plan.Resolve(NewDay, At));
        Assert.Null(plan.Resolve(NewDay, At).Mapping.ValidTo);
        Assert.Throws<MarketDataNormalizationException>(() => plan.Resolve(new(2027, 1, 31), At));
        Assert.Throws<InvalidDataException>(() => plan.Resolve(NewDay, At.AddDays(2)));
        Assert.Equal(2, plan.Manifest!.Versions.Length);
    }

    [Theory]
    [InlineData("overlap")]
    [InlineData("ambiguous")]
    [InlineData("future-verification")]
    [InlineData("unbounded-currentness")]
    [InlineData("missing-evidence")]
    [InlineData("symbol-change")]
    [InlineData("venue-change")]
    [InlineData("id-change")]
    [InlineData("mixed-form")]
    [InlineData("too-many-versions")]
    [InlineData("null-versions")]
    [InlineData("nested-version")]
    public async Task InvalidManifestNeverReachesTransport(string defect)
    {
        using var f = new Fixture(); var entry = Versioned(); var second = entry.MappingVersions[1];
        switch (defect)
        {
            case "overlap": second.Snapshot.ValidFrom = OldDay; break;
            case "ambiguous": entry.MappingVersions[1] = entry.MappingVersions[0]; break;
            case "future-verification": second.VerifiedAtUtc = At.AddHours(1); break;
            case "unbounded-currentness": second.RevalidateByUtc = At.AddDays(8); break;
            case "missing-evidence": second.VerificationEvidence = ""; break;
            case "symbol-change": second.Snapshot.ProviderSymbol = "XYZ"; break;
            case "venue-change": second.Snapshot.Mic = "XNYS"; break;
            case "id-change": second.Snapshot.InstrumentId = "OTHER"; break;
            case "mixed-form": entry.ProviderSymbol = "ABC"; break;
            case "too-many-versions": entry.MappingVersions = Enumerable.Repeat(second, 5).ToArray(); break;
            case "null-versions": entry.MappingVersions = null!; break;
            case "nested-version": second.Snapshot.MappingVersions = [Version(true)]; break;
        }
        var runtime = f.Runtime(entry);
        f.Clock.Now += TimeSpan.FromHours(1);
        Assert.Equal(ObservationRuntimeState.Misconfigured, (await runtime.RunCycleAsync(TestContext.Current.CancellationToken)).State);
        Assert.Equal(0, f.Calls); Assert.Equal(0L, f.Db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
    }

    [Fact]
    public async Task ExpiredHistoryCoexistsWithCurrentVersionWithoutReauthorizingOldRequests()
    {
        using var f = new Fixture(); var entry = Versioned();
        var original = await f.Acquire(entry, OldDay);
        var bytes = f.Db.Scalar("SELECT receipt_json FROM market_observation_receipts");
        var binding = f.Db.Scalar("SELECT manifest_id FROM observation_mapping_receipts");
        var cutoff = f.Clock.Now;
        var projection = JsonSerializer.Serialize(f.Db.Memory().MarketKnowledgeAt(cutoff, f.Clock, Policy));
        f.Clock.Now = At.AddDays(3);
        entry.MappingVersions[1].VerifiedAtUtc = f.Clock.Now;
        entry.MappingVersions[1].RevalidateByUtc = f.Clock.Now.AddDays(1);
        var plan = new ObservationInstrumentPlan(entry, f.Clock.Now);
        Assert.Equal(original.Receipt.Mapping, plan.ResolveHistorical(OldDay).Mapping);
        var next = await f.Acquire(entry, NewDay);
        Assert.Equal(ObservationAcquisitionKind.New, next.Kind);
        Assert.Equal(Snapshot(false).Canonical().Mapping, next.Receipt.Mapping);
        Assert.True(next.Receipt.KnowledgeTimeUtc > cutoff);
        Assert.Equal(2, f.Calls);
        await Assert.ThrowsAsync<InvalidDataException>(() => f.Acquire(entry, OldDay));
        Assert.Equal(2, f.Calls); // Expired historical evidence is not permission to reacquire.
        Assert.Equal(projection, JsonSerializer.Serialize(f.Db.Memory().MarketKnowledgeAt(cutoff, f.Clock, Policy)));
        Assert.Equal(bytes, f.Db.Scalar("SELECT receipt_json FROM market_observation_receipts ORDER BY ingested_ticks LIMIT 1"));
        Assert.Equal(2L, f.Db.Scalar("SELECT COUNT(*) FROM observation_mapping_manifests"));
        // Fresh authorization for the exact same old snapshot permits duplicate/value revision only.
        entry.MappingVersions[0].VerifiedAtUtc = f.Clock.Now;
        entry.MappingVersions[0].RevalidateByUtc = f.Clock.Now.AddDays(1);
        var duplicate = await f.Acquire(entry, OldDay);
        Assert.Equal(ObservationAcquisitionKind.Duplicate, duplicate.Kind);
        Assert.Equal(original.Receipt, duplicate.Receipt);
        Assert.Equal(binding, f.Db.Scalar("SELECT manifest_id FROM observation_mapping_receipts ORDER BY rowid LIMIT 1"));
        f.Clock.Now += TimeSpan.FromMinutes(1); f.Close++;
        var revision = await f.Acquire(entry, OldDay);
        Assert.Equal(ObservationAcquisitionKind.Revision, revision.Kind);
        Assert.Equal(original.Receipt.Id, revision.Receipt.CorrectsId);
        Assert.Equal(original.Receipt.Mapping, revision.Receipt.Mapping);
        Assert.Equal(bytes, f.Db.Scalar("SELECT receipt_json FROM market_observation_receipts ORDER BY ingested_ticks LIMIT 1"));
        Assert.Equal(projection, JsonSerializer.Serialize(f.Db.Memory().MarketKnowledgeAt(cutoff, f.Clock, Policy)));
        f.AssertNoScience();
    }

    [Fact]
    public async Task RuntimeRejectsOnlyExpiredSelectedVersionBeforeNetwork()
    {
        using var f = new Fixture(); var entry = Versioned();
        entry.MappingVersions[0].RevalidateByUtc = At; // Already expired at plan construction.
        var runtime = f.Runtime(entry);
        f.Clock.Now += TimeSpan.FromHours(1);
        var result = await runtime.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ObservationRuntimeState.Degraded, result.State);
        Assert.Equal(1, f.Calls);
        Assert.Contains(result.Attempts, x => x.SourceDay == OldDay && x.Failure == ObservationFailure.Rejected);
        Assert.Contains(result.Attempts, x => x.SourceDay == NewDay && x.Outcome == ObservationAcquisitionKind.New);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("changed")]
    public async Task ExpiredHistoricalSnapshotStillCannotBeRemovedOrChanged(string defect)
    {
        using var f = new Fixture(); var entry = Versioned(); await f.Acquire(entry, OldDay);
        f.Clock.Now = At.AddDays(3);
        entry.MappingVersions[1].VerifiedAtUtc = f.Clock.Now;
        entry.MappingVersions[1].RevalidateByUtc = f.Clock.Now.AddDays(1);
        if (defect == "missing") entry.MappingVersions = [entry.MappingVersions[1]];
        else entry.MappingVersions[0].Snapshot.MappingEvidence = "fixture:changed";
        Assert.Throws<InvalidDataException>(() => f.Prepare(entry, NewDay));
        Assert.Equal(1, f.Calls);
    }

    [Theory]
    [InlineData("window")]
    [InlineData("utc")]
    [InlineData("order")]
    [InlineData("evidence")]
    public void ExpiredHistoricalAssertionsStillRequireValidStructure(string defect)
    {
        var entry = Versioned(); var old = entry.MappingVersions[0];
        old.VerifiedAtUtc = At.AddDays(-3); old.RevalidateByUtc = At.AddDays(-1);
        switch (defect)
        {
            case "window": old.VerifiedAtUtc = At.AddDays(-10); break;
            case "utc": old.VerifiedAtUtc = old.VerifiedAtUtc.ToOffset(TimeSpan.FromHours(1)); break;
            case "order": old.RevalidateByUtc = old.VerifiedAtUtc; break;
            case "evidence": old.VerificationEvidence = ""; break;
        }
        Assert.Throws<InvalidDataException>(() => new ObservationInstrumentPlan(entry, At));
    }

    [Fact]
    public async Task FutureVerificationAtOriginalRecordingIsNeverHealedByWaiting()
    {
        using var f = new Fixture(); await f.Acquire(Versioned(), OldDay);
        var id = (string)f.Db.Scalar("SELECT id FROM observation_mapping_manifests")!;
        // Corrupt only the recording chronology, with a consistent checksum so chronology is tested.
        var recorded = At.AddHours(-2); // Verification was At - 1 hour.
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = f.Db.Path }.ToString()))
        {
            connection.Open(); using var command = connection.CreateCommand();
            command.CommandText = "UPDATE observation_mapping_manifests SET recorded_ticks=$ticks,checksum=$hash";
            command.Parameters.AddWithValue("$ticks", recorded.UtcTicks);
            command.Parameters.AddWithValue("$hash", MarketObservationIntegrity.Hash(new[] { id, MarketObservationIntegrity.Utc(recorded) }));
            command.ExecuteNonQuery();
        }
        f.Clock.Now = At.AddDays(3);
        Assert.Throws<InvalidDataException>(() => f.Db.Memory().MarketKnowledgeAt(At, f.Clock, Policy));
        var entry = Versioned();
        entry.MappingVersions[1].VerifiedAtUtc = f.Clock.Now;
        entry.MappingVersions[1].RevalidateByUtc = f.Clock.Now.AddDays(1);
        Assert.Throws<InvalidDataException>(() => f.Prepare(entry, NewDay));
        Assert.Equal(1, f.Calls);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("duplicate")]
    public void NestedJsonRejectsUnknownOrDuplicateProperties(string defect)
    {
        var json = JsonSerializer.Serialize(new[] { Versioned() });
        json = json.Replace("\"VerificationEvidence\":", defect == "unknown" ?
            "\"Unexpected\":true,\"VerificationEvidence\":" : "\"VerificationEvidence\":\"fixture:other\",\"VerificationEvidence\":", StringComparison.Ordinal);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Finance:ObservationRuntime:InstrumentsJson"] = json }).Build();
        Assert.Empty(FinanceObservationRuntimeOptions.FromConfiguration(config).Instruments);
    }

    [Fact]
    public async Task LegacyReceiptSurvivesTransitionDuplicateRevisionAndReopenWithSealedHistory()
    {
        using var f = new Fixture();
        var original = await f.Acquire(Snapshot(true), OldDay);
        var json = f.Db.Scalar("SELECT receipt_json FROM market_observation_receipts");
        var cutoff = f.Clock.Now;
        var projection = JsonSerializer.Serialize(f.Db.Memory().MarketKnowledgeAt(cutoff, f.Clock, Policy));
        f.Clock.Now += TimeSpan.FromMinutes(1);
        var duplicate = await f.Acquire(Versioned(), OldDay);
        Assert.Equal(ObservationAcquisitionKind.Duplicate, duplicate.Kind); Assert.Equal(original.Receipt, duplicate.Receipt);
        Assert.Equal(json, f.Db.Scalar("SELECT receipt_json FROM market_observation_receipts"));
        Assert.Equal(0L, f.Db.Scalar("SELECT COUNT(*) FROM observation_mapping_receipts")); // Legacy payload not retrofitted.
        f.Clock.Now += TimeSpan.FromMinutes(1); f.Close = 101;
        var revision = await f.Acquire(Versioned(), OldDay);
        Assert.Equal(ObservationAcquisitionKind.Revision, revision.Kind); Assert.Equal(original.Receipt.Id, revision.Receipt.CorrectsId);
        Assert.Equal(original.Receipt.Instrument, revision.Receipt.Instrument); Assert.Equal(original.Receipt.Mapping, revision.Receipt.Mapping);
        f.Clock.Now += TimeSpan.FromMinutes(1);
        var next = await f.Acquire(Versioned(), NewDay);
        Assert.Equal(ObservationAcquisitionKind.New, next.Kind);
        Assert.Equal(Snapshot(false).Canonical().Mapping, next.Receipt.Mapping);
        Assert.True(next.Receipt.KnowledgeTimeUtc > cutoff);
        Assert.Equal(projection, JsonSerializer.Serialize(f.Db.Memory().MarketKnowledgeAt(cutoff, f.Clock, Policy)));
        Assert.Equal(3, f.Db.Memory().MarketKnowledgeAt(f.Clock.Now, f.Clock, Policy).Observations.Length);
        Assert.Equal(1L, f.Db.Scalar("SELECT COUNT(*) FROM observation_mapping_manifests"));
        Assert.Equal(2L, f.Db.Scalar("SELECT COUNT(*) FROM observation_mapping_receipts"));
        Assert.Equal(4, f.Calls);
        Assert.Equal(json, f.Db.Scalar("SELECT receipt_json FROM market_observation_receipts ORDER BY ingested_ticks LIMIT 1"));
        f.AssertNoScience();
    }

    [Theory]
    [InlineData("mapping-evidence")]
    [InlineData("valid-end")]
    [InlineData("valid-start")]
    [InlineData("display-name")]
    public async Task ConfigurationCannotReinterpretHistoricalProvenanceEvenBeforeNetwork(string mutation)
    {
        using var f = new Fixture(); await f.Acquire(Snapshot(true), OldDay); var entry = Versioned();
        var old = entry.MappingVersions[0].Snapshot;
        switch (mutation)
        {
            case "mapping-evidence": old.MappingEvidence = "fixture:other"; break;
            case "valid-end": old.ValidTo = OldDay.AddDays(1); break;
            case "valid-start": old.ValidFrom = old.ValidFrom.AddDays(1); break;
            case "display-name": old.DisplayName = "Changed"; break;
        }
        f.Clock.Now += TimeSpan.FromMinutes(1);
        Assert.ThrowsAny<Exception>(() => f.Prepare(entry, OldDay));
        Assert.Equal(1, f.Calls); Assert.Equal(1L, f.Db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
    }

    [Fact]
    public async Task CurrentnessExpiresDuringRequestAndReverificationDoesNotRewriteOriginalAuthorization()
    {
        using var f = new Fixture(); var entry = Versioned();
        var first = await f.Acquire(entry, NewDay);
        f.Clock.Now = At.AddDays(2);
        Assert.Throws<InvalidDataException>(() => f.Prepare(entry, NewDay));
        foreach (var v in entry.MappingVersions) { v.VerifiedAtUtc = f.Clock.Now; v.RevalidateByUtc = f.Clock.Now.AddDays(1); }
        var duplicate = await f.Acquire(entry, NewDay);
        Assert.Equal(first.Receipt, duplicate.Receipt);
        Assert.Equal(2L, f.Db.Scalar("SELECT COUNT(*) FROM observation_mapping_manifests"));
        Assert.Equal(1L, f.Db.Scalar("SELECT COUNT(*) FROM observation_mapping_receipts"));
        // Historical replay works even after expiry and after a later assertion was recorded.
        Assert.Single(f.Db.Memory().MarketKnowledgeAt(first.Receipt.KnowledgeTimeUtc, f.Clock, Policy).Observations);
        var previous = f.Db.Scalar("SELECT COUNT(*) FROM market_observation_receipts");
        f.OnSend = () => f.Clock.Now = entry.MappingVersions[0].RevalidateByUtc;
        await Assert.ThrowsAsync<InvalidDataException>(() => f.Acquire(entry, OldDay));
        Assert.Equal(previous, f.Db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
    }

    [Theory]
    [InlineData("downgrade")]
    [InlineData("remove")]
    [InlineData("corrupt-manifest")]
    [InlineData("corrupt-binding")]
    [InlineData("clock")]
    public async Task DurableManifestAndReceiptBindingsFailClosed(string defect)
    {
        using var f = new Fixture(); await f.Acquire(Versioned(), NewDay); f.Clock.Now += TimeSpan.FromMinutes(1);
        var entry = Versioned();
        switch (defect)
        {
            case "downgrade": entry = Snapshot(false); break;
            case "remove": entry.MappingVersions = [entry.MappingVersions[1]]; break;
            case "corrupt-manifest": f.Db.Execute("UPDATE observation_mapping_manifests SET checksum='corrupt'"); break;
            case "corrupt-binding": f.Db.Execute("PRAGMA foreign_keys=OFF; UPDATE observation_mapping_receipts SET manifest_id='missing'"); break;
            case "clock": f.Clock.Now = At.AddMinutes(-1); break;
        }
        Assert.ThrowsAny<Exception>(() => f.Prepare(entry, NewDay));
        Assert.Equal(1, f.Calls); Assert.Equal(1L, f.Db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
    }

    [Fact]
    public async Task RuntimeSelectsOneVersionPerDayAndDoesNotRetryGapOrOverlap()
    {
        using var f = new Fixture(); var runtime = f.Runtime(Versioned());
        f.Clock.Now += TimeSpan.FromHours(1);
        var result = await runtime.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ObservationRuntimeState.Healthy, result.State);
        Assert.Equal(2, f.Calls); Assert.Equal(2, result.Attempts.Length);
        f.AssertNoScience();
        var gap = Versioned(); gap.MappingVersions[1].Snapshot.ValidFrom = NewDay.AddDays(1);
        using var g = new Fixture(); runtime = g.Runtime(gap); g.Clock.Now += TimeSpan.FromHours(1);
        result = await runtime.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ObservationRuntimeState.Degraded, result.State);
        Assert.Equal(1, g.Calls); Assert.Contains(result.Attempts, x => x.SourceDay == NewDay && x.Failure == ObservationFailure.Rejected);
    }

    [Fact]
    public async Task ConcurrentReopenAndCancellationKeepAtomicReceiptAndBinding()
    {
        using var f = new Fixture(); var entry = Versioned();
        var receipts = await Task.WhenAll(Task.Run(() => f.Acquire(entry, NewDay)), Task.Run(() => f.Acquire(entry, NewDay)));
        Assert.Equal(receipts[0].Receipt, receipts[1].Receipt);
        Assert.Equal(1L, f.Db.Scalar("SELECT COUNT(*) FROM observation_mapping_manifests"));
        Assert.Equal(1L, f.Db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
        using var stop = new CancellationTokenSource();
        f.Clock.Now += TimeSpan.FromMinutes(1);
        var memory = f.Db.Memory(); memory.ObservationTestHook = stage => { if (stage == "before-observation-commit") stop.Cancel(); };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Acquire(entry, OldDay, memory, stop.Token));
        Assert.Equal(1L, f.Db.Scalar("SELECT COUNT(*) FROM observation_mapping_receipts"));
        Assert.Single(f.Db.Memory().MarketKnowledgeAt(f.Clock.Now, f.Clock, Policy).Observations);
        f.AssertNoScience();
    }

    [Fact]
    public void MigrationIsAdditiveRollbackSafeAndDoesNotRenewSpentAuthority()
    {
        using var db = new FinanceMarketObservationTests.Database(false);
        Assert.Throws<IOException>(() => FinanceSchemaMigrator.Migrate(db.Path, v => { if (v == 98) throw new IOException(); }));
        Assert.Equal(97, FinanceSchemaMigrator.State(db.Path).CurrentVersion);
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM sqlite_master WHERE name='observation_mapping_manifests'"));
        FinanceSchemaMigrator.Migrate(db.Path);
        var memory = db.Memory(); memory.EnrollSyntheticLearning(ResearchLearningFixture.Scope(), LearningExposure.Unexposed);
        Assert.True(memory.ReserveLearningInvocation());
        FinanceSchemaMigrator.Migrate(db.Path);
        Assert.False(db.Memory().ReserveLearningInvocation());
        Assert.Equal(98, FinanceSchemaMigrator.State(db.Path).CurrentVersion);
    }

    [Fact]
    public async Task ShadowV1StillRejectsMixedMappingsAndOldCutoffReplaysWithoutScienceExecution()
    {
        using var f = new Fixture();
        foreach (var day in new[] { new DateOnly(2027, 1, 22), new(2027, 1, 25), new(2027, 1, 26), new(2027, 1, 27), new(2027, 1, 28), OldDay })
        {
            await f.Acquire(Snapshot(true), day); f.Close++; f.Clock.Now += TimeSpan.FromTicks(1);
        }
        // Synthetic retained evidence only: no scientific engine or research session is invoked.
        var config = new BacktestRunConfiguration(["fixture"], "fixture", new("momentum", "v1"),
            new Dictionary<string, decimal> { ["period"] = 5 }, "fixture", BacktestCostModel.Conservative, 0,
            [Instrument], new(2027, 1, 22), OldDay, "fixture", 0);
        var metrics = JsonSerializer.Deserialize<BacktestMetrics>("{}")!;
        BacktestResult Result(string id)
        {
            var value = new BacktestResult(id, "", config, metrics, [], [], [], "RESEARCH", ["SYNTHETIC TEST ONLY"]);
            return value with
            {
                Checksum = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(
                JsonSerializer.SerializeToUtf8Bytes(value, WebJson)))
            };
        }
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = f.Db.Path }.ToString()))
        {
            connection.Open(); FinanceBacktestPersistence.PersistBacktest(connection, Result("fixture-before"));
            FinanceBacktestPersistence.PersistBacktest(connection, Result("fixture-after"));
        }
        var memory = f.Db.Memory();
        var candidate = memory.FreezeDailyShadow("fixture-before", Snapshot(true).Canonical().Mapping, Policy, f.Clock);
        var cutoff = f.Clock.Now;
        var result = memory.EvaluateDailyShadow(candidate.Id, cutoff, Policy, f.Clock);
        f.Clock.Now += TimeSpan.FromMinutes(1);
        await f.Acquire(Versioned(), NewDay);
        Assert.Equal(result, f.Db.Memory().EvaluateDailyShadow(candidate.Id, cutoff, Policy, f.Clock));
        Assert.Throws<InvalidDataException>(() => f.Db.Memory().EvaluateDailyShadow(candidate.Id, f.Clock.Now, Policy, f.Clock));
        Assert.Throws<InvalidDataException>(() => f.Db.Memory().FreezeDailyShadow("fixture-after", Snapshot(false).Canonical().Mapping, Policy, f.Clock));
        Assert.Equal(1L, f.Db.Scalar("SELECT COUNT(*) FROM prospective_daily_candidates"));
        Assert.Equal(1L, f.Db.Scalar("SELECT COUNT(*) FROM prospective_daily_results"));
        Assert.Equal(0L, f.Db.Scalar("SELECT COUNT(*) FROM shadow_predictions"));
    }

    [Fact]
    public async Task UpgradeFrom97PreservesExactLegacyReceiptBytes()
    {
        using var f = new Fixture(); await f.Acquire(Snapshot(true), OldDay);
        var bytes = f.Db.Scalar("SELECT receipt_json FROM market_observation_receipts");
        f.Db.Execute("DROP TABLE observation_mapping_receipts; DROP TABLE observation_mapping_manifests; DELETE FROM finance_schema_migrations WHERE version=98;");
        Assert.Throws<IOException>(() => FinanceSchemaMigrator.Migrate(f.Db.Path, v => { if (v == 98) throw new IOException(); }));
        Assert.Equal(bytes, f.Db.Scalar("SELECT receipt_json FROM market_observation_receipts"));
        FinanceSchemaMigrator.Migrate(f.Db.Path);
        Assert.Equal(bytes, f.Db.Scalar("SELECT receipt_json FROM market_observation_receipts"));
        Assert.Single(f.Db.Memory().MarketKnowledgeAt(At, f.Clock, Policy).Observations);
    }

    [Fact]
    public async Task AdoptionDuringLegacyRequestCannotCommitAnUnauthorizedReceipt()
    {
        using var f = new Fixture();
        f.OnSend = () => f.Prepare(Versioned(), NewDay);
        await Assert.ThrowsAsync<InvalidDataException>(() => f.Acquire(Snapshot(false), NewDay));
        Assert.Equal(1, f.Calls);
        Assert.Equal(0L, f.Db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
    }

    private sealed class Fixture : IDisposable
    {
        internal FinanceMarketObservationTests.Database Db { get; } = new();
        internal FinanceMarketObservationTests.Clock Clock { get; } = new(At);
        private int _calls;
        internal int Calls => _calls;
        internal decimal Close { get; set; } = 100;
        internal Action? OnSend { get; set; }
        internal FinanceObservationRuntime Runtime(ObservationRuntimeInstrument entry) => new(Options(entry), Transport,
            Db.Memory(), Clock, () => true, Source, (duration, _) => { Clock.Now += duration; return Task.CompletedTask; });
        internal (ObservationInstrumentPlan Plan, string? Authorization) Prepare(ObservationRuntimeInstrument entry, DateOnly day)
        {
            var plan = new ObservationInstrumentPlan(entry, Clock.Now);
            return (plan, Db.Memory().PrepareObservationMapping(plan, day, Clock.Now));
        }
        internal Task<ObservationAcquisition> Acquire(ObservationRuntimeInstrument entry, DateOnly day) =>
            Acquire(entry, day, null, TestContext.Current.CancellationToken);
        internal async Task<ObservationAcquisition> Acquire(ObservationRuntimeInstrument entry, DateOnly day,
            EodhdMarketMemory? memory, CancellationToken token)
        {
            var (plan, authorization) = Prepare(entry, day); var selected = plan.Resolve(day, Clock.Now);
            using var source = Source(day);
            return await (memory ?? Db.Memory()).ReobserveDailyAsync(source, selected.Instrument, selected.Mapping,
                Policy, Clock, token, authorization);
        }
        private AlpacaDailyMarketObservations Source(DateOnly day) => new(Transport, day, new Handler((request, token) =>
        {
            Interlocked.Increment(ref _calls); OnSend?.Invoke(); token.ThrowIfCancellationRequested();
            var symbol = request.RequestUri!.AbsolutePath.Split('/')[3];
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    symbol,
                    next_page_token = (string?)null,
                    bars = new[] { new { t = DailyMarketEvidence.DayStart(day).ToString("O", CultureInfo.InvariantCulture),
                    o = Close, h = Close + 1, l = Close - 1, c = Close, v = 10000 } }
                }), Encoding.UTF8, "application/json")
            });
        }));
        internal void AssertNoScience()
        {
            foreach (var table in new[] { "prospective_daily_candidates", "prospective_daily_results", "backtest_runs", "shadow_predictions" })
                Assert.Equal(0L, Db.Scalar("SELECT COUNT(*) FROM " + table));
        }
        public void Dispose() => Db.Dispose();
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
