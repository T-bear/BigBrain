using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using BigBrain.Api.SystemRecovery;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Finance;

// Finite internal maintenance entry, never a hosted worker/public API or generic provider CLI.
internal static class FinanceObservationMaintenanceCommand
{
    internal const string Name = "finance-alpaca-daily-once";
    // A misspelled maintenance verb must not fall through to starting the application host.
    internal static bool IsCommand(string[] args) => args.Length > 0 && args[0].StartsWith("finance-alpaca-", StringComparison.Ordinal);

    internal static int Run(string[] args, IConfiguration configuration)
    {
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        using var terminate = OperatingSystem.IsLinux() ? PosixSignalRegistration.Create(PosixSignal.SIGTERM,
            context => { context.Cancel = true; stop.Cancel(); }) : null;
        try
        {
            return ExecuteAsync(args, configuration, BuildRevision.Current, TimeProvider.System,
                Console.Out, stop.Token).GetAwaiter().GetResult();
        }
        finally { Console.CancelKeyPress -= cancel; }
    }

    internal static async Task<int> ExecuteAsync(string[] args, IConfiguration configuration, string revision,
        TimeProvider clock, TextWriter output, CancellationToken cancellationToken,
        HttpMessageHandler? fixtureTransport = null, Action<EodhdMarketMemory>? fixtureSetup = null)
    {
        var stage = "Configuration";
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (args.Length != 3 || args[0] != Name || BuildRevision.Normalize(revision) == "UNKNOWN")
                throw new InvalidDataException();
            var options = FinanceObservationRuntimeOptions.FromConfiguration(configuration);
            if (options.Enabled) throw new InvalidDataException();
            var transport = AlpacaDailyObservationOptions.FromConfiguration(configuration);
            var trusted = new FinanceObservationRuntime.GuardedClock(clock);
            var now = trusted.GetUtcNow();
            var (policy, instruments) = FinanceObservationRuntime.ValidateConfiguration(options, transport, now);
            if (!DateOnly.TryParseExact(args[2], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) ||
                day >= DailyMarketEvidence.SourceDate(now) || !UsMarketCalendar.IsSession(day)) throw new InvalidDataException();
            var plan = instruments.SingleOrDefault(x => x.InstrumentId == args[1]) ?? throw new InvalidDataException();
            var selected = plan.Resolve(day, now);
            if (selected.Instrument is null || selected.Mapping.ProviderReference is not ("AAPL" or "MSFT") ||
                selected.Instrument.Id.Value != "US:XNAS:" + selected.Mapping.ProviderReference || selected.Mapping.Mic != "XNAS")
                throw new InvalidDataException();
            var finance = configuration.GetSection(EodhdFinanceOptions.Section).Get<EodhdFinanceOptions>() ?? new();
            var recovery = configuration.GetSection(SystemRecoveryOptions.SectionName).Get<SystemRecoveryOptions>() ?? new();
            stage = "Recovery";
            SystemRecoveryCoordinator.RequireMaintenancePrerequisites(recovery, finance.DatabasePath);
            // Same-store local process exclusion; never delete the lock pathname/reacquire a different inode.
            // Failure to obtain it is final, not a waiting queue or a retry.
            using var owned = new FileStream(finance.DatabasePath + ".observation-once.lock", FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None);
            stage = "Evidence";
            EodhdMarketMemory.RequireObservationMaintenanceEvidence(finance.DatabasePath, trusted.GetUtcNow());
            cancellationToken.ThrowIfCancellationRequested();
            var memory = new EodhdMarketMemory(finance);
            fixtureSetup?.Invoke(memory);
            memory.RequireObservationRuntimeReady(policy, trusted.GetUtcNow());
            var authorization = memory.PrepareObservationMapping(plan, day, trusted.GetUtcNow());
            using var source = fixtureTransport is null ? new AlpacaDailyMarketObservations(transport, day) :
                new AlpacaDailyMarketObservations(transport, day, fixtureTransport);
            stage = "Acquisition";
            // Exactly one call. Existing adapter makes at most one HTTP request, never retries.
            var result = await memory.ReobserveDailyAsync(source, selected.Instrument, selected.Mapping, policy,
                trusted, cancellationToken, authorization).ConfigureAwait(false);
            var receipt = result.Receipt;
            output.WriteLine(JsonSerializer.Serialize(new
            {
                status = result.Kind.ToString(),
                revision,
                instrument = receipt.Instrument.Id.Value,
                symbol = receipt.Mapping.ProviderReference,
                sourceDay = day,
                provider = receipt.Provider.Value,
                dataset = receipt.Dataset.Value,
                origin = receipt.Origin.ToString(),
                receipt = receipt.Id,
                checksum = receipt.Checksum,
                acquiredAtUtc = receipt.AcquiredAtUtc,
                ingestedAtUtc = receipt.IngestedAtUtc,
                knowledgeTimeUtc = receipt.KnowledgeTimeUtc,
                predecessor = receipt.CorrectsId
            }));
            return 0;
        }
        catch (Exception e)
        {
            // Never exception.Message/ToString, request, configuration or raw provider content.
            var category = e is OperationCanceledException ? "Cancelled" : e is ObservationSourceException source ?
                source.Failure.ToString() : FinanceObservationRuntime.Failure(e).ToString();
            output.WriteLine(JsonSerializer.Serialize(new { status = "Failed", stage, category }));
            return 1; // A possibly committed but uncertain result is never retried/refunded/deleted.
        }
    }
}
