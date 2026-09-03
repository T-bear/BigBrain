using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceResearchCampaignTests
{
    [Fact]
    public void PopulationIsDeterministicBoundedAndPredeclared()
    {
        var first=FinanceResearchCampaignPolicy.Population();var second=FinanceResearchCampaignPolicy.Population();
        Assert.Equal(FinanceResearchContracts.Fingerprint(first),FinanceResearchContracts.Fingerprint(second));Assert.Equal(3,first.Count);Assert.True(first.Select(x=>x.FamilyId).Distinct().Count()<=FinanceResearchCampaignPolicy.Limits.MaximumFamilies);
        Assert.All(first.GroupBy(x=>x.FamilyId),x=>Assert.True(x.Count()<=FinanceResearchCampaignPolicy.Limits.MaximumVariantsPerFamily));
        Assert.Equal(24,FinanceResearchCampaignPolicy.Limits.MaximumRuns);Assert.Equal(1,FinanceResearchCampaignPolicy.Limits.MaximumConcurrency);Assert.Equal(0,FinanceResearchCampaignPolicy.Limits.MaximumRetries);
    }

    [Theory]
    [InlineData(false,true,true,true,true,true,true,ResearchCampaignDisposition.InconclusiveNotEvaluable,"DATASET_INELIGIBLE")]
    [InlineData(true,false,true,true,true,true,true,ResearchCampaignDisposition.InconclusiveNotEvaluable,"SCHEMA_INCOMPATIBLE")]
    [InlineData(true,true,true,true,false,true,true,ResearchCampaignDisposition.Rejected,"HOLDOUT_CONTAMINATED")]
    [InlineData(true,true,false,true,true,true,true,ResearchCampaignDisposition.Rejected,"RESEARCH_INTEGRITY_FAILURE")]
    [InlineData(true,true,true,false,true,true,true,ResearchCampaignDisposition.Rejected,"OOS_FAILURE")]
    [InlineData(true,true,true,true,true,true,false,ResearchCampaignDisposition.Rejected,"COST_FRAGILE")]
    [InlineData(true,true,true,true,true,false,true,ResearchCampaignDisposition.Rejected,"ROBUSTNESS_FAILURE")]
    [InlineData(true,true,true,true,true,true,true,ResearchCampaignDisposition.RobustCandidate,"ROBUSTNESS_PASS")]
    public void DispositionIsCategoricalAndFailedGatesOverrideReturn(bool eligible,bool compatible,bool integrity,bool oos,bool holdout,bool robustness,bool costs,ResearchCampaignDisposition expected,string reason)
    {var actual=FinanceResearchCampaignPolicy.Disposition(eligible,compatible,integrity,oos,holdout,robustness,costs);Assert.Equal((expected,reason),actual);}
}
