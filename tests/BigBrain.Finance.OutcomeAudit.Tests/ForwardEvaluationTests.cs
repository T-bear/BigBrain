using BigBrain.Finance.OutcomeAudit;
using BigBrain.Modules.Finance;
using Xunit;

namespace BigBrain.Finance.OutcomeAudit.Tests;

public sealed class ForwardEvaluationTests
{
    [Fact]
    public void Complete120SessionSyntheticSequenceHasExpectedMetrics()
    {
        var (manifest, rows) = Sequence();
        var report = Evaluate(manifest, rows);

        Assert.Equal("EVALUABLE", report.Status);
        Assert.Equal("SYNTHETIC_ONLY", report.EvidenceKind);
        Assert.Equal(ForwardEvaluation.Contract, report.Contract);
        Assert.Null(report.Reason);
        Assert.Equal(120, report.GenuineFutureSessions);
        Assert.Equal(ForwardEvaluation.ManifestHash(manifest), report.ManifestHash);
        Assert.Equal(ForwardEvaluation.EvidenceHash(rows), report.EvidenceHash);
        Assert.Equal(ForwardMetric.Known(0.5m), report.Metrics.Accuracy);
        Assert.Equal(ForwardMetric.Known(0.3125m), report.Metrics.BrierScore);
        Assert.Equal(ForwardMetric.Known(0.001m), report.Metrics.MeanCostFraction);
        Assert.Equal(ForwardMetric.Known(0.1m), report.Metrics.SampledNetEquityDrawdown);
        Assert.Equal(ForwardMetric.Known(0.1m), report.Metrics.WorstNetLoss);
        Assert.Equal(ForwardMetric.Known(0.1m), report.Metrics.EmpiricalNetLossTail5Percent);
        var group = Assert.Single(report.Groups);
        Assert.Equal(manifest.Series[0], group.Key);
        Assert.Null(group.Regime);
        Assert.Equal(120, group.Sessions);
        Assert.Equal(report.Metrics, group.Metrics);
    }

    [Fact]
    public void Only119SessionsCannotEvaluatePredeclaredCohort()
    {
        var (manifest, rows) = Sequence();
        AssertStopped(Evaluate(manifest, rows[..119]), "insufficient-or-incomplete-cohort");
    }

    [Fact]
    public void ChangedManifestCannotReuseOriginalCommitment()
    {
        var (manifest, rows) = Sequence();
        var report = ForwardEvaluation.Evaluate(manifest with { RegimeDefinition = "changed" },
            ForwardEvaluation.ManifestHash(manifest), rows, ForwardEvaluation.EvidenceHash(rows), AsOf(rows));
        AssertStopped(report, "manifest-tampered");
    }

    [Fact]
    public void ChangedEvidenceCannotReuseOriginalCommitment()
    {
        var (manifest, rows) = Sequence();
        var commitment = ForwardEvaluation.EvidenceHash(rows);
        rows[0] = rows[0] with { RealizedClose = 999m };
        AssertStopped(ForwardEvaluation.Evaluate(manifest, ForwardEvaluation.ManifestHash(manifest),
            rows, commitment, AsOf(rows)), "evidence-tampered");
    }

    [Fact]
    public void DuplicatePredictionCannotCountAsAnotherSession()
    {
        var (manifest, rows) = Sequence();
        rows[1] = rows[1] with { PredictionId = rows[0].PredictionId };
        AssertStopped(Evaluate(manifest, rows), "duplicate-or-ambiguous-key");
    }

    [Theory]
    [InlineData("retrospective")]
    [InlineData("source-identity-drift")]
    [InlineData("outcome-source-drift")]
    [InlineData("late-source")]
    [InlineData("late-feature")]
    [InlineData("prediction-at-open")]
    [InlineData("evaluation-before-target-known")]
    public void InvalidLineageAndChronologyFailEvenWithMatchingEvidenceHash(string mutation)
    {
        var (manifest, rows) = Sequence();
        var row = rows[0];
        var drift = row.PredictionKey with { SourceIdentity = "different-source" };
        var lateSource = row.Source with { KnownAt = row.Cutoff.AddMinutes(1) };
        rows[0] = mutation switch
        {
            "retrospective" => row with { Retrospective = true },
            "source-identity-drift" => row with { PredictionKey = drift, OutcomeKey = drift },
            "outcome-source-drift" => row with { OutcomeKey = drift },
            "late-source" => row with { Source = lateSource, FeatureInputs = [lateSource] },
            "late-feature" => row with { FeatureKnownAt = row.Cutoff.AddMinutes(1) },
            "prediction-at-open" => row with { CreatedAt = UsMarketCalendar.Session(row.TargetSession)!.OpenUtc },
            "evaluation-before-target-known" => row with { EvaluatedAt = row.Target.KnownAt.AddTicks(-1) },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation))
        };
        AssertStopped(Evaluate(manifest, rows), "invalid-lineage-or-chronology");
    }

    [Fact]
    public void ConflictingSourceRevisionCannotReplacePriorTargetEvidence()
    {
        var (manifest, rows) = Sequence();
        var changed = rows[1].Source with { Checksum = Digest('b') };
        rows[1] = rows[1] with { Source = changed, FeatureInputs = [changed] };
        AssertStopped(Evaluate(manifest, rows), "conflicting-revision");
    }

    [Fact]
    public void MissingOptionalMetricsRemainUnknownWhileAccuracyIsEvaluable()
    {
        var (manifest, rows) = Sequence();
        // One missing observation is enough: optional metrics cannot silently drop it.
        rows[0] = rows[0] with { ProbabilityUp = null, Equity = null };
        var report = Evaluate(manifest, rows);
        Assert.Equal("EVALUABLE", report.Status);
        Assert.Equal(ForwardMetric.Known(0.5m), report.Metrics.Accuracy);
        Assert.Equal(ForwardMetric.Unknown, report.Metrics.BrierScore);
        Assert.Equal(ForwardMetric.Unknown, report.Metrics.MeanCostFraction);
        Assert.Equal(ForwardMetric.Unknown, report.Metrics.SampledNetEquityDrawdown);
        Assert.Equal(ForwardMetric.Unknown, report.Metrics.WorstNetLoss);
        Assert.Equal(ForwardMetric.Unknown, report.Metrics.EmpiricalNetLossTail5Percent);
        Assert.Equal(report.Metrics, Assert.Single(report.Groups).Metrics);
    }

    private static void AssertStopped(ForwardReport report, string reason)
    {
        Assert.Equal("NOT_EVALUABLE", report.Status);
        Assert.Equal(reason, report.Reason);
        Assert.Equal(0, report.GenuineFutureSessions);
        Assert.Equal(ForwardMetrics.Unknown, report.Metrics);
        Assert.Empty(report.Groups);
    }

    private static ForwardReport Evaluate(ForwardManifest manifest, ForwardEvidence[] rows) =>
        ForwardEvaluation.Evaluate(manifest, ForwardEvaluation.ManifestHash(manifest), rows,
            ForwardEvaluation.EvidenceHash(rows), AsOf(rows));

    private static DateTimeOffset AsOf(ForwardEvidence[] rows) => rows[^1].EvaluatedAt.AddDays(1);
    private static string Digest(char value) => "sha256:" + new string(value, 64);

    internal static (ForwardManifest Manifest, ForwardEvidence[] Rows) Sequence()
    {
        var sessions = new List<DateOnly>();
        for (var date = new DateOnly(2026, 1, 5); sessions.Count < 121; date = date.AddDays(1))
            if (UsMarketCalendar.IsSession(date)) sessions.Add(date);
        var key = new ForwardKey("synthetic", "v1", Digest('a'), "synthetic-source",
            Digest('b'), "v1", "SYNTHETIC", ForwardEvaluation.Horizon);
        var manifest = new ForwardManifest("synthetic-cohort", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            120, UsMarketCalendar.Version, [key], sessions.Skip(1).ToArray(), [],
            "no-regimes", "synthetic-unitized-net-equity-v1");
        var hash = ForwardEvaluation.ManifestHash(manifest);
        var inputs = sessions.Select((date, i) =>
        {
            var close = UsMarketCalendar.Session(date)!.CloseUtc;
            return new ForwardInput($"revision-{i}", Digest('a'), close, close.AddMinutes(1), close.AddMinutes(2));
        }).ToArray();
        var rows = Enumerable.Range(0, 120).Select(i => new ForwardEvidence(
            $"prediction-{i}", $"outcome-{i}", hash, manifest.Cohort, key, key, sessions[i], sessions[i + 1],
            inputs[i], $"feature-{i}", Digest('c'), [inputs[i]], inputs[i].KnownAt.AddMinutes(1),
            inputs[i].KnownAt.AddMinutes(2), inputs[i].KnownAt.AddMinutes(3), inputs[i + 1],
            inputs[i + 1].KnownAt.AddMinutes(1), 100m, i % 2 == 0 ? 90m : 110m, true, 0.75m,
            null, null, new ForwardEquity(i % 2 == 0 ? 100m : 90m, i % 2 == 0 ? 90m : 100m, 0.001m))).ToArray();
        return (manifest, rows);
    }
}
