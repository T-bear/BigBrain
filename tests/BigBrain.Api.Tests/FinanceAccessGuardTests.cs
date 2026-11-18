using System.Net;
using BigBrain.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BigBrain.Api.Tests;

public sealed class FinanceAccessGuardTests
{
    private static readonly string Token = new('a', 64);

    [Fact]
    public void SecureComparisonIsIndependentOfUntrustedNetworkDetails()
    {
        var guard = new FinanceAccessGuard(true, Token);
        Assert.True(guard.Allows("Bearer " + Token));
        Assert.False(guard.Allows(null));
        Assert.False(guard.Allows("Bearer " + new string('b', 64)));
        Assert.False(guard.Allows("Basic " + Token));
        Assert.False(guard.Allows("Bearer " + Token + " extra"));
    }

    [Fact]
    public void MissingOrShortProductionTokenFailsClosed()
    {
        Assert.Throws<InvalidOperationException>(() => new FinanceAccessGuard(true, null));
        Assert.Throws<InvalidOperationException>(() => new FinanceAccessGuard(true, "weak"));
    }

    [Fact]
    public async Task FinanceRejectsUnauthenticatedClientsButNonFinanceStillWorks()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(web => web.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:FinanceAccessRequired"] = "true",
                    ["Security:FinanceAccessToken"] = Token,
                })));
        using var client = factory.CreateClient();

        using var unauthenticated = await client.GetAsync("/api/v1/modules/finance/risk/status",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        Assert.Equal("no-store", unauthenticated.Headers.CacheControl?.ToString());

        using var wrong = new HttpRequestMessage(HttpMethod.Get, "/api/v1/modules/finance/risk/status");
        wrong.Headers.TryAddWithoutValidation("Authorization", "Bearer " + new string('b', 64));
        using var denied = await client.SendAsync(wrong, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        using var correct = new HttpRequestMessage(HttpMethod.Get, "/api/v1/modules/finance/risk/status");
        correct.Headers.TryAddWithoutValidation("Authorization", "Bearer " + Token);
        using var allowed = await client.SendAsync(correct, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);

        using var health = await client.GetAsync("/api/v1/system/health", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }
}
