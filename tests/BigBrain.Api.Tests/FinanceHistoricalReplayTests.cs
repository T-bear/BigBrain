using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceHistoricalReplayTests
{
    private static readonly Currency Sek = new("SEK");
    private static readonly MarketDataProvider Provider = new("ExampleData");
    private static readonly ProviderDataset Dataset = new("Synthetic-EOD-Personal");
    private static readonly DatasetRevisionId Revision = new("replay-revision-001");
    private static readonly InstrumentId InstrumentId = new("BB-EQ-TEST-001");
    private static readonly MarketVenue Xsto = new("XSTO", "Synthetic Stockholm");
    private const string Zone = "Europe/Stockholm";

    [Fact]
    public void TradingSessionsReplayChronologicallyWithoutLookahead()
    {
        var dates = new[] { new DateOnly(2024, 6, 3), new DateOnly(2024, 6, 4), new DateOnly(2024, 6, 5) };
        var request = Request(dates.Select(date => Bar(date)).ToArray(), [], [], From(dates[0]), Until(dates[^1]));
        var events = Replay(Calendar(dates.Select(Trading))).Replay(request);

        Assert.Equal(dates, events.Where(value => value.Type == HistoricalReplayEventType.ObservationAvailable).Select(value => value.TradingDate));
        Assert.True(events.Zip(events.Skip(1), (left, right) => left.EffectiveAtUtc <= right.EffectiveAtUtc).All(value => value));
        Assert.All(events, value => Assert.InRange(value.EffectiveAtUtc, request.FromUtc, request.UntilUtc));
    }

    [Fact]
    public void ExplicitClosureIsNotMissingData()
    {
        var date = new DateOnly(2024, 6, 8);
        var events = Replay(Calendar([Closed(date)])).Replay(Request([], [], [], From(date), Until(date)));
        var closure = Assert.Single(events);
        Assert.Equal(HistoricalReplayEventType.ExpectedClosure, closure.Type);
        Assert.Equal(ObservationGapClassification.ExpectedClosure, closure.GapClassification);
        Assert.DoesNotContain(events, value => value.Type == HistoricalReplayEventType.MissingObservationDetected);
    }

    [Fact]
    public void UnknownCalendarDateRemainsUnknown()
    {
        var date = new DateOnly(2024, 6, 9);
        var unknown = Assert.Single(Replay(Calendar([])).Replay(Request([], [], [], From(date), Until(date))));
        Assert.Equal(HistoricalReplayEventType.UnknownSession, unknown.Type);
        Assert.Equal(ObservationGapClassification.UnknownSession, unknown.GapClassification);
        Assert.NotEqual(ObservationGapClassification.ProviderGap, unknown.GapClassification);
    }

    [Fact]
    public void ExpectedSessionWithoutBarIsGenericMissingObservation()
    {
        var date = new DateOnly(2024, 6, 3);
        var missing = Assert.Single(Replay(Calendar([Trading(date)])).Replay(Request([], [], [], From(date), Until(date))),
            value => value.Type == HistoricalReplayEventType.MissingObservationDetected);
        Assert.Equal(ObservationGapClassification.MissingObservation, missing.GapClassification);
    }

    [Fact]
    public void ProviderGapRequiresAndPreservesExplicitEvidence()
    {
        var date = new DateOnly(2024, 6, 3);
        var evidence = Finding(date, MarketDataFindingCode.ProviderGap, ObservationGapClassification.ProviderGap);
        var events = Replay(Calendar([Trading(date)])).Replay(Request([], [], [evidence], From(date), Until(date)));
        var gap = Assert.Single(events, value => value.Type == HistoricalReplayEventType.MissingObservationDetected);
        Assert.Equal(ObservationGapClassification.ProviderGap, gap.GapClassification);
        Assert.Contains(events, value => value.QualityEvidence == evidence);
        Assert.Throws<ArgumentException>(() => Finding(date, MarketDataFindingCode.MissingObservation, ObservationGapClassification.ProviderGap).Validate());
    }

    [Fact]
    public void InvalidObservationIsNotEmittedAsMarketPrice()
    {
        var date = new DateOnly(2024, 6, 3);
        var evidence = Finding(date, MarketDataFindingCode.InvalidPriceRange, ObservationGapClassification.InvalidObservation);
        var events = Replay(Calendar([Trading(date)])).Replay(Request([Bar(date)], [], [evidence], From(date), Until(date)));
        Assert.DoesNotContain(events, value => value.Type == HistoricalReplayEventType.ObservationAvailable);
        Assert.Contains(events, value => value.GapClassification == ObservationGapClassification.InvalidObservation);
    }

    [Fact]
    public void LaterQualityEvidenceDoesNotLeakBackwardsToHideEarlierObservation()
    {
        var date = new DateOnly(2024, 6, 3);
        var late = Finding(date, MarketDataFindingCode.InvalidPriceRange, ObservationGapClassification.InvalidObservation) with
        { ObservedAtUtc = new DateTimeOffset(date.ToDateTime(new TimeOnly(19, 0), DateTimeKind.Utc)) };
        var events = Replay(Calendar([Trading(date)])).Replay(Request([Bar(date)], [], [late], From(date), Until(date)));
        var observation = Assert.Single(events, value => value.Type == HistoricalReplayEventType.ObservationAvailable);
        var finding = Assert.Single(events, value => value.Type == HistoricalReplayEventType.QualityFindingObserved);
        Assert.True(observation.EffectiveAtUtc < finding.EffectiveAtUtc);
    }

    [Fact]
    public void ReplayUsesHistoricalProviderReferenceAcrossTickerBoundary()
    {
        var oldDate = new DateOnly(2024, 5, 31);
        var newDate = new DateOnly(2024, 6, 1);
        var events = Replay(Calendar([Trading(oldDate), Trading(newDate)])).Replay(
            Request([Bar(oldDate, "TEST-A"), Bar(newDate, "TEST-B")], [], [], From(oldDate), Until(newDate)));
        Assert.All(events.Where(value => value.TradingDate == oldDate), value => Assert.Equal("TEST-A", value.ProviderReference));
        Assert.All(events.Where(value => value.TradingDate == newDate), value => Assert.Equal("TEST-B", value.ProviderReference));
        Assert.All(events, value => Assert.Equal(InstrumentId, value.InstrumentId));
    }

    [Fact]
    public void CorporateActionsRemainExplicitAndSameTimeOrderIsStable()
    {
        var date = new DateOnly(2024, 6, 3);
        var dividend = Action(date, CorporateActionType.CashDividend);
        var split = Action(date, CorporateActionType.StockSplit);
        var request = Request([Bar(date)], [split, dividend], [], From(date), Until(date));
        var first = Replay(Calendar([Trading(date)])).Replay(request);
        var second = Replay(Calendar([Trading(date)])).Replay(request);
        Assert.Equal(first.Select(value => value.LogicalIdentity), second.Select(value => value.LogicalIdentity));
        Assert.Equal(new[] { HistoricalReplayEventType.SessionOpened, HistoricalReplayEventType.DividendEffective,
            HistoricalReplayEventType.SplitEffective }, first.Take(3).Select(value => value.Type));
        Assert.Equal(new ExactRatio(2, 1), first.Single(value => value.Type == HistoricalReplayEventType.SplitEffective).CorporateAction!.SplitRatio);
        Assert.Equal(PriceAdjustment.Raw, first.Single(value => value.Type == HistoricalReplayEventType.ObservationAvailable).Bar!.Adjustment);
    }

    [Fact]
    public void MixedDatasetRevisionFailsWithoutMutatingInput()
    {
        var date = new DateOnly(2024, 6, 3);
        var original = Bar(date);
        var foreign = original with { Provenance = Provenance(date, new DatasetRevisionId("other-revision")) };
        var bars = new[] { original, foreign };
        var request = Request(bars, [], [], From(date), Until(date));
        Assert.Throws<ArgumentException>(() => Replay(Calendar([Trading(date)])).Replay(request));
        Assert.Same(original, bars[0]);
        Assert.Same(foreign, bars[1]);
    }

    [Fact]
    public void FutureEvidenceAndFutureSymbolAreNotVisibleBeforeRangeEnd()
    {
        var current = new DateOnly(2024, 5, 31);
        var future = new DateOnly(2024, 6, 1);
        var request = Request([Bar(current, "TEST-A"), Bar(future, "TEST-B")], [Action(future, CorporateActionType.StockSplit)], [],
            From(current), Trading(current).ClosesAtUtc!.Value);
        var events = Replay(Calendar([Trading(current), Trading(future)])).Replay(request);
        Assert.All(events, value => Assert.Equal(current, value.TradingDate));
        Assert.All(events, value => Assert.Equal("TEST-A", value.ProviderReference));
        Assert.DoesNotContain(events, value => value.CorporateAction is not null);
    }

    [Fact]
    public void SessionConversionUsesExplicitTimezoneAndDstOffset()
    {
        var winter = Trading(new DateOnly(2024, 3, 29));
        var summer = Trading(new DateOnly(2024, 4, 2));
        Assert.Equal(TimeSpan.FromHours(8), winter.OpensAtUtc!.Value.TimeOfDay);
        Assert.Equal(TimeSpan.FromHours(7), summer.OpensAtUtc!.Value.TimeOfDay);
        Assert.Equal(Zone, winter.TimeZoneId);
        Assert.Equal(TimeSpan.Zero, winter.OpensAtUtc.Value.Offset);
    }

    [Fact]
    public void AmbiguousOrInvalidDstLocalTimeFailsInsteadOfGuessing()
    {
        Assert.Throws<ArgumentException>(() => MarketSession.Trading(new MarketSessionId("dst-gap"), Xsto, "XSTO",
            new DateOnly(2024, 3, 31), new TimeOnly(2, 30), new TimeOnly(4, 0), Zone,
            MarketSessionKind.Exceptional, new EvidenceReference("fixture:dst")));
        Assert.Throws<ArgumentException>(() => MarketSession.Trading(new MarketSessionId("dst-overlap"), Xsto, "XSTO",
            new DateOnly(2024, 10, 27), new TimeOnly(2, 30), new TimeOnly(4, 0), Zone,
            MarketSessionKind.Exceptional, new EvidenceReference("fixture:dst")));
    }

    private static DeterministicHistoricalReplay Replay(IMarketSessionCalendar calendar) => new(calendar, Catalog());

    private static SyntheticMarketSessionCalendar Calendar(IEnumerable<MarketSession> sessions) => new(Xsto, "XSTO", Zone, sessions);

    private static MarketSession Trading(DateOnly date) => MarketSession.Trading(new MarketSessionId($"XSTO-{date:yyyyMMdd}"), Xsto,
        "XSTO", date, new TimeOnly(9, 0), new TimeOnly(17, 30), Zone, MarketSessionKind.Regular, new EvidenceReference("fixture:calendar-v1"));

    private static MarketSession Closed(DateOnly date) => MarketSession.Closed(new MarketSessionId($"XSTO-CLOSED-{date:yyyyMMdd}"), Xsto,
        "XSTO", date, Zone, MarketSessionKind.Regular, new EvidenceReference("fixture:known-weekend"));

    private static HistoricalReplayRequest Request(IReadOnlyList<CanonicalMarketBar> bars,
        IReadOnlyList<CanonicalCorporateAction> actions, IReadOnlyList<ReplayQualityEvidence> findings,
        DateTimeOffset from, DateTimeOffset until) => new(Revision, Provider, Dataset, "XSTO", from, until,
            [InstrumentId], bars, actions, findings);

    private static CanonicalMarketBar Bar(DateOnly date, string? symbol = null)
    {
        var actualSymbol = symbol ?? (date <= new DateOnly(2024, 5, 31) ? "TEST-A" : "TEST-B");
        return new SyntheticMarketDataNormalizer(Catalog()).Normalize(new SyntheticRawDailyBar(Provider, Dataset,
            actualSymbol, "XSTO", date, 10m, 11m, 9m, 10m, 1000m, Sek, PriceAdjustment.Raw, null,
            new DateTimeOffset(date.ToDateTime(new TimeOnly(18, 0), DateTimeKind.Utc)),
            new DateTimeOffset(date.ToDateTime(new TimeOnly(18, 5), DateTimeKind.Utc)), Revision, Policy(),
            new VersionReference("fixture-v1"), new VersionReference("schema-v1")));
    }

    private static CanonicalCorporateAction Action(DateOnly date, CorporateActionType type) =>
        new(new CorporateActionId(type == CorporateActionType.CashDividend ? "dividend-001" : "split-001"), type,
            InstrumentId, date, date, type == CorporateActionType.CashDividend ? new Money(1m, Sek) : null,
            type == CorporateActionType.StockSplit ? new ExactRatio(2, 1) : null, Provenance(date, Revision));

    private static ReplayQualityEvidence Finding(DateOnly date, MarketDataFindingCode code, ObservationGapClassification classification) =>
        new(InstrumentId, date, code, classification,
            new DateTimeOffset(date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc)), Revision, new EvidenceReference("fixture:quality-v1"));

    private static MarketDataProvenance Provenance(DateOnly date, DatasetRevisionId revision) => new(Provider, Dataset,
        new DateTimeOffset(date.ToDateTime(new TimeOnly(18, 5), DateTimeKind.Utc)),
        new DateTimeOffset(date.ToDateTime(new TimeOnly(18, 0), DateTimeKind.Utc)), InstrumentId, Xsto,
        revision, Policy(), MarketDataClassification.Raw, [], new VersionReference("fixture-v1"),
        new VersionReference("schema-v1"), MarketDataQualityStatus.Valid);

    private static InstrumentMappingCatalog Catalog() => new([
        new CanonicalInstrument(InstrumentId, InstrumentType.Equity, "Synthetic Example AB", Sek, Xsto, "XSTO",
            InstrumentLifecycle.Active, new DateOnly(2020, 1, 1))], [
        new ProviderInstrumentMapping(InstrumentId, Provider, Dataset, "TEST-A", Xsto, "XSTO", new DateOnly(2020, 1, 1),
            new DateOnly(2024, 5, 31), new EvidenceReference("fixture:mapping-v1")),
        new ProviderInstrumentMapping(InstrumentId, Provider, Dataset, "TEST-B", Xsto, "XSTO", new DateOnly(2024, 6, 1),
            null, new EvidenceReference("fixture:mapping-v1"))]);

    private static MarketDataPolicyReference Policy() => new(new PolicyId("synthetic-policy"), new PolicyVersion("1"));
    private static DateTimeOffset From(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
    private static DateTimeOffset Until(DateOnly date) => new(date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));
}
