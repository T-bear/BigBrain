using BigBrain.Sentinel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BigBrain.Sentinel;

public sealed partial class Program
{
    public static void Main(string[] args)
    {
        var builder = SentinelHost.CreateBuilder(args);
        var app = SentinelHost.Build(builder);

        app.Run();
    }
}

public static class SentinelHost
{
    public static WebApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole();

        builder.Services
            .AddOptions<SentinelOptions>()
            .Bind(builder.Configuration.GetSection(SentinelOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddHealthChecks();
        builder.Services.AddSingleton<ICapabilityRegistry, EmptyCapabilityRegistry>();
        builder.Services.AddSingleton<ISentinelVersionProvider, AssemblySentinelVersionProvider>();

        return builder;
    }

    public static WebApplication Build(WebApplicationBuilder builder)
    {
        var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentinelOptions>>().Value;
        var capabilities = app.Services.GetRequiredService<ICapabilityRegistry>();
        var version = app.Services.GetRequiredService<ISentinelVersionProvider>().GetVersion();

        SentinelLog.BootstrapInitialized(
            app.Logger,
            version.Version,
            capabilities.Count);

        app.MapGet(
            options.HealthPath,
            async (
                HealthCheckService healthChecks,
                ICapabilityRegistry capabilities,
                ISentinelVersionProvider versionProvider,
                CancellationToken cancellationToken) =>
            {
                var report = await healthChecks.CheckHealthAsync(cancellationToken);
                var response = new SentinelHealthResponse(
                    report.Status.ToString(),
                    versionProvider.GetVersion().Version,
                    capabilities.Count,
                    DateTimeOffset.UtcNow);

                return report.Status == HealthStatus.Healthy
                    ? Results.Ok(response)
                    : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
            });

        return app;
    }
}
