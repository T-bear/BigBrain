using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceRobustnessEvaluationTests
{
    [Theory]
    [InlineData(ChronologicalSplitRatio.SixtyForty,60,35)]
    [InlineData(ChronologicalSplitRatio.SeventyThirty,70,25)]
    [InlineData(ChronologicalSplitRatio.EightyTwenty,80,15)]
    public void ChronologicalSplitsApplyRatioAndEmbargoWithoutOverlap(ChronologicalSplitRatio ratio,int train,int test)
    {
        var sessions=Enumerable.Range(0,100).Select(i=>new DateOnly(2025,1,1).AddDays(i)).ToArray();
        var split=DeterministicRobustnessEvaluator.Split(sessions,ratio,5);
        Assert.Equal(train,split.TrainSessions);Assert.Equal(test,split.TestSessions);Assert.True(split.TrainTo<split.TestFrom);Assert.Equal(5,split.TestFrom.DayNumber-split.TrainTo.DayNumber-1);
    }

    [Fact]
    public void SplitRejectsReversedSessionsAndEmbargoThatConsumesTest()
    {
        var sessions=Enumerable.Range(0,10).Select(i=>new DateOnly(2025,1,1).AddDays(i)).Reverse().ToArray();
        Assert.Throws<ArgumentException>(()=>DeterministicRobustnessEvaluator.Split(sessions,ChronologicalSplitRatio.SeventyThirty,0));
        Assert.Throws<ArgumentException>(()=>DeterministicRobustnessEvaluator.Split(sessions.Reverse().ToArray(),ChronologicalSplitRatio.EightyTwenty,2));
    }

    [Fact]
    public void ParameterGridIsBoundedValidAndDeterministicallyOrdered()
    {
        var sma=DeterministicRobustnessEvaluator.ParameterVariants("sma-crossover");var momentum=DeterministicRobustnessEvaluator.ParameterVariants("momentum");
        Assert.Equal(4,sma.Count);Assert.All(sma,x=>Assert.True(x["fastPeriod"]<x["slowPeriod"]));
        Assert.Equal([5m,10m,20m],momentum.Select(x=>x["period"]));Assert.Empty(DeterministicRobustnessEvaluator.ParameterVariants("buy-and-hold"));
        Assert.Equal(sma,DeterministicRobustnessEvaluator.ParameterVariants("sma-crossover"));
    }

    [Fact]
    public void IsolatedReferencePeakIsClassifiedAsFragile()
    {
        var reference=new Dictionary<string,decimal>{{"period",20}};
        var points=new[]{Point(15,0.01m),Point(20,0.20m),Point(25,0.00m)};
        var result=DeterministicRobustnessEvaluator.SummarizeParameterSensitivity(points,reference,new("v1",1,1,1,.05m,.10m,70));
        Assert.Equal(ParameterStabilityVerdict.FragileIsolatedPeak,result.Verdict);
    }

    [Fact]
    public void EvaluationIsDeterministicInsufficientAndCostsAreMonotonic()
    {
        var fixture=Fixture(252);var strategy=new MomentumResearchStrategy();var plan=Plan(strategy,fixture.Bars);
        var first=DeterministicRobustnessEvaluator.Evaluate(plan,strategy,fixture.Bars,fixture.Features);
        var second=DeterministicRobustnessEvaluator.Evaluate(plan,strategy,fixture.Bars,fixture.Features);
        Assert.Equal(first.Evaluation.EvaluationId,second.Evaluation.EvaluationId);Assert.Equal(first.Evaluation.Checksum,second.Evaluation.Checksum);
        Assert.Equal(first.Evaluation.UnderlyingRunIds,second.Evaluation.UnderlyingRunIds);Assert.Equal(RobustnessVerdict.InsufficientData,first.Evaluation.Verdict);
        Assert.Equal(3,first.Evaluation.WalkForwardWindows.Count);Assert.Equal(3,first.Evaluation.ParameterVariantsEvaluated);
        Assert.True(first.Evaluation.CostSensitivity.Points.Zip(first.Evaluation.CostSensitivity.Points.Skip(1),(a,b)=>a.NetReturn>=b.NetReturn).All(x=>x));
    }

    [Fact]
    public void FutureTestDataCannotAlterTrainMetricsOrEarlierWalkForwardWindow()
    {
        var fixture=Fixture(252);var strategy=new MomentumResearchStrategy();var plan=Plan(strategy,fixture.Bars);
        var original=DeterministicRobustnessEvaluator.Evaluate(plan,strategy,fixture.Bars,fixture.Features).Evaluation;
        var split=DeterministicRobustnessEvaluator.Split(fixture.Bars.Select(x=>x.SessionDate).Distinct().Order().ToArray(),plan.SplitRatio,plan.EmbargoSessions);
        var changedBars=fixture.Bars.Select(x=>x.SessionDate>=split.TestFrom?x with{Open=x.Open*5,Close=x.Close*5}:x).ToArray();
        var changedFeatures=fixture.Features.Select(x=>x.SessionDate>=split.TestFrom?x with{Value=x.Value is null?null:-x.Value}:x).ToArray();
        var changed=DeterministicRobustnessEvaluator.Evaluate(plan,strategy,changedBars,changedFeatures).Evaluation;
        Assert.Equal(original.PrimarySplit.Train,changed.PrimarySplit.Train);
        Assert.Equal(original.WalkForwardWindows[0].TrainRunId,changed.WalkForwardWindows[0].TrainRunId);
        Assert.Equal(original.WalkForwardWindows[0].TestRunId,changed.WalkForwardWindows[0].TestRunId);
        Assert.Equal(original.ParameterSensitivity.Points.Select(x=>x.TrainNetReturn),changed.ParameterSensitivity.Points.Select(x=>x.TrainNetReturn));
    }

    [Fact]
    public void FutureKnowledgeFeatureRemainsInvisibleAndShortDataCannotClaimRobustness()
    {
        var fixture=Fixture(90,futureKnowledge:true);var strategy=new MomentumResearchStrategy();var plan=Plan(strategy,fixture.Bars) with{EmbargoSessions=5,WalkForward=new(40,10,10,true)};
        var build=DeterministicRobustnessEvaluator.Evaluate(plan,strategy,fixture.Bars,fixture.Features);var result=build.Evaluation;
        Assert.Equal(RobustnessVerdict.InsufficientData,result.Verdict);Assert.Contains(result.VerdictReasons,x=>x=="data.insufficient");
        Assert.All(build.UnderlyingRuns.Where(x=>x.Configuration.Strategy.Id=="momentum"),x=>Assert.Empty(x.Fills));
    }

    private static ParameterSensitivityPoint Point(decimal period,decimal result)=>new(new Dictionary<string,decimal>{{"period",period}},"train","test",result,result,-.01m,.01m);

    private static EvaluationPlan Plan(IResearchBacktestStrategy strategy,IReadOnlyList<BacktestMarketBar> bars)=>DeterministicRobustnessEvaluator.CreatePlan(["market-1"],"feature-1",strategy,["US:XNAS:TEST"],bars.Min(x=>x.SessionDate),bars.Max(x=>x.SessionDate));
    private static (BacktestMarketBar[] Bars,BacktestFeatureValue[] Features) Fixture(int sessions,bool futureKnowledge=false)
    {
        var start=new DateOnly(2025,1,1);var bars=new List<BacktestMarketBar>();var features=new List<BacktestFeatureValue>();
        for(var i=0;i<sessions;i++){var date=start.AddDays(i);var price=100m+i*.1m+(i%17-8)*.2m;var knowledge=new DateTimeOffset(date.ToDateTime(new TimeOnly(22,0)),TimeSpan.Zero);bars.Add(new(new("US:XNAS:TEST"),"market-1",date,price,price,knowledge));
            foreach(var period in new[]{5,10,20,50})features.Add(new(new("US:XNAS:TEST"),date,$"sma.{period}",i>=period?price-(period==5?-.2m:.1m):null,futureKnowledge?knowledge.AddDays(500):knowledge,"feature-1"));
            foreach(var period in new[]{5,10,20})features.Add(new(new("US:XNAS:TEST"),date,$"momentum.{period}",i>=period?(i%40<25?1m:-1m):null,futureKnowledge?knowledge.AddDays(500):knowledge,"feature-1"));}
        return(bars.ToArray(),features.ToArray());
    }
}
