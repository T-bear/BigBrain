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

    [Fact]
    public void TryCreateDiskItemCalculatesConsistentCapacity()
    {
        var filesystem = new SentinelFilesystemOptions
        {
            FilesystemId = "system",
            DisplayName = "System Storage",
            SentinelPath = "/configured/path"
        };

        var success = SystemMetricsSnapshotService.TryCreateDiskItem(
            filesystem,
            1_000,
            600,
            out var item);

        Assert.True(success);
        Assert.NotNull(item);
        Assert.Equal("system", item.FilesystemId);
        Assert.Equal("System Storage", item.DisplayName);
        Assert.Equal(1_000, item.TotalBytes);
        Assert.Equal(400, item.UsedBytes);
        Assert.Equal(600, item.AvailableBytes);
        Assert.Equal(40, item.UsagePercent);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(100, -1)]
    [InlineData(100, 101)]
    public void TryCreateDiskItemRejectsInvalidCapacity(long totalBytes, long availableBytes)
    {
        var filesystem = new SentinelFilesystemOptions
        {
            FilesystemId = "system",
            DisplayName = "System Storage",
            SentinelPath = "/configured/path"
        };

        var success = SystemMetricsSnapshotService.TryCreateDiskItem(
            filesystem,
            totalBytes,
            availableBytes,
            out var item);

        Assert.False(success);
        Assert.Null(item);
    }

    [Fact]
    public void ReadDisksMeasuresOnlyConfiguredFilesystemsAndIsolatesFailures()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"bigbrain-disk-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var result = SystemMetricsSnapshotService.ReadDisks(
            [
                new SentinelFilesystemOptions
                {
                    FilesystemId = "available",
                    DisplayName = "Available Storage",
                    SentinelPath = directory
                },
                new SentinelFilesystemOptions
                {
                    FilesystemId = "missing",
                    DisplayName = "Missing Storage",
                    SentinelPath = Path.Combine(directory, "missing")
                }
            ]);

            Assert.Equal("partial", result.Status);
            Assert.Collection(
                result.Items,
                available =>
                {
                    Assert.Equal("available", available.FilesystemId);
                    Assert.Equal("available", available.Status);
                    Assert.True(available.TotalBytes > 0);
                },
                missing =>
                {
                    Assert.Equal("missing", missing.FilesystemId);
                    Assert.Equal("unavailable", missing.Status);
                    Assert.Null(missing.TotalBytes);
                });
        }
        finally
        {
            Directory.Delete(directory);
        }
    }

    [Fact]
    public void ReadDisksReturnsMultipleExplicitlyConfiguredFilesystems()
    {
        var firstDirectory = Path.Combine(
            Path.GetTempPath(),
            $"bigbrain-disk-first-{Guid.NewGuid():N}");
        var secondDirectory = Path.Combine(
            Path.GetTempPath(),
            $"bigbrain-disk-second-{Guid.NewGuid():N}");
        Directory.CreateDirectory(firstDirectory);
        Directory.CreateDirectory(secondDirectory);
        try
        {
            var result = SystemMetricsSnapshotService.ReadDisks(
            [
                new SentinelFilesystemOptions
                {
                    FilesystemId = "first",
                    DisplayName = "First Storage",
                    SentinelPath = firstDirectory
                },
                new SentinelFilesystemOptions
                {
                    FilesystemId = "second",
                    DisplayName = "Second Storage",
                    SentinelPath = secondDirectory
                }
            ]);

            Assert.Equal("available", result.Status);
            Assert.Collection(
                result.Items,
                first => Assert.Equal("first", first.FilesystemId),
                second => Assert.Equal("second", second.FilesystemId));
        }
        finally
        {
            Directory.Delete(firstDirectory);
            Directory.Delete(secondDirectory);
        }
    }
}
