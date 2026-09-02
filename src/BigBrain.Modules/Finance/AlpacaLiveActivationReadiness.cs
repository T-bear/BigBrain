namespace BigBrain.Modules.Finance;

public enum LiveMarketCoverage
{
    Unknown = 0,
    IexSingleExchange,
    ConsolidatedUs
}

public enum LiveProviderActivationState
{
    Unknown = 0,
    NotActivated,
    Activated
}

public enum LiveProviderAcquisitionDecision
{
    Unknown = 0,
    Blocked,
    Allowed
}

public enum LiveProviderTechnicalState
{
    Unknown = 0,
    ContractKnownWithMappingGap,
    Ready
}

public enum LiveObservationType
{
    Unknown = 0,
    Trade,
    Quote,
    Bar
}

public sealed record LiveProviderActivationStatus(
    string Provider,
    string Product,
    string Feed,
    string AssetClass,
    string TransportCandidate,
    ObservationFreshness Freshness,
    LiveMarketCoverage MarketCoverage,
    int MaximumWebSocketSymbols,
    bool CredentialsRequired,
    bool CredentialsConfigured,
    LiveProviderTechnicalState TechnicalCapability,
    EntitlementEvidenceClass Entitlement,
    LiveProviderActivationState Activation,
    LiveProviderAcquisitionDecision Acquisition,
    string Reason,
    MarketDataPolicyReference Policy,
    EvidenceReference Evidence,
    string EventTimestampMapping,
    string ProviderTimestampMapping,
    string ReceivedTimestampMapping,
    string KnowledgeTimestampMapping);

/// <summary>
/// BB-128A activation metadata only. This class has no network client, endpoint URI or credentials.
/// The existing entitlement evaluator remains the authority for any future acquisition path.
/// </summary>
public static class AlpacaBasicIexReadiness
{
    public const string ProviderName = "Alpaca";
    public const string ProductName = "Basic";
    public const string FeedName = "IEX";
    public const int MaximumWebSocketSymbols = 30;
    public const string BlockReason = "durableDataLifecycleNotConfirmed";
    public static readonly MarketDataPolicyReference PolicyReference = new(
        new PolicyId("alpaca-basic-iex-live"), new PolicyVersion("bb-128a-v1"));

    public static readonly EvidenceReference Evidence = new("BB-125-AND-ALPACA-FIRST-PARTY-DOCS-REVIEW-2026-09-02");

    public static MarketDataEntitlementPolicy EntitlementPolicy(DateTimeOffset reviewedAtUtc)
    {
        FinanceTime.RequireUtc(reviewedAtUtc, nameof(reviewedAtUtc));
        var uses = Enum.GetValues<MarketDataUse>()
            .Where(value => value != MarketDataUse.Unknown)
            .ToDictionary(value => value, _ => EntitlementDecision.Unknown);
        return new(
            PolicyReference.Id,
            PolicyReference.Version,
            new MarketDataProvider(ProviderName),
            new ProviderDataset($"{ProductName}-{FeedName}-LIVE"),
            Evidence,
            reviewedAtUtc,
            reviewedAtUtc,
            null,
            uses,
            EntitlementDecision.Unknown,
            EntitlementDecision.Unknown,
            RetentionClassification.Unknown,
            DeletionRequirement.Unknown,
            evidenceClass: EntitlementEvidenceClass.HumanConfirmationRequired,
            rationale: "Persistent raw/normalized observations, revisions, backups, derived evidence and post-account retention are unresolved.");
    }

    public static LiveProviderActivationStatus Status() => new(
        ProviderName,
        ProductName,
        FeedName,
        "US stocks and ETFs",
        "WebSocket",
        ObservationFreshness.RealTime,
        LiveMarketCoverage.IexSingleExchange,
        MaximumWebSocketSymbols,
        CredentialsRequired: true,
        CredentialsConfigured: false,
        LiveProviderTechnicalState.ContractKnownWithMappingGap,
        EntitlementEvidenceClass.HumanConfirmationRequired,
        LiveProviderActivationState.NotActivated,
        LiveProviderAcquisitionDecision.Blocked,
        BlockReason,
        PolicyReference,
        Evidence,
        "Alpaca message field t is the provider-documented market event timestamp.",
        "UNKNOWN: Alpaca live messages do not document a separate provider-processing timestamp.",
        "Assigned by BigBrain only after a future adapter receives a message.",
        "Assigned no earlier than receive time after validation; existing causal ordering remains authoritative.");

    public static LiveStreamId CreateStreamId(
        InstrumentId instrument,
        LiveObservationType observationType,
        LiveObservationGranularity granularity,
        LiveMarketCoverage coverage)
    {
        RequiredText.Require(instrument.Value, nameof(instrument));
        if (observationType == LiveObservationType.Unknown || !Enum.IsDefined(observationType))
            throw new ArgumentException("Observation type is required.", nameof(observationType));
        if (granularity == LiveObservationGranularity.Unknown || !Enum.IsDefined(granularity))
            throw new ArgumentException("Granularity is required.", nameof(granularity));
        if (coverage != LiveMarketCoverage.IexSingleExchange)
            throw new ArgumentException("Alpaca Basic live identity must remain IEX single-exchange coverage.", nameof(coverage));
        return new LiveStreamId($"alpaca:basic:iex:{instrument.Value}:{observationType}:{granularity}:{PolicyReference.Id.Value}@{PolicyReference.Version.Value}".ToLowerInvariant());
    }
}

public static class AlpacaLiveActivationGate
{
    private static readonly MarketDataUse[] RequiredUses =
    [
        MarketDataUse.LiveDisplay,
        MarketDataUse.HistoricalAnalysis,
        MarketDataUse.WalkForward,
        MarketDataUse.StrategyTraining,
        MarketDataUse.DerivedMetrics,
        MarketDataUse.LongTermStorage
    ];

    public static LiveProviderActivationStatus Evaluate(MarketDataEntitlementPolicy? policy)
    {
        var status = AlpacaBasicIexReadiness.Status();
        if (policy is null || policy.Reference != status.Policy)
            return status;
        var now = policy.ReviewedAtUtc;
        var context = new MarketDataEntitlementContext(now, RequiresPersistence: true, SubscriptionActive: false,
            MarketDataClassification.Raw, policy.Provider, policy.ProviderDataset);
        foreach (var use in RequiredUses)
        {
            if (!MarketDataEntitlementEvaluator.Evaluate(policy, use, context).IsAllowed)
                return status;
        }
        return status with
        {
            Entitlement = policy.EvidenceClass,
            TechnicalCapability = LiveProviderTechnicalState.Ready,
            Activation = LiveProviderActivationState.Activated,
            Acquisition = LiveProviderAcquisitionDecision.Allowed,
            Reason = MarketDataEntitlementReasons.Allowed
        };
    }

    public static async Task ExecuteIfAllowedAsync(
        MarketDataEntitlementPolicy? policy,
        Func<CancellationToken, Task> acquisition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(acquisition);
        var decision = Evaluate(policy);
        if (decision.Acquisition != LiveProviderAcquisitionDecision.Allowed)
            throw new InvalidOperationException($"Alpaca acquisition blocked: {decision.Reason}.");
        await acquisition(cancellationToken).ConfigureAwait(false);
    }
}
