using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

public sealed record FinanceRobustnessBuildEvidence(string FeatureRevisionId,IReadOnlyList<string> MarketRevisionIds,
    IReadOnlyList<string> EvaluationIds,IReadOnlyList<string> Checksums,int UniqueBacktestRuns,int EvaluationWindows,
    int ParameterVariants,int CostVariants,long ElapsedMilliseconds,bool Idempotent);
public sealed record FinanceRobustnessSummary(string EvaluationId,string Checksum,string PlanId,string PlanVersion,
    string StrategyId,string StrategyVersion,RobustnessVerdict Verdict,decimal Score,RobustnessEvidenceLabel EvidenceLabel,
    int TrainSessions,int TestSessions,int EmbargoSessions,int WalkForwardWindows,int ParameterVariants,int CostVariants,
    string FeatureRevisionId,IReadOnlyList<string> MarketRevisionIds,IReadOnlyList<string> Limitations,
    SelectionGovernanceOutcome? SelectionOutcome,HoldoutEvidenceState? HoldoutState,int SelectionCandidates);
public sealed record FinanceRobustnessCatalog(DateTimeOffset GeneratedAtUtc,string OperatingMode,
    IReadOnlyList<EvaluationPlan> Plans,IReadOnlyList<FinanceRobustnessSummary> Evaluations);

internal sealed partial class EodhdMarketMemory
{
    private static readonly JsonSerializerOptions EvaluationJson=new(JsonSerializerDefaults.Web);
    private static void InitializeRobustnessStorage(SqliteConnection connection)
    {
        using var command=connection.CreateCommand();command.CommandText="""
            CREATE TABLE IF NOT EXISTS robustness_evaluations(
              evaluation_id TEXT PRIMARY KEY,checksum TEXT NOT NULL,plan_id TEXT NOT NULL,plan_version TEXT NOT NULL,
              strategy_id TEXT NOT NULL,strategy_version TEXT NOT NULL,feature_revision_id TEXT NOT NULL,
              market_revisions_json TEXT NOT NULL,verdict TEXT NOT NULL,score TEXT NOT NULL,result_json TEXT NOT NULL,created_utc TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS robustness_run_references(evaluation_id TEXT NOT NULL,run_id TEXT NOT NULL,PRIMARY KEY(evaluation_id,run_id));
            CREATE TABLE IF NOT EXISTS robustness_windows(evaluation_id TEXT NOT NULL,window_id TEXT NOT NULL,window_json TEXT NOT NULL,PRIMARY KEY(evaluation_id,window_id));
            CREATE TABLE IF NOT EXISTS robustness_parameter_sensitivity(evaluation_id TEXT NOT NULL,ordinal INTEGER NOT NULL,point_json TEXT NOT NULL,PRIMARY KEY(evaluation_id,ordinal));
            CREATE TABLE IF NOT EXISTS robustness_cost_sensitivity(evaluation_id TEXT NOT NULL,ordinal INTEGER NOT NULL,point_json TEXT NOT NULL,PRIMARY KEY(evaluation_id,ordinal));
            CREATE INDEX IF NOT EXISTS ix_robustness_read ON robustness_evaluations(strategy_id,created_utc,evaluation_id);
            CREATE TABLE IF NOT EXISTS robustness_deletion_receipts(receipt_id TEXT PRIMARY KEY,evaluations INTEGER NOT NULL,windows INTEGER NOT NULL,parameter_points INTEGER NOT NULL,cost_points INTEGER NOT NULL,run_references INTEGER NOT NULL);
            """;command.ExecuteNonQuery();
    }

    internal FinanceRobustnessBuildEvidence BuildRobustnessEvaluations()
    {
        var watch=Stopwatch.StartNew();using var connection=new SqliteConnection(ConnectionString);connection.Open();
        var featureRevision=LatestFeatureRevisionForEvaluation(connection)??throw new InvalidOperationException("No immutable feature revision is available.");
        var marketRevisions=JsonSerializer.Deserialize<string[]>(EvaluationScalar(connection,"SELECT source_revisions_json FROM feature_revisions WHERE revision_id=$id",("$id",featureRevision)))??[];
        var market=ReadEvaluationMarket(connection,marketRevisions);var features=ReadEvaluationFeatures(connection,featureRevision);
        if(market.Count==0)throw new InvalidOperationException("No exact market observations are available for robustness evaluation.");
        var universe=market.Select(x=>x.InstrumentId.Value).Distinct().Order(StringComparer.Ordinal).ToArray();var from=market.Min(x=>x.SessionDate);var to=market.Max(x=>x.SessionDate);
        var builds=new List<RobustnessEvaluationBuild>();var anyNew=false;
        foreach(var strategy in new IResearchBacktestStrategy[]{new BuyAndHoldResearchStrategy(),new SmaCrossoverResearchStrategy(),new MomentumResearchStrategy()})
        {
            var existing=ReadEvaluations(connection).FirstOrDefault(x=>x.Plan.Version==DeterministicRobustnessEvaluator.PlanVersion&&
                x.Plan.Strategy==strategy.Identity&&x.Plan.FeatureRevisionId==featureRevision&&
                x.Plan.MarketRevisionIds.Order(StringComparer.Ordinal).SequenceEqual(marketRevisions.Order(StringComparer.Ordinal),StringComparer.Ordinal)&&
                x.Plan.From==from&&x.Plan.To==to);
            if(existing is not null){builds.Add(new(existing,[]));continue;}
            var priorHoldoutEvaluations=ReadEvaluations(connection).Any(x=>x.Plan.Strategy==strategy.Identity&&x.Plan.FeatureRevisionId==featureRevision&&
                x.Plan.MarketRevisionIds.Order(StringComparer.Ordinal).SequenceEqual(marketRevisions.Order(StringComparer.Ordinal),StringComparer.Ordinal)&&
                x.Plan.From==from&&x.Plan.To==to&&x.SelectionGovernance?.FinalHoldoutState==HoldoutEvidenceState.Evaluated)?1:0;
            var plan=DeterministicRobustnessEvaluator.CreatePlan(marketRevisions,featureRevision,strategy,universe,from,to,priorHoldoutEvaluations:priorHoldoutEvaluations);
            var build=DeterministicRobustnessEvaluator.Evaluate(plan,strategy,market,features);
            foreach(var run in build.UnderlyingRuns)PersistBacktest(connection,run);
            anyNew|=PersistEvaluation(connection,build.Evaluation);builds.Add(build);
        }
        watch.Stop();var allRuns=builds.SelectMany(x=>x.Evaluation.UnderlyingRunIds).Distinct(StringComparer.Ordinal).Count();
        return new(featureRevision,marketRevisions,builds.Select(x=>x.Evaluation.EvaluationId).ToArray(),builds.Select(x=>x.Evaluation.Checksum).ToArray(),allRuns,
            builds.Sum(x=>x.Evaluation.WalkForwardWindows.Count),builds.Sum(x=>x.Evaluation.ParameterVariantsEvaluated),builds.Sum(x=>x.Evaluation.CostSensitivity.Points.Count),watch.ElapsedMilliseconds,!anyNew);
    }

    internal FinanceRobustnessCatalog RobustnessCatalog()
    {
        using var connection=new SqliteConnection(ConnectionString);connection.Open();
        var evaluations=ReadEvaluations(connection).GroupBy(x=>x.Plan.Strategy.Id,StringComparer.Ordinal).Select(x=>x.First()).ToArray();
        return new(DateTimeOffset.UtcNow,"RESEARCH",evaluations.Select(x=>x.Plan).ToArray(),evaluations.Select(Summary).ToArray());
    }
    internal RobustnessEvaluationResult? RobustnessEvaluation(string id)
    {
        using var connection=new SqliteConnection(ConnectionString);connection.Open();var json=EvaluationScalarOrNull(connection,"SELECT result_json FROM robustness_evaluations WHERE evaluation_id=$id",("$id",id));
        return json is null?null:JsonSerializer.Deserialize<RobustnessEvaluationResult>(json,EvaluationJson);
    }
    private static bool PersistEvaluation(SqliteConnection connection,RobustnessEvaluationResult value)
    {
        var existing=EvaluationScalarOrNull(connection,"SELECT checksum FROM robustness_evaluations WHERE evaluation_id=$id",("$id",value.EvaluationId));
        if(existing is not null){if(existing!=value.Checksum)throw new InvalidOperationException("Immutable robustness evaluation identity conflict.");return false;}
        using var transaction=connection.BeginTransaction();Execute(connection,transaction,"INSERT INTO robustness_evaluations VALUES($id,$checksum,$plan,$planVersion,$strategy,$strategyVersion,$feature,$markets,$verdict,$score,$json,$created)",
            ("$id",value.EvaluationId),("$checksum",value.Checksum),("$plan",value.Plan.Id),("$planVersion",value.Plan.Version),("$strategy",value.Plan.Strategy.Id),("$strategyVersion",value.Plan.Strategy.Version),("$feature",value.Plan.FeatureRevisionId),("$markets",JsonSerializer.Serialize(value.Plan.MarketRevisionIds)),("$verdict",value.Verdict.ToString()),("$score",value.Score.Total.ToString(CultureInfo.InvariantCulture)),("$json",JsonSerializer.Serialize(value,EvaluationJson)),("$created",DateTimeOffset.UtcNow.ToString("O")));
        foreach(var run in value.UnderlyingRunIds)Execute(connection,transaction,"INSERT INTO robustness_run_references VALUES($evaluation,$run)",( "$evaluation",value.EvaluationId),("$run",run));
        foreach(var window in value.WalkForwardWindows)Execute(connection,transaction,"INSERT INTO robustness_windows VALUES($evaluation,$id,$json)",( "$evaluation",value.EvaluationId),("$id",window.Id),("$json",JsonSerializer.Serialize(window,EvaluationJson)));
        for(var i=0;i<value.ParameterSensitivity.Points.Count;i++)Execute(connection,transaction,"INSERT INTO robustness_parameter_sensitivity VALUES($evaluation,$ordinal,$json)",( "$evaluation",value.EvaluationId),("$ordinal",i),("$json",JsonSerializer.Serialize(value.ParameterSensitivity.Points[i],EvaluationJson)));
        for(var i=0;i<value.CostSensitivity.Points.Count;i++)Execute(connection,transaction,"INSERT INTO robustness_cost_sensitivity VALUES($evaluation,$ordinal,$json)",( "$evaluation",value.EvaluationId),("$ordinal",i),("$json",JsonSerializer.Serialize(value.CostSensitivity.Points[i],EvaluationJson)));
        transaction.Commit();return true;
    }
    private static List<RobustnessEvaluationResult> ReadEvaluations(SqliteConnection connection){using var command=connection.CreateCommand();command.CommandText="SELECT result_json FROM robustness_evaluations ORDER BY strategy_id,created_utc DESC,evaluation_id DESC";using var reader=command.ExecuteReader();var values=new List<RobustnessEvaluationResult>();while(reader.Read())values.Add(JsonSerializer.Deserialize<RobustnessEvaluationResult>(reader.GetString(0),EvaluationJson)!);return values;}
    private static FinanceRobustnessSummary Summary(RobustnessEvaluationResult x)=>new(x.EvaluationId,x.Checksum,x.Plan.Id,x.Plan.Version,x.Plan.Strategy.Id,x.Plan.Strategy.Version,x.Verdict,x.Score.Total,x.Score.Label,x.TrainSessions,x.TestSessions,x.Plan.EmbargoSessions,x.WalkForwardWindows.Count,x.ParameterVariantsEvaluated,x.CostSensitivity.Points.Count,x.Plan.FeatureRevisionId,x.Plan.MarketRevisionIds,x.Limitations,x.SelectionGovernance?.Outcome,x.SelectionGovernance?.FinalHoldoutState,x.SelectionGovernance?.CandidateCount??0);
    private static string? LatestFeatureRevisionForEvaluation(SqliteConnection connection)=>EvaluationScalarOrNull(connection,"SELECT revision_id FROM feature_revisions ORDER BY created_utc DESC,revision_id DESC LIMIT 1");
    private static string EvaluationScalar(SqliteConnection c,string sql,params(string Name,object Value)[] args)=>EvaluationScalarOrNull(c,sql,args)??throw new InvalidOperationException("Required evaluation lineage is unavailable.");
    private static string? EvaluationScalarOrNull(SqliteConnection c,string sql,params(string Name,object Value)[] args){using var command=c.CreateCommand();command.CommandText=sql;foreach(var x in args)command.Parameters.AddWithValue(x.Name,x.Value);return command.ExecuteScalar() as string;}
    private static List<BacktestMarketBar> ReadEvaluationMarket(SqliteConnection c,IReadOnlyList<string> revisions){var rows=new List<BacktestMarketBar>();using var command=c.CreateCommand();command.CommandText="SELECT instrument_id,revision_id,session_date,open,close,acquired_utc,volume FROM observations ORDER BY session_date,instrument_id";using var reader=command.ExecuteReader();while(reader.Read())if(revisions.Contains(reader.GetString(1),StringComparer.Ordinal))rows.Add(new(new(reader.GetString(0)),reader.GetString(1),DateOnly.Parse(reader.GetString(2),CultureInfo.InvariantCulture),decimal.Parse(reader.GetString(3),CultureInfo.InvariantCulture),decimal.Parse(reader.GetString(4),CultureInfo.InvariantCulture),DateTimeOffset.Parse(reader.GetString(5),CultureInfo.InvariantCulture),reader.GetInt64(6)));return rows;}
    private static List<BacktestFeatureValue> ReadEvaluationFeatures(SqliteConnection c,string revision){var rows=new List<BacktestFeatureValue>();using var command=c.CreateCommand();command.CommandText="SELECT instrument_id,session_date,definition_id,value,knowledge_utc FROM feature_values WHERE revision_id=$id ORDER BY session_date,instrument_id,definition_id";command.Parameters.AddWithValue("$id",revision);using var reader=command.ExecuteReader();while(reader.Read())rows.Add(new(new(reader.GetString(0)),DateOnly.Parse(reader.GetString(1),CultureInfo.InvariantCulture),reader.GetString(2),reader.IsDBNull(3)?null:decimal.Parse(reader.GetString(3),CultureInfo.InvariantCulture),DateTimeOffset.Parse(reader.GetString(4),CultureInfo.InvariantCulture),revision));return rows;}
}

public interface IFinanceRobustnessReader{FinanceRobustnessCatalog GetCatalog();RobustnessEvaluationResult? GetEvaluation(string id);}
internal sealed class EodhdFinanceRobustnessReader(EodhdMarketMemory memory):IFinanceRobustnessReader{public FinanceRobustnessCatalog GetCatalog()=>memory.RobustnessCatalog();public RobustnessEvaluationResult? GetEvaluation(string id)=>memory.RobustnessEvaluation(id);}
internal sealed class FinanceRobustnessBuildWorker(EodhdFinanceOptions options,EodhdMarketMemory memory,BigBrain.Api.SystemRecovery.SystemRecoveryCoordinator recovery):BackgroundService
{protected override async Task ExecuteAsync(CancellationToken token){await recovery.WaitUntilRecoveredAsync(token);if(!options.Enabled||!options.AccountActive)return;await Task.Delay(TimeSpan.FromSeconds(24),token);try{memory.BuildRobustnessEvaluations();}catch(InvalidOperationException){}}}
