using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BigBrain.Api.Media;

public static class MediaRequestStatuses
{
    public const string PreviewReady = "previewReady";
    public const string Created = "created";
    public const string AlreadyExists = "alreadyExists";
    public const string Rejected = "rejected";
    public const string ProviderUnavailable = "providerUnavailable";
    public const string Expired = "expired";
}

public static class MediaRequestErrors
{
    public const string LookupNotFound = "lookupNotFound";
    public const string AlreadyRegistered = "alreadyRegistered";
    public const string InvalidRootFolder = "invalidRootFolder";
    public const string InvalidQualityProfile = "invalidQualityProfile";
    public const string InvalidMonitoringOption = "invalidMonitoringOption";
    public const string RequestExpired = "requestExpired";
    public const string DuplicateRequest = "duplicateRequest";
    public const string ProviderUnavailable = "providerUnavailable";
    public const string ProviderConfigurationInvalid = "providerConfigurationInvalid";
    public const string ProviderRejectedRequest = "providerRejectedRequest";
    public const string RequestsDisabled = "requestsDisabled";
}

public sealed record MediaAddOption(string Id, string DisplayName, long? FreeSpaceBytes = null);

public sealed record MediaAddOptionsResponse(
    string Provider,
    string MediaType,
    bool RequestsEnabled,
    IReadOnlyList<MediaAddOption> RootFolders,
    IReadOnlyList<MediaAddOption> QualityProfiles,
    IReadOnlyList<MediaAddOption> MonitoringOptions,
    IReadOnlyList<MediaAddOption> SeriesTypes,
    string? DefaultRootFolderId,
    string? DefaultQualityProfileId,
    string DefaultMonitoringOptionId,
    string? DefaultSeriesTypeId,
    bool DefaultSearchAfterAdd);

public sealed record MediaRequestPreviewInput(
    string Provider,
    string MediaType,
    string ForeignId,
    string RootFolderId,
    string QualityProfileId,
    string Monitor,
    string? SeriesType,
    bool? SearchAfterAdd);

public sealed record MediaRequestSummary(
    string Title,
    int? Year,
    string Provider,
    string MediaType,
    string RootFolder,
    string QualityProfile,
    string Monitoring,
    string? SeriesType,
    bool SearchAfterAdd);

public sealed record MediaRequestPreviewResponse(
    string RequestToken,
    DateTimeOffset ExpiresAtUtc,
    string Status,
    MediaRequestSummary Summary);

public sealed record MediaRequestConfirmInput(string RequestToken, string IdempotencyKey);

public sealed record MediaRequestConfirmResponse(
    string Status,
    string Provider,
    string MediaType,
    string SourceId,
    string Title);

internal sealed record ProviderOption(int Id, string Value, string DisplayName, long? FreeSpaceBytes = null);
internal sealed record ProviderAddOptions(
    string Provider,
    string MediaType,
    IReadOnlyList<ProviderOption> RootFolders,
    IReadOnlyList<ProviderOption> QualityProfiles,
    IReadOnlyList<string> MonitoringOptions,
    IReadOnlyList<string> SeriesTypes);
internal sealed record ProviderAddCommand(
    string ForeignId,
    string Title,
    int? Year,
    int QualityProfileId,
    string RootFolderValue,
    string Monitor,
    string? SeriesType,
    bool SearchAfterAdd);
internal sealed record ProviderAddResult(string SourceId, string Title);

internal interface IMediaRequestProvider
{
    string ProviderName { get; }
    string SupportedMediaType { get; }
    Task<ProviderAddOptions> GetAddOptionsAsync(CancellationToken cancellationToken);
    Task<MediaLookupResult?> GetLookupItemAsync(string foreignId, CancellationToken cancellationToken);
    Task<ProviderAddResult?> GetRegisteredAsync(string foreignId, string title, int? year, CancellationToken cancellationToken);
}

internal interface IMediaAddProvider
{
    string ProviderName { get; }
    Task<ProviderAddResult> AddAsync(ProviderAddCommand command, CancellationToken cancellationToken);
}

public interface IMediaAddOptionsService
{
    Task<MediaAddOptionsResponse> GetAsync(string mediaType, CancellationToken cancellationToken);
}

public interface IMediaRequestService
{
    Task<MediaRequestPreviewResponse> PreviewAsync(
        MediaRequestPreviewInput input,
        CancellationToken cancellationToken);
    Task<MediaRequestConfirmResponse> ConfirmAsync(
        MediaRequestConfirmInput input,
        CancellationToken cancellationToken);
}

public sealed class MediaRequestException(string code, string safeMessage, int statusCode) : Exception
{
    public string Code { get; } = code;
    public string SafeMessage { get; } = safeMessage;
    public int StatusCode { get; } = statusCode;
}

internal sealed class MediaOpaqueIdProtector
{
    private readonly byte[] key = RandomNumberGenerator.GetBytes(32);

    public string Protect(string provider, string kind, int value)
    {
        var input = Encoding.UTF8.GetBytes($"{provider}:{kind}:{value.ToString(CultureInfo.InvariantCulture)}");
        return Convert.ToHexString(HMACSHA256.HashData(key, input)).ToLowerInvariant();
    }
}

internal sealed class MediaAddOptionsService(
    IEnumerable<IMediaRequestProvider> providers,
    MediaOpaqueIdProtector protector,
    MediaOptions options) : IMediaAddOptionsService
{
    public async Task<MediaAddOptionsResponse> GetAsync(string mediaType, CancellationToken cancellationToken)
    {
        if (!options.Requests.Enabled)
        {
            throw Disabled();
        }

        var provider = providers.SingleOrDefault(item => item.SupportedMediaType == mediaType)
            ?? throw Invalid(MediaRequestErrors.LookupNotFound, "The media request provider was not found.");
        ProviderAddOptions available;
        try
        {
            available = await provider.GetAddOptionsAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (MediaRequestException)
        {
            throw;
        }
        catch
        {
            throw new MediaRequestException(
                MediaRequestErrors.ProviderUnavailable,
                "The media provider is unavailable.",
                StatusCodes.Status503ServiceUnavailable);
        }
        var roots = available.RootFolders.Select(item => new MediaAddOption(
            protector.Protect(provider.ProviderName, "root", item.Id),
            item.DisplayName,
            item.FreeSpaceBytes)).ToArray();
        var qualities = available.QualityProfiles.Select(item => new MediaAddOption(
            protector.Protect(provider.ProviderName, "quality", item.Id),
            item.DisplayName)).ToArray();
        var monitoring = available.MonitoringOptions.Select(item =>
            new MediaAddOption(item, Friendly(item))).ToArray();
        var seriesTypes = available.SeriesTypes.Select(item =>
            new MediaAddOption(item, Friendly(item))).ToArray();
        return new(
            provider.ProviderName,
            mediaType,
            true,
            roots,
            qualities,
            monitoring,
            seriesTypes,
            roots.FirstOrDefault()?.Id,
            qualities.FirstOrDefault()?.Id,
            monitoring.FirstOrDefault()?.Id ?? "none",
            seriesTypes.FirstOrDefault()?.Id,
            options.Requests.DefaultSearchAfterAdd);
    }

    internal static string Friendly(string value) =>
        string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? $" {char.ToLowerInvariant(character)}" : character.ToString()));

    internal static MediaRequestException Disabled() =>
        new(MediaRequestErrors.RequestsDisabled, "Media requests are disabled.", StatusCodes.Status403Forbidden);
    internal static MediaRequestException Invalid(string code, string message) =>
        new(code, message, StatusCodes.Status400BadRequest);
}

internal sealed record PendingMediaRequest(
    string TokenHash,
    DateTimeOffset ExpiresAtUtc,
    string Provider,
    string MediaType,
    string ForeignId,
    string RootFolderId,
    string QualityProfileId,
    string Monitor,
    string? SeriesType,
    bool SearchAfterAdd,
    MediaRequestSummary Summary,
    bool InProgress = false,
    bool AddAttempted = false,
    string? IdempotencyKey = null,
    MediaRequestConfirmResponse? Result = null);

internal sealed class MediaRequestStore
{
    private readonly object gate = new();
    private readonly Dictionary<string, PendingMediaRequest> requests = [];
    private readonly Dictionary<string, MediaRequestConfirmResponse> completedByIdempotency = [];

    public void Add(PendingMediaRequest request)
    {
        lock (gate) requests[request.TokenHash] = request;
    }

    public (PendingMediaRequest? Request, MediaRequestConfirmResponse? Existing) Acquire(
        string tokenHash,
        string idempotencyKey,
        DateTimeOffset now)
    {
        lock (gate)
        {
            if (completedByIdempotency.TryGetValue(idempotencyKey, out var completed))
                return (null, completed);
            if (!requests.TryGetValue(tokenHash, out var request) || request.ExpiresAtUtc <= now)
                throw new MediaRequestException(
                    MediaRequestErrors.RequestExpired,
                    "The media request preview has expired.",
                    StatusCodes.Status410Gone);
            if (request.Result is not null)
                return (null, request.Result);
            if (request.InProgress)
                throw new MediaRequestException(
                    MediaRequestErrors.DuplicateRequest,
                    "This media request is already being processed.",
                    StatusCodes.Status409Conflict);
            var acquired = request with { InProgress = true, IdempotencyKey = idempotencyKey };
            requests[tokenHash] = acquired;
            return (acquired, null);
        }
    }

    public void Complete(string tokenHash, string idempotencyKey, MediaRequestConfirmResponse response)
    {
        lock (gate)
        {
            if (requests.TryGetValue(tokenHash, out var request))
                requests[tokenHash] = request with { InProgress = false, Result = response };
            completedByIdempotency[idempotencyKey] = response;
        }
    }

    public void MarkAddAttempted(string tokenHash)
    {
        lock (gate)
        {
            if (requests.TryGetValue(tokenHash, out var request))
                requests[tokenHash] = request with { AddAttempted = true };
        }
    }

    public void Release(string tokenHash)
    {
        lock (gate)
        {
            if (requests.TryGetValue(tokenHash, out var request) && request.Result is null)
                requests[tokenHash] = request with { InProgress = false };
        }
    }
}

internal sealed class MediaRequestService(
    IEnumerable<IMediaRequestProvider> requestProviders,
    IEnumerable<IMediaAddProvider> addProviders,
    MediaOpaqueIdProtector protector,
    MediaRequestStore store,
    MediaOptions options,
    ILogger<MediaRequestService> logger) : IMediaRequestService, IDisposable
{
    private static readonly Action<ILogger, string, string, string, Exception?> PreviewCreated =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(2410, "MediaRequestPreviewCreated"),
            "Media request preview created for provider {Provider}, media type {MediaType}, foreign id {ForeignId}");
    private static readonly Action<ILogger, string, string, string, Exception?> AlreadyExisted =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(2411, "MediaRequestAlreadyExisted"),
            "Media request already existed for provider {Provider}, media type {MediaType}, foreign id {ForeignId}");
    private static readonly Action<ILogger, string, string, string, string, Exception?> RequestConfirmed =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(2412, "MediaRequestConfirmed"),
            "Media request confirmed for provider {Provider}, media type {MediaType}, foreign id {ForeignId}, status {Status}");
    private static readonly Action<ILogger, string, string, string, string, Exception?> RequestRejected =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(2413, "MediaRequestRejected"),
            "Media request rejected for provider {Provider}, media type {MediaType}, foreign id {ForeignId}, status {Status}");
    private static readonly Action<ILogger, string, string, string, Exception?> ProviderUnavailable =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(2414, "MediaRequestProviderUnavailable"),
            "Media request provider unavailable for provider {Provider}, media type {MediaType}, foreign id {ForeignId}");
    private readonly SemaphoreSlim concurrency = new(options.Requests.MaximumConcurrentRequests);

    public async Task<MediaRequestPreviewResponse> PreviewAsync(
        MediaRequestPreviewInput input,
        CancellationToken cancellationToken)
    {
        try
        {
            return await PreviewCoreAsync(input, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (MediaRequestException exception)
        {
            RequestRejected(logger, input.Provider, input.MediaType, input.ForeignId, exception.Code, null);
            throw;
        }
        catch
        {
            ProviderUnavailable(logger, input.Provider, input.MediaType, input.ForeignId, null);
            throw new MediaRequestException(
                MediaRequestErrors.ProviderUnavailable,
                "The media provider is unavailable.",
                StatusCodes.Status503ServiceUnavailable);
        }
    }

    private async Task<MediaRequestPreviewResponse> PreviewCoreAsync(
        MediaRequestPreviewInput input,
        CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var provider = FindRequestProvider(input.Provider, input.MediaType);
        var lookup = await provider.GetLookupItemAsync(input.ForeignId, cancellationToken)
            ?? throw MediaAddOptionsService.Invalid(MediaRequestErrors.LookupNotFound, "The lookup item was not found.");
        if (await provider.GetRegisteredAsync(input.ForeignId, lookup.Title, lookup.Year, cancellationToken) is not null)
            throw new MediaRequestException(
                MediaRequestErrors.AlreadyRegistered,
                "The title is already registered.",
                StatusCodes.Status409Conflict);
        var providerOptions = await provider.GetAddOptionsAsync(cancellationToken);
        var root = providerOptions.RootFolders.FirstOrDefault(item =>
            protector.Protect(provider.ProviderName, "root", item.Id) == input.RootFolderId)
            ?? throw MediaAddOptionsService.Invalid(MediaRequestErrors.InvalidRootFolder, "The root folder selection is invalid.");
        var quality = providerOptions.QualityProfiles.FirstOrDefault(item =>
            protector.Protect(provider.ProviderName, "quality", item.Id) == input.QualityProfileId)
            ?? throw MediaAddOptionsService.Invalid(MediaRequestErrors.InvalidQualityProfile, "The quality profile selection is invalid.");
        if (!providerOptions.MonitoringOptions.Contains(input.Monitor, StringComparer.Ordinal))
            throw MediaAddOptionsService.Invalid(MediaRequestErrors.InvalidMonitoringOption, "The monitoring selection is invalid.");
        if (input.MediaType == MediaLookupTypes.Series
            && (input.SeriesType is null || !providerOptions.SeriesTypes.Contains(input.SeriesType, StringComparer.Ordinal)))
            throw MediaAddOptionsService.Invalid(MediaRequestErrors.InvalidMonitoringOption, "The series type selection is invalid.");

        var searchAfterAdd = input.SearchAfterAdd ?? options.Requests.DefaultSearchAfterAdd;
        var summary = new MediaRequestSummary(
            lookup.Title,
            lookup.Year,
            provider.ProviderName,
            input.MediaType,
            root.DisplayName,
            quality.DisplayName,
            MediaAddOptionsService.Friendly(input.Monitor),
            input.SeriesType is null ? null : MediaAddOptionsService.Friendly(input.SeriesType),
            searchAfterAdd);
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var expires = DateTimeOffset.UtcNow.AddMinutes(options.Requests.PreviewTokenLifetimeMinutes);
        store.Add(new(
            Hash(token),
            expires,
            provider.ProviderName,
            input.MediaType,
            input.ForeignId,
            input.RootFolderId,
            input.QualityProfileId,
            input.Monitor,
            input.SeriesType,
            searchAfterAdd,
            summary));
        PreviewCreated(logger, provider.ProviderName, input.MediaType, input.ForeignId, null);
        return new(token, expires, MediaRequestStatuses.PreviewReady, summary);
    }

    public async Task<MediaRequestConfirmResponse> ConfirmAsync(
        MediaRequestConfirmInput input,
        CancellationToken cancellationToken)
    {
        EnsureEnabled();
        if (string.IsNullOrWhiteSpace(input.IdempotencyKey) || input.IdempotencyKey.Length > 128)
            throw MediaAddOptionsService.Invalid(MediaRequestErrors.DuplicateRequest, "A valid idempotency key is required.");
        if (string.IsNullOrWhiteSpace(input.RequestToken))
            throw MediaAddOptionsService.Invalid(MediaRequestErrors.RequestExpired, "A valid request token is required.");
        var tokenHash = Hash(input.RequestToken);
        var acquired = store.Acquire(tokenHash, input.IdempotencyKey, DateTimeOffset.UtcNow);
        if (acquired.Existing is not null) return acquired.Existing;
        var request = acquired.Request!;

        var enteredConcurrencyGate = false;
        try
        {
            await concurrency.WaitAsync(cancellationToken);
            enteredConcurrencyGate = true;
            var provider = FindRequestProvider(request.Provider, request.MediaType);
            var lookup = await provider.GetLookupItemAsync(request.ForeignId, cancellationToken)
                ?? throw MediaAddOptionsService.Invalid(MediaRequestErrors.LookupNotFound, "The lookup item was not found.");
            var registered = await provider.GetRegisteredAsync(request.ForeignId, lookup.Title, lookup.Year, cancellationToken);
            if (registered is not null)
            {
                if (request.AddAttempted)
                    return Complete(tokenHash, input.IdempotencyKey, request, registered);
                AlreadyExisted(logger, request.Provider, request.MediaType, request.ForeignId, null);
                throw new MediaRequestException(
                    MediaRequestErrors.AlreadyRegistered,
                    "The title is already registered.",
                    StatusCodes.Status409Conflict);
            }
            var currentOptions = await provider.GetAddOptionsAsync(cancellationToken);
            var root = currentOptions.RootFolders.FirstOrDefault(item =>
                protector.Protect(request.Provider, "root", item.Id) == request.RootFolderId)
                ?? throw MediaAddOptionsService.Invalid(MediaRequestErrors.InvalidRootFolder, "The root folder selection is no longer valid.");
            var quality = currentOptions.QualityProfiles.FirstOrDefault(item =>
                protector.Protect(request.Provider, "quality", item.Id) == request.QualityProfileId)
                ?? throw MediaAddOptionsService.Invalid(MediaRequestErrors.InvalidQualityProfile, "The quality profile selection is no longer valid.");
            if (!currentOptions.MonitoringOptions.Contains(request.Monitor, StringComparer.Ordinal))
                throw MediaAddOptionsService.Invalid(MediaRequestErrors.InvalidMonitoringOption, "The monitoring selection is no longer valid.");
            var addProvider = addProviders.Single(item => item.ProviderName == request.Provider);
            store.MarkAddAttempted(tokenHash);
            var created = await addProvider.AddAsync(new(
                request.ForeignId,
                lookup.Title,
                lookup.Year,
                quality.Id,
                root.Value,
                request.Monitor,
                request.SeriesType,
                request.SearchAfterAdd), cancellationToken);
            return Complete(tokenHash, input.IdempotencyKey, request, created);
        }
        catch (MediaRequestException)
        {
            store.Release(tokenHash);
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            store.Release(tokenHash);
            throw;
        }
        catch (TaskCanceledException)
        {
            var reconciled = await ReconcileAsync(request, tokenHash, input.IdempotencyKey);
            if (reconciled is not null) return reconciled;
            store.Release(tokenHash);
            ProviderUnavailable(logger, request.Provider, request.MediaType, request.ForeignId, null);
            throw new MediaRequestException(
                MediaRequestErrors.ProviderUnavailable,
                "The media provider timed out.",
                StatusCodes.Status503ServiceUnavailable);
        }
        catch (MediaAuthenticationException)
        {
            store.Release(tokenHash);
            RequestRejected(
                logger,
                request.Provider,
                request.MediaType,
                request.ForeignId,
                MediaRequestErrors.ProviderConfigurationInvalid,
                null);
            throw new MediaRequestException(
                MediaRequestErrors.ProviderConfigurationInvalid,
                "The media provider rejected its configured credentials.",
                StatusCodes.Status502BadGateway);
        }
        catch (HttpRequestException exception) when (exception.StatusCode is null
            || (int)exception.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            var reconciled = await ReconcileAsync(request, tokenHash, input.IdempotencyKey);
            if (reconciled is not null) return reconciled;
            store.Release(tokenHash);
            ProviderUnavailable(logger, request.Provider, request.MediaType, request.ForeignId, null);
            throw new MediaRequestException(
                MediaRequestErrors.ProviderUnavailable,
                "The media provider is unavailable.",
                StatusCodes.Status503ServiceUnavailable);
        }
        catch (HttpRequestException)
        {
            store.Release(tokenHash);
            RequestRejected(
                logger,
                request.Provider,
                request.MediaType,
                request.ForeignId,
                MediaRequestErrors.ProviderRejectedRequest,
                null);
            throw new MediaRequestException(
                MediaRequestErrors.ProviderRejectedRequest,
                "The media provider rejected the request.",
                StatusCodes.Status502BadGateway);
        }
        finally
        {
            if (enteredConcurrencyGate) concurrency.Release();
        }
    }

    private async Task<MediaRequestConfirmResponse?> ReconcileAsync(
        PendingMediaRequest request,
        string tokenHash,
        string idempotencyKey)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 2, 15)));
            var provider = FindRequestProvider(request.Provider, request.MediaType);
            var registered = await provider.GetRegisteredAsync(
                request.ForeignId, request.Summary.Title, request.Summary.Year, timeout.Token);
            return registered is null ? null : Complete(tokenHash, idempotencyKey, request, registered);
        }
        catch
        {
            return null;
        }
    }

    private MediaRequestConfirmResponse Complete(
        string tokenHash,
        string idempotencyKey,
        PendingMediaRequest request,
        ProviderAddResult created)
    {
        var response = new MediaRequestConfirmResponse(
            MediaRequestStatuses.Created,
            request.Provider,
            request.MediaType,
            created.SourceId,
            created.Title);
        store.Complete(tokenHash, idempotencyKey, response);
        RequestConfirmed(logger, request.Provider, request.MediaType, request.ForeignId, response.Status, null);
        return response;
    }

    private IMediaRequestProvider FindRequestProvider(string provider, string mediaType) =>
        requestProviders.SingleOrDefault(item =>
            string.Equals(item.ProviderName, provider, StringComparison.OrdinalIgnoreCase)
            && item.SupportedMediaType == mediaType)
        ?? throw MediaAddOptionsService.Invalid(MediaRequestErrors.LookupNotFound, "The media request provider was not found.");

    private void EnsureEnabled()
    {
        if (!options.Requests.Enabled) throw MediaAddOptionsService.Disabled();
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    public void Dispose()
    {
        concurrency.Dispose();
        GC.SuppressFinalize(this);
    }
}
