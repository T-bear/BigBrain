using System.Text.Json;
using BigBrain.Api.Sentinel;
using BigBrain.Api.SystemRecovery;
using BigBrain.Modules.Finance;
using Microsoft.Extensions.Options;

namespace BigBrain.Api.Finance;

// A maintenance invocation, not an API/scheduler. It never activates acquisition or enrolls implicitly.
internal static class FinanceHistoricalResearchCommand
{
    private static readonly string[] CompetingWorkers = ["ResearchScheduler", "Eodhd", "Fred"];
    internal static bool IsCommand(string[] args) => args.Length > 0 && args[0].StartsWith("finance-historical-", StringComparison.Ordinal);
    internal static async Task<int> RunAsync(string[] args, IConfiguration configuration, TimeProvider clock, TextWriter output,
        Func<IConfiguration, EodhdMarketMemory, FinanceRecurringReasonerBinding?>? compose, CancellationToken token)
    {
        try
        {
            if (args.Length == 3 && args[0] == "finance-historical-enroll")
                return FinanceRecurringMaintenanceCommand.Run(["finance-recurring-enroll", args[1], args[2]], configuration, clock, output, historical: true);
            if (args.Length != 1 || args[0] is not ("finance-historical-inspect" or "finance-historical-result" or "finance-historical-run"))
                throw new InvalidOperationException();
            var options = configuration.GetSection(EodhdFinanceOptions.Section).Get<EodhdFinanceOptions>() ?? new();
            if (args[0] == "finance-historical-inspect")
            {
                var source = EodhdMarketMemory.InspectHistoricalResearchSource(options.DatabasePath, clock.GetUtcNow());
                output.WriteLine(JsonSerializer.Serialize(new
                {
                    contract = HistoricalResearchAdmission.Version,
                    candidate = HistoricalResearchAdmission.Candidate,
                    revision = HistoricalResearchAdmission.Revision,
                    source.Artifact,
                    source.Commitment,
                    source.Product,
                    instrument = "US:XNAS:AAPL",
                    count = source.Rows.Length,
                    from = source.Rows[0].Date,
                    to = source.Rows[^1].Date,
                    evaluatedFrom = source.Rows[0].Date,
                    evaluatedTo = source.Rows[499].Date,
                    limitations = HistoricalResearchAdmission.RequiredLimitations
                }));
                return 0;
            }
            var state = EodhdMarketMemory.ReadRecurringAuthorityState(options.DatabasePath);
            if (state.Authorization.Historical is null || state.ModelFreeFixture) throw new InvalidOperationException();
            if (args[0] == "finance-historical-result")
            {
                WriteResult(state, output);
                return 0; // verified existing source/native result commitments, read-only; never retries
            }
            RequireGates(configuration);
            SystemRecoveryCoordinator.RequireMaintenancePrerequisites(
                configuration.GetSection(SystemRecoveryOptions.SectionName).Get<SystemRecoveryOptions>() ?? new(), options.DatabasePath);
            var authority = RecurringResearchAuthority.FromConfiguration(configuration);
            authority.Require(state); // before construction or model composition
            var memory = new EodhdMarketMemory(options) { RecurringAuthority = authority };
            var binding = compose?.Invoke(configuration, memory) ?? throw new InvalidOperationException();
            using var sentinel = new LocalSentinelClient(Options.Create(
                configuration.GetSection(SentinelClientOptions.SectionName).Get<SentinelClientOptions>() ?? new()));
            var governor = new FinanceResearchResourceGovernor(
                configuration.GetSection(FinanceResearchResourceGovernorOptions.Section).Get<FinanceResearchResourceGovernorOptions>() ?? new(),
                new SentinelSystemMetricsProvider(sentinel));
            var result = await ExecuteAsync(memory, binding, governor, clock, token).ConfigureAwait(false);
            WriteResult(result, output);
            return result.State == RecurringResearchState.Completed ? 0 : 1;
        }
        catch (Exception) { output.WriteLine("HistoricalResearchRejectedOrUncertain; inspect persisted result; do not retry or renew authority."); return 1; }
    }

    internal static void RequireGates(IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("Finance:ResearchOperations:MaintenancePaused") ||
            CompetingWorkers.Any(s => configuration.GetValue<bool>("Finance:" + s + ":Enabled")))
            throw new InvalidOperationException("Historical maintenance requires paused competing research.");
        // ObservationRuntime/Alpaca are independent and deliberately remain untouched.
    }

    internal static async Task<RecurringResearchProgram> ExecuteAsync(EodhdMarketMemory memory,
        FinanceRecurringReasonerBinding binding, IFinanceResearchResourceGovernor governor, TimeProvider clock, CancellationToken token)
    {
        var state = memory.ReadRecurringResearchProgram() ?? throw new InvalidOperationException();
        if (state.Authorization.Historical is null || binding.ModelFreeFixture != state.ModelFreeFixture ||
            binding.Identity != state.Authorization.ReasonerIdentity || binding.CompletionTimeout <= TimeSpan.Zero ||
            binding.CompletionTimeout > TimeSpan.FromSeconds(state.Authorization.Budget.CompletionSeconds))
            throw new InvalidOperationException("Historical binding mismatch.");
        memory.RequireRecurringAuthority(state);
        if ((await governor.EvaluateAsync(clock.GetUtcNow(), token).ConfigureAwait(false)).Decision != FinanceResearchResourceDecisionKind.Allow)
            throw new InvalidOperationException("Historical resource gate closed.");
        return await memory.RunRecurringResearchAsync(1, binding.Identity, clock, binding.Reason, token).ConfigureAwait(false);
    }

    private static void WriteResult(RecurringResearchProgram state, TextWriter output) => output.WriteLine(JsonSerializer.Serialize(new
    {
        program = state.Id,
        status = state.State.ToString(),
        state.StopReason,
        state.Invocations,
        state.Evaluations,
        dataset = state.Authorization.Historical!.RevisionId,
        model = state.Authorization.Historical.ModelArtifactChecksum,
        sourceCommitment = state.Authorization.Historical.SnapshotChecksum,
        reasoner = state.Authorization.ReasonerIdentity,
        attempts = state.Attempts.Select(x => new
        {
            x.Number,
            x.Outcome,
            x.CutoffUtc,
            x.AvailableAtUtc,
            x.ResponseChecksum,
            x.EvaluationId,
            x.EvaluationChecksum,
            x.ReferenceValidationExcessReturn,
            hypothesis = x.Proposal,
            input = x.Scope.Input.InputChecksum,
            x.RuntimeAudit
        })
    }));
}
