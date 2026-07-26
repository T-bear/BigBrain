using System.Diagnostics;

namespace BigBrain.Api.Media;

public static class MediaLookupTypes
{
    public const string All = "all";
    public const string Series = "series";
    public const string Movie = "movie";

    public static bool IsValid(string value) => value is All or Series or Movie;
}

public static class MediaLookupStates
{
    public const string External = "external";
    public const string AlreadyRegistered = "alreadyRegistered";
    public const string Unavailable = "unavailable";
    public const string Unknown = "unknown";
}

public static class MediaProviderErrorCodes
{
    public const string Timeout = "timeout";
    public const string AuthenticationFailure = "authenticationFailure";
    public const string ProviderUnavailable = "providerUnavailable";
    public const string ValidationError = "validationError";
    public const string RootFolderUnavailable = "rootFolderUnavailable";
    public const string AlreadyExists = "alreadyExists";
    public const string UnknownError = "unknownError";
}

public sealed record MediaLookupResult(
    string Provider,
    string ForeignId,
    string Title,
    string? OriginalTitle,
    int? Year,
    string? Overview,
    string? Network,
    int? RuntimeMinutes,
    string? Status,
    string MediaType,
    string LookupState,
    bool ImageAvailable,
    bool AlreadyRegistered,
    string? ExistingSourceId,
    string ProviderId = "",
    string? PosterUrl = null,
    bool? Monitored = null,
    bool CanRequest = false,
    string? RequestState = null,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public bool AlreadyExists => AlreadyRegistered;
}

public sealed record MediaLookupProviderResult(
    string Provider,
    string Status,
    string? Error,
    IReadOnlyList<MediaLookupResult> Results,
    string? ErrorCode = null);

public sealed record MediaLookupResponse(
    string Query,
    string MediaType,
    DateTimeOffset LookedUpAtUtc,
    string Status,
    bool RequestsEnabled,
    IReadOnlyList<MediaLookupProviderResult> Providers);

public interface IMediaLookupProvider
{
    string ProviderName { get; }
    string SupportedMediaType { get; }
    Task<MediaLookupProviderResult> LookupAsync(string query, int limit, CancellationToken cancellationToken);
}

public interface IMediaLookupService
{
    Task<MediaLookupResponse> LookupAsync(string query, string mediaType, CancellationToken cancellationToken);
}

public sealed class MediaLookupService(
    IEnumerable<IMediaLookupProvider> providers,
    MediaOptions options,
    ILogger<MediaLookupService> logger) : IMediaLookupService
{
    private static readonly Action<ILogger, string, int, Exception?> LookupStarted =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(2401, "MediaLookupStarted"),
            "Media lookup started for media type {MediaType} with {ProviderCount} provider(s)");
    private static readonly Action<ILogger, string, string, long, Exception?> LookupCompleted =
        LoggerMessage.Define<string, string, long>(
            LogLevel.Information,
            new EventId(2402, "MediaLookupCompleted"),
            "Media lookup completed for media type {MediaType} with status {Status} in {ElapsedMilliseconds} ms");

    internal const int ResultsPerProvider = 10;

    public async Task<MediaLookupResponse> LookupAsync(
        string query,
        string mediaType,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var selected = providers
            .Where(provider => mediaType == MediaLookupTypes.All || provider.SupportedMediaType == mediaType)
            .ToArray();
        LookupStarted(logger, mediaType, selected.Length, null);

        var results = await Task.WhenAll(selected.Select(provider =>
            LookupProviderAsync(provider, query, cancellationToken)));
        var successful = results.Count(result => result.Status == MediaStatuses.Online);
        var status = successful switch
        {
            0 => MediaSearchStatuses.Unavailable,
            _ when successful == results.Length => MediaSearchStatuses.Complete,
            _ => MediaSearchStatuses.Partial
        };
        LookupCompleted(logger, mediaType, status, timer.ElapsedMilliseconds, null);
        return new(query, mediaType, DateTimeOffset.UtcNow, status, options.Requests.Enabled, results);
    }

    private static async Task<MediaLookupProviderResult> LookupProviderAsync(
        IMediaLookupProvider provider,
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await provider.LookupAsync(query, ResultsPerProvider, cancellationToken);
            return result with { Results = result.Results.Take(ResultsPerProvider).ToArray() };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var (code, message, status) = MediaProviderFailures.Map(exception);
            if (code == MediaProviderErrorCodes.UnknownError)
                message = "The provider lookup failed.";
            return new(provider.ProviderName, status, message, [], code);
        }
    }
}
