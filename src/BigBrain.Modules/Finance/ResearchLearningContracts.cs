using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BigBrain.Modules.Finance;

public enum LearningAdmissionReason
{
    Admitted, NoUsefulProposal, Duplicate, Malformed, UnsupportedContract, EvidenceMismatch,
    UnsupportedScope, UnsupportedStrategy, UnsupportedParameter, InvalidVariantCount,
    InvalidFalsification, InvalidParent, IneligibleDataset, InvalidFeatures, InvalidPlan,
    BudgetExceeded, ExposureUnknown, ExposureConsumed, RiskVeto
}
public enum LearningExposure { Unknown, Unexposed, Consumed }
public enum LearningComparison { LessThanOrEqual }
public sealed record LearningFalsification(string Metric, string Phase, LearningComparison Comparison,
    decimal Threshold, string Unit, string SamplePolicy)
{
    public bool? IsFalsified(decimal? validationExcessReturn) => this == LearningAdmissionPolicy.Criterion
        ? validationExcessReturn is { } value ? value <= Threshold : null
        : throw new InvalidOperationException("Unsupported criterion.");
}
public sealed record LearningVariant(StrategyIdentity Strategy, decimal Period, string BindingHandle);
public sealed record LearningProposalDraft(string Question, string Rationale, LearningVariant Variant,
    LearningFalsification Falsification);
public sealed record LearningProtocolLimits(int ReasonerInvocations, int SubmittedProposals, int CallerVariants,
    int Instruments, int EffectiveTrials, int Evaluations, int MaximumUnderlyingRuns, int Concurrency,
    int AutomaticRetries, int ReasonerDeadlineSeconds, int IterationCapSeconds);
public sealed record LearningHistorySummary(int SubmittedProposals, int AdmittedTrials, int Evaluations,
    int ReusedResults, LearningExposure Exposure);
public sealed record LearningDevelopmentInput(string Version, string ProjectionVersion, string ScopeHandle,
    string TargetId, DateTimeOffset KnowledgeCutoffUtc, int DevelopmentSessions, string DevelopmentChecksum,
    string AllowedStrategy, int AllowedPeriod, int EffectiveScientificTrials, string HistoryChecksum,
    string InputChecksum)
{
    public LearningProtocolLimits Limits { get; } = new(1, 1, 1, 1, 3, 1, 64, 1, 0, 30, 300);
    public LearningHistorySummary History { get; } = new(0, 0, 0, 0, LearningExposure.Unexposed);
}
// Finance-owned snapshot, never accepted from the reasoner. B supplies this from its test-only ledger.
public sealed record LearningAdmissionState(int SubmittedProposals, int AdmittedTrials, int Evaluations,
    LearningExposure Exposure, string? PreviousFingerprint = null, string? PreviousProposalId = null);
public sealed record LearningAdmission(LearningAdmissionReason Reason, CommittedLearningProposal? Proposal = null,
    string? ReusedProposalId = null);
public sealed record LearningRunReference(string RunId, string Checksum);
public sealed record LearningResultReference(string EvaluationId, string Checksum, RobustnessVerdict Verdict,
    SelectionGovernanceOutcome Selection, ImmutableArray<LearningRunReference> Runs)
{
    public string OperatingMode { get; } = "RESEARCH";
    public string ExecutionAuthority { get; } = "NONE";
    public bool EngineeringOnly { get; } = true;
}

public sealed class CommittedLearningProposal
{
    internal CommittedLearningProposal(LearningProposalDraft draft, SyntheticLearningScope scope, string responseChecksum)
    {
        Draft = draft;
        Scope = scope;
        ResponseChecksum = responseChecksum;
        ExecutionFingerprint = scope.ExecutionFingerprint;
        ProposalId = LearningIdentity.Hash(new
        {
            Version = LearningAdmissionPolicy.Version,
            scope.Input.InputChecksum,
            ExecutionFingerprint,
            Draft
        });
        Hypothesis = new(ProposalId, LearningAdmissionPolicy.Version, FinanceResearchContracts.EngineVersion,
            "synthetic-contract-proof", draft.Rationale, ImmutableArray.Create("momentum.5", "momentum.10", "momentum.20"),
            scope.Input.TargetId, 1, scope.Plan.Universe, scope.Plan.MarketRevisionIds, scope.Plan.FeatureRevisionId,
            scope.Input.KnowledgeCutoffUtc, "synthetic-momentum-family-v1", ExecutionFingerprint);
    }
    public LearningProposalDraft Draft { get; }
    public SyntheticLearningScope Scope { get; }
    public ResearchHypothesis Hypothesis { get; }
    public string ProposalId { get; }
    public string ResponseChecksum { get; }
    public string ExecutionFingerprint { get; }
}

// Explicitly synthetic, trusted Finance input; not a real-data admission API or a persistence store.
// Snapshots use immutable collections so a caller cannot mutate an admitted plan after commitment.
public sealed class SyntheticLearningScope
{
    public const string Handle = "synthetic-momentum20-v1";
    private static readonly ImmutableArray<int> EffectivePeriods = [5, 10, 20];
    private SyntheticLearningScope(EvaluationPlan plan, ImmutableArray<BacktestMarketBar> bars,
        ImmutableArray<BacktestFeatureValue> features, ResearchDatasetFacts facts,
        ResearchDatasetPurpose purpose, string historyChecksum)
    {
        Plan = plan with
        {
            MarketRevisionIds = plan.MarketRevisionIds.ToImmutableArray(),
            Universe = plan.Universe.ToImmutableArray(),
            CostModels = plan.CostModels.ToImmutableArray(),
            ReferenceParameters = plan.ReferenceParameters.ToImmutableDictionary(StringComparer.Ordinal)
        };
        Bars = bars;
        Features = features;
        Facts = facts with { Limitations = facts.Limitations.ToImmutableArray() };
        Purpose = purpose;
        ValidationReason = Validate();
        // B offers only the initial empty-history projection; a later history-aware projector is not implemented.
        if (historyChecksum != LearningIdentity.Hash(Array.Empty<string>())) ValidationReason = LearningAdmissionReason.UnsupportedScope;
        var development = ImmutableArray<BacktestMarketBar>.Empty;
        var developmentFeatures = ImmutableArray<BacktestFeatureValue>.Empty;
        if (ValidationReason == LearningAdmissionReason.Admitted)
        {
            // Resolve semantically equal representations to the engine's exact default policy snapshot.
            // Existing engine identities remain untouched; caller decimal scale cannot fork replay IDs.
            var resolved = DeterministicRobustnessEvaluator.CreatePlan(Plan.MarketRevisionIds, Plan.FeatureRevisionId,
                new MomentumResearchStrategy(), Plan.Universe, Plan.From, Plan.To);
            Plan = resolved with
            {
                CostModels = resolved.CostModels.ToImmutableArray(),
                ReferenceParameters = resolved.ReferenceParameters.ToImmutableDictionary(StringComparer.Ordinal)
            };
            var partition = DeterministicRobustnessEvaluator.CreatePartition(bars.Select(x => x.SessionDate).ToArray(), Plan.AntiOverfitting!);
            development = bars.Where(x => x.SessionDate <= partition.ValidationTo).ToImmutableArray();
            developmentFeatures = features.Where(x => x.SessionDate <= partition.ValidationTo).ToImmutableArray();
            // Conservative bound on ALL engine calls, including repeated identical runs, not just unique IDs.
            // Fixed momentum path: primary 4 + neighborhood 6 + selection/holdout 10 + cost diagnostics 10.
            var available = development.Length - Plan.WalkForward.InitialTrainSessions - Plan.EmbargoSessions - Plan.WalkForward.TestSessions;
            var windows = available < 0 ? 0 : 1 + available / Plan.WalkForward.StepSessions;
            MaximumEngineCalls = 30 + 4 * windows;
            if (MaximumEngineCalls > 64) ValidationReason = LearningAdmissionReason.BudgetExceeded;
        }
        Input = new(LearningAdmissionPolicy.Version, "development-only-v1", Handle, "synthetic-next-session-direction",
            development.Length == 0 ? DateTimeOffset.UnixEpoch : development.Max(x => x.KnowledgeTimeUtc),
            development.Length, LearningIdentity.Hash(new { Bars = development, Features = developmentFeatures }),
            "momentum/v1", 20, 3, historyChecksum, "");
        Input = Input with { InputChecksum = LearningIdentity.Hash(Input) };
        ExecutionFingerprint = LearningIdentity.Hash(new
        {
            Normalization = "learning-execution-v1",
            Plan,
            Bars,
            Features,
            Facts,
            Purpose,
            Fill = BacktestFillModel.NextSessionOpen,
            Falsification = LearningAdmissionPolicy.Criterion,
            EffectivePeriods,
            Calendar = UsMarketCalendar.Version,
            RiskPolicy = "synthetic-no-execution-v1"
        });
    }
    public EvaluationPlan Plan { get; }
    public ImmutableArray<BacktestMarketBar> Bars { get; }
    public ImmutableArray<BacktestFeatureValue> Features { get; }
    public ResearchDatasetFacts Facts { get; }
    public ResearchDatasetPurpose Purpose { get; }
    public LearningDevelopmentInput Input { get; }
    public string ExecutionFingerprint { get; }
    public int MaximumEngineCalls { get; }
    public LearningAdmissionReason ValidationReason { get; private set; }

    public static SyntheticLearningScope Create(EvaluationPlan plan, IEnumerable<BacktestMarketBar> bars,
        IEnumerable<BacktestFeatureValue> features, ResearchDatasetFacts facts,
        ResearchDatasetPurpose purpose, string historyChecksum) => new(plan,
            bars.Select(x => x with { Open = Normalize(x.Open), Close = Normalize(x.Close) })
                .OrderBy(x => x.SessionDate).ToImmutableArray(),
            features.Select(x => x with { Value = x.Value is { } value ? Normalize(value) : null })
                .OrderBy(x => x.SessionDate).ThenBy(x => x.DefinitionId, StringComparer.Ordinal).ToImmutableArray(),
            facts, purpose, historyChecksum);

    private static decimal Normalize(decimal value) => decimal.Parse(value.ToString("G29", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    private LearningAdmissionReason Validate()
    {
        if (Purpose != ResearchDatasetPurpose.TrainValidationHoldout ||
            !Enum.IsDefined(Facts.DatasetClass) || !Enum.IsDefined(Facts.OwnerDecision) ||
            Facts.OwnerDecision != DatasetOwnerRightsDecision.ApprovedByOwner || string.IsNullOrWhiteSpace(Facts.OwnerDecisionEvidence) ||
            Facts.ExternalRights != DatasetEvidenceResult.Pass || Facts.TechnicalIntegrity != DatasetEvidenceResult.Pass ||
            Facts.HistoricalIdentity != DatasetEvidenceResult.Pass || Facts.PriceBasis != DatasetEvidenceResult.Pass ||
            Facts.CorporateActions != DatasetEvidenceResult.Pass ||
            ResearchDatasetEligibilityPolicyV1.Evaluate(Facts).For(Purpose).State != ResearchEligibilityState.Eligible)
            return LearningAdmissionReason.IneligibleDataset;
        if (Plan.MarketRevisionIds.Count != 1 || Plan.Universe.Count != 1 ||
            !Plan.MarketRevisionIds[0].StartsWith("synthetic-", StringComparison.Ordinal) ||
            !Plan.FeatureRevisionId.StartsWith("synthetic-", StringComparison.Ordinal) || Bars.Length is < 300 or > 1000 ||
            Bars.Select(x => x.SessionDate).Distinct().Count() != Bars.Length ||
            Bars.Any(x => x.MarketRevisionId != Plan.MarketRevisionIds[0] || x.InstrumentId.Value != Plan.Universe[0] ||
                x.SessionDate < Plan.From || x.SessionDate > Plan.To || x.Open <= 0 || x.Close <= 0 ||
                x.KnowledgeTimeUtc.Offset != TimeSpan.Zero || DateOnly.FromDateTime(x.KnowledgeTimeUtc.UtcDateTime) != x.SessionDate))
            return LearningAdmissionReason.InvalidPlan;
        var expected = DeterministicRobustnessEvaluator.CreatePlan(Plan.MarketRevisionIds, Plan.FeatureRevisionId,
            new MomentumResearchStrategy(), Plan.Universe, Plan.From, Plan.To);
        if (LearningIdentity.Hash(Plan) != LearningIdentity.Hash(expected)) return LearningAdmissionReason.InvalidPlan;
        string[] periods = ["momentum.5", "momentum.10", "momentum.20"];
        if (Features.Length != Bars.Length * 3 || Features.GroupBy(x => (x.SessionDate, x.DefinitionId)).Any(x => x.Count() != 1) ||
            Features.Any(x => x.FeatureRevisionId != Plan.FeatureRevisionId || x.InstrumentId.Value != Plan.Universe[0] ||
                !periods.Contains(x.DefinitionId, StringComparer.Ordinal) ||
                !Bars.Any(b => b.SessionDate == x.SessionDate && b.KnowledgeTimeUtc == x.KnowledgeTimeUtc)))
            return LearningAdmissionReason.InvalidFeatures;
        foreach (var period in EffectivePeriods)
        {
            var rows = Features.Where(x => x.DefinitionId == $"momentum.{period}").ToArray();
            if (rows.Take(period).Any(x => x.Value is not null) || rows.Skip(period).Any(x => x.Value is null))
                return LearningAdmissionReason.InvalidFeatures;
        }
        return LearningAdmissionReason.Admitted;
    }
}

// New contract identity only. No existing Finance identity algorithm is changed.
// Ordinal property order, preserved array order, invariant minimal decimal representation, UTF-8.
public static class LearningIdentity
{
    public static string Hash<T>(T value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) Write(writer, document.RootElement);
        return HashBytes(stream.ToArray());
    }
    public static string HashBytes(ReadOnlySpan<byte> bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static void Write(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var p in value.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                { writer.WritePropertyName(p.Name); Write(writer, p.Value); }
                writer.WriteEndObject(); break;
            case JsonValueKind.Array:
                writer.WriteStartArray(); foreach (var item in value.EnumerateArray()) Write(writer, item);
                writer.WriteEndArray(); break;
            case JsonValueKind.Number:
                writer.WriteRawValue(value.GetDecimal().ToString("G29", CultureInfo.InvariantCulture)); break;
            default: value.WriteTo(writer); break;
        }
    }
}
