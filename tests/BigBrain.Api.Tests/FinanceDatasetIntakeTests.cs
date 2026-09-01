using System.IO.Compression;
using System.Text;
using System.Globalization;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceDatasetIntakeTests
{
    [Fact]
    public void StateMachineRejectsSkippedAndTerminalTransitions()
    {
        DatasetCandidateStateMachine.EnsureTransition(DatasetCandidateState.Discovered,DatasetCandidateState.Downloading);
        Assert.Throws<InvalidOperationException>(()=>DatasetCandidateStateMachine.EnsureTransition(DatasetCandidateState.Discovered,DatasetCandidateState.Promoted));
        Assert.Throws<InvalidOperationException>(()=>DatasetCandidateStateMachine.EnsureTransition(DatasetCandidateState.Superseded,DatasetCandidateState.Approved));
    }

    [Fact]
    public void ChecksumAndSchemaFingerprintAreDeterministic()
    {
        using var a=new MemoryStream(Encoding.UTF8.GetBytes("safe fixture"));using var b=new MemoryStream(Encoding.UTF8.GetBytes("safe fixture"));
        Assert.Equal(DatasetContentIdentity.Sha256(a),DatasetContentIdentity.Sha256(b));
        Assert.Equal(DatasetContentIdentity.SchemaFingerprint(["Ticker"," Date "]),DatasetContentIdentity.SchemaFingerprint(["ticker","date"]));
    }

    [Fact]
    public void PromotionPolicyPassRejectAndUnknownAreFailClosed()
    {
        var pass=Summary(Enum.GetValues<DatasetGate>().Select(x=>new DatasetGateResult(x,DatasetEvidenceResult.Pass,"pass","pass")));
        Assert.True(DatasetPromotionPolicyV1.Decide(pass).AutomaticallyPromote);
        Assert.Equal(DatasetCandidateState.Rejected,DatasetPromotionPolicyV1.Decide(Summary(pass.Gates.Select(x=>x.Gate==DatasetGate.Ohlcv?x with{Result=DatasetEvidenceResult.Fail}:x))).State);
        Assert.Equal(DatasetCandidateState.ManualReviewRequired,DatasetPromotionPolicyV1.Decide(Summary(pass.Gates.Select(x=>x.Gate==DatasetGate.Provenance?x with{Result=DatasetEvidenceResult.Unknown}:x))).State);
    }

    [Fact]
    public void CrossSourceComparisonIsDeterministicAndClassifiesMaterialConflict()
    {
        var a=Enumerable.Range(1,25).Select(i=>new DatasetComparableBar("AAPL",new DateOnly(2024,1,1).AddDays(i),10,11,9,10,100,DatasetPriceBasis.Raw)).ToArray();
        var b=a.Select(x=>x with{Close=20}).ToArray();var first=DatasetCrossSourceComparerV1.Compare(a,b);var second=DatasetCrossSourceComparerV1.Compare(a.Reverse(),b.Reverse());
        Assert.Equal(DatasetComparisonClass.MaterialConflict,first.Classification);Assert.Equal(first with{AbsoluteRelativeVolumeDifferences=[]},second with{AbsoluteRelativeVolumeDifferences=[]});Assert.Equal(first.AbsoluteRelativeVolumeDifferences,second.AbsoluteRelativeVolumeDifferences);
    }

    [Fact]
    public void CsvParserHandlesQuotesAndRejectsUnterminatedField()
    {
        Assert.Equal(["AAPL","hello, world","3"],FinanceDatasetIntakeStore.Csv("AAPL,\"hello, world\",3"));
        Assert.Throws<InvalidDataException>(()=>FinanceDatasetIntakeStore.Csv("AAPL,\"broken"));
    }

    [Fact]
    public void CompatibleFixtureAutomaticallyPromotesBoundedMappedScopeAndIsIdempotent()
    {
        using var fixture=new IntakeFixture();var candidate=WikiCandidate();var csv=fixture.Write("wiki.csv","ticker,date,open,high,low,close,volume,ex-dividend,split_ratio,adj_open,adj_high,adj_low,adj_close,adj_volume\nAAPL,2016-01-04,100,102,99,101,1000,0,1,100,102,99,101,1000\nMSFT,2016-01-04,50,51,49,50.5,2000,0,1,50,51,49,50.5,2000\nUNMAPPED,2016-01-04,10,11,9,10,100,0,1,10,11,9,10,100\n");
        var result=fixture.Store.InspectValidatePromote(candidate,csv);Assert.Equal("Promoted",result.Status);Assert.Equal(["AAPL","MSFT"],result.PromotedSymbols);Assert.NotNull(result.CanonicalRevisionId);
        var repeated=fixture.Store.InspectValidatePromote(candidate,csv);Assert.Equal(result.CanonicalRevisionId,repeated.CanonicalRevisionId);Assert.Equal(result.ArtifactSha256,repeated.ArtifactSha256);Assert.Equal(result.PromotedSymbols,repeated.PromotedSymbols);
        var changed=fixture.Write("changed.csv",File.ReadAllText(csv)+"AAPL,2016-01-05,101,103,100,102,900,0,1,101,103,100,102,900\n");Assert.Throws<InvalidDataException>(()=>fixture.Store.InspectValidatePromote(candidate,changed));
        Assert.Equal(2,fixture.Count("observations","provider='NASDAQ-WIKI'"));Assert.Equal(1,fixture.Count("revisions","revision_id LIKE 'wiki-%'"));
    }

    [Fact]
    public void ZipTraversalIsRejectedBeforeExtraction()
    {
        using var fixture=new IntakeFixture();var zip=fixture.Zip("unsafe.zip",archive=>{var entry=archive.CreateEntry("../escape.csv");using var writer=new StreamWriter(entry.Open());writer.Write("ticker,date,open,high,low,close,volume\nAAPL,2024-01-02,1,1,1,1,1\n");});
        Assert.Throws<InvalidDataException>(()=>fixture.Store.InspectValidatePromote(WikiCandidate() with{CandidateId="unsafe-zip",OriginalFilename="unsafe.zip"},zip));
    }

    [Fact]
    public void UnknownUnderlyingRightsRemainManualReviewAndPublishNothing()
    {
        using var fixture=new IntakeFixture();var csv=fixture.Write("zenodo.csv","ticker,date,open,high,low,close,volume,adj_close\nAAPL,2024-01-02,100,102,99,101,1000,90\n");var rights=new DatasetRightsEvidence(DatasetLicenseClass.CcBy,"CC BY 4.0","https://zenodo.org/records/fixture",new DateOnly(2026,8,15),"fixture",DatasetEvidenceResult.Unknown,true,"fixture attribution");
        var candidate=new ExternalDatasetCandidate("zenodo-fixture","Zenodo","https://zenodo.org/records/fixture","Zenodo","fixture.csv",rights,"Yahoo provenance unresolved",DatasetPriceBasis.RawAndAdjusted,DatasetSurvivorshipBias.CurrentConstituentsOnly);
        var result=fixture.Store.InspectValidatePromote(candidate,csv);Assert.Equal("ManualReviewRequired",result.Status);Assert.Null(result.CanonicalRevisionId);Assert.Equal(0,fixture.Count("observations","provider='Zenodo'"));
    }

    [Fact]
    public void OwnerDropRequiresReadyMarkerAndReportsMissingDataAsWaiting()
    {
        using var fixture=new IntakeFixture();fixture.Drop("not-ready.csv",CsvRows());
        Assert.Empty(fixture.Scanner.ScanOnce());
        fixture.Drop("missing.csv.ready","");
        var waiting=Assert.Single(fixture.Scanner.ScanOnce());
        Assert.Equal("Waiting",waiting.Status);Assert.Equal("dataFileMissingOrNotRegular",waiting.Reason);
    }

    [Fact]
    public void OwnerCsvIsQuarantinedInspectedIdempotentlyAndNeverAutomaticallyPromoted()
    {
        using var fixture=new IntakeFixture();fixture.ReadyDrop("owner.csv",CsvRows());
        var first=Assert.Single(fixture.Scanner.ScanOnce());
        Assert.Equal("ManualReviewRequired",first.Status);Assert.NotNull(first.Inspection);
        Assert.Equal("Unknown",first.Inspection!.LicenseClass);Assert.Equal("Unknown",first.Inspection.ProvenanceResult);
        Assert.Equal(2,first.Inspection.ObservationCount);Assert.Equal(2,first.Inspection.SafelyMappedInstruments);
        Assert.Equal("PASS",first.Inspection.TechnicalQuality);Assert.Equal("HUMAN_CONFIRMATION_REQUIRED",first.Inspection.RightsStatus);
        Assert.Equal("BLOCKED",first.Inspection.PromotionEligibility);Assert.Equal("UTF-8",first.Inspection.Encoding);
        Assert.Equal("comma",first.Inspection.Delimiter);Assert.Contains("ticker",first.Inspection.Headers);
        Assert.Null(first.Inspection.CanonicalRevisionId);Assert.Equal(0,fixture.Count("observations","provider LIKE 'OWNER-DROP%'") );
        var repeated=Assert.Single(fixture.Scanner.ScanOnce());
        Assert.Equal(first.CandidateId,repeated.CandidateId);Assert.Equal(first.ArtifactSha256,repeated.ArtifactSha256);
        Assert.Single(fixture.Store.Catalog().Datasets);
    }

    [Fact]
    public void ChangedOwnerBytesCreateDistinctImmutableCandidates()
    {
        using var fixture=new IntakeFixture();fixture.ReadyDrop("owner.csv",CsvRows());
        var first=Assert.Single(fixture.Scanner.ScanOnce());
        fixture.Drop("owner.csv",CsvRows()+"AAPL,2024-01-03,101,103,100,102,900\n");
        var second=Assert.Single(fixture.Scanner.ScanOnce());
        Assert.NotEqual(first.CandidateId,second.CandidateId);Assert.NotEqual(first.ArtifactSha256,second.ArtifactSha256);
        Assert.Equal(2,fixture.Store.Catalog().Datasets.Count);
    }

    [Fact]
    public void OwnerZipUsesExistingSafeCsvPathAndArchiveBombLimitFailsClosed()
    {
        using var fixture=new IntakeFixture();fixture.ReadyZip("owner.zip",archive=>
        {var entry=archive.CreateEntry("prices.csv");using var writer=new StreamWriter(entry.Open());writer.Write(CsvRows());});
        Assert.Equal("ManualReviewRequired",Assert.Single(fixture.Scanner.ScanOnce()).Status);

        using var bounded=new IntakeFixture(maximumExtractedBytes:20);bounded.ReadyZip("large.zip",archive=>
        {var entry=archive.CreateEntry("prices.csv");using var writer=new StreamWriter(entry.Open());writer.Write(CsvRows());});
        var rejected=Assert.Single(bounded.Scanner.ScanOnce());
        Assert.Equal("Rejected",rejected.Status);Assert.Equal("invalidOrUnsafeSidecar",rejected.Reason);
        Assert.Null(rejected.Inspection);
    }

    [Fact]
    public void OwnerZipReadsMatchingEmbeddedSidecarAndSeparatesOwnerApprovalFromExternalRights()
    {
        using var fixture=new IntakeFixture();fixture.ReadyZip("owner-package.zip",archive=>
        {
            var csv=archive.CreateEntry("goog.csv");using(var writer=new StreamWriter(csv.Open()))
                writer.Write("ticker,date,open,high,low,close,volume\nGOOG,2024-01-02,100,101,99,100,0\nGOOG,2024-01-05,20,21,19,20,100\n");
            var metadata=archive.CreateEntry("goog.metadata.json");using var metadataWriter=new StreamWriter(metadata.Open());
            metadataWriter.Write("{\"sourceProvider\":\"Google Finance / GOOGLEFINANCE\",\"declaredLicense\":\"OWNER_APPROVED\",\"permissionReference\":\"OWNER_APPROVED_BY_OWNER_TEST\",\"priceBasis\":\"RAW\",\"expectedSymbols\":[\"GOOG\"],\"expectedMarket\":\"NASDAQ\"}");
        });
        var result=Assert.Single(fixture.Scanner.ScanOnce()).Inspection!;
        Assert.Equal("ApprovedByOwner",result.OwnerRightsDecision);Assert.Equal("Unknown",result.ExternalRightsVerification);
        Assert.Equal("RAW",result.OwnerDeclaredPriceBasis);Assert.Equal("Unclear",result.PriceBasis);
        Assert.Equal("HUMAN_CONFIRMATION_REQUIRED",result.RightsStatus);Assert.Equal("BLOCKED",result.PromotionEligibility);
        Assert.Equal(1,result.UnmappedInstruments);Assert.Equal(0,result.SafelyMappedInstruments);
        Assert.Equal(1,result.ZeroVolume);Assert.True(result.MissingSessions>0);Assert.Equal(1,result.SplitLikeJumps);
        Assert.Equal("LIMITED",result.TechnicalQuality);Assert.Null(result.CanonicalRevisionId);
    }

    [Fact]
    public void OwnerDropRejectsZipSlipUnsafeSidecarAndUnsupportedSchema()
    {
        using var traversal=new IntakeFixture();traversal.ReadyZip("unsafe.zip",archive=>
        {var entry=archive.CreateEntry("../escape.csv");using var writer=new StreamWriter(entry.Open());writer.Write(CsvRows());});
        Assert.Equal("Rejected",Assert.Single(traversal.Scanner.ScanOnce()).Status);

        using var secret=new IntakeFixture();secret.ReadyDrop("secret.csv",CsvRows());
        secret.Drop("secret.metadata.json","{\"ownerNotes\":\"api_key=forbidden\"}");
        var sidecar=Assert.Single(secret.Scanner.ScanOnce());
        Assert.Equal("Rejected",sidecar.Status);Assert.Equal("invalidOrUnsafeSidecar",sidecar.Reason);
        Assert.Empty(secret.Store.Catalog().Datasets);

        using var unknown=new IntakeFixture();unknown.ReadyDrop("unknown.csv","name,value\nAAPL,1\n");
        var schema=Assert.Single(unknown.Scanner.ScanOnce());
        Assert.Equal("Rejected",schema.Status);Assert.Equal("inspectionRejected",schema.Reason);
        Assert.Null(schema.Inspection!.CanonicalRevisionId);
    }

    [Fact]
    public void OwnerDropDoesNotFollowSymlinkedArtifacts()
    {
        if (OperatingSystem.IsWindows()) return;
        using var fixture=new IntakeFixture();var outside=fixture.Write("outside.csv",CsvRows());
        Directory.CreateDirectory(fixture.Options.OwnerDropDirectory);
        File.CreateSymbolicLink(Path.Combine(fixture.Options.OwnerDropDirectory,"linked.csv"),outside);
        fixture.Drop("linked.csv.ready","");
        var result=Assert.Single(fixture.Scanner.ScanOnce());
        Assert.Equal("Waiting",result.Status);Assert.Equal("dataFileMissingOrNotRegular",result.Reason);
        Assert.Empty(fixture.Store.Catalog().Datasets);
    }

    [Fact]
    public void OwnerSidecarIsUnverifiedAndAmbiguousIdentityFailsClosedAcrossRestart()
    {
        using var fixture=new IntakeFixture();fixture.ReadyDrop("unmapped.csv","ticker,date,open,high,low,close,volume\nUNKNOWN,2024-01-02,10,11,9,10,100\n");
        fixture.Drop("unmapped.metadata.json","{\"sourceProvider\":\"Owner Source\",\"originalUrl\":\"https://example.test/data\",\"declaredLicense\":\"owner says permitted\",\"priceBasis\":\"raw\",\"downloadedManually\":true}");
        var result=Assert.Single(fixture.Scanner.ScanOnce());
        Assert.Equal("Rejected",result.Status);Assert.Equal(0,result.Inspection!.SafelyMappedInstruments);
        Assert.Equal(1,result.Inspection.UnmappedInstruments);Assert.Equal("Unclear",result.Inspection.PriceBasis);
        Assert.Contains("possession is not entitlement",result.Inspection.Provenance,StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.Inspection.CanonicalRevisionId);
        var restarted=new FinanceDatasetIntakeStore(fixture.Market,fixture.Options);
        var scanner=new FinanceOwnerDatasetDropScanner(fixture.Options,restarted);
        Assert.Equal(result.CandidateId,Assert.Single(scanner.ScanOnce()).CandidateId);
        Assert.Single(restarted.Catalog().Datasets);
    }

    [Fact]
    public void ReviewOnlyModeStopsEvenAllPassEvidenceBeforeCanonicalPromotion()
    {
        using var fixture=new IntakeFixture();var csv=fixture.Write("review.csv",CsvRows());
        var result=fixture.Store.InspectValidateForReview(WikiCandidate() with{CandidateId="review-only",OriginalFilename="review.csv"},csv);
        Assert.Equal("Approved",result.Status);Assert.Equal("Pass",result.PromotionDecision);
        Assert.Contains("ExplicitPromotionReviewRequired",result.Limitations);
        Assert.Equal("READY_FOR_EXPLICIT_PROMOTION_REVIEW",result.PromotionEligibility);
        Assert.Null(result.CanonicalRevisionId);Assert.Equal(0,fixture.Count("observations","provider='NASDAQ-WIKI'"));
    }

    private static DatasetValidationSummary Summary(IEnumerable<DatasetGateResult> gates)=>new([..gates],"sha256:test",100,1,new(2024,1,1),new(2024,6,1),0,0,0,DatasetComparisonClass.Consistent,[]);
    private static ExternalDatasetCandidate WikiCandidate()=>new("wiki-fixture","NASDAQ-WIKI","https://github.com/example","GitHub Git LFS","wiki.csv",new(DatasetLicenseClass.PublicDomain,"Public domain","https://docs.data.nasdaq.com",new(2026,8,15),"fixture",DatasetEvidenceResult.Pass,true,"Nasdaq"),"fixture mirror",DatasetPriceBasis.RawAndAdjusted,DatasetSurvivorshipBias.SurvivorshipUnknown);

    private sealed class IntakeFixture:IDisposable
    {
        private readonly string _root=Path.Combine(Path.GetTempPath(),"bb-dataset-tests",Guid.NewGuid().ToString("N"));
        internal IntakeFixture(long maximumExtractedBytes=1_000_000_000){Market=new(){DatabasePath=Path.Combine(_root,"finance.db"),PayloadDirectory=Path.Combine(_root,"payloads")};Options=new(){QuarantineDirectory=Path.Combine(_root,"quarantine"),OwnerDropDirectory=Path.Combine(_root,"drop"),MinimumFreeBytesAfterDownload=0,MaximumExtractedBytes=maximumExtractedBytes};_=new EodhdMarketMemory(Market);Store=new(Market,Options);Scanner=new(Options,Store);}
        internal EodhdFinanceOptions Market{get;}internal FinanceDatasetOptions Options{get;}internal FinanceOwnerDatasetDropScanner Scanner{get;}
        internal FinanceDatasetIntakeStore Store{get;}internal string Write(string name,string content){Directory.CreateDirectory(_root);var path=Path.Combine(_root,name);File.WriteAllText(path,content);return path;}
        internal string Drop(string name,string content){Directory.CreateDirectory(Options.OwnerDropDirectory);var path=Path.Combine(Options.OwnerDropDirectory,name);File.WriteAllText(path,content);return path;}
        internal void ReadyDrop(string name,string content){Drop(name,content);Drop(name+".ready","");}
        internal void ReadyZip(string name,Action<ZipArchive> write){Directory.CreateDirectory(Options.OwnerDropDirectory);var path=Path.Combine(Options.OwnerDropDirectory,name);using(var archive=ZipFile.Open(path,ZipArchiveMode.Create))write(archive);Drop(name+".ready","");}
        internal string Zip(string name,Action<ZipArchive> write){Directory.CreateDirectory(_root);var path=Path.Combine(_root,name);using var archive=ZipFile.Open(path,ZipArchiveMode.Create);write(archive);return path;}
        internal int Count(string table,string where){using var c=new Microsoft.Data.Sqlite.SqliteConnection(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder{DataSource=Market.DatabasePath}.ToString());c.Open();using var x=c.CreateCommand();x.CommandText=$"SELECT COUNT(*) FROM {table} WHERE {where}";return Convert.ToInt32(x.ExecuteScalar(),CultureInfo.InvariantCulture);}
        public void Dispose(){if(Directory.Exists(_root))Directory.Delete(_root,true);}
    }

    private static string CsvRows()=>"ticker,date,open,high,low,close,volume\nAAPL,2024-01-02,100,102,99,101,1000\nMSFT,2024-01-02,50,51,49,50.5,2000\n";
}
