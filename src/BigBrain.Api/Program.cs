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
        builder.Services.AddSingleton<IModuleRegistry>(
            new InMemoryModuleRegistry([SystemModule.Definition]));

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
            (IModuleRegistry registry) => Results.Ok(registry.GetModules()));

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true
        });

        app.Run();
    }
}
