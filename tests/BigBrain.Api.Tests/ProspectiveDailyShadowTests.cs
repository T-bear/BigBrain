using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Tests;

#pragma warning disable CA1861 // Bounded explicit fixtures.
public sealed class ProspectiveDailyShadowTests
{
    private static readonly DateTimeOffset At = new(2027, 2, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly ProviderInstrumentMapping Mapping = new(FinanceMarketObservationTests.Instrument.Id,
        new("Alpaca"), new(DailyMarketEvidence.Dataset), "AAPL", FinanceMarketObservationTests.Instrument.Venue,
        "XNAS", new(2000, 1, 1), null, new("fixture:alpaca-effective-mapping"));
    private static MarketDataEntitlementPolicy Policy => AlpacaDailyOwnerDecision.Create(At.AddYears(-1));

    [Fact]
    public async Task FreezeFutureObservationSealedResultReopensAndCannotBeContaminated()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        var run = await Seed(db, clock);
        var candidate = db.Memory().FreezeDailyShadow(run, Mapping, Policy, clock);
        Assert.Equal(clock.Now, candidate.FrozenAtUtc); Assert.Equal(candidate.FrozenAtUtc, candidate.KnowledgeCutoffUtc);
        Assert.Equal(ResearchIntentKind.TargetLong, candidate.Signal);
        Assert.Equal(JsonSerializer.Serialize(candidate), JsonSerializer.Serialize(db.Memory().FreezeDailyShadow(run, Mapping, Policy, clock)));
        var empty = db.Memory().EvaluateDailyShadow(candidate.Id, clock.Now, Policy, clock);
        Assert.Equal("INSUFFICIENT_DATA", empty.State);
        clock.Now = clock.Now.AddMinutes(1);
        // Source day is before freeze wall time, but was unknown to Finance then.
        var future = await Acquire(db, clock, new(2027, 1, 28), 110);
        Assert.True(future.Value.EventTimeUtc < candidate.FrozenAtUtc);
        Assert.True(future.KnowledgeTimeUtc > candidate.FrozenAtUtc);
        var cutoff = clock.Now;
        var result = db.Memory().EvaluateDailyShadow(candidate.Id, cutoff, Policy, clock);
        Assert.Equal("OBSERVED", result.State); Assert.Equal(future.Id, result.OutcomeReceiptId);
        Assert.Equal(110m / candidate.ReferenceClose - 1, result.ObservedCloseReturn);
        Assert.Equal("CORRECT", result.DirectionResult);
        var projection = db.Memory().MarketKnowledgeAt(cutoff, clock, Policy);
        clock.Now = clock.Now.AddMinutes(1);
        await Acquire(db, clock, new(2027, 1, 28), 80, future.Id);
        clock.Now = clock.Now.AddMinutes(1);
        await Acquire(db, clock, new(2027, 1, 29), 70);
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(db.Memory().EvaluateDailyShadow(candidate.Id, cutoff, Policy, clock)));
        Assert.Equal(JsonSerializer.Serialize(projection), JsonSerializer.Serialize(db.Memory().MarketKnowledgeAt(cutoff, clock, Policy)));
        Assert.Equal(empty, db.Memory().EvaluateDailyShadow(candidate.Id, candidate.FrozenAtUtc, Policy, clock));
        Assert.Equal(JsonSerializer.Serialize(candidate), JsonSerializer.Serialize(db.Memory().FreezeDailyShadow(run, Mapping, Policy, clock)));
        Assert.Equal(1L, db.Scalar("SELECT COUNT(*) FROM prospective_daily_candidates"));
        Assert.Equal(1L, db.Scalar("SELECT COUNT(*) FROM backtest_runs"));
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM shadow_predictions"));
    }

    [Fact]
    public async Task DuplicateRevisionAndKnowledgeRulesPreserveEvidence()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        var receipt = await Acquire(db, clock, new(2027, 1, 25), 100);
        Assert.Equal(DailyMarketEvidence.Contract, receipt.Version); Assert.Equal(LiveObservationGranularity.Daily, receipt.Granularity);
        Assert.Equal("XNAS", receipt.Mapping.Mic); Assert.Equal(DailyMarketEvidence.Dataset, receipt.Dataset.Value);
        Assert.Null(receipt.Value.ProviderAvailableAtUtc);
        Assert.Empty(db.Memory().MarketKnowledgeAt(At.AddTicks(-1), clock, Policy).Observations);
        clock.Now = clock.Now.AddMinutes(1);
        Assert.Equal(receipt, await Acquire(db, clock, new(2027, 1, 25), 100));
        await Assert.ThrowsAsync<InvalidDataException>(() => Acquire(db, clock, new(2027, 1, 25), 101));
        var revision = await Acquire(db, clock, new(2027, 1, 25), 101, receipt.Id);
        Assert.Equal(receipt.Id, revision.CorrectsId); Assert.True(revision.KnowledgeTimeUtc > receipt.KnowledgeTimeUtc);
        Assert.Equal(receipt, Assert.Single(db.Memory().MarketKnowledgeAt(At, clock, Policy).Observations));
        Assert.Equal(revision, await Acquire(db, clock, new(2027, 1, 25), 101, receipt.Id));
        Assert.Throws<InvalidDataException>(() => db.Memory().MarketKnowledgeAt(clock.Now.AddTicks(1), clock, Policy));
        clock.Now = At.AddTicks(-1);
        await Assert.ThrowsAsync<InvalidDataException>(() => Acquire(db, clock, new(2027, 1, 26), 102));
    }

    [Theory]
    [InlineData("current")]
    [InlineData("future")]
    [InlineData("midday")]
    [InlineData("publication")]
    [InlineData("feed")]
    [InlineData("missing-daily")]
    [InlineData("ohlc")]
    public async Task InvalidDailyEvidenceIsNotPersisted(string failure)
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        var day = failure == "current" ? new DateOnly(2027, 2, 1) : failure == "future" ? new(2027, 2, 2) : new(2027, 1, 25);
        var value = Value(day, 100);
        value = failure switch
        {
            "midday" => value with { EventTimeUtc = value.EventTimeUtc.AddHours(1) },
            "publication" => value with { ProviderAvailableAtUtc = At.AddDays(-1) },
            "feed" => value with { Daily = value.Daily! with { SourceContract = "sip-pretending-to-be-iex" } },
            "missing-daily" => value with { Daily = null },
            "ohlc" => value with { High = 1 },
            _ => value
        };
        await Assert.ThrowsAnyAsync<Exception>(() => db.Memory().AcquireMarketObservationAsync(new Source(value),
            FinanceMarketObservationTests.Instrument, Mapping, Policy, clock, CancellationToken.None));
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
    }

    [Fact]
    public async Task KnownDeletionOrRevokedUseStopsAcquisitionAndFurtherShadowUse()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        var run = await Seed(db, clock); var c = db.Memory().FreezeDailyShadow(run, Mapping, Policy, clock);
        Assert.Equal(DeletionRequirement.Unknown, Policy.Deletion);
        Assert.Equal(EntitlementEvidenceClass.OwnerAcceptedPersonalResearch, Policy.EvidenceClass);
        foreach (var blocked in new[] { AlpacaDailyOwnerDecision.Create(At.AddYears(-1), DeletionRequirement.DeleteAtSubscriptionEnd),
            AlpacaDailyOwnerDecision.Create(At.AddYears(-1), DeletionRequirement.DeleteByDeadline),
            AlpacaDailyOwnerDecision.Create(At.AddYears(-1), affectedUseEnabled: false) })
        {
            Assert.Throws<InvalidDataException>(() => db.Memory().EvaluateDailyShadow(c.Id, clock.Now, blocked, clock));
            Assert.Throws<InvalidDataException>(() => db.Memory().MarketKnowledgeAt(clock.Now, clock, blocked));
            var source = new Source(Value(new(2027, 1, 28), 100));
            await Assert.ThrowsAsync<InvalidDataException>(() => db.Memory().AcquireMarketObservationAsync(source,
                FinanceMarketObservationTests.Instrument, Mapping, blocked, clock, CancellationToken.None));
            Assert.Equal(0, source.Calls);
        }
    }

    [Theory]
    [InlineData("candidate")]
    [InlineData("observation")]
    [InlineData("result")]
    [InlineData("research")]
    [InlineData("missing")]
    public async Task CorruptedOrMissingDependenciesFailClosed(string target)
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        var run = await Seed(db, clock); var c = db.Memory().FreezeDailyShadow(run, Mapping, Policy, clock);
        db.Memory().EvaluateDailyShadow(c.Id, clock.Now, Policy, clock);
        db.Execute(target switch
        {
            "candidate" => "UPDATE prospective_daily_candidates SET checksum='corrupt'",
            "observation" => "UPDATE market_observation_receipts SET checksum='corrupt'",
            "result" => "UPDATE prospective_daily_results SET checksum='corrupt'",
            "research" => "UPDATE backtest_runs SET checksum='corrupt'",
            _ => "DELETE FROM market_observation_receipts"
        });
        Assert.Throws<InvalidDataException>(() => db.Memory().EvaluateDailyShadow(c.Id, clock.Now, Policy, clock));
    }

    [Fact]
    public async Task ConcurrentFreezeEvaluationAndCrashRecoveryAreIdempotent()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        var run = await Seed(db, clock);
        var crash = db.Memory(); crash.DailyShadowTestHook = _ => throw new IOException("fixture crash");
        Assert.Throws<IOException>(() => crash.FreezeDailyShadow(run, Mapping, Policy, clock));
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM prospective_daily_candidates"));
        var candidates = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() => db.Memory().FreezeDailyShadow(run, Mapping, Policy, clock))));
        Assert.Equal(JsonSerializer.Serialize(candidates[0]), JsonSerializer.Serialize(candidates[1]));
        Assert.Throws<IOException>(() => crash.EvaluateDailyShadow(candidates[0].Id, clock.Now, Policy, clock));
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM prospective_daily_results"));
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() => db.Memory().EvaluateDailyShadow(candidates[0].Id, clock.Now, Policy, clock))));
        Assert.Equal(results[0], results[1]);
        Assert.Equal(1L, db.Scalar("SELECT COUNT(*) FROM prospective_daily_results"));
        Assert.Equal(97L, db.Scalar("SELECT MAX(version) FROM finance_schema_migrations"));
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("duplicate")]
    [InlineData("pagination")]
    [InlineData("missing")]
    [InlineData("oversized")]
    [InlineData("wrong-symbol")]
    [InlineData("wrong-day")]
    [InlineData("unauthorized")]
    public async Task AdapterUsesFixedDailyFeedAndBoundsUntrustedResponse(string kind)
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        var transport = new Transport(kind);
        using var source = new AlpacaDailyMarketObservations(new() { Enabled = true, ApiKey = "fixture", ApiSecret = "fixture" }, new(2027, 1, 25), transport);
        var action = () => db.Memory().AcquireMarketObservationAsync(source, FinanceMarketObservationTests.Instrument, Mapping, Policy, clock, CancellationToken.None);
        if (kind == "valid")
        {
            var receipt = await action(); Assert.Equal(MarketObservationOrigin.DeterministicFixture, receipt.Origin);
            Assert.Equal(100m, receipt.Value.Close); Assert.Equal(receipt, Assert.Single(db.Memory().MarketKnowledgeAt(clock.Now, clock, Policy).Observations));
        }
        else { await Assert.ThrowsAnyAsync<Exception>(action); Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM market_observation_receipts")); }
        Assert.Equal(1, transport.Calls);
    }

    [Fact]
    public async Task DisabledAdapterDoesNotCallTransport()
    {
        var transport = new Transport("valid"); using var source = new AlpacaDailyMarketObservations(new(), new(2027, 1, 25), transport);
        await Assert.ThrowsAsync<InvalidOperationException>(() => source.ReadAsync(Mapping, CancellationToken.None));
        Assert.Equal(0, transport.Calls);
    }

    [Theory]
    [InlineData(2026, 3, 8, 5)]
    [InlineData(2026, 3, 9, 4)]
    [InlineData(2026, 11, 1, 4)]
    [InlineData(2026, 11, 2, 5)]
    public void DailyBoundariesRespectNewYorkDst(int year, int month, int day, int utcHour)
    {
        var date = new DateOnly(year, month, day);
        Assert.Equal(new DateTimeOffset(year, month, day, utcHour, 0, 0, TimeSpan.Zero), DailyMarketEvidence.DayStart(date));
        Assert.Equal(date, DailyMarketEvidence.SourceDate(DailyMarketEvidence.DayStart(date)));
    }

    [Fact]
    public void AdditiveMigrationRollbackDoesNotReopenSpentAuthority()
    {
        using var db = new FinanceMarketObservationTests.Database(false);
        Assert.Throws<IOException>(() => FinanceSchemaMigrator.Migrate(db.Path, version =>
        { if (version == 97) throw new IOException("fixture crash"); }));
        Assert.Equal(96, FinanceSchemaMigrator.State(db.Path).CurrentVersion);
        Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM sqlite_master WHERE name='prospective_daily_candidates'"));
        FinanceSchemaMigrator.Migrate(db.Path);
        var memory = db.Memory(); memory.EnrollSyntheticLearning(ResearchLearningFixture.Scope(), LearningExposure.Unexposed);
        Assert.True(memory.ReserveLearningInvocation());
        FinanceSchemaMigrator.Migrate(db.Path);
        Assert.False(db.Memory().ReserveLearningInvocation());
        Assert.Equal(97, FinanceSchemaMigrator.State(db.Path).CurrentVersion);
    }

    [Fact]
    public async Task ClockFutureCutoffAndMissingSupportingResearchFailClosed()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        Assert.Throws<InvalidDataException>(() => db.Memory().FreezeDailyShadow("not-persisted", Mapping, Policy, clock));
        var run = await Seed(db, clock); var c = db.Memory().FreezeDailyShadow(run, Mapping, Policy, clock);
        Assert.Throws<InvalidDataException>(() => db.Memory().EvaluateDailyShadow(c.Id, clock.Now.AddSeconds(1), Policy, clock));
        Assert.Throws<InvalidDataException>(() => db.Memory().EvaluateDailyShadow(c.Id, c.FrozenAtUtc.AddTicks(-1), Policy, clock));
        clock.Now = c.FrozenAtUtc.AddTicks(-1);
        Assert.Throws<InvalidDataException>(() => db.Memory().FreezeDailyShadow(run, Mapping, Policy, clock));
    }

    [Fact]
    public async Task ConcurrentDailyRevisionCannotForkLineage()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        var first = await Acquire(db, clock, new(2027, 1, 25), 100); clock.Now = clock.Now.AddSeconds(1);
        var attempts = await Task.WhenAll(new[] { 101m, 102m }.Select(price => Task.Run(async () =>
        {
            try { await Acquire(db, clock, new(2027, 1, 25), price, first.Id); return true; }
            catch (InvalidDataException) { return false; }
        })));
        Assert.Single(attempts, x => x);
        Assert.Equal(2, db.Memory().MarketKnowledgeAt(clock.Now, clock, Policy).Observations.Length);
    }

    [Fact]
    public async Task CancellationDoesNotPersistOrRetry()
    {
        using var db = new FinanceMarketObservationTests.Database(); var clock = new FinanceMarketObservationTests.Clock(At);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        var source = new Source(Value(new(2027, 1, 25), 100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => db.Memory().AcquireMarketObservationAsync(source,
            FinanceMarketObservationTests.Instrument, Mapping, Policy, clock, cancelled.Token));
        Assert.Equal(0, source.Calls); Assert.Equal(0L, db.Scalar("SELECT COUNT(*) FROM market_observation_receipts"));
    }

    private sealed class Transport(string kind) : HttpMessageHandler
    {
        internal int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++; Assert.Equal("data.alpaca.markets", request.RequestUri!.Host);
            Assert.Equal("/v2/stocks/AAPL/bars", request.RequestUri.AbsolutePath);
            Assert.Contains("feed=iex&adjustment=raw&asof=-", request.RequestUri.Query, StringComparison.Ordinal);
            Assert.Contains("timeframe=1Day", request.RequestUri.Query, StringComparison.Ordinal);
            Assert.True(request.Headers.Contains("APCA-API-SECRET-KEY"));
            var json = """{"symbol":"AAPL","bars":[{"t":"2027-01-25T05:00:00Z","o":100,"h":101,"l":99,"c":100,"v":10000,"n":10,"vw":100}],"next_page_token":null}""";
            json = kind switch
            {
                "duplicate" => json.Replace("\"c\":100", "\"c\":100,\"c\":101", StringComparison.Ordinal),
                "pagination" => json.Replace("null", "\"next\"", StringComparison.Ordinal),
                "missing" => "{}",
                "oversized" => new string(' ', 65537),
                "wrong-symbol" => json.Replace("AAPL", "OTHER", StringComparison.Ordinal),
                "wrong-day" => json.Replace("25T05", "26T05", StringComparison.Ordinal),
                _ => json
            };
            return Task.FromResult(new HttpResponseMessage(kind == "unauthorized" ? HttpStatusCode.Unauthorized : HttpStatusCode.OK)
            { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }

    private static ProviderObservation Value(DateOnly day, decimal close) => new("AAPL", "XNAS", new("USD"), Timeframe.OneDay,
        DailyMarketEvidence.DayStart(day), null, close, close + 1, close - 1, close, 10000,
        MarketObservationIntegrity.Hash(new[] { day.ToString("O", CultureInfo.InvariantCulture), close.ToString(CultureInfo.InvariantCulture) }),
        new(DailyMarketEvidence.Eligibility, day, "America/New_York", DailyMarketEvidence.SourceContract, DailyMarketEvidence.SymbolPolicy));
    private static Task<MarketObservationReceipt> Acquire(FinanceMarketObservationTests.Database db,
        FinanceMarketObservationTests.Clock clock, DateOnly day, decimal close, string? corrects = null) =>
        db.Memory().AcquireMarketObservationAsync(new Source(Value(day, close)), FinanceMarketObservationTests.Instrument, Mapping, Policy, clock, CancellationToken.None, corrects);
    private sealed class Source(ProviderObservation value) : IMarketObservationSource
    {
        public string ContractVersion => DailyMarketEvidence.Contract;
        public MarketDataProvider Provider => Mapping.Provider;
        public ProviderDataset Dataset => Mapping.ProviderDataset;
        public string AdapterVersion => "fixture-daily-v1";
        public MarketObservationOrigin Origin => MarketObservationOrigin.DeterministicFixture;
        internal int Calls { get; private set; }
        public Task<ProviderObservation> ReadAsync(ProviderInstrumentMapping mapping, CancellationToken cancellationToken)
        { Calls++; cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(value); }
    }
    private static async Task<string> Seed(FinanceMarketObservationTests.Database db, FinanceMarketObservationTests.Clock clock)
    {
        var dates = new[] { new DateOnly(2027, 1, 20), new(2027, 1, 21), new(2027, 1, 22), new(2027, 1, 25), new(2027, 1, 26), new(2027, 1, 27) };
        foreach (var (day, index) in dates.Select((d, i) => (d, i)))
        { await Acquire(db, clock, day, 100 + index); clock.Now = clock.Now.AddTicks(1); }
        // Existing deterministic science creates/persists supporting research. The shadow operations
        // themselves neither call that engine nor alter its retained output or any research grant.
        var strategy = new MomentumResearchStrategy(5);
        var config = new BacktestRunConfiguration(["fixture-market"], "fixture-feature", strategy.Identity, strategy.Parameters,
            DeterministicBacktestEngine.SimulationModel, BacktestCostModel.Conservative, 10000, [Mapping.InstrumentId.Value],
            dates[0], dates[^1], DeterministicBacktestEngine.SizingPolicy, 0, FillModel: BacktestFillModel.NextSessionOpen);
        var result = DeterministicBacktestEngine.Run(config, strategy, dates.Select((d, i) => new BacktestMarketBar(
            Mapping.InstrumentId, "fixture-market", d, 100 + i, 100 + i, DailyMarketEvidence.DayStart(d).AddHours(22))), []);
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = db.Path }.ToString()); connection.Open();
        FinanceBacktestPersistence.PersistBacktest(connection, result);
        return result.RunId;
    }
}
