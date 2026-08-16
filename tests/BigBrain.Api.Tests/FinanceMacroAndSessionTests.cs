using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;

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
    public void MacroMemoryMigrationAndPromotionAreRestartSafe()
    {
        var options=new EodhdFinanceOptions{DatabasePath=Path.Combine(_root,"finance.db"),PayloadDirectory=Path.Combine(_root,"payloads")};
        var first=new FinanceMacroMemory(options);var row=Observation(330m,new(2026,8,12));var id=first.Promote([row]);
        var second=new FinanceMacroMemory(options);Assert.Equal(id,second.Promote([row]));Assert.Equal(1,second.Snapshot().Status.RevisionCount);Assert.Equal(1,second.Snapshot().Status.ObservationCount);
    }

    private static MacroObservation Observation(decimal value,DateOnly known){var knowledge=At(known);return new("CPIAUCSL",new(2026,7,1),value,knowledge,knowledge.AddHours(1),known,new(9999,12,31),"sha256:fixture",MacroEvidenceClass.PointInTimeCausal);}
    private static DateTimeOffset At(DateOnly d)=>new(d.ToDateTime(new(14,0)),TimeSpan.Zero);
    public void Dispose(){if(Directory.Exists(_root))Directory.Delete(_root,true);GC.SuppressFinalize(this);}
}
