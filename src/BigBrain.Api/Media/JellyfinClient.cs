using System.Diagnostics;

namespace BigBrain.Api.Media;

public sealed class JellyfinClient(HttpClient httpClient, MediaOptions options)
    : MediaClientBase(httpClient, "Jellyfin"), IJellyfinClient
{
    public async Task<JellyfinOverview> GetOverviewAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Jellyfin.ApiKey))
        {
            return Empty(NotConfigured());
        }

        var timer = Stopwatch.StartNew();
        try
        {
            using var status = await GetJellyfinJsonAsync("System/Info", options.Jellyfin.ApiKey, cancellationToken);
            using var libraries = await GetJellyfinJsonAsync("Library/VirtualFolders", options.Jellyfin.ApiKey, cancellationToken);
            using var counts = await GetJellyfinJsonAsync("Items/Counts", options.Jellyfin.ApiKey, cancellationToken);
            using var sessions = await GetJellyfinJsonAsync("Sessions", options.Jellyfin.ApiKey, cancellationToken);

            var activeSessions = sessions.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array
                ? sessions.RootElement.EnumerateArray().Count(item =>
                    item.TryGetProperty("NowPlayingItem", out var playing)
                    && playing.ValueKind == System.Text.Json.JsonValueKind.Object)
                : 0;
            return new JellyfinOverview(
                Online(GetString(status.RootElement, "Version"), timer),
                ArrayLength(libraries.RootElement),
                GetInt32(counts.RootElement, "MovieCount"),
                GetInt32(counts.RootElement, "SeriesCount"),
                activeSessions);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Empty(Failure(exception, timer));
        }
    }

    private async Task<System.Text.Json.JsonDocument> GetJellyfinJsonAsync(
        string path,
        string apiKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("X-Emby-Token", apiKey);
        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        EnsureSuccess(response);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await System.Text.Json.JsonDocument.ParseAsync(
            stream,
            new System.Text.Json.JsonDocumentOptions { MaxDepth = 32 },
            cancellationToken);
    }

    private static JellyfinOverview Empty(MediaServiceStatus status) => new(status, 0, 0, 0, 0);
}
