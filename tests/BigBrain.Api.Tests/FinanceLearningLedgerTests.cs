using System.Text.Json;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Tests;

public sealed class FinanceLearningLedgerTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public void MigrationIsOwnedIdempotentAndPreservesOlderData()
    {
        using var db = new LedgerDatabase(initialize: false);
        Assert.Throws<InvalidOperationException>(() => FinanceSchemaMigrator.Migrate(db.Path, version =>
        { if (version == 94) throw new InvalidOperationException("crash-before-version-record"); }));
        Assert.Equal(93, FinanceSchemaMigrator.State(db.Path).CurrentVersion);
        using (var c = db.Open())
        {
            Assert.Equal(0L, Scalar(c, "SELECT COUNT(*) FROM sqlite_master WHERE name='learning_governance'"));
            Execute(c, "INSERT INTO macro_revisions VALUES('synthetic-legacy','synthetic','hash','2026-01-01','synthetic','pass')");
        }
        var memory = db.Memory();
        Assert.Equal(94, FinanceSchemaMigrator.State(db.Path).CurrentVersion);
        Assert.Equal(LearningLifecycle.Uninitialized, memory.ReadLearningLedger().Lifecycle);
        var scope = ResearchLearningFixture.Scope();
        memory.EnrollSyntheticLearning(scope, LearningExposure.Unexposed);
        Assert.True(memory.ReserveLearningInvocation());
        var before = Snapshot(memory);
        FinanceSchemaMigrator.Migrate(db.Path);
        Assert.Equal(before, Snapshot(db.Memory()));
        using var verify = db.Open();
        Assert.Equal(1L, Scalar(verify, "SELECT COUNT(*) FROM macro_revisions WHERE revision_id='synthetic-legacy'"));
    }

    [Fact]
    public void ReservationSurvivesRestartAndCannotBeRefundedByMetadataOrEnrollment()
    {
        using var db = new LedgerDatabase();
        var scope = ResearchLearningFixture.Scope();
        var memory = Enroll(db, scope);
        Assert.True(memory.ReserveLearningInvocation());
        var fake = new ResearchLearningFixture.FakeReasoner(x => ResearchLearningFixture.Proposal(scope));
        var admitted = memory.ReserveLearningProposal(fake.Propose(scope.Input), scope);
        Assert.Equal(LearningAdmissionReason.Admitted, admitted.Reason);
        var reopened = db.Memory();
        reopened.EnrollSyntheticLearning(scope, LearningExposure.Unexposed);
        Assert.False(reopened.ReserveLearningInvocation());
        var duplicate = reopened.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope, "New model/request/rationale"), scope);
        Assert.Equal(LearningAdmissionReason.Duplicate, duplicate.Reason);
        Assert.Equal(admitted.ProposalId, duplicate.ProposalId);
        var state = reopened.ReadLearningLedger();
        Assert.Equal(3, state.Trials); Assert.Equal(0, state.EngineStarts);
        Assert.Equal(scope.MaximumEngineCalls, state.ReservedRuns);
        Assert.Equal(LearningExposure.Consumed, state.Exposure);
        Assert.Equal(0, state.ReusedResults);
        Assert.Equal(1, fake.Calls);
        Assert.Throws<InvalidOperationException>(() => reopened.EnrollSyntheticLearning(ResearchLearningFixture.Scope(401), LearningExposure.Unexposed));
    }

    [Theory]
    [InlineData("malformed", LearningAdmissionReason.Malformed)]
    [InlineData("unsupported", LearningAdmissionReason.UnsupportedStrategy)]
    [InlineData("parameter", LearningAdmissionReason.UnsupportedParameter)]
    [InlineData("empty", LearningAdmissionReason.NoUsefulProposal)]
    [InlineData("rights", LearningAdmissionReason.IneligibleDataset)]
    [InlineData("budget", LearningAdmissionReason.BudgetExceeded)]
    public void NegativeHistorySurvivesAndNoEngineAuthorityIsIssued(string kind, LearningAdmissionReason expected)
    {
        using var db = new LedgerDatabase();
        var original = ResearchLearningFixture.Scope();
        var memory = Enroll(db, original);
        Assert.True(memory.ReserveLearningInvocation());
        var scope = kind switch
        {
            "rights" => SyntheticLearningScope.Create(original.Plan, original.Bars, original.Features,
                original.Facts with { ExternalRights = DatasetEvidenceResult.Unknown }, original.Purpose, LearningIdentity.Hash(Array.Empty<string>())),
            "budget" => ResearchLearningFixture.Scope(900),
            _ => original
        };
        var response = kind switch
        {
            "malformed" => "{",
            "unsupported" => ResearchLearningFixture.Proposal(scope).Replace("momentum\"", "unknown\"", StringComparison.Ordinal),
            "parameter" => ResearchLearningFixture.Proposal(scope).Replace("20", "21", StringComparison.Ordinal),
            "empty" => ResearchLearningFixture.NoUseful(scope),
            _ => ResearchLearningFixture.Proposal(scope)
        };
        if (kind == "parameter") response = ResearchLearningFixture.Proposal(scope).Replace("\"period\":20", "\"period\":21", StringComparison.Ordinal);
        Assert.Equal(expected, memory.ReserveLearningProposal(response, scope).Reason);
        var reopened = db.Memory();
        Assert.False(reopened.ReserveLearningInvocation());
        var state = reopened.ReadLearningLedger();
        Assert.Equal(0, state.EngineStarts); Assert.Equal(0, state.Trials); Assert.Equal(1, state.Submitted);
        Assert.Contains(state.History, x => x.Submission && x.Reason == expected);
        Assert.Null(state.Commitment);
        Assert.Throws<InvalidOperationException>(() => reopened.StartLearningExecution("invented"));
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("unavailable")]
    public void ReasonerFailureConsumesInvocationAndDoesNotAffectDirectFinance(string failure)
    {
        using var db = new LedgerDatabase();
        var scope = ResearchLearningFixture.Scope(); var memory = Enroll(db, scope);
        Assert.True(memory.ReserveLearningInvocation());
        var fake = new ResearchLearningFixture.FakeReasoner(_ => failure == "timeout"
            ? throw new TimeoutException() : throw new InvalidOperationException());
        Assert.ThrowsAny<Exception>(() => fake.Propose(scope.Input));
        memory.FailLearningIteration(failure == "timeout" ? LearningFailure.ReasonerTimeout : LearningFailure.ReasonerUnavailable);
        var reopened = db.Memory(); Assert.False(reopened.ReserveLearningInvocation());
        Assert.Equal(0, reopened.ReadLearningLedger().EngineStarts);
        Assert.Equal(1, fake.Calls);
        Assert.Contains(reopened.ReadLearningLedger().History, x => x.Failure ==
            (failure == "timeout" ? LearningFailure.ReasonerTimeout : LearningFailure.ReasonerUnavailable));
        var direct = DeterministicRobustnessEvaluator.Evaluate(scope.Plan, new MomentumResearchStrategy(), scope.Bars, scope.Features);
        Assert.NotEmpty(direct.UnderlyingRuns);
    }

    [Theory]
    [InlineData("before-commit", false)]
    [InlineData("after-commit", true)]
    public void ReservationCrashBoundaryIsAtomic(string stage, bool committed)
    {
        using var db = new LedgerDatabase(); var scope = ResearchLearningFixture.Scope(); var memory = Enroll(db, scope);
        Assert.True(memory.ReserveLearningInvocation());
        memory.LearningLedgerTestHook = point => { if (point == stage) throw new InvalidOperationException("synthetic-crash"); };
        Assert.Throws<InvalidOperationException>(() => memory.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope), scope));
        var reopened = db.Memory(); var state = reopened.ReadLearningLedger();
        Assert.Equal(committed ? 3 : 0, state.Trials);
        Assert.Equal(committed ? LearningExposure.Consumed : LearningExposure.Unexposed, state.Exposure);
        Assert.Equal(1, state.Invocations); // Invocation reservation was an earlier durable boundary.
        Assert.False(reopened.ReserveLearningInvocation());
        reopened.MarkLearningIndeterminate();
        Assert.NotEqual(LearningAdmissionReason.Admitted, reopened.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope), scope).Reason);
        Assert.Equal(0, reopened.ReadLearningLedger().EngineStarts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrashAfterStartNeverPermitsSecondExecutionAndCanBindExistingResults(bool persistBeforeCrash)
    {
        using var db = new LedgerDatabase(); var scope = ResearchLearningFixture.Scope(); var memory = Enroll(db, scope);
        Assert.True(memory.ReserveLearningInvocation());
        var id = memory.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope), scope).ProposalId!;
        var frozen = memory.StartLearningExecution(id);
        var build = DeterministicRobustnessEvaluator.Evaluate(frozen.Plan, new MomentumResearchStrategy(), frozen.Bars, frozen.Features);
        if (persistBeforeCrash) memory.PersistLearningResults(id, build);
        var reopened = db.Memory(); reopened.MarkLearningIndeterminate();
        Assert.Equal(LearningLifecycle.Indeterminate, reopened.ReadLearningLedger().Lifecycle);
        Assert.Throws<InvalidOperationException>(() => reopened.StartLearningExecution(id));
        if (!persistBeforeCrash)
        {
            Assert.Throws<InvalidOperationException>(() => reopened.CompleteLearningIteration(id, build.Evaluation.EvaluationId));
            // Preserve uncertainty; no second evaluation is performed to make recovery convenient.
            Assert.Equal(LearningExposure.Consumed, reopened.ReadLearningLedger().Exposure);
            return;
        }
        reopened.CompleteLearningIteration(id, build.Evaluation.EvaluationId);
        var completed = reopened.ReadLearningLedger();
        Assert.Equal(LearningLifecycle.Completed, completed.Lifecycle);
        Assert.Equal(build.Evaluation.Checksum, completed.Result!.Checksum);
        Assert.Equal(build.Evaluation.Verdict, completed.Result.Verdict);
        Assert.Equal("RESEARCH", completed.Result.OperatingMode); Assert.Equal("NONE", completed.Result.ExecutionAuthority);
        var reader = new EodhdFinanceBacktestReader(db.Memory());
        foreach (var run in build.UnderlyingRuns)
            Assert.Equal(JsonSerializer.Serialize(run, Json), JsonSerializer.Serialize(reader.GetResult(run.RunId), Json));
        // Reopen/reconcile never calls a reasoner or engine. Exact source-writer replay inserts no rows.
        using var c = db.Open(); var count = Scalar(c, "SELECT COUNT(*) FROM backtest_runs");
        reopened.PersistLearningResults(id, build);
        reopened.CompleteLearningIteration(id, build.Evaluation.EvaluationId);
        Assert.Equal(count, Scalar(c, "SELECT COUNT(*) FROM backtest_runs"));
        Assert.Equal(Snapshot(reopened), Snapshot(db.Memory()));
        Assert.Equal(LearningAdmissionReason.Duplicate, reopened.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope, "changed prose"), scope).Reason);
        Assert.Equal(1, reopened.ReadLearningLedger().ReusedResults);
        Assert.Throws<InvalidOperationException>(() => reopened.StartLearningExecution(id));
    }

    [Fact]
    public void FrozenReplayUsesStoredScopeWithoutReasonerAndPreservesIdentities()
    {
        using var db = new LedgerDatabase(); var scope = ResearchLearningFixture.Scope(); var memory = Enroll(db, scope);
        Assert.True(memory.ReserveLearningInvocation());
        var fake = new ResearchLearningFixture.FakeReasoner(_ => ResearchLearningFixture.Proposal(scope));
        var id = memory.ReserveLearningProposal(fake.Propose(scope.Input), scope).ProposalId!;
        var frozen = memory.StartLearningExecution(id);
        var first = DeterministicRobustnessEvaluator.Evaluate(frozen.Plan, new MomentumResearchStrategy(), frozen.Bars, frozen.Features);
        memory.PersistLearningResults(id, first); memory.CompleteLearningIteration(id, first.Evaluation.EvaluationId);
        var restored = db.Memory().ReadLearningLedger().Scope!.Resolve();
        // Explicit test-only reproduction of consumed engineering evidence, not a new start grant.
        var replay = DeterministicRobustnessEvaluator.Evaluate(restored.Plan, new MomentumResearchStrategy(), restored.Bars, restored.Features);
        Assert.Equal(JsonSerializer.Serialize(first, Json), JsonSerializer.Serialize(replay, Json));
        Assert.Equal(1, fake.Calls);
        Assert.Equal(1, db.Memory().ReadLearningLedger().EngineStarts);
    }

    [Fact]
    public void RenamedRevisionCannotRenewExposureAndUnknownCohortCannotBeEnrolled()
    {
        using var db = new LedgerDatabase(); var scope = ResearchLearningFixture.Scope(); var memory = Enroll(db, scope);
        Assert.True(memory.ReserveLearningInvocation());
        memory.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope), scope);
        var plan = scope.Plan with { MarketRevisionIds = new[] { "synthetic-renamed-market" }, FeatureRevisionId = "synthetic-renamed-feature" };
        // CreatePlan also binds plan identity, rather than changing labels in an invalid plan.
        plan = DeterministicRobustnessEvaluator.CreatePlan(plan.MarketRevisionIds, plan.FeatureRevisionId,
            new MomentumResearchStrategy(), plan.Universe, plan.From, plan.To);
        var renamed = SyntheticLearningScope.Create(plan,
            scope.Bars.Select(x => x with { MarketRevisionId = "synthetic-renamed-market" }),
            scope.Features.Select(x => x with { FeatureRevisionId = "synthetic-renamed-feature" }), scope.Facts, scope.Purpose,
            LearningIdentity.Hash(Array.Empty<string>()));
        Assert.Equal(LearningAdmissionReason.Admitted, renamed.ValidationReason);
        Assert.Equal(LearningAdmissionReason.ExposureConsumed, db.Memory().ReserveLearningProposal(ResearchLearningFixture.Proposal(renamed), renamed).Reason);
        var other = ResearchLearningFixture.Scope(401);
        Assert.Equal(LearningAdmissionReason.ExposureUnknown, db.Memory().ReserveLearningProposal(ResearchLearningFixture.Proposal(other), other).Reason);
        Assert.Equal(3, db.Memory().ReadLearningLedger().Trials);
    }

    [Fact]
    public void UnknownExposureCannotBeReenrolledAsFresh()
    {
        using var db = new LedgerDatabase(); var scope = ResearchLearningFixture.Scope(); var memory = db.Memory();
        memory.EnrollSyntheticLearning(scope, LearningExposure.Unknown);
        memory.EnrollSyntheticLearning(scope, LearningExposure.Unexposed);
        Assert.False(memory.ReserveLearningInvocation());
        Assert.Equal(LearningAdmissionReason.ExposureUnknown, memory.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope), scope).Reason);
        Assert.Equal(0, db.Memory().ReadLearningLedger().EngineStarts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SeparateConnectionsCannotDoubleReserveLastBudgetOrCohort(bool changedWording)
    {
        using var db = new LedgerDatabase(); var scope = ResearchLearningFixture.Scope(); var first = Enroll(db, scope);
        var second = db.Memory();
        var invocation = await Task.WhenAll(Task.Run(first.ReserveLearningInvocation), Task.Run(second.ReserveLearningInvocation));
        Assert.Single(invocation, x => x);
        using var barrier = new Barrier(2);
        Task<LearningReservation> Submit(EodhdMarketMemory memory, string rationale) => Task.Run(() =>
        { barrier.SignalAndWait(); return memory.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope, rationale), scope); });
        var results = await Task.WhenAll(Submit(first, "one"), Submit(second, changedWording ? "two" : "one"));
        Assert.Single(results, x => x.Reason == LearningAdmissionReason.Admitted);
        Assert.Single(results, x => x.Reason == LearningAdmissionReason.Duplicate);
        var state = db.Memory().ReadLearningLedger(); Assert.Equal(3, state.Trials); Assert.Equal(2, state.Submitted);
        var id = results.Single(x => x.Reason == LearningAdmissionReason.Admitted).ProposalId!;
        first.StartLearningExecution(id);
        Assert.Throws<InvalidOperationException>(() => second.StartLearningExecution(id));
    }

    [Theory]
    [InlineData("DELETE FROM learning_governance")]
    [InlineData("UPDATE learning_governance SET version='unknown'")]
    [InlineData("UPDATE learning_governance SET snapshot_json='{}'")]
    [InlineData("UPDATE learning_governance SET checksum='corrupt'")]
    public void MissingOrCorruptStateNeverRecreatesAuthority(string corruption)
    {
        using var db = new LedgerDatabase(); var scope = ResearchLearningFixture.Scope(); var memory = Enroll(db, scope);
        using (var c = db.Open()) Execute(c, corruption);
        var reopened = db.Memory();
        Assert.ThrowsAny<Exception>(() => reopened.ReadLearningLedger());
        Assert.ThrowsAny<Exception>(() => reopened.EnrollSyntheticLearning(scope, LearningExposure.Unexposed));
        Assert.ThrowsAny<Exception>(() => reopened.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope), scope));
    }

    [Fact]
    public void CompletionRejectsMissingOrConflictingResultsWithoutRefund()
    {
        using var db = new LedgerDatabase(); var scope = ResearchLearningFixture.Scope(); var memory = Enroll(db, scope);
        Assert.True(memory.ReserveLearningInvocation());
        var id = memory.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope), scope).ProposalId!;
        memory.StartLearningExecution(id);
        var build = DeterministicRobustnessEvaluator.Evaluate(scope.Plan, new MomentumResearchStrategy(), scope.Bars, scope.Features);
        Assert.Throws<InvalidOperationException>(() => memory.CompleteLearningIteration(id, build.Evaluation.EvaluationId));
        memory.PersistLearningResults(id, build);
        using (var c = db.Open()) Execute(c, "UPDATE backtest_runs SET checksum='synthetic-conflict'");
        Assert.Throws<InvalidOperationException>(() => memory.CompleteLearningIteration(id, build.Evaluation.EvaluationId));
        Assert.Throws<InvalidOperationException>(() => memory.PersistLearningResults(id, build));
        Assert.Equal(LearningExposure.Consumed, db.Memory().ReadLearningLedger().Exposure);
        Assert.Null(db.Memory().ReadLearningLedger().Result);
    }

    [Fact]
    public void LostStartGrantAfterCommitCannotBeIssuedAgain()
    {
        using var db = new LedgerDatabase(); var scope = ResearchLearningFixture.Scope(); var memory = Enroll(db, scope);
        Assert.True(memory.ReserveLearningInvocation());
        var id = memory.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope), scope).ProposalId!;
        memory.LearningLedgerTestHook = point => { if (point == "after-commit") throw new InvalidOperationException("lost-return"); };
        Assert.Throws<InvalidOperationException>(() => memory.StartLearningExecution(id));
        var reopened = db.Memory(); reopened.MarkLearningIndeterminate();
        Assert.Equal(1, reopened.ReadLearningLedger().EngineStarts);
        Assert.Equal(LearningExposure.Consumed, reopened.ReadLearningLedger().Exposure);
        Assert.Throws<InvalidOperationException>(() => reopened.StartLearningExecution(id));
    }

    [Fact]
    public void CompletionRollbackLeavesResultsRecoverableWithoutReexecution()
    {
        using var db = new LedgerDatabase(); var scope = ResearchLearningFixture.Scope(); var memory = Enroll(db, scope);
        Assert.True(memory.ReserveLearningInvocation());
        var id = memory.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope), scope).ProposalId!;
        memory.StartLearningExecution(id);
        var build = DeterministicRobustnessEvaluator.Evaluate(scope.Plan, new MomentumResearchStrategy(), scope.Bars, scope.Features);
        memory.PersistLearningResults(id, build);
        memory.LearningLedgerTestHook = point => { if (point == "before-commit") throw new InvalidOperationException("completion-crash"); };
        Assert.Throws<InvalidOperationException>(() => memory.CompleteLearningIteration(id, build.Evaluation.EvaluationId));
        var reopened = db.Memory(); reopened.MarkLearningIndeterminate();
        Assert.Null(reopened.ReadLearningLedger().Result);
        reopened.CompleteLearningIteration(id, build.Evaluation.EvaluationId);
        Assert.Equal(1, reopened.ReadLearningLedger().EngineStarts);
        Assert.Equal(LearningLifecycle.Completed, reopened.ReadLearningLedger().Lifecycle);
        using var c = db.Open();
        Execute(c, "DELETE FROM backtest_runs WHERE run_id=(SELECT run_id FROM backtest_runs LIMIT 1)");
        Assert.Throws<InvalidOperationException>(() => db.Memory().ReadLearningLedger());
        Assert.Throws<InvalidOperationException>(() => reopened.ReserveLearningInvocation());
    }

    [Theory]
    [InlineData("INSERT INTO finance_schema_migrations VALUES(95,'unknown','2026-01-01')")]
    [InlineData("UPDATE learning_governance SET snapshot_json=replace(snapshot_json,'\"invocations\":1','\"invocations\":\"1\"')")]
    [InlineData("UPDATE learning_governance SET snapshot_json=replace(snapshot_json,'\"invocations\":1','\"invocations\":1,\"invocations\":1')")]
    public void UnsupportedOrAmbiguousPersistedShapeFailsClosed(string mutation)
    {
        using var db = new LedgerDatabase(); var scope = ResearchLearningFixture.Scope(); var memory = Enroll(db, scope);
        Assert.True(memory.ReserveLearningInvocation());
        using (var c = db.Open()) Execute(c, mutation);
        Assert.ThrowsAny<Exception>(() => memory.ReadLearningLedger());
        Assert.ThrowsAny<Exception>(() => memory.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope), scope));
    }

    [Fact]
    public void RecomputedChecksumCannotHideInvalidLedgerCounters()
    {
        using var db = new LedgerDatabase(); var scope = ResearchLearningFixture.Scope(); var memory = Enroll(db, scope);
        Assert.True(memory.ReserveLearningInvocation());
        memory.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope), scope);
        var state = memory.ReadLearningLedger();
        using var c = db.Open(); using var command = c.CreateCommand();
        command.CommandText = "UPDATE learning_governance SET snapshot_json=replace(snapshot_json,'\"trials\":3','\"trials\":0'),checksum=$checksum";
        command.Parameters.AddWithValue("$checksum", LearningIdentity.Hash(state with { Trials = 0 })); command.ExecuteNonQuery();
        Assert.Throws<InvalidOperationException>(() => memory.ReadLearningLedger());
    }

    [Fact]
    public async Task DistinctScientificFingerprintsCannotDoubleConsumeSameCohort()
    {
        using var db = new LedgerDatabase(); var scope = ResearchLearningFixture.Scope(); var first = Enroll(db, scope);
        Assert.True(first.ReserveLearningInvocation()); var second = db.Memory();
        var changed = SyntheticLearningScope.Create(scope.Plan, scope.Bars.Select(x => x with { Close = x.Close + 1 }),
            scope.Features, scope.Facts, scope.Purpose, LearningIdentity.Hash(Array.Empty<string>()));
        Assert.NotEqual(scope.ExecutionFingerprint, changed.ExecutionFingerprint);
        using var barrier = new Barrier(2);
        var one = Task.Run(() => { barrier.SignalAndWait(); return first.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope), scope); });
        var two = Task.Run(() => { barrier.SignalAndWait(); return second.ReserveLearningProposal(ResearchLearningFixture.Proposal(changed), changed); });
        var results = await Task.WhenAll(one, two);
        // If the unsupported replacement arrives first it terminates the one invocation; no
        // experiment is preferable to silently substituting a different frozen population.
        Assert.True(results.Count(x => x.Reason == LearningAdmissionReason.Admitted) <= 1);
        Assert.NotEqual(LearningAdmissionReason.Admitted, results[1].Reason);
        Assert.True(db.Memory().ReadLearningLedger().Trials <= 3);
    }

    [Theory]
    [InlineData("Deny")]
    [InlineData("Halt")]
    [InlineData("InsufficientData")]
    public void NativeRiskVetoCanTerminateResearchButLedgerNeverGrantsExecution(string risk)
    {
        using var db = new LedgerDatabase(); var scope = ResearchLearningFixture.Scope(); var memory = Enroll(db, scope);
        Assert.True(memory.ReserveLearningInvocation());
        var id = memory.ReserveLearningProposal(ResearchLearningFixture.Proposal(scope), scope).ProposalId!;
        Assert.NotEqual(FinanceRiskVerdict.Allow, Enum.Parse<FinanceRiskVerdict>(risk));
        memory.FailLearningIteration(LearningFailure.RiskVeto);
        Assert.Throws<InvalidOperationException>(() => db.Memory().StartLearningExecution(id));
        Assert.Contains(db.Memory().ReadLearningLedger().History, x => x.Failure == LearningFailure.RiskVeto);
        Assert.Equal(0, db.Memory().ReadLearningLedger().EngineStarts);
        Assert.Equal(LearningExposure.Consumed, db.Memory().ReadLearningLedger().Exposure);
    }

    private static EodhdMarketMemory Enroll(LedgerDatabase db, SyntheticLearningScope scope)
    { var memory = db.Memory(); memory.EnrollSyntheticLearning(scope, LearningExposure.Unexposed); return memory; }
    private static string Snapshot(EodhdMarketMemory memory) => JsonSerializer.Serialize(memory.ReadLearningLedger(), Json);
    private static object? Scalar(SqliteConnection c, string sql) { using var cmd = c.CreateCommand(); cmd.CommandText = sql; return cmd.ExecuteScalar(); }
    private static void Execute(SqliteConnection c, string sql) { using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery(); }
    private sealed class LedgerDatabase : IDisposable
    {
        private readonly string _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bb131c", Guid.NewGuid().ToString("N"));
        internal string Path => System.IO.Path.Combine(_root, "finance.db");
        private EodhdFinanceOptions Options => new() { DatabasePath = Path, PayloadDirectory = System.IO.Path.Combine(_root, "payloads") };
        internal LedgerDatabase(bool initialize = true) { Directory.CreateDirectory(_root); if (initialize) _ = Memory(); }
        internal EodhdMarketMemory Memory() => new(Options);
        internal SqliteConnection Open() { var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path, Pooling = false }.ToString()); c.Open(); return c; }
        public void Dispose() => Directory.Delete(_root, true);
    }
}
