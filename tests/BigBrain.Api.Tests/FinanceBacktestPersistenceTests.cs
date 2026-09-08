using System.Data;
using System.Text.Json;
using BigBrain.Api.Finance;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Tests;

public sealed class FinanceBacktestPersistenceTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public void CompleteEvidenceReplayConflictAndRestartPreserveExactStoredJson()
    {
        using var fixture = new Fixture();
        var result = Result();
        Assert.NotEmpty(result.Events);
        Assert.NotEmpty(result.Fills);
        Assert.NotEmpty(result.EquityCurve);
        using var connection = fixture.Open();
        Assert.True(FinanceBacktestPersistence.PersistBacktest(connection, result));
        Assert.Equal(JsonSerializer.Serialize(result, Json), Read(connection, "SELECT result_json FROM backtest_runs").Single());
        Assert.Equal(JsonSerializer.Serialize(result.Configuration.MarketRevisionIds), Read(connection, "SELECT market_revisions_json FROM backtest_runs").Single());
        Assert.Equal(result.Configuration.FeatureRevisionId, Read(connection, "SELECT feature_revision_id FROM backtest_runs").Single());
        Assert.Equal(result.Events.Select(x => JsonSerializer.Serialize(x, Json)), Read(connection, "SELECT event_json FROM backtest_events ORDER BY sequence"));
        Assert.Equal(result.Fills.OrderBy(x => x.FillId, StringComparer.Ordinal).Select(x => JsonSerializer.Serialize(x, Json)), Read(connection, "SELECT fill_json FROM backtest_fills ORDER BY fill_id"));
        Assert.Equal(result.EquityCurve.Select(x => JsonSerializer.Serialize(x, Json)), Read(connection, "SELECT point_json FROM backtest_equity ORDER BY session_date"));
        var before = Snapshot(connection);
        Assert.Equal("backtest-4013c330c3197923", result.RunId);
        Assert.Equal("sha256:30d506781dfbbf0be5cd21d3008ebf24c5d889ebc76328080f6222b9cb843120", result.Checksum);
        var jsonRows = Read(connection, "SELECT result_json FROM backtest_runs")
            .Concat(Read(connection, "SELECT event_json FROM backtest_events ORDER BY sequence"))
            .Concat(Read(connection, "SELECT fill_json FROM backtest_fills ORDER BY fill_id"))
            .Concat(Read(connection, "SELECT point_json FROM backtest_equity ORDER BY session_date"));
        // Captured on unchanged production: LF-joined UTF-8 stored JSON, no terminal LF.
        Assert.Equal("12C9479C7F3C3349BF1ECE0532C72C0FF8D7ADB3C0D98CB12ED3EF6A16738459",
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(string.Join("\n", jsonRows)))));
        Assert.False(FinanceBacktestPersistence.PersistBacktest(connection, result));
        var conflict = Assert.Throws<InvalidOperationException>(() => FinanceBacktestPersistence.PersistBacktest(connection, result with { Checksum = "synthetic-conflict" }));
        Assert.Equal($"Immutable backtest identity conflict for {result.RunId}: stored {result.Checksum}, computed synthetic-conflict.", conflict.Message);
        Assert.Equal(before, Snapshot(connection));
        Assert.Equal(ConnectionState.Open, connection.State);
        var restarted = fixture.Memory();
        Assert.Equal(JsonSerializer.Serialize(result, Json), JsonSerializer.Serialize(restarted.BacktestResult(result.RunId), Json));
        Assert.Equal(result.RunId, Assert.Single(restarted.BacktestCatalog().Runs).RunId);
        using var reopened = fixture.Open();
        Assert.False(FinanceBacktestPersistence.PersistBacktest(reopened, Result()));
        Assert.Equal(before, Snapshot(reopened));
    }

    [Fact]
    public void EquityInsertionFailureRollsBackRunAndEveryChildFamily()
    {
        using var fixture = new Fixture();
        using var connection = fixture.Open();
        var result = Result();
        Assert.NotEmpty(result.Events);
        Assert.NotEmpty(result.Fills);
        Assert.NotEmpty(result.EquityCurve);
        // Duplicate the last family's primary key after all valid children were inserted.
        var invalid = result with { EquityCurve = result.EquityCurve.Concat([result.EquityCurve[0]]).ToArray() };
        var failure = Assert.Throws<SqliteException>(() => FinanceBacktestPersistence.PersistBacktest(connection, invalid));
        Assert.Equal(19, failure.SqliteErrorCode);
        Assert.Empty(Snapshot(connection));
        Assert.Equal(ConnectionState.Open, connection.State);
        Assert.True(FinanceBacktestPersistence.PersistBacktest(connection, result));
        Assert.Equal(1 + result.Events.Count + result.Fills.Count + result.EquityCurve.Count, Snapshot(connection).Length);
    }

    [Fact]
    public void ReaderCatalogOrdersStoredRunsAndReopensWithoutChangingEvidence()
    {
        using var fixture = new Fixture();
        using var connection = fixture.Open();
        var results = new[]
        {
            Result(2, BacktestCostModel.Conservative, momentum: true),
            Result(1, BacktestCostModel.Zero),
            Result(2),
            Result(1)
        };
        foreach (var result in results.Reverse()) Assert.True(FinanceBacktestPersistence.PersistBacktest(connection, result));
        var before = Snapshot(connection);
        var memory = fixture.Memory();
        var reader = new EodhdFinanceBacktestReader(memory);
        var expected = results.OrderBy(x => x.Configuration.Strategy.Id, StringComparer.Ordinal)
            .ThenBy(x => $"{x.Configuration.CostModel.Id}-{x.Configuration.CostModel.Version}", StringComparer.Ordinal)
            .ThenBy(x => x.RunId, StringComparer.Ordinal).ToArray();
        var catalog = reader.GetCatalog();
        Assert.Equal("RESEARCH", catalog.OperatingMode);
        string[] expectedStrategies = ["buy-and-hold", "sma-crossover", "momentum"];
        Assert.Equal(expectedStrategies, catalog.Strategies.Select(x => x.Id));
        Assert.Equal(expected.Select(x => x.RunId), catalog.Runs.Select(x => x.RunId));
        Assert.Equal(expected.Select(x => x.Checksum), catalog.Runs.Select(x => x.Checksum));
        foreach (var result in expected)
        {
            var summary = catalog.Runs.Single(x => x.RunId == result.RunId);
            Assert.Equal(result.Configuration.FeatureRevisionId, summary.FeatureRevisionId);
            Assert.Equal(result.Configuration.MarketRevisionIds, summary.MarketRevisionIds);
            Assert.Equal(Math.Max(0, result.Metrics.GrossReturn - result.Metrics.NetReturn), summary.CostImpact);
            Assert.Equal(JsonSerializer.Serialize(result, Json), JsonSerializer.Serialize(reader.GetResult(result.RunId), Json));
        }
        Assert.Null(reader.GetResult("missing-synthetic-run"));
        Assert.Null(reader.GetResult("' OR 1=1 --"));
        var reopened = new EodhdFinanceBacktestReader(fixture.Memory());
        Assert.Equal(JsonSerializer.Serialize(catalog.Runs, Json), JsonSerializer.Serialize(reopened.GetCatalog().Runs, Json));
        Assert.Equal(before, Snapshot(connection));
        Assert.Equal("0", Read(connection, "SELECT CAST(COUNT(*) AS TEXT) FROM acquisitions").Single());
    }

    [Fact]
    public void ReaderRejectsMalformedStoredJsonWithoutRepairingOrSkippingTheRow()
    {
        using var fixture = new Fixture();
        using var connection = fixture.Open();
        var result = Result();
        Assert.True(FinanceBacktestPersistence.PersistBacktest(connection, result));
        // Deliberately damaged synthetic payload; no production data or repair path.
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "UPDATE backtest_runs SET result_json=$json WHERE run_id=$id";
            command.Parameters.AddWithValue("$json", "{");
            command.Parameters.AddWithValue("$id", result.RunId);
            Assert.Equal(1, command.ExecuteNonQuery());
        }
        var before = Snapshot(connection);
        var reader = new EodhdFinanceBacktestReader(fixture.Memory());
        Assert.Throws<JsonException>(() => reader.GetResult(result.RunId));
        Assert.Throws<JsonException>(() => reader.GetCatalog());
        Assert.Null(reader.GetResult("missing-synthetic-run"));
        Assert.Equal(before, Snapshot(connection));
    }

    private static string[] Snapshot(SqliteConnection connection) =>
        Read(connection, "SELECT run_id || checksum || result_json || created_utc FROM backtest_runs ORDER BY run_id")
        .Concat(Read(connection, "SELECT event_json FROM backtest_events ORDER BY run_id,sequence"))
        .Concat(Read(connection, "SELECT fill_json FROM backtest_fills ORDER BY run_id,fill_id"))
        .Concat(Read(connection, "SELECT point_json FROM backtest_equity ORDER BY run_id,session_date")).ToArray();

    private static string[] Read(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read()) rows.Add(reader.GetString(0));
        return rows.ToArray();
    }

    private static BacktestResult Result(int seed = 0, BacktestCostModel? cost = null, bool momentum = false)
    {
        IResearchBacktestStrategy strategy = momentum ? new MomentumResearchStrategy() : new BuyAndHoldResearchStrategy();
        var configuration = new BacktestRunConfiguration(["market-1"], "feature-1", strategy.Identity,
            strategy.Parameters, DeterministicBacktestEngine.SimulationModel, cost ?? BacktestCostModel.Conservative,
            10_000, ["US:XNAS:TEST"], new(2026, 1, 2), new(2026, 1, 30),
            DeterministicBacktestEngine.SizingPolicy, seed, FillModel: BacktestFillModel.NextSessionOpen);
        var dates = new[] { new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6) };
        var bars = dates.Select((date, i) => new BacktestMarketBar(new("US:XNAS:TEST"), "market-1",
            date, 100 + i * 10, 100 + i * 10, new DateTimeOffset(2026, 1, 1, 22, 0, 0, TimeSpan.Zero).AddDays(i)));
        return DeterministicBacktestEngine.Run(configuration, strategy, bars, []);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "bb-writer-tests", Guid.NewGuid().ToString("N"));
        private readonly EodhdFinanceOptions _options;
        internal Fixture()
        {
            _options = new() { DatabasePath = Path.Combine(_root, "finance.db"), PayloadDirectory = Path.Combine(_root, "payloads") };
            _ = Memory();
        }
        internal EodhdMarketMemory Memory() => new(_options);
        internal SqliteConnection Open()
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _options.DatabasePath, Pooling = false }.ToString());
            connection.Open();
            return connection;
        }
        public void Dispose() => Directory.Delete(_root, true);
    }
}
