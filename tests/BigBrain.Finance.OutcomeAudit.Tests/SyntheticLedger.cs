using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using BigBrain.Finance.OutcomeAudit;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Finance.OutcomeAudit.Tests;

internal sealed partial class SyntheticLedger : IDisposable
{
    internal string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "bigbrain-synthetic-outcome-" + Guid.NewGuid().ToString("N"));
    internal string DatabasePath => Path.Combine(DirectoryPath, "synthetic.db");
    internal static readonly DateTimeOffset SnapshotUtc = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
    internal const string Source = "eodhd-aaaaaaaaaaaaaaaa";
    internal const string Target = "eodhd-bbbbbbbbbbbbbbbb";
    internal const string Feature = "feature-synthetic";
    internal const string Prediction = "shadow-synthetic";
    internal SqliteConnection Writer { get; }
    internal DateOnly SourceDate { get; }
    internal DateOnly TargetDate { get; }
    internal DateTimeOffset SourceKnown => UsMarketCalendar.Session(SourceDate)!.CloseUtc.AddHours(1);
    internal DateTimeOffset TargetKnown => UsMarketCalendar.Session(TargetDate)!.CloseUtc.AddHours(1);

    internal SyntheticLedger(string date = "2026-09-03", bool wal = false)
    {
        Directory.CreateDirectory(DirectoryPath);
        Writer = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = DatabasePath, Pooling = false }.ToString());
        Writer.Open();
        Execute("PRAGMA foreign_keys=OFF; PRAGMA journal_mode=" + (wal ? "WAL" : "DELETE") + "; PRAGMA wal_autocheckpoint=0;");
        // Extract the actual accepted schema, without referencing or constructing API/storage owners.
        foreach (var name in new[] { "EodhdFinanceMarketData.cs", "FinanceShadowResearch.cs", "FinanceFeatureStore.cs" })
        {
            var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "schema", name));
            foreach (Match match in TableDdl().Matches(source))
                if (match.Groups[1].Value is "revisions" or "observations" or "feature_revisions" or "shadow_predictions" or "shadow_outcomes") Execute(match.Value);
        }
        SourceDate = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        TargetDate = SourceDate.AddDays(1);
        while (!UsMarketCalendar.IsSession(TargetDate)) TargetDate = TargetDate.AddDays(1);
        AddRevision(Source, SourceKnown);
        AddRevision(Target, TargetKnown);
        AddBar(Source, SourceDate, SourceKnown, "100");
        AddBar(Target, TargetDate, TargetKnown, "101");
        Execute("""
            INSERT INTO feature_revisions VALUES($id,'core-daily-v1',$hash,'daily-feature-engine-v1',$sources,NULL,NULL,1,1,0,0,$hash,$time,0,'raw close/OHLC; provider volume classification; no adjusted-price mixing');
            """, ("$id", Feature), ("$hash", "sha256:" + new string('c', 64)), ("$sources", "[\"" + Source + "\"]"), ("$time", SourceKnown.ToString("O")));
        Execute("""
            INSERT INTO shadow_predictions VALUES($id,'US:XNAS:AAPL','AAPL',$date,'EODHD',$revision,$known,$created,$feature,'buy-and-hold','v1',$parameters,'TargetLong','next-eligible-source-session-close-v1',$created,'Pending','RESEARCH','["original.synthetic.reason"]');
            """, ("$id", Prediction), ("$date", Day(SourceDate)), ("$revision", Source), ("$known", SourceKnown.ToString("O")),
            ("$created", SourceKnown.AddMinutes(1).ToString("O")), ("$feature", Feature), ("$parameters", "sha256:" + new string('d', 64)));
        Execute("""
            INSERT INTO shadow_outcomes VALUES($id,$prediction,$date,$revision,$known,'100','101','0.01','CORRECT',$evaluated);
            """, ("$id", LedgerAudit.OutcomeId(Prediction, Target, Day(TargetDate))), ("$prediction", Prediction), ("$date", Day(TargetDate)),
            ("$revision", Target), ("$known", TargetKnown.ToString("O")), ("$evaluated", TargetKnown.AddMinutes(1).ToString("O")));
    }

    internal void AddRevision(string id, DateTimeOffset time) => Execute("INSERT INTO revisions VALUES($id,$hash,$time,1,1)",
        ("$id", id), ("$hash", "sha256:" + new string(id[^1], 64)), ("$time", time.ToString("O")));
    internal void AddBar(string revision, DateOnly date, DateTimeOffset known, string close) => Execute("""
        INSERT INTO observations VALUES('EODHD','Free','eodhd-free-personal-v2026-08-11','US:XNAS:AAPL','AAPL','AAPL.US','XNAS',$date,'100','102','99',$close,$close,1,$known,$revision)
        """, ("$revision", revision), ("$date", Day(date)), ("$known", known.ToString("O")), ("$close", close));
    internal void Execute(string sql, params (string Name, object Value)[] values)
    {
        using var command = Writer.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in values) command.Parameters.AddWithValue(name, value);
        command.ExecuteNonQuery();
    }
    internal AuditReport Read() => OfflineImage.Read(DatabasePath, Digest(DatabasePath), SnapshotUtc);
    internal static string Digest(string path) { using var input = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(input)); }
    internal static string Day(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    internal string Snapshot()
    {
        var path = Path.Combine(DirectoryPath, "synthetic-complete.db");
        using var destination = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
        destination.Open();
        Writer.BackupDatabase(destination); // Only synthetic fixtures; auditor itself has no copy API.
        using var command = destination.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=DELETE";
        command.ExecuteNonQuery();
        return path;
    }
    public void Dispose() { Writer.Dispose(); Directory.Delete(DirectoryPath, true); }
    [GeneratedRegex(@"CREATE TABLE IF NOT EXISTS (\w+)\([\s\S]*?\);", RegexOptions.CultureInvariant)]
    private static partial Regex TableDdl();
}
