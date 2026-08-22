using System.Globalization;
using System.Text.Json;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

public sealed record AutonomousResearchRunRequest(string IdempotencyKey, int? MaximumExperiments = null);
public sealed record AutonomousResearchExperiment(string ExperimentId, string HypothesisId, string FamilyId,
    int FamilyAttemptCount, ResearchExperimentState State, ResearchExperimentVerdict Verdict, string? RejectionReason,
    ResearchComplexity Complexity, ResearchIntegrityVerdict Integrity, string RobustnessEvaluationId,
    decimal? TrainNetReturn, decimal? OutOfSampleNetReturn, decimal? OutOfSampleExpectancy,
    decimal? ProfitFactor, decimal? WinRate, decimal? MaxDrawdown, string CostModel,
    IReadOnlyList<string> MarketRevisionIds, string FeatureRevisionId, DateTimeOffset KnowledgeCutoffUtc,
    DateTimeOffset CreatedAtUtc);
public sealed record AutonomousResearchRun(string RunId, string IdempotencyKey, ResearchExperimentState State,
    int ExperimentCount, int RejectedCount, int ChallengerCount, DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc, IReadOnlyList<AutonomousResearchExperiment> Experiments);
public sealed record AutonomousResearchSnapshot(DateTimeOffset GeneratedAtUtc, string OperatingMode, decimal BudgetSek,
    string EngineVersion, string FeatureLibraryVersion, int TotalExperiments, int RejectedCount, int ChallengerCount,
    AutonomousResearchRun? LatestRun, IReadOnlyList<ResearchHypothesis> Hypotheses,
    IReadOnlyList<ResearchFeatureDefinition> Features, string Status, string ExecutionAuthority);

internal sealed partial class EodhdMarketMemory
{
    private static readonly JsonSerializerOptions ResearchJson = new(JsonSerializerDefaults.Web);

    private static void InitializeAutonomousResearchStorage(SqliteConnection c)
    {
        using var command = c.CreateCommand(); command.CommandText = """
          CREATE TABLE IF NOT EXISTS research_runs(run_id TEXT PRIMARY KEY,idempotency_key TEXT NOT NULL UNIQUE,state TEXT NOT NULL,created_utc TEXT NOT NULL,completed_utc TEXT,result_json TEXT);
          CREATE TABLE IF NOT EXISTS research_hypotheses(hypothesis_id TEXT PRIMARY KEY,fingerprint TEXT NOT NULL UNIQUE,family_id TEXT NOT NULL,hypothesis_json TEXT NOT NULL,created_utc TEXT NOT NULL);
          CREATE TABLE IF NOT EXISTS research_experiments(experiment_id TEXT PRIMARY KEY,hypothesis_id TEXT NOT NULL,family_id TEXT NOT NULL,robustness_evaluation_id TEXT NOT NULL,state TEXT NOT NULL,verdict TEXT NOT NULL,result_json TEXT NOT NULL,created_utc TEXT NOT NULL,UNIQUE(hypothesis_id,robustness_evaluation_id));
          CREATE INDEX IF NOT EXISTS ix_research_family ON research_experiments(family_id,created_utc,experiment_id);
          """; command.ExecuteNonQuery();
        Execute(c, null, "UPDATE research_runs SET state='Failed',completed_utc=COALESCE(completed_utc,$now) WHERE state IN ('Pending','Running')", ("$now", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)));
    }

    internal AutonomousResearchRun RunAutonomousResearch(string idempotencyKey, int maximumExperiments)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128) throw new ArgumentException("A bounded idempotency key is required.");
        maximumExperiments = Math.Clamp(maximumExperiments, 1, FinanceResearchContracts.MaximumTotalExperimentsPerRun);
        using var c = new SqliteConnection(ConnectionString); c.Open();
        var existing = ResearchScalar(c, "SELECT result_json FROM research_runs WHERE idempotency_key=$key", ("$key", idempotencyKey));
        if (existing is not null) return JsonSerializer.Deserialize<AutonomousResearchRun>(existing, ResearchJson)!;
        var runId = "research-run-" + FinanceResearchContracts.Fingerprint(new { idempotencyKey, FinanceResearchContracts.EngineVersion })[7..23];
        var created = DateTimeOffset.UtcNow;
        try { Execute(c, null, "INSERT INTO research_runs VALUES($id,$key,'Running',$at,NULL,NULL)", ("$id", runId), ("$key", idempotencyKey), ("$at", created.ToString("O"))); }
        catch (SqliteException) { var raced = ResearchScalar(c, "SELECT result_json FROM research_runs WHERE idempotency_key=$key", ("$key", idempotencyKey)); if (raced is not null) return JsonSerializer.Deserialize<AutonomousResearchRun>(raced, ResearchJson)!; throw new InvalidOperationException("An identical research run is already in progress."); }

        try
        {
            BuildRobustnessEvaluations();
            var evaluations = ReadResearchEvaluations(c).Where(x => x.Plan.Strategy.Id != "buy-and-hold").Take(maximumExperiments).ToArray();
            var experiments = new List<AutonomousResearchExperiment>();
            foreach (var evidence in evaluations)
            {
                var featureIds = evidence.Plan.Strategy.Id == "momentum" ? new[] { "momentum.20.sign" } : new[] { "trend.sma.fast-slow-relation" };
                foreach (var id in featureIds) _ = FinanceResearchFeatureLibrary.Require(id);
                var familyId = "family-" + evidence.Plan.Strategy.Id + "-v1";
                var hypothesisSeed = new { engine = FinanceResearchContracts.EngineVersion, familyId, featureIds, evidence.Plan.FeatureRevisionId, evidence.Plan.MarketRevisionIds, horizon = 5 };
                var fingerprint = FinanceResearchContracts.Fingerprint(hypothesisSeed);
                var hypothesis = new ResearchHypothesis("hypothesis-" + fingerprint[7..23], "v1", FinanceResearchContracts.EngineVersion,
                    "bounded-existing-strategy-evidence", evidence.Plan.Strategy.Id == "momentum" ? "Known momentum may have different held-out expectancy than its reference benchmark." : "Known moving-average relation may have different held-out expectancy than its reference benchmark.",
                    featureIds, "next-session portfolio expectancy", 5, evidence.Plan.Universe, evidence.Plan.MarketRevisionIds,
                    evidence.Plan.FeatureRevisionId, ResearchKnowledgeCutoff(c, evidence.Plan.FeatureRevisionId), familyId, fingerprint);
                Execute(c, null, "INSERT OR IGNORE INTO research_hypotheses VALUES($id,$fingerprint,$family,$json,$at)", ("$id", hypothesis.HypothesisId), ("$fingerprint", fingerprint), ("$family", familyId), ("$json", JsonSerializer.Serialize(hypothesis, ResearchJson)), ("$at", created.ToString("O")));
                var variants = Math.Max(1, evidence.ParameterVariantsEvaluated);
                var priorFamilyEvaluations = Convert.ToInt32(ResearchScalar(c, "SELECT CAST(COUNT(*) AS TEXT) FROM research_experiments WHERE family_id=$family", ("$family", familyId)) ?? "0", CultureInfo.InvariantCulture);
                var attempts = priorFamilyEvaluations * variants + variants;
                var complexity = FinanceResearchContracts.Complexity(featureIds.Length, 1, evidence.Plan.ReferenceParameters.Count, variants);
                var integrity = FinanceResearchContracts.Assess(evidence, attempts, complexity, true, evidence.CostSensitivity.Points.Count > 0);
                var challenger = integrity.State == ResearchIntegrityState.Pass;
                var verdict = challenger ? ResearchExperimentVerdict.Challenger : evidence.Verdict == RobustnessVerdict.InsufficientData ? ResearchExperimentVerdict.Inconclusive : ResearchExperimentVerdict.Rejected;
                var experimentId = "experiment-" + FinanceResearchContracts.Fingerprint(new { hypothesis.Fingerprint, evidence.EvaluationId, FinanceResearchContracts.EngineVersion })[7..23];
                var experiment = new AutonomousResearchExperiment(experimentId, hypothesis.HypothesisId, familyId, attempts,
                    ResearchExperimentState.Completed, verdict, challenger ? null : integrity.ReasonCode, complexity, integrity,
                    evidence.EvaluationId, evidence.PrimarySplit.Train.NetReturn, evidence.PrimarySplit.Test.NetReturn,
                    null, null, evidence.PrimarySplit.Test.WinningExits + evidence.PrimarySplit.Test.LosingExits == 0 ? null :
                        (decimal)evidence.PrimarySplit.Test.WinningExits / (evidence.PrimarySplit.Test.WinningExits + evidence.PrimarySplit.Test.LosingExits),
                    evidence.PrimarySplit.Test.MaxDrawdown,
                    "hypothetical-conservative-v1", evidence.Plan.MarketRevisionIds, evidence.Plan.FeatureRevisionId,
                    hypothesis.KnowledgeCutoffUtc, created);
                Execute(c, null, "INSERT OR IGNORE INTO research_experiments VALUES($id,$hypothesis,$family,$evaluation,'Completed',$verdict,$json,$at)", ("$id", experimentId), ("$hypothesis", hypothesis.HypothesisId), ("$family", familyId), ("$evaluation", evidence.EvaluationId), ("$verdict", verdict.ToString()), ("$json", JsonSerializer.Serialize(experiment, ResearchJson)), ("$at", created.ToString("O")));
                experiments.Add(ReadResearchExperiment(c, experimentId));
            }
            var completed = DateTimeOffset.UtcNow;
            var result = new AutonomousResearchRun(runId, idempotencyKey, ResearchExperimentState.Completed, experiments.Count,
                experiments.Count(x => x.Verdict is ResearchExperimentVerdict.Rejected or ResearchExperimentVerdict.Inconclusive or ResearchExperimentVerdict.NotEvaluable), experiments.Count(x => x.Verdict == ResearchExperimentVerdict.Challenger), created, completed, experiments);
            Execute(c, null, "UPDATE research_runs SET state='Completed',completed_utc=$done,result_json=$json WHERE run_id=$id", ("$done", completed.ToString("O")), ("$json", JsonSerializer.Serialize(result, ResearchJson)), ("$id", runId));
            return result;
        }
        catch
        {
            var failedAt=DateTimeOffset.UtcNow;var failed=new AutonomousResearchRun(runId,idempotencyKey,ResearchExperimentState.Failed,0,0,0,created,failedAt,[]);
            Execute(c, null, "UPDATE research_runs SET state='Failed',completed_utc=$done,result_json=$json WHERE run_id=$id", ("$done", failedAt.ToString("O")),("$json",JsonSerializer.Serialize(failed,ResearchJson)), ("$id", runId)); throw;
        }
    }

    internal AutonomousResearchSnapshot AutonomousResearchSnapshot()
    {
        using var c = new SqliteConnection(ConnectionString); c.Open();
        var experiments = ReadAllResearchExperiments(c); var hypotheses = ReadResearchHypotheses(c);
        var latestJson = ResearchScalar(c, "SELECT result_json FROM research_runs WHERE result_json IS NOT NULL ORDER BY created_utc DESC,run_id DESC LIMIT 1");
        var latest = latestJson is null ? null : JsonSerializer.Deserialize<AutonomousResearchRun>(latestJson, ResearchJson);
        return new(DateTimeOffset.UtcNow, "RESEARCH", 0m, FinanceResearchContracts.EngineVersion,
            FinanceResearchFeatureLibrary.Version, experiments.Count, experiments.Count(x => x.Verdict != ResearchExperimentVerdict.Challenger),
            experiments.Count(x => x.Verdict == ResearchExperimentVerdict.Challenger), latest, hypotheses,
            FinanceResearchFeatureLibrary.Definitions, experiments.Any(x => x.Verdict == ResearchExperimentVerdict.Challenger) ? "NEEDS_MORE_EVIDENCE" : "CONTINUE_RESEARCH", "NONE");
    }

    private static List<RobustnessEvaluationResult> ReadResearchEvaluations(SqliteConnection c) { using var x = c.CreateCommand(); x.CommandText = "SELECT result_json FROM robustness_evaluations ORDER BY strategy_id,evaluation_id"; using var r = x.ExecuteReader(); var rows = new List<RobustnessEvaluationResult>(); while (r.Read()) rows.Add(JsonSerializer.Deserialize<RobustnessEvaluationResult>(r.GetString(0), ResearchJson)!); return rows; }
    private static DateTimeOffset ResearchKnowledgeCutoff(SqliteConnection c, string featureRevision) => DateTimeOffset.Parse(ResearchScalar(c, "SELECT MAX(knowledge_utc) FROM feature_values WHERE revision_id=$id", ("$id", featureRevision)) ?? throw new InvalidOperationException("Feature knowledge cutoff is unavailable."), CultureInfo.InvariantCulture);
    private static AutonomousResearchExperiment ReadResearchExperiment(SqliteConnection c, string id) => JsonSerializer.Deserialize<AutonomousResearchExperiment>(ResearchScalar(c, "SELECT result_json FROM research_experiments WHERE experiment_id=$id", ("$id", id))!, ResearchJson)!;
    private static List<AutonomousResearchExperiment> ReadAllResearchExperiments(SqliteConnection c) { using var x = c.CreateCommand(); x.CommandText = "SELECT result_json FROM research_experiments ORDER BY created_utc DESC,experiment_id"; using var r = x.ExecuteReader(); var rows = new List<AutonomousResearchExperiment>(); while (r.Read()) rows.Add(JsonSerializer.Deserialize<AutonomousResearchExperiment>(r.GetString(0), ResearchJson)!); return rows; }
    private static List<ResearchHypothesis> ReadResearchHypotheses(SqliteConnection c) { using var x = c.CreateCommand(); x.CommandText = "SELECT hypothesis_json FROM research_hypotheses ORDER BY created_utc DESC,hypothesis_id"; using var r = x.ExecuteReader(); var rows = new List<ResearchHypothesis>(); while (r.Read()) rows.Add(JsonSerializer.Deserialize<ResearchHypothesis>(r.GetString(0), ResearchJson)!); return rows; }
    private static string? ResearchScalar(SqliteConnection c, string sql, params (string Name, object Value)[] args) { using var x = c.CreateCommand(); x.CommandText = sql; foreach (var arg in args) x.Parameters.AddWithValue(arg.Name, arg.Value); return x.ExecuteScalar() as string; }
}
