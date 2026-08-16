using BigBrain.Api.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceRiskEngineTests:IDisposable
{
    private readonly string _root=Path.Combine(Path.GetTempPath(),$"bigbrain-risk-{Guid.NewGuid():N}");
    private static readonly DateTimeOffset T0=new(2026,8,16,12,0,0,TimeSpan.Zero);

    [Fact]
    public void PolicyIsVersionedCentralValidatedAndResearchOnly()
    {
        var policy=new FinanceRiskEngine(new FinanceRiskOptions()).Policy();
        Assert.Equal("research-eod-v1",policy.PolicyVersion);Assert.Equal("RESEARCH",policy.OperatingMode);
        Assert.Equal(100_000m,policy.ResearchCapital);Assert.Equal(.05m,policy.MaximumPositionFraction);
        Assert.Contains("Hypothetical",policy.CapitalSemantics);Assert.Contains("NOT_EVALUABLE",policy.SpreadRule);
        Assert.Throws<InvalidOperationException>(()=>new FinanceRiskEngine(new(){MaximumPositionFraction=2}));
        Assert.Throws<InvalidOperationException>(()=>new FinanceRiskEngine(new(){PolicyVersion=""}));
    }

    [Fact]
    public void AllowAndReduceAreDeterministicBoundedAndNeverOrderInstructions()
    {
        var engine=new FinanceRiskEngine(new FinanceRiskOptions());
        var allow=engine.Evaluate(Proposal(requested:4_000));var same=engine.Evaluate(Proposal(requested:4_000));
        Assert.Equal(FinanceRiskVerdict.Allow,allow.Verdict);Assert.Equal(allow.EvaluationId,same.EvaluationId);Assert.Equal(allow.Verdict,same.Verdict);Assert.Equal(allow.ReasonCodes,same.ReasonCodes);Assert.Equal(4_000,allow.AllowedExposure);
        var reduce=engine.Evaluate(Proposal(requested:7_500));
        Assert.Equal(FinanceRiskVerdict.Reduce,reduce.Verdict);Assert.Equal(5_000,reduce.AllowedExposure);
        Assert.True(reduce.AllowedExposure<=reduce.ResearchCapital*.05m);Assert.DoesNotContain("order",reduce.EvidenceLineage,StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ZeroOrNegativeExposureFailsClosed(decimal requested)
    {var result=new FinanceRiskEngine(new FinanceRiskOptions()).Evaluate(Proposal(requested:requested));Assert.Equal(FinanceRiskVerdict.Deny,result.Verdict);Assert.Equal(0,result.AllowedExposure);}

    [Fact]
    public void InvalidModePriceInstrumentLineageClockAndClientVerdictNeverAllow()
    {
        var engine=new FinanceRiskEngine(new FinanceRiskOptions());
        var proposals=new[]{Proposal(mode:"PAPER"),Proposal(price:0),Proposal(instrument:"FAKE"),Proposal(sourceValid:false),
            Proposal(featureValid:false),Proposal(clock:false),Proposal(clientVerdict:"ALLOW"),Proposal(direction:"FORGED")};
        Assert.All(proposals,p=>Assert.NotEqual(FinanceRiskVerdict.Allow,engine.Evaluate(p).Verdict));
        Assert.All(proposals,p=>Assert.Equal(0,engine.Evaluate(p).AllowedExposure));
    }

    [Fact]
    public void MissingRequiredEvidenceIsInsufficientData()
    {
        var engine=new FinanceRiskEngine(new FinanceRiskOptions());
        Assert.Equal(FinanceRiskVerdict.InsufficientData,engine.Evaluate(Proposal(volatility:null)).Verdict);
        Assert.Equal(FinanceRiskVerdict.InsufficientData,engine.Evaluate(Proposal(volume:null)).Verdict);
        Assert.Equal(FinanceRiskVerdict.InsufficientData,engine.Evaluate(Proposal(warmup:false)).Verdict);
    }

    [Fact]
    public void WeekendEodIsFreshButSecondCompletedWeekdayIsStale()
    {
        var engine=new FinanceRiskEngine(new FinanceRiskOptions());
        var sunday=engine.Evaluate(Proposal(at:T0,session:new(2026,8,14)));
        var tuesday=engine.Evaluate(Proposal(at:new(2026,8,18,12,0,0,TimeSpan.Zero),session:new(2026,8,14)));
        Assert.Equal(FinanceRiskVerdict.Allow,sunday.Verdict);Assert.Equal(FinanceRiskVerdict.Deny,tuesday.Verdict);
        Assert.Contains("risk.data.stale",tuesday.ReasonCodes);
    }

    [Fact]
    public void VolatilityMoveLiquidityAndHealthFailClosedWhileSpreadIsHonest()
    {
        var engine=new FinanceRiskEngine(new FinanceRiskOptions());
        Assert.Equal(FinanceRiskVerdict.Deny,engine.Evaluate(Proposal(volatility:.09m)).Verdict);
        Assert.Equal(FinanceRiskVerdict.Deny,engine.Evaluate(Proposal(price:130,previous:100)).Verdict);
        Assert.Equal(FinanceRiskVerdict.Deny,engine.Evaluate(Proposal(volume:.05m)).Verdict);
        Assert.Equal(FinanceRiskVerdict.Deny,engine.Evaluate(Proposal(provider:false)).Verdict);
        var spread=engine.Evaluate(Proposal()).Rules.Single(x=>x.RuleId=="market.spread");
        Assert.Equal(FinanceRiskRuleState.NotEvaluable,spread.State);Assert.Equal("risk.spread.notAvailable",spread.ReasonCode);
    }

    [Fact]
    public void LossDrawdownConsecutiveLossAndDurableHaltAlwaysHalt()
    {
        var engine=new FinanceRiskEngine(new FinanceRiskOptions());
        Assert.Equal(FinanceRiskVerdict.Halt,engine.Evaluate(Proposal(dailyLoss:.03m)).Verdict);
        Assert.Equal(FinanceRiskVerdict.Halt,engine.Evaluate(Proposal(drawdown:.10m)).Verdict);
        Assert.Equal(FinanceRiskVerdict.Halt,engine.Evaluate(Proposal(losses:3)).Verdict);
        Assert.Equal(FinanceRiskVerdict.Halt,engine.Evaluate(Proposal(),true,"risk.halt.manual").Verdict);
    }

    [Fact]
    public void ImmutableEvaluationIsIdempotentAndHaltSurvivesRestartWithAudit()
    {
        var options=Options();var memory=new EodhdMarketMemory(options);var proposal=Proposal();
        var first=memory.RecordRiskEvaluation(proposal);var duplicate=memory.RecordRiskEvaluation(proposal);
        Assert.Equal(first.EvaluationId,duplicate.EvaluationId);Assert.Equal(first.Verdict,duplicate.Verdict);Assert.Equal(first.ReasonCodes,duplicate.ReasonCodes);Assert.Single(memory.RiskEvaluations());
        var changed=T0.AddMinutes(1);var audit=memory.SetRiskHalt(true,"risk.halt.test",changed,"deterministic fixture");
        var reopened=new EodhdMarketMemory(options);Assert.True(reopened.RiskStatus().ActiveHalt);Assert.Equal("risk.halt.test",reopened.RiskStatus().HaltReason);
        Assert.Equal(FinanceRiskVerdict.Halt,reopened.RecordRiskEvaluation(Proposal(id:"proposal-2",at:T0.AddMinutes(2))).Verdict);
        var recovery=reopened.SetRiskHalt(false,"risk.recovery.explicitFixture",T0.AddMinutes(3),audit.AuditId);
        Assert.Equal("HALTED",recovery.PreviousState);Assert.Equal("ACTIVE",recovery.NewState);Assert.False(new EodhdMarketMemory(options).RiskStatus().ActiveHalt);
    }

    [Fact]
    public void ShadowSignalRemainsSeparateFromImmutableRiskVerdict()
    {
        var result=new FinanceRiskEngine(new FinanceRiskOptions()).Evaluate(Proposal(direction:"TargetLong",provider:false));
        Assert.Equal("TargetLong",result.Direction);Assert.Equal(FinanceRiskVerdict.Deny,result.Verdict);
        Assert.Equal("shadow-existing",result.ShadowPredictionId);
    }

    private EodhdFinanceOptions Options()=>new(){DatabasePath=Path.Combine(_root,"risk.db"),PayloadDirectory=Path.Combine(_root,"payloads")};
    private static FinanceRiskProposal Proposal(string id="proposal-1",string instrument="US:XNAS:AAPL",string mode="RESEARCH",string direction="TargetLong",
        decimal price=101,decimal previous=100,decimal requested=4_000,decimal? volatility=.02m,decimal? volume=1m,bool clock=true,
        bool sourceValid=true,bool featureValid=true,bool warmup=true,bool provider=true,decimal dailyLoss=0,decimal drawdown=0,int losses=0,
        string? clientVerdict=null,DateTimeOffset? at=null,DateOnly? session=null)
    {var when=at??T0;return new(id,instrument,"momentum","v1","sha256:parameters","shadow-existing","source-revision","feature-revision",session??new(2026,8,14),when.AddHours(-1),when.AddMinutes(-1),when,mode,direction,price,previous,requested,volatility,volume,clock,sourceValid,featureValid,warmup,provider,true,dailyLoss,drawdown,losses,clientVerdict);}
    public void Dispose(){if(Directory.Exists(_root))Directory.Delete(_root,true);GC.SuppressFinalize(this);}
}
