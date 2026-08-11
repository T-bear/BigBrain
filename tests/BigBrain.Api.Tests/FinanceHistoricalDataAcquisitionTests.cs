using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceHistoricalDataAcquisitionTests
{
    private static readonly MarketDataProvider Provider = new(SyntheticHistoricalDataAdapter.ProviderName);
    private static readonly ProviderDataset Product = new("Synthetic-EOD-Personal");
    private static readonly InstrumentId Instrument = new("BB-EQ-TEST-001");
    private static readonly MarketVenue Xsto = new("XSTO", "Synthetic Stockholm");
    private static readonly Currency Sek = new("SEK");
    private static readonly DateTimeOffset Acquired = Utc(2024, 6, 5, 20);

    [Fact]
    public void AcquisitionContractIsDeterministicAndContainsNoCredentialSurface()
    {
        Assert.Equal(Request(), Request());
        var names = typeof(HistoricalDataAcquisitionRequest).GetProperties().Select(value => value.Name)
            .Concat(typeof(AcquisitionJournalEntry).GetProperties().Select(value => value.Name));
        Assert.DoesNotContain(names, value => value.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Token", StringComparison.OrdinalIgnoreCase) || value.Contains("ApiKey", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Authorization", StringComparison.OrdinalIgnoreCase) || value.Contains("Header", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MissingEntitlementIsDeniedBeforeAdapterIsCalled()
    {
        var adapter = new CountingAdapter();
        var result = Pipeline().Prepare(Request(), null, adapter);
        Assert.Equal(AcquisitionOutcome.Rejected, result.Journal.Outcome);
        Assert.Equal(MarketDataEntitlementReasons.PolicyMissing, result.Journal.ReasonCode);
        Assert.Equal(0, adapter.Calls);
        Assert.Null(result.Revision);
    }

    [Fact]
    public void SyntheticEntitlementAcceptsOnlyUnmistakablySyntheticSource()
    {
        var result = Pipeline().Prepare(Request(), Policy(), Adapter(Batch("batch-1", [Bar("bar-1", new DateOnly(2024, 6, 3), 10m)])));
        Assert.Equal(AcquisitionOutcome.Accepted, result.Journal.Outcome);
        Assert.Single(result.Bars);
        Assert.Equal(new DatasetRevisionId("revision-001"), result.Revision!.Id);
        Assert.Equal("fixture:synthetic-acquisition-policy", result.Journal.PolicyEvidence.Value);
    }

    [Fact]
    public void UnauthorizedProviderAndRetentionFailClosed()
    {
        var wrongProvider = Policy(provider: new MarketDataProvider("NotSynthetic"));
        var providerResult = AcquisitionEntitlementGate.Evaluate(Request(), wrongProvider);
        var retentionResult = AcquisitionEntitlementGate.Evaluate(Request(), Policy(persistence: EntitlementDecision.Denied));
        Assert.False(providerResult.IsAllowed);
        Assert.Equal("marketData.acquisition.syntheticPolicyInvalid", providerResult.ReasonCode);
        Assert.False(retentionResult.IsAllowed);
        Assert.Equal(MarketDataEntitlementReasons.PersistenceDenied, retentionResult.ReasonCode);
    }

    [Fact]
    public void DuplicateBatchAndOverlappingPaginationAreIdempotent()
    {
        var first = Batch("batch-1", [Bar("bar-1", new DateOnly(2024, 6, 3), 10m)]);
        var firstCopy = Batch("batch-1", [Bar("bar-1", new DateOnly(2024, 6, 3), 10m)]);
        var overlap = Batch("batch-2", [Bar("bar-1-copy", new DateOnly(2024, 6, 3), 10m),
            Bar("bar-2", new DateOnly(2024, 6, 4), 11m)], requestCursor: "page-1");
        var result = Pipeline().Prepare(Request(), Policy(), Adapter(overlap, first, firstCopy));
        Assert.Equal(3, result.Journal.ReceivedBatchCount);
        Assert.Equal(1, result.Journal.DuplicateBatchCount);
        Assert.Equal(1, result.Journal.DuplicateObservationCount);
        Assert.Equal(2, result.Bars.Length);
        Assert.Equal([new DateOnly(2024, 6, 3), new DateOnly(2024, 6, 4)], result.Bars.Select(value => value.SessionDate));
    }

    [Fact]
    public void PaginationAndProviderOrderingDoNotChangeRevisionOrJournal()
    {
        var one = Batch("batch-b", [Bar("bar-2", new DateOnly(2024, 6, 4), 11m)], requestCursor: "page-a");
        var two = Batch("batch-a", [Bar("bar-1", new DateOnly(2024, 6, 3), 10m)], nextCursor: "page-a");
        var left = Pipeline().Prepare(Request(), Policy(), Adapter(one, two));
        var right = Pipeline().Prepare(Request(), Policy(), Adapter(two, one));
        Assert.Equal(left.Journal, right.Journal);
        Assert.Equal(left.Revision!.Members.Select(Fingerprint), right.Revision!.Members.Select(Fingerprint));
    }

    [Fact]
    public void ConflictingRepeatedBatchIdentityFailsExplicitly()
    {
        var first = Batch("same", [Bar("bar-1", new DateOnly(2024, 6, 3), 10m)]);
        var conflict = Batch("same", [Bar("bar-1", new DateOnly(2024, 6, 3), 12m)]);
        Assert.Throws<ArgumentException>(() => Pipeline().Prepare(Request(), Policy(), Adapter(first, conflict)));
    }

    [Fact]
    public void CorrectionCreatesSupersedingRevisionWithoutOverwritingParent()
    {
        var first = Pipeline().Prepare(Request(), Policy(), Adapter(Batch("base", [Bar("bar-v1", new DateOnly(2024, 6, 3), 10m)])));
        var correctionTime = Acquired.AddDays(2);
        var childRequest = Request("revision-002", correctionTime, first.Revision!.Id);
        var corrected = Bar("bar-v2", new DateOnly(2024, 6, 3), 10.5m, "revision-002", correctionTime);
        var correction = new AcquiredCorrection(new DatasetCorrectionId("correction-1"), new ProviderObservationId("bar-v1"),
            new ProviderObservationId("bar-v2"), correctionTime, "provider.correction.synthetic", new EvidenceReference("fixture:correction-1"));
        var second = Pipeline().Prepare(childRequest, Policy(validAt: correctionTime),
            Adapter(Batch("correction", [corrected], corrections: [correction], receivedAt: correctionTime,
                revision: "revision-002")), first.Revision);
        Assert.Equal(10m, Assert.Single(first.Revision.Members).Bar!.Close.Value);
        Assert.Equal(10.5m, Assert.Single(second.Revision!.Members).Bar!.Close.Value);
        Assert.Equal(first.Revision.Id, second.Revision.ParentRevisionId);
        Assert.Single(second.Revision.Corrections);
    }

    [Fact]
    public void JournalRecordsDividendSplitCountsAndDeletionObligation()
    {
        var batch = Batch("actions", [Bar("bar-1", new DateOnly(2024, 6, 3), 10m)], actions:
            [Dividend("div-1"), Split("split-1")]);
        var result = Pipeline().Prepare(Request(), Policy(), Adapter(batch));
        Assert.Equal(3, result.Journal.AcceptedObservationCount);
        Assert.Equal(DeletionRequirement.DeleteAtSubscriptionEnd, result.Journal.Deletion);
        Assert.Equal(2, result.CorporateActions.Length);
        Assert.Contains(result.CorporateActions, value => value.Type == CorporateActionType.CashDividend);
        Assert.Contains(result.CorporateActions, value => value.SplitRatio == new ExactRatio(2, 1));
    }

    [Fact]
    public void GapEvidenceFlowsIntoImmutableRevisionAndReplay()
    {
        var gap = new AcquiredGapEvidence(new ProviderObservationId("gap-1"), Instrument, new DateOnly(2024, 6, 4),
            MarketDataFindingCode.ProviderGap, ObservationGapClassification.ProviderGap, Acquired,
            new EvidenceReference("fixture:explicit-provider-gap"));
        var result = Pipeline().Prepare(Request(), Policy(), Adapter(Batch("gap", [], gaps: [gap])));
        var member = Assert.Single(result.Revision!.Members);
        Assert.Equal(ObservationGapClassification.ProviderGap, member.QualityEvidence!.Classification);

        var calendar = new SyntheticMarketSessionCalendar(Xsto, "XSTO", "Europe/Stockholm",
            [Trading(new DateOnly(2024, 6, 4))]);
        var replay = new DeterministicHistoricalReplay(calendar, Catalog()).Replay(new HistoricalReplayRequest(
            result.Revision.Id, Provider, Product, "XSTO", Utc(2024, 6, 4, 0), Utc(2024, 6, 6, 0),
            [Instrument], [], [], [member.QualityEvidence]));
        Assert.Contains(replay, value => value.GapClassification == ObservationGapClassification.ProviderGap);
    }

    [Fact]
    public void CanonicalNormalizationAndReplayAreIdenticalForRepeatedEvidence()
    {
        var batch = Batch("batch-1", [Bar("bar-1", new DateOnly(2024, 6, 3), 10m)]);
        var first = Pipeline().Prepare(Request(), Policy(), Adapter(batch));
        var second = Pipeline().Prepare(Request(), Policy(), Adapter(batch));
        var calendar = new SyntheticMarketSessionCalendar(Xsto, "XSTO", "Europe/Stockholm",
            [Trading(new DateOnly(2024, 6, 3))]);
        var replay = new DeterministicHistoricalReplay(calendar, Catalog());
        var firstEvents = replay.Replay(ReplayRequest(first));
        var secondEvents = replay.Replay(ReplayRequest(second));
        Assert.Equal(first.Bars, second.Bars);
        Assert.Equal(firstEvents, secondEvents);
    }

    [Fact]
    public void FutureCorrectionIsInvisibleBeforeItsAvailabilityBoundary()
    {
        var first = Pipeline().Prepare(Request(), Policy(), Adapter(Batch("base", [Bar("bar-v1", new DateOnly(2024, 6, 3), 10m)])));
        var correctionTime = Acquired.AddDays(2);
        var childRequest = Request("revision-002", correctionTime, first.Revision!.Id);
        var correction = new AcquiredCorrection(new DatasetCorrectionId("correction-1"), new ProviderObservationId("bar-v1"),
            new ProviderObservationId("bar-v2"), correctionTime, "provider.correction.synthetic", new EvidenceReference("fixture:correction"));
        var second = Pipeline().Prepare(childRequest, Policy(validAt: correctionTime), Adapter(Batch("corrected",
            [Bar("bar-v2", new DateOnly(2024, 6, 3), 10.5m, "revision-002", correctionTime)],
            corrections: [correction], receivedAt: correctionTime, revision: "revision-002")), first.Revision);
        var catalog = new ImmutableDatasetRevisionCatalog([second.Revision!, first.Revision]);
        var revisionAvailable = childRequest.DestinationRevision.CreatedAtUtc;
        Assert.Equal(first.Revision.Id, catalog.ResolveAsOf(revisionAvailable.AddTicks(-1)).Id);
        Assert.Equal(second.Revision!.Id, catalog.ResolveAsOf(revisionAvailable).Id);
    }

    private static HistoricalDataIngestionPipeline Pipeline() => new(Catalog());
    private static SyntheticHistoricalDataAdapter Adapter(params HistoricalDataAcquisitionBatch[] batches) => new(batches);

    private static HistoricalDataAcquisitionRequest Request(string revision = "revision-001",
        DateTimeOffset? acquired = null, DatasetRevisionId? parent = null)
    {
        var time = acquired ?? Acquired;
        var metadata = new DatasetRevision(new DatasetRevisionId(revision), new DatasetId("synthetic-history"), parent,
            Provider, Product, new VersionReference("synthetic-adapter-v1"), new VersionReference("synthetic-schema-v1"),
            new DatasetChecksum($"sha256:{revision}"), time.AddMinutes(1), time, DatasetRevisionStatus.Complete);
        return new HistoricalDataAcquisitionRequest(new AcquisitionRequestId($"request:{revision}"),
            AcquisitionSourceKind.SyntheticFixture, Provider, Product, Instrument, "TEST-B", "XSTO",
            new DateOnly(2024, 6, 3), new DateOnly(2024, 6, 5), MarketDataInterval.Daily,
            PriceAdjustment.Raw, time, "Europe/Stockholm", PolicyReference(), metadata);
    }

    private static MarketDataEntitlementPolicy Policy(EntitlementDecision persistence = EntitlementDecision.Allowed,
        MarketDataProvider? provider = null, DateTimeOffset? validAt = null)
    {
        var time = validAt ?? Acquired;
        return new MarketDataEntitlementPolicy(new PolicyId("synthetic-acquisition-policy"), new PolicyVersion("1"),
            provider ?? Provider, Product, new EvidenceReference("fixture:synthetic-acquisition-policy"), time.AddDays(-1),
            time.AddDays(-2), time.AddDays(30), new Dictionary<MarketDataUse, EntitlementDecision>
            {
                [MarketDataUse.HistoricalAnalysis] = EntitlementDecision.Allowed,
                [MarketDataUse.Backtest] = EntitlementDecision.Allowed,
                [MarketDataUse.DerivedMetrics] = EntitlementDecision.Allowed,
                [MarketDataUse.LongTermStorage] = EntitlementDecision.Allowed
            }, persistence, EntitlementDecision.Denied, RetentionClassification.SubscriptionOnly,
            DeletionRequirement.DeleteAtSubscriptionEnd);
    }

    private static HistoricalDataAcquisitionBatch Batch(string id, IReadOnlyList<AcquiredRawBar> bars,
        IReadOnlyList<AcquiredRawCorporateAction>? actions = null, IReadOnlyList<AcquiredGapEvidence>? gaps = null,
        IReadOnlyList<AcquiredCorrection>? corrections = null, string? requestCursor = null, string? nextCursor = null,
        DateTimeOffset? receivedAt = null, string revision = "revision-001") =>
        new(new AcquisitionBatchId(id), new AcquisitionRequestId($"request:{revision}"), Provider, Product,
            receivedAt ?? Acquired, AcquisitionCompleteness.Complete, new EvidenceReference($"fixture:batch:{id}"), bars,
            actions ?? [], gaps ?? [], corrections ?? [], requestCursor, nextCursor);

    private static AcquiredRawBar Bar(string id, DateOnly date, decimal close, string revision = "revision-001",
        DateTimeOffset? available = null)
    {
        var time = available ?? Acquired;
        var value = new SyntheticRawDailyBar(Provider, Product, "TEST-B", "XSTO", date, close, close + 1m,
            close - 1m, close, 1000m, Sek, PriceAdjustment.Raw, null, Utc(date.Year, date.Month, date.Day, 18), time,
            new DatasetRevisionId(revision), PolicyReference(), new VersionReference("synthetic-adapter-v1"),
            new VersionReference("synthetic-schema-v1"));
        return new AcquiredRawBar(new ProviderObservationId(id), time, value);
    }

    private static AcquiredRawCorporateAction Dividend(string id) => new(new ProviderObservationId(id), Acquired,
        new SyntheticRawCorporateAction(new CorporateActionId(id), CorporateActionType.CashDividend, Provider, Product,
            "TEST-B", "XSTO", new DateOnly(2024, 6, 3), new DateOnly(2024, 6, 3), new Money(1m, Sek), null,
            Utc(2024, 6, 3, 8), Acquired, new DatasetRevisionId("revision-001"), PolicyReference(),
            new VersionReference("synthetic-adapter-v1"), new VersionReference("synthetic-schema-v1")));

    private static AcquiredRawCorporateAction Split(string id) => new(new ProviderObservationId(id), Acquired,
        new SyntheticRawCorporateAction(new CorporateActionId(id), CorporateActionType.StockSplit, Provider, Product,
            "TEST-B", "XSTO", new DateOnly(2024, 6, 4), new DateOnly(2024, 6, 4), null, new ExactRatio(2, 1),
            Utc(2024, 6, 4, 8), Acquired, new DatasetRevisionId("revision-001"), PolicyReference(),
            new VersionReference("synthetic-adapter-v1"), new VersionReference("synthetic-schema-v1")));

    private static HistoricalReplayRequest ReplayRequest(HistoricalDataIngestionResult result) => new(
        result.Revision!.Id, Provider, Product, "XSTO", Utc(2024, 6, 3, 0), Utc(2024, 6, 4, 23),
        [Instrument], result.Bars, result.CorporateActions, []);

    private static MarketSession Trading(DateOnly date) => MarketSession.Trading(
        new MarketSessionId($"XSTO-{date:yyyyMMdd}"), Xsto, "XSTO", date, new TimeOnly(9), new TimeOnly(17, 30),
        "Europe/Stockholm", MarketSessionKind.Regular, new EvidenceReference("fixture:calendar"));

    private static InstrumentMappingCatalog Catalog() => new([
        new CanonicalInstrument(Instrument, InstrumentType.Equity, "Synthetic Example AB", Sek, Xsto, "XSTO",
            InstrumentLifecycle.Active, new DateOnly(2020, 1, 1))], [
        new ProviderInstrumentMapping(Instrument, Provider, Product, "TEST-B", Xsto, "XSTO",
            new DateOnly(2020, 1, 1), null, new EvidenceReference("fixture:mapping"))]);

    private static MarketDataPolicyReference PolicyReference() =>
        new(new PolicyId("synthetic-acquisition-policy"), new PolicyVersion("1"));
    private static string Fingerprint(DatasetRevisionMember value) => $"{value.Id}|{value.LogicalObservationIdentity}|{value.AvailableAtUtc:O}";
    private static DateTimeOffset Utc(int year, int month, int day, int hour) => new(year, month, day, hour, 0, 0, TimeSpan.Zero);

    private sealed class CountingAdapter : IHistoricalDataAcquisitionAdapter
    {
        public int Calls { get; private set; }
        public AcquisitionSourceKind SourceKind => AcquisitionSourceKind.SyntheticFixture;
        public IReadOnlyList<HistoricalDataAcquisitionBatch> Acquire(HistoricalDataAcquisitionRequest request)
        { Calls++; return []; }
    }
}
