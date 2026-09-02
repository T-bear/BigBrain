using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceAlpacaActivationReadinessTests
{
    private static readonly DateTimeOffset ReviewedAt = new(2026, 9, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UnresolvedEntitlementBlocksBeforeProviderRequest()
    {
        var requests = 0;
        var policy = AlpacaBasicIexReadiness.EntitlementPolicy(ReviewedAt);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AlpacaLiveActivationGate.ExecuteIfAllowedAsync(policy, _ => { requests++; return Task.CompletedTask; },
                TestContext.Current.CancellationToken));

        Assert.Equal(0, requests);
        Assert.Contains(AlpacaBasicIexReadiness.BlockReason, error.Message, StringComparison.Ordinal);
        var status = AlpacaLiveActivationGate.Evaluate(policy);
        Assert.Equal(LiveProviderAcquisitionDecision.Blocked, status.Acquisition);
        Assert.Equal(LiveProviderActivationState.NotActivated, status.Activation);
        Assert.Equal(EntitlementEvidenceClass.HumanConfirmationRequired, status.Entitlement);
        Assert.False(status.CredentialsConfigured);
        Assert.DoesNotContain("key", status.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", status.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BasicCapabilityIsExplicitlyIexAndNeverConsolidated()
    {
        var status = AlpacaBasicIexReadiness.Status();
        Assert.Equal("IEX", status.Feed);
        Assert.Equal(LiveMarketCoverage.IexSingleExchange, status.MarketCoverage);
        Assert.Equal(ObservationFreshness.RealTime, status.Freshness);
        Assert.Equal(30, status.MaximumWebSocketSymbols);
        Assert.Throws<ArgumentException>(() => AlpacaBasicIexReadiness.CreateStreamId(
            new InstrumentId("US:XNAS:AAPL"), LiveObservationType.Trade,
            LiveObservationGranularity.Snapshot, LiveMarketCoverage.ConsolidatedUs));
    }

    [Fact]
    public void StreamIdentitySeparatesObservationTypeGranularityInstrumentAndPolicy()
    {
        var aaplTrade = AlpacaBasicIexReadiness.CreateStreamId(new InstrumentId("US:XNAS:AAPL"),
            LiveObservationType.Trade, LiveObservationGranularity.Snapshot, LiveMarketCoverage.IexSingleExchange);
        var aaplBar = AlpacaBasicIexReadiness.CreateStreamId(new InstrumentId("US:XNAS:AAPL"),
            LiveObservationType.Bar, LiveObservationGranularity.FiveMinutes, LiveMarketCoverage.IexSingleExchange);
        var spyTrade = AlpacaBasicIexReadiness.CreateStreamId(new InstrumentId("US:ARCX:SPY"),
            LiveObservationType.Trade, LiveObservationGranularity.Snapshot, LiveMarketCoverage.IexSingleExchange);

        Assert.NotEqual(aaplTrade, aaplBar);
        Assert.NotEqual(aaplTrade, spyTrade);
        Assert.Contains("alpaca:basic:iex", aaplTrade.Value, StringComparison.Ordinal);
        Assert.Contains("bb-128a-v1", aaplTrade.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingTemporalContractsRemainFailClosed()
    {
        Assert.Throws<ArgumentException>(() => Observation(
            ObservationFreshness.RealTime, TimeSpan.FromMinutes(15), ReviewedAt, ReviewedAt, ReviewedAt, ReviewedAt));
        Assert.Throws<ArgumentException>(() => Observation(
            ObservationFreshness.RealTime, TimeSpan.Zero, ReviewedAt.AddSeconds(1), ReviewedAt, ReviewedAt, ReviewedAt));
    }

    private static LiveMarketObservation Observation(
        ObservationFreshness freshness, TimeSpan delay, DateTimeOffset eventTime,
        DateTimeOffset providerTime, DateTimeOffset receivedTime, DateTimeOffset knowledgeTime) => new(
            new LiveObservationId("alpaca-fixture-observation"),
            AlpacaBasicIexReadiness.CreateStreamId(new InstrumentId("US:XNAS:AAPL"), LiveObservationType.Bar,
                LiveObservationGranularity.FiveMinutes, LiveMarketCoverage.IexSingleExchange),
            1, new InstrumentId("US:XNAS:AAPL"), new MarketDataProvider("Alpaca"),
            new ProviderDataset("Basic-IEX-LIVE"), "AAPL", new MarketVenue("IEX", "Investors Exchange"), "IEXG",
            new DateOnly(2026, 9, 2), LiveObservationGranularity.FiveMinutes, PriceAdjustment.Raw,
            new Price(100, new Currency("USD")), new Price(101, new Currency("USD")),
            new Price(99, new Currency("USD")), new Price(100, new Currency("USD")), 10,
            eventTime, providerTime, receivedTime, knowledgeTime, freshness, delay, MarketSessionState.Trading,
            MarketDataQualityStatus.Valid, new DatasetRevisionId("fixture-revision"),
            AlpacaBasicIexReadiness.PolicyReference, new EvidenceReference("fixture-evidence"),
            new AcquisitionRequestId("fixture-journal"));
}
