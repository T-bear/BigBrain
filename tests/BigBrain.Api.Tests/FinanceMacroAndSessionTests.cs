using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using System.Text;

namespace BigBrain.Api.Tests;

public sealed class FinanceMacroAndSessionTests : IDisposable
{
    private readonly string _root=Path.Combine(Path.GetTempPath(),"bb-macro-tests",Guid.NewGuid().ToString("N"));
    [Fact]
    public void NewYorkSessionHandlesBothDstStatesWithoutMachineTimezone()
    {
        var winter=UsMarketCalendar.Session(new(2026,1,5))!;var summer=UsMarketCalendar.Session(new(2026,7,6))!;
        Assert.Equal(new TimeOnly(14,30),TimeOnly.FromDateTime(winter.OpenUtc.UtcDateTime));
        Assert.Equal(new TimeOnly(13,30),TimeOnly.FromDateTime(summer.OpenUtc.UtcDateTime));
        Assert.Equal(new TimeOnly(21,0),TimeOnly.FromDateTime(winter.CloseUtc.UtcDateTime));
        Assert.Equal(new TimeOnly(20,0),TimeOnly.FromDateTime(summer.CloseUtc.UtcDateTime));
    }

    [Fact]
    public void SessionUtcTimesFollowActualDstTransitions()
    {
        Assert.Equal(new TimeOnly(14,30),TimeOnly.FromDateTime(UsMarketCalendar.Session(new(2026,3,6))!.OpenUtc.UtcDateTime));
        Assert.Equal(new TimeOnly(13,30),TimeOnly.FromDateTime(UsMarketCalendar.Session(new(2026,3,9))!.OpenUtc.UtcDateTime));
        Assert.Equal(new TimeOnly(13,30),TimeOnly.FromDateTime(UsMarketCalendar.Session(new(2026,10,30))!.OpenUtc.UtcDateTime));
        Assert.Equal(new TimeOnly(14,30),TimeOnly.FromDateTime(UsMarketCalendar.Session(new(2026,11,2))!.OpenUtc.UtcDateTime));
    }

    [Fact]
    public void JuneteenthBeginsWithExchangeObservanceIn2022()
    {
        Assert.NotNull(UsMarketCalendar.Session(new(2021,6,18)));
        Assert.Null(UsMarketCalendar.Session(new(2022,6,20)));
    }

    [Fact]
    public void WeekendHolidayAndExceptionalClosureNeverBecomeSessions()
    {
        Assert.Null(UsMarketCalendar.Session(new(2026,8,16)));
        Assert.Null(UsMarketCalendar.Session(new(2026,7,3)));
        Assert.Null(UsMarketCalendar.Session(new(2001,9,12)));
        Assert.NotNull(UsMarketCalendar.Session(new(2026,8,17)));
    }

    [Fact]
    public void MonthlyMacroIsInvisibleBeforeKnowledgeTimeAndForwardFilledAfterward()
    {
        var acquired=new DateTimeOffset(2026,8,12,15,0,0,TimeSpan.Zero);var release=new DateTimeOffset(2026,8,12,13,30,0,TimeSpan.Zero);
        var rows=new[]{new MacroObservation("CPIAUCSL",new(2026,7,1),332.813m,release,acquired,new(2026,8,12),new(9999,12,31),"sha256:fixture",MacroEvidenceClass.RevisedHistoryExploratory)};
        Assert.Null(MacroFeatureEngine.At(new(2026,8,11),new(2026,8,11,20,0,0,TimeSpan.Zero),rows,"fred-fixture").Single(x=>x.Id=="inflation.cpi-yoy").Value);
        Assert.Contains(MacroFeatureEngine.At(new(2026,8,13),new(2026,8,13,20,0,0,TimeSpan.Zero),rows,"fred-fixture"),x=>x.Id=="inflation.cpi-yoy");
    }

    [Fact]
    public void RevisedAndOriginalVintagesRespectCausalCutoff()
    {
        var rows=new[]{Observation(330m,new(2026,8,12)),Observation(331m,new(2026,9,11))};
        var early=MacroFeatureEngine.At(new(2026,8,20),At(new(2026,8,20)),rows,"r1");
        var late=MacroFeatureEngine.At(new(2026,9,20),At(new(2026,9,20)),rows,"r1");
        Assert.NotEqual(early.Single(x=>x.Id=="inflation.cpi-yoy").KnowledgeTimeUtc,late.Single(x=>x.Id=="inflation.cpi-yoy").KnowledgeTimeUtc);
    }

    [Fact]
    public void PointInTimeSelectionNeverFallsBackToRevisedHistory()
    {
        var revised=new MacroObservation("CPIAUCSL",new(2026,7,1),331m,At(new(2026,8,12)),At(new(2026,8,13)),new(2026,8,12),new(9999,12,31),"sha256:revised",MacroEvidenceClass.RevisedHistoryExploratory);
        var features=MacroFeatureEngine.At(new(2026,8,20),At(new(2026,8,20)),[revised],"r1",MacroEvidenceClass.PointInTimeCausal);
        Assert.All(features,x=>Assert.Null(x.Value));
    }

    [Fact]
    public void QuarantineRetainsRejectedCandidateAndNeverPromotesIt()
    {
        Directory.CreateDirectory(_root);var database=Path.Combine(_root,"rejected.db");var options=new EodhdFinanceOptions{DatabasePath=database,PayloadDirectory=Path.Combine(_root,"payloads")};var fred=new FinanceFredOptions{QuarantineDirectory=Path.Combine(_root,"quarantine")};
        var paths=FredMacroPackV1.Series.Select(s=>{var path=Path.Combine(_root,s.SeriesId+".csv");File.WriteAllText(path,$"observation_date,{s.SeriesId}\n2026-01-01,{(s.SeriesId=="DFF"?"malformed":"1.0")}\n");return path;}).ToArray();
        Assert.Throws<InvalidDataException>(()=>new FinanceMacroMemory(options).StageAndPromoteCsvPack(paths,fred));
        using var c=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={database}");c.Open();using var x=c.CreateCommand();x.CommandText="SELECT validation_result||'|'||promotion_decision||'|'||COALESCE(canonical_revision_id,'') FROM macro_candidates";Assert.Equal("FAIL|REJECTED|",x.ExecuteScalar());
    }

    [Fact]
    public void MacroMemoryMigrationAndPromotionAreRestartSafe()
    {
        var options=new EodhdFinanceOptions{DatabasePath=Path.Combine(_root,"finance.db"),PayloadDirectory=Path.Combine(_root,"payloads")};
        var fred=new FinanceFredOptions{QuarantineDirectory=Path.Combine(_root,"quarantine")};
        var artifact=Encoding.UTF8.GetBytes("{\"output_type\":2,\"realtime_end\":\"2026-08-16\",\"observations\":[{\"date\":\"2026-07-01\",\"CPIAUCSL_20260812\":\"330.0\"}]}");
        var acquired=new DateTimeOffset(2026,8,13,0,0,0,TimeSpan.Zero);
        var first=new FinanceMacroMemory(options);var id=first.StageAndPromoteVintage("CPIAUCSL",artifact,fred,acquired);
        var second=new FinanceMacroMemory(options);Assert.Equal(id,second.StageAndPromoteVintage("CPIAUCSL",artifact,fred,acquired));Assert.Equal(1,second.Snapshot().Status.RevisionCount);Assert.Equal(1,second.Snapshot().Status.ObservationCount);
    }

    [Fact]
    public void OfficialOutputTypeTwoColumnsBecomeCausalVintages()
    {
        var options=new EodhdFinanceOptions{DatabasePath=Path.Combine(_root,"vintages.db"),PayloadDirectory=Path.Combine(_root,"payloads")};var fred=new FinanceFredOptions{QuarantineDirectory=Path.Combine(_root,"quarantine")};
        var artifact=Encoding.UTF8.GetBytes("{\"output_type\":2,\"realtime_end\":\"2021-02-01\",\"observations\":[{\"date\":\"2020-01-01\",\"CPIAUCSL_20200213\":\"258.7\",\"CPIAUCSL_20210113\":\"258.8\"}]}");var acquired=new DateTimeOffset(2026,8,16,0,0,0,TimeSpan.Zero);
        _=new FinanceMacroMemory(options).StageAndPromoteVintage("CPIAUCSL",artifact,fred,acquired);var rows=new FinanceMacroMemory(options).Snapshot().Observations;
        Assert.Equal(2,rows.Count);Assert.Equal(new DateTimeOffset(2020,2,14,0,0,0,TimeSpan.Zero),rows[0].KnowledgeTimeUtc);Assert.Equal(new DateOnly(2021,1,12),rows[0].RealtimeEnd);Assert.Equal(258.8m,rows[1].Value);Assert.All(rows,x=>Assert.Equal(MacroEvidenceClass.PointInTimeCausal,x.EvidenceClass));
    }

    [Fact]
    public void MalformedVintageArtifactIsRetainedAndRejected()
    {
        var database=Path.Combine(_root,"bad-vintage.db");var options=new EodhdFinanceOptions{DatabasePath=database,PayloadDirectory=Path.Combine(_root,"payloads")};var fred=new FinanceFredOptions{QuarantineDirectory=Path.Combine(_root,"quarantine")};var artifact=Encoding.UTF8.GetBytes("{\"output_type\":1,\"realtime_end\":\"2021-02-01\",\"observations\":[]}");
        Assert.Throws<InvalidDataException>(()=>new FinanceMacroMemory(options).StageAndPromoteVintage("CPIAUCSL",artifact,fred,new DateTimeOffset(2026,8,16,0,0,0,TimeSpan.Zero)));using var c=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={database}");c.Open();using var x=c.CreateCommand();x.CommandText="SELECT validation_result||'|'||promotion_decision||'|'||COALESCE(canonical_revision_id,'') FROM macro_candidates";Assert.Equal("FAIL|REJECTED|",x.ExecuteScalar());Assert.True(Directory.EnumerateFiles(fred.QuarantineDirectory,"*.json",SearchOption.AllDirectories).Any());
    }

    private static MacroObservation Observation(decimal value,DateOnly known){var knowledge=At(known);return new("CPIAUCSL",new(2026,7,1),value,knowledge,knowledge.AddHours(1),known,new(9999,12,31),"sha256:fixture",MacroEvidenceClass.PointInTimeCausal);}
    private static DateTimeOffset At(DateOnly d)=>new(d.ToDateTime(new(14,0)),TimeSpan.Zero);
    public void Dispose(){if(Directory.Exists(_root))Directory.Delete(_root,true);GC.SuppressFinalize(this);}
}
