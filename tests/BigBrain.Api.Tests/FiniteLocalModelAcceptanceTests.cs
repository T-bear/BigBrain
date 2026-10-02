using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using BigBrain.Api.Finance;
using BigBrain.Brain;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

// One explicitly authorized C session only. Ordinary CI never loads a model.
// Finance production orchestration is reused; this is controlled local acceptance composition,
// not a public endpoint, scheduled service, new scientific engine or another learning ledger.
public sealed class FiniteLocalModelAcceptanceTests
{
    public static bool Enabled => Environment.GetEnvironmentVariable("BB132C_LOCAL_ACCEPTANCE") == "one-session";

    [Fact(Skip = "One owner-authorized BB-132C real session only, after green model-free gates.", SkipUnless = nameof(Enabled))]
    [Trait("Category", "ControlledLocalModel")]
    public async Task OneRealFiniteSessionPersistsNAndReopensHistoryForNPlusOne()
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        var token = TestContext.Current.CancellationToken;
        var artifacts = Environment.GetEnvironmentVariable("BB132A_ARTIFACT_ROOT")
            ?? throw new InvalidOperationException("Pinned local artifact root required.");
        var root = Environment.GetEnvironmentVariable("BB132C_EVIDENCE_ROOT")
            ?? throw new InvalidOperationException("Separate C evidence root required.");
        Assert.NotEqual(Path.GetFullPath(artifacts), Path.GetFullPath(root));
        var model = Path.Combine(artifacts, "Qwen3-1.7B-Q4_K_M.gguf");
        Assert.Equal(1107409472L, new FileInfo(model).Length);
        await using (var weights = File.OpenRead(model))
            Assert.Equal("b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897",
                Convert.ToHexStringLower(await SHA256.HashDataAsync(weights, token)));
        Assert.Equal("783737a2c7b93fa1d165c0f011757b9a13576730a72301a206da1462039ad188",
            HashFile(Path.Combine(root, "native-worker")));
        var prior = JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(artifacts, "final-evidence-manifest.json")))!;
        foreach (var (path, checksum) in prior) Assert.Equal(checksum, HashFile(Path.Combine(artifacts, path)));
        using (var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(artifacts, "runtime-manifest.json"))))
            foreach (var file in manifest.RootElement.EnumerateObject().Where(x => x.Name != "native-worker"))
                Assert.Equal(file.Value.GetProperty("sha256").GetString(), HashFile(Path.Combine(artifacts, "runtime", file.Name)));

        Directory.CreateDirectory(root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Assert.False(File.Exists(Path.Combine(root, "finance.db")), "An existing C session cannot be replaced.");
        // CreateNew/fsync prevents an interrupted acceptance command from silently becoming a retry.
        using var audit = new FileStream(Path.Combine(root, "session.jsonl"), new FileStreamOptions
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
            Authorization = "BB-132C-one-finite-real-session",
            MaximumInvocations = 2,
            MaximumEvaluations = 1,
            Model = "Qwen3-1.7B-Q4_K_M",
            RuntimeDeadlineSeconds = 180,
            Refunds = 0,
            Retries = 0
        });
        EodhdMarketMemory Open() => new(new EodhdFinanceOptions
        { DatabasePath = Path.Combine(root, "finance.db"), PayloadDirectory = Path.Combine(root, "payloads") });
        var scope = ResearchLearningFixture.Scope();
        var clock = TimeProvider.System;
        var memory = Open();
        var session = memory.CreateFiniteResearchSession(scope, clock);
        Record(new { session.SessionId, session.Version, session.Grant, session.ScopeChecksum });
        var starts = 0;
        try
        {
            var n = await Invoke(memory, 1);
            Assert.True(n.State == FiniteIterationState.Completed && n.Outcome == LearningAdmissionReason.Admitted,
                $"C stopped at N: {n.State}/{n.Outcome}/{n.Failure}; no replacement invocation.");
            var scientific = memory.ReadLearningLedger();
            Assert.Equal(1, scientific.EngineStarts); Assert.Equal(3, scientific.Trials);
            Assert.InRange(scientific.UniqueRuns, 1, 64); Assert.Equal(LearningExposure.Consumed, scientific.Exposure);
            Assert.NotEmpty(scientific.Result!.Runs);
            // Reconstruct through the actual Finance reopen/read path; no fake or manufactured history.
            var reopened = Open();
            var result = reopened.RobustnessEvaluation(scientific.Result.EvaluationId)!;
            Assert.Equal(scientific.Result.Checksum, result.Checksum);
            Record(new
            {
                Reopened = true,
                session.SessionId,
                scientific.Result.EvaluationId,
                scientific.Result.Checksum,
                scientific.UniqueRuns,
                scientific.EngineStarts,
                scientific.Trials
            });
            var next = await Invoke(reopened, 2);
            Assert.Equal(LearningIdentity.Hash(scientific), LearningIdentity.Hash(Open().ReadLearningLedger()));
            Assert.Equal(2, starts);
            Assert.True(next.State == FiniteIterationState.Completed &&
                next.Outcome is LearningAdmissionReason.NoUsefulProposal or LearningAdmissionReason.BudgetExceeded,
                $"C stopped at N+1: {next.State}/{next.Outcome}/{next.Failure}; no retry.");
            var state = Open().ReadFiniteResearchSession()!;
            Assert.Equal(2, state.Iterations.Length);
            Assert.Equal(result.PrimarySplit.Test.ExcessReturn, state.Iterations[1].Input.FiniteHistory!.ReferenceValidationExcessReturn);
            Record(new
            {
                CompleteSequence = true,
                Invocations = starts,
                EngineStarts = 1,
                SessionChecksum = LearningIdentity.Hash(state),
                Finance = "RESEARCH",
                Capital = "0 SEK",
                Authority = "NONE"
            });
        }
        finally
        {
            foreach (var (path, checksum) in prior) Assert.Equal(checksum, HashFile(Path.Combine(artifacts, path)));
            Record(new { PriorAEvidenceUnchanged = true, PriorFiles = prior.Count, ActualRuntimeStarts = starts });
        }

        async Task<FiniteResearchIteration> Invoke(EodhdMarketMemory owner, int number)
        {
            var cutoff = clock.GetUtcNow();
            var input = owner.PreviewFiniteResearchInput(session.SessionId, number, cutoff);
            var inputHash = Convert.ToHexStringLower(SHA256.HashData(LocalReasonerProtocol.Request(input)));
            if (number == 2)
            {
                Assert.NotNull(input.FiniteHistory);
                Assert.Equal(LearningAdmissionReason.Admitted, input.FiniteHistory.PreviousOutcome);
                Assert.Equal(0, input.FiniteHistory.RemainingScientificEvaluations);
            }
            await using var runtime = new LocalReasonerRuntime(new()
            {
                Enabled = true,
                ControlledRealModelAcceptance = true,
                InvocationTimeout = TimeSpan.FromSeconds(180),
                WorkerExecutable = Path.Combine(root, "native-worker"),
                WorkerArguments = [Path.Combine(artifacts, "runtime"), model,
                    File.ReadAllText(Path.Combine(artifacts, "cgroup-path.private")).Trim()],
                CoordinationDirectory = root
            }, item =>
            {
                if (item.Phase == LocalReasonerAuditPhase.Started) { starts++; Assert.InRange(starts, 1, 2); }
                Assert.Equal(inputHash, item.InputHash);
                Record(new { Iteration = number, Runtime = item });
            });
            Record(new
            {
                Iteration = number,
                input.InputChecksum,
                input.HistoryChecksum,
                input.ProjectionVersion,
                HasFiniteHistory = input.FiniteHistory is not null,
                CompletionBudgetMilliseconds = runtime.CompletionTimeout.TotalMilliseconds
            });
            IResearchReasoner port = runtime;
            var watch = Stopwatch.StartNew();
            var step = await owner.RunFiniteResearchIterationAsync(session.SessionId, number, input.InputChecksum,
                cutoff, clock, port.ReasonAsync, token, runtime.CompletionTimeout);
            var governance = owner.ReadLearningLedger();
            Record(new
            {
                Iteration = number,
                State = step.State.ToString(),
                Outcome = step.Outcome?.ToString(),
                Failure = step.Failure?.ToString(),
                WallMilliseconds = watch.ElapsedMilliseconds,
                step.ResponseChecksum,
                step.AuthorityChecksum,
                governance.EngineStarts,
                governance.Trials,
                governance.UniqueRuns
            });
            return step;
        }
    }

    private static string HashFile(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
}
