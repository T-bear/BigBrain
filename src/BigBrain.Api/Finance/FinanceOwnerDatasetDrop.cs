using System.Security.Cryptography;
using System.IO.Compression;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Finance;

public sealed record OwnerDatasetDropMetadata
{
    public string? SourceProvider { get; init; }
    public string? OriginalUrl { get; init; }
    public DateOnly? DownloadedOn { get; init; }
    public string? LicenseOrTermsUrl { get; init; }
    public string? DeclaredLicense { get; init; }
    public string? OwnerNotes { get; init; }
    public IReadOnlyList<string>? ExpectedSymbols { get; init; }
    public string? ExpectedMarket { get; init; }
    public string? PriceBasis { get; init; }
    public bool? DownloadedManually { get; init; }
    public string? PermissionReference { get; init; }
    public string? OwnerRightsDecision { get; init; }
}

public sealed record OwnerDatasetDropScanResult(string Filename, string Status, string Reason,
    string? CandidateId, string? ArtifactSha256, DatasetCatalogItem? Inspection);

internal sealed class FinanceOwnerDatasetDropScanner
{
    private static readonly JsonSerializerOptions SidecarJson = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    private readonly FinanceDatasetOptions _options;
    private readonly FinanceDatasetIntakeStore _store;

    public FinanceOwnerDatasetDropScanner(FinanceDatasetOptions options, FinanceDatasetIntakeStore store)
    {
        _options = options;
        _store = store;
    }

    internal IReadOnlyList<OwnerDatasetDropScanResult> ScanOnce()
    {
        if (!Directory.Exists(_options.OwnerDropDirectory)) return [];
        var results = new List<OwnerDatasetDropScanResult>();
        foreach (var readyPath in Directory.EnumerateFiles(_options.OwnerDropDirectory, "*.ready", SearchOption.TopDirectoryOnly)
                     .Order(StringComparer.Ordinal))
            results.Add(ProcessReadyMarker(readyPath));
        return results;
    }

    private OwnerDatasetDropScanResult ProcessReadyMarker(string readyPath)
    {
        var ready = new FileInfo(readyPath);
        if (!IsDirectRegularFile(ready)) return Result(ready.Name, "Rejected", "readyMarkerNotRegularFile");
        var filename = ready.Name[..^".ready".Length];
        if (filename.Length is 0 or > 180 || filename != Path.GetFileName(filename))
            return Result(ready.Name, "Rejected", "unsafeFilename");
        var extension = Path.GetExtension(filename);
        if (!extension.Equals(".csv", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            return Result(filename, "Rejected", "unsupportedFileType");
        var sourcePath = Path.Combine(_options.OwnerDropDirectory, filename);
        var source = new FileInfo(sourcePath);
        if (!IsDirectRegularFile(source)) return Result(filename, "Waiting", "dataFileMissingOrNotRegular");
        if (source.Length is <= 0 || source.Length > _options.MaximumDownloadBytes)
            return Result(filename, "Rejected", "artifactSizeOutsideLimit");

        OwnerDatasetDropMetadata? metadata = null;
        string? sidecarHash = null;
        var sidecarName = Path.GetFileNameWithoutExtension(filename) + ".metadata.json";
        var sidecarPath = Path.Combine(_options.OwnerDropDirectory, sidecarName);
        byte[]? sidecarBytes;
        try { sidecarBytes = ReadSidecarBytes(sourcePath, filename, sidecarPath); }
        catch (Exception exception) when (exception is InvalidDataException or IOException)
        { return Result(filename, "Rejected", "invalidOrUnsafeSidecar"); }
        if (sidecarBytes is not null)
        {
            try
            {
                sidecarHash = Hex(SHA256.HashData(sidecarBytes));
                metadata = JsonSerializer.Deserialize<OwnerDatasetDropMetadata>(sidecarBytes, SidecarJson)
                           ?? throw new InvalidDataException("Sidecar is empty.");
                ValidateMetadata(metadata);
            }
            catch (Exception exception) when (exception is JsonException or InvalidDataException or IOException)
            {
                return Result(filename, "Rejected", "invalidOrUnsafeSidecar");
            }
        }

        var beforeLength = source.Length;
        var beforeWrite = source.LastWriteTimeUtc;
        string sourceHash;
        try
        {
            using var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            sourceHash = DatasetContentIdentity.Sha256(input);
        }
        catch (IOException)
        {
            return Result(filename, "Waiting", "fileNotStable");
        }
        source.Refresh();
        if (source.Length != beforeLength || source.LastWriteTimeUtc != beforeWrite)
            return Result(filename, "Waiting", "fileNotStable");

        var identityBytes = Encoding.UTF8.GetBytes(sourceHash + "\n" + (sidecarHash ?? "none"));
        var candidateId = "owner-drop-" + Hex(SHA256.HashData(identityBytes))[..24];
        var targetDirectory = Path.Combine(_options.QuarantineDirectory, candidateId, "artifact");
        Directory.CreateDirectory(targetDirectory);
        var targetPath = Path.Combine(targetDirectory, filename);
        if (!File.Exists(targetPath))
        {
            var partial = targetPath + ".partial";
            try
            {
                using (var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var output = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[81_920];
                    long total = 0;
                    for (int read; (read = input.Read(buffer, 0, buffer.Length)) > 0;)
                    {
                        total += read;
                        if (total > _options.MaximumDownloadBytes) throw new InvalidDataException("Owner artifact exceeds configured size limit.");
                        output.Write(buffer, 0, read);
                    }
                }
                using var copied = File.OpenRead(partial);
                if (DatasetContentIdentity.Sha256(copied) != sourceHash) return Unstable(partial, filename);
                source.Refresh();
                if (source.Length != beforeLength || source.LastWriteTimeUtc != beforeWrite) return Unstable(partial, filename);
                File.Move(partial, targetPath);
            }
            catch (IOException)
            {
                if (File.Exists(partial)) File.Delete(partial);
                return Result(filename, "Waiting", "fileNotStable");
            }
        }
        else
        {
            using var existing = File.OpenRead(targetPath);
            if (DatasetContentIdentity.Sha256(existing) != sourceHash)
                return Result(filename, "Rejected", "quarantineIdentityConflict", candidateId, sourceHash);
        }

        var candidate = Candidate(candidateId, filename, metadata, sidecarHash, beforeLength);
        try
        {
            var inspection = _store.InspectValidateForReview(candidate, targetPath);
            return Result(filename, inspection.Status, PromotionReason(inspection), candidateId, sourceHash, inspection);
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException or IOException or DecoderFallbackException)
        {
            var rejected = _store.RejectQuarantinedInspection(candidate, targetPath, "inspectionRejected:" + exception.GetType().Name);
            return Result(filename, "Rejected", "inspectionRejected", candidateId, sourceHash, rejected);
        }
    }

    private static OwnerDatasetDropScanResult Unstable(string partial, string filename)
    {
        if (File.Exists(partial)) File.Delete(partial);
        return Result(filename, "Waiting", "fileNotStable");
    }

    private static ExternalDatasetCandidate Candidate(string candidateId, string filename,
        OwnerDatasetDropMetadata? metadata, string? sidecarHash, long bytes)
    {
        var provider = Clean(metadata?.SourceProvider, 120) ?? "OWNER-DROP-UNKNOWN";
        var sourceUrl = SafeUrl(metadata?.OriginalUrl) ?? "owner-drop:" + filename;
        var evidenceUrl = SafeUrl(metadata?.LicenseOrTermsUrl) ?? "";
        var declared = Clean(metadata?.DeclaredLicense, 240) ?? "UNKNOWN";
        var notes = new[]
        {
            "Owner-controlled manual drop; possession is not entitlement.",
            metadata?.DownloadedOn is { } date ? "DownloadedOn=" + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null,
            metadata?.DownloadedManually is { } manual ? "DownloadedManually=" + manual : null,
            Clean(metadata?.ExpectedMarket, 80) is { } market ? "ExpectedMarket=" + market : null,
            metadata?.ExpectedSymbols is { Count: > 0 } symbols ? "ExpectedSymbols=" + string.Join(',', symbols.Select(x => Clean(x, 32)).Where(x => x is not null)) : null,
            Clean(metadata?.PriceBasis, 40) is { } basis ? "OwnerDeclaredPriceBasis=" + basis : null,
            Clean(metadata?.PermissionReference, 500) is { } permission ? "PermissionReference=" + permission : null,
            Clean(metadata?.OwnerNotes, 2_000) is { } ownerNotes ? "OwnerNotes=" + ownerNotes : null,
            sidecarHash is null ? "Sidecar=absent" : "SidecarSha256=" + sidecarHash
        };
        return new(candidateId, provider, sourceUrl, "Owner-controlled local drop", filename,
            new(DatasetLicenseClass.Unknown, declared, evidenceUrl, DateOnly.FromDateTime(DateTime.UtcNow),
                "Owner-supplied metadata is retained as a claim and requires independent review.", DatasetEvidenceResult.Unknown,
                false, Clean(metadata?.PermissionReference, 500) ?? ""),
            string.Join("; ", notes.Where(x => x is not null)), DatasetPriceBasis.Unclear,
            DatasetSurvivorshipBias.SurvivorshipUnknown, bytes, OwnerDecision(metadata),
            Clean(metadata?.PermissionReference, 500) ?? "", Clean(metadata?.PriceBasis, 40)?.ToUpperInvariant() ?? "UNKNOWN");
    }

    private byte[]? ReadSidecarBytes(string sourcePath, string filename, string externalSidecarPath)
    {
        if (File.Exists(externalSidecarPath))
        {
            var sidecar = new FileInfo(externalSidecarPath);
            if (!IsDirectRegularFile(sidecar) || sidecar.Length > _options.MaximumSidecarBytes)
                throw new InvalidDataException("External sidecar is not a bounded regular file.");
            return File.ReadAllBytes(externalSidecarPath);
        }
        if (!Path.GetExtension(filename).Equals(".zip", StringComparison.OrdinalIgnoreCase)) return null;
        using var archive = ZipFile.OpenRead(sourcePath);
        if (archive.Entries.Count > _options.MaximumArchiveFiles) throw new InvalidDataException("Archive contains too many files.");
        long expanded = 0;
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.StartsWith('/') || entry.FullName.StartsWith('\\') || Path.IsPathRooted(entry.FullName) ||
                entry.FullName.Split('/', '\\').Any(part => part == "..")) throw new InvalidDataException("Unsafe archive path.");
            expanded += entry.Length;
            if (expanded > _options.MaximumExtractedBytes) throw new InvalidDataException("Archive exceeds extraction limit.");
        }
        var csv = archive.Entries.Where(entry => Path.GetExtension(entry.Name).Equals(".csv", StringComparison.OrdinalIgnoreCase) &&
            !entry.Name.Equals("manifest.csv", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (csv.Length != 1) return null;
        var expected = Path.GetFileNameWithoutExtension(csv[0].Name) + ".metadata.json";
        var metadata = archive.Entries.SingleOrDefault(entry => entry.FullName == entry.Name && entry.Name.Equals(expected, StringComparison.Ordinal));
        if (metadata is null) return null;
        if (metadata.Length > _options.MaximumSidecarBytes) throw new InvalidDataException("Embedded sidecar exceeds configured limit.");
        using var input = metadata.Open();
        using var output = new MemoryStream((int)metadata.Length);
        input.CopyTo(output);
        return output.ToArray();
    }

    private static DatasetOwnerRightsDecision OwnerDecision(OwnerDatasetDropMetadata? metadata)
    {
        var declared = metadata?.OwnerRightsDecision ?? metadata?.DeclaredLicense;
        return declared is not null && (declared.Equals("APPROVED_BY_OWNER", StringComparison.OrdinalIgnoreCase) ||
            declared.Equals("OWNER_APPROVED", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(metadata?.PermissionReference)
            ? DatasetOwnerRightsDecision.ApprovedByOwner : DatasetOwnerRightsDecision.NotProvided;
    }

    private static void ValidateMetadata(OwnerDatasetDropMetadata metadata)
    {
        var values = new[] { metadata.SourceProvider, metadata.OriginalUrl, metadata.LicenseOrTermsUrl,
            metadata.DeclaredLicense, metadata.OwnerNotes, metadata.ExpectedMarket, metadata.PriceBasis,
            metadata.PermissionReference, metadata.OwnerRightsDecision }.Concat(metadata.ExpectedSymbols ?? []);
        if ((metadata.ExpectedSymbols?.Count ?? 0) > 100 || values.Any(x => x is { Length: > 2_000 }))
            throw new InvalidDataException("Sidecar exceeds field limits.");
        _ = Clean(metadata.SourceProvider, 120);
        _ = Clean(metadata.DeclaredLicense, 240);
        _ = Clean(metadata.ExpectedMarket, 80);
        _ = Clean(metadata.PriceBasis, 40);
        _ = Clean(metadata.PermissionReference, 500);
        _ = Clean(metadata.OwnerRightsDecision, 80);
        _ = Clean(metadata.OwnerNotes, 2_000);
        foreach (var symbol in metadata.ExpectedSymbols ?? []) _ = Clean(symbol, 32);
        var forbidden = new[] { "api_key", "apikey", "access_token", "password", "bearer ", "cookie=" };
        if (values.Where(x => x is not null).Any(x => forbidden.Any(f => x!.Contains(f, StringComparison.OrdinalIgnoreCase))))
            throw new InvalidDataException("Sidecar appears to contain prohibited secret material.");
        _ = SafeUrl(metadata.OriginalUrl, true);
        _ = SafeUrl(metadata.LicenseOrTermsUrl, true);
    }

    private static bool IsDirectRegularFile(FileInfo file) => file.Exists && file.LinkTarget is null &&
        (file.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) == 0;
    private static string? SafeUrl(string? value, bool reject = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Length <= 2_000 && Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            uri.Scheme is "http" or "https") return uri.AbsoluteUri;
        if (reject) throw new InvalidDataException("Sidecar URL must be absolute HTTP(S).");
        return null;
    }
    private static string? Clean(string? value, int maximum) => string.IsNullOrWhiteSpace(value) ? null :
        value.Trim().Length <= maximum ? value.Trim() : throw new InvalidDataException("Sidecar field exceeds limit.");
    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();
    private static string PromotionReason(DatasetCatalogItem item) => item.Status switch
    {
        "Approved" => "readyForExplicitPromotionReview",
        "ManualReviewRequired" => "provenanceOrRightsReviewRequired",
        "Rejected" => "technicalOrPolicyGateFailed",
        _ => "inspectionComplete"
    };
    private static OwnerDatasetDropScanResult Result(string filename, string status, string reason,
        string? candidateId = null, string? hash = null, DatasetCatalogItem? inspection = null) =>
        new(filename, status, reason, candidateId, hash, inspection);
}

internal sealed class FinanceOwnerDatasetDropWorker(FinanceDatasetOptions options,
    FinanceOwnerDatasetDropScanner scanner, ILogger<FinanceOwnerDatasetDropWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, string, string, Exception?> InspectionWarning =
        LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(12601, "OwnerDropInspection"),
            "Finance owner-drop inspection {Status}: {Reason}");
    private static readonly Action<ILogger, Exception?> ScanFailure =
        LoggerMessage.Define(LogLevel.Error, new EventId(12602, "OwnerDropScanFailure"),
            "Finance owner-drop scan failed safely.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var result in scanner.ScanOnce())
                    if (result.Status is "Rejected" or "Waiting")
                        InspectionWarning(logger, result.Status, result.Reason, null);
            }
            catch (Exception exception)
            {
                ScanFailure(logger, exception);
            }
            await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(options.OwnerDropScanSeconds, 10, 3_600)), stoppingToken);
        }
    }
}
