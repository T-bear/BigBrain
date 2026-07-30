namespace BigBrain.Sentinel.Tests;

public sealed class SystemMetricsSnapshotServiceTests
{
    [Fact]
    public void TryCalculateCpuUsageReturnsAggregateNonIdlePercentageAndProcessorCount()
    {
        const string first =
            """
            cpu  100 0 50 800 50 0 0 0
            cpu0 50 0 25 400 25 0 0 0
            cpu1 50 0 25 400 25 0 0 0
            """;
        const string second =
            """
            cpu  140 0 70 860 50 0 0 0
            cpu0 70 0 35 430 25 0 0 0
            cpu1 70 0 35 430 25 0 0 0
            """;

        var success = SystemMetricsSnapshotService.TryCalculateCpuUsage(
            first,
            second,
            250,
            out var data);

        Assert.True(success);
        Assert.NotNull(data);
        Assert.Equal(50, data.UsagePercent);
        Assert.Equal(2, data.LogicalProcessorCount);
        Assert.Equal(250, data.SampleWindowMilliseconds);
    }

    [Fact]
    public void TryCalculateCpuUsageRejectsMalformedSamples()
    {
        var success = SystemMetricsSnapshotService.TryCalculateCpuUsage(
            "not cpu data",
            "still not cpu data",
            250,
            out var data);

        Assert.False(success);
        Assert.Null(data);
    }

    [Fact]
    public void TryParseMemorySnapshotCalculatesBytesAndUsage()
    {
        const string snapshot =
            """
            MemTotal:       16777216 kB
            MemFree:         1048576 kB
            MemAvailable:    8388608 kB
            """;

        var success = SystemMetricsSnapshotService.TryParseMemorySnapshot(
            snapshot,
            out var data);

        Assert.True(success);
        Assert.NotNull(data);
        Assert.Equal(17_179_869_184, data.TotalBytes);
        Assert.Equal(8_589_934_592, data.UsedBytes);
        Assert.Equal(8_589_934_592, data.AvailableBytes);
        Assert.Equal(50, data.UsagePercent);
    }

    [Theory]
    [InlineData("MemTotal: -1 kB\nMemAvailable: 0 kB")]
    [InlineData("MemTotal: 100 kB\nMemAvailable: 101 kB")]
    [InlineData("MemTotal: 0 kB\nMemAvailable: 0 kB")]
    [InlineData("MemTotal: 100 kB")]
    public void TryParseMemorySnapshotRejectsInvalidValues(string snapshot)
    {
        var success = SystemMetricsSnapshotService.TryParseMemorySnapshot(
            snapshot,
            out var data);

        Assert.False(success);
        Assert.Null(data);
    }
}
