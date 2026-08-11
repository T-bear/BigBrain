using BigBrain.Modules;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceFoundationTests
{
    private static readonly Currency Sek = new("sek");
    private static readonly InstrumentId InstrumentId = new("fixture-1");
    private static readonly DateTimeOffset MarketTime = new(2026, 8, 10, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MoneyUsesDecimalPrecisionAndBankersRounding()
    {
        var total = new Money(0.1m, Sek) + new Money(0.2m, Sek);

        Assert.Equal(0.3m, total.Amount);
        Assert.Equal(1.00m, new Money(1.005m, Sek).Round().Amount);
    }

    [Fact]
    public void MoneyRejectsCrossCurrencyArithmetic()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _ = new Money(1m, Sek) + new Money(1m, new Currency("EUR")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PriceAndQuantityMustBePositive(decimal value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Price(value, Sek));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(value));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void PercentageMustBeBounded(decimal value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Percentage(value));
    }

    [Fact]
    public void MarketObservationRequiresUtcAndCausalObservationTime()
    {
        var observation = CreateObservation() with
        {
            ObservedAtUtc = new DateTimeOffset(2026, 8, 10, 7, 59, 0, TimeSpan.Zero)
        };

        Assert.Throws<ArgumentException>(() => observation.Validate());
        Assert.Throws<ArgumentException>(() =>
            FinanceTime.RequireUtc(new DateTimeOffset(2026, 8, 10, 8, 0, 0, TimeSpan.FromHours(2)), "timestamp"));
    }

    [Fact]
    public void CandleRejectsInconsistentOhlcValues()
    {
        var candle = new Candle(
            MarketTime,
            Timeframe.OneMinute,
            new Price(100m, Sek),
            new Price(99m, Sek),
            new Price(98m, Sek),
            new Price(100m, Sek),
            1m);

        Assert.Throws<ArgumentException>(() => candle.Validate());
    }

    [Fact]
    public async Task MarketDataContractIsProviderNeutralAndDeterministic()
    {
        IMarketDataSource source = new FixtureMarketDataSource(CreateObservation());

        var first = await source.GetObservationAsync(InstrumentId, TestContext.Current.CancellationToken);
        var second = await source.GetObservationAsync(InstrumentId, TestContext.Current.CancellationToken);

        Assert.Equal(first, second);
        Assert.Equal("fixture-v1", first.DataVersion);
    }

    [Fact]
    public void StrategyContractProducesEvaluationButCannotProduceOrder()
    {
        IFinanceStrategy strategy = new NoTradeFixtureStrategy();
        var observation = CreateObservation();
        var evaluation = strategy.Evaluate(new StrategyContext(
            NewId(), NewId(), observation, new Dictionary<string, decimal>(), observation.ObservedAtUtc));

        Assert.Equal(SignalDirection.None, evaluation.Signal.Direction);
        Assert.DoesNotContain(typeof(IFinanceStrategy).GetMethods(), method => method.ReturnType == typeof(PaperOrder));
    }

    [Fact]
    public void TradingModeDefaultsToResearch()
    {
        Assert.Equal(TradingMode.Research, default);
    }

    [Fact]
    public void MissingRiskInformationFailsClosedAndIsJournaledAsRejected()
    {
        var journal = new InMemoryDecisionJournal();
        var entry = new ReferenceDecisionPipeline(journal).Evaluate(
            CreateEvaluation(SignalDirection.Buy),
            new RiskEvaluation(EvaluationResult.Missing, "risk-v1", ["risk.missing"]),
            new PolicyEvaluation(EvaluationResult.Accepted, "policy-v1", []),
            TradingMode.Paper,
            MarketTime.AddSeconds(3));

        Assert.Equal(DecisionAction.Rejected, entry.Decision.Action);
        Assert.Contains("risk.missing", entry.Decision.ReasonCodes);
        Assert.Single(journal.GetEntries());
    }

    [Fact]
    public void PolicyRejectionFailsClosedAndIsJournaled()
    {
        var entry = Evaluate(
            CreateEvaluation(SignalDirection.Buy),
            new RiskEvaluation(EvaluationResult.Accepted, "risk-v1", []),
            new PolicyEvaluation(EvaluationResult.Rejected, "policy-v1", ["policy.blocked"]),
            TradingMode.Paper);

        Assert.Equal(DecisionAction.Rejected, entry.Decision.Action);
        Assert.Contains("policy.blocked", entry.Decision.ReasonCodes);
    }

    [Fact]
    public void NoTradeSignalIsAlwaysJournaledAsNoTrade()
    {
        var entry = Evaluate(
            CreateEvaluation(SignalDirection.None),
            new RiskEvaluation(EvaluationResult.Missing, "risk-v1", ["not.evaluated"]),
            new PolicyEvaluation(EvaluationResult.Missing, "policy-v1", ["not.evaluated"]),
            TradingMode.Research);

        Assert.Equal(DecisionAction.NoTrade, entry.Decision.Action);
        Assert.Null(entry.PaperOrderId);
        Assert.Null(entry.OutcomeId);
    }

    [Theory]
    [InlineData(TradingMode.Research)]
    [InlineData(TradingMode.Backtest)]
    [InlineData(TradingMode.ManualApproval)]
    [InlineData(TradingMode.LimitedAuto)]
    [InlineData(TradingMode.Auto)]
    [InlineData(TradingMode.Halted)]
    public void NonPaperModesCannotCreatePaperIntent(TradingMode mode)
    {
        var entry = Evaluate(
            CreateEvaluation(SignalDirection.Buy),
            new RiskEvaluation(EvaluationResult.Accepted, "risk-v1", []),
            new PolicyEvaluation(EvaluationResult.Accepted, "policy-v1", []),
            mode);

        Assert.Equal(DecisionAction.NoTrade, entry.Decision.Action);
        Assert.False(entry.Decision.CreatesPaperIntent);
    }

    [Fact]
    public void PaperCandidateRequiresBothRiskAndPolicyAcceptance()
    {
        var entry = Evaluate(
            CreateEvaluation(SignalDirection.Buy),
            new RiskEvaluation(EvaluationResult.Accepted, "risk-v1", []),
            new PolicyEvaluation(EvaluationResult.Accepted, "policy-v1", []),
            TradingMode.Paper);

        Assert.Equal(DecisionAction.PaperBuy, entry.Decision.Action);
        Assert.True(entry.Decision.CreatesPaperIntent);
        Assert.Null(entry.PaperOrderId);
    }

    [Fact]
    public void JournalPreservesCorrelationChain()
    {
        var evaluation = CreateEvaluation(SignalDirection.Sell);
        var entry = Evaluate(
            evaluation,
            new RiskEvaluation(EvaluationResult.Accepted, "risk-v1", []),
            new PolicyEvaluation(EvaluationResult.Accepted, "policy-v1", []),
            TradingMode.Paper);

        Assert.Equal(evaluation.CorrelationId, entry.CorrelationId);
        Assert.Equal(evaluation.EvaluationId, entry.EvaluationId);
        Assert.Equal(evaluation.ObservationId, entry.ObservationId);
        Assert.Equal(entry.CorrelationId, entry.Decision.CorrelationId);
    }

    [Fact]
    public void FinanceModuleIsReadOnlyResearchSurface()
    {
        Assert.Equal("Research", FinanceModule.Definition.Status);
        Assert.Equal(["finance.research.read"], FinanceModule.Definition.Capabilities);
        var widget = Assert.Single(FinanceModule.Definition.DashboardWidgets);
        Assert.Equal("finance-observation", widget.Id);
        Assert.Equal("/api/v1/modules/finance/observation", widget.DataEndpoint);
    }

    private static DecisionJournalEntry Evaluate(
        StrategyEvaluation evaluation,
        RiskEvaluation risk,
        PolicyEvaluation policy,
        TradingMode mode) =>
        new ReferenceDecisionPipeline(new InMemoryDecisionJournal()).Evaluate(
            evaluation, risk, policy, mode, MarketTime.AddSeconds(3));

    private static StrategyEvaluation CreateEvaluation(SignalDirection direction)
    {
        var evaluationId = NewId();
        return new StrategyEvaluation(
            evaluationId,
            NewId(),
            new StrategyIdentity("fixture.strategy", "1.0.0"),
            CreateObservation().ObservationId,
            InstrumentId,
            MarketTime.AddSeconds(2),
            new StrategySignal(
                direction,
                direction == SignalDirection.None ? 0m : 0.5m,
                direction == SignalDirection.None ? null : new Price(100m, Sek),
                null,
                null,
                direction == SignalDirection.None ? ["fixture.no-trade"] : ["fixture.candidate"]));
    }

    private static MarketDataObservation CreateObservation()
    {
        var candle = new Candle(
            MarketTime,
            Timeframe.OneMinute,
            new Price(100m, Sek),
            new Price(101m, Sek),
            new Price(99m, Sek),
            new Price(100.5m, Sek),
            10m).Validate();
        return new MarketDataObservation(
            NewId(),
            new Instrument(InstrumentId, "Synthetic fixture", new MarketVenue("test", "Fixture venue"), Sek),
            MarketTime,
            MarketTime.AddSeconds(1),
            "fixture-v1",
            [candle],
            new Quote(new Price(100m, Sek), new Price(100.1m, Sek))).Validate();
    }

    private static FinanceId NewId() => new(Guid.NewGuid());

    private sealed class FixtureMarketDataSource(MarketDataObservation observation) : IMarketDataSource
    {
        public Task<MarketDataObservation> GetObservationAsync(
            InstrumentId instrumentId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal(observation.Instrument.Id, instrumentId);
            return Task.FromResult(observation);
        }
    }

    private sealed class NoTradeFixtureStrategy : IFinanceStrategy
    {
        public StrategyIdentity Identity { get; } = new("fixture.no-trade", "1.0.0");

        public StrategyEvaluation Evaluate(StrategyContext context) => new(
            context.EvaluationId,
            context.CorrelationId,
            Identity,
            context.Observation.ObservationId,
            context.Observation.Instrument.Id,
            context.EvaluatedAtUtc,
            new StrategySignal(SignalDirection.None, 0m, null, null, null, ["fixture.no-trade"]));
    }
}
