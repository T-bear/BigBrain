using System.Net;
using System.Net.Http.Json;
using BigBrain.Api;
using BigBrain.Modules;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BigBrain.Api.Tests;

public sealed class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task SystemHealthReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/api/v1/system/health", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", Assert.IsType<HealthResponse>(body).Status);
    }

    [Fact]
    public async Task SystemOverviewReturnsUnavailableWithoutHostMetrics()
    {
        var response = await _client.GetAsync("/api/v1/system/overview", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<SystemOverviewResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var overview = Assert.IsType<SystemOverviewResponse>(body);
        Assert.Equal("Unavailable", overview.Status);
        Assert.Equal("Unavailable", overview.Hostname);
        Assert.Equal("Unavailable", overview.OperatingSystem);
        Assert.Null(overview.Cpu.UsagePercent);
        Assert.Equal(0, overview.Cpu.LogicalProcessorCount);
        Assert.Null(overview.Memory.UsagePercent);
        Assert.Empty(overview.Disks);
        Assert.Contains("Host metrics require Sentinel integration.", overview.Warnings);
        Assert.True(overview.CollectedAtUtc <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task UnavailableTemperatureDoesNotCauseServerError()
    {
        var response = await _client.GetAsync("/api/v1/system/overview", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<SystemOverviewResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Null(body.TemperatureCelsius);
        Assert.Equal("Unavailable", body.Status);
    }

    [Fact]
    public async Task SystemProviderIsHostIndependentAndLinuxProviderDoesNotExist()
    {
        var provider = new UnavailableSystemMetricsProvider();

        var overview = await provider.GetOverviewAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Unavailable", overview.Status);
        Assert.Empty(overview.Disks);
        Assert.Null(typeof(ISystemMetricsProvider).Assembly.GetType("BigBrain.Modules.LinuxSystemMetricsProvider"));
    }

    [Fact]
    public async Task DockerInventoryReturnsUnavailableWithoutContainers()
    {
        var response = await _client.GetAsync("/api/v1/docker/containers", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<DockerInventoryResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var inventory = Assert.IsType<DockerInventoryResponse>(body);
        Assert.False(inventory.Availability.Available);
        Assert.False(string.IsNullOrWhiteSpace(inventory.Availability.Reason));
        Assert.Empty(inventory.Containers);
    }

    [Fact]
    public async Task ModulesReturnsSystemAndDockerModules()
    {
        var response = await _client.GetAsync("/api/v1/modules", TestContext.Current.CancellationToken);
        var modules = await response.Content.ReadFromJsonAsync<ModuleResponse[]>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = Assert.IsType<ModuleResponse[]>(modules);
        Assert.Equal(["docker", "media", "system"], result.Select(module => module.Id));
        Assert.Equal("Unavailable", Assert.Single(result, module => module.Id == "docker").Status);
        Assert.Equal("NotConfigured", Assert.Single(result, module => module.Id == "media").Status);
        var system = Assert.Single(result, module => module.Id == "system");
        Assert.Equal("Unavailable", system.Status);
        Assert.Contains(system.DashboardWidgets, widget => widget.Id == "system-overview");
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task DockerHasNoMutatingEndpoints(string method)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1/docker/containers/example");
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MediaOverviewReturnsNotConfiguredWithoutCredentials()
    {
        var response = await _client.GetAsync("/api/v1/modules/media", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<MediaOverviewResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var overview = Assert.IsType<MediaOverviewResponse>(body);
        Assert.Equal("notConfigured", overview.Status);
        Assert.Equal(0, overview.HealthScore);
        Assert.Equal("Action recommended", overview.HealthSummary);
        Assert.Equal("critical", overview.HealthStatusLevel);
        Assert.Empty(overview.Insights);
        Assert.Equal(5, overview.Services.Count);
        Assert.All(overview.Services, service =>
        {
            Assert.Equal("notConfigured", service.Status);
            Assert.False(service.IsConfigured);
        });
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task MediaHasNoMutatingEndpoints(string method)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1/modules/media");
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task UnknownApiRouteReturnsProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/unknown", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private sealed record HealthResponse(string Status);
    private sealed record ModuleResponse(string Id, string Status, IReadOnlyList<WidgetResponse> DashboardWidgets);
    private sealed record WidgetResponse(string Id);
    private sealed record CpuResponse(double? UsagePercent, int LogicalProcessorCount);
    private sealed record MemoryResponse(double? UsagePercent);
    private sealed record DiskResponse(double? UsagePercent);
    private sealed record SystemOverviewResponse(
        string Hostname,
        string OperatingSystem,
        CpuResponse Cpu,
        MemoryResponse Memory,
        IReadOnlyList<DiskResponse> Disks,
        double? TemperatureCelsius,
        DateTimeOffset CollectedAtUtc,
        string Status,
        IReadOnlyList<string> Warnings);
    private sealed record AvailabilityResponse(bool Available, string Reason);
    private sealed record DockerInventoryResponse(AvailabilityResponse Availability, IReadOnlyList<object> Containers);
    private sealed record MediaServiceResponse(string Status, bool IsConfigured);
    private sealed record MediaOverviewResponse(
        string Status,
        int HealthScore,
        string HealthSummary,
        string HealthStatusLevel,
        IReadOnlyList<object> Insights,
        IReadOnlyList<MediaServiceResponse> Services);
}
