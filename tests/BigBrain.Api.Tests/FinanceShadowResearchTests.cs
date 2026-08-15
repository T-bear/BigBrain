using System.Text;
using BigBrain.Api.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceShadowResearchTests:IDisposable
{
    private readonly string _root=Path.Combine(Path.GetTempPath(),$"bigbrain-shadow-{Guid.NewGuid():N}");

    [Fact]
    public void PredictionIsExactlyOnceResearchOnlyAndClockGated()
    {
        var now=DateTimeOffset.UtcNow.AddMinutes(1);var memory=Ready(now);
        Assert.Equal(0,memory.RunShadowCycle(now,false));
        Assert.Equal(3,memory.RunShadowCycle(now,true));
        Assert.Equal(0,memory.RunShadowCycle(now,true));
        Assert.Equal(0,new EodhdMarketMemory(new EodhdFinanceOptions{Enabled=true,AccountActive=true,DatabasePath=Path.Combine(_root,"memory.db"),PayloadDirectory=Path.Combine(_root,"payloads")}).RunShadowCycle(now.AddMinutes(1),true));
        var catalog=memory.ShadowCatalog(null,null,null,null,null,50);
        Assert.Equal(3,catalog.Total);Assert.Equal("RESEARCH",catalog.OperatingMode);
        Assert.All(catalog.Predictions,p=>{Assert.Equal("RESEARCH",p.OperatingMode);Assert.Equal(FinanceShadowIdentity.Horizon,p.Horizon);Assert.True(p.ObservationKnowledgeUtc<=p.KnowledgeCutoffUtc);Assert.StartsWith("sha256:",p.ParameterFingerprint);});
    }

    [Fact]
    public void FutureObservationAppendsOutcomeWithoutRewritingPrediction()
    {
        var t0=DateTimeOffset.UtcNow.AddMinutes(1);var memory=Ready(t0);memory.RunShadowCycle(t0,true);
        var before=memory.ShadowCatalog(null,"momentum",null,null,null,50).Predictions.Single();
        var instrument=EodhdCatalog.Watchlist.Single(x=>x.Symbol=="AAPL");var t1=t0.AddDays(2);
        memory.Store(instrument,[new(new(2026,8,17),130,132,129,131,131,1000)],Encoding.UTF8.GetBytes("next"),new(2026,8,17),new(2026,8,17),t1.AddMinutes(-1),t1,0);
        memory.BuildFeatures();memory.RunShadowCycle(t1,true);memory.RunShadowCycle(t1.AddMinutes(1),true);
        var after=memory.ShadowPrediction(before.PredictionId)!;
        Assert.Equal(before.PredictionId,after.PredictionId);Assert.Equal(before.KnowledgeCutoffUtc,after.KnowledgeCutoffUtc);
        Assert.Equal(before.Signal,after.Signal);Assert.Equal(FinanceShadowState.Evaluated,after.State);Assert.Equal(before.ReasonCodes,after.ReasonCodes);
        Assert.Single(memory.ShadowCatalog(null,"momentum","Evaluated",null,null,50).Predictions);
    }

    [Fact]
    public void LateStartNeverBackfillsOldObservationAndQueriesAreBounded()
    {
        var acquired=DateTimeOffset.UtcNow.AddMinutes(1);var now=acquired.AddDays(5);var memory=Ready(acquired);
        Assert.Equal(0,memory.RunShadowCycle(now,true));Assert.Empty(memory.ShadowCatalog(null,null,null,null,null,50).Predictions);
        Assert.Throws<ArgumentException>(()=>memory.ShadowCatalog(null,null,null,null,null,201));
        Assert.Throws<ArgumentException>(()=>memory.ShadowPrediction("' OR 1=1 --"));
    }

    [Fact]
    public void CadenceScheduleSkipsWeekendAndOverviewUsesActualBreadthAndResearchSignals()
    {
        Assert.False(FinanceCadenceSchedule.IsProviderWindow(new(2026,8,15,23,0,0,TimeSpan.Zero),22));
        Assert.False(FinanceCadenceSchedule.IsProviderWindow(new(2026,8,17,21,59,0,TimeSpan.Zero),22));
        Assert.True(FinanceCadenceSchedule.IsProviderWindow(new(2026,8,17,22,0,0,TimeSpan.Zero),22));
        var now=DateTimeOffset.UtcNow.AddMinutes(1);var memory=Ready(now);memory.RunShadowCycle(now,true);memory.RecordCadenceCheck(now,false,false,"no-provider-check");
        var provider=new EodhdFinanceOptions{Enabled=true,AccountActive=true,ApiToken="fixture",DatabasePath=Path.Combine(_root,"memory.db"),PayloadDirectory=Path.Combine(_root,"payloads")};
        var overview=memory.Overview(provider,new FinanceCadenceOptions(),true);
        Assert.Equal("RESEARCH",overview.Mode);Assert.Equal("CURRENT EOD / PROSPECTIVE EOD",overview.ObservationClass);
        Assert.Contains("bevakade instrument",overview.MarketSummary);Assert.Single(overview.Signals);Assert.Equal(3,overview.Signals[0].StrategyCount);
        Assert.Equal(3,overview.Prospective.Valid);Assert.Equal(3,overview.Prospective.Pending);Assert.Equal(0,overview.Prospective.Evaluated);Assert.Empty(overview.Prospective.Curve);
        Assert.Equal("Healthy",overview.Cadence.Health);Assert.Null(overview.Cadence.LastProviderCheckUtc);Assert.Null(overview.Cadence.LastSuccessfulAcquisitionUtc);
        memory.RecordCadenceCheck(now,true,true,"provider-check-no-new-session");overview=memory.Overview(provider,new FinanceCadenceOptions(),true);
        Assert.Equal(now,overview.Cadence.LastProviderCheckUtc);Assert.Equal(now,overview.Cadence.LastSuccessfulAcquisitionUtc);Assert.True(overview.Cadence.ClockIntegrity);Assert.Equal("RESEARCH",overview.Cadence.OperatingMode);
    }

    private EodhdMarketMemory Ready(DateTimeOffset acquired)
    {
        var options=new EodhdFinanceOptions{Enabled=true,AccountActive=true,DatabasePath=Path.Combine(_root,"memory.db"),PayloadDirectory=Path.Combine(_root,"payloads")};
        var memory=new EodhdMarketMemory(options);var instrument=EodhdCatalog.Watchlist.Single(x=>x.Symbol=="AAPL");var bars=new List<EodhdDailyBar>();
        var date=new DateOnly(2026,7,13);for(var i=0;i<25;i++){while(date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)date=date.AddDays(1);var close=100+i;bars.Add(new(date,close-1,close+1,close-2,close,close,1000+i));date=date.AddDays(1);}
        memory.Store(instrument,bars,Encoding.UTF8.GetBytes("fixture-25"),bars[0].Date,bars[^1].Date,acquired.AddMinutes(-1),acquired,0);memory.BuildFeatures();return memory;
    }
    public void Dispose(){if(Directory.Exists(_root))Directory.Delete(_root,true);GC.SuppressFinalize(this);}
}
