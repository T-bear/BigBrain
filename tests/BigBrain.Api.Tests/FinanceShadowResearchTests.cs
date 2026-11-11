using System.Text;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Tests;

public sealed class FinanceShadowResearchTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"bigbrain-shadow-{Guid.NewGuid():N}");

    [Fact]
    public void PredictionIsExactlyOnceResearchOnlyAndClockGated()
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(1); var memory = Ready(now);
        Assert.Equal(0, memory.RunShadowCycle(now, false));
        Assert.Equal(3, memory.RunShadowCycle(now, true));
        Assert.Equal(0, memory.RunShadowCycle(now, true));
        Assert.Equal(0, new EodhdMarketMemory(new EodhdFinanceOptions { Enabled = true, AccountActive = true, DatabasePath = Path.Combine(_root, "memory.db"), PayloadDirectory = Path.Combine(_root, "payloads") }).RunShadowCycle(now.AddMinutes(1), true));
        var catalog = memory.ShadowCatalog(null, null, null, null, null, 50);
        Assert.Equal(3, catalog.Total); Assert.Equal("RESEARCH", catalog.OperatingMode);
        Assert.All(catalog.Predictions, p => { Assert.Equal("RESEARCH", p.OperatingMode); Assert.Equal(FinanceShadowIdentity.Horizon, p.Horizon); Assert.True(p.ObservationKnowledgeUtc <= p.KnowledgeCutoffUtc); Assert.StartsWith("sha256:", p.ParameterFingerprint); });
    }

    [Fact]
    public void FutureObservationAppendsOutcomeWithoutRewritingPrediction()
    {
        var source = DateOnly.FromDateTime(DateTimeOffset.UtcNow.AddDays(10).UtcDateTime);
        while (!UsMarketCalendar.IsSession(source)) source = source.AddDays(1);
        var t0 = new DateTimeOffset(source.ToDateTime(new TimeOnly(22, 0)), TimeSpan.Zero);
        var memory = Ready(t0, source); memory.RunShadowCycle(t0, true);
        var before = memory.ShadowCatalog(null, "momentum", null, null, null, 50).Predictions.Single();
        var instrument = EodhdCatalog.Watchlist.Single(x => x.Symbol == "AAPL");
        var next = source.AddDays(1); while (!UsMarketCalendar.IsSession(next)) next = next.AddDays(1);
        var t1 = new DateTimeOffset(next.ToDateTime(new TimeOnly(22, 0)), TimeSpan.Zero);
        memory.Store(instrument, [new(next, 130, 132, 129, 131, 131, 1000)], Encoding.UTF8.GetBytes("next"), next, next, t1.AddMinutes(-1), t1, 0);
        memory.BuildFeatures(); memory.RunShadowCycle(t1, true); memory.RunShadowCycle(t1.AddMinutes(1), true);
        var after = memory.ShadowPrediction(before.PredictionId)!;
        Assert.Equal(before.PredictionId, after.PredictionId); Assert.Equal(before.KnowledgeCutoffUtc, after.KnowledgeCutoffUtc);
        Assert.Equal(before.Signal, after.Signal); Assert.Equal(FinanceShadowState.Evaluated, after.State); Assert.Equal(before.ReasonCodes, after.ReasonCodes);
        Assert.Single(memory.ShadowCatalog(null, "momentum", "Evaluated", null, null, 50).Predictions);
    }

    [Fact]
    public void LateStartNeverBackfillsOldObservationAndQueriesAreBounded()
    {
        var acquired = DateTimeOffset.UtcNow.AddMinutes(1); var now = acquired.AddDays(5); var memory = Ready(acquired);
        Assert.Equal(0, memory.RunShadowCycle(now, true)); Assert.Empty(memory.ShadowCatalog(null, null, null, null, null, 50).Predictions);
        Assert.Throws<ArgumentException>(() => memory.ShadowCatalog(null, null, null, null, null, 201));
        Assert.Throws<ArgumentException>(() => memory.ShadowPrediction("' OR 1=1 --"));
    }

    [Fact]
    public void CadenceScheduleSkipsWeekendAndOverviewUsesActualBreadthAndResearchSignals()
    {
        Assert.False(FinanceCadenceSchedule.IsProviderWindow(new(2026, 8, 15, 23, 0, 0, TimeSpan.Zero), 22));
        Assert.False(FinanceCadenceSchedule.IsProviderWindow(new(2026, 8, 17, 21, 59, 0, TimeSpan.Zero), 22));
        Assert.True(FinanceCadenceSchedule.IsProviderWindow(new(2026, 8, 17, 22, 0, 0, TimeSpan.Zero), 22));
        var now = DateTimeOffset.UtcNow.AddMinutes(1); var memory = Ready(now); memory.RunShadowCycle(now, true); memory.RecordCadenceCheck(now, false, false, "no-provider-check");
        var provider = new EodhdFinanceOptions { Enabled = true, AccountActive = true, ApiToken = "fixture", DatabasePath = Path.Combine(_root, "memory.db"), PayloadDirectory = Path.Combine(_root, "payloads") };
        var overview = memory.Overview(provider, new FinanceCadenceOptions(), true);
        Assert.Equal("RESEARCH", overview.Mode); Assert.Equal("CURRENT EOD / PROSPECTIVE EOD", overview.ObservationClass);
        Assert.Contains("bevakade instrument", overview.MarketSummary); Assert.Single(overview.Signals); Assert.Equal(3, overview.Signals[0].StrategyCount);
        Assert.Equal(3, overview.Prospective.Valid); Assert.Equal(3, overview.Prospective.Pending); Assert.Equal(0, overview.Prospective.Evaluated); Assert.Empty(overview.Prospective.Curve);
        Assert.Equal("Healthy", overview.Cadence.Health); Assert.Null(overview.Cadence.LastProviderCheckUtc); Assert.Null(overview.Cadence.LastSuccessfulAcquisitionUtc);
        memory.RecordCadenceCheck(now, true, true, "provider-check-no-new-session"); overview = memory.Overview(provider, new FinanceCadenceOptions(), true);
        Assert.Equal(now, overview.Cadence.LastProviderCheckUtc); Assert.Equal(now, overview.Cadence.LastSuccessfulAcquisitionUtc); Assert.True(overview.Cadence.ClockIntegrity); Assert.Equal("RESEARCH", overview.Cadence.OperatingMode);
    }

    [Fact]
    public void ReadModelsAgreeForOutcomesInvalidationAndRevisionsWithoutChangingEvidence()
    {
        var options = new EodhdFinanceOptions { DatabasePath = Path.Combine(_root, "counts.db"), PayloadDirectory = Path.Combine(_root, "payloads") };
        var memory = new EodhdMarketMemory(options);
        using var connection = new SqliteConnection($"Data Source={options.DatabasePath}");
        connection.Open();
        foreach (var (id, state) in new[] { ("pending", "Pending"), ("revision-a", "Pending"), ("revision-b", "Pending"),
            ("evaluated", "Pending"), ("invalid", "Invalidated"), ("invalid-outcome", "Invalidated"),
            ("insufficient", "InsufficientData"), ("missed", "MissedProspectiveWindow") })
        {
            using var insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT INTO shadow_predictions VALUES($id,'US:XNAS:AAPL','AAPL','2026-10-02','EODHD',$revision,
                '2026-10-02T22:00:00Z','2026-10-02T22:01:00Z','fixture-feature','momentum','v1','fixture-parameters',
                'TargetLong','next-eligible-source-session-close-v1','2026-10-02T22:01:00Z',$state,'RESEARCH','[]');
                """;
            insert.Parameters.AddWithValue("$id", "shadow-" + id);
            insert.Parameters.AddWithValue("$revision", "fixture-" + id);
            insert.Parameters.AddWithValue("$state", state);
            insert.ExecuteNonQuery();
        }
        foreach (var id in new[] { "evaluated", "invalid-outcome" })
        {
            using var insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT OR IGNORE INTO shadow_outcomes VALUES($id,$prediction,'2026-10-05','fixture-future',
                '2026-10-05T22:00:00Z','100','101','0.01','CORRECT','2026-10-05T22:01:00Z');
                """;
            insert.Parameters.AddWithValue("$id", "outcome-" + id);
            insert.Parameters.AddWithValue("$prediction", "shadow-" + id);
            Assert.Equal(1, insert.ExecuteNonQuery());
            Assert.Equal(0, insert.ExecuteNonQuery());
        }
        var before = EvidenceSnapshot(connection);
        for (var repeat = 0; repeat < 2; repeat++)
        {
            var catalog = memory.ShadowCatalog(null, null, null, null, null, 50);
            var cadence = memory.CadenceSnapshot(false, true, 22, 30);
            var overview = memory.Overview(options, new FinanceCadenceOptions(), true);
            Assert.Equal(8, catalog.Total);
            Assert.Equal(3, catalog.Pending);
            Assert.Equal(1, catalog.Evaluated);
            Assert.Equal(2, catalog.Invalidated);
            Assert.Equal(1, catalog.Insufficient);
            Assert.Equal(1, catalog.Missed);
            Assert.Equal(catalog.Pending, memory.ShadowStatus(true).Pending);
            Assert.Equal(catalog.Pending, cadence.Pending);
            Assert.Equal(catalog.Evaluated, cadence.Evaluated);
            Assert.Equal(catalog.Invalidated, cadence.Invalidated);
            Assert.Equal(catalog.Pending, overview.Prospective.Pending);
            Assert.Equal(catalog.Evaluated, overview.Prospective.Evaluated);
            Assert.Equal(catalog.Invalidated, overview.Prospective.Invalidated);
            Assert.Equal(2, memory.ShadowCatalog(null, null, "Invalidated", null, null, 50).Predictions.Count);
            Assert.Equal(FinanceShadowState.Invalidated, memory.ShadowPrediction("shadow-invalid-outcome")!.State);
        }
        Assert.Equal(before, EvidenceSnapshot(connection));
    }

    [Theory]
    [InlineData(2026, 11, 26, 22, false)] // Thanksgiving
    [InlineData(2026, 11, 27, 22, true)]  // Early close: conservative EOD window still safe
    [InlineData(2026, 12, 25, 22, false)]
    [InlineData(2027, 1, 1, 22, false)]
    [InlineData(2026, 11, 11, 20, false)] // Winter close has not happened
    [InlineData(2026, 7, 2, 19, false)]   // Summer close has not happened
    [InlineData(2026, 11, 11, 22, true)]
    public void ProviderWindowRequiresAnExchangeSessionAndCompletedClose(int year, int month, int day, int hour, bool expected)
    {
        Assert.Equal(expected, FinanceCadenceSchedule.IsProviderWindow(new(year, month, day, hour, 0, 0, TimeSpan.Zero), 0));
    }

    private static string EvidenceSnapshot(SqliteConnection connection)
    {
        var rows = new List<object[]>();
        foreach (var table in new[] { "shadow_predictions", "shadow_outcomes" })
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM {table} ORDER BY 1";
            using var reader = command.ExecuteReader();
            while (reader.Read()) { var row = new object[reader.FieldCount]; reader.GetValues(row); rows.Add(row); }
        }
        return System.Text.Json.JsonSerializer.Serialize(rows);
    }

    private EodhdMarketMemory Ready(DateTimeOffset acquired, DateOnly? sourceSession = null)
    {
        var options = new EodhdFinanceOptions { Enabled = true, AccountActive = true, DatabasePath = Path.Combine(_root, "memory.db"), PayloadDirectory = Path.Combine(_root, "payloads") };
        var memory = new EodhdMarketMemory(options); var instrument = EodhdCatalog.Watchlist.Single(x => x.Symbol == "AAPL"); var bars = new List<EodhdDailyBar>();
        var end = sourceSession ?? PreviousWeekday(DateOnly.FromDateTime(acquired.UtcDateTime)); var sessions = new List<DateOnly>(); for (var date = end; sessions.Count < 25; date = date.AddDays(-1)) if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) sessions.Add(date); sessions.Reverse(); for (var i = 0; i < sessions.Count; i++) { var close = 100 + i; bars.Add(new(sessions[i], close - 1, close + 1, close - 2, close, close, 1000 + i)); }
        memory.Store(instrument, bars, Encoding.UTF8.GetBytes("fixture-25"), bars[0].Date, bars[^1].Date, acquired.AddMinutes(-1), acquired, 0); memory.BuildFeatures(); return memory;
    }
    private static DateOnly PreviousWeekday(DateOnly date) { date = date.AddDays(-1); while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) date = date.AddDays(-1); return date; }
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); GC.SuppressFinalize(this); }
}
