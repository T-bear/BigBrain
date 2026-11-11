using System.Globalization;
using System.Text;
using System.Text.Json;
using BigBrain.Api.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Tests;

public sealed class FinanceShadowOutcomeCausalityTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"bigbrain-outcome-{Guid.NewGuid():N}");
    private readonly EodhdMarketMemory _memory;
    private readonly SqliteConnection _connection;
    private readonly string _source;
    private static readonly DateTimeOffset Evaluation = DateTimeOffset.Parse("2026-10-06T22:00:00Z", CultureInfo.InvariantCulture);

    public FinanceShadowOutcomeCausalityTests()
    {
        var options = new EodhdFinanceOptions { DatabasePath = Path.Combine(_root, "memory.db"), PayloadDirectory = Path.Combine(_root, "payloads") };
        _memory = new(options);
        _source = Store("2026-10-01", "2026-10-01T22:00:00Z", 100, "source");
        _connection = new($"Data Source={options.DatabasePath}");
        _connection.Open();
        Execute("""
            INSERT INTO feature_revisions VALUES('feature-fixture','fixture','fixture','daily-feature-engine-v1',
                $sources,'2026-10-01','2026-10-01',1,1,0,0,'fixture','2026-10-01T22:01:00Z',0,'raw');
            INSERT INTO shadow_predictions VALUES('shadow-fixture','US:XNAS:AAPL','AAPL','2026-10-01','EODHD',$source,
                '2026-10-01T22:00:00Z','2026-10-01T22:02:00Z','feature-fixture','momentum','v1','fixture',
                'TargetLong','next-eligible-source-session-close-v1','2026-10-01T22:02:00Z','Pending','RESEARCH','["original-belief"]');
            """, ("$source", _source), ("$sources", JsonSerializer.Serialize(new[] { _source })));
    }

    [Fact]
    public void DelayedFutureObservationPinsFirstRevisionAndNeverRewritesEvidence()
    {
        var originalPrediction = Snapshot("shadow_predictions");
        var first = Store("2026-10-02", "2026-10-05T22:00:00Z", 101, "delayed-first");
        Store("2026-10-02", "2026-10-06T21:00:00Z", 90, "revised");
        EodhdMarketMemory.EvaluatePending(_connection, Evaluation);
        Assert.Equal(1L, Scalar("SELECT COUNT(*) FROM shadow_outcomes"));
        Assert.Equal(first, Scalar("SELECT source_revision_id FROM shadow_outcomes"));
        Assert.Equal("0.01", Scalar("SELECT realized_return FROM shadow_outcomes"));
        Assert.Equal(originalPrediction, Snapshot("shadow_predictions"));
        var evidence = Snapshot("shadow_outcomes");
        Store("2026-10-02", "2026-10-07T22:00:00Z", 120, "later-correction");
        EodhdMarketMemory.EvaluatePending(_connection, Evaluation.AddDays(5));
        Assert.Equal(evidence, Snapshot("shadow_outcomes"));
        Assert.Equal(originalPrediction, Snapshot("shadow_predictions"));
    }

    [Theory]
    [InlineData("2026-10-03T20:00:00Z", "2026-10-03T19:00:00Z")] // Exact issue #57 counterexample.
    [InlineData("2026-10-03T20:00:00Z", "2026-10-04T19:00:00Z")] // Late import cannot rescue a post-event prediction.
    [InlineData("2026-10-02T13:30:00Z", "2026-10-02T22:00:00Z")] // At target open is too late.
    [InlineData("2026-10-02T20:01:00Z", "2026-10-03T22:00:00Z")]
    public void LegacyOrLatePredictionCannotBecomeProspective(string created, string known)
    {
        Execute("UPDATE shadow_predictions SET created_utc=$t,knowledge_cutoff_utc=$t", ("$t", created));
        Store("2026-10-02", known, 101, "late");
        var before = Snapshot("shadow_predictions");
        EodhdMarketMemory.EvaluatePending(_connection, Evaluation);
        Assert.Equal(0L, Scalar("SELECT COUNT(*) FROM shadow_outcomes"));
        Assert.Equal(before, Snapshot("shadow_predictions"));
    }

    [Theory]
    [InlineData("2026-10-03", "2026-10-03T22:00:00Z")] // Weekend.
    [InlineData("2026-10-05", "2026-10-05T22:00:00Z")] // Missing next session cannot be replaced.
    [InlineData("2026-10-02", "2026-10-02T19:59:59Z")] // Bar known before its close.
    [InlineData("2026-10-02", "2026-10-07T22:00:00Z")] // Not yet known at evaluation.
    public void WrongOrUnavailableTargetFailsClosed(string date, string known)
    {
        Store(date, known, 101, "target");
        EodhdMarketMemory.EvaluatePending(_connection, Evaluation);
        Assert.Equal(0L, Scalar("SELECT COUNT(*) FROM shadow_outcomes"));
    }

    [Theory]
    [InlineData("2026-10-02T13:29:59.9999999Z", true)]
    [InlineData("2026-10-02T15:30:00+02:00", false)] // Same instant as open.
    [InlineData("2026-10-02T09:29:59-04:00", true)]
    public void PredictionBoundaryUsesInstantsAndStrictPreOpen(string created, bool expected)
    {
        Execute("UPDATE shadow_predictions SET created_utc=$t,knowledge_cutoff_utc=$t", ("$t", created));
        Store("2026-10-02", "2026-10-03T00:00:00+02:00", 101, "target-offset");
        EodhdMarketMemory.EvaluatePending(_connection, Evaluation);
        Assert.Equal(expected ? 1L : 0L, Scalar("SELECT COUNT(*) FROM shadow_outcomes"));
    }

    [Fact]
    public void RevisionSelectionOrdersActualInstantsNotTimestampText()
    {
        var first = Store("2026-10-02", "2026-10-03T00:00:00+02:00", 101, "first-offset");
        Store("2026-10-02", "2026-10-02T23:00:00Z", 90, "later-lexically-earlier");
        EodhdMarketMemory.EvaluatePending(_connection, Evaluation);
        Assert.Equal(first, Scalar("SELECT source_revision_id FROM shadow_outcomes"));
    }

    [Fact]
    public void ConflictingRevisionAtSameInstantFailsClosed()
    {
        Store("2026-10-02", "2026-10-03T00:00:00+02:00", 101, "one");
        Store("2026-10-02", "2026-10-02T22:00:00Z", 102, "two");
        EodhdMarketMemory.EvaluatePending(_connection, Evaluation);
        Assert.Equal(0L, Scalar("SELECT COUNT(*) FROM shadow_outcomes"));
    }

    [Fact]
    public void InvalidEarlierRevisionIsNotSkippedForAConvenientLaterRevision()
    {
        Store("2026-10-02", "2026-10-02T19:00:00Z", 101, "premature");
        Store("2026-10-02", "2026-10-02T22:00:00Z", 102, "later-valid");
        EodhdMarketMemory.EvaluatePending(_connection, Evaluation);
        Assert.Equal(0L, Scalar("SELECT COUNT(*) FROM shadow_outcomes"));
    }

    [Theory]
    [InlineData("2026-04-02", "2026-04-06", "2026-04-06T20:00:00Z")] // Good Friday and weekend.
    [InlineData("2026-10-30", "2026-11-02", "2026-11-02T21:00:00Z")] // DST ends.
    [InlineData("2026-12-24", "2026-12-28", "2026-12-28T21:00:00Z")] // Christmas and weekend.
    public void TargetFollowsExchangeCalendarAcrossHolidaysAndDst(string sourceDate, string targetDate, string targetClose)
    {
        var sourceKnown = sourceDate + "T22:00:00Z";
        var created = sourceDate + "T22:02:00Z";
        Execute("""
            UPDATE observations SET session_date=$date,acquired_utc=$known;
            UPDATE revisions SET created_utc=$known;
            UPDATE feature_revisions SET created_utc=$known;
            UPDATE shadow_predictions SET session_date=$date,observation_knowledge_utc=$known,knowledge_cutoff_utc=$created,created_utc=$created;
            """, ("$date", sourceDate), ("$known", sourceKnown), ("$created", created));
        Store(targetDate, targetClose, 101, "calendar-target");
        var evaluation = DateTimeOffset.Parse(targetClose, CultureInfo.InvariantCulture).AddMinutes(1);
        EodhdMarketMemory.EvaluatePending(_connection, evaluation);
        Assert.Equal(1L, Scalar("SELECT COUNT(*) FROM shadow_outcomes"));
        Assert.Equal(targetDate, Scalar("SELECT target_session FROM shadow_outcomes"));
    }

    [Theory]
    [InlineData("UPDATE shadow_predictions SET observation_knowledge_utc='2026-10-01T22:03:00Z'")]
    [InlineData("UPDATE shadow_predictions SET knowledge_cutoff_utc='2026-10-01T23:00:00Z'")]
    [InlineData("UPDATE shadow_predictions SET created_utc='2026-10-01T22:02:00'")]
    [InlineData("UPDATE shadow_predictions SET knowledge_cutoff_utc='2026-10-01T22:02:00'")]
    [InlineData("UPDATE shadow_predictions SET observation_knowledge_utc='2026-10-01T22:00:00'")]
    [InlineData("UPDATE shadow_predictions SET source_revision_id='missing'")]
    [InlineData("UPDATE shadow_predictions SET provider='OTHER'")]
    [InlineData("UPDATE shadow_predictions SET symbol='OTHER'")]
    [InlineData("UPDATE shadow_predictions SET horizon='other'")]
    [InlineData("UPDATE shadow_predictions SET state='Invalidated'")]
    [InlineData("UPDATE feature_revisions SET source_revisions_json='[]'")]
    [InlineData("UPDATE feature_revisions SET source_revisions_json=json_insert(source_revisions_json,'$[#]','missing-input')")]
    [InlineData("UPDATE feature_revisions SET source_revisions_json=json_insert(source_revisions_json,'$[#]',(SELECT revision_id FROM observations WHERE session_date='2026-10-02'))")]
    [InlineData("UPDATE feature_revisions SET created_utc='2026-10-02T22:00:00Z'")]
    [InlineData("UPDATE revisions SET checksum='sha256:broken'")]
    [InlineData("DELETE FROM revisions")]
    [InlineData("UPDATE observations SET product='other' WHERE session_date='2026-10-02'")]
    [InlineData("UPDATE observations SET policy='other' WHERE session_date='2026-10-02'")]
    [InlineData("UPDATE observations SET provider_symbol='OTHER.US' WHERE session_date='2026-10-02'")]
    [InlineData("UPDATE observations SET acquired_utc='2026-10-03T22:00:00Z' WHERE session_date='2026-10-02'")]
    [InlineData("UPDATE observations SET acquired_utc='2026-10-02T22:00:00' WHERE session_date='2026-10-02'")]
    [InlineData("UPDATE observations SET close='0' WHERE session_date='2026-10-01'")]
    public void InconsistentStoredLineageCannotCreateAnOutcome(string corruption)
    {
        Store("2026-10-02", "2026-10-02T22:00:00Z", 101, "target");
        Execute(corruption);
        var predictions = Snapshot("shadow_predictions");
        EodhdMarketMemory.EvaluatePending(_connection, Evaluation);
        Assert.Equal(0L, Scalar("SELECT COUNT(*) FROM shadow_outcomes"));
        Assert.Equal(predictions, Snapshot("shadow_predictions"));
    }

    [Fact]
    public void InvalidationPreservesOriginalReasonsAndExistingOutcomesIdempotently()
    {
        Store("2026-10-02", "2026-10-02T22:00:00Z", 101, "target");
        EodhdMarketMemory.EvaluatePending(_connection, Evaluation);
        var outcomes = Snapshot("shadow_outcomes");
        Execute("UPDATE feature_revisions SET source_revisions_json='[]'");
        var reasons = Scalar("SELECT reasons_json FROM shadow_predictions");
        EodhdMarketMemory.InvalidateBrokenLineage(_connection);
        Assert.Equal("Invalidated", Scalar("SELECT state FROM shadow_predictions"));
        Assert.Equal(reasons, Scalar("SELECT reasons_json FROM shadow_predictions"));
        var invalidated = Snapshot("shadow_predictions");
        EodhdMarketMemory.InvalidateBrokenLineage(_connection);
        EodhdMarketMemory.EvaluatePending(_connection, Evaluation.AddDays(1));
        Assert.Equal(invalidated, Snapshot("shadow_predictions"));
        Assert.Equal(outcomes, Snapshot("shadow_outcomes"));
        Execute("UPDATE shadow_predictions SET reasons_json='[\"legacy-specific-reason\",\"original-belief\"]'");
        var legacy = Snapshot("shadow_predictions");
        EodhdMarketMemory.InvalidateBrokenLineage(_connection);
        Assert.Equal(legacy, Snapshot("shadow_predictions"));
    }

    [Fact]
    public void EvaluationClockRequiresUtc()
    {
        Assert.Throws<ArgumentException>(() => EodhdMarketMemory.EvaluatePending(_connection, Evaluation.ToOffset(TimeSpan.FromHours(2))));
    }

    private string Store(string date, string known, decimal close, string payload)
    {
        var day = DateOnly.Parse(date, CultureInfo.InvariantCulture);
        var at = DateTimeOffset.Parse(known, CultureInfo.InvariantCulture);
        return _memory.Store(EodhdCatalog.Watchlist.Single(item => item.Symbol == "AAPL"),
            [new(day, close, close + 1, close - 1, close, close, 1000)], Encoding.UTF8.GetBytes(payload), day, day, at.AddMinutes(-1), at, 0);
    }

    private void Execute(string sql, params (string Name, object Value)[] values)
    {
        using var command = _connection.CreateCommand(); command.CommandText = sql;
        foreach (var (name, value) in values) command.Parameters.AddWithValue(name, value);
        command.ExecuteNonQuery();
    }

    private object? Scalar(string sql)
    {
        using var command = _connection.CreateCommand(); command.CommandText = sql; return command.ExecuteScalar();
    }

    private string Snapshot(string table)
    {
        using var command = _connection.CreateCommand(); command.CommandText = $"SELECT * FROM {table} ORDER BY 1";
        using var reader = command.ExecuteReader(); var rows = new List<object[]>();
        while (reader.Read()) { var row = new object[reader.FieldCount]; reader.GetValues(row); rows.Add(row); }
        return JsonSerializer.Serialize(rows);
    }

    public void Dispose()
    {
        _connection.Dispose(); SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
        GC.SuppressFinalize(this);
    }
}
