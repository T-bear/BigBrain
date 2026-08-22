using System.Globalization;
using System.Text;
using System.Text.Json;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Tests;

public sealed class FinanceDataProtectionTests
{
    [Fact]
    public void ProviderClassificationIsExplicitAndUnknownRightsFailClosed()
    {
        var wiki=FinanceBackupPolicyV1.Classify("NASDAQ-WIKI","WIKI/PRICES",DatasetPromotionPolicyV1.Id,"PublicDomain","Pass","Promoted",null);
        var eodhd=FinanceBackupPolicyV1.Classify(EodhdMarketMemory.Provider,EodhdMarketMemory.Product,EodhdMarketMemory.Policy,null,null,null,new DateTimeOffset(2026,8,31,0,0,0,TimeSpan.Zero));
        var unknown=FinanceBackupPolicyV1.Classify("Unknown","Unknown","unknown",null,null,null,null);
        Assert.Equal(FinanceBackupEligibility.Eligible,wiki.BackupEligibility);Assert.Equal("Indefinite",wiki.RetentionClass);
        Assert.Equal(FinanceBackupEligibility.Restricted,eodhd.BackupEligibility);Assert.Equal("DeleteAtSubscriptionEnd",eodhd.DeletionRequirement);Assert.Equal(new DateTimeOffset(2026,9,30,0,0,0,TimeSpan.Zero),eodhd.DeletionDeadlineUtc);
        Assert.Equal(FinanceBackupEligibility.Excluded,unknown.BackupEligibility);Assert.False(unknown.RestoreEligible);
    }

    [Fact]
    public void PublicDomainBackupIsDeterministicVerifiableAndRestoresExactIdentity()
    {
        using var fixture=new ProtectionFixture();var promoted=fixture.PromoteWiki();fixture.AddEodhd();var operationalAt=new DateTimeOffset(2026,8,15,17,0,0,TimeSpan.Zero);var opportunity=fixture.Memory.CreateOrReadResearchOpportunity("finance-research-scheduler-v1:2026-08-14",FinanceResearchSchedulerOptions.CurrentVersion,new(2026,8,14),operationalAt,operationalAt);var failed=fixture.Memory.UpdateResearchOpportunity(opportunity.OpportunityId,FinanceResearchOpportunityState.Failed,operationalAt,null,"finance.research.scheduler.unexpected.SqliteException",null);fixture.Memory.RecordResearchOperationsEvaluation(operationalAt,failed);
        var feature=fixture.Memory.BuildFeatures([promoted.CanonicalRevisionId!]);
        var first=fixture.Protection.CreatePublicDomainBackup(new DateTimeOffset(2026,8,15,18,0,0,TimeSpan.Zero),"test");
        var repeated=fixture.Protection.CreatePublicDomainBackup(new DateTimeOffset(2026,8,16,18,0,0,TimeSpan.Zero),"test");
        Assert.Equal(first.BackupId,repeated.BackupId);Assert.Single(first.Revisions);Assert.Equal(promoted.CanonicalRevisionId,first.Revisions[0].RevisionId);
        Assert.Equal(promoted.PromotedObservationCount,first.Revisions[0].ObservationCount);Assert.Contains(feature.RevisionId,first.FeatureRevisionIds);
        Assert.DoesNotContain(first.Sources,x=>x.Provider==EodhdMarketMemory.Provider);Assert.True(fixture.Protection.Verify(first.BackupId));
        var drill=fixture.Protection.DrillRestore(first.BackupId);Assert.True(drill.Verified);Assert.True(drill.RestoredIdentityMatches);Assert.Equal(first.Revisions[0].ObservationCount,drill.ObservationCount);Assert.Single(fixture.Memory.ResearchOperationalIncidents(0,10).Incidents);
        var corruption=fixture.Protection.DrillCorruption(first.BackupId);Assert.True(corruption.ChecksumMismatchDetected);Assert.True(corruption.RestoreRejected);
        var inventory=fixture.Protection.Inventory();Assert.Contains(inventory.SourcePolicies,x=>x.Provider==EodhdMarketMemory.Provider&&x.BackupEligibility==FinanceBackupEligibility.Restricted);Assert.Single(inventory.Backups);
    }

    [Fact]
    public void CorruptionAndIncompleteBackupsAreRejectedAndCrashStagingIsRemoved()
    {
        using var fixture=new ProtectionFixture();fixture.PromoteWiki();var manifest=fixture.Protection.CreatePublicDomainBackup(DateTimeOffset.UtcNow,"test");
        var artifact=Path.Combine(fixture.BackupDirectory,manifest.Artifacts.Single().Path);var original=File.ReadAllBytes(artifact);File.AppendAllText(artifact,"corruption");
        Assert.False(fixture.Protection.Verify(manifest.BackupId));Assert.Throws<InvalidDataException>(()=>fixture.Protection.DrillRestore(manifest.BackupId));File.WriteAllBytes(artifact,original);Assert.True(fixture.Protection.Verify(manifest.BackupId));
        var crash=Path.Combine(fixture.BackupDirectory,".staging-interrupted");Directory.CreateDirectory(crash);File.WriteAllText(Path.Combine(crash,"partial"),"partial");
        _=new FinanceDataProtectionStore(fixture.Market,fixture.ProtectionOptions);Assert.False(Directory.Exists(crash));
        File.WriteAllText(Path.Combine(fixture.BackupDirectory,"incomplete.manifest.json"),JsonSerializer.Serialize(manifest with{BackupId="incomplete",Status="Staging"}));
        Assert.DoesNotContain(fixture.Protection.Inventory().Backups,x=>x.BackupId=="incomplete");
    }

    [Fact]
    public void DiskGateFailsBeforeBackupPublication()
    {
        using var fixture=new ProtectionFixture(minimumFreeBytes:long.MaxValue);fixture.PromoteWiki();
        Assert.Throws<IOException>(()=>fixture.Protection.CreatePublicDomainBackup(DateTimeOffset.UtcNow,"test"));Assert.Empty(Directory.GetFiles(fixture.BackupDirectory,"*.manifest.json"));
    }

    [Fact]
    public void RejectedCleanupRetainsManifestIsIdempotentAndProtectsManualAndCanonicalData()
    {
        using var fixture=new ProtectionFixture();var promoted=fixture.PromoteWiki();var rejected=fixture.RejectCandidate("rejected",DatasetLicenseClass.Incompatible,DatasetEvidenceResult.Fail);var manual=fixture.RejectCandidate("manual",DatasetLicenseClass.CcBy,DatasetEvidenceResult.Unknown);
        var rejectedPayload=fixture.CopyIntoQuarantine(rejected.CandidateId,"rejected.csv");var manualPayload=fixture.CopyIntoQuarantine(manual.CandidateId,"manual.csv");var observations=fixture.Count("observations");
        var first=fixture.Intake.CleanupRejected(DateTimeOffset.UtcNow.AddDays(1));Assert.Equal(1,first.EligibleCandidates);Assert.Equal(1,first.PayloadsDeleted);Assert.True(first.ManifestsRetained>0);Assert.False(File.Exists(rejectedPayload));Assert.True(File.Exists(manualPayload));Assert.Equal(observations,fixture.Count("observations"));Assert.True(fixture.RevisionExists(promoted.CanonicalRevisionId!));
        var second=fixture.Intake.CleanupRejected(DateTimeOffset.UtcNow.AddDays(1));Assert.True(second.Idempotent);Assert.Equal(0,second.PayloadsDeleted);
        var catalog=fixture.Intake.Catalog();Assert.Equal("PayloadDeleted",catalog.Datasets.Single(x=>x.CandidateId==rejected.CandidateId).CleanupState);Assert.True(catalog.Datasets.Single(x=>x.CandidateId==rejected.CandidateId).ManifestRetained);Assert.Equal("Retained",catalog.Datasets.Single(x=>x.CandidateId==manual.CandidateId).CleanupState);
        var crash=fixture.RejectCandidate("crash",DatasetLicenseClass.Incompatible,DatasetEvidenceResult.Fail);var crashPayload=fixture.CopyIntoQuarantine(crash.CandidateId,"crash.csv");fixture.Execute("UPDATE dataset_candidates SET cleanup_state='CleanupPending' WHERE candidate_id='crash'");File.Delete(crashPayload);
        _=new FinanceDatasetIntakeStore(fixture.Market,new(){QuarantineDirectory=Path.Combine(fixture.Root,"quarantine"),MinimumFreeBytesAfterDownload=0});Assert.Equal("PayloadDeleted",fixture.Intake.Catalog().Datasets.Single(x=>x.CandidateId=="crash").CleanupState);
    }

    private sealed class ProtectionFixture:IDisposable
    {
        private readonly string _root=Path.Combine(Path.GetTempPath(),"bb-protection-tests",Guid.NewGuid().ToString("N"));
        internal ProtectionFixture(long minimumFreeBytes=0)
        {
            Market=new(){DatabasePath=Path.Combine(_root,"finance.db"),PayloadDirectory=Path.Combine(_root,"payloads"),EntitlementEndsAtUtc=new DateTimeOffset(2026,8,31,0,0,0,TimeSpan.Zero)};
            Memory=new(Market);Intake=new(Market,new(){QuarantineDirectory=Path.Combine(_root,"quarantine"),MinimumFreeBytesAfterDownload=0});
            BackupDirectory=Path.Combine(_root,"backups");ProtectionOptions=new(){BackupDirectory=BackupDirectory,RestoreStagingDirectory=Path.Combine(_root,"restore"),MinimumFreeBytesAfterOperation=minimumFreeBytes};Protection=new(Market,ProtectionOptions);
        }
        internal EodhdFinanceOptions Market{get;}internal EodhdMarketMemory Memory{get;}internal FinanceDatasetIntakeStore Intake{get;}internal FinanceDataProtectionStore Protection{get;}internal FinanceDataProtectionOptions ProtectionOptions{get;}internal string BackupDirectory{get;}
        internal string Root=>_root;
        internal DatasetCatalogItem PromoteWiki()
        {
            var csv=new StringBuilder("ticker,date,open,high,low,close,volume,ex-dividend,split_ratio,adj_close\n");for(var i=0;i<50;i++){var date=new DateOnly(2016,1,1).AddDays(i);csv.Append(CultureInfo.InvariantCulture,$"AAPL,{date:yyyy-MM-dd},{100+i},{102+i},{99+i},{101+i},{1000+i},0,1,{101+i}\n");}
            var path=Write("wiki.csv",csv.ToString());return Intake.InspectValidatePromote(Candidate("wiki","wiki.csv",DatasetLicenseClass.PublicDomain,DatasetEvidenceResult.Pass),path);
        }
        internal DatasetCatalogItem RejectCandidate(string id,DatasetLicenseClass license,DatasetEvidenceResult provenance)
        {var name=id+".csv";var path=Write(name,"ticker,date,open,high,low,close,volume,adj_close\nAAPL,2024-01-02,100,102,99,101,1000,101\n");return Intake.InspectValidatePromote(Candidate(id,name,license,provenance),path);}
        internal void AddEodhd(){var now=new DateTimeOffset(2026,8,15,12,0,0,TimeSpan.Zero);Memory.Store(EodhdCatalog.Watchlist[0],[new(new(2026,8,14),10,11,9,10,10,100)],Encoding.UTF8.GetBytes("fixture"),new(2026,8,14),new(2026,8,14),now,now,0);}
        internal string CopyIntoQuarantine(string id,string name){var path=Path.Combine(_root,"quarantine",id,"artifact",name);Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.Copy(Path.Combine(_root,name),path);return path;}
        internal int Count(string table){using var c=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Market.DatabasePath}.ToString());c.Open();using var x=c.CreateCommand();x.CommandText=$"SELECT COUNT(*) FROM {table}";return Convert.ToInt32(x.ExecuteScalar(),CultureInfo.InvariantCulture);}
        internal bool RevisionExists(string id){using var c=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Market.DatabasePath}.ToString());c.Open();using var x=c.CreateCommand();x.CommandText="SELECT COUNT(*) FROM revisions WHERE revision_id=$id";x.Parameters.AddWithValue("$id",id);return Convert.ToInt32(x.ExecuteScalar(),CultureInfo.InvariantCulture)==1;}
        internal void Execute(string sql){using var c=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Market.DatabasePath}.ToString());c.Open();using var x=c.CreateCommand();x.CommandText=sql;x.ExecuteNonQuery();}
        private string Write(string name,string content){Directory.CreateDirectory(_root);var path=Path.Combine(_root,name);File.WriteAllText(path,content);return path;}
        private static ExternalDatasetCandidate Candidate(string id,string name,DatasetLicenseClass license,DatasetEvidenceResult provenance)=>new(id,"NASDAQ-WIKI","https://example.invalid","fixture",name,new(license,license.ToString(),"https://example.invalid",new(2026,8,15),"fixture",provenance,true,"fixture"),"fixture",DatasetPriceBasis.RawAndAdjusted,DatasetSurvivorshipBias.SurvivorshipUnknown);
        public void Dispose(){if(Directory.Exists(_root))Directory.Delete(_root,true);}
    }
}
