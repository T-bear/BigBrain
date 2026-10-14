using System.Collections.Immutable;

namespace BigBrain.Modules.Finance;

// A new explicit finite authority, never a renewal of an A/B/C grant.
public sealed record RecurringResearchBudget(int Invocations, int Evaluations, int Trials,
    int UnderlyingCalls, int ReservedReasonerSeconds, int CompletionSeconds);
public sealed record RecurringResearchCohort(DateOnly From, DateOnly To, DateTimeOffset DueAtUtc);
public sealed record RecurringResearchAuthorization(string OwnerDecision, string EligibilityEvidence,
    CanonicalInstrument Instrument, ProviderInstrumentMapping Mapping, ResearchDatasetFacts Facts,
    string ReasonerIdentity, DateTimeOffset ExpiresAtUtc, RecurringResearchBudget Budget,
    ImmutableArray<RecurringResearchCohort> Cohorts)
{
    public const string Version = "finance-recurring-research-v1";
    public void Validate(DateTimeOffset registered)
    {
        FinanceTime.RequireUtc(registered, nameof(registered));
        FinanceTime.RequireUtc(ExpiresAtUtc, nameof(ExpiresAtUtc));
        foreach (var text in new[] { OwnerDecision, EligibilityEvidence, ReasonerIdentity })
            if (string.IsNullOrWhiteSpace(text) || text.Length > 256) throw new InvalidOperationException("Explicit bounded research authority required.");
        if (ExpiresAtUtc <= registered || Budget.Invocations is < 1 or > 2 || Budget.Evaluations < 1 ||
            Budget.Evaluations > Budget.Invocations || Budget.Trials < 3 || Budget.Trials > 6 ||
            Budget.UnderlyingCalls is < 64 or > 128 || Budget.CompletionSeconds is < 1 or > 300 ||
            Budget.ReservedReasonerSeconds < Budget.CompletionSeconds || Budget.ReservedReasonerSeconds > 600 ||
            Cohorts.IsDefaultOrEmpty || Cohorts.Length > 2 || Budget.Evaluations > Cohorts.Length)
            throw new InvalidOperationException("Invalid finite research budget.");
        _ = new InstrumentMappingCatalog([Instrument], [Mapping]);
        if (Instrument.Type != InstrumentType.Equity || Instrument.Currency.Code != "USD" ||
            Mapping.Provider.Value != "Alpaca" || Mapping.ProviderDataset.Value != DailyMarketEvidence.Dataset)
            throw new InvalidOperationException("Unsupported real research source.");
        if (Facts.DatasetClass != ResearchDatasetClass.DailyOhlcv || Facts.OwnerDecision != DatasetOwnerRightsDecision.ApprovedByOwner ||
            new[] { Facts.ExternalRights, Facts.TechnicalIntegrity, Facts.HistoricalIdentity, Facts.PriceBasis, Facts.CorporateActions }.Any(x => x != DatasetEvidenceResult.Pass) ||
            new[] { ResearchDatasetPurpose.TrainValidationHoldout, ResearchDatasetPurpose.TechnicalSignalResearch,
                ResearchDatasetPurpose.BoundedHistoricalBacktest, ResearchDatasetPurpose.RobustnessSensitivity,
                ResearchDatasetPurpose.WalkForward }.Any(p => ResearchDatasetEligibilityPolicyV1.Evaluate(Facts).For(p).State != ResearchEligibilityState.Eligible))
            throw new InvalidOperationException("Explicit eligible scientific evidence required.");
        DateOnly? previous = null; DateTimeOffset? previousDue = null;
        foreach (var cohort in Cohorts)
        {
            FinanceTime.RequireUtc(cohort.DueAtUtc, nameof(Cohorts));
            var sessions = Sessions(cohort);
            if (sessions.Length is < 300 or > 500 || sessions[0] != cohort.From || sessions[^1] != cohort.To ||
                previous >= cohort.From || previousDue >= cohort.DueAtUtc || cohort.DueAtUtc <= registered || cohort.DueAtUtc >= ExpiresAtUtc ||
                !Mapping.IsValidOn(cohort.From) || !Mapping.IsValidOn(cohort.To))
                throw new InvalidOperationException("Cohorts must be bounded, ordered, disjoint and explicitly scheduled.");
            var plan = DeterministicRobustnessEvaluator.CreatePlan(["pending"], "pending", new MomentumResearchStrategy(), [Instrument.Id.Value], cohort.From, cohort.To);
            var partition = DeterministicRobustnessEvaluator.CreatePartition(sessions, plan.AntiOverfitting!);
            if (UsMarketCalendar.Session(partition.HoldoutFrom)!.OpenUtc <= registered ||
                cohort.DueAtUtc <= UsMarketCalendar.Session(cohort.To)!.CloseUtc)
                throw new InvalidOperationException("Holdout must begin after program registration; schedule must follow source coverage.");
            previous = cohort.To; previousDue = cohort.DueAtUtc;
        }
    }
    public static DateOnly[] Sessions(RecurringResearchCohort cohort) =>
        Enumerable.Range(0, Math.Clamp(cohort.To.DayNumber - cohort.From.DayNumber + 1, 0, 1001))
            .Select(i => cohort.From.AddDays(i)).Where(UsMarketCalendar.IsSession).ToArray();
}

// Only fixed reference validation outcome and operational facts may cross the model boundary.
public sealed record RecurringResearchHistoryEntry(int Iteration, string Outcome, DateTimeOffset AvailableAtUtc,
    decimal? ReferenceValidationExcessReturn);
public sealed record RecurringResearchHistory(string Version, ImmutableArray<RecurringResearchHistoryEntry> Entries);
