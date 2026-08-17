using System.Security.Cryptography;
using System.Text;

namespace BigBrain.Modules.Finance;

public enum MacroEvidenceClass { RevisedHistoryExploratory, PointInTimeCausal }
public enum MacroRegion { Us, Sweden, EuroArea, Global }
public sealed record MacroSeriesDefinition(string SeriesId,string Title,string Units,string Frequency,string Source,string RightsClass,string Transformation);
public sealed record MacroObservation(string SeriesId,DateOnly ReferencePeriod,decimal? Value,DateTimeOffset KnowledgeTimeUtc,DateTimeOffset AcquiredAtUtc,DateOnly RealtimeStart,DateOnly RealtimeEnd,string ArtifactHash,MacroEvidenceClass EvidenceClass,string Provider="FRED",MacroRegion Region=MacroRegion.Us,string Unit="",string Frequency="",string? BaseCurrency=null,string? QuoteCurrency=null);
public sealed record MacroFeature(string Id,DateOnly Session,decimal? Value,string Units,string Transformation,DateTimeOffset KnowledgeTimeUtc,string Revision);
public sealed record MarketRegime(string PolicyVersion,DateOnly Session,string RateTrend,string YieldCurve,string Inflation,string Labor,string Composite,string MacroRevision,string FeatureRevision,MacroEvidenceClass EvidenceClass,DateTimeOffset CausalCutoffUtc);

public static class FredMacroPackV1
{
    public const string FeatureRevision="macro-context-v1";
    public const string RegimePolicy="market-regime-v1";
    public static readonly IReadOnlyList<MacroSeriesDefinition> Series=
    [
        new("DFF","Federal Funds Effective Rate","Percent","Daily","Federal Reserve Board via FRED","PublicDomainCitationRequested","level"),
        new("DGS2","2-Year Treasury Constant Maturity Rate","Percent","Daily","Federal Reserve Board via FRED","PublicDomainCitationRequested","level"),
        new("DGS10","10-Year Treasury Constant Maturity Rate","Percent","Daily","Federal Reserve Board via FRED","PublicDomainCitationRequested","level"),
        new("CPIAUCSL","CPI All Urban Consumers","Index 1982-1984=100","Monthly","BLS via FRED","PublicDomainCitationRequested","year-over-year percent change"),
        new("UNRATE","Unemployment Rate","Percent","Monthly","BLS via FRED","PublicDomainCitationRequested","level and 3-month change")
    ];
}

public static class EuropeanMacroPackV1
{
    public static readonly IReadOnlyList<MacroSeriesDefinition> Riksbank=
    [
        new("SECBREPOEFF","Swedish policy rate","Percent","Daily","Sveriges Riksbank","LocalResearchAttributionRequired","level"),
        new("SEKEURPMI","EUR/SEK","SEK per EUR","Daily","Sveriges Riksbank","LocalResearchAttributionRequired","source observation"),
        new("SEKUSDPMI","USD/SEK","SEK per USD","Daily","Sveriges Riksbank","LocalResearchAttributionRequired","source observation")
    ];
    public static readonly IReadOnlyList<MacroSeriesDefinition> Ecb=
    [
        new("EXR.D.USD.EUR.SP00.A","EUR/USD","USD per EUR","Daily","European Central Bank","LocalResearchAttributionRequired","source observation"),
        new("EXR.D.SEK.EUR.SP00.A","EUR/SEK","SEK per EUR","Daily","European Central Bank","LocalResearchAttributionRequired","source observation"),
        new("FM.D.U2.EUR.4F.KR.MRR_FR.LEV","ECB main refinancing operations rate","Percent","Daily","European Central Bank","LocalResearchAttributionRequired","level")
    ];
}

public sealed record FxComparison(DateOnly ReferencePeriod,string Classification,decimal? RiksbankValue,decimal? EcbValue,decimal? AbsoluteDifference,string Rule);

public static class MacroAsOf
{
    public static IReadOnlyList<MacroObservation> Select(IEnumerable<MacroObservation> source,MacroRegion region,DateTimeOffset asOfUtc,MacroEvidenceClass evidenceClass) =>
        source.Where(x=>x.Region==region&&x.EvidenceClass==evidenceClass&&x.KnowledgeTimeUtc<=asOfUtc)
            .GroupBy(x=>(x.Provider,x.SeriesId,x.ReferencePeriod))
            .Select(x=>x.OrderByDescending(v=>v.KnowledgeTimeUtc).First()).OrderBy(x=>x.Provider).ThenBy(x=>x.SeriesId).ThenBy(x=>x.ReferencePeriod).ToArray();
}

public static class FxCrossProviderValidator
{
    public static IReadOnlyList<FxComparison> CompareEurSek(IEnumerable<MacroObservation> source,decimal expectedTolerance=0.0001m,decimal mismatchTolerance=0.02m)
    {
        var rows=source.Where(x=>x.BaseCurrency=="EUR"&&x.QuoteCurrency=="SEK").ToArray();var days=rows.Select(x=>x.ReferencePeriod).Distinct().Order().ToArray();var result=new List<FxComparison>();
        foreach(var day in days){var r=rows.SingleOrDefault(x=>x.ReferencePeriod==day&&x.Provider=="RIKSBANK");var e=rows.SingleOrDefault(x=>x.ReferencePeriod==day&&x.Provider=="ECB");if(day<new DateOnly(2023,11,27)){result.Add(new(day,"INSUFFICIENT_COMPARABILITY",r?.Value,e?.Value,null,"Riksbank used a different FX source methodology before 2023-11-27"));continue;}if(r?.Value is null||e?.Value is null){result.Add(new(day,"INSUFFICIENT_COMPARABILITY",r?.Value,e?.Value,null,"same reference date and EUR base/SEK quote required"));continue;}var d=Math.Abs(r.Value.Value-e.Value.Value);result.Add(new(day,d<=expectedTolerance?"CONSISTENT":d<=mismatchTolerance?"EXPECTED_METHODOLOGY_DIFFERENCE":"MISMATCH",r.Value,e.Value,d,$"absolute tolerance {expectedTolerance}/{mismatchTolerance}"));}return result;
    }
}

public static class MacroFeatureEngine
{
    public static IReadOnlyList<MacroFeature> At(DateOnly session,DateTimeOffset cutoff,IReadOnlyList<MacroObservation> source,string macroRevision,MacroEvidenceClass? requiredEvidenceClass=null)
    {
        var eligible=requiredEvidenceClass is null?source:source.Where(x=>x.EvidenceClass==requiredEvidenceClass).ToArray();
        MacroObservation? Latest(string id)=>eligible.Where(x=>x.SeriesId==id&&x.KnowledgeTimeUtc<=cutoff&&x.ReferencePeriod<=session&&x.Value.HasValue).OrderByDescending(x=>x.KnowledgeTimeUtc).ThenByDescending(x=>x.ReferencePeriod).FirstOrDefault();
        var list=new List<MacroFeature>();
        void Add(string id,decimal? value,string units,string transform,params MacroObservation?[] used)=>list.Add(new(id,session,value,units,transform,used.Where(x=>x is not null).Select(x=>x!.KnowledgeTimeUtc).DefaultIfEmpty(cutoff).Max(),FredMacroPackV1.FeatureRevision));
        var dff=Latest("DFF");var two=Latest("DGS2");var ten=Latest("DGS10");Add("policy-rate.level",dff?.Value,"Percent","latest known, forward-filled only after knowledge time",dff);Add("treasury.2y",two?.Value,"Percent","latest known",two);Add("treasury.10y",ten?.Value,"Percent","latest known",ten);Add("yield-curve.10y2y",ten?.Value-two?.Value,"Percentage points","DGS10 minus DGS2",two,ten);
        var cpi=eligible.Where(x=>x.SeriesId=="CPIAUCSL"&&x.KnowledgeTimeUtc<=cutoff&&x.Value.HasValue).OrderBy(x=>x.ReferencePeriod).ToArray();var current=cpi.LastOrDefault();var prior=current is null?null:cpi.LastOrDefault(x=>x.ReferencePeriod<=current.ReferencePeriod.AddYears(-1));Add("inflation.cpi-yoy",current is null||prior?.Value is null?null:(current.Value!.Value/prior.Value.Value-1)*100,"Percent","year-over-year",current,prior);
        var unemployment=Latest("UNRATE");var old=unemployment is null?null:eligible.Where(x=>x.SeriesId=="UNRATE"&&x.KnowledgeTimeUtc<=cutoff&&x.ReferencePeriod<=unemployment.ReferencePeriod.AddMonths(-3)&&x.Value.HasValue).OrderByDescending(x=>x.ReferencePeriod).FirstOrDefault();Add("labor.unemployment",unemployment?.Value,"Percent","latest known",unemployment);Add("labor.unemployment-change-3m",unemployment?.Value-old?.Value,"Percentage points","three-month change",unemployment,old);return list;
    }

    public static MarketRegime Regime(DateOnly session,DateTimeOffset cutoff,IReadOnlyList<MacroFeature> f,string macroRevision,MacroEvidenceClass evidence)
    {
        decimal? V(string id)=>f.SingleOrDefault(x=>x.Id==id)?.Value;var spread=V("yield-curve.10y2y");var inflation=V("inflation.cpi-yoy");var labor=V("labor.unemployment-change-3m");
        var rate="UNKNOWN";var curve=spread is null?"UNKNOWN":spread<0?"INVERTED":spread<=0.25m?"FLAT":"NORMAL";var inf=inflation is null?"UNKNOWN":inflation<2m?"LOW":inflation<=3m?"MODERATE":"HIGH";var lab=labor is null?"UNKNOWN":labor<=-0.2m?"IMPROVING":labor>=0.2m?"DETERIORATING":"STABLE";
        return new(FredMacroPackV1.RegimePolicy,session,rate,curve,inf,lab,$"Rates:{rate}; YieldCurve:{curve}; Inflation:{inf}; Labor:{lab}",macroRevision,FredMacroPackV1.FeatureRevision,evidence,cutoff);
    }

    public static string Hash(IEnumerable<MacroObservation> rows)=>"sha256:"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n',rows.OrderBy(x=>x.SeriesId).ThenBy(x=>x.ReferencePeriod).ThenBy(x=>x.KnowledgeTimeUtc).Select(x=>$"{x.SeriesId}|{x.ReferencePeriod}|{x.Value}|{x.KnowledgeTimeUtc:O}|{x.RealtimeStart}|{x.RealtimeEnd}|{x.ArtifactHash}"))))).ToLowerInvariant();
}
