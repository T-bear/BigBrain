using System.Net;
using System.Text;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceEodhdIntegrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"bigbrain-eodhd-tests-{Guid.NewGuid():N}");

    [Fact]
    public void ParsesDocumentedRawOhlcvWithoutInventingAdjustedOhlc()
    {
        var bars = EodhdAdapter.Parse(Fixture());

        Assert.Equal(2, bars.Count);
        Assert.Equal(new DateOnly(2026, 8, 7), bars[0].Date);
        Assert.Equal(101.5m, bars[0].Close);
        Assert.Equal(100.9m, bars[0].AdjustedClose);
        Assert.Equal(1200, bars[0].Volume);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[{\"date\":\"2026-08-07\",\"open\":100,\"high\":99,\"low\":98,\"close\":101,\"adjusted_close\":100,\"volume\":1}]")]
    public void RejectsMalformedOrImpossiblePayload(string payload) =>
        Assert.ThrowsAny<Exception>(() => EodhdAdapter.Parse(Encoding.UTF8.GetBytes(payload)));

    [Fact]
    public async Task Retries429WithinBoundAndNeverRequiresNetwork()
    {
        var handler = new SequenceHandler(HttpStatusCode.TooManyRequests, HttpStatusCode.OK, Fixture());
        using var adapter = new EodhdAdapter(Options(token: "test-secret") with { MaximumRetries = 2 }, handler);

        var result = await adapter.FetchAsync("AAPL.US", new(2026, 8, 1), new(2026, 8, 11), TestContext.Current.CancellationToken);

        Assert.Equal(1, result.Retries);
        Assert.Equal(2, result.Bars.Count);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public void DurableStoreIsIdempotentRestartSafeAndReplayDeterministic()
    {
        var options = Options(); var instrument = EodhdCatalog.Watchlist.Single(value => value.Symbol == "AAPL");
        var bars = EodhdAdapter.Parse(Fixture()); var acquired = new DateTimeOffset(2026, 8, 11, 18, 0, 0, TimeSpan.Zero);
        var memory = new EodhdMarketMemory(options);
        var first = memory.Store(instrument, bars, Fixture(), new(2026, 8, 1), new(2026, 8, 11), acquired.AddSeconds(-1), acquired, 0);
        var second = memory.Store(instrument, bars, Fixture(), new(2026, 8, 1), new(2026, 8, 11), acquired.AddSeconds(-1), acquired, 0);
        var reopened = new EodhdMarketMemory(options); var snapshot = reopened.Snapshot(true, true, true);

        Assert.Equal(first, second);
        Assert.False(reopened.ShouldAcquire(instrument.ProviderSymbol, new(2026, 8, 11)));
        Assert.True(reopened.ShouldAcquire(instrument.ProviderSymbol, new(2026, 8, 12)));
        Assert.Equal(2, snapshot.HistoricalMemory.ObservationCount);
        Assert.Equal(HistoricalPersistenceState.Durable, snapshot.HistoricalMemory.Persistence);
        Assert.Equal(ObservationDataKind.Real, snapshot.DataKind);
        Assert.Equal("ownerAcceptedPersonalResearch", snapshot.Provider.EvidenceClass);
        Assert.Equal(reopened.ReplayChecksum(first, new(2026, 8, 1), new(2026, 8, 11)),
            reopened.ReplayChecksum(first, new(2026, 8, 1), new(2026, 8, 11)));
        var evidence = reopened.RuntimeEvidence();
        Assert.Equal(2, evidence.ExternalRequests);
        Assert.Equal(2, evidence.SuccessfulAttempts);
        Assert.Equal(2, evidence.Observations);
        Assert.Single(evidence.RevisionIds);
        Assert.Equal(["AAPL.US"], evidence.SuccessfulSymbols);
        Assert.Empty(evidence.FailedSymbols);
        Assert.True(evidence.CausalKnowledgeTimes);
        Assert.Equal(0, evidence.MissingPayloadFiles);
    }

    [Fact]
    public void ExpiryBlocksUseAndDeletionRequiresExactFreshPreview()
    {
        var unrelated = Path.Combine(_root, "unrelated.txt"); Directory.CreateDirectory(_root); File.WriteAllText(unrelated, "preserve");
        var options = Options() with { EntitlementEndsAtUtc = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero) };
        var memory = new EodhdMarketMemory(options); var instrument = EodhdCatalog.Watchlist[3]; var acquired = DateTimeOffset.UtcNow;
        var revision = memory.Store(instrument, EodhdAdapter.Parse(Fixture()), Fixture(), new(2026, 8, 1), new(2026, 8, 11), acquired.AddSeconds(-1), acquired, 0);
        var blocked = memory.Snapshot(true, true, false); var preview = memory.PreviewDeletion();
        var beforeDeletion = memory.ReplayChecksum(revision, new(2026, 8, 1), new(2026, 8, 11));

        Assert.False(blocked.Safety.IngestionAllowed);
        Assert.Equal(FinanceRetentionState.ExpiredBlocked, blocked.Retention!.State);
        Assert.Throws<InvalidOperationException>(() => memory.ExecuteDeletion(preview, "DELETE", acquired));
        var receipt = memory.ExecuteDeletion(preview, $"DELETE {preview.PreviewId}", acquired);
        Assert.StartsWith("eodhd-delete-", receipt);
        Assert.True(File.Exists(unrelated));
        Assert.NotEqual(beforeDeletion, memory.ReplayChecksum(revision, new(2026, 8, 1), new(2026, 8, 11)));
        var complete = memory.Snapshot(true, true, false);
        Assert.Equal(FinanceRetentionState.DeletionComplete, complete.Retention!.State);
        Assert.Equal(0, complete.HistoricalMemory.ObservationCount);
    }

    [Fact]
    public void MissingKeyKeepsAuthorizedCandidateDisabledAndSanitized()
    {
        var snapshot = new EodhdMarketMemory(Options()).Snapshot(true, false, false);

        Assert.Equal(MarketDataProviderState.Candidate, snapshot.Provider.State);
        Assert.False(snapshot.Safety.IngestionAllowed);
        Assert.DoesNotContain("token", snapshot.Provider.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(FinanceOperatingMode.Research, snapshot.Safety.Mode);
        Assert.False(snapshot.Safety.BrokerConnected);
    }

    [Fact]
    public void EntitlementAllowsOnlyActiveFreeResearchAndDeniesPostExpiryRetentionAndTrading()
    {
        var options = Options(); var policy = EodhdEntitlement.Create(options);
        var active = new MarketDataEntitlementContext(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero), true, true,
            MarketDataClassification.Raw, policy.Provider, policy.ProviderDataset);
        var ended = active with { SubscriptionActive = false };

        Assert.Equal(0, policy.MonetaryCostSek);
        Assert.Equal(EntitlementEvidenceClass.OwnerAcceptedPersonalResearch, policy.EvidenceClass);
        Assert.True(MarketDataEntitlementEvaluator.Evaluate(policy, MarketDataUse.Backtest, active).IsAllowed);
        Assert.False(MarketDataEntitlementEvaluator.Evaluate(policy, MarketDataUse.PaperTrading, active).IsAllowed);
        Assert.Equal(MarketDataEntitlementReasons.PostSubscriptionRetentionDenied,
            MarketDataEntitlementEvaluator.Evaluate(policy, MarketDataUse.HistoricalAnalysis, ended).ReasonCode);
    }

    private EodhdFinanceOptions Options(string token = "") => new()
    {
        Enabled = true, AccountActive = true, ApiToken = token, DatabasePath = Path.Combine(_root, "memory.db"),
        PayloadDirectory = Path.Combine(_root, "payloads"), BaseUrl = "https://example.invalid/api", TimeoutSeconds = 3
    };

    private static byte[] Fixture() => Encoding.UTF8.GetBytes("""
        [{"date":"2026-08-07","open":100,"high":102,"low":99,"close":101.5,"adjusted_close":100.9,"volume":1200},
         {"date":"2026-08-10","open":102,"high":104,"low":101,"close":103,"adjusted_close":102.5,"volume":1400}]
        """);

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
        GC.SuppressFinalize(this);
    }

    private sealed class SequenceHandler(HttpStatusCode first, HttpStatusCode second, byte[] payload) : HttpMessageHandler
    {
        internal int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var response = new HttpResponseMessage(Calls == 1 ? first : second)
            { Content = new ByteArrayContent(Calls == 1 ? [] : payload), RequestMessage = request };
            return Task.FromResult(response);
        }
    }
}
