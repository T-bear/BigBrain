using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace BigBrain.Sentinel;

public sealed class SentinelProtocolOptions
{
    public const string SectionName = "SentinelProtocol";

    public bool Enabled { get; init; }

    [Required]
    [RegularExpression("^node:[A-Za-z0-9._-]{1,59}$")]
    public string NodeId { get; init; } = "node:local";

    [Required]
    public string SocketPath { get; init; } = "/run/bigbrain/sentinel.sock";

    public string ServerCertificatePath { get; init; } = string.Empty;

    public string TrustedClientCertificatePath { get; init; } = string.Empty;

    public string ProofPublicKeyPath { get; init; } = string.Empty;

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string ProofKeyId { get; init; } = "local-control-plane";

    public IReadOnlyList<SentinelFilesystemOptions> Filesystems { get; init; } = [];
}

public sealed class SentinelFilesystemOptions
{
    public string FilesystemId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string SentinelPath { get; init; } = string.Empty;
}

public sealed class SentinelProtocolOptionsValidator : IValidateOptions<SentinelProtocolOptions>
{
    public ValidateOptionsResult Validate(string? name, SentinelProtocolOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        RequireAbsolutePath(options.SocketPath, nameof(options.SocketPath), failures);
        RequireAbsolutePath(options.ServerCertificatePath, nameof(options.ServerCertificatePath), failures);
        RequireAbsolutePath(
            options.TrustedClientCertificatePath,
            nameof(options.TrustedClientCertificatePath),
            failures);
        RequireAbsolutePath(options.ProofPublicKeyPath, nameof(options.ProofPublicKeyPath), failures);
        var filesystemIds = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < options.Filesystems.Count; index++)
        {
            var filesystem = options.Filesystems[index];
            var prefix = $"{nameof(options.Filesystems)}[{index}]";
            if (string.IsNullOrWhiteSpace(filesystem.FilesystemId))
            {
                failures.Add($"{prefix}.FilesystemId is required.");
            }
            else if (!filesystemIds.Add(filesystem.FilesystemId))
            {
                failures.Add($"{prefix}.FilesystemId must be unique.");
            }
            if (string.IsNullOrWhiteSpace(filesystem.DisplayName))
            {
                failures.Add($"{prefix}.DisplayName is required.");
            }
            if (!Path.IsPathFullyQualified(filesystem.SentinelPath))
            {
                failures.Add($"{prefix}.SentinelPath must be an absolute path.");
            }
            else if (!Directory.Exists(filesystem.SentinelPath))
            {
                failures.Add($"{prefix}.SentinelPath must exist.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void RequireAbsolutePath(string value, string propertyName, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value))
        {
            failures.Add($"{propertyName} must be an absolute path when Sentinel protocol is enabled.");
        }
    }
}
