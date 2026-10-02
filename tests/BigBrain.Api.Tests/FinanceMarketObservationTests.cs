using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Tests;

#pragma warning disable CA1861 // Small immutable fixture arrays.

public sealed class FinanceMarketObservationTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 1, 15, 0, 0, TimeSpan.Zero);
    private static readonly MarketVenue Venue = new("NASDAQ", "NASDAQ");
    internal static readonly CanonicalInstrument Instrument = new(new("US:AAPL:XNAS"), InstrumentType.Equity,
        "Apple", new("USD"), Venue, "XNAS", InstrumentLifecycle.Active, new(2000, 1, 1));
    internal static readonly ProviderInstrumentMapping Mapping = new(Instrument.Id, new("fixture"), new("quote"),
        "AAPL", Venue, "XNAS", new(2000, 1, 1), null, new("fixture:mapping"));
    private static ProviderObservation Value => new("AAPL", "XNAS", new("USD"), Timeframe.OneMinute, At.AddMinutes(-1), null,
        100, 102, 99, 101, 10000, MarketObservationIntegrity.Hash(new[] { "fixture-only" }));

    [Fact]
    public async Task PersistReopenCutoffAndFutureNoninterference()
    {
        using var db = new Database(); var clock = new Clock(At); var source = new Source(Value);
        var receipt = await Acquire(db.Memory(), source, clock);
        Assert.Equal(MarketObservationOrigin.DeterministicFixture, receipt.Origin);
        Assert.Null(receipt.Value.ProviderAvailableAtUtc);
        var reopened = db.Memory();
        var before = reopened.MarketKnowledgeAt(At.AddTicks(-1), clock);
        Assert.Empty(before.Observations);
        var known = reopened.MarketKnowledgeAt(At, clock);
        Assert.Equal(receipt, Assert.Single(known.Observations));
        Assert.Equal(receipt.Checksum, MarketObservationIntegrity.Checksum(receipt));
        clock.Now = At.AddMinutes(1);
        await Acquire(reopened, new Source(Value with { EventTimeUtc = At }), clock);
        Assert.Equal(known.Checksum, db.Memory().MarketKnowledgeAt(At, clock).Checksum);
        Assert.Equal(before.Checksum, db.Memory().MarketKnowledgeAt(At.AddTicks(-1), clock).Checksum);
        Assert.Equal(2, db.Memory().MarketKnowledgeAt(clock.Now, clock).Observations.Length);
    }

    [Fact]
    public async Task DuplicateAndExplicitCorrectionPreserveOriginal()
    {
        using var db = new Database(); var clock = new Clock(At);
        var original = await Acquire(db.Memory(), new Source(Value), clock);
        clock.Now = At.AddMinutes(1);
        Assert.Equal(original, await Acquire(db.Memory(), new Source(Value), clock));
        var changed = new Source(Value with { Close = 100 });
        await Assert.ThrowsAsync<InvalidDataException>(() => Acquire(db.Memory(), changed, clock));
        var correction = await Acquire(db.Memory(), changed, clock, original.Id);
        Assert.Equal(original.Id, correction.CorrectsId);
        Assert.NotEqual(original.Id, correction.Id);
        Assert.Equal(original, Assert.Single(db.Memory().MarketKnowledgeAt(At, clock).Observations));
        Assert.Equal(2, db.Memory().MarketKnowledgeAt(clock.Now, clock).Observations.Length);
        Assert.Equal(correction, await Acquire(db.Memory(), changed, clock, original.Id));
        await Assert.ThrowsAsync<InvalidDataException>(() => Acquire(db.Memory(), new Source(Value with { Close = 102 }), clock, original.Id));
    }

    [Theory]
    [InlineData("future-event")]
    [InlineData("future-publication")]
    [InlineData("publication-before-event")]
    [InlineData("timezone")]
    [InlineData("ohlc")]
    [InlineData("volume")]
    [InlineData("currency")]
    [InlineData("mic")]
    [InlineData("symbol")]
    [InlineData("checksum")]
    [InlineData("interval")]
    [InlineData("unaligned")]
    public async Task InvalidExternalValuesNeverEnterKnowledge(string invalid)
    {
        using var db = new Database(); var clock = new Clock(At);
        var value = invalid switch
        {
            "future-event" => Value with { EventTimeUtc = At.AddTicks(1) },
            "future-publication" => Value with { ProviderAvailableAtUtc = At.AddTicks(1) },
            "publication-before-event" => Value with { ProviderAvailableAtUtc = At.AddHours(-1) },
            "timezone" => Value with { EventTimeUtc = Value.EventTimeUtc.ToOffset(TimeSpan.FromHours(1)) },
            "ohlc" => Value with { Close = 103 },
            "volume" => Value with { Volume = -1 },
            "currency" => Value with { Currency = new("SEK") },
            "mic" => Value with { Mic = "XNYS" },
            "symbol" => Value with { ProviderReference = "MSFT" },
            "interval" => Value with { Interval = Timeframe.OneDay },
            "unaligned" => Value with { EventTimeUtc = At.AddSeconds(-1) },
            _ => Value with { PayloadSha256 = "invalid" }
        };
        await Assert.ThrowsAnyAsync<Exception>(() => Acquire(db.Memory(), new Source(value), clock));
        Assert.Empty(db.Memory().MarketKnowledgeAt(At, clock).Observations);
    }

    [Theory]
    [InlineData(MarketObservationOrigin.Unknown)]
    [InlineData(MarketObservationOrigin.HistoricalImport)]
    public async Task HistoricalOrUnclassifiedSourceCannotMasqueradeAsAcquired(MarketObservationOrigin origin)
    {
        using var db = new Database(); var source = new Source(Value) { Origin = origin };
        await Assert.ThrowsAsync<InvalidDataException>(() => Acquire(db.Memory(), source, new(At)));
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task TrustedReceiptIsConservativeAndClockCannotBackdateNewKnowledge()
    {
        using var db = new Database(); var clock = new Clock(At);
        var receipt = await Acquire(db.Memory(), new Source(Value with { ProviderAvailableAtUtc = At.AddSeconds(-10) }), clock);
        Assert.Equal(At, receipt.KnowledgeTimeUtc);
        var known = db.Memory().MarketKnowledgeAt(At.AddSeconds(10), new Clock(At.AddSeconds(10)));
        clock.Now = At.AddSeconds(5);
        await Assert.ThrowsAsync<InvalidDataException>(() => Acquire(db.Memory(), new Source(Value with { EventTimeUtc = At }), clock));
        Assert.Equal(known.Checksum, db.Memory().MarketKnowledgeAt(At.AddSeconds(10), new Clock(At.AddSeconds(20))).Checksum);
        Assert.Throws<InvalidDataException>(() => db.Memory().MarketKnowledgeAt(At.AddDays(1), clock));
    }

    [Theory]
    [InlineData("before-observation-commit", 0)]
    [InlineData("after-observation-commit", 1)]
    public async Task InterruptedTransactionNeverLeavesPartialEvidence(string stage, int count)
    {
        using var db = new Database(); var memory = db.Memory();
        memory.ObservationTestHook = point => { if (point == stage) throw new InvalidOperationException("fixture-crash"); };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Acquire(memory, new Source(Value), new(At)));
        Assert.Equal(count, db.Memory().MarketKnowledgeAt(At, new Clock(At)).Observations.Length);
    }

    [Fact]
    public async Task ConcurrentConnectionsCannotCommitConflictingOrDuplicateIdentity()
    {
        using var db = new Database(); var first = db.Memory(); var second = db.Memory(); var clock = new Clock(At);
        var results = await Task.WhenAll(Task.Run(() => Acquire(first, new Source(Value), clock)),
            Task.Run(() => Acquire(second, new Source(Value), clock)));
        Assert.Equal(results[0], results[1]);
        Assert.Single(db.Memory().MarketKnowledgeAt(At, clock).Observations);
        clock.Now = At.AddMinutes(1);
        var attempts = await Task.WhenAll(new[] { 100m, 102m }.Select(close => Task.Run(async () =>
        {
            try { await Acquire(db.Memory(), new Source(Value with { Close = close }), clock, results[0].Id); return true; }
            catch (InvalidDataException) { return false; }
        })));
        Assert.Single(attempts, x => x);
        Assert.Equal(2, db.Memory().MarketKnowledgeAt(clock.Now, clock).Observations.Length);
    }

    [Fact]
    public void MigrationRollbackAndReopenPreserveV1Authority()
    {
        using var db = new Database(false);
        Assert.Throws<InvalidOperationException>(() => FinanceSchemaMigrator.Migrate(db.Path, v =>
        { if (v == 96) throw new InvalidOperationException("fixture-crash"); }));
        Assert.Equal(95, FinanceSchemaMigrator.State(db.Path).CurrentVersion);
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM sqlite_master WHERE name='market_observation_receipts'"));
        var memory = db.Memory(); memory.EnrollSyntheticLearning(ResearchLearningFixture.Scope(), LearningExposure.Unexposed);
        Assert.True(memory.ReserveLearningInvocation());
        FinanceSchemaMigrator.Migrate(db.Path);
        Assert.False(db.Memory().ReserveLearningInvocation());
        Assert.Equal(96, FinanceSchemaMigrator.State(db.Path).CurrentVersion);
    }

    [Theory]
    [InlineData("UPDATE market_observation_receipts SET checksum='broken'")]
    [InlineData("UPDATE market_observation_receipts SET ingested_ticks=1")]
    [InlineData("DELETE FROM market_observation_clock")]
    [InlineData("UPDATE market_observation_clock SET watermark_ticks=0")]
    [InlineData("INSERT INTO finance_schema_migrations VALUES(999,'fixture','fixture')")]
    public async Task CorruptOrUnsupportedStateFailsClosed(string sql)
    {
        using var db = new Database(); var memory = db.Memory(); await Acquire(memory, new Source(Value), new(At));
        db.Execute(sql);
        Assert.ThrowsAny<Exception>(() => memory.MarketKnowledgeAt(At, new Clock(At)));
    }

    [Fact]
    public async Task DeniedRightsCancellationAndSourceFailureCreateNothing()
    {
        using var db = new Database(); var clock = new Clock(At); var source = new Source(Value);
        await Assert.ThrowsAsync<InvalidDataException>(() => db.Memory().AcquireMarketObservationAsync(source, Instrument, Mapping,
            Policy(EntitlementDecision.Denied), clock, CancellationToken.None));
        Assert.Equal(0, source.Calls);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => db.Memory().AcquireMarketObservationAsync(source, Instrument, Mapping,
            Policy(), clock, new CancellationToken(true)));
        Assert.Equal(0, source.Calls);
        source.Fail = true;
        await Assert.ThrowsAsync<IOException>(() => Acquire(db.Memory(), source, clock));
        Assert.Empty(db.Memory().MarketKnowledgeAt(At, clock).Observations);
    }

    [Fact]
    public async Task ConcurrentCutoffAndReceiptCannotChangePublishedPast()
    {
        using var db = new Database(); var writer = db.Memory(); var reader = db.Memory(); var clock = new Clock(At);
        using var start = new ManualResetEventSlim();
        var write = Task.Run(async () =>
        {
            start.Wait(TestContext.Current.CancellationToken);
            try { await Acquire(writer, new Source(Value), clock); }
            catch (InvalidDataException) { /* Query sealed the instant first: fail closed. */ }
        }, TestContext.Current.CancellationToken);
        var read = Task.Run(() => { start.Wait(TestContext.Current.CancellationToken); return reader.MarketKnowledgeAt(At, clock); });
        start.Set(); await Task.WhenAll(write, read);
        Assert.Equal((await read).Checksum, db.Memory().MarketKnowledgeAt(At, clock).Checksum);
    }

    [Fact]
    public async Task OldEventAcquiredNowIsNeverPretendedToHaveBeenKnownEarlier()
    {
        using var db = new Database(); var clock = new Clock(At);
        var old = new Source(Value with { EventTimeUtc = At.AddDays(-30) });
        var receipt = await Acquire(db.Memory(), old, clock);
        Assert.Equal(At, receipt.KnowledgeTimeUtc);
        Assert.Empty(db.Memory().MarketKnowledgeAt(At.AddDays(-1), clock).Observations);
    }

    [Fact]
    public async Task SameValuesCannotSilentlyReplaceMappingProvenance()
    {
        using var db = new Database(); var clock = new Clock(At); var source = new Source(Value);
        var original = await Acquire(db.Memory(), source, clock);
        var remapped = new ProviderInstrumentMapping(Mapping.InstrumentId, Mapping.Provider, Mapping.ProviderDataset,
            Mapping.ProviderReference, Mapping.Venue, Mapping.Mic, Mapping.ValidFrom, Mapping.ValidTo, new("fixture:different-mapping"));
        clock.Now = At.AddMinutes(1);
        await Assert.ThrowsAsync<InvalidDataException>(() => db.Memory().AcquireMarketObservationAsync(source, Instrument,
            remapped, Policy(), clock, CancellationToken.None));
        Assert.Equal(original, Assert.Single(db.Memory().MarketKnowledgeAt(clock.Now, clock).Observations));
    }

    internal static MarketDataEntitlementPolicy Policy(EntitlementDecision persistence = EntitlementDecision.Allowed, ProviderInstrumentMapping? mapping = null) => new(
        new("fixture-observation"), new("v1"), (mapping ?? Mapping).Provider, (mapping ?? Mapping).ProviderDataset, new("fixture:observation-rights"),
        At.AddDays(-1), At.AddDays(-1), null,
        new Dictionary<MarketDataUse, EntitlementDecision>
        {
            [MarketDataUse.HistoricalAnalysis] = EntitlementDecision.Allowed,
            [MarketDataUse.LongTermStorage] = EntitlementDecision.Allowed
        }, persistence, EntitlementDecision.Allowed,
        RetentionClassification.LongTerm, DeletionRequirement.None, evidenceClass: EntitlementEvidenceClass.ExplicitProviderGrant);
    private static Task<MarketObservationReceipt> Acquire(EodhdMarketMemory memory, Source source, Clock clock, string? corrects = null) =>
        memory.AcquireMarketObservationAsync(source, Instrument, Mapping, Policy(), clock, CancellationToken.None, corrects);
    internal sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Source(ProviderObservation value) : IMarketObservationSource
    {
        public MarketDataProvider Provider => Mapping.Provider;
        public ProviderDataset Dataset => Mapping.ProviderDataset;
        public string AdapterVersion => "fixture-v1";
        public MarketObservationOrigin Origin { get; init; } = MarketObservationOrigin.DeterministicFixture;
        internal int Calls { get; private set; }
        internal bool Fail { get; set; }
        public Task<ProviderObservation> ReadAsync(ProviderInstrumentMapping mapping, CancellationToken cancellationToken)
        { Calls++; return Fail ? throw new IOException("fixture unavailable") : Task.FromResult(value); }
    }
    internal sealed class Database : IDisposable
    {
        private readonly string _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bb132d", Guid.NewGuid().ToString("N"));
        internal string Path => System.IO.Path.Combine(_root, "finance.db");
        internal Database(bool initialize = true) { Directory.CreateDirectory(_root); if (initialize) _ = Memory(); }
        internal EodhdMarketMemory Memory() => new(new EodhdFinanceOptions { DatabasePath = Path, PayloadDirectory = System.IO.Path.Combine(_root, "payloads") });
        internal object? Scalar(string sql) { using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = sql; return cmd.ExecuteScalar(); }
        internal void Execute(string sql) { using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery(); }
        private SqliteConnection Open() { var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path, Pooling = false }.ToString()); c.Open(); return c; }
        public void Dispose() => Directory.Delete(_root, true);
    }
}
