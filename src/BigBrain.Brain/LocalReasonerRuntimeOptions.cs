using System.Collections.Immutable;

namespace BigBrain.Brain;

// Trusted composition only. Never deserialized from a reasoner request/reply or exposed by an API.
public sealed record LocalReasonerRuntimeOptions
{
    public bool Enabled { get; init; } // Disabled by default; no external mode exists.
    public required string WorkerExecutable { get; init; }
    public ImmutableArray<string> WorkerArguments { get; init; } = [];
    public required string CoordinationDirectory { get; init; }
    public TimeSpan InvocationTimeout { get; init; } = TimeSpan.FromSeconds(30);
    // Owner-authorized BB-132A acceptance harness only; no public/runtime configuration switch.
    // Does not change Finance projection limits, ledger budget or ordinary proof-worker policy.
    internal bool ControlledRealModelAcceptance { get; init; }
    public TimeSpan GracePeriod { get; init; } = TimeSpan.FromMilliseconds(250);
    public TimeSpan ReapTimeout { get; init; } = TimeSpan.FromSeconds(2);

    internal void Validate()
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("Linux runtime required.");
        if (!Path.IsPathFullyQualified(WorkerExecutable) || !Path.IsPathFullyQualified(CoordinationDirectory) ||
            WorkerArguments.IsDefault || WorkerArguments.Length > 8 ||
            WorkerArguments.Any(x => x is null || x.Length > 256 || x.Contains('\0', StringComparison.Ordinal)) ||
            InvocationTimeout < TimeSpan.FromMilliseconds(100) ||
            (ControlledRealModelAcceptance
                ? InvocationTimeout != TimeSpan.FromSeconds(180)
                : InvocationTimeout > TimeSpan.FromSeconds(30)) ||
            GracePeriod < TimeSpan.FromMilliseconds(50) || GracePeriod > TimeSpan.FromSeconds(1) ||
            ReapTimeout < TimeSpan.FromMilliseconds(100) || ReapTimeout > TimeSpan.FromSeconds(2))
            throw new ArgumentException("Invalid bounded local runtime configuration.");
        var directory = new DirectoryInfo(CoordinationDirectory);
        if (!directory.Exists || directory.LinkTarget is not null ||
            File.GetUnixFileMode(CoordinationDirectory) != (UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute))
            throw new ArgumentException("A provisioned private coordination directory is required.");
    }
}

public enum LocalReasonerFailure
{
    Disabled, Busy, InvalidInput, InvalidTransport, InvalidReply, Timeout, WorkerFailed,
    AuditFailed, StopUnconfirmed
}

public sealed class LocalReasonerException(LocalReasonerFailure failure) : Exception($"Local reasoner: {failure}.")
{
    public LocalReasonerFailure Failure { get; } = failure;
}

public enum LocalReasonerAuditPhase { Started, ResponseStarted, Completed }
public enum LocalReasonerOutcome { Started, Proposal, NoUsefulProposal, Cancelled, Disabled, Failed }

// Fixed metadata only; no PID/path, raw response/prompt, free-form reason or Finance snapshot.
// Sink is trusted controller composition, NOT a capability given to the worker.
public sealed record LocalReasonerAudit(Guid InvocationId, LocalReasonerAuditPhase Phase,
    LocalReasonerOutcome Outcome, LocalReasonerFailure? Failure, DateTimeOffset AtUtc,
    long ElapsedMilliseconds, string InputHash, string WorkerConfigurationHash, string? ResponseHash)
{
    public string Mode { get; } = "Local";
    public string Protocol { get; } = "BRF1";
    public string ControllerVersion { get; } = "bb131f-v1";
    public string CallerKind { get; } = "InternalWorkloadUnattested";
    // Observed only through the retained owned child; never an input or process target.
    // Exit codes are diagnostics, not scientific evidence or proof of OOM/signal cause.
    public int? WorkerExitCode { get; init; }
    public bool? WorkerCleanupRequired { get; init; }
}
