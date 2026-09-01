using System.Collections.Immutable;

namespace BigBrain.Modules.Finance;

public enum ResearchDatasetClass
{
    DailyOhlcv,
    DailyCloseOnlyMarketContext,
    DailyCloseOnlyFx,
    CurrentSnapshotMetadata
}

public enum ResearchDatasetPurpose
{
    ExploratoryStatistics,
    TechnicalSignalResearch,
    BoundedHistoricalBacktest,
    TrainValidationHoldout,
    WalkForward,
    RobustnessSensitivity,
    MarketRegimeContext,
    CrossInstrumentComparison,
    LongHorizonPerformance,
    FeatureModelExperiment
}

public enum ResearchEligibilityState
{
    Ineligible,
    Eligible,
    EligibleWithLimitations
}

public sealed record ResearchDatasetFacts(
    ResearchDatasetClass DatasetClass,
    DatasetOwnerRightsDecision OwnerDecision,
    string OwnerDecisionEvidence,
    DatasetEvidenceResult ExternalRights,
    DatasetEvidenceResult TechnicalIntegrity,
    DatasetEvidenceResult HistoricalIdentity,
    DatasetEvidenceResult PriceBasis,
    DatasetEvidenceResult CorporateActions,
    IReadOnlyList<string> Limitations);

public sealed record ResearchCapabilityDecision(ResearchDatasetPurpose Purpose,
    ResearchEligibilityState State, IReadOnlyList<string> ReasonCodes);

public sealed record ResearchDatasetEligibility(string PolicyId,
    IReadOnlyList<ResearchCapabilityDecision> Capabilities, IReadOnlyList<string> Limitations)
{
    public ResearchCapabilityDecision For(ResearchDatasetPurpose purpose) =>
        Capabilities.Single(x => x.Purpose == purpose);
}

public static class ResearchDatasetEligibilityPolicyV1
{
    public const string Id = "owner-research-eligibility-v1";

    public static ResearchDatasetEligibility Evaluate(ResearchDatasetFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.OwnerDecision == DatasetOwnerRightsDecision.ApprovedByOwner &&
            string.IsNullOrWhiteSpace(facts.OwnerDecisionEvidence))
            throw new ArgumentException("Owner approval requires a durable evidence reference.", nameof(facts));

        var decisions = Enum.GetValues<ResearchDatasetPurpose>()
            .Select(purpose => Decide(facts, purpose)).ToImmutableArray();
        return new(Id, decisions, facts.Limitations.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
    }

    private static ResearchCapabilityDecision Decide(ResearchDatasetFacts facts, ResearchDatasetPurpose purpose)
    {
        var reasons = new List<string>();
        if (facts.OwnerDecision != DatasetOwnerRightsDecision.ApprovedByOwner)
            reasons.Add("ownerApproval.missing");
        if (facts.ExternalRights == DatasetEvidenceResult.Fail)
            reasons.Add("externalRights.explicitlyDenied");
        if (facts.TechnicalIntegrity == DatasetEvidenceResult.Fail)
            reasons.Add("technicalIntegrity.failed");
        if (reasons.Count > 0) return new(purpose, ResearchEligibilityState.Ineligible, reasons);

        if (facts.DatasetClass == ResearchDatasetClass.CurrentSnapshotMetadata)
            return purpose == ResearchDatasetPurpose.ExploratoryStatistics
                ? Limited(purpose, "currentSnapshot.notHistoricalPointInTime")
                : Ineligible(purpose, "currentSnapshot.lookAheadBoundary");

        var requiresOhlcv = purpose is ResearchDatasetPurpose.TechnicalSignalResearch or
            ResearchDatasetPurpose.BoundedHistoricalBacktest or ResearchDatasetPurpose.TrainValidationHoldout or
            ResearchDatasetPurpose.WalkForward or ResearchDatasetPurpose.RobustnessSensitivity or
            ResearchDatasetPurpose.FeatureModelExperiment;
        if (requiresOhlcv && facts.DatasetClass != ResearchDatasetClass.DailyOhlcv)
            return Ineligible(purpose, "schema.dailyOhlcvRequired");

        if (purpose == ResearchDatasetPurpose.MarketRegimeContext && facts.DatasetClass == ResearchDatasetClass.DailyOhlcv)
            reasons.Add("purpose.marketContextDerivedFromTradableSeries");

        if (purpose == ResearchDatasetPurpose.LongHorizonPerformance &&
            (facts.PriceBasis != DatasetEvidenceResult.Pass || facts.CorporateActions != DatasetEvidenceResult.Pass))
            return Ineligible(purpose, "semantics.totalReturnNotEstablished");

        if (facts.ExternalRights == DatasetEvidenceResult.Unknown)
            reasons.Add("externalRights.unknownOwnerAcceptedRisk");
        if (facts.HistoricalIdentity != DatasetEvidenceResult.Pass)
            reasons.Add("historicalIdentity.boundedOwnerClaimOnly");
        if (facts.DatasetClass == ResearchDatasetClass.DailyOhlcv && facts.PriceBasis != DatasetEvidenceResult.Pass)
            reasons.Add("priceBasis.ownerClaimOnly");
        if (facts.DatasetClass == ResearchDatasetClass.DailyOhlcv && facts.CorporateActions != DatasetEvidenceResult.Pass)
            reasons.Add("corporateActions.unresolved");
        if (facts.TechnicalIntegrity == DatasetEvidenceResult.Unknown)
            reasons.Add("technicalIntegrity.limited");
        reasons.AddRange(facts.Limitations);

        return new(purpose, reasons.Count == 0 ? ResearchEligibilityState.Eligible :
            ResearchEligibilityState.EligibleWithLimitations,
            reasons.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
    }

    private static ResearchCapabilityDecision Ineligible(ResearchDatasetPurpose purpose, string reason) =>
        new(purpose, ResearchEligibilityState.Ineligible, [reason]);

    private static ResearchCapabilityDecision Limited(ResearchDatasetPurpose purpose, string reason) =>
        new(purpose, ResearchEligibilityState.EligibleWithLimitations, [reason]);
}

public sealed record ResearchDatasetLineage(string DatasetRevisionId, string CandidateId,
    string ArtifactSha256, string DatasetFingerprint, string SourceClaim, string SheetName,
    string OwnerDecisionEvidence, DatasetEvidenceResult ExternalRights,
    ResearchDatasetPurpose Purpose, ResearchEligibilityState Eligibility,
    IReadOnlyList<string> Limitations);
