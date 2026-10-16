using System.Collections.Immutable;

namespace BigBrain.Modules.Finance;

// Explicit retrospective authority. This is neither synthetic nor prospective evidence.
// Full source commitment remains Finance-owned; only development statistics cross BRF1.
public sealed record HistoricalResearchAdmission(string Contract, string CandidateId, string RevisionId,
    string ArtifactChecksum, string SnapshotChecksum, string ModelArtifactChecksum)
{
    public const string Version = "wiki-historical-research-v1";
    public const string Scope = "historical-daily-momentum20-v1";
    public const string Projection = "historical-development-v1";
    public const string Candidate = "wiki-eod-mirror-kmfranz-v1";
    public const string Model = "sha256:b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897";
    public const string Artifact = "sha256:dd5127aae478d270150904fcbad6e96a42e461e13c3d48a1587edb9b89cea43e";
    public const string Revision = "wiki-5713d7dccfa38f56";
    public static readonly DateOnly From = new(2014, 1, 2);
    public static readonly DateOnly To = new(2016, 12, 19);
    public static readonly ImmutableArray<string> RequiredLimitations =
        ["historical.notProspective", "raw.notTotalReturn", "corporateActions.unresolved", "survivorship.unknown", "historical.priorExposureNotProven"];

    public void Validate(RecurringResearchAuthorization grant, DateTimeOffset registered)
    {
        if (Contract != Version || CandidateId != Candidate || RevisionId != Revision ||
            ModelArtifactChecksum != Model || ArtifactChecksum != Artifact ||
            new[] { ArtifactChecksum, SnapshotChecksum, ModelArtifactChecksum }.Any(x =>
                x is null || x.Length != 71 || !x.StartsWith("sha256:", StringComparison.Ordinal) ||
                x[7..].Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f'))))
            throw new InvalidOperationException("Pinned historical source/model commitments required.");
        if (grant.Instrument.Id.Value != "US:XNAS:AAPL" || grant.Instrument.Type != InstrumentType.Equity ||
            grant.Instrument.Currency.Code != "USD" || grant.Instrument.Mic != "XNAS" ||
            grant.Mapping.Provider.Value != "NASDAQ-WIKI" || grant.Mapping.ProviderReference != "AAPL" ||
            grant.Budget.Invocations != 1 || grant.Budget.Evaluations != 1 || grant.Budget.Trials != 3 ||
            grant.Budget.UnderlyingCalls != 64 || grant.Cohorts.Length != 1 ||
            grant.Cohorts[0].From != From || grant.Cohorts[0].To != Sessions()[499] ||
            grant.Cohorts[0].DueAtUtc < registered || grant.Cohorts[0].DueAtUtc >= grant.ExpiresAtUtc ||
            !grant.Mapping.IsValidOn(From) || !grant.Mapping.IsValidOn(To))
            throw new InvalidOperationException("Historical authority is one fixed first-500-session experiment from the pinned 748-row source.");
        FinanceTime.RequireUtc(grant.Cohorts[0].DueAtUtc, nameof(grant.Cohorts));
        var facts = grant.Facts;
        if (facts.DatasetClass != ResearchDatasetClass.DailyOhlcv || facts.OwnerDecision != DatasetOwnerRightsDecision.ApprovedByOwner ||
            facts.ExternalRights != DatasetEvidenceResult.Pass || facts.TechnicalIntegrity != DatasetEvidenceResult.Pass ||
            facts.PriceBasis != DatasetEvidenceResult.Pass || facts.CorporateActions != DatasetEvidenceResult.Unknown ||
            RequiredLimitations.Any(x => !facts.Limitations.Contains(x, StringComparer.Ordinal)) ||
            new[] { ResearchDatasetPurpose.BoundedHistoricalBacktest, ResearchDatasetPurpose.TrainValidationHoldout,
                ResearchDatasetPurpose.RobustnessSensitivity, ResearchDatasetPurpose.WalkForward }.Any(p =>
                ResearchDatasetEligibilityPolicyV1.Evaluate(facts).For(p).State == ResearchEligibilityState.Ineligible))
            throw new InvalidOperationException("Historical admission requires explicit rights and truthful raw-price limitations.");
    }

    public static DateOnly[] Sessions() => Enumerable.Range(0, To.DayNumber - From.DayNumber + 1)
        .Select(i => From.AddDays(i)).Where(UsMarketCalendar.IsSession).ToArray();
}

public sealed record HistoricalDevelopmentSample(DateOnly Date, decimal RawClose, decimal? Momentum20);
public sealed record HistoricalDevelopmentEvidence(DateOnly From, DateOnly To, int Sessions,
    decimal FirstRawClose, decimal LastRawClose, decimal MinimumRawClose, decimal MaximumRawClose,
    ImmutableArray<HistoricalDevelopmentSample> Recent)
{
    public string Interpretation { get; } = "Retrospective raw prices; simulated session clock, not actual historical knowledge; no total-return or prospective claim.";
}
