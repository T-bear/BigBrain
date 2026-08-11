using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

var scales = args.Contains("--full", StringComparer.Ordinal)
    ? new[] { new Scale("small", 10, 252), new Scale("medium", 100, 1260), new Scale("large", 500, 2520) }
    : new[] { new Scale("small", 10, 252), new Scale("medium", 100, 1260) };
var root = Path.Combine(Path.GetTempPath(), $"bb-finance-persistence-benchmark-{Environment.ProcessId}");
Directory.CreateDirectory(root);
var results = new List<Result>();
try
{
    foreach (var scale in scales)
    {
        results.Add(RunJsonLines(scale, root));
        results.Add(RunSqlite(scale, root));
    }
    Console.WriteLine(JsonSerializer.Serialize(new { generatedAt = "supplied-by-benchmark-run", runtime = Environment.Version.ToString(), results },
        new JsonSerializerOptions { WriteIndented = true }));
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, true);
}

static Result RunJsonLines(Scale scale, string root)
{
    var path = Path.Combine(root, $"{scale.Name}.jsonl");
    var manifest = Path.Combine(root, $"{scale.Name}.manifest");
    var watch = Stopwatch.StartNew();
    using (var writer = new StreamWriter(path, false, new UTF8Encoding(false), 1 << 20))
        foreach (var row in Rows(scale)) writer.WriteLine(Line(row, "revision-001"));
    File.WriteAllText(manifest, $"revision-001|{scale.Rows}|parent:-", Encoding.UTF8);
    watch.Stop(); var write = watch.Elapsed.TotalMilliseconds;
    watch.Restart();
    using (var writer = new StreamWriter(path, true, new UTF8Encoding(false), 1 << 16))
        foreach (var row in AppendRows(scale)) writer.WriteLine(Line(row, "revision-002"));
    File.AppendAllText(manifest, $"{Environment.NewLine}revision-002|{scale.Instruments * 5}|parent:revision-001", Encoding.UTF8);
    watch.Stop(); var append = watch.Elapsed.TotalMilliseconds;
    watch.Restart(); _ = File.ReadLines(manifest).Single(line => line.StartsWith("revision-002|", StringComparison.Ordinal)); watch.Stop(); var revision = watch.Elapsed.TotalMilliseconds;
    watch.Restart(); var queryCount = File.ReadLines(path).Count(line => line.StartsWith("revision-001|SYN-00001|", StringComparison.Ordinal) && Date(line) is >= 100 and <= 199); watch.Stop(); var query = watch.Elapsed.TotalMilliseconds;
    watch.Restart(); var sequential = File.ReadLines(path).Count(line => line.StartsWith("revision-001|", StringComparison.Ordinal)); watch.Stop(); var read = watch.Elapsed.TotalMilliseconds;
    watch.Restart(); var checksum = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))); watch.Stop(); var verify = watch.Elapsed.TotalMilliseconds;
    var bytes = new FileInfo(path).Length + new FileInfo(manifest).Length;
    watch.Restart(); File.Delete(path); watch.Stop(); var deletion = watch.Elapsed.TotalMilliseconds;
    return new Result(scale.Name, "jsonl-v1", scale.Rows, write, append, revision, query, read, verify, deletion, bytes, queryCount, sequential, checksum[..16]);
}

static Result RunSqlite(Scale scale, string root)
{
    var path = Path.Combine(root, $"{scale.Name}.sqlite");
    var connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
    using var connection = new SqliteConnection(connectionString); connection.Open();
    Execute(connection, "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; CREATE TABLE revisions(id TEXT PRIMARY KEY,parent TEXT,row_count INTEGER,complete INTEGER,checksum TEXT); CREATE TABLE bars(provider TEXT,product TEXT,policy TEXT,revision TEXT,instrument TEXT,day INTEGER,open TEXT,high TEXT,low TEXT,close TEXT,volume TEXT,PRIMARY KEY(revision,instrument,day)); CREATE INDEX ix_bars_query ON bars(revision,instrument,day); CREATE INDEX ix_bars_delete ON bars(provider,product,policy);");
    var watch = Stopwatch.StartNew();
    using (var transaction = connection.BeginTransaction())
    {
        using var command = InsertCommand(connection, transaction);
        foreach (var row in Rows(scale)) Insert(command, row, "revision-001");
        Execute(connection, "INSERT INTO revisions VALUES('revision-001',NULL,$count,1,'synthetic');", transaction, ("$count", scale.Rows));
        transaction.Commit();
    }
    watch.Stop(); var write = watch.Elapsed.TotalMilliseconds;
    watch.Restart();
    using (var transaction = connection.BeginTransaction())
    {
        using var command = InsertCommand(connection, transaction);
        foreach (var row in AppendRows(scale)) Insert(command, row, "revision-002");
        Execute(connection, "INSERT INTO revisions VALUES('revision-002','revision-001',$count,1,'synthetic');", transaction, ("$count", scale.Instruments * 5));
        transaction.Commit();
    }
    watch.Stop(); var append = watch.Elapsed.TotalMilliseconds;
    watch.Restart(); _ = Scalar(connection, "SELECT row_count FROM revisions WHERE id='revision-002'"); watch.Stop(); var revision = watch.Elapsed.TotalMilliseconds;
    watch.Restart(); var queryCount = Convert.ToInt32(Scalar(connection, "SELECT count(*) FROM bars WHERE revision='revision-001' AND instrument='SYN-00001' AND day BETWEEN 100 AND 199"), CultureInfo.InvariantCulture); watch.Stop(); var query = watch.Elapsed.TotalMilliseconds;
    watch.Restart(); var sequential = Convert.ToInt32(Scalar(connection, "SELECT count(*) FROM bars WHERE revision='revision-001' ORDER BY instrument,day"), CultureInfo.InvariantCulture); watch.Stop(); var read = watch.Elapsed.TotalMilliseconds;
    watch.Restart(); var checksum = SqliteChecksum(connection, "revision-001"); watch.Stop(); var verify = watch.Elapsed.TotalMilliseconds;
    Execute(connection, "PRAGMA wal_checkpoint(TRUNCATE);"); var bytes = new FileInfo(path).Length;
    watch.Restart(); using (var transaction = connection.BeginTransaction()) { Execute(connection, "DELETE FROM bars WHERE provider='SyntheticFixture' AND product='Synthetic-EOD-Personal' AND policy='synthetic-policy@1';", transaction); transaction.Commit(); } watch.Stop(); var deletion = watch.Elapsed.TotalMilliseconds;
    return new Result(scale.Name, "sqlite-v1", scale.Rows, write, append, revision, query, read, verify, deletion, bytes, queryCount, sequential, checksum);
}

static IEnumerable<Row> Rows(Scale scale)
{
    for (var instrument = 0; instrument < scale.Instruments; instrument++)
        for (var day = 0; day < scale.Days; day++) yield return Create(instrument, day);
}
static IEnumerable<Row> AppendRows(Scale scale)
{
    for (var instrument = 0; instrument < scale.Instruments; instrument++)
        for (var day = scale.Days; day < scale.Days + 5; day++) yield return Create(instrument, day);
}
static Row Create(int instrument, int day)
{
    var close = 10m + instrument * 0.01m + day * 0.001m;
    return new Row($"SYN-{instrument:D5}", day, close - 0.1m, close + 0.2m, close - 0.2m, close, 1000 + instrument + day);
}
static string Line(Row row, string revision) => string.Create(CultureInfo.InvariantCulture, $"{revision}|{row.Instrument}|{row.Day}|{row.Open:F4}|{row.High:F4}|{row.Low:F4}|{row.Close:F4}|{row.Volume:F0}|SyntheticFixture|Synthetic-EOD-Personal|synthetic-policy@1");
static int Date(string line) { var first = line.IndexOf('|'); var second = line.IndexOf('|', first + 1); var third = line.IndexOf('|', second + 1); return int.Parse(line.AsSpan(second + 1, third - second - 1), CultureInfo.InvariantCulture); }

static SqliteCommand InsertCommand(SqliteConnection connection, SqliteTransaction transaction)
{
    var command = connection.CreateCommand(); command.Transaction = transaction;
    command.CommandText = "INSERT INTO bars VALUES('SyntheticFixture','Synthetic-EOD-Personal','synthetic-policy@1',$revision,$instrument,$day,$open,$high,$low,$close,$volume);";
    foreach (var name in new[] { "$revision", "$instrument", "$day", "$open", "$high", "$low", "$close", "$volume" }) command.Parameters.Add(new SqliteParameter(name, null));
    return command;
}
static void Insert(SqliteCommand command, Row row, string revision)
{
    command.Parameters["$revision"].Value = revision; command.Parameters["$instrument"].Value = row.Instrument; command.Parameters["$day"].Value = row.Day;
    command.Parameters["$open"].Value = row.Open.ToString(CultureInfo.InvariantCulture); command.Parameters["$high"].Value = row.High.ToString(CultureInfo.InvariantCulture);
    command.Parameters["$low"].Value = row.Low.ToString(CultureInfo.InvariantCulture); command.Parameters["$close"].Value = row.Close.ToString(CultureInfo.InvariantCulture);
    command.Parameters["$volume"].Value = row.Volume.ToString(CultureInfo.InvariantCulture); command.ExecuteNonQuery();
}
static string SqliteChecksum(SqliteConnection connection, string revision)
{
    using var command = connection.CreateCommand(); command.CommandText = "SELECT instrument,day,open,high,low,close,volume FROM bars WHERE revision=$revision ORDER BY instrument,day";
    command.Parameters.AddWithValue("$revision", revision); using var reader = command.ExecuteReader(); using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    while (reader.Read()) hash.AppendData(Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture, $"{reader.GetString(0)}|{reader.GetInt32(1)}|{reader.GetString(2)}|{reader.GetString(3)}|{reader.GetString(4)}|{reader.GetString(5)}|{reader.GetString(6)}\n")));
    return Convert.ToHexString(hash.GetHashAndReset())[..16];
}
static object? Scalar(SqliteConnection connection, string sql) { using var command = connection.CreateCommand(); command.CommandText = sql; return command.ExecuteScalar(); }
static void Execute(SqliteConnection connection, string sql, SqliteTransaction? transaction = null, params (string Name, object Value)[] parameters)
{ using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = sql; foreach (var value in parameters) command.Parameters.AddWithValue(value.Name, value.Value); command.ExecuteNonQuery(); }

sealed record Scale(string Name, int Instruments, int Days) { public int Rows => Instruments * Days; }
sealed record Row(string Instrument, int Day, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);
sealed record Result(string Scale, string Candidate, int Rows, double InitialWriteMs, double AppendMs, double ExactRevisionLookupMs,
    double InstrumentRangeQueryMs, double SequentialReadMs, double IntegrityVerifyMs, double ProviderDeletionMs, long Bytes,
    int QueryRows, int SequentialRows, string IntegrityFingerprint);
