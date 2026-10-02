using System.Collections.Concurrent;
using System.Text.RegularExpressions;
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
            cutoff, clock, port.ReasonAsync, TestContext.Current.CancellationToken, runtime.CompletionTimeout);
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
    public async Task RuntimeDerivedBudgetAllowsLegitimateReplyBeyondThirtySecondsWithoutWallClockSleep()
    {
        using var db = new ResearchReasonerContractTests.Database(); using var worker = new ProofRuntime();
        await using var runtime = worker.Create("valid", controlled180: true); // Policy only; no worker start.
        var clock = new ManualClock(); var memory = db.Memory(); var scope = ResearchLearningFixture.Scope();
        var session = memory.CreateFiniteResearchSession(scope, clock);
        var complete = new TaskCompletionSource<LearningReasonerReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0; CancellationToken received = default;
        Task<LearningReasonerReply> Reason(LearningDevelopmentInput input, CancellationToken token)
        { calls++; received = token; return complete.Task; }
        var run = memory.RunFiniteResearchIterationAsync(session.SessionId, 1, scope.Input.InputChecksum,
            clock.GetUtcNow(), clock, Reason, CancellationToken.None, runtime.CompletionTimeout);
        Assert.Equal(TimeSpan.FromSeconds(182.25), runtime.CompletionTimeout);
        clock.Advance(TimeSpan.FromSeconds(51));
        Assert.False(received.IsCancellationRequested); Assert.False(run.IsCompleted);
        complete.SetResult(LearningReplyParser.Parse(ResearchLearningFixture.NoUseful(scope), scope.Input).Reply!);
        var result = await run;
        Assert.Equal(LearningAdmissionReason.NoUsefulProposal, result.Outcome);
        Assert.Equal(result, await memory.RunFiniteResearchIterationAsync(session.SessionId, 1,
            scope.Input.InputChecksum, clock.GetUtcNow(), clock, Reason, CancellationToken.None, runtime.CompletionTimeout));
        Assert.Equal(1, calls); Assert.Equal(0, memory.ReadLearningLedger().EngineStarts); Assert.Empty(worker.Events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HungOrCancelledReasonerRetainsSpentAuthorityAndDiscardsLateReply(bool cancel)
    {
        using var db = new ResearchReasonerContractTests.Database(); using var worker = new ProofRuntime();
        await using var runtime = worker.Create("valid", controlled180: true);
        var clock = new ManualClock(); var memory = db.Memory(); var scope = ResearchLearningFixture.Scope();
        var session = memory.CreateFiniteResearchSession(scope, clock);
        using var caller = new CancellationTokenSource();
        var complete = new TaskCompletionSource<LearningReasonerReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0; CancellationToken received = default;
        Task<LearningReasonerReply> Reason(LearningDevelopmentInput input, CancellationToken token)
        { calls++; received = token; return complete.Task; }
        var run = memory.RunFiniteResearchIterationAsync(session.SessionId, 1, scope.Input.InputChecksum,
            clock.GetUtcNow(), clock, Reason, caller.Token, runtime.CompletionTimeout);
        if (cancel) caller.Cancel(); else clock.Advance(runtime.CompletionTimeout);
        var result = await run;
        Assert.True(received.IsCancellationRequested);
        Assert.Equal(cancel ? FiniteIterationFailure.Cancelled : FiniteIterationFailure.TimedOut, result.Failure);
        complete.SetResult(LearningReplyParser.Parse(ResearchLearningFixture.Proposal(scope), scope.Input).Reply!);
        Assert.Equal(result, await memory.RunFiniteResearchIterationAsync(session.SessionId, 1,
            scope.Input.InputChecksum, clock.GetUtcNow(), clock, Reason, CancellationToken.None, runtime.CompletionTimeout));
        Assert.Equal(1, calls); Assert.Equal(1, memory.ReadLearningLedger().Invocations);
        Assert.Equal(0, memory.ReadLearningLedger().EngineStarts);
        Assert.Throws<InvalidOperationException>(() => db.Memory().PreviewFiniteResearchInput(session.SessionId, 2, clock.GetUtcNow()));
    }

    [Fact]
    public async Task CallerCancellationReapsActualOwnedProofChildWithoutRefund()
    {
        using var db = new ResearchReasonerContractTests.Database(); using var worker = new ProofRuntime();
        var memory = db.Memory(); var scope = ResearchLearningFixture.Scope();
        var session = memory.CreateFiniteResearchSession(scope, TimeProvider.System);
        using var caller = new CancellationTokenSource();
        var runtime = worker.Create("hang", controlled180: true);
        try
        {
            var run = memory.RunFiniteResearchIterationAsync(session.SessionId, 1, scope.Input.InputChecksum,
                DateTimeOffset.UtcNow, TimeProvider.System, runtime.ReasonAsync, caller.Token, runtime.CompletionTimeout);
            await worker.ResponseStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            caller.Cancel();
            Assert.Equal(FiniteIterationFailure.Cancelled, (await run).Failure);
        }
        finally { await runtime.DisposeAsync(); }
        Assert.True(worker.Events.Last().WorkerCleanupRequired);
        Assert.NotNull(worker.Events.Last().WorkerExitCode);
        Assert.Equal(0, db.Memory().ReadLearningLedger().EngineStarts);
        Assert.Equal(1, db.Memory().ReadLearningLedger().Invocations);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(301)]
    public async Task InvalidOrUnboundedCompletionPolicyRejectsBeforeReservation(int seconds)
    {
        using var db = new ResearchReasonerContractTests.Database();
        var memory = db.Memory(); var scope = ResearchLearningFixture.Scope();
        var session = memory.CreateFiniteResearchSession(scope, TimeProvider.System);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => memory.RunFiniteResearchIterationAsync(
            session.SessionId, 1, scope.Input.InputChecksum, DateTimeOffset.UtcNow, TimeProvider.System,
            (_, _) => throw new InvalidOperationException("Must not invoke"), CancellationToken.None, TimeSpan.FromSeconds(seconds)));
        Assert.Empty(memory.ReadFiniteResearchSession()!.Iterations); Assert.Equal(0, memory.ReadLearningLedger().Invocations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NativePromptRendersOnlyExactFinanceProjectionWithCorrectHistorySemantics(bool hasHistory)
    {
        using var db = new ResearchReasonerContractTests.Database(); using var worker = new ProofRuntime();
        var memory = db.Memory(); var scope = ResearchLearningFixture.Scope();
        var session = memory.CreateFiniteResearchSession(scope, TimeProvider.System);
        if (hasHistory) await Invoke(memory, session.SessionId, 1, worker, "valid");
        var input = db.Memory().PreviewFiniteResearchInput(session.SessionId, hasHistory ? 2 : 1, DateTimeOffset.UtcNow);
        var json = Encoding.UTF8.GetString(LocalReasonerProtocol.Request(input));
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "contract-characterization", "worker.cpp"));
        var start = source.IndexOf("static std::string prompt(", StringComparison.Ordinal);
        var end = source.IndexOf("// Generation constraint", start, StringComparison.Ordinal);
        var body = source[start..end];
        // Evaluate only the actual two literal strings + exact input expression, no parallel template.
        var match = Regex.Match(body, "return R\"\\((.*?)\\)\"\\+input\\+R\"\\((.*?)\\)\";", RegexOptions.Singleline);
        Assert.True(match.Success);
        var prompt = match.Groups[1].Value + json + match.Groups[2].Value;
        Assert.Contains("Absent finiteHistory means empty initial research history.", prompt, StringComparison.Ordinal);
        Assert.Contains("Present finiteHistory contains only Finance-authorized prior outcome", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("Initial history is empty: no previous", prompt, StringComparison.Ordinal);
        Assert.Contains(json, prompt, StringComparison.Ordinal);
        Assert.Equal(hasHistory, input.FiniteHistory is not null);
        if (hasHistory)
        {
            Assert.Equal(LearningAdmissionReason.Admitted, input.FiniteHistory!.PreviousOutcome);
            Assert.Equal(0, input.FiniteHistory.RemainingScientificEvaluations);
            var native = memory.ReadLearningLedger().Result!;
            Assert.DoesNotContain(native.EvaluationId, prompt, StringComparison.Ordinal);
            Assert.DoesNotContain(native.Checksum, prompt, StringComparison.Ordinal);
            Assert.DoesNotContain("holdout", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("selection", json, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 2, 0, 0, 0, TimeSpan.Zero);
        private readonly List<ManualTimer> _timers = [];
        public override DateTimeOffset GetUtcNow() => _now;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Assert.Equal(Timeout.InfiniteTimeSpan, period);
            var timer = new ManualTimer(this, callback, state); timer.Change(dueTime, period); _timers.Add(timer); return timer;
        }
        internal void Advance(TimeSpan delta)
        { _now += delta; foreach (var timer in _timers.ToArray()) timer.Fire(); }
        private sealed class ManualTimer(ManualClock clock, TimerCallback callback, object? state) : ITimer
        {
            private DateTimeOffset? _due;
            public bool Change(TimeSpan dueTime, TimeSpan period)
            { _due = dueTime == Timeout.InfiniteTimeSpan ? null : clock._now + dueTime; return true; }
            internal void Fire() { if (_due is { } due && due <= clock._now) { _due = null; callback(state); } }
            public void Dispose() => _due = null;
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
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
            cutoff, clock, port.ReasonAsync, TestContext.Current.CancellationToken, runtime.CompletionTimeout);
    }

    private sealed class ProofRuntime : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "bb132c-proof-" + Guid.NewGuid().ToString("N"));
        internal ConcurrentQueue<LocalReasonerAudit> Events { get; } = new();
        internal TaskCompletionSource ResponseStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
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
        }, record => { Events.Enqueue(record); if (record.Phase == LocalReasonerAuditPhase.ResponseStarted) ResponseStarted.TrySetResult(); });
        public void Dispose() => Directory.Delete(_root, true);
    }
}
