using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

#pragma warning disable CA1859 // IReadOnlyList is the public audit contract.

namespace BigBrain.Api.Finance;

internal sealed record FinanceSchemaState(int CurrentVersion,IReadOnlyList<int> AppliedVersions);

internal static class FinanceSchemaMigrator
{
    internal const int LatestVersion=93;
    private sealed record Migration(int Version,string Name,string Sql);
    private static readonly Migration[] Migrations=
    [
        new(1,"legacy Finance schema baseline","SELECT 1;"),
        new(90,"BB-090 macro memory foundation","""
          CREATE TABLE IF NOT EXISTS macro_revisions(revision_id TEXT PRIMARY KEY,provider TEXT NOT NULL,artifact_hash TEXT NOT NULL,created_utc TEXT NOT NULL,evidence_class TEXT NOT NULL,quality_result TEXT NOT NULL);
          CREATE TABLE IF NOT EXISTS macro_observations(revision_id TEXT NOT NULL,series_id TEXT NOT NULL,reference_period TEXT NOT NULL,value TEXT,knowledge_time_utc TEXT NOT NULL,acquired_utc TEXT NOT NULL,realtime_start TEXT NOT NULL,realtime_end TEXT NOT NULL,artifact_hash TEXT NOT NULL,evidence_class TEXT NOT NULL,PRIMARY KEY(revision_id,series_id,reference_period,knowledge_time_utc));
          """),
        new(91,"market revision price capabilities","""
          CREATE TABLE IF NOT EXISTS revision_price_capabilities(revision_id TEXT PRIMARY KEY,raw_capability TEXT NOT NULL,adjusted_capability TEXT NOT NULL,audit_version TEXT NOT NULL,evidence TEXT NOT NULL,audited_utc TEXT NOT NULL);
          """),
        new(92,"macro quarantine evidence","""
          CREATE TABLE IF NOT EXISTS macro_candidates(candidate_id TEXT PRIMARY KEY,provider TEXT NOT NULL,source_url TEXT NOT NULL,artifact_path TEXT NOT NULL,artifact_hash TEXT NOT NULL,acquired_utc TEXT NOT NULL,rights_class TEXT NOT NULL,rights_evidence_url TEXT NOT NULL,series_json TEXT NOT NULL,schema_fingerprint TEXT NOT NULL,validation_result TEXT NOT NULL,promotion_decision TEXT NOT NULL,canonical_revision_id TEXT);
          """)
        ,new(93,"BB-091 provider-neutral macro and FX metadata","""
          ALTER TABLE macro_observations ADD COLUMN provider TEXT NOT NULL DEFAULT 'FRED';
          ALTER TABLE macro_observations ADD COLUMN region TEXT NOT NULL DEFAULT 'Us';
          ALTER TABLE macro_observations ADD COLUMN unit TEXT NOT NULL DEFAULT '';
          ALTER TABLE macro_observations ADD COLUMN frequency TEXT NOT NULL DEFAULT '';
          ALTER TABLE macro_observations ADD COLUMN base_currency TEXT;
          ALTER TABLE macro_observations ADD COLUMN quote_currency TEXT;
          CREATE INDEX IF NOT EXISTS ix_macro_asof ON macro_observations(region,evidence_class,knowledge_time_utc);
          """)
    ];

    internal static FinanceSchemaState Migrate(string databasePath,Action<int>? beforeRecord=null)
    {
        var directory=Path.GetDirectoryName(Path.GetFullPath(databasePath));if(directory is not null)Directory.CreateDirectory(directory);
        using var c=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=databasePath,DefaultTimeout=30}.ToString());c.Open();Exec(c,"PRAGMA busy_timeout=30000; CREATE TABLE IF NOT EXISTS finance_schema_migrations(version INTEGER PRIMARY KEY,name TEXT NOT NULL,applied_utc TEXT NOT NULL);");
        var applied=Applied(c);foreach(var migration in Migrations.Where(x=>!applied.Contains(x.Version)).OrderBy(x=>x.Version))
        {
            Exec(c,"BEGIN IMMEDIATE;");try{if(MigrationRecorded(c,migration.Version)){Exec(c,"COMMIT;");continue;}Exec(c,migration.Sql);beforeRecord?.Invoke(migration.Version);using var record=c.CreateCommand();record.CommandText="INSERT INTO finance_schema_migrations VALUES($version,$name,$at)";record.Parameters.AddWithValue("$version",migration.Version);record.Parameters.AddWithValue("$name",migration.Name);record.Parameters.AddWithValue("$at",DateTimeOffset.UtcNow.ToString("O",CultureInfo.InvariantCulture));record.ExecuteNonQuery();Exec(c,"COMMIT;");}catch{try{Exec(c,"ROLLBACK;");}catch(SqliteException){}throw;}
        }
        var final=Applied(c).Order().ToArray();return new(final.DefaultIfEmpty(0).Max(),final);
    }

    internal static FinanceSchemaState State(string databasePath){using var c=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=databasePath}.ToString());c.Open();if(!Table(c,"finance_schema_migrations"))return new(0,[]);var applied=Applied(c).Order().ToArray();return new(applied.DefaultIfEmpty(0).Max(),applied);}
    private static HashSet<int> Applied(SqliteConnection c){using var x=c.CreateCommand();x.CommandText="SELECT version FROM finance_schema_migrations";using var r=x.ExecuteReader();var result=new HashSet<int>();while(r.Read())result.Add(r.GetInt32(0));return result;}
    private static bool MigrationRecorded(SqliteConnection c,int version){using var x=c.CreateCommand();x.CommandText="SELECT 1 FROM finance_schema_migrations WHERE version=$version";x.Parameters.AddWithValue("$version",version);return x.ExecuteScalar() is not null;}
    private static bool Table(SqliteConnection c,string table){using var x=c.CreateCommand();x.CommandText="SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name";x.Parameters.AddWithValue("$name",table);return x.ExecuteScalar() is not null;}
    private static void Exec(SqliteConnection c,string sql){using var x=c.CreateCommand();x.CommandText=sql;x.ExecuteNonQuery();}
}

internal enum AdjustedPriceCapability { AdjustedUnavailable, RawAndAdjustedValid, AdjustedSemanticsInvalid, AuditRequired }
internal sealed record RevisionPriceCapability(string RevisionId,string RawCapability,AdjustedPriceCapability AdjustedCapability,string AuditVersion,string Evidence);

internal sealed class FinanceAdjustedPriceAudit(EodhdFinanceOptions options)
{
    internal const string Version="adjusted-history-audit-v1";
    private string Connection=>new SqliteConnectionStringBuilder{DataSource=options.DatabasePath}.ToString();
    internal IReadOnlyList<RevisionPriceCapability> Audit()
    {
        FinanceSchemaMigrator.Migrate(options.DatabasePath);using var c=new SqliteConnection(Connection);c.Open();var revisions=new List<(string Id,string Provider,string Product,int Values,int Different)>();using(var x=c.CreateCommand()){x.CommandText="SELECT revision_id,provider,product,SUM(CASE WHEN adjusted_close<>'' THEN 1 ELSE 0 END),SUM(CASE WHEN adjusted_close<>'' AND adjusted_close<>close THEN 1 ELSE 0 END) FROM observations GROUP BY revision_id,provider,product ORDER BY revision_id";using var r=x.ExecuteReader();while(r.Read())revisions.Add((r.GetString(0),r.GetString(1),r.GetString(2),r.GetInt32(3),r.GetInt32(4)));}
        foreach(var row in revisions){var capability=row.Provider switch{"EODHD"=>AdjustedPriceCapability.RawAndAdjustedValid,"NASDAQ-WIKI"=>AdjustedPriceCapability.AdjustedSemanticsInvalid,_ when row.Values==0=>AdjustedPriceCapability.AdjustedUnavailable,_=>AdjustedPriceCapability.AuditRequired};var evidence=row.Provider switch{"EODHD"=>"EODHD adapter required and validated source adjusted_close; immutable payload hash retained.","NASDAQ-WIKI"=>"Pre-BB-090 importer persisted raw close into adjusted_close; immutable revision preserved and adjusted research denied.",_ when row.Values==0=>"Source revision contains no adjusted values.",_=>"Adjusted values exist but source semantics require explicit audit."};using var y=c.CreateCommand();y.CommandText="INSERT OR IGNORE INTO revision_price_capabilities VALUES($id,'RAW_ONLY_VALID',$adjusted,$version,$evidence,$at)";y.Parameters.AddWithValue("$id",row.Id);y.Parameters.AddWithValue("$adjusted",capability.ToString());y.Parameters.AddWithValue("$version",Version);y.Parameters.AddWithValue("$evidence",evidence);y.Parameters.AddWithValue("$at",DateTimeOffset.UtcNow.ToString("O",CultureInfo.InvariantCulture));y.ExecuteNonQuery();}
        return Read(c);
    }
    internal void Require(string revisionId,bool adjusted){using var c=new SqliteConnection(Connection);c.Open();using var x=c.CreateCommand();x.CommandText="SELECT adjusted_capability FROM revision_price_capabilities WHERE revision_id=$id";x.Parameters.AddWithValue("$id",revisionId);var value=x.ExecuteScalar() as string;if(!adjusted)return;if(value!=AdjustedPriceCapability.RawAndAdjustedValid.ToString())throw new InvalidOperationException("Adjusted-price research requires an explicitly valid adjusted capability.");}
    private static IReadOnlyList<RevisionPriceCapability> Read(SqliteConnection c){using var x=c.CreateCommand();x.CommandText="SELECT revision_id,raw_capability,adjusted_capability,audit_version,evidence FROM revision_price_capabilities ORDER BY revision_id";using var r=x.ExecuteReader();var rows=new List<RevisionPriceCapability>();while(r.Read())rows.Add(new(r.GetString(0),r.GetString(1),Enum.Parse<AdjustedPriceCapability>(r.GetString(2)),r.GetString(3),r.GetString(4)));return rows;}
}

internal static class FinanceClosureMaintenanceCommand
{
    internal static bool TryRun(string[] args,IConfiguration configuration)
    {
        if(args.Length==0||args[0] is not ("finance-schema-status" or "finance-adjusted-audit" or "finance-evidence-counts"))return false;
        var options=configuration.GetSection(EodhdFinanceOptions.Section).Get<EodhdFinanceOptions>()??new();
        if(args[0]=="finance-evidence-counts")
        {
            using var c=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=options.DatabasePath}.ToString());c.Open();var tables=new[]{"revisions","observations","feature_revisions","backtest_runs","robustness_evaluations","shadow_predictions","shadow_outcomes","risk_evaluations","risk_halt_audit","macro_revisions","macro_observations","macro_candidates"};var counts=new Dictionary<string,long>(StringComparer.Ordinal);foreach(var table in tables){using var exists=c.CreateCommand();exists.CommandText="SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name";exists.Parameters.AddWithValue("$name",table);if(exists.ExecuteScalar() is null){counts[table]=0;continue;}using var count=c.CreateCommand();count.CommandText=$"SELECT COUNT(*) FROM {table}";counts[table]=Convert.ToInt64(count.ExecuteScalar(),CultureInfo.InvariantCulture);}Console.WriteLine(JsonSerializer.Serialize(new{database=Path.GetFileName(options.DatabasePath),counts}));return true;
        }
        if(args[0]=="finance-schema-status")
        {
            var state=FinanceSchemaMigrator.Migrate(options.DatabasePath);Console.WriteLine(JsonSerializer.Serialize(new{state.CurrentVersion,state.AppliedVersions}));return true;
        }
        var rows=new FinanceAdjustedPriceAudit(options).Audit();Console.WriteLine(JsonSerializer.Serialize(new{auditVersion=FinanceAdjustedPriceAudit.Version,total=rows.Count,classifications=rows.GroupBy(x=>x.AdjustedCapability).ToDictionary(x=>x.Key.ToString(),x=>x.Count()),revisions=rows}));return true;
    }
}
