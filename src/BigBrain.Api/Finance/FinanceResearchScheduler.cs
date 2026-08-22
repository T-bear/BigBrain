using System.Globalization;
using System.Text.Json;
using BigBrain.Api.SystemRecovery;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

public sealed record FinanceResearchSchedulerOptions
{
    public const string Section="Finance:ResearchScheduler";
    public const string CurrentVersion="finance-research-scheduler-v1";
    public bool Enabled{get;set;}
    public string SchedulerVersion{get;set;}=CurrentVersion;
    public int CheckIntervalMinutes{get;set;}=60;
    public int ScheduledUtcHour{get;set;}=2;
    public int MaximumExperimentsPerRun{get;set;}=2;
    public void Validate()
    {
        if(SchedulerVersion!=CurrentVersion)throw new InvalidOperationException("Finance research scheduler version is invalid.");
        if(CheckIntervalMinutes is<15 or>360)throw new InvalidOperationException("Finance research scheduler interval must be between 15 and 360 minutes.");
        if(ScheduledUtcHour is<0 or>23)throw new InvalidOperationException("Finance research scheduler UTC hour must be between 0 and 23.");
        if(MaximumExperimentsPerRun is<1 or>FinanceResearchContracts.MaximumTotalExperimentsPerRun)throw new InvalidOperationException($"Finance research scheduler experiment budget must be between 1 and {FinanceResearchContracts.MaximumTotalExperimentsPerRun}.");
    }
}

public enum FinanceResearchOpportunityState{Pending,Skipped,Deferred,Started,Completed,Failed}
public sealed record FinanceResearchOpportunity(string OpportunityId,string SchedulerVersion,DateOnly ResearchDate,
    DateTimeOffset DueAtUtc,DateTimeOffset CreatedAtUtc,DateTimeOffset? AttemptedAtUtc,DateTimeOffset? CompletedAtUtc,
    FinanceResearchOpportunityState State,string? ResearchRunId,string? Reason,DateTimeOffset? NextEligibilityUtc);
public sealed record FinanceResearchSchedulerStatus(DateTimeOffset CurrentUtc,bool Enabled,string SchedulerVersion,
    DateTimeOffset? NextDueUtc,FinanceResearchOpportunity? LastOpportunity,string? LastResearchRunId,string LastOutcome,
    string? LastReason,bool ResearchCurrentlyRunning,string OperatingMode,decimal BudgetSek,string ExecutionAuthority,
    bool DataReady,string ReadinessReason,int CurrentInstrumentCount,int ExpectedInstrumentCount);
public sealed record FinanceResearchSchedulerHistory(DateTimeOffset GeneratedAtUtc,string OperatingMode,int Offset,int Limit,
    int Total,IReadOnlyList<FinanceResearchOpportunity> Opportunities);
internal sealed record FinanceResearchDataReadiness(DateOnly RequiredResearchDate,IReadOnlyList<string> ExpectedUniverse,
    IReadOnlyList<string> CurrentInstruments,IReadOnlyList<string> StaleInstruments,IReadOnlyList<string> MissingInstruments,
    string? FeatureRevisionId,DateOnly? FeatureCoverageDate,bool IsReady,string ReasonCode);

internal static class FinanceResearchSchedule
{
    internal static DateTimeOffset DueFor(DateTimeOffset nowUtc,int hour)=>new(nowUtc.Year,nowUtc.Month,nowUtc.Day,hour,0,0,TimeSpan.Zero);
    internal static DateOnly ResearchDate(DateTimeOffset dueUtc)=>DateOnly.FromDateTime(dueUtc.UtcDateTime).AddDays(-1);
    internal static string OpportunityId(string version,DateOnly researchDate)=>$"{version}:{researchDate:yyyy-MM-dd}";
    internal static DateTimeOffset NextDue(DateTimeOffset nowUtc,int hour){var today=DueFor(nowUtc,hour);return nowUtc<today?today:today.AddDays(1);}
}

internal sealed partial class EodhdMarketMemory
{
    private static void InitializeResearchSchedulerStorage(SqliteConnection connection)
    {
        using var command=connection.CreateCommand();command.CommandText="""
          CREATE TABLE IF NOT EXISTS research_schedule_opportunities(
            opportunity_id TEXT PRIMARY KEY,scheduler_version TEXT NOT NULL,research_date TEXT NOT NULL,
            due_utc TEXT NOT NULL,created_utc TEXT NOT NULL,attempted_utc TEXT,completed_utc TEXT,state TEXT NOT NULL,
            research_run_id TEXT,reason TEXT,next_eligibility_utc TEXT);
          CREATE INDEX IF NOT EXISTS ix_research_schedule_history ON research_schedule_opportunities(due_utc DESC,opportunity_id DESC);
          UPDATE research_schedule_opportunities SET state='Deferred',reason='finance.research.scheduler.interruptedBeforeResearch',next_eligibility_utc=NULL
            WHERE state='Started' AND NOT EXISTS(SELECT 1 FROM research_runs WHERE idempotency_key=research_schedule_opportunities.opportunity_id);
          """;command.ExecuteNonQuery();
    }

    internal FinanceResearchOpportunity CreateOrReadResearchOpportunity(string id,string version,DateOnly researchDate,DateTimeOffset due,DateTimeOffset now)
    {
        using var c=new SqliteConnection(ConnectionString);c.Open();Execute(c,null,"INSERT OR IGNORE INTO research_schedule_opportunities VALUES($id,$version,$date,$due,$created,NULL,NULL,'Pending',NULL,NULL,NULL)",("$id",id),("$version",version),("$date",researchDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),("$due",due.ToString("O",CultureInfo.InvariantCulture)),("$created",now.ToString("O",CultureInfo.InvariantCulture)));return ReadResearchOpportunity(c,id)!;
    }
    internal bool TryClaimResearchOpportunity(string id,DateTimeOffset now)
    {
        using var c=new SqliteConnection(ConnectionString);c.Open();Execute(c,null,"BEGIN IMMEDIATE;");try{using var x=c.CreateCommand();x.CommandText="UPDATE research_schedule_opportunities SET state='Started',attempted_utc=$now,reason=NULL,next_eligibility_utc=NULL WHERE opportunity_id=$id AND (state='Pending' OR (state='Deferred' AND (next_eligibility_utc IS NULL OR next_eligibility_utc<=$now)))";x.Parameters.AddWithValue("$id",id);x.Parameters.AddWithValue("$now",now.ToString("O",CultureInfo.InvariantCulture));var claimed=x.ExecuteNonQuery()==1;Execute(c,null,"COMMIT;");return claimed;}catch{TryRollback(c);throw;}
    }
    internal FinanceResearchOpportunity UpdateResearchOpportunity(string id,FinanceResearchOpportunityState state,DateTimeOffset now,string? runId,string? reason,DateTimeOffset? next)
    {
        using var c=new SqliteConnection(ConnectionString);c.Open();Execute(c,null,"UPDATE research_schedule_opportunities SET state=$state,research_run_id=COALESCE($run,research_run_id),reason=$reason,next_eligibility_utc=$next,completed_utc=CASE WHEN $terminal=1 THEN $now ELSE NULL END WHERE opportunity_id=$id",("$state",state.ToString()),("$run",runId??(object)DBNull.Value),("$reason",reason??(object)DBNull.Value),("$next",next?.ToString("O",CultureInfo.InvariantCulture)??(object)DBNull.Value),("$terminal",state is FinanceResearchOpportunityState.Completed or FinanceResearchOpportunityState.Failed or FinanceResearchOpportunityState.Skipped?1:0),("$now",now.ToString("O",CultureInfo.InvariantCulture)),("$id",id));return ReadResearchOpportunity(c,id)!;
    }
    internal AutonomousResearchRun? AutonomousResearchRunByKey(string key){using var c=new SqliteConnection(ConnectionString);c.Open();return ReadRunByKey(c,key);}
    internal bool AutonomousResearchIsRunning(){using var c=new SqliteConnection(ConnectionString);c.Open();return ResearchScalar(c,"SELECT run_id FROM research_runs WHERE state='Running' LIMIT 1") is not null;}
    internal FinanceResearchDataReadiness ResearchDataReadiness(DateOnly requiredDate)
    {
        var expected=EodhdCatalog.Watchlist.Select(x=>x.InstrumentId).Order(StringComparer.Ordinal).ToArray();
        using var c=new SqliteConnection(ConnectionString);c.Open();var current=new List<string>();var stale=new List<string>();var missing=new List<string>();
        foreach(var instrument in expected)
        {
            using var x=c.CreateCommand();x.CommandText="SELECT session_date,revision_id FROM observations WHERE instrument_id=$instrument ORDER BY session_date DESC,acquired_utc DESC,revision_id DESC LIMIT 1";x.Parameters.AddWithValue("$instrument",instrument);using var r=x.ExecuteReader();
            if(!r.Read()){missing.Add(instrument);continue;}var date=DateOnly.Parse(r.GetString(0),CultureInfo.InvariantCulture);if(date!=requiredDate){stale.Add(instrument);continue;}current.Add(instrument);
        }
        if(missing.Count>0||stale.Count>0)return new(requiredDate,expected,current,stale,missing,null,null,false,"finance.research.scheduler.universeIncomplete");
        var canonicalRevisions=new List<string>();using(var lineage=c.CreateCommand()){lineage.CommandText="SELECT DISTINCT revision_id FROM (SELECT revision_id,ROW_NUMBER() OVER(PARTITION BY instrument_id,session_date ORDER BY acquired_utc DESC,revision_id DESC) rank FROM observations WHERE session_date<=$date) WHERE rank=1 ORDER BY revision_id";lineage.Parameters.AddWithValue("$date",requiredDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));using var r=lineage.ExecuteReader();while(r.Read())canonicalRevisions.Add(r.GetString(0));}
        using var feature=c.CreateCommand();feature.CommandText="SELECT revision_id,source_revisions_json,coverage_to FROM feature_revisions WHERE coverage_to=$date ORDER BY created_utc DESC,revision_id DESC";feature.Parameters.AddWithValue("$date",requiredDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));using var reader=feature.ExecuteReader();var candidates=new List<(string Id,string[] Sources,DateOnly? Coverage)>();while(reader.Read())candidates.Add((reader.GetString(0),JsonSerializer.Deserialize<string[]>(reader.GetString(1))??[],reader.IsDBNull(2)?null:DateOnly.Parse(reader.GetString(2),CultureInfo.InvariantCulture)));
        if(candidates.Count==0)return new(requiredDate,expected,current,stale,missing,null,null,false,"finance.research.scheduler.featuresNotReady");
        var normalized=canonicalRevisions.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();var matching=candidates.Where(x=>x.Sources.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).SequenceEqual(normalized,StringComparer.Ordinal)).ToArray();
        if(matching.Length==0)return new(requiredDate,expected,current,stale,missing,candidates[0].Id,candidates[0].Coverage,false,"finance.research.scheduler.featureLineageIncomplete");
        var selected=matching[0];
        foreach(var instrument in expected){using var x=c.CreateCommand();x.CommandText="SELECT COUNT(*) FROM feature_values WHERE revision_id=$revision AND instrument_id=$instrument AND session_date=$date";x.Parameters.AddWithValue("$revision",selected.Id);x.Parameters.AddWithValue("$instrument",instrument);x.Parameters.AddWithValue("$date",requiredDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));if(Convert.ToInt32(x.ExecuteScalar(),CultureInfo.InvariantCulture)==0)return new(requiredDate,expected,current,stale,missing,selected.Id,selected.Coverage,false,"finance.research.scheduler.featuresNotReady");}
        return new(requiredDate,expected,current,stale,missing,selected.Id,selected.Coverage,true,"finance.research.scheduler.ready");
    }
    internal void SupersedeDeferredResearchOpportunities(DateOnly currentResearchDate,DateTimeOffset now)
    {using var c=new SqliteConnection(ConnectionString);c.Open();Execute(c,null,"UPDATE research_schedule_opportunities SET state='Skipped',reason='finance.research.scheduler.superseded',completed_utc=$now,next_eligibility_utc=NULL WHERE state='Deferred' AND research_date<$date",("$now",now.ToString("O",CultureInfo.InvariantCulture)),("$date",currentResearchDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)));}
    internal FinanceResearchOpportunity? ResearchOpportunity(string id){using var c=new SqliteConnection(ConnectionString);c.Open();return ReadResearchOpportunity(c,id);}
    internal FinanceResearchSchedulerHistory ResearchSchedulerHistory(int offset,int limit)
    {
        if(offset<0||limit is<1 or>100)throw new ArgumentException("Research scheduler history requires offset >= 0 and limit between 1 and 100.");using var c=new SqliteConnection(ConnectionString);c.Open();using var count=c.CreateCommand();count.CommandText="SELECT COUNT(*) FROM research_schedule_opportunities";var total=Convert.ToInt32(count.ExecuteScalar(),CultureInfo.InvariantCulture);using var x=c.CreateCommand();x.CommandText="SELECT opportunity_id FROM research_schedule_opportunities ORDER BY due_utc DESC,opportunity_id DESC LIMIT $limit OFFSET $offset";x.Parameters.AddWithValue("$limit",limit);x.Parameters.AddWithValue("$offset",offset);using var r=x.ExecuteReader();var ids=new List<string>();while(r.Read())ids.Add(r.GetString(0));r.Close();return new(DateTimeOffset.UtcNow,"RESEARCH",offset,limit,total,ids.Select(id=>ReadResearchOpportunity(c,id)!).ToArray());
    }
    internal FinanceResearchOpportunity? LatestResearchOpportunity(){using var c=new SqliteConnection(ConnectionString);c.Open();var id=ResearchScalar(c,"SELECT opportunity_id FROM research_schedule_opportunities ORDER BY due_utc DESC,opportunity_id DESC LIMIT 1");return id is null?null:ReadResearchOpportunity(c,id);}
    private static FinanceResearchOpportunity? ReadResearchOpportunity(SqliteConnection c,string id){using var x=c.CreateCommand();x.CommandText="SELECT scheduler_version,research_date,due_utc,created_utc,attempted_utc,completed_utc,state,research_run_id,reason,next_eligibility_utc FROM research_schedule_opportunities WHERE opportunity_id=$id";x.Parameters.AddWithValue("$id",id);using var r=x.ExecuteReader();if(!r.Read())return null;return new(id,r.GetString(0),DateOnly.Parse(r.GetString(1),CultureInfo.InvariantCulture),DateTimeOffset.Parse(r.GetString(2),CultureInfo.InvariantCulture),DateTimeOffset.Parse(r.GetString(3),CultureInfo.InvariantCulture),r.IsDBNull(4)?null:DateTimeOffset.Parse(r.GetString(4),CultureInfo.InvariantCulture),r.IsDBNull(5)?null:DateTimeOffset.Parse(r.GetString(5),CultureInfo.InvariantCulture),Enum.Parse<FinanceResearchOpportunityState>(r.GetString(6)),r.IsDBNull(7)?null:r.GetString(7),r.IsDBNull(8)?null:r.GetString(8),r.IsDBNull(9)?null:DateTimeOffset.Parse(r.GetString(9),CultureInfo.InvariantCulture));}
}

internal sealed class FinanceResearchOrchestrator(FinanceResearchSchedulerOptions options,EodhdMarketMemory memory)
{
    internal FinanceResearchOpportunity? CheckAndRun(DateTimeOffset nowUtc,bool mayStartTimeSensitiveWork,CancellationToken token)
    {
        options.Validate();if(!options.Enabled)return null;if(nowUtc.Offset!=TimeSpan.Zero)throw new ArgumentException("Research scheduler requires UTC time.");token.ThrowIfCancellationRequested();var due=FinanceResearchSchedule.DueFor(nowUtc,options.ScheduledUtcHour);if(nowUtc<due)return null;var researchDate=FinanceResearchSchedule.ResearchDate(due);memory.SupersedeDeferredResearchOpportunities(researchDate,nowUtc);var id=FinanceResearchSchedule.OpportunityId(options.SchedulerVersion,researchDate);var opportunity=memory.CreateOrReadResearchOpportunity(id,options.SchedulerVersion,researchDate,due,nowUtc);
        if(opportunity.State is FinanceResearchOpportunityState.Completed or FinanceResearchOpportunityState.Failed or FinanceResearchOpportunityState.Skipped)return opportunity;
        if(opportunity is{State:FinanceResearchOpportunityState.Deferred,NextEligibilityUtc:not null}&&opportunity.NextEligibilityUtc>nowUtc)return opportunity;
        if(!UsMarketCalendar.IsSession(researchDate))return memory.UpdateResearchOpportunity(id,FinanceResearchOpportunityState.Skipped,nowUtc,null,"finance.research.scheduler.nonResearchDay",null);
        if(!mayStartTimeSensitiveWork)return memory.UpdateResearchOpportunity(id,FinanceResearchOpportunityState.Deferred,nowUtc,null,"finance.research.scheduler.recoveryNotReady",nowUtc.AddMinutes(options.CheckIntervalMinutes));
        var readiness=memory.ResearchDataReadiness(researchDate);if(!readiness.IsReady)return memory.UpdateResearchOpportunity(id,FinanceResearchOpportunityState.Deferred,nowUtc,null,readiness.ReasonCode,nowUtc.AddMinutes(options.CheckIntervalMinutes));
        token.ThrowIfCancellationRequested();
        if(!memory.TryClaimResearchOpportunity(id,nowUtc)){var current=memory.ResearchOpportunity(id)!;return Reconcile(current,nowUtc);}
        try{var run=memory.RunAutonomousResearch(id,options.MaximumExperimentsPerRun);return run.State switch{ResearchExperimentState.Completed=>memory.UpdateResearchOpportunity(id,FinanceResearchOpportunityState.Completed,nowUtc,run.RunId,"finance.research.scheduler.completed",null),ResearchExperimentState.Failed=>memory.UpdateResearchOpportunity(id,FinanceResearchOpportunityState.Failed,nowUtc,run.RunId,run.FailureReason??"finance.research.scheduler.researchFailed",null),_=>memory.UpdateResearchOpportunity(id,FinanceResearchOpportunityState.Started,nowUtc,run.RunId,"finance.research.scheduler.researchRunning",null)};}
        catch(AutonomousResearchBusyException){return memory.UpdateResearchOpportunity(id,FinanceResearchOpportunityState.Deferred,nowUtc,null,"finance.research.scheduler.researchBusy",nowUtc.AddMinutes(options.CheckIntervalMinutes));}
        catch(CurrentResearchEvidenceUnavailableException exception){var run=memory.AutonomousResearchRunByKey(id);return memory.UpdateResearchOpportunity(id,FinanceResearchOpportunityState.Failed,nowUtc,run?.RunId,exception.ReasonCode,null);}
        catch(OperationCanceledException)when(token.IsCancellationRequested){return memory.UpdateResearchOpportunity(id,FinanceResearchOpportunityState.Deferred,nowUtc,null,"finance.research.scheduler.shutdown",nowUtc.AddMinutes(options.CheckIntervalMinutes));}
        catch(Exception exception){var run=memory.AutonomousResearchRunByKey(id);var reason=run?.State==ResearchExperimentState.Failed?$"finance.research.scheduler.researchFailed.{run.FailureReason}":$"finance.research.scheduler.unexpected.{exception.GetType().Name}";return memory.UpdateResearchOpportunity(id,FinanceResearchOpportunityState.Failed,nowUtc,run?.RunId,reason,null);}
    }
    private FinanceResearchOpportunity Reconcile(FinanceResearchOpportunity opportunity,DateTimeOffset now){var run=memory.AutonomousResearchRunByKey(opportunity.OpportunityId);if(run is null)return opportunity;if(run.State==ResearchExperimentState.Completed)return memory.UpdateResearchOpportunity(opportunity.OpportunityId,FinanceResearchOpportunityState.Completed,now,run.RunId,"finance.research.scheduler.completed",null);if(run.State==ResearchExperimentState.Failed)return memory.UpdateResearchOpportunity(opportunity.OpportunityId,FinanceResearchOpportunityState.Failed,now,run.RunId,run.FailureReason,null);return memory.UpdateResearchOpportunity(opportunity.OpportunityId,FinanceResearchOpportunityState.Started,now,run.RunId,"finance.research.scheduler.researchRunning",null);}
    internal FinanceResearchSchedulerStatus Status(DateTimeOffset nowUtc){var last=memory.LatestResearchOpportunity();var next=!options.Enabled?null:last is{State:FinanceResearchOpportunityState.Deferred,NextEligibilityUtc:not null}?last.NextEligibilityUtc:FinanceResearchSchedule.NextDue(nowUtc,options.ScheduledUtcHour);var date=last?.ResearchDate??FinanceResearchSchedule.ResearchDate(FinanceResearchSchedule.DueFor(nowUtc,options.ScheduledUtcHour));var readiness=memory.ResearchDataReadiness(date);return new(nowUtc,options.Enabled,options.SchedulerVersion,next,last,last?.ResearchRunId,last?.State.ToString()??"NotRun",last?.Reason,memory.AutonomousResearchIsRunning(),"RESEARCH",0m,"NONE",readiness.IsReady,readiness.ReasonCode,readiness.CurrentInstruments.Count,readiness.ExpectedUniverse.Count);}
}

internal sealed class FinanceResearchSchedulerWorker(FinanceResearchSchedulerOptions options,FinanceResearchOrchestrator orchestrator,SystemRecoveryCoordinator recovery,TimeProvider clock,ILogger<FinanceResearchSchedulerWorker> logger):BackgroundService
{
    private static readonly Action<ILogger,string,Exception?> Started=LoggerMessage.Define<string>(LogLevel.Information,new EventId(9300,"FinanceResearchSchedulerStarted"),"Finance research scheduler {SchedulerVersion} started.");
    private static readonly Action<ILogger,string,string,Exception?> Outcome=LoggerMessage.Define<string,string>(LogLevel.Information,new EventId(9301,"FinanceResearchSchedulerOutcome"),"Finance research scheduler opportunity {OpportunityId}: {State}.");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if(!options.Enabled)return;await recovery.WaitUntilRecoveredAsync(stoppingToken);Started(logger,options.SchedulerVersion,null);using var timer=new PeriodicTimer(TimeSpan.FromMinutes(options.CheckIntervalMinutes),clock);string? lastLogged=null;
        while(!stoppingToken.IsCancellationRequested){var result=orchestrator.CheckAndRun(clock.GetUtcNow(),recovery.MayStartTimeSensitiveWork,stoppingToken);var signature=result is null?null:$"{result.OpportunityId}:{result.State}:{result.Reason}";if(result is not null&&signature!=lastLogged){Outcome(logger,result.OpportunityId,result.State.ToString(),null);lastLogged=signature;}if(!await timer.WaitForNextTickAsync(stoppingToken))break;}
    }
}
