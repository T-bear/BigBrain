using System.Net;
using System.Net.Http.Json;
using BigBrain.Api;
using BigBrain.Api.Media;
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
    public async Task AutonomousResearchHistoryEndpointsAreBoundedReadOnlyContracts()
    {
        var runs=await _client.GetAsync("/api/v1/modules/finance/research/autonomous/runs?offset=0&limit=10",TestContext.Current.CancellationToken);
        var experiments=await _client.GetAsync("/api/v1/modules/finance/research/autonomous/experiments?offset=0&limit=10&state=completed",TestContext.Current.CancellationToken);
        var invalid=await _client.GetAsync("/api/v1/modules/finance/research/autonomous/experiments?limit=101",TestContext.Current.CancellationToken);
        var missingRun=await _client.GetAsync("/api/v1/modules/finance/research/autonomous/runs/research-run-missing",TestContext.Current.CancellationToken);
        var missingExperiment=await _client.GetAsync("/api/v1/modules/finance/research/autonomous/experiments/experiment-missing",TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK,runs.StatusCode);Assert.Equal(HttpStatusCode.OK,experiments.StatusCode);Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);Assert.Equal(HttpStatusCode.NotFound,missingRun.StatusCode);Assert.Equal(HttpStatusCode.NotFound,missingExperiment.StatusCode);
    }

    [Fact]
    public async Task ResearchSchedulerStatusIsDisabledByDefaultAndHistoryIsBounded()
    {
        var status=await _client.GetAsync("/api/v1/modules/finance/research/scheduler/status",TestContext.Current.CancellationToken);var history=await _client.GetAsync("/api/v1/modules/finance/research/scheduler/history?offset=0&limit=10",TestContext.Current.CancellationToken);var invalid=await _client.GetAsync("/api/v1/modules/finance/research/scheduler/history?limit=101",TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK,status.StatusCode);var body=await status.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);Assert.Contains("\"enabled\":false",body,StringComparison.Ordinal);Assert.Contains("\"dataReady\":false",body,StringComparison.Ordinal);Assert.Contains("\"readinessReason\":\"finance.research.scheduler.universeIncomplete\"",body,StringComparison.Ordinal);Assert.Contains("\"expectedInstrumentCount\":8",body,StringComparison.Ordinal);Assert.Contains("\"operatingMode\":\"RESEARCH\"",body,StringComparison.Ordinal);Assert.Contains("\"executionAuthority\":\"NONE\"",body,StringComparison.Ordinal);Assert.Equal(HttpStatusCode.OK,history.StatusCode);Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);
    }

    [Fact]
    public async Task ResearchGovernorStatusFailsClosedWithoutSentinelAndRemainsResearchOnly()
    {
        var response=await _client.GetAsync("/api/v1/modules/finance/research/governor/status",TestContext.Current.CancellationToken);var body=await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Contains("\"decision\":\"defer\"",body,StringComparison.Ordinal);Assert.Contains("finance.research.scheduler.resource.metricsUnavailable",body,StringComparison.Ordinal);Assert.Contains("\"temperatureSupported\":false",body,StringComparison.Ordinal);Assert.Contains("\"operatingMode\":\"RESEARCH\"",body,StringComparison.Ordinal);Assert.Contains("\"budgetSek\":0",body,StringComparison.Ordinal);Assert.Contains("\"executionAuthority\":\"NONE\"",body,StringComparison.Ordinal);
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
    public async Task SentinelPingReturnsProblemDetailsWhenLocalTransportIsDisabled()
    {
        var response = await _client.GetAsync(
            "/api/v1/system/sentinel/ping",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("sentinelUnavailable", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SentinelMetricsSkeletonReturnsProblemDetailsWhenLocalTransportIsDisabled()
    {
        var response = await _client.PostAsync(
            "/api/v1/system/sentinel/read-system-metrics",
            content: null,
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("sentinelUnavailable", body, StringComparison.Ordinal);
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
    public async Task ModulesReturnsRegisteredModules()
    {
        var response = await _client.GetAsync("/api/v1/modules", TestContext.Current.CancellationToken);
        var modules = await response.Content.ReadFromJsonAsync<ModuleResponse[]>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = Assert.IsType<ModuleResponse[]>(modules);
        Assert.Equal(["docker", "finance", "shopping-list", "calendar", "meal-planner", "media", "system"], result.Select(module => module.Id));
        Assert.Equal("Unavailable", Assert.Single(result, module => module.Id == "docker").Status);
        Assert.Equal("Available", Assert.Single(result, module => module.Id == "meal-planner").Status);
        Assert.Equal("Available", Assert.Single(result, module => module.Id == "shopping-list").Status);
        Assert.Equal("NotConfigured", Assert.Single(result, module => module.Id == "media").Status);
        Assert.Equal("Research", Assert.Single(result, module => module.Id == "finance").Status);
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
        Assert.Equal("Configure media services to calculate health.", overview.HealthSummary);
        Assert.Equal("notConfigured", overview.HealthStatusLevel);
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

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    [InlineData(" a ")]
    public async Task MediaSearchRejectsQueriesShorterThanTwoCharacters(string query)
    {
        var response = await _client.GetAsync(
            $"/api/v1/modules/media/search?query={Uri.EscapeDataString(query)}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task MediaSearchReturnsAllNotConfiguredProviders()
    {
        var response = await _client.GetAsync(
            "/api/v1/modules/media/search?query=Family%20Guy",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<MediaSearchResponse>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var search = Assert.IsType<MediaSearchResponse>(body);
        Assert.Equal(MediaSearchStatuses.Unavailable, search.Status);
        Assert.Equal(["Jellyfin", "Sonarr", "Radarr"], search.Providers.Select(provider => provider.Provider));
        Assert.All(search.Providers, provider => Assert.Equal(MediaStatuses.NotConfigured, provider.Status));
    }

    [Theory]
    [InlineData("", "all")]
    [InlineData("a", "all")]
    [InlineData("title", "invalid")]
    public async Task MediaLookupValidatesQueryAndMediaType(string query, string mediaType)
    {
        var response = await _client.GetAsync(
            $"/api/v1/modules/media/lookup?query={Uri.EscapeDataString(query)}&mediaType={mediaType}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task MediaLookupKeepsProvidersSeparateWhenNotConfigured()
    {
        var response = await _client.GetAsync(
            "/api/v1/modules/media/lookup?query=Expanse&mediaType=all",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<MediaLookupResponse>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["Sonarr", "Radarr"], Assert.IsType<MediaLookupResponse>(body).Providers.Select(item => item.Provider));
    }

    [Fact]
    public async Task MediaJobsReturnsEmptyReadOnlySnapshotWhenProvidersAreNotConfigured()
    {
        var response = await _client.GetAsync(
            "/api/v1/modules/media/jobs",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<MediaJobsResponse>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(Assert.IsType<MediaJobsResponse>(body).Jobs);
    }

    [Theory]
    [InlineData("/api/v1/modules/media/jobs?limit=101")]
    [InlineData("/api/v1/modules/media/jobs?status=providerRawState")]
    [InlineData("/api/v1/modules/media/jobs?mediaType=torrent")]
    [InlineData("/api/v1/modules/media/jobs?provider=internalHost")]
    public async Task MediaJobsRejectsInvalidFiltersWithProblemDetails(string path)
    {
        var response = await _client.GetAsync(path, TestContext.Current.CancellationToken);
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("exception", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("http://", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("", "123", "series")]
    [InlineData("Sonarr", "invalid", "series")]
    [InlineData("Radarr", "123", "series")]
    public async Task MediaLibraryStatusRejectsInvalidIdentity(
        string provider,
        string foreignId,
        string mediaType)
    {
        var response = await _client.GetAsync(
            $"/api/v1/modules/media/library-status?provider={provider}&foreignId={foreignId}&mediaType={mediaType}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task MediaPlayRejectsUnsafeItemIdentifier()
    {
        var response = await _client.GetAsync(
            "/api/v1/modules/media/play/unsafe%2Fitem",
            TestContext.Current.CancellationToken);

        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound);
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
