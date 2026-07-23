using System.Net;
using System.Net.Http.Json;
using BigBrain.Sentinel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BigBrain.Sentinel.Tests;

public sealed class SentinelBootstrapTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SentinelBootstrapTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task HealthReturnsBoundedProcessStatus()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<SentinelHealthResponse>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var health = Assert.IsType<SentinelHealthResponse>(body);
        Assert.Equal("Healthy", health.Status);
        Assert.False(string.IsNullOrWhiteSpace(health.Version));
        Assert.Equal(0, health.CapabilityCount);
        Assert.True(health.CheckedAtUtc <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void DependencyInjectionProvidesEmptySingletonRegistry()
    {
        var first = _factory.Services.GetRequiredService<ICapabilityRegistry>();
        var second = _factory.Services.GetRequiredService<ICapabilityRegistry>();

        Assert.Same(first, second);
        Assert.IsType<EmptyCapabilityRegistry>(first);
        Assert.Equal(0, first.Count);
    }

    [Fact]
    public void DependencyInjectionProvidesDeterministicVersion()
    {
        var provider = _factory.Services.GetRequiredService<ISentinelVersionProvider>();

        Assert.Equal(provider.GetVersion(), provider.GetVersion());
        Assert.StartsWith("0.1.0-alpha", provider.GetVersion().Version, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidConfigurationFailsStartup()
    {
        using var invalidFactory = _factory.WithWebHostBuilder(
            builder => builder.UseSetting("Sentinel:HealthPath", "not-an-absolute-path"));

        var exception = Assert.ThrowsAny<Exception>(() => invalidFactory.CreateClient());

        Assert.Contains(
            "SentinelOptions",
            exception.ToString(),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/api/v1/sentinel")]
    [InlineData("/capabilities")]
    [InlineData("/version")]
    public async Task BootstrapExposesNoManagementEndpoints(string path)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void SentinelAssemblyHasNoAdapterOrExecutionTypes()
    {
        var prohibitedFragments = new[] { "Adapter", "Command", "Dispatcher", "Executor", "Docker" };
        var typeNames = typeof(Program).Assembly.GetTypes().Select(type => type.FullName ?? type.Name);

        Assert.DoesNotContain(
            typeNames,
            typeName => prohibitedFragments.Any(
                fragment => typeName.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void ConfigurationContainsOnlyBootstrapSettings()
    {
        var options = _factory.Services.GetRequiredService<IOptions<SentinelOptions>>().Value;

        Assert.Equal("BigBrain Sentinel", options.ServiceName);
        Assert.Equal("/health", options.HealthPath);
    }
}
