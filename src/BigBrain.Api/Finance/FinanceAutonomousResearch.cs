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
    DateTimeOffset CreatedAtUtc, string? RunId = null, int? AttemptCount = null,
    IReadOnlyList<string>? RunIds = null);
public sealed record AutonomousResearchRun(string RunId, string IdempotencyKey, ResearchExperimentState State,
    int ExperimentCount, int RejectedCount, int InconclusiveCount, int NotEvaluableCount, int PromisingCount,
    int ChallengerCount, DateTimeOffset CreatedAtUtc, DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<AutonomousResearchExperiment> Experiments, string? FailureReason = null, string RecoveryStatus = "NONE");
public sealed record AutonomousResearchRunSummary(string RunId, ResearchExperimentState State,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? CompletedAtUtc, int ExperimentCount, int RejectedCount,
    int InconclusiveCount, int NotEvaluableCount, int PromisingCount, int ChallengerCount,
    string? FailureReason, string RecoveryStatus);
public sealed record AutonomousResearchRunCatalog(DateTimeOffset GeneratedAtUtc, string OperatingMode,
    int Offset, int Limit, int Total, IReadOnlyList<AutonomousResearchRunSummary> Runs);
public sealed record AutonomousResearchExperimentCatalog(DateTimeOffset GeneratedAtUtc, string OperatingMode,
    int Offset, int Limit, int Total, IReadOnlyList<AutonomousResearchExperiment> Experiments);
public sealed record AutonomousResearchSnapshot(DateTimeOffset GeneratedAtUtc, string OperatingMode, decimal BudgetSek,
    string EngineVersion, string FeatureLibraryVersion, int TotalExperiments, int RejectedCount,
    int InconclusiveCount, int NotEvaluableCount, int PromisingCount, int ChallengerCount,
    AutonomousResearchRun? LatestRun, IReadOnlyList<ResearchHypothesis> Hypotheses,
    IReadOnlyList<ResearchFeatureDefinition> Features, string Status, string ExecutionAuthority);

internal sealed class AutonomousResearchBusyException(string currentRunId) : InvalidOperationException("Autonomous research is already running.")
{ internal string CurrentRunId { get; } = currentRunId; }

internal sealed partial class EodhdMarketMemory
{
    private static readonly JsonSerializerOptions ResearchJson = new(JsonSerializerDefaults.Web);
    internal Action<string>? AutonomousResearchTestHook { get; set; }

    private static void InitializeAutonomousResearchStorage(SqliteConnection c)
    {
        using(var command=c.CreateCommand()){command.CommandText="""
          CREATE TABLE IF NOT EXISTS research_runs(run_id TEXT PRIMARY KEY,idempotency_key TEXT NOT NULL UNIQUE,state TEXT NOT NULL,created_utc TEXT NOT NULL,completed_utc TEXT,result_json TEXT);
          CREATE TABLE IF NOT EXISTS research_hypotheses(hypothesis_id TEXT PRIMARY KEY,fingerprint TEXT NOT NULL UNIQUE,family_id TEXT NOT NULL,hypothesis_json TEXT NOT NULL,created_utc TEXT NOT NULL);
          CREATE TABLE IF NOT EXISTS research_experiments(experiment_id TEXT PRIMARY KEY,hypothesis_id TEXT NOT NULL,family_id TEXT NOT NULL,robustness_evaluation_id TEXT NOT NULL,state TEXT NOT NULL,verdict TEXT NOT NULL,result_json TEXT NOT NULL,created_utc TEXT NOT NULL,UNIQUE(hypothesis_id,robustness_evaluation_id));
          CREATE TABLE IF NOT EXISTS research_run_experiments(run_id TEXT NOT NULL,experiment_id TEXT NOT NULL,ordinal INTEGER NOT NULL,PRIMARY KEY(run_id,experiment_id),UNIQUE(run_id,ordinal));
          CREATE TABLE IF NOT EXISTS research_schema_versions(version INTEGER PRIMARY KEY,name TEXT NOT NULL,applied_utc TEXT NOT NULL);
          CREATE INDEX IF NOT EXISTS ix_research_family ON research_experiments(family_id,created_utc,experiment_id);
          """;command.ExecuteNonQuery();}
        AddColumn(c,"research_experiments","run_id","TEXT");AddColumn(c,"research_experiments","attempt_count","INTEGER");
        Execute(c,null,"""
          UPDATE research_experiments SET attempt_count=CAST(json_extract(result_json,'$.complexity.parameterVariants') AS INTEGER)
          WHERE attempt_count IS NULL AND json_extract(result_json,'$.complexity.parameterVariants') IS NOT NULL;
          UPDATE research_experiments SET run_id=(SELECT r.run_id FROM research_runs r,json_each(r.result_json,'$.experiments') e
            WHERE json_extract(e.value,'$.experimentId')=research_experiments.experiment_id LIMIT 1) WHERE run_id IS NULL;
          INSERT OR IGNORE INTO research_run_experiments(run_id,experiment_id,ordinal)
            SELECT r.run_id,json_extract(e.value,'$.experimentId'),CAST(e.key AS INTEGER)
            FROM research_runs r,json_each(r.result_json,'$.experiments') e
            WHERE json_extract(e.value,'$.experimentId') IS NOT NULL;
          """);
        RecoverInterruptedRuns(c,DateTimeOffset.UtcNow);
        Execute(c,null,"CREATE UNIQUE INDEX IF NOT EXISTS ux_research_single_running ON research_runs((1)) WHERE state='Running'; INSERT OR IGNORE INTO research_schema_versions VALUES(2,'BB-092 recovery audit and single-flight',$now);",("$now",DateTimeOffset.UtcNow.ToString("O",CultureInfo.InvariantCulture)));
    }

    private static void AddColumn(SqliteConnection c,string table,string column,string type)
    {using var info=c.CreateCommand();info.CommandText=$"PRAGMA table_info({table})";using var reader=info.ExecuteReader();while(reader.Read())if(reader.GetString(1)==column)return;reader.Close();Execute(c,null,$"ALTER TABLE {table} ADD COLUMN {column} {type}");}

    private static void RecoverInterruptedRuns(SqliteConnection c,DateTimeOffset recoveredAt)
    {
        var stale=new List<(string Id,string Key,DateTimeOffset Created)>();using(var x=c.CreateCommand()){x.CommandText="SELECT run_id,idempotency_key,created_utc FROM research_runs WHERE state IN ('Pending','Running') ORDER BY created_utc,run_id";using var r=x.ExecuteReader();while(r.Read())stale.Add((r.GetString(0),r.GetString(1),DateTimeOffset.Parse(r.GetString(2),CultureInfo.InvariantCulture)));}
        foreach(var row in stale){var experiments=ReadRunExperiments(c,row.Id);var failed=CreateRun(row.Id,row.Key,ResearchExperimentState.Failed,row.Created,recoveredAt,experiments,"research.run.interruptedBeforeCompletion","RECOVERED_AFTER_RESTART");Execute(c,null,"UPDATE research_runs SET state='Failed',completed_utc=$done,result_json=$json WHERE run_id=$id AND state IN ('Pending','Running')",("$done",recoveredAt.ToString("O",CultureInfo.InvariantCulture)),("$json",JsonSerializer.Serialize(failed,ResearchJson)),("$id",row.Id));}
    }

    internal AutonomousResearchRun RunAutonomousResearch(string idempotencyKey,int maximumExperiments)
    {
        if(string.IsNullOrWhiteSpace(idempotencyKey)||idempotencyKey.Length>128)throw new ArgumentException("A bounded idempotency key is required.");maximumExperiments=Math.Clamp(maximumExperiments,1,FinanceResearchContracts.MaximumTotalExperimentsPerRun);
        using var c=new SqliteConnection(ConnectionString);c.Open();var runId="research-run-"+FinanceResearchContracts.Fingerprint(new{idempotencyKey,FinanceResearchContracts.EngineVersion})[7..23];var created=DateTimeOffset.UtcNow;
        Execute(c,null,"BEGIN IMMEDIATE;");try{var existing=ReadRunByKey(c,idempotencyKey);if(existing is not null){Execute(c,null,"COMMIT;");return existing;}var running=ResearchScalar(c,"SELECT run_id FROM research_runs WHERE state='Running' ORDER BY created_utc,run_id LIMIT 1");if(running is not null){Execute(c,null,"ROLLBACK;");throw new AutonomousResearchBusyException(running);}Execute(c,null,"INSERT INTO research_runs VALUES($id,$key,'Running',$at,NULL,NULL)",("$id",runId),("$key",idempotencyKey),("$at",created.ToString("O",CultureInfo.InvariantCulture)));Execute(c,null,"COMMIT;");}
        catch(SqliteException){TryRollback(c);var existing=ReadRunByKey(c,idempotencyKey);if(existing is not null)return existing;var running=ResearchScalar(c,"SELECT run_id FROM research_runs WHERE state='Running' ORDER BY created_utc,run_id LIMIT 1");if(running is not null)throw new AutonomousResearchBusyException(running);throw;}catch{TryRollback(c);throw;}
        AutonomousResearchTestHook?.Invoke("lease-acquired");
        try
        {
            BuildRobustnessEvaluations();var evaluations=ReadResearchEvaluations(c).Where(x=>x.Plan.Strategy.Id!="buy-and-hold").Take(maximumExperiments).ToArray();var experiments=new List<AutonomousResearchExperiment>();
            foreach(var evidence in evaluations)
            {
                var featureIds=evidence.Plan.Strategy.Id=="momentum"?new[]{"momentum.20.sign"}:new[]{"trend.sma.fast-slow-relation"};foreach(var id in featureIds)_=FinanceResearchFeatureLibrary.Require(id);var familyId="family-"+evidence.Plan.Strategy.Id+"-v1";
                var hypothesisSeed=new{engine=FinanceResearchContracts.EngineVersion,familyId,featureIds,evidence.Plan.FeatureRevisionId,evidence.Plan.MarketRevisionIds,horizon=1};var fingerprint=FinanceResearchContracts.Fingerprint(hypothesisSeed);
                var hypothesis=new ResearchHypothesis("hypothesis-"+fingerprint[7..23],"v1",FinanceResearchContracts.EngineVersion,"bounded-existing-strategy-evidence",evidence.Plan.Strategy.Id=="momentum"?"Known momentum may have different held-out next-session portfolio expectancy than its reference benchmark.":"Known moving-average relation may have different held-out next-session portfolio expectancy than its reference benchmark.",featureIds,"next-session portfolio expectancy",1,evidence.Plan.Universe,evidence.Plan.MarketRevisionIds,evidence.Plan.FeatureRevisionId,ResearchKnowledgeCutoff(c,evidence.Plan.FeatureRevisionId),familyId,fingerprint);
                Execute(c,null,"INSERT OR IGNORE INTO research_hypotheses VALUES($id,$fingerprint,$family,$json,$at)",( "$id",hypothesis.HypothesisId),("$fingerprint",fingerprint),("$family",familyId),("$json",JsonSerializer.Serialize(hypothesis,ResearchJson)),("$at",created.ToString("O")));
                var attempts=Math.Max(1,evidence.ParameterVariantsEvaluated);var cumulative=FamilyAttemptTotal(c,familyId)+attempts;var complexity=FinanceResearchContracts.Complexity(featureIds.Length,1,evidence.Plan.ReferenceParameters.Count,attempts);var integrity=FinanceResearchContracts.Assess(evidence,cumulative,complexity,true,evidence.CostSensitivity.Points.Count>0);var challenger=integrity.State==ResearchIntegrityState.Pass;var verdict=challenger?ResearchExperimentVerdict.Challenger:evidence.Verdict==RobustnessVerdict.InsufficientData?ResearchExperimentVerdict.Inconclusive:ResearchExperimentVerdict.Rejected;
                var experimentId="experiment-"+FinanceResearchContracts.Fingerprint(new{hypothesis.Fingerprint,evidence.EvaluationId,FinanceResearchContracts.EngineVersion})[7..23];var experiment=new AutonomousResearchExperiment(experimentId,hypothesis.HypothesisId,familyId,cumulative,ResearchExperimentState.Completed,verdict,challenger?null:integrity.ReasonCode,complexity,integrity,evidence.EvaluationId,evidence.PrimarySplit.Train.NetReturn,evidence.PrimarySplit.Test.NetReturn,null,null,evidence.PrimarySplit.Test.WinningExits+evidence.PrimarySplit.Test.LosingExits==0?null:(decimal)evidence.PrimarySplit.Test.WinningExits/(evidence.PrimarySplit.Test.WinningExits+evidence.PrimarySplit.Test.LosingExits),evidence.PrimarySplit.Test.MaxDrawdown,"hypothetical-conservative-v1",evidence.Plan.MarketRevisionIds,evidence.Plan.FeatureRevisionId,hypothesis.KnowledgeCutoffUtc,created,runId,attempts);
                Execute(c,null,"BEGIN IMMEDIATE;");try{Execute(c,null,"INSERT OR IGNORE INTO research_experiments(experiment_id,hypothesis_id,family_id,robustness_evaluation_id,state,verdict,result_json,created_utc,run_id,attempt_count) VALUES($id,$hypothesis,$family,$evaluation,'Completed',$verdict,$json,$at,$run,$attempts)",( "$id",experimentId),("$hypothesis",hypothesis.HypothesisId),("$family",familyId),("$evaluation",evidence.EvaluationId),("$verdict",verdict.ToString()),("$json",JsonSerializer.Serialize(experiment,ResearchJson)),("$at",created.ToString("O")),("$run",runId),("$attempts",attempts));Execute(c,null,"INSERT OR IGNORE INTO research_run_experiments VALUES($run,$experiment,$ordinal)",( "$run",runId),("$experiment",experimentId),("$ordinal",experiments.Count));Execute(c,null,"COMMIT;");}catch{TryRollback(c);throw;}var persisted=ReadResearchExperiment(c,experimentId)??throw new InvalidOperationException("Experiment persistence failed.");experiments.Add(persisted);AutonomousResearchTestHook?.Invoke("experiment-persisted");
            }
            var result=CreateRun(runId,idempotencyKey,ResearchExperimentState.Completed,created,DateTimeOffset.UtcNow,experiments,null,"NONE");Execute(c,null,"UPDATE research_runs SET state='Completed',completed_utc=$done,result_json=$json WHERE run_id=$id AND state='Running'",("$done",result.CompletedAtUtc!.Value.ToString("O")),("$json",JsonSerializer.Serialize(result,ResearchJson)),("$id",runId));return result;
        }
        catch{var failedAt=DateTimeOffset.UtcNow;var experiments=ReadRunExperiments(c,runId);var failed=CreateRun(runId,idempotencyKey,ResearchExperimentState.Failed,created,failedAt,experiments,"research.run.failed","NONE");Execute(c,null,"UPDATE research_runs SET state='Failed',completed_utc=$done,result_json=$json WHERE run_id=$id AND state='Running'",("$done",failedAt.ToString("O")),("$json",JsonSerializer.Serialize(failed,ResearchJson)),("$id",runId));throw;}
    }

    internal AutonomousResearchSnapshot AutonomousResearchSnapshot(){using var c=new SqliteConnection(ConnectionString);c.Open();var experiments=ReadExperiments(c,"1=1",[]);var hypotheses=ReadResearchHypotheses(c);var latestId=ResearchScalar(c,"SELECT run_id FROM research_runs ORDER BY created_utc DESC,run_id DESC LIMIT 1");var latest=latestId is null?null:ReadRun(c,latestId);return new(DateTimeOffset.UtcNow,"RESEARCH",0m,FinanceResearchContracts.EngineVersion,FinanceResearchFeatureLibrary.Version,experiments.Count,Count(experiments,ResearchExperimentVerdict.Rejected),Count(experiments,ResearchExperimentVerdict.Inconclusive),Count(experiments,ResearchExperimentVerdict.NotEvaluable),Count(experiments,ResearchExperimentVerdict.Promising),Count(experiments,ResearchExperimentVerdict.Challenger),latest,hypotheses,FinanceResearchFeatureLibrary.Definitions,experiments.Any(x=>x.Verdict==ResearchExperimentVerdict.Challenger)?"NEEDS_MORE_EVIDENCE":"CONTINUE_RESEARCH","NONE");}
    internal AutonomousResearchRunCatalog ResearchRuns(int offset,int limit){ValidatePage(offset,limit);using var c=new SqliteConnection(ConnectionString);c.Open();var total=Convert.ToInt32(ResearchScalar(c,"SELECT CAST(COUNT(*) AS TEXT) FROM research_runs")??"0",CultureInfo.InvariantCulture);using var x=c.CreateCommand();x.CommandText="SELECT run_id FROM research_runs ORDER BY created_utc DESC,run_id DESC LIMIT $limit OFFSET $offset";x.Parameters.AddWithValue("$limit",limit);x.Parameters.AddWithValue("$offset",offset);using var r=x.ExecuteReader();var ids=new List<string>();while(r.Read())ids.Add(r.GetString(0));r.Close();return new(DateTimeOffset.UtcNow,"RESEARCH",offset,limit,total,ids.Select(id=>Summary(ReadRun(c,id)!)).ToArray());}
    internal AutonomousResearchRun? ResearchRun(string runId){ValidateId(runId,"run");using var c=new SqliteConnection(ConnectionString);c.Open();return ReadRun(c,runId);}
    internal AutonomousResearchExperimentCatalog ResearchExperiments(int offset,int limit,string? family,string? verdict,string? state,string? hypothesis,string? run){ValidatePage(offset,limit);if(family is not null)ValidateId(family,"family");if(hypothesis is not null)ValidateId(hypothesis,"hypothesis");if(run is not null)ValidateId(run,"run");var parsedVerdict=ParseOptional<ResearchExperimentVerdict>(verdict,"verdict");var parsedState=ParseOptional<ResearchExperimentState>(state,"state");using var c=new SqliteConnection(ConnectionString);c.Open();var clauses=new List<string>{"1=1"};var args=new List<(string,object)>();void Filter(string column,string name,object? value){if(value is null)return;clauses.Add($"{column}={name}");args.Add((name,value.ToString()!));}Filter("family_id","$family",family);Filter("verdict","$verdict",parsedVerdict);Filter("state","$state",parsedState);Filter("hypothesis_id","$hypothesis",hypothesis);if(run is not null){clauses.Add("experiment_id IN (SELECT experiment_id FROM research_run_experiments WHERE run_id=$run)");args.Add(("$run",run));}var where=string.Join(" AND ",clauses);var total=ResearchCount(c,where,args);var page=ReadExperiments(c,where,args,offset,limit);return new(DateTimeOffset.UtcNow,"RESEARCH",offset,limit,total,page);}
    internal AutonomousResearchExperiment? ResearchExperiment(string id){ValidateId(id,"experiment");using var c=new SqliteConnection(ConnectionString);c.Open();return ReadResearchExperiment(c,id);}

    private static AutonomousResearchRun CreateRun(string id,string key,ResearchExperimentState state,DateTimeOffset created,DateTimeOffset? completed,List<AutonomousResearchExperiment> experiments,string? failure,string recovery)=>new(id,key,state,experiments.Count,Count(experiments,ResearchExperimentVerdict.Rejected),Count(experiments,ResearchExperimentVerdict.Inconclusive),Count(experiments,ResearchExperimentVerdict.NotEvaluable),Count(experiments,ResearchExperimentVerdict.Promising),Count(experiments,ResearchExperimentVerdict.Challenger),created,completed,experiments,failure,recovery);
    private static int Count(IReadOnlyList<AutonomousResearchExperiment> x,ResearchExperimentVerdict verdict)=>x.Count(v=>v.Verdict==verdict);
    private static AutonomousResearchRunSummary Summary(AutonomousResearchRun x)=>new(x.RunId,x.State,x.CreatedAtUtc,x.CompletedAtUtc,x.ExperimentCount,x.RejectedCount,x.InconclusiveCount,x.NotEvaluableCount,x.PromisingCount,x.ChallengerCount,x.FailureReason,x.RecoveryStatus);
    private static void ValidatePage(int offset,int limit){if(offset<0||limit is<1 or>100)throw new ArgumentException("Research history requires offset >= 0 and limit between 1 and 100.");}
    private static void ValidateId(string value,string name){if(string.IsNullOrWhiteSpace(value)||value.Length>160||value.Any(ch=>!(char.IsAsciiLetterOrDigit(ch)||ch is '-' or '.' or '_')))throw new ArgumentException($"Invalid research {name} identifier.");}
    private static T? ParseOptional<T>(string? value,string name)where T:struct,Enum{if(value is null)return null;if(!Enum.TryParse<T>(value,true,out var parsed))throw new ArgumentException($"Invalid research {name} filter.");return parsed;}
    private static void TryRollback(SqliteConnection c){try{Execute(c,null,"ROLLBACK;");}catch(SqliteException){}}
    private static int FamilyAttemptTotal(SqliteConnection c,string family)=>Convert.ToInt32(ResearchScalar(c,"SELECT CAST(COALESCE(SUM(attempt_count),0) AS TEXT) FROM research_experiments WHERE family_id=$family",("$family",family))??"0",CultureInfo.InvariantCulture);
    private static int ResearchCount(SqliteConnection c,string where,IReadOnlyList<(string Name,object Value)> args){using var x=c.CreateCommand();x.CommandText=$"SELECT COUNT(*) FROM research_experiments WHERE {where}";foreach(var a in args)x.Parameters.AddWithValue(a.Name,a.Value);return Convert.ToInt32(x.ExecuteScalar(),CultureInfo.InvariantCulture);}
    private static List<AutonomousResearchExperiment> ReadExperiments(SqliteConnection c,string where,IReadOnlyList<(string Name,object Value)> args,int? offset=null,int? limit=null){using var x=c.CreateCommand();x.CommandText=$"SELECT experiment_id,result_json,run_id,attempt_count,family_id,created_utc FROM research_experiments WHERE {where} ORDER BY created_utc DESC,experiment_id DESC"+(limit is null?string.Empty:" LIMIT $limit OFFSET $offset");foreach(var a in args)x.Parameters.AddWithValue(a.Name,a.Value);if(limit is not null){x.Parameters.AddWithValue("$limit",limit.Value);x.Parameters.AddWithValue("$offset",offset??0);}using var r=x.ExecuteReader();var raw=new List<(string Id,string Json,string? Run,int? Attempts,string Family,string Created)>();while(r.Read())raw.Add((r.GetString(0),r.GetString(1),r.IsDBNull(2)?null:r.GetString(2),r.IsDBNull(3)?null:r.GetInt32(3),r.GetString(4),r.GetString(5)));r.Close();var result=new List<AutonomousResearchExperiment>();foreach(var row in raw){var item=JsonSerializer.Deserialize<AutonomousResearchExperiment>(row.Json,ResearchJson)!;var cumulative=Convert.ToInt32(ResearchScalar(c,"SELECT CAST(COALESCE(SUM(attempt_count),0) AS TEXT) FROM research_experiments WHERE family_id=$family AND (created_utc<$created OR (created_utc=$created AND experiment_id<=$id))",("$family",row.Family),("$created",row.Created),("$id",row.Id))??"0",CultureInfo.InvariantCulture);var runIds=ReadStrings(c,"SELECT run_id FROM research_run_experiments WHERE experiment_id=$id ORDER BY run_id",("$id",row.Id));result.Add(item with{ExperimentId=row.Id,FamilyId=row.Family,RunId=row.Run,AttemptCount=row.Attempts,FamilyAttemptCount=cumulative,RunIds=runIds});}return result;}
    private static List<AutonomousResearchExperiment> ReadRunExperiments(SqliteConnection c,string runId){var ids=ReadStrings(c,"SELECT experiment_id FROM research_run_experiments WHERE run_id=$run ORDER BY ordinal",("$run",runId));var result=new List<AutonomousResearchExperiment>();foreach(var id in ids)if(ReadResearchExperiment(c,id) is{} value)result.Add(value);return result;}
    private static string[] ReadStrings(SqliteConnection c,string sql,params(string Name,object Value)[] args){using var x=c.CreateCommand();x.CommandText=sql;foreach(var a in args)x.Parameters.AddWithValue(a.Name,a.Value);using var r=x.ExecuteReader();var values=new List<string>();while(r.Read())values.Add(r.GetString(0));return values.ToArray();}
    private static AutonomousResearchExperiment? ReadResearchExperiment(SqliteConnection c,string id)=>ReadExperiments(c,"experiment_id=$id",[("$id",id)]).SingleOrDefault();
    private static AutonomousResearchRun? ReadRunByKey(SqliteConnection c,string key){var id=ResearchScalar(c,"SELECT run_id FROM research_runs WHERE idempotency_key=$key",("$key",key));return id is null?null:ReadRun(c,id);}
    private static AutonomousResearchRun? ReadRun(SqliteConnection c,string id){using var x=c.CreateCommand();x.CommandText="SELECT idempotency_key,state,created_utc,completed_utc,result_json FROM research_runs WHERE run_id=$id";x.Parameters.AddWithValue("$id",id);using var r=x.ExecuteReader();if(!r.Read())return null;var key=r.GetString(0);var state=Enum.Parse<ResearchExperimentState>(r.GetString(1));var created=DateTimeOffset.Parse(r.GetString(2),CultureInfo.InvariantCulture);DateTimeOffset? completed=r.IsDBNull(3)?null:DateTimeOffset.Parse(r.GetString(3),CultureInfo.InvariantCulture);var json=r.IsDBNull(4)?null:r.GetString(4);r.Close();var experiments=ReadRunExperiments(c,id);if(json is not null&&state!=ResearchExperimentState.Running){var stored=JsonSerializer.Deserialize<AutonomousResearchRun>(json,ResearchJson)!;return CreateRun(id,key,state,created,completed,experiments,stored.FailureReason,stored.RecoveryStatus);}return CreateRun(id,key,state,created,completed,experiments,state==ResearchExperimentState.Running?null:"research.run.legacyIncomplete",state==ResearchExperimentState.Running?"NONE":"LEGACY_RECONSTRUCTED");}
    private static List<RobustnessEvaluationResult> ReadResearchEvaluations(SqliteConnection c){using var x=c.CreateCommand();x.CommandText="SELECT result_json FROM robustness_evaluations ORDER BY strategy_id,evaluation_id";using var r=x.ExecuteReader();var rows=new List<RobustnessEvaluationResult>();while(r.Read())rows.Add(JsonSerializer.Deserialize<RobustnessEvaluationResult>(r.GetString(0),ResearchJson)!);return rows;}
    private static DateTimeOffset ResearchKnowledgeCutoff(SqliteConnection c,string revision)=>DateTimeOffset.Parse(ResearchScalar(c,"SELECT MAX(knowledge_utc) FROM feature_values WHERE revision_id=$id",("$id",revision))??throw new InvalidOperationException("Feature knowledge cutoff is unavailable."),CultureInfo.InvariantCulture);
    private static List<ResearchHypothesis> ReadResearchHypotheses(SqliteConnection c){using var x=c.CreateCommand();x.CommandText="SELECT hypothesis_json FROM research_hypotheses ORDER BY created_utc DESC,hypothesis_id";using var r=x.ExecuteReader();var rows=new List<ResearchHypothesis>();while(r.Read())rows.Add(JsonSerializer.Deserialize<ResearchHypothesis>(r.GetString(0),ResearchJson)!);return rows;}
    private static string? ResearchScalar(SqliteConnection c,string sql,params(string Name,object Value)[] args){using var x=c.CreateCommand();x.CommandText=sql;foreach(var a in args)x.Parameters.AddWithValue(a.Name,a.Value);return x.ExecuteScalar() as string;}
}
