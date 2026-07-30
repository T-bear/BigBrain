using BigBrain.Sentinel.Contracts;
using Microsoft.Extensions.Options;

namespace BigBrain.Sentinel;

public interface ISystemMetricsSnapshotService
{
    SentinelSnapshotResponse ReadSnapshot();
}

public sealed class SystemMetricsSnapshotService(
    IHostUptimeReader uptimeReader,
    IOptions<SentinelProtocolOptions> options) : ISystemMetricsSnapshotService
{
    private static readonly SentinelProtocolError NotImplemented =
        new(
            SentinelProtocol.CapabilityUnavailable,
            "The metric is not implemented.",
            false);

    public SentinelSnapshotResponse ReadSnapshot()
    {
        var uptimeSeconds = uptimeReader.ReadUptimeSeconds();
        var uptime = double.IsFinite(uptimeSeconds) && uptimeSeconds >= 0
            ? new SentinelUptimeSection(
                "available",
                new SentinelUptimeData(uptimeSeconds),
                null)
            : new SentinelUptimeSection(
                "unavailable",
                null,
                new SentinelProtocolError(
                    "VALUE_INVALID",
                    "The uptime value is invalid.",
                    false));

        return new SentinelSnapshotResponse(
            $"snapshot:{Guid.NewGuid():N}",
            options.Value.NodeId,
            DateTimeOffset.UtcNow,
            uptime.Status == "available" ? "partial" : "unavailable",
            new SentinelSnapshotSections(
                uptime,
                new SentinelUnavailableSection("unavailable", NotImplemented),
                new SentinelUnavailableSection("unavailable", NotImplemented),
                new SentinelDiskSection("unavailable", [], NotImplemented)),
            uptime.Status == "available"
                ? ["CPU, memory and disk metrics are not implemented."]
                : ["System uptime is unavailable."]);
    }
}
