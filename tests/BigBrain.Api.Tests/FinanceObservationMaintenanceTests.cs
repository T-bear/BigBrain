using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using BigBrain.Api.Finance;
using BigBrain.Api.SystemRecovery;
using BigBrain.Modules.Finance;
using Microsoft.Extensions.Configuration;

namespace BigBrain.Api.Tests;

#pragma warning disable CA1861
public sealed class FinanceObservationMaintenanceTests
{
    private const string Revision = "3a332c476ce6acf7007e463791ba416530c15302";
    private const string Id = "US:XNAS:AAPL";
    private static readonly DateTimeOffset At = new(2027, 2, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Day = new(2027, 2, 1);
    private static MarketDataEntitlementPolicy Policy => AlpacaDailyOwnerDecision.Create(At.AddYears(-1));

    [Theory]
    [InlineData("enabled")]
    [InlineData("missing-key")]
    [InlineData("bad-key")]
    [InlineData("missing-secret")]
    [InlineData("bad-secret")]
    [InlineData("transport-disabled")]
    [InlineData("unknown-id")]
    [InlineData("spy")]
    [InlineData("bad-mic")]
    [InlineData("mapping-not-yet-valid")]
    [InlineData("mapping-expired")]
    [InlineData("empty-universe")]
    [InlineData("large-universe")]
    [InlineData("duplicate-universe")]
    [InlineData("bad-json")]
    [InlineData("missing-policy")]
    [InlineData("future-policy")]
    [InlineData("revoked-policy")]
    [InlineData("deletion")]
    [InlineData("bad-day")]
    [InlineData("weekend")]
    [InlineData("holiday")]
    [InlineData("current-day")]
    [InlineData("future-day")]
    [InlineData("extra-arg")]
    [InlineData("unknown-command")]
    [InlineData("unknown-revision")]
    public async Task InvalidOperationFailsBeforeNetwork(string failure)
    {
        using var fixture = new Fixture(); var args = Args(); var revision = Revision;
        var entry = Entry();
        switch (failure)
        {
            case "enabled": fixture.Set("ObservationRuntime:Enabled", "true"); break;
            case "missing-key": fixture.Set("AlpacaDailyObservation:ApiKey", ""); break;
            case "bad-key": fixture.Set("AlpacaDailyObservation:ApiKey", "SECRET\r\ninjected"); break;
            case "missing-secret": fixture.Set("AlpacaDailyObservation:ApiSecret", ""); break;
            case "bad-secret": fixture.Set("AlpacaDailyObservation:ApiSecret", "SECRET!"); break;
            case "transport-disabled": fixture.Set("AlpacaDailyObservation:Enabled", "false"); break;
            case "unknown-id": args[1] = "SECRET-unconfigured"; break;
            case "spy": entry.ProviderSymbol = "SPY"; entry.InstrumentId = "US:ARCX:SPY"; entry.Mic = "ARCX"; fixture.Entries(entry); args[1] = entry.InstrumentId; break;
            case "bad-mic": entry.Mic = "UNKNOWN"; fixture.Entries(entry); break;
            case "mapping-not-yet-valid": entry.ValidFrom = Day.AddDays(1); fixture.Entries(entry); break;
            case "mapping-expired": entry.ValidTo = Day.AddDays(-1); fixture.Entries(entry); break;
            case "empty-universe": fixture.Entries(); break;
            case "large-universe": fixture.Entries(Enumerable.Repeat(entry, 5).ToArray()); break;
            case "duplicate-universe": fixture.Entries(entry, entry); break;
            case "bad-json": fixture.Set("ObservationRuntime:InstrumentsJson", "SECRET{"); break;
            case "missing-policy": fixture.Set("ObservationRuntime:OwnerAcceptanceVersion", ""); break;
            case "future-policy": fixture.Set("ObservationRuntime:PolicyRecordedAtUtc", At.AddDays(1).ToString("O", CultureInfo.InvariantCulture)); break;
            case "revoked-policy": fixture.Set("ObservationRuntime:AffectedUseEnabled", "false"); break;
            case "deletion": fixture.Set("ObservationRuntime:CurrentDeletion", "DeleteAtSubscriptionEnd"); break;
            case "bad-day": args[2] = "SECRET-not-date"; break;
            case "weekend": args[2] = "2027-01-31"; break;
            case "holiday": args[2] = "2027-01-18"; break;
            case "current-day": args[2] = "2027-02-02"; break;
            case "future-day": args[2] = "2027-02-03"; break;
            case "unknown-command": args[0] = "finance-alpaca-unknown"; Assert.True(FinanceObservationMaintenanceCommand.IsCommand(args)); break;
            case "extra-arg": args = [.. args, "--Enabled=true"]; break;
            case "unknown-revision": revision = "UNKNOWN"; break;
        }
        var result = await fixture.Run(args, revision, token: TestContext.Current.CancellationToken);
        Assert.Equal(1, result.Code); Assert.Equal(0, fixture.Calls);
        Assert.Equal(0L, fixture.Db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
        Assert.DoesNotContain("SECRET", result.Output); Assert.DoesNotContain("fixturekey", result.Output);
        Assert.DoesNotContain("fixturesecret", result.Output);
    }

    [Fact]
    public async Task ActualSingleRequestPathReopensDuplicatesRevisesAndPreservesKnowledge()
    {
        using var fixture = new Fixture();
        var before = At.AddTicks(-1);
        Assert.Equal(0, (await fixture.Run(token: TestContext.Current.CancellationToken)).Code); Assert.Equal(1, fixture.Calls);
        var memory = fixture.Db.Memory();
        var receipt = Assert.Single(memory.MarketKnowledgeAt(At, fixture.Clock, Policy).Observations);
        Assert.Equal(At, receipt.KnowledgeTimeUtc);
        Assert.True(receipt.Value.EventTimeUtc < receipt.AcquiredAtUtc);
        Assert.Null(receipt.Value.ProviderAvailableAtUtc);
        Assert.Empty(memory.MarketKnowledgeAt(before, fixture.Clock, Policy).Observations);
        var original = JsonSerializer.Serialize(memory.MarketKnowledgeAt(At, fixture.Clock, Policy));
        fixture.Clock.Now += TimeSpan.FromMinutes(1);
        var duplicate = await fixture.Run(token: TestContext.Current.CancellationToken); Assert.Contains("Duplicate", duplicate.Output);
        Assert.Equal(2, fixture.Calls);
        fixture.Close = 101; fixture.Clock.Now += TimeSpan.FromMinutes(1);
        var revised = await fixture.Run(token: TestContext.Current.CancellationToken); Assert.Contains("Revision", revised.Output);
        Assert.Equal(3, fixture.Calls); Assert.Equal(2L, fixture.Db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
        var last = fixture.Db.Memory().MarketKnowledgeAt(fixture.Clock.Now, fixture.Clock, Policy).Observations.Last();
        Assert.Equal(receipt.Id, last.CorrectsId); Assert.True(last.KnowledgeTimeUtc > receipt.KnowledgeTimeUtc);
        Assert.Equal(original, JsonSerializer.Serialize(fixture.Db.Memory().MarketKnowledgeAt(At, fixture.Clock, Policy)));
        foreach (var table in new[] { "prospective_daily_candidates", "prospective_daily_results", "backtest_runs", "shadow_predictions", "robustness_evaluations" })
            Assert.Equal(0L, fixture.Db.Scalar("SELECT COUNT(*) FROM " + table));
        Assert.False(FinanceObservationRuntimeOptions.FromConfiguration(fixture.Configuration).Enabled);
        Assert.Contains("New", fixture.FirstOutput);
        Assert.DoesNotContain("fixturekey", fixture.FirstOutput); Assert.DoesNotContain("fixturesecret", fixture.FirstOutput);
        Assert.DoesNotContain("bars", fixture.FirstOutput);
    }

    [Theory]
    [InlineData("http", "Rejected")]
    [InlineData("network", "Transport")]
    [InlineData("timeout", "Timeout")]
    [InlineData("malformed", "Rejected")]
    [InlineData("wrong-day", "Rejected")]
    [InlineData("wrong-symbol", "Rejected")]
    public async Task FailedProviderIsOneAttemptAndOutputIsSanitized(string failure, string category)
    {
        using var fixture = new Fixture();
        fixture.Send = (_, _) => failure switch
        {
            "http" => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("SECRET provider body") }),
            "network" => throw new HttpRequestException("SECRET credential URL"),
            "timeout" => throw new OperationCanceledException("SECRET timeout"),
            "wrong-day" => Task.FromResult(Response(Day.AddDays(1), "AAPL", 100)),
            "wrong-symbol" => Task.FromResult(Response(Day, "MSFT", 100)),
            _ => Task.FromResult(Json("{SECRET malformed"))
        };
        var result = await fixture.Run(token: TestContext.Current.CancellationToken); Assert.Equal(1, result.Code); Assert.Equal(1, fixture.Calls);
        Assert.Contains(category, result.Output); Assert.DoesNotContain("SECRET", result.Output);
        Assert.Equal(0L, fixture.Db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
    }

    [Fact]
    public async Task CancellationAndOverlappingCommandNeverRetryOrCommitPartialEvidence()
    {
        using var fixture = new Fixture(); using var stop = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Send = async (_, token) => { entered.SetResult(); await Task.Delay(Timeout.InfiniteTimeSpan, token); return Response(Day, "AAPL", 100); };
        var pending = fixture.Run(token: stop.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1, (await fixture.Run(token: TestContext.Current.CancellationToken)).Code); Assert.Equal(1, fixture.Calls);
        stop.Cancel(); Assert.Contains("Cancelled", (await pending).Output);
        Assert.Equal(0L, fixture.Db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
    }

    [Fact]
    public async Task CancellationBeforeCommitRollsBackAndUncertainCommitIsNotRetried()
    {
        using var fixture = new Fixture(); using var stop = new CancellationTokenSource();
        var cancelled = await fixture.Run(token: stop.Token, setup: m => m.ObservationTestHook = stage =>
        { if (stage == "before-observation-commit") stop.Cancel(); });
        Assert.Equal(1, cancelled.Code); Assert.Equal(1, fixture.Calls);
        Assert.Equal(0L, fixture.Db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
        var uncertain = await fixture.Run(token: TestContext.Current.CancellationToken, setup: m => m.ObservationTestHook = stage =>
        { if (stage == "after-observation-commit") throw new IOException("SECRET uncertain"); });
        Assert.Equal(1, uncertain.Code); Assert.Equal(2, fixture.Calls);
        Assert.DoesNotContain("SECRET", uncertain.Output);
        Assert.Equal(1L, fixture.Db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
        Assert.Single(fixture.Db.Memory().MarketKnowledgeAt(At, fixture.Clock, Policy).Observations);
    }

    [Theory]
    [InlineData("clock-reopen")]
    [InlineData("clock-during")]
    [InlineData("corrupt")]
    [InlineData("mapping-change")]
    [InlineData("watermark")]
    public async Task ClockAndProvenanceCannotCreateRewrittenEvidence(string failure)
    {
        using var fixture = new Fixture(); Assert.Equal(0, (await fixture.Run(token: TestContext.Current.CancellationToken)).Code);
        fixture.Clock.Now += TimeSpan.FromMinutes(1);
        switch (failure)
        {
            case "clock-reopen": fixture.Clock.Now = At.AddMinutes(-1); break;
            case "clock-during": fixture.Send = (_, _) => { fixture.Clock.Now = At.AddMinutes(-1); return Task.FromResult(Response(Day, "AAPL", 101)); }; break;
            case "corrupt": fixture.Db.Execute("UPDATE market_observation_receipts SET checksum='corrupt'"); break;
            case "mapping-change": var entry = Entry(); entry.MappingEvidence = "fixture:changed"; fixture.Entries(entry); break;
            case "watermark": fixture.Db.Execute("DELETE FROM market_observation_clock"); break;
        }
        var result = await fixture.Run(token: TestContext.Current.CancellationToken); Assert.Equal(1, result.Code);
        Assert.Equal(1L, fixture.Db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
        Assert.Equal(failure == "clock-during" ? 2 : 1, fixture.Calls);
    }

    [Theory]
    [InlineData("clock-marker")]
    [InlineData("lifecycle-db")]
    [InlineData("disk")]
    [InlineData("finance-db")]
    [InlineData("schema")]
    public async Task MaintenancePrerequisitesFailClosedBeforeNetwork(string failure)
    {
        using var fixture = new Fixture();
        switch (failure)
        {
            case "clock-marker": File.Delete(Path.Combine(fixture.Root, "synchronized")); break;
            case "lifecycle-db": fixture.Values["SystemRecovery:DatabasePath"] = Path.Combine(fixture.Root, "absent.db"); break;
            case "finance-db": fixture.Set("Eodhd:DatabasePath", Path.Combine(fixture.Root, "absent.db")); break;
            case "disk": fixture.Values["SystemRecovery:LowDiskCriticalBytes"] = long.MaxValue.ToString(CultureInfo.InvariantCulture); break;
            case "schema": fixture.Db.Execute("DELETE FROM finance_schema_migrations"); break;
        }
        Assert.Equal(1, (await fixture.Run(token: TestContext.Current.CancellationToken)).Code); Assert.Equal(0, fixture.Calls);
    }

    [Fact]
    public async Task MsftIsRepresentableWithoutDefaultUniverse()
    {
        using var fixture = new Fixture(); var entry = Entry(); entry.ProviderSymbol = "MSFT"; entry.InstrumentId = "US:XNAS:MSFT";
        fixture.Entries(entry); var args = Args(); args[1] = entry.InstrumentId;
        Assert.Equal(0, (await fixture.Run(args, token: TestContext.Current.CancellationToken)).Code); Assert.Equal(1, fixture.Calls);
        Assert.Empty(new FinanceObservationRuntimeOptions().Instruments);
        Assert.False(new FinanceObservationRuntimeOptions().Enabled);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("unknown")]
    [InlineData("oversized")]
    [InlineData("mixed")]
    public void ExternalMappingJsonCannotWeakenBoundedConfiguration(string failure)
    {
        using var fixture = new Fixture(); var text = JsonSerializer.Serialize(new[] { Entry() });
        fixture.Set("ObservationRuntime:InstrumentsJson", failure switch
        {
            "duplicate" => text.Replace("\"Mic\":\"XNAS\"", "\"Mic\":\"XNAS\",\"Mic\":\"XNYS\"", StringComparison.Ordinal),
            "unknown" => text.Replace("\"Mic\":\"XNAS\"", "\"Mic\":\"XNAS\",\"Capability\":\"shell\"", StringComparison.Ordinal),
            "oversized" => new string(' ', 16385),
            _ => text
        });
        if (failure == "mixed") fixture.Set("ObservationRuntime:Instruments:0:InstrumentId", Id);
        Assert.Empty(FinanceObservationRuntimeOptions.FromConfiguration(fixture.Configuration).Instruments);
    }

    [Theory]
    [InlineData(Revision, Revision)]
    [InlineData(null, "UNKNOWN")]
    [InlineData("", "UNKNOWN")]
    [InlineData("UNKNOWN", "UNKNOWN")]
    [InlineData("main", "UNKNOWN")]
    [InlineData("0000000000000000000000000000000000000000", "UNKNOWN")]
    [InlineData("SECRET-invalid-revision", "UNKNOWN")]
    public void BuildIdentityIsExactOrUnknown(string? supplied, string expected) => Assert.Equal(expected, BuildRevision.Normalize(supplied));

    [Fact]
    public async Task VersionedOneShotUsesSelectedSnapshotWithoutChangingMaintenanceAuthority()
    {
        using var fixture = new Fixture();
        var old = Entry(); old.ValidTo = Day.AddDays(-1);
        var current = Entry(); current.ValidFrom = Day; current.MappingEvidence = "fixture:next-mapping";
        ObservationMappingVersionOptions Version(ObservationRuntimeInstrument snapshot) => new()
        {
            Snapshot = snapshot,
            VerificationEvidence = "fixture:verified",
            VerifiedAtUtc = At.AddHours(-1),
            RevalidateByUtc = At.AddDays(1)
        };
        fixture.Entries(new ObservationRuntimeInstrument { InstrumentId = Id, MappingVersions = [Version(old), Version(current)] });
        Assert.Equal(0, (await fixture.Run(token: TestContext.Current.CancellationToken)).Code);
        Assert.Equal(1, fixture.Calls);
        var receipt = Assert.Single(fixture.Db.Memory().MarketKnowledgeAt(At, fixture.Clock, Policy).Observations);
        Assert.Equal(current.Canonical().Mapping, receipt.Mapping);
        Assert.Equal(1L, fixture.Db.Scalar("SELECT COUNT(*) FROM observation_mapping_receipts"));
        Assert.False(FinanceObservationRuntimeOptions.FromConfiguration(fixture.Configuration).Enabled);
    }

    private static string[] Args() => [FinanceObservationMaintenanceCommand.Name, Id, "2027-02-01"];
    private static ObservationRuntimeInstrument Entry() => new()
    {
        InstrumentId = Id,
        DisplayName = "Apple Inc.",
        ProviderSymbol = "AAPL",
        Mic = "XNAS",
        VenueCode = "NASDAQ",
        VenueName = "Nasdaq",
        ValidFrom = new(2020, 1, 1),
        MappingEvidence = "fixture:mapping-v1"
    };
    private sealed class Fixture : IDisposable
    {
        internal FinanceMarketObservationTests.Database Db { get; } = new();
        internal string Root => Path.GetDirectoryName(Db.Path)!;
        internal FinanceMarketObservationTests.Clock Clock { get; } = new(At);
        internal Dictionary<string, string?> Values { get; } = new(StringComparer.Ordinal);
        internal IConfiguration Configuration => new ConfigurationBuilder().AddInMemoryCollection(Values).Build();
        internal int Calls { get; private set; }
        internal decimal Close { get; set; } = 100;
        internal string? FirstOutput { get; private set; }
        internal Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? Send { get; set; }
        internal Fixture()
        {
            File.WriteAllText(Path.Combine(Root, "synchronized"), "");
            Values["SystemRecovery:ClockSyncDirectory"] = Root;
            Values["SystemRecovery:DatabasePath"] = Db.Path; // Temporary SQLite double, read only; no appliance lifecycle writes.
            Values["SystemRecovery:LowDiskCriticalBytes"] = "1";
            Set("Eodhd:DatabasePath", Db.Path); Set("Eodhd:PayloadDirectory", Path.Combine(Root, "payloads"));
            Set("ObservationRuntime:Enabled", "false"); Set("ObservationRuntime:OwnerAcceptanceVersion", AlpacaDailyOwnerDecision.Version);
            Set("ObservationRuntime:PolicyRecordedAtUtc", At.AddYears(-1).ToString("O", CultureInfo.InvariantCulture));
            Set("ObservationRuntime:AffectedUseEnabled", "true"); Set("AlpacaDailyObservation:Enabled", "true");
            Set("AlpacaDailyObservation:ApiKey", "fixturekey"); Set("AlpacaDailyObservation:ApiSecret", "fixturesecret");
            Entries(Entry());
        }
        internal void Set(string key, string value) => Values["Finance:" + key] = value;
        internal void Entries(params ObservationRuntimeInstrument[] entries) => Set("ObservationRuntime:InstrumentsJson", JsonSerializer.Serialize(entries));
        internal async Task<(int Code, string Output)> Run(string[]? args = null, string revision = Revision,
            Action<EodhdMarketMemory>? setup = null, CancellationToken token = default)
        {
            using var output = new StringWriter(CultureInfo.InvariantCulture);
            using var handler = new Handler(async (request, cancellation) =>
            {
                Calls++;
                Assert.Equal("data.alpaca.markets", request.RequestUri!.Host);
                Assert.Contains("timeframe=1Day", request.RequestUri.Query); Assert.Contains("feed=iex", request.RequestUri.Query);
                Assert.Contains("adjustment=raw", request.RequestUri.Query);
                return Send is null ? Response(Day, request.RequestUri.AbsolutePath.Split('/')[3], Close) : await Send(request, cancellation);
            });
            var code = await FinanceObservationMaintenanceCommand.ExecuteAsync(args ?? Args(), Configuration, revision, Clock,
                output, token, handler, setup);
            var result = output.ToString(); FirstOutput ??= result; return (code, result);
        }
        public void Dispose() => Db.Dispose();
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
    private static HttpResponseMessage Json(string text) => new(HttpStatusCode.OK) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
    private static HttpResponseMessage Response(DateOnly day, string symbol, decimal close) => Json(JsonSerializer.Serialize(new
    {
        symbol,
        next_page_token = (string?)null,
        bars = new[] { new { t = DailyMarketEvidence.DayStart(day).ToString("O", CultureInfo.InvariantCulture), o = close, h = close + 1, l = close - 1, c = close, v = 10000 } }
    }));
}
