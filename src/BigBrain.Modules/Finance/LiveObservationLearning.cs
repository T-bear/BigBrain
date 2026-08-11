using System.Collections.Immutable;

namespace BigBrain.Modules.Finance;

public enum LiveObservationGranularity { Unknown = 0, Snapshot, FiveMinutes, Daily }
public enum ObservationFreshness { Unknown = 0, RealTime, Delayed, EndOfDay }
public enum LiveFeedEventKind { Unknown = 0, Observation, MissingObservation, SessionBoundary, ProviderOutage }
public enum ShadowPredictionDirection { Unknown = 0, Up, Down, Flat }

public readonly record struct LiveStreamId
{
    public LiveStreamId(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
}

public readonly record struct LiveObservationId
{
    public LiveObservationId(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
}

public readonly record struct ShadowPredictionId
{
    public ShadowPredictionId(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
}

public sealed record LiveMarketObservation
{
    public LiveMarketObservation(
        LiveObservationId id, LiveStreamId streamId, long sequence, InstrumentId instrumentId,
        MarketDataProvider provider, ProviderDataset providerDataset, string providerReference,
        MarketVenue venue, string mic, DateOnly sessionDate, LiveObservationGranularity granularity,
        PriceAdjustment adjustment, Price open, Price high, Price low, Price close, decimal volume,
        DateTimeOffset eventTimeUtc, DateTimeOffset providerTimeUtc, DateTimeOffset receivedTimeUtc,
        DateTimeOffset knowledgeTimeUtc, ObservationFreshness freshness, TimeSpan declaredDelay,
        MarketSessionState sessionState, MarketDataQualityStatus quality, DatasetRevisionId datasetRevisionId,
        MarketDataPolicyReference policy, EvidenceReference provenance, AcquisitionRequestId ingestionJournalReference,
        LiveObservationId? correctsObservationId = null)
    {
        RequiredText.Require(id.Value, nameof(id)); RequiredText.Require(streamId.Value, nameof(streamId));
        ArgumentOutOfRangeException.ThrowIfNegative(sequence);
        RequiredText.Require(instrumentId.Value, nameof(instrumentId)); RequiredText.Require(provider.Value, nameof(provider));
        RequiredText.Require(providerDataset.Value, nameof(providerDataset));
        ProviderReference = RequiredText.Normalize(providerReference, nameof(providerReference)).ToUpperInvariant();
        ArgumentNullException.ThrowIfNull(venue); Mic = RequiredText.Normalize(mic, nameof(mic)).ToUpperInvariant();
        if (granularity == LiveObservationGranularity.Unknown || !Enum.IsDefined(granularity)) throw new ArgumentException("Granularity is required.", nameof(granularity));
        if (adjustment == PriceAdjustment.Unknown || !Enum.IsDefined(adjustment)) throw new ArgumentException("Adjustment is required.", nameof(adjustment));
        if (open.Currency != high.Currency || open.Currency != low.Currency || open.Currency != close.Currency ||
            high.Value < open.Value || high.Value < close.Value || high.Value < low.Value || low.Value > open.Value || low.Value > close.Value)
            throw new ArgumentException("Live OHLC range or currency is invalid.");
        ArgumentOutOfRangeException.ThrowIfNegative(volume);
        FinanceTime.RequireUtc(eventTimeUtc, nameof(eventTimeUtc)); FinanceTime.RequireUtc(providerTimeUtc, nameof(providerTimeUtc));
        FinanceTime.RequireUtc(receivedTimeUtc, nameof(receivedTimeUtc)); FinanceTime.RequireUtc(knowledgeTimeUtc, nameof(knowledgeTimeUtc));
        if (providerTimeUtc < eventTimeUtc || receivedTimeUtc < providerTimeUtc || knowledgeTimeUtc < receivedTimeUtc)
            throw new ArgumentException("Event, provider, received and knowledge time must be causally ordered.");
        if (freshness == ObservationFreshness.Unknown || !Enum.IsDefined(freshness)) throw new ArgumentException("Freshness is required.", nameof(freshness));
        ArgumentOutOfRangeException.ThrowIfLessThan(declaredDelay, TimeSpan.Zero);
        if (freshness == ObservationFreshness.RealTime && declaredDelay > TimeSpan.Zero)
            throw new ArgumentException("Delayed evidence cannot be labelled real-time.", nameof(freshness));
        if (freshness == ObservationFreshness.Delayed && declaredDelay == TimeSpan.Zero)
            throw new ArgumentException("Delayed evidence requires an explicit positive delay.", nameof(declaredDelay));
        if (sessionState == MarketSessionState.Unknown || !Enum.IsDefined(sessionState)) throw new ArgumentException("Session state must be explicit.", nameof(sessionState));
        if (quality == MarketDataQualityStatus.Unknown || !Enum.IsDefined(quality)) throw new ArgumentException("Quality is required.", nameof(quality));
        RequiredText.Require(datasetRevisionId.Value, nameof(datasetRevisionId)); policy.Validate();
        RequiredText.Require(provenance.Value, nameof(provenance)); RequiredText.Require(ingestionJournalReference.Value, nameof(ingestionJournalReference));
        if (correctsObservationId is { } correction && correction == id) throw new ArgumentException("An observation cannot correct itself.", nameof(correctsObservationId));

        Id = id; StreamId = streamId; Sequence = sequence; InstrumentId = instrumentId; Provider = provider;
        ProviderDataset = providerDataset; Venue = venue; SessionDate = sessionDate; Granularity = granularity;
        Adjustment = adjustment; Open = open; High = high; Low = low; Close = close; Volume = volume;
        EventTimeUtc = eventTimeUtc; ProviderTimeUtc = providerTimeUtc; ReceivedTimeUtc = receivedTimeUtc;
        KnowledgeTimeUtc = knowledgeTimeUtc; Freshness = freshness; DeclaredDelay = declaredDelay;
        SessionState = sessionState; Quality = quality; DatasetRevisionId = datasetRevisionId; Policy = policy;
        Provenance = provenance; IngestionJournalReference = ingestionJournalReference; CorrectsObservationId = correctsObservationId;
    }

    public LiveObservationId Id { get; }
    public LiveStreamId StreamId { get; }
    public long Sequence { get; }
    public InstrumentId InstrumentId { get; }
    public MarketDataProvider Provider { get; }
    public ProviderDataset ProviderDataset { get; }
    public string ProviderReference { get; }
    public MarketVenue Venue { get; }
    public string Mic { get; }
    public DateOnly SessionDate { get; }
    public LiveObservationGranularity Granularity { get; }
    public PriceAdjustment Adjustment { get; }
    public Price Open { get; }
    public Price High { get; }
    public Price Low { get; }
    public Price Close { get; }
    public decimal Volume { get; }
    public DateTimeOffset EventTimeUtc { get; }
    public DateTimeOffset ProviderTimeUtc { get; }
    public DateTimeOffset ReceivedTimeUtc { get; }
    public DateTimeOffset KnowledgeTimeUtc { get; }
    public ObservationFreshness Freshness { get; }
    public TimeSpan DeclaredDelay { get; }
    public MarketSessionState SessionState { get; }
    public MarketDataQualityStatus Quality { get; }
    public DatasetRevisionId DatasetRevisionId { get; }
    public MarketDataPolicyReference Policy { get; }
    public EvidenceReference Provenance { get; }
    public AcquisitionRequestId IngestionJournalReference { get; }
    public LiveObservationId? CorrectsObservationId { get; }
    public bool IsUsableAt(DateTimeOffset knowledgeBoundaryUtc) =>
        KnowledgeTimeUtc <= knowledgeBoundaryUtc && Quality is MarketDataQualityStatus.Valid or MarketDataQualityStatus.Corrected;
}

public sealed record SyntheticLiveFeedEvent(
    string EventId, LiveFeedEventKind Kind, DateTimeOffset DeliveryTimeUtc, long Sequence,
    LiveMarketObservation? Observation, InstrumentId InstrumentId, DateOnly SessionDate,
    MarketSessionState SessionState, MarketDataFindingCode? FindingCode, EvidenceReference Evidence);

public sealed class SyntheticLiveMarketFeed
{
    private readonly ImmutableArray<SyntheticLiveFeedEvent> _events;

    public SyntheticLiveMarketFeed(IEnumerable<SyntheticLiveFeedEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        _events = events.OrderBy(value => value.DeliveryTimeUtc).ThenBy(value => value.Sequence)
            .ThenBy(value => value.EventId, StringComparer.Ordinal).ToImmutableArray();
        if (_events.Select(value => value.EventId).Distinct(StringComparer.Ordinal).Count() != _events.Length)
            throw new ArgumentException("Synthetic feed event IDs must be unique.", nameof(events));
        foreach (var item in _events)
        {
            RequiredText.Require(item.EventId, nameof(events)); FinanceTime.RequireUtc(item.DeliveryTimeUtc, nameof(events));
            RequiredText.Require(item.Evidence.Value, nameof(events));
            if (item.Kind == LiveFeedEventKind.Observation && item.Observation is null) throw new ArgumentException("Observation events require an observation.", nameof(events));
            if (item.Kind != LiveFeedEventKind.Observation && item.Observation is not null) throw new ArgumentException("Non-observation events cannot contain prices.", nameof(events));
            if (item.Observation is { } observation && (observation.Provider.Value != SyntheticHistoricalDataAdapter.ProviderName ||
                !observation.ProviderDataset.Value.StartsWith("Synthetic-", StringComparison.Ordinal) || observation.KnowledgeTimeUtc != item.DeliveryTimeUtc))
                throw new ArgumentException("Synthetic feed observations require unmistakable fixture identity and delivery at knowledge time.", nameof(events));
        }
    }

    public IReadOnlyList<SyntheticLiveFeedEvent> ReadThrough(DateTimeOffset knowledgeBoundaryUtc)
    {
        FinanceTime.RequireUtc(knowledgeBoundaryUtc, nameof(knowledgeBoundaryUtc));
        return _events.Where(value => value.DeliveryTimeUtc <= knowledgeBoundaryUtc).ToImmutableArray();
    }
}

public sealed record LiveObservationEntitlementResult(bool IsAllowed, string ReasonCode, MarketDataPolicyReference? Policy);

public static class LiveObservationEntitlementGate
{
    private static readonly MarketDataUse[] RequiredUses =
        [MarketDataUse.HistoricalAnalysis, MarketDataUse.WalkForward, MarketDataUse.StrategyTraining, MarketDataUse.DerivedMetrics, MarketDataUse.LongTermStorage];

    public static LiveObservationEntitlementResult Evaluate(LiveMarketObservation observation, MarketDataEntitlementPolicy? policy)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (policy is not null && policy.Reference != observation.Policy)
            return new(false, MarketDataEntitlementReasons.PolicyScopeMismatch, policy.Reference);
        var context = new MarketDataEntitlementContext(observation.KnowledgeTimeUtc, true, true,
            MarketDataClassification.Raw, observation.Provider, observation.ProviderDataset);
        foreach (var use in RequiredUses)
        {
            var result = MarketDataEntitlementEvaluator.Evaluate(policy, use, context);
            if (!result.IsAllowed) return new(false, result.ReasonCode, result.Policy);
        }
        return new(true, MarketDataEntitlementReasons.Allowed, policy!.Reference);
    }
}

public sealed record ShadowStrategyVersion(
    string StrategyId, string StrategyVersion, DatasetChecksum ConfigurationFingerprint,
    VersionReference FeatureSetVersion, VersionReference RiskPolicyVersion, VersionReference BuildReference)
{
    public string Key => $"{StrategyId}@{StrategyVersion}|{ConfigurationFingerprint.Value}|{FeatureSetVersion.Value}|{RiskPolicyVersion.Value}|{BuildReference.Value}";
    public ShadowStrategyVersion Validate()
    {
        RequiredText.Require(StrategyId, nameof(StrategyId)); RequiredText.Require(StrategyVersion, nameof(StrategyVersion));
        RequiredText.Require(ConfigurationFingerprint.Value, nameof(ConfigurationFingerprint)); RequiredText.Require(FeatureSetVersion.Value, nameof(FeatureSetVersion));
        RequiredText.Require(RiskPolicyVersion.Value, nameof(RiskPolicyVersion)); RequiredText.Require(BuildReference.Value, nameof(BuildReference)); return this;
    }
}

public sealed record ShadowPrediction(
    ShadowPredictionId Id, InstrumentId InstrumentId, DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset MarketEventTimeUtc, DateTimeOffset KnowledgeBoundaryUtc, ShadowStrategyVersion Strategy,
    DatasetRevisionId DatasetRevisionId, LiveStreamId StreamId, MarketDataPolicyReference Policy,
    ShadowPredictionDirection Direction, decimal Score, TimeSpan Horizon, Price HypotheticalEntry,
    Price? HypotheticalStop, Price? HypotheticalTarget, string MarketRegime,
    ImmutableArray<LiveObservationId> Evidence, ImmutableArray<string> ReasonCodes);

public sealed record ShadowOutcome(
    ShadowPredictionId PredictionId, DateTimeOffset ObservedAtUtc, LiveObservationId ClosingObservationId,
    decimal ReturnPercent, decimal MaximumFavorableExcursionPercent, decimal MaximumAdverseExcursionPercent,
    decimal VolatilityPercent, bool HypotheticalStopTriggered, bool HypotheticalTargetTriggered,
    decimal HypotheticalCostPercent, decimal NetReturnPercent, string CalibrationResult, string ResultCode);

public sealed class InMemoryShadowLearningJournal
{
    private readonly Dictionary<ShadowPredictionId, ShadowPrediction> _predictions = [];
    private readonly Dictionary<ShadowPredictionId, ShadowOutcome> _outcomes = [];
    public IReadOnlyList<ShadowPrediction> Predictions => _predictions.Values.OrderBy(value => value.EvaluatedAtUtc).ThenBy(value => value.Id.Value, StringComparer.Ordinal).ToImmutableArray();
    public IReadOnlyList<ShadowOutcome> Outcomes => _outcomes.Values.OrderBy(value => value.ObservedAtUtc).ThenBy(value => value.PredictionId.Value, StringComparer.Ordinal).ToImmutableArray();
    public void AppendPrediction(ShadowPrediction prediction)
    {
        ArgumentNullException.ThrowIfNull(prediction);
        if (!_predictions.TryAdd(prediction.Id, prediction) && _predictions[prediction.Id] != prediction)
            throw new InvalidOperationException("A shadow prediction is immutable.");
    }
    public void AppendOutcome(ShadowOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        if (!_predictions.TryGetValue(outcome.PredictionId, out var prediction)) throw new InvalidOperationException("Outcome requires an existing prediction.");
        if (outcome.ObservedAtUtc < prediction.EvaluatedAtUtc + prediction.Horizon) throw new InvalidOperationException("Outcome cannot be attached before the prediction horizon.");
        if (!_outcomes.TryAdd(outcome.PredictionId, outcome) && _outcomes[outcome.PredictionId] != outcome)
            throw new InvalidOperationException("A shadow outcome is append-only and immutable.");
    }
}

public sealed class SyntheticShadowLearningPipeline
{
    public const string NonProductionStrategyId = "TEST-SYNTHETIC-MOMENTUM-NON-TRADING";

    public static ShadowPrediction Evaluate(InstrumentId instrumentId, DateTimeOffset evaluatedAtUtc, TimeSpan horizon,
        ShadowStrategyVersion strategy, IEnumerable<LiveMarketObservation> observations)
    {
        FinanceTime.RequireUtc(evaluatedAtUtc, nameof(evaluatedAtUtc)); strategy.Validate();
        if (strategy.StrategyId != NonProductionStrategyId) throw new InvalidOperationException("Only the explicit non-production fixture strategy is supported.");
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(horizon, TimeSpan.Zero); ArgumentNullException.ThrowIfNull(observations);
        var known = observations.Where(value => value.InstrumentId == instrumentId && value.IsUsableAt(evaluatedAtUtc))
            .OrderBy(value => value.EventTimeUtc).ThenBy(value => value.Sequence).ThenBy(value => value.Id.Value, StringComparer.Ordinal).ToArray();
        if (known.Length < 2) throw new InvalidOperationException("Two known observations are required for the fixture momentum condition.");
        var previous = known[^2]; var current = known[^1];
        if (current.DatasetRevisionId != previous.DatasetRevisionId || current.Policy != previous.Policy || current.StreamId != previous.StreamId)
            throw new InvalidOperationException("Shadow evidence cannot silently mix revision, policy or stream identity.");
        var direction = current.Close.Value > previous.Close.Value ? ShadowPredictionDirection.Up :
            current.Close.Value < previous.Close.Value ? ShadowPredictionDirection.Down : ShadowPredictionDirection.Flat;
        var score = (current.Close.Value - previous.Close.Value) / previous.Close.Value;
        var id = new ShadowPredictionId($"shadow:{strategy.StrategyId}:{strategy.StrategyVersion}:{instrumentId.Value}:{evaluatedAtUtc:O}");
        return new ShadowPrediction(id, instrumentId, evaluatedAtUtc, current.EventTimeUtc, evaluatedAtUtc, strategy,
            current.DatasetRevisionId, current.StreamId, current.Policy, direction, score, horizon, current.Close,
            direction == ShadowPredictionDirection.Flat ? null : new Price(current.Close.Value * (direction == ShadowPredictionDirection.Up ? 0.99m : 1.01m), current.Close.Currency),
            direction == ShadowPredictionDirection.Flat ? null : new Price(current.Close.Value * (direction == ShadowPredictionDirection.Up ? 1.01m : 0.99m), current.Close.Currency),
            "fixture:unclassified", [previous.Id, current.Id], ["fixture.momentum.twoObservation"]);
    }

    public static ShadowOutcome ObserveOutcome(ShadowPrediction prediction, DateTimeOffset observedAtUtc,
        IEnumerable<LiveMarketObservation> observations, decimal hypotheticalCostBasisPoints)
    {
        ArgumentNullException.ThrowIfNull(prediction); FinanceTime.RequireUtc(observedAtUtc, nameof(observedAtUtc));
        ArgumentOutOfRangeException.ThrowIfNegative(hypotheticalCostBasisPoints);
        var eligible = observations.Where(value => value.InstrumentId == prediction.InstrumentId && value.EventTimeUtc > prediction.MarketEventTimeUtc &&
            value.KnowledgeTimeUtc <= observedAtUtc && value.DatasetRevisionId == prediction.DatasetRevisionId && value.Policy == prediction.Policy &&
            value.StreamId == prediction.StreamId && value.IsUsableAt(observedAtUtc)).OrderBy(value => value.EventTimeUtc).ThenBy(value => value.Sequence).ToArray();
        if (observedAtUtc < prediction.EvaluatedAtUtc + prediction.Horizon || eligible.Length == 0)
            throw new InvalidOperationException("No known outcome exists at the completed horizon.");
        var sign = prediction.Direction == ShadowPredictionDirection.Down ? -1m : prediction.Direction == ShadowPredictionDirection.Up ? 1m : 0m;
        decimal Change(Price price) => sign * (price.Value - prediction.HypotheticalEntry.Value) / prediction.HypotheticalEntry.Value * 100m;
        var returns = eligible.Select(value => Change(value.Close)).ToArray();
        var result = returns[^1]; var favorable = returns.Max(); var adverse = returns.Min(); var mean = returns.Average();
        var variance = returns.Select(value => (value - mean) * (value - mean)).Average(); var volatility = DecimalSqrt(variance);
        var cost = hypotheticalCostBasisPoints / 100m; var net = result - cost;
        var stopTriggered = prediction.HypotheticalStop is not null && adverse <= -1m;
        var targetTriggered = prediction.HypotheticalTarget is not null && favorable >= 1m;
        var calibrated = (prediction.Direction != ShadowPredictionDirection.Flat && result > 0) ||
            (prediction.Direction == ShadowPredictionDirection.Flat && result == 0);
        return new ShadowOutcome(prediction.Id, observedAtUtc, eligible[^1].Id, result, favorable, adverse,
            volatility, stopTriggered, targetTriggered, cost, net, calibrated ? "shadow.calibration.aligned" : "shadow.calibration.missed",
            net > 0 ? "shadow.outcome.positive" : net < 0 ? "shadow.outcome.negative" : "shadow.outcome.flat");
    }

    private static decimal DecimalSqrt(decimal value)
    {
        if (value == 0) return 0; var estimate = value > 1 ? value : 1;
        for (var index = 0; index < 24; index++) estimate = (estimate + value / estimate) / 2;
        return estimate;
    }
}

public sealed record ProspectiveStrategyMetrics(
    string StrategyVersionKey, int SignalCount, int Wins, int Losses, decimal WinRatePercent,
    decimal AverageNetReturnPercent, decimal MedianNetReturnPercent, decimal ExpectedValuePercent,
    decimal AverageMaximumFavorableExcursionPercent, decimal AverageMaximumAdverseExcursionPercent,
    decimal OutcomeVolatilityPercent);

public static class ProspectiveMetricsCalculator
{
    public static ProspectiveStrategyMetrics Calculate(ShadowStrategyVersion strategy,
        IEnumerable<ShadowPrediction> predictions, IEnumerable<ShadowOutcome> outcomes)
    {
        strategy.Validate(); ArgumentNullException.ThrowIfNull(predictions); ArgumentNullException.ThrowIfNull(outcomes);
        var selected = predictions.Where(value => value.Strategy.Key == strategy.Key).ToDictionary(value => value.Id);
        var values = outcomes.Where(value => selected.ContainsKey(value.PredictionId)).OrderBy(value => value.PredictionId.Value, StringComparer.Ordinal).ToArray();
        if (values.Length == 0) return new(strategy.Key, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        var ordered = values.Select(value => value.NetReturnPercent).Order().ToArray();
        var median = ordered.Length % 2 == 1 ? ordered[ordered.Length / 2] : (ordered[ordered.Length / 2 - 1] + ordered[ordered.Length / 2]) / 2;
        var mean = ordered.Average(); var variance = ordered.Select(value => (value - mean) * (value - mean)).Average();
        var wins = ordered.Count(value => value > 0); var losses = ordered.Count(value => value < 0);
        return new(strategy.Key, values.Length, wins, losses, (decimal)wins / values.Length * 100m, mean, median, mean,
            values.Average(value => value.MaximumFavorableExcursionPercent), values.Average(value => value.MaximumAdverseExcursionPercent), DecimalSqrt(variance));
    }

    private static decimal DecimalSqrt(decimal value)
    {
        if (value == 0) return 0; var estimate = value > 1 ? value : 1;
        for (var index = 0; index < 24; index++) estimate = (estimate + value / estimate) / 2;
        return estimate;
    }
}
