using System.Text.Json;
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
                null),
            new SentinelDiskSection(
                "available",
                [
                    AvailableDisk("system", "System Storage", 1_000, 400, 600, 40),
                    AvailableDisk("media", "Media Storage", 2_000, 500, 1_500, 25)
                ],
                null));
        var provider = new SentinelSystemMetricsProvider(new StubSentinelClient(snapshot));

        var result = await provider.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Healthy", result.Status);
        Assert.Equal(collectedAtUtc, result.CollectedAtUtc);
        Assert.Equal(310_920, result.UptimeSeconds);
        Assert.Equal(23.5, result.Cpu.UsagePercent);
        Assert.Equal(8, result.Cpu.LogicalProcessorCount);
        Assert.Equal(17_179_869_184, result.Memory.TotalBytes);
        Assert.Equal(8_589_934_592, result.Memory.UsedBytes);
        Assert.Equal(8_589_934_592, result.Memory.AvailableBytes);
        Assert.Equal(50, result.Memory.UsagePercent);
        Assert.Collection(
            result.Disks,
            system =>
            {
                Assert.Equal("system", system.FilesystemId);
                Assert.Equal("System Storage", system.DisplayName);
                Assert.Equal(1_000, system.TotalBytes);
                Assert.Equal(400, system.UsedBytes);
                Assert.Equal(600, system.AvailableBytes);
                Assert.Equal(40, system.UsagePercent);
            },
            media =>
            {
                Assert.Equal("media", media.FilesystemId);
                Assert.Equal("Media Storage", media.DisplayName);
                Assert.Equal(2_000, media.TotalBytes);
                Assert.Equal(500, media.UsedBytes);
                Assert.Equal(1_500, media.AvailableBytes);
                Assert.Equal(25, media.UsagePercent);
            });
        var json = JsonSerializer.Serialize(result, JsonSerializerOptions.Web);
        Assert.DoesNotContain("mountPoint", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sentinelPath", json, StringComparison.OrdinalIgnoreCase);
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
        SentinelMemorySection memory,
        SentinelDiskSection disks) =>
        new(
            "snapshot-1",
            "node-1",
            collectedAtUtc,
            "available",
            new SentinelSnapshotSections(
                uptime,
                cpu,
                memory,
                disks),
            []);

    private static SentinelDiskItem AvailableDisk(
        string filesystemId,
        string displayName,
        long totalBytes,
        long usedBytes,
        long availableBytes,
        double usagePercent) =>
        new(
            filesystemId,
            displayName,
            "available",
            totalBytes,
            usedBytes,
            availableBytes,
            usagePercent,
            null);

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
