using System.Collections.Immutable;

namespace BigBrain.Modules.Finance;

public enum MarketDataUse
{
    Unknown = 0,
    LiveDisplay,
    HistoricalAnalysis,
    Backtest,
    WalkForward,
    PaperTrading,
    StrategyTraining,
    DerivedMetrics,
    LongTermStorage
}

public enum EntitlementDecision
{
    Unknown = 0,
    Allowed,
    Denied
}

public enum EntitlementEvidenceClass
{
    Unknown = 0,
    ExplicitProviderGrant,
    OwnerAcceptedPersonalResearch,
    HumanConfirmationRequired,
    Denied
}

public enum MarketDataClassification
{
    Unknown = 0,
    Raw,
    Derived
}

public enum RetentionClassification
{
    Unknown = 0,
    Prohibited,
    SubscriptionOnly,
    TimeLimited,
    LongTerm
}

public enum DeletionRequirement
{
    Unknown = 0,
    None,
    DeleteAtSubscriptionEnd,
    DeleteByDeadline
}

public enum DatasetRevisionStatus
{
    Unknown = 0,
    Incomplete,
    Complete,
    Quarantined,
    Superseded,
    Rejected
}

public enum MarketDataQualityStatus
{
    Unknown = 0,
    Valid,
    Incomplete,
    Stale,
    Duplicate,
    Gap,
    Conflict,
    Corrected,
    Quarantined,
    Rejected
}

public readonly record struct PolicyId
{
    public PolicyId(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct PolicyVersion
{
    public PolicyVersion(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct MarketDataProvider
{
    public MarketDataProvider(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct ProviderDataset
{
    public ProviderDataset(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct EvidenceReference
{
    public EvidenceReference(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct DatasetId
{
    public DatasetId(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct DatasetRevisionId
{
    public DatasetRevisionId(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct VersionReference
{
    public VersionReference(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct DatasetChecksum
{
    public DatasetChecksum(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct MarketDataPolicyReference(PolicyId Id, PolicyVersion Version)
{
    public MarketDataPolicyReference Validate()
    {
        RequiredText.Require(Id.Value, nameof(Id));
        RequiredText.Require(Version.Value, nameof(Version));
        return this;
    }
}

public sealed record MarketDataEntitlementPolicy
{
    private readonly ImmutableDictionary<MarketDataUse, EntitlementDecision> _useDecisions;

    public MarketDataEntitlementPolicy(
        PolicyId id,
        PolicyVersion version,
        MarketDataProvider provider,
        ProviderDataset providerDataset,
        EvidenceReference evidence,
        DateTimeOffset reviewedAtUtc,
        DateTimeOffset validFromUtc,
        DateTimeOffset? validUntilUtc,
        IReadOnlyDictionary<MarketDataUse, EntitlementDecision> useDecisions,
        EntitlementDecision persistence,
        EntitlementDecision postSubscriptionRetention,
        RetentionClassification retention,
        DeletionRequirement deletion,
        DateTimeOffset? deletionDeadlineUtc = null,
        EntitlementEvidenceClass evidenceClass = EntitlementEvidenceClass.Unknown,
        decimal monetaryCostSek = 0,
        string? ownerAcceptanceVersion = null,
        string? rationale = null)
    {
        RequiredText.Require(id.Value, nameof(id));
        RequiredText.Require(version.Value, nameof(version));
        RequiredText.Require(provider.Value, nameof(provider));
        RequiredText.Require(providerDataset.Value, nameof(providerDataset));
        RequiredText.Require(evidence.Value, nameof(evidence));
        FinanceTime.RequireUtc(reviewedAtUtc, nameof(reviewedAtUtc));
        FinanceTime.RequireUtc(validFromUtc, nameof(validFromUtc));
        if (validUntilUtc is { } validUntil)
        {
            FinanceTime.RequireUtc(validUntil, nameof(validUntilUtc));
            if (validUntil < validFromUtc)
            {
                throw new ArgumentException("Policy validity cannot end before it starts.", nameof(validUntilUtc));
            }
        }

        if (deletionDeadlineUtc is { } deletionDeadline)
        {
            FinanceTime.RequireUtc(deletionDeadline, nameof(deletionDeadlineUtc));
        }

        ArgumentNullException.ThrowIfNull(useDecisions);
        if (useDecisions.ContainsKey(MarketDataUse.Unknown) ||
            useDecisions.Any(pair => !Enum.IsDefined(pair.Key) || !Enum.IsDefined(pair.Value)))
        {
            throw new ArgumentException("Policy decisions must use known values.", nameof(useDecisions));
        }

        if (!Enum.IsDefined(persistence) || !Enum.IsDefined(postSubscriptionRetention) ||
            !Enum.IsDefined(retention) || !Enum.IsDefined(deletion) || !Enum.IsDefined(evidenceClass))
        {
            throw new ArgumentException("Policy classifications must use defined values.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(monetaryCostSek);

        if (evidenceClass == EntitlementEvidenceClass.OwnerAcceptedPersonalResearch)
        {
            if (monetaryCostSek != 0)
            {
                throw new ArgumentException("Owner-accepted personal research is limited to zero-cost sources.", nameof(monetaryCostSek));
            }

            RequiredText.Require(ownerAcceptanceVersion, nameof(ownerAcceptanceVersion));
            RequiredText.Require(rationale, nameof(rationale));

            if (useDecisions.GetValueOrDefault(MarketDataUse.PaperTrading) == EntitlementDecision.Allowed)
            {
                throw new ArgumentException(
                    "Owner-accepted personal research cannot authorize broker-interacting paper trading.",
                    nameof(useDecisions));
            }
        }

        if (deletion == DeletionRequirement.DeleteByDeadline && deletionDeadlineUtc is null)
        {
            throw new ArgumentException("A deletion deadline is required.", nameof(deletionDeadlineUtc));
        }

        if (deletion != DeletionRequirement.DeleteByDeadline && deletionDeadlineUtc is not null)
        {
            throw new ArgumentException("A deletion deadline is only valid for deadline deletion.", nameof(deletionDeadlineUtc));
        }

        Id = id;
        Version = version;
        Provider = provider;
        ProviderDataset = providerDataset;
        Evidence = evidence;
        ReviewedAtUtc = reviewedAtUtc;
        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
        _useDecisions = useDecisions.ToImmutableDictionary();
        Persistence = persistence;
        PostSubscriptionRetention = postSubscriptionRetention;
        Retention = retention;
        Deletion = deletion;
        DeletionDeadlineUtc = deletionDeadlineUtc;
        EvidenceClass = evidenceClass;
        MonetaryCostSek = monetaryCostSek;
        OwnerAcceptanceVersion = ownerAcceptanceVersion;
        Rationale = rationale;
    }

    public PolicyId Id { get; }
    public PolicyVersion Version { get; }
    public MarketDataProvider Provider { get; }
    public ProviderDataset ProviderDataset { get; }
    public EvidenceReference Evidence { get; }
    public DateTimeOffset ReviewedAtUtc { get; }
    public DateTimeOffset ValidFromUtc { get; }
    public DateTimeOffset? ValidUntilUtc { get; }
    public IReadOnlyDictionary<MarketDataUse, EntitlementDecision> UseDecisions => _useDecisions;
    public EntitlementDecision Persistence { get; }
    public EntitlementDecision PostSubscriptionRetention { get; }
    public RetentionClassification Retention { get; }
    public DeletionRequirement Deletion { get; }
    public DateTimeOffset? DeletionDeadlineUtc { get; }
    public EntitlementEvidenceClass EvidenceClass { get; }
    public decimal MonetaryCostSek { get; }
    public string? OwnerAcceptanceVersion { get; }
    public string? Rationale { get; }
    public MarketDataPolicyReference Reference => new(Id, Version);

    public EntitlementDecision DecisionFor(MarketDataUse requestedUse) =>
        _useDecisions.GetValueOrDefault(requestedUse, EntitlementDecision.Unknown);
}

public sealed record DatasetRevision
{
    public DatasetRevision(
        DatasetRevisionId id,
        DatasetId datasetId,
        DatasetRevisionId? parentRevisionId,
        MarketDataProvider provider,
        ProviderDataset providerDataset,
        VersionReference adapterVersion,
        VersionReference schemaVersion,
        DatasetChecksum checksum,
        DateTimeOffset createdAtUtc,
        DateTimeOffset retrievedAtUtc,
        DatasetRevisionStatus status)
    {
        RequiredText.Require(id.Value, nameof(id));
        RequiredText.Require(datasetId.Value, nameof(datasetId));
        if (parentRevisionId is { } parent)
        {
            RequiredText.Require(parent.Value, nameof(parentRevisionId));
            if (parent == id)
            {
                throw new ArgumentException("A dataset revision cannot be its own parent.", nameof(parentRevisionId));
            }
        }
        RequiredText.Require(provider.Value, nameof(provider));
        RequiredText.Require(providerDataset.Value, nameof(providerDataset));
        RequiredText.Require(adapterVersion.Value, nameof(adapterVersion));
        RequiredText.Require(schemaVersion.Value, nameof(schemaVersion));
        RequiredText.Require(checksum.Value, nameof(checksum));
        FinanceTime.RequireUtc(createdAtUtc, nameof(createdAtUtc));
        FinanceTime.RequireUtc(retrievedAtUtc, nameof(retrievedAtUtc));
        if (createdAtUtc < retrievedAtUtc)
        {
            throw new ArgumentException("Dataset revision cannot be created before retrieval.", nameof(createdAtUtc));
        }

        if (status == DatasetRevisionStatus.Unknown || !Enum.IsDefined(status))
        {
            throw new ArgumentException("Dataset revision status is required.", nameof(status));
        }

        Id = id;
        DatasetId = datasetId;
        ParentRevisionId = parentRevisionId;
        Provider = provider;
        ProviderDataset = providerDataset;
        AdapterVersion = adapterVersion;
        SchemaVersion = schemaVersion;
        Checksum = checksum;
        CreatedAtUtc = createdAtUtc;
        RetrievedAtUtc = retrievedAtUtc;
        Status = status;
    }

    public DatasetRevisionId Id { get; }
    public DatasetId DatasetId { get; }
    public DatasetRevisionId? ParentRevisionId { get; }
    public MarketDataProvider Provider { get; }
    public ProviderDataset ProviderDataset { get; }
    public VersionReference AdapterVersion { get; }
    public VersionReference SchemaVersion { get; }
    public DatasetChecksum Checksum { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset RetrievedAtUtc { get; }
    public DatasetRevisionStatus Status { get; }
}

public sealed record MarketDataProvenance
{
    public MarketDataProvenance(
        MarketDataProvider provider,
        ProviderDataset providerDataset,
        DateTimeOffset retrievedAtUtc,
        DateTimeOffset sourceTimestampUtc,
        InstrumentId instrumentId,
        MarketVenue venue,
        DatasetRevisionId datasetRevisionId,
        MarketDataPolicyReference policy,
        MarketDataClassification classification,
        IEnumerable<DatasetRevisionId> parentDatasetRevisions,
        VersionReference adapterVersion,
        VersionReference schemaVersion,
        MarketDataQualityStatus quality)
    {
        RequiredText.Require(provider.Value, nameof(provider));
        RequiredText.Require(providerDataset.Value, nameof(providerDataset));
        if (string.IsNullOrWhiteSpace(instrumentId.Value))
        {
            throw new ArgumentException("Instrument identity is required.", nameof(instrumentId));
        }
        RequiredText.Require(datasetRevisionId.Value, nameof(datasetRevisionId));
        RequiredText.Require(adapterVersion.Value, nameof(adapterVersion));
        RequiredText.Require(schemaVersion.Value, nameof(schemaVersion));
        ArgumentNullException.ThrowIfNull(venue);
        policy.Validate();
        FinanceTime.RequireUtc(retrievedAtUtc, nameof(retrievedAtUtc));
        FinanceTime.RequireUtc(sourceTimestampUtc, nameof(sourceTimestampUtc));
        if (retrievedAtUtc < sourceTimestampUtc)
        {
            throw new ArgumentException("Retrieval cannot precede the source timestamp.", nameof(retrievedAtUtc));
        }

        if (classification == MarketDataClassification.Unknown || !Enum.IsDefined(classification))
        {
            throw new ArgumentException("Data classification is required.", nameof(classification));
        }

        if (quality == MarketDataQualityStatus.Unknown || !Enum.IsDefined(quality))
        {
            throw new ArgumentException("Data quality status is required.", nameof(quality));
        }

        ArgumentNullException.ThrowIfNull(parentDatasetRevisions);
        var parents = parentDatasetRevisions.ToImmutableArray();
        if (parents.Any(parent => string.IsNullOrWhiteSpace(parent.Value)) || parents.Distinct().Count() != parents.Length)
        {
            throw new ArgumentException("Parent dataset revisions must be valid and unique.", nameof(parentDatasetRevisions));
        }

        if (classification == MarketDataClassification.Raw && !parents.IsEmpty)
        {
            throw new ArgumentException("Raw provenance cannot declare derived input revisions.", nameof(parentDatasetRevisions));
        }

        if (classification == MarketDataClassification.Derived && parents.IsEmpty)
        {
            throw new ArgumentException("Derived provenance requires at least one input revision.", nameof(parentDatasetRevisions));
        }

        Provider = provider;
        ProviderDataset = providerDataset;
        RetrievedAtUtc = retrievedAtUtc;
        SourceTimestampUtc = sourceTimestampUtc;
        InstrumentId = instrumentId;
        Venue = venue;
        DatasetRevisionId = datasetRevisionId;
        Policy = policy;
        Classification = classification;
        ParentDatasetRevisions = parents;
        AdapterVersion = adapterVersion;
        SchemaVersion = schemaVersion;
        Quality = quality;
    }

    public MarketDataProvider Provider { get; }
    public ProviderDataset ProviderDataset { get; }
    public DateTimeOffset RetrievedAtUtc { get; }
    public DateTimeOffset SourceTimestampUtc { get; }
    public InstrumentId InstrumentId { get; }
    public MarketVenue Venue { get; }
    public DatasetRevisionId DatasetRevisionId { get; }
    public MarketDataPolicyReference Policy { get; }
    public MarketDataClassification Classification { get; }
    public ImmutableArray<DatasetRevisionId> ParentDatasetRevisions { get; }
    public VersionReference AdapterVersion { get; }
    public VersionReference SchemaVersion { get; }
    public MarketDataQualityStatus Quality { get; }
}

public sealed record MarketDataEntitlementContext(
    DateTimeOffset EvaluatedAtUtc,
    bool RequiresPersistence,
    bool SubscriptionActive,
    MarketDataClassification Classification,
    MarketDataProvider Provider,
    ProviderDataset ProviderDataset)
{
    public MarketDataEntitlementContext Validate()
    {
        FinanceTime.RequireUtc(EvaluatedAtUtc, nameof(EvaluatedAtUtc));
        if (Classification == MarketDataClassification.Unknown || !Enum.IsDefined(Classification))
        {
            throw new ArgumentException("Data classification is required.", nameof(Classification));
        }

        RequiredText.Require(Provider.Value, nameof(Provider));
        RequiredText.Require(ProviderDataset.Value, nameof(ProviderDataset));

        return this;
    }
}

public sealed record MarketDataEntitlementEvaluation(
    bool IsAllowed,
    EntitlementDecision SourceDecision,
    string ReasonCode,
    MarketDataPolicyReference? Policy);

public static class MarketDataEntitlementReasons
{
    public const string Allowed = "marketData.entitlement.allowed";
    public const string PolicyMissing = "marketData.entitlement.policyMissing";
    public const string PolicyNotYetValid = "marketData.entitlement.policyNotYetValid";
    public const string PolicyExpired = "marketData.entitlement.policyExpired";
    public const string PolicyScopeMismatch = "marketData.entitlement.policyScopeMismatch";
    public const string UsageDenied = "marketData.entitlement.usageDenied";
    public const string UsageUnknown = "marketData.entitlement.usageUnknown";
    public const string UnsupportedUse = "marketData.entitlement.unsupportedUse";
    public const string PersistenceDenied = "marketData.entitlement.persistenceDenied";
    public const string PersistenceUnknown = "marketData.entitlement.persistenceUnknown";
    public const string PostSubscriptionRetentionDenied = "marketData.entitlement.postSubscriptionRetentionDenied";
    public const string PostSubscriptionRetentionUnknown = "marketData.entitlement.postSubscriptionRetentionUnknown";
    public const string HumanConfirmationRequired = "marketData.entitlement.humanConfirmationRequired";
    public const string EvidenceDenied = "marketData.entitlement.evidenceDenied";
}

public static class MarketDataEntitlementEvaluator
{
    public static MarketDataEntitlementEvaluation Evaluate(
        MarketDataEntitlementPolicy? policy,
        MarketDataUse requestedUse,
        MarketDataEntitlementContext context)
    {
        context.Validate();
        if (policy is null)
        {
            return Deny(EntitlementDecision.Unknown, MarketDataEntitlementReasons.PolicyMissing, null);
        }

        var reference = policy.Reference;
        if (requestedUse == MarketDataUse.Unknown || !Enum.IsDefined(requestedUse))
        {
            return Deny(EntitlementDecision.Unknown, MarketDataEntitlementReasons.UnsupportedUse, reference);
        }

        if (context.Provider != policy.Provider || context.ProviderDataset != policy.ProviderDataset)
        {
            return Deny(EntitlementDecision.Unknown, MarketDataEntitlementReasons.PolicyScopeMismatch, reference);
        }

        var useDecision = policy.DecisionFor(requestedUse);
        if (useDecision == EntitlementDecision.Denied)
        {
            return Deny(useDecision, MarketDataEntitlementReasons.UsageDenied, reference);
        }

        if (context.RequiresPersistence && policy.Persistence == EntitlementDecision.Denied)
        {
            return Deny(policy.Persistence, MarketDataEntitlementReasons.PersistenceDenied, reference);
        }

        if (!context.SubscriptionActive && policy.PostSubscriptionRetention == EntitlementDecision.Denied)
        {
            return Deny(
                policy.PostSubscriptionRetention,
                MarketDataEntitlementReasons.PostSubscriptionRetentionDenied,
                reference);
        }

        if (policy.EvidenceClass == EntitlementEvidenceClass.Denied)
        {
            return Deny(EntitlementDecision.Denied, MarketDataEntitlementReasons.EvidenceDenied, reference);
        }

        if (policy.EvidenceClass == EntitlementEvidenceClass.HumanConfirmationRequired)
        {
            return Deny(EntitlementDecision.Unknown, MarketDataEntitlementReasons.HumanConfirmationRequired, reference);
        }

        if (context.EvaluatedAtUtc < policy.ValidFromUtc)
        {
            return Deny(useDecision, MarketDataEntitlementReasons.PolicyNotYetValid, reference);
        }

        if (policy.ValidUntilUtc is { } validUntil && context.EvaluatedAtUtc > validUntil)
        {
            return Deny(useDecision, MarketDataEntitlementReasons.PolicyExpired, reference);
        }

        if (useDecision == EntitlementDecision.Unknown)
        {
            return Deny(useDecision, MarketDataEntitlementReasons.UsageUnknown, reference);
        }

        if (context.RequiresPersistence && policy.Persistence == EntitlementDecision.Unknown)
        {
            return Deny(policy.Persistence, MarketDataEntitlementReasons.PersistenceUnknown, reference);
        }

        if (!context.SubscriptionActive && policy.PostSubscriptionRetention == EntitlementDecision.Unknown)
        {
            return Deny(
                policy.PostSubscriptionRetention,
                MarketDataEntitlementReasons.PostSubscriptionRetentionUnknown,
                reference);
        }

        return new MarketDataEntitlementEvaluation(
            true,
            EntitlementDecision.Allowed,
            MarketDataEntitlementReasons.Allowed,
            reference);
    }

    private static MarketDataEntitlementEvaluation Deny(
        EntitlementDecision sourceDecision,
        string reasonCode,
        MarketDataPolicyReference? policy) =>
        new(false, sourceDecision, reasonCode, policy);
}

internal static class RequiredText
{
    public static string Normalize(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }

    public static void Require(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }
    }
}
