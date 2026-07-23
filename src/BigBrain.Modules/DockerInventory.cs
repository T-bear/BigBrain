namespace BigBrain.Modules;

public sealed record DockerAvailability(bool Available, string Reason);
public sealed record DockerPort(int PrivatePort, int? PublicPort, string Protocol);
public sealed record DockerContainer(
    string Id,
    string Name,
    string Image,
    string State,
    string Status,
    string? Health,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    IReadOnlyList<DockerPort> Ports,
    double? CpuUsagePercent,
    long? MemoryUsageBytes,
    long? MemoryLimitBytes,
    double? MemoryUsagePercent);
public sealed record DockerInventory(
    DockerAvailability Availability,
    DateTimeOffset CollectedAtUtc,
    IReadOnlyList<DockerContainer> Containers);

public interface IDockerInventoryProvider
{
    Task<DockerInventory> GetContainersAsync(CancellationToken cancellationToken);
}

public sealed class UnavailableDockerInventoryProvider : IDockerInventoryProvider
{
    public Task<DockerInventory> GetContainersAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new DockerInventory(
            new DockerAvailability(false, "Docker inventory requires Sentinel integration."),
            DateTimeOffset.UtcNow,
            []));
}
