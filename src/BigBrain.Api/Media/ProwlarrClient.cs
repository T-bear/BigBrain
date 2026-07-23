using System.Diagnostics;
using System.Text.Json;

namespace BigBrain.Api.Media;

public sealed class ProwlarrClient(HttpClient httpClient, MediaOptions options)
    : MediaClientBase(httpClient, "Prowlarr"), IProwlarrClient
{
    public async Task<ProwlarrOverview> GetOverviewAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Prowlarr.ApiKey))
        {
            return Empty(NotConfigured());
        }

        var timer = Stopwatch.StartNew();
        try
        {
            using var status = await GetJsonAsync("api/v1/system/status", options.Prowlarr.ApiKey, cancellationToken);
            using var indexers = await GetJsonAsync("api/v1/indexer", options.Prowlarr.ApiKey, cancellationToken);
            using var health = await GetJsonAsync("api/v1/health", options.Prowlarr.ApiKey, cancellationToken);
            using var applications = await GetJsonAsync("api/v1/applications", options.Prowlarr.ApiKey, cancellationToken);

            var indexerItems = indexers.RootElement.EnumerateArray().Take(25).ToArray();
            var appNames = applications.RootElement.ValueKind == JsonValueKind.Array
                ? applications.RootElement.EnumerateArray()
                    .Select(item => GetString(item, "name"))
                    .Where(name => name is not null
                        && (name.Contains("Sonarr", StringComparison.OrdinalIgnoreCase)
                            || name.Contains("Radarr", StringComparison.OrdinalIgnoreCase)))
                    .Select(name => name!.Contains("Sonarr", StringComparison.OrdinalIgnoreCase) ? "Sonarr" : "Radarr")
                    .Distinct(StringComparer.Ordinal)
                    .Take(10)
                    .ToArray()
                : [];
            return new ProwlarrOverview(
                Online(GetString(status.RootElement, "version"), timer),
                indexerItems.Length,
                indexerItems.Count(item => Boolean(item, "enable")),
                indexerItems.Select((item, index) => $"Indexer {index + 1}: {(Boolean(item, "enable") ? "enabled" : "disabled")}").ToArray(),
                appNames,
                HealthWarnings(health.RootElement, "Prowlarr"));
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Empty(Failure(exception, timer));
        }
    }

    private static ProwlarrOverview Empty(MediaServiceStatus status) => new(status, 0, 0, [], [], []);
}
