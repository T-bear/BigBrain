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
                null));
        var provider = new SentinelSystemMetricsProvider(new StubSentinelClient(snapshot));

        var result = await provider.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Degraded", result.Status);
        Assert.Equal(collectedAtUtc, result.CollectedAtUtc);
        Assert.Equal(310_920, result.UptimeSeconds);
        Assert.Null(result.Cpu.UsagePercent);
        Assert.Null(result.Memory.TotalBytes);
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
        SentinelUptimeSection uptime)
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
                new SentinelUnavailableSection("unavailable", unavailable),
                new SentinelUnavailableSection("unavailable", unavailable),
                new SentinelDiskSection("unavailable", [], unavailable)),
            ["Only uptime is available."]);
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
