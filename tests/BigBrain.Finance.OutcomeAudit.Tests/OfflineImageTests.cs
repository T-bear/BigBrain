using System.Text.Json;
using BigBrain.Finance.OutcomeAudit;
using Microsoft.Data.Sqlite;
using Xunit;

namespace BigBrain.Finance.OutcomeAudit.Tests;

public sealed class OfflineImageTests
{
    private readonly ITestOutputHelper _output;
    public OfflineImageTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void LiveWalIsRejectedWithoutOpeningOrChangingAnyFile()
    {
        using var ledger = new SyntheticLedger(wal: true);
        Assert.True(File.Exists(ledger.DatabasePath + "-wal"));
        Assert.True(File.Exists(ledger.DatabasePath + "-shm"));
        var before = OutcomeAuditTests.Files(ledger.DirectoryPath);
        Assert.Equal("image.sidecar", ledger.Read().StopCode);
        Assert.Equal(before, OutcomeAuditTests.Files(ledger.DirectoryPath));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WalWithoutShmOrWithHiddenSidecarsStillFailsClosed(bool includeWal)
    {
        using var ledger = new SyntheticLedger(wal: true);
        var copy = Path.Combine(ledger.DirectoryPath, "synthetic-unsafe.db");
        File.Copy(ledger.DatabasePath, copy); // Synthetic negative fixture, never production.
        if (includeWal) File.Copy(ledger.DatabasePath + "-wal", copy + "-wal");
        var before = OutcomeAuditTests.Files(ledger.DirectoryPath);
        var report = OfflineImage.Read(copy, SyntheticLedger.Digest(copy), SyntheticLedger.SnapshotUtc);
        Assert.Equal(includeWal ? "image.sidecar" : "image.not-complete-rollback-image", report.StopCode);
        Assert.False(File.Exists(copy + "-shm"));
        Assert.Equal(before, OutcomeAuditTests.Files(ledger.DirectoryPath));
    }

    [Fact]
    public void PlainReadonlyWalCanCreateShmInExactDotnetProvider()
    {
        using var ledger = new SyntheticLedger(wal: true);
        var copy = Path.Combine(ledger.DirectoryPath, "synthetic-ro-hazard.db");
        File.Copy(ledger.DatabasePath, copy);
        File.Copy(ledger.DatabasePath + "-wal", copy + "-wal");
        var databaseHash = SyntheticLedger.Digest(copy);
        var walHash = SyntheticLedger.Digest(copy + "-wal");
        using (var readOnly = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = copy, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
        {
            readOnly.Open();
            using var command = readOnly.CreateCommand();
            command.CommandText = "PRAGMA query_only=ON; BEGIN; SELECT COUNT(*) FROM shadow_outcomes;";
            Assert.Equal(1L, command.ExecuteScalar());
            Assert.True(File.Exists(copy + "-shm")); // SQL read-only did not provide filesystem no-write.
            command.CommandText = "ROLLBACK; SELECT sqlite_version();";
            _output.WriteLine("Microsoft.Data.Sqlite=" + typeof(SqliteConnection).Assembly.GetName().Version + "; SQLite=" + command.ExecuteScalar());
        }
        Assert.Equal(databaseHash, SyntheticLedger.Digest(copy));
        Assert.Equal(walHash, SyntheticLedger.Digest(copy + "-wal"));
    }

    [Fact]
    public void ExistingShmPlainReadonlyIsCharacterizedButNeverAuthorized()
    {
        using var ledger = new SyntheticLedger(wal: true);
        var before = SyntheticLedger.Digest(ledger.DatabasePath + "-shm");
        using var readOnly = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = ledger.DatabasePath, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        readOnly.Open();
        using var command = readOnly.CreateCommand();
        command.CommandText = "PRAGMA query_only=ON; BEGIN; SELECT COUNT(*) FROM shadow_outcomes;";
        Assert.Equal(1L, command.ExecuteScalar());
        var during = SyntheticLedger.Digest(ledger.DatabasePath + "-shm");
        _output.WriteLine("Existing SHM bytes changed during plain read-only transaction: " + (before != during));
        // Presence alone rejects, regardless of whether this VFS changed a byte on this run.
        Assert.Equal("image.sidecar", ledger.Read().StopCode);
        command.CommandText = "ROLLBACK";
        command.ExecuteNonQuery();
    }

    [Fact]
    public void ImmutableOnLiveWalOmitsCommittedTablesAndIsNotAShortcut()
    {
        using var ledger = new SyntheticLedger(wal: true);
        using var unsafeReader = OfflineImage.OpenImmutable(ledger.DatabasePath); // Deliberate synthetic negative control.
        using var command = unsafeReader.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM shadow_outcomes";
        Assert.Throws<SqliteException>(() => command.ExecuteScalar());
        Assert.Equal("image.sidecar", ledger.Read().StopCode);
    }

    [Fact]
    public void CompletedSyntheticBackupSeesWalRowsAndIsStableWhileSourceWriterAdvances()
    {
        using var ledger = new SyntheticLedger(wal: true);
        var snapshot = ledger.Snapshot();
        var snapshotHash = SyntheticLedger.Digest(snapshot);
        ledger.Execute("UPDATE shadow_outcomes SET realized_return='0.99'");
        var before = OutcomeAuditTests.Files(ledger.DirectoryPath);
        var report = OfflineImage.Read(snapshot, snapshotHash, SyntheticLedger.SnapshotUtc);
        Assert.Empty(report.Findings);
        Assert.Equal(1, report.Counts["shadow_outcomes"]);
        Assert.Equal(1, report.Counts["v2-criteria-satisfied"]);
        Assert.Equal(before, OutcomeAuditTests.Files(ledger.DirectoryPath));
        Assert.False(File.Exists(snapshot + "-shm"));
        Assert.False(File.Exists(snapshot + "-wal"));
    }

    [Fact]
    public void UncommittedSourceRowsAreNotPartOfConsistentSyntheticSnapshot()
    {
        using var ledger = new SyntheticLedger(wal: true);
        using var secondWriter = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = ledger.DatabasePath, Pooling = false }.ToString());
        secondWriter.Open();
        using var transaction = secondWriter.BeginTransaction();
        using var command = secondWriter.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE shadow_outcomes SET realized_return='0.99'";
        command.ExecuteNonQuery();
        var snapshot = ledger.Snapshot();
        var report = OfflineImage.Read(snapshot, SyntheticLedger.Digest(snapshot), SyntheticLedger.SnapshotUtc);
        Assert.Empty(report.Findings);
        Assert.Equal(1, report.Counts["shadow_outcomes"]);
    }

    [Theory]
    [InlineData("UPDATE shadow_predictions SET reasons_json='[]'")]
    [InlineData("DELETE FROM shadow_outcomes")]
    [InlineData("CREATE TABLE forbidden(id INT)")]
    [InlineData("CREATE TEMP TABLE forbidden(id INT)")]
    public void ReadonlyAndQueryOnlyDenyWritesWithUnchangedBytes(string sql)
    {
        using var ledger = new SyntheticLedger();
        var before = OutcomeAuditTests.Files(ledger.DirectoryPath);
        using (var readOnly = OfflineImage.OpenImmutable(ledger.DatabasePath))
        using (var transaction = readOnly.BeginTransaction(deferred: true))
        using (var command = readOnly.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = sql;
            Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
        }
        Assert.Equal(before, OutcomeAuditTests.Files(ledger.DirectoryPath));
    }

    [Fact]
    public void SqliteReadonlyIndependentlyDeniesDmlIfQueryOnlyIsDisabledInTest()
    {
        using var ledger = new SyntheticLedger();
        using var readOnly = OfflineImage.OpenImmutable(ledger.DatabasePath);
        using var command = readOnly.CreateCommand();
        command.CommandText = "PRAGMA query_only=OFF; DELETE FROM shadow_outcomes";
        var error = Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
        Assert.Equal(8, error.SqliteErrorCode); // SQLITE_READONLY
    }

    [Theory]
    [InlineData("-wal")]
    [InlineData("-shm")]
    [InlineData("-journal")]
    public void AnySidecarIsRejectedEvenForARollbackImage(string suffix)
    {
        using var ledger = new SyntheticLedger();
        File.WriteAllText(ledger.DatabasePath + suffix, "synthetic-sidecar");
        var before = OutcomeAuditTests.Files(ledger.DirectoryPath);
        Assert.Equal("image.sidecar", ledger.Read().StopCode);
        Assert.Equal(before, OutcomeAuditTests.Files(ledger.DirectoryPath));
    }

    [Fact]
    public void DigestMismatchMissingImageSchemaOrLinkNeverReturnsPartialSuccess()
    {
        using var ledger = new SyntheticLedger();
        Assert.Equal("image.digest", OfflineImage.Read(ledger.DatabasePath, "wrong", SyntheticLedger.SnapshotUtc).StopCode);
        Assert.Equal("image.io", OfflineImage.Read(Path.Combine(ledger.DirectoryPath, "missing.db"), "wrong", SyntheticLedger.SnapshotUtc).StopCode);
        Assert.Single(Directory.GetFiles(ledger.DirectoryPath));
        var link = Path.Combine(ledger.DirectoryPath, "synthetic-link.db");
        File.CreateSymbolicLink(link, ledger.DatabasePath);
        Assert.Equal("image.link", OfflineImage.Read(link, SyntheticLedger.Digest(ledger.DatabasePath), SyntheticLedger.SnapshotUtc).StopCode);
        ledger.Execute("DROP TABLE shadow_outcomes");
        Assert.Equal("schema.table", ledger.Read().StopCode);
    }

    [Fact]
    public void ViewCannotSupplyLedgerAndReportContainsNoExceptionOrPath()
    {
        using var ledger = new SyntheticLedger();
        ledger.Execute("ALTER TABLE shadow_outcomes RENAME TO hidden; CREATE VIEW shadow_outcomes AS SELECT * FROM hidden");
        var report = ledger.Read();
        Assert.Equal("schema.table", report.StopCode);
        Assert.Empty(report.Counts);
        Assert.DoesNotContain(ledger.DirectoryPath, JsonSerializer.Serialize(report), StringComparison.Ordinal);
    }

    [Fact]
    public void SnapshotUtcIsRequiredAndFutureEvidenceIsRejected()
    {
        using var ledger = new SyntheticLedger();
        var hash = SyntheticLedger.Digest(ledger.DatabasePath);
        Assert.Equal("image.snapshot-clock", OfflineImage.Read(ledger.DatabasePath, hash, default).StopCode);
        Assert.Equal("image.snapshot-clock", OfflineImage.Read(ledger.DatabasePath, hash, SyntheticLedger.SnapshotUtc.ToOffset(TimeSpan.FromHours(1))).StopCode);
        var report = OfflineImage.Read(ledger.DatabasePath, hash, ledger.SourceKnown);
        Assert.Contains("time.after-snapshot", report.Findings.Keys);
        Assert.Equal(0, report.Counts["v2-criteria-satisfied"]);
    }

    [Fact]
    public void RowBoundStopsWithoutReturningTruncatedCounts()
    {
        using var ledger = new SyntheticLedger();
        ledger.Execute("WITH RECURSIVE n(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM n WHERE x<100000) INSERT INTO revisions SELECT 'synthetic-'||x,'hash','time',0,0 FROM n;");
        var report = ledger.Read();
        Assert.Equal("ledger.limit", report.StopCode);
        Assert.Empty(report.Counts);
    }

    [Fact]
    public void IsolatedLibraryHasNoApplicationBrokerOrNetworkAssemblyReference()
    {
        var references = typeof(OfflineImage).Assembly.GetReferencedAssemblies().Select(name => name.Name!).ToArray();
        Assert.DoesNotContain(references, name => name.StartsWith("BigBrain.", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("System.Net", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.Contains("Alpaca", StringComparison.OrdinalIgnoreCase));
        Assert.Null(typeof(OfflineImage).Assembly.EntryPoint); // No executable production reader shipped.
        Assert.All(typeof(OfflineImage).Assembly.GetExportedTypes(), type => Assert.StartsWith("BigBrain.Modules.Finance.Us", type.FullName!));
    }
}
