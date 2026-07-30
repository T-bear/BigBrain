using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace BigBrain.Api.Sentinel;

public sealed class SentinelClientOptions
{
    public const string SectionName = "Sentinel";

    public bool Enabled { get; init; }

    [Required]
    [RegularExpression("^node:[A-Za-z0-9._-]{1,59}$")]
    public string NodeId { get; init; } = "node:local";

    [Required]
    public string SocketPath { get; init; } = "/run/bigbrain/sentinel.sock";

    public string ClientCertificatePath { get; init; } = string.Empty;

    public string TrustedServerCertificatePath { get; init; } = string.Empty;

    public string ProofPrivateKeyPath { get; init; } = string.Empty;

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string ProofKeyId { get; init; } = "local-control-plane";
}

public sealed class SentinelClientOptionsValidator : IValidateOptions<SentinelClientOptions>
{
    public ValidateOptionsResult Validate(string? name, SentinelClientOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        RequireAbsolutePath(options.SocketPath, nameof(options.SocketPath), failures);
        RequireAbsolutePath(options.ClientCertificatePath, nameof(options.ClientCertificatePath), failures);
        RequireAbsolutePath(
            options.TrustedServerCertificatePath,
            nameof(options.TrustedServerCertificatePath),
            failures);
        RequireAbsolutePath(options.ProofPrivateKeyPath, nameof(options.ProofPrivateKeyPath), failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void RequireAbsolutePath(string value, string propertyName, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value))
        {
            failures.Add($"{propertyName} must be an absolute path when Sentinel is enabled.");
        }
    }
}
