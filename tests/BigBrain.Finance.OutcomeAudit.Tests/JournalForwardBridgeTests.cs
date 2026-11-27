using System.Text.RegularExpressions;
using BigBrain.Finance.OutcomeAudit;
using BigBrain.Modules.Finance;
using Xunit;

namespace BigBrain.Finance.OutcomeAudit.Tests;

public sealed class JournalForwardBridgeTests
{
    [Fact]
    public void MirrorsExactlyMatchActualReadModelFieldNamesOrderAndTypes()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "schema", "FinanceShadowResearch.cs"));
        Check(typeof(JournalPrediction), "FinanceShadowPrediction");
        Check(typeof(JournalOutcome), "FinanceShadowOutcome");
        void Check(Type mirror, string original)
        {
            var declaration = Regex.Match(source, @"record " + original + @"\((.*?)\);", RegexOptions.Singleline).Groups[1].Value;
            var expected = declaration.Split(',').Select(s => Regex.Replace(s.Trim(), @"\s+", " ")
                .Replace("FinanceShadowState", "string", StringComparison.Ordinal));
            var actual = mirror.GetConstructors().Single().GetParameters().Select(p =>
                (p.ParameterType == typeof(string) ? "string" : p.ParameterType == typeof(decimal) ? "decimal" :
                    p.ParameterType == typeof(IReadOnlyList<string>) ? "IReadOnlyList<string>" : p.ParameterType.Name) + " " + p.Name);
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void CompleteSyntheticPanelMapsOnlyActualFieldsAndNeverCreatesGenuineEvidence()
    {
        var (manifest, evidence) = ForwardEvaluationTests.Sequence();
        var (predictions, outcomes) = Project(evidence);
        var result = Evaluate(manifest, predictions, outcomes);
        Assert.Equal(120, result.Mapped.Count);
        for (var i = 0; i < evidence.Length; i++)
        {
            var e = evidence[i];
            Assert.Equal(new PartialForwardEvidence(predictions[i], outcomes[i], e.PredictionKey.Strategy,
                e.PredictionKey.StrategyVersion, e.PredictionKey.ParametersHash, e.PredictionKey.Instrument,
                e.PredictionKey.Horizon, e.SourceSession, e.TargetSession, e.Source.Revision, e.Source.KnownAt,
                e.FeatureRevision, e.Cutoff, e.CreatedAt, e.Target.Revision, e.Target.KnownAt, e.EvaluatedAt,
                e.ReferenceClose, e.RealizedClose, e.PredictsUp), result.Mapped[i]);
        }
        Assert.Equal(8, result.Diagnostics.Count);
        Assert.Contains(result.Diagnostics, d => d.Contains("FeatureInputs,FeatureKnownAt", StringComparison.Ordinal));
        Assert.Contains(result.Diagnostics, d => d.Contains("Source.Checksum", StringComparison.Ordinal));
        Assert.Contains(result.Diagnostics, d => d.Contains("RegimeKnownAt,Equity", StringComparison.Ordinal));
        Assert.Equal("", result.Report.EvidenceHash);
    }

    [Theory]
    [InlineData("late-source", "invalid-chronology")]
    [InlineData("late-cutoff", "invalid-chronology")]
    [InlineData("at-open", "invalid-chronology")]
    [InlineData("early-target", "invalid-chronology")]
    [InlineData("early-evaluation", "invalid-chronology")]
    [InlineData("future-evaluation", "invalid-chronology")]
    [InlineData("offset", "invalid-chronology")]
    [InlineData("skip-session", "invalid-chronology")]
    [InlineData("version", "unmatched-series-or-version")]
    [InlineData("parameters", "unmatched-series-or-version")]
    [InlineData("horizon", "unmatched-series-or-version")]
    [InlineData("invalidated", "invalid-state-mode-or-signal")]
    [InlineData("signal", "invalid-state-mode-or-signal")]
    [InlineData("duplicate-prediction", "duplicate-or-ambiguous-key")]
    [InlineData("duplicate-outcome", "duplicate-or-ambiguous-key")]
    [InlineData("duplicate-join", "duplicate-or-ambiguous-key")]
    [InlineData("missing-outcome", "insufficient-or-incomplete-cohort")]
    [InlineData("missing-session", "insufficient-or-incomplete-cohort")]
    [InlineData("orphan", "orphan-outcome")]
    [InlineData("return", "inconsistent-outcome")]
    public void ObservableDefectsAreDiagnosedWithoutRepair(string mutation, string diagnostic)
    {
        var (manifest, evidence) = ForwardEvaluationTests.Sequence();
        var (p, o) = Project(evidence);
        switch (mutation)
        {
            case "late-source": p[0] = p[0] with { ObservationKnowledgeUtc = p[0].KnowledgeCutoffUtc.AddTicks(1) }; break;
            case "late-cutoff": p[0] = p[0] with { KnowledgeCutoffUtc = p[0].CreatedAtUtc.AddTicks(1) }; break;
            case "at-open": p[0] = p[0] with { CreatedAtUtc = UsMarketCalendar.Session(o[0].TargetSession)!.OpenUtc }; break;
            case "early-target": o[0] = o[0] with { KnowledgeUtc = p[0].CreatedAtUtc }; break;
            case "early-evaluation": o[0] = o[0] with { EvaluatedAtUtc = o[0].KnowledgeUtc.AddTicks(-1) }; break;
            case "future-evaluation": o[0] = o[0] with { EvaluatedAtUtc = AsOf.AddDays(1) }; break;
            case "offset": p[0] = p[0] with { KnowledgeCutoffUtc = p[0].KnowledgeCutoffUtc.ToOffset(TimeSpan.FromHours(1)) }; break;
            case "skip-session": o[0] = o[0] with { TargetSession = o[1].TargetSession }; break;
            case "version": p[0] = p[0] with { StrategyVersion = "v2" }; break;
            case "parameters": p[0] = p[0] with { ParameterFingerprint = "sha256:" + new string('f', 64) }; break;
            case "horizon": p[0] = p[0] with { Horizon = "legacy" }; break;
            case "invalidated": p[0] = p[0] with { State = "Invalidated" }; break;
            case "signal": p[0] = p[0] with { Signal = "Hold" }; break;
            case "duplicate-prediction": p[1] = p[0]; break;
            case "duplicate-outcome": o[1] = o[1] with { OutcomeId = o[0].OutcomeId }; break;
            case "duplicate-join": o[1] = o[1] with { PredictionId = o[0].PredictionId }; break;
            case "missing-outcome": o = o[..119]; break;
            case "missing-session": p = p[..119]; o = o[..119]; break;
            case "orphan": o[0] = o[0] with { PredictionId = "absent" }; break;
            case "return": o[0] = o[0] with { RealizedReturn = 9m }; break;
        }
        Assert.Contains(diagnostic, Evaluate(manifest, p, o).Diagnostics);
    }

    [Fact]
    public void FlatMeansNotUpAndProviderNeverBecomesSourceIdentity()
    {
        var (manifest, evidence) = ForwardEvaluationTests.Sequence();
        var (p, o) = Project(evidence);
        p[0] = p[0] with { Signal = "TargetFlat", Provider = "a-provider-is-not-a-source-identity" };
        o[0] = o[0] with { DirectionResult = "CORRECT" };
        var result = Evaluate(manifest, p, o);
        Assert.False(result.Mapped[0].PredictsUp);
        Assert.Equal(p[0].Provider, result.Mapped[0].Prediction.Provider);
        Assert.DoesNotContain("inconsistent-outcome", result.Diagnostics);
        Assert.Contains(result.Diagnostics, d => d.StartsWith("missing:PredictionKey.SourceIdentity", StringComparison.Ordinal));
    }

    [Fact]
    public void InvalidManifestUsesExistingEvaluatorValidation()
    {
        var (manifest, evidence) = ForwardEvaluationTests.Sequence();
        var (p, o) = Project(evidence);
        Assert.Contains("boundary:invalid-manifest-or-boundary", Evaluate(manifest with { MinimumSessions = 1 }, p, o).Diagnostics);
        var result = JournalForwardBridge.EvaluateSynthetic(manifest, "tampered", p, o, AsOf);
        Assert.Contains("boundary:manifest-tampered", result.Diagnostics);
        Assert.Empty(result.Mapped);
    }

    private static readonly DateTimeOffset AsOf = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static JournalForwardResult Evaluate(ForwardManifest manifest, JournalPrediction[] p, JournalOutcome[] o)
    {
        var result = JournalForwardBridge.EvaluateSynthetic(manifest, ForwardEvaluation.ManifestHash(manifest), p, o, AsOf);
        Assert.Equal("NOT_EVALUABLE", result.Report.Status);
        Assert.Equal("SYNTHETIC_ONLY", result.Report.EvidenceKind);
        Assert.Equal(ForwardMetrics.Unknown, result.Report.Metrics);
        Assert.Equal(0, result.Report.GenuineFutureSessions);
        Assert.Empty(result.Report.Groups);
        return result;
    }

    private static (JournalPrediction[], JournalOutcome[]) Project(ForwardEvidence[] rows) =>
        (rows.Select(e => new JournalPrediction(e.PredictionId, e.PredictionKey.Instrument, "SYNTHETIC", e.SourceSession,
            "SYNTHETIC", e.Source.Revision, e.Source.KnownAt, e.Cutoff, e.FeatureRevision, e.PredictionKey.Strategy,
            e.PredictionKey.StrategyVersion, e.PredictionKey.ParametersHash, e.PredictsUp ? "TargetLong" : "TargetFlat",
            e.PredictionKey.Horizon, e.CreatedAt, "Evaluated", "RESEARCH", ["synthetic"])).ToArray(),
        rows.Select(e => new JournalOutcome(e.OutcomeId, e.PredictionId, e.TargetSession, e.Target.Revision,
            e.Target.KnownAt, e.ReferenceClose, e.RealizedClose, e.RealizedClose / e.ReferenceClose - 1,
            e.PredictsUp == (e.RealizedClose > e.ReferenceClose) ? "CORRECT" : "INCORRECT", e.EvaluatedAt)).ToArray());
}
