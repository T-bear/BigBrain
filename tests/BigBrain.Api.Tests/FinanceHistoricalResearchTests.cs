using System.Globalization;
using System.Text.Json;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using BigBrain.Finance.Hosting;

namespace BigBrain.Api.Tests;

// All prices/metadata below are generated fixtures. No production data, provider or Qwen execution.
public sealed class FinanceHistoricalResearchTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const string Hash = HistoricalResearchAdmission.Artifact;

    private static RecurringResearchAuthorization Seed(FinanceMarketObservationTests.Database db, bool changeHoldout = false)
    {
        var root = Path.GetDirectoryName(db.Path)!;
        _ = new FinanceDatasetIntakeStore(new() { DatabasePath = db.Path }, new()
        { QuarantineDirectory = Path.Combine(root, "quarantine"), OwnerDropDirectory = Path.Combine(root, "drop") });
        var dates = HistoricalResearchAdmission.Sessions(); Assert.Equal(748, dates.Length);
        Assert.Equal(new DateOnly(2015, 12, 24), dates[499]);
        using var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = db.Path }.ToString()); c.Open();
        using var tx = c.BeginTransaction();
        Execute("""
            INSERT INTO dataset_candidates(candidate_id,source,source_url,hosting_platform,filename,state,license_class,declared_license,
              license_evidence_url,license_retrieved_on,provenance,provenance_result,downloader_version,acquisition_method,updated_utc,
              artifact_sha256,manifest_json,validation_json,promotion_result,canonical_revision_id)
            VALUES($candidate,'NASDAQ-WIKI','fixture:no-network','fixture','fixture','Promoted','PublicDomain','fixture',
              'fixture','2026-10-15','fixture','Pass','fixture','fixture',$at,$hash,'{"fixture":true}','{"fixture":true}','Pass',$revision);
            INSERT INTO revisions VALUES($revision,$hash,$at,748,1);
            INSERT INTO revision_price_capabilities VALUES($revision,'RAW_ONLY_VALID','AdjustedSemanticsInvalid','fixture','fixture',$at);
            """);
        for (var i = 0; i < dates.Length; i++)
        {
            var close = 100m + i / 10m + i % 7 / 5m + (changeHoldout && i >= 400 ? 20 : 0);
            using var q = c.CreateCommand(); q.Transaction = tx;
            q.CommandText = """
                INSERT INTO observations VALUES('NASDAQ-WIKI','PRICES','dataset-promotion-v1','US:XNAS:AAPL','AAPL','AAPL','XNAS',
                $day,$close,$high,$low,$close,$close,10000,$at,$revision)
                """;
            q.Parameters.AddWithValue("$day", dates[i].ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            q.Parameters.AddWithValue("$close", close.ToString(CultureInfo.InvariantCulture));
            q.Parameters.AddWithValue("$high", (close + 1).ToString(CultureInfo.InvariantCulture));
            q.Parameters.AddWithValue("$low", (close - 1).ToString(CultureInfo.InvariantCulture));
            q.Parameters.AddWithValue("$at", Now.AddDays(-1).ToString("O", CultureInfo.InvariantCulture));
            q.Parameters.AddWithValue("$revision", HistoricalResearchAdmission.Revision); q.ExecuteNonQuery();
        }
        tx.Commit();
        var source = EodhdMarketMemory.InspectHistoricalResearchSource(db.Path, Now);
        var instrument = new CanonicalInstrument(new("US:XNAS:AAPL"), InstrumentType.Equity, "Fixture AAPL", new("USD"),
            new("NASDAQ", "NASDAQ"), "XNAS", InstrumentLifecycle.Active, dates[0], dates[^1]);
        var mapping = new ProviderInstrumentMapping(instrument.Id, new("NASDAQ-WIKI"), new("PRICES"), "AAPL",
            instrument.Venue, "XNAS", dates[0], dates[^1], new("fixture:historical-mapping"));
        return new("fixture:separate-one-shot-owner-authority", "fixture:pinned-raw-history", instrument, mapping,
            ResearchLearningFixture.Facts with { CorporateActions = DatasetEvidenceResult.Unknown, Limitations = HistoricalResearchAdmission.RequiredLimitations },
            "fixture:historical-reasoner", Now.AddHours(1), new(1, 1, 3, 64, 33, 33), [new(dates[0], dates[499], Now)])
        { Historical = new(HistoricalResearchAdmission.Version, HistoricalResearchAdmission.Candidate, HistoricalResearchAdmission.Revision, source.Artifact, source.Commitment, HistoricalResearchAdmission.Model) };

        void Execute(string sql)
        {
            using var q = c.CreateCommand(); q.Transaction = tx; q.CommandText = sql;
            q.Parameters.AddWithValue("$candidate", HistoricalResearchAdmission.Candidate);
            q.Parameters.AddWithValue("$revision", HistoricalResearchAdmission.Revision);
            q.Parameters.AddWithValue("$at", Now.AddDays(-1).ToString("O", CultureInfo.InvariantCulture));
            q.Parameters.AddWithValue("$hash", Hash); q.ExecuteNonQuery();
        }
    }

    private static LearningReasonerReply Proposal(LearningDevelopmentInput input)
    {
        var fixture = ResearchLearningFixture.Scope();
        var json = ResearchLearningFixture.Proposal(fixture).Replace(SyntheticLearningScope.Handle, input.ScopeHandle, StringComparison.Ordinal)
            .Replace(fixture.Input.InputChecksum, input.InputChecksum, StringComparison.Ordinal)
            .Replace("synthetic-next-session-direction", input.TargetId, StringComparison.Ordinal);
        var reply = LearningReplyParser.Parse(json, input); Assert.Null(reply.Rejection); return reply.Reply!;
    }

    [Fact]
    public async Task HistoricalIterationPersistsHypothesisNativeEvaluationAndBudgetAndReopensWithoutReplay()
    {
        using var db = new FinanceMarketObservationTests.Database(); var grant = Seed(db);
        var clock = new FinanceMarketObservationTests.Clock(Now); var memory = db.Memory();
        memory.CreateRecurringResearchProgram(grant, clock, true); var calls = 0;
        Task<LearningReasonerReply> Reason(LearningDevelopmentInput input, CancellationToken _)
        {
            calls++; Assert.NotNull(input.HistoricalDevelopment); Assert.Equal(LearningExposure.Unknown, input.History.Exposure);
            Assert.True(input.HistoricalDevelopment!.LastRawClose > 100); Assert.Equal(8, input.HistoricalDevelopment.Recent.Length);
            Assert.DoesNotContain(grant.Historical!.SnapshotChecksum, JsonSerializer.Serialize(input, Json), StringComparison.Ordinal);
            return Task.FromResult(Proposal(input));
        }
        var result = await memory.RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Reason, CancellationToken.None);
        Assert.Equal(RecurringResearchState.Completed, result.State); Assert.Equal(1, calls);
        var attempt = Assert.Single(result.Attempts); Assert.Equal("Evaluated", attempt.Outcome);
        Assert.NotNull(attempt.Proposal); Assert.NotNull(attempt.EvaluationChecksum); Assert.Empty(attempt.Scope.ReceiptIds);
        Assert.Equal(500, attempt.Scope.Bars.Length); Assert.Equal(64, result.Calls); Assert.Equal(3, result.Trials);
        var saved = JsonSerializer.Serialize(result, Json);
        Assert.Equal(saved, JsonSerializer.Serialize(db.Memory().ReadRecurringResearchProgram(), Json));
        await db.Memory().RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Reason, CancellationToken.None);
        Assert.Equal(1, calls); Assert.Equal(1L, db.Scalar("SELECT COUNT(*) FROM robustness_evaluations"));
    }

    [Fact]
    public async Task ProtectedHoldoutMutationCannotChangeModelInputButChangesFinanceCommitment()
    {
        async Task<(string Input, string Evidence)> Run(bool changed)
        {
            using var db = new FinanceMarketObservationTests.Database(); var grant = Seed(db, changed); var memory = db.Memory();
            var clock = new FinanceMarketObservationTests.Clock(Now); memory.CreateRecurringResearchProgram(grant, clock, true);
            var result = await memory.RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock,
                (input, _) => Task.FromResult(Proposal(input)), CancellationToken.None);
            var scope = Assert.Single(result.Attempts).Scope;
            return (JsonSerializer.Serialize(scope.Input, Json), scope.EvidenceChecksum);
        }
        var original = await Run(false); var changed = await Run(true);
        Assert.Equal(original.Input, changed.Input); Assert.NotEqual(original.Evidence, changed.Evidence);
    }

    [Theory]
    [InlineData("UPDATE observations SET close='0' WHERE session_date='2014-01-02'")]
    [InlineData("UPDATE observations SET acquired_utc='2027-01-01T00:00:00+00:00'")]
    [InlineData("DELETE FROM observations WHERE session_date='2014-01-02'")]
    [InlineData("UPDATE dataset_candidates SET license_class='Unknown'")]
    [InlineData("UPDATE dataset_candidates SET manifest_json='{}'")]
    [InlineData("UPDATE revision_price_capabilities SET raw_capability='INVALID'")]
    public void CorruptedOrChangedSourceFailsAdmissionBeforeInference(string mutation)
    {
        using var db = new FinanceMarketObservationTests.Database(); var grant = Seed(db); db.Execute(mutation);
        Assert.ThrowsAny<InvalidDataException>(() => db.Memory().CreateRecurringResearchProgram(grant, new FinanceMarketObservationTests.Clock(Now), true));
        Assert.Null(db.Memory().ReadRecurringResearchProgram());
    }

    [Fact]
    public async Task NoProtectedAuthorityMeansNoInferenceAndFailureDoesNotRefund()
    {
        using var db = new FinanceMarketObservationTests.Database(); var grant = Seed(db); var clock = new FinanceMarketObservationTests.Clock(Now);
        var memory = db.Memory(); memory.CreateRecurringResearchProgram(grant, clock);
        await Assert.ThrowsAsync<RecurringResearchReconciliationRequiredException>(() => memory.RunRecurringResearchAsync(1,
            grant.ReasonerIdentity, clock, (_, _) => throw new Xunit.Sdk.XunitException("Must not infer"), CancellationToken.None));
        Assert.Equal(0, memory.ReadRecurringResearchProgram()!.Invocations);
    }

    [Fact]
    public async Task ConcurrentCallsAndUncertainReasonerNeverReinvokeOrRefund()
    {
        using var db = new FinanceMarketObservationTests.Database(); var grant = Seed(db); var clock = new FinanceMarketObservationTests.Clock(Now);
        var memory = db.Memory(); memory.CreateRecurringResearchProgram(grant, clock, true); var calls = 0;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<LearningReasonerReply> Reason(LearningDevelopmentInput _, CancellationToken token)
        { calls++; entered.SetResult(); await release.Task.WaitAsync(token); throw new OperationCanceledException(); }
        var first = memory.RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Reason, CancellationToken.None);
        await entered.Task;
        var duplicate = await db.Memory().RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Reason, CancellationToken.None);
        Assert.Equal(RecurringResearchState.ReservedInference, duplicate.State); release.SetResult();
        Assert.Equal(RecurringResearchState.Indeterminate, (await first).State);
        var replay = await db.Memory().RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, Reason, CancellationToken.None);
        Assert.Equal(1, calls); Assert.Equal(1, replay.Invocations); Assert.Equal(33, replay.ReservedSeconds); Assert.Equal(0, replay.Evaluations);
    }

    [Fact]
    public void HistoricalMaintenanceLeavesObservationEnabledAndDoesNotPermitResearchWorkers()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Finance:ResearchOperations:MaintenancePaused"] = "true",
            ["Finance:ObservationRuntime:Enabled"] = "true",
            ["Finance:AlpacaDailyObservation:Enabled"] = "true"
        }).Build();
        FinanceHistoricalResearchCommand.RequireGates(config);
        config["Finance:ResearchScheduler:Enabled"] = "true";
        Assert.Throws<InvalidOperationException>(() => FinanceHistoricalResearchCommand.RequireGates(config));
    }

    [Fact]
    public async Task SameProtectedJournalPreservesHistoricalBudgetAcrossReopenAndRejectsOldDatabaseState()
    {
        using var db = new FinanceMarketObservationTests.Database(); var grant = Seed(db);
        var clock = new FinanceMarketObservationTests.Clock(Now); var memory = db.Memory();
        var initial = memory.CreateRecurringResearchProgram(grant, clock);
        var oldJson = (string)db.Scalar("SELECT snapshot_json FROM recurring_research_program")!;
        var oldHash = (string)db.Scalar("SELECT checksum FROM recurring_research_program")!;
        var path = Path.Combine(Path.GetDirectoryName(db.Path)!, "test-only-journal");
        var enrollment = RecurringResearchAuthority.Enrollment(initial); File.WriteAllText(path, enrollment);
        RecurringResearchAuthority Journal() => new(() => new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite),
            RecurringResearchAuthority.EnrollmentChecksum(enrollment));
        memory.RecurringAuthority = Journal();
        var done = await memory.RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock,
            (input, _) => Task.FromResult(Proposal(input)), CancellationToken.None);
        Assert.Equal(RecurringResearchState.Completed, done.State);
        memory = db.Memory(); memory.RecurringAuthority = Journal(); memory.RequireRecurringAuthority(memory.ReadRecurringResearchProgram()!);
        using var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = db.Path }.ToString()); c.Open();
        using var q = c.CreateCommand(); q.CommandText = "UPDATE recurring_research_program SET checksum=$hash,snapshot_json=$json";
        q.Parameters.AddWithValue("$hash", oldHash); q.Parameters.AddWithValue("$json", oldJson); q.ExecuteNonQuery();
        await Assert.ThrowsAsync<RecurringResearchReconciliationRequiredException>(() => memory.RunRecurringResearchAsync(1,
            grant.ReasonerIdentity, clock, (_, _) => throw new Xunit.Sdk.XunitException("Rollback must not infer"), CancellationToken.None));
    }

    private sealed class Governor(bool allow = true) : IFinanceResearchResourceGovernor
    {
        public Task<FinanceResearchResourceDecision> EvaluateAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken) => Task.FromResult(
            new FinanceResearchResourceDecision(allow ? FinanceResearchResourceDecisionKind.Allow : FinanceResearchResourceDecisionKind.Defer,
                nowUtc, FinanceResearchResourceGovernorOptions.CurrentVersion, ["fixture"],
                new(0, 0, long.MaxValue, long.MaxValue, 1, null, false, "Healthy", nowUtc), "RESEARCH", 0, "NONE"));
    }

    [Fact]
    public async Task OwnedBrf1CompositionTransportsHistoricalInputWithoutModelAndGovernorBlocksBeforeReservation()
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        using var db = new FinanceMarketObservationTests.Database(); var grant = Seed(db);
        var root = Path.Combine(Path.GetDirectoryName(db.Path)!, "coordination");
        Directory.CreateDirectory(root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var clock = new FinanceMarketObservationTests.Clock(Now); var memory = db.Memory();
        await using var composition = new LocalRecurringResearchComposition(new("fixture:approved-proof", "fixture:no-model-isolation",
            Now, Now.AddHours(1), new()
            {
                Enabled = true,
                CoordinationDirectory = root,
                WorkerExecutable = Path.Combine(AppContext.BaseDirectory, "reasoner-proof", "BigBrain.Reasoner.ProofWorker"),
                WorkerArguments = ["no-useful"],
                InvocationTimeout = TimeSpan.FromSeconds(10)
            }), memory.RecordRecurringRuntimeAudit, clock);
        grant = grant with { ReasonerIdentity = composition.Binding.Identity };
        memory.CreateRecurringResearchProgram(grant, clock, true);
        var binding = composition.Binding with { ModelFreeFixture = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => FinanceHistoricalResearchCommand.ExecuteAsync(memory, binding,
            new Governor(false), clock, CancellationToken.None));
        Assert.Equal(0, memory.ReadRecurringResearchProgram()!.Invocations);
        var result = await FinanceHistoricalResearchCommand.ExecuteAsync(memory, binding, new Governor(), clock, CancellationToken.None);
        Assert.Equal("NoUsefulProposal", result.StopReason); Assert.Equal(1, result.Invocations);
        Assert.Equal(0, result.Evaluations); Assert.NotEmpty(result.Attempts[0].RuntimeAudit!.Value);
        Assert.Equal(JsonSerializer.Serialize(result, Json), JsonSerializer.Serialize(db.Memory().ReadRecurringResearchProgram(), Json));
    }

    [Fact]
    public async Task EnrollmentAndReadOnlyInspectionDoNotActivateObservationOrResearchOrProvisionJournal()
    {
        using var db = new FinanceMarketObservationTests.Database(); var grant = Seed(db);
        var root = Path.GetDirectoryName(db.Path)!; var clock = new FinanceMarketObservationTests.Clock(Now);
        File.WriteAllText(Path.Combine(root, "synchronized"), "");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Finance:Eodhd:DatabasePath"] = db.Path,
            ["Finance:Eodhd:PayloadDirectory"] = Path.Combine(root, "payloads"),
            ["Finance:ResearchOperations:MaintenancePaused"] = "true",
            ["Finance:ObservationRuntime:Enabled"] = "true",
            ["Finance:AlpacaDailyObservation:Enabled"] = "true",
            ["SystemRecovery:ClockSyncDirectory"] = root,
            ["SystemRecovery:DatabasePath"] = db.Path,
            ["SystemRecovery:LowDiskCriticalBytes"] = "1"
        }).Build();
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        Assert.Equal(0, await FinanceHistoricalResearchCommand.RunAsync(["finance-historical-inspect"], config, clock, output, null, CancellationToken.None));
        Assert.Null(db.Memory().ReadRecurringResearchProgram());
        var bytes = EodhdMarketMemory.ExportRecurringAuthorization(grant); var path = Path.Combine(root, "grant.json"); File.WriteAllBytes(path, bytes);
        Assert.Equal(0, await FinanceHistoricalResearchCommand.RunAsync(["finance-historical-enroll", path, LearningIdentity.HashBytes(bytes)],
            config, clock, output, null, CancellationToken.None));
        Assert.Equal(0, db.Memory().ReadRecurringResearchProgram()!.Invocations);
        Assert.Equal("true", config["Finance:ObservationRuntime:Enabled"]);
        Assert.Equal(1, await FinanceHistoricalResearchCommand.RunAsync(["finance-historical-run"], config, clock, output,
            (_, _) => throw new Xunit.Sdk.XunitException("No authority"), CancellationToken.None));
        Assert.Equal(0, db.Memory().ReadRecurringResearchProgram()!.Invocations);
    }

    [Fact]
    public async Task HistoricalProgramCannotBeAutomaticallyScheduledAndRetainedEvidenceIsRevalidated()
    {
        using var db = new FinanceMarketObservationTests.Database(); var grant = Seed(db);
        var clock = new FinanceMarketObservationTests.Clock(Now); var memory = db.Memory();
        memory.CreateRecurringResearchProgram(grant, clock, true);
        var scheduler = new FinanceResearchOrchestrator(new() { Enabled = true, RecurringProgramMode = true }, memory,
            new Governor(), new(), new(grant.ReasonerIdentity, (_, _) => throw new Xunit.Sdk.XunitException("No automatic historical inference"),
                TimeSpan.FromSeconds(30), true), clock);
        Assert.Null(await scheduler.CheckAndRunAsync(Now, true, CancellationToken.None)); Assert.Null(scheduler.Status(Now).NextDueUtc);
        await memory.RunRecurringResearchAsync(1, grant.ReasonerIdentity, clock, (input, _) => Task.FromResult(Proposal(input)), CancellationToken.None);
        db.Execute("UPDATE observations SET close='101' WHERE session_date='2014-01-02'");
        Assert.Throws<InvalidDataException>(() => db.Memory().ReadRecurringResearchProgram());
        Assert.Equal(99, FinanceSchemaMigrator.State(db.Path).CurrentVersion);
    }

    [Theory]
    [InlineData("budget")]
    [InlineData("cohort")]
    [InlineData("rights")]
    [InlineData("limitations")]
    [InlineData("model")]
    [InlineData("prospective")]
    public void AuthorityCannotBroadenOrMislabelHistoricalScope(string change)
    {
        using var db = new FinanceMarketObservationTests.Database(); var grant = Seed(db);
        grant = change switch
        {
            "budget" => grant with { Budget = grant.Budget with { Invocations = 2 } },
            "cohort" => grant with { Cohorts = [grant.Cohorts[0] with { From = grant.Cohorts[0].From.AddDays(1) }] },
            "rights" => grant with { Facts = grant.Facts with { ExternalRights = DatasetEvidenceResult.Unknown } },
            "limitations" => grant with { Facts = grant.Facts with { Limitations = [] } },
            "model" => grant with { Historical = grant.Historical! with { ModelArtifactChecksum = "unverified" } },
            _ => grant with { Historical = null }
        };
        Assert.Throws<InvalidOperationException>(() => grant.Validate(Now));
    }

    [Fact]
    public void HostingRejectsDifferentActualModelBytesBeforeRuntimeConstruction()
    {
        using var db = new FinanceMarketObservationTests.Database(); var grant = Seed(db);
        var clock = new FinanceMarketObservationTests.Clock(Now); var memory = db.Memory();
        memory.CreateRecurringResearchProgram(grant, clock, true);
        var model = Path.Combine(Path.GetDirectoryName(db.Path)!, "not-a-model");
        File.WriteAllText(model, "model-free negative fixture; never infer");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Finance:RecurringLocalReasoner:Enabled"] = "true",
            ["Finance:RecurringLocalReasoner:Profile:Runtime:WorkerArguments:0"] = "fixture-unused",
            ["Finance:RecurringLocalReasoner:Profile:Runtime:WorkerArguments:1"] = model,
            ["Finance:RecurringLocalReasoner:Profile:Runtime:WorkerArguments:2"] = "fixture-unused"
        }).Build();
        var error = Assert.Throws<InvalidOperationException>(() => LocalRecurringResearchComposition.FromConfiguration(config, memory, clock));
        Assert.Equal("Historical model identity mismatch.", error.Message);
        Assert.Equal(0, memory.ReadRecurringResearchProgram()!.Invocations);
    }
}
