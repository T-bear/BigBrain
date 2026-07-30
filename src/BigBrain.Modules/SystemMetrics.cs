namespace BigBrain.Modules;

public sealed record CpuMetrics(double? UsagePercent, int LogicalProcessorCount);
public sealed record MemoryMetrics(long? TotalBytes, long? UsedBytes, long? AvailableBytes, double? UsagePercent);
public sealed record DiskMetrics(
    string FilesystemId,
    string DisplayName,
    long? TotalBytes,
    long? UsedBytes,
    long? AvailableBytes,
    double? UsagePercent);
public sealed record SystemOverview(
    string Hostname,
    string OperatingSystem,
    string Architecture,
    double? UptimeSeconds,
    CpuMetrics Cpu,
    MemoryMetrics Memory,
    IReadOnlyList<DiskMetrics> Disks,
    double? TemperatureCelsius,
    DateTimeOffset CollectedAtUtc,
    string Status,
    IReadOnlyList<string> Warnings);

public interface ISystemMetricsProvider
{
    Task<SystemOverview> GetOverviewAsync(CancellationToken cancellationToken);
}

public sealed class UnavailableSystemMetricsProvider : ISystemMetricsProvider
{
    public Task<SystemOverview> GetOverviewAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new SystemOverview(
            "Unavailable",
            "Unavailable",
            "Unavailable",
            null,
            new CpuMetrics(null, 0),
            new MemoryMetrics(null, null, null, null),
            [],
            null,
            DateTimeOffset.UtcNow,
            "Unavailable",
            ["Host metrics require Sentinel integration."]));
}
