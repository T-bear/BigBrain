namespace BigBrain.Api.Media;

public static class MediaSearchStatuses
{
    public const string Complete = "complete";
    public const string Partial = "partial";
    public const string Unavailable = "unavailable";
}

public static class MediaTypes
{
    public const string Movie = "movie";
    public const string Series = "series";
    public const string Season = "season";
    public const string Episode = "episode";
    public const string Unknown = "unknown";
}

public static class MediaSearchStates
{
    public const string Available = "available";
    public const string Monitored = "monitored";
    public const string Unmonitored = "unmonitored";
    public const string Missing = "missing";
    public const string Unknown = "unknown";
}

public sealed record MediaSearchMetadata(
    int? SeasonCount = null,
    int? EpisodeCount = null,
    int? EpisodeFileCount = null,
    bool? HasFile = null,
    bool? AvailableInLibrary = null,
    bool? ImageAvailable = null);

public sealed record MediaSearchResult(
    string SourceId,
    string Title,
    int? Year,
    string MediaType,
    string State,
    string? PosterUrl,
    MediaSearchMetadata Metadata);

public sealed record MediaSearchProviderResult(
    string Provider,
    string Status,
    string? Error,
    IReadOnlyList<MediaSearchResult> Results);

public sealed record MediaSearchResponse(
    string Query,
    DateTimeOffset SearchedAtUtc,
    string Status,
    IReadOnlyList<MediaSearchProviderResult> Providers);

public interface IMediaSearchProvider
{
    string ProviderName { get; }
    Task<MediaSearchProviderResult> SearchAsync(string query, int limit, CancellationToken cancellationToken);
}

public interface IMediaSearchService
{
    Task<MediaSearchResponse> SearchAsync(string query, CancellationToken cancellationToken);
}

public sealed class MediaSearchService(IEnumerable<IMediaSearchProvider> providers) : IMediaSearchService
{
    internal const int ResultsPerProvider = 10;

    public async Task<MediaSearchResponse> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Trim();
        var tasks = providers.Select(provider => SearchProviderAsync(provider, normalizedQuery, cancellationToken));
        var results = await Task.WhenAll(tasks);
        var successfulCount = results.Count(result => result.Status == MediaStatuses.Online);
        var status = successfulCount switch
        {
            0 => MediaSearchStatuses.Unavailable,
            _ when successfulCount == results.Length => MediaSearchStatuses.Complete,
            _ => MediaSearchStatuses.Partial
        };

        return new MediaSearchResponse(normalizedQuery, DateTimeOffset.UtcNow, status, results);
    }

    private static async Task<MediaSearchProviderResult> SearchProviderAsync(
        IMediaSearchProvider provider,
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await provider.SearchAsync(query, ResultsPerProvider, cancellationToken);
            return result with { Results = result.Results.Take(ResultsPerProvider).ToArray() };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return new MediaSearchProviderResult(
                provider.ProviderName,
                MediaStatuses.Degraded,
                "The provider search failed.",
                []);
        }
    }
}
