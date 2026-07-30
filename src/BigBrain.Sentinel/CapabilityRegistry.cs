using System.Collections.ObjectModel;
using BigBrain.Sentinel.Contracts;

namespace BigBrain.Sentinel;

public interface ICapabilityRegistry
{
    int Count { get; }

    IReadOnlyList<SentinelCapabilityDescriptor> GetCapabilities();

    bool Contains(string name, int version);
}

public sealed class EmptyCapabilityRegistry : ICapabilityRegistry
{
    public int Count => 0;

    public IReadOnlyList<SentinelCapabilityDescriptor> GetCapabilities() => [];

    public bool Contains(string name, int version) => false;
}

public sealed class SystemMetricsCapabilityRegistry : ICapabilityRegistry
{
    private static readonly SentinelCapabilityDescriptor Uptime =
        new(
            SentinelProtocol.HostReadUptime,
            SentinelProtocol.HostReadUptimeVersion,
            "read",
            "available");

    private static readonly SentinelCapabilityDescriptor Snapshot =
        new(
            SentinelProtocol.InventoryReadSnapshot,
            SentinelProtocol.InventoryReadSnapshotVersion,
            "read",
            "partial");

    private static readonly SentinelCapabilityDescriptor Cpu =
        new(
            SentinelProtocol.HostReadCpu,
            SentinelProtocol.HostReadCpuVersion,
            "read",
            "available");

    private static readonly SentinelCapabilityDescriptor Memory =
        new(
            SentinelProtocol.HostReadMemory,
            SentinelProtocol.HostReadMemoryVersion,
            "read",
            "available");

    private static readonly ReadOnlyCollection<SentinelCapabilityDescriptor> Capabilities =
        Array.AsReadOnly([Uptime, Cpu, Memory, Snapshot]);

    public int Count => Capabilities.Count;

    public IReadOnlyList<SentinelCapabilityDescriptor> GetCapabilities() => Capabilities;

    public bool Contains(string name, int version) =>
        Capabilities.Any(
            capability =>
                string.Equals(name, capability.Name, StringComparison.Ordinal)
                && version == capability.Version);
}
