using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceDeterministicBacktestTests
{
    private static readonly DateTimeOffset Knowledge = new(2026,1,1,22,0,0,TimeSpan.Zero);

    [Fact]
    public void GoldenNextOpenFillUsesWholeSharesAndUpdatesCash()
    {
        var result=Run(new BuyAndHoldResearchStrategy(),BacktestCostModel.Conservative,Bars(100,110,120),[]);
        var fill=Assert.Single(result.Fills);
        Assert.Equal(new DateOnly(2026,1,2),fill.IntentSession); Assert.Equal(new DateOnly(2026,1,5),fill.FillSession);
        Assert.Equal(90,fill.Quantity); Assert.Equal(110m,fill.ReferenceOpen); Assert.Equal(110.055m,fill.FillPrice);
        Assert.Equal(1m,fill.Commission); Assert.Equal(4.95m,fill.EstimatedSlippage); Assert.Equal(94.05m,fill.CashAfter);
        Assert.Equal(10_894.05m,result.Metrics.FinalEquity); Assert.True(result.Metrics.NetReturn<result.Metrics.GrossReturn);
    }

    [Fact]
    public void SmaDecisionCannotSeeFutureFeatureAndCannotFillAtSameClose()
    {
        var strategy=new SmaCrossoverResearchStrategy(); var bars=Bars(100,110,120);
        var features=new[]{Feature(new DateOnly(2026,1,2),"sma.10",11),Feature(new DateOnly(2026,1,2),"sma.20",10),
            Feature(new DateOnly(2026,1,5),"sma.10",9,Knowledge.AddDays(20)),Feature(new DateOnly(2026,1,5),"sma.20",10,Knowledge.AddDays(20))};
        var result=Run(strategy,BacktestCostModel.Zero,bars,features);
        var fill=Assert.Single(result.Fills); Assert.Equal(new DateOnly(2026,1,5),fill.FillSession); Assert.Equal(110m,fill.FillPrice);
        Assert.DoesNotContain(result.Fills,x=>x.FillSession==x.IntentSession);
        Assert.Contains(result.Events,x=>x.Session==new DateOnly(2026,1,5)&&x.Intent==ResearchIntentKind.NoAction);
    }

    [Fact]
    public void FutureBarsDoNotAlterEarlierDecisionsAndRepeatedRunIsBitDeterministic()
    {
        var strategy=new MomentumResearchStrategy(); var feature=new[]{Feature(new DateOnly(2026,1,2),"momentum.20",1)};
        var first=Run(strategy,BacktestCostModel.Zero,Bars(100,110),feature);
        var second=Run(strategy,BacktestCostModel.Zero,Bars(100,110,999),feature);
        Assert.Equal(first.Events.Where(x=>x.Session<=new DateOnly(2026,1,5)).Select(x=>x.Intent),second.Events.Where(x=>x.Session<=new DateOnly(2026,1,5)).Select(x=>x.Intent));
        var repeated=Run(strategy,BacktestCostModel.Zero,Bars(100,110),feature);
        Assert.Equal(first.RunId,repeated.RunId); Assert.Equal(first.Checksum,repeated.Checksum); Assert.Equal(first.Fills,repeated.Fills); Assert.Equal(first.EquityCurve,repeated.EquityCurve);
    }

    [Fact]
    public void CostsDifferWithoutNegativeCashAndEndOfDataIntentRemainsUnfilled()
    {
        var strategy=new BuyAndHoldResearchStrategy(); var zero=Run(strategy,BacktestCostModel.Zero,Bars(100,10),[]);
        var costly=Run(strategy,new("cost","v1",10,100,500),Bars(100,10),[]);
        Assert.True(costly.Metrics.FinalEquity<zero.Metrics.FinalEquity); Assert.All(costly.Fills,x=>Assert.True(x.CashAfter>=0));
        var noNext=Run(strategy,BacktestCostModel.Zero,Bars(100),[]); Assert.Empty(noNext.Fills);
    }

    [Fact]
    public void MissingFeatureAndRepeatedSignalDoNotDuplicateFills()
    {
        var strategy=new MomentumResearchStrategy(); var features=new[]{Feature(new DateOnly(2026,1,5),"momentum.20",1,Knowledge.AddDays(1)),Feature(new DateOnly(2026,1,6),"momentum.20",1,Knowledge.AddDays(2))};
        var result=Run(strategy,BacktestCostModel.Zero,Bars(100,110,120,130),features);
        Assert.Single(result.Fills); Assert.Contains(result.Events,x=>x.Detail.Contains("warmup-or-unavailable"));
    }

    private static BacktestResult Run(IResearchBacktestStrategy strategy,BacktestCostModel cost,IEnumerable<BacktestMarketBar> bars,IEnumerable<BacktestFeatureValue> features)
    {
        var config=new BacktestRunConfiguration(["market-1"],"feature-1",strategy.Identity,strategy.Parameters,DeterministicBacktestEngine.SimulationModel,cost,10_000,["US:XNAS:TEST"],new(2026,1,2),new(2026,1,30),DeterministicBacktestEngine.SizingPolicy,0);
        return DeterministicBacktestEngine.Run(config,strategy,bars,features);
    }
    private static BacktestMarketBar[] Bars(params decimal[] opens)
    {
        var dates=new[]{new DateOnly(2026,1,2),new DateOnly(2026,1,5),new DateOnly(2026,1,6),new DateOnly(2026,1,7)};
        return opens.Select((x,i)=>new BacktestMarketBar(new("US:XNAS:TEST"),"market-1",dates[i],x,x,Knowledge.AddDays(i))).ToArray();
    }
    private static BacktestFeatureValue Feature(DateOnly date,string id,decimal value,DateTimeOffset? knowledge=null)=>new(new("US:XNAS:TEST"),date,id,value,knowledge??Knowledge.AddDays(date.Day-2),"feature-1");
}
