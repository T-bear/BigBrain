using System.Net;
using System.Net.Http.Json;
using BigBrain.Api;
using BigBrain.Api.Settings;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BigBrain.Api.Tests;

public sealed class SettingsTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"bigbrain-settings-{Guid.NewGuid():N}");

    [Fact]
    public async Task ThemePersistsAcrossStoreAndApiClients()
    {
        await using var factory = Factory();
        using var first = factory.CreateClient();
        var initial = (await first.GetFromJsonAsync<ThemeSetting>("/api/v1/settings/theme", TestContext.Current.CancellationToken))!;
        Assert.Equal("bigbrain-dark", initial.Theme); Assert.False(initial.Configured);
        var changed = await first.PutAsJsonAsync("/api/v1/settings/theme", new ThemeSetting("bigbrain-obsidian-gold"), TestContext.Current.CancellationToken);
        changed.EnsureSuccessStatusCode();
        using var second = factory.CreateClient();
        var persisted = (await second.GetFromJsonAsync<ThemeSetting>("/api/v1/settings/theme", TestContext.Current.CancellationToken))!;
        Assert.Equal("bigbrain-obsidian-gold", persisted.Theme); Assert.True(persisted.Configured);
    }

    [Fact]
    public async Task InvalidThemeUsesProblemDetailsWithoutChangingStoredValue()
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync("/api/v1/settings/theme", new ThemeSetting("unknown"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("settingsInvalidTheme", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
        Assert.Equal("bigbrain-dark", (await client.GetFromJsonAsync<ThemeSetting>("/api/v1/settings/theme", TestContext.Current.CancellationToken))!.Theme);
    }

    private WebApplicationFactory<Program> Factory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
    {
        services.RemoveAll<SettingsOptions>(); services.RemoveAll<SettingsStore>();
        services.AddSingleton(new SettingsOptions { DatabasePath = Path.Combine(directory, "settings.db") });
        services.AddSingleton<SettingsStore>();
    }));

    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
