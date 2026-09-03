using System.Globalization;
using System.Text.Json;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

public enum ResearchCampaignStatus { Completed }
public enum ResearchCampaignDisposition { Rejected, InconclusiveNotEvaluable, SurvivedInitialScreen, RobustCandidate }
public sealed record ResearchCampaignLimits(int MaximumFamilies,int MaximumVariantsPerFamily,int MaximumInstruments,int MaximumRuns,int MaximumRobustnessVariants,int MaximumConcurrency,int MaximumRetries,int MaximumWallSeconds);
public sealed record ResearchCampaignVariant(string HypothesisId,string FamilyId,string StrategyId,IReadOnlyDictionary<string,decimal> Parameters);
public sealed record ResearchCampaignDefinition(string SchemaVersion,DateTimeOffset KnowledgeTimeUtc,string EngineVersion,string FeatureLibraryVersion,string BacktestPolicyVersion,string ExecutionPolicyVersion,string IntegrityPolicyVersion,string EligibilityPolicyVersion,string RobustnessPolicyVersion,IReadOnlyList<string> DatasetRevisionIds,IReadOnlyList<ResearchCampaignVariant> Population,ResearchCampaignLimits Limits,int DeterministicSeed,IReadOnlyList<string> Limitations);
public sealed record ResearchCampaignResult(string ResultId,string HypothesisId,string FamilyId,string Instrument,string DatasetRevisionId,string DatasetFingerprint,int FamilyAttemptOrdinal,ResearchCampaignDisposition Disposition,IReadOnlyList<string> ReasonCodes,IReadOnlyList<string> Limitations,string? BacktestRunId);
public sealed record ResearchCampaignScorecard(int TotalAttempts,int Rejected,int InconclusiveNotEvaluable,int SurvivedInitialScreen,int RobustCandidates,IReadOnlyDictionary<string,int> Reasons,string OrderingRule,string SafetyState);
public sealed record ResearchCampaign(string CampaignId,string Checksum,ResearchCampaignStatus Status,DateTimeOffset CreatedUtc,ResearchCampaignDefinition Definition,IReadOnlyList<ResearchCampaignResult> Results,ResearchCampaignScorecard Scorecard);
public sealed record ResearchCampaignSummary(string CampaignId,string Checksum,ResearchCampaignStatus Status,DateTimeOffset CreatedUtc,int TotalAttempts,int RobustCandidates);
public sealed record ResearchCampaignCatalog(string OperatingMode,decimal BudgetSek,string ExecutionAuthority,IReadOnlyList<ResearchCampaignSummary> Campaigns);

internal static class FinanceResearchCampaignPolicy
{
    internal const string SchemaVersion="finance-research-campaign-v1";
    internal static readonly ResearchCampaignLimits Limits=new(2,3,8,24,5,1,0,300);
    internal static IReadOnlyList<ResearchCampaignVariant> Population()
    {
        ResearchCampaignVariant[] variants=[
            new("momentum-20-v1","momentum-v1","momentum/v1",new Dictionary<string,decimal>{{"lookback",20}}),
            new("sma-10-40-v1","trend-v1","sma-crossover/v1",new Dictionary<string,decimal>{{"fast",10},{"slow",40}}),
            new("sma-20-80-v1","trend-v1","sma-crossover/v1",new Dictionary<string,decimal>{{"fast",20},{"slow",80}})];
        if(variants.Select(x=>x.FamilyId).Distinct().Count()>Limits.MaximumFamilies||variants.GroupBy(x=>x.FamilyId).Any(x=>x.Count()>Limits.MaximumVariantsPerFamily))throw new InvalidOperationException("Predeclared campaign population exceeds its hard bounds.");
        return variants;
    }
    internal static (ResearchCampaignDisposition,string) Disposition(bool eligible,bool compatible,bool integrityPass,bool oosPass,bool holdoutClean,bool robustnessPass,bool costsPass)
    {
        if(!eligible)return(ResearchCampaignDisposition.InconclusiveNotEvaluable,"DATASET_INELIGIBLE");
        if(!compatible)return(ResearchCampaignDisposition.InconclusiveNotEvaluable,"SCHEMA_INCOMPATIBLE");
        if(!holdoutClean)return(ResearchCampaignDisposition.Rejected,"HOLDOUT_CONTAMINATED");
        if(!integrityPass)return(ResearchCampaignDisposition.Rejected,"RESEARCH_INTEGRITY_FAILURE");
        if(!oosPass)return(ResearchCampaignDisposition.Rejected,"OOS_FAILURE");
        if(!costsPass)return(ResearchCampaignDisposition.Rejected,"COST_FRAGILE");
        if(!robustnessPass)return(ResearchCampaignDisposition.Rejected,"ROBUSTNESS_FAILURE");
        return(ResearchCampaignDisposition.RobustCandidate,"ROBUSTNESS_PASS");
    }
}

internal sealed partial class FinanceDatasetIntakeStore
{
    private static void InitializeResearchCampaignStorage(SqliteConnection c)=>Exec(c,"CREATE TABLE IF NOT EXISTS research_campaigns(campaign_id TEXT PRIMARY KEY,checksum TEXT NOT NULL UNIQUE,status TEXT NOT NULL,created_utc TEXT NOT NULL,definition_json TEXT NOT NULL,results_json TEXT NOT NULL,scorecard_json TEXT NOT NULL)");
    internal ResearchCampaign RunResearchCampaign(DateTimeOffset knowledgeTimeUtc)
    {
        var datasets=ResearchCatalog().Datasets.OrderBy(x=>x.RevisionId,StringComparer.Ordinal).Take(FinanceResearchCampaignPolicy.Limits.MaximumInstruments).ToArray();var population=FinanceResearchCampaignPolicy.Population();
        var definition=new ResearchCampaignDefinition(FinanceResearchCampaignPolicy.SchemaVersion,knowledgeTimeUtc,FinanceResearchContracts.EngineVersion,FinanceResearchFeatureLibrary.Version,DeterministicBacktestEngine.SimulationModel,"daily-next-session-open-v2/next-session-open-full-fill-v2",FinanceResearchContracts.IntegrityVersion,ResearchDatasetEligibilityPolicyV1.Id,"finance-robustness-v2",datasets.Select(x=>x.RevisionId).ToArray(),population,FinanceResearchCampaignPolicy.Limits,0,["Owner research evidence is noncanonical.","Unknown price basis and corporate-action semantics prohibit adjusted/total-return claims."]);
        var checksum=FinanceResearchContracts.Fingerprint(definition);var campaignId="campaign-"+checksum[7..23];using var c=new SqliteConnection(ConnectionString);c.Open();InitializeResearchCampaignStorage(c);if(ReadCampaign(c,campaignId)is{} existing)return existing;
        var results=new List<ResearchCampaignResult>();var familyAttempts=new Dictionary<string,int>(StringComparer.Ordinal);
        foreach(var dataset in datasets)foreach(var variant in population){if(results.Count>=definition.Limits.MaximumRuns)throw new InvalidOperationException("Campaign run limit exceeded.");var attempt=familyAttempts.TryGetValue(variant.FamilyId,out var n)?n+1:1;familyAttempts[variant.FamilyId]=attempt;var capability=dataset.Capabilities.Single(x=>x.Purpose==ResearchDatasetPurpose.TrainValidationHoldout);var compatible=dataset.SchemaClass==ResearchDatasetClass.DailyOhlcv.ToString();var reason=capability.State==ResearchEligibilityState.Ineligible?"DATASET_INELIGIBLE":!compatible?"SCHEMA_INCOMPATIBLE":"INSUFFICIENT_DATA";results.Add(new("campaign-result-"+FinanceResearchContracts.Fingerprint(new{campaignId,dataset.RevisionId,variant.HypothesisId})[7..23],variant.HypothesisId,variant.FamilyId,dataset.Symbol,dataset.RevisionId,dataset.DatasetFingerprint,attempt,ResearchCampaignDisposition.InconclusiveNotEvaluable,[reason],dataset.Limitations.Concat(capability.ReasonCodes).Distinct().Order().ToArray(),null));}
        var reasons=results.SelectMany(x=>x.ReasonCodes).GroupBy(x=>x).OrderBy(x=>x.Key).ToDictionary(x=>x.Key,x=>x.Count());var score=new ResearchCampaignScorecard(results.Count,0,results.Count,0,0,reasons,"categorical-v1: eligibility, compatibility, integrity, OOS, clean holdout, costs, robustness; returns never override a failed gate","RESEARCH / 0 SEK / NONE");var campaign=new ResearchCampaign(campaignId,checksum,ResearchCampaignStatus.Completed,knowledgeTimeUtc,definition,results,score);
        Exec(c,"INSERT OR IGNORE INTO research_campaigns VALUES($id,$checksum,$status,$created,$definition,$results,$score)",( "$id",campaignId),("$checksum",checksum),("$status",campaign.Status.ToString()),("$created",knowledgeTimeUtc.ToString("O",CultureInfo.InvariantCulture)),("$definition",JsonSerializer.Serialize(definition,ResearchJson)),("$results",JsonSerializer.Serialize(results,ResearchJson)),("$score",JsonSerializer.Serialize(score,ResearchJson)));return ReadCampaign(c,campaignId)!;
    }
    internal ResearchCampaignCatalog ResearchCampaigns(){using var c=new SqliteConnection(ConnectionString);c.Open();InitializeResearchCampaignStorage(c);using var q=c.CreateCommand();q.CommandText="SELECT campaign_id FROM research_campaigns ORDER BY created_utc DESC,campaign_id";using var r=q.ExecuteReader();var ids=new List<string>();while(r.Read())ids.Add(r.GetString(0));r.Close();return new("RESEARCH",0m,"NONE",ids.Select(x=>ReadCampaign(c,x)!).Select(x=>new ResearchCampaignSummary(x.CampaignId,x.Checksum,x.Status,x.CreatedUtc,x.Scorecard.TotalAttempts,x.Scorecard.RobustCandidates)).ToArray());}
    internal ResearchCampaign? ResearchCampaign(string id){using var c=new SqliteConnection(ConnectionString);c.Open();InitializeResearchCampaignStorage(c);return ReadCampaign(c,id);}
    private static ResearchCampaign? ReadCampaign(SqliteConnection c,string id){using var q=c.CreateCommand();q.CommandText="SELECT checksum,status,created_utc,definition_json,results_json,scorecard_json FROM research_campaigns WHERE campaign_id=$id";q.Parameters.AddWithValue("$id",id);using var r=q.ExecuteReader();if(!r.Read())return null;return new(id,r.GetString(0),Enum.Parse<ResearchCampaignStatus>(r.GetString(1)),DateTimeOffset.Parse(r.GetString(2),CultureInfo.InvariantCulture),JsonSerializer.Deserialize<ResearchCampaignDefinition>(r.GetString(3),ResearchJson)!,JsonSerializer.Deserialize<ResearchCampaignResult[]>(r.GetString(4),ResearchJson)!,JsonSerializer.Deserialize<ResearchCampaignScorecard>(r.GetString(5),ResearchJson)!);}
}
public interface IFinanceResearchCampaignReader{ResearchCampaignCatalog GetCatalog();ResearchCampaign? GetDetail(string id);}
internal sealed class FinanceResearchCampaignReader(FinanceDatasetIntakeStore store):IFinanceResearchCampaignReader{public ResearchCampaignCatalog GetCatalog()=>store.ResearchCampaigns();public ResearchCampaign? GetDetail(string id)=>store.ResearchCampaign(id);}
