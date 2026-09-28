using BigBrain.Modules.Finance;

namespace BigBrain.Brain;

// Contract only; no production adapter, registration, endpoint or capability dependencies.
// Input must be Finance's authorized initial development projection after invocation reservation.
// Replies remain untrusted: current Finance admission/reservation is mandatory before computation.
// Cancellation propagates as OperationCanceledException; operational/parse failures are exceptions,
// never a scientific reply. Callers must discard late replies, retain spent governance, and not retry
// or fall back. Worker termination/resource enforcement is a separate future isolation gate.
public interface IResearchReasoner
{
    Task<LearningReasonerReply> ReasonAsync(LearningDevelopmentInput input, CancellationToken cancellationToken);
}
