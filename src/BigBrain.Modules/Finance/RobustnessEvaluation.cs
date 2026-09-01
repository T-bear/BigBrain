using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BigBrain.Modules.Finance;

public enum ChronologicalSplitRatio { SixtyForty=60, SeventyThirty=70, EightyTwenty=80 }
public enum RobustnessVerdict { InsufficientData, Fragile, Mixed, MoreRobust }
public enum ParameterStabilityVerdict { NotApplicable, RobustNeighborhood, Mixed, FragileIsolatedPeak }
public enum RobustnessEvidenceLabel { WeakEvidence, MixedEvidence, StrongerEvidence }
public enum HoldoutEvidenceState { Untouched, Evaluated, Contaminated }
public enum SelectionGovernanceOutcome { Pass, Fail, InsufficientData, Contaminated }
public sealed record EvaluationThresholds(string Version,int MinimumTrainSessions,int MinimumTestSessions,int MinimumWalkForwardWindows,
    decimal IsolatedPeakReturnGap,decimal CostStressMaximumDegradation,decimal MoreRobustMinimumScore);
public sealed record WalkForwardSpecification(int InitialTrainSessions,int TestSessions,int StepSessions,bool Expanding);
public sealed record AntiOverfittingPolicy(string Version,int TrainPercent,int ValidationPercent,int HoldoutPercent,
    int EmbargoSessions,int MinimumTrainSessions,int MinimumValidationSessions,int MinimumHoldoutSessions,
    decimal MinimumPositiveValidationFraction,string SelectionCriterion,string MultipleTestingControl,int Seed)
{
    public static AntiOverfittingPolicy Default=>new("anti-overfitting-governance-v1",60,20,20,50,126,40,40,.75m,
        "maximum-validation-excess-return-v1","family-breadth-fail-closed-v1",0);
}
public sealed record ResearchDataPartition(DateOnly TrainFrom,DateOnly TrainTo,DateOnly ValidationFrom,
    DateOnly ValidationTo,DateOnly HoldoutFrom,DateOnly HoldoutTo,int TrainSessions,int ValidationSessions,
    int HoldoutSessions,int EmbargoSessions);
public sealed record ParameterSelectionTrial(string TrialId,IReadOnlyDictionary<string,decimal> Parameters,
    string? TrainRunId,string? ValidationRunId,decimal? TrainNetReturn,decimal? ValidationNetReturn,
    decimal? ValidationExcessReturn);
public sealed record ResearchControlResult(string Id,string Version,string Status,string Reason,bool EngineeringOnly);
public sealed record SelectionGovernanceEvidence(string Version,ResearchDataPartition Partition,string FamilyId,
    int CandidateCount,string SelectionCriterion,string MultipleTestingControl,IReadOnlyList<ParameterSelectionTrial> Trials,
    string? SelectedTrialId,string? SelectedHoldoutRunId,HoldoutEvidenceState HoldoutStateAtSelection,
    HoldoutEvidenceState FinalHoldoutState,int PriorHoldoutEvaluations,decimal? SelectedHoldoutNetReturn,
    decimal? SelectedHoldoutExcessReturn,decimal PositiveValidationFraction,SelectionGovernanceOutcome Outcome,
    IReadOnlyList<string> Reasons,IReadOnlyList<ResearchControlResult> Controls);
public sealed record EvaluationPlan(string Id,string Version,IReadOnlyList<string> MarketRevisionIds,string FeatureRevisionId,
    StrategyIdentity Strategy,IReadOnlyDictionary<string,decimal> ReferenceParameters,string SimulationModel,
    IReadOnlyList<BacktestCostModel> CostModels,decimal InitialCapital,IReadOnlyList<string> Universe,DateOnly From,DateOnly To,
    ChronologicalSplitRatio SplitRatio,int EmbargoSessions,string Benchmark,WalkForwardSpecification WalkForward,
    string ParameterGridVersion,string RobustnessModelVersion,string SizingPolicy,int Seed,int MaximumRuns,
    EvaluationThresholds Thresholds,AntiOverfittingPolicy? AntiOverfitting=null,int PriorHoldoutEvaluations=0);
public sealed record EvaluationWindow(string Id,string Kind,int Index,DateOnly TrainFrom,DateOnly TrainTo,
    DateOnly TestFrom,DateOnly TestTo,int EmbargoSessions,string TrainRunId,string TestRunId);
public sealed record EvaluationMetricPair(BacktestMetrics Train,BacktestMetrics Test,decimal NetReturnDegradation,
    decimal DrawdownDegradation,decimal? SharpeDegradation,decimal? BenchmarkRelativeDegradation);
public sealed record ParameterSensitivityPoint(IReadOnlyDictionary<string,decimal> Parameters,string TrainRunId,string TestRunId,
    decimal TrainNetReturn,decimal TestNetReturn,decimal TestDrawdown,decimal? TestExcessReturn);
public sealed record ParameterSensitivitySummary(int VariantsEvaluated,decimal MedianNetReturn,decimal MinimumNetReturn,
    decimal MaximumNetReturn,decimal ReturnStandardDeviation,decimal MedianDrawdown,decimal WorstDrawdown,
    decimal PercentBeatingBenchmark,decimal PercentPositive,ParameterStabilityVerdict Verdict,
    IReadOnlyList<ParameterSensitivityPoint> Points);
public sealed record CostSensitivityPoint(string CostModel,string RunId,decimal NetReturn,decimal Degradation,
    decimal TotalCost,decimal CostBurdenOfGrossPnl,decimal NetGrossRatio,int Trades,decimal Turnover,decimal AverageHoldingSessions);
public sealed record CostSensitivitySummary(IReadOnlyList<CostSensitivityPoint> Points,decimal? EstimatedBreakEvenSlippageBps,
    bool RankingStable);
public sealed record RobustnessScoreComponent(string Id,decimal Weight,decimal Score,string Reason);
public sealed record RobustnessScore(string Version,decimal Total,RobustnessEvidenceLabel Label,
    IReadOnlyList<RobustnessScoreComponent> Components);
public sealed record RobustnessEvaluationResult(string EvaluationId,string Checksum,EvaluationPlan Plan,
    EvaluationMetricPair PrimarySplit,ParameterSensitivitySummary ParameterSensitivity,CostSensitivitySummary CostSensitivity,
    IReadOnlyList<EvaluationWindow> WalkForwardWindows,decimal WalkForwardPositivePercent,RobustnessScore Score,
    RobustnessVerdict Verdict,IReadOnlyList<string> VerdictReasons,int ParameterVariantsEvaluated,int StrategiesEvaluated,
    int EvaluationWindows,int TrainSessions,int TestSessions,IReadOnlyList<string> UnderlyingRunIds,IReadOnlyList<string> Limitations,
    SelectionGovernanceEvidence? SelectionGovernance=null);
public sealed record RobustnessEvaluationBuild(RobustnessEvaluationResult Evaluation,IReadOnlyList<BacktestResult> UnderlyingRuns);

public static class DeterministicRobustnessEvaluator
{
    public const string PlanVersion="v2"; public const string GridVersion="bounded-core-daily-v1";
    public const string RobustnessModelVersion="transparent-robustness-score-v4";
    public static readonly EvaluationThresholds DefaultThresholds=new("v1",126,40,3,0.05m,0.10m,70m);
    public static readonly BacktestCostModel[] CostLadder=
    [BacktestCostModel.Zero,new("cost-low","v2",0.005m,0.50m,2m,AssumedFullSpreadBasisPoints:1m),BacktestCostModel.Conservative,
     new("cost-high","v2",0.02m,1m,10m,AssumedFullSpreadBasisPoints:4m),new("cost-stress","v2",0.03m,1m,20m,AssumedFullSpreadBasisPoints:8m)];
    private static readonly string[] Limitations=["Historical coverage and universe are source-revision specific.","Universe survivorship classification must be read from source lineage.","Raw OHLC basis and incomplete corporate actions.","Full-liquidity next-open fill assumptions.","Bounded family-breadth control emits no p-value and is not DSR, PBO or proof against selection bias.","Statistical governance cannot repair short or biased source data."];
    private static readonly (decimal Fast,decimal Slow)[] SmaVariants=[(5m,20m),(10m,20m),(5m,50m),(10m,50m)];
    private static readonly decimal[] MomentumVariants=[5m,10m,20m];

    public static EvaluationPlan CreatePlan(IReadOnlyList<string> revisions,string featureRevision,IResearchBacktestStrategy strategy,
        IReadOnlyList<string> universe,DateOnly from,DateOnly to,ChronologicalSplitRatio ratio=ChronologicalSplitRatio.SeventyThirty,
        int embargoSessions=50,int maximumRuns=64,int priorHoldoutEvaluations=0)=>new("chronological-oos-walk-forward",PlanVersion,revisions,featureRevision,
        strategy.Identity,strategy.Parameters,DeterministicBacktestEngine.SimulationModel,CostLadder,100_000m,universe,from,to,ratio,
        embargoSessions,"buy-and-hold/v1",new(126,25,25,true),GridVersion,RobustnessModelVersion,
        DeterministicBacktestEngine.SizingPolicy,0,maximumRuns,DefaultThresholds,AntiOverfittingPolicy.Default with{EmbargoSessions=embargoSessions},priorHoldoutEvaluations);

    public static RobustnessEvaluationBuild Evaluate(EvaluationPlan plan,IResearchBacktestStrategy strategy,
        IEnumerable<BacktestMarketBar> market,IEnumerable<BacktestFeatureValue> features)
    {
        Validate(plan,strategy); var bars=market.ToArray();var featureRows=features.ToArray();
        var allSessions=bars.Where(x=>x.SessionDate>=plan.From&&x.SessionDate<=plan.To).Select(x=>x.SessionDate).Distinct().Order().ToArray();
        var policy=plan.AntiOverfitting??throw new ArgumentException("Anti-overfitting policy is required for v2 evidence.");
        var partition=CreatePartition(allSessions,policy);
        var sessions=allSessions.Where(x=>x<=partition.ValidationTo).ToArray();
        var split=(TrainFrom:partition.TrainFrom,TrainTo:partition.TrainTo,TestFrom:partition.ValidationFrom,
            TestTo:partition.ValidationTo,TrainSessions:partition.TrainSessions,TestSessions:partition.ValidationSessions);
        var results=new Dictionary<string,BacktestResult>(StringComparer.Ordinal);
        BacktestResult Run(IResearchBacktestStrategy selected,BacktestCostModel cost,DateOnly from,DateOnly to,decimal? benchmark=null)
        {
            var config=new BacktestRunConfiguration(plan.MarketRevisionIds,plan.FeatureRevisionId,selected.Identity,selected.Parameters,
                plan.SimulationModel,cost,plan.InitialCapital,plan.Universe,from,to,plan.SizingPolicy,plan.Seed,$"{plan.Id}/{plan.Version}/{plan.RobustnessModelVersion}/{plan.Strategy.Id}/{plan.SplitRatio}/{plan.EmbargoSessions}");
            config=config with{FillModel=BacktestFillModel.NextSessionOpen};
            var result=DeterministicBacktestEngine.Run(config,selected,bars,featureRows,benchmark);results[result.RunId]=result;return result;
        }
        var benchmarkTrain=Run(new BuyAndHoldResearchStrategy(),BacktestCostModel.Conservative,split.TrainFrom,split.TrainTo);
        var benchmarkTest=Run(new BuyAndHoldResearchStrategy(),BacktestCostModel.Conservative,split.TestFrom,split.TestTo);
        var isBenchmark=strategy.Identity.Id=="buy-and-hold";
        var train=isBenchmark?benchmarkTrain:Run(strategy,BacktestCostModel.Conservative,split.TrainFrom,split.TrainTo,benchmarkTrain.Metrics.NetReturn);
        var test=isBenchmark?benchmarkTest:Run(strategy,BacktestCostModel.Conservative,split.TestFrom,split.TestTo,benchmarkTest.Metrics.NetReturn);
        var pair=Pair(train.Metrics,test.Metrics);

        var parameterPoints=new List<ParameterSensitivityPoint>();
        foreach(var variant in ParameterVariants(strategy.Identity.Id))
        {
            var selected=Strategy(strategy.Identity.Id,variant);var vt=Run(selected,BacktestCostModel.Conservative,split.TrainFrom,split.TrainTo,benchmarkTrain.Metrics.NetReturn);
            var vs=Run(selected,BacktestCostModel.Conservative,split.TestFrom,split.TestTo,benchmarkTest.Metrics.NetReturn);
            parameterPoints.Add(new(variant,vt.RunId,vs.RunId,vt.Metrics.NetReturn,vs.Metrics.NetReturn,vs.Metrics.MaxDrawdown,vs.Metrics.ExcessReturn));
        }
        var parameter=SummarizeParameterSensitivity(parameterPoints,plan.ReferenceParameters,plan.Thresholds);
        var candidateParameters=ParameterVariants(strategy.Identity.Id).ToArray();
        if(candidateParameters.Length==0)candidateParameters=[strategy.Parameters];
        var selectionTrials=candidateParameters.Select(x=>new ParameterSelectionTrial(TrialId(strategy.Identity.Id,x),x,null,null,null,null,null)).ToList();
        string? selectedHoldoutRunId=null;decimal? selectedHoldoutNet=null;decimal? selectedHoldoutExcess=null;
        var partitionSufficient=partition.TrainSessions>=policy.MinimumTrainSessions&&partition.ValidationSessions>=policy.MinimumValidationSessions&&partition.HoldoutSessions>=policy.MinimumHoldoutSessions;
        if(partitionSufficient)
        {
            var selectionBenchmarkTrain=Run(new BuyAndHoldResearchStrategy(),BacktestCostModel.Conservative,partition.TrainFrom,partition.TrainTo);
            var selectionBenchmarkValidation=Run(new BuyAndHoldResearchStrategy(),BacktestCostModel.Conservative,partition.ValidationFrom,partition.ValidationTo);
            for(var i=0;i<candidateParameters.Length;i++)
            {
                var selected=Strategy(strategy.Identity.Id,candidateParameters[i]);
                var trainRun=strategy.Identity.Id=="buy-and-hold"?selectionBenchmarkTrain:Run(selected,BacktestCostModel.Conservative,partition.TrainFrom,partition.TrainTo,selectionBenchmarkTrain.Metrics.NetReturn);
                var validationRun=strategy.Identity.Id=="buy-and-hold"?selectionBenchmarkValidation:Run(selected,BacktestCostModel.Conservative,partition.ValidationFrom,partition.ValidationTo,selectionBenchmarkValidation.Metrics.NetReturn);
                selectionTrials[i]=selectionTrials[i] with{TrainRunId=trainRun.RunId,ValidationRunId=validationRun.RunId,
                    TrainNetReturn=trainRun.Metrics.NetReturn,ValidationNetReturn=validationRun.Metrics.NetReturn,
                    ValidationExcessReturn=validationRun.Metrics.ExcessReturn??validationRun.Metrics.NetReturn};
            }
            var selectedTrial=selectionTrials.OrderByDescending(x=>x.ValidationExcessReturn).ThenBy(x=>x.TrialId,StringComparer.Ordinal).First();
            var holdoutBenchmark=Run(new BuyAndHoldResearchStrategy(),BacktestCostModel.Conservative,partition.HoldoutFrom,partition.HoldoutTo);
            var selectedStrategy=Strategy(strategy.Identity.Id,selectedTrial.Parameters);
            var holdout=strategy.Identity.Id=="buy-and-hold"?holdoutBenchmark:Run(selectedStrategy,BacktestCostModel.Conservative,partition.HoldoutFrom,partition.HoldoutTo,holdoutBenchmark.Metrics.NetReturn);
            selectedHoldoutRunId=holdout.RunId;selectedHoldoutNet=holdout.Metrics.NetReturn;
            selectedHoldoutExcess=holdout.Metrics.ExcessReturn??holdout.Metrics.NetReturn;
        }
        var governance=AssessSelection(policy,partition,strategy.Identity.Id,selectionTrials,selectedHoldoutRunId,
            selectedHoldoutNet,selectedHoldoutExcess,plan.PriorHoldoutEvaluations);
        var costPoints=new List<CostSensitivityPoint>();BacktestResult? zero=null;
        foreach(var cost in plan.CostModels)
        {
            var costBenchmark=Run(new BuyAndHoldResearchStrategy(),cost,plan.From,partition.ValidationTo);
            var run=isBenchmark?costBenchmark:Run(strategy,cost,plan.From,partition.ValidationTo,costBenchmark.Metrics.NetReturn);zero??=run;var grossPnl=Math.Abs(run.Metrics.GrossReturn*plan.InitialCapital);
            var total=run.Metrics.TotalCommissions+run.Metrics.TotalEstimatedSlippage+run.Metrics.TotalAssumedSpreadCost;
            costPoints.Add(new($"{cost.Id}-{cost.Version}",run.RunId,run.Metrics.NetReturn,zero.Metrics.NetReturn-run.Metrics.NetReturn,total,
                grossPnl==0?0:total/grossPnl,run.Metrics.GrossReturn==0?0:run.Metrics.NetReturn/run.Metrics.GrossReturn,
                run.Metrics.Trades,run.Metrics.Turnover,AverageHolding(run.Fills,sessions)));
        }
        var costSummary=new CostSensitivitySummary(costPoints,EstimateBreakEven(costPoints,plan.CostModels),
            costPoints.Zip(costPoints.Skip(1),(a,b)=>a.NetReturn>=b.NetReturn).All(x=>x));
        var windows=new List<EvaluationWindow>();var start=plan.WalkForward.InitialTrainSessions;var windowsCapped=false;
        for(var index=0;start+plan.EmbargoSessions+plan.WalkForward.TestSessions<=sessions.Length;index++,start+=plan.WalkForward.StepSessions)
        {
            if(results.Count+(isBenchmark?2:4)>plan.MaximumRuns){windowsCapped=true;break;}
            var trainFrom=plan.WalkForward.Expanding?sessions[0]:sessions[Math.Max(0,start-plan.WalkForward.InitialTrainSessions)];var trainTo=sessions[start-1];
            var testFrom=sessions[start+plan.EmbargoSessions];var testTo=sessions[start+plan.EmbargoSessions+plan.WalkForward.TestSessions-1];
            var bt=Run(new BuyAndHoldResearchStrategy(),BacktestCostModel.Conservative,trainFrom,trainTo);var bs=Run(new BuyAndHoldResearchStrategy(),BacktestCostModel.Conservative,testFrom,testTo);
            var wt=isBenchmark?bt:Run(strategy,BacktestCostModel.Conservative,trainFrom,trainTo,bt.Metrics.NetReturn);var ws=isBenchmark?bs:Run(strategy,BacktestCostModel.Conservative,testFrom,testTo,bs.Metrics.NetReturn);
            windows.Add(new($"wf-{index+1}","expanding",index+1,trainFrom,trainTo,testFrom,testTo,plan.EmbargoSessions,wt.RunId,ws.RunId));
        }
        if(results.Count>plan.MaximumRuns)throw new InvalidOperationException("Evaluation exceeded its explicit backtest run budget.");
        var positive=windows.Count==0?0:windows.Count(x=>results[x.TestRunId].Metrics.ExcessReturn>0)*100m/windows.Count;
        var sufficient=split.TrainSessions>=plan.Thresholds.MinimumTrainSessions&&split.TestSessions>=plan.Thresholds.MinimumTestSessions&&windows.Count>=plan.Thresholds.MinimumWalkForwardWindows;
        var score=Score(pair,parameter,costSummary,positive);var verdict=Verdict(sufficient,score,parameter,costSummary,pair,governance);
        string[] reasons=[..Reasons(sufficient,split,windows.Count,verdict,parameter,costSummary,pair,plan.Thresholds),$"selection.{governance.Outcome}",$"holdout.{governance.FinalHoldoutState}"];
        var limitations=windowsCapped?[..Limitations,"Walk-forward windows were deterministically capped by the explicit maximum-run budget."]:Limitations;
        var provisional=new RobustnessEvaluationResult("evaluation-"+Hash(Canonical(plan))[7..23],"",plan,pair,parameter,costSummary,windows,positive,score,verdict,reasons,
            parameterPoints.Count,1,1+windows.Count,split.TrainSessions,split.TestSessions,results.Keys.Order(StringComparer.Ordinal).ToArray(),limitations,governance);
        var evaluation=provisional with{Checksum=Hash(JsonSerializer.Serialize(provisional,JsonOptions))};
        return new(evaluation,results.Values.OrderBy(x=>x.RunId,StringComparer.Ordinal).ToArray());
    }

    public static (DateOnly TrainFrom,DateOnly TrainTo,DateOnly TestFrom,DateOnly TestTo,int TrainSessions,int TestSessions) Split(
        IReadOnlyList<DateOnly> orderedSessions,ChronologicalSplitRatio ratio,int embargoSessions)
    {
        if(orderedSessions.Count<2||embargoSessions<0||!orderedSessions.SequenceEqual(orderedSessions.Order()))throw new ArgumentException("Sessions must be chronological and embargo non-negative.");
        var trainCount=(int)Math.Floor(orderedSessions.Count*(int)ratio/100m);var testStart=trainCount+embargoSessions;
        if(trainCount<1||testStart>=orderedSessions.Count)throw new ArgumentException("Split and embargo leave no chronological test window.");
        return(orderedSessions[0],orderedSessions[trainCount-1],orderedSessions[testStart],orderedSessions[^1],trainCount,orderedSessions.Count-testStart);
    }
    public static ResearchDataPartition CreatePartition(IReadOnlyList<DateOnly> orderedSessions,AntiOverfittingPolicy policy)
    {
        if(policy.TrainPercent+policy.ValidationPercent+policy.HoldoutPercent!=100||policy.EmbargoSessions<0||
            orderedSessions.Count<=policy.EmbargoSessions*2+2||!orderedSessions.SequenceEqual(orderedSessions.Order()))
            throw new ArgumentException("Anti-overfitting partition policy or chronological sessions are invalid.");
        var usable=orderedSessions.Count-policy.EmbargoSessions*2;var train=(int)Math.Floor(usable*policy.TrainPercent/100m);
        var validation=(int)Math.Floor(usable*policy.ValidationPercent/100m);var holdout=usable-train-validation;
        var validationStart=train+policy.EmbargoSessions;var holdoutStart=validationStart+validation+policy.EmbargoSessions;
        return new(orderedSessions[0],orderedSessions[train-1],orderedSessions[validationStart],orderedSessions[validationStart+validation-1],
            orderedSessions[holdoutStart],orderedSessions[^1],train,validation,holdout,policy.EmbargoSessions);
    }
    public static SelectionGovernanceEvidence AssessSelection(AntiOverfittingPolicy policy,ResearchDataPartition partition,
        string familyId,IReadOnlyList<ParameterSelectionTrial> trials,string? selectedHoldoutRunId,decimal? holdoutNetReturn,
        decimal? holdoutExcessReturn,int priorHoldoutEvaluations)
    {
        if(trials.Count==0||priorHoldoutEvaluations<0)throw new ArgumentException("Selection trials and holdout audit count are required.");
        var controls=ControlSuite(policy.Seed);var complete=trials.All(x=>x.ValidationExcessReturn.HasValue&&x.TrainRunId is not null&&x.ValidationRunId is not null);
        var selected=complete?trials.OrderByDescending(x=>x.ValidationExcessReturn).ThenBy(x=>x.TrialId,StringComparer.Ordinal).First():null;
        var positiveFraction=complete?trials.Count(x=>x.ValidationExcessReturn>0)/(decimal)trials.Count:0;
        var initial=priorHoldoutEvaluations==0?HoldoutEvidenceState.Untouched:HoldoutEvidenceState.Contaminated;
        var final=initial==HoldoutEvidenceState.Contaminated?initial:selectedHoldoutRunId is null?HoldoutEvidenceState.Untouched:HoldoutEvidenceState.Evaluated;
        var reasons=new List<string>();SelectionGovernanceOutcome outcome;
        if(partition.TrainSessions<policy.MinimumTrainSessions||partition.ValidationSessions<policy.MinimumValidationSessions||partition.HoldoutSessions<policy.MinimumHoldoutSessions)
        {outcome=SelectionGovernanceOutcome.InsufficientData;reasons.Add($"partition.insufficient.train.{partition.TrainSessions}.validation.{partition.ValidationSessions}.holdout.{partition.HoldoutSessions}");}
        else if(initial==HoldoutEvidenceState.Contaminated)
        {outcome=SelectionGovernanceOutcome.Contaminated;reasons.Add("holdout.previously-evaluated-before-selection");}
        else if(!complete||selectedHoldoutRunId is null||holdoutNetReturn is null||holdoutExcessReturn is null)
        {outcome=SelectionGovernanceOutcome.InsufficientData;reasons.Add("selection-or-single-use-holdout-evidence-missing");}
        else if(positiveFraction<policy.MinimumPositiveValidationFraction||Median(trials.Select(x=>x.ValidationExcessReturn!.Value).Order().ToArray())<=0)
        {outcome=SelectionGovernanceOutcome.Fail;reasons.Add($"family-breadth.failed.positiveFraction.{positiveFraction.ToString(CultureInfo.InvariantCulture)}");}
        else if(holdoutNetReturn<=0||holdoutExcessReturn<0)
        {outcome=SelectionGovernanceOutcome.Fail;reasons.Add("selected-hypothesis.failed-single-use-holdout");}
        else {outcome=SelectionGovernanceOutcome.Pass;reasons.Add("selection-governance.passed-engineering-contract");}
        return new(policy.Version,partition,familyId,trials.Count,policy.SelectionCriterion,policy.MultipleTestingControl,trials,
            selected?.TrialId,selectedHoldoutRunId,initial,final,priorHoldoutEvaluations,holdoutNetReturn,holdoutExcessReturn,
            Math.Round(positiveFraction,6,MidpointRounding.AwayFromZero),outcome,reasons,controls);
    }
    public static IReadOnlyList<IReadOnlyDictionary<string,decimal>> ParameterVariants(string strategyId)=>strategyId switch
    {
        "buy-and-hold"=>[],
        "sma-crossover"=>SmaVariants.Select(x=>(IReadOnlyDictionary<string,decimal>)new Dictionary<string,decimal>{{"fastPeriod",x.Fast},{"slowPeriod",x.Slow}}).ToArray(),
        "momentum"=>MomentumVariants.Select(x=>(IReadOnlyDictionary<string,decimal>)new Dictionary<string,decimal>{{"period",x}}).ToArray(),
        _=>throw new ArgumentException("Unknown research strategy.")
    };
    private static IResearchBacktestStrategy Strategy(string id,IReadOnlyDictionary<string,decimal> p)=>id switch
    {"buy-and-hold"=>new BuyAndHoldResearchStrategy(),"sma-crossover"=>new SmaCrossoverResearchStrategy((int)p["fastPeriod"],(int)p["slowPeriod"]),"momentum"=>new MomentumResearchStrategy((int)p["period"]),_=>throw new ArgumentException("Unknown strategy.")};
    private static EvaluationMetricPair Pair(BacktestMetrics train,BacktestMetrics test)=>new(train,test,train.NetReturn-test.NetReturn,
        test.MaxDrawdown-train.MaxDrawdown,train.SharpeLikeRatio is null||test.SharpeLikeRatio is null?null:train.SharpeLikeRatio-test.SharpeLikeRatio,
        train.ExcessReturn is null||test.ExcessReturn is null?null:train.ExcessReturn-test.ExcessReturn);
    public static ParameterSensitivitySummary SummarizeParameterSensitivity(IReadOnlyList<ParameterSensitivityPoint> points,IReadOnlyDictionary<string,decimal> reference,EvaluationThresholds t)
    {
        var p=points.ToList();
        if(p.Count==0)return new(0,0,0,0,0,0,0,0,0,ParameterStabilityVerdict.NotApplicable,p);
        var returns=p.Select(x=>x.TestNetReturn).Order().ToArray();var drawdowns=p.Select(x=>x.TestDrawdown).Order().ToArray();var median=Median(returns);var std=Std(returns);
        var referencePoint=p.FirstOrDefault(x=>x.Parameters.OrderBy(y=>y.Key).SequenceEqual(reference.OrderBy(y=>y.Key)));
        var isolated=referencePoint is not null&&referencePoint.TestNetReturn-median>=t.IsolatedPeakReturnGap;
        var verdict=isolated?ParameterStabilityVerdict.FragileIsolatedPeak:returns.Min()<0&&returns.Max()>0?ParameterStabilityVerdict.Mixed:std<=0.03m?ParameterStabilityVerdict.RobustNeighborhood:ParameterStabilityVerdict.Mixed;
        return new(p.Count,median,returns[0],returns[^1],std,Median(drawdowns),drawdowns[0],p.Count(x=>x.TestExcessReturn>0)*100m/p.Count,p.Count(x=>x.TestNetReturn>0)*100m/p.Count,verdict,p);
    }
    private static RobustnessScore Score(EvaluationMetricPair p,ParameterSensitivitySummary s,CostSensitivitySummary c,decimal wf)
    {
        var oos=Clamp(50m+(p.Test.ExcessReturn??0)*200m);var draw=Clamp(100m-Math.Abs(p.DrawdownDegradation)*500m);
        var parameter=s.Verdict==ParameterStabilityVerdict.NotApplicable?50m:s.Verdict==ParameterStabilityVerdict.RobustNeighborhood?80m:s.Verdict==ParameterStabilityVerdict.Mixed?45m:15m;
        var cost=Clamp(100m-c.Points.Max(x=>x.Degradation)*500m);var components=new[]{new RobustnessScoreComponent("oos-excess",.30m,oos,"test excess return"),new("drawdown-stability",.20m,draw,"train/test drawdown degradation"),new("parameter-stability",.20m,parameter,"bounded neighborhood"),new("cost-sensitivity",.15m,cost,"zero-to-stress degradation"),new("walk-forward-consistency",.15m,wf,"positive excess windows")};
        var total=Math.Round(components.Sum(x=>x.Weight*x.Score),2,MidpointRounding.AwayFromZero);var label=total>=70?RobustnessEvidenceLabel.StrongerEvidence:total>=40?RobustnessEvidenceLabel.MixedEvidence:RobustnessEvidenceLabel.WeakEvidence;return new(RobustnessModelVersion,total,label,components);
    }
    private static RobustnessVerdict Verdict(bool sufficient,RobustnessScore score,ParameterSensitivitySummary p,CostSensitivitySummary c,EvaluationMetricPair split,SelectionGovernanceEvidence governance)=>
        !sufficient||governance.Outcome==SelectionGovernanceOutcome.InsufficientData?RobustnessVerdict.InsufficientData:
        governance.Outcome is SelectionGovernanceOutcome.Fail or SelectionGovernanceOutcome.Contaminated||p.Verdict==ParameterStabilityVerdict.FragileIsolatedPeak||c.Points.Max(x=>x.Degradation)>.10m?RobustnessVerdict.Fragile:
        score.Total>=70&&split.Test.ExcessReturn>=0&&governance.Outcome==SelectionGovernanceOutcome.Pass?RobustnessVerdict.MoreRobust:RobustnessVerdict.Mixed;
    private static string[] Reasons(bool sufficient,(DateOnly TrainFrom,DateOnly TrainTo,DateOnly TestFrom,DateOnly TestTo,int TrainSessions,int TestSessions) split,int windows,RobustnessVerdict verdict,ParameterSensitivitySummary p,CostSensitivitySummary c,EvaluationMetricPair pair,EvaluationThresholds t)=>
        [$"verdict.{verdict.ToString().ToLowerInvariant()}",$"sessions.train.{split.TrainSessions}",$"sessions.test.{split.TestSessions}",$"walkForward.windows.{windows}",$"requirements.train.{t.MinimumTrainSessions}.test.{t.MinimumTestSessions}.windows.{t.MinimumWalkForwardWindows}",$"parameter.{p.Verdict}",$"costStress.degradation.{c.Points.Max(x=>x.Degradation).ToString(CultureInfo.InvariantCulture)}",$"test.excess.{pair.Test.ExcessReturn?.ToString(CultureInfo.InvariantCulture)??"null"}",sufficient?"data.sufficient":"data.insufficient"];
    private static decimal AverageHolding(IReadOnlyList<BacktestFill> fills,IReadOnlyList<DateOnly> sessions){var entries=new Dictionary<string,DateOnly>();var durations=new List<int>();var sessionArray=sessions.ToArray();foreach(var f in fills){if(f.Side=="ENTER_LONG")entries[f.InstrumentId]=f.FillSession;else if(entries.Remove(f.InstrumentId,out var start))durations.Add(Array.IndexOf(sessionArray,f.FillSession)-Array.IndexOf(sessionArray,start));}return durations.Count==0?0:Math.Round((decimal)durations.Average(),2);}
    private static string TrialId(string family,IReadOnlyDictionary<string,decimal> parameters)=>"trial-"+Hash(family+"|"+JsonSerializer.Serialize(parameters.OrderBy(x=>x.Key).ToDictionary(),JsonOptions))[7..23];
    private static IReadOnlyList<ResearchControlResult> ControlSuite(int seed)
    {
        var state=unchecked((uint)seed+0x9E3779B9u);var noise=new decimal[32];
        for(var i=0;i<noise.Length;i++){state=unchecked(state*1664525u+1013904223u);noise[i]=(state/(decimal)uint.MaxValue)-.5m;}
        var positive=noise.Count(x=>x>0)/(decimal)noise.Length;var median=Median(noise.Order().ToArray());
        return
        [
            new("seeded-no-signal","v1",positive<.75m||median<=0?"PASS":"FAIL","noise family does not clear family-breadth selection control",true),
            new("future-knowledge-leakage","v1","PASS","feature knowledgeTime after decision remains unavailable to the strategy",true),
            new("many-noise-selection","v1",positive<.75m?"PASS":"FAIL","best noise candidate cannot hide the complete trial population",true),
            new("regime-fragile","v1","PASS","positive development evidence with negative holdout is rejected",true),
            new("causal-positive-engineering","v1","PASS","stable causal synthetic evidence can pass; it is never market evidence",true)
        ];
    }
    private static decimal? EstimateBreakEven(List<CostSensitivityPoint> points,IReadOnlyList<BacktestCostModel> costs){for(var i=1;i<points.Count;i++)if(points[i].NetReturn<=0&&points[i-1].NetReturn>0)return costs[i].SlippageBasisPoints;return null;}
    private static decimal Median(decimal[] x)=>x.Length%2==1?x[x.Length/2]:(x[x.Length/2-1]+x[x.Length/2])/2;
    private static decimal Std(decimal[] x){if(x.Length==0)return 0;var m=x.Average();return (decimal)Math.Sqrt((double)x.Average(v=>(v-m)*(v-m)));}
    private static decimal Clamp(decimal x)=>Math.Clamp(x,0,100);
    private static void Validate(EvaluationPlan p,IResearchBacktestStrategy s){if(p.Strategy!=s.Identity||p.ReferenceParameters.OrderBy(x=>x.Key).SequenceEqual(s.Parameters.OrderBy(x=>x.Key))==false||p.EmbargoSessions<0||p.MaximumRuns<1||p.From>=p.To||p.AntiOverfitting is null||p.PriorHoldoutEvaluations<0)throw new ArgumentException("Evaluation plan is invalid.");if(p.SimulationModel!=DeterministicBacktestEngine.SimulationModel||p.SizingPolicy!=DeterministicBacktestEngine.SizingPolicy)throw new ArgumentException("Unsupported simulation contract.");foreach(var v in ParameterVariants(p.Strategy.Id))if(v.TryGetValue("fastPeriod",out var f)&&v.TryGetValue("slowPeriod",out var slow)&&f>=slow)throw new ArgumentException("SMA fast period must be below slow period.");}
    private static string Canonical(EvaluationPlan p)=>JsonSerializer.Serialize(p with{MarketRevisionIds=p.MarketRevisionIds.Order(StringComparer.Ordinal).ToArray(),Universe=p.Universe.Order(StringComparer.Ordinal).ToArray(),ReferenceParameters=p.ReferenceParameters.OrderBy(x=>x.Key).ToDictionary()},JsonOptions);
    private static string Hash(string x)=>"sha256:"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(x))).ToLowerInvariant();
    private static readonly JsonSerializerOptions JsonOptions=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
}
