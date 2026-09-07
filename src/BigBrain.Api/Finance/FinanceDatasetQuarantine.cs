namespace BigBrain.Api.Finance;

// Physical raw-payload lifetime only. The intake store owns cleanup eligibility,
// lifecycle reconciliation and immutable evidence; extracted files are not reclaimed here.
internal sealed class FinanceDatasetQuarantine(FinanceDatasetOptions options)
{
    internal void EnsureDirectory() => Directory.CreateDirectory(options.QuarantineDirectory);

    internal string PrepareDownloadPath(string candidateId, string originalFilename)
    {
        var target = PayloadPath(candidateId, originalFilename);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        return target;
    }

    internal bool PayloadExists(string candidateId, string originalFilename) =>
        File.Exists(PayloadPath(candidateId, originalFilename));

    internal bool DeletePayloadIfPresent(string candidateId, string originalFilename)
    {
        var path = PayloadPath(candidateId, originalFilename);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    internal void EnsureDownloadSpace(long expectedBytes)
    {
        var root = Path.GetPathRoot(Path.GetFullPath(options.QuarantineDirectory))!;
        var drive = new DriveInfo(root);
        if (expectedBytes > options.MaximumDownloadBytes ||
            drive.AvailableFreeSpace - expectedBytes < options.MinimumFreeBytesAfterDownload)
            throw new IOException("Dataset download blocked by configured size/disk safety gate.");
    }

    private string PayloadPath(string candidateId, string originalFilename) =>
        Path.Combine(options.QuarantineDirectory, candidateId, "artifact", SafeName(originalFilename));

    private static string SafeName(string value)
    {
        var name = Path.GetFileName(value);
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Invalid artifact filename.");
        return name;
    }
}
