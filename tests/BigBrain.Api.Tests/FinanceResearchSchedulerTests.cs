using BigBrain.Api.Finance;
using BigBrain.Api.SystemRecovery;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;

namespace BigBrain.Api.Tests;

public sealed class FinanceResearchSchedulerTests
{
    [Fact]
    public void DueOpportunityRunsOnceAndRepeatedChecksAndRestartReconcile()
    {
        using var fixture=Ready();var options=Enabled();var orchestrator=new FinanceResearchOrchestrator(options,fixture.Memory);var now=Due(fixture);var riskPolicy=fixture.Memory.RiskPolicySnapshot();
        var first=Assert.IsType<FinanceResearchOpportunity>(orchestrator.CheckAndRun(now,true,CancellationToken.None));var repeated=Assert.IsType<FinanceResearchOpportunity>(orchestrator.CheckAndRun(now.AddMinutes(5),true,CancellationToken.None));
        Assert.Equal(FinanceResearchOpportunityState.Completed,first.State);Assert.Equal(first.OpportunityId,repeated.OpportunityId);Assert.Equal(first.ResearchRunId,repeated.ResearchRunId);Assert.Equal("finance-research-scheduler-v1:"+fixture.LastSession.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),first.OpportunityId);
        Assert.Equal(2,fixture.Memory.AutonomousResearchSnapshot().TotalExperiments);Assert.Single(fixture.Memory.ResearchSchedulerHistory(0,10).Opportunities);
        var safety=fixture.Memory.Snapshot(false,false,false).Safety;Assert.Equal(FinanceOperatingMode.Research,safety.Mode);Assert.False(safety.LiveTradingEnabled);Assert.False(safety.PaperTradingEnabled);Assert.False(safety.BrokerConnected);Assert.Equal(riskPolicy,fixture.Memory.RiskPolicySnapshot());
        var reopened=new EodhdMarketMemory(fixture.Options);var afterRestart=new FinanceResearchOrchestrator(options,reopened).CheckAndRun(now.AddMinutes(10),true,CancellationToken.None);Assert.Equal(first.ResearchRunId,afterRestart!.ResearchRunId);Assert.Equal(2,reopened.AutonomousResearchSnapshot().TotalExperiments);
    }

    [Fact]
    public void MissedDaysCreateOnlyCurrentOpportunityWithoutCatchUpStorm()
    {
        using var fixture=Ready();var orchestrator=new FinanceResearchOrchestrator(Enabled(),fixture.Memory);var now=Due(fixture).AddDays(10);var result=orchestrator.CheckAndRun(now,true,CancellationToken.None);var history=fixture.Memory.ResearchSchedulerHistory(0,100);
        Assert.NotNull(result);Assert.Single(history.Opportunities);Assert.Equal(DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1),history.Opportunities[0].ResearchDate);Assert.DoesNotContain(history.Opportunities,x=>x.ResearchDate< DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1));
    }

    [Fact]
    public async Task ActiveManualResearchDefersScheduledOpportunityWithoutSecondRun()
    {
        using var fixture=Ready();using var acquired=new ManualResetEventSlim();using var release=new ManualResetEventSlim();fixture.Memory.AutonomousResearchTestHook=phase=>{if(phase=="lease-acquired"){acquired.Set();release.Wait(TimeSpan.FromSeconds(10),TestContext.Current.CancellationToken);}};
        var manual=Task.Run(()=>fixture.Memory.RunAutonomousResearch("manual-active",2),TestContext.Current.CancellationToken);Assert.True(acquired.Wait(TimeSpan.FromSeconds(10),TestContext.Current.CancellationToken));var scheduled=new FinanceResearchOrchestrator(Enabled(),fixture.Memory).CheckAndRun(Due(fixture),true,CancellationToken.None);release.Set();await manual;
        Assert.Equal(FinanceResearchOpportunityState.Deferred,scheduled!.State);Assert.Equal("finance.research.scheduler.researchBusy",scheduled.Reason);Assert.Equal(1,fixture.Memory.ResearchRuns(0,10).Total);
    }

    [Fact]
    public void CurrentEvidenceUnavailableIsDurableAndDoesNotRetryOrCreateExperiments()
    {
        using var fixture=Ready();var options=Enabled();var feature=fixture.Memory.BuildFeatures();fixture.Memory.AutonomousResearchTestHook=phase=>{if(phase!="robustness-built")return;using var c=Open(fixture);using var x=c.CreateCommand();x.CommandText="DELETE FROM robustness_evaluations WHERE feature_revision_id=$feature AND strategy_id='sma-crossover'";x.Parameters.AddWithValue("$feature",feature.RevisionId);x.ExecuteNonQuery();};var orchestrator=new FinanceResearchOrchestrator(options,fixture.Memory);var now=Due(fixture);
        var failed=orchestrator.CheckAndRun(now,true,CancellationToken.None);fixture.Memory.AutonomousResearchTestHook=null;var repeated=orchestrator.CheckAndRun(now.AddMinutes(options.CheckIntervalMinutes),true,CancellationToken.None);
        Assert.Equal(FinanceResearchOpportunityState.Failed,failed!.State);Assert.Equal("finance.research.currentEvidenceIncomplete",failed.Reason);Assert.Equal(failed,repeated);Assert.Empty(fixture.Memory.ResearchExperiments(0,10,null,null,null,null,null).Experiments);Assert.Equal(ResearchExperimentState.Failed,fixture.Memory.ResearchRun(failed.ResearchRunId!)!.State);
    }

    [Fact]
    public void RecoveryAndDataReadinessDeferUntilNextBoundedCheck()
    {
        using var fixture=Ready();var options=Enabled();var orchestrator=new FinanceResearchOrchestrator(options,fixture.Memory);var now=Due(fixture);var recovery=orchestrator.CheckAndRun(now,false,CancellationToken.None);
        Assert.Equal(FinanceResearchOpportunityState.Deferred,recovery!.State);Assert.Equal("finance.research.scheduler.recoveryNotReady",recovery.Reason);Assert.Null(recovery.ResearchRunId);
        var early=orchestrator.CheckAndRun(now.AddMinutes(1),true,CancellationToken.None);Assert.Equal(recovery.NextEligibilityUtc,early!.NextEligibilityUtc);Assert.Null(early.ResearchRunId);
        var completed=orchestrator.CheckAndRun(now.AddMinutes(options.CheckIntervalMinutes),true,CancellationToken.None);Assert.Equal(FinanceResearchOpportunityState.Completed,completed!.State);
    }

    [Fact]
    public void InterruptedClaimWithoutResearchRunBecomesDeferredOnReopen()
    {
        using var fixture=Ready();var options=Enabled();var now=Due(fixture);var id=FinanceResearchSchedule.OpportunityId(options.SchedulerVersion,fixture.LastSession);fixture.Memory.CreateOrReadResearchOpportunity(id,options.SchedulerVersion,fixture.LastSession,FinanceResearchSchedule.DueFor(now,options.ScheduledUtcHour),now);Assert.True(fixture.Memory.TryClaimResearchOpportunity(id,now));
        var reopened=new EodhdMarketMemory(fixture.Options);Assert.Equal(FinanceResearchOpportunityState.Deferred,reopened.ResearchOpportunity(id)!.State);var completed=new FinanceResearchOrchestrator(options,reopened).CheckAndRun(now.AddMinutes(1),true,CancellationToken.None);Assert.Equal(FinanceResearchOpportunityState.Completed,completed!.State);
    }

    [Fact]
    public void RestartReconcilesCompletedResearchBeforeJournalCompletion()
    {
        using var fixture=Ready();var options=Enabled();var now=Due(fixture);var id=FinanceResearchSchedule.OpportunityId(options.SchedulerVersion,fixture.LastSession);fixture.Memory.CreateOrReadResearchOpportunity(id,options.SchedulerVersion,fixture.LastSession,FinanceResearchSchedule.DueFor(now,options.ScheduledUtcHour),now);Assert.True(fixture.Memory.TryClaimResearchOpportunity(id,now));var run=fixture.Memory.RunAutonomousResearch(id,2);Assert.Equal(ResearchExperimentState.Completed,run.State);
        var reopened=new EodhdMarketMemory(fixture.Options);var reconciled=new FinanceResearchOrchestrator(options,reopened).CheckAndRun(now.AddMinutes(2),true,CancellationToken.None);Assert.Equal(FinanceResearchOpportunityState.Completed,reconciled!.State);Assert.Equal(run.RunId,reconciled.ResearchRunId);Assert.Equal(2,reopened.AutonomousResearchSnapshot().TotalExperiments);
    }

    [Fact]
    public async Task DisabledDefaultAndCancellationNeverCreateResearchOrFailure()
    {
        using var fixture=Ready();var disabled=new FinanceResearchSchedulerOptions();var orchestrator=new FinanceResearchOrchestrator(disabled,fixture.Memory);Assert.Null(orchestrator.CheckAndRun(Due(fixture),true,CancellationToken.None));Assert.Empty(fixture.Memory.ResearchSchedulerHistory(0,10).Opportunities);
        var recoveryRoot=Path.Combine(Path.GetTempPath(),"bb093-recovery",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(recoveryRoot);try{var recovery=new SystemRecoveryCoordinator(new(){DatabasePath=Path.Combine(recoveryRoot,"recovery.db"),ClockSyncDirectory=recoveryRoot,LowDiskWarningBytes=0,LowDiskCriticalBytes=0},NullLogger<SystemRecoveryCoordinator>.Instance);var worker=new FinanceResearchSchedulerWorker(disabled,orchestrator,recovery,TimeProvider.System,NullLogger<FinanceResearchSchedulerWorker>.Instance);await worker.StartAsync(CancellationToken.None);await worker.StopAsync(CancellationToken.None);Assert.Empty(fixture.Memory.ResearchRuns(0,10).Runs);}finally{Directory.Delete(recoveryRoot,true);}
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();Assert.Throws<OperationCanceledException>(()=>new FinanceResearchOrchestrator(Enabled(),fixture.Memory).CheckAndRun(Due(fixture),true,cancelled.Token));Assert.Empty(fixture.Memory.ResearchSchedulerHistory(0,10).Opportunities);
    }

    [Fact]
    public void ConfigurationBoundsAndStatusSafetyAreExplicit()
    {
        Assert.Throws<InvalidOperationException>(()=>new FinanceResearchSchedulerOptions{SchedulerVersion="v0"}.Validate());Assert.Throws<InvalidOperationException>(()=>new FinanceResearchSchedulerOptions{CheckIntervalMinutes=1}.Validate());Assert.Throws<InvalidOperationException>(()=>new FinanceResearchSchedulerOptions{ScheduledUtcHour=24}.Validate());Assert.Throws<InvalidOperationException>(()=>new FinanceResearchSchedulerOptions{MaximumExperimentsPerRun=0}.Validate());Assert.Throws<InvalidOperationException>(()=>new FinanceResearchSchedulerOptions{MaximumExperimentsPerRun=FinanceResearchContracts.MaximumTotalExperimentsPerRun+1}.Validate());
        using var fixture=Ready();var status=new FinanceResearchOrchestrator(Enabled(),fixture.Memory).Status(Due(fixture));Assert.Equal("RESEARCH",status.OperatingMode);Assert.Equal(0m,status.BudgetSek);Assert.Equal("NONE",status.ExecutionAuthority);Assert.False(status.ResearchCurrentlyRunning);
    }

    [Fact]
    public void PartialUniverseDefersWithoutResearchThenSameOpportunityProceedsWhenCoherent()
    {
        using var fixture=Ready();using(var c=Open(fixture)){using var x=c.CreateCommand();x.CommandText="DELETE FROM observations WHERE instrument_id='US:XNYS:XOM' AND session_date=$date";x.Parameters.AddWithValue("$date",fixture.LastSession.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));x.ExecuteNonQuery();}
        var options=Enabled();var orchestrator=new FinanceResearchOrchestrator(options,fixture.Memory);var now=Due(fixture);var deferred=orchestrator.CheckAndRun(now,true,CancellationToken.None);
        Assert.Equal(FinanceResearchOpportunityState.Deferred,deferred!.State);Assert.Equal("finance.research.scheduler.universeIncomplete",deferred.Reason);Assert.Empty(fixture.Memory.ResearchRuns(0,10).Runs);Assert.Empty(fixture.Memory.ResearchExperiments(0,10,null,null,null,null,null).Experiments);
        Store(fixture,EodhdCatalog.Watchlist.Single(x=>x.Symbol=="XOM"),fixture.Bars,"restored",now);fixture.Memory.BuildFeatures();var completed=orchestrator.CheckAndRun(now.AddMinutes(options.CheckIntervalMinutes),true,CancellationToken.None);
        Assert.Equal(FinanceResearchOpportunityState.Completed,completed!.State);Assert.Equal(deferred.OpportunityId,completed.OpportunityId);Assert.Single(fixture.Memory.ResearchRuns(0,10).Runs);
    }

    [Fact]
    public void CurrentMarketRequiresMatchingCurrentFeatureGeneration()
    {
        using var fixture=Ready();var next=NextSession(fixture.LastSession);var bars=new[]{new EodhdDailyBar(next,150m,151m,149m,150.5m,150.5m,2000)};var acquired=new DateTimeOffset(next.AddDays(1),new TimeOnly(0,30),TimeSpan.Zero);foreach(var instrument in EodhdCatalog.Watchlist)Store(fixture,instrument,bars,"advance-"+instrument.Symbol,acquired);
        var options=Enabled();var orchestrator=new FinanceResearchOrchestrator(options,fixture.Memory);var now=new DateTimeOffset(next.AddDays(1),new TimeOnly(3,0),TimeSpan.Zero);var deferred=orchestrator.CheckAndRun(now,true,CancellationToken.None);
        Assert.Equal("finance.research.scheduler.featuresNotReady",deferred!.Reason);Assert.Empty(fixture.Memory.ResearchRuns(0,10).Runs);
        fixture.Memory.BuildFeatures();var completed=orchestrator.CheckAndRun(now.AddMinutes(options.CheckIntervalMinutes),true,CancellationToken.None);Assert.Equal(FinanceResearchOpportunityState.Completed,completed!.State);Assert.Equal(deferred.OpportunityId,completed.OpportunityId);
    }

    [Fact]
    public void FeatureLineageMismatchFailsClosedDeterministically()
    {
        using var fixture=Ready();using(var c=Open(fixture)){using var x=c.CreateCommand();x.CommandText="UPDATE feature_revisions SET source_revisions_json='[]'";x.ExecuteNonQuery();}
        var readiness=fixture.Memory.ResearchDataReadiness(fixture.LastSession);var again=fixture.Memory.ResearchDataReadiness(fixture.LastSession);
        Assert.False(readiness.IsReady);Assert.Equal("finance.research.scheduler.featureLineageIncomplete",readiness.ReasonCode);Assert.Equal(readiness.ReasonCode,again.ReasonCode);Assert.Equal(readiness.FeatureRevisionId,again.FeatureRevisionId);Assert.Equal(readiness.CurrentInstruments,again.CurrentInstruments);
        var result=new FinanceResearchOrchestrator(Enabled(),fixture.Memory).CheckAndRun(Due(fixture),true,CancellationToken.None);Assert.Equal(FinanceResearchOpportunityState.Deferred,result!.State);Assert.Empty(fixture.Memory.ResearchRuns(0,10).Runs);
    }

    [Fact]
    public void DeferredPriorDateIsExplicitlySupersededWithoutCatchUp()
    {
        using var fixture=Ready();using(var c=Open(fixture)){using var x=c.CreateCommand();x.CommandText="DELETE FROM observations WHERE instrument_id='US:XNYS:XOM' AND session_date=$date";x.Parameters.AddWithValue("$date",fixture.LastSession.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));x.ExecuteNonQuery();}
        var orchestrator=new FinanceResearchOrchestrator(Enabled(),fixture.Memory);var first=orchestrator.CheckAndRun(Due(fixture),true,CancellationToken.None);Assert.Equal(FinanceResearchOpportunityState.Deferred,first!.State);
        _=orchestrator.CheckAndRun(Due(fixture).AddDays(1),true,CancellationToken.None);var old=fixture.Memory.ResearchOpportunity(first.OpportunityId)!;Assert.Equal(FinanceResearchOpportunityState.Skipped,old.State);Assert.Equal("finance.research.scheduler.superseded",old.Reason);Assert.NotNull(old.CompletedAtUtc);
    }

    private static FinanceResearchSchedulerOptions Enabled()=>new(){Enabled=true,CheckIntervalMinutes=60,ScheduledUtcHour=2,MaximumExperimentsPerRun=2};
    private static DateTimeOffset Due(Fixture fixture)=>new(fixture.LastSession.AddDays(1),new TimeOnly(3,0),TimeSpan.Zero);
    private static Fixture Ready()
    {
        var root=Path.Combine(Path.GetTempPath(),"bb093-scheduler",Guid.NewGuid().ToString("N"));var options=new EodhdFinanceOptions{DatabasePath=Path.Combine(root,"finance.db"),PayloadDirectory=Path.Combine(root,"payloads")};var memory=new EodhdMarketMemory(options);var bars=new List<EodhdDailyBar>();var date=new DateOnly(2025,1,2);
        for(var i=0;i<280;i++){while(date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)date=date.AddDays(1);var close=100m+i*.1m+(i%17-8)*.2m;bars.Add(new(date,close-.1m,close+.5m,close-.5m,close,close,1000+i));date=date.AddDays(1);}var acquired=new DateTimeOffset(2026,8,22,12,0,0,TimeSpan.Zero);foreach(var instrument in EodhdCatalog.Watchlist)memory.Store(instrument,bars,System.Text.Encoding.UTF8.GetBytes("bb093-fixture-"+instrument.Symbol),bars[0].Date,bars[^1].Date,acquired.AddMinutes(-1),acquired,0);memory.BuildFeatures();return new(root,options,memory,bars[^1].Date,bars);
    }
    private static DateOnly NextSession(DateOnly date){do date=date.AddDays(1);while(!UsMarketCalendar.IsSession(date));return date;}
    private static void Store(Fixture fixture,EodhdInstrument instrument,IReadOnlyList<EodhdDailyBar> bars,string suffix,DateTimeOffset acquired)=>fixture.Memory.Store(instrument,bars,System.Text.Encoding.UTF8.GetBytes("bb093-"+suffix),bars[0].Date,bars[^1].Date,acquired.AddMinutes(-1),acquired,0);
    private static SqliteConnection Open(Fixture fixture){var c=new SqliteConnection($"Data Source={fixture.Options.DatabasePath}");c.Open();return c;}
    private sealed record Fixture(string Root,EodhdFinanceOptions Options,EodhdMarketMemory Memory,DateOnly LastSession,IReadOnlyList<EodhdDailyBar> Bars):IDisposable{public void Dispose(){if(Directory.Exists(Root))Directory.Delete(Root,true);}}
}
