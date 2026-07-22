namespace BigBrain.Modules;

public interface IModuleRegistry
{
    IReadOnlyList<ModuleDefinition> GetModules();
}

