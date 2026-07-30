using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BigBrain.Sentinel.Contracts;

public static class SentinelProtocol
{
    public const string PingPath = "/sentinel/v1/ping";
    public const string CapabilityRegistryPath = "/sentinel/v1/capabilities";
    public const string ReadSnapshotPath = "/sentinel/v1/capabilities/inventory.read-snapshot";
    public const string InventoryReadSnapshot = "Inventory.ReadSnapshot";
    public const int InventoryReadSnapshotVersion = 1;
    public const string HostReadUptime = "Host.ReadUptime";
    public const int HostReadUptimeVersion = 1;
    public const string HostReadCpu = "Host.ReadCpu";
    public const int HostReadCpuVersion = 1;
    public const string HostReadMemory = "Host.ReadMemory";
    public const int HostReadMemoryVersion = 1;
    public const string CapabilityUnavailable = "CAPABILITY_UNAVAILABLE";
}

public sealed record SentinelPingResponse(
    string Status,
    string NodeId,
    string Version,
    int CapabilityCount,
    DateTimeOffset CheckedAtUtc);

public sealed record SentinelCapabilityDescriptor(
    string Name,
    int Version,
    string Effect,
    string Availability);

public sealed record SentinelCapabilityRegistryResponse(
    string NodeId,
    IReadOnlyList<SentinelCapabilityDescriptor> Capabilities);

public sealed record SentinelResourceSelector(string FilesystemSet);

public sealed record SentinelSnapshotSection(
    string Capability,
    IReadOnlyList<string> Fields,
    SentinelResourceSelector? ResourceSelector = null);

public sealed record SentinelSnapshotArguments(IReadOnlyList<SentinelSnapshotSection> Sections);

public sealed record SentinelAuthorizationProof(string KeyId, string Signature);

public sealed record SentinelCapabilityRequest(
    string MessageId,
    string NodeId,
    DateTimeOffset ExpiresAtUtc,
    string Capability,
    int Version,
    SentinelSnapshotArguments Arguments,
    SentinelAuthorizationProof AuthorizationProof);

public sealed record SentinelProtocolError(
    string Code,
    string Message,
    bool Retryable);

public sealed record SentinelUptimeData(double UptimeSeconds);

public sealed record SentinelUptimeSection(
    string Status,
    SentinelUptimeData? Data,
    SentinelProtocolError? Error);

public sealed record SentinelCpuData(
    int LogicalProcessorCount,
    double UsagePercent,
    int SampleWindowMilliseconds);

public sealed record SentinelCpuSection(
    string Status,
    SentinelCpuData? Data,
    SentinelProtocolError? Error);

public sealed record SentinelMemoryData(
    long TotalBytes,
    long UsedBytes,
    long AvailableBytes,
    double UsagePercent);

public sealed record SentinelMemorySection(
    string Status,
    SentinelMemoryData? Data,
    SentinelProtocolError? Error);

public sealed record SentinelUnavailableSection(
    string Status,
    SentinelProtocolError Error);

public sealed record SentinelDiskSection(
    string Status,
    IReadOnlyList<object> Items,
    SentinelProtocolError Error);

public sealed record SentinelSnapshotSections(
    SentinelUptimeSection Uptime,
    SentinelCpuSection Cpu,
    SentinelMemorySection Memory,
    SentinelDiskSection Disks);

public sealed record SentinelSnapshotResponse(
    string SnapshotId,
    string NodeId,
    DateTimeOffset CollectedAtUtc,
    string Status,
    SentinelSnapshotSections Sections,
    IReadOnlyList<string> Warnings);

public static class SentinelSnapshotRequest
{
    private static readonly IReadOnlyList<string> UptimeFields =
        Array.AsReadOnly(["uptimeSeconds"]);
    private static readonly IReadOnlyList<string> CpuFields =
        Array.AsReadOnly(["logicalProcessorCount", "usagePercent", "sampleWindowMilliseconds"]);
    private static readonly IReadOnlyList<string> MemoryFields =
        Array.AsReadOnly(["totalBytes", "usedBytes", "availableBytes", "usagePercent"]);
    private static readonly IReadOnlyList<string> DiskFields =
        Array.AsReadOnly(
            ["filesystemId", "displayName", "totalBytes", "usedBytes", "availableBytes", "usagePercent"]);

    public static SentinelSnapshotArguments CreateArguments() =>
        new(
        [
            new("Host.ReadUptime@1", UptimeFields),
            new("Host.ReadCpu@1", CpuFields),
            new("Host.ReadMemory@1", MemoryFields),
            new(
                "Host.ReadDisk@1",
                DiskFields,
                new SentinelResourceSelector("system-dashboard"))
        ]);
}

public static class SentinelRequestCanonicalizer
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public static byte[] CreateSigningPayload(
        string messageId,
        string nodeId,
        DateTimeOffset expiresAtUtc,
        string capability,
        int version,
        SentinelSnapshotArguments arguments)
    {
        var argumentsHash = Convert.ToHexString(
            SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(arguments, JsonOptions)));
        var value = string.Join(
            '\n',
            messageId,
            nodeId,
            expiresAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            capability,
            version.ToString(CultureInfo.InvariantCulture),
            argumentsHash);

        return Encoding.UTF8.GetBytes(value);
    }
}
