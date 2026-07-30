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
    private static readonly SentinelCapabilityDescriptor Snapshot =
        new(
            SentinelProtocol.InventoryReadSnapshot,
            SentinelProtocol.InventoryReadSnapshotVersion,
            "read",
            "notImplemented");

    private static readonly ReadOnlyCollection<SentinelCapabilityDescriptor> Capabilities =
        Array.AsReadOnly([Snapshot]);

    public int Count => Capabilities.Count;

    public IReadOnlyList<SentinelCapabilityDescriptor> GetCapabilities() => Capabilities;

    public bool Contains(string name, int version) =>
        string.Equals(name, Snapshot.Name, StringComparison.Ordinal)
        && version == Snapshot.Version;
}
