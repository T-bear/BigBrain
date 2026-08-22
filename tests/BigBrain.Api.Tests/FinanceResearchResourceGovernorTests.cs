using BigBrain.Api.Finance;
using BigBrain.Modules;

namespace BigBrain.Api.Tests;

public sealed class FinanceResearchResourceGovernorTests
{
    private static readonly DateTimeOffset Now=new(2026,8,22,12,0,0,TimeSpan.Zero);

    [Fact]
    public async Task HealthyTrustedSnapshotAllowsResearchResources()
    {
        var result=await Governor(Overview()).EvaluateAsync(Now,CancellationToken.None);
        Assert.Equal(FinanceResearchResourceDecisionKind.Allow,result.Decision);Assert.Equal(["finance.research.scheduler.resource.ready"],result.ReasonCodes);Assert.Equal("RESEARCH",result.OperatingMode);Assert.Equal(0m,result.BudgetSek);Assert.Equal("NONE",result.ExecutionAuthority);Assert.False(result.Evidence.TemperatureSupported);
    }

    [Fact]
    public async Task CpuMemoryAndLowDiskDeferWithEveryReason()
    {
        var result=await Governor(Overview(cpu:90,memoryUsage:90,availableMemory:512L*Mb,disk:5L*Gb)).EvaluateAsync(Now,CancellationToken.None);
        Assert.Equal(FinanceResearchResourceDecisionKind.Defer,result.Decision);Assert.Equal(["finance.research.scheduler.resource.cpu","finance.research.scheduler.resource.disk","finance.research.scheduler.resource.memory"],result.ReasonCodes);
    }

    [Fact]
    public async Task CriticalDiskBlocksAndWinsOverOtherPressure()
    {
        var result=await Governor(Overview(cpu:95,memoryUsage:95,availableMemory:256L*Mb,disk:1L*Gb)).EvaluateAsync(Now,CancellationToken.None);
        Assert.Equal(FinanceResearchResourceDecisionKind.Block,result.Decision);Assert.Contains("finance.research.scheduler.resource.diskCritical",result.ReasonCodes);Assert.Contains("finance.research.scheduler.resource.cpu",result.ReasonCodes);
    }

    [Fact]
    public async Task MissingStaleAndFailingMetricsFailClosedWithoutInventedTemperature()
    {
        var unavailable=await Governor(new UnavailableSystemMetricsProvider()).EvaluateAsync(Now,CancellationToken.None);var stale=await Governor(Overview() with{CollectedAtUtc=Now.AddMinutes(-6)}).EvaluateAsync(Now,CancellationToken.None);var failed=await Governor(new ThrowingMetricsProvider()).EvaluateAsync(Now,CancellationToken.None);
        Assert.All([unavailable,stale,failed],x=>{Assert.Equal(FinanceResearchResourceDecisionKind.Defer,x.Decision);Assert.Contains("finance.research.scheduler.resource.metricsUnavailable",x.ReasonCodes);Assert.False(x.Evidence.TemperatureSupported);});
    }

    [Fact]
    public void ConfigurationBoundsAreVersionedAndConservative()
    {
        Assert.Throws<InvalidOperationException>(()=>new FinanceResearchResourceGovernorOptions{GovernorVersion="v0"}.Validate());Assert.Throws<InvalidOperationException>(()=>new FinanceResearchResourceGovernorOptions{CpuDeferPercent=0}.Validate());Assert.Throws<InvalidOperationException>(()=>new FinanceResearchResourceGovernorOptions{MemoryDeferPercent=101}.Validate());Assert.Throws<InvalidOperationException>(()=>new FinanceResearchResourceGovernorOptions{MinimumFreeMemoryMb=0}.Validate());Assert.Throws<InvalidOperationException>(()=>new FinanceResearchResourceGovernorOptions{MinimumFreeDiskGb=2,CriticalFreeDiskGb=2}.Validate());Assert.Throws<InvalidOperationException>(()=>new FinanceResearchResourceGovernorOptions{MaximumSnapshotAgeMinutes=31}.Validate());new FinanceResearchResourceGovernorOptions().Validate();
    }

    private static FinanceResearchResourceGovernor Governor(SystemOverview overview)=>Governor(new FixedMetricsProvider(overview));
    private static FinanceResearchResourceGovernor Governor(ISystemMetricsProvider provider)=>new(new(),provider);
    private static SystemOverview Overview(double cpu=25,double memoryUsage=50,long availableMemory=8L*Gb,long disk=100L*Gb)=>new("Unavailable","Unavailable","Unavailable",1000,new(cpu,8),new(16L*Gb,8L*Gb,availableMemory,memoryUsage),[new("system","System",200L*Gb,100L*Gb,disk,50)],null,Now,"Healthy",[]);
    private const long Mb=1024L*1024L;private const long Gb=1024L*1024L*1024L;
    private sealed class FixedMetricsProvider(SystemOverview overview):ISystemMetricsProvider{public Task<SystemOverview> GetOverviewAsync(CancellationToken cancellationToken)=>Task.FromResult(overview);}
    private sealed class ThrowingMetricsProvider:ISystemMetricsProvider{public Task<SystemOverview> GetOverviewAsync(CancellationToken cancellationToken)=>Task.FromException<SystemOverview>(new InvalidOperationException("fixture"));}
}
