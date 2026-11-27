using System.Text.RegularExpressions;
using BigBrain.Finance.OutcomeAudit;
using BigBrain.Modules.Finance;
using Xunit;

namespace BigBrain.Finance.OutcomeAudit.Tests;

public sealed class JournalProvenanceTests
{
    [Fact]
    public void SnapshotColumnsMatchActualSchemasInOrder()
    {
        Check<RevisionSnapshot>("EodhdFinanceMarketData.cs", "revisions", "revision_id checksum created_utc observation_count payload_count");
        Check<ObservationSnapshot>("EodhdFinanceMarketData.cs", "observations", "provider product policy instrument_id symbol provider_symbol mic session_date open high low close adjusted_close volume acquired_utc revision_id");
        Check<FeatureRevisionSnapshot>("FinanceFeatureStore.cs", "feature_revisions", "revision_id feature_set_id feature_set_fingerprint engine_version source_revisions_json coverage_from coverage_to value_count available_count warmup_count quality_issue_count checksum created_utc elapsed_ms price_basis");
        Check<FeatureValueSnapshot>("FinanceFeatureStore.cs", "feature_values", "revision_id instrument_id definition_id definition_version definition_fingerprint session_date value source_revision_id source_from source_to knowledge_utc state quality engine_version");
        static void Check<T>(string file, string table, string columns)
        {
            var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "schema", file));
            var declaration = Regex.Match(source, @"CREATE TABLE IF NOT EXISTS " + table + @"\((.*?)\);", RegexOptions.Singleline).Groups[1].Value;
            var actual = Regex.Matches(declaration, @"\b(\w+) (TEXT|INTEGER)\b").Select(m => m.Groups[1].Value).ToArray();
            Assert.Equal(columns.Split(' '), actual);
            var fields = typeof(T).GetConstructors().Single().GetParameters();
            Assert.Equal(actual.Length, fields.Length);
            for (var i = 0; i < fields.Length; i++)
            {
                Assert.Equal(actual[i].Replace("_", "", StringComparison.Ordinal).Replace("json", "", StringComparison.Ordinal), fields[i].Name!.ToLowerInvariant());
                var type = Nullable.GetUnderlyingType(fields[i].ParameterType) ?? fields[i].ParameterType;
                var sqlType = Regex.Matches(declaration, @"\b(\w+) (TEXT|INTEGER)\b")[i].Groups[2].Value;
                Assert.Equal(type == typeof(int) || type == typeof(long) ? "INTEGER" : "TEXT", sqlType);
            }
        }
    }

    [Fact]
    public void ValidSyntheticSnapshotsProduceEnrichedPartialWithFullRebuiltLineageOnly()
    {
        var f = Fixture.Create();
        var result = f.Evaluate();
        var row = Assert.Single(result.VerifiedPartial);
        Assert.Equal(f.P, row.Prediction);
        Assert.Equal(f.O, row.Outcome);
        Assert.Equal(f.R[0].Checksum, row.Source.Revision.Checksum);
        Assert.Equal(f.B[0].AcquiredUtc, row.Source.Observation.AcquiredUtc);
        Assert.Equal(f.F.Checksum, row.FeatureRevision.Checksum);
        Assert.Equal(f.V, row.FeatureValues);
        Assert.Equal(2, row.FeatureInputs.Count);
        Assert.Equal("source.rights-entitlement-unknown", Assert.Single(result.Diagnostics));
        Assert.Contains("observation-checksum-absent", result.NotProvable);
        Assert.Contains("independent-outcome-version-key", result.NotProvable);
        Assert.Equal("NOT_EVALUABLE", result.Report.Status);
        Assert.Equal("SYNTHETIC_ONLY", result.Report.EvidenceKind);
        Assert.Equal(ForwardMetrics.Unknown, result.Report.Metrics);
        Assert.Empty(result.Report.Groups);
        Assert.Equal(0, result.Report.GenuineFutureSessions);
        Assert.Empty(result.Report.EvidenceHash);
    }

    [Theory]
    [InlineData("missing-source", "source.observation.missing-row")]
    [InlineData("missing-revision", "observation.orphan-revision")]
    [InlineData("duplicate-observation", "source.observation.multiple-matches")]
    [InlineData("duplicate-revision", "revision.duplicate-identity")]
    [InlineData("conflict-revision", "revision.conflicting-versions")]
    [InlineData("future-source", "source.future-acquired-revision")]
    [InlineData("checksum-format", "revision.checksum-format")]
    [InlineData("checksum-id", "revision.checksum-identity")]
    [InlineData("feature-source", "feature.source-mismatch")]
    [InlineData("feature-known", "feature.knowledge-time")]
    [InlineData("value-known", "feature.value-knowledge-time")]
    [InlineData("value-source", "feature.value-source-mismatch")]
    [InlineData("value-missing", "feature.values-or-lineage-mismatch")]
    [InlineData("value-tampered", "feature.values-or-lineage-mismatch")]
    [InlineData("value-definition", "feature.values-or-lineage-mismatch")]
    [InlineData("feature-engine", "feature.definition-or-engine-mismatch")]
    [InlineData("feature-hash", "feature.checksum-mismatch")]
    [InlineData("feature-fingerprint", "feature.definition-or-engine-mismatch")]
    [InlineData("feature-count", "feature.metadata-mismatch")]
    [InlineData("feature-orphan", "feature.input-revision.missing-row")]
    [InlineData("duplicate-value", "feature-value.duplicate-identity")]
    [InlineData("prediction-version", "prediction.version-identity")]
    [InlineData("outcome-version", "outcome.version-identity")]
    [InlineData("outcome-orphan", "outcome.orphan-prediction")]
    [InlineData("outcome-price", "outcome.price-link")]
    [InlineData("outcome-time", "journal.invalid-chronology")]
    [InlineData("observation-time", "source.acquisition-revision-time")]
    [InlineData("late-feature-input", "feature.input.future-acquired-revision")]
    [InlineData("missing-history", "revision.incomplete-snapshot")]
    [InlineData("bad-rights-label", "source.source-mapping")]
    public void InvalidInputsFailClosedWithSpecificDeterministicDiagnostics(string mutation, string diagnostic)
    {
        var f = Fixture.Create();
        switch (mutation)
        {
            case "missing-source": f.B = f.B.Where(b => b.SessionDate != f.P.SessionDate).ToArray(); break;
            case "missing-revision": f.R = f.R[1..]; break;
            case "duplicate-observation": f.B = [.. f.B, f.B[1]]; break;
            case "duplicate-revision": f.R = [.. f.R, f.R[0]]; break;
            case "conflict-revision": f.R = [.. f.R, f.R[0] with { Checksum = Hash('c') }]; break;
            case "future-source": f.R[0] = f.R[0] with { CreatedUtc = f.P.CreatedAtUtc.AddDays(1) }; f.B[1] = f.B[1] with { AcquiredUtc = f.R[0].CreatedUtc }; break;
            case "checksum-format": f.R[0] = f.R[0] with { Checksum = "bad" }; break;
            case "checksum-id": f.R[0] = f.R[0] with { Checksum = Hash('c') }; break;
            case "feature-source": f.F = f.F with { SourceRevisions = [f.R[1].RevisionId] }; break;
            case "feature-known": f.F = f.F with { CreatedUtc = f.P.CreatedAtUtc.AddDays(1) }; break;
            case "value-known": f.V[0] = f.V[0] with { KnowledgeUtc = f.P.CreatedAtUtc.AddDays(1) }; break;
            case "value-source": f.V[^1] = f.V[^1] with { SourceRevisionId = f.R[1].RevisionId }; break;
            case "value-missing": f.V = f.V[1..]; break;
            case "value-tampered": f.V[^1] = f.V[^1] with { Value = 99 }; break;
            case "value-definition": f.V[0] = f.V[0] with { DefinitionFingerprint = Hash('e') }; break;
            case "feature-engine": f.F = f.F with { EngineVersion = "different" }; break;
            case "feature-hash": f.F = f.F with { Checksum = Hash('e') }; break;
            case "feature-fingerprint": f.F = f.F with { FeatureSetFingerprint = Hash('e') }; break;
            case "feature-count": f.F = f.F with { ValueCount = 999 }; break;
            case "feature-orphan": f.F = f.F with { SourceRevisions = [.. f.F.SourceRevisions, "absent"] }; break;
            case "duplicate-value": f.V = [.. f.V, f.V[0]]; break;
            case "prediction-version": f.P = f.P with { StrategyVersion = "other" }; break;
            case "outcome-version": f.O = f.O with { OutcomeId = "other" }; break;
            case "outcome-orphan": f.O = f.O with { PredictionId = "absent" }; break;
            case "outcome-price": f.O = f.O with { ReferenceClose = 999 }; break;
            case "outcome-time": f.O = f.O with { EvaluatedAtUtc = f.P.CreatedAtUtc }; break;
            case "observation-time": f.B[1] = f.B[1] with { AcquiredUtc = f.B[1].AcquiredUtc.AddTicks(-1) }; break;
            case "late-feature-input": f.F = f.F with { CreatedUtc = f.R[0].CreatedUtc.AddTicks(-1) }; break;
            case "missing-history": f.B = f.B[1..]; break;
            case "bad-rights-label": f.B[1] = f.B[1] with { Product = "unknown" }; break;
        }
        var result = f.Evaluate();
        Assert.Empty(result.VerifiedPartial);
        Assert.Contains(diagnostic, result.Diagnostics);
        Assert.Equal(result.Diagnostics, f.Evaluate(reverse: true).Diagnostics);
        Assert.Equal("NOT_EVALUABLE", result.Report.Status);
    }

    private static string Hash(char c) => "sha256:" + new string(c, 64);
    private sealed class Fixture
    {
        internal required JournalPrediction P;
        internal required JournalOutcome O;
        internal required RevisionSnapshot[] R;
        internal required ObservationSnapshot[] B;
        internal required FeatureRevisionSnapshot F;
        internal required FeatureValueSnapshot[] V;
        internal ProvenanceResult Evaluate(bool reverse = false) => JournalProvenance.EvaluateSynthetic([P], [O],
            reverse ? R.Reverse().ToArray() : R, reverse ? B.Reverse().ToArray() : B, [F],
            reverse ? V.Reverse().ToArray() : V, O.KnowledgeUtc.AddDays(3));
        internal static Fixture Create()
        {
            var date = new DateOnly(2026, 1, 6);
            var acquired = UsMarketCalendar.Session(date)!.CloseUtc.AddMinutes(10);
            var later = UsMarketCalendar.Session(date.AddDays(1))!.CloseUtc.AddMinutes(10);
            RevisionSnapshot[] r = [new("eodhd-" + new string('a', 16), Hash('a'), acquired, 2, 1),
                new("eodhd-" + new string('b', 16), Hash('b'), later, 1, 1)];
            ObservationSnapshot Bar(DateOnly d, RevisionSnapshot revision, decimal close) => new("EODHD", "Free",
                "eodhd-free-personal-v2026-08-11", "US:XNAS:SYNTH", "SYNTH", "SYNTH.US", "XNAS", d,
                close, close, close, close, close, 100, revision.CreatedUtc, revision.RevisionId);
            ObservationSnapshot[] b = [Bar(date.AddDays(-1), r[0], 99), Bar(date, r[0], 100), Bar(date.AddDays(1), r[1], 101)];
            var build = DeterministicDailyFeatureEngine.Build(b[..2].Select(b => new DailyFeatureObservation(new(b.InstrumentId),
                b.SessionDate, b.Open, b.High, b.Low, b.Close, b.Volume, b.AcquiredUtc, new(b.RevisionId))));
            var feature = new FeatureRevisionSnapshot("feature-" + build.DeterministicChecksum[7..23], build.FeatureSetId,
                build.FeatureSetFingerprint, build.EngineVersion, [r[0].RevisionId], build.CoverageFrom, build.CoverageTo,
                build.Values.Length, build.Values.Count(v => v.State == FeatureValueState.Available),
                build.Values.Count(v => v.State == FeatureValueState.Warmup), 0, build.DeterministicChecksum,
                acquired.AddMinutes(1), 0, "raw close/OHLC; provider volume classification; no adjusted-price mixing");
            var values = build.Values.Select(v => new FeatureValueSnapshot(feature.RevisionId, v.InstrumentId.Value,
                v.DefinitionId, v.DefinitionVersion, v.DefinitionFingerprint, v.SessionDate, v.Value, v.SourceRevisionId.Value,
                v.SourceFrom, v.SourceTo, v.KnowledgeTimeUtc, v.State.ToString(), v.Quality.ToString(), v.EngineVersion)).ToArray();
            var p = new JournalPrediction("", b[1].InstrumentId, b[1].Symbol, date, b[1].Provider, r[0].RevisionId,
                acquired, acquired.AddMinutes(2), feature.RevisionId, "synthetic", "v1", Hash('d'), "TargetLong",
                ForwardEvaluation.Horizon, acquired.AddMinutes(2), "Evaluated", "RESEARCH", []);
            p = p with { PredictionId = JournalProvenance.PredictionId(p) };
            var o = new JournalOutcome("", p.PredictionId, date.AddDays(1), r[1].RevisionId, later, 100, 101, .01m, "CORRECT", later.AddMinutes(1));
            o = o with { OutcomeId = JournalProvenance.OutcomeId(o) };
            return new() { P = p, O = o, R = r, B = b, F = feature, V = values };
        }
    }
}
