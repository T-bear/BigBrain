using BigBrain.Api.Sentinel;
using BigBrain.Modules;
using BigBrain.Sentinel.Contracts;

namespace BigBrain.Api.Tests;

public sealed class SentinelSystemMetricsProviderTests
{
    [Fact]
    public async Task GetOverviewAsyncMapsAvailableUptimeFromSentinel()
    {
        var collectedAtUtc = new DateTimeOffset(2026, 7, 30, 10, 0, 0, TimeSpan.Zero);
        var snapshot = CreateSnapshot(
            collectedAtUtc,
            new SentinelUptimeSection(
                "available",
                new SentinelUptimeData(310_920),
                null),
            new SentinelCpuSection(
                "available",
                new SentinelCpuData(8, 23.5, 250),
                null),
            new SentinelMemorySection(
                "available",
                new SentinelMemoryData(
                    17_179_869_184,
                    8_589_934_592,
                    8_589_934_592,
                    50),
                null));
        var provider = new SentinelSystemMetricsProvider(new StubSentinelClient(snapshot));

        var result = await provider.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Degraded", result.Status);
        Assert.Equal(collectedAtUtc, result.CollectedAtUtc);
        Assert.Equal(310_920, result.UptimeSeconds);
        Assert.Equal(23.5, result.Cpu.UsagePercent);
        Assert.Equal(8, result.Cpu.LogicalProcessorCount);
        Assert.Equal(17_179_869_184, result.Memory.TotalBytes);
        Assert.Equal(8_589_934_592, result.Memory.UsedBytes);
        Assert.Equal(8_589_934_592, result.Memory.AvailableBytes);
        Assert.Equal(50, result.Memory.UsagePercent);
        Assert.Empty(result.Disks);
    }

    [Fact]
    public async Task GetOverviewAsyncReturnsUnavailableWhenSentinelCannotBeReached()
    {
        var provider = new SentinelSystemMetricsProvider(
            new StubSentinelClient(new SentinelClientUnavailableException("Sentinel unavailable.")));

        var result = await provider.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Unavailable", result.Status);
        Assert.Null(result.UptimeSeconds);
    }

    private static SentinelSnapshotResponse CreateSnapshot(
        DateTimeOffset collectedAtUtc,
        SentinelUptimeSection uptime,
        SentinelCpuSection cpu,
        SentinelMemorySection memory)
    {
        var unavailable = new SentinelProtocolError(
            "CAPABILITY_UNAVAILABLE",
            "Capability is not available.",
            false);

        return new SentinelSnapshotResponse(
            "snapshot-1",
            "node-1",
            collectedAtUtc,
            "partial",
            new SentinelSnapshotSections(
                uptime,
                cpu,
                memory,
                new SentinelDiskSection("unavailable", [], unavailable)),
            ["Disk metrics are not implemented."]);
    }

    private sealed class StubSentinelClient : ISentinelClient
    {
        private readonly SentinelSnapshotResponse? _snapshot;
        private readonly Exception? _exception;

        public StubSentinelClient(SentinelSnapshotResponse snapshot)
        {
            _snapshot = snapshot;
        }

        public StubSentinelClient(Exception exception)
        {
            _exception = exception;
        }

        public Task<SentinelPingResponse> PingAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<SentinelSnapshotResponse> ReadSystemMetricsAsync(
            CancellationToken cancellationToken = default)
        {
            return _exception is null
                ? Task.FromResult(_snapshot!)
                : Task.FromException<SentinelSnapshotResponse>(_exception);
        }
    }
}
