using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Finance;

public sealed record FinanceRiskOptions
{
    public const string Section = "Finance:Risk";
    public string PolicyVersion { get; set; } = "research-eod-v1";
    public decimal ResearchCapital { get; set; } = 100_000m;
    public decimal MaximumPositionFraction { get; set; } = 0.05m;
    public decimal MaximumRequestedExposureFraction { get; set; } = 0.10m;
    public decimal MaximumDailyMoveFraction { get; set; } = 0.15m;
    public decimal MaximumRollingVolatility20 { get; set; } = 0.08m;
    public decimal MinimumVolumeRatio { get; set; } = 0.10m;
    public decimal DailyLossHaltFraction { get; set; } = 0.03m;
    public decimal RollingDrawdownHaltFraction { get; set; } = 0.10m;
    public int RollingDrawdownWindowSessions { get; set; } = 20;
    public int ConsecutiveLossesToHalt { get; set; } = 3;
    public int MaximumCompletedSessionsSinceObservation { get; set; } = 1;
    public string ResearchUniverseVersion { get; set; } = "us-research-universe-v1";
    public string[] ApprovedInstrumentIds { get; set; } = ["US:ARCX:SPY","US:XNAS:QQQ","US:ARCX:IWM","US:XNAS:AAPL","US:XNAS:MSFT","US:XNYS:JPM","US:XNYS:XOM","US:XNYS:JNJ"];
    public string StrategyPolicyVersion { get; set; } = "deterministic-research-strategies-v1";
    public string[] ApprovedStrategies { get; set; } = ["buy-and-hold/v1","sma-crossover/v1","momentum/v1"];

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(PolicyVersion) || PolicyVersion.Length > 80 || ResearchCapital <= 0 ||
            MaximumPositionFraction is <= 0 or > 1 || MaximumRequestedExposureFraction is <= 0 or > 1 ||
            MaximumPositionFraction > MaximumRequestedExposureFraction || MaximumDailyMoveFraction is <= 0 or > 1 ||
            MaximumRollingVolatility20 <= 0 || MinimumVolumeRatio < 0 || DailyLossHaltFraction is <= 0 or > 1 ||
            RollingDrawdownHaltFraction is <= 0 or > 1 || RollingDrawdownWindowSessions < 2 ||
            ConsecutiveLossesToHalt < 1 || MaximumCompletedSessionsSinceObservation < 0 ||
            string.IsNullOrWhiteSpace(ResearchUniverseVersion) || string.IsNullOrWhiteSpace(StrategyPolicyVersion) ||
            ApprovedInstrumentIds.Length==0 || ApprovedStrategies.Length==0 ||
            ApprovedInstrumentIds.Any(string.IsNullOrWhiteSpace) || ApprovedStrategies.Any(x=>string.IsNullOrWhiteSpace(x)||!x.Contains('/',StringComparison.Ordinal)))
            throw new InvalidOperationException("Finance risk policy configuration is invalid.");
    }
}

internal enum FinanceRiskVerdict { Allow, Reduce, Deny, Halt, InsufficientData }
internal enum FinanceRiskRuleState { Pass, Fail, NotEvaluable }
internal enum FinanceRiskReasonCategory { None, DataMissing, WarmupIncomplete, StaleData, InvalidLineage, PolicyDenial, SystemHalt }
internal sealed record FinanceRiskRuleResult(string RuleId, FinanceRiskRuleState State, FinanceRiskReasonCategory Category, string ReasonCode, string Explanation, string Evidence);
internal interface IResearchUniversePolicy { string Version { get; } bool IsApproved(string instrumentId); }
internal interface IResearchStrategyPolicy { string Version { get; } bool IsApproved(string strategyId,string version); }
internal sealed class ConfiguredResearchUniversePolicy(FinanceRiskOptions options) : IResearchUniversePolicy
{
    private readonly HashSet<string> _approved=options.ApprovedInstrumentIds.ToHashSet(StringComparer.Ordinal);
    public string Version=>options.ResearchUniverseVersion;
    public bool IsApproved(string id)=>_approved.Contains(id);
}
internal sealed class ConfiguredResearchStrategyPolicy(FinanceRiskOptions options) : IResearchStrategyPolicy
{
    private readonly HashSet<string> _approved=options.ApprovedStrategies.ToHashSet(StringComparer.Ordinal);
    public string Version=>options.StrategyPolicyVersion;
    public bool IsApproved(string id,string version)=>_approved.Contains($"{id}/{version}");
}
internal sealed record FinanceRiskProposal(string ProposalId,string InstrumentId,string StrategyId,string StrategyVersion,
    string ParameterFingerprint,string? ShadowPredictionId,string SourceRevisionId,string FeatureRevisionId,
    DateOnly ObservationSession,DateTimeOffset ObservationKnowledgeUtc,DateTimeOffset KnowledgeCutoffUtc,
    DateTimeOffset EvaluationUtc,string OperatingMode,string Direction,decimal Price,decimal PreviousClose,
    decimal RequestedExposure,decimal? RollingVolatility20,decimal? VolumeRatio,bool ClockIntegrity,bool SourceLineageValid,
    bool FeatureLineageValid,bool WarmupComplete,bool ProviderHealthy,bool CadenceHealthy,decimal HypotheticalDailyLoss,
    decimal HypotheticalRollingDrawdown,int ConsecutiveLosses,string? ClientSuppliedVerdict=null);
internal sealed record FinanceRiskEvaluation(string EvaluationId,string PolicyVersion,string ProposalId,string InstrumentId,
    string StrategyId,string StrategyVersion,string ParameterFingerprint,string? ShadowPredictionId,string SourceRevisionId,
    string FeatureRevisionId,DateTimeOffset KnowledgeCutoffUtc,DateTimeOffset EvaluatedAtUtc,string OperatingMode,string Direction,
    decimal ResearchCapital,decimal RequestedExposure,decimal AllowedExposure,decimal RiskAdjustedExposure,
    FinanceRiskVerdict Verdict,IReadOnlyList<string> ReasonCodes,IReadOnlyList<FinanceRiskRuleResult> Rules,string EvidenceLineage);
internal sealed record FinanceRiskPolicyReadModel(string PolicyVersion,string OperatingMode,decimal ResearchCapital,
    decimal MaximumPositionFraction,decimal MaximumRequestedExposureFraction,decimal MaximumDailyMoveFraction,
    decimal MaximumRollingVolatility20,decimal MinimumVolumeRatio,decimal DailyLossHaltFraction,
    decimal RollingDrawdownHaltFraction,int RollingDrawdownWindowSessions,int ConsecutiveLossesToHalt,
    int MaximumCompletedSessionsSinceObservation,string CapitalSemantics,string SpreadRule,string SectorRule,string ExecutionAuthority);
internal sealed record FinanceRiskStatus(string PolicyVersion,string OperatingMode,string EngineHealth,string SafetyState,
    bool ActiveHalt,string HaltScope,string? HaltReason,DateTimeOffset? HaltedAtUtc,int EvaluationCount,
    DateTimeOffset? LastEvaluationUtc,string ExecutionAuthority);
internal sealed record FinanceRiskHaltAudit(string AuditId,string Scope,string PreviousState,string NewState,string ReasonCode,
    string PolicyVersion,DateTimeOffset ChangedAtUtc,string Evidence);

internal static class FinanceRiskIdentity
{
    internal static string Evaluation(FinanceRiskProposal p,string policyVersion) => "risk-"+Hash($"{policyVersion}|{p.ProposalId}|{p.InstrumentId}|{p.StrategyId}|{p.StrategyVersion}|{p.ParameterFingerprint}|{p.SourceRevisionId}|{p.FeatureRevisionId}|{p.KnowledgeCutoffUtc:O}")[7..23];
    internal static string Audit(string scope,string from,string to,string reason,string policy,DateTimeOffset at)=>"risk-audit-"+Hash($"{scope}|{from}|{to}|{reason}|{policy}|{at:O}")[7..23];
    private static string Hash(string value)=>"sha256:"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

internal sealed class FinanceRiskEngine
{
    private readonly FinanceRiskOptions _policy;
    private readonly IResearchUniversePolicy _universe;
    private readonly IResearchStrategyPolicy _strategies;
    internal FinanceRiskEngine(FinanceRiskOptions policy,IResearchUniversePolicy? universe=null,IResearchStrategyPolicy? strategies=null){policy.Validate();_policy=policy;_universe=universe??new ConfiguredResearchUniversePolicy(policy);_strategies=strategies??new ConfiguredResearchStrategyPolicy(policy);}

    internal FinanceRiskEvaluation Evaluate(FinanceRiskProposal p,bool activeHalt=false,string? haltReason=null)
    {
        var rules=new List<FinanceRiskRuleResult>();
        void Rule(string id,bool pass,string code,string explanation,string evidence,FinanceRiskReasonCategory category=FinanceRiskReasonCategory.PolicyDenial)=>rules.Add(new(id,pass?FinanceRiskRuleState.Pass:FinanceRiskRuleState.Fail,pass?FinanceRiskReasonCategory.None:category,code,explanation,evidence));
        void NotEvaluable(string id,string code,string explanation)=>rules.Add(new(id,FinanceRiskRuleState.NotEvaluable,FinanceRiskReasonCategory.DataMissing,code,explanation,"not available in CURRENT EOD research"));
        Rule("mode.research",p.OperatingMode=="RESEARCH","risk.mode.invalid","Riskmotorn godkänner endast RESEARCH i BB-089.",p.OperatingMode);
        Rule("proposal.client-verdict",string.IsNullOrWhiteSpace(p.ClientSuppliedVerdict),"risk.clientVerdict.rejected","Klienten får inte ange riskutfall.","server authority");
        Rule("instrument.universe",_universe.IsApproved(p.InstrumentId),"risk.instrument.notAllowed","Instrumentet ingår inte i den godkända researchuniversen.",$"{_universe.Version}:{p.InstrumentId}");
        Rule("strategy.approved",_strategies.IsApproved(p.StrategyId,p.StrategyVersion),"risk.strategy.notApproved","Strategin eller versionen är inte godkänd för researchpolicyn.",$"{_strategies.Version}:{p.StrategyId}/{p.StrategyVersion}");
        Rule("signal.known",p.Direction is "TargetLong" or "TargetFlat" or "NoAction","risk.signal.invalid","Strategisignalen är okänd eller ogiltig.",p.Direction);
        Rule("price.positive",p.Price>0&&p.PreviousClose>0,"risk.price.invalid","Prisunderlaget är ogiltigt.","positive close and previous close required");
        Rule("lineage.source",p.SourceLineageValid&&!string.IsNullOrWhiteSpace(p.SourceRevisionId),"risk.lineage.sourceInvalid","Källans lineage är ogiltig eller saknas.",p.SourceRevisionId,FinanceRiskReasonCategory.InvalidLineage);
        Rule("lineage.feature",p.FeatureLineageValid&&!string.IsNullOrWhiteSpace(p.FeatureRevisionId),"risk.lineage.featureInvalid","Feature-lineage är ogiltig eller saknas.",p.FeatureRevisionId,FinanceRiskReasonCategory.InvalidLineage);
        Rule("feature.warmup",p.WarmupComplete,"risk.feature.insufficientWarmup","Featurehistoriken har inte tillräcklig warmup.",p.FeatureRevisionId,FinanceRiskReasonCategory.WarmupIncomplete);
        Rule("clock.integrity",p.ClockIntegrity&&p.EvaluationUtc.Offset==TimeSpan.Zero&&p.ObservationKnowledgeUtc<=p.KnowledgeCutoffUtc&&p.KnowledgeCutoffUtc<=p.EvaluationUtc,"risk.clock.invalid","Tidsintegriteten kan inte verifieras.","UTC causal ordering");
        Rule("provider.health",p.ProviderHealthy,"risk.provider.unhealthy","Dataproviderns hälsa räcker inte för ett nytt riskgodkännande.","provider health");
        Rule("cadence.health",p.CadenceHealthy,"risk.cadence.unhealthy","Finance-cadencen är inte verifierat frisk.","cadence health");
        var age=UsMarketCalendar.CompletedSessionsAfter(p.ObservationSession,DateOnly.FromDateTime(p.EvaluationUtc.UtcDateTime));
        Rule("data.current-eod-freshness",age<=_policy.MaximumCompletedSessionsSinceObservation,"risk.data.stale","Marknadsdatan är för gammal för ett nytt riskgodkännande.",$"completedSessions={age}; maximum={_policy.MaximumCompletedSessionsSinceObservation}",FinanceRiskReasonCategory.StaleData);
        var move=p.PreviousClose>0?Math.Abs(p.Price/p.PreviousClose-1):decimal.MaxValue;
        Rule("market.daily-move",move<=_policy.MaximumDailyMoveFraction,"risk.move.excessive","Den senaste prisrörelsen överskrider policyn.",$"fraction={move.ToString(CultureInfo.InvariantCulture)}");
        Rule("market.volatility",p.RollingVolatility20.HasValue&&p.RollingVolatility20>=0&&p.RollingVolatility20<=_policy.MaximumRollingVolatility20,
            p.RollingVolatility20.HasValue?"risk.volatility.excessive":"risk.volatility.insufficientData","20-sessioners populationsstandardavvikelse för enkla dagsavkastningar saknas eller överskrider policyn.",p.RollingVolatility20?.ToString(CultureInfo.InvariantCulture)??"missing",p.RollingVolatility20.HasValue?FinanceRiskReasonCategory.PolicyDenial:FinanceRiskReasonCategory.DataMissing);
        Rule("market.liquidity",p.VolumeRatio.HasValue&&p.VolumeRatio>=_policy.MinimumVolumeRatio,
            p.VolumeRatio.HasValue?"risk.liquidity.insufficient":"risk.liquidity.insufficientData","Volymunderlaget saknas eller understiger policyn.",p.VolumeRatio?.ToString(CultureInfo.InvariantCulture)??"missing",p.VolumeRatio.HasValue?FinanceRiskReasonCategory.PolicyDenial:FinanceRiskReasonCategory.DataMissing);
        NotEvaluable("market.spread","risk.spread.notAvailable","Tillförlitlig bid/ask-spread finns inte i EODHD Free och fabriceras inte.");
        NotEvaluable("portfolio.sector","risk.sector.notEvaluable","Tillförlitlig sektorklassificering finns inte i denna slice.");
        Rule("exposure.positive",p.RequestedExposure>0,"risk.exposure.invalid","Föreslagen hypotetisk exponering måste vara positiv.",p.RequestedExposure.ToString(CultureInfo.InvariantCulture));
        Rule("exposure.request-cap",p.RequestedExposure<=_policy.ResearchCapital*_policy.MaximumRequestedExposureFraction,"risk.exposure.requestTooLarge","Föreslagen exponering överskrider researchpolicyns absoluta förslagsgräns.",p.RequestedExposure.ToString(CultureInfo.InvariantCulture));
        var breaker=activeHalt||p.HypotheticalDailyLoss>=_policy.DailyLossHaltFraction||p.HypotheticalRollingDrawdown>=_policy.RollingDrawdownHaltFraction||p.ConsecutiveLosses>=_policy.ConsecutiveLossesToHalt;
        Rule("circuit-breaker",!breaker,activeHalt?(haltReason??"risk.halt.active"):p.HypotheticalDailyLoss>=_policy.DailyLossHaltFraction?"risk.dailyLoss.halt":p.HypotheticalRollingDrawdown>=_policy.RollingDrawdownHaltFraction?"risk.drawdown.halt":"risk.consecutiveLosses.halt","En hard risk-spärr blockerar ny hypotetisk exponering.","hypothetical research evidence");

        var failed=rules.Where(x=>x.State==FinanceRiskRuleState.Fail).ToArray();
        var insufficient=failed.Any(x=>x.Category is FinanceRiskReasonCategory.DataMissing or FinanceRiskReasonCategory.WarmupIncomplete);
        var verdict=breaker?FinanceRiskVerdict.Halt:insufficient?FinanceRiskVerdict.InsufficientData:failed.Length>0?FinanceRiskVerdict.Deny:
            p.RequestedExposure>_policy.ResearchCapital*_policy.MaximumPositionFraction?FinanceRiskVerdict.Reduce:FinanceRiskVerdict.Allow;
        var cap=_policy.ResearchCapital*_policy.MaximumPositionFraction;
        var allowed=verdict is FinanceRiskVerdict.Allow or FinanceRiskVerdict.Reduce?Math.Min(p.RequestedExposure,cap):0;
        var reasons=failed.Select(x=>x.ReasonCode).Distinct(StringComparer.Ordinal).ToArray();
        if(verdict==FinanceRiskVerdict.Reduce)reasons=[..reasons,"risk.exposure.reducedToPositionCap"];
        return new(FinanceRiskIdentity.Evaluation(p,_policy.PolicyVersion),_policy.PolicyVersion,p.ProposalId,p.InstrumentId,p.StrategyId,p.StrategyVersion,
            p.ParameterFingerprint,p.ShadowPredictionId,p.SourceRevisionId,p.FeatureRevisionId,p.KnowledgeCutoffUtc,p.EvaluationUtc,p.OperatingMode,p.Direction,
            _policy.ResearchCapital,p.RequestedExposure,allowed,allowed,verdict,reasons,rules,$"source={p.SourceRevisionId}; feature={p.FeatureRevisionId}; proposal={p.ProposalId}");
    }

    internal FinanceRiskPolicyReadModel Policy()=>new(_policy.PolicyVersion,"RESEARCH",_policy.ResearchCapital,_policy.MaximumPositionFraction,
        _policy.MaximumRequestedExposureFraction,_policy.MaximumDailyMoveFraction,_policy.MaximumRollingVolatility20,_policy.MinimumVolumeRatio,
        _policy.DailyLossHaltFraction,_policy.RollingDrawdownHaltFraction,_policy.RollingDrawdownWindowSessions,_policy.ConsecutiveLossesToHalt,
        _policy.MaximumCompletedSessionsSinceObservation,"Hypothetical deterministic research capital; never account cash or buying power.",
        "NOT_EVALUABLE — reliable bid/ask is unavailable","NOT_EVALUABLE — trustworthy sector metadata is unavailable","NONE — no broker or order path exists");

    private static int CompletedWeekdaysAfter(DateOnly observation,DateOnly evaluation){var count=0;for(var day=observation.AddDays(1);day<=evaluation;day=day.AddDays(1))if(day.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)count++;return count;}
}

internal sealed partial class EodhdMarketMemory
{
    private FinanceRiskOptions RiskPolicy => _riskPolicy;
    private readonly FinanceRiskOptions _riskPolicy;
    private static void InitializeRiskStorage(SqliteConnection c){using var x=c.CreateCommand();x.CommandText="""
      CREATE TABLE IF NOT EXISTS risk_evaluations(evaluation_id TEXT PRIMARY KEY,policy_version TEXT NOT NULL,proposal_id TEXT NOT NULL UNIQUE,
        instrument_id TEXT NOT NULL,strategy_id TEXT NOT NULL,strategy_version TEXT NOT NULL,parameter_fingerprint TEXT NOT NULL,
        shadow_prediction_id TEXT,source_revision_id TEXT NOT NULL,feature_revision_id TEXT NOT NULL,knowledge_cutoff_utc TEXT NOT NULL,
        evaluated_utc TEXT NOT NULL,operating_mode TEXT NOT NULL,direction TEXT NOT NULL,research_capital TEXT NOT NULL,
        requested_exposure TEXT NOT NULL,allowed_exposure TEXT NOT NULL,risk_adjusted_exposure TEXT NOT NULL,verdict TEXT NOT NULL,
        reasons_json TEXT NOT NULL,rules_json TEXT NOT NULL,evidence_lineage TEXT NOT NULL);
      CREATE TABLE IF NOT EXISTS risk_halt_state(scope TEXT PRIMARY KEY,state TEXT NOT NULL,reason_code TEXT,policy_version TEXT NOT NULL,changed_utc TEXT NOT NULL,evidence TEXT NOT NULL);
      CREATE TABLE IF NOT EXISTS risk_halt_audit(audit_id TEXT PRIMARY KEY,scope TEXT NOT NULL,previous_state TEXT NOT NULL,new_state TEXT NOT NULL,reason_code TEXT NOT NULL,policy_version TEXT NOT NULL,changed_utc TEXT NOT NULL,evidence TEXT NOT NULL);
      """;x.ExecuteNonQuery();}

    internal FinanceRiskEvaluation RecordRiskEvaluation(FinanceRiskProposal proposal)
    {
        using var c=new SqliteConnection(ConnectionString);c.Open();var halt=ReadHalt(c,"SYSTEM");
        var evaluation=new FinanceRiskEngine(RiskPolicy).Evaluate(proposal,halt.Active,halt.Reason);
        using var x=c.CreateCommand();x.CommandText="INSERT OR IGNORE INTO risk_evaluations VALUES($id,$pv,$p,$i,$s,$sv,$pf,$shadow,$source,$feature,$cutoff,$at,$mode,$direction,$capital,$requested,$allowed,$adjusted,$verdict,$reasons,$rules,$lineage)";
        foreach(var v in new[]{("$id",(object)evaluation.EvaluationId),("$pv",evaluation.PolicyVersion),("$p",evaluation.ProposalId),("$i",evaluation.InstrumentId),("$s",evaluation.StrategyId),("$sv",evaluation.StrategyVersion),("$pf",evaluation.ParameterFingerprint),("$shadow",evaluation.ShadowPredictionId??(object)DBNull.Value),("$source",evaluation.SourceRevisionId),("$feature",evaluation.FeatureRevisionId),("$cutoff",evaluation.KnowledgeCutoffUtc.ToString("O")),("$at",evaluation.EvaluatedAtUtc.ToString("O")),("$mode",evaluation.OperatingMode),("$direction",evaluation.Direction),("$capital",evaluation.ResearchCapital.ToString(CultureInfo.InvariantCulture)),("$requested",evaluation.RequestedExposure.ToString(CultureInfo.InvariantCulture)),("$allowed",evaluation.AllowedExposure.ToString(CultureInfo.InvariantCulture)),("$adjusted",evaluation.RiskAdjustedExposure.ToString(CultureInfo.InvariantCulture)),("$verdict",evaluation.Verdict.ToString()),("$reasons",JsonSerializer.Serialize(evaluation.ReasonCodes)),("$rules",JsonSerializer.Serialize(evaluation.Rules)),("$lineage",evaluation.EvidenceLineage)})x.Parameters.AddWithValue(v.Item1,v.Item2);x.ExecuteNonQuery();
        return RiskEvaluation(evaluation.EvaluationId)!;
    }

    internal IReadOnlyList<FinanceRiskEvaluation> RiskEvaluations(int limit=50){if(limit<1||limit>200)throw new ArgumentException("Limit must be between 1 and 200.");using var c=new SqliteConnection(ConnectionString);c.Open();using var x=c.CreateCommand();x.CommandText="SELECT * FROM risk_evaluations ORDER BY evaluated_utc DESC,evaluation_id LIMIT $limit";x.Parameters.AddWithValue("$limit",limit);using var r=x.ExecuteReader();var list=new List<FinanceRiskEvaluation>();while(r.Read())list.Add(ReadRisk(r));return list;}
    internal FinanceRiskEvaluation? RiskEvaluation(string id){if(string.IsNullOrWhiteSpace(id)||id.Length>80||!id.StartsWith("risk-",StringComparison.Ordinal))throw new ArgumentException("Malformed risk evaluation ID.");return RiskEvaluations(200).SingleOrDefault(x=>x.EvaluationId==id);}
    internal FinanceRiskPolicyReadModel RiskPolicySnapshot()=>new FinanceRiskEngine(RiskPolicy).Policy();
    internal FinanceRiskStatus RiskStatus(){using var c=new SqliteConnection(ConnectionString);c.Open();var h=ReadHalt(c,"SYSTEM");var count=Scalar(c,"SELECT COUNT(*) FROM risk_evaluations");var last=TimeOrNull(c,"SELECT MAX(evaluated_utc) FROM risk_evaluations");return new(RiskPolicy.PolicyVersion,"RESEARCH","Healthy",h.Active?"NEW_EXPOSURE_BLOCKED":"READY",h.Active,"SYSTEM",h.Reason,h.At,count,last,"NONE — research evidence only; no orders");}
    internal FinanceRiskHaltAudit SetRiskHalt(bool active,string reason,DateTimeOffset at,string evidence){if(string.IsNullOrWhiteSpace(reason)||at.Offset!=TimeSpan.Zero)throw new ArgumentException("Audited halt transition requires reason and UTC time.");using var c=new SqliteConnection(ConnectionString);c.Open();var old=ReadHalt(c,"SYSTEM");var from=old.Active?"HALTED":"ACTIVE";var to=active?"HALTED":"ACTIVE";var id=FinanceRiskIdentity.Audit("SYSTEM",from,to,reason,RiskPolicy.PolicyVersion,at);Execute(c,null,"INSERT OR IGNORE INTO risk_halt_audit VALUES($id,'SYSTEM',$from,$to,$reason,$policy,$at,$evidence)",( "$id",id),("$from",from),("$to",to),("$reason",reason),("$policy",RiskPolicy.PolicyVersion),("$at",at.ToString("O")),("$evidence",evidence));Execute(c,null,"INSERT INTO risk_halt_state VALUES('SYSTEM',$state,$reason,$policy,$at,$evidence) ON CONFLICT(scope) DO UPDATE SET state=$state,reason_code=$reason,policy_version=$policy,changed_utc=$at,evidence=$evidence",("$state",to),("$reason",reason),("$policy",RiskPolicy.PolicyVersion),("$at",at.ToString("O")),("$evidence",evidence));return new(id,"SYSTEM",from,to,reason,RiskPolicy.PolicyVersion,at,evidence);}
    private static (bool Active,string? Reason,DateTimeOffset? At) ReadHalt(SqliteConnection c,string scope){using var x=c.CreateCommand();x.CommandText="SELECT state,reason_code,changed_utc FROM risk_halt_state WHERE scope=$scope";x.Parameters.AddWithValue("$scope",scope);using var r=x.ExecuteReader();return r.Read()?(r.GetString(0)=="HALTED",r.IsDBNull(1)?null:r.GetString(1),DateTimeOffset.Parse(r.GetString(2),CultureInfo.InvariantCulture)):(false,null,null);}
    private static FinanceRiskEvaluation ReadRisk(SqliteDataReader r)=>new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetString(6),r.IsDBNull(7)?null:r.GetString(7),r.GetString(8),r.GetString(9),DateTimeOffset.Parse(r.GetString(10),CultureInfo.InvariantCulture),DateTimeOffset.Parse(r.GetString(11),CultureInfo.InvariantCulture),r.GetString(12),r.GetString(13),decimal.Parse(r.GetString(14),CultureInfo.InvariantCulture),decimal.Parse(r.GetString(15),CultureInfo.InvariantCulture),decimal.Parse(r.GetString(16),CultureInfo.InvariantCulture),decimal.Parse(r.GetString(17),CultureInfo.InvariantCulture),Enum.Parse<FinanceRiskVerdict>(r.GetString(18)),JsonSerializer.Deserialize<string[]>(r.GetString(19))??[],JsonSerializer.Deserialize<FinanceRiskRuleResult[]>(r.GetString(20))??[],r.GetString(21));
}
