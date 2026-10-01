using System.Text;
using System.Text.Json.Nodes;
using BigBrain.Api.Finance;
using BigBrain.Brain;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

// BB-132B pre-implementation characterization, not an autonomous-loop implementation.
// Reuses E's test-only orchestration and C's actual SQLite authority; no real model/runtime.
public sealed class ResearchLearningIterationBoundaryTests
{
    private static readonly string[] RetainedHistory = ["retained-negative-outcome"];

    [Fact]
    public async Task CompletedIterationReopensButCannotGrantAHistoryAwareSecondInvocation()
    {
        using var db = new ResearchReasonerContractTests.Database();
        var scope = ResearchLearningFixture.Scope();
        var first = new ResearchReasonerContractTests.TestOnlyInvocation(db.Enroll(scope), scope);
        var proposer = new TestOnlyReasoner(ResearchLearningFixture.Proposal(scope));
        Assert.Equal(LearningAdmissionReason.Admitted, await first.Invoke(proposer, CancellationToken.None));
        Assert.Equal(1, proposer.Calls);
        Assert.Equal(1, first.EngineCalls);

        var reopened = db.Memory();
        var before = reopened.ReadLearningLedger();
        Assert.Equal(LearningLifecycle.Completed, before.Lifecycle);
        Assert.Equal(1, before.Invocations);
        Assert.Equal(3, before.Trials);
        Assert.Equal(LearningExposure.Consumed, before.Exposure);
        Assert.Equal(first.Build!.Evaluation.EvaluationId, before.Result!.EvaluationId);
        Assert.Equal(first.Build.Evaluation.Checksum, before.Result.Checksum);
        Assert.Equal(before.Result.Checksum, reopened.RobustnessEvaluation(before.Result.EvaluationId)!.Checksum);
        Assert.NotEmpty(before.Result.Runs);

        // Restoring the scope restores the original initial projection, NOT a history projection.
        var restored = before.Scope!.Resolve();
        Assert.Equal(scope.Input, restored.Input);
        Assert.Equal(0, restored.Input.History.Evaluations);
        Assert.Equal(0, restored.Input.History.AdmittedTrials);
        Assert.Equal(LearningIdentity.Hash(Array.Empty<string>()), restored.Input.HistoryChecksum);
        Assert.Equal(1, restored.Input.Limits.ReasonerInvocations);

        // Even a second fake that would decline may not be invoked under the spent protocol.
        var next = new ResearchReasonerContractTests.TestOnlyInvocation(reopened, restored);
        var declining = new TestOnlyReasoner(ResearchLearningFixture.NoUseful(restored));
        Assert.Equal(LearningAdmissionReason.BudgetExceeded, await next.Invoke(declining, CancellationToken.None));
        Assert.Equal(0, declining.Calls);
        Assert.Equal(0, next.EngineCalls);
        var after = db.Memory().ReadLearningLedger();
        Assert.Equal(before.Commitment, after.Commitment);
        Assert.Equal(LearningIdentity.Hash(before.Result), LearningIdentity.Hash(after.Result));
        Assert.Equal(before.History, after.History.Take(before.History.Length));
        Assert.Equal(LearningAdmissionReason.BudgetExceeded, after.History[^1].Reason);
        Assert.Equal(1, after.Invocations);
        Assert.Equal(1, after.EngineStarts);
        Assert.Equal(LearningExposure.Consumed, after.Exposure);

        // Decline is not a back door to a second invocation or a refund.
        var decline = LearningAdmissionPolicy.Admit(ResearchLearningFixture.NoUseful(restored), restored,
            new(after.Submitted, after.Trials, after.EngineStarts, after.Exposure));
        Assert.Equal(LearningAdmissionReason.BudgetExceeded, decline.Reason);
    }

    [Fact]
    public void NonemptyRetainedHistoryCannotBeAdmittedAsTheInitialSyntheticProjection()
    {
        var scope = ResearchLearningFixture.Scope();
        var withHistory = SyntheticLearningScope.Create(scope.Plan, scope.Bars, scope.Features,
            scope.Facts, scope.Purpose, LearningIdentity.Hash(RetainedHistory));
        Assert.Equal(LearningAdmissionReason.UnsupportedScope, withHistory.ValidationReason);
        var response = JsonNode.Parse(ResearchLearningFixture.Proposal(scope))!;
        response["inputChecksum"] = withHistory.Input.InputChecksum;
        Assert.Equal(LearningAdmissionReason.UnsupportedScope,
            LearningAdmissionPolicy.Admit(response.ToJsonString(), withHistory, ResearchLearningFixture.Fresh).Reason);
    }

    [Theory]
    [InlineData("history", "evaluations", 1)]
    [InlineData("limits", "reasonerInvocations", 2)]
    public void ExistingWireContractRejectsInventedHistoryOrLargerInvocationAllowance(string section, string field, int value)
    {
        var input = ResearchLearningFixture.Scope().Input;
        var original = LocalReasonerProtocol.Request(input);
        Assert.Equal(input, LocalReasonerProtocol.ReadRequest(original));
        var changed = JsonNode.Parse(original)!;
        changed[section]![field] = value;
        Assert.Throws<LocalReasonerException>(() =>
            LocalReasonerProtocol.ReadRequest(Encoding.UTF8.GetBytes(changed.ToJsonString())));
    }

    [Fact]
    public void NewScopeEnrollmentCannotManufactureAnotherProgramOrFreshBudget()
    {
        using var db = new ResearchReasonerContractTests.Database();
        var scope = ResearchLearningFixture.Scope();
        var memory = db.Enroll(scope);
        Assert.True(memory.ReserveLearningInvocation());
        memory.FailLearningIteration(LearningFailure.ReasonerUnavailable);
        var before = memory.ReadLearningLedger();
        Assert.Throws<InvalidOperationException>(() =>
            db.Memory().EnrollSyntheticLearning(ResearchLearningFixture.Scope(401), LearningExposure.Unexposed));
        Assert.Equal(LearningIdentity.Hash(before), LearningIdentity.Hash(db.Memory().ReadLearningLedger()));
        Assert.False(db.Memory().ReserveLearningInvocation());
        var after = db.Memory().ReadLearningLedger();
        Assert.Equal(1, after.Invocations);
        Assert.Equal(0, after.EngineStarts);
        Assert.Contains(after.History, x => x.Failure == LearningFailure.ReasonerUnavailable);
    }

    private sealed class TestOnlyReasoner(string response) : IResearchReasoner
    {
        internal int Calls { get; private set; }
        public Task<LearningReasonerReply> ReasonAsync(LearningDevelopmentInput input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            var parsed = LearningReplyParser.Parse(response, input);
            Assert.Null(parsed.Rejection);
            return Task.FromResult(parsed.Reply!);
        }
    }
}
