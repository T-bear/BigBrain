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

    private static FinanceResearchSchedulerOptions Enabled()=>new(){Enabled=true,CheckIntervalMinutes=60,ScheduledUtcHour=2,MaximumExperimentsPerRun=2};
    private static DateTimeOffset Due(Fixture fixture)=>new(fixture.LastSession.AddDays(1),new TimeOnly(3,0),TimeSpan.Zero);
    private static Fixture Ready()
    {
        var root=Path.Combine(Path.GetTempPath(),"bb093-scheduler",Guid.NewGuid().ToString("N"));var options=new EodhdFinanceOptions{DatabasePath=Path.Combine(root,"finance.db"),PayloadDirectory=Path.Combine(root,"payloads")};var memory=new EodhdMarketMemory(options);var instrument=EodhdCatalog.Watchlist.Single(x=>x.Symbol=="AAPL");var bars=new List<EodhdDailyBar>();var date=new DateOnly(2025,1,2);
        for(var i=0;i<280;i++){while(date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)date=date.AddDays(1);var close=100m+i*.1m+(i%17-8)*.2m;bars.Add(new(date,close-.1m,close+.5m,close-.5m,close,close,1000+i));date=date.AddDays(1);}var acquired=new DateTimeOffset(2026,8,22,12,0,0,TimeSpan.Zero);memory.Store(instrument,bars,System.Text.Encoding.UTF8.GetBytes("bb093-fixture"),bars[0].Date,bars[^1].Date,acquired.AddMinutes(-1),acquired,0);memory.BuildFeatures();return new(root,options,memory,bars[^1].Date);
    }
    private static SqliteConnection Open(Fixture fixture){var c=new SqliteConnection($"Data Source={fixture.Options.DatabasePath}");c.Open();return c;}
    private sealed record Fixture(string Root,EodhdFinanceOptions Options,EodhdMarketMemory Memory,DateOnly LastSession):IDisposable{public void Dispose(){if(Directory.Exists(Root))Directory.Delete(Root,true);}}
}
