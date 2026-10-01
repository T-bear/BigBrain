using System.Text.Json;
using System.Text.Json.Nodes;
using BigBrain.Api.Finance;
using BigBrain.Brain;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Tests;

public sealed class FiniteResearchSessionTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly int[] Periods = [5, 10, 20];
    private static readonly string[] ForbiddenHistoryFields = ["holdout", "verdict", "selection", "evaluationId", "runId", "proposalId", "executionFingerprint", "score"];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EvaluatedNReopensAndNPlusOneActuallyReasonsWithoutAnotherEvaluation(bool proposesAgain)
    {
        using var db = new Database(); var memory = db.Memory(); var scope = ResearchLearningFixture.Scope(); var clock = new Clock();
        Assert.Null(memory.ReadFiniteResearchSession());
        var session = memory.CreateFiniteResearchSession(scope, clock);
        Assert.Equal(FiniteResearchGrant.Initial, session.Grant);
        var first = new Fake(input => Proposal(scope, input));
        var n = await Invoke(memory, session, 1, clock, first);
        Assert.Equal(LearningAdmissionReason.Admitted, n.Outcome);
        Assert.Equal(1, first.Calls);
        var scientific = memory.ReadLearningLedger();
        Assert.Equal(1, scientific.EngineStarts); Assert.Equal(3, scientific.Trials);
        Assert.Equal(LearningExposure.Consumed, scientific.Exposure);
        Assert.InRange(scientific.UniqueRuns, 1, 64);
        Assert.NotEmpty(scientific.Result!.Runs);
        var evaluation = memory.RobustnessEvaluation(scientific.Result.EvaluationId)!;
        Assert.Equal(scientific.Result.Checksum, evaluation.Checksum);
        using (var c = db.Open()) Assert.Equal(scientific.UniqueRuns, Convert.ToInt32(Scalar(c, "SELECT COUNT(*) FROM backtest_runs"), System.Globalization.CultureInfo.InvariantCulture));

        clock.Now = clock.Now.AddMinutes(1);
        var reopened = db.Memory();
        var preview = reopened.PreviewFiniteResearchInput(session.SessionId, 2, clock.Now);
        Assert.Equal(evaluation.PrimarySplit.Test.ExcessReturn, preview.FiniteHistory!.ReferenceValidationExcessReturn);
        Assert.Equal(LearningAdmissionReason.Admitted, preview.FiniteHistory.PreviousOutcome);
        Assert.Equal(0, preview.FiniteHistory.RemainingScientificEvaluations);
        Assert.Equal(n.AvailableAtUtc, preview.FiniteHistory.AvailableAtUtc);
        Assert.Equal(LearningIdentity.Hash(preview.FiniteHistory), preview.HistoryChecksum);
        Assert.NotEqual(scope.Input.InputChecksum, preview.InputChecksum);
        Assert.DoesNotContain(scientific.Result.Checksum, JsonSerializer.Serialize(preview));
        foreach (var forbidden in ForbiddenHistoryFields)
            Assert.DoesNotContain(forbidden, JsonSerializer.Serialize(preview), StringComparison.OrdinalIgnoreCase);
        var second = new Fake(input =>
        {
            Assert.Equal(LearningIdentity.Hash(preview), LearningIdentity.Hash(input));
            Assert.NotNull(input.FiniteHistory);
            return proposesAgain ? Proposal(scope, input) : Decline(scope, input);
        });
        var next = await Invoke(reopened, session, 2, clock, second);
        Assert.Equal(proposesAgain ? LearningAdmissionReason.BudgetExceeded : LearningAdmissionReason.NoUsefulProposal, next.Outcome);
        Assert.Equal(1, second.Calls);
        Assert.Equal(LearningIdentity.Hash(scientific), LearningIdentity.Hash(db.Memory().ReadLearningLedger()));
        var reopenedState = db.Memory().ReadFiniteResearchSession()!;
        Assert.Equal(2, reopenedState.Iterations.Length);
        Assert.Equal(LearningIdentity.Hash(preview), LearningIdentity.Hash(db.Memory().PreviewFiniteResearchInput(session.SessionId, 2, clock.Now)));
        var mustNotRun = new Fake(_ => throw new InvalidOperationException("Replay must not call a reasoner"));
        var replay = await Invoke(db.Memory(), session, 2, clock, mustNotRun);
        Assert.Equal(next, replay); Assert.Equal(0, mustNotRun.Calls);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Invoke(db.Memory(), session, 3, clock, mustNotRun));
        Assert.Equal(0, mustNotRun.Calls);
        Assert.Equal(LearningIdentity.Hash(reopenedState), LearningIdentity.Hash(memory.CreateFiniteResearchSession(scope, clock)));
    }

    [Fact]
    public async Task ProtectedHoldoutChangesNativeEvidenceButNotVisibleHistoryOrIdentity()
    {
        var original = ResearchLearningFixture.Scope();
        var partition = DeterministicRobustnessEvaluator.CreatePartition(original.Bars.Select(x => x.SessionDate).ToArray(), original.Plan.AntiOverfitting!);
        var bars = original.Bars.Select((bar, index) => bar.SessionDate < partition.HoldoutFrom ? bar :
            bar with { Open = bar.Open + index * 7m, Close = bar.Close + index * 8m }).ToArray();
        var features = bars.SelectMany((bar, index) => Periods.Select(period => new BacktestFeatureValue(bar.InstrumentId,
            bar.SessionDate, $"momentum.{period}", index < period ? null : bar.Close - bars[index - period].Close,
            bar.KnowledgeTimeUtc, original.Plan.FeatureRevisionId))).ToArray();
        var changed = SyntheticLearningScope.Create(original.Plan, bars, features, original.Facts, original.Purpose, original.Input.HistoryChecksum);
        Assert.Equal(LearningAdmissionReason.Admitted, changed.ValidationReason);
        Assert.Equal(original.Input.InputChecksum, changed.Input.InputChecksum);
        Assert.NotEqual(original.ExecutionFingerprint, changed.ExecutionFingerprint);
        using var a = new Database(); using var b = new Database(); var clock = new Clock();
        var ma = a.Memory(); var mb = b.Memory();
        var sa = ma.CreateFiniteResearchSession(original, clock); var sb = mb.CreateFiniteResearchSession(changed, clock);
        await Invoke(ma, sa, 1, clock, new Fake(input => Proposal(original, input)));
        await Invoke(mb, sb, 1, clock, new Fake(input => Proposal(changed, input)));
        var ra = ma.ReadLearningLedger().Result!; var rb = mb.ReadLearningLedger().Result!;
        Assert.NotEqual(ra.Checksum, rb.Checksum);
        Assert.NotEqual(ma.RobustnessEvaluation(ra.EvaluationId)!.SelectionGovernance!.SelectedHoldoutNetReturn,
            mb.RobustnessEvaluation(rb.EvaluationId)!.SelectionGovernance!.SelectedHoldoutNetReturn);
        var pa = ma.PreviewFiniteResearchInput(sa.SessionId, 2, clock.Now);
        var pb = mb.PreviewFiniteResearchInput(sb.SessionId, 2, clock.Now);
        Assert.Equal(JsonSerializer.Serialize(pa), JsonSerializer.Serialize(pb));
        Assert.Equal(pa.HistoryChecksum, pb.HistoryChecksum);
        Assert.Equal(pa.InputChecksum, pb.InputChecksum);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExistingV1EnrollmentOrSpentAuthorityCannotAcquireGrant(bool spent)
    {
        using var db = new Database(); var scope = ResearchLearningFixture.Scope(); var memory = db.Memory();
        memory.EnrollSyntheticLearning(scope, LearningExposure.Unexposed);
        if (spent) { Assert.True(memory.ReserveLearningInvocation()); memory.FailLearningIteration(); }
        var before = LearningIdentity.Hash(memory.ReadLearningLedger());
        Assert.Throws<InvalidOperationException>(() => memory.CreateFiniteResearchSession(scope, new Clock()));
        Assert.Null(db.Memory().ReadFiniteResearchSession());
        Assert.Equal(before, LearningIdentity.Hash(db.Memory().ReadLearningLedger()));
    }

    [Fact]
    public void ExplicitCreationRollsBackAtomicallyAndReopenDoesNotEnroll()
    {
        using var db = new Database(); var memory = db.Memory(); var before = LearningIdentity.Hash(memory.ReadLearningLedger());
        memory.FiniteSessionTestHook = stage => { if (stage == "before-create-commit") throw new InvalidOperationException("crash"); };
        Assert.Throws<InvalidOperationException>(() => memory.CreateFiniteResearchSession(ResearchLearningFixture.Scope(), new Clock()));
        Assert.Null(db.Memory().ReadFiniteResearchSession());
        Assert.Equal(before, LearningIdentity.Hash(db.Memory().ReadLearningLedger()));
    }

    [Theory]
    [InlineData("before-invocation-commit", 0)]
    [InlineData("after-invocation-commit", 1)]
    public async Task CrashReservationCommitBoundaryNeverRefundsCommittedInvocation(string point, int expected)
    {
        using var db = new Database(); var memory = db.Memory(); var scope = ResearchLearningFixture.Scope(); var clock = new Clock();
        var session = memory.CreateFiniteResearchSession(scope, clock); var fake = new Fake(input => Proposal(scope, input));
        memory.FiniteSessionTestHook = stage => { if (stage == point) throw new InvalidOperationException("crash"); };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Invoke(memory, session, 1, clock, fake));
        var reopened = db.Memory();
        Assert.Equal(expected, reopened.ReadFiniteResearchSession()!.Iterations.Length); Assert.Equal(0, fake.Calls);
        Assert.Equal(expected, reopened.ReadLearningLedger().Invocations);
        if (expected == 1)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => Invoke(reopened, session, 1, clock, fake));
            reopened.MarkFiniteResearchIndeterminate(session.SessionId, clock);
            Assert.Equal(FiniteIterationState.Indeterminate, reopened.ReadFiniteResearchSession()!.Iterations[0].State);
            await Assert.ThrowsAsync<InvalidOperationException>(() => Invoke(reopened, session, 2, clock, fake));
            Assert.Equal(0, fake.Calls);
        }
    }

    [Fact]
    public async Task CrashAfterNativePersistenceRetainsResultsAndNoFreshAuthority()
    {
        using var db = new Database(); var memory = db.Memory(); var scope = ResearchLearningFixture.Scope(); var clock = new Clock();
        var session = memory.CreateFiniteResearchSession(scope, clock);
        memory.FiniteSessionTestHook = stage => { if (stage == "after-results-before-binding") throw new InvalidOperationException("crash"); };
        var result = await Invoke(memory, session, 1, clock, new Fake(input => Proposal(scope, input)));
        Assert.Equal(FiniteIterationFailure.ExecutionOrPersistence, result.Failure);
        var legacy = db.Memory().ReadLearningLedger();
        Assert.Equal(LearningLifecycle.Indeterminate, legacy.Lifecycle); Assert.Equal(1, legacy.EngineStarts);
        Assert.Equal(LearningExposure.Consumed, legacy.Exposure);
        using var c = db.Open(); Assert.Equal(1L, Scalar(c, "SELECT COUNT(*) FROM robustness_evaluations"));
        var fake = new Fake(_ => throw new InvalidOperationException());
        Assert.Equal(result, await Invoke(db.Memory(), session, 1, clock, fake));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Invoke(db.Memory(), session, 2, clock, fake));
        Assert.Equal(0, fake.Calls);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("unsupported")]
    [InlineData("throw")]
    [InlineData("cancel")]
    public async Task UntrustedOrFailedReasonerNeverExecutesOrRestoresAuthority(string mode)
    {
        using var db = new Database(); var memory = db.Memory(); var scope = ResearchLearningFixture.Scope(); var clock = new Clock();
        var session = memory.CreateFiniteResearchSession(scope, clock); using var cts = new CancellationTokenSource();
        var fake = new Fake(input => mode switch
        {
            "malformed" => "{",
            "unsupported" => Proposal(scope, input).Replace("momentum", "unsupported", StringComparison.Ordinal),
            "cancel" => Cancel(),
            _ => throw new InvalidOperationException("not persisted")
        });
        string Cancel() { cts.Cancel(); return Decline(scope, scope.Input); }
        var result = await InvokeWithCancellation(memory, session, 1, clock, fake, cts.Token);
        Assert.Equal(FiniteIterationState.Failed, result.State); Assert.Equal(1, fake.Calls);
        Assert.Equal(1, db.Memory().ReadLearningLedger().Invocations); Assert.Equal(0, db.Memory().ReadLearningLedger().EngineStarts);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Invoke(db.Memory(), session, 2, clock, fake));
        Assert.Equal(1, fake.Calls);
    }

    [Fact]
    public async Task NegativeHistoryIsVisibleWithoutInventingEvaluationOrAllowingSecondOne()
    {
        using var db = new Database(); var memory = db.Memory(); var scope = ResearchLearningFixture.Scope(); var clock = new Clock();
        var session = memory.CreateFiniteResearchSession(scope, clock);
        var first = await Invoke(memory, session, 1, clock, new Fake(input => Decline(scope, input)));
        Assert.Equal(LearningAdmissionReason.NoUsefulProposal, first.Outcome);
        var second = new Fake(input =>
        {
            Assert.Equal(LearningAdmissionReason.NoUsefulProposal, input.FiniteHistory!.PreviousOutcome);
            Assert.Null(input.FiniteHistory.ReferenceValidationExcessReturn);
            return Proposal(scope, input);
        });
        Assert.Equal(LearningAdmissionReason.BudgetExceeded, (await Invoke(db.Memory(), session, 2, clock, second)).Outcome);
        Assert.Equal(1, second.Calls); Assert.Equal(0, db.Memory().ReadLearningLedger().EngineStarts);
    }

    [Fact]
    public async Task TypedReplyFromAnotherInputStillRejectsAgainstFrozenHistory()
    {
        using var db = new Database(); var memory = db.Memory(); var scope = ResearchLearningFixture.Scope(); var clock = new Clock();
        var session = memory.CreateFiniteResearchSession(scope, clock);
        var other = scope.Input with { InputChecksum = LearningIdentity.Hash("different-history") };
        var foreign = LearningReplyParser.Parse(Decline(scope, other), other).Reply!;
        var result = await memory.RunFiniteResearchIterationAsync(session.SessionId, 1, scope.Input.InputChecksum,
            clock.Now, clock, (_, _) => Task.FromResult(foreign), CancellationToken.None);
        Assert.Equal(LearningAdmissionReason.EvidenceMismatch, result.Outcome);
        Assert.Equal(0, memory.ReadLearningLedger().EngineStarts);
        Assert.Equal(LearningAdmissionReason.EvidenceMismatch,
            db.Memory().PreviewFiniteResearchInput(session.SessionId, 2, clock.Now).FiniteHistory!.PreviousOutcome);
    }

    [Fact]
    public async Task StaleHistoryAndFutureOrUnavailableCutoffsFailBeforeInvocation()
    {
        using var db = new Database(); var memory = db.Memory(); var scope = ResearchLearningFixture.Scope(); var clock = new Clock();
        var session = memory.CreateFiniteResearchSession(scope, clock); var fake = new Fake(input => Proposal(scope, input));
        await Assert.ThrowsAsync<InvalidOperationException>(() => memory.RunFiniteResearchIterationAsync(session.SessionId, 1,
            "invented", clock.Now, clock, fake.ReasonAsync, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => memory.RunFiniteResearchIterationAsync(session.SessionId, 1,
            scope.Input.InputChecksum, clock.Now.AddDays(1), clock, fake.ReasonAsync, CancellationToken.None));
        Assert.Equal(0, fake.Calls); Assert.Empty(memory.ReadFiniteResearchSession()!.Iterations);
        clock.Now = clock.Now.AddMinutes(1);
        await Invoke(memory, session, 1, clock, fake);
        Assert.Throws<InvalidOperationException>(() => db.Memory().PreviewFiniteResearchInput(session.SessionId, 2, session.CreatedAtUtc));
        var next = new Fake(input => Decline(scope, input));
        await Assert.ThrowsAsync<InvalidOperationException>(() => memory.RunFiniteResearchIterationAsync(session.SessionId, 2,
            scope.Input.InputChecksum, clock.Now, clock, next.ReasonAsync, CancellationToken.None));
        Assert.Equal(0, next.Calls);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task IndependentConnectionsCannotInvokeTheSameRemainingSlotConcurrently(int iteration)
    {
        using var db = new Database(); var memory = db.Memory(); var scope = ResearchLearningFixture.Scope(); var clock = new Clock();
        var session = memory.CreateFiniteResearchSession(scope, clock);
        if (iteration == 2) await Invoke(memory, session, 1, clock, new Fake(input => Proposal(scope, input)));
        var input = memory.PreviewFiniteResearchInput(session.SessionId, iteration, clock.Now);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        async Task<LearningReasonerReply> Reason(LearningDevelopmentInput projected, CancellationToken token)
        {
            Interlocked.Increment(ref calls); entered.TrySetResult();
            await release.Task.WaitAsync(token);
            return LearningReplyParser.Parse(Decline(scope, projected), projected).Reply!;
        }
        var active = memory.RunFiniteResearchIterationAsync(session.SessionId, iteration, input.InputChecksum, clock.Now, clock, Reason, CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.Memory().RunFiniteResearchIterationAsync(session.SessionId,
                iteration, input.InputChecksum, clock.Now, clock, Reason, CancellationToken.None));
            Assert.Equal(1, calls);
        }
        finally { release.TrySetResult(); }
        await active;
        Assert.Equal(iteration, db.Memory().ReadFiniteResearchSession()!.Iterations.Length);
    }

    [Fact]
    public void AdditiveMigrationRollbackAndReopenPreserveExistingV1BytesWithoutGrant()
    {
        using var db = new Database(false);
        Assert.Throws<InvalidOperationException>(() => FinanceSchemaMigrator.Migrate(db.Path, version =>
        { if (version == 95) throw new InvalidOperationException("migration-crash"); }));
        Assert.Equal(94, FinanceSchemaMigrator.State(db.Path).CurrentVersion);
        string before;
        using (var c = db.Open())
        {
            before = (string)Scalar(c, "SELECT snapshot_json || checksum FROM learning_governance")!;
            Assert.Equal(0L, Scalar(c, "SELECT COUNT(*) FROM sqlite_master WHERE name='learning_finite_session'"));
        }
        var memory = db.Memory();
        Assert.Null(memory.ReadFiniteResearchSession());
        FinanceSchemaMigrator.Migrate(db.Path);
        using var after = db.Open();
        Assert.Equal(before, Scalar(after, "SELECT snapshot_json || checksum FROM learning_governance"));
        Assert.Equal(0L, Scalar(after, "SELECT COUNT(*) FROM learning_finite_session"));
    }

    [Theory]
    [InlineData("UPDATE learning_finite_session SET version='unknown'")]
    [InlineData("UPDATE learning_finite_session SET checksum='bad'")]
    [InlineData("UPDATE learning_finite_session SET snapshot_json='{}'")]
    [InlineData("INSERT INTO finance_schema_migrations VALUES(96,'unsupported','2026-01-01')")]
    public void CorruptOrUnknownSessionFailsClosed(string sql)
    {
        using var db = new Database(); var memory = db.Memory();
        memory.CreateFiniteResearchSession(ResearchLearningFixture.Scope(), new Clock());
        using (var c = db.Open()) Execute(c, sql);
        Assert.ThrowsAny<Exception>(() => db.Memory().ReadFiniteResearchSession());
    }

    [Fact]
    public async Task MissingNativeResultPreventsHistoryProjection()
    {
        using var db = new Database(); var memory = db.Memory(); var scope = ResearchLearningFixture.Scope(); var clock = new Clock();
        var session = memory.CreateFiniteResearchSession(scope, clock);
        await Invoke(memory, session, 1, clock, new Fake(input => Proposal(scope, input)));
        using (var c = db.Open()) Execute(c, "DELETE FROM robustness_evaluations");
        Assert.Throws<InvalidOperationException>(() => db.Memory().PreviewFiniteResearchInput(session.SessionId, 2, clock.Now));
    }

    [Fact]
    public async Task CancellationDiscardsLateReplyAndNeverRetries()
    {
        using var db = new Database(); var memory = db.Memory(); var scope = ResearchLearningFixture.Scope(); var clock = new Clock();
        var session = memory.CreateFiniteResearchSession(scope, clock);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var completion = new TaskCompletionSource<LearningReasonerReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        Task<LearningReasonerReply> IgnoreCancellation(LearningDevelopmentInput input, CancellationToken token)
        { calls++; entered.TrySetResult(); return completion.Task; }
        var active = memory.RunFiniteResearchIterationAsync(session.SessionId, 1, scope.Input.InputChecksum,
            clock.Now, clock, IgnoreCancellation, cts.Token);
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        cts.Cancel();
        var failed = await active;
        Assert.Equal(FiniteIterationFailure.Cancelled, failed.Failure);
        completion.SetResult(LearningReplyParser.Parse(Proposal(scope, scope.Input), scope.Input).Reply!);
        Assert.Equal(failed, await memory.RunFiniteResearchIterationAsync(session.SessionId, 1, scope.Input.InputChecksum,
            clock.Now, clock, IgnoreCancellation, TestContext.Current.CancellationToken));
        Assert.Equal(1, calls); Assert.Equal(0, db.Memory().ReadLearningLedger().EngineStarts);
    }

    [Fact]
    public async Task RecomputedEnvelopeCannotMakeInventedDevelopmentHistoryAuthoritative()
    {
        using var db = new Database(); var memory = db.Memory(); var scope = ResearchLearningFixture.Scope(); var clock = new Clock();
        var session = memory.CreateFiniteResearchSession(scope, clock);
        await Invoke(memory, session, 1, clock, new Fake(input => Proposal(scope, input)));
        await Invoke(memory, session, 2, clock, new Fake(input => Decline(scope, input)));
        var state = memory.ReadFiniteResearchSession()!;
        var step = state.Iterations[1]; var history = step.Input.FiniteHistory!;
        var forged = new FiniteResearchHistory(history.KnowledgeCutoffUtc, history.AvailableAtUtc,
            history.PreviousOutcome, (history.ReferenceValidationExcessReturn ?? 0) + 1m);
        var input = step.Input with { FiniteHistory = forged, HistoryChecksum = LearningIdentity.Hash(forged), InputChecksum = "" };
        input = input with { InputChecksum = LearningIdentity.Hash(input) };
        var changed = state with { Iterations = state.Iterations.SetItem(1, step with { Input = input }) };
        using (var c = db.Open())
        {
            using var command = c.CreateCommand();
            command.CommandText = "UPDATE learning_finite_session SET snapshot_json=$json,checksum=$checksum";
            command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(changed, Json));
            command.Parameters.AddWithValue("$checksum", LearningIdentity.Hash(changed));
            command.ExecuteNonQuery();
        }
        Assert.Throws<InvalidOperationException>(() => db.Memory().ReadFiniteResearchSession());
    }

    [Fact]
    public void V1WireAndInputIdentityRemainUnextendedWithoutExplicitHistory()
    {
        var input = ResearchLearningFixture.Scope().Input;
        var bytes = LocalReasonerProtocol.Request(input);
        Assert.Equal(input, LocalReasonerProtocol.ReadRequest(bytes));
        Assert.DoesNotContain("finiteHistory", System.Text.Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
        Assert.Equal(1, input.Limits.ReasonerInvocations);
        Assert.Null(input.FiniteHistory);
    }

    private static Task<FiniteResearchIteration> Invoke(EodhdMarketMemory memory, FiniteResearchSession session, int iteration,
        Clock clock, Fake fake) => InvokeWithCancellation(memory, session, iteration, clock, fake, TestContext.Current.CancellationToken);

    private static Task<FiniteResearchIteration> InvokeWithCancellation(EodhdMarketMemory memory, FiniteResearchSession session, int iteration,
        Clock clock, Fake fake, CancellationToken token)
    {
        var input = memory.PreviewFiniteResearchInput(session.SessionId, iteration, clock.Now);
        return memory.RunFiniteResearchIterationAsync(session.SessionId, iteration, input.InputChecksum, clock.Now, clock, fake.ReasonAsync, token);
    }
    private static string Proposal(SyntheticLearningScope scope, LearningDevelopmentInput input) => Rebind(ResearchLearningFixture.Proposal(scope), input);
    private static string Decline(SyntheticLearningScope scope, LearningDevelopmentInput input) => Rebind(ResearchLearningFixture.NoUseful(scope), input);
    private static string Rebind(string wire, LearningDevelopmentInput input)
    { var json = JsonNode.Parse(wire)!; json["inputChecksum"] = input.InputChecksum; return json.ToJsonString(); }
    private sealed class Fake(Func<LearningDevelopmentInput, string> respond) : IResearchReasoner
    {
        internal int Calls { get; private set; }
        public Task<LearningReasonerReply> ReasonAsync(LearningDevelopmentInput input, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested(); Calls++;
            var parsed = LearningReplyParser.Parse(respond(input), input);
            return Task.FromResult(parsed.Reply ?? throw new InvalidOperationException("Test-only invalid reply"));
        }
    }
    private sealed class Clock : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Database : IDisposable
    {
        private readonly string _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bb132b", Guid.NewGuid().ToString("N"));
        internal string Path => System.IO.Path.Combine(_root, "finance.db");
        internal Database(bool initialize = true) { Directory.CreateDirectory(_root); if (initialize) _ = Memory(); }
        internal EodhdMarketMemory Memory() => new(new EodhdFinanceOptions { DatabasePath = Path, PayloadDirectory = System.IO.Path.Combine(_root, "payloads") });
        internal SqliteConnection Open() { var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path, Pooling = false }.ToString()); c.Open(); return c; }
        public void Dispose() => Directory.Delete(_root, true);
    }
    private static object? Scalar(SqliteConnection c, string sql) { using var command = c.CreateCommand(); command.CommandText = sql; return command.ExecuteScalar(); }
    private static void Execute(SqliteConnection c, string sql) { using var command = c.CreateCommand(); command.CommandText = sql; command.ExecuteNonQuery(); }
}
