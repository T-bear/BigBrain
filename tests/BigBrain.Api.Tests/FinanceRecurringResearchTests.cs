using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceRecurringResearchTests
{
    private static readonly DateTimeOffset Registered = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly CanonicalInstrument Instrument = new(new("TEST:XNAS:RECURRING"), InstrumentType.Equity,
        "Model-free fixture", new("USD"), new("NASDAQ", "Nasdaq"), "XNAS", InstrumentLifecycle.Active, new(2025, 1, 1));
    private static readonly ProviderInstrumentMapping Mapping = new(Instrument.Id, new("Alpaca"), new(DailyMarketEvidence.Dataset),
        "FIXTURE", Instrument.Venue, "XNAS", new(2025, 1, 1), null, new("fixture:scientific-mapping"));
    private static readonly MarketDataEntitlementPolicy Policy = AlpacaDailyOwnerDecision.Create(Registered);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static RecurringResearchAuthorization Grant()
    {
        var dates = Enumerable.Range(1, 1500).Select(i => new DateOnly(2025, 1, 1).AddDays(i)).Where(UsMarketCalendar.IsSession).Take(640).ToArray();
        return new("fixture:explicit-finite-owner-grant", "fixture:all-scientific-facts-verified", Instrument, Mapping,
            ResearchLearningFixture.Facts, "fixture:reasoner-v1", DailyMarketEvidence.DayStart(dates[^1].AddDays(10)),
            new(2, 2, 6, 128, 60, 30),
            [new(dates[0], dates[319], DailyMarketEvidence.DayStart(dates[319].AddDays(1)).AddHours(3)),
             new(dates[320], dates[639], DailyMarketEvidence.DayStart(dates[639].AddDays(1)).AddHours(3))]);
    }
    private static async Task Seed(FinanceMarketObservationTests.Database db, FinanceMarketObservationTests.Clock clock,
        RecurringResearchCohort cohort, bool changeHoldout = false, bool late = false, bool fractionalVolume = false)
    {
        var memory = db.Memory(); var dates = RecurringResearchAuthorization.Sessions(cohort);
        foreach (var (day, index) in dates.Select((d, i) => (d, i)))
        {
            clock.Now = DailyMarketEvidence.DayStart(day.AddDays(1)).AddHours(late && index == 0 ? 12 : 1);
            var close = 100m + index / 10m + (index % 17) / 5m + (changeHoldout && index > 280 ? 30m : 0m);
            var value = new ProviderObservation("FIXTURE", "XNAS", new("USD"), Timeframe.OneDay, DailyMarketEvidence.DayStart(day), null,
                close, close + 1, close - 1, close, fractionalVolume ? 10000.5m : 10000,
                MarketObservationIntegrity.Hash(new[] { day.ToString("O", CultureInfo.InvariantCulture), close.ToString(CultureInfo.InvariantCulture) }),
                new(DailyMarketEvidence.Eligibility, day, "America/New_York", DailyMarketEvidence.SourceContract, DailyMarketEvidence.SymbolPolicy));
            await memory.AcquireMarketObservationAsync(new Source(value), Instrument, Mapping, Policy, clock, CancellationToken.None);
        }
        clock.Now = cohort.DueAtUtc;
    }
    private sealed class Source(ProviderObservation value) : IMarketObservationSource
    {
        public string ContractVersion => DailyMarketEvidence.Contract;
        public MarketDataProvider Provider => Mapping.Provider;
        public ProviderDataset Dataset => Mapping.ProviderDataset;
        public string AdapterVersion => "fixture-no-network";
        public MarketObservationOrigin Origin => MarketObservationOrigin.DeterministicFixture;
        public Task<ProviderObservation> ReadAsync(ProviderInstrumentMapping mapping, CancellationToken cancellationToken) => Task.FromResult(value);
    }
    private static LearningReasonerReply Reply(LearningDevelopmentInput input, bool decline = false)
    {
        var json = decline ? JsonSerializer.Serialize(new
        {
            version = LearningAdmissionPolicy.Version,
            discriminator = "NoUsefulProposal",
            inputChecksum = input.InputChecksum,
            scopeHandle = input.ScopeHandle,
            reasonCode = "NoSupportedQuestion",
            explanation = "Fixture decline."
        }) :
            ResearchLearningFixture.Proposal(ResearchLearningFixture.Scope()).Replace(SyntheticLearningScope.Handle, input.ScopeHandle, StringComparison.Ordinal)
                .Replace(ResearchLearningFixture.Scope().Input.InputChecksum, input.InputChecksum, StringComparison.Ordinal)
                .Replace("synthetic-next-session-direction", input.TargetId, StringComparison.Ordinal);
        var parsed = LearningReplyParser.Parse(json, input); Assert.Null(parsed.Rejection); return parsed.Reply!;
    }
    private sealed class Reasoner(Func<LearningDevelopmentInput, CancellationToken, Task<LearningReasonerReply>> reason) : BigBrain.Brain.IResearchReasoner
    {
        public Task<LearningReasonerReply> ReasonAsync(LearningDevelopmentInput input, CancellationToken token) => reason(input, token);
    }
    private sealed class Governor : IFinanceResearchResourceGovernor
    {
        public Task<FinanceResearchResourceDecision> EvaluateAsync(DateTimeOffset now, CancellationToken token) => Task.FromResult(
            new FinanceResearchResourceDecision(FinanceResearchResourceDecisionKind.Allow, now, FinanceResearchResourceGovernorOptions.CurrentVersion,
                ["fixture.ready"], new(0, 0, long.MaxValue, long.MaxValue, 1, null, false, "Healthy", now), "RESEARCH", 0, "NONE"));
    }
    private static FinanceResearchOrchestrator Scheduler(EodhdMarketMemory memory, FinanceMarketObservationTests.Clock clock,
        Func<LearningDevelopmentInput, CancellationToken, Task<LearningReasonerReply>> reason, bool maintenance = false) =>
        new(new() { Enabled = true, RecurringProgramMode = true }, memory, new Governor(), new() { MaintenancePaused = maintenance },
            new("fixture:reasoner-v1", new Reasoner(reason).ReasonAsync, TimeSpan.FromSeconds(30), true), clock);

    [Fact]
    public async Task TwoDisjointIterationsPersistNativeScienceAndSanitizedHistoryAcrossReopen()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(Registered);
        var grant = Grant(); var memory = db.Memory(); memory.CreateRecurringResearchProgram(grant, clock, true);
        var inputs = new List<LearningDevelopmentInput>();
        Task<LearningReasonerReply> Reason(LearningDevelopmentInput input, CancellationToken _) { inputs.Add(input); return Task.FromResult(Reply(input)); }
        await Seed(db, clock, grant.Cohorts[0]);
        var first = await Scheduler(memory, clock, Reason).CheckAndRunAsync(clock.Now, true, CancellationToken.None);
        Assert.Equal(FinanceResearchOpportunityState.Completed, first!.State);
        var retained = db.Memory().ReadRecurringResearchProgram()!;
        Assert.Equal(1, retained.Evaluations); Assert.Single(retained.Attempts);
        Assert.Equal(1L, db.Scalar("SELECT COUNT(*) FROM robustness_evaluations"));
        var snapshot = JsonSerializer.Serialize(retained, Json);
        await memory.RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Reason, CancellationToken.None);
        Assert.Single(inputs); Assert.Equal(snapshot, JsonSerializer.Serialize(db.Memory().ReadRecurringResearchProgram(), Json));
        await Seed(db, clock, grant.Cohorts[1]);
        var second = await Scheduler(db.Memory(), clock, Reason).CheckAndRunAsync(clock.Now, true, CancellationToken.None);
        Assert.Equal(FinanceResearchOpportunityState.Completed, second!.State);
        var finished = db.Memory().ReadRecurringResearchProgram()!;
        Assert.Equal(RecurringResearchState.Completed, finished.State); Assert.Equal(2, finished.Invocations); Assert.Equal(2, finished.Evaluations);
        Assert.Equal(6, finished.Trials); Assert.Equal(128, finished.Calls); Assert.Equal(60, finished.ReservedSeconds);
        Assert.Equal(2L, db.Scalar("SELECT COUNT(*) FROM robustness_evaluations"));
        Assert.Null(Scheduler(db.Memory(), clock, Reason).Status(clock.Now).NextDueUtc);
        Assert.Single(inputs[1].RecurringHistory!.Entries); Assert.Equal("Evaluated", inputs[1].RecurringHistory!.Entries[0].Outcome);
        Assert.Equal(grant.Cohorts[1].DueAtUtc, inputs[1].RecurringHistory!.Entries[0].AvailableAtUtc);
        Assert.DoesNotContain(finished.Attempts[0].EvaluationId!, JsonSerializer.Serialize(inputs[1], Json), StringComparison.Ordinal);
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM prospective_daily_candidates"));
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM research_runs"));
        Assert.Throws<InvalidOperationException>(() => db.Memory().CreateRecurringResearchProgram(grant with { OwnerDecision = "fixture:renew" }, clock, true));
        Assert.Throws<InvalidOperationException>(() => db.Memory().BuildRobustnessEvaluations());
    }

    [Fact]
    public async Task MissingDataMaintenanceAndRecoveryNeverSpendAuthority()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(Registered); var grant = Grant();
        db.Memory().CreateRecurringResearchProgram(grant, clock, true); clock.Now = grant.Cohorts[0].DueAtUtc;
        Task<LearningReasonerReply> Never(LearningDevelopmentInput _, CancellationToken __) => throw new Xunit.Sdk.XunitException("Reasoner must not run.");
        var scheduler = Scheduler(db.Memory(), clock, Never);
        Assert.Equal("finance.research.scheduler.recoveryNotReady", (await scheduler.CheckAndRunAsync(clock.Now, false, CancellationToken.None))!.Reason);
        Assert.Equal("finance.research.scheduler.maintenancePaused", (await Scheduler(db.Memory(), clock, Never, true).CheckAndRunAsync(clock.Now, true, CancellationToken.None))!.Reason);
        Assert.Equal("finance.research.recurring.dataPending", (await scheduler.CheckAndRunAsync(clock.Now, true, CancellationToken.None))!.Reason);
        Assert.Equal(0, db.Memory().ReadRecurringResearchProgram()!.Invocations);
    }
    [Theory]
    [InlineData("decline")]
    [InlineData("mismatch")]
    [InlineData("failure")]
    [InlineData("cancel")]
    public async Task NegativeOutcomesRemainSpentAndCannotBeRenewed(string mode)
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(Registered); var grant = Grant();
        db.Memory().CreateRecurringResearchProgram(grant, clock, true); await Seed(db, clock, grant.Cohorts[0]);
        var calls = 0;
        Task<LearningReasonerReply> Reason(LearningDevelopmentInput input, CancellationToken token)
        {
            calls++;
            if (mode == "failure") throw new IOException("fixture failure");
            if (mode == "cancel") throw new OperationCanceledException(token);
            if (mode == "mismatch") return Task.FromResult(Reply(input with { InputChecksum = "wrong-checksum" }));
            return Task.FromResult(Reply(input, true));
        }
        var state = await db.Memory().RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Reason, CancellationToken.None);
        Assert.Equal(1, state.Invocations); Assert.Equal(30, state.ReservedSeconds); Assert.Equal(0, state.Evaluations);
        Assert.Contains(state.State, new[] { RecurringResearchState.Stopped, RecurringResearchState.Indeterminate });
        var reopened = db.Memory(); Assert.Equal(state.State, reopened.ReadRecurringResearchProgram()!.State);
        await reopened.RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Reason, CancellationToken.None); Assert.Equal(1, calls);
        await Assert.ThrowsAsync<InvalidOperationException>(() => reopened.RunRecurringResearchAsync(2, grant.ReasonerIdentity, clock, Reason, CancellationToken.None));
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM robustness_evaluations"));
    }

    [Fact]
    public async Task ConcurrentInvocationAndCrashDoNotRefundOrCreateAnotherExperiment()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(Registered); var grant = Grant();
        var memory = db.Memory(); memory.CreateRecurringResearchProgram(grant, clock, true); await Seed(db, clock, grant.Cohorts[0]);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var calls = 0;
        async Task<LearningReasonerReply> Reason(LearningDevelopmentInput input, CancellationToken _)
        { calls++; entered.SetResult(); await release.Task; return Reply(input); }
        var first = memory.RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Reason, CancellationToken.None);
        await entered.Task;
        Assert.True(Scheduler(db.Memory(), clock, Reason).Status(clock.Now).ResearchCurrentlyRunning);
        var competing = await db.Memory().RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Reason, CancellationToken.None);
        Assert.Equal(RecurringResearchState.ReservedInference, competing.State); Assert.Equal(1, calls);
        memory.RecurringResearchTestHook = point => { if (point == "after-evaluation-commit") throw new IOException("fixture interrupted execution"); };
        release.SetResult(); var failed = await first;
        Assert.Equal(RecurringResearchState.Indeterminate, failed.State); Assert.Equal(1, failed.Evaluations); Assert.Equal(64, failed.Calls);
        Assert.Equal(RecurringResearchState.Indeterminate, db.Memory().ReadRecurringResearchProgram()!.State);
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM robustness_evaluations"));
    }

    [Fact]
    public async Task ProtectedHoldoutChangesDoNotChangeVisibleInputOrLaterHistoryDigest()
    {
        async Task<(string Visible, string Private)> Input(bool changed)
        {
            using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(Registered); var grant = Grant();
            db.Memory().CreateRecurringResearchProgram(grant, clock, true); await Seed(db, clock, grant.Cohorts[0], changed);
            var visible = new List<LearningDevelopmentInput>();
            var first = await db.Memory().RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock,
                (input, _) => { visible.Add(input); return Task.FromResult(Reply(input)); }, CancellationToken.None);
            Assert.Equal(RecurringResearchState.Ready, first.State);
            await Seed(db, clock, grant.Cohorts[1]);
            await db.Memory().RunRecurringResearchAsync(2, grant.ReasonerIdentity, clock,
                (input, _) => { visible.Add(input); return Task.FromResult(Reply(input, true)); }, CancellationToken.None);
            Assert.Equal(2, visible.Count);
            return (JsonSerializer.Serialize(visible, Json), first.Attempts[0].EvaluationChecksum!);
        }
        var before = await Input(false); var after = await Input(true);
        Assert.NotEqual(before.Private, after.Private);
        Assert.Equal(before.Visible, after.Visible);
    }

    [Theory]
    [InlineData("I")]
    [InlineData("E")]
    [InlineData("T")]
    [InlineData("C")]
    [InlineData("W")]
    public async Task ExhaustedBudgetAndClockRegressionStopBeforeReasoner(string limit)
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(Registered);
        var budget = limit switch
        {
            "I" => new RecurringResearchBudget(1, 1, 6, 128, 60, 30),
            "E" => new(2, 1, 6, 128, 60, 30),
            "T" => new(2, 2, 3, 128, 60, 30),
            "C" => new(2, 2, 6, 64, 60, 30),
            _ => new(2, 2, 6, 128, 30, 30)
        };
        var grant = Grant() with { Budget = budget };
        db.Memory().CreateRecurringResearchProgram(grant, clock, true); await Seed(db, clock, grant.Cohorts[0]); var calls = 0;
        Task<LearningReasonerReply> Reason(LearningDevelopmentInput input, CancellationToken _) { calls++; return Task.FromResult(Reply(input)); }
        var first = await db.Memory().RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Reason, CancellationToken.None);
        Assert.Equal(1, first.Evaluations);
        clock.Now = clock.Now.AddSeconds(-1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.Memory().RunRecurringResearchAsync(2, grant.ReasonerIdentity, clock, Reason, CancellationToken.None));
        clock.Now = grant.Cohorts[1].DueAtUtc;
        var stopped = await db.Memory().RunRecurringResearchAsync(2, grant.ReasonerIdentity, clock, Reason, CancellationToken.None);
        Assert.Equal("BudgetOrExpiry", stopped.StopReason); Assert.Equal(1, calls); Assert.Equal(1, stopped.Invocations);
    }

    [Fact]
    public void ExplicitGrantValidationAndExistingLegacyExclusionAreFailClosed()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(Registered); var grant = Grant();
        Assert.Throws<InvalidOperationException>(() => db.Memory().CreateRecurringResearchProgram(grant with { Facts = grant.Facts with { CorporateActions = DatasetEvidenceResult.Unknown } }, clock));
        Assert.Throws<InvalidOperationException>(() => db.Memory().CreateRecurringResearchProgram(grant with { Cohorts = [grant.Cohorts[0], grant.Cohorts[0]] }, clock));
        var memory = db.Memory(); memory.EnrollSyntheticLearning(ResearchLearningFixture.Scope(), LearningExposure.Unexposed);
        Assert.True(memory.ReserveLearningInvocation());
        Assert.Throws<InvalidOperationException>(() => memory.CreateRecurringResearchProgram(grant, clock));
        Assert.Null(memory.ReadRecurringResearchProgram());
    }

    [Fact]
    public async Task ProvenanceCorruptionFailsBeforeInvocation()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(Registered); var grant = Grant();
        var memory = db.Memory(); memory.CreateRecurringResearchProgram(grant, clock, true); await Seed(db, clock, grant.Cohorts[0]);
        db.Execute("UPDATE market_observation_receipts SET checksum='corrupt' WHERE id=(SELECT id FROM market_observation_receipts LIMIT 1)");
        await Assert.ThrowsAsync<InvalidDataException>(() => memory.RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock,
            (_, _) => throw new Xunit.Sdk.XunitException("No reasoner on corrupt lineage"), CancellationToken.None));
        Assert.Equal(0, memory.ReadRecurringResearchProgram()!.Invocations);
    }

    [Fact]
    public async Task LaterReceiptCannotChangeSealedCompletedResearch()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(Registered); var grant = Grant();
        var memory = db.Memory(); memory.CreateRecurringResearchProgram(grant, clock, true); await Seed(db, clock, grant.Cohorts[0]);
        var state = await memory.RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, (input, _) => Task.FromResult(Reply(input)), CancellationToken.None);
        var before = JsonSerializer.Serialize(state.Attempts[0], Json);
        await Seed(db, clock, grant.Cohorts[1]);
        Assert.Equal(before, JsonSerializer.Serialize(db.Memory().ReadRecurringResearchProgram()!.Attempts[0], Json));
        Assert.Equal(state.Attempts[0].Scope.Input.InputChecksum, db.Memory().ReadRecurringResearchProgram()!.Attempts[0].Scope.Input.InputChecksum);
    }

    [Fact]
    public void AdditiveMigrationReopensWithoutGrantOrLegacyMutationAndRollsBackOnFailure()
    {
        using var db = new FinanceMarketObservationTests.Database();
        var original = db.Scalar("SELECT snapshot_json FROM learning_governance WHERE singleton=1");
        db.Execute("DROP TABLE recurring_research_program; DROP TABLE finance_scientific_lease; DELETE FROM finance_schema_migrations WHERE version=99;");
        Assert.Throws<IOException>(() => FinanceSchemaMigrator.Migrate(db.Path, v => { if (v == 99) throw new IOException("fixture crash"); }));
        Assert.Equal(98, FinanceSchemaMigrator.State(db.Path).CurrentVersion);
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM sqlite_master WHERE name='recurring_research_program'"));
        var reopened = db.Memory(); Assert.Null(reopened.ReadRecurringResearchProgram());
        Assert.Equal(original, db.Scalar("SELECT snapshot_json FROM learning_governance WHERE singleton=1"));
        Assert.Equal(99, FinanceSchemaMigrator.State(db.Path).CurrentVersion);
    }

    [Fact]
    public async Task ExistingOwnedRuntimeTransportsRecurringInputWithoutModelOrNetwork()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(Registered); var grant = Grant();
        var memory = db.Memory(); memory.CreateRecurringResearchProgram(grant, clock, true); await Seed(db, clock, grant.Cohorts[0]);
        var directory = Path.Combine(Path.GetTempPath(), "bb-recurring-proof-" + Guid.NewGuid().ToString("N"));
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            var audit = new List<BigBrain.Brain.LocalReasonerAudit>();
            await using var runtime = new BigBrain.Brain.LocalReasonerRuntime(new()
            {
                Enabled = true,
                WorkerExecutable = Path.Combine(AppContext.BaseDirectory, "reasoner-proof", "BigBrain.Reasoner.ProofWorker"),
                WorkerArguments = ["no-useful"],
                CoordinationDirectory = directory,
                InvocationTimeout = TimeSpan.FromSeconds(10)
            }, audit.Add);
            var scheduler = new FinanceResearchOrchestrator(new() { Enabled = true, RecurringProgramMode = true }, memory, new Governor(), new(),
                new(grant.ReasonerIdentity, runtime.ReasonAsync, runtime.CompletionTimeout, true), clock);
            await scheduler.CheckAndRunAsync(clock.Now, true, CancellationToken.None);
            var result = db.Memory().ReadRecurringResearchProgram()!;
            Assert.Equal("NoUsefulProposal", result.StopReason); Assert.Equal(1, result.Invocations); Assert.Equal(0, result.Evaluations);
            Assert.Contains(audit, x => x.Outcome == BigBrain.Brain.LocalReasonerOutcome.NoUsefulProposal);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task LateKnowledgeAndLossyVolumeCannotEnterExistingScientificSemantics(bool late, bool fractional)
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(Registered); var grant = Grant();
        var memory = db.Memory(); memory.CreateRecurringResearchProgram(grant, clock, true);
        await Seed(db, clock, grant.Cohorts[0], late: late, fractionalVolume: fractional);
        await Assert.ThrowsAsync<InvalidDataException>(() => memory.RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock,
            (_, _) => throw new Xunit.Sdk.XunitException("Ineligible data must not reach reasoner"), CancellationToken.None));
        Assert.Equal(0, memory.ReadRecurringResearchProgram()!.Invocations);
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM robustness_evaluations"));
    }

}
