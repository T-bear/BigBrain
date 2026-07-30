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

            return new SystemOverview(
                "Unavailable",
                "Unavailable",
                "Unavailable",
                uptimeSeconds,
                new CpuMetrics(null, 0),
                new MemoryMetrics(null, null, null, null),
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
