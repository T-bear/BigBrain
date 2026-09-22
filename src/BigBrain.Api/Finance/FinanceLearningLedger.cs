using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

internal enum LearningLifecycle { Uninitialized, Ready, AwaitingProposal, Reserved, Running, Completed, Failed, Indeterminate }
internal enum LearningFailure { ReasonerTimeout, ReasonerUnavailable, ExecutionFailure, RiskVeto }
internal enum LearningLedgerOutcome { Enrolled, InvocationReserved, Admitted, Rejected, Duplicate, Started, Completed, Failed, Indeterminate }
internal sealed record LearningLedgerAttempt(LearningLedgerOutcome Outcome, LearningAdmissionReason? Reason,
    string? ResponseChecksum, string? ProposalId, bool Submission = false, LearningFailure? Failure = null)
{
    public DateTimeOffset RecordedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
internal sealed record LearningFrozenScope(EvaluationPlan Plan, ImmutableArray<BacktestMarketBar> Bars,
    ImmutableArray<BacktestFeatureValue> Features, ResearchDatasetFacts Facts, ResearchDatasetPurpose Purpose)
{
    internal static LearningFrozenScope Freeze(SyntheticLearningScope scope) => new(scope.Plan, scope.Bars, scope.Features, scope.Facts, scope.Purpose);
    internal SyntheticLearningScope Resolve() => SyntheticLearningScope.Create(Plan, Bars, Features, Facts, Purpose,
        LearningIdentity.Hash(Array.Empty<string>()));
}
internal sealed record LearningCommitment(string ProposalId, string ExecutionFingerprint, string InputChecksum,
    string ResponseChecksum, LearningProposalDraft Draft);
internal sealed record LearningLedgerSnapshot(
    string Version, string Protocol, string Family, LearningLifecycle Lifecycle, LearningFrozenScope? Scope,
    string? Cohort, int Invocations, int Submitted, int Trials, int ReservedRuns, int EngineStarts,
    int UniqueRuns, int ReusedResults, LearningExposure Exposure, LearningCommitment? Commitment,
    LearningResultReference? Result, ImmutableArray<LearningLedgerAttempt> History)
{
    internal const string Contract = "finance-learning-ledger-v1";
    internal static LearningLedgerSnapshot Empty => new(Contract, "synthetic-momentum20-protocol-v1",
        "synthetic-momentum-family-v1", LearningLifecycle.Uninitialized, null, null, 0, 0, 0, 0, 0, 0, 0,
        LearningExposure.Unknown, null, null, []);
}
internal sealed record LearningReservation(LearningAdmissionReason Reason, string? ProposalId);

// Existing Finance owner and database. No endpoint, reasoner, scheduler or new scientific engine.
internal sealed partial class EodhdMarketMemory
{
    private static readonly JsonSerializerOptions LearningJson = new(JsonSerializerDefaults.Web)
    { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, PropertyNameCaseInsensitive = false, NumberHandling = JsonNumberHandling.Strict, Converters = { new LearningInstrumentConverter() } };
    // The frozen ledger envelope is a new serialization boundary. InstrumentId has get-only
    // state; explicitly round-trip it here without changing existing Finance serializers/types.
    private sealed class LearningInstrumentConverter : JsonConverter<InstrumentId>
    {
        public override InstrumentId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1 ||
                !root.TryGetProperty("value", out var member) || member.ValueKind != JsonValueKind.String)
                throw new JsonException("Invalid frozen instrument.");
            var value = member.GetString()!;
            var id = new InstrumentId(value);
            if (id.Value != value) throw new JsonException("Noncanonical frozen instrument.");
            return id;
        }
        public override void Write(Utf8JsonWriter writer, InstrumentId value, JsonSerializerOptions options)
        { writer.WriteStartObject(); writer.WriteString("value", value.Value); writer.WriteEndObject(); }
    }

    internal Action<string>? LearningLedgerTestHook { get; set; }

    // Only FinanceSchemaMigrator executes this DDL/seed, in its existing migration transaction.
    internal static string LearningLedgerMigration => $"""
        CREATE TABLE learning_governance(
          singleton INTEGER PRIMARY KEY CHECK(singleton=1),
          version TEXT NOT NULL, snapshot_json TEXT NOT NULL, checksum TEXT NOT NULL);
        INSERT INTO learning_governance VALUES(1,'{LearningLedgerSnapshot.Contract}',
          '{JsonSerializer.Serialize(LearningLedgerSnapshot.Empty, LearningJson)}',
          '{LearningIdentity.Hash(LearningLedgerSnapshot.Empty)}');
        """;

    // Explicit trusted synthetic enrollment, never inferred on read/restart or from a model ID.
    // There is one fixed program, not a caller-selectable namespace that can replenish budget.
    internal void EnrollSyntheticLearning(SyntheticLearningScope scope, LearningExposure exposure)
    {
        if (scope.ValidationReason != LearningAdmissionReason.Admitted ||
            exposure is not (LearningExposure.Unexposed or LearningExposure.Unknown))
            throw new InvalidOperationException("Invalid synthetic enrollment.");
        LearningTransaction((_, state) =>
        {
            if (state.Lifecycle != LearningLifecycle.Uninitialized)
            {
                if (LearningIdentity.Hash(state.Scope) != LearningIdentity.Hash(LearningFrozenScope.Freeze(scope)))
                    throw new InvalidOperationException("Frozen protocol cannot be replaced.");
                return state; // Idempotent enrollment NEVER restores exposure/counters.
            }
            return AppendLearning(state with
            {
                Lifecycle = LearningLifecycle.Ready,
                Scope = LearningFrozenScope.Freeze(scope),
                Cohort = LearningCohort(scope),
                Exposure = exposure
            },
                new(LearningLedgerOutcome.Enrolled, null, null, null));
        });
    }

    internal bool ReserveLearningInvocation()
    {
        var reserved = false;
        LearningTransaction((_, state) =>
        {
            if (state.Lifecycle != LearningLifecycle.Ready || state.Invocations != 0 || state.Exposure != LearningExposure.Unexposed)
                return AppendLearning(state, new(LearningLedgerOutcome.Rejected,
                    state.Exposure == LearningExposure.Unknown ? LearningAdmissionReason.ExposureUnknown : LearningAdmissionReason.BudgetExceeded, null, null));
            reserved = true;
            return AppendLearning(state with { Invocations = 1, Lifecycle = LearningLifecycle.AwaitingProposal },
                new(LearningLedgerOutcome.InvocationReserved, null, null, null));
        });
        return reserved; // Commit succeeded before any reasoner call is permitted.
    }

    internal LearningReservation ReserveLearningProposal(string response, SyntheticLearningScope scope)
    {
        LearningReservation result = new(LearningAdmissionReason.Malformed, null);
        LearningTransaction((_, state) =>
        {
            var knownCohort = state.Cohort == LearningCohort(scope);
            var exposure = knownCohort ? state.Exposure : LearningExposure.Unknown;
            var admission = LearningAdmissionPolicy.Admit(response, scope, new(state.Submitted, state.Trials,
                state.EngineStarts, exposure, state.Commitment?.ExecutionFingerprint, state.Commitment?.ProposalId));
            // The initial B projection is valid only for the original offered scope and one invocation.
            if (admission.Reason != LearningAdmissionReason.Duplicate && admission.Proposal is not null &&
                (state.Lifecycle != LearningLifecycle.AwaitingProposal || state.Invocations != 1 ||
                 LearningIdentity.Hash(state.Scope) != LearningIdentity.Hash(LearningFrozenScope.Freeze(scope))))
                admission = new(LearningAdmissionReason.BudgetExceeded);
            var checksum = response is null || Encoding.UTF8.GetByteCount(response) > 65536 ? null : LearningIdentity.HashBytes(Encoding.UTF8.GetBytes(response));
            state = state with { Submitted = checked(state.Submitted + 1) };
            if (admission.Proposal is { } proposal)
            {
                state = state with
                {
                    Lifecycle = LearningLifecycle.Reserved,
                    Exposure = LearningExposure.Consumed,
                    Trials = 3,
                    ReservedRuns = scope.MaximumEngineCalls,
                    Commitment = new(proposal.ProposalId, proposal.ExecutionFingerprint, scope.Input.InputChecksum,
                        proposal.ResponseChecksum, proposal.Draft)
                };
                result = new(LearningAdmissionReason.Admitted, proposal.ProposalId);
                return AppendLearning(state, new(LearningLedgerOutcome.Admitted, admission.Reason, checksum, proposal.ProposalId, true));
            }
            if (admission.Reason == LearningAdmissionReason.Duplicate)
            {
                // A pending reservation is linked, never called a reused result or re-executed.
                if (state.Result is not null) state = state with { ReusedResults = checked(state.ReusedResults + 1) };
                result = new(admission.Reason, admission.ReusedProposalId);
                return AppendLearning(state, new(LearningLedgerOutcome.Duplicate, admission.Reason, checksum, admission.ReusedProposalId, true));
            }
            if (state.Lifecycle is LearningLifecycle.Ready or LearningLifecycle.AwaitingProposal)
                state = state with { Lifecycle = LearningLifecycle.Failed, Invocations = 1 };
            result = new(admission.Reason, null);
            return AppendLearning(state, new(LearningLedgerOutcome.Rejected, admission.Reason, checksum, null, true));
        });
        return result; // Reservation/budget/exposure transaction is durable before return.
    }

    internal SyntheticLearningScope StartLearningExecution(string proposalId)
    {
        SyntheticLearningScope? scope = null;
        LearningTransaction((_, state) =>
        {
            RequireLearningProposal(state, proposalId);
            if (state.Lifecycle != LearningLifecycle.Reserved || state.EngineStarts != 0)
                throw new InvalidOperationException("No unused execution reservation.");
            scope = state.Scope!.Resolve();
            return AppendLearning(state with { Lifecycle = LearningLifecycle.Running, EngineStarts = 1 },
                new(LearningLedgerOutcome.Started, null, null, proposalId));
        });
        return scope!; // One persisted start grant; a crash after this call remains uncertain/spent.
    }

    internal void FailLearningIteration(LearningFailure failure = LearningFailure.ExecutionFailure) => LearningTransaction((_, state) =>
    {
        if (!Enum.IsDefined(failure) || state.Lifecycle is not (LearningLifecycle.AwaitingProposal or LearningLifecycle.Reserved or LearningLifecycle.Running))
            throw new InvalidOperationException("No active iteration or unsupported failure.");
        return AppendLearning(state with { Lifecycle = LearningLifecycle.Failed },
            new(LearningLedgerOutcome.Failed, null, null, state.Commitment?.ProposalId, Failure: failure));
    });

    // Explicit recovery, never constructor-driven cancellation of another process's running work.
    // No replay/retry authority is granted. Reconciliation of exact persisted results may follow.
    internal void MarkLearningIndeterminate() => LearningTransaction((_, state) =>
    {
        if (state.Lifecycle is not (LearningLifecycle.AwaitingProposal or LearningLifecycle.Reserved or LearningLifecycle.Running)) return state;
        return AppendLearning(state with { Lifecycle = LearningLifecycle.Indeterminate },
            new(LearningLedgerOutcome.Indeterminate, null, null, state.Commitment?.ProposalId));
    });

    // Existing immutable writers retain their own transactions. Deliberately separate from ledger
    // completion; a crash here leaves spent governance and rediscoverable source results.
    internal void PersistLearningResults(string proposalId, RobustnessEvaluationBuild build)
    {
        var state = ReadLearningLedger();
        RequireLearningProposal(state, proposalId);
        if (state.EngineStarts != 1 || state.Lifecycle is not (LearningLifecycle.Running or LearningLifecycle.Indeterminate or LearningLifecycle.Completed))
            throw new InvalidOperationException("No started reservation.");
        ValidateLearningEvaluation(state, build.Evaluation);
        if (!build.UnderlyingRuns.Select(x => x.RunId).Order(StringComparer.Ordinal)
            .SequenceEqual(build.Evaluation.UnderlyingRunIds.Order(StringComparer.Ordinal), StringComparer.Ordinal))
            throw new InvalidOperationException("Incomplete source results.");
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        foreach (var run in build.UnderlyingRuns)
        {
            ValidateLearningRun(state, run);
            FinanceBacktestPersistence.PersistBacktest(connection, run);
        }
        PersistEvaluation(connection, build.Evaluation);
    }

    internal void CompleteLearningIteration(string proposalId, string evaluationId) => LearningTransaction((connection, state) =>
    {
        RequireLearningProposal(state, proposalId);
        if (state.EngineStarts != 1 || state.Lifecycle is not (LearningLifecycle.Running or LearningLifecycle.Indeterminate or LearningLifecycle.Completed))
            throw new InvalidOperationException("No reconcilable execution.");
        var result = ReadLearningResult(connection, state, evaluationId);
        if (state.Result is not null)
        {
            if (LearningIdentity.Hash(state.Result) != LearningIdentity.Hash(result)) throw new InvalidOperationException("Immutable result binding conflict.");
            return state;
        }
        return AppendLearning(state with { Lifecycle = LearningLifecycle.Completed, Result = result, UniqueRuns = result.Runs.Length },
            new(LearningLedgerOutcome.Completed, null, null, proposalId));
    });

    private static LearningResultReference ReadLearningResult(SqliteConnection connection, LearningLedgerSnapshot state, string evaluationId)
    {
        var json = EvaluationScalarOrNull(connection, "SELECT result_json FROM robustness_evaluations WHERE evaluation_id=$id", ("$id", evaluationId))
            ?? throw new InvalidOperationException("Missing immutable evaluation.");
        var evaluation = JsonSerializer.Deserialize<RobustnessEvaluationResult>(json, EvaluationJson)!;
        if (evaluation.EvaluationId != evaluationId || EvaluationScalarOrNull(connection,
            "SELECT checksum FROM robustness_evaluations WHERE evaluation_id=$id", ("$id", evaluationId)) != evaluation.Checksum)
            throw new InvalidOperationException("Evaluation identity conflict.");
        ValidateLearningEvaluation(state, evaluation);
        var references = ImmutableArray.CreateBuilder<LearningRunReference>();
        foreach (var id in evaluation.UnderlyingRunIds.Order(StringComparer.Ordinal))
        {
            var runJson = EvaluationScalarOrNull(connection, "SELECT result_json FROM backtest_runs WHERE run_id=$id", ("$id", id))
                ?? throw new InvalidOperationException("Missing immutable backtest.");
            var run = JsonSerializer.Deserialize<BacktestResult>(runJson, EvaluationJson)!;
            if (run.RunId != id || EvaluationScalarOrNull(connection, "SELECT checksum FROM backtest_runs WHERE run_id=$id", ("$id", id)) != run.Checksum)
                throw new InvalidOperationException("Backtest identity conflict.");
            ValidateLearningRun(state, run);
            references.Add(new(id, run.Checksum));
        }
        return new LearningResultReference(evaluationId, evaluation.Checksum, evaluation.Verdict,
            evaluation.SelectionGovernance!.Outcome, references.ToImmutable());
    }

    internal LearningLedgerSnapshot ReadLearningLedger()
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        return ReadLearningState(connection);
    }

    private static string LearningCohort(SyntheticLearningScope scope) => LearningIdentity.Hash(new
    {
        Version = "synthetic-cohort-sessions-v1",
        Members = scope.Bars.Select(x => new { x.InstrumentId, x.SessionDate })
    }); // Revision labels, features, prices, model/request/rationale cannot make these sessions unseen.

    private void LearningTransaction(Func<SqliteConnection, LearningLedgerSnapshot, LearningLedgerSnapshot> change)
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        var state = ReadLearningState(connection);
        var updated = change(connection, state);
        ValidateLearningState(updated);
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "UPDATE learning_governance SET snapshot_json=$json,checksum=$checksum WHERE singleton=1 AND version=$version";
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(updated, LearningJson));
        command.Parameters.AddWithValue("$checksum", LearningIdentity.Hash(updated));
        command.Parameters.AddWithValue("$version", LearningLedgerSnapshot.Contract);
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Missing ledger authority.");
        LearningLedgerTestHook?.Invoke("before-commit");
        transaction.Commit();
        LearningLedgerTestHook?.Invoke("after-commit");
    }

    private static LearningLedgerSnapshot ReadLearningState(SqliteConnection connection)
    {
        // Never run migrations, CREATE, INSERT OR IGNORE or seed on a read/reopen path.
        using (var schema = connection.CreateCommand())
        {
            schema.CommandText = "SELECT CASE WHEN MAX(version)=94 AND SUM(CASE WHEN version=94 THEN 1 ELSE 0 END)=1 THEN 1 ELSE 0 END FROM finance_schema_migrations";
            if (Convert.ToInt64(schema.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) != 1)
                throw new InvalidOperationException("Unsupported learning schema.");
        }
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT version,snapshot_json,checksum FROM learning_governance WHERE singleton=1";
        using var reader = command.ExecuteReader();
        if (!reader.Read() || reader.GetString(0) != LearningLedgerSnapshot.Contract || reader.GetString(1).Length > 4_000_000)
            throw new InvalidOperationException("Missing, unsupported or excessive ledger state.");
        using var document = JsonDocument.Parse(reader.GetString(1));
        RequireUniqueLearningProperties(document.RootElement);
        var state = JsonSerializer.Deserialize<LearningLedgerSnapshot>(reader.GetString(1), LearningJson)
            ?? throw new InvalidOperationException("Missing ledger state.");
        if (LearningIdentity.Hash(state) != reader.GetString(2)) throw new InvalidOperationException("Ledger integrity mismatch.");
        ValidateLearningState(state);
        reader.Close();
        if (state.Result is { } result && LearningIdentity.Hash(result) !=
            LearningIdentity.Hash(ReadLearningResult(connection, state, result.EvaluationId)))
            throw new InvalidOperationException("Stored result reference conflict.");
        return state;
    }

    private static void RequireUniqueLearningProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var member in value.EnumerateObject())
            {
                if (!names.Add(member.Name)) throw new InvalidOperationException("Ambiguous ledger property.");
                RequireUniqueLearningProperties(member.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) RequireUniqueLearningProperties(item);
    }

    private static LearningLedgerSnapshot AppendLearning(LearningLedgerSnapshot state, LearningLedgerAttempt attempt)
    {
        if (state.History.Length >= 256) throw new InvalidOperationException("Bounded history capacity exhausted; human review required.");
        return state with { History = state.History.Add(attempt) };
    }

    private static void RequireLearningProposal(LearningLedgerSnapshot state, string proposalId)
    {
        if (state.Commitment is null || state.Commitment.ProposalId != proposalId)
            throw new InvalidOperationException("Unknown committed proposal.");
    }

    private static void ValidateLearningState(LearningLedgerSnapshot state)
    {
        if (state.Version != LearningLedgerSnapshot.Contract || state.Protocol != LearningLedgerSnapshot.Empty.Protocol ||
            state.Family != LearningLedgerSnapshot.Empty.Family || !Enum.IsDefined(state.Lifecycle) || !Enum.IsDefined(state.Exposure) ||
            state.History.IsDefault || state.History.Length > 256 || state.Invocations is < 0 or > 1 ||
            state.Submitted < 0 || state.Trials is not (0 or 3) || state.EngineStarts is < 0 or > 1 ||
            state.ReservedRuns is < 0 or > 64 || state.UniqueRuns < 0 || state.UniqueRuns > state.ReservedRuns || state.ReusedResults < 0 ||
            state.History.Any(x => !Enum.IsDefined(x.Outcome) || (x.Reason is { } r && !Enum.IsDefined(r)) ||
                (x.Failure is { } f && !Enum.IsDefined(f)) || x.RecordedAtUtc.Offset != TimeSpan.Zero) ||
            state.Submitted != state.History.Count(x => x.Submission) ||
            state.Trials != 3 * state.History.Count(x => x.Outcome == LearningLedgerOutcome.Admitted) ||
            state.EngineStarts != state.History.Count(x => x.Outcome == LearningLedgerOutcome.Started) ||
            state.Invocations < state.History.Count(x => x.Outcome == LearningLedgerOutcome.InvocationReserved) ||
            state.ReusedResults > state.History.Count(x => x.Outcome == LearningLedgerOutcome.Duplicate))
            throw new InvalidOperationException("Invalid governance state.");
        if (state.Lifecycle == LearningLifecycle.Uninitialized)
        {
            if (LearningIdentity.Hash(state) != LearningIdentity.Hash(LearningLedgerSnapshot.Empty)) throw new InvalidOperationException("Invalid initial ledger.");
            return;
        }
        var scope = state.Scope?.Resolve() ?? throw new InvalidOperationException("Missing frozen scope.");
        if (scope.ValidationReason != LearningAdmissionReason.Admitted || state.Cohort != LearningCohort(scope))
            throw new InvalidOperationException("Invalid frozen scope.");
        if (state.Commitment is { } commitment)
        {
            var draft = commitment.Draft;
            if (draft is null || draft.Variant is null || draft.Variant.Strategy != scope.Plan.Strategy ||
                draft.Variant.Period != 20 || draft.Variant.BindingHandle != SyntheticLearningScope.Handle ||
                draft.Falsification != LearningAdmissionPolicy.Criterion || string.IsNullOrWhiteSpace(draft.Question) ||
                draft.Question.Length > 1024 || string.IsNullOrWhiteSpace(draft.Rationale) || draft.Rationale.Length > 2048)
                throw new InvalidOperationException("Invalid committed draft.");
            if (state.Trials != 3 || state.Exposure != LearningExposure.Consumed || state.ReservedRuns != scope.MaximumEngineCalls ||
                state.Invocations != 1 || state.Submitted < 1 || commitment.ExecutionFingerprint != scope.ExecutionFingerprint ||
                commitment.InputChecksum != scope.Input.InputChecksum ||
                commitment.ProposalId != LearningIdentity.Hash(new
                {
                    Version = LearningAdmissionPolicy.Version,
                    commitment.InputChecksum,
                    commitment.ExecutionFingerprint,
                    Draft = commitment.Draft
                }))
                throw new InvalidOperationException("Invalid commitment.");
        }
        else if (state.Trials != 0 || state.ReservedRuns != 0 || state.EngineStarts != 0 || state.Result is not null)
            throw new InvalidOperationException("Missing commitment.");
        if ((state.Lifecycle is LearningLifecycle.Ready or LearningLifecycle.AwaitingProposal && state.Commitment is not null) ||
            (state.Lifecycle == LearningLifecycle.Ready && (state.Invocations != 0 || state.Submitted != 0)) ||
            (state.Lifecycle == LearningLifecycle.AwaitingProposal && (state.Invocations != 1 || state.Submitted != 0)) ||
            (state.Lifecycle == LearningLifecycle.Reserved && (state.Commitment is null || state.EngineStarts != 0)) ||
            (state.Lifecycle == LearningLifecycle.Running && (state.Commitment is null || state.EngineStarts != 1)))
            throw new InvalidOperationException("Invalid lifecycle.");
        if ((state.Lifecycle == LearningLifecycle.Completed) != (state.Result is not null) ||
            (state.Result is not null && (state.EngineStarts != 1 || state.UniqueRuns != state.Result.Runs.Length)))
            throw new InvalidOperationException("Invalid completion.");
    }

    private static void ValidateLearningEvaluation(LearningLedgerSnapshot state, RobustnessEvaluationResult value)
    {
        if (state.Scope is null || LearningIdentity.Hash(value.Plan) != LearningIdentity.Hash(state.Scope.Plan) ||
            value.Checksum != LearningIdentity.HashBytes(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value with { Checksum = "" }, EvaluationJson))) ||
            value.UnderlyingRunIds.Count == 0 || value.UnderlyingRunIds.Count > state.ReservedRuns ||
            value.UnderlyingRunIds.Distinct(StringComparer.Ordinal).Count() != value.UnderlyingRunIds.Count || value.SelectionGovernance is null)
            throw new InvalidOperationException("Evaluation does not bind to frozen plan.");
    }

    private static void ValidateLearningRun(LearningLedgerSnapshot state, BacktestResult value)
    {
        var plan = state.Scope!.Plan;
        if (value.Checksum != LearningIdentity.HashBytes(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value with { Checksum = "" }, EvaluationJson))) ||
            !value.Configuration.MarketRevisionIds.SequenceEqual(plan.MarketRevisionIds, StringComparer.Ordinal) ||
            value.Configuration.FeatureRevisionId != plan.FeatureRevisionId || value.Configuration.From < plan.From || value.Configuration.To > plan.To)
            throw new InvalidOperationException("Backtest does not bind to frozen evidence.");
    }
}
