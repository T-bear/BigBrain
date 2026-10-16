using BigBrain.Api.SystemRecovery;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Finance;

// Explicit initial enrollment only; no resume/reset/renewal command and no HTTP endpoint.
internal static class FinanceRecurringMaintenanceCommand
{
    internal static readonly string[] DisabledSections = ["ResearchScheduler", "RecurringLocalReasoner", "ObservationRuntime", "AlpacaDailyObservation", "Eodhd", "Fred"];
    internal static bool IsCommand(string[] args) => args.Length > 0 && args[0].StartsWith("finance-recurring-", StringComparison.Ordinal);
    internal static int Run(string[] args, IConfiguration configuration, TimeProvider clock, TextWriter output, bool historical = false)
    {
        try
        {
            var check = args.Length == 1 && args[0] == "finance-recurring-authority-check";
            if ((!check && (args.Length != 3 || args[0] != "finance-recurring-enroll")) ||
                !configuration.GetValue<bool>("Finance:ResearchOperations:MaintenancePaused") ||
                DisabledSections.Where(name => !historical || name is not ("ObservationRuntime" or "AlpacaDailyObservation"))
                    .Any(name => configuration.GetValue<bool>("Finance:" + name + ":Enabled")))
                throw new InvalidOperationException();
            if (check)
            {
                var options = configuration.GetSection(EodhdFinanceOptions.Section).Get<EodhdFinanceOptions>() ?? new();
                var prerequisites = configuration.GetSection(SystemRecoveryOptions.SectionName).Get<SystemRecoveryOptions>() ?? new();
                SystemRecoveryCoordinator.RequireMaintenancePrerequisites(prerequisites, options.DatabasePath);
                var state = EodhdMarketMemory.ReadRecurringAuthorityState(options.DatabasePath);
                RecurringResearchAuthority.FromConfiguration(configuration).Require(state);
                output.WriteLine("RecurringAuthorityMatched");
                return 0;
            }
            var info = new FileInfo(args[1]);
            if (!info.Exists || info.Length is <= 0 or > 65536) throw new InvalidOperationException();
            byte[] bytes;
            using (var stream = new FileStream(args[1], FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length is <= 0 or > 65536) throw new InvalidOperationException();
                bytes = new byte[checked((int)stream.Length)]; stream.ReadExactly(bytes);
                if (stream.ReadByte() != -1) throw new InvalidOperationException();
            }
            if (LearningIdentity.HashBytes(bytes) != args[2]) throw new InvalidOperationException();
            var grant = EodhdMarketMemory.ParseRecurringAuthorization(bytes);
            if ((grant.Historical is not null) != historical) throw new InvalidOperationException();
            grant.Validate(clock.GetUtcNow());
            var finance = configuration.GetSection(EodhdFinanceOptions.Section).Get<EodhdFinanceOptions>() ?? new();
            var recovery = configuration.GetSection(SystemRecoveryOptions.SectionName).Get<SystemRecoveryOptions>() ?? new();
            SystemRecoveryCoordinator.RequireMaintenancePrerequisites(recovery, finance.DatabasePath);
            using var lease = new FileStream(finance.DatabasePath + ".recurring-enroll.lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var memory = new EodhdMarketMemory(finance);
            var existing = memory.ReadRecurringResearchProgram();
            if (existing is not null) _ = RecurringResearchAuthority.Enrollment(existing); // spent/uncertain cannot export fresh authority
            var program = memory.CreateRecurringResearchProgram(grant, clock);
            output.Write(RecurringResearchAuthority.Enrollment(program)); // seed proposal only; never creates the protected witness
            return 0;
        }
        catch (Exception) { output.WriteLine("RecurringEnrollmentRejected"); return 1; } // no private paths/JSON/provider exceptions
    }
}
