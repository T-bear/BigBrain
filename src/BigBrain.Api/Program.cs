using BigBrain.Modules;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BigBrain.Api;

public sealed record SystemHealthResponse(string Status, DateTimeOffset CheckedAtUtc);

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();
        builder.Services.AddSingleton<ISystemMetricsProvider, UnavailableSystemMetricsProvider>();
        builder.Services.AddSingleton<IDockerInventoryProvider, UnavailableDockerInventoryProvider>();
        builder.Services.AddSingleton<IModuleRegistry>(
            new InMemoryModuleRegistry([SystemModule.Definition, DockerModule.Definition]));

        var app = builder.Build();

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.MapGet(
            "/api/v1/system/health",
            async (HealthCheckService healthChecks, CancellationToken cancellationToken) =>
            {
                var report = await healthChecks.CheckHealthAsync(cancellationToken);
                var response = new SystemHealthResponse(
                    report.Status.ToString(),
                    DateTimeOffset.UtcNow);

                return report.Status == HealthStatus.Healthy
                    ? Results.Ok(response)
                    : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
            });

        app.MapGet(
            "/api/v1/modules",
            async (
                IModuleRegistry registry,
                ISystemMetricsProvider systemProvider,
                IDockerInventoryProvider dockerProvider,
                CancellationToken cancellationToken) =>
            {
                var systemOverview = await systemProvider.GetOverviewAsync(cancellationToken);
                var inventory = await dockerProvider.GetContainersAsync(cancellationToken);
                var dockerStatus = !inventory.Availability.Available
                    ? "Unavailable"
                    : string.IsNullOrWhiteSpace(inventory.Availability.Reason) ? "Available" : "Degraded";
                var modules = registry.GetModules()
                    .Select(module => module.Id switch
                    {
                        "system" => module with { Status = systemOverview.Status },
                        "docker" => module with { Status = dockerStatus },
                        _ => module
                    });
                return Results.Ok(modules);
            });

        app.MapGet(
            "/api/v1/system/overview",
            async (ISystemMetricsProvider provider, CancellationToken cancellationToken) =>
                Results.Ok(await provider.GetOverviewAsync(cancellationToken)));

        app.MapGet(
            "/api/v1/docker/containers",
            async (IDockerInventoryProvider provider, CancellationToken cancellationToken) =>
                Results.Ok(await provider.GetContainersAsync(cancellationToken)));

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true
        });

        app.Run();
    }
}
