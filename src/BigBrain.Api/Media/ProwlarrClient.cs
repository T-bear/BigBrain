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
            using var indexerStatus = await GetJsonAsync("api/v1/indexerstatus", options.Prowlarr.ApiKey, cancellationToken);
            using var history = await GetJsonAsync(
                "api/v1/history?page=1&pageSize=10&sortKey=date&sortDirection=descending",
                options.Prowlarr.ApiKey,
                cancellationToken);

            var indexerItems = indexers.RootElement.EnumerateArray().Take(25).ToArray();
            var unavailableIds = indexerStatus.RootElement.ValueKind == JsonValueKind.Array
                ? indexerStatus.RootElement.EnumerateArray()
                    .Where(item => DateTimeOffset.TryParse(GetString(item, "disabledTill"), out var disabledTill)
                        && disabledTill > DateTimeOffset.UtcNow)
                    .Select(item => GetInt32(item, "indexerId"))
                    .ToHashSet()
                : [];
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
                indexerItems.Count(item => Boolean(item, "enable") && !unavailableIds.Contains(GetInt32(item, "id"))),
                indexerItems.Count(item => Boolean(item, "enableRss")),
                indexerItems.Select(item =>
                {
                    var name = GetString(item, "name") ?? "Unnamed indexer";
                    var state = !Boolean(item, "enable") ? "disabled"
                        : unavailableIds.Contains(GetInt32(item, "id")) ? "offline" : "online";
                    return $"{name}: {state}";
                }).ToArray(),
                appNames,
                MapFailures(Records(history.RootElement)),
                HealthWarnings(health.RootElement, "Prowlarr"));
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Empty(Failure(exception, timer));
        }
    }

    private static MediaHistoryItem[] MapFailures(JsonElement records) =>
        records.ValueKind != JsonValueKind.Array ? [] : records.EnumerateArray()
            .Where(item => (GetString(item, "eventType") ?? "").Contains("fail", StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .Select(item => new MediaHistoryItem(
                GetString(item, "sourceTitle") ?? GetString(item, "message") ?? "Indexer failure",
                GetString(item, "eventType") ?? "failure",
                DateTimeOffset.TryParse(GetString(item, "date"), out var date) ? date : null))
            .ToArray();

    private static ProwlarrOverview Empty(MediaServiceStatus status) => new(status, 0, 0, 0, 0, [], [], [], []);
}
