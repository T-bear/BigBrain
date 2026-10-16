using System.Collections.Immutable;
using System.Text.Json;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

// Trusted composition binds IResearchReasoner.ReasonAsync and its validated completion/cleanup bound.
internal sealed record FinanceRecurringReasonerBinding(string Identity,
    Func<LearningDevelopmentInput, CancellationToken, Task<LearningReasonerReply>> Reason,
    TimeSpan CompletionTimeout, bool ModelFreeFixture = false);

internal sealed class RecurringResearchReconciliationRequiredException : InvalidOperationException
{
    internal const string Reason = "finance.research.recurring.restoreReconciliationRequired";
    internal RecurringResearchReconciliationRequiredException() : base(Reason) { }
}

// Closed transport metadata only. No prompt, reply, path or model-provided rationale.
internal sealed record FinanceRecurringRuntimeAudit(Guid InvocationId, string Phase, string Outcome,
    string? Failure, DateTimeOffset AtUtc, long ElapsedMilliseconds, string InputHash,
    string WorkerConfigurationHash, string? ResponseHash, int? WorkerExitCode);

internal enum RecurringResearchState { Ready, ReservedInference, ReservedEvaluation, Completed, Stopped, Indeterminate }
internal sealed record RecurringResearchScope(ImmutableArray<string> ReceiptIds, string EvidenceChecksum,
    EvaluationPlan Plan, ImmutableArray<BacktestMarketBar> Bars, ImmutableArray<BacktestFeatureValue> Features,
    LearningDevelopmentInput Input);
internal sealed record RecurringResearchAttempt(int Number, DateTimeOffset CutoffUtc, RecurringResearchScope Scope,
    string Outcome, DateTimeOffset? AvailableAtUtc = null, string? ResponseChecksum = null,
    string? EvaluationId = null, string? EvaluationChecksum = null, decimal? ReferenceValidationExcessReturn = null, long? ReasonerElapsedMilliseconds = null, LearningProposalDraft? Proposal = null)
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ImmutableArray<FinanceRecurringRuntimeAudit>? RuntimeAudit { get; init; }
}
internal sealed record RecurringResearchProgram(string Id, RecurringResearchAuthorization Authorization,
    bool ModelFreeFixture,
    DateTimeOffset RegisteredAtUtc, DateTimeOffset WatermarkUtc, RecurringResearchState State,
    int Invocations, int Evaluations, int Trials, int Calls, int ReservedSeconds,
    ImmutableArray<RecurringResearchAttempt> Attempts, string? StopReason = null);
internal sealed class RecurringResearchDataPendingException : InvalidOperationException;

internal sealed partial class EodhdMarketMemory
{
    internal const string RecurringResearchMigration = """
        CREATE TABLE recurring_research_program(singleton INTEGER PRIMARY KEY CHECK(singleton=1),checksum TEXT NOT NULL,snapshot_json TEXT NOT NULL);
        CREATE TABLE finance_scientific_lease(owner TEXT PRIMARY KEY);
        """;
    // No seeds, grant migration, inferred owner decision or automatic new program after completion.
    private static readonly JsonSerializerOptions RecurringJson = new(ObservationJson);
    private static readonly JsonSerializerOptions RecurringGrantJson = new(ObservationJson)
    { UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };
    internal static byte[] ExportRecurringAuthorization(RecurringResearchAuthorization value) => JsonSerializer.SerializeToUtf8Bytes(value, RecurringGrantJson);
    internal static RecurringResearchAuthorization ParseRecurringAuthorization(byte[] value) =>
        JsonSerializer.Deserialize<RecurringResearchAuthorization>(value, RecurringGrantJson) ?? throw new InvalidDataException();
    internal Action<string>? RecurringResearchTestHook { get; set; }
    internal RecurringResearchAuthority? RecurringAuthority { get; set; }
    internal void RequireRecurringAuthority(RecurringResearchProgram state)
    {
        if (state.ModelFreeFixture && RecurringAuthority is null) return;
        (RecurringAuthority ?? throw new RecurringResearchReconciliationRequiredException()).Require(state);
    }

    internal RecurringResearchProgram CreateRecurringResearchProgram(RecurringResearchAuthorization authorization, TimeProvider clock, bool modelFreeFixture = false)
    {
        var now = clock.GetUtcNow();
        using var c = new SqliteConnection(ConnectionString); c.Open(); using var tx = c.BeginTransaction(deferred: false);
        var existing = ReadRecurring(c);
        if (existing is not null)
        {
            if (existing.ModelFreeFixture != modelFreeFixture || LearningIdentity.Hash(existing.Authorization) != LearningIdentity.Hash(authorization))
                throw new InvalidOperationException("Existing recurring authority cannot be replaced or renewed.");
            return existing;
        }
        authorization.Validate(now);
        RequireRecurringClock(c, now, now);
        if (ReadFiniteState(c)?.Iterations.Any(x => x.State is FiniteIterationState.Reserved or FiniteIterationState.Indeterminate) == true)
            throw new InvalidOperationException("Competing finite session authority.");
        if (EvaluationScalarOrNull(c, "SELECT owner FROM finance_scientific_lease LIMIT 1") is not null ||
            EvaluationScalarOrNull(c, "SELECT run_id FROM research_runs WHERE state='Running' LIMIT 1") is not null ||
            ReadLearningState(c).Lifecycle is LearningLifecycle.AwaitingProposal or LearningLifecycle.Reserved or LearningLifecycle.Running or LearningLifecycle.Indeterminate)
            throw new InvalidOperationException("Competing or uncertain scientific authority.");
        // Fresh holdouts + exclusive science prevent unknown historical exposure. No retroactive
        // registration against previously retained future-source evidence is accepted.
        if (authorization.Historical is not null)
            _ = ReadHistoricalResearchSource(c, authorization, now);
        foreach (var cohort in authorization.Historical is null ? authorization.Cohorts : [])
        {
            var rows = DailyRows(c, now, authorization.Instrument.Id.Value, authorization.Mapping.Provider.Value, authorization.Mapping.ProviderDataset.Value);
            var sessions = RecurringResearchAuthorization.Sessions(cohort);
            var plan = DeterministicRobustnessEvaluator.CreatePlan(["pending"], "pending", new MomentumResearchStrategy(), [authorization.Instrument.Id.Value], cohort.From, cohort.To);
            var holdout = DeterministicRobustnessEvaluator.CreatePartition(sessions, plan.AntiOverfitting!).HoldoutFrom;
            if (rows.Any(x => x.Value.Daily!.SourceDate >= holdout && x.Value.Daily.SourceDate <= cohort.To))
                throw new InvalidOperationException("Holdout exposure is not fresh.");
        }
        var value = new RecurringResearchProgram(LearningIdentity.Hash(new { Version = RecurringResearchAuthorization.Version, authorization, registered = now }),
            authorization, modelFreeFixture, now, now, RecurringResearchState.Ready, 0, 0, 0, 0, 0, []);
        WriteRecurring(c, tx, value); SealDailyCutoff(c, tx, now); tx.Commit(); return value;
    }

    internal static RecurringResearchProgram ReadRecurringAuthorityState(string database)
    {
        using var c = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = database, Mode = SqliteOpenMode.ReadOnly }.ToString());
        c.Open(); using var tx = c.BeginTransaction(deferred: true);
        return ReadRecurring(c) ?? throw new RecurringResearchReconciliationRequiredException();
    }

    internal RecurringResearchProgram? ReadRecurringResearchProgram()
    {
        using var c = new SqliteConnection(ConnectionString); c.Open(); using var tx = c.BeginTransaction(deferred: true);
        return ReadRecurring(c);
    }
    private static RecurringResearchProgram? ReadRecurring(SqliteConnection c)
    {
        using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT checksum,snapshot_json FROM recurring_research_program WHERE singleton=1";
        using var r = cmd.ExecuteReader(); if (!r.Read()) return null;
        var json = r.GetString(1); if (json.Length > 8_000_000) throw new InvalidDataException("Recurring manifest exceeds bound.");
        var value = JsonSerializer.Deserialize<RecurringResearchProgram>(json, RecurringJson) ?? throw new InvalidDataException("Missing recurring manifest.");
        if (LearningIdentity.Hash(value) != r.GetString(0)) throw new InvalidDataException("Recurring manifest checksum mismatch.");
        r.Close(); value.Authorization.Validate(value.RegisteredAtUtc);
        if (value.Id != LearningIdentity.Hash(new { Version = RecurringResearchAuthorization.Version, authorization = value.Authorization, registered = value.RegisteredAtUtc }) ||
            value.Invocations != value.Attempts.Length || value.Invocations > value.Authorization.Budget.Invocations ||
            value.Evaluations > value.Authorization.Budget.Evaluations || value.Trials != value.Evaluations * 3 || value.Calls != value.Evaluations * 64 ||
            value.ReservedSeconds != value.Invocations * value.Authorization.Budget.CompletionSeconds || value.WatermarkUtc < value.RegisteredAtUtc)
            throw new InvalidDataException("Recurring authority mismatch.");
        foreach (var attempt in value.Attempts)
        {
            var reconstructed = ProjectRecurring(c, value, attempt.Number, attempt.CutoffUtc);
            if (LearningIdentity.Hash(reconstructed) != LearningIdentity.Hash(attempt.Scope))
                throw new InvalidDataException("Recurring history/evidence identity mismatch.");
            if (attempt.EvaluationId is { } id)
            {
                var stored = EvaluationScalarOrNull(c, "SELECT checksum FROM robustness_evaluations WHERE evaluation_id=$id", ("$id", id));
                if (stored != attempt.EvaluationChecksum) throw new InvalidDataException("Native scientific evidence missing or changed.");
                var evaluation = JsonSerializer.Deserialize<RobustnessEvaluationResult>(EvaluationScalar(c,
                    "SELECT result_json FROM robustness_evaluations WHERE evaluation_id=$id", ("$id", id)), EvaluationJson)!;
                if (evaluation.EvaluationId != id || evaluation.Checksum != stored ||
                    evaluation.Checksum != LearningIdentity.HashBytes(JsonSerializer.SerializeToUtf8Bytes(evaluation with { Checksum = "" }, EvaluationJson)) ||
                    LearningIdentity.Hash(evaluation.Plan) != LearningIdentity.Hash(attempt.Scope.Plan) ||
                    evaluation.PrimarySplit.Test.ExcessReturn != attempt.ReferenceValidationExcessReturn)
                    throw new InvalidDataException("Native evaluation commitment mismatch.");
                foreach (var runId in evaluation.UnderlyingRunIds)
                {
                    var run = JsonSerializer.Deserialize<BacktestResult>(EvaluationScalar(c,
                        "SELECT result_json FROM backtest_runs WHERE run_id=$id", ("$id", runId)), EvaluationJson)!;
                    if (run.RunId != runId || run.Checksum != EvaluationScalar(c, "SELECT checksum FROM backtest_runs WHERE run_id=$id", ("$id", runId)) ||
                        run.Checksum != LearningIdentity.HashBytes(JsonSerializer.SerializeToUtf8Bytes(run with { Checksum = "" }, EvaluationJson)) ||
                        !run.Configuration.MarketRevisionIds.SequenceEqual(attempt.Scope.Plan.MarketRevisionIds) ||
                        run.Configuration.FeatureRevisionId != attempt.Scope.Plan.FeatureRevisionId)
                        throw new InvalidDataException("Native underlying run commitment mismatch.");
                }
            }
        }
        return value;
    }
    internal void RecordRecurringRuntimeAudit(FinanceRecurringRuntimeAudit audit)
    {
        FinanceTime.RequireUtc(audit.AtUtc, nameof(audit.AtUtc));
        if (audit.InvocationId == Guid.Empty || audit.ElapsedMilliseconds < 0 ||
            audit.Phase is not ("Started" or "ResponseStarted" or "Completed") ||
            audit.Outcome is not ("Started" or "Proposal" or "NoUsefulProposal" or "Cancelled" or "Disabled" or "Failed") ||
            audit.Failure is not (null or "Disabled" or "Busy" or "InvalidInput" or "InvalidTransport" or "InvalidReply" or "Timeout" or "WorkerFailed" or "AuditFailed" or "StopUnconfirmed") ||
            !IsHash(audit.WorkerConfigurationHash) || !(audit.InputHash == "" || IsHash(audit.InputHash)) ||
            audit.ResponseHash is not null && !IsHash(audit.ResponseHash))
            throw new InvalidDataException("Invalid recurring runtime audit.");
        using var c = new SqliteConnection(ConnectionString); c.Open(); using var tx = c.BeginTransaction(deferred: false);
        var program = ReadRecurring(c) ?? throw new InvalidDataException("Runtime audit requires reserved authority.");
        if (program.State is not (RecurringResearchState.ReservedInference or RecurringResearchState.Indeterminate) || program.Attempts.IsEmpty)
            throw new InvalidDataException("Runtime audit requires reserved inference.");
        var attempt = program.Attempts[^1]; var events = attempt.RuntimeAudit ?? [];
        if (events.Length >= 4 || events.Any(x => x.InvocationId != audit.InvocationId) ||
            events.IsEmpty && audit.Phase != "Started" ||
            !events.IsEmpty && (audit.Phase == "Started" || audit.ElapsedMilliseconds < events[^1].ElapsedMilliseconds))
            throw new InvalidDataException("Runtime audit ownership/order mismatch.");
        WriteRecurring(c, tx, program with
        {
            Attempts = program.Attempts.SetItem(program.Attempts.Length - 1,
            attempt with { RuntimeAudit = events.Add(audit) })
        });
        tx.Commit();
        static bool IsHash(string value) => value.Length == 64 && value.All(x => x is >= '0' and <= '9' or >= 'a' and <= 'f');
    }

    private void WriteRecurring(SqliteConnection c, SqliteTransaction tx, RecurringResearchProgram value)
    {
        var oldJson = EvaluationScalarOrNull(c, "SELECT snapshot_json FROM recurring_research_program WHERE singleton=1");
        if (oldJson is not null && (!value.ModelFreeFixture || RecurringAuthority is not null))
        {
            var old = ReadRecurring(c) ?? throw new RecurringResearchReconciliationRequiredException();
            (RecurringAuthority ?? throw new RecurringResearchReconciliationRequiredException()).Advance(old, value);
        }
        Execute(c, tx, "INSERT INTO recurring_research_program VALUES(1,$hash,$json) ON CONFLICT(singleton) DO UPDATE SET checksum=$hash,snapshot_json=$json",
            ("$hash", LearningIdentity.Hash(value)), ("$json", JsonSerializer.Serialize(value, RecurringJson)));
    }
    private static void RequireRecurringClock(SqliteConnection c, DateTimeOffset now, DateTimeOffset previous)
    {
        FinanceTime.RequireUtc(now, nameof(now));
        if (now < previous || now.UtcTicks < ObservationWatermark(c)) throw new InvalidOperationException("Research clock regression.");
    }
    private static RecurringResearchScope ProjectRecurring(SqliteConnection c, RecurringResearchProgram state, int number, DateTimeOffset cutoff)
    {
        if (state.Authorization.Historical is not null) return ProjectHistoricalResearch(c, state, number, cutoff);
        var grant = state.Authorization; var cohort = grant.Cohorts[number - 1];
        var rows = LatestDailyAtCutoff(DailyRows(c, cutoff, grant.Instrument.Id.Value, grant.Mapping.Provider.Value, grant.Mapping.ProviderDataset.Value)
            .Where(x => x.Value.Daily!.SourceDate >= cohort.From && x.Value.Daily.SourceDate <= cohort.To).ToImmutableArray());
        var sessions = RecurringResearchAuthorization.Sessions(cohort);
        if (!rows.Select(x => x.Value.Daily!.SourceDate).SequenceEqual(sessions)) throw new RecurringResearchDataPendingException();
        if (rows.Any(x => x.Instrument != grant.Instrument || x.Mapping != grant.Mapping || x.Origin != (state.ModelFreeFixture ? MarketObservationOrigin.DeterministicFixture : MarketObservationOrigin.AcquiredProviderDaily) ||
            x.Value.Daily!.SourceContract != DailyMarketEvidence.SourceContract || x.KnowledgeTimeUtc > cutoff ||
            x.Value.Volume != decimal.Truncate(x.Value.Volume)))
            throw new InvalidDataException("Ineligible recurring receipt provenance.");
        // No late daily acquisition may masquerade as evidence available for the next-open fill.
        foreach (var row in rows)
        {
            var next = row.Value.Daily!.SourceDate.AddDays(1); while (!UsMarketCalendar.IsSession(next)) next = next.AddDays(1);
            if (row.KnowledgeTimeUtc >= UsMarketCalendar.Session(next)!.OpenUtc)
                throw new InvalidDataException("Daily evidence was unknown before the next execution open.");
        }
        var featureBuild = DeterministicDailyFeatureEngine.Build(rows.Select(x => new DailyFeatureObservation(x.Instrument.Id,
            x.Value.Daily!.SourceDate, x.Value.Open, x.Value.High, x.Value.Low, x.Value.Close, x.Value.Volume, x.KnowledgeTimeUtc, new(x.Id))));
        var evidence = LearningIdentity.Hash(rows.Select(x => new { x.Id, x.Checksum }).ToArray());
        var bars = rows.Select(x => new BacktestMarketBar(x.Instrument.Id, evidence, x.Value.Daily!.SourceDate,
            x.Value.Open, x.Value.Close, x.KnowledgeTimeUtc, checked((long)x.Value.Volume))).ToImmutableArray();
        var features = featureBuild.Values.Where(x => x.DefinitionId is "momentum.5" or "momentum.10" or "momentum.20")
            .Select(x => new BacktestFeatureValue(x.InstrumentId, x.SessionDate, x.DefinitionId, x.Value, x.KnowledgeTimeUtc, featureBuild.DeterministicChecksum)).ToImmutableArray();
        return BuildResearchScope(state, number, cutoff, rows.Select(x => x.Id).ToImmutableArray(), evidence, featureBuild.DeterministicChecksum, bars, features, sessions);
    }

    private static RecurringResearchScope BuildResearchScope(RecurringResearchProgram state, int number, DateTimeOffset cutoff,
        ImmutableArray<string> identities, string evidence, string featureChecksum, ImmutableArray<BacktestMarketBar> bars,
        ImmutableArray<BacktestFeatureValue> features, DateOnly[] sessions)
    {
        var grant = state.Authorization; var cohort = grant.Cohorts[number - 1];
        var plan = DeterministicRobustnessEvaluator.CreatePlan([evidence], featureChecksum, new MomentumResearchStrategy(),
            [grant.Instrument.Id.Value], cohort.From, cohort.To);
        var partition = DeterministicRobustnessEvaluator.CreatePartition(sessions, plan.AntiOverfitting!);
        var development = bars.Where(x => x.SessionDate <= partition.ValidationTo).ToArray();
        var available = development.Length - plan.WalkForward.InitialTrainSessions - plan.EmbargoSessions - plan.WalkForward.TestSessions;
        if (30 + 4 * (available < 0 ? 0 : 1 + available / plan.WalkForward.StepSessions) > 64)
            throw new InvalidDataException("Scientific call envelope exceeds grant.");
        var history = new RecurringResearchHistory(RecurringResearchAuthorization.Version,
            state.Attempts.Where(x => x.Number < number).Select(x =>
            {
                // Release history only at the next precommitted opportunity, never expose the
                // evaluator's data-dependent completion timing as a holdout proxy.
                var released = grant.Cohorts[x.Number].DueAtUtc;
                if (x.AvailableAtUtc is null || x.AvailableAtUtc > released || released > cutoff)
                    throw new InvalidDataException("History missed its precommitted release boundary.");
                return new RecurringResearchHistoryEntry(x.Number, x.Outcome, released, x.ReferenceValidationExcessReturn);
            }).ToImmutableArray());
        // Strip receipt, full feature-revision and selected/holdout identities before hashing visible input.
        var devHash = LearningIdentity.Hash(new
        {
            Bars = development.Select(x => new { x.SessionDate, x.Open, x.Close, x.Volume, x.KnowledgeTimeUtc }).ToArray(),
            Features = features.Where(x => x.SessionDate <= partition.ValidationTo).Select(x => new { x.SessionDate, x.DefinitionId, x.Value, x.KnowledgeTimeUtc }).ToArray()
        });
        var input = new LearningDevelopmentInput(LearningAdmissionPolicy.Version, grant.Historical is null ? "recurring-development-v1" : HistoricalResearchAdmission.Projection,
            grant.Historical is null ? "real-daily-momentum20-v1" : HistoricalResearchAdmission.Scope,
            grant.Historical is null ? "real-next-session-direction" : "historical-next-session-direction", cutoff, development.Length, devHash, "momentum/v1", 20, 3, LearningIdentity.Hash(history), "")
        {
            RecurringHistory = history,
            HistoricalDevelopment = grant.Historical is null ? null : new(
            development[0].SessionDate, development[^1].SessionDate, development.Length, development[0].Close, development[^1].Close,
            development.Min(x => x.Close), development.Max(x => x.Close), development.TakeLast(8).Select(x =>
                new HistoricalDevelopmentSample(x.SessionDate, x.Close, features.SingleOrDefault(f => f.SessionDate == x.SessionDate && f.DefinitionId == "momentum.20")?.Value)).ToImmutableArray())
        };
        input = input with { InputChecksum = LearningIdentity.Hash(input) };
        return new(identities, evidence, plan, bars, features, input);
    }

    // Internal trusted composition only. The caller binds this delegate to IResearchReasoner;
    // no fake production provider, model download, endpoint or automatic grant provisioning.
    internal async Task<RecurringResearchProgram> RunRecurringResearchAsync(int number, string reasonerIdentity,
        TimeProvider clock, Func<LearningDevelopmentInput, CancellationToken, Task<LearningReasonerReply>> reason,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        RecurringResearchProgram state;
        using (var c = new SqliteConnection(ConnectionString))
        {
            c.Open(); using var tx = c.BeginTransaction(deferred: false);
            state = ReadRecurring(c) ?? throw new InvalidOperationException("Explicit research program required.");
            var now = clock.GetUtcNow(); RequireRecurringClock(c, now, state.WatermarkUtc);
            if (number <= state.Attempts.Length) return state; // replay never becomes the next opportunity
            RequireRecurringAuthority(state);
            if (state.State != RecurringResearchState.Ready || number != state.Attempts.Length + 1 || number > state.Authorization.Cohorts.Length)
                throw new InvalidOperationException("Program is stopped, busy or opportunity is out of order.");
            var grant = state.Authorization; var budget = grant.Budget;
            if (number < grant.Cohorts.Length && now >= grant.Cohorts[number].DueAtUtc)
            {
                state = state with { State = RecurringResearchState.Stopped, StopReason = "MissedOpportunity", WatermarkUtc = now };
                WriteRecurring(c, tx, state); tx.Commit(); return state;
            }
            if (now >= grant.ExpiresAtUtc || state.Invocations >= budget.Invocations || state.Evaluations >= budget.Evaluations ||
                state.Trials + 3 > budget.Trials || state.Calls + 64 > budget.UnderlyingCalls || state.ReservedSeconds + budget.CompletionSeconds > budget.ReservedReasonerSeconds)
            {
                state = state with { State = RecurringResearchState.Stopped, StopReason = "BudgetOrExpiry", WatermarkUtc = now };
                WriteRecurring(c, tx, state); tx.Commit(); return state;
            }
            if (reasonerIdentity != grant.ReasonerIdentity) throw new InvalidOperationException("Reasoner identity mismatch.");
            if (now < grant.Cohorts[number - 1].DueAtUtc) throw new RecurringResearchDataPendingException();
            var scope = ProjectRecurring(c, state, number, now);
            if (ReadEvaluations(c).Any(x => x.Plan.Universe.Contains(grant.Instrument.Id.Value, StringComparer.Ordinal) &&
                x.Plan.From <= scope.Plan.To && x.Plan.To >= scope.Plan.From))
            {
                state = state with { State = RecurringResearchState.Indeterminate, StopReason = "PreviouslyExposedPlan", WatermarkUtc = now };
                WriteRecurring(c, tx, state); tx.Commit(); return state;
            }
            state = state with
            {
                State = RecurringResearchState.ReservedInference,
                Invocations = state.Invocations + 1,
                ReservedSeconds = state.ReservedSeconds + budget.CompletionSeconds,
                WatermarkUtc = now,
                Attempts = state.Attempts.Add(new(number, now, scope, "ReservedInference"))
            };
            WriteRecurring(c, tx, state); SealDailyCutoff(c, tx, now); tx.Commit();
        }
        RecurringResearchTestHook?.Invoke("after-invocation-commit");
        RequireRecurringAuthority(state);
        var attempt = state.Attempts[^1]; LearningReasonerReply reply;
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(state.Authorization.Budget.CompletionSeconds), clock);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, token);
        try
        {
            reply = await reason(attempt.Scope.Input, deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
            deadline.Token.ThrowIfCancellationRequested();
        }
        catch (Exception)
        {
            return FinishRecurring(number, clock, "ReasonerFailure", null, null, elapsed.ElapsedMilliseconds, indeterminate: true);
        }
        var parsed = LearningReplyParser.Parse(reply.ResponseJson, attempt.Scope.Input);
        var responseHash = LearningIdentity.Hash(reply.ResponseJson);
        if (parsed.Rejection is { } rejection) return FinishRecurring(number, clock, rejection.ToString(), responseHash, null, elapsed.ElapsedMilliseconds);
        if (parsed.Reply is LearningReasonerReply.NoUsefulProposal) return FinishRecurring(number, clock, "NoUsefulProposal", responseHash, null, elapsed.ElapsedMilliseconds);
        elapsed.Stop();
        try
        {
            using (var c = new SqliteConnection(ConnectionString))
            {
                c.Open(); using var tx = c.BeginTransaction(deferred: false); state = ReadRecurring(c)!;
                var now = clock.GetUtcNow(); RequireRecurringClock(c, now, state.WatermarkUtc);
                RequireRecurringAuthority(state);
                if (state.State != RecurringResearchState.ReservedInference || state.Attempts[^1].Number != number || now >= state.Authorization.ExpiresAtUtc)
                    throw new InvalidOperationException("Scientific reservation no longer authorized.");
                token.ThrowIfCancellationRequested();
                state = state with
                {
                    State = RecurringResearchState.ReservedEvaluation,
                    Evaluations = state.Evaluations + 1,
                    Trials = state.Trials + 3,
                    Calls = state.Calls + 64,
                    WatermarkUtc = now,
                    Attempts = state.Attempts.SetItem(number - 1, state.Attempts[number - 1] with { Outcome = "ReservedEvaluation", ResponseChecksum = responseHash, ReasonerElapsedMilliseconds = elapsed.ElapsedMilliseconds, Proposal = ((LearningReasonerReply.Proposal)parsed.Reply!).Draft })
                };
                WriteRecurring(c, tx, state); tx.Commit();
            }
            RecurringResearchTestHook?.Invoke("after-evaluation-commit");
            RequireRecurringAuthority(state);
            // Synchronous existing bounded science: never abandon a still-running engine on cancellation.
            var build = DeterministicRobustnessEvaluator.Evaluate(attempt.Scope.Plan, new MomentumResearchStrategy(), attempt.Scope.Bars, attempt.Scope.Features);
            using (var c = new SqliteConnection(ConnectionString))
            {
                c.Open(); foreach (var run in build.UnderlyingRuns) FinanceBacktestPersistence.PersistBacktest(c, run);
                PersistEvaluation(c, build.Evaluation);
            }
            RecurringResearchTestHook?.Invoke("after-native-persistence");
            return FinishRecurring(number, clock, "Evaluated", responseHash, build.Evaluation, elapsed.ElapsedMilliseconds);
        }
        catch (Exception) { return FinishRecurring(number, clock, "ExecutionUncertain", responseHash, null, elapsed.ElapsedMilliseconds, indeterminate: true); }
    }

    private RecurringResearchProgram FinishRecurring(int number, TimeProvider clock, string outcome, string? response,
        RobustnessEvaluationResult? evaluation, long elapsedMilliseconds, bool indeterminate = false)
    {
        using var c = new SqliteConnection(ConnectionString); c.Open(); using var tx = c.BeginTransaction(deferred: false);
        var state = ReadRecurring(c) ?? throw new InvalidDataException("Missing program.");
        var now = clock.GetUtcNow(); RequireRecurringClock(c, now, state.WatermarkUtc);
        if (state.Attempts.Length != number || state.State is not (RecurringResearchState.ReservedInference or RecurringResearchState.ReservedEvaluation))
            throw new InvalidDataException("Completion ownership mismatch.");
        var item = state.Attempts[^1] with
        {
            Outcome = outcome,
            ResponseChecksum = response,
            AvailableAtUtc = now,
            EvaluationId = evaluation?.EvaluationId,
            EvaluationChecksum = evaluation?.Checksum,
            ReasonerElapsedMilliseconds = elapsedMilliseconds,
            ReferenceValidationExcessReturn = evaluation?.PrimarySplit.Test.ExcessReturn
        };
        state = state with
        {
            WatermarkUtc = now,
            Attempts = state.Attempts.SetItem(number - 1, item),
            State = indeterminate ? RecurringResearchState.Indeterminate : evaluation is null ? RecurringResearchState.Stopped :
                number >= state.Authorization.Cohorts.Length ? RecurringResearchState.Completed : RecurringResearchState.Ready,
            StopReason = evaluation is null ? outcome : null
        };
        WriteRecurring(c, tx, state); tx.Commit(); return state;
    }
}
