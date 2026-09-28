using System.Text;

namespace BigBrain.Modules.Finance;

// Pure admission: no engine, provider, persistence, clock or network access.
public static class LearningAdmissionPolicy
{
    public const string Version = "finance-research-learning-v1";
    public static LearningFalsification Criterion => new("validation.excessReturn", "validation",
        LearningComparison.LessThanOrEqual, 0m, "fraction", AntiOverfittingPolicy.Default.Version);

    public static LearningAdmission Admit(string response, SyntheticLearningScope scope, LearningAdmissionState state)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(state);
        var parsed = LearningReplyParser.Read(response, scope.Input, discriminator =>
        {
            if (scope.ValidationReason != LearningAdmissionReason.Admitted) return scope.ValidationReason;
            if (state.SubmittedProposals < 0 || state.AdmittedTrials < 0 || state.Evaluations < 0)
                return LearningAdmissionReason.BudgetExceeded;
            if (discriminator == "NoUsefulProposal" &&
                (state.SubmittedProposals != 0 || state.AdmittedTrials != 0 || state.Evaluations != 0))
                return LearningAdmissionReason.BudgetExceeded;
            return null;
        });
        if (parsed.Rejection is { } rejection) return new(rejection);
        if (parsed.Reply is LearningReasonerReply.NoUsefulProposal) return new(LearningAdmissionReason.NoUsefulProposal);
        if (parsed.Reply is not LearningReasonerReply.Proposal proposal) return new(LearningAdmissionReason.Malformed);
        // A duplicate links an already committed identity; it never admits a new execution or exposure.
        if (state.PreviousFingerprint == scope.ExecutionFingerprint && state.PreviousProposalId is not null)
            return new(LearningAdmissionReason.Duplicate, ReusedProposalId: state.PreviousProposalId);
        if (state.Exposure != LearningExposure.Unexposed)
            return new(state.Exposure == LearningExposure.Consumed ? LearningAdmissionReason.ExposureConsumed : LearningAdmissionReason.ExposureUnknown);
        if (state.SubmittedProposals != 0 || state.AdmittedTrials != 0 || state.Evaluations != 0 ||
            state.PreviousFingerprint is not null || state.PreviousProposalId is not null)
            return new(LearningAdmissionReason.BudgetExceeded);
        return new(LearningAdmissionReason.Admitted, new(proposal.Draft, scope,
            LearningIdentity.HashBytes(Encoding.UTF8.GetBytes(response))));
    }
}
