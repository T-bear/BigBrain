using System.Diagnostics;
using System.Text.Json;
using System.Security.Cryptography;
using BigBrain.Brain;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Tests;

// Explicit controlled local acceptance only. Ordinary CI never downloads/loads model artifacts.
// The opt-in path is trusted operator configuration, never a Finance API/request/reply property.
public sealed class LocalModelAcceptanceTests
{
    public static bool Enabled => Environment.GetEnvironmentVariable("BB132A_LOCAL_ACCEPTANCE") == "1";

    [Fact(Skip = "Explicit controlled local model opt-in required.", SkipUnless = nameof(Enabled))]
    [Trait("Category", "ControlledLocalModel")]
    public async Task NativeWorkerModelReplyRequiresExistingFinanceAdmission()
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        var root = Environment.GetEnvironmentVariable("BB132A_ARTIFACT_ROOT")
            ?? throw new InvalidOperationException("Explicit local artifact root required.");
        var model = Path.Combine(root, "Qwen3-4B-Q4_K_M.gguf");
        Assert.Equal(2497280256L, new FileInfo(model).Length);
        await using (var weights = File.OpenRead(model))
        {
            Assert.Equal("7485fe6f11af29433bc51cab58009521f205840f5b4ae3a32fa7f92e8534fdf5",
                Convert.ToHexStringLower(await SHA256.HashDataAsync(weights, TestContext.Current.CancellationToken)));
        }
        var directory = Path.Combine(root, "acceptance");
        Directory.CreateDirectory(directory);
        File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var auditPath = Path.Combine(directory, "invocation.jsonl");
        // Preserve all prior evidence: no overwrites or automatic retry after an interrupted proof.
        using var audit = new FileStream(auditPath, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.Read,
            Options = FileOptions.WriteThrough,
            UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite
        });
        var scope = ResearchLearningFixture.Scope();
        var memory = new EodhdMarketMemory(new EodhdFinanceOptions
        { DatabasePath = Path.Combine(directory, "finance.db"), PayloadDirectory = Path.Combine(directory, "payloads") });
        memory.EnrollSyntheticLearning(scope, LearningExposure.Unexposed);
        Assert.True(memory.ReserveLearningInvocation());
        await using var runtime = new LocalReasonerRuntime(new()
        {
            Enabled = true,
            WorkerExecutable = Path.Combine(root, "native-worker"),
            WorkerArguments = [Path.Combine(root, "runtime"), Path.Combine(root, "Qwen3-4B-Q4_K_M.gguf"),
                File.ReadAllText(Path.Combine(root, "cgroup-path.private")).Trim()],
            CoordinationDirectory = directory,
            InvocationTimeout = TimeSpan.FromSeconds(30)
        }, record =>
        {
            JsonSerializer.Serialize(audit, record);
            audit.WriteByte((byte)'\n'); audit.Flush(flushToDisk: true);
        });
        var watch = Stopwatch.StartNew();
        LearningReasonerReply reply;
        try { reply = await runtime.ReasonAsync(scope.Input, CancellationToken.None); }
        catch (LocalReasonerException error)
        {
            memory.FailLearningIteration(error.Failure == LocalReasonerFailure.Timeout
                ? BigBrain.Api.Finance.LearningFailure.ReasonerTimeout : BigBrain.Api.Finance.LearningFailure.ReasonerUnavailable);
            Assert.False(memory.ReserveLearningInvocation());
            throw new InvalidOperationException($"Controlled model failure: {error.Failure}; elapsed {watch.ElapsedMilliseconds} ms.", error);
        }
        var result = memory.ReserveLearningProposal(reply.ResponseJson, scope);
        // A parsed model answer is not eligibility or risk approval. Existing Finance alone admits it.
        Assert.True(result.Reason == LearningAdmissionReason.Admitted,
            $"Actual Finance disposition: {result.Reason}; elapsed {watch.ElapsedMilliseconds} ms.");
        Assert.False(memory.ReserveLearningInvocation());
        Assert.Equal(LearningAdmissionReason.Duplicate, memory.ReserveLearningProposal(reply.ResponseJson, scope).Reason);
        // No deterministic evaluation, risk ALLOW, execution, orders or trading is authorized by this test.
    }
    public static bool Controlled180Enabled => Environment.GetEnvironmentVariable("BB132A_LOCAL_ACCEPTANCE") == "180";

    [Fact]
    public void ExtendedDeadlineIsInternalAndDoesNotChangeOrdinaryRuntimePolicy()
    {
        if (!OperatingSystem.IsLinux()) return;
        var path = Path.Combine(Path.GetTempPath(), "bb132a-options-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            var options = new LocalReasonerRuntimeOptions { WorkerExecutable = "/not-started", CoordinationDirectory = path };
            options.Validate();
            Assert.Equal(TimeSpan.FromSeconds(30), options.InvocationTimeout);
            Assert.Throws<ArgumentException>(() => (options with { InvocationTimeout = TimeSpan.FromSeconds(31) }).Validate());
            Assert.Throws<ArgumentException>(() => (options with { InvocationTimeout = TimeSpan.FromSeconds(180) }).Validate());
            var controlled = options with { ControlledRealModelAcceptance = true, InvocationTimeout = TimeSpan.FromSeconds(180) };
            controlled.Validate();
            Assert.Throws<ArgumentException>(() => (controlled with { InvocationTimeout = TimeSpan.FromSeconds(181) }).Validate());
            Assert.Throws<ArgumentException>(() => (controlled with { InvocationTimeout = TimeSpan.FromSeconds(30) }).Validate());
            Assert.Null(typeof(LocalReasonerRuntimeOptions).GetProperty("ControlledRealModelAcceptance"));
            Assert.Equal(30, ResearchLearningFixture.Scope().Input.Limits.ReasonerDeadlineSeconds);
        }
        finally { Directory.Delete(path); }
    }

    [Fact(Skip = "Explicit separately authorized 180-second acceptance only.", SkipUnless = nameof(Controlled180Enabled))]
    [Trait("Category", "ControlledLocalModel")]
    public async Task SeparateAuthorized180SecondInvocationPreservesFailedLedger() => await RunControlledAcceptance(false);

    public static bool Qwen17Enabled => Environment.GetEnvironmentVariable("BB132A_LOCAL_ACCEPTANCE") == "1.7b-180";

    [Fact(Skip = "Explicit Qwen3-1.7B acceptance authorization required.", SkipUnless = nameof(Qwen17Enabled))]
    [Trait("Category", "ControlledLocalModel")]
    public async Task Qwen17BSeparatelyAuthorizedInvocationPreservesBoth4BFailures() => await RunControlledAcceptance(true);

    public static bool DiagnosticEnabled => Environment.GetEnvironmentVariable("BB132A_LOCAL_ACCEPTANCE") == "1.7b-diagnostic";

    [Fact(Skip = "Explicit bounded WorkerFailed diagnosis only.", SkipUnless = nameof(DiagnosticEnabled))]
    [Trait("Category", "ControlledLocalModel")]
    public async Task Qwen17DiagnosticPreservesPriorFailuresWithoutAdmission() => await RunControlledAcceptance(true, diagnostic: true);

    public static bool CorrectedEnabled => Environment.GetEnvironmentVariable("BB132A_LOCAL_ACCEPTANCE") == "1.7b-corrected";

    [Fact(Skip = "One conditionally authorized corrected1.7B acceptance only.", SkipUnless = nameof(CorrectedEnabled))]
    [Trait("Category", "ControlledLocalModel")]
    public async Task Qwen17CorrectedOutputConfigurationRequiresFinanceAdmission() => await RunControlledAcceptance(true, corrected: true);

    public static bool Rc03Enabled => Environment.GetEnvironmentVariable("BB132A_LOCAL_ACCEPTANCE") == "1.7b-rc03";

    [Fact(Skip = "Exactly one owner-authorized RC03 diagnostic invocation only.", SkipUnless = nameof(Rc03Enabled))]
    [Trait("Category", "ControlledLocalModel")]
    public async Task Qwen17Rc03DiagnosticRecordsSanitizedReplyRejectionOnly() => await RunControlledAcceptance(true, rc03: true);

    public static bool FinalAcceptanceEnabled => Environment.GetEnvironmentVariable("BB132A_LOCAL_ACCEPTANCE") == "1.7b-final";

    [Fact(Skip = "Exactly one final owner-authorized BB-132A acceptance invocation only.", SkipUnless = nameof(FinalAcceptanceEnabled))]
    [Trait("Category", "ControlledLocalModel")]
    public async Task Qwen17FinalVersionBoundAcceptanceRequiresFinanceAdmission() => await RunControlledAcceptance(true, finalAcceptance: true);

    private static async Task RunControlledAcceptance(bool qwen17, bool diagnostic = false, bool corrected = false, bool rc03 = false, bool finalAcceptance = false)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        var token = TestContext.Current.CancellationToken;
        var root = Environment.GetEnvironmentVariable("BB132A_ARTIFACT_ROOT")
            ?? throw new InvalidOperationException("Explicit artifact root required.");
        var model = Path.Combine(root, qwen17 ? "Qwen3-1.7B-Q4_K_M.gguf" : "Qwen3-4B-Q4_K_M.gguf");
        Assert.Equal(qwen17 ? 1107409472L : 2497280256L, new FileInfo(model).Length);
        await using (var weights = File.OpenRead(model))
            Assert.Equal(qwen17 ? "b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897"
                : "7485fe6f11af29433bc51cab58009521f205840f5b4ae3a32fa7f92e8534fdf5",
                Convert.ToHexStringLower(await SHA256.HashDataAsync(weights, token)));
        var previous = Path.Combine(root, "acceptance");
        var priorHashes = Directory.GetFiles(previous, "*", SearchOption.AllDirectories)
            .ToDictionary(x => x, x => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(x))), StringComparer.Ordinal);
        if (qwen17)
        {
            var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(Path.Combine(root, finalAcceptance ? "rc03-final-evidence-manifest.json" : rc03 ? "diagnostic-final-evidence-manifest.json" : corrected ? "prior-corrected-evidence-manifest.json" : diagnostic ? "prior-diagnostic-evidence-manifest.json" : "prior-4b-evidence-manifest.json")))!;
            foreach (var (relativePath, hash) in manifest)
            {
                var file = Path.Combine(root, relativePath);
                Assert.Equal(hash, Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file))));
                priorHashes[file] = hash;
            }
            Assert.Equal("a88c7a31b6e92dcb3b3b5d6c1b456d2f71c79e5c3c516ecd1f1afb11ea8e4b95",
                Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, "native-worker-180")))));
        }
        if (rc03)
            Assert.Equal("44bbeca4bca8103299fbd8201c2df5fb8d9133411c9544492493ef91b45a1011",
                Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, "native-worker-corrected")))));
        if (finalAcceptance)
            Assert.Equal("631deb1ccfece198309c0485ada2d5eae62ecd3b146c1efc945078e97dd2eb1b",
                Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, "native-worker-final-version")))));
        Assert.Equal(0, new FileInfo(Path.Combine(previous, "finance.db-wal")).Length);
        // Frozen prior database: immutable read avoids even updating WAL shared-memory read marks.
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = new Uri(Path.Combine(previous, "finance.db")).AbsoluteUri + "?immutable=1",
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT snapshot_json FROM learning_governance WHERE singleton=1";
        using var prior = JsonDocument.Parse(Assert.IsType<string>(command.ExecuteScalar()));
        var state = prior.RootElement;
        Assert.Equal((int)LearningLifecycle.Failed, state.GetProperty("lifecycle").GetInt32());
        Assert.Equal(1, state.GetProperty("invocations").GetInt32());
        Assert.Equal(0, state.GetProperty("engineStarts").GetInt32());
        var admissionState = new LearningAdmissionState(state.GetProperty("submitted").GetInt32(),
            state.GetProperty("trials").GetInt32(), state.GetProperty("engineStarts").GetInt32(),
            (LearningExposure)state.GetProperty("exposure").GetInt32());
        Assert.Null(state.GetProperty("commitment").Deserialize<LearningCommitment>());
        connection.Close();

        var directory = Path.Combine(root, finalAcceptance ? "acceptance-17b-final" : rc03 ? "diagnostic-17b-rc03" : corrected ? "acceptance-17b-corrected" : diagnostic ? "diagnostic-17b" : qwen17 ? "acceptance-17b-180" : "acceptance-180");
        Directory.CreateDirectory(directory);
        File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        using var audit = new FileStream(Path.Combine(directory, "invocation.jsonl"), new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.Read,
            Options = FileOptions.WriteThrough,
            UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite
        });
        void Record(object value) { JsonSerializer.Serialize(audit, value); audit.WriteByte((byte)'\n'); audit.Flush(true); }
        Record(new
        {
            Authorization = finalAcceptance ? "BB-132A-owner-final-version-bound-1.7B-acceptance" : rc03 ? "BB-132A-RC03-owner-one-ReplyRejection-diagnostic" : corrected ? "BB-132A-owner-one-corrected-1.7B-acceptance" : diagnostic ? "BB-132A-owner-bounded-WorkerFailed-diagnosis" : qwen17 ? "BB-132A-owner-Qwen3-1.7B-separate-180s" : "BB-132A-owner-separate-invocation-180s",
            Model = qwen17 ? "Qwen3-1.7B-Q4_K_M" : "Qwen3-4B-Q4_K_M",
            MaximumSeconds = 180,
            PreviousLedgerSha256 = priorHashes[Path.Combine(previous, "finance.db")],
            PriorInvocationCount = 1,
            ScientificBudgetRefund = false,
            EngineExecutionAuthorized = false
        });
        var scope = ResearchLearningFixture.Scope();
        await using var runtime = new LocalReasonerRuntime(new()
        {
            Enabled = true,
            ControlledRealModelAcceptance = true,
            InvocationTimeout = TimeSpan.FromSeconds(180),
            WorkerExecutable = Path.Combine(root, finalAcceptance ? "native-worker-final-version" : rc03 || corrected ? "native-worker-corrected" : diagnostic ? "native-worker-diagnostic" : "native-worker-180"),
            WorkerArguments = [Path.Combine(root, "runtime"), model,
                File.ReadAllText(Path.Combine(root, "cgroup-path.private")).Trim()],
            CoordinationDirectory = directory
        }, record => Record(record));
        var watch = Stopwatch.StartNew();
        try
        {
            var reply = await runtime.ReasonAsync(scope.Input, token);
            if (diagnostic || rc03)
            {
                Record(new { DiagnosticCompleteReply = true, Acceptance = false, EngineCalls = 0 });
                return; // Diagnostic reproduction never admits a proposal or stores raw model text.
            }
            // Synthetic acceptance evidence, separate from operational audit. Never executed or repaired.
            using (var evidence = new FileStream(Path.Combine(directory, "parsed-model-response.json"), new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Options = FileOptions.WriteThrough,
                UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite
            }))
            {
                evidence.Write(System.Text.Encoding.UTF8.GetBytes(reply.ResponseJson));
                evidence.Flush(true);
            }
            // Pure Finance admission only, against retained scientific counters. Never revive the failed C iteration.
            var admission = LearningAdmissionPolicy.Admit(reply.ResponseJson, scope, admissionState);
            Record(new
            {
                Disposition = admission.Reason.ToString(),
                ElapsedMilliseconds = watch.ElapsedMilliseconds,
                ProposalId = admission.Proposal?.ProposalId,
                ExecutionFingerprint = admission.Proposal?.ExecutionFingerprint,
                EngineCalls = 0
            });
            Assert.IsType<LearningReasonerReply.Proposal>(reply);
            Assert.Equal(LearningAdmissionReason.Admitted, admission.Reason);
        }
        catch (LocalReasonerException error)
        {
            Record(new
            {
                Failure = error.Failure.ToString(),
                ReplyRejection = error.ReplyRejection?.ToString(),
                ElapsedMilliseconds = watch.ElapsedMilliseconds,
                EngineCalls = 0
            });
            throw;
        }
        finally
        {
            foreach (var (file, hash) in priorHashes)
                Assert.Equal(hash, Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file))));
        }
    }

}
