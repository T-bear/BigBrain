using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using BigBrain.Api.Finance;
using BigBrain.Api.SystemRecovery;
using Microsoft.Extensions.Logging.Abstractions;
using BigBrain.Modules.Finance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BigBrain.Api.Tests;

#pragma warning disable CA1861 // Explicit bounded fixtures.
public sealed class FinanceObservationRuntimeTests
{
    // Tuesday, also prior Monday in New York. All transport is an HTTP double; never live.
    private static readonly DateTimeOffset At = new(2027, 2, 2, 12, 0, 0, TimeSpan.Zero);
    private static MarketDataEntitlementPolicy Policy => AlpacaDailyOwnerDecision.Create(At.AddYears(-1));
    private static AlpacaDailyObservationOptions Transport => new() { Enabled = true, ApiKey = "fixturekey", ApiSecret = "fixturesecret" };
    private static ObservationRuntimeInstrument Entry(string symbol = "AAPL")
    {
        return new()
        {
            InstrumentId = "US:" + symbol + ":XNAS",
            DisplayName = symbol,
            ProviderSymbol = symbol,
            Mic = "XNAS",
            VenueCode = "NASDAQ",
            VenueName = "NASDAQ",
            ValidFrom = new(2000, 1, 1),
            MappingEvidence = "fixture:effective-mapping"
        };
    }

    private static FinanceObservationRuntimeOptions Options() => new()
    {
        Enabled = true,
        CadenceMinutes = 60,
        LookbackDays = 1,
        OwnerAcceptanceVersion = AlpacaDailyOwnerDecision.Version,
        PolicyRecordedAtUtc = At.AddYears(-1),
        AffectedUseEnabled = true,
        Instruments = [Entry()]
    };

    [Theory]
    [InlineData("disabled")]
    [InlineData("no-key")]
    [InlineData("bad-key")]
    [InlineData("no-secret")]
    [InlineData("bad-secret")]
    [InlineData("transport-disabled")]
    [InlineData("empty")]
    [InlineData("too-many")]
    [InlineData("duplicate")]
    [InlineData("cadence")]
    [InlineData("lookback")]
    [InlineData("timeout")]
    [InlineData("policy")]
    [InlineData("deletion")]
    [InlineData("revoked")]
    public async Task DisabledOrInvalidConfigurationCannotMakeRequests(string kind)
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        var options = Options(); var transport = Transport;
        switch (kind)
        {
            case "disabled": options = new(); break;
            case "no-key": transport.ApiKey = ""; break;
            case "bad-key": transport.ApiKey = "secret\r\nInjected"; break;
            case "no-secret": transport.ApiSecret = ""; break;
            case "bad-secret": transport.ApiSecret = "do-not-log!"; break;
            case "transport-disabled": transport.Enabled = false; break;
            case "empty": options.Instruments = []; break;
            case "too-many": options.Instruments = Enumerable.Range(0, 5).Select(i => Entry("X" + i)).ToArray(); break;
            case "duplicate": options.Instruments = [Entry(), Entry()]; break;
            case "cadence": options.CadenceMinutes = 1; break;
            case "lookback": options.LookbackDays = 8; break;
            case "timeout": transport.TimeoutSeconds = 31; break;
            case "policy": options.OwnerAcceptanceVersion = "unknown"; break;
            case "deletion": options.CurrentDeletion = DeletionRequirement.DeleteAtSubscriptionEnd; break;
            case "revoked": options.AffectedUseEnabled = false; break;
        }
        var calls = 0;
        var runtime = new FinanceObservationRuntime(options, transport, db.Memory(), clock, () => true,
            _ => { calls++; throw new InvalidOperationException(); });
        clock.Now += TimeSpan.FromHours(2);
        var result = await runtime.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(kind == "disabled" ? ObservationRuntimeState.Disabled : ObservationRuntimeState.Misconfigured, result.State);
        Assert.Equal(0, calls); Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
        Assert.DoesNotContain(transport.ApiKey.Length > 0 ? transport.ApiKey : "no-secret-placeholder", JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task RealAdapterOwnerPathReopensDuplicatesRevisesAndPreservesSealedHistory()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        var close = 100; var calls = 0;
        FinanceObservationRuntime Open() => Runtime(db.Memory(), clock, Options(), (day, request, token) =>
        { calls++; return Task.FromResult(Response(day, request, close)); });
        var runtime = Open();
        Assert.Equal(ObservationRuntimeState.Waiting, (await runtime.RunCycleAsync(TestContext.Current.CancellationToken)).State);
        Assert.Equal(0, calls);
        clock.Now += TimeSpan.FromHours(1);
        var first = await runtime.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ObservationAcquisitionKind.New, Assert.Single(first.Attempts).Outcome);
        var cutoff = clock.Now; var before = db.Memory().MarketKnowledgeAt(cutoff, clock, Policy);
        var original = Assert.Single(before.Observations);
        Assert.Equal(MarketObservationOrigin.DeterministicFixture, original.Origin);
        Assert.Equal(cutoff, original.KnowledgeTimeUtc);
        runtime = Open(); clock.Now += TimeSpan.FromHours(1);
        Assert.Equal(ObservationAcquisitionKind.Duplicate, Assert.Single((await runtime.RunCycleAsync(TestContext.Current.CancellationToken)).Attempts).Outcome);
        close = 101; clock.Now += TimeSpan.FromHours(1);
        Assert.Equal(ObservationAcquisitionKind.Revision, Assert.Single((await runtime.RunCycleAsync(TestContext.Current.CancellationToken)).Attempts).Outcome);
        var revised = db.Memory().MarketKnowledgeAt(clock.Now, clock, Policy).Observations.Last();
        Assert.Equal(original.Id, revised.CorrectsId);
        runtime = Open(); clock.Now += TimeSpan.FromHours(1);
        Assert.Equal(ObservationAcquisitionKind.Duplicate, Assert.Single((await runtime.RunCycleAsync(TestContext.Current.CancellationToken)).Attempts).Outcome);
        Assert.Equal(2L, db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(db.Memory().MarketKnowledgeAt(cutoff, clock, Policy)));
        // Reversion to the original price is a new revision, never a return to stale old authority/time.
        close = 100; clock.Now += TimeSpan.FromHours(1);
        await runtime.RunCycleAsync(TestContext.Current.CancellationToken);
        var reverted = db.Memory().MarketKnowledgeAt(clock.Now, clock, Policy).Observations.Last();
        Assert.Equal(revised.Id, reverted.CorrectsId); Assert.True(reverted.KnowledgeTimeUtc > revised.KnowledgeTimeUtc);
        foreach (var table in new[] { "prospective_daily_candidates", "prospective_daily_results", "backtest_runs", "shadow_predictions" })
            Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM " + table));
    }

    [Theory]
    [InlineData("http", "Rejected")]
    [InlineData("malformed", "Rejected")]
    [InlineData("transport", "Transport")]
    [InlineData("timeout", "Timeout")]
    [InlineData("current", "Rejected")]
    [InlineData("symbol", "Rejected")]
    [InlineData("oversized", "Rejected")]
    public async Task ProviderFailureCreatesNoEvidenceAndDoesNotRetry(string kind, string failure)
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At); var calls = 0;
        var runtime = Runtime(db.Memory(), clock, Options(), (day, request, token) =>
        {
            calls++;
            return kind switch
            {
                "transport" => throw new HttpRequestException("SECRET must not escape"),
                "timeout" => throw new OperationCanceledException("SECRET deadline"),
                "http" => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)),
                "malformed" => Task.FromResult(Json("{SECRET")),
                "current" => Task.FromResult(Response(DailyMarketEvidence.SourceDate(clock.Now), request, 100)),
                "symbol" => Task.FromResult(Json("{\"symbol\":\"MSFT\",\"bars\":[],\"next_page_token\":null}")),
                "oversized" => Task.FromResult(Json(new string('x', 65537))),
                _ => throw new InvalidOperationException()
            };
        });
        clock.Now += TimeSpan.FromHours(1);
        var result = await runtime.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ObservationRuntimeState.Degraded, result.State); Assert.Equal(failure, Assert.Single(result.Attempts).Failure.ToString());
        await runtime.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, calls); Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
        Assert.DoesNotContain("SECRET", JsonSerializer.Serialize(result));
        Assert.Equal(HealthStatus.Degraded, (await runtime.CheckHealthAsync(new(), TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task OneInstrumentFailureCannotManufactureAnotherAndWorkIsBounded()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        var options = Options(); options.Instruments = [Entry(), Entry("MSFT")];
        var calls = 0; var delays = 0;
        var runtime = Runtime(db.Memory(), clock, options, (day, request, token) =>
        {
            calls++;
            return Task.FromResult(request.RequestUri!.AbsolutePath.Contains("AAPL", StringComparison.Ordinal) ? Json("invalid") : Response(day, request, 100));
        }, delay: (duration, token) => { Assert.Equal(TimeSpan.FromSeconds(5), duration); delays++; return Task.CompletedTask; });
        clock.Now += TimeSpan.FromHours(1);
        var result = await runtime.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, calls); Assert.Equal(1, delays); Assert.Equal(2, result.Attempts.Length);
        Assert.Equal("US:MSFT:XNAS", Assert.Single(db.Memory().MarketKnowledgeAt(clock.Now, clock, Policy).Observations).Instrument.Id.Value);
    }

    [Fact]
    public async Task CancellationAndOverlappingCyclesRemainSingleFlightWithNoPartialReceipt()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        using var stop = new CancellationTokenSource(); var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var calls = 0;
        var runtime = Runtime(db.Memory(), clock, Options(), async (day, request, token) =>
        {
            calls++; entered.SetResult(); await Task.Delay(Timeout.InfiniteTimeSpan, token); return Response(day, request, 100);
        });
        clock.Now += TimeSpan.FromHours(1);
        var running = runtime.RunCycleAsync(stop.Token); await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(ObservationRuntimeState.Running, (await runtime.RunCycleAsync(TestContext.Current.CancellationToken)).State);
        stop.Cancel(); var result = await running;
        Assert.Equal(ObservationRuntimeState.Cancelled, result.State); Assert.Equal(ObservationFailure.Cancelled, result.Failure);
        Assert.Equal(1, calls); Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
    }

    [Theory]
    [InlineData("clock-before")]
    [InlineData("clock-during")]
    [InlineData("clock-reopen")]
    [InlineData("corrupt")]
    [InlineData("missing-table")]
    public async Task ClockAndEvidenceFailuresStopWithoutNewKnowledge(string kind)
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At); var calls = 0;
        FinanceObservationRuntime Open() => Runtime(db.Memory(), clock, Options(), (day, request, token) =>
        { calls++; if (kind == "clock-during") clock.Now -= TimeSpan.FromHours(2); return Task.FromResult(Response(day, request, 100)); });
        var runtime = Open();
        if (kind is "clock-reopen" or "corrupt")
        {
            clock.Now += TimeSpan.FromHours(1); await runtime.RunCycleAsync(TestContext.Current.CancellationToken);
            if (kind == "corrupt") db.Execute("UPDATE market_observation_receipts SET checksum='corrupt'");
            else { clock.Now -= TimeSpan.FromHours(3); runtime = Open(); }
        }
        if (kind == "missing-table") db.Execute("DROP TABLE market_observation_clock");
        if (kind == "clock-before") clock.Now -= TimeSpan.FromSeconds(1); else clock.Now += TimeSpan.FromHours(1);
        var previous = (long)db.Scalar("SELECT COUNT(*) FROM market_observation_receipts")!;
        var result = await runtime.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Contains(result.State, new[] { ObservationRuntimeState.ClockBlocked, ObservationRuntimeState.Degraded });
        Assert.Equal(previous, db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
        if (kind.StartsWith("clock", StringComparison.Ordinal)) Assert.Equal(ObservationFailure.Clock, result.Failure);
    }

    [Fact]
    public async Task ConfigurationBindingUsesExistingCanonicalMappingsAndHealthIsSanitized()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        using var json = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(Options()));
        var config = new ConfigurationBuilder().AddJsonStream(json).Build();
        var bound = config.Get<FinanceObservationRuntimeOptions>()!;
        var runtime = Runtime(db.Memory(), clock, bound, (day, request, token) => Task.FromResult(Response(day, request, 100)));
        Assert.Equal(ObservationRuntimeState.Waiting, runtime.Snapshot.State);
        clock.Now += TimeSpan.FromHours(1);
        Assert.Equal(ObservationRuntimeState.Healthy, (await runtime.RunCycleAsync(TestContext.Current.CancellationToken)).State);
        var health = await runtime.CheckHealthAsync(new(), TestContext.Current.CancellationToken);
        Assert.Equal(HealthStatus.Healthy, health.Status);
        var diagnostic = JsonSerializer.Serialize(health.Data);
        Assert.DoesNotContain("fixturekey", diagnostic); Assert.DoesNotContain("fixturesecret", diagnostic);
        Assert.DoesNotContain("bars", diagnostic); Assert.DoesNotContain("ApiKey", diagnostic);
    }

    [Theory]
    [InlineData("2027-03-15T00:30:00Z")] // Still Sunday in New York; DST already active.
    [InlineData("2027-11-08T00:30:00Z")] // Still Sunday; standard time again.
    public async Task WorkPlanUsesPriorNewYorkDatesAndBoundedCalendarWindow(string instant)
    {
        using var db = new FinanceMarketObservationTests.Database();
        var now = DateTimeOffset.Parse(instant, CultureInfo.InvariantCulture);
        var clock = new FinanceMarketObservationTests.Clock(now.AddHours(-1));
        var options = Options(); options.LookbackDays = 7; options.Instruments = [Entry(), Entry("MSFT"), Entry("IBM"), Entry("XOM")];
        var days = new List<DateOnly>(); var delays = 0;
        var runtime = Runtime(db.Memory(), clock, options, (day, request, token) =>
        { days.Add(day); clock.Now = clock.Now.AddTicks(1); return Task.FromResult(Response(day, request, 100)); },
            (duration, token) => { delays++; return Task.CompletedTask; });
        clock.Now = now;
        var result = await runtime.RunCycleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ObservationRuntimeState.Healthy, result.State);
        Assert.Equal(7, result.SourceDays.Length);
        Assert.InRange(days.Count, 1, 28); Assert.Equal(days.Count - 1, delays);
        Assert.All(days, day => { Assert.True(day < DailyMarketEvidence.SourceDate(now)); Assert.True(day >= DailyMarketEvidence.SourceDate(now).AddDays(-7)); });
        Assert.Equal(days.Count, result.Attempts.Length);
        Assert.Equal(days.Count, Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"), CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task RecoveryGatePreventsNetworkAndCancellationBeforeCommitRollsBack()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        var calls = 0;
        var blocked = new FinanceObservationRuntime(Options(), Transport, db.Memory(), clock, () => false,
            _ => { calls++; throw new InvalidOperationException(); });
        clock.Now += TimeSpan.FromHours(1);
        Assert.Equal(ObservationRuntimeState.RecoveryBlocked, (await blocked.RunCycleAsync(TestContext.Current.CancellationToken)).State);
        Assert.Equal(0, calls);
        using var stop = new CancellationTokenSource(); var memory = db.Memory();
        memory.ObservationTestHook = stage => { if (stage == "before-observation-commit") stop.Cancel(); };
        var runtime = Runtime(memory, clock, Options(), (day, request, token) => Task.FromResult(Response(day, request, 100)));
        clock.Now += TimeSpan.FromHours(1);
        Assert.Equal(ObservationRuntimeState.Cancelled, (await runtime.RunCycleAsync(stop.Token)).State);
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
    }

    [Fact]
    public async Task MappingProvenanceCannotChangeThroughReacquisition()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        var options = Options();
        var runtime = Runtime(db.Memory(), clock, options, (day, request, token) => Task.FromResult(Response(day, request, 100)));
        clock.Now += TimeSpan.FromHours(1); await runtime.RunCycleAsync(TestContext.Current.CancellationToken);
        options.Instruments[0].MappingEvidence = "fixture:different-provenance";
        runtime = Runtime(db.Memory(), clock, options, (day, request, token) => Task.FromResult(Response(day, request, 101)));
        clock.Now += TimeSpan.FromHours(1);
        Assert.Equal(ObservationRuntimeState.Degraded, (await runtime.RunCycleAsync(TestContext.Current.CancellationToken)).State);
        Assert.Equal(1L, db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
    }

    [Fact]
    public async Task HostedWorkerWaitsForCadenceAndShutdownCancelsOwnedAcquisition()
    {
        using var db = new FinanceMarketObservationTests.Database(); using var clock = new WorkerClock(At);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var calls = 0;
        var runtime = Runtime(db.Memory(), clock, Options(), async (day, request, token) =>
        { calls++; entered.TrySetResult(); await Task.Delay(Timeout.InfiniteTimeSpan, token); return Response(day, request, 100); });
        var root = Path.Combine(Path.GetTempPath(), "bb132f-recovery-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            using var recovery = new SystemRecoveryCoordinator(new()
            {
                DatabasePath = Path.Combine(root, "recovery.db"),
                ClockSyncDirectory = root,
                LowDiskWarningBytes = 0,
                LowDiskCriticalBytes = 0
            }, NullLogger<SystemRecoveryCoordinator>.Instance);
            await recovery.StartAsync(TestContext.Current.CancellationToken);
            using var worker = new FinanceObservationWorker(runtime, recovery, clock, NullLogger<FinanceObservationWorker>.Instance);
            await worker.StartAsync(TestContext.Current.CancellationToken);
            await clock.TimerCreated.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal(0, calls); clock.Advance(TimeSpan.FromHours(1));
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal(1, calls); Assert.Equal(ObservationRuntimeState.Cancelled, runtime.Snapshot.State);
            Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
            await recovery.StopAsync(TestContext.Current.CancellationToken);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task HungProviderHitsActualAdapterDeadlineWithoutRetry()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        var transport = Transport; transport.TimeoutSeconds = 1; var calls = 0;
        var runtime = new FinanceObservationRuntime(Options(), transport, db.Memory(), clock, () => true,
            day => new(transport, day, new Handler(async (request, token) =>
            { calls++; await Task.Delay(Timeout.InfiniteTimeSpan, token); return Response(day, request, 100); })));
        clock.Now += TimeSpan.FromHours(1);
        var result = await runtime.RunCycleAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(ObservationFailure.Timeout, Assert.Single(result.Attempts).Failure);
        Assert.Equal(1, calls); Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
    }

    [Fact]
    public async Task ConcurrentOwnersConvergeAndUncertainCommittedReceiptIsNotRewrittenOnRestart()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        var both = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var calls = 0;
        async Task<HttpResponseMessage> Send(DateOnly day, HttpRequestMessage request, CancellationToken token)
        {
            if (Interlocked.Increment(ref calls) == 2) both.TrySetResult();
            await both.Task.WaitAsync(TimeSpan.FromSeconds(5), token); return Response(day, request, 100);
        }
        var a = Runtime(db.Memory(), clock, Options(), Send); var b = Runtime(db.Memory(), clock, Options(), Send);
        clock.Now += TimeSpan.FromHours(1);
        var results = await Task.WhenAll(a.RunCycleAsync(TestContext.Current.CancellationToken), b.RunCycleAsync(TestContext.Current.CancellationToken));
        Assert.Contains(results, x => Assert.Single(x.Attempts).Outcome == ObservationAcquisitionKind.New);
        Assert.Contains(results, x => Assert.Single(x.Attempts).Outcome == ObservationAcquisitionKind.Duplicate);
        Assert.Equal(1L, db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
        var memory = db.Memory(); memory.ObservationTestHook = stage => { if (stage == "after-observation-commit") throw new IOException("fixture-crash"); };
        var uncertain = Runtime(memory, clock, Options(), (day, request, token) => Task.FromResult(Response(day, request, 101)));
        clock.Now += TimeSpan.FromHours(1);
        Assert.Equal(ObservationRuntimeState.Degraded, (await uncertain.RunCycleAsync(TestContext.Current.CancellationToken)).State);
        var cutoff = clock.Now; var committed = db.Memory().MarketKnowledgeAt(cutoff, clock, Policy);
        var reopened = Runtime(db.Memory(), clock, Options(), (day, request, token) => Task.FromResult(Response(day, request, 101)));
        clock.Now += TimeSpan.FromHours(1);
        Assert.Equal(ObservationAcquisitionKind.Duplicate, Assert.Single((await reopened.RunCycleAsync(TestContext.Current.CancellationToken)).Attempts).Outcome);
        Assert.Equal(JsonSerializer.Serialize(committed), JsonSerializer.Serialize(db.Memory().MarketKnowledgeAt(cutoff, clock, Policy)));
    }

    private sealed class WorkerClock(DateTimeOffset now) : TimeProvider, IDisposable
    {
        private readonly object _gate = new();
        private DateTimeOffset _now = now;
        private TestTimer? _timer;
        internal TaskCompletionSource TimerCreated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override DateTimeOffset GetUtcNow() { lock (_gate) return _now; }
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Assert.Equal(TimeSpan.FromHours(1), dueTime); Assert.Equal(Timeout.InfiniteTimeSpan, period);
            lock (_gate) _timer = new(callback, state);
            TimerCreated.TrySetResult(); return _timer;
        }
        public void Dispose() { lock (_gate) _timer?.Dispose(); }
        internal void Advance(TimeSpan by)
        {
            TestTimer? timer;
            lock (_gate) { _now += by; timer = _timer; }
            timer?.Fire();
        }
        private sealed class TestTimer(TimerCallback callback, object? state) : ITimer
        {
            private int _disposed;
            internal void Fire() { if (Interlocked.Exchange(ref _disposed, 1) == 0) callback(state); }
            public bool Change(TimeSpan dueTime, TimeSpan period) => false;
            public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }

    private static FinanceObservationRuntime Runtime(EodhdMarketMemory memory, TimeProvider clock, FinanceObservationRuntimeOptions options,
        Func<DateOnly, HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send,
        Func<TimeSpan, CancellationToken, Task>? delay = null) => new(options, Transport, memory, clock, () => true,
            day => new(Transport, day, new Handler((request, token) => send(day, request, token))), delay ?? ((_, _) => Task.CompletedTask));
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
    private static HttpResponseMessage Json(string text) => new(HttpStatusCode.OK) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
    private static HttpResponseMessage Response(DateOnly day, HttpRequestMessage request, decimal close)
    {
        Assert.Equal("https", request.RequestUri!.Scheme); Assert.Equal("data.alpaca.markets", request.RequestUri.Host);
        Assert.Contains("feed=iex", request.RequestUri.Query); Assert.Contains("adjustment=raw", request.RequestUri.Query);
        Assert.DoesNotContain("fixturekey", request.RequestUri.ToString());
        var symbol = request.RequestUri.AbsolutePath.Split('/')[3];
        return Json(JsonSerializer.Serialize(new
        {
            symbol,
            next_page_token = (string?)null,
            bars = new[] { new { t = DailyMarketEvidence.DayStart(day).ToString("O", CultureInfo.InvariantCulture), o = close, h = close + 1, l = close - 1, c = close, v = 10000 } }
        }));
    }
}
