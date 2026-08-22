using BigBrain.Modules;

namespace BigBrain.Api.Finance;

public sealed record FinanceResearchResourceGovernorOptions
{
    public const string Section="Finance:ResearchResourceGovernor";
    public const string CurrentVersion="finance-research-resource-governor-v1";
    public string GovernorVersion{get;set;}=CurrentVersion;
    public double CpuDeferPercent{get;set;}=80;
    public double MemoryDeferPercent{get;set;}=85;
    public long MinimumFreeMemoryMb{get;set;}=1024;
    public long MinimumFreeDiskGb{get;set;}=10;
    public long CriticalFreeDiskGb{get;set;}=2;
    public int MaximumSnapshotAgeMinutes{get;set;}=5;
    public void Validate()
    {
        if(GovernorVersion!=CurrentVersion)throw new InvalidOperationException("Finance research resource governor version is invalid.");
        if(CpuDeferPercent is<1 or>100)throw new InvalidOperationException("CPU defer percent must be between 1 and 100.");
        if(MemoryDeferPercent is<1 or>100)throw new InvalidOperationException("Memory defer percent must be between 1 and 100.");
        if(MinimumFreeMemoryMb<1)throw new InvalidOperationException("Minimum free memory must be positive.");
        if(CriticalFreeDiskGb<1||MinimumFreeDiskGb<=CriticalFreeDiskGb)throw new InvalidOperationException("Disk thresholds must be positive and low-disk must exceed critical-disk.");
        if(MaximumSnapshotAgeMinutes is<1 or>30)throw new InvalidOperationException("Maximum metrics snapshot age must be between 1 and 30 minutes.");
    }
}

public enum FinanceResearchResourceDecisionKind{Allow,Defer,Block}
public sealed record FinanceResearchResourceEvidence(double? CpuUsagePercent,double? MemoryUsagePercent,
    long? AvailableMemoryBytes,long? MinimumAvailableDiskBytes,int AvailableDiskCount,double? TemperatureCelsius,
    bool TemperatureSupported,string MetricsStatus,DateTimeOffset? CollectedAtUtc);
public sealed record FinanceResearchResourceDecision(FinanceResearchResourceDecisionKind Decision,
    DateTimeOffset EvaluatedAtUtc,string GovernorVersion,IReadOnlyList<string> ReasonCodes,
    FinanceResearchResourceEvidence Evidence,string OperatingMode,decimal BudgetSek,string ExecutionAuthority);

internal interface IFinanceResearchResourceGovernor
{
    Task<FinanceResearchResourceDecision> EvaluateAsync(DateTimeOffset nowUtc,CancellationToken cancellationToken);
}

internal sealed class FinanceResearchResourceGovernor(FinanceResearchResourceGovernorOptions options,
    ISystemMetricsProvider metrics):IFinanceResearchResourceGovernor
{
    private const long Mb=1024L*1024L;
    private const long Gb=1024L*1024L*1024L;
    internal FinanceResearchResourceGovernorOptions Options=>options;
    public async Task<FinanceResearchResourceDecision> EvaluateAsync(DateTimeOffset nowUtc,CancellationToken cancellationToken)
    {
        options.Validate();SystemOverview snapshot;
        try{snapshot=await metrics.GetOverviewAsync(cancellationToken);}
        catch(OperationCanceledException)when(cancellationToken.IsCancellationRequested){throw;}
        catch{return Decision(FinanceResearchResourceDecisionKind.Defer,nowUtc,["finance.research.scheduler.resource.metricsUnavailable"],new(null,null,null,null,0,null,false,"Unavailable",null));}
        var reasons=new SortedSet<string>(StringComparer.Ordinal);var strongest=FinanceResearchResourceDecisionKind.Allow;
        void Defer(string reason){reasons.Add(reason);if(strongest==FinanceResearchResourceDecisionKind.Allow)strongest=FinanceResearchResourceDecisionKind.Defer;}
        void Block(string reason){reasons.Add(reason);strongest=FinanceResearchResourceDecisionKind.Block;}
        if(!string.Equals(snapshot.Status,"Healthy",StringComparison.Ordinal)||nowUtc-snapshot.CollectedAtUtc>TimeSpan.FromMinutes(options.MaximumSnapshotAgeMinutes)||snapshot.CollectedAtUtc>nowUtc.AddMinutes(1))Defer("finance.research.scheduler.resource.metricsUnavailable");
        if(snapshot.Cpu.UsagePercent is not{} cpu)Defer("finance.research.scheduler.resource.metricsUnavailable");else if(cpu>=options.CpuDeferPercent)Defer("finance.research.scheduler.resource.cpu");
        if(snapshot.Memory.AvailableBytes is not{} availableMemory||snapshot.Memory.UsagePercent is not{} memoryUsage)Defer("finance.research.scheduler.resource.metricsUnavailable");else if(availableMemory<options.MinimumFreeMemoryMb*Mb||memoryUsage>=options.MemoryDeferPercent)Defer("finance.research.scheduler.resource.memory");
        var availableDisks=snapshot.Disks.Where(x=>x.AvailableBytes.HasValue).OrderBy(x=>x.FilesystemId,StringComparer.Ordinal).ToArray();var minimumDisk=availableDisks.Select(x=>x.AvailableBytes!.Value).DefaultIfEmpty().Min();
        if(availableDisks.Length==0)Defer("finance.research.scheduler.resource.metricsUnavailable");else if(minimumDisk<options.CriticalFreeDiskGb*Gb)Block("finance.research.scheduler.resource.diskCritical");else if(minimumDisk<options.MinimumFreeDiskGb*Gb)Defer("finance.research.scheduler.resource.disk");
        if(reasons.Count==0)reasons.Add("finance.research.scheduler.resource.ready");
        return Decision(strongest,nowUtc,reasons.ToArray(),new(snapshot.Cpu.UsagePercent,snapshot.Memory.UsagePercent,snapshot.Memory.AvailableBytes,availableDisks.Length==0?null:minimumDisk,availableDisks.Length,snapshot.TemperatureCelsius,false,snapshot.Status,snapshot.CollectedAtUtc));
    }
    private FinanceResearchResourceDecision Decision(FinanceResearchResourceDecisionKind kind,DateTimeOffset now,IReadOnlyList<string> reasons,FinanceResearchResourceEvidence evidence)=>new(kind,now,options.GovernorVersion,reasons,evidence,"RESEARCH",0m,"NONE");
}
