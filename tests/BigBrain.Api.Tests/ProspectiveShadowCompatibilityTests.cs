using System.Text.Json;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

// BB-132E characterization only. No prospective strategy or snapshot-to-daily adapter.
public sealed class ProspectiveShadowCompatibilityTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 1, 15, 0, 0, TimeSpan.Zero);
    private static readonly string FixturePayloadChecksum = MarketObservationIntegrity.Hash(["bb132e-fixture-only"]);

    [Fact]
    public async Task FullScheduledMinuteCoverageAndElapsedCloseDoNotEstablishFinality()
    {
        using var db = new FinanceMarketObservationTests.Database();
        var session = Assert.IsType<UsEquitySession>(UsMarketCalendar.Session(new(2026, 10, 1)));
        var minuteCount = checked((int)(session.CloseUtc - session.OpenUtc).TotalMinutes);
        var clock = new FinanceMarketObservationTests.Clock(session.OpenUtc);
        MarketObservationReceipt? last = null;
        for (var minute = 0; minute < minuteCount; minute++)
        {
            var eventTime = session.OpenUtc.AddMinutes(minute);
            clock.Now = eventTime.AddSeconds(10); // Still inside each minute: a valid partial snapshot.
            last = await Acquire(db, clock, Value(eventTime));
        }
        clock.Now = session.CloseUtc.AddSeconds(1);
        var cutoff = clock.Now;
        var afterClose = db.Memory().MarketKnowledgeAt(cutoff, clock);
        Assert.Equal(minuteCount, afterClose.Observations.Length);
        Assert.Equal(Enumerable.Range(0, minuteCount).Select(i => session.OpenUtc.AddMinutes(i)),
            afterClose.Observations.Select(row => row.Value.EventTimeUtc));
        Assert.All(afterClose.Observations, row =>
        {
            Assert.Equal(LiveObservationGranularity.Snapshot, row.Granularity);
            Assert.True(row.KnowledgeTimeUtc < row.Value.EventTimeUtc.AddMinutes(1));
        });
        Assert.NotNull(last);
        // Silence after the scheduled close does not refresh the old receipt or assert finality.
        Assert.Equal(last, await Acquire(db, clock, last.Value));
        clock.Now = cutoff.AddSeconds(1);
        var revised = await Acquire(db, clock, last.Value with { High = 150, Close = 125, Volume = 20000 }, last.Id);
        Assert.Equal(last.Id, revised.CorrectsId);
        Assert.Equal(LiveObservationGranularity.Snapshot, revised.Granularity);
        Assert.True(revised.KnowledgeTimeUtc > cutoff);
        Assert.Equal(JsonSerializer.Serialize(afterClose),
            JsonSerializer.Serialize(db.Memory().MarketKnowledgeAt(cutoff, clock)));
        Assert.Equal(minuteCount + 1, db.Memory().MarketKnowledgeAt(clock.Now, clock).Observations.Length);
        // A later correction alone does not disprove finality: the missing fact is the
        // explicit completed-session/selected-minute commitment, absent from this contract.
    }

    [Fact]
    public async Task PersistedMinuteSnapshotsCannotBeRelabelledAsDailyFeatureObservations()
    {
        using var db = new FinanceMarketObservationTests.Database();
        var clock = new FinanceMarketObservationTests.Clock(At);
        var first = await Acquire(db, clock, Value(At.AddMinutes(-2)));
        clock.Now = At.AddSeconds(1);
        await Acquire(db, clock, Value(At.AddMinutes(-1)));
        var observations = db.Memory().MarketKnowledgeAt(clock.Now, clock).Observations;
        Assert.Equal(2, observations.Length);
        Assert.All(observations, row =>
        {
            Assert.Equal(LiveObservationGranularity.Snapshot, row.Granularity);
            Assert.Equal(Timeframe.OneMinute, row.Value.Interval);
            Assert.Equal(PriceAdjustment.Raw, row.Adjustment);
            Assert.Null(row.Value.ProviderAvailableAtUtc);
        });
        Assert.Equal(first.Checksum, observations[0].Checksum);

        // Deliberately lossy TEST-ONLY cast witnesses why DateOnly is not an adapter.
        // The real daily engine rejects the collapsed instrument/session identities.
        var relabelled = observations.Select(row => new DailyFeatureObservation(row.Instrument.Id,
            DateOnly.FromDateTime(row.Value.EventTimeUtc.UtcDateTime), row.Value.Open, row.Value.High,
            row.Value.Low, row.Value.Close, row.Value.Volume, row.KnowledgeTimeUtc, new(row.Id)));
        var error = Assert.Throws<ArgumentException>(() => DeterministicDailyFeatureEngine.Build(relabelled));
        Assert.StartsWith("Feature inputs must be unique per instrument/session.", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReceiptRevisionsRemainSnapshotsAndDoNotAssertFinalSessionClose()
    {
        using var db = new FinanceMarketObservationTests.Database();
        var clock = new FinanceMarketObservationTests.Clock(At);
        var original = await Acquire(db, clock, Value(At.AddMinutes(-1)));
        clock.Now = At.AddSeconds(1);
        var revision = await Acquire(db, clock, Value(At.AddMinutes(-1)) with { Close = 100 }, original.Id);
        var reopened = db.Memory().MarketKnowledgeAt(clock.Now, clock).Observations;
        Assert.Equal(2, reopened.Length);
        Assert.Equal(original, reopened[0]);
        Assert.Equal(revision, reopened[1]);
        Assert.Equal(original.LogicalIdentity, revision.LogicalIdentity);
        Assert.Equal(original.Id, revision.CorrectsId);
        Assert.All(reopened, row => Assert.Equal(LiveObservationGranularity.Snapshot, row.Granularity));
        Assert.Equal(original, Assert.Single(db.Memory().MarketKnowledgeAt(At, clock).Observations));
    }

    [Fact]
    public async Task SealedKnowledgeReopensUnchangedAfterLaterAcquisitionOfOlderEvent()
    {
        using var db = new FinanceMarketObservationTests.Database();
        var clock = new FinanceMarketObservationTests.Clock(At);
        // This is only a hypothetical freeze boundary, not an implemented shadow candidate.
        var before = db.Memory().MarketKnowledgeAt(At, clock);
        clock.Now = At.AddSeconds(1);
        var receipt = await Acquire(db, clock, Value(At.AddDays(-1)));
        Assert.True(receipt.Value.EventTimeUtc < At);
        Assert.True(receipt.KnowledgeTimeUtc > At);
        var cutoff = clock.Now;
        var sealedProjection = db.Memory().MarketKnowledgeAt(cutoff, clock);
        Assert.Equal(receipt, Assert.Single(sealedProjection.Observations));
        clock.Now = At.AddSeconds(2);
        await Acquire(db, clock, Value(At.AddDays(-2)));
        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(db.Memory().MarketKnowledgeAt(At, clock)));
        Assert.Equal(JsonSerializer.Serialize(sealedProjection),
            JsonSerializer.Serialize(db.Memory().MarketKnowledgeAt(cutoff, clock)));
    }

    [Fact]
    public void SyntheticSnapshotPipelineCannotSubstituteForAcceptedMomentumStrategy()
    {
        var strategy = new ShadowStrategyVersion(new MomentumResearchStrategy().Identity.Id, "v1",
            new("fixture:configuration"), new("fixture:features"), new("fixture:risk"), new("fixture:build"));
        var error = Assert.Throws<InvalidOperationException>(() => SyntheticShadowLearningPipeline.Evaluate(
            FinanceMarketObservationTests.Instrument.Id, At, TimeSpan.FromMinutes(1), strategy, []));
        Assert.Equal("Only the explicit non-production fixture strategy is supported.", error.Message);
    }

    private static ProviderObservation Value(DateTimeOffset eventTime) => new("AAPL", "XNAS", new("USD"),
        Timeframe.OneMinute, eventTime, null, 100, 102, 99, 101, 10000,
        FixturePayloadChecksum);

    private static Task<MarketObservationReceipt> Acquire(FinanceMarketObservationTests.Database db,
        FinanceMarketObservationTests.Clock clock, ProviderObservation value, string? preceding = null) =>
        db.Memory().AcquireMarketObservationAsync(new Source(value), FinanceMarketObservationTests.Instrument,
            FinanceMarketObservationTests.Mapping, FinanceMarketObservationTests.Policy(), clock,
            TestContext.Current.CancellationToken, preceding);

    private sealed class Source(ProviderObservation value) : IMarketObservationSource
    {
        public MarketDataProvider Provider => FinanceMarketObservationTests.Mapping.Provider;
        public ProviderDataset Dataset => FinanceMarketObservationTests.Mapping.ProviderDataset;
        public string AdapterVersion => "bb132e-characterization-v1";
        public MarketObservationOrigin Origin => MarketObservationOrigin.DeterministicFixture;
        public Task<ProviderObservation> ReadAsync(ProviderInstrumentMapping mapping, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(value);
        }
    }
}
