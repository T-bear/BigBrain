using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BigBrain.Modules.Finance;

public enum ResearchFeatureCategory { Trend, Momentum, Volatility, Volume }
public enum ResearchExperimentVerdict { Rejected, Inconclusive, Promising, Challenger, NotEvaluable }
public enum ResearchExperimentState { Pending, Running, Completed, Failed }
public enum ResearchIntegrityState { Pass, Fail, NotEvaluable }

public sealed record ResearchParameterBound(string Name, decimal Minimum, decimal Maximum);
public sealed record ResearchFeatureDefinition(string Id, string Version, ResearchFeatureCategory Category,
    string Description, string SourceDefinitionId, IReadOnlyList<string> RequiredInputs,
    IReadOnlyList<ResearchParameterBound> ParameterBounds, int ComplexityCost,
    string KnowledgeTimeRequirement, int MinimumEvidenceSessions);

public static class FinanceResearchFeatureLibrary
{
    public const string Version = "finance-research-signals-v1";
    public static readonly IReadOnlyList<ResearchFeatureDefinition> Definitions =
    [
        new("trend.sma.fast-slow-relation", "v1", ResearchFeatureCategory.Trend,
            "Measures the relation between known allowlisted fast and slow moving averages; it is not a trading rule.", "sma.10+sma.20",
            ["sma.5", "sma.10", "sma.20", "sma.50"], [new("fastPeriod", 5m, 10m), new("slowPeriod", 20m, 50m)], 2,
            "All inputs must have knowledge_time <= decision_time and pinned revisions.", 60),
        new("momentum.20.sign", "v1", ResearchFeatureCategory.Momentum,
            "Measures the sign of known 20-session momentum; it is not a trading rule.", "momentum.20",
            ["momentum.20"], [new("threshold", -0.10m, 0.10m)], 1,
            "All inputs must have knowledge_time <= decision_time and pinned revisions.", 60),
        new("volatility.20.level", "v1", ResearchFeatureCategory.Volatility,
            "Measures known rolling 20-session volatility as research context.", "volatility.20",
            ["volatility.20"], [new("threshold", 0m, 0.20m)], 1,
            "All inputs must have knowledge_time <= decision_time and pinned revisions.", 60),
        new("volume.ratio20.level", "v1", ResearchFeatureCategory.Volume,
            "Measures known volume relative to its 20-session average.", "volume.ratio.20",
            ["volume.ratio.20"], [new("threshold", 0.5m, 3m)], 1,
            "All inputs must have knowledge_time <= decision_time and pinned revisions.", 60)
    ];

    public static ResearchFeatureDefinition Require(string id) => Definitions.SingleOrDefault(x => x.Id == id)
        ?? throw new ArgumentException("Unknown or non-allowlisted research feature.", nameof(id));
}

public sealed record ResearchHypothesis(string HypothesisId, string Version, string EngineVersion,
    string RationaleCode, string Explanation, IReadOnlyList<string> FeatureIds, string Target,
    int HorizonSessions, IReadOnlyList<string> Universe, IReadOnlyList<string> MarketRevisionIds,
    string FeatureRevisionId, DateTimeOffset KnowledgeCutoffUtc, string FamilyId, string Fingerprint);

public sealed record ResearchComplexity(string Version, int FeatureCount, int ConditionCount,
    int TunableParameterCount, int ParameterVariants, int Score, string Explanation);

public sealed record ResearchIntegrityCheck(string Id, ResearchIntegrityState State, string Evidence);
public sealed record ResearchIntegrityVerdict(string Version, ResearchIntegrityState State,
    IReadOnlyList<ResearchIntegrityCheck> Checks, string ReasonCode);

public static class FinanceResearchContracts
{
    private static readonly JsonSerializerOptions CanonicalJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public const string EngineVersion = "autonomous-research-v1";
    public const string IntegrityVersion = "research-integrity-v2";
    public const string ComplexityVersion = "research-complexity-v1";
    public const int MaximumHypothesesPerRun = 3;
    public const int MaximumChallengersPerHypothesis = 1;
    public const int MaximumFeaturesPerHypothesis = 2;
    public const int MaximumTotalExperimentsPerRun = 3;

    public static ResearchComplexity Complexity(int features, int conditions, int parameters, int variants)
    {
        if (features is < 1 or > MaximumFeaturesPerHypothesis || conditions < 1 || parameters < 0 || variants < 1)
            throw new ArgumentOutOfRangeException(nameof(features), "Research complexity inputs exceed the bounded v1 contract.");
        var score = features * 2 + conditions + parameters * 2 + Math.Max(0, variants - 1);
        return new(ComplexityVersion, features, conditions, parameters, variants, score,
            "score = 2*features + conditions + 2*tunableParameters + max(0, variants-1)");
    }

    public static string Fingerprint(object value)
    {
        var canonical = JsonSerializer.Serialize(value, CanonicalJson);
        return "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    public static ResearchIntegrityVerdict Assess(RobustnessEvaluationResult evidence, int familyAttempts,
        ResearchComplexity complexity, bool lineageComplete, bool costAssumptionPresent)
    {
        var checks = new List<ResearchIntegrityCheck>
        {
            new("sample-size", evidence.TrainSessions >= evidence.Plan.Thresholds.MinimumTrainSessions && evidence.TestSessions >= evidence.Plan.Thresholds.MinimumTestSessions ? ResearchIntegrityState.Pass : ResearchIntegrityState.Fail, $"train={evidence.TrainSessions};test={evidence.TestSessions}"),
            new("out-of-sample", evidence.PrimarySplit.Test.NetReturn > 0 && evidence.PrimarySplit.Test.ExcessReturn >= 0 ? ResearchIntegrityState.Pass : ResearchIntegrityState.Fail, $"net={evidence.PrimarySplit.Test.NetReturn};excess={evidence.PrimarySplit.Test.ExcessReturn}"),
            new("walk-forward", evidence.WalkForwardWindows.Count >= evidence.Plan.Thresholds.MinimumWalkForwardWindows ? ResearchIntegrityState.Pass : ResearchIntegrityState.Fail, $"windows={evidence.WalkForwardWindows.Count}"),
            new("costs", costAssumptionPresent && evidence.CostSensitivity.Points.Count > 1 ? ResearchIntegrityState.Pass : ResearchIntegrityState.Fail, costAssumptionPresent ? "versioned hypothetical cost ladder evaluated" : "cost assumption missing"),
            new("lineage", lineageComplete && evidence.Plan.MarketRevisionIds.Count > 0 && !string.IsNullOrWhiteSpace(evidence.Plan.FeatureRevisionId) ? ResearchIntegrityState.Pass : ResearchIntegrityState.Fail, lineageComplete ? "market and feature revisions pinned" : "lineage incomplete"),
            new("multiple-testing", familyAttempts > 0 ? ResearchIntegrityState.Pass : ResearchIntegrityState.Fail, $"familyAttempts={familyAttempts}; no independent-significance claim"),
            new("selection-governance", evidence.SelectionGovernance?.Outcome == SelectionGovernanceOutcome.Pass ? ResearchIntegrityState.Pass : ResearchIntegrityState.Fail,
                evidence.SelectionGovernance is null ? "anti-overfitting governance evidence missing" : $"outcome={evidence.SelectionGovernance.Outcome};candidates={evidence.SelectionGovernance.CandidateCount}"),
            new("holdout-freshness", evidence.SelectionGovernance?.HoldoutStateAtSelection == HoldoutEvidenceState.Untouched && evidence.SelectionGovernance.FinalHoldoutState == HoldoutEvidenceState.Evaluated ? ResearchIntegrityState.Pass : ResearchIntegrityState.Fail,
                evidence.SelectionGovernance is null ? "holdout lifecycle evidence missing" : $"atSelection={evidence.SelectionGovernance.HoldoutStateAtSelection};final={evidence.SelectionGovernance.FinalHoldoutState};prior={evidence.SelectionGovernance.PriorHoldoutEvaluations}"),
            new("complexity", complexity.Score <= 12 ? ResearchIntegrityState.Pass : ResearchIntegrityState.Fail, $"{complexity.Version};score={complexity.Score}"),
            new("robustness", evidence.Verdict == RobustnessVerdict.MoreRobust ? ResearchIntegrityState.Pass : ResearchIntegrityState.Fail, evidence.Verdict.ToString()),
            new("dsr", ResearchIntegrityState.NotEvaluable, "Return-series moments and selection-population assumptions are not retained by v1 evidence."),
            new("pbo-cscv", ResearchIntegrityState.NotEvaluable, "CSCV combinatorial partitions are not present in v1 evidence.")
        };
        var failed = checks.FirstOrDefault(x => x.State == ResearchIntegrityState.Fail);
        return new(IntegrityVersion, failed is null ? ResearchIntegrityState.Pass : ResearchIntegrityState.Fail,
            checks, failed?.Id is { } id ? $"integrity.{id}.failed" : "integrity.passed");
    }
}
