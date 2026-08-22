using BigBrain.Modules;
using BigBrain.Api.Media;
using BigBrain.Api.MealPlanner;
using BigBrain.Api.ShoppingList;
using BigBrain.Api.Calendar;
using BigBrain.Api.Settings;
using BigBrain.Api.Sentinel;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using BigBrain.Sentinel.Contracts;
using BigBrain.Api.SystemRecovery;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BigBrain.Api;

public sealed record SystemHealthResponse(string Status, DateTimeOffset CheckedAtUtc);

public partial class Program
{
    private static readonly System.Text.Json.JsonSerializerOptions WebJsonOptions =
        new(System.Text.Json.JsonSerializerDefaults.Web);

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        if (EodhdMaintenanceCommand.TryRun(args, builder.Configuration) || FinanceDatasetMaintenanceCommand.TryRun(args, builder.Configuration) || FinanceDataProtectionMaintenanceCommand.TryRun(args, builder.Configuration) || FinanceMacroMaintenanceCommand.TryRun(args,builder.Configuration) || FinanceClosureMaintenanceCommand.TryRun(args,builder.Configuration)) return;

        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();
        var recoveryOptions = builder.Configuration.GetSection(SystemRecoveryOptions.SectionName).Get<SystemRecoveryOptions>() ?? new();
        builder.Services.AddSingleton(recoveryOptions);
        builder.Services.AddSingleton<SystemRecoveryCoordinator>();
        builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<SystemRecoveryCoordinator>());
        var mealPlannerOptions = builder.Configuration
            .GetSection(MealPlannerOptions.SectionName)
            .Get<MealPlannerOptions>() ?? new MealPlannerOptions();
        builder.Services.AddSingleton(mealPlannerOptions);
        builder.Services.AddSingleton<MealPlannerStore>();
        builder.Services.AddSingleton<IFamilySchedule, TwoWeekFamilySchedule>();
        builder.Services.AddSingleton<IMealSelectionRandomFactory, SeededMealSelectionRandomFactory>();
        builder.Services.AddSingleton<MealPlanGenerator>();
        builder.Services.AddSingleton<MealPlannerService>();
        var shoppingListOptions = builder.Configuration.GetSection(ShoppingListOptions.SectionName).Get<ShoppingListOptions>() ?? new();
        builder.Services.AddSingleton(shoppingListOptions);
        builder.Services.AddSingleton<ShoppingListStore>();
        var calendarOptions = builder.Configuration.GetSection(CalendarOptions.SectionName).Get<CalendarOptions>() ?? new();
        builder.Services.AddSingleton(calendarOptions);
        builder.Services.AddSingleton<CalendarStore>();
        builder.Services.AddSingleton<HeromaScheduleParser>();
        builder.Services.AddSingleton<CalendarImportService>();
        var settingsOptions = builder.Configuration.GetSection(SettingsOptions.SectionName).Get<SettingsOptions>() ?? new();
        builder.Services.AddSingleton(settingsOptions);
        builder.Services.AddSingleton<SettingsStore>();
        builder.Services
            .AddOptions<SentinelClientOptions>()
            .BindConfiguration(SentinelClientOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<SentinelClientOptions>, SentinelClientOptionsValidator>();
        var sentinelOptions = builder.Configuration
            .GetSection(SentinelClientOptions.SectionName)
            .Get<SentinelClientOptions>() ?? new SentinelClientOptions();
        if (sentinelOptions.Enabled)
        {
            builder.Services.AddSingleton<ISentinelClient, LocalSentinelClient>();
            builder.Services.AddSingleton<ISystemMetricsProvider, SentinelSystemMetricsProvider>();
        }
        else
        {
            builder.Services.AddSingleton<ISentinelClient, DisabledSentinelClient>();
            builder.Services.AddSingleton<ISystemMetricsProvider, UnavailableSystemMetricsProvider>();
        }
        builder.Services.AddOptions<MediaOptions>()
            .BindConfiguration(MediaOptions.SectionName)
            .Validate(MediaOptions.IsValid, "Media configuration is invalid; Smart Shuffle requires a Jellyfin UserId when enabled.")
            .ValidateOnStart();
        builder.Services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MediaOptions>>().Value);
        AddMediaClient<IJellyfinClient, JellyfinClient>(
            builder.Services,
            "Jellyfin",
            options => options.Jellyfin.BaseUrl,
            options => Math.Max(options.TimeoutSeconds, options.SmartShuffle.RequestTimeoutSeconds));
        AddMediaClient<ISonarrClient, SonarrClient>(builder.Services, "Sonarr", options => options.Sonarr.BaseUrl);
        AddMediaClient<IRadarrClient, RadarrClient>(builder.Services, "Radarr", options => options.Radarr.BaseUrl);
        AddMediaClient<IProwlarrClient, ProwlarrClient>(builder.Services, "Prowlarr", options => options.Prowlarr.BaseUrl);
        builder.Services.AddHttpClient<IQBittorrentClient, QBittorrentClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<MediaOptions>();
            ConfigureMediaClient(httpClient, options.QBittorrent.BaseUrl, options.TimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(options.QBittorrent.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", options.QBittorrent.ApiKey);
            }
        }).AddHttpMessageHandler(serviceProvider =>
            new ProviderHttpLoggingHandler(
                "qBittorrent",
                serviceProvider.GetRequiredService<ILogger<ProviderHttpLoggingHandler>>()));
        builder.Services.AddSingleton<IMediaHealthEngine, MediaHealthEngine>();
        builder.Services.AddTransient<IMediaService, MediaService>();
        builder.Services.AddTransient<IMediaSearchProvider>(serviceProvider =>
            (IMediaSearchProvider)serviceProvider.GetRequiredService<IJellyfinClient>());
        builder.Services.AddTransient<IMediaSearchProvider>(serviceProvider =>
            (IMediaSearchProvider)serviceProvider.GetRequiredService<ISonarrClient>());
        builder.Services.AddTransient<IMediaSearchProvider>(serviceProvider =>
            (IMediaSearchProvider)serviceProvider.GetRequiredService<IRadarrClient>());
        builder.Services.AddTransient<IMediaSearchService, MediaSearchService>();
        builder.Services.AddTransient<IMediaLookupProvider>(serviceProvider =>
            (IMediaLookupProvider)serviceProvider.GetRequiredService<ISonarrClient>());
        builder.Services.AddTransient<IMediaLookupProvider>(serviceProvider =>
            (IMediaLookupProvider)serviceProvider.GetRequiredService<IRadarrClient>());
        builder.Services.AddTransient<IMediaLookupService, MediaLookupService>();
        builder.Services.AddSingleton<MediaPosterService>();
        builder.Services.AddHttpClient("MediaPosters")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            })
            .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(5));
        builder.Services.AddTransient<IMediaJobsProvider>(serviceProvider =>
            (IMediaJobsProvider)serviceProvider.GetRequiredService<ISonarrClient>());
        builder.Services.AddTransient<IMediaJobsProvider>(serviceProvider =>
            (IMediaJobsProvider)serviceProvider.GetRequiredService<IRadarrClient>());
        builder.Services.AddTransient<IMediaJobsProvider>(serviceProvider =>
            (IMediaJobsProvider)serviceProvider.GetRequiredService<IQBittorrentClient>());
        builder.Services.AddTransient<IQBittorrentQueueClient>(serviceProvider =>
            (IQBittorrentQueueClient)serviceProvider.GetRequiredService<IQBittorrentClient>());
        builder.Services.AddSingleton<DownloadControlStore>();
        builder.Services.AddTransient<IDownloadControlService, DownloadControlService>();
        builder.Services.AddTransient<IMediaLibraryCatalog>(serviceProvider =>
            (IMediaLibraryCatalog)serviceProvider.GetRequiredService<IJellyfinClient>());
        builder.Services.AddTransient<IJellyfinPlaybackClient>(serviceProvider =>
            (IJellyfinPlaybackClient)serviceProvider.GetRequiredService<IJellyfinClient>());
        builder.Services.AddSingleton<IMediaJobsService, MediaJobsService>();
        builder.Services.AddSingleton<ISmartShuffleRandom, SmartShuffleRandom>();
        builder.Services.AddSingleton<SmartShuffleSelector>();
        builder.Services.AddSingleton<ISmartShuffleStore, SmartShuffleStore>();
        builder.Services.AddTransient<ISmartShuffleService, SmartShuffleService>();
        builder.Services.AddHostedService<SmartShuffleCoordinator>();
        builder.Services.AddTransient<IMediaRequestProvider>(serviceProvider =>
            (IMediaRequestProvider)serviceProvider.GetRequiredService<ISonarrClient>());
        builder.Services.AddTransient<IMediaRequestProvider>(serviceProvider =>
            (IMediaRequestProvider)serviceProvider.GetRequiredService<IRadarrClient>());
        builder.Services.AddTransient<IMediaAddProvider>(serviceProvider =>
            (IMediaAddProvider)serviceProvider.GetRequiredService<ISonarrClient>());
        builder.Services.AddTransient<IMediaAddProvider>(serviceProvider =>
            (IMediaAddProvider)serviceProvider.GetRequiredService<IRadarrClient>());
        builder.Services.AddSingleton<MediaOpaqueIdProtector>();
        builder.Services.AddSingleton<MediaRequestStore>();
        builder.Services.AddTransient<IMediaAddOptionsService, MediaAddOptionsService>();
        builder.Services.AddSingleton<IMediaRequestService, MediaRequestService>();
        builder.Services.AddSingleton<IDockerInventoryProvider, UnavailableDockerInventoryProvider>();
        var eodhdOptions = new EodhdFinanceOptions();
        builder.Configuration.GetSection(EodhdFinanceOptions.Section).Bind(eodhdOptions);
        builder.Services.AddSingleton(eodhdOptions);
        var datasetOptions = builder.Configuration.GetSection(FinanceDatasetOptions.Section).Get<FinanceDatasetOptions>() ?? new();
        builder.Services.AddSingleton(datasetOptions);
        var dataProtectionOptions = builder.Configuration.GetSection(FinanceDataProtectionOptions.Section).Get<FinanceDataProtectionOptions>() ?? new();
        builder.Services.AddSingleton(dataProtectionOptions);
        var cadenceOptions=builder.Configuration.GetSection(FinanceCadenceOptions.Section).Get<FinanceCadenceOptions>()??new();
        builder.Services.AddSingleton(cadenceOptions);
        var researchSchedulerOptions=builder.Configuration.GetSection(FinanceResearchSchedulerOptions.Section).Get<FinanceResearchSchedulerOptions>()??new();
        researchSchedulerOptions.Validate();
        builder.Services.AddSingleton(researchSchedulerOptions);
        var riskOptions=builder.Configuration.GetSection(FinanceRiskOptions.Section).Get<FinanceRiskOptions>()??new();
        riskOptions.Validate();
        builder.Services.AddSingleton(riskOptions);
        var fredOptions=builder.Configuration.GetSection(FinanceFredOptions.Section).Get<FinanceFredOptions>()??new();
        fredOptions.Validate();
        builder.Services.AddSingleton(fredOptions);
        builder.Services.AddSingleton<FredApiClient>();
        builder.Services.AddSingleton(_ => new EodhdMarketMemory(eodhdOptions,riskOptions));
        builder.Services.AddSingleton(_ => new FinanceMacroMemory(eodhdOptions,fredOptions));
        builder.Services.AddSingleton<FinanceDatasetIntakeStore>();
        builder.Services.AddSingleton<FinanceDataProtectionStore>();
        builder.Services.AddHostedService<FinanceProspectiveCadenceWorker>();
        builder.Services.AddHostedService<FinanceFeatureBuildWorker>();
        builder.Services.AddHostedService<FinanceBacktestBuildWorker>();
        builder.Services.AddHostedService<FinanceRobustnessBuildWorker>();
        builder.Services.AddSingleton<FinanceResearchOrchestrator>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddHostedService<FinanceResearchSchedulerWorker>();
        builder.Services.AddSingleton<IFinanceObservationReader, EodhdFinanceObservationReader>();
        builder.Services.AddSingleton<IFinanceFeatureReader, EodhdFinanceFeatureReader>();
        builder.Services.AddSingleton<IFinanceBacktestReader, EodhdFinanceBacktestReader>();
        builder.Services.AddSingleton<IFinanceRobustnessReader, EodhdFinanceRobustnessReader>();
        builder.Services.AddSingleton<IFinanceDatasetReader, FinanceDatasetReader>();
        builder.Services.AddSingleton<IFinanceBackupReader, FinanceBackupReader>();
        builder.Services.AddSingleton<IModuleRegistry>(
            new InMemoryModuleRegistry([SystemModule.Definition, DockerModule.Definition, MediaModule.Definition, MealPlannerModule.Definition, ShoppingListModule.Definition, CalendarModule.Definition, FinanceModule.Definition]));

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
        var recoveryJson = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        recoveryJson.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        app.MapGet("/api/v1/system/recovery", (SystemRecoveryCoordinator recovery) => Results.Json(recovery.Snapshot(), recoveryJson));

        app.MapGet(
            "/api/v1/modules",
            async (
                IModuleRegistry registry,
                ISystemMetricsProvider systemProvider,
                IDockerInventoryProvider dockerProvider,
                MediaOptions mediaOptions,
                MealPlannerStore mealPlannerStore,
                ShoppingListStore shoppingListStore,
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
                        "meal-planner" => module with { Status = mealPlannerStore.IsAvailable ? "Available" : "Unavailable" },
                        "shopping-list" => module with { Status = shoppingListStore.IsAvailable ? "Available" : "Unavailable" },
                        _ => module
                    });
                return Results.Ok(modules);
            });

        app.MapGet(
            "/api/v1/system/overview",
            async (ISystemMetricsProvider provider, CancellationToken cancellationToken) =>
                Results.Ok(await provider.GetOverviewAsync(cancellationToken)));

        app.MapGet(
            "/api/v1/system/sentinel/ping",
            async (ISentinelClient sentinel, CancellationToken cancellationToken) =>
            {
                try
                {
                    return Results.Ok(await sentinel.PingAsync(cancellationToken));
                }
                catch (SentinelClientUnavailableException)
                {
                    return SentinelProblem(
                        "sentinelUnavailable",
                        "Sentinel communication is unavailable.",
                        StatusCodes.Status503ServiceUnavailable);
                }
            });

        app.MapPost(
            "/api/v1/system/sentinel/read-system-metrics",
            async (ISentinelClient sentinel, CancellationToken cancellationToken) =>
            {
                try
                {
                    return Results.Ok(await sentinel.ReadSystemMetricsAsync(cancellationToken));
                }
                catch (SentinelClientUnavailableException)
                {
                    return SentinelProblem(
                        "sentinelUnavailable",
                        "Sentinel communication is unavailable.",
                        StatusCodes.Status503ServiceUnavailable);
                }
            });

        app.MapGet(
            "/api/v1/docker/containers",
            async (IDockerInventoryProvider provider, CancellationToken cancellationToken) =>
                Results.Ok(await provider.GetContainersAsync(cancellationToken)));

        app.MapFinanceEndpoints();

        app.MapGet(
            "/api/v1/modules/media",
            async (IMediaService mediaService, CancellationToken cancellationToken) =>
                Results.Ok(await mediaService.GetOverviewAsync(cancellationToken)));

        app.MapGet(
            "/api/v1/modules/media/service-links",
            (MediaOptions options) => Results.Ok(MediaServiceLinks.From(options)));

        app.MapGet(
            "/api/v1/modules/media/posters/{token}",
            async (
                string token,
                HttpContext context,
                MediaPosterService posters,
                CancellationToken cancellationToken) =>
            {
                var poster = await posters.GetAsync(token, cancellationToken);
                if (poster is null) return Results.NotFound();
                context.Response.Headers.CacheControl = "public,max-age=3600";
                return Results.File(poster.Value.Bytes, poster.Value.ContentType);
            });

        app.MapGet(
            "/api/v1/modules/media/search",
            async (string? query, IMediaSearchService searchService, CancellationToken cancellationToken) =>
            {
                var normalizedQuery = query?.Trim() ?? string.Empty;
                if (normalizedQuery.Length < 2)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid media search query",
                        detail: "The query must contain at least two characters.");
                }

                return Results.Ok(await searchService.SearchAsync(normalizedQuery, cancellationToken));
            });

        app.MapGet(
            "/api/v1/modules/media/lookup",
            async (
                string? query,
                string? mediaType,
                IMediaLookupService lookupService,
                CancellationToken cancellationToken) =>
            {
                var normalizedQuery = query?.Trim() ?? string.Empty;
                if (normalizedQuery.Length < 2)
                    return ApiProblem("queryTooShort", "The query must contain at least two characters.", StatusCodes.Status400BadRequest);
                var normalizedMediaType = string.IsNullOrWhiteSpace(mediaType)
                    ? MediaLookupTypes.All
                    : mediaType.Trim().ToLowerInvariant();
                if (!MediaLookupTypes.IsValid(normalizedMediaType))
                    return ApiProblem("invalidMediaType", "The media type must be series, movie or all.", StatusCodes.Status400BadRequest);
                return Results.Ok(await lookupService.LookupAsync(normalizedQuery, normalizedMediaType, cancellationToken));
            });

        app.MapGet(
            "/api/v1/modules/media/jobs",
            async (
                string? status,
                string? mediaType,
                string? provider,
                bool? includeCompleted,
                int? limit,
                IMediaJobsService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var response = await service.GetJobsAsync(
                        new(status, mediaType, provider, includeCompleted ?? false, limit ?? 50),
                        cancellationToken);
                    return Results.Json(response, WebJsonOptions);
                }
                catch (MediaJobsException exception)
                {
                    return JobsProblem(exception);
                }
            });

        app.MapGet(
            "/api/v1/modules/media/jobs/events",
            async (HttpContext context, IMediaJobsService service, CancellationToken cancellationToken) =>
            {
                context.Response.ContentType = "text/event-stream";
                context.Response.Headers.CacheControl = "no-cache";
                context.Response.Headers.Append("X-Accel-Buffering", "no");
                try
                {
                    await context.Response.WriteAsync("retry: 5000\n\n", cancellationToken);
                    await context.Response.Body.FlushAsync(cancellationToken);
                    await foreach (var snapshot in service.StreamJobsAsync(cancellationToken))
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(
                            snapshot,
                            WebJsonOptions);
                        await context.Response.WriteAsync($"event: jobs\ndata: {json}\n\n", cancellationToken);
                        await context.Response.Body.FlushAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // The browser disconnected.
                }
            });

        app.MapGet(
            "/api/v1/modules/media/jobs/{id}",
            async (string id, IMediaJobsService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var job = await service.GetJobAsync(id, cancellationToken);
                    return job is null
                        ? ApiProblem("mediaJobNotFound", "The media job was not found.", StatusCodes.Status404NotFound)
                        : Results.Ok(job);
                }
                catch (MediaJobsException exception)
                {
                    return JobsProblem(exception);
                }
            });

        app.MapGet("/api/v1/modules/media/downloads",
            async (IDownloadControlService service, CancellationToken cancellationToken) =>
            {
                try { return Results.Ok(await service.GetAsync(cancellationToken)); }
                catch (DownloadControlException exception) { return DownloadProblem(exception); }
            });
        app.MapGet("/api/v1/modules/media/downloads/{id}",
            async (string id, IDownloadControlService service, CancellationToken cancellationToken) =>
            {
                try { return Results.Ok(await service.GetAsync(id, cancellationToken)); }
                catch (DownloadControlException exception) { return DownloadProblem(exception); }
            });
        app.MapPost("/api/v1/modules/media/downloads/{id}/remove-preview",
            async (string id, DownloadRemovalPreviewInput input, IDownloadControlService service, CancellationToken cancellationToken) =>
            {
                try { return Results.Ok(await service.PreviewAsync(id, input, cancellationToken)); }
                catch (DownloadControlException exception) { return DownloadProblem(exception); }
            });
        app.MapPost("/api/v1/modules/media/downloads/{id}/remove",
            async (string id, DownloadRemovalInput input, IDownloadControlService service, CancellationToken cancellationToken) =>
            {
                try { return Results.Ok(await service.RemoveAsync(id, input, cancellationToken)); }
                catch (DownloadControlException exception) { return DownloadProblem(exception); }
            });
        app.MapPost("/api/v1/modules/media/downloads/{id}/actions/{operation}",
            async (string id, string operation, IDownloadControlService service, CancellationToken cancellationToken) =>
            {
                try { return Results.Ok(await service.OperateAsync(id, operation, cancellationToken)); }
                catch (DownloadControlException exception) { return DownloadProblem(exception); }
            });
        app.MapPost("/api/v1/modules/media/downloads/actions/batch",
            async (DownloadBatchInput input, IDownloadControlService service, CancellationToken cancellationToken) =>
            {
                try { return Results.Ok(await service.BatchAsync(input, cancellationToken)); }
                catch (DownloadControlException exception) { return DownloadProblem(exception); }
            });

        app.MapGet(
            "/api/v1/modules/media/library-status",
            async (
                string? provider,
                string? foreignId,
                string? mediaType,
                IMediaJobsService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    return Results.Ok(await service.GetLibraryStatusAsync(
                        provider ?? string.Empty,
                        foreignId ?? string.Empty,
                        mediaType ?? string.Empty,
                        cancellationToken));
                }
                catch (MediaJobsException exception)
                {
                    return JobsProblem(exception);
                }
            });

        app.MapGet(
            "/api/v1/modules/media/play/{id}",
            async (string id, IMediaJobsService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var item = await service.GetPlayAsync(id, cancellationToken);
                    return item is null
                        ? ApiProblem("playItemNotFound", "The Jellyfin item was not found.", StatusCodes.Status404NotFound)
                        : Results.Ok(item);
                }
                catch (MediaJobsException exception)
                {
                    return JobsProblem(exception);
                }
            });

        app.MapGet(
            "/api/v1/modules/media/add-options/series",
            async (IMediaAddOptionsService service, CancellationToken cancellationToken) =>
            {
                try { return Results.Ok(await service.GetAsync(MediaLookupTypes.Series, cancellationToken)); }
                catch (MediaRequestException exception) { return RequestProblem(exception); }
            });

        app.MapGet(
            "/api/v1/modules/media/add-options/movie",
            async (IMediaAddOptionsService service, CancellationToken cancellationToken) =>
            {
                try { return Results.Ok(await service.GetAsync(MediaLookupTypes.Movie, cancellationToken)); }
                catch (MediaRequestException exception) { return RequestProblem(exception); }
            });

        app.MapPost(
            "/api/v1/modules/media/requests/preview",
            async (
                MediaRequestPreviewInput input,
                IMediaRequestService service,
                CancellationToken cancellationToken) =>
            {
                try { return Results.Ok(await service.PreviewAsync(input, cancellationToken)); }
                catch (MediaRequestException exception) { return RequestProblem(exception); }
            });

        app.MapPost(
            "/api/v1/modules/media/requests/confirm",
            async (
                MediaRequestConfirmInput input,
                IMediaRequestService service,
                CancellationToken cancellationToken) =>
            {
                try { return Results.Ok(await service.ConfirmAsync(input, cancellationToken)); }
                catch (MediaRequestException exception) { return RequestProblem(exception); }
            });

        app.MapGet("/api/v1/modules/media/smart-shuffle/options",
            async (ISmartShuffleService service, CancellationToken cancellationToken) =>
            {
                try { return Results.Ok(await service.GetOptionsAsync(cancellationToken)); }
                catch (SmartShuffleException exception) { return SmartShuffleProblem(exception); }
            });
        app.MapGet("/api/v1/modules/media/smart-shuffle/devices",
            async (ISmartShuffleService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetDevicesAsync(cancellationToken)));
        app.MapPost("/api/v1/modules/media/smart-shuffle/sessions",
            async (CreateSmartShuffleSession input, ISmartShuffleService service, CancellationToken cancellationToken) =>
            {
                try { return Results.Ok(await service.CreateAsync(input, cancellationToken)); }
                catch (SmartShuffleException exception) { return SmartShuffleProblem(exception); }
            });
        app.MapGet("/api/v1/modules/media/smart-shuffle/sessions/{id}",
            (string id, ISmartShuffleService service) => service.Get(id) is { } session
                ? Results.Ok(session)
                : ApiProblem("sessionNotFound", "Shuffle-sessionen hittades inte.", StatusCodes.Status404NotFound));
        app.MapPost("/api/v1/modules/media/smart-shuffle/sessions/{id}/skip",
            async (string id, ISmartShuffleService service, CancellationToken cancellationToken) =>
            {
                try { return Results.Ok(await service.SkipAsync(id, cancellationToken)); }
                catch (SmartShuffleException exception) { return SmartShuffleProblem(exception); }
            });
        app.MapPost("/api/v1/modules/media/smart-shuffle/sessions/{id}/stop",
            (string id, ISmartShuffleService service) =>
            {
                try { return Results.Ok(service.Stop(id)); }
                catch (SmartShuffleException exception) { return SmartShuffleProblem(exception); }
            });

        app.MapMealPlannerEndpoints();
        app.MapShoppingListEndpoints();
        app.MapCalendarEndpoints();
        app.MapSettingsEndpoints();
        _ = app.Services.GetRequiredService<SettingsStore>();

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true
        });

        app.Run();
    }

    private static void AddMediaClient<TClient, TImplementation>(
        IServiceCollection services,
        string provider,
        Func<MediaOptions, string> getBaseUrl,
        Func<MediaOptions, int>? getTimeoutSeconds = null)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddHttpClient<TClient, TImplementation>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<MediaOptions>();
            ConfigureMediaClient(httpClient, getBaseUrl(options), getTimeoutSeconds?.Invoke(options) ?? options.TimeoutSeconds);
        }).AddHttpMessageHandler(serviceProvider =>
            new ProviderHttpLoggingHandler(
                provider,
                serviceProvider.GetRequiredService<ILogger<ProviderHttpLoggingHandler>>()));
    }

    private static void ConfigureMediaClient(HttpClient httpClient, string baseUrl, int timeoutSeconds)
    {
        httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    private static IResult RequestProblem(MediaRequestException exception) =>
        ApiProblem(exception.Code, exception.SafeMessage, exception.StatusCode);

    private static IResult JobsProblem(MediaJobsException exception) =>
        ApiProblem(exception.Code, exception.SafeMessage, exception.StatusCode);

    private static IResult SmartShuffleProblem(SmartShuffleException exception) =>
        ApiProblem(exception.Code, exception.SafeMessage, exception.StatusCode);

    private static IResult DownloadProblem(DownloadControlException exception) =>
        ApiProblem(exception.Code, exception.SafeMessage, exception.StatusCode);

    private static IResult SentinelProblem(string code, string detail, int statusCode) =>
        Results.Problem(
            statusCode: statusCode,
            title: "Sentinel request could not be completed",
            detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });

    private static IResult ApiProblem(string code, string detail, int statusCode) =>
        Results.Problem(
            statusCode: statusCode,
            title: "Media request could not be completed",
            detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });
}
