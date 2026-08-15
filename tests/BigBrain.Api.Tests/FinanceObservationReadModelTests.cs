using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BigBrain.Api;
using BigBrain.Modules.Finance;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BigBrain.Api.Tests;

public sealed class FinanceObservationReadModelTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FinanceObservationReadModelTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public void SafeDefaultFailsClosedAndHasConfiguredResearchWatchlistOnly()
    {
        var snapshot = new SafeDefaultFinanceObservationReader().GetSnapshot();

        Assert.Equal(FinanceOperatingMode.Research, snapshot.Safety.Mode);
        Assert.False(snapshot.Safety.LiveTradingEnabled);
        Assert.False(snapshot.Safety.PaperTradingEnabled);
        Assert.False(snapshot.Safety.BrokerConnected);
        Assert.False(snapshot.Safety.IngestionAllowed);
        Assert.False(snapshot.Safety.RealProviderStorageAllowed);
        Assert.Equal(MarketDataProviderState.NoneAuthorized, snapshot.Provider.State);
        Assert.Equal(EntitlementState.PendingWrittenConfirmation, snapshot.Provider.Entitlement);
        Assert.Equal("ZERO-COST ENTITLEMENT GATE", snapshot.Provider.EntitlementGate);
        Assert.Equal("zero-cost-provider-unresolved", snapshot.HistoricalMemory.Policy);
        Assert.Equal(ObservationDataKind.None, snapshot.DataKind);
        Assert.Equal(8, snapshot.Watchlist.Count);
        Assert.All(snapshot.Watchlist, item => { Assert.Null(item.Price); Assert.Equal(ObservationDataKind.None, item.DataKind); });
        Assert.Equal(0, snapshot.HistoricalMemory.ObservationCount);
    }

    [Fact]
    public async Task ReadOnlyApiReturnsSanitizedFailClosedSnapshot()
    {
        var response = await _client.GetAsync("/api/v1/modules/finance/observation", TestContext.Current.CancellationToken);
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"mode\":\"research\"", raw);
        Assert.Contains("\"entitlement\":\"authorized\"", raw);
        Assert.Contains("EODHD FREE PERSONAL RESEARCH", raw);
        Assert.DoesNotContain("STATE B", raw);
        Assert.DoesNotContain("apiKey", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("order", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task ObservationApiHasNoMutation(string method)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1/modules/finance/observation");
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task FeatureApiIsBoundedReadOnlyResearchAndContainsNoSecretOrTradeSurface()
    {
        var response = await _client.GetAsync("/api/v1/modules/finance/features?instrumentId=US%3AARCX%3ASPY&featureId=sma.20&limit=20", TestContext.Current.CancellationToken);
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"operatingMode\":\"research\"", raw);
        Assert.Contains("core-daily-v1", raw);
        Assert.DoesNotContain("apiToken", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("order", raw, StringComparison.OrdinalIgnoreCase);
        using var mutation = new HttpRequestMessage(HttpMethod.Post, "/api/v1/modules/finance/features");
        Assert.Equal(HttpStatusCode.MethodNotAllowed,
            (await _client.SendAsync(mutation, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task BackupInventoryApiIsSanitizedReadOnlyResearch()
    {
        var response=await _client.GetAsync("/api/v1/modules/finance/backups",TestContext.Current.CancellationToken);var raw=await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Contains("\"operatingMode\":\"RESEARCH\"",raw);Assert.DoesNotContain("databasePath",raw,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("restoreStagingDirectory",raw,StringComparison.OrdinalIgnoreCase);
        using var mutation=new HttpRequestMessage(HttpMethod.Post,"/api/v1/modules/finance/backups");Assert.Equal(HttpStatusCode.MethodNotAllowed,(await _client.SendAsync(mutation,TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task ShadowApiIsBoundedReadOnlyResearchAndRejectsMalformedFilters()
    {
        var response=await _client.GetAsync("/api/v1/modules/finance/shadow/scorecard",TestContext.Current.CancellationToken);var raw=await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Contains("\"operatingMode\":\"RESEARCH\"",raw);Assert.Contains("CURRENT EOD / PROSPECTIVE EOD",raw);Assert.DoesNotContain("apiToken",raw,StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.BadRequest,(await _client.GetAsync("/api/v1/modules/finance/shadow/predictions?limit=999",TestContext.Current.CancellationToken)).StatusCode);
        using var mutation=new HttpRequestMessage(HttpMethod.Post,"/api/v1/modules/finance/shadow/predictions");Assert.Equal(HttpStatusCode.MethodNotAllowed,(await _client.SendAsync(mutation,TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public void DeterministicSyntheticSnapshotRetainsSafetyAndExplicitClassification()
    {
        var instant = new DateTimeOffset(2026, 8, 11, 10, 0, 0, TimeSpan.Zero);
        var snapshot = Fixture(instant);
        var repeated = Fixture(instant);

        Assert.Equal(JsonSerializer.Serialize(snapshot), JsonSerializer.Serialize(repeated));
        Assert.Equal(ObservationDataKind.SyntheticFixture, snapshot.DataKind);
        Assert.Equal(ObservationFreshnessState.Stale, snapshot.Watchlist[0].Freshness);
        Assert.Equal(ObservationSessionState.Gap, snapshot.Watchlist[0].Session);
        Assert.Equal(ObservationQualityState.Warning, snapshot.Watchlist[0].Quality);
        Assert.False(snapshot.Safety.IngestionAllowed);
    }

    private static FinanceObservationSnapshot Fixture(DateTimeOffset instant) => new(
        instant, new(FinanceOperatingMode.Research, false, false, false, false, false),
        new(MarketDataProviderState.NoneAuthorized, "Synthetic test fixture", EntitlementState.PendingWrittenConfirmation,
            "BB-071 / STATE B", "Fixture only"), instant, ObservationDataKind.SyntheticFixture,
        [new("US:XNAS:MSFT", "MSFT", "Microsoft", 100m, "USD", null, instant,
            ObservationFreshnessState.Stale, ObservationSessionState.Gap, ObservationQualityState.Warning,
            ObservationDataKind.SyntheticFixture, [new(instant, 100m, true)])],
        new(1, "revision-fixture-1", null, new(2026, 8, 11), new(2026, 8, 11), instant, 1, 0,
            HistoricalPersistenceState.FixtureMemory, "fixture", "fixture", "fixture-policy", "fixture:evidence"));
}
