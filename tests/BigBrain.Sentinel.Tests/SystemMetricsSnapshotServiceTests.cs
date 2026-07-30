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
}
