using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

public sealed record FinanceBacktestStrategySummary(string Id, string Version, string Name,
    IReadOnlyDictionary<string, decimal> DefaultParameters);
public sealed record FinanceBacktestRunSummary(string RunId, string Checksum, string StrategyId, string StrategyVersion,
    IReadOnlyDictionary<string, decimal> Parameters, string CostModel, DateOnly From, DateOnly To,
    decimal InitialEquity, decimal FinalEquity, decimal GrossReturn, decimal NetReturn, decimal MaxDrawdown,
    int Trades, decimal CostImpact, decimal? BenchmarkReturn, decimal? ExcessReturn, IReadOnlyList<string> MarketRevisionIds,
    string FeatureRevisionId, string SimulationModel, string SizingPolicy, string Status, IReadOnlyList<string> Limitations);
public sealed record FinanceBacktestCatalog(DateTimeOffset GeneratedAtUtc, string OperatingMode,
    IReadOnlyList<FinanceBacktestStrategySummary> Strategies, IReadOnlyList<FinanceBacktestRunSummary> Runs);
public sealed record FinanceBacktestBuildEvidence(IReadOnlyList<string> RunIds, IReadOnlyList<string> Checksums,
    string FeatureRevisionId, IReadOnlyList<string> MarketRevisionIds, int Sessions, int Instruments,
    int FeatureReads, int Fills, int Events, long ElapsedMilliseconds, bool Idempotent);

internal sealed partial class EodhdMarketMemory
{
    private static readonly JsonSerializerOptions BacktestJson = new(JsonSerializerDefaults.Web);

    private static void InitializeBacktestStorage(SqliteConnection connection)
    {
        using var command = connection.CreateCommand(); command.CommandText = """
            CREATE TABLE IF NOT EXISTS backtest_runs(
              run_id TEXT PRIMARY KEY, checksum TEXT NOT NULL, strategy_id TEXT NOT NULL, strategy_version TEXT NOT NULL,
              cost_model TEXT NOT NULL, feature_revision_id TEXT NOT NULL, market_revisions_json TEXT NOT NULL,
              from_date TEXT NOT NULL, to_date TEXT NOT NULL, result_json TEXT NOT NULL, created_utc TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS backtest_events(run_id TEXT NOT NULL, sequence INTEGER NOT NULL, event_json TEXT NOT NULL, PRIMARY KEY(run_id,sequence));
            CREATE TABLE IF NOT EXISTS backtest_fills(run_id TEXT NOT NULL, fill_id TEXT NOT NULL, fill_json TEXT NOT NULL, PRIMARY KEY(run_id,fill_id));
            CREATE TABLE IF NOT EXISTS backtest_equity(run_id TEXT NOT NULL, session_date TEXT NOT NULL, point_json TEXT NOT NULL, PRIMARY KEY(run_id,session_date));
            CREATE INDEX IF NOT EXISTS ix_backtest_runs_read ON backtest_runs(strategy_id,from_date,to_date,cost_model);
            CREATE TABLE IF NOT EXISTS backtest_deletion_receipts(receipt_id TEXT PRIMARY KEY, runs INTEGER NOT NULL, events INTEGER NOT NULL, fills INTEGER NOT NULL, equity_points INTEGER NOT NULL);
            """; command.ExecuteNonQuery();
    }

    internal FinanceBacktestBuildEvidence BuildReferenceBacktests()
    {
        var watch = Stopwatch.StartNew(); using var connection = new SqliteConnection(ConnectionString); connection.Open();
        var featureRevision = LatestFeatureRevisionId(connection) ?? throw new InvalidOperationException("No immutable feature revision is available.");
        var marketRevisions = JsonSerializer.Deserialize<string[]>(ScalarText(connection, "SELECT source_revisions_json FROM feature_revisions WHERE revision_id=$id", ("$id", featureRevision))) ?? [];
        var market = ReadBacktestMarket(connection, marketRevisions); var features = ReadBacktestFeatures(connection, featureRevision);
        if (market.Count == 0) throw new InvalidOperationException("No exact market observations are available for the feature lineage.");
        var universe = market.Select(x => x.InstrumentId.Value).Distinct().Order(StringComparer.Ordinal).ToArray();
        var from = market.Min(x => x.SessionDate); var to = market.Max(x => x.SessionDate); var results = new List<BacktestResult>(); var anyNew = false;
        foreach (var cost in new[] { BacktestCostModel.Zero, BacktestCostModel.Conservative })
        {
            var benchmarkStrategy = new BuyAndHoldResearchStrategy(); var benchmark = Run(benchmarkStrategy, cost, null);
            anyNew |= PersistBacktest(connection, benchmark); results.Add(benchmark);
            foreach (var strategy in new IResearchBacktestStrategy[] { new SmaCrossoverResearchStrategy(), new MomentumResearchStrategy() })
            {
                var result = Run(strategy, cost, benchmark.Metrics.NetReturn); anyNew |= PersistBacktest(connection, result); results.Add(result);
            }
        }
        watch.Stop();
        return new(results.Select(x => x.RunId).ToArray(), results.Select(x => x.Checksum).ToArray(), featureRevision, marketRevisions,
            market.Select(x => x.SessionDate).Distinct().Count(), universe.Length, features.Count, results.Sum(x => x.Fills.Count),
            results.Sum(x => x.Events.Count), watch.ElapsedMilliseconds, !anyNew);

        BacktestResult Run(IResearchBacktestStrategy strategy, BacktestCostModel cost, decimal? benchmarkReturn)
        {
            var config = new BacktestRunConfiguration(marketRevisions, featureRevision, strategy.Identity, strategy.Parameters,
                DeterministicBacktestEngine.SimulationModel, cost, 100_000m, universe, from, to,
                DeterministicBacktestEngine.SizingPolicy, 0, FillModel: BacktestFillModel.NextSessionOpen);
            return DeterministicBacktestEngine.Run(config, strategy, market, features, benchmarkReturn);
        }
    }

    internal FinanceBacktestCatalog BacktestCatalog()
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        var runs = new List<FinanceBacktestRunSummary>(); using var command = connection.CreateCommand();
        command.CommandText = "SELECT result_json FROM backtest_runs ORDER BY strategy_id,cost_model,run_id";
        using var reader = command.ExecuteReader(); while (reader.Read())
        {
            var result = JsonSerializer.Deserialize<BacktestResult>(reader.GetString(0), BacktestJson)!;
            runs.Add(Summary(result));
        }
        return new(DateTimeOffset.UtcNow, "RESEARCH",
            [new("buy-and-hold","v1","Buy and hold benchmark",new Dictionary<string,decimal>()),
             new("sma-crossover","v1","SMA crossover research",new Dictionary<string,decimal>{{"fastPeriod",10},{"slowPeriod",20}}),
             new("momentum","v1","Momentum research",new Dictionary<string,decimal>{{"period",20}})], runs);
    }

    internal BacktestResult? BacktestResult(string runId)
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        var json = ScalarTextOrNull(connection, "SELECT result_json FROM backtest_runs WHERE run_id=$id", ("$id", runId));
        return json is null ? null : JsonSerializer.Deserialize<BacktestResult>(json, BacktestJson);
    }

    private static bool PersistBacktest(SqliteConnection connection, BacktestResult result)
    {
        var existing = ScalarTextOrNull(connection, "SELECT checksum FROM backtest_runs WHERE run_id=$id", ("$id", result.RunId));
        if (existing is not null)
        {
            if (existing != result.Checksum) throw new InvalidOperationException($"Immutable backtest identity conflict for {result.RunId}: stored {existing}, computed {result.Checksum}.");
            return false;
        }
        using var transaction = connection.BeginTransaction();
        using var insert=connection.CreateCommand();insert.Transaction=transaction;insert.CommandText="INSERT OR IGNORE INTO backtest_runs VALUES($id,$checksum,$strategy,$version,$cost,$feature,$markets,$from,$to,$json,$created)";
        foreach(var value in new (string Name,object Value)[]{("$id",result.RunId),("$checksum",result.Checksum),("$strategy",result.Configuration.Strategy.Id),("$version",result.Configuration.Strategy.Version),
            ("$cost",$"{result.Configuration.CostModel.Id}-{result.Configuration.CostModel.Version}"),("$feature",result.Configuration.FeatureRevisionId),
            ("$markets",JsonSerializer.Serialize(result.Configuration.MarketRevisionIds)),("$from",result.Configuration.From.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),
            ("$to",result.Configuration.To.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),("$json",JsonSerializer.Serialize(result,BacktestJson)),("$created",DateTimeOffset.UtcNow.ToString("O"))})insert.Parameters.AddWithValue(value.Name,value.Value);
        if(insert.ExecuteNonQuery()==0)
        {
            transaction.Rollback();var winner=ScalarTextOrNull(connection,"SELECT checksum FROM backtest_runs WHERE run_id=$id",("$id",result.RunId));
            if(winner!=result.Checksum)throw new InvalidOperationException($"Immutable backtest identity conflict for {result.RunId}: stored {winner??"missing"}, computed {result.Checksum}.");
            return false;
        }
        foreach (var item in result.Events) Execute(connection, transaction, "INSERT INTO backtest_events VALUES($run,$sequence,$json)",("$run",result.RunId),("$sequence",item.Sequence),("$json",JsonSerializer.Serialize(item,BacktestJson)));
        foreach (var item in result.Fills) Execute(connection, transaction, "INSERT INTO backtest_fills VALUES($run,$id,$json)",("$run",result.RunId),("$id",item.FillId),("$json",JsonSerializer.Serialize(item,BacktestJson)));
        foreach (var item in result.EquityCurve) Execute(connection, transaction, "INSERT INTO backtest_equity VALUES($run,$date,$json)",("$run",result.RunId),("$date",item.Session.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),("$json",JsonSerializer.Serialize(item,BacktestJson)));
        transaction.Commit(); return true;
    }

    private static List<BacktestMarketBar> ReadBacktestMarket(SqliteConnection connection, IReadOnlyList<string> revisions)
    {
        var rows = new List<BacktestMarketBar>(); using var command = connection.CreateCommand();
        command.CommandText = "SELECT instrument_id,revision_id,session_date,open,close,acquired_utc,volume FROM observations ORDER BY session_date,instrument_id";
        using var reader = command.ExecuteReader(); while(reader.Read()) if(revisions.Contains(reader.GetString(1),StringComparer.Ordinal))
            rows.Add(new(new InstrumentId(reader.GetString(0)),reader.GetString(1),DateOnly.Parse(reader.GetString(2),CultureInfo.InvariantCulture),decimal.Parse(reader.GetString(3),CultureInfo.InvariantCulture),decimal.Parse(reader.GetString(4),CultureInfo.InvariantCulture),DateTimeOffset.Parse(reader.GetString(5),CultureInfo.InvariantCulture),reader.GetInt64(6)));
        return rows;
    }
    private static List<BacktestFeatureValue> ReadBacktestFeatures(SqliteConnection connection,string revision)
    {
        var rows=new List<BacktestFeatureValue>(); using var command=connection.CreateCommand(); command.CommandText="SELECT instrument_id,session_date,definition_id,value,knowledge_utc FROM feature_values WHERE revision_id=$id ORDER BY session_date,instrument_id,definition_id"; command.Parameters.AddWithValue("$id",revision);
        using var reader=command.ExecuteReader(); while(reader.Read()) rows.Add(new(new InstrumentId(reader.GetString(0)),DateOnly.Parse(reader.GetString(1),CultureInfo.InvariantCulture),reader.GetString(2),reader.IsDBNull(3)?null:decimal.Parse(reader.GetString(3),CultureInfo.InvariantCulture),DateTimeOffset.Parse(reader.GetString(4),CultureInfo.InvariantCulture),revision)); return rows;
    }
    private static string? LatestFeatureRevisionId(SqliteConnection connection)=>ScalarTextOrNull(connection,"SELECT revision_id FROM feature_revisions ORDER BY created_utc DESC,revision_id DESC LIMIT 1");
    private static string ScalarText(SqliteConnection c,string sql,params (string Name,object Value)[] args)=>ScalarTextOrNull(c,sql,args)??throw new InvalidOperationException("Required value is unavailable.");
    private static string? ScalarTextOrNull(SqliteConnection c,string sql,params (string Name,object Value)[] args){using var command=c.CreateCommand();command.CommandText=sql;foreach(var x in args)command.Parameters.AddWithValue(x.Name,x.Value);return command.ExecuteScalar() as string;}
    private static FinanceBacktestRunSummary Summary(BacktestResult x)=>new(x.RunId,x.Checksum,x.Configuration.Strategy.Id,x.Configuration.Strategy.Version,x.Configuration.StrategyParameters,$"{x.Configuration.CostModel.Id}-{x.Configuration.CostModel.Version}",x.Configuration.From,x.Configuration.To,x.Metrics.InitialEquity,x.Metrics.FinalEquity,x.Metrics.GrossReturn,x.Metrics.NetReturn,x.Metrics.MaxDrawdown,x.Metrics.Trades,Math.Max(0,x.Metrics.GrossReturn-x.Metrics.NetReturn),x.Metrics.BenchmarkReturn,x.Metrics.ExcessReturn,x.Configuration.MarketRevisionIds,x.Configuration.FeatureRevisionId,x.Configuration.SimulationModel,x.Configuration.SizingPolicy,x.Status,x.Limitations);
}

public interface IFinanceBacktestReader { FinanceBacktestCatalog GetCatalog(); BacktestResult? GetResult(string runId); }
internal sealed class EodhdFinanceBacktestReader(EodhdMarketMemory memory):IFinanceBacktestReader
{ public FinanceBacktestCatalog GetCatalog()=>memory.BacktestCatalog(); public BacktestResult? GetResult(string runId)=>memory.BacktestResult(runId); }

internal sealed class FinanceBacktestBuildWorker(EodhdFinanceOptions options,EodhdMarketMemory memory,BigBrain.Api.SystemRecovery.SystemRecoveryCoordinator recovery):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token){await recovery.WaitUntilRecoveredAsync(token);if(!options.Enabled||!options.AccountActive)return;await Task.Delay(TimeSpan.FromSeconds(18),token);try{memory.BuildReferenceBacktests();}catch(InvalidOperationException){}}
}
