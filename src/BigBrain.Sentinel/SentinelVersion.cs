using System.Reflection;

namespace BigBrain.Sentinel;

public sealed record SentinelVersion(string Version);

public interface ISentinelVersionProvider
{
    SentinelVersion GetVersion();
}

public sealed class AssemblySentinelVersionProvider : ISentinelVersionProvider
{
    private static readonly SentinelVersion Current = new(
        typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? throw new InvalidOperationException("Sentinel informational version is unavailable."));

    public SentinelVersion GetVersion() => Current;
}
