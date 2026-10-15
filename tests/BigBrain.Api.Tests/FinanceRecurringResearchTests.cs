using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using BigBrain.Finance.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;

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
        RecurringResearchCohort cohort, bool changeHoldout = false, bool late = false, bool fractionalVolume = false, bool realOriginShape = false)
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
            await memory.AcquireMarketObservationAsync(new Source(value, realOriginShape), Instrument, Mapping, Policy, clock, CancellationToken.None);
        }
        clock.Now = cohort.DueAtUtc;
    }
    private sealed class Source(ProviderObservation value, bool realOriginShape = false) : IMarketObservationSource
    {
        public string ContractVersion => DailyMarketEvidence.Contract;
        public MarketDataProvider Provider => Mapping.Provider;
        public ProviderDataset Dataset => Mapping.ProviderDataset;
        public string AdapterVersion => "fixture-no-network";
        public MarketObservationOrigin Origin => realOriginShape ? MarketObservationOrigin.AcquiredProviderDaily : MarketObservationOrigin.DeterministicFixture;
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
        Func<LearningDevelopmentInput, CancellationToken, Task<LearningReasonerReply>> reason, bool maintenance = false, bool fixture = true) =>
        new(new() { Enabled = true, RecurringProgramMode = true }, memory, new Governor(), new() { MaintenancePaused = maintenance },
            new("fixture:reasoner-v1", new Reasoner(reason).ReasonAsync, TimeSpan.FromSeconds(30), fixture), clock);

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

    // Journal I/O uses isolated ordinary files here; production's independent OS protection is
    // deliberately not simulated as verified. No runtime config can select this test constructor.
    private sealed class AuthorityFixture : IDisposable
    {
        internal readonly string Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "authority-test-" + Guid.NewGuid().ToString("N"));
        internal string EnrollmentHash { get; }
        internal AuthorityFixture(RecurringResearchProgram initial)
        {
            var enrollment = RecurringResearchAuthority.Enrollment(initial);
            EnrollmentHash = RecurringResearchAuthority.EnrollmentChecksum(enrollment);
            File.WriteAllText(Path, enrollment);
        }
        internal RecurringResearchAuthority Open() => new(() => new FileStream(Path, FileMode.Open,
            FileAccess.ReadWrite, FileShare.ReadWrite), EnrollmentHash);
        internal EodhdMarketMemory Attach(FinanceMarketObservationTests.Database db)
        { var memory = db.Memory(); memory.RecurringAuthority = Open(); return memory; }
        public void Dispose() => File.Delete(Path);
    }

    [Fact]
    public async Task IndependentAuthorityPermitsTwoRealOriginShapedIterationsAcrossNormalRestart()
    {
        using var db = new FinanceMarketObservationTests.Database();
        var clock = new FinanceMarketObservationTests.Clock(Registered); var grant = Grant();
        using var journal = new AuthorityFixture(db.Memory().CreateRecurringResearchProgram(grant, clock));
        var calls = 0;
        Task<LearningReasonerReply> Reason(LearningDevelopmentInput input, CancellationToken _)
        { calls++; return Task.FromResult(Reply(input)); }
        await Seed(db, clock, grant.Cohorts[0], realOriginShape: true);
        var first = await Scheduler(journal.Attach(db), clock, Reason, fixture: false).CheckAndRunAsync(clock.Now, true, CancellationToken.None);
        Assert.Equal(FinanceResearchOpportunityState.Completed, first!.State);
        var saved = JsonSerializer.Serialize(db.Memory().ReadRecurringResearchProgram(), Json);
        Assert.Equal(saved, JsonSerializer.Serialize(await journal.Attach(db).RunRecurringResearchAsync(1,
            grant.ReasonerIdentity, clock, Reason, CancellationToken.None), Json));
        await Seed(db, clock, grant.Cohorts[1], realOriginShape: true);
        var second = await Scheduler(journal.Attach(db), clock, Reason, fixture: false).CheckAndRunAsync(clock.Now, true, CancellationToken.None);
        Assert.Equal(FinanceResearchOpportunityState.Completed, second!.State);
        var done = db.Memory().ReadRecurringResearchProgram()!;
        Assert.Equal(RecurringResearchState.Completed, done.State);
        Assert.Equal(2, calls); Assert.Equal(2, done.Invocations); Assert.Equal(2, done.Evaluations);
        Assert.Equal(128, done.Calls); journal.Open().Require(done);
        Assert.Null(Scheduler(journal.Attach(db), clock, Reason, fixture: false).Status(clock.Now).NextDueUtc);
        Assert.Throws<RecurringResearchReconciliationRequiredException>(() => RecurringResearchAuthority.Enrollment(done));
    }

    [Theory]
    [InlineData("old-database")]
    [InlineData("missing")]
    [InlineData("partial")]
    [InlineData("changed")]
    [InlineData("older-journal")]
    public async Task IndependentAuthorityRefusesRollbackOrUncertainJournalBeforeAnyNewCall(string fault)
    {
        using var db = new FinanceMarketObservationTests.Database(); using var snapshot = new FinanceMarketObservationTests.Database();
        var clock = new FinanceMarketObservationTests.Clock(Registered); var grant = Grant();
        using var journal = new AuthorityFixture(db.Memory().CreateRecurringResearchProgram(grant, clock));
        await Seed(db, clock, grant.Cohorts[0], realOriginShape: true);
        using (var source = new SqliteConnection("Data Source=" + db.Path))
        using (var target = new SqliteConnection("Data Source=" + snapshot.Path))
        { source.Open(); target.Open(); source.BackupDatabase(target); }
        var oldJournal = File.ReadAllBytes(journal.Path);
        await journal.Attach(db).RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock,
            (input, _) => Task.FromResult(Reply(input)), CancellationToken.None);
        var memory = journal.Attach(fault == "old-database" ? snapshot : db);
        if (fault == "missing") File.Delete(journal.Path);
        if (fault == "partial") File.AppendAllText(journal.Path, "{incomplete");
        if (fault == "changed") File.WriteAllText(journal.Path, "{}\n");
        if (fault == "older-journal") File.WriteAllBytes(journal.Path, oldJournal);
        var before = LearningIdentity.Hash(memory.ReadRecurringResearchProgram());
        await Assert.ThrowsAsync<RecurringResearchReconciliationRequiredException>(() => memory.RunRecurringResearchAsync(
            fault == "old-database" ? 1 : 2, grant.ReasonerIdentity, clock,
            (_, _) => throw new Xunit.Sdk.XunitException("No extra inference"), CancellationToken.None));
        Assert.Equal(before, LearningIdentity.Hash(memory.ReadRecurringResearchProgram()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task JournalAheadOfSqliteAfterCrashNeverRefundsOrExecutes(bool evaluationReservation)
    {
        using var db = new FinanceMarketObservationTests.Database();
        var clock = new FinanceMarketObservationTests.Clock(Registered); var grant = Grant();
        using var journal = new AuthorityFixture(db.Memory().CreateRecurringResearchProgram(grant, clock));
        await Seed(db, clock, grant.Cohorts[0], realOriginShape: true);
        var memory = journal.Attach(db); var calls = 0;
        if (!evaluationReservation) memory.RecurringAuthority!.AfterAdvanceTestHook = () => throw new IOException("fixture crash");
        await Assert.ThrowsAsync<RecurringResearchReconciliationRequiredException>(() => memory.RunRecurringResearchAsync(1,
            grant.ReasonerIdentity, clock, (input, _) =>
            {
                calls++;
                memory.RecurringAuthority!.AfterAdvanceTestHook = () => throw new IOException("fixture crash");
                return Task.FromResult(Reply(input));
            }, CancellationToken.None));
        var retained = db.Memory().ReadRecurringResearchProgram()!;
        Assert.Equal(evaluationReservation ? 1 : 0, calls);
        Assert.Equal(0, retained.Evaluations);
        Assert.Throws<RecurringResearchReconciliationRequiredException>(() => journal.Open().Require(retained));
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM robustness_evaluations"));
    }

    [Theory]
    [InlineData("after-invocation-commit", 0)]
    [InlineData("after-evaluation-commit", 0)]
    [InlineData("after-native-persistence", 1)]
    public async Task CommittedInterruptedAuthorityCannotRestartWork(string point, long nativeResults)
    {
        using var db = new FinanceMarketObservationTests.Database();
        var clock = new FinanceMarketObservationTests.Clock(Registered); var grant = Grant();
        using var journal = new AuthorityFixture(db.Memory().CreateRecurringResearchProgram(grant, clock));
        await Seed(db, clock, grant.Cohorts[0], realOriginShape: true);
        var memory = journal.Attach(db); var calls = 0;
        memory.RecurringResearchTestHook = at => { if (at == point) throw new IOException("fixture interruption"); };
        Task<LearningReasonerReply> Reason(LearningDevelopmentInput input, CancellationToken _)
        { calls++; return Task.FromResult(Reply(input)); }
        var exception = await Record.ExceptionAsync(() => memory.RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Reason, CancellationToken.None));
        if (point == "after-invocation-commit") Assert.IsType<IOException>(exception); else Assert.Null(exception);
        var retained = EodhdMarketMemory.ReadRecurringAuthorityState(db.Path);
        journal.Open().Require(retained);
        Assert.Equal(1, retained.Invocations);
        Assert.Equal(point == "after-invocation-commit" ? 0 : 1, retained.Evaluations);
        var reopened = journal.Attach(db);
        var before = calls;
        await reopened.RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Reason, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => reopened.RunRecurringResearchAsync(2,
            grant.ReasonerIdentity, clock, Reason, CancellationToken.None));
        Assert.Equal(before, calls);
        Assert.Equal(nativeResults, db.Scalar("SELECT COUNT(*) FROM robustness_evaluations"));
    }

    [Fact]
    public async Task JournalReservationsStaySpentAcrossConcurrentCancellationAndReopen()
    {
        using var db = new FinanceMarketObservationTests.Database();
        var clock = new FinanceMarketObservationTests.Clock(Registered); var grant = Grant();
        using var journal = new AuthorityFixture(db.Memory().CreateRecurringResearchProgram(grant, clock));
        await Seed(db, clock, grant.Cohorts[0], realOriginShape: true);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancel = new CancellationTokenSource(); var calls = 0;
        async Task<LearningReasonerReply> Reason(LearningDevelopmentInput input, CancellationToken token)
        { calls++; entered.SetResult(); await Task.Delay(Timeout.InfiniteTimeSpan, token); return Reply(input); }
        var first = journal.Attach(db).RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Reason, cancel.Token);
        await entered.Task;
        var replay = await journal.Attach(db).RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Reason, CancellationToken.None);
        Assert.Equal(RecurringResearchState.ReservedInference, replay.State);
        cancel.Cancel(); var stopped = await first;
        Assert.Equal(RecurringResearchState.Indeterminate, stopped.State); Assert.Equal(1, calls);
        journal.Open().Require(db.Memory().ReadRecurringResearchProgram()!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => journal.Attach(db).RunRecurringResearchAsync(2,
            grant.ReasonerIdentity, clock, Reason, CancellationToken.None));
        Assert.Equal(1, db.Memory().ReadRecurringResearchProgram()!.Invocations);
    }

    [Fact]
    public void ProductionAuthorityRejectsOrdinaryFileAndMissingBoundaryConfiguration()
    {
        using var db = new FinanceMarketObservationTests.Database();
        var initial = db.Memory().CreateRecurringResearchProgram(Grant(), new FinanceMarketObservationTests.Clock(Registered));
        using var journal = new AuthorityFixture(initial);
        Assert.Throws<RecurringResearchReconciliationRequiredException>(() => RecurringResearchAuthority.FromConfiguration(new ConfigurationBuilder().Build()));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Finance:RecurringAuthority:JournalPath"] = journal.Path,
            ["Finance:RecurringAuthority:EnrollmentChecksum"] = journal.EnrollmentHash,
            ["Finance:RecurringAuthority:RestoreBoundaryDecision"] = "fixture:not-a-real-protection-proof"
        }).Build();
        var production = RecurringResearchAuthority.FromConfiguration(config);
        Assert.Throws<RecurringResearchReconciliationRequiredException>(() => production.Require(initial));
        Assert.Equal(RecurringResearchAuthority.Enrollment(initial), File.ReadAllText(journal.Path));
        config["Finance:Eodhd:DatabasePath"] = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(journal.Path)!, "store.db");
        Assert.Throws<RecurringResearchReconciliationRequiredException>(() => RecurringResearchAuthority.FromConfiguration(config));
    }

    [Fact]
    public void AuthorityProtectionRequiresRootCustodyAppendFlagAndNoReplacementAccess()
    {
        ProtectedAppendJournal.RequireMetadata(0x7ff, 0, 0x81b0, 1, 0x20, 0x20, false); // root regular0660
        ProtectedAppendJournal.RequireMetadata(0x7ff, 0, 0x41e8, 1, 0, 0, true); // root directory0750
        Assert.Throws<RecurringResearchReconciliationRequiredException>(() => ProtectedAppendJournal.RequireMetadata(0x7ff, 1000, 0x81b0, 1, 0x20, 0x20, false));
        Assert.Throws<RecurringResearchReconciliationRequiredException>(() => ProtectedAppendJournal.RequireMetadata(0x7ff, 0, 0x81b0, 2, 0x20, 0x20, false));
        Assert.Throws<RecurringResearchReconciliationRequiredException>(() => ProtectedAppendJournal.RequireMetadata(0x7ff, 0, 0x81b0, 1, 0x20, 0, false));
        Assert.Throws<RecurringResearchReconciliationRequiredException>(() => ProtectedAppendJournal.RequireMetadata(0x7ff, 0, 0x81b0, 1, 0, 0x20, false));
        Assert.Throws<RecurringResearchReconciliationRequiredException>(() => ProtectedAppendJournal.RequireMetadata(0x7ff, 0, 0x81b6, 1, 0x20, 0x20, false));
        Assert.Throws<RecurringResearchReconciliationRequiredException>(() => ProtectedAppendJournal.RequireMetadata(0x7ff, 0, 0x41f8, 1, 0, 0, true)); // group writable
    }

    [Fact]
    public void ExplicitEnrollmentRequiresMaintenanceAndExactGrantAndCannotReplaceProgram()
    {
        using var db = new FinanceMarketObservationTests.Database();
        var root = System.IO.Path.GetDirectoryName(db.Path)!;
        var clock = new FinanceMarketObservationTests.Clock(Registered);
        File.WriteAllText(System.IO.Path.Combine(root, "synchronized"), "");
        var grantFile = System.IO.Path.Combine(root, "grant.json");
        var bytes = EodhdMarketMemory.ExportRecurringAuthorization(Grant()); File.WriteAllBytes(grantFile, bytes);
        EodhdMarketMemory.ParseRecurringAuthorization(bytes).Validate(clock.GetUtcNow());
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Finance:Eodhd:DatabasePath"] = db.Path,
            ["Finance:Eodhd:PayloadDirectory"] = System.IO.Path.Combine(root, "payloads"),
            ["Finance:ResearchOperations:MaintenancePaused"] = "true",
            ["SystemRecovery:ClockSyncDirectory"] = root,
            ["SystemRecovery:DatabasePath"] = db.Path,
            ["SystemRecovery:LowDiskCriticalBytes"] = "1"
        }).Build();
        string[] args = ["finance-recurring-enroll", grantFile, LearningIdentity.HashBytes(bytes)];
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        config["Finance:ResearchScheduler:Enabled"] = "true";
        Assert.Equal(1, FinanceRecurringMaintenanceCommand.Run(args, config, clock, output));
        Assert.Null(db.Memory().ReadRecurringResearchProgram());
        config["Finance:ResearchScheduler:Enabled"] = "false";
        Assert.Equal(1, FinanceRecurringMaintenanceCommand.Run([args[0], args[1], "wrong-checksum"], config, clock, output));
        Assert.Null(db.Memory().ReadRecurringResearchProgram());
        output.GetStringBuilder().Clear();
        Assert.Equal(0, FinanceRecurringMaintenanceCommand.Run(args, config, clock, output));
        var initial = db.Memory().ReadRecurringResearchProgram()!;
        Assert.False(initial.ModelFreeFixture); Assert.Equal(0, initial.Invocations);
        Assert.Equal(RecurringResearchAuthority.Enrollment(initial), output.ToString());
        output.GetStringBuilder().Clear();
        Assert.Equal(0, FinanceRecurringMaintenanceCommand.Run(args, config, clock, output));
        Assert.Equal(RecurringResearchAuthority.Enrollment(initial), output.ToString());
        bytes = EodhdMarketMemory.ExportRecurringAuthorization(Grant() with { OwnerDecision = "fixture:replacement" });
        File.WriteAllBytes(grantFile, bytes);
        Assert.Equal(1, FinanceRecurringMaintenanceCommand.Run([args[0], args[1], LearningIdentity.HashBytes(bytes)], config, clock, output));
        Assert.Equal(LearningIdentity.Hash(initial), LearningIdentity.Hash(db.Memory().ReadRecurringResearchProgram()));
        Assert.False(File.Exists(System.IO.Path.Combine(root, "authority.journal")));
    }

    [Fact]
    public async Task SingleIterationPersistsResultAndSpentBudgetWithoutReplayExecution()
    {
        using var db = new FinanceMarketObservationTests.Database();
        var clock = new FinanceMarketObservationTests.Clock(Registered);
        var template = Grant();
        var grant = template with { Budget = new(1, 1, 3, 64, 30, 30), Cohorts = [template.Cohorts[0]] };
        db.Memory().CreateRecurringResearchProgram(grant, clock, true);
        await Seed(db, clock, grant.Cohorts[0]);
        var calls = 0;
        Task<LearningReasonerReply> Reason(LearningDevelopmentInput input, CancellationToken _)
        {
            calls++;
            Assert.Empty(input.RecurringHistory!.Entries);
            return Task.FromResult(Reply(input));
        }
        var outcome = await Scheduler(db.Memory(), clock, Reason).CheckAndRunAsync(clock.Now, true, CancellationToken.None);
        Assert.Equal(FinanceResearchOpportunityState.Completed, outcome!.State);
        var retained = db.Memory().ReadRecurringResearchProgram()!;
        Assert.Equal(RecurringResearchState.Completed, retained.State);
        Assert.Equal(1, retained.Invocations); Assert.Equal(1, retained.Evaluations);
        Assert.Equal(3, retained.Trials); Assert.Equal(64, retained.Calls); Assert.Equal(30, retained.ReservedSeconds);
        var attempt = Assert.Single(retained.Attempts);
        Assert.Equal("Evaluated", attempt.Outcome);
        Assert.NotNull(attempt.EvaluationId); Assert.NotNull(attempt.EvaluationChecksum);
        var snapshot = JsonSerializer.Serialize(retained, Json);
        var reopened = db.Memory();
        Assert.Equal(snapshot, JsonSerializer.Serialize(await reopened.RunRecurringResearchAsync(1, grant.ReasonerIdentity,
            clock, Reason, CancellationToken.None), Json));
        Assert.Null(Scheduler(reopened, clock, Reason).Status(clock.Now).NextDueUtc);
        await Assert.ThrowsAsync<InvalidOperationException>(() => reopened.RunRecurringResearchAsync(2,
            grant.ReasonerIdentity, clock, Reason, CancellationToken.None));
        Assert.Throws<InvalidOperationException>(() => reopened.CreateRecurringResearchProgram(
            grant with { OwnerDecision = "fixture:no-replacement-grant" }, clock, true));
        Assert.Equal(snapshot, JsonSerializer.Serialize(db.Memory().ReadRecurringResearchProgram(), Json));
        Assert.Equal(1, calls);
        Assert.Equal(1L, db.Scalar("SELECT COUNT(*) FROM robustness_evaluations"));
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM prospective_daily_candidates"));
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM research_runs"));
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RealAuthorityNeverResumesFromCleanReopenOrConsistentOldRestore(bool singleIteration)
    {
        using var db = new FinanceMarketObservationTests.Database();
        using var snapshot = new FinanceMarketObservationTests.Database();
        var clock = new FinanceMarketObservationTests.Clock(Registered); var grant = Grant();
        if (singleIteration) grant = grant with { Budget = new(1, 1, 3, 64, 30, 30), Cohorts = [grant.Cohorts[0]] };
        var original = db.Memory().CreateRecurringResearchProgram(grant, clock); // not fixture authority
        using (var source = new SqliteConnection("Data Source=" + db.Path))
        using (var target = new SqliteConnection("Data Source=" + snapshot.Path))
        { source.Open(); target.Open(); source.BackupDatabase(target); } // isolated test files only
        clock.Now = grant.Cohorts[0].DueAtUtc;
        Task<LearningReasonerReply> Never(LearningDevelopmentInput _, CancellationToken __) => throw new Xunit.Sdk.XunitException("Real authority must remain blocked.");
        foreach (var memory in new[] { db.Memory(), snapshot.Memory() })
        {
            Assert.Equal(LearningIdentity.Hash(original), LearningIdentity.Hash(memory.ReadRecurringResearchProgram()));
            var scheduler = new FinanceResearchOrchestrator(new() { Enabled = true, RecurringProgramMode = true }, memory,
                new Governor(), new(), new(grant.ReasonerIdentity, Never, TimeSpan.FromSeconds(30)), clock);
            var outcome = await scheduler.CheckAndRunAsync(clock.Now, true, CancellationToken.None);
            Assert.Equal(RecurringResearchReconciliationRequiredException.Reason, outcome!.Reason);
            await Assert.ThrowsAsync<RecurringResearchReconciliationRequiredException>(() =>
                memory.RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Never, CancellationToken.None));
            Assert.Equal(0, memory.ReadRecurringResearchProgram()!.Invocations);
        }
    }

    private static IConfiguration HostingConfiguration(string directory, string mode = "valid")
    {
        const string p = "Finance:RecurringLocalReasoner:";
        return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [p + "Enabled"] = "true",
            [p + "Profile:OwnerDecision"] = "fixture:profile-approval",
            [p + "Profile:ArtifactAndIsolationEvidence"] = "fixture:owned-proof-no-model",
            [p + "Profile:ApprovedAtUtc"] = Registered.ToString("O", CultureInfo.InvariantCulture),
            [p + "Profile:ExpiresAtUtc"] = Grant().ExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture),
            [p + "Profile:Runtime:WorkerExecutable"] = Path.Combine(AppContext.BaseDirectory, "reasoner-proof", "BigBrain.Reasoner.ProofWorker"),
            [p + "Profile:Runtime:WorkerArguments:0"] = mode,
            [p + "Profile:Runtime:CoordinationDirectory"] = directory,
            [p + "Profile:Runtime:InvocationTimeout"] = "00:00:20",
            [p + "Profile:Runtime:GracePeriod"] = "00:00:00.250",
            [p + "Profile:Runtime:ReapTimeout"] = "00:00:02"
        }).Build();
    }

    [Theory]
    [InlineData("valid", "Evaluated")]
    [InlineData("no-useful", "NoUsefulProposal")]
    public async Task ProductionCompositionUsesOwnedProofAndPersistsBoundedAudit(string mode, string expected)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        var directory = Path.Combine(Path.GetTempPath(), "bb-recurring-host-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(Registered);
            var memory = db.Memory();
            await using var host = LocalRecurringResearchComposition.FromConfiguration(HostingConfiguration(directory, mode), memory, clock)!;
            var grant = Grant() with { ReasonerIdentity = host.Binding.Identity };
            using var journal = new AuthorityFixture(memory.CreateRecurringResearchProgram(grant, clock, true));
            memory.RecurringAuthority = journal.Open();
            await Seed(db, clock, grant.Cohorts[0]);
            // Fixture permission exists only in tests, never in the host's configuration surface.
            var scheduler = new FinanceResearchOrchestrator(new() { Enabled = true, RecurringProgramMode = true }, memory,
                new Governor(), new(), host.Binding with { ModelFreeFixture = true }, clock);
            await scheduler.CheckAndRunAsync(clock.Now, true, CancellationToken.None);
            var retained = db.Memory().ReadRecurringResearchProgram()!;
            journal.Open().Require(retained);
            Assert.True(expected == retained.Attempts[0].Outcome,
                $"Expected {expected}; actual {retained.Attempts[0].Outcome}; transport={JsonSerializer.Serialize(retained.Attempts[0].RuntimeAudit, Json)}");
            Assert.Equal(1, retained.Invocations);
            var audit = retained.Attempts[0].RuntimeAudit!.Value;
            Assert.Equal(3, audit.Length); Assert.Equal("Started", audit[0].Phase); Assert.Equal("Completed", audit[^1].Phase);
            Assert.Equal(0, audit[^1].WorkerExitCode);
            Assert.Equal(mode == "valid" ? 1 : 0, retained.Evaluations);
            await db.Memory().RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, host.Binding.Reason, CancellationToken.None);
            Assert.Equal(LearningIdentity.Hash(retained), LearningIdentity.Hash(db.Memory().ReadRecurringResearchProgram()));
            Assert.False(File.Exists(Path.Combine(directory, ".local-reasoner-reservation")));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task HostingDefaultsOffAndRequiresExplicitCurrentBoundedProfileAndGrant()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(Registered);
        Assert.Null(LocalRecurringResearchComposition.FromConfiguration(new ConfigurationBuilder().Build(), db.Memory(), clock));
        var invalid = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { [LocalRecurringResearchComposition.Section + ":Enabled"] = "true" }).Build();
        Assert.Throws<InvalidOperationException>(() => LocalRecurringResearchComposition.FromConfiguration(invalid, db.Memory(), clock));
        var config = HostingConfiguration("/not-provisioned");
        config[LocalRecurringResearchComposition.Section + ":Profile:ApprovedAtUtc"] = Registered.AddSeconds(1).ToString("O", CultureInfo.InvariantCulture);
        Assert.Throws<InvalidOperationException>(() => LocalRecurringResearchComposition.FromConfiguration(config, db.Memory(), clock));
        config[LocalRecurringResearchComposition.Section + ":Profile:ApprovedAtUtc"] = Registered.ToString("O", CultureInfo.InvariantCulture);
        config[LocalRecurringResearchComposition.Section + ":Profile:Runtime:InvocationTimeout"] = "00:03:00";
        Assert.Throws<ArgumentException>(() => LocalRecurringResearchComposition.FromConfiguration(config, db.Memory(), clock));
        Assert.Null(db.Memory().ReadRecurringResearchProgram());
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.Memory().RunRecurringResearchAsync(1, "missing", clock,
            (_, _) => throw new Xunit.Sdk.XunitException("No grant."), CancellationToken.None));
        Assert.DoesNotContain(typeof(Program).Assembly.GetReferencedAssemblies(), x => x.Name is "BigBrain.Brain" or "BigBrain.Finance.Hosting");
        Assert.DoesNotContain(typeof(BigBrain.Brain.IResearchReasoner).Assembly.GetReferencedAssemblies(), x => x.Name is "BigBrain.Api" or "BigBrain.Finance.Hosting");
    }

    [Fact]
    public async Task ComposedProfileExpiryAndRegressionRefuseBeforeWorkerAndProfileChangeChangesIdentity()
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        var directory = Path.Combine(Path.GetTempPath(), "bb-profile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(Registered);
            var config = HostingConfiguration(directory);
            await using var host = LocalRecurringResearchComposition.FromConfiguration(config, db.Memory(), clock)!;
            config[LocalRecurringResearchComposition.Section + ":Profile:OwnerDecision"] = "fixture:different-profile-decision";
            await using var changed = LocalRecurringResearchComposition.FromConfiguration(config, db.Memory(), clock)!;
            Assert.NotEqual(host.Binding.Identity, changed.Binding.Identity);
            clock.Now = Registered.AddTicks(-1);
            await Assert.ThrowsAsync<InvalidOperationException>(() => host.Binding.Reason(ResearchLearningFixture.Scope().Input, CancellationToken.None));
            clock.Now = Grant().ExpiresAtUtc;
            await Assert.ThrowsAsync<InvalidOperationException>(() => host.Binding.Reason(ResearchLearningFixture.Scope().Input, CancellationToken.None));
            Assert.Empty(Directory.GetFiles(directory));
            Assert.Null(db.Memory().ReadRecurringResearchProgram());
        }
        finally { Directory.Delete(directory, true); }
    }

}
