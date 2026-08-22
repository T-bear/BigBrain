using BigBrain.Modules.Finance;
using BigBrain.Api.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceAutonomousResearchTests
{
    [Fact]
    public void ResearchRunPersistsRejectedAttemptsIsIdempotentAndSurvivesRestart()
    {
        var root=Path.Combine(Path.GetTempPath(),"bb-autonomous-research",Guid.NewGuid().ToString("N"));
        try
        {
            var options=new EodhdFinanceOptions{DatabasePath=Path.Combine(root,"finance.db"),PayloadDirectory=Path.Combine(root,"payloads")};
            var memory=new EodhdMarketMemory(options);var instrument=EodhdCatalog.Watchlist.Single(x=>x.Symbol=="AAPL");var bars=new List<EodhdDailyBar>();var date=new DateOnly(2025,1,2);
            for(var i=0;i<280;i++){while(date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)date=date.AddDays(1);var close=100m+i*.1m+(i%17-8)*.2m;bars.Add(new(date,close-.1m,close+.5m,close-.5m,close,close,1000+i));date=date.AddDays(1);}
            var acquired=new DateTimeOffset(2026,8,21,22,0,0,TimeSpan.Zero);memory.Store(instrument,bars,System.Text.Encoding.UTF8.GetBytes("bb092-fixture"),bars[0].Date,bars[^1].Date,acquired.AddMinutes(-1),acquired,0);memory.BuildFeatures();
            var first=memory.RunAutonomousResearch("bb092-test-key",2);var repeated=memory.RunAutonomousResearch("bb092-test-key",2);
            Assert.Equal(first.RunId,repeated.RunId);Assert.Equal(first.Experiments.Select(x=>x.ExperimentId),repeated.Experiments.Select(x=>x.ExperimentId));Assert.Equal(2,first.ExperimentCount);
            Assert.All(first.Experiments,x=>{Assert.Equal(ResearchExperimentState.Completed,x.State);Assert.True(x.FamilyAttemptCount>=3);Assert.NotEmpty(x.MarketRevisionIds);Assert.NotEqual(ResearchExperimentVerdict.Promising,x.Verdict);});
            var restarted=new EodhdMarketMemory(options).AutonomousResearchSnapshot();Assert.Equal(2,restarted.TotalExperiments);Assert.Equal(0m,restarted.BudgetSek);Assert.Equal("NONE",restarted.ExecutionAuthority);
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }

    [Fact]
    public void FeatureLibraryIsVersionedAllowlistedAndRejectsUnknownIds()
    {
        Assert.Equal("finance-research-signals-v1", FinanceResearchFeatureLibrary.Version);
        Assert.Equal(4, FinanceResearchFeatureLibrary.Definitions.Count);
        Assert.All(FinanceResearchFeatureLibrary.Definitions, feature =>
        {
            Assert.Equal("v1", feature.Version);
            Assert.NotEmpty(feature.RequiredInputs);
            Assert.True(feature.MinimumEvidenceSessions > 0);
            Assert.DoesNotContain("BUY", feature.Description, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Throws<ArgumentException>(() => FinanceResearchFeatureLibrary.Require("eval(user-code)"));
    }

    [Fact]
    public void ParameterBoundsAndComplexityAreBoundedDeterministicAndExplainable()
    {
        var momentum = FinanceResearchFeatureLibrary.Require("momentum.20.sign");
        Assert.Collection(momentum.ParameterBounds, bound => { Assert.Equal(-.10m, bound.Minimum); Assert.Equal(.10m, bound.Maximum); });
        var simple = FinanceResearchContracts.Complexity(1, 1, 1, 3);
        var repeated = FinanceResearchContracts.Complexity(1, 1, 1, 3);
        var complex = FinanceResearchContracts.Complexity(2, 2, 3, 4);
        Assert.Equal(simple, repeated); Assert.True(complex.Score > simple.Score); Assert.Contains("score =", simple.Explanation);
        Assert.Throws<ArgumentOutOfRangeException>(() => FinanceResearchContracts.Complexity(3, 1, 0, 1));
    }

    [Fact]
    public void HypothesisFingerprintIsStableAndSensitiveToPinnedRevision()
    {
        var first = FinanceResearchContracts.Fingerprint(new { engine = "v1", feature = "momentum.20.sign", revision = "feature-1", horizon = 5 });
        var repeat = FinanceResearchContracts.Fingerprint(new { engine = "v1", feature = "momentum.20.sign", revision = "feature-1", horizon = 5 });
        var changed = FinanceResearchContracts.Fingerprint(new { engine = "v1", feature = "momentum.20.sign", revision = "feature-2", horizon = 5 });
        Assert.Equal(first, repeat); Assert.NotEqual(first, changed); Assert.StartsWith("sha256:", first);
    }

    [Fact]
    public void StrongTrainCannotBypassFailedOosAndMissingEvidenceFailsClosed()
    {
        var evaluation = Evidence(trainReturn: .40m, testReturn: -.05m, robustness: RobustnessVerdict.MoreRobust);
        var integrity = FinanceResearchContracts.Assess(evaluation, 7,
            FinanceResearchContracts.Complexity(1, 1, 1, 3), lineageComplete: false, costAssumptionPresent: false);
        Assert.Equal(ResearchIntegrityState.Fail, integrity.State);
        Assert.Contains(integrity.Checks, x => x.Id == "out-of-sample" && x.State == ResearchIntegrityState.Fail);
        Assert.Contains(integrity.Checks, x => x.Id == "lineage" && x.State == ResearchIntegrityState.Fail);
        Assert.Contains(integrity.Checks, x => x.Id == "costs" && x.State == ResearchIntegrityState.Fail);
        Assert.Contains(integrity.Checks, x => x.Id == "dsr" && x.State == ResearchIntegrityState.NotEvaluable);
        Assert.Contains(integrity.Checks, x => x.Id == "pbo-cscv" && x.State == ResearchIntegrityState.NotEvaluable);
    }

    [Fact]
    public void MultipleAttemptsAreVisibleAndExcessiveComplexityFailsGate()
    {
        var integrity = FinanceResearchContracts.Assess(Evidence(.10m, .10m, RobustnessVerdict.MoreRobust), 31,
            FinanceResearchContracts.Complexity(2, 2, 3, 4), true, true);
        Assert.Contains(integrity.Checks, x => x.Id == "multiple-testing" && x.Evidence.Contains("31", StringComparison.Ordinal));
        Assert.Contains(integrity.Checks, x => x.Id == "complexity" && x.State == ResearchIntegrityState.Fail);
        Assert.Equal(ResearchIntegrityState.Fail, integrity.State);
    }

    private static RobustnessEvaluationResult Evidence(decimal trainReturn, decimal testReturn, RobustnessVerdict robustness)
    {
        var metrics = new BacktestMetrics(100, 110, .1m, trainReturn, null, -.05m, null, .1m, 1m, 10, 5, 5, 1m, 1m, 1m, .05m, .05m);
        var test = metrics with { NetReturn = testReturn, ExcessReturn = testReturn };
        var thresholds = new EvaluationThresholds("v1", 10, 5, 1, .05m, .10m, 70);
        var plan = new EvaluationPlan("plan", "v1", ["market-1"], "feature-1", new("momentum", "v1"),
            new Dictionary<string, decimal> { ["period"] = 20 }, "sim", [BacktestCostModel.Conservative], 100,
            ["US:XNAS:TEST"], new(2025, 1, 1), new(2025, 12, 31), ChronologicalSplitRatio.SeventyThirty, 1,
            "benchmark", new(10, 5, 5, true), "grid", "robustness", "sizing", 0, 10, thresholds);
        var cost = new CostSensitivitySummary([new("cost-conservative-v1", "run", testReturn, 0, 1, 1, 1, 10, 1, 2)], null, true);
        return new("evaluation", "checksum", plan, new(metrics, test, trainReturn-testReturn, 0, 0, 0),
            new(3, testReturn, testReturn, testReturn, 0, -.05m, -.05m, 100, 100, ParameterStabilityVerdict.RobustNeighborhood, []),
            cost, [new("wf-1", "expanding", 1, new(2025,1,1), new(2025,6,1), new(2025,6,3), new(2025,7,1), 1, "train", "test")],
            100, new("v", 80, RobustnessEvidenceLabel.StrongerEvidence, []), robustness, [], 3, 1, 2, 100, 40, [], []);
    }
}
