using System.Globalization;
using BigBrain.Api.SystemRecovery;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

public sealed record FinanceResearchOperationsOptions
{
    public const string Section="Finance:ResearchOperations";
    public const string CurrentVersion="finance-research-operations-v1";
    public string OperationsVersion{get;set;}=CurrentVersion;
    public bool MaintenancePaused{get;set;}
    public int AttentionFailureThreshold{get;set;}=3;
    public int StaleSchedulerMinutes{get;set;}=180;
    public int PersistentWaitHours{get;set;}=24;
    public void Validate()
    {
        if(OperationsVersion!=CurrentVersion)throw new InvalidOperationException("Finance research operations version is invalid.");
        if(AttentionFailureThreshold is<2 or>10)throw new InvalidOperationException("Operational failure threshold must be between 2 and 10.");
        if(StaleSchedulerMinutes is<60 or>1440)throw new InvalidOperationException("Stale scheduler threshold must be between 60 and 1440 minutes.");
        if(PersistentWaitHours is<1 or>168)throw new InvalidOperationException("Persistent wait threshold must be between 1 and 168 hours.");
    }
}

public enum FinanceResearchOperationalState{Disabled,Maintenance,Waiting,Ready,Running,Deferred,Degraded,AttentionRequired}
public sealed record FinanceResearchOperationalIncident(long IncidentId,string OpportunityId,DateTimeOffset OccurredAtUtc,string Reason);
public sealed record FinanceResearchOperationsStatus(string OperationsVersion,DateTimeOffset EvaluatedAtUtc,
    FinanceResearchOperationalState State,bool RequiresAttention,string CurrentActivity,bool SchedulerEnabled,bool MaintenancePaused,
    DateTimeOffset? LastSchedulerEvaluationUtc,DateTimeOffset? LastSuccessfulResearchUtc,DateTimeOffset? LastOperationalFailureUtc,
    int ConsecutiveOperationalFailures,string? LastFailureReason,DateTimeOffset? LastSuccessfulEvidenceRefreshUtc,
    string DataReadiness,string? ResourceDecision,string? ActiveResearchRunId,string OperatingMode,decimal BudgetSek,string ExecutionAuthority);
public sealed record FinanceResearchOperationalIncidentCatalog(DateTimeOffset GeneratedAtUtc,int Offset,int Limit,int Total,IReadOnlyList<FinanceResearchOperationalIncident> Incidents);

internal sealed partial class EodhdMarketMemory
{
    private static void InitializeResearchOperationsStorage(SqliteConnection c)
    {
        Execute(c,null,"""
          CREATE TABLE IF NOT EXISTS research_operations(
            singleton INTEGER PRIMARY KEY CHECK(singleton=1),last_scheduler_evaluation_utc TEXT,last_success_utc TEXT,
            last_failure_utc TEXT,consecutive_operational_failures INTEGER NOT NULL,last_failure_reason TEXT,updated_utc TEXT NOT NULL);
          CREATE TABLE IF NOT EXISTS research_operational_incidents(
            incident_id INTEGER PRIMARY KEY AUTOINCREMENT,opportunity_id TEXT NOT NULL UNIQUE,occurred_utc TEXT NOT NULL,reason TEXT NOT NULL);
          INSERT OR IGNORE INTO research_operations VALUES(1,NULL,NULL,NULL,0,NULL,'1970-01-01T00:00:00Z');
          CREATE INDEX IF NOT EXISTS ix_research_operational_incidents ON research_operational_incidents(occurred_utc DESC,incident_id DESC);
          """);
    }

    internal void ReconcileResearchOperations(DateTimeOffset now)
    {
        var ids=new List<string>();using(var c=new SqliteConnection(ConnectionString)){c.Open();using var x=c.CreateCommand();x.CommandText="SELECT opportunity_id FROM research_schedule_opportunities WHERE state='Started' ORDER BY opportunity_id";using var r=x.ExecuteReader();while(r.Read())ids.Add(r.GetString(0));}
        foreach(var id in ids){using var c=new SqliteConnection(ConnectionString);c.Open();var run=ReadRunByKey(c,id);FinanceResearchOpportunity? repaired=null;if(run?.State==ResearchExperimentState.Completed)repaired=UpdateResearchOpportunity(id,FinanceResearchOpportunityState.Completed,now,run.RunId,"finance.research.scheduler.completed",null);else if(run?.State==ResearchExperimentState.Failed)repaired=UpdateResearchOpportunity(id,FinanceResearchOpportunityState.Failed,now,run.RunId,run.FailureReason??"finance.research.scheduler.recoveredResearchFailed",null);else if(run is null)repaired=UpdateResearchOpportunity(id,FinanceResearchOpportunityState.Deferred,now,null,"finance.research.scheduler.interruptedBeforeResearch",now);if(repaired is not null)RecordResearchOperationsEvaluation(now,repaired);}
    }

    internal void RecordResearchOperationsEvaluation(DateTimeOffset now,FinanceResearchOpportunity? opportunity)
    {
        using var c=new SqliteConnection(ConnectionString);c.Open();Execute(c,null,"UPDATE research_operations SET last_scheduler_evaluation_utc=$at,updated_utc=$at WHERE singleton=1",("$at",now.ToString("O",CultureInfo.InvariantCulture)));
        if(opportunity is null)return;
        if(opportunity.State==FinanceResearchOpportunityState.Completed){Execute(c,null,"UPDATE research_operations SET last_success_utc=$at,consecutive_operational_failures=0,last_failure_reason=NULL,updated_utc=$at WHERE singleton=1",("$at",now.ToString("O",CultureInfo.InvariantCulture)));return;}
        if(opportunity.State!=FinanceResearchOpportunityState.Failed||!IsOperationalFailure(opportunity.Reason))return;
        using var transaction=c.BeginTransaction();using var insert=c.CreateCommand();insert.Transaction=transaction;insert.CommandText="INSERT OR IGNORE INTO research_operational_incidents(opportunity_id,occurred_utc,reason) VALUES($id,$at,$reason)";insert.Parameters.AddWithValue("$id",opportunity.OpportunityId);insert.Parameters.AddWithValue("$at",now.ToString("O",CultureInfo.InvariantCulture));insert.Parameters.AddWithValue("$reason",opportunity.Reason!);var created=insert.ExecuteNonQuery()==1;if(created)Execute(c,transaction,"UPDATE research_operations SET last_failure_utc=$at,consecutive_operational_failures=consecutive_operational_failures+1,last_failure_reason=$reason,updated_utc=$at WHERE singleton=1",("$at",now.ToString("O",CultureInfo.InvariantCulture)),("$reason",opportunity.Reason!));transaction.Commit();
    }

    internal FinanceResearchOperationsStatus ResearchOperationsStatus(DateTimeOffset now,FinanceResearchOperationsOptions options,
        FinanceResearchSchedulerOptions scheduler,SystemRecoverySnapshot recovery,FinanceCadenceSnapshot cadence)
    {
        using var c=new SqliteConnection(ConnectionString);c.Open();using var x=c.CreateCommand();x.CommandText="SELECT last_scheduler_evaluation_utc,last_success_utc,last_failure_utc,consecutive_operational_failures,last_failure_reason FROM research_operations WHERE singleton=1";using var r=x.ExecuteReader();r.Read();DateTimeOffset? ReadTime(int i)=>r.IsDBNull(i)?null:DateTimeOffset.Parse(r.GetString(i),CultureInfo.InvariantCulture);var lastEvaluation=ReadTime(0);var lastSuccess=ReadTime(1);var lastFailure=ReadTime(2);var streak=r.GetInt32(3);var lastReason=r.IsDBNull(4)?null:r.GetString(4);r.Close();
        lastSuccess??=ReadNullableOperationsTime(c,"SELECT MAX(completed_utc) FROM research_schedule_opportunities WHERE state='Completed'");var latest=LatestResearchOpportunity();var active=ResearchScalar(c,"SELECT run_id FROM research_runs WHERE state='Running' ORDER BY created_utc,run_id LIMIT 1");var readiness=latest?.Reason is{} reason&&reason.Contains("featuresNotReady",StringComparison.Ordinal)?"FEATURES_NOT_READY":latest?.Reason is{} dataReason&&dataReason.Contains("universeIncomplete",StringComparison.Ordinal)?"DATA_NOT_READY":"READY";var resource=latest?.ResourceDecision?.Decision.ToString().ToUpperInvariant();
        var state=FinanceResearchOperationalState.Waiting;var attention=false;var activity="WAITING";
        if(!scheduler.Enabled){state=FinanceResearchOperationalState.Disabled;activity="SCHEDULER_DISABLED";}
        else if(options.MaintenancePaused){state=FinanceResearchOperationalState.Maintenance;activity="MAINTENANCE_PAUSED";}
        else if(active is not null){state=FinanceResearchOperationalState.Running;activity="RESEARCH_RUNNING";}
        else if(streak>=options.AttentionFailureThreshold){state=FinanceResearchOperationalState.AttentionRequired;attention=true;activity="OPERATIONAL_FAILURE_STREAK";}
        else if(lastEvaluation is null?(now-recovery.BootedAtUtc>TimeSpan.FromMinutes(options.StaleSchedulerMinutes)):(now-lastEvaluation>TimeSpan.FromMinutes(options.StaleSchedulerMinutes))){state=FinanceResearchOperationalState.Degraded;attention=true;activity="SCHEDULER_STALE";}
        else if(latest is{State:FinanceResearchOpportunityState.Deferred}&&now-latest.CreatedAtUtc>TimeSpan.FromHours(options.PersistentWaitHours)){state=FinanceResearchOperationalState.Degraded;attention=true;activity=latest.Reason?.Contains("resource",StringComparison.Ordinal)==true?"RESOURCE_WAIT_PERSISTENT":"DATA_WAIT_PERSISTENT";}
        else if(cadence.Health=="Degraded"){state=FinanceResearchOperationalState.Degraded;activity="ACQUISITION_DEGRADED";}
        else if(latest?.State==FinanceResearchOpportunityState.Deferred){state=FinanceResearchOperationalState.Deferred;activity="TEMPORARILY_DEFERRED";}
        else if(latest?.State==FinanceResearchOpportunityState.Completed){state=FinanceResearchOperationalState.Ready;activity="LAST_CYCLE_COMPLETED";}
        return new(options.OperationsVersion,now,state,attention,activity,scheduler.Enabled,options.MaintenancePaused,lastEvaluation,lastSuccess,lastFailure,streak,lastReason,cadence.LastSuccessfulAcquisitionUtc,readiness,resource,active,"RESEARCH",0m,"NONE");
    }

    internal FinanceResearchOperationalIncidentCatalog ResearchOperationalIncidents(int offset,int limit)
    {
        if(offset<0||limit is<1 or>100)throw new ArgumentException("Research operations history requires offset >= 0 and limit between 1 and 100.");using var c=new SqliteConnection(ConnectionString);c.Open();var total=Convert.ToInt32(ResearchScalar(c,"SELECT CAST(COUNT(*) AS TEXT) FROM research_operational_incidents")??"0",CultureInfo.InvariantCulture);using var x=c.CreateCommand();x.CommandText="SELECT incident_id,opportunity_id,occurred_utc,reason FROM research_operational_incidents ORDER BY occurred_utc DESC,incident_id DESC LIMIT $limit OFFSET $offset";x.Parameters.AddWithValue("$limit",limit);x.Parameters.AddWithValue("$offset",offset);using var r=x.ExecuteReader();var incidents=new List<FinanceResearchOperationalIncident>();while(r.Read())incidents.Add(new(r.GetInt64(0),r.GetString(1),DateTimeOffset.Parse(r.GetString(2),CultureInfo.InvariantCulture),r.GetString(3)));return new(DateTimeOffset.UtcNow,offset,limit,total,incidents);
    }

    private static bool IsOperationalFailure(string? reason)=>reason is not null&&(reason.StartsWith("finance.research.scheduler.unexpected.",StringComparison.Ordinal)||reason.StartsWith("finance.research.scheduler.researchFailed.",StringComparison.Ordinal)||reason.Contains("interrupted",StringComparison.OrdinalIgnoreCase));
    private static DateTimeOffset? ReadNullableOperationsTime(SqliteConnection c,string sql){var value=ResearchScalar(c,sql);return value is null?null:DateTimeOffset.Parse(value,CultureInfo.InvariantCulture);}
}

internal sealed class FinanceResearchOperationsCoordinator(FinanceResearchOperationsOptions options,FinanceResearchSchedulerOptions scheduler,
    EodhdFinanceOptions provider,FinanceCadenceOptions cadence,EodhdMarketMemory memory,SystemRecoveryCoordinator recovery,TimeProvider clock)
{
    internal FinanceResearchOperationsStatus Status()=>memory.ResearchOperationsStatus(clock.GetUtcNow(),options,scheduler,recovery.Snapshot(),memory.CadenceSnapshot(provider.Enabled,recovery.MayStartTimeSensitiveWork,cadence.ProviderWindowStartUtcHour,cadence.InternalCheckMinutes));
}

internal sealed class FinanceResearchOperationsWorker(EodhdMarketMemory memory,SystemRecoveryCoordinator recovery,TimeProvider clock,ILogger<FinanceResearchOperationsWorker> logger):BackgroundService
{
    private static readonly Action<ILogger,Exception?> Reconciled=LoggerMessage.Define(LogLevel.Information,new EventId(9500,"FinanceResearchOperationsReconciled"),"Finance research operations reconciliation completed.");
    protected override async Task ExecuteAsync(CancellationToken token){await recovery.WaitUntilRecoveredAsync(token);memory.ReconcileResearchOperations(clock.GetUtcNow());Reconciled(logger,null);await Task.Delay(Timeout.InfiniteTimeSpan,token);}
}
