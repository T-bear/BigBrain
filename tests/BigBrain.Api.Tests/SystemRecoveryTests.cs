using BigBrain.Api.SystemRecovery;
using Microsoft.Extensions.Logging.Abstractions;

namespace BigBrain.Api.Tests;

public sealed class SystemRecoveryTests
{
    [Fact]
    public async Task GracefulStopIsObservedAsCleanByNextSession()
    {
        using var directory = new TestDirectory();
        var first = Coordinator(directory.Path);
        await first.StartAsync(CancellationToken.None);
        await first.WaitUntilRecoveredAsync(CancellationToken.None);
        await first.StopAsync(CancellationToken.None);

        var second = Coordinator(directory.Path);
        Assert.Equal(PreviousShutdownState.Clean, second.Snapshot().PreviousShutdown);
        await second.StartAsync(CancellationToken.None);
        await second.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task MissingCleanMarkerIsObservedAsUncleanAndRecoveryIsIdempotent()
    {
        using var directory = new TestDirectory();
        var interrupted = Coordinator(directory.Path);
        await interrupted.StartAsync(CancellationToken.None);
        await interrupted.WaitUntilRecoveredAsync(CancellationToken.None);

        var recovered = Coordinator(directory.Path);
        Assert.Equal(PreviousShutdownState.Unclean, recovered.Snapshot().PreviousShutdown);
        await recovered.StartAsync(CancellationToken.None);
        await recovered.WaitUntilRecoveredAsync(CancellationToken.None);
        var once = recovered.Snapshot();
        var twice = recovered.Snapshot();
        Assert.Equal(once.Overall, twice.Overall);
        Assert.Equal(once.Components, twice.Components);
        Assert.True(twice.RecoveryCompleted);
        await recovered.StopAsync(CancellationToken.None);
        await interrupted.StopAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData("finance-eodhd-daily", MissedRunPolicy.CatchUpOnce)]
    [InlineData("finance-features", MissedRunPolicy.DerivedFromSourceState)]
    [InlineData("finance-backtests", MissedRunPolicy.DerivedFromSourceState)]
    [InlineData("finance-robustness", MissedRunPolicy.DerivedFromSourceState)]
    [InlineData("media-refresh", MissedRunPolicy.SkipToNext)]
    public void ScheduledJobsHaveExplicitRecoveryPolicies(string job, MissedRunPolicy expected)
    {
        Assert.Equal(expected, Assert.Single(SystemRecoveryCoordinator.JobPolicies, value => value.Job == job).Policy);
    }

    private static SystemRecoveryCoordinator Coordinator(string directory) => new(
        new SystemRecoveryOptions
        {
            DatabasePath = Path.Combine(directory, "lifecycle.db"),
            ClockSyncDirectory = directory,
            LowDiskWarningBytes = 1,
            LowDiskCriticalBytes = 1
        }, NullLogger<SystemRecoveryCoordinator>.Instance);

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory() => Path = Directory.CreateTempSubdirectory("bigbrain-recovery-test-").FullName;
        public string Path { get; }
        public void Dispose() => Directory.Delete(Path, true);
    }
}
