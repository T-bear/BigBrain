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
