using BigBrain.Modules;
using BigBrain.Api.Media;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Http.Headers;

namespace BigBrain.Api;

public sealed record SystemHealthResponse(string Status, DateTimeOffset CheckedAtUtc);

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();
        builder.Services.AddOptions<MediaOptions>()
            .BindConfiguration(MediaOptions.SectionName)
            .Validate(MediaOptions.IsValid, "Media URLs and timeout must be valid.")
            .ValidateOnStart();
        builder.Services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MediaOptions>>().Value);
        AddMediaClient<IJellyfinClient, JellyfinClient>(builder.Services, options => options.Jellyfin.BaseUrl);
        AddMediaClient<ISonarrClient, SonarrClient>(builder.Services, options => options.Sonarr.BaseUrl);
        AddMediaClient<IRadarrClient, RadarrClient>(builder.Services, options => options.Radarr.BaseUrl);
        AddMediaClient<IProwlarrClient, ProwlarrClient>(builder.Services, options => options.Prowlarr.BaseUrl);
        builder.Services.AddHttpClient<IQBittorrentClient, QBittorrentClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<MediaOptions>();
            ConfigureMediaClient(httpClient, options.QBittorrent.BaseUrl, options.TimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(options.QBittorrent.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", options.QBittorrent.ApiKey);
            }
        });
        builder.Services.AddSingleton<IMediaHealthEngine, MediaHealthEngine>();
        builder.Services.AddTransient<IMediaService, MediaService>();
        builder.Services.AddSingleton<ISystemMetricsProvider, UnavailableSystemMetricsProvider>();
        builder.Services.AddSingleton<IDockerInventoryProvider, UnavailableDockerInventoryProvider>();
        builder.Services.AddSingleton<IModuleRegistry>(
            new InMemoryModuleRegistry([SystemModule.Definition, DockerModule.Definition, MediaModule.Definition]));

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
                MediaOptions mediaOptions,
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
                        "media" => module with { Status = mediaOptions.IsAnyServiceConfigured ? "Available" : "NotConfigured" },
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

        app.MapGet(
            "/api/v1/modules/media",
            async (IMediaService mediaService, CancellationToken cancellationToken) =>
                Results.Ok(await mediaService.GetOverviewAsync(cancellationToken)));

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true
        });

        app.Run();
    }

    private static void AddMediaClient<TClient, TImplementation>(
        IServiceCollection services,
        Func<MediaOptions, string> getBaseUrl)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddHttpClient<TClient, TImplementation>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<MediaOptions>();
            ConfigureMediaClient(httpClient, getBaseUrl(options), options.TimeoutSeconds);
        });
    }

    private static void ConfigureMediaClient(HttpClient httpClient, string baseUrl, int timeoutSeconds)
    {
        httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }
}
