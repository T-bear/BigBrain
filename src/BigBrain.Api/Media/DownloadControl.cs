using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace BigBrain.Api.Media;

internal sealed record QBittorrentQueueItem(
    string Hash, string Name, string State, string? Category, string? SavePath, string? ContentPath,
    double Progress, long SizeBytes, long DownloadedBytes, long DownloadSpeedBytesPerSecond,
    long UploadSpeedBytesPerSecond, int QueuePosition);

public sealed record DownloadSummary(
    string Id, string Name, string Status, double ProgressPercent, long SizeBytes,
    long DownloadedBytes, long DownloadSpeedBytesPerSecond, long UploadSpeedBytesPerSecond,
    int? QueuePosition, string Category, string Ownership, string ImportStatus,
    bool DestructiveRemovalAllowed, IReadOnlyList<string> Warnings);
public sealed record DownloadsResponse(DateTimeOffset CollectedAtUtc, IReadOnlyList<DownloadSummary> Downloads);
public sealed record DownloadRemovalPreviewInput(bool DeleteData = false);
public sealed record DownloadRemovalPreview(
    string ConfirmationToken, DateTimeOffset ExpiresAtUtc, string Name, string Status,
    string Category, string Ownership, long DownloadedBytes, bool FilesWillBePreserved,
    bool DestructiveRemovalAllowed, IReadOnlyList<string> Warnings);
public sealed record DownloadRemovalInput(string ConfirmationToken, bool DeleteData = false);
public sealed record DownloadRemovalResult(
    string Status, bool Removed, bool DataPreserved, bool AlreadyMissing, string Ownership, string? ErrorCode);

public sealed class DownloadControlException(string code, string safeMessage, int statusCode) : Exception
{
    public string Code { get; } = code;
    public string SafeMessage { get; } = safeMessage;
    public int StatusCode { get; } = statusCode;
}

internal sealed record DownloadIdentity(
    string OpaqueId, string Hash, string Fingerprint, DateTimeOffset ExpiresAtUtc);
internal sealed record RemovalConfirmation(
    string TokenHash, string OpaqueId, string Fingerprint, bool DeleteData, DateTimeOffset ExpiresAtUtc,
    bool InProgress = false, DownloadRemovalResult? Result = null);

internal sealed class DownloadControlStore
{
    private readonly object gate = new();
    private readonly Dictionary<string, DownloadIdentity> identities = [];
    private readonly Dictionary<string, RemovalConfirmation> confirmations = [];

    public string PutIdentity(string hash, string fingerprint, DateTimeOffset now)
    {
        lock (gate)
        {
            Purge(now);
            var existing = identities.Values.FirstOrDefault(item =>
                CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(item.Hash), Encoding.UTF8.GetBytes(hash))
                && item.Fingerprint == fingerprint);
            if (existing is not null) return existing.OpaqueId;
            var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(18)).ToLowerInvariant();
            identities[id] = new(id, hash, fingerprint, now.AddMinutes(5));
            return id;
        }
    }

    public DownloadIdentity GetIdentity(string id, DateTimeOffset now)
    {
        lock (gate)
        {
            Purge(now);
            return identities.TryGetValue(id, out var identity) ? identity
                : throw new DownloadControlException("downloadNotFound", "Nedladdningen hittades inte eller har löpt ut.", StatusCodes.Status404NotFound);
        }
    }

    public string AddConfirmation(string opaqueId, string fingerprint, bool deleteData, DateTimeOffset now)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        lock (gate)
        {
            Purge(now);
            confirmations[Hash(token)] = new(Hash(token), opaqueId, fingerprint, deleteData, now.AddMinutes(2));
        }
        return token;
    }

    public RemovalConfirmation Acquire(string token, string opaqueId, bool deleteData, DateTimeOffset now)
    {
        lock (gate)
        {
            Purge(now);
            var key = Hash(token);
            if (!confirmations.TryGetValue(key, out var value) || value.OpaqueId != opaqueId || value.DeleteData != deleteData)
                throw new DownloadControlException("confirmationExpired", "Bekräftelsen har löpt ut eller matchar inte åtgärden.", StatusCodes.Status410Gone);
            if (value.Result is not null) return value;
            if (value.InProgress)
                throw new DownloadControlException("downloadRemovalConflict", "Borttagningen pågår redan.", StatusCodes.Status409Conflict);
            value = value with { InProgress = true };
            confirmations[key] = value;
            return value;
        }
    }

    public void Complete(string token, DownloadRemovalResult result)
    {
        lock (gate)
        {
            var key = Hash(token);
            if (confirmations.TryGetValue(key, out var value)) confirmations[key] = value with { InProgress = false, Result = result };
        }
    }

    public void Release(string token)
    {
        lock (gate)
        {
            var key = Hash(token);
            if (confirmations.TryGetValue(key, out var value) && value.Result is null) confirmations[key] = value with { InProgress = false };
        }
    }

    private void Purge(DateTimeOffset now)
    {
        foreach (var key in identities.Where(item => item.Value.ExpiresAtUtc <= now).Select(item => item.Key).ToArray()) identities.Remove(key);
        foreach (var key in confirmations.Where(item => item.Value.ExpiresAtUtc <= now).Select(item => item.Key).ToArray()) confirmations.Remove(key);
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

internal interface IDownloadControlService
{
    Task<DownloadsResponse> GetAsync(CancellationToken cancellationToken);
    Task<DownloadSummary> GetAsync(string id, CancellationToken cancellationToken);
    Task<DownloadRemovalPreview> PreviewAsync(string id, DownloadRemovalPreviewInput input, CancellationToken cancellationToken);
    Task<DownloadRemovalResult> RemoveAsync(string id, DownloadRemovalInput input, CancellationToken cancellationToken);
}

internal sealed class DownloadControlService(
    IQBittorrentQueueClient client, DownloadControlStore store, ILogger<DownloadControlService> logger) : IDownloadControlService
{
    private static readonly Action<ILogger, string, bool, string, Exception?> Audit =
        LoggerMessage.Define<string, bool, string>(LogLevel.Information, new EventId(2420, "DownloadRemoval"),
            "Download control operation={Operation} deleteData={DeleteData} result={Result}");

    public async Task<DownloadsResponse> GetAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var queue = await ReadQueue(cancellationToken);
        return new(now, queue.Select(item => Map(item, queue, store.PutIdentity(item.Hash, Fingerprint(item), now))).ToArray());
    }

    public async Task<DownloadSummary> GetAsync(string id, CancellationToken cancellationToken)
    {
        var queue = await ReadQueue(cancellationToken);
        var item = Revalidate(store.GetIdentity(id, DateTimeOffset.UtcNow), queue);
        return Map(item, queue, id);
    }

    public async Task<DownloadRemovalPreview> PreviewAsync(string id, DownloadRemovalPreviewInput input, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var queue = await ReadQueue(cancellationToken);
        var item = Revalidate(store.GetIdentity(id, now), queue);
        var risk = Risk(item, queue);
        if (input.DeleteData && !risk.Allowed) throw DestructiveBlocked(risk.Code);
        var token = store.AddConfirmation(id, Fingerprint(item), input.DeleteData, now);
        return new(token, now.AddMinutes(2), item.Name, NormalizeStatus(item), DisplayCategory(item), Ownership(item),
            item.DownloadedBytes, !input.DeleteData, risk.Allowed, Warnings(item, risk));
    }

    public async Task<DownloadRemovalResult> RemoveAsync(string id, DownloadRemovalInput input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.ConfirmationToken))
            throw new DownloadControlException("confirmationExpired", "En giltig bekräftelse krävs.", StatusCodes.Status410Gone);
        var confirmation = store.Acquire(input.ConfirmationToken, id, input.DeleteData, DateTimeOffset.UtcNow);
        if (confirmation.Result is not null) return confirmation.Result;
        try
        {
            var queue = await ReadQueue(cancellationToken);
            var identity = store.GetIdentity(id, DateTimeOffset.UtcNow);
            var item = queue.SingleOrDefault(candidate => candidate.Hash == identity.Hash);
            if (item is null)
            {
                var missing = new DownloadRemovalResult("alreadyMissing", false, !input.DeleteData, true, "unknown", null);
                store.Complete(input.ConfirmationToken, missing);
                Audit(logger, "remove", input.DeleteData, "alreadyMissing", null);
                return missing;
            }
            if (Fingerprint(item) != identity.Fingerprint || Fingerprint(item) != confirmation.Fingerprint)
                throw new DownloadControlException("downloadIdentityChanged", "Nedladdningen ändrades efter bekräftelsen.", StatusCodes.Status409Conflict);
            var risk = Risk(item, queue);
            if (input.DeleteData && !risk.Allowed) throw DestructiveBlocked(risk.Code);
            await client.RemoveAsync(item.Hash, input.DeleteData, cancellationToken);
            var result = new DownloadRemovalResult("removed", true, !input.DeleteData, false, Ownership(item), null);
            store.Complete(input.ConfirmationToken, result);
            Audit(logger, "remove", input.DeleteData, "removed", null);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { store.Release(input.ConfirmationToken); throw; }
        catch (DownloadControlException) { store.Release(input.ConfirmationToken); throw; }
        catch (TaskCanceledException) { store.Release(input.ConfirmationToken); throw new DownloadControlException("providerTimeout", "qBittorrent svarade inte i tid.", StatusCodes.Status503ServiceUnavailable); }
        catch (MediaAuthenticationException) { store.Release(input.ConfirmationToken); throw new DownloadControlException("providerAuthenticationFailure", "qBittorrent-autentiseringen misslyckades.", StatusCodes.Status503ServiceUnavailable); }
        catch { store.Release(input.ConfirmationToken); throw new DownloadControlException("providerUnavailable", "qBittorrent är inte tillgänglig.", StatusCodes.Status503ServiceUnavailable); }
    }

    private async Task<IReadOnlyList<QBittorrentQueueItem>> ReadQueue(CancellationToken token)
    {
        try { return await client.GetQueueAsync(token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (DownloadControlException) { throw; }
        catch (TaskCanceledException) { throw new DownloadControlException("providerTimeout", "qBittorrent svarade inte i tid.", StatusCodes.Status503ServiceUnavailable); }
        catch (MediaAuthenticationException) { throw new DownloadControlException("providerAuthenticationFailure", "qBittorrent-autentiseringen misslyckades.", StatusCodes.Status503ServiceUnavailable); }
        catch { throw new DownloadControlException("providerUnavailable", "qBittorrent är inte tillgänglig.", StatusCodes.Status503ServiceUnavailable); }
    }

    private static QBittorrentQueueItem Revalidate(DownloadIdentity identity, IReadOnlyList<QBittorrentQueueItem> queue)
    {
        var item = queue.SingleOrDefault(candidate => candidate.Hash == identity.Hash)
            ?? throw new DownloadControlException("downloadNotFound", "Nedladdningen finns inte längre.", StatusCodes.Status404NotFound);
        if (Fingerprint(item) != identity.Fingerprint)
            throw new DownloadControlException("downloadIdentityChanged", "Nedladdningen ändrades efter visningen.", StatusCodes.Status409Conflict);
        return item;
    }

    private static DownloadSummary Map(QBittorrentQueueItem item, IReadOnlyList<QBittorrentQueueItem> queue, string id)
    {
        var risk = Risk(item, queue);
        return new(id, item.Name, NormalizeStatus(item), item.Progress * 100, item.SizeBytes, item.DownloadedBytes,
            item.DownloadSpeedBytesPerSecond, item.UploadSpeedBytesPerSecond, item.QueuePosition >= 0 ? item.QueuePosition : null,
            DisplayCategory(item), Ownership(item), item.Progress < 1 ? "notImported" : "unknown", risk.Allowed, Warnings(item, risk));
    }

    private static (bool Allowed, string Code) Risk(QBittorrentQueueItem item, IReadOnlyList<QBittorrentQueueItem> queue)
    {
        if (string.IsNullOrWhiteSpace(item.ContentPath)) return (false, "destructiveRemovalNotAllowed");
        if (item.Progress >= 1) return (false, "destructiveRemovalNotAllowed");
        if (string.Equals(NormalizePath(item.ContentPath), NormalizePath(item.SavePath), StringComparison.Ordinal)) return (false, "sharedPathRisk");
        if (queue.Count(other => string.Equals(NormalizePath(other.ContentPath), NormalizePath(item.ContentPath), StringComparison.Ordinal)) != 1)
            return (false, "sharedPathRisk");
        return (true, string.Empty);
    }

    private static DownloadControlException DestructiveBlocked(string code) => new(
        code, "Destruktiv borttagning är blockerad eftersom berörda data inte kan avgränsas säkert.", StatusCodes.Status409Conflict);
    private static string Fingerprint(QBittorrentQueueItem item) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        $"{item.Hash}|{item.Name}|{item.SizeBytes}|{item.Category}|{NormalizePath(item.SavePath)}|{NormalizePath(item.ContentPath)}"))).ToLowerInvariant();
    private static string NormalizePath(string? value) => (value ?? string.Empty).Trim().TrimEnd('/');
    private static string Ownership(QBittorrentQueueItem item) => item.Category?.Trim().ToLowerInvariant() switch { "sonarr" => "sonarr", "radarr" => "radarr", "" or null => "manual", _ => "unknown" };
    private static string DisplayCategory(QBittorrentQueueItem item) => string.IsNullOrWhiteSpace(item.Category) ? "Ingen kategori" : item.Category.Trim().Length <= 40 ? item.Category.Trim() : "Annan kategori";
    private static string NormalizeStatus(QBittorrentQueueItem item) => QBittorrentClient.NormalizeJobStatus(item.State, item.Progress * 100) switch
    { MediaJobStatuses.Downloading => "active", MediaJobStatuses.Queued => item.State.Contains("stopped", StringComparison.OrdinalIgnoreCase) ? "paused" : "queued", MediaJobStatuses.Stalled => "error", MediaJobStatuses.Failed => "error", MediaJobStatuses.Completed => "completed", _ => "unknown" };
    private static List<string> Warnings(QBittorrentQueueItem item, (bool Allowed, string Code) risk)
    {
        var warnings = new List<string>();
        if (Ownership(item) is "sonarr" or "radarr") warnings.Add($"Det här jobbet verkar hanteras av {Ownership(item) switch { "sonarr" => "Sonarr", _ => "Radarr" }}. BigBrain tar endast bort den aktuella posten från qBittorrent. Tjänsten kan försöka hämta den igen.");
        if (!risk.Allowed) warnings.Add("Radering av nedladdade data är blockerad eftersom påverkan inte kan avgränsas säkert.");
        return warnings;
    }
}
