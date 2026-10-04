using System.Reflection;

namespace BigBrain.Api;

internal static class BuildRevision
{
    internal static string Current { get; } = Normalize(typeof(BuildRevision).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>().SingleOrDefault(x => x.Key == "GitRevision")?.Value);

    internal static string Normalize(string? value) => value is { Length: 40 } &&
        value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f') && value.Any(c => c != '0') ? value : "UNKNOWN";
}
