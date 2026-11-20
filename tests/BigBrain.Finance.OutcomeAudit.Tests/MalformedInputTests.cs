using System.Text;
using System.Text.Json;
using BigBrain.Finance.OutcomeAudit;
using Xunit;

namespace BigBrain.Finance.OutcomeAudit.Tests;

public sealed class MalformedInputTests
{
    private const string SensitiveMarker = "PRIVATE-SYNTHETIC-PAYLOAD";

    [Theory]
    [InlineData("shadow_predictions", "prediction_id")]
    [InlineData("shadow_outcomes", "realized_return")]
    [InlineData("revisions", "checksum")]
    [InlineData("observations", "close")]
    [InlineData("feature_revisions", "source_revisions_json")]
    public void BlobInTextColumnStopsWithoutLeakingOrChangingFiles(string table, string column)
    {
        using var ledger = new SyntheticLedger();
        ledger.Execute($"UPDATE {table} SET {column}=$value", ("$value", Encoding.UTF8.GetBytes(SensitiveMarker)));
        AssertSanitizedStop(ledger, ledger.DatabasePath, "ledger.storage-type");
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(0.01)]
    [InlineData(null)]
    public void NonTextStorageCannotBeSilentlyCoercedToLedgerText(object? value)
    {
        using var ledger = new SyntheticLedger();
        // No affinity: preserve INTEGER/REAL storage instead of TEXT affinity converting it.
        ledger.Execute("""
            CREATE TABLE malformed(outcome_id,prediction_id,target_session,source_revision_id,
                knowledge_utc,reference_close,realized_close,realized_return,direction_result,evaluated_utc);
            INSERT INTO malformed SELECT * FROM shadow_outcomes;
            DROP TABLE shadow_outcomes;
            ALTER TABLE malformed RENAME TO shadow_outcomes;
            UPDATE shadow_outcomes SET realized_return=$value;
            """, ("$value", value ?? DBNull.Value));
        AssertSanitizedStop(ledger, ledger.DatabasePath, "ledger.storage-type");
    }

    [Theory]
    [InlineData("short", "image.size")]
    [InlineData("signature", "image.not-complete-rollback-image")]
    [InlineData("page-size", "image.sqlite")]
    [InlineData("schema-page", "image.sqlite")]
    [InlineData("truncated", "image.sqlite")]
    public void CorruptOfflineImageStopsWithoutLeakingOrChangingFiles(string corruption, string code)
    {
        using var ledger = new SyntheticLedger();
        // Only a completed synthetic snapshot is corrupted; the source remains untouched.
        var path = ledger.Snapshot();
        var bytes = File.ReadAllBytes(path);
        switch (corruption)
        {
            case "short": bytes = bytes[..99]; break;
            case "signature": bytes[0] = 0; break;
            case "page-size": bytes[16] = 0; bytes[17] = 0; break;
            case "schema-page": bytes[100] = 0; break;
            case "truncated": bytes = bytes[..100]; break;
        }
        File.WriteAllBytes(path, bytes);
        // Matching digest makes the structurally corrupt cases reach SQLite validation.
        AssertSanitizedStop(ledger, path, code);
    }

    private static void AssertSanitizedStop(SyntheticLedger ledger, string path, string code)
    {
        var before = OutcomeAuditTests.Files(ledger.DirectoryPath);
        var report = OfflineImage.Read(path, SyntheticLedger.Digest(path), SyntheticLedger.SnapshotUtc);
        Assert.Equal("STOP", report.Status);
        Assert.Equal(code, report.StopCode);
        Assert.Empty(report.Counts);
        Assert.Empty(report.Findings);
        var json = JsonSerializer.Serialize(report);
        Assert.Equal("{\"Contract\":\"outcome-ledger-audit-v1\",\"Status\":\"STOP\",\"StopCode\":\"" + code + "\",\"Counts\":{},\"Findings\":{}}", json);
        Assert.DoesNotContain(SensitiveMarker, json, StringComparison.Ordinal);
        Assert.DoesNotContain(path, json, StringComparison.Ordinal);
        Assert.DoesNotContain(SyntheticLedger.Prediction, json, StringComparison.Ordinal);
        Assert.Equal(before, OutcomeAuditTests.Files(ledger.DirectoryPath));
    }
}
