namespace BigBrain.Modules;

public sealed class InMemoryModuleRegistry(IEnumerable<ModuleDefinition> modules) : IModuleRegistry
{
    private readonly IReadOnlyList<ModuleDefinition> _modules = modules
        .OrderBy(module => module.Name, StringComparer.Ordinal)
        .ToArray();

    public IReadOnlyList<ModuleDefinition> GetModules() => _modules;
}

