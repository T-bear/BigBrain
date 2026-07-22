using BigBrain.Modules;

namespace BigBrain.Api.Tests;

public sealed class ModuleRegistryTests
{
    [Fact]
    public void RegistryReturnsRegisteredModulesInStableOrder()
    {
        var alpha = SystemModule.Definition with { Id = "alpha", Name = "Alpha" };
        var zulu = SystemModule.Definition with { Id = "zulu", Name = "Zulu" };
        var registry = new InMemoryModuleRegistry([zulu, alpha]);

        var modules = registry.GetModules();

        Assert.Equal(["alpha", "zulu"], modules.Select(module => module.Id));
    }
}
