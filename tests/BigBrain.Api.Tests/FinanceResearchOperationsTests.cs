using BigBrain.Api.Finance;
using BigBrain.Api.SystemRecovery;

namespace BigBrain.Api.Tests;

public sealed class FinanceResearchOperationsTests
{
    private static readonly DateTimeOffset Now=new(2026,8,22,12,0,0,TimeSpan.Zero);

    [Fact]
    public void DisabledIsHealthyWhileEnabledStaleRequiresAttention()
    {
        using var fixture=new Fixture();var disabled=fixture.Status(Now,new(){Enabled=false},new());Assert.Equal(FinanceResearchOperationalState.Disabled,disabled.State);Assert.False(disabled.RequiresAttention);
        var stale=fixture.Status(Now,new(){Enabled=true},new(){StaleSchedulerMinutes=60},Now.AddHours(-2));Assert.Equal(FinanceResearchOperationalState.Degraded,stale.State);Assert.True(stale.RequiresAttention);Assert.Equal("SCHEDULER_STALE",stale.CurrentActivity);
    }

    [Fact]
    public void OperationalFailuresAreDeduplicatedReachAttentionAndSuccessResetsStreak()
    {
        using var fixture=new Fixture();for(var i=0;i<3;i++){var opportunity=fixture.Opportunity(i);var failed=fixture.Memory.UpdateResearchOpportunity(opportunity.OpportunityId,FinanceResearchOpportunityState.Failed,Now.AddMinutes(i),null,"finance.research.scheduler.unexpected.SqliteException",null);fixture.Memory.RecordResearchOperationsEvaluation(Now.AddMinutes(i),failed);fixture.Memory.RecordResearchOperationsEvaluation(Now.AddMinutes(i),failed);}
        var attention=fixture.Status(Now.AddMinutes(5),new(){Enabled=true},new());Assert.Equal(FinanceResearchOperationalState.AttentionRequired,attention.State);Assert.Equal(3,attention.ConsecutiveOperationalFailures);Assert.Equal(3,fixture.Memory.ResearchOperationalIncidents(0,10).Total);
        var completed=fixture.Memory.UpdateResearchOpportunity(fixture.Opportunity(3).OpportunityId,FinanceResearchOpportunityState.Completed,Now.AddMinutes(6),"research-run-clean","finance.research.scheduler.completed",null);fixture.Memory.RecordResearchOperationsEvaluation(Now.AddMinutes(6),completed);var recovered=fixture.Status(Now.AddMinutes(7),new(){Enabled=true},new());Assert.Equal(0,recovered.ConsecutiveOperationalFailures);Assert.False(recovered.RequiresAttention);Assert.Equal(3,fixture.Memory.ResearchOperationalIncidents(0,10).Total);
    }

    [Fact]
    public void ScientificFailureAndTemporaryDeferralNeverIncrementOperationalFailures()
    {
        using var fixture=new Fixture();var scientific=fixture.Memory.UpdateResearchOpportunity(fixture.Opportunity(0).OpportunityId,FinanceResearchOpportunityState.Failed,Now,null,"finance.research.currentEvidenceIncomplete",null);fixture.Memory.RecordResearchOperationsEvaluation(Now,scientific);var deferred=fixture.Memory.UpdateResearchOpportunity(fixture.Opportunity(1).OpportunityId,FinanceResearchOpportunityState.Deferred,Now.AddMinutes(1),null,"finance.research.scheduler.resource.cpu",Now.AddHours(1));fixture.Memory.RecordResearchOperationsEvaluation(Now.AddMinutes(1),deferred);var status=fixture.Status(Now.AddMinutes(2),new(){Enabled=true},new());Assert.Equal(0,status.ConsecutiveOperationalFailures);Assert.Empty(fixture.Memory.ResearchOperationalIncidents(0,10).Incidents);Assert.Equal(FinanceResearchOperationalState.Deferred,status.State);
    }

    [Fact]
    public void PersistentDataAndResourceWaitBecomeVisibleWithoutBypass()
    {
        using var data=new Fixture();var old=data.Memory.UpdateResearchOpportunity(data.Opportunity(0,Now.AddHours(-25)).OpportunityId,FinanceResearchOpportunityState.Deferred,Now.AddHours(-25),null,"finance.research.scheduler.featuresNotReady",Now.AddHours(-24));data.Memory.RecordResearchOperationsEvaluation(Now,old);var dataStatus=data.Status(Now,new(){Enabled=true},new());Assert.Equal(FinanceResearchOperationalState.Degraded,dataStatus.State);Assert.Equal("DATA_WAIT_PERSISTENT",dataStatus.CurrentActivity);Assert.Empty(data.Memory.ResearchRuns(0,10).Runs);
        using var resource=new Fixture();var pressured=resource.Memory.UpdateResearchOpportunity(resource.Opportunity(0,Now.AddHours(-25)).OpportunityId,FinanceResearchOpportunityState.Deferred,Now.AddHours(-25),null,"finance.research.scheduler.resource.memory",Now.AddHours(-24));resource.Memory.RecordResearchOperationsEvaluation(Now,pressured);var resourceStatus=resource.Status(Now,new(){Enabled=true},new());Assert.Equal("RESOURCE_WAIT_PERSISTENT",resourceStatus.CurrentActivity);Assert.Empty(resource.Memory.ResearchRuns(0,10).Runs);
    }

    [Fact]
    public void ReconciliationIsIdempotentAndPreservesCompletedRunIdentity()
    {
        using var fixture=new Fixture();var opportunity=fixture.Opportunity(0);Assert.True(fixture.Memory.TryClaimResearchOpportunity(opportunity.OpportunityId,Now));fixture.Memory.ReconcileResearchOperations(Now.AddMinutes(1));fixture.Memory.ReconcileResearchOperations(Now.AddMinutes(1));var recovered=fixture.Memory.ResearchOpportunity(opportunity.OpportunityId)!;Assert.Equal(FinanceResearchOpportunityState.Deferred,recovered.State);Assert.Empty(fixture.Memory.ResearchRuns(0,10).Runs);Assert.Empty(fixture.Memory.ResearchExperiments(0,10,null,null,null,null,null).Experiments);
    }

    [Fact]
    public void ConfigurationAndIncidentPagingAreBounded()
    {
        Assert.Throws<InvalidOperationException>(()=>new FinanceResearchOperationsOptions{OperationsVersion="v0"}.Validate());Assert.Throws<InvalidOperationException>(()=>new FinanceResearchOperationsOptions{AttentionFailureThreshold=1}.Validate());Assert.Throws<InvalidOperationException>(()=>new FinanceResearchOperationsOptions{StaleSchedulerMinutes=59}.Validate());Assert.Throws<InvalidOperationException>(()=>new FinanceResearchOperationsOptions{PersistentWaitHours=0}.Validate());new FinanceResearchOperationsOptions().Validate();using var fixture=new Fixture();Assert.Throws<ArgumentException>(()=>fixture.Memory.ResearchOperationalIncidents(0,101));var completedAt=Now.AddMinutes(1);fixture.Memory.UpdateResearchOpportunity(fixture.Opportunity(0).OpportunityId,FinanceResearchOpportunityState.Completed,completedAt,"legacy-run","finance.research.scheduler.completed",null);Assert.Equal(completedAt,fixture.Status(Now.AddMinutes(2),new(){Enabled=false},new()).LastSuccessfulResearchUtc);
    }

    private sealed class Fixture:IDisposable
    {
        private readonly string root=Path.Combine(Path.GetTempPath(),"bb095-operations",Guid.NewGuid().ToString("N"));internal EodhdMarketMemory Memory{get;}
        internal Fixture(){var options=new EodhdFinanceOptions{DatabasePath=Path.Combine(root,"finance.db"),PayloadDirectory=Path.Combine(root,"payloads")};Memory=new(options);}
        internal FinanceResearchOpportunity Opportunity(int index,DateTimeOffset? created=null){var date=new DateOnly(2026,8,21).AddDays(index);var now=created??Now.AddMinutes(index);return Memory.CreateOrReadResearchOpportunity($"finance-research-scheduler-v1:{date:yyyy-MM-dd}",FinanceResearchSchedulerOptions.CurrentVersion,date,now,now);}
        internal FinanceResearchOperationsStatus Status(DateTimeOffset now,FinanceResearchSchedulerOptions scheduler,FinanceResearchOperationsOptions operations,DateTimeOffset? booted=null)=>Memory.ResearchOperationsStatus(now,operations,scheduler,Recovery(booted??Now),Cadence());
        public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    private static SystemRecoverySnapshot Recovery(DateTimeOffset booted)=>new(RuntimeLifecycleState.Healthy,"boot",booted,PreviousShutdownState.Clean,true,true,"fixture",100_000_000_000,false,null,Now,[],[],[],0,"RESEARCH");
    private static FinanceCadenceSnapshot Cadence()=>new(false,"EODHD","CURRENT EOD","Healthy",null,null,null,null,null,0,0,0,true,"disabled","fixture","RESEARCH");
}
