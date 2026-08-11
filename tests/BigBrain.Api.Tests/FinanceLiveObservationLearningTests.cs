using System.Collections.Immutable;
using System.Text.Json;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceLiveObservationLearningTests
{
    private static readonly string[] SensitivePropertyTerms = ["secret", "token", "credential", "header"];
    private static readonly Currency Sek = new("SEK");
    private static readonly InstrumentId Instrument = new("BB-EQ-LIVE-TEST-001");
    private static readonly MarketDataProvider Provider = new(SyntheticHistoricalDataAdapter.ProviderName);
    private static readonly ProviderDataset Product = new("Synthetic-Live-Observation");
    private static readonly MarketVenue Venue = new("XSTO", "Synthetic Stockholm");
    private static readonly MarketDataPolicyReference PolicyReference = new(new PolicyId("synthetic-live"), new PolicyVersion("1"));
    private static readonly DatasetRevisionId Revision = new("live-revision-001");
    private static readonly LiveStreamId Stream = new("synthetic-live-stream-001");
    private static readonly DateOnly SessionDate = new(2024, 6, 3);
    private static readonly DateTimeOffset Nine = Utc(9, 0);

    [Fact]
    public void ObservationSeparatesEventProviderReceivedAndKnowledgeTime()
    {
        var value = Observation("one", 1, 100m, Nine, TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(4));
        Assert.True(value.EventTimeUtc < value.ProviderTimeUtc);
        Assert.True(value.ProviderTimeUtc < value.ReceivedTimeUtc);
        Assert.True(value.ReceivedTimeUtc < value.KnowledgeTimeUtc);
        Assert.Equal(ObservationFreshness.Delayed, value.Freshness);
        Assert.False(value.IsUsableAt(value.KnowledgeTimeUtc.AddTicks(-1)));
        Assert.True(value.IsUsableAt(value.KnowledgeTimeUtc));
    }

    [Fact]
    public void DelayedObservationCannotBeLabelledRealtime() =>
        Assert.Throws<ArgumentException>(() => Observation("bad", 1, 100m, Nine, TimeSpan.FromMinutes(1), freshness: ObservationFreshness.RealTime));

    [Fact]
    public void SyntheticFeedIsDeterministicAndDeliveryOrdered()
    {
        var lateEvent = Observation("late-event", 2, 101m, Nine.AddMinutes(5), TimeSpan.FromMinutes(10));
        var earlyEventDeliveredLater = Observation("early-event", 1, 99m, Nine, TimeSpan.FromMinutes(20));
        var events = new[] { Event(lateEvent), Event(earlyEventDeliveredLater) };
        var first = new SyntheticLiveMarketFeed(events).ReadThrough(Nine.AddMinutes(30));
        var second = new SyntheticLiveMarketFeed(events.Reverse()).ReadThrough(Nine.AddMinutes(30));
        Assert.Equal(first.Select(value => value.EventId), second.Select(value => value.EventId));
        Assert.Equal(["late-event", "early-event"], first.Select(value => value.EventId));
        Assert.True(first[1].Observation!.EventTimeUtc < first[0].Observation!.EventTimeUtc);
    }

    [Fact]
    public void FeedRepresentsDuplicateAndCorrectionWithoutOverwrite()
    {
        var original = Observation("original", 1, 100m, Nine, TimeSpan.Zero);
        var duplicate = Observation("duplicate", 2, 100m, Nine, TimeSpan.FromSeconds(1));
        var correction = Observation("correction", 3, 101m, Nine, TimeSpan.FromMinutes(1),
            quality: MarketDataQualityStatus.Corrected, corrects: original.Id);
        var output = new SyntheticLiveMarketFeed([Event(original), Event(duplicate), Event(correction)]).ReadThrough(Nine.AddMinutes(2));
        Assert.Equal(3, output.Count);
        Assert.Equal(original.Id, output[2].Observation!.CorrectsObservationId);
        Assert.Equal(100m, output[0].Observation!.Close.Value);
    }

    [Fact]
    public void FeedRepresentsSessionBoundaryMissingObservationAndOutage()
    {
        var items = new[]
        {
            NonPriceEvent("open", LiveFeedEventKind.SessionBoundary, Nine, MarketSessionState.Trading, null),
            NonPriceEvent("missing", LiveFeedEventKind.MissingObservation, Nine.AddMinutes(5), MarketSessionState.Trading, MarketDataFindingCode.MissingObservation),
            NonPriceEvent("outage", LiveFeedEventKind.ProviderOutage, Nine.AddMinutes(10), MarketSessionState.Trading, MarketDataFindingCode.ProviderGap),
            NonPriceEvent("close", LiveFeedEventKind.SessionBoundary, Nine.AddHours(8), MarketSessionState.Closed, null)
        };
        var output = new SyntheticLiveMarketFeed(items).ReadThrough(Nine.AddHours(8));
        Assert.Equal([LiveFeedEventKind.SessionBoundary, LiveFeedEventKind.MissingObservation, LiveFeedEventKind.ProviderOutage, LiveFeedEventKind.SessionBoundary], output.Select(value => value.Kind));
        Assert.All(output, value => Assert.Null(value.Observation));
    }

    [Fact]
    public void ShadowEvaluationCannotSeeFutureKnowledge()
    {
        var first = Observation("first", 1, 100m, Nine, TimeSpan.Zero);
        var future = Observation("future", 2, 110m, Nine.AddMinutes(5), TimeSpan.FromHours(1));
        Assert.Throws<InvalidOperationException>(() => SyntheticShadowLearningPipeline.Evaluate(Instrument, Nine.AddMinutes(10), TimeSpan.FromMinutes(5), Strategy("1"), [first, future]));
    }

    [Fact]
    public void ShadowPredictionIsVersionedAndContainsOnlyKnownEvidence()
    {
        var first = Observation("first", 1, 100m, Nine, TimeSpan.Zero);
        var second = Observation("second", 2, 101m, Nine.AddMinutes(5), TimeSpan.Zero);
        var prediction = SyntheticShadowLearningPipeline.Evaluate(Instrument, Nine.AddMinutes(5), TimeSpan.FromMinutes(5), Strategy("1"), [second, first]);
        Assert.Equal(ShadowPredictionDirection.Up, prediction.Direction);
        Assert.Equal([first.Id, second.Id], prediction.Evidence);
        Assert.Equal(SyntheticShadowLearningPipeline.NonProductionStrategyId, prediction.Strategy.StrategyId);
        Assert.Equal(Revision, prediction.DatasetRevisionId);
    }

    [Fact]
    public void PredictionJournalRejectsMutation()
    {
        var prediction = Prediction(); var journal = new InMemoryShadowLearningJournal(); journal.AppendPrediction(prediction);
        journal.AppendPrediction(prediction);
        Assert.Throws<InvalidOperationException>(() => journal.AppendPrediction(prediction with { Score = 0.9m }));
        Assert.Single(journal.Predictions);
    }

    [Fact]
    public void OutcomeIsAppendedAfterHorizonWithoutChangingPrediction()
    {
        var prediction = Prediction(); var future = Observation("outcome", 3, 103m, Nine.AddMinutes(10), TimeSpan.Zero);
        var outcome = SyntheticShadowLearningPipeline.ObserveOutcome(prediction, Nine.AddMinutes(10), [future], 10m);
        var journal = new InMemoryShadowLearningJournal(); journal.AppendPrediction(prediction); journal.AppendOutcome(outcome);
        Assert.Equal((103m - 101m) / 101m * 100m - 0.1m, outcome.NetReturnPercent);
        Assert.True(outcome.HypotheticalTargetTriggered);
        Assert.Equal("shadow.calibration.aligned", outcome.CalibrationResult);
        Assert.Single(journal.Predictions); Assert.Single(journal.Outcomes);
        Assert.Equal(101m, journal.Predictions[0].HypotheticalEntry.Value);
    }

    [Fact]
    public void OutcomeBeforeHorizonOrWithFutureKnowledgeFails()
    {
        var prediction = Prediction();
        var futureKnowledge = Observation("future", 3, 103m, Nine.AddMinutes(10), TimeSpan.FromHours(1));
        Assert.Throws<InvalidOperationException>(() => SyntheticShadowLearningPipeline.ObserveOutcome(prediction, Nine.AddMinutes(9), [futureKnowledge], 0));
        Assert.Throws<InvalidOperationException>(() => SyntheticShadowLearningPipeline.ObserveOutcome(prediction, Nine.AddMinutes(10), [futureKnowledge], 0));
    }

    [Fact]
    public void StrategyVersionsNeverMixMetrics()
    {
        var one = Prediction(); var two = one with { Id = new ShadowPredictionId("shadow:v2"), Strategy = Strategy("2") };
        var outcomes = new[] { Outcome(one.Id, 2m), Outcome(two.Id, -5m) };
        var metrics = ProspectiveMetricsCalculator.Calculate(Strategy("1"), [one, two], outcomes);
        Assert.Equal(1, metrics.SignalCount); Assert.Equal(1, metrics.Wins); Assert.Equal(2m, metrics.ExpectedValuePercent);
    }

    [Fact]
    public void MetricsIncludeTailLossAndDoNotOptimizeWinRateAlone()
    {
        var strategy = Strategy("1");
        var predictions = Enumerable.Range(1, 4).Select(index => Prediction() with { Id = new ShadowPredictionId($"p:{index}") }).ToArray();
        var outcomes = new[] { Outcome(predictions[0].Id, 1m), Outcome(predictions[1].Id, 1m), Outcome(predictions[2].Id, 1m), Outcome(predictions[3].Id, -10m) };
        var metrics = ProspectiveMetricsCalculator.Calculate(strategy, predictions, outcomes);
        Assert.Equal(75m, metrics.WinRatePercent);
        Assert.Equal(-1.75m, metrics.ExpectedValuePercent);
        Assert.True(metrics.OutcomeVolatilityPercent > 0);
    }

    [Fact]
    public void EntitlementGateDeniesMissingAndUnauthorizedPersistence()
    {
        var observation = Observation("one", 1, 100m, Nine, TimeSpan.Zero);
        Assert.False(LiveObservationEntitlementGate.Evaluate(observation, null).IsAllowed);
        Assert.Equal(MarketDataEntitlementReasons.PersistenceDenied,
            LiveObservationEntitlementGate.Evaluate(observation, Entitlement(persistence: EntitlementDecision.Denied)).ReasonCode);
    }

    [Fact]
    public void SyntheticEntitlementAllowsForwardObservationUses()
    {
        var result = LiveObservationEntitlementGate.Evaluate(Observation("one", 1, 100m, Nine, TimeSpan.Zero), Entitlement());
        Assert.True(result.IsAllowed); Assert.Equal(PolicyReference, result.Policy);
    }

    [Fact]
    public void ObservationCarriesPersistenceAndJournalReferencesWithoutSecrets()
    {
        var observation = Observation("one", 1, 100m, Nine, TimeSpan.Zero);
        Assert.Equal(Revision, observation.DatasetRevisionId);
        Assert.Equal("live-request-001", observation.IngestionJournalReference.Value);
        var propertyNames = typeof(LiveMarketObservation).GetProperties().Select(value => value.Name).ToArray();
        Assert.DoesNotContain(propertyNames, value => SensitivePropertyTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase)));
        Assert.DoesNotContain("Authorization", JsonSerializer.Serialize(observation), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LiveLearningSurfaceCannotCreateBrokerOrOrderObjects()
    {
        var names = typeof(LiveMarketObservation).Assembly.GetTypes().Where(value => value.Namespace == typeof(LiveMarketObservation).Namespace &&
            (value == typeof(LiveMarketObservation) || value == typeof(SyntheticLiveMarketFeed) || value == typeof(SyntheticShadowLearningPipeline) || value == typeof(ShadowPrediction) || value == typeof(ShadowOutcome)))
            .SelectMany(value => value.GetProperties()).Select(value => value.PropertyType.Name).ToArray();
        Assert.DoesNotContain(names, value => value.Contains("Order", StringComparison.OrdinalIgnoreCase) || value.Contains("Broker", StringComparison.OrdinalIgnoreCase));
    }

    private static ShadowPrediction Prediction()
    {
        var first = Observation("first", 1, 100m, Nine, TimeSpan.Zero);
        var second = Observation("second", 2, 101m, Nine.AddMinutes(5), TimeSpan.Zero);
        return SyntheticShadowLearningPipeline.Evaluate(Instrument, Nine.AddMinutes(5), TimeSpan.FromMinutes(5), Strategy("1"), [first, second]);
    }

    private static ShadowOutcome Outcome(ShadowPredictionId id, decimal net) =>
        new(id, Nine.AddMinutes(10), new LiveObservationId($"outcome:{id.Value}"), net, net + 1, net - 1,
            0.5m, net - 1 <= -1, net + 1 >= 1, 0.1m, net, "fixture:calibration", net > 0 ? "positive" : "negative");

    private static ShadowStrategyVersion Strategy(string version) => new(
        SyntheticShadowLearningPipeline.NonProductionStrategyId, version, new DatasetChecksum($"sha256:configuration-{version}"),
        new VersionReference("features-v1"), new VersionReference("risk-v1"), new VersionReference("build-fixture"));

    private static LiveMarketObservation Observation(string id, long sequence, decimal close, DateTimeOffset eventTime,
        TimeSpan delay, TimeSpan? providerLag = null, TimeSpan? knowledgeLag = null,
        ObservationFreshness freshness = ObservationFreshness.Delayed, MarketDataQualityStatus quality = MarketDataQualityStatus.Valid,
        LiveObservationId? corrects = null)
    {
        var providerTime = eventTime + (providerLag ?? TimeSpan.Zero);
        var received = eventTime + delay;
        var knowledge = received + (knowledgeLag ?? TimeSpan.Zero);
        if (delay == TimeSpan.Zero && freshness == ObservationFreshness.Delayed) freshness = ObservationFreshness.RealTime;
        return new(new LiveObservationId(id), Stream, sequence, Instrument, Provider, Product, "TEST-LIVE", Venue, "XSTO",
            SessionDate, LiveObservationGranularity.FiveMinutes, PriceAdjustment.Raw,
            new Price(close - 0.2m, Sek), new Price(close + 0.3m, Sek), new Price(close - 0.3m, Sek), new Price(close, Sek), 1000m,
            eventTime, providerTime, received, knowledge, freshness, delay, MarketSessionState.Trading, quality,
            Revision, PolicyReference, new EvidenceReference($"fixture:{id}"), new AcquisitionRequestId("live-request-001"), corrects);
    }

    private static SyntheticLiveFeedEvent Event(LiveMarketObservation observation) =>
        new(observation.Id.Value, LiveFeedEventKind.Observation, observation.KnowledgeTimeUtc, observation.Sequence, observation,
            observation.InstrumentId, observation.SessionDate, observation.SessionState, null, observation.Provenance);

    private static SyntheticLiveFeedEvent NonPriceEvent(string id, LiveFeedEventKind kind, DateTimeOffset delivery,
        MarketSessionState session, MarketDataFindingCode? finding) =>
        new(id, kind, delivery, delivery.UtcTicks, null, Instrument, SessionDate, session, finding, new EvidenceReference($"fixture:{id}"));

    private static MarketDataEntitlementPolicy Entitlement(EntitlementDecision persistence = EntitlementDecision.Allowed)
    {
        var uses = Enum.GetValues<MarketDataUse>().Where(value => value != MarketDataUse.Unknown)
            .ToDictionary(value => value, _ => EntitlementDecision.Allowed);
        return new(PolicyReference.Id, PolicyReference.Version, Provider, Product, new EvidenceReference("fixture:synthetic-live-policy"),
            Nine.AddDays(-1), Nine.AddDays(-1), null, uses, persistence, EntitlementDecision.Denied,
            RetentionClassification.SubscriptionOnly, DeletionRequirement.DeleteAtSubscriptionEnd);
    }

    private static DateTimeOffset Utc(int hour, int minute) => new(2024, 6, 3, hour, minute, 0, TimeSpan.Zero);
}
