using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using BigBrain.Api.Finance;
using BigBrain.Brain;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

// Model-free compatibility evidence for C. Uses the existing production session/controller,
// with F's deterministic child; never starts the native worker, libllama or a model.
public sealed class FiniteLocalReasonerIntegrationTests
{
    [Theory]
    [InlineData("valid", LearningAdmissionReason.BudgetExceeded)]
    [InlineData("no-useful", LearningAdmissionReason.NoUsefulProposal)]
    public async Task ExistingSessionBindsRuntimeThroughPortAndReopensHistoryWithoutSecondEvaluation(
        string secondMode, LearningAdmissionReason secondOutcome)
    {
        using var db = new ResearchReasonerContractTests.Database();
        using var worker = new ProofRuntime();
        var memory = db.Memory();
        var scope = ResearchLearningFixture.Scope();
        var clock = TimeProvider.System;
        var session = memory.CreateFiniteResearchSession(scope, clock);
        var n = await Invoke(memory, session.SessionId, 1, worker, "valid");
        Assert.Equal(LearningAdmissionReason.Admitted, n.Outcome);
        var scientific = memory.ReadLearningLedger();
        Assert.Equal(1, scientific.EngineStarts);
        Assert.Equal(3, scientific.Trials);
        Assert.InRange(scientific.UniqueRuns, 1, 64);
        Assert.Equal(LearningExposure.Consumed, scientific.Exposure);
        Assert.NotEmpty(scientific.Result!.Runs);

        var reopened = db.Memory();
        var native = reopened.RobustnessEvaluation(scientific.Result.EvaluationId)!;
        Assert.Equal(scientific.Result.Checksum, native.Checksum);
        var cutoff = clock.GetUtcNow();
        var input = reopened.PreviewFiniteResearchInput(session.SessionId, 2, cutoff);
        Assert.Equal(native.PrimarySplit.Test.ExcessReturn, input.FiniteHistory!.ReferenceValidationExcessReturn);
        Assert.Equal(0, input.FiniteHistory.RemainingScientificEvaluations);
        Assert.Equal(LearningAdmissionReason.Admitted, input.FiniteHistory.PreviousOutcome);
        var bytes = LocalReasonerProtocol.Request(input);
        Assert.Equal(input, LocalReasonerProtocol.ReadRequest(bytes));
        Assert.DoesNotContain(scientific.Result.EvaluationId, Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
        Assert.DoesNotContain(scientific.Result.Checksum, Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);

        await using var runtime = worker.Create(secondMode);
        IResearchReasoner port = runtime;
        var next = await reopened.RunFiniteResearchIterationAsync(session.SessionId, 2, input.InputChecksum,
            cutoff, clock, port.ReasonAsync, TestContext.Current.CancellationToken);
        Assert.Equal(secondOutcome, next.Outcome);
        var started = worker.Events.Where(x => x.Phase == LocalReasonerAuditPhase.Started).ToArray();
        Assert.Equal(2, started.Length);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), started[1].InputHash);
        Assert.Equal(LearningIdentity.Hash(scientific), LearningIdentity.Hash(db.Memory().ReadLearningLedger()));
        var persisted = db.Memory().ReadFiniteResearchSession()!;
        Assert.Equal(2, persisted.Iterations.Length);
        Assert.Equal(LearningIdentity.Hash(input), LearningIdentity.Hash(persisted.Iterations[1].Input));
        Assert.Equal(next, await db.Memory().RunFiniteResearchIterationAsync(session.SessionId, 2,
            input.InputChecksum, cutoff, clock, port.ReasonAsync, TestContext.Current.CancellationToken));
        Assert.Equal(2, worker.Events.Count(x => x.Phase == LocalReasonerAuditPhase.Started));
        Assert.Throws<InvalidOperationException>(() => db.Memory().PreviewFiniteResearchInput(session.SessionId, 3, clock.GetUtcNow()));
        Assert.Equal("NONE", scientific.Result.ExecutionAuthority);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("exit")]
    [InlineData("hang")]
    public async Task RuntimeFailureSpendsSessionInvocationAndCannotProgressOrRetry(string mode)
    {
        using var db = new ResearchReasonerContractTests.Database();
        using var worker = new ProofRuntime();
        var memory = db.Memory();
        var session = memory.CreateFiniteResearchSession(ResearchLearningFixture.Scope(), TimeProvider.System);
        var n = await Invoke(memory, session.SessionId, 1, worker, mode);
        Assert.Equal(FiniteIterationState.Failed, n.State);
        Assert.Equal(0, db.Memory().ReadLearningLedger().EngineStarts);
        Assert.Equal(1, db.Memory().ReadLearningLedger().Invocations);
        Assert.Throws<InvalidOperationException>(() => db.Memory().PreviewFiniteResearchInput(session.SessionId, 2, DateTimeOffset.UtcNow));
        Assert.Equal(n, await Invoke(db.Memory(), session.SessionId, 1, worker, "valid"));
        Assert.Single(worker.Events, x => x.Phase == LocalReasonerAuditPhase.Started);
        var terminal = worker.Events.Last();
        Assert.Equal(LocalReasonerAuditPhase.Completed, terminal.Phase);
        Assert.NotNull(terminal.WorkerExitCode);
        if (mode == "malformed") Assert.Equal(LearningAdmissionReason.Malformed, terminal.ReplyRejection);
    }

    [Fact]
    public async Task FiniteSessionThirtySecondDeadlineStillCancelsAControlled180SecondRuntime()
    {
        using var db = new ResearchReasonerContractTests.Database();
        using var worker = new ProofRuntime();
        var memory = db.Memory();
        var scope = ResearchLearningFixture.Scope();
        var clock = TimeProvider.System;
        var session = memory.CreateFiniteResearchSession(scope, clock);
        Assert.Equal(30, scope.Input.Limits.ReasonerDeadlineSeconds);
        var runtime = worker.Create("hang", controlled180: true);
        var watch = Stopwatch.StartNew();
        FiniteResearchIteration n;
        try
        {
            IResearchReasoner port = runtime;
            n = await memory.RunFiniteResearchIterationAsync(session.SessionId, 1, scope.Input.InputChecksum,
                clock.GetUtcNow(), clock, port.ReasonAsync, TestContext.Current.CancellationToken);
        }
        finally { await runtime.DisposeAsync(); } // Wait for actual owned-child cleanup, not just the session's WaitAsync.
        Assert.Equal(FiniteIterationFailure.TimedOut, n.Failure);
        Assert.InRange(watch.Elapsed.TotalSeconds, 29, 60);
        var terminal = worker.Events.Last();
        Assert.Equal(LocalReasonerOutcome.Cancelled, terminal.Outcome); // Parent's shorter token wins.
        Assert.True(terminal.WorkerCleanupRequired);
        Assert.NotNull(terminal.WorkerExitCode);
        Assert.Equal(0, db.Memory().ReadLearningLedger().EngineStarts);
        Assert.Single(db.Memory().ReadFiniteResearchSession()!.Iterations);
        Assert.Throws<InvalidOperationException>(() => db.Memory().PreviewFiniteResearchInput(session.SessionId, 2, clock.GetUtcNow()));
    }

    [Fact]
    public void AcceptedNativePromptAssertsEmptyHistoryUnconditionally()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "contract-characterization", "worker.cpp"));
        var start = source.IndexOf("static std::string prompt(", StringComparison.Ordinal);
        var end = source.IndexOf("// Generation constraint", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        var prompt = source[start..end];
        Assert.Contains("Initial history is empty: no previous\nexperiments/results exist in this projection.", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("finiteHistory", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("if(", prompt, StringComparison.Ordinal);
        Assert.Contains(")\"+input+R\"(", prompt, StringComparison.Ordinal);
        // Source evidence only: no claim about how Qwen would resolve contradictory instructions.
    }

    private static async Task<FiniteResearchIteration> Invoke(EodhdMarketMemory memory, string sessionId,
        int iteration, ProofRuntime worker, string mode)
    {
        var clock = TimeProvider.System;
        var cutoff = clock.GetUtcNow();
        var input = memory.PreviewFiniteResearchInput(sessionId, iteration, cutoff);
        await using var runtime = worker.Create(mode);
        IResearchReasoner port = runtime;
        return await memory.RunFiniteResearchIterationAsync(sessionId, iteration, input.InputChecksum,
            cutoff, clock, port.ReasonAsync, TestContext.Current.CancellationToken);
    }

    private sealed class ProofRuntime : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "bb132c-proof-" + Guid.NewGuid().ToString("N"));
        internal ConcurrentQueue<LocalReasonerAudit> Events { get; } = new();
        internal ProofRuntime()
        {
            if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
            Directory.CreateDirectory(_root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        internal LocalReasonerRuntime Create(string mode, bool controlled180 = false) => new(new()
        {
            Enabled = true,
            WorkerExecutable = Path.Combine(AppContext.BaseDirectory, "reasoner-proof", "BigBrain.Reasoner.ProofWorker"),
            WorkerArguments = [mode],
            CoordinationDirectory = _root,
            ControlledRealModelAcceptance = controlled180,
            InvocationTimeout = TimeSpan.FromSeconds(controlled180 ? 180 : mode == "hang" ? 2 : 10)
        }, Events.Enqueue);
        public void Dispose() => Directory.Delete(_root, true);
    }
}
