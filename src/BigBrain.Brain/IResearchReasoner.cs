using BigBrain.Modules.Finance;

namespace BigBrain.Brain;

// Provider-neutral contract; no API registration, endpoint or capability dependencies.
// Input must be Finance's authorized initial development projection after invocation reservation.
// Replies remain untrusted: current Finance admission/reservation is mandatory before computation.
// Cancellation propagates as OperationCanceledException; operational/parse failures are exceptions,
// never a scientific reply. Callers must discard late replies, retain spent governance, and not retry
// or fall back. The local F controller enforces owned-worker termination and bounded transport;
// native sandboxing/hard resource limits remain separately reviewed first-model gates.
public interface IResearchReasoner
{
    Task<LearningReasonerReply> ReasonAsync(LearningDevelopmentInput input, CancellationToken cancellationToken);
}
