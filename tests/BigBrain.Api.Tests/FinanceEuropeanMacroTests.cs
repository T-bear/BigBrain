using System.Text;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Tests;

public sealed class FinanceEuropeanMacroTests : IDisposable
{
    private readonly string _root=Path.Combine(Path.GetTempPath(),"bb-091",Guid.NewGuid().ToString("N"));
    private EodhdFinanceOptions Options=>new(){DatabasePath=Path.Combine(_root,"finance.db"),PayloadDirectory=Path.Combine(_root,"payloads")};
    private FinanceMacroMemory Memory()=>new(Options,new FinanceFredOptions{QuarantineDirectory=Path.Combine(_root,"quarantine")});
    private static readonly DateTimeOffset Acquired=new(2026,8,17,18,0,0,TimeSpan.Zero);

    [Theory]
    [InlineData("SECBREPOEFF",1.75,"Percent",null,null)]
    [InlineData("SEKEURPMI",10.999,"SEK per EUR","EUR","SEK")]
    [InlineData("SEKUSDPMI",9.508,"SEK per USD","USD","SEK")]
    public void RiksbankPackPromotesExplicitSemanticsIdempotently(string series,decimal value,string unit,string? @base,string? quote)
    {
        var artifact=Encoding.UTF8.GetBytes($"[{{\"date\":\"2026-08-14\",\"value\":{value.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}]");var memory=Memory();var first=memory.StageAndPromoteRiksbank(series,artifact,Acquired);var second=memory.StageAndPromoteRiksbank(series,artifact,Acquired);Assert.Equal(first,second);var row=memory.AsOf(MacroRegion.Sweden,Acquired,MacroEvidenceClass.RevisedHistoryExploratory).Single();Assert.Equal("RIKSBANK",row.Provider);Assert.Equal(unit,row.Unit);Assert.Equal(@base,row.BaseCurrency);Assert.Equal(quote,row.QuoteCurrency);Assert.Equal(Acquired,row.KnowledgeTimeUtc);
    }

    [Theory]
    [InlineData("EXR.D.USD.EUR.SP00.A","USD","EUR",1.1567)]
    [InlineData("EXR.D.SEK.EUR.SP00.A","SEK","EUR",10.999)]
    public void EcbFxPromotesWithDenominatorAsCanonicalBase(string series,string currency,string denom,decimal expected)
    {
        var csv=$"KEY,FREQ,CURRENCY,CURRENCY_DENOM,TIME_PERIOD,OBS_VALUE,UNIT\n{series},D,{currency},{denom},2026-08-14,{expected.ToString(System.Globalization.CultureInfo.InvariantCulture)},{currency}\n";var memory=Memory();var id=memory.StageAndPromoteEcb(series,Encoding.UTF8.GetBytes(csv),Acquired);Assert.StartsWith("ecb-",id);var row=memory.AsOf(MacroRegion.EuroArea,Acquired,MacroEvidenceClass.RevisedHistoryExploratory).Single();Assert.Equal(denom,row.BaseCurrency);Assert.Equal(currency,row.QuoteCurrency);Assert.Equal(expected,row.Value);
    }

    [Fact]
    public void EcbPublishedMissingObservationRemainsMissing()
    {
        const string series="EXR.D.USD.EUR.SP00.A";var csv=$"KEY,FREQ,CURRENCY,CURRENCY_DENOM,TIME_PERIOD,OBS_VALUE\n{series},D,USD,EUR,2006-12-25,\n{series},D,USD,EUR,2006-12-27,1.31\n";var memory=Memory();memory.StageAndPromoteEcb(series,Encoding.UTF8.GetBytes(csv),Acquired);var rows=memory.AsOf(MacroRegion.EuroArea,Acquired,MacroEvidenceClass.RevisedHistoryExploratory);Assert.Null(rows.Single(x=>x.ReferencePeriod==new DateOnly(2006,12,25)).Value);Assert.Equal(1.31m,rows.Single(x=>x.ReferencePeriod==new DateOnly(2006,12,27)).Value);
    }

    [Fact]
    public void RightsAndMalformedArtifactsFailClosedInQuarantine()
    {
        var memory=Memory();var valid=Encoding.UTF8.GetBytes("[{\"date\":\"2026-08-14\",\"value\":10.999}]");Assert.Throws<InvalidDataException>(()=>memory.StageAndPromoteRiksbank("SEKEURPMI",valid,Acquired,false));Assert.Throws<InvalidDataException>(()=>memory.StageAndPromoteEcb("EXR.D.SEK.EUR.SP00.A",Encoding.UTF8.GetBytes("bad"),Acquired));using var c=new SqliteConnection($"Data Source={Options.DatabasePath}");c.Open();using var x=c.CreateCommand();x.CommandText="SELECT COUNT(*) FROM macro_candidates WHERE promotion_decision='REJECTED' AND canonical_revision_id IS NULL";Assert.Equal(2L,(long)x.ExecuteScalar()!);using var y=c.CreateCommand();y.CommandText="SELECT COUNT(*) FROM macro_revisions";Assert.Equal(0L,(long)y.ExecuteScalar()!);
    }

    [Fact]
    public void CrossProviderComparisonHandlesToleranceMismatchAndMissingDays()
    {
        var day=new DateOnly(2026,8,14);MacroObservation Fx(string provider,decimal value,DateOnly? date=null)=>new("fx",date??day,value,Acquired,Acquired,day,new(9999,12,31),"sha256:x",MacroEvidenceClass.RevisedHistoryExploratory,provider,provider=="ECB"?MacroRegion.EuroArea:MacroRegion.Sweden,"SEK per EUR","Daily","EUR","SEK");
        Assert.Equal("CONSISTENT",FxCrossProviderValidator.CompareEurSek([Fx("RIKSBANK",10.999m),Fx("ECB",10.99905m)]).Single().Classification);
        Assert.Equal("EXPECTED_METHODOLOGY_DIFFERENCE",FxCrossProviderValidator.CompareEurSek([Fx("RIKSBANK",10.999m),Fx("ECB",11.001m)]).Single().Classification);
        Assert.Equal("MISMATCH",FxCrossProviderValidator.CompareEurSek([Fx("RIKSBANK",10.9m),Fx("ECB",11.1m)]).Single().Classification);
        Assert.Contains(FxCrossProviderValidator.CompareEurSek([Fx("RIKSBANK",10.9m),Fx("ECB",10.9m,day.AddDays(1))]),x=>x.Classification=="INSUFFICIENT_COMPARABILITY");
    }

    [Fact]
    public void MultiRegionAsOfNeverLeaksLaterOrRevisedEvidence()
    {
        var early=new DateTimeOffset(2026,1,2,0,0,0,TimeSpan.Zero);var late=early.AddDays(1);MacroObservation Row(string id,string provider,MacroRegion region,DateTimeOffset known,MacroEvidenceClass evidence,decimal value)=>new(id,new(2026,1,1),value,known,late,new(2026,1,1),new(9999,12,31),"sha256:x",evidence,provider,region);
        var rows=new[]{Row("DFF","FRED",MacroRegion.Us,early,MacroEvidenceClass.PointInTimeCausal,1),Row("SECBREPOEFF","RIKSBANK",MacroRegion.Sweden,early,MacroEvidenceClass.PointInTimeCausal,2),Row("ECB","ECB",MacroRegion.EuroArea,early,MacroEvidenceClass.PointInTimeCausal,3),Row("SECBREPOEFF","RIKSBANK",MacroRegion.Sweden,late,MacroEvidenceClass.PointInTimeCausal,4),Row("SECBREPOEFF","RIKSBANK",MacroRegion.Sweden,early,MacroEvidenceClass.RevisedHistoryExploratory,99)};
        Assert.Equal(1,MacroAsOf.Select(rows,MacroRegion.Us,early,MacroEvidenceClass.PointInTimeCausal).Single().Value);Assert.Equal(2,MacroAsOf.Select(rows,MacroRegion.Sweden,early,MacroEvidenceClass.PointInTimeCausal).Single().Value);Assert.Equal(3,MacroAsOf.Select(rows,MacroRegion.EuroArea,early,MacroEvidenceClass.PointInTimeCausal).Single().Value);Assert.DoesNotContain(MacroAsOf.Select(rows,MacroRegion.Sweden,early,MacroEvidenceClass.PointInTimeCausal),x=>x.Value is 4 or 99);
    }

    public void Dispose(){if(Directory.Exists(_root))Directory.Delete(_root,true);GC.SuppressFinalize(this);}
}
