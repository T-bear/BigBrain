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
                [],
                null,
                snapshot.CollectedAtUtc,
                uptimeSeconds is null ? "Unavailable" : "Degraded",
                snapshot.Warnings);
        }
        catch (SentinelClientUnavailableException)
        {
            return await new UnavailableSystemMetricsProvider().GetOverviewAsync(cancellationToken);
        }
    }
}
