namespace BigBrain.Modules.Finance;

// One preceding iteration only. No arbitrary text, IDs, result objects or collections.
// The fixed reference momentum20 validation return is development evidence, NOT the
// selected candidate's return, a robustness verdict, holdout outcome or trading signal.
public sealed record FiniteResearchHistory
{
    public FiniteResearchHistory(DateTimeOffset knowledgeCutoffUtc, DateTimeOffset availableAtUtc,
        LearningAdmissionReason previousOutcome, decimal? referenceValidationExcessReturn)
    {
        if (knowledgeCutoffUtc.Offset != TimeSpan.Zero || availableAtUtc.Offset != TimeSpan.Zero ||
            availableAtUtc > knowledgeCutoffUtc || !Enum.IsDefined(previousOutcome) ||
            (previousOutcome != LearningAdmissionReason.Admitted && referenceValidationExcessReturn is not null))
            throw new ArgumentException("Invalid finite research history.");
        KnowledgeCutoffUtc = knowledgeCutoffUtc;
        AvailableAtUtc = availableAtUtc;
        PreviousOutcome = previousOutcome;
        ReferenceValidationExcessReturn = referenceValidationExcessReturn;
    }
    public string Version { get; } = "finance-finite-history-v1";
    public int PreviousIteration { get; } = 1;
    public int RemainingScientificEvaluations { get; }
    public DateTimeOffset KnowledgeCutoffUtc { get; }
    public DateTimeOffset AvailableAtUtc { get; }
    public LearningAdmissionReason PreviousOutcome { get; }
    public decimal? ReferenceValidationExcessReturn { get; }
}
