using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceMarketDataNormalizationTests
{
    private static readonly Currency Sek = new("SEK");
    private static readonly MarketDataProvider Provider = new("ExampleData");
    private static readonly ProviderDataset Dataset = new("Synthetic-EOD-Personal");
    private static readonly MarketVenue Xsto = new("XSTO", "Synthetic Stockholm");
    private static readonly DateTimeOffset Retrieved = new(2024, 6, 3, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CanonicalIdentitySurvivesSymbolChangeAndBoundaryIsInclusive()
    {
        var catalog = Catalog();
        var oldMapping = catalog.Resolve(Provider, Dataset, "TEST-A", "XSTO", new DateOnly(2024, 5, 31));
        var newMapping = catalog.Resolve(Provider, Dataset, "TEST-B", "XSTO", new DateOnly(2024, 6, 1));
        Assert.Equal("BB-EQ-TEST-001", oldMapping.Instrument.Id.Value);
        Assert.Equal(oldMapping.Instrument.Id, newMapping.Instrument.Id);
        Assert.Throws<MarketDataNormalizationException>(() => catalog.Resolve(Provider, Dataset, "TEST-A", "XSTO", new DateOnly(2024, 6, 1)));
    }

    [Fact]
    public void SameTickerOnDifferentVenuesResolvesByMic()
    {
        var other = Instrument("BB-EQ-TEST-002", "XNYS");
        var catalog = new InstrumentMappingCatalog([Instrument(), other], [
            Mapping("BB-EQ-TEST-001", "SAME", "XSTO", new DateOnly(2020, 1, 1), null),
            Mapping("BB-EQ-TEST-002", "SAME", "XNYS", new DateOnly(2020, 1, 1), null)]);
        Assert.Equal("BB-EQ-TEST-001", catalog.Resolve(Provider, Dataset, "SAME", "XSTO", new DateOnly(2024, 1, 1)).Instrument.Id.Value);
        Assert.Equal("BB-EQ-TEST-002", catalog.Resolve(Provider, Dataset, "SAME", "XNYS", new DateOnly(2024, 1, 1)).Instrument.Id.Value);
    }

    [Fact]
    public void OverlappingMappingsAreRejected() =>
        Assert.Throws<ArgumentException>(() => new InstrumentMappingCatalog([Instrument()], [
            Mapping("BB-EQ-TEST-001", "A", "XSTO", new DateOnly(2020, 1, 1), new DateOnly(2024, 6, 1)),
            Mapping("BB-EQ-TEST-001", "B", "XSTO", new DateOnly(2024, 6, 1), null)]));

    [Fact]
    public void UnknownMappingFailsWithStableFinding()
    {
        var error = Assert.Throws<MarketDataNormalizationException>(() => Catalog().Resolve(Provider, Dataset, "UNKNOWN", "XSTO", new DateOnly(2024, 6, 3)));
        Assert.Equal(MarketDataFindingCode.MissingMapping, error.FindingCode);
    }

    [Fact]
    public void ValidDailyBarPreservesRevisionAdjustmentPolicyAndIsDeterministic()
    {
        var normalizer = new SyntheticMarketDataNormalizer(Catalog());
        var raw = Bar();
        var first = normalizer.Normalize(raw);
        var second = normalizer.Normalize(raw);
        Assert.Equal(first, second);
        Assert.Equal(MarketDataInterval.Daily, first.Interval);
        Assert.Equal(new DatasetRevisionId("revision-001"), first.Provenance.DatasetRevisionId);
        Assert.Equal(new PolicyId("synthetic-policy"), first.Provenance.Policy.Id);
        Assert.Equal(PriceAdjustment.Raw, first.Adjustment);
    }

    [Fact]
    public void AdjustedClassificationAndBasisArePreserved()
    {
        var bar = new SyntheticMarketDataNormalizer(Catalog()).Normalize(Bar() with { Adjustment = PriceAdjustment.Adjusted, AdjustmentBasis = "split-and-dividend-v1" });
        Assert.Equal(PriceAdjustment.Adjusted, bar.Adjustment);
        Assert.Equal("split-and-dividend-v1", bar.AdjustmentBasis);
    }

    [Fact]
    public void InvalidPriceRangeIsRejected()
    {
        var error = Assert.Throws<MarketDataNormalizationException>(() => new SyntheticMarketDataNormalizer(Catalog()).Normalize(Bar() with { High = 9m }));
        Assert.Equal(MarketDataFindingCode.InvalidPriceRange, error.FindingCode);
    }

    [Fact]
    public void NegativeVolumeIsRejected()
    {
        var error = Assert.Throws<MarketDataNormalizationException>(() => new SyntheticMarketDataNormalizer(Catalog()).Normalize(Bar() with { Volume = -1m }));
        Assert.Equal(MarketDataFindingCode.NegativeVolume, error.FindingCode);
    }

    [Fact]
    public void CashDividendPreservesMoneyAndProvenance()
    {
        var action = new SyntheticMarketDataNormalizer(Catalog()).Normalize(Action(CorporateActionType.CashDividend) with { CashAmount = new Money(1.25m, Sek) });
        Assert.Equal(new Money(1.25m, Sek), action.CashAmount);
        Assert.Equal(new DatasetRevisionId("revision-001"), action.Provenance.DatasetRevisionId);
        Assert.Equal(new PolicyId("synthetic-policy"), action.Provenance.Policy.Id);
    }

    [Fact]
    public void StockSplitRetainsReducedExactRatio()
    {
        var action = new SyntheticMarketDataNormalizer(Catalog()).Normalize(Action(CorporateActionType.StockSplit) with { SplitRatio = new ExactRatio(4, 2) });
        Assert.Equal(new ExactRatio(2, 1), action.SplitRatio);
        Assert.Equal(2m, action.SplitRatio!.Value.AsDecimal());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void InvalidSplitRatioIsRejected(long numerator, long denominator) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExactRatio(numerator, denominator));

    [Fact]
    public void ExactAndConflictingDuplicatesAreReportedWithoutOverwrite()
    {
        var normalizer = new SyntheticMarketDataNormalizer(Catalog());
        var input = Bar();
        var result = normalizer.NormalizeBatch([input, input, input with { Close = 10.5m }]);
        Assert.Single(result.Accepted);
        Assert.Equal([MarketDataFindingCode.DuplicateObservation, MarketDataFindingCode.ConflictingObservation], result.Findings.Select(value => value.Code));
        Assert.Equal(10m, result.Accepted[0].Close.Value);
    }

    private static CanonicalInstrument Instrument(string id = "BB-EQ-TEST-001", string mic = "XSTO") =>
        new(new InstrumentId(id), InstrumentType.Equity, "Synthetic Example AB", Sek, new MarketVenue(mic, $"Synthetic {mic}"), mic,
            InstrumentLifecycle.Active, new DateOnly(2020, 1, 1));

    private static ProviderInstrumentMapping Mapping(string id, string symbol, string mic, DateOnly from, DateOnly? to) =>
        new(new InstrumentId(id), Provider, Dataset, symbol, new MarketVenue(mic, $"Synthetic {mic}"), mic, from, to, new EvidenceReference("fixture:mapping-v1"));

    private static InstrumentMappingCatalog Catalog() => new([Instrument()], [
        Mapping("BB-EQ-TEST-001", "TEST-A", "XSTO", new DateOnly(2020, 1, 1), new DateOnly(2024, 5, 31)),
        Mapping("BB-EQ-TEST-001", "TEST-B", "XSTO", new DateOnly(2024, 6, 1), null)]);

    private static SyntheticRawDailyBar Bar() => new(Provider, Dataset, "TEST-B", "XSTO", new DateOnly(2024, 6, 3),
        10m, 11m, 9m, 10m, 1000m, Sek, PriceAdjustment.Raw, null, Retrieved.AddHours(-1), Retrieved,
        new DatasetRevisionId("revision-001"), Policy(), new VersionReference("fixture-v1"), new VersionReference("schema-v1"));

    private static SyntheticRawCorporateAction Action(CorporateActionType type) => new(new CorporateActionId("action-001"), type,
        Provider, Dataset, "TEST-B", "XSTO", new DateOnly(2024, 6, 3), new DateOnly(2024, 6, 3), null, null,
        Retrieved.AddHours(-1), Retrieved, new DatasetRevisionId("revision-001"), Policy(), new VersionReference("fixture-v1"), new VersionReference("schema-v1"));

    private static MarketDataPolicyReference Policy() => new(new PolicyId("synthetic-policy"), new PolicyVersion("1"));
}
