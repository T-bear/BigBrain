using System.Net;
using BigBrain.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BigBrain.Api.Tests;

public sealed class MediaIngressIntegrationTests
{
    [Fact]
    public async Task DeniesFinanceButPreservesNonFinanceWhenSourceIsBlocked()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:MediaIngressDeniedCidrs"] = "0.0.0.0/0,::/0"
                })));
        using var client = factory.CreateClient();

        var finance = await client.GetAsync("/api/v1/modules/finance/risk/status",
            TestContext.Current.CancellationToken);
        var health = await client.GetAsync("/api/v1/system/health",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, finance.StatusCode);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }
}
