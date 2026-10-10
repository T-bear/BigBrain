using System.Collections.Immutable;
using System.Text.Json;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

internal enum FiniteIterationState { Reserved, Completed, Failed, Indeterminate }
internal enum FiniteIterationFailure { ReasonerUnavailable, Cancelled, TimedOut, ExecutionOrPersistence }
internal sealed record FiniteResearchGrant(int Invocations, int Evaluations, int Trials, int UnderlyingCalls, int Concurrency, int Retries)
{
    internal static FiniteResearchGrant Initial => new(2, 1, 3, 64, 1, 0);
}
internal sealed record FiniteResearchIteration(int Number, LearningDevelopmentInput Input, DateTimeOffset ReservedAtUtc,
    FiniteIterationState State, DateTimeOffset? AvailableAtUtc = null, LearningAdmissionReason? Outcome = null,
    FiniteIterationFailure? Failure = null, string? ResponseChecksum = null, string? AuthorityChecksum = null);
internal sealed record FiniteResearchSession(string Version, string SessionId, FiniteResearchGrant Grant,
    DateTimeOffset CreatedAtUtc, string ScopeChecksum, ImmutableArray<FiniteResearchIteration> Iterations)
{
    internal const string Contract = "finance-finite-research-session-v1";
}

// Finance-owned finite orchestration and additive governance, in the existing Finance database.
// The narrow delegate is bound to IResearchReasoner.ReasonAsync by the caller. API has no Brain
// reference/registration, endpoint, worker configuration, scheduler or generic capability bag.
internal sealed partial class EodhdMarketMemory
{
    internal const string FiniteSessionMigration = """
        CREATE TABLE learning_finite_session(
          singleton INTEGER PRIMARY KEY CHECK(singleton=1),
          version TEXT NOT NULL, snapshot_json TEXT NOT NULL, checksum TEXT NOT NULL);
        """; // NO grant/seed/backfill/migration of existing v1 records.
    internal Action<string>? FiniteSessionTestHook { get; set; }

    internal FiniteResearchSession CreateFiniteResearchSession(SyntheticLearningScope scope, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        var now = clock.GetUtcNow();
        if (scope.ValidationReason != LearningAdmissionReason.Admitted || now.Offset != TimeSpan.Zero ||
            now < scope.Bars.Max(x => x.KnowledgeTimeUtc))
            throw new InvalidOperationException("Ineligible or future synthetic evidence.");
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireFiniteSchema(connection);
        var existing = ReadFiniteState(connection);
        if (existing is not null)
        {
            if (existing.ScopeChecksum != LearningIdentity.Hash(LearningFrozenScope.Freeze(scope)))
                throw new InvalidOperationException("Session scope is immutable.");
            return existing; // Reopening is not another grant.
        }
        var legacy = ReadLearningState(connection);
        if (legacy.Lifecycle != LearningLifecycle.Uninitialized)
            throw new InvalidOperationException("Existing v1 authority cannot acquire a finite session grant.");
        // Explicit atomic creation enrolls the still-unused scientific slot using v1's exact state
        // machine. No previously enrolled/spent record can enter this path. All later scientific
        // reservations, writes and recovery use the unchanged v1 APIs.
        legacy = AppendLearning(legacy with
        {
            Lifecycle = LearningLifecycle.Ready,
            Scope = LearningFrozenScope.Freeze(scope),
            Cohort = LearningCohort(scope),
            Exposure = LearningExposure.Unexposed
        }, new(LearningLedgerOutcome.Enrolled, null, null, null) { RecordedAtUtc = now });
        WriteFiniteScientificState(connection, transaction, legacy);
        var scopeChecksum = LearningIdentity.Hash(legacy.Scope);
        var session = new FiniteResearchSession(FiniteResearchSession.Contract,
            LearningIdentity.Hash(new { Version = FiniteResearchSession.Contract, scopeChecksum, CreatedAtUtc = now }),
            FiniteResearchGrant.Initial, now, scopeChecksum, []);
        WriteFiniteState(connection, transaction, session, insert: true);
        FiniteSessionTestHook?.Invoke("before-create-commit");
        transaction.Commit();
        return session;
    }

    internal FiniteResearchSession? ReadFiniteResearchSession()
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: true);
        return ReadFiniteState(connection);
    }

    // Explicit iteration number and expected history identity prevent stale requests becoming a
    // subsequent invocation. Replays return persisted outcomes without invoking any reasoner.
    internal LearningDevelopmentInput PreviewFiniteResearchInput(string sessionId, int iteration, DateTimeOffset cutoff)
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: true);
        var state = RequireFiniteSession(connection, sessionId);
        if (iteration > 0 && iteration <= state.Iterations.Length) return state.Iterations[iteration - 1].Input;
        return ProjectFiniteInput(connection, state, iteration, cutoff);
    }

    internal async Task<FiniteResearchIteration> RunFiniteResearchIterationAsync(string sessionId, int iteration,
        string expectedInputChecksum, DateTimeOffset cutoff, TimeProvider clock,
        Func<LearningDevelopmentInput, CancellationToken, Task<LearningReasonerReply>> reason,
        CancellationToken cancellationToken, TimeSpan? reasonerCompletionTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(reason);
        cancellationToken.ThrowIfCancellationRequested();
        // Trusted composition supplies the actual reasoner deadline plus its bounded cleanup.
        // Model-free callers retain the projected default; neither duration grants more science.
        // Validate before spending authority. Reservation rechecks the projection atomically.
        var preview = PreviewFiniteResearchInput(sessionId, iteration, cutoff);
        var completionTimeout = reasonerCompletionTimeout ?? TimeSpan.FromSeconds(preview.Limits.ReasonerDeadlineSeconds);
        if (completionTimeout <= TimeSpan.Zero || completionTimeout > TimeSpan.FromSeconds(preview.Limits.IterationCapSeconds))
            throw new ArgumentOutOfRangeException(nameof(reasonerCompletionTimeout));
        var reserved = ReserveFiniteIteration(sessionId, iteration, expectedInputChecksum, cutoff, clock.GetUtcNow(), out var replay);
        if (replay) return reserved;
        FiniteSessionTestHook?.Invoke("after-invocation-commit");
        LearningReasonerReply reply;
        using var expiry = new CancellationTokenSource(completionTimeout, clock);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, expiry.Token);
        try
        {
            reply = await reason(reserved.Input, deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
            deadline.Token.ThrowIfCancellationRequested(); // Discard even a typed late reply.
        }
        catch (Exception)
        {
            var failure = cancellationToken.IsCancellationRequested ? FiniteIterationFailure.Cancelled :
                deadline.IsCancellationRequested ? FiniteIterationFailure.TimedOut : FiniteIterationFailure.ReasonerUnavailable;
            if (iteration == 1) FailLearningIteration(failure == FiniteIterationFailure.TimedOut
                ? LearningFailure.ReasonerTimeout : LearningFailure.ReasonerUnavailable);
            return FinishFiniteIteration(sessionId, iteration, clock.GetUtcNow(), null, failure, null);
        }
        // Closed replies are still untrusted. Reparse the exact original bytes against the frozen
        // input. No raw response or model rationale is copied into the history projection.
        var wire = reply?.ResponseJson;
        var parsed = LearningReplyParser.Parse(wire!, reserved.Input);
        var responseChecksum = wire is null ? null : LearningIdentity.Hash(wire);
        if (iteration == 2)
        {
            var outcome = parsed.Rejection ?? (parsed.Reply is LearningReasonerReply.NoUsefulProposal
                ? LearningAdmissionReason.NoUsefulProposal : LearningAdmissionReason.BudgetExceeded);
            return FinishFiniteIteration(sessionId, iteration, clock.GetUtcNow(), outcome, null, responseChecksum);
        }
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var scope = ReadLearningLedger().Scope!.Resolve();
            var admission = ReserveLearningProposal(wire!, scope); // Unchanged pure Finance admission + v1 atomic budget/exposure.
            if (admission.Reason == LearningAdmissionReason.Admitted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var frozen = StartLearningExecution(admission.ProposalId!);
                FiniteSessionTestHook?.Invoke("before-engine");
                var build = DeterministicRobustnessEvaluator.Evaluate(frozen.Plan, new MomentumResearchStrategy(), frozen.Bars, frozen.Features);
                PersistLearningResults(admission.ProposalId!, build);
                FiniteSessionTestHook?.Invoke("after-results-before-binding");
                CompleteLearningIteration(admission.ProposalId!, build.Evaluation.EvaluationId);
            }
            return FinishFiniteIteration(sessionId, iteration, clock.GetUtcNow(), admission.Reason, null, responseChecksum);
        }
        catch (Exception)
        {
            // Never overwrite completed native evidence or imply that computation did not happen.
            MarkLearningIndeterminate();
            return FinishFiniteIteration(sessionId, iteration, clock.GetUtcNow(), null,
                FiniteIterationFailure.ExecutionOrPersistence, responseChecksum);
        }
    }

    // Explicit operator recovery, no computation/result reconstruction and no refund. A pending
    // reservation on reopen blocks both iterations until classified, then remains terminal.
    internal void MarkFiniteResearchIndeterminate(string sessionId, TimeProvider clock)
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        var state = RequireFiniteSession(connection, sessionId);
        if (state.Iterations.Length == 0 || state.Iterations[^1].State != FiniteIterationState.Reserved) return;
        var step = state.Iterations[^1];
        var now = clock.GetUtcNow();
        if (now < step.ReservedAtUtc) throw new InvalidOperationException("Recovery clock moved backwards.");
        var updated = step with { State = FiniteIterationState.Indeterminate, AvailableAtUtc = now };
        WriteFiniteState(connection, transaction, state with { Iterations = state.Iterations.SetItem(step.Number - 1, updated) });
        transaction.Commit();
    }

    private FiniteResearchIteration ReserveFiniteIteration(string sessionId, int iteration, string checksum,
        DateTimeOffset cutoff, DateTimeOffset now, out bool replay)
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        var state = RequireFiniteSession(connection, sessionId);
        replay = iteration > 0 && iteration <= state.Iterations.Length;
        if (replay)
        {
            var previous = state.Iterations[iteration - 1];
            if (previous.Input.InputChecksum != checksum || previous.State == FiniteIterationState.Reserved)
                throw new InvalidOperationException("Stale identity or active/uncertain invocation; no retry authority.");
            return previous;
        }
        if (cutoff > now || now.Offset != TimeSpan.Zero) throw new InvalidOperationException("Future knowledge cutoff.");
        var input = ProjectFiniteInput(connection, state, iteration, cutoff);
        if (input.InputChecksum != checksum) throw new InvalidOperationException("History identity mismatch.");
        if (iteration == 1)
        {
            // Same v1 transition, committed atomically with the new invocation. No gap leaves
            // an uncertain new call beside a still-Ready legacy opportunity after a crash.
            var legacy = ReadLearningState(connection);
            if (legacy.Lifecycle != LearningLifecycle.Ready || legacy.Invocations != 0 || legacy.Exposure != LearningExposure.Unexposed)
                throw new InvalidOperationException("No unused scientific invocation authority.");
            WriteFiniteScientificState(connection, transaction, AppendLearning(legacy with
            { Lifecycle = LearningLifecycle.AwaitingProposal, Invocations = 1 },
                new(LearningLedgerOutcome.InvocationReserved, null, null, null) { RecordedAtUtc = now }));
        }
        var step = new FiniteResearchIteration(iteration, input, now, FiniteIterationState.Reserved);
        WriteFiniteState(connection, transaction, state with { Iterations = state.Iterations.Add(step) });
        FiniteSessionTestHook?.Invoke("before-invocation-commit");
        transaction.Commit();
        return step;
    }

    private FiniteResearchIteration FinishFiniteIteration(string sessionId, int iteration, DateTimeOffset now,
        LearningAdmissionReason? outcome, FiniteIterationFailure? failure, string? responseChecksum)
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        var state = RequireFiniteSession(connection, sessionId);
        var step = state.Iterations[iteration - 1];
        if (step.State != FiniteIterationState.Reserved || now.Offset != TimeSpan.Zero || now < step.ReservedAtUtc)
            throw new InvalidOperationException("No active invocation or nonmonotonic completion.");
        var updated = step with
        {
            State = failure is null ? FiniteIterationState.Completed : FiniteIterationState.Failed,
            AvailableAtUtc = now,
            Outcome = outcome,
            Failure = failure,
            ResponseChecksum = responseChecksum,
            AuthorityChecksum = LearningIdentity.Hash(ReadLearningState(connection))
        };
        WriteFiniteState(connection, transaction, state with { Iterations = state.Iterations.SetItem(iteration - 1, updated) });
        transaction.Commit();
        return updated;
    }

    private static LearningDevelopmentInput ProjectFiniteInput(SqliteConnection connection, FiniteResearchSession state,
        int iteration, DateTimeOffset cutoff)
    {
        if (iteration is < 1 or > 2 || iteration != state.Iterations.Length + 1 || cutoff.Offset != TimeSpan.Zero ||
            cutoff < state.CreatedAtUtc || state.Iterations.Any(x => x.State != FiniteIterationState.Completed))
            throw new InvalidOperationException("No ordered finite invocation authority or eligible history.");
        var legacy = ReadLearningState(connection);
        var scope = legacy.Scope!.Resolve();
        if (iteration == 1)
        {
            if (legacy.Lifecycle != LearningLifecycle.Ready) throw new InvalidOperationException("Scientific opportunity already used.");
            return scope.Input;
        }
        var previous = state.Iterations[0];
        if (previous.AvailableAtUtc is not { } available || available > cutoff || previous.Outcome is not { } outcome ||
            previous.AuthorityChecksum != LearningIdentity.Hash(legacy))
            throw new InvalidOperationException("Unavailable or changed scientific history.");
        decimal? developmentReturn = null;
        if (outcome == LearningAdmissionReason.Admitted)
        {
            if (legacy.Result is null) throw new InvalidOperationException("Missing native result binding.");
            // ReadLearningState already validates immutable native references/checksums. Only the
            // PRESELECTION, fixed-reference validation return crosses this explicit projection.
            var json = EvaluationScalar(connection, "SELECT result_json FROM robustness_evaluations WHERE evaluation_id=$id", ("$id", legacy.Result.EvaluationId));
            developmentReturn = JsonSerializer.Deserialize<RobustnessEvaluationResult>(json, EvaluationJson)!.PrimarySplit.Test.ExcessReturn;
        }
        var history = new FiniteResearchHistory(cutoff, available, outcome, developmentReturn);
        var input = scope.Input with
        {
            ProjectionVersion = history.Version,
            FiniteHistory = history,
            HistoryChecksum = LearningIdentity.Hash(history),
            InputChecksum = ""
        };
        return input with { InputChecksum = LearningIdentity.Hash(input) };
    }

    private static FiniteResearchSession RequireFiniteSession(SqliteConnection connection, string id)
    {
        var state = ReadFiniteState(connection) ?? throw new InvalidOperationException("No explicitly created finite session.");
        if (state.SessionId != id) throw new InvalidOperationException("Session identity mismatch.");
        return state;
    }

    private static FiniteResearchSession? ReadFiniteState(SqliteConnection connection)
    {
        RequireFiniteSchema(connection);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT version,snapshot_json,checksum FROM learning_finite_session WHERE singleton=1";
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        if (reader.GetString(0) != FiniteResearchSession.Contract || reader.GetString(1).Length > 65536)
            throw new InvalidOperationException("Unsupported or oversized finite session.");
        using var document = JsonDocument.Parse(reader.GetString(1));
        RequireUniqueLearningProperties(document.RootElement);
        var state = JsonSerializer.Deserialize<FiniteResearchSession>(reader.GetString(1), LearningJson)
            ?? throw new InvalidOperationException("Missing finite session.");
        if (LearningIdentity.Hash(state) != reader.GetString(2)) throw new InvalidOperationException("Finite session integrity mismatch.");
        reader.Close();
        ValidateFiniteState(connection, state);
        return state;
    }

    private static void ValidateFiniteState(SqliteConnection connection, FiniteResearchSession state)
    {
        var legacy = ReadLearningState(connection);
        if (state.Version != FiniteResearchSession.Contract || state.Grant != FiniteResearchGrant.Initial ||
            state.CreatedAtUtc.Offset != TimeSpan.Zero || state.Iterations.IsDefault || state.Iterations.Length > 2 ||
            legacy.Scope is null || state.ScopeChecksum != LearningIdentity.Hash(legacy.Scope) ||
            state.CreatedAtUtc < legacy.Scope.Bars.Max(x => x.KnowledgeTimeUtc) ||
            state.SessionId != LearningIdentity.Hash(new
            {
                Version = FiniteResearchSession.Contract,
                scopeChecksum = state.ScopeChecksum,
                CreatedAtUtc = state.CreatedAtUtc
            }))
            throw new InvalidOperationException("Invalid finite session grant/lineage.");
        for (var index = 0; index < state.Iterations.Length; index++)
        {
            var step = state.Iterations[index];
            if (step.Number != index + 1 || !Enum.IsDefined(step.State) || step.ReservedAtUtc.Offset != TimeSpan.Zero ||
                step.ReservedAtUtc < state.CreatedAtUtc || step.Input is null ||
                (step.Outcome is { } reason && !Enum.IsDefined(reason)) ||
                (step.Failure is { } failure && !Enum.IsDefined(failure)) ||
                (step.State == FiniteIterationState.Reserved) != (step.AvailableAtUtc is null) ||
                (step.AvailableAtUtc is { } at && (at.Offset != TimeSpan.Zero || at < step.ReservedAtUtc)) ||
                (step.State == FiniteIterationState.Completed) != (step.Outcome is not null) ||
                (step.State == FiniteIterationState.Failed) != (step.Failure is not null))
                throw new InvalidOperationException("Invalid finite iteration state.");
            if (index == 0)
            {
                if (step.State == FiniteIterationState.Completed &&
                    (legacy.History.LastOrDefault(x => x.Submission)?.Reason != step.Outcome ||
                     (step.Outcome == LearningAdmissionReason.Admitted && legacy.Result is null)))
                    throw new InvalidOperationException("Outcome does not bind to native scientific history.");
                if (LearningIdentity.Hash(step.Input) != LearningIdentity.Hash(legacy.Scope.Resolve().Input))
                    throw new InvalidOperationException("Initial projection mismatch.");
            }
            else
            {
                if (step.Outcome is LearningAdmissionReason.Admitted or LearningAdmissionReason.Duplicate)
                    throw new InvalidOperationException("No second scientific admission or independent success.");
                var prefix = state with { Iterations = state.Iterations.Take(index).ToImmutableArray() };
                var cutoff = step.Input.FiniteHistory?.KnowledgeCutoffUtc ?? throw new InvalidOperationException("Missing history.");
                if (cutoff > step.ReservedAtUtc || LearningIdentity.Hash(step.Input) !=
                    LearningIdentity.Hash(ProjectFiniteInput(connection, prefix, step.Number, cutoff)))
                    throw new InvalidOperationException("Frozen history projection mismatch.");
            }
        }
        var last = state.Iterations.LastOrDefault();
        if (last is { State: FiniteIterationState.Completed or FiniteIterationState.Failed } &&
            last.AuthorityChecksum != LearningIdentity.Hash(legacy))
            throw new InvalidOperationException("Scientific authority changed after completion.");
    }

    private static void WriteFiniteState(SqliteConnection connection, SqliteTransaction transaction, FiniteResearchSession state, bool insert = false)
    {
        ValidateFiniteState(connection, state);
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = insert
            ? "INSERT INTO learning_finite_session VALUES(1,$version,$json,$checksum)"
            : "UPDATE learning_finite_session SET snapshot_json=$json,checksum=$checksum WHERE singleton=1 AND version=$version";
        command.Parameters.AddWithValue("$version", FiniteResearchSession.Contract);
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(state, LearningJson));
        command.Parameters.AddWithValue("$checksum", LearningIdentity.Hash(state));
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Missing finite session authority.");
    }

    private static void WriteFiniteScientificState(SqliteConnection connection, SqliteTransaction transaction, LearningLedgerSnapshot state)
    {
        ValidateLearningState(state);
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "UPDATE learning_governance SET snapshot_json=$json,checksum=$checksum WHERE singleton=1 AND version=$version";
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(state, LearningJson));
        command.Parameters.AddWithValue("$checksum", LearningIdentity.Hash(state));
        command.Parameters.AddWithValue("$version", LearningLedgerSnapshot.Contract);
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Missing scientific authority.");
    }

    private static void RequireFiniteSchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN MAX(version) IN (95,96,97,98) AND SUM(CASE WHEN version=94 THEN 1 ELSE 0 END)=1 THEN 1 ELSE 0 END FROM finance_schema_migrations";
        if (Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) != 1)
            throw new InvalidOperationException("Unsupported finite session schema.");
    }
}
