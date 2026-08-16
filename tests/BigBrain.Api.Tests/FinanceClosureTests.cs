using BigBrain.Api.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Tests;

public sealed class FinanceClosureTests : IDisposable
{
    private readonly string _root=Path.Combine(Path.GetTempPath(),"bb-finance-closure",Guid.NewGuid().ToString("N"));
    private string Database=>Path.Combine(_root,"finance.db");

    [Fact]
    public void OrderedMigrationsBootstrapLegacyDatabaseAndAreRestartSafe()
    {
        Directory.CreateDirectory(_root);
        using(var c=new SqliteConnection($"Data Source={Database}")){c.Open();using var x=c.CreateCommand();x.CommandText="CREATE TABLE legacy_evidence(id TEXT PRIMARY KEY); INSERT INTO legacy_evidence VALUES('preserve-me');";x.ExecuteNonQuery();}
        var first=FinanceSchemaMigrator.Migrate(Database);var second=FinanceSchemaMigrator.Migrate(Database);
        Assert.Equal(FinanceSchemaMigrator.LatestVersion,first.CurrentVersion);Assert.Equal(first.CurrentVersion,second.CurrentVersion);Assert.Equal(first.AppliedVersions,second.AppliedVersions);
        using var verify=new SqliteConnection($"Data Source={Database}");verify.Open();using var command=verify.CreateCommand();command.CommandText="SELECT id FROM legacy_evidence";Assert.Equal("preserve-me",command.ExecuteScalar());
    }

    [Fact]
    public void FailedMigrationRollsBackBeforeVersionRecordAndCanRetry()
    {
        Assert.Throws<InvalidOperationException>(()=>FinanceSchemaMigrator.Migrate(Database,v=>{if(v==91)throw new InvalidOperationException("fixture interruption");}));
        var interrupted=FinanceSchemaMigrator.State(Database);Assert.DoesNotContain(91,interrupted.AppliedVersions);
        var retried=FinanceSchemaMigrator.Migrate(Database);Assert.Equal(FinanceSchemaMigrator.LatestVersion,retried.CurrentVersion);
    }

    [Fact]
    public async Task ConcurrentMigrationAttemptsSerializeSafely()
    {
        var states=await Task.WhenAll(Enumerable.Range(0,2).Select(_=>Task.Run(()=>FinanceSchemaMigrator.Migrate(Database))));
        Assert.All(states,x=>Assert.Equal(FinanceSchemaMigrator.LatestVersion,x.CurrentVersion));
    }

    [Fact]
    public void HistoricalWikiAdjustedSemanticsAreDeniedWithoutChangingRawEvidence()
    {
        var options=new EodhdFinanceOptions{DatabasePath=Database,PayloadDirectory=Path.Combine(_root,"payloads")};_ = new EodhdMarketMemory(options);
        using(var c=new SqliteConnection($"Data Source={Database}")){c.Open();using var x=c.CreateCommand();x.CommandText="INSERT INTO revisions VALUES('wiki-old','sha256:old','2026-08-01T00:00:00Z',1,1); INSERT INTO observations VALUES('NASDAQ-WIKI','WIKI/PRICES','legacy','US:XNAS:AAPL','AAPL','AAPL','XNAS','2017-01-03','100','101','99','100','100',1000,'2026-08-01T00:00:00Z','wiki-old');";x.ExecuteNonQuery();}
        var audit=new FinanceAdjustedPriceAudit(options);var capability=audit.Audit().Single(x=>x.RevisionId=="wiki-old");
        Assert.Equal(AdjustedPriceCapability.AdjustedSemanticsInvalid,capability.AdjustedCapability);audit.Require("wiki-old",false);Assert.Throws<InvalidOperationException>(()=>audit.Require("wiki-old",true));
        using var verify=new SqliteConnection($"Data Source={Database}");verify.Open();using var command=verify.CreateCommand();command.CommandText="SELECT close||'|'||adjusted_close FROM observations WHERE revision_id='wiki-old'";Assert.Equal("100|100",command.ExecuteScalar());
    }

    public void Dispose(){if(Directory.Exists(_root))Directory.Delete(_root,true);GC.SuppressFinalize(this);}
}
