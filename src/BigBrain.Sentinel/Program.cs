using System.Security.Cryptography.X509Certificates;
using BigBrain.Sentinel.Contracts;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
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
        builder.Services
            .AddOptions<SentinelProtocolOptions>()
            .Bind(builder.Configuration.GetSection(SentinelProtocolOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<SentinelProtocolOptions>, SentinelProtocolOptionsValidator>();

        builder.Services.AddHealthChecks();
        builder.Services.AddSingleton<ISentinelVersionProvider, AssemblySentinelVersionProvider>();

        var protocolOptions = builder.Configuration
            .GetSection(SentinelProtocolOptions.SectionName)
            .Get<SentinelProtocolOptions>() ?? new SentinelProtocolOptions();

        if (protocolOptions.Enabled)
        {
            ConfigureProtocolTransport(builder, protocolOptions);
            builder.Services.AddSingleton<ICapabilityRegistry, SystemMetricsCapabilityRegistry>();
            builder.Services.AddSingleton<ISentinelRequestAuthorizer, SentinelRequestAuthorizer>();
        }
        else
        {
            builder.Services.AddSingleton<ICapabilityRegistry, EmptyCapabilityRegistry>();
        }

        return builder;
    }

    public static WebApplication Build(WebApplicationBuilder builder)
    {
        var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentinelOptions>>().Value;
        var protocolOptions = app.Services.GetRequiredService<IOptions<SentinelProtocolOptions>>().Value;
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

        if (protocolOptions.Enabled)
        {
            MapProtocolEndpoints(app, protocolOptions);
        }

        return app;
    }

    private static void ConfigureProtocolTransport(
        WebApplicationBuilder builder,
        SentinelProtocolOptions options)
    {
        var serverCertificate = SentinelCertificateLoader.LoadPkcs12(options.ServerCertificatePath);
        var trustedClientCertificate =
            SentinelCertificateLoader.LoadPkcs12(options.TrustedClientCertificatePath);

        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.ListenUnixSocket(options.SocketPath, listen =>
            {
                listen.Protocols = HttpProtocols.Http2;
                listen.UseHttps(https =>
                {
                    https.ServerCertificate = serverCertificate;
                    https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    https.ClientCertificateValidation = (certificate, _, _) =>
                        SentinelCertificateLoader.Matches(certificate, trustedClientCertificate);
                });
            });
        });
    }

    private static void MapProtocolEndpoints(WebApplication app, SentinelProtocolOptions options)
    {
        app.MapGet(
            SentinelProtocol.PingPath,
            (
                ICapabilityRegistry capabilities,
                ISentinelVersionProvider versionProvider) =>
                Results.Ok(
                    new SentinelPingResponse(
                        "Healthy",
                        options.NodeId,
                        versionProvider.GetVersion().Version,
                        capabilities.Count,
                        DateTimeOffset.UtcNow)));

        app.MapGet(
            SentinelProtocol.CapabilityRegistryPath,
            (ICapabilityRegistry capabilities) =>
                Results.Ok(
                    new SentinelCapabilityRegistryResponse(
                        options.NodeId,
                        capabilities.GetCapabilities())));

        app.MapPost(
            SentinelProtocol.ReadSnapshotPath,
            (
                SentinelCapabilityRequest request,
                ICapabilityRegistry capabilities,
                ISentinelRequestAuthorizer authorizer,
                ILogger<Program> logger) =>
            {
                var validationError = SentinelSnapshotRequestValidator.Validate(request, capabilities, options);
                if (validationError is not null)
                {
                    return Results.Json(validationError, statusCode: StatusCodes.Status400BadRequest);
                }

                var authorizationError = authorizer.Authorize(request);
                if (authorizationError is not null)
                {
                    var statusCode = authorizationError.Code == "REPLAY_DETECTED"
                        ? StatusCodes.Status409Conflict
                        : StatusCodes.Status403Forbidden;
                    return Results.Json(authorizationError, statusCode: statusCode);
                }

                SentinelLog.CapabilityCompleted(
                    logger,
                    request.Capability,
                    request.Version,
                    "NotImplemented");

                return Results.Json(
                    new SentinelProtocolError(
                        SentinelProtocol.CapabilityUnavailable,
                        "System Metrics collection is not implemented.",
                        false),
                    statusCode: StatusCodes.Status501NotImplemented);
            });
    }
}
