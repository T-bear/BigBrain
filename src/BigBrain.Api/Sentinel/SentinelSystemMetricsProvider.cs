using BigBrain.Modules;

namespace BigBrain.Api.Sentinel;

public sealed class SentinelSystemMetricsProvider(ISentinelClient sentinel) : ISystemMetricsProvider
{
    public async Task<SystemOverview> GetOverviewAsync(CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await sentinel.ReadSystemMetricsAsync(cancellationToken);
            var uptimeSeconds = snapshot.Sections.Uptime.Status == "available"
                ? snapshot.Sections.Uptime.Data?.UptimeSeconds
                : null;
            var cpu = snapshot.Sections.Cpu.Status == "available"
                ? snapshot.Sections.Cpu.Data
                : null;
            var memory = snapshot.Sections.Memory.Status == "available"
                ? snapshot.Sections.Memory.Data
                : null;
            var disks = snapshot.Sections.Disks.Items
                .Where(item => item.Status == "available")
                .Select(
                    item => new DiskMetrics(
                        item.FilesystemId,
                        item.DisplayName,
                        item.TotalBytes,
                        item.UsedBytes,
                        item.AvailableBytes,
                        item.UsagePercent))
                .ToArray();

            return new SystemOverview(
                "Unavailable",
                "Unavailable",
                "Unavailable",
                uptimeSeconds,
                new CpuMetrics(cpu?.UsagePercent, cpu?.LogicalProcessorCount ?? 0),
                new MemoryMetrics(
                    memory?.TotalBytes,
                    memory?.UsedBytes,
                    memory?.AvailableBytes,
                    memory?.UsagePercent),
                disks,
                null,
                snapshot.CollectedAtUtc,
                snapshot.Status == "available"
                    ? "Healthy"
                    : snapshot.Status == "unavailable"
                        ? "Unavailable"
                        : "Degraded",
                snapshot.Warnings);
        }
        catch (SentinelClientUnavailableException)
        {
            return await new UnavailableSystemMetricsProvider().GetOverviewAsync(cancellationToken);
        }
    }
}
