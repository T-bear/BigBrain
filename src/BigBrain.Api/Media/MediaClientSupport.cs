using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace BigBrain.Api.Media;

public sealed class ProviderHttpLoggingHandler(
    string provider,
    ILogger<ProviderHttpLoggingHandler> logger) : DelegatingHandler
{
    private static readonly Action<ILogger, string, string, string, string, long, string, Exception?> ProviderRequestCompleted =
        LoggerMessage.Define<string, string, string, string, long, string>(
            LogLevel.Information,
            new EventId(2301, nameof(ProviderRequestCompleted)),
            "Provider request completed: provider={Provider} operation={Operation} status={Status} errorCategory={ErrorCategory} duration={Duration} correlationId={CorrelationId}");

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            Log(
                request,
                ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture),
                Category(response.StatusCode),
                timer);
            return response;
        }
        catch (Exception exception)
        {
            Log(
                request,
                "failed",
                exception is TaskCanceledException ? "timeout" : "transport",
                timer);
            throw;
        }
    }

    private void Log(HttpRequestMessage request, string status, string errorCategory, Stopwatch timer) =>
        ProviderRequestCompleted(
            logger,
            provider,
            request.Method.Method,
            status,
            errorCategory,
            timer.ElapsedMilliseconds,
            Activity.Current?.TraceId.ToString() ?? "none",
            null);

    private static string Category(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "configuration",
        HttpStatusCode.Conflict => "duplicate",
        >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError => "validation",
        >= HttpStatusCode.InternalServerError => "upstream",
        _ => "none"
    };
}

public abstract class MediaClientBase(HttpClient httpClient, string serviceName)
{
    protected HttpClient HttpClient { get; } = httpClient;
    protected string ServiceName { get; } = serviceName;
    public string ProviderName => ServiceName;

    protected async Task<JsonDocument> GetJsonAsync(
        string requestUri,
        string? apiKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
        }

        using var response = await HttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        EnsureSuccess(response);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(
            stream,
            new JsonDocumentOptions { MaxDepth = 32 },
            cancellationToken);
    }

    protected static void EnsureSuccess(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new MediaAuthenticationException();
        }

        response.EnsureSuccessStatusCode();
    }

    protected MediaServiceStatus Online(string? version, Stopwatch stopwatch) =>
        new(ServiceName, MediaStatuses.Online, version, stopwatch.ElapsedMilliseconds, DateTimeOffset.UtcNow, null, true);

    protected MediaServiceStatus NotConfigured() =>
        new(ServiceName, MediaStatuses.NotConfigured, null, null, DateTimeOffset.UtcNow, "Service credentials are not configured.", false);

    protected MediaServiceStatus Failure(Exception exception, Stopwatch stopwatch)
    {
        var (status, message) = exception switch
        {
            MediaAuthenticationException => (MediaStatuses.Degraded, "Authentication was rejected by the service."),
            JsonException => (MediaStatuses.Degraded, "The service returned an invalid response."),
            TaskCanceledException => (MediaStatuses.Unavailable, "The service timed out."),
            HttpRequestException => (MediaStatuses.Unavailable, "The service could not be reached."),
            _ => (MediaStatuses.Degraded, "The service check failed.")
        };
        return new MediaServiceStatus(
            ServiceName,
            status,
            null,
            stopwatch.ElapsedMilliseconds,
            DateTimeOffset.UtcNow,
            message,
            true);
    }

    protected MediaSearchProviderResult SearchFailure(Exception exception)
    {
        var status = exception switch
        {
            TaskCanceledException => MediaStatuses.Unavailable,
            HttpRequestException => MediaStatuses.Unavailable,
            _ => MediaStatuses.Degraded
        };
        var message = exception switch
        {
            MediaAuthenticationException => "Authentication was rejected by the provider.",
            JsonException => "The provider returned an invalid response.",
            TaskCanceledException => "The provider search timed out.",
            HttpRequestException => "The provider could not be reached.",
            _ => "The provider search failed."
        };
        return new MediaSearchProviderResult(ServiceName, status, message, []);
    }

    protected static int ArrayLength(JsonElement root) =>
        root.ValueKind == JsonValueKind.Array ? root.GetArrayLength() : 0;

    protected static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    protected static int GetInt32(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetInt32(out var result) ? result : 0;

    protected static long GetInt64(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetInt64(out var result) ? result : 0;

    protected static double GetDouble(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetDouble(out var result) ? result : 0;

    protected static bool Boolean(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;

    protected static JsonElement Records(JsonElement root) =>
        root.TryGetProperty("records", out var records) && records.ValueKind == JsonValueKind.Array
            ? records
            : default;

    protected static IReadOnlyList<MediaHealthWarning> HealthWarnings(JsonElement root, string source) =>
        root.ValueKind != JsonValueKind.Array
            ? []
            : root.EnumerateArray()
                .Take(25)
                .Select(item => new MediaHealthWarning(source, GetString(item, "message") ?? "Health warning reported."))
                .ToArray();

    protected static double ClampPercent(double value) => Math.Clamp(value, 0, 100);
}

public sealed class MediaAuthenticationException : Exception;
