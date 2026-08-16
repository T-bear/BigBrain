using System.Security.Cryptography;
using System.Text;

namespace BigBrain.Modules.Finance;

public enum MacroEvidenceClass { RevisedHistoryExploratory, PointInTimeCausal }
public sealed record MacroSeriesDefinition(string SeriesId,string Title,string Units,string Frequency,string Source,string RightsClass,string Transformation);
public sealed record MacroObservation(string SeriesId,DateOnly ReferencePeriod,decimal? Value,DateTimeOffset KnowledgeTimeUtc,DateTimeOffset AcquiredAtUtc,DateOnly RealtimeStart,DateOnly RealtimeEnd,string ArtifactHash,MacroEvidenceClass EvidenceClass);
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
