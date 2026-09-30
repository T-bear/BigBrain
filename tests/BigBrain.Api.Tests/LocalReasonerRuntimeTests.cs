using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using BigBrain.Brain;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class LocalReasonerRuntimeTests
{
    [Theory]
    [InlineData("exit", 3)]
    [InlineData("truncated-header", 0)]
    public async Task TerminalAuditRetainsOwnedExitCodeEvenWhenFrameIsIncomplete(string mode, int expectedExit)
    {
        using var fixture = new RuntimeFixture();
        await using var runtime = fixture.Runtime(mode);
        await Assert.ThrowsAsync<LocalReasonerException>(() => runtime.ReasonAsync(fixture.Input, CancellationToken.None));
        var terminal = fixture.Events.Last();
        Assert.Equal(LocalReasonerAuditPhase.Completed, terminal.Phase);
        Assert.Equal(expectedExit, terminal.WorkerExitCode);
        Assert.Null(terminal.ResponseHash);
        Assert.Null(fixture.Events.First().WorkerExitCode);
        Assert.DoesNotContain("UNTRUSTED", JsonSerializer.Serialize(terminal), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisabledAndPreCancelledNeverStartWorker()
    {
        using var fixture = new RuntimeFixture();
        await using var runtime = fixture.Runtime("valid", enabled: false);
        Assert.Equal(LocalReasonerFailure.Disabled, (await Assert.ThrowsAsync<LocalReasonerException>(() => runtime.ReasonAsync(fixture.Input, CancellationToken.None))).Failure);
        Assert.Empty(fixture.Events); Assert.False(File.Exists(fixture.Reservation));
        await using var enabled = fixture.Runtime("valid");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => enabled.ReasonAsync(fixture.Input, new CancellationToken(true)));
        Assert.Empty(fixture.Events);
    }

    [Fact]
    public async Task ValidRuntimeReplyStillRequiresCAdmissionAndNativeScience()
    {
        using var fixture = new RuntimeFixture(); using var db = new ResearchReasonerContractTests.Database();
        var scope = ResearchLearningFixture.Scope(); var memory = db.Enroll(scope);
        await using var runtime = fixture.Runtime("valid");
        var run = new ResearchReasonerContractTests.TestOnlyInvocation(memory, scope);
        Assert.Equal(LearningAdmissionReason.Admitted, await run.Invoke(runtime, CancellationToken.None));
        Assert.Equal(1, run.EngineCalls);
        var state = db.Memory().ReadLearningLedger();
        Assert.Equal(3, state.Trials); Assert.Equal(1, state.Invocations); Assert.Equal(1, state.EngineStarts);
        Assert.Equal("RESEARCH", state.Result!.OperatingMode); Assert.Equal("NONE", state.Result.ExecutionAuthority);
        Assert.Equal(run.Build!.Evaluation.Checksum, state.Result.Checksum);
        Assert.Equal(LocalReasonerOutcome.Proposal, fixture.Events.Last().Outcome);
        Assert.All(fixture.Events, x => Assert.Null(x.ReplyRejection));
        Assert.False(File.Exists(fixture.Reservation));
        Assert.Equal(LearningAdmissionReason.BudgetExceeded, await run.Invoke(runtime, CancellationToken.None));
        Assert.Equal(3, fixture.Events.Count); // No retry or second invocation.
        var changedRights = SyntheticLearningScope.Create(scope.Plan, scope.Bars, scope.Features,
            ResearchLearningFixture.Facts with { OwnerDecision = DatasetOwnerRightsDecision.NotProvided }, scope.Purpose, scope.Input.HistoryChecksum);
        Assert.NotEqual(LearningAdmissionReason.Admitted,
            LearningAdmissionPolicy.Admit(ResearchLearningFixture.Proposal(scope), changedRights, ResearchLearningFixture.Fresh).Reason);
    }

    [Theory]
    [InlineData("no-useful", LearningAdmissionReason.NoUsefulProposal)]
    [InlineData("malformed", null)]
    [InlineData("oversize", null)]
    [InlineData("exit", null)]
    [InlineData("hang", null)]
    public async Task DeclineOrRuntimeFailurePreservesCInvocationWithoutEngineOrRetry(string mode, LearningAdmissionReason? expected)
    {
        using var fixture = new RuntimeFixture(); using var db = new ResearchReasonerContractTests.Database();
        var scope = ResearchLearningFixture.Scope(); var memory = db.Enroll(scope);
        await using var runtime = fixture.Runtime(mode, timeout: TimeSpan.FromSeconds(2));
        var run = new ResearchReasonerContractTests.TestOnlyInvocation(memory, scope);
        Assert.Equal(expected, await run.Invoke(runtime, CancellationToken.None));
        Assert.Equal(0, run.EngineCalls);
        var state = db.Memory().ReadLearningLedger();
        Assert.Equal(1, state.Invocations); Assert.Equal(0, state.Trials); Assert.Equal(0, state.EngineStarts);
        Assert.Null(state.Result);
        Assert.Equal(LearningAdmissionReason.BudgetExceeded, await run.Invoke(runtime, CancellationToken.None));
        Assert.Single(fixture.Events, x => x.Phase == LocalReasonerAuditPhase.Started);
    }

    [Fact]
    public async Task RealWorkerCancellationDoesNotRefundCOrRunScience()
    {
        using var fixture = new RuntimeFixture(); using var db = new ResearchReasonerContractTests.Database();
        var scope = ResearchLearningFixture.Scope(); var memory = db.Enroll(scope);
        await using var runtime = fixture.Runtime("late"); using var cancel = new CancellationTokenSource();
        var run = new ResearchReasonerContractTests.TestOnlyInvocation(memory, scope);
        var pending = run.Invoke(runtime, cancel.Token);
        await fixture.ResponseStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        cancel.Cancel(); Assert.Null(await pending);
        var reopened = db.Memory().ReadLearningLedger();
        Assert.Equal(1, reopened.Invocations); Assert.Equal(0, reopened.EngineStarts); Assert.Equal(0, run.EngineCalls);
        Assert.Null(reopened.Result); Assert.Equal(LearningLifecycle.Failed, reopened.Lifecycle);
        Assert.Equal(LearningAdmissionReason.BudgetExceeded, await run.Invoke(runtime, CancellationToken.None));
        Assert.Single(fixture.Events, x => x.Phase == LocalReasonerAuditPhase.Started);
    }

    [Fact]
    public void TrustedConfigurationIsBoundedAndRequiresPrivateCoordinationDirectory()
    {
        using var fixture = new RuntimeFixture(); var options = fixture.Options("valid");
        foreach (var invalid in new[] { options with { WorkerExecutable = "relative" },
            options with { WorkerArguments = [new string('x', 257)] },
            options with { InvocationTimeout = TimeSpan.FromSeconds(31) },
            options with { GracePeriod = TimeSpan.Zero }, options with { ReapTimeout = TimeSpan.FromDays(1) } })
            Assert.Throws<ArgumentException>(() => new LocalReasonerRuntime(invalid, _ => { }));
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        File.SetUnixFileMode(fixture.Root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead);
        Assert.Throws<ArgumentException>(() => new LocalReasonerRuntime(options, _ => { }));
    }

    [Theory]
    [InlineData("Deny")]
    [InlineData("Halt")]
    [InlineData("InsufficientData")]
    [InlineData(null)]
    public async Task WorkerRiskAndTradingProseCannotOverrideVeto(string? risk)
    {
        using var fixture = new RuntimeFixture(); using var db = new ResearchReasonerContractTests.Database();
        var scope = ResearchLearningFixture.Scope(); var memory = db.Enroll(scope);
        await using var runtime = fixture.Runtime("valid");
        var run = new ResearchReasonerContractTests.TestOnlyInvocation(memory, scope)
        { RequireRisk = true, MockedRisk = risk is null ? null : Enum.Parse<FinanceRiskVerdict>(risk) };
        Assert.Equal(LearningAdmissionReason.RiskVeto, await run.Invoke(runtime, CancellationToken.None));
        Assert.Equal(0, run.EngineCalls); Assert.Equal(0, db.Memory().ReadLearningLedger().EngineStarts);
    }

    [Theory]
    [InlineData("malformed", LearningAdmissionReason.Malformed)]
    [InlineData("duplicate", LearningAdmissionReason.Malformed)]
    [InlineData("fence", LearningAdmissionReason.Malformed)]
    [InlineData("trailing-json", LearningAdmissionReason.Malformed)]
    [InlineData("depth", LearningAdmissionReason.Malformed)]
    [InlineData("null", LearningAdmissionReason.Malformed)]
    [InlineData("tool", LearningAdmissionReason.Malformed)]
    [InlineData("pid", LearningAdmissionReason.Malformed)]
    [InlineData("executable", LearningAdmissionReason.Malformed)]
    [InlineData("signal", LearningAdmissionReason.Malformed)]
    [InlineData("risk", LearningAdmissionReason.Malformed)]
    [InlineData("PAPER", LearningAdmissionReason.Malformed)]
    [InlineData("version", LearningAdmissionReason.UnsupportedContract)]
    [InlineData("evidence", LearningAdmissionReason.EvidenceMismatch)]
    [InlineData("strategy", LearningAdmissionReason.UnsupportedStrategy)]
    [InlineData("parameter", LearningAdmissionReason.UnsupportedParameter)]
    [InlineData("multiple", LearningAdmissionReason.InvalidVariantCount)]
    [InlineData("missing", LearningAdmissionReason.Malformed)]
    public async Task RealPipeReplyUsesExistingFinanceParserAndNeverRepairs(string mode, LearningAdmissionReason expectedRejection)
    {
        using var fixture = new RuntimeFixture(); await using var runtime = fixture.Runtime(mode);
        var failure = await Assert.ThrowsAsync<LocalReasonerException>(() => runtime.ReasonAsync(fixture.Input, CancellationToken.None));
        Assert.Equal(LocalReasonerFailure.InvalidReply, failure.Failure);
        Assert.Equal(expectedRejection, failure.ReplyRejection);
        Assert.Equal(expectedRejection, fixture.Events.Last().ReplyRejection);
        Assert.All(fixture.Events.Where(x => x.Phase != LocalReasonerAuditPhase.Completed), x => Assert.Null(x.ReplyRejection));
        Assert.Null(failure.InnerException);
        Assert.Equal("Local reasoner: InvalidReply.", failure.Message);
        Assert.Null(fixture.Events.Last().ResponseHash);
        Assert.False(File.Exists(fixture.Reservation));
    }

    [Theory]
    [InlineData("bad-frame")]
    [InlineData("oversize")]
    [InlineData("negative")]
    [InlineData("truncated-header")]
    [InlineData("truncated")]
    [InlineData("utf8")]
    [InlineData("trailing-frame")]
    [InlineData("stderr")]
    public async Task TransportFailsClosedBeforeFinanceReply(string mode)
    {
        using var fixture = new RuntimeFixture(); await using var runtime = fixture.Runtime(mode);
        var failure = await Assert.ThrowsAsync<LocalReasonerException>(() => runtime.ReasonAsync(fixture.Input, CancellationToken.None));
        Assert.Contains(failure.Failure, new[] { LocalReasonerFailure.InvalidTransport, LocalReasonerFailure.WorkerFailed });
        Assert.Null(failure.ReplyRejection);
        Assert.All(fixture.Events, x => Assert.Null(x.ReplyRejection));
        Assert.DoesNotContain("UNTRUSTED", JsonSerializer.Serialize(fixture.Events), StringComparison.Ordinal);
        Assert.InRange(fixture.Events.Count, 2, 3); Assert.False(File.Exists(fixture.Reservation));
    }

    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(65537)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task LengthIsRejectedAfterEightBytesWithoutReadingPayload(int length)
    {
        var header = new byte[8]; "BRF1"u8.CopyTo(header); BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), length);
        using var stream = new MemoryStream(header);
        Assert.Equal(LocalReasonerFailure.InvalidTransport,
            (await Assert.ThrowsAsync<LocalReasonerException>(() => LocalReasonerProtocol.ReadAsync(stream, CancellationToken.None))).Failure);
        Assert.Equal(8, stream.Position);
    }

    [Fact]
    public async Task OversizedRequestFailsBeforeSerializationOrWorker()
    {
        using var fixture = new RuntimeFixture(); await using var runtime = fixture.Runtime("valid");
        var input = fixture.Input with { ScopeHandle = new string('x', 65537) };
        Assert.Equal(LocalReasonerFailure.InvalidInput,
            (await Assert.ThrowsAsync<LocalReasonerException>(() => runtime.ReasonAsync(input, CancellationToken.None))).Failure);
        Assert.DoesNotContain(fixture.Events, x => x.Phase == LocalReasonerAuditPhase.Started);
        Assert.False(File.Exists(fixture.Reservation));
    }

    [Fact]
    public async Task WorkerRejectsUnknownDuplicateAndOversizedRequestFrames()
    {
        using var fixture = new RuntimeFixture();
        foreach (var payload in new[] { "{}", "{\"version\":1,\"version\":1}", "{\"pid\":1}", "null" })
        {
            using var worker = new OwnedReasonerWorker(fixture.Options("valid"));
            await LocalReasonerProtocol.WriteAsync(worker.Input, Encoding.UTF8.GetBytes(payload), CancellationToken.None);
            worker.Input.Close();
            await worker.WaitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
            Assert.Equal(4, worker.ExitCode);
        }
        using var excessive = new OwnedReasonerWorker(fixture.Options("valid"));
        var header = new byte[8]; "BRF1"u8.CopyTo(header); BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), int.MaxValue);
        await excessive.Input.WriteAsync(header, TestContext.Current.CancellationToken); excessive.Input.Close();
        await excessive.WaitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        Assert.Equal(4, excessive.ExitCode);
    }

    [Fact]
    public async Task SingleFlightAppliesAcrossControllerInstancesAndRetainedReservation()
    {
        using var fixture = new RuntimeFixture();
        await using var first = fixture.Runtime("hang"); await using var second = fixture.Runtime("valid");
        using var cancel = new CancellationTokenSource();
        var pending = first.ReasonAsync(fixture.Input, cancel.Token);
        await fixture.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(LocalReasonerFailure.Busy,
            (await Assert.ThrowsAsync<LocalReasonerException>(() => first.ReasonAsync(fixture.Input, CancellationToken.None))).Failure);
        Assert.Equal(LocalReasonerFailure.Busy,
            (await Assert.ThrowsAsync<LocalReasonerException>(() => second.ReasonAsync(fixture.Input, CancellationToken.None))).Failure);
        cancel.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.False(File.Exists(fixture.Reservation));
        await File.WriteAllTextAsync(fixture.Reservation, "uncertain previous process", TestContext.Current.CancellationToken); // Simulated crash; never auto-clear.
        await using var restarted = fixture.Runtime("valid");
        Assert.Equal(LocalReasonerFailure.Busy,
            (await Assert.ThrowsAsync<LocalReasonerException>(() => restarted.ReasonAsync(fixture.Input, CancellationToken.None))).Failure);
        Assert.True(File.Exists(fixture.Reservation));
        Assert.Single(fixture.Events, x => x.Phase == LocalReasonerAuditPhase.Started);
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("disable")]
    [InlineData("dispose")]
    public async Task CancellationOrKillSwitchReapsOwnedWorkerAndDiscardsLateReply(string action)
    {
        using var fixture = new RuntimeFixture(); await using var runtime = fixture.Runtime("late");
        using var cancel = new CancellationTokenSource();
        var pending = runtime.ReasonAsync(fixture.Input, cancel.Token);
        await fixture.ResponseStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        if (action == "cancel") cancel.Cancel();
        else if (action == "disable") runtime.Disable();
        else await runtime.DisposeAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(LocalReasonerOutcome.Cancelled, fixture.Events.Last().Outcome);
        Assert.Null(fixture.Events.Last().ResponseHash); Assert.False(File.Exists(fixture.Reservation));
        Assert.Single(fixture.Events, x => x.Phase == LocalReasonerAuditPhase.Started);
    }

    [Theory]
    [InlineData("hang")]
    [InlineData("no-read")]
    public async Task TimeoutStopsWorkAndTripsInstanceWithoutRetry(string mode)
    {
        using var fixture = new RuntimeFixture(); await using var runtime = fixture.Runtime(mode, timeout: TimeSpan.FromSeconds(2));
        var watch = Stopwatch.StartNew();
        Assert.Equal(LocalReasonerFailure.Timeout,
            (await Assert.ThrowsAsync<LocalReasonerException>(() => runtime.ReasonAsync(fixture.Input, CancellationToken.None))).Failure);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(7));
        Assert.True(fixture.Events.Last().WorkerCleanupRequired);
        Assert.NotNull(fixture.Events.Last().WorkerExitCode);
        Assert.Null(fixture.Events.Last().ResponseHash);
        Assert.False(File.Exists(fixture.Reservation));
        Assert.Equal(LocalReasonerFailure.Disabled,
            (await Assert.ThrowsAsync<LocalReasonerException>(() => runtime.ReasonAsync(fixture.Input, CancellationToken.None))).Failure);
        Assert.InRange(fixture.Events.Count, 2, 3);
    }

    [Fact]
    public async Task RetainedIdentityTerminationCannotAffectAnotherOwnedChildOrReattachAfterDispose()
    {
        using var fixture = new RuntimeFixture();
        using var first = new OwnedReasonerWorker(fixture.Options("hang"));
        using var other = new OwnedReasonerWorker(fixture.Options("hang"));
        try
        {
            foreach (var worker in new[] { first, other })
            {
                await LocalReasonerProtocol.WriteAsync(worker.Input, LocalReasonerProtocol.Request(fixture.Input), CancellationToken.None);
                worker.Input.Close();
                // Proof worker has installed SIGTERM-ignore and is actually hung, not merely starting.
                await worker.Output.ReadExactlyAsync(new byte[8], new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
            }
            Assert.True(await first.StopAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(2)));
            Assert.True(first.Exited); Assert.False(other.Exited);
            first.Dispose();
            Assert.True(await first.StopAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(2)));
            Assert.False(other.Exited); // Disposed authority does nothing; cannot reacquire/retarget.
        }
        finally { Assert.True(await other.StopAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(2))); }
    }

    [Theory]
    [InlineData(LocalReasonerAuditPhase.Started)]
    [InlineData(LocalReasonerAuditPhase.Completed)]
    public async Task AuditFailureIsSanitizedAndNeverReturnsAReply(LocalReasonerAuditPhase phase)
    {
        using var fixture = new RuntimeFixture();
        await using var runtime = new LocalReasonerRuntime(fixture.Options("valid"), record =>
        { fixture.Events.Enqueue(record); if (record.Phase == phase) throw new IOException("SECRET RAW LOG MUST NOT ESCAPE"); });
        var failure = await Assert.ThrowsAsync<LocalReasonerException>(() => runtime.ReasonAsync(fixture.Input, CancellationToken.None));
        Assert.Equal(LocalReasonerFailure.AuditFailed, failure.Failure); Assert.Null(failure.InnerException);
        Assert.DoesNotContain("SECRET", failure.ToString(), StringComparison.Ordinal);
        Assert.Equal(phase == LocalReasonerAuditPhase.Completed, File.Exists(fixture.Reservation));
    }

    [Fact]
    public async Task AuditContainsOnlyBoundedMetadataAndMaliciousProseRemainsInert()
    {
        using var fixture = new RuntimeFixture(); await using var runtime = fixture.Runtime("valid");
        var reply = Assert.IsType<LearningReasonerReply.Proposal>(await runtime.ReasonAsync(fixture.Input, CancellationToken.None));
        Assert.Contains("/bin/sh", reply.Draft.Rationale, StringComparison.Ordinal);
        var audit = fixture.Events.ToArray(); Assert.Equal(3, audit.Length);
        Assert.Equal(audit[0].InvocationId, audit[1].InvocationId);
        Assert.All(audit, x => { Assert.Equal(64, x.InputHash.Length); Assert.Equal(64, x.WorkerConfigurationHash.Length); Assert.Equal("Local", x.Mode); });
        Assert.Equal(64, audit[^1].ResponseHash!.Length);
        var json = JsonSerializer.Serialize(audit);
        foreach (var secret in new[] { "SELECT", "/bin/sh", "https://", "ignore previous", fixture.Root, "holdout", "broker" })
            Assert.DoesNotContain(secret, json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancellationDuringTerminalAuditCannotDeliverAReply()
    {
        using var fixture = new RuntimeFixture(); using var cancel = new CancellationTokenSource();
        await using var runtime = new LocalReasonerRuntime(fixture.Options("valid"), record =>
        {
            fixture.Events.Enqueue(record);
            if (record.Phase == LocalReasonerAuditPhase.Completed && record.Outcome == LocalReasonerOutcome.Proposal)
                cancel.Cancel();
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runtime.ReasonAsync(fixture.Input, cancel.Token));
        Assert.Equal(LocalReasonerOutcome.Cancelled, fixture.Events.Last().Outcome);
        Assert.Null(fixture.Events.Last().ResponseHash); Assert.False(File.Exists(fixture.Reservation));
    }

    [Fact]
    public void OwnedLifecycleAndPortExposeNoAttachKillPidOrToolService()
    {
        Assert.False(typeof(OwnedReasonerWorker).IsPublic);
        Assert.Empty(typeof(OwnedReasonerWorker).GetConstructors());
        Assert.DoesNotContain(typeof(OwnedReasonerWorker).GetMethods(BindingFlags.Public | BindingFlags.Instance),
            x => x.GetParameters().Any(p => p.ParameterType == typeof(int) || p.ParameterType == typeof(Process)));
        Assert.DoesNotContain(typeof(LocalReasonerRuntime).GetMethods(), x => x.Name.Contains("Kill", StringComparison.Ordinal) || x.Name.Contains("StartProcess", StringComparison.Ordinal));
        Assert.DoesNotContain(typeof(LearningDevelopmentInput).GetProperties(), x => x.Name is "Pid" or "Executable" or "Arguments" or "Signal");
        Assert.Equal(new[] { typeof(LearningDevelopmentInput), typeof(CancellationToken) },
            typeof(IResearchReasoner).GetMethod("ReasonAsync")!.GetParameters().Select(x => x.ParameterType));
        Assert.DoesNotContain(typeof(LocalReasonerRuntime).Assembly.GetReferencedAssemblies(),
            x => x.Name is "BigBrain.Api" or "Microsoft.Data.Sqlite" or "System.Net.Http");
        Assert.DoesNotContain(typeof(LocalReasonerRuntimeOptions).GetProperties(), x => x.Name.Contains("External", StringComparison.Ordinal));
    }

    private sealed class RuntimeFixture : IDisposable
    {
        internal string Root { get; } = Path.Combine(Path.GetTempPath(), "bb131f-" + Guid.NewGuid().ToString("N"));
        internal string Reservation => Path.Combine(Root, ".local-reasoner-reservation");
        internal LearningDevelopmentInput Input { get; } = ResearchLearningFixture.Scope().Input;
        internal ConcurrentQueue<LocalReasonerAudit> Events { get; } = new();
        internal TaskCompletionSource ResponseStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal RuntimeFixture()
        {
            if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("Linux proof required.");
            Directory.CreateDirectory(Root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        internal LocalReasonerRuntimeOptions Options(string mode) => new()
        {
            Enabled = true,
            WorkerExecutable = Path.Combine(AppContext.BaseDirectory, "reasoner-proof", "BigBrain.Reasoner.ProofWorker"),
            WorkerArguments = [mode],
            CoordinationDirectory = Root
        };
        internal LocalReasonerRuntime Runtime(string mode, bool enabled = true, TimeSpan? timeout = null) =>
            new(Options(mode) with { Enabled = enabled, InvocationTimeout = timeout ?? TimeSpan.FromSeconds(10) }, record =>
            { Events.Enqueue(record); if (record.Phase == LocalReasonerAuditPhase.Started) Started.TrySetResult(); if (record.Phase == LocalReasonerAuditPhase.ResponseStarted) ResponseStarted.TrySetResult(); });
        public void Dispose() => Directory.Delete(Root, true);
    }
}
