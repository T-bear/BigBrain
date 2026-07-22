using System.Net;
using System.Net.Http.Json;
using BigBrain.Api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BigBrain.Api.Tests;

public sealed class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SystemHealthReturnsHealthyStatus()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/v1/system/health", cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.Status);
    }

    [Fact]
    public async Task ModulesReturnsSystemModuleWithWidget()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/v1/modules", cancellationToken);
        var modules = await response.Content.ReadFromJsonAsync<ModuleResponse[]>(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var module = Assert.Single(Assert.IsType<ModuleResponse[]>(modules));
        Assert.Equal("system", module.Id);
        Assert.Contains(module.DashboardWidgets, widget => widget.Id == "system-health");
    }

    [Fact]
    public async Task UnknownApiRouteReturnsProblemDetails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/v1/unknown", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private sealed record HealthResponse(string Status);

    private sealed record ModuleResponse(
        string Id,
        IReadOnlyList<WidgetResponse> DashboardWidgets);

    private sealed record WidgetResponse(string Id);
}
