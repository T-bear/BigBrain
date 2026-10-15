
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
            if (args.Length > 0 && args[0] == "finance-recurring-profile-identity")
            {
                try
                {
                    if (args.Length != 1) throw new InvalidOperationException();
                    var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(new Microsoft.AspNetCore.Builder.WebApplicationOptions
                    { Args = [], ApplicationName = typeof(BigBrain.Api.Program).Assembly.FullName });
                    owned = new(LocalRecurringResearchComposition.ReadProfile(builder.Configuration), _ => throw new InvalidOperationException(), TimeProvider.System);
                    Console.WriteLine(owned.Binding.Identity); // no process start, no grant, no profile/path echo
                }
                catch (Exception) { Console.WriteLine("ProfileRejected"); Environment.ExitCode = 1; }
                return;
            }
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
