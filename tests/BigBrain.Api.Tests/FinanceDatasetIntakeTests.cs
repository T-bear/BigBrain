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

    private static DatasetValidationSummary Summary(IEnumerable<DatasetGateResult> gates)=>new([..gates],"sha256:test",100,1,new(2024,1,1),new(2024,6,1),0,0,0,DatasetComparisonClass.Consistent,[]);
    private static ExternalDatasetCandidate WikiCandidate()=>new("wiki-fixture","NASDAQ-WIKI","https://github.com/example","GitHub Git LFS","wiki.csv",new(DatasetLicenseClass.PublicDomain,"Public domain","https://docs.data.nasdaq.com",new(2026,8,15),"fixture",DatasetEvidenceResult.Pass,true,"Nasdaq"),"fixture mirror",DatasetPriceBasis.RawAndAdjusted,DatasetSurvivorshipBias.SurvivorshipUnknown);

    private sealed class IntakeFixture:IDisposable
    {
        private readonly string _root=Path.Combine(Path.GetTempPath(),"bb-dataset-tests",Guid.NewGuid().ToString("N"));private readonly EodhdFinanceOptions _market;
        internal IntakeFixture(){_market=new(){DatabasePath=Path.Combine(_root,"finance.db"),PayloadDirectory=Path.Combine(_root,"payloads")};_=new EodhdMarketMemory(_market);Store=new(_market,new(){QuarantineDirectory=Path.Combine(_root,"quarantine"),MinimumFreeBytesAfterDownload=0});}
        internal FinanceDatasetIntakeStore Store{get;}internal string Write(string name,string content){Directory.CreateDirectory(_root);var path=Path.Combine(_root,name);File.WriteAllText(path,content);return path;}
        internal string Zip(string name,Action<ZipArchive> write){Directory.CreateDirectory(_root);var path=Path.Combine(_root,name);using var archive=ZipFile.Open(path,ZipArchiveMode.Create);write(archive);return path;}
        internal int Count(string table,string where){using var c=new Microsoft.Data.Sqlite.SqliteConnection(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder{DataSource=_market.DatabasePath}.ToString());c.Open();using var x=c.CreateCommand();x.CommandText=$"SELECT COUNT(*) FROM {table} WHERE {where}";return Convert.ToInt32(x.ExecuteScalar(),CultureInfo.InvariantCulture);}
        public void Dispose(){if(Directory.Exists(_root))Directory.Delete(_root,true);}
    }
}
