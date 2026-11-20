using System.Text.Json;
using BigBrain.Finance.OutcomeAudit;
using Microsoft.Data.Sqlite;
using Xunit;

namespace BigBrain.Finance.OutcomeAudit.Tests;

public sealed class OutcomeAuditTests
{
    [Theory]
    [InlineData("2026-09-03")]
    [InlineData("2026-09-04")] // Labor Day: Friday -> Tuesday.
    [InlineData("2026-10-30")] // DST change: UTC open shifts.
    [InlineData("2026-03-06")]
    [InlineData("2026-11-25")] // Thanksgiving and conservative normal close on short Friday.
    public void CompleteSyntheticLedgerIsHistoricalOnlyAndIdempotent(string date)
    {
        using var ledger = new SyntheticLedger(date);
        var before = Files(ledger.DirectoryPath);
        var first = ledger.Read();
        var second = ledger.Read();
        Assert.Equal("HISTORICAL_AUDIT_ONLY", first.Status);
        Assert.Null(first.StopCode);
        Assert.Empty(first.Findings);
        Assert.Equal(1, first.Counts["v2-criteria-satisfied"]);
        Assert.Equal(1, first.Counts["historical-unversioned-outcomes"]);
        Assert.Equal(2, first.Counts["payload-checksums-not-recomputed"]);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.Equal(before, Files(ledger.DirectoryPath));
        var report = JsonSerializer.Serialize(first);
        Assert.DoesNotContain("AAPL", report, StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic", report, StringComparison.Ordinal);
        Assert.DoesNotContain(ledger.DirectoryPath, report, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("UPDATE shadow_predictions SET created_utc='2026-09-04T22:00:00Z'", "time.outcome-already-known")]
    [InlineData("UPDATE shadow_predictions SET created_utc='2026-09-04T13:30:00Z'", "time.not-pre-open")]
    [InlineData("UPDATE shadow_predictions SET created_utc='2026-09-03T22:00:00'", "time.missing-or-invalid-zone")]
    [InlineData("UPDATE shadow_predictions SET created_utc='2026-09-03Z'", "time.missing-or-invalid-zone")]
    [InlineData("UPDATE shadow_outcomes SET knowledge_utc='2026-09-03T22:00:00Z'", "time.outcome-order")]
    [InlineData("UPDATE shadow_outcomes SET evaluated_utc='2026-09-04T20:00:00Z'", "time.outcome-order")]
    [InlineData("UPDATE shadow_outcomes SET target_session='2026-09-05'", "session.not-immediate-next")]
    [InlineData("UPDATE shadow_outcomes SET target_session='2026-09-08'", "session.not-immediate-next")]
    [InlineData("UPDATE shadow_predictions SET session_date='not-a-date'", "session.not-immediate-next")]
    [InlineData("UPDATE shadow_predictions SET state='Invalidated'", "prediction.invalidated")]
    [InlineData("UPDATE shadow_predictions SET state='MissedProspectiveWindow'", "prediction.state")]
    [InlineData("UPDATE shadow_predictions SET provider='OTHER'", "prediction.contract")]
    [InlineData("UPDATE shadow_predictions SET strategy_version=''", "prediction.strategy-identity")]
    [InlineData("UPDATE shadow_predictions SET symbol='PRIVATE-ROW-TEXT'", "source.mapping")]
    [InlineData("UPDATE observations SET provider_symbol='WRONG'", "target.mapping")]
    [InlineData("UPDATE observations SET product='OTHER'", "source.mapping")]
    [InlineData("UPDATE observations SET acquired_utc='2026-09-03T20:59:00Z' WHERE revision_id='eodhd-aaaaaaaaaaaaaaaa'", "source.knowledge-link")]
    [InlineData("DELETE FROM observations WHERE revision_id='eodhd-bbbbbbbbbbbbbbbb'", "target.missing-or-ambiguous-observation")]
    [InlineData("DELETE FROM revisions WHERE revision_id='eodhd-bbbbbbbbbbbbbbbb'", "target.missing-or-ambiguous-revision")]
    [InlineData("UPDATE revisions SET checksum='bad'", "target.checksum-identity")]
    [InlineData("UPDATE revisions SET created_utc='2026-09-04T22:00:00Z'", "target.revision-time")]
    [InlineData("DELETE FROM feature_revisions", "feature.missing-or-ambiguous")]
    [InlineData("UPDATE feature_revisions SET created_utc='2026-09-04T22:00:00Z'", "feature.future-or-unknown-time")]
    [InlineData("UPDATE feature_revisions SET source_revisions_json='[]'", "feature.source-membership")]
    [InlineData("UPDATE feature_revisions SET source_revisions_json='[\"eodhd-aaaaaaaaaaaaaaaa\",\"missing\"]'", "feature.input-lineage")]
    [InlineData("UPDATE feature_revisions SET source_revisions_json='[\"eodhd-aaaaaaaaaaaaaaaa\",null]'", "feature.input-lineage")]
    [InlineData("UPDATE feature_revisions SET source_revisions_json='{}'", "feature.invalid-json")]
    [InlineData("UPDATE feature_revisions SET price_basis='adjusted'", "feature.price-basis")]
    [InlineData("UPDATE shadow_outcomes SET reference_close='99'", "outcome.price-link")]
    [InlineData("UPDATE shadow_outcomes SET realized_return='0.1'", "outcome.return")]
    [InlineData("UPDATE shadow_outcomes SET direction_result='INCORRECT'", "outcome.direction")]
    [InlineData("UPDATE shadow_predictions SET signal='unknown'", "prediction.signal")]
    [InlineData("UPDATE shadow_outcomes SET outcome_id='PRIVATE-ROW-TEXT'", "outcome.identity")]
    [InlineData("UPDATE observations SET close='0'", "outcome.missing-or-invalid-price")]
    public void InvalidEvidenceHasSanitizedSeparateFindingAndNeverPasses(string mutation, string finding)
    {
        using var ledger = new SyntheticLedger();
        ledger.Execute(mutation);
        var before = Files(ledger.DirectoryPath);
        var report = ledger.Read();
        Assert.Equal("HISTORICAL_AUDIT_ONLY", report.Status);
        Assert.True(report.Findings.ContainsKey(finding), JsonSerializer.Serialize(report));
        Assert.Equal(0, report.Counts["v2-criteria-satisfied"]);
        Assert.Equal(before, Files(ledger.DirectoryPath));
        Assert.DoesNotContain("PRIVATE-ROW-TEXT", JsonSerializer.Serialize(report), StringComparison.Ordinal);
    }

    [Fact]
    public void OrphansAndDuplicateLinksAreCountedWithoutSilentJoinLoss()
    {
        using var ledger = new SyntheticLedger();
        ledger.Execute("CREATE TABLE copy AS SELECT * FROM shadow_outcomes; DROP TABLE shadow_outcomes; ALTER TABLE copy RENAME TO shadow_outcomes; INSERT INTO shadow_outcomes SELECT * FROM shadow_outcomes; UPDATE shadow_predictions SET prediction_id='missing-original';");
        var report = ledger.Read();
        Assert.Equal(2, report.Counts["shadow_outcomes"]);
        Assert.Equal(2, report.Findings["outcome.orphan-prediction"]);
        Assert.Equal(1, report.Findings["outcome.duplicate-id"]);
        Assert.Equal(1, report.Findings["outcome.duplicate-prediction"]);
        Assert.Equal(0, report.Counts["v2-criteria-satisfied"]);
    }

    [Fact]
    public void DuplicateOutcomesCannotCountAsV2Satisfied()
    {
        using var ledger = new SyntheticLedger();
        ledger.Execute("CREATE TABLE copy AS SELECT * FROM shadow_outcomes; DROP TABLE shadow_outcomes; ALTER TABLE copy RENAME TO shadow_outcomes; INSERT INTO shadow_outcomes SELECT * FROM shadow_outcomes;");
        var report = ledger.Read();
        Assert.Equal(2, report.Findings["outcome.nonunique-link"]);
        Assert.Equal(0, report.Counts["v2-criteria-satisfied"]);
    }

    [Theory]
    [InlineData(-1, "target.not-first-revision")]
    [InlineData(0, "target.ambiguous-first-revision")]
    public void EarlierOrAmbiguousRevisionCannotBeSkipped(int minutes, string finding)
    {
        using var ledger = new SyntheticLedger();
        var time = ledger.TargetKnown.AddMinutes(minutes);
        ledger.AddRevision("eodhd-eeeeeeeeeeeeeeee", time);
        ledger.AddBar("eodhd-eeeeeeeeeeeeeeee", ledger.TargetDate, time, "102");
        Assert.True(ledger.Read().Findings.ContainsKey(finding));
    }

    [Fact]
    public void LateArrivalAndLaterCorrectionPreserveFirstGenuinelyFutureOutcome()
    {
        using var ledger = new SyntheticLedger();
        ledger.Execute("UPDATE observations SET acquired_utc='2026-09-08T22:00:00Z' WHERE revision_id=$r; UPDATE revisions SET created_utc='2026-09-08T22:00:00Z' WHERE revision_id=$r; UPDATE shadow_outcomes SET knowledge_utc='2026-09-08T22:00:00Z',evaluated_utc='2026-09-08T22:01:00Z';", ("$r", SyntheticLedger.Target));
        ledger.AddRevision("eodhd-eeeeeeeeeeeeeeee", ledger.TargetKnown.AddDays(10));
        ledger.AddBar("eodhd-eeeeeeeeeeeeeeee", ledger.TargetDate, ledger.TargetKnown.AddDays(10), "102");
        Assert.Empty(ledger.Read().Findings); // Not a cohort evidence-lock approval.
    }

    [Fact]
    public void TimezonesAreInstantsNotTextOrder()
    {
        using var ledger = new SyntheticLedger();
        ledger.Execute("UPDATE shadow_outcomes SET knowledge_utc='2026-09-04T23:00:00+02:00'; UPDATE shadow_predictions SET created_utc='2026-09-04T15:29:59+02:00';");
        Assert.Empty(ledger.Read().Findings);
        ledger.AddRevision("eodhd-eeeeeeeeeeeeeeee", ledger.TargetKnown.AddMinutes(-1));
        ledger.AddBar("eodhd-eeeeeeeeeeeeeeee", ledger.TargetDate, ledger.TargetKnown.AddMinutes(-1), "102");
        ledger.Execute("UPDATE observations SET acquired_utc='2026-09-04T23:59:00+03:00' WHERE revision_id='eodhd-eeeeeeeeeeeeeeee'");
        Assert.Contains("target.not-first-revision", ledger.Read().Findings.Keys);
    }

    [Theory]
    [InlineData("UPDATE shadow_outcomes SET realized_return='-0.01',realized_close='99',direction_result='INCORRECT'; UPDATE observations SET close='99' WHERE revision_id='eodhd-bbbbbbbbbbbbbbbb';")]
    [InlineData("UPDATE shadow_outcomes SET realized_return='0',realized_close='100',direction_result='CORRECT'; UPDATE observations SET close='100' WHERE revision_id='eodhd-bbbbbbbbbbbbbbbb'; UPDATE shadow_predictions SET signal='TargetFlat';")]
    public void NegativeAndZeroReturnsAreAuditableWithoutSelectingOnlyWinners(string sql)
    {
        using var ledger = new SyntheticLedger();
        ledger.Execute(sql);
        Assert.Empty(ledger.Read().Findings);
    }

    [Fact]
    public void ForeignKeysAndUnlinkedRowsAreIncludedInWholeLedgerCounts()
    {
        using var ledger = new SyntheticLedger();
        ledger.Execute("UPDATE shadow_outcomes SET prediction_id='orphan'; DELETE FROM revisions WHERE revision_id='eodhd-aaaaaaaaaaaaaaaa'; DELETE FROM feature_revisions;");
        var report = ledger.Read();
        Assert.Equal(1, report.Counts["sqlite-foreign-key-violations"]);
        Assert.Equal(1, report.Findings["prediction.orphan-source"]);
        Assert.Equal(1, report.Findings["prediction.orphan-feature"]);
        Assert.Equal(1, report.Findings["observation.orphan-revision"]);
        Assert.Equal(1, report.Findings["outcome.orphan-prediction"]);
    }

    [Theory]
    [InlineData("revisions", "target.missing-or-ambiguous-revision")]
    [InlineData("observations", "target.missing-or-ambiguous-observation")]
    [InlineData("feature_revisions", "feature.missing-or-ambiguous")]
    public void AmbiguousLineageIsNotSilentlyDeduplicated(string table, string finding)
    {
        using var ledger = new SyntheticLedger();
        ledger.Execute($"CREATE TABLE copy AS SELECT * FROM {table}; DROP TABLE {table}; ALTER TABLE copy RENAME TO {table}; INSERT INTO {table} SELECT * FROM {table};");
        var report = ledger.Read();
        Assert.Contains(finding, report.Findings.Keys);
        Assert.Equal(0, report.Counts["v2-criteria-satisfied"]);
    }

    [Fact]
    public void UnknownFirstTimeAndFutureFeatureInputFailClosed()
    {
        using var ledger = new SyntheticLedger();
        ledger.AddRevision("eodhd-eeeeeeeeeeeeeeee", ledger.TargetKnown);
        ledger.AddBar("eodhd-eeeeeeeeeeeeeeee", ledger.TargetDate, ledger.TargetKnown, "102");
        ledger.Execute("UPDATE observations SET acquired_utc='missing-zone' WHERE revision_id='eodhd-eeeeeeeeeeeeeeee'; UPDATE feature_revisions SET source_revisions_json='[\"eodhd-aaaaaaaaaaaaaaaa\",\"eodhd-bbbbbbbbbbbbbbbb\"]';");
        var report = ledger.Read();
        Assert.Contains("target.unknown-first-revision", report.Findings.Keys);
        Assert.Contains("feature.input-lineage", report.Findings.Keys);
        Assert.Equal(0, report.Counts["v2-criteria-satisfied"]);
    }

    internal static string Files(string directory) => string.Join("\n", Directory.GetFiles(directory).Order(StringComparer.Ordinal)
        .Select(path => Path.GetFileName(path) + ":" + SyntheticLedger.Digest(path)));
}
