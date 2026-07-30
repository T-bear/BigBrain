using System.Diagnostics;
using System.Globalization;
using BigBrain.Sentinel.Contracts;
using Microsoft.Extensions.Options;

namespace BigBrain.Sentinel;

public interface ISystemMetricsSnapshotService
{
    Task<SentinelSnapshotResponse> ReadSnapshotAsync(CancellationToken cancellationToken);
}

public sealed class SystemMetricsSnapshotService(
    IHostUptimeReader uptimeReader,
    IOptions<SentinelProtocolOptions> options) : ISystemMetricsSnapshotService
{
    private static readonly TimeSpan CpuSampleWindow = TimeSpan.FromMilliseconds(250);
    private static readonly SentinelProtocolError NotImplemented =
        new(
            SentinelProtocol.CapabilityUnavailable,
            "The metric is not implemented.",
            false);

    public async Task<SentinelSnapshotResponse> ReadSnapshotAsync(
        CancellationToken cancellationToken)
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

        var cpu = await ReadCpuAsync(cancellationToken);
        var warnings = new List<string>();
        if (uptime.Status != "available")
        {
            warnings.Add("System uptime is unavailable.");
        }
        if (cpu.Status != "available")
        {
            warnings.Add("CPU metrics are unavailable.");
        }
        warnings.Add("Memory and disk metrics are not implemented.");

        return new SentinelSnapshotResponse(
            $"snapshot:{Guid.NewGuid():N}",
            options.Value.NodeId,
            DateTimeOffset.UtcNow,
            "partial",
            new SentinelSnapshotSections(
                uptime,
                cpu,
                new SentinelUnavailableSection("unavailable", NotImplemented),
                new SentinelDiskSection("unavailable", [], NotImplemented)),
            warnings);
    }

    internal static bool TryCalculateCpuUsage(
        string firstSnapshot,
        string secondSnapshot,
        int sampleWindowMilliseconds,
        out SentinelCpuData? data)
    {
        data = null;
        if (!TryParseCpuSnapshot(firstSnapshot, out var first)
            || !TryParseCpuSnapshot(secondSnapshot, out var second)
            || second.Total <= first.Total
            || second.Idle < first.Idle)
        {
            return false;
        }

        var totalDelta = second.Total - first.Total;
        var idleDelta = second.Idle - first.Idle;
        if (idleDelta > totalDelta)
        {
            return false;
        }

        var usagePercent = (totalDelta - idleDelta) * 100d / totalDelta;
        data = new SentinelCpuData(
            first.LogicalProcessorCount,
            usagePercent,
            sampleWindowMilliseconds);
        return true;
    }

    private static async Task<SentinelCpuSection> ReadCpuAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var firstSnapshot = await File.ReadAllTextAsync("/proc/stat", cancellationToken);
            var stopwatch = Stopwatch.StartNew();
            await Task.Delay(CpuSampleWindow, cancellationToken);
            var secondSnapshot = await File.ReadAllTextAsync("/proc/stat", cancellationToken);
            stopwatch.Stop();

            if (TryCalculateCpuUsage(
                    firstSnapshot,
                    secondSnapshot,
                    checked((int)stopwatch.ElapsedMilliseconds),
                    out var data))
            {
                return new SentinelCpuSection("available", data, null);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return new SentinelCpuSection(
            "unavailable",
            null,
            new SentinelProtocolError(
                "DEPENDENCY_UNAVAILABLE",
                "CPU metrics are unavailable.",
                true));
    }

    private static bool TryParseCpuSnapshot(string snapshot, out CpuSnapshot parsed)
    {
        parsed = default;
        var lines = snapshot.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var aggregate = lines.FirstOrDefault(
            line => line.StartsWith("cpu ", StringComparison.Ordinal));
        var logicalProcessorCount = lines.Count(
            line => line.Length > 3
                && line.StartsWith("cpu", StringComparison.Ordinal)
                && char.IsAsciiDigit(line[3]));

        if (aggregate is null || logicalProcessorCount <= 0)
        {
            return false;
        }

        var fields = aggregate.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (fields.Length < 8)
        {
            return false;
        }

        Span<ulong> counters = stackalloc ulong[8];
        for (var index = 0; index < counters.Length; index++)
        {
            if (!ulong.TryParse(
                    fields[index + 1],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out counters[index]))
            {
                return false;
            }
        }

        ulong total = 0;
        foreach (var counter in counters)
        {
            total += counter;
        }

        parsed = new CpuSnapshot(total, counters[3] + counters[4], logicalProcessorCount);
        return true;
    }

    private readonly record struct CpuSnapshot(
        ulong Total,
        ulong Idle,
        int LogicalProcessorCount);
}
