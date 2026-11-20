using BigBrain.Finance.OutcomeAudit;
using Microsoft.Data.Sqlite;
using Xunit;

namespace BigBrain.Finance.OutcomeAudit.Tests;

// SYNTHETIC ONLY. A closed snapshot plus a digest is not an OS seal or an operator grant.
// These tests exercise the existing internal library; no production route is installed.
public sealed class OperatorBoundaryTests
{
    [Fact]
    public void PinnedSnapshotExcludesACommitMadeDuringItsReadTransaction()
    {
        using var ledger = new SyntheticLedger(wal: true);
        using var snapshotTransaction = ledger.Writer.BeginTransaction(deferred: true);
        using var pinnedRead = ledger.Writer.CreateCommand();
        pinnedRead.Transaction = snapshotTransaction;
        pinnedRead.CommandText = "SELECT COUNT(*) FROM shadow_outcomes";
        Assert.Equal(1L, pinnedRead.ExecuteScalar()); // Establish the source snapshot before the writer commits.

        using (var concurrentWriter = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = ledger.DatabasePath, Pooling = false, DefaultTimeout = 1 }.ToString()))
        {
            concurrentWriter.Open();
            using var command = concurrentWriter.CreateCommand();
            command.CommandText = "BEGIN; DELETE FROM shadow_outcomes; DELETE FROM shadow_predictions; COMMIT; SELECT COUNT(*) FROM shadow_outcomes";
            Assert.Equal(0L, command.ExecuteScalar()); // Both changes commit while the reader stays pinned.
        }

        var snapshot = ledger.Snapshot(); // SQLite backup on the pinned source connection.
        var digest = SyntheticLedger.Digest(snapshot);
        var before = OutcomeAuditTests.Files(ledger.DirectoryPath);
        var report = OfflineImage.Read(snapshot, digest, SyntheticLedger.SnapshotUtc);
        Assert.Equal("HISTORICAL_AUDIT_ONLY", report.Status);
        Assert.Empty(report.Findings);
        Assert.Equal(1, report.Counts["shadow_predictions"]);
        Assert.Equal(1, report.Counts["shadow_outcomes"]);
        Assert.Equal(2, report.Counts["revisions"]);
        Assert.Equal(2, report.Counts["observations"]);
        Assert.Equal(1, report.Counts["feature_revisions"]);
        Assert.Equal(before, OutcomeAuditTests.Files(ledger.DirectoryPath));
        snapshotTransaction.Rollback();
        // The live WAL input is rejected, not read as an empty successful snapshot.
        Assert.Equal("image.sidecar", ledger.Read().StopCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PostReceiptReplacementWithDifferentBytesStops(bool replaceParent)
    {
        using var ledger = new SyntheticLedger();
        var staging = Path.Combine(ledger.DirectoryPath, "synthetic-staging");
        Directory.CreateDirectory(staging);
        var image = Path.Combine(staging, "image.db");
        File.Copy(ledger.Snapshot(), image);
        var receiptDigest = SyntheticLedger.Digest(image);
        if (replaceParent)
        {
            Directory.Move(staging, staging + "-old");
            Directory.CreateDirectory(staging);
        }
        else File.Move(image, image + ".old");

        ledger.Execute("DELETE FROM shadow_outcomes");
        File.Copy(ledger.DatabasePath, image); // Different, valid rollback image at the same name.
        var before = SyntheticLedger.Digest(image);
        AssertStop(OfflineImage.Read(image, receiptDigest, SyntheticLedger.SnapshotUtc), "image.digest");
        Assert.Equal(before, SyntheticLedger.Digest(image));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PostReceiptSymlinkAtLeafOrParentStops(bool replaceParent)
    {
        using var ledger = new SyntheticLedger();
        var staging = Path.Combine(ledger.DirectoryPath, "synthetic-staging");
        Directory.CreateDirectory(staging);
        var image = Path.Combine(staging, "image.db");
        File.Copy(ledger.Snapshot(), image);
        var digest = SyntheticLedger.Digest(image);
        if (replaceParent)
        {
            Directory.Move(staging, staging + "-old");
            Directory.CreateSymbolicLink(staging, staging + "-old");
        }
        else
        {
            File.Move(image, image + ".old");
            File.CreateSymbolicLink(image, image + ".old");
        }
        AssertStop(OfflineImage.Read(image, digest, SyntheticLedger.SnapshotUtc), "image.link");
        Assert.Equal(digest, SyntheticLedger.Digest(image));
    }

    [Fact]
    public void SameByteReplacementShowsDigestDoesNotAuthenticateCustody()
    {
        using var ledger = new SyntheticLedger();
        var image = ledger.Snapshot();
        var digest = SyntheticLedger.Digest(image);
        File.Move(image, image + ".old");
        File.Copy(image + ".old", image);
        var before = OutcomeAuditTests.Files(ledger.DirectoryPath);
        var report = OfflineImage.Read(image, digest, SyntheticLedger.SnapshotUtc);
        Assert.Equal("HISTORICAL_AUDIT_ONLY", report.Status);
        Assert.Empty(report.Findings);
        Assert.Equal(before, OutcomeAuditTests.Files(ledger.DirectoryPath));
        // Expected limitation: only an independent custody boundary can reject this substitution.
    }

    [Fact]
    public void ConsistentButOmittedPopulationNeedsAnExternalCompletenessReceipt()
    {
        using var ledger = new SyntheticLedger();
        var image = ledger.Snapshot();
        using (var writer = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = image, Pooling = false }.ToString()))
        {
            writer.Open();
            using var command = writer.CreateCommand();
            command.CommandText = "BEGIN; DELETE FROM shadow_outcomes; DELETE FROM shadow_predictions; COMMIT";
            command.ExecuteNonQuery();
        }
        var before = OutcomeAuditTests.Files(ledger.DirectoryPath);
        var report = OfflineImage.Read(image, SyntheticLedger.Digest(image), SyntheticLedger.SnapshotUtc);
        Assert.Equal("HISTORICAL_AUDIT_ONLY", report.Status);
        Assert.Empty(report.Findings);
        Assert.Equal(0, report.Counts["shadow_predictions"]);
        Assert.Equal(0, report.Counts["shadow_outcomes"]);
        Assert.Equal(1, ledger.Read().Counts["shadow_outcomes"]);
        Assert.Equal(before, OutcomeAuditTests.Files(ledger.DirectoryPath));
        // Structural integrity cannot prove all source rows were delivered. No scientific PASS.
    }

    [Fact]
    public void OversizedSparseImageStopsWithoutChangingItsBytesOrCreatingSidecars()
    {
        using var ledger = new SyntheticLedger();
        var image = ledger.Snapshot();
        using (var file = new FileStream(image, FileMode.Open, FileAccess.Write))
            file.SetLength(268435457); // One byte above the existing 256 MiB bound; sparse fixture.
        var digest = SyntheticLedger.Digest(image);
        var before = OutcomeAuditTests.Files(ledger.DirectoryPath);
        AssertStop(OfflineImage.Read(image, digest, SyntheticLedger.SnapshotUtc), "image.size");
        Assert.Equal(before, OutcomeAuditTests.Files(ledger.DirectoryPath));
    }

    private static void AssertStop(AuditReport report, string code)
    {
        Assert.Equal("STOP", report.Status);
        Assert.Equal(code, report.StopCode);
        Assert.Empty(report.Counts);
        Assert.Empty(report.Findings);
    }
}
