using System.Collections.Immutable;
using System.Text.Json;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Tests;

// No production reasoner, orchestration, journal, DI, endpoints or schema. All data are synthetic.
internal static class ResearchLearningFixture
{
    private static readonly int[] Periods = [5, 10, 20];
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    internal static ResearchDatasetFacts Facts => new(ResearchDatasetClass.DailyOhlcv,
        DatasetOwnerRightsDecision.ApprovedByOwner, "synthetic-fixture-owner-evidence-v1",
        DatasetEvidenceResult.Pass, DatasetEvidenceResult.Pass, DatasetEvidenceResult.Pass,
        DatasetEvidenceResult.Pass, DatasetEvidenceResult.Pass, []);
    internal static SyntheticLearningScope Scope(int count = 400)
    {
        var dates = Enumerable.Range(0, count * 2).Select(i => new DateOnly(2024, 1, 2).AddDays(i))
            .Where(UsMarketCalendar.IsSession).Take(count).ToArray();
        var bars = dates.Select((date, i) => new BacktestMarketBar(new("US:XNAS:SYNTHETIC"), "synthetic-market-v1",
            date, 100m + i / 10m + (i % 31 - 15) / 5m, 100m + i / 10m + (i % 31 - 15) / 5m,
            new DateTimeOffset(date.ToDateTime(new TimeOnly(22, 0)), TimeSpan.Zero), 10000)).ToArray();
        // Exact existing momentum definition: close[t] - close[t-period], null during warmup.
        var features = bars.SelectMany((bar, i) => Periods.Select(period =>
            new BacktestFeatureValue(bar.InstrumentId, bar.SessionDate, $"momentum.{period}",
                i < period ? null : bar.Close - bars[i - period].Close, bar.KnowledgeTimeUtc, "synthetic-feature-v1"))).ToArray();
        var plan = DeterministicRobustnessEvaluator.CreatePlan(["synthetic-market-v1"], "synthetic-feature-v1",
            new MomentumResearchStrategy(), ["US:XNAS:SYNTHETIC"], dates[0], dates[^1]);
        return SyntheticLearningScope.Create(plan, bars, features, Facts, ResearchDatasetPurpose.TrainValidationHoldout,
            LearningIdentity.Hash(Array.Empty<string>()));
    }
    internal static string Proposal(SyntheticLearningScope scope, string rationale = "Engineering-only question, not market evidence.") =>
        JsonSerializer.Serialize(new
        {
            version = LearningAdmissionPolicy.Version,
            discriminator = "Proposal",
            inputChecksum = scope.Input.InputChecksum,
            scopeHandle = SyntheticLearningScope.Handle,
            targetId = scope.Input.TargetId,
            question = "Does the synthetic reference fail the fixed validation-excess criterion?",
            rationale,
            parentHypothesisRef = (string?)null,
            variants = new[] { new { strategy = new { id = "momentum", version = "v1" },
                parameters = new { period = 20m }, bindingHandle = SyntheticLearningScope.Handle } },
            falsificationCriteria = new[] { new { metric = "validation.excessReturn", phase = "validation",
                comparison = "LessThanOrEqual", threshold = 0m, unit = "fraction", samplePolicy = AntiOverfittingPolicy.Default.Version } }
        });
    internal static string NoUseful(SyntheticLearningScope scope) => JsonSerializer.Serialize(new
    {
        version = LearningAdmissionPolicy.Version,
        discriminator = "NoUsefulProposal",
        inputChecksum = scope.Input.InputChecksum,
        scopeHandle = SyntheticLearningScope.Handle,
        reasonCode = "NoSupportedQuestion",
        explanation = "No supported synthetic question."
    });
    internal static LearningAdmissionState Fresh => new(0, 0, 0, LearningExposure.Unexposed);

    internal sealed class FakeReasoner(Func<LearningDevelopmentInput, string> respond)
    {
        internal int Calls { get; private set; }
        internal string ModelLabel { get; init; } = "test-only-fixture";
        internal string RequestId { get; init; } = "request-1";
        internal DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UnixEpoch;
        internal string Propose(LearningDevelopmentInput input) { Calls++; return respond(input); }
    }

    internal sealed record Attempt(string ResponseChecksum, LearningAdmissionReason? Admission, string? Failure);
    internal sealed record Commitment(string Version, string ProposalId, string ExecutionFingerprint, string InputChecksum,
        LearningProposalDraft Draft, EvaluationPlan Plan, LearningAdmissionState ReservedState);
    internal sealed record Manifest(string Version, string ProposalId, string ExecutionFingerprint, string InputChecksum,
        LearningProposalDraft Draft, EvaluationPlan Plan, LearningResultReference Result);

    // One ledger instance owns the entire fixture cohort regardless of proposal/revision/model naming.
    // It is deliberately NOT production cross-cohort exposure accounting or crash-safe admission storage.
    internal sealed class Protocol
    {
        private readonly Lock _gate = new();
        internal Action<string>? PersistCommitment { get; init; }
        internal string? CommitmentJson { get; private set; }
        private Manifest? _reloaded;
        internal int Invocations { get; private set; }
        internal int Submitted { get; private set; }
        internal int Trials { get; private set; }
        internal int EngineEvaluations { get; private set; }
        internal int UniqueBacktestRuns { get; private set; }
        internal int ReusedResults { get; private set; }
        internal bool RequireRiskEvidence { get; init; }
        internal FinanceRiskVerdict? MockedRisk { get; init; }
        internal LearningExposure Exposure { get; set; } = LearningExposure.Unexposed;
        internal List<Attempt> History { get; } = [];
        internal CommittedLearningProposal? Committed { get; private set; }
        internal RobustnessEvaluationBuild? Build { get; private set; }
        internal LearningAdmission? Invoke(FakeReasoner reasoner, SyntheticLearningScope scope)
        {
            lock (_gate)
            {
                if (Invocations != 0 || Submitted != 0 || Trials != 0 || EngineEvaluations != 0)
                { History.Add(new("", LearningAdmissionReason.BudgetExceeded, null)); return new(LearningAdmissionReason.BudgetExceeded); }
                if (Exposure != LearningExposure.Unexposed)
                {
                    var reason = Exposure == LearningExposure.Consumed ? LearningAdmissionReason.ExposureConsumed : LearningAdmissionReason.ExposureUnknown;
                    History.Add(new("", reason, null));
                    return new(reason); // Never send a false initial-history projection to the reasoner.
                }
                Invocations++;
                string response;
                try { response = reasoner.Propose(scope.Input); }
                catch (Exception error) when (error is TimeoutException or InvalidOperationException)
                { History.Add(new("", null, error is TimeoutException ? "reasoner.timeout" : "reasoner.unavailable")); return null; }
                return Submit(response, scope); // Engine/persistence failures are not mislabeled as reasoner failures.
            }
        }
        internal LearningAdmission Submit(string response, SyntheticLearningScope scope)
        {
            lock (_gate)
            {
                var admission = LearningAdmissionPolicy.Admit(response, scope,
                    new(Submitted, Trials, EngineEvaluations, Exposure, Committed?.ExecutionFingerprint ?? _reloaded?.ExecutionFingerprint, Committed?.ProposalId ?? _reloaded?.ProposalId));
                Submitted++;
                // No current prospective risk evidence can be manufactured by a historical synthetic proposal.
                // The fixture accepts no ALLOW; native veto/missing outcomes stop the same execution path.
                string? riskFailure = null;
                if (admission.Proposal is not null && RequireRiskEvidence)
                {
                    riskFailure = MockedRisk switch
                    {
                        FinanceRiskVerdict.Deny => "risk.deny",
                        FinanceRiskVerdict.Halt => "risk.halt",
                        FinanceRiskVerdict.InsufficientData => "risk.insufficient-data",
                        null => "risk.missing",
                        _ => "risk.unapproved"
                    };
                    admission = new(LearningAdmissionReason.RiskVeto);
                }
                History.Add(new(LearningIdentity.HashBytes(System.Text.Encoding.UTF8.GetBytes(response)), admission.Reason, riskFailure));
                if (admission.Reason == LearningAdmissionReason.Duplicate) { if (Build is not null || _reloaded is not null) ReusedResults++; return admission; }
                if (admission.Proposal is not { } proposal) return admission;
                Committed = proposal;
                Trials += 3;
                Exposure = LearningExposure.Consumed; // Reserve before combined selection/holdout, never refund.
                CommitmentJson = JsonSerializer.Serialize(new Commitment(LearningAdmissionPolicy.Version, proposal.ProposalId,
                    proposal.ExecutionFingerprint, scope.Input.InputChecksum, proposal.Draft, scope.Plan,
                    new(Submitted, Trials, 1, Exposure, proposal.ExecutionFingerprint, proposal.ProposalId)), Json);
                PersistCommitment?.Invoke(CommitmentJson); // In the SQLite proof this writes a test-only file before any engine call.
                EngineEvaluations++;
                Build = DeterministicRobustnessEvaluator.Evaluate(proposal.Scope.Plan, new MomentumResearchStrategy(),
                    proposal.Scope.Bars, proposal.Scope.Features);
                UniqueBacktestRuns = Build.UnderlyingRuns.Count;
                return admission;
            }
        }
        internal static Protocol Reload(Manifest manifest, SyntheticLearningScope scope)
        {
            ValidateManifest(manifest, scope);
            return new()
            {
                _reloaded = manifest,
                Invocations = 1,
                Submitted = 1,
                Trials = 3,
                EngineEvaluations = 1,
                UniqueBacktestRuns = manifest.Result.Runs.Length,
                Exposure = LearningExposure.Consumed
            };
        }
        internal static void ValidateManifest(Manifest manifest, SyntheticLearningScope scope)
        {
            if (manifest.Version != LearningAdmissionPolicy.Version ||
                manifest.Draft.Variant.Strategy != scope.Plan.Strategy || manifest.Draft.Variant.Period != 20m ||
                manifest.Draft.Variant.BindingHandle != SyntheticLearningScope.Handle || manifest.Draft.Falsification != LearningAdmissionPolicy.Criterion ||
                string.IsNullOrWhiteSpace(manifest.Draft.Question) || manifest.Draft.Question.Length > 1024 ||
                string.IsNullOrWhiteSpace(manifest.Draft.Rationale) || manifest.Draft.Rationale.Length > 2048 || scope.ValidationReason != LearningAdmissionReason.Admitted ||
                manifest.ExecutionFingerprint != scope.ExecutionFingerprint || manifest.InputChecksum != scope.Input.InputChecksum ||
                LearningIdentity.Hash(manifest.Plan) != LearningIdentity.Hash(scope.Plan) ||
                manifest.ProposalId != LearningIdentity.Hash(new
                {
                    Version = manifest.Version,
                    manifest.InputChecksum,
                    manifest.ExecutionFingerprint,
                    Draft = manifest.Draft
                }))
                throw new InvalidOperationException("Frozen synthetic manifest mismatch.");
        }
        internal Manifest FreezeManifest()
        {
            var committed = Committed ?? throw new InvalidOperationException("No commitment.");
            var build = Build ?? throw new InvalidOperationException("No result.");
            var result = build.Evaluation;
            return new(LearningAdmissionPolicy.Version, committed.ProposalId, committed.ExecutionFingerprint,
                committed.Scope.Input.InputChecksum, committed.Draft, committed.Scope.Plan,
                new(result.EvaluationId, result.Checksum, result.Verdict, result.SelectionGovernance!.Outcome,
                    build.UnderlyingRuns.Select(x => new LearningRunReference(x.RunId, x.Checksum)).ToImmutableArray()));
        }
    }

    internal sealed class Database : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "bb131b-synthetic", Guid.NewGuid().ToString("N"));
        private readonly EodhdFinanceOptions _options;
        internal Database()
        {
            _options = new() { DatabasePath = Path.Combine(_root, "finance.db"), PayloadDirectory = Path.Combine(_root, "payloads") };
            _ = new EodhdMarketMemory(_options);
        }
        internal void SaveManifest(string name, string json)
        {
            if (System.Text.Encoding.UTF8.GetByteCount(json) > 65536) throw new InvalidOperationException("Test manifest too large.");
            File.WriteAllText(Path.Combine(_root, name), json);
        }
        internal string ReadManifest(string name) => File.ReadAllText(Path.Combine(_root, name));
        internal EodhdFinanceBacktestReader ReopenReader() => new(new EodhdMarketMemory(_options));
        internal SqliteConnection Open()
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _options.DatabasePath, Pooling = false }.ToString());
            connection.Open(); return connection;
        }
        public void Dispose() => Directory.Delete(_root, true);
    }
}
