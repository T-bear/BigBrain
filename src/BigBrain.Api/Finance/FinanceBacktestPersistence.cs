using System.Globalization;
using System.Text.Json;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

// Writes one immutable result atomically; connection lifetime and schema belong to callers.
internal static class FinanceBacktestPersistence
{
    private static readonly JsonSerializerOptions BacktestJson = new(JsonSerializerDefaults.Web);

    internal static bool PersistBacktest(SqliteConnection connection, BacktestResult result)
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

    private static string? ScalarTextOrNull(SqliteConnection c,string sql,params (string Name,object Value)[] args){using var command=c.CreateCommand();command.CommandText=sql;foreach(var x in args)command.Parameters.AddWithValue(x.Name,x.Value);return command.ExecuteScalar() as string;}
    private static void Execute(SqliteConnection connection, SqliteTransaction? transaction, string sql, params (string Name, object Value)[] values)
    { using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = sql; foreach (var value in values) command.Parameters.AddWithValue(value.Name, value.Value); command.ExecuteNonQuery(); }
}
