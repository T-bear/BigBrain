using BigBrain.Api.Finance;
using BigBrain.Brain;
using BigBrain.Modules.Finance;
using Microsoft.Extensions.Configuration;

namespace BigBrain.Finance.Hosting;

// Trusted startup configuration, never supplied by a request, model or database grant.
// No approved/default real profile is shipped. Artifact/OS provisioning remains an owner gate.
internal sealed record RecurringLocalProfile(string OwnerDecision, string ArtifactAndIsolationEvidence,
    DateTimeOffset ApprovedAtUtc, DateTimeOffset ExpiresAtUtc, LocalReasonerRuntimeOptions Runtime);

internal sealed class LocalRecurringResearchComposition : IAsyncDisposable
{
    internal const string Section = "Finance:RecurringLocalReasoner";
    private readonly LocalReasonerRuntime _runtime;
    private readonly RecurringLocalProfile _profile;
    private readonly TimeProvider _clock;
    private DateTimeOffset _watermark;
    private readonly Lock _gate = new();
    internal FinanceRecurringReasonerBinding Binding { get; }

    internal static LocalRecurringResearchComposition? FromConfiguration(IConfiguration configuration,
        EodhdMarketMemory memory, TimeProvider clock)
    {
        var section = configuration.GetSection(Section);
        if (!section.GetValue<bool>("Enabled")) return null;
        var p = section.GetSection("Profile"); var runtime = p.GetSection("Runtime");
        // Explicit resource values; no inherited 180s acceptance flag or guessed model defaults.
        var options = new LocalReasonerRuntimeOptions
        {
            Enabled = true,
            WorkerExecutable = runtime["WorkerExecutable"] ?? "",
            WorkerArguments = [.. runtime.GetSection("WorkerArguments").Get<string[]>() ?? []],
            CoordinationDirectory = runtime["CoordinationDirectory"] ?? "",
            InvocationTimeout = runtime.GetValue<TimeSpan?>("InvocationTimeout") ?? TimeSpan.Zero,
            GracePeriod = runtime.GetValue<TimeSpan?>("GracePeriod") ?? TimeSpan.Zero,
            ReapTimeout = runtime.GetValue<TimeSpan?>("ReapTimeout") ?? TimeSpan.Zero
        };
        return new(new(p["OwnerDecision"] ?? "", p["ArtifactAndIsolationEvidence"] ?? "",
            p.GetValue<DateTimeOffset?>("ApprovedAtUtc") ?? default,
            p.GetValue<DateTimeOffset?>("ExpiresAtUtc") ?? default, options), memory.RecordRecurringRuntimeAudit, clock);
    }

    internal LocalRecurringResearchComposition(RecurringLocalProfile profile,
        Action<FinanceRecurringRuntimeAudit> audit, TimeProvider clock)
    {
        _profile = profile; _clock = clock;
        foreach (var reference in new[] { profile.OwnerDecision, profile.ArtifactAndIsolationEvidence })
            if (string.IsNullOrWhiteSpace(reference) || reference.Length > 256 || reference.Any(char.IsControl))
                throw new InvalidOperationException("Explicit approved model profile required.");
        FinanceTime.RequireUtc(profile.ApprovedAtUtc, nameof(profile.ApprovedAtUtc));
        FinanceTime.RequireUtc(profile.ExpiresAtUtc, nameof(profile.ExpiresAtUtc));
        if (profile.Runtime is null || !profile.Runtime.Enabled || profile.ApprovedAtUtc >= profile.ExpiresAtUtc)
            throw new InvalidOperationException("Invalid approved model profile.");
        RequireCurrent();
        // Uses the existing ordinary runtime policy. ControlledRealModelAcceptance is inaccessible
        // here: the old 180-second experimental exception is NOT reused or exposed in configuration.
        _runtime = new(profile.Runtime, item => audit(new(item.InvocationId, item.Phase.ToString(),
            item.Outcome.ToString(), item.Failure?.ToString(), item.AtUtc, item.ElapsedMilliseconds,
            item.InputHash, item.WorkerConfigurationHash, item.ResponseHash, item.WorkerExitCode)));
        Binding = new(LearningIdentity.Hash(profile), ReasonAsync, _runtime.CompletionTimeout);
    }

    private void RequireCurrent()
    {
        lock (_gate)
        {
            var now = _clock.GetUtcNow();
            FinanceTime.RequireUtc(now, nameof(now));
            if (now < _profile.ApprovedAtUtc || now >= _profile.ExpiresAtUtc || now < _watermark)
                throw new InvalidOperationException("Model profile expired, unapproved or clock regressed.");
            _watermark = now;
        }
    }

    private async Task<LearningReasonerReply> ReasonAsync(LearningDevelopmentInput input, CancellationToken token)
    {
        RequireCurrent();
        var remaining = _profile.ExpiresAtUtc - _clock.GetUtcNow();
        if (remaining <= TimeSpan.Zero) throw new InvalidOperationException("Model profile expired.");
        using var expiry = new CancellationTokenSource(remaining < _runtime.CompletionTimeout ? remaining : _runtime.CompletionTimeout, _clock);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token, expiry.Token);
        var result = await _runtime.ReasonAsync(input, stop.Token).ConfigureAwait(false);
        RequireCurrent();
        return result;
    }

    public ValueTask DisposeAsync() => _runtime.DisposeAsync();
}
