using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceMarketDataEntitlementTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(MarketDataUse.HistoricalAnalysis)]
    [InlineData(MarketDataUse.Backtest)]
    public void ExplicitValidUseIsAllowedOnlyWithinDeclaredScope(MarketDataUse requestedUse)
    {
        var result = MarketDataEntitlementEvaluator.Evaluate(CreatePolicy(), requestedUse, ActiveContext());

        Assert.True(result.IsAllowed);
        Assert.Equal(EntitlementDecision.Allowed, result.SourceDecision);
        Assert.Equal(MarketDataEntitlementReasons.Allowed, result.ReasonCode);
        Assert.Equal(new PolicyId("example-policy"), result.Policy?.Id);
    }

    [Fact]
    public void UnknownLongTermStorageFailsClosed()
    {
        var result = MarketDataEntitlementEvaluator.Evaluate(
            CreatePolicy(),
            MarketDataUse.LongTermStorage,
            ActiveContext(requiresPersistence: true));

        Assert.False(result.IsAllowed);
        Assert.Equal(EntitlementDecision.Unknown, result.SourceDecision);
        Assert.Equal(MarketDataEntitlementReasons.UsageUnknown, result.ReasonCode);
    }

    [Fact]
    public void ExplicitDeniedUseBeatsOtherAllowedScope()
    {
        var result = MarketDataEntitlementEvaluator.Evaluate(CreatePolicy(), MarketDataUse.StrategyTraining, ActiveContext());

        Assert.False(result.IsAllowed);
        Assert.Equal(EntitlementDecision.Denied, result.SourceDecision);
        Assert.Equal(MarketDataEntitlementReasons.UsageDenied, result.ReasonCode);
    }

    [Fact]
    public void ExpiredPreviouslyAllowedPolicyFailsClosed()
    {
        var policy = CreatePolicy(validUntilUtc: Now.AddMinutes(-1));

        var result = MarketDataEntitlementEvaluator.Evaluate(policy, MarketDataUse.Backtest, ActiveContext());

        Assert.False(result.IsAllowed);
        Assert.Equal(EntitlementDecision.Allowed, result.SourceDecision);
        Assert.Equal(MarketDataEntitlementReasons.PolicyExpired, result.ReasonCode);
    }

    [Fact]
    public void ExplicitDeniedUseWinsEvenWhenPolicyIsExpired()
    {
        var policy = CreatePolicy(validUntilUtc: Now.AddMinutes(-1));

        var result = MarketDataEntitlementEvaluator.Evaluate(
            policy,
            MarketDataUse.StrategyTraining,
            ActiveContext());

        Assert.False(result.IsAllowed);
        Assert.Equal(EntitlementDecision.Denied, result.SourceDecision);
        Assert.Equal(MarketDataEntitlementReasons.UsageDenied, result.ReasonCode);
    }

    [Fact]
    public void MissingPolicyFailsClosedWithoutPolicyReference()
    {
        var result = MarketDataEntitlementEvaluator.Evaluate(null, MarketDataUse.Backtest, ActiveContext());

        Assert.False(result.IsAllowed);
        Assert.Equal(EntitlementDecision.Unknown, result.SourceDecision);
        Assert.Equal(MarketDataEntitlementReasons.PolicyMissing, result.ReasonCode);
        Assert.Null(result.Policy);
    }

    [Theory]
    [InlineData(MarketDataUse.Unknown)]
    [InlineData((MarketDataUse)999)]
    public void UnsupportedUseFailsClosed(MarketDataUse requestedUse)
    {
        var result = MarketDataEntitlementEvaluator.Evaluate(CreatePolicy(), requestedUse, ActiveContext());

        Assert.False(result.IsAllowed);
        Assert.Equal(MarketDataEntitlementReasons.UnsupportedUse, result.ReasonCode);
    }

    [Fact]
    public void PolicyForAnotherProviderProductFailsClosed()
    {
        var context = ActiveContext() with { ProviderDataset = new ProviderDataset("Unknown-Product") };

        var result = MarketDataEntitlementEvaluator.Evaluate(
            CreatePolicy(),
            MarketDataUse.Backtest,
            context);

        Assert.False(result.IsAllowed);
        Assert.Equal(EntitlementDecision.Unknown, result.SourceDecision);
        Assert.Equal(MarketDataEntitlementReasons.PolicyScopeMismatch, result.ReasonCode);
    }

    [Theory]
    [InlineData(MarketDataClassification.Raw)]
    [InlineData(MarketDataClassification.Derived)]
    public void PersistenceRequiresExplicitPermissionForEveryClassification(
        MarketDataClassification classification)
    {
        var policy = CreatePolicy(persistence: EntitlementDecision.Unknown);

        var result = MarketDataEntitlementEvaluator.Evaluate(
            policy,
            MarketDataUse.Backtest,
            ActiveContext(requiresPersistence: true, classification: classification));

        Assert.False(result.IsAllowed);
        Assert.Equal(EntitlementDecision.Unknown, result.SourceDecision);
        Assert.Equal(MarketDataEntitlementReasons.PersistenceUnknown, result.ReasonCode);
    }

    [Fact]
    public void ExplicitPersistenceDenialFailsClosed()
    {
        var policy = CreatePolicy(persistence: EntitlementDecision.Denied);

        var result = MarketDataEntitlementEvaluator.Evaluate(
            policy,
            MarketDataUse.Backtest,
            ActiveContext(requiresPersistence: true));

        Assert.False(result.IsAllowed);
        Assert.Equal(EntitlementDecision.Denied, result.SourceDecision);
        Assert.Equal(MarketDataEntitlementReasons.PersistenceDenied, result.ReasonCode);
    }

    [Fact]
    public void DerivedLineageIsRequiredAndPreserved()
    {
        var parent = new DatasetRevisionId("raw-revision-001");
        var provenance = CreateProvenance(
            MarketDataClassification.Derived,
            [parent],
            revisionId: new DatasetRevisionId("derived-indicator-001"));

        Assert.Equal(MarketDataClassification.Derived, provenance.Classification);
        Assert.Equal([parent], provenance.ParentDatasetRevisions);
        Assert.Throws<ArgumentException>(() => CreateProvenance(
            MarketDataClassification.Derived,
            [],
            revisionId: new DatasetRevisionId("invalid-derived")));
    }

    [Fact]
    public void RawProvenanceCannotDisguiseDerivedInputs()
    {
        Assert.Throws<ArgumentException>(() => CreateProvenance(
            MarketDataClassification.Raw,
            [new DatasetRevisionId("hidden-parent")],
            revisionId: new DatasetRevisionId("raw-revision")));
    }

    [Fact]
    public void DerivedDataIsNotAutomaticallyFree()
    {
        var rawResult = MarketDataEntitlementEvaluator.Evaluate(
            CreatePolicy(),
            MarketDataUse.Backtest,
            ActiveContext(classification: MarketDataClassification.Raw));
        var derivedResult = MarketDataEntitlementEvaluator.Evaluate(
            CreatePolicy(),
            MarketDataUse.DerivedMetrics,
            ActiveContext(classification: MarketDataClassification.Derived));

        Assert.True(rawResult.IsAllowed);
        Assert.False(derivedResult.IsAllowed);
        Assert.Equal(EntitlementDecision.Unknown, derivedResult.SourceDecision);
    }

    [Theory]
    [InlineData(EntitlementDecision.Denied, "marketData.entitlement.postSubscriptionRetentionDenied")]
    [InlineData(EntitlementDecision.Unknown, "marketData.entitlement.postSubscriptionRetentionUnknown")]
    public void EndedSubscriptionRequiresExplicitRetentionPermission(
        EntitlementDecision retentionDecision,
        string expectedReason)
    {
        var policy = CreatePolicy(postSubscriptionRetention: retentionDecision);

        var result = MarketDataEntitlementEvaluator.Evaluate(
            policy,
            MarketDataUse.HistoricalAnalysis,
            ActiveContext(requiresPersistence: true, subscriptionActive: false));

        Assert.False(result.IsAllowed);
        Assert.Equal(retentionDecision, result.SourceDecision);
        Assert.Equal(expectedReason, result.ReasonCode);
    }

    [Fact]
    public void DatasetRevisionIsImmutableValueState()
    {
        var revision = CreateRevision();
        var sameRevision = CreateRevision();

        Assert.Equal(revision, sameRevision);
        Assert.Equal(DatasetRevisionStatus.Complete, revision.Status);
        Assert.DoesNotContain(
            typeof(DatasetRevision).GetProperties(),
            property => property.SetMethod is not null);
    }

    [Fact]
    public void SameInputProducesSameDecisionAndReason()
    {
        var policy = CreatePolicy();
        var context = ActiveContext();

        var first = MarketDataEntitlementEvaluator.Evaluate(policy, MarketDataUse.Backtest, context);
        var second = MarketDataEntitlementEvaluator.Evaluate(policy, MarketDataUse.Backtest, context);

        Assert.Equal(first, second);
    }

    [Fact]
    public void OwnerAcceptedPersonalResearchAllowsOnlyDeclaredZeroCostCapability()
    {
        var policy = CreatePolicy(
            evidenceClass: EntitlementEvidenceClass.OwnerAcceptedPersonalResearch,
            ownerAcceptanceVersion: "BB-076/2026-08-11",
            rationale: "Private zero-cost read-only personal research; no identified prohibition.");

        var allowed = MarketDataEntitlementEvaluator.Evaluate(policy, MarketDataUse.Backtest, ActiveContext());
        var prohibited = MarketDataEntitlementEvaluator.Evaluate(policy, MarketDataUse.StrategyTraining, ActiveContext());

        Assert.True(allowed.IsAllowed);
        Assert.Equal(EntitlementEvidenceClass.OwnerAcceptedPersonalResearch, policy.EvidenceClass);
        Assert.False(prohibited.IsAllowed);
        Assert.Equal(MarketDataEntitlementReasons.UsageDenied, prohibited.ReasonCode);
    }

    [Fact]
    public void OwnerAcceptanceCannotAuthorizeAPaidSource()
    {
        Assert.Throws<ArgumentException>(() => CreatePolicy(
            evidenceClass: EntitlementEvidenceClass.OwnerAcceptedPersonalResearch,
            monetaryCostSek: 1,
            ownerAcceptanceVersion: "BB-076/2026-08-11",
            rationale: "Invalid paid acceptance."));
    }

    [Fact]
    public void OwnerAcceptanceCannotAuthorizeTrading()
    {
        var decisions = new Dictionary<MarketDataUse, EntitlementDecision>
        {
            [MarketDataUse.PaperTrading] = EntitlementDecision.Allowed
        };

        Assert.Throws<ArgumentException>(() => new MarketDataEntitlementPolicy(
            new PolicyId("invalid-trading-policy"), new PolicyVersion("1"),
            new MarketDataProvider("ExampleData"), new ProviderDataset("Free"),
            new EvidenceReference("BB-076"), Now, Now, null, decisions,
            EntitlementDecision.Denied, EntitlementDecision.Denied,
            RetentionClassification.Prohibited, DeletionRequirement.None,
            evidenceClass: EntitlementEvidenceClass.OwnerAcceptedPersonalResearch,
            ownerAcceptanceVersion: "BB-076/2026-08-11", rationale: "Invalid trading grant."));
    }

    [Theory]
    [InlineData(EntitlementEvidenceClass.HumanConfirmationRequired, "marketData.entitlement.humanConfirmationRequired")]
    [InlineData(EntitlementEvidenceClass.Denied, "marketData.entitlement.evidenceDenied")]
    public void EvidenceGateFailsClosedWhenConfirmationOrDenialApplies(
        EntitlementEvidenceClass evidenceClass,
        string expectedReason)
    {
        var result = MarketDataEntitlementEvaluator.Evaluate(
            CreatePolicy(evidenceClass: evidenceClass),
            MarketDataUse.Backtest,
            ActiveContext());

        Assert.False(result.IsAllowed);
        Assert.Equal(expectedReason, result.ReasonCode);
    }

    [Fact]
    public void SyntheticFixtureCanReferenceCorporateActionDatasetWithoutProviderPayload()
    {
        var provenance = new MarketDataProvenance(
            new MarketDataProvider("ExampleData"),
            new ProviderDataset("Synthetic-Corporate-Actions-Personal"),
            Now,
            Now.AddHours(-1),
            new InstrumentId("TEST-XSTO"),
            new MarketVenue("XSTO-TEST", "Synthetic Stockholm venue"),
            new DatasetRevisionId("corporate-action-revision-001"),
            CreatePolicy().Reference,
            MarketDataClassification.Raw,
            [],
            new VersionReference("fixture-adapter-v1"),
            new VersionReference("fixture-schema-v1"),
            MarketDataQualityStatus.Valid);

        Assert.Equal("ExampleData", provenance.Provider.Value);
        Assert.Equal("Synthetic-Corporate-Actions-Personal", provenance.ProviderDataset.Value);
    }

    private static MarketDataEntitlementPolicy CreatePolicy(
        DateTimeOffset? validUntilUtc = null,
        EntitlementDecision persistence = EntitlementDecision.Allowed,
        EntitlementDecision postSubscriptionRetention = EntitlementDecision.Denied,
        EntitlementEvidenceClass evidenceClass = EntitlementEvidenceClass.Unknown,
        decimal monetaryCostSek = 0,
        string? ownerAcceptanceVersion = null,
        string? rationale = null) =>
        new(
            new PolicyId("example-policy"),
            new PolicyVersion("1.0"),
            new MarketDataProvider("ExampleData"),
            new ProviderDataset("Synthetic-EOD-Personal"),
            new EvidenceReference("evidence:synthetic-policy-v1"),
            Now.AddDays(-2),
            Now.AddDays(-1),
            validUntilUtc ?? Now.AddDays(1),
            new Dictionary<MarketDataUse, EntitlementDecision>
            {
                [MarketDataUse.HistoricalAnalysis] = EntitlementDecision.Allowed,
                [MarketDataUse.Backtest] = EntitlementDecision.Allowed,
                [MarketDataUse.StrategyTraining] = EntitlementDecision.Denied,
                [MarketDataUse.LongTermStorage] = EntitlementDecision.Unknown,
                [MarketDataUse.DerivedMetrics] = EntitlementDecision.Unknown
            },
            persistence,
            postSubscriptionRetention,
            RetentionClassification.SubscriptionOnly,
            DeletionRequirement.DeleteAtSubscriptionEnd,
            evidenceClass: evidenceClass,
            monetaryCostSek: monetaryCostSek,
            ownerAcceptanceVersion: ownerAcceptanceVersion,
            rationale: rationale);

    private static MarketDataEntitlementContext ActiveContext(
        bool requiresPersistence = false,
        bool subscriptionActive = true,
        MarketDataClassification classification = MarketDataClassification.Raw) =>
        new(
            Now,
            requiresPersistence,
            subscriptionActive,
            classification,
            new MarketDataProvider("ExampleData"),
            new ProviderDataset("Synthetic-EOD-Personal"));

    private static DatasetRevision CreateRevision() => new(
        new DatasetRevisionId("raw-revision-001"),
        new DatasetId("synthetic-eod-import-001"),
        null,
        new MarketDataProvider("ExampleData"),
        new ProviderDataset("Synthetic-EOD-Personal"),
        new VersionReference("fixture-adapter-v1"),
        new VersionReference("fixture-schema-v1"),
        new DatasetChecksum("sha256:synthetic-not-a-payload"),
        Now,
        Now.AddMinutes(-1),
        DatasetRevisionStatus.Complete);

    private static MarketDataProvenance CreateProvenance(
        MarketDataClassification classification,
        IEnumerable<DatasetRevisionId> parents,
        DatasetRevisionId revisionId) =>
        new(
            new MarketDataProvider("ExampleData"),
            new ProviderDataset("Synthetic-EOD-Personal"),
            Now,
            Now.AddMinutes(-5),
            new InstrumentId("TEST-XSTO"),
            new MarketVenue("XSTO-TEST", "Synthetic Stockholm venue"),
            revisionId,
            CreatePolicy().Reference,
            classification,
            parents,
            new VersionReference("fixture-adapter-v1"),
            new VersionReference("fixture-schema-v1"),
            MarketDataQualityStatus.Valid);
}
