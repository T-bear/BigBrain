
namespace BigBrain.Finance.Hosting;

// Alternative entry point for the SAME API host, not another worker/service/scheduler.
// No assembly scan, reflection plugin, endpoint, grant creation or implicit runtime profile.
internal static class FinanceResearchHost
{
    internal static async Task Main(string[] args)
    {
        LocalRecurringResearchComposition? owned = null;
        try
        {
            BigBrain.Api.Program.Run(args, (configuration, memory) =>
            {
                owned = LocalRecurringResearchComposition.FromConfiguration(configuration, memory, TimeProvider.System);
                return owned?.Binding;
            });
        }
        finally
        {
            if (owned is not null) await owned.DisposeAsync().ConfigureAwait(false);
        }
    }
}
