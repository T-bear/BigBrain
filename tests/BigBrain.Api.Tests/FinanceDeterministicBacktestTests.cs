using BigBrain.Modules.Finance;
using System.Text.Json;

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
        Assert.Equal(90,fill.Quantity); Assert.Equal(110m,fill.ReferenceOpen); Assert.Equal(110.066m,fill.FillPrice);
        Assert.Equal(1m,fill.Commission); Assert.Equal(4.95m,fill.EstimatedSlippage); Assert.Equal(.99m,fill.EstimatedSpreadCost);
        Assert.Equal(93.06m,fill.CashAfter); Assert.Equal(10_893.06m,result.Metrics.FinalEquity);
        Assert.True(result.Metrics.NetReturn<result.Metrics.GrossReturn);
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

    [Fact]
    public void FixedProportionalSpreadAndSlippageAreExplicitAndAdverseOnBothSides()
    {
        var cost=new BacktestCostModel("combined","v2",0,0,30,FixedCommissionPerFill:2,
            ProportionalCommissionBasisPoints:10,AssumedFullSpreadBasisPoints:20);
        var result=Run(new EnterThenExitStrategy(),cost,Bars(100,110,120),[]);
        Assert.Equal(2,result.Fills.Count);var buy=result.Fills[0];var sell=result.Fills[1];
        Assert.Equal(110.44m,buy.FillPrice); Assert.Equal(119.52m,sell.FillPrice);
        Assert.True(buy.FillPrice>buy.ReferenceOpen); Assert.True(sell.FillPrice<sell.ReferenceOpen);
        Assert.Equal(29.7m,buy.EstimatedSlippage); Assert.Equal(9.9m,buy.EstimatedSpreadCost);
        Assert.Equal(32.4m,sell.EstimatedSlippage); Assert.Equal(10.8m,sell.EstimatedSpreadCost);
        Assert.Equal(11.9396m,buy.Commission); Assert.Equal(12.7568m,sell.Commission);
        Assert.Equal(20.7m,result.Metrics.TotalAssumedSpreadCost);
        Assert.Equal(62.1m,result.Metrics.TotalEstimatedSlippage);
        Assert.Equal(24.6964m,result.Fills.Sum(x=>x.Commission)); Assert.Equal(24.70m,result.Metrics.TotalCommissions);
        Assert.All(result.ExecutionAttempts!,x=>Assert.Equal("FILLED",x.Status));
    }

    [Fact]
    public void MissingExactNextSessionRejectsWithoutLookingAhead()
    {
        var bars=new[]{Bar(new(2026,1,2),100),Bar(new(2026,1,6),120)};
        var result=Run(new BuyAndHoldResearchStrategy(),BacktestCostModel.Zero,bars,[]);
        Assert.Empty(result.Fills);var attempt=Assert.Single(result.ExecutionAttempts!,x=>x.Reason=="MISSING_NEXT_SESSION_BAR");
        Assert.Equal(new DateOnly(2026,1,5),attempt.ExpectedFillSession);
        Assert.Equal("MISSING_NEXT_SESSION_BAR",attempt.Reason);
        Assert.Equal(2,result.Metrics.RejectedOrUnfilled);
    }

    [Fact]
    public void InvalidOpenAndInsufficientCashFailDeterministically()
    {
        var invalid=Run(new BuyAndHoldResearchStrategy(),BacktestCostModel.Zero,
            [Bar(new(2026,1,2),100),Bar(new(2026,1,5),0)],[]);
        Assert.Empty(invalid.Fills);Assert.Single(invalid.ExecutionAttempts!,x=>x.Reason=="INVALID_OPEN");

        var impossible=new BacktestCostModel("impossible","v2",0,0,0,FixedCommissionPerFill:20_000);
        var cash=Run(new BuyAndHoldResearchStrategy(),impossible,Bars(100,110),[]);
        Assert.Empty(cash.Fills);Assert.Contains(cash.ExecutionAttempts!,x=>x.Reason=="INSUFFICIENT_CASH");
        Assert.Equal(10_000m,cash.Metrics.FinalEquity);
    }

    [Fact]
    public void FrictionChangesEvidenceIdentityAndNetResultButNotSignalGeneration()
    {
        var strategy=new EnterThenExitStrategy();var bars=Bars(100,110,120);
        var zero=Run(strategy,BacktestCostModel.Zero,bars,[]);
        var friction=Run(strategy,BacktestCostModel.Conservative,bars,[]);
        Assert.NotEqual(zero.RunId,friction.RunId);Assert.True(friction.Metrics.NetReturn<zero.Metrics.NetReturn);
        Assert.Equal(zero.Events.Where(x=>x.Type=="STRATEGY_INTENT").Select(x=>x.Intent),
            friction.Events.Where(x=>x.Type=="STRATEGY_INTENT").Select(x=>x.Intent));
    }

    [Fact]
    public void LegacyCostJsonRemainsReadableWithoutInventingNewAssumptions()
    {
        var old=JsonSerializer.Deserialize<BacktestCostModel>("""{"Id":"conservative-cost","Version":"v1","CommissionPerShare":0.01,"MinimumCommission":1,"SlippageBasisPoints":5}""")!;
        Assert.Equal(0,old.FixedCommissionPerFill);Assert.Equal(0,old.ProportionalCommissionBasisPoints);
        Assert.Equal(0,old.AssumedFullSpreadBasisPoints);
    }

    private static BacktestResult Run(IResearchBacktestStrategy strategy,BacktestCostModel cost,IEnumerable<BacktestMarketBar> bars,IEnumerable<BacktestFeatureValue> features)
    {
        var config=new BacktestRunConfiguration(["market-1"],"feature-1",strategy.Identity,strategy.Parameters,DeterministicBacktestEngine.SimulationModel,cost,10_000,["US:XNAS:TEST"],new(2026,1,2),new(2026,1,30),DeterministicBacktestEngine.SizingPolicy,0,FillModel:BacktestFillModel.NextSessionOpen);
        return DeterministicBacktestEngine.Run(config,strategy,bars,features);
    }
    private static BacktestMarketBar[] Bars(params decimal[] opens)
    {
        var dates=new[]{new DateOnly(2026,1,2),new DateOnly(2026,1,5),new DateOnly(2026,1,6),new DateOnly(2026,1,7)};
        return opens.Select((x,i)=>new BacktestMarketBar(new("US:XNAS:TEST"),"market-1",dates[i],x,x,Knowledge.AddDays(i))).ToArray();
    }
    private static BacktestMarketBar Bar(DateOnly date,decimal open)=>new(new("US:XNAS:TEST"),"market-1",date,open,open,Knowledge.AddDays(date.Day-2));
    private static BacktestFeatureValue Feature(DateOnly date,string id,decimal value,DateTimeOffset? knowledge=null)=>new(new("US:XNAS:TEST"),date,id,value,knowledge??Knowledge.AddDays(date.Day-2),"feature-1");

    private sealed class EnterThenExitStrategy:IResearchBacktestStrategy
    {
        public StrategyIdentity Identity=>new("round-trip-test","v1");
        public IReadOnlyDictionary<string,decimal> Parameters{get;}=new Dictionary<string,decimal>();
        public ResearchStrategyIntent Evaluate(ResearchStrategyContext context)=>context.SessionDate==new DateOnly(2026,1,2)
            ?new(ResearchIntentKind.TargetLong,["test.enter"]):context.Portfolio.Quantity>0
                ?new(ResearchIntentKind.TargetFlat,["test.exit"]):new(ResearchIntentKind.NoAction,["test.done"]);
    }
}
