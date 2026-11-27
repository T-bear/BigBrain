using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BigBrain.Modules.Finance;

namespace BigBrain.Finance.OutcomeAudit;

// Typed table snapshots, in schema column order. JSON source membership is decoded by the caller.
internal sealed record RevisionSnapshot(string RevisionId, string Checksum, DateTimeOffset CreatedUtc,
    int ObservationCount, int PayloadCount);
internal sealed record ObservationSnapshot(string Provider, string Product, string Policy, string InstrumentId,
    string Symbol, string ProviderSymbol, string Mic, DateOnly SessionDate, decimal Open, decimal High,
    decimal Low, decimal Close, decimal AdjustedClose, long Volume, DateTimeOffset AcquiredUtc, string RevisionId);
internal sealed record FeatureRevisionSnapshot(string RevisionId, string FeatureSetId, string FeatureSetFingerprint,
    string EngineVersion, IReadOnlyList<string> SourceRevisions, DateOnly? CoverageFrom, DateOnly? CoverageTo,
    int ValueCount, int AvailableCount, int WarmupCount, int QualityIssueCount, string Checksum,
    DateTimeOffset CreatedUtc, long ElapsedMs, string PriceBasis);
internal sealed record FeatureValueSnapshot(string RevisionId, string InstrumentId, string DefinitionId,
    string DefinitionVersion, string DefinitionFingerprint, DateOnly SessionDate, decimal? Value,
    string SourceRevisionId, DateOnly SourceFrom, DateOnly SourceTo, DateTimeOffset KnowledgeUtc,
    string State, string Quality, string EngineVersion);
internal sealed record JoinedObservation(ObservationSnapshot Observation, RevisionSnapshot Revision);
internal sealed record VerifiedPartialProvenance(JournalPrediction Prediction, JournalOutcome Outcome,
    JoinedObservation Source, JoinedObservation Target, FeatureRevisionSnapshot FeatureRevision,
    IReadOnlyList<FeatureValueSnapshot> FeatureValues, IReadOnlyList<JoinedObservation> FeatureInputs);
internal sealed record ProvenanceResult(ForwardReport Report, IReadOnlyList<VerifiedPartialProvenance> VerifiedPartial,
    IReadOnlyList<string> Diagnostics, IReadOnlyList<string> NotProvable);

internal static class JournalProvenance
{
    private static readonly SearchValues<char> HexDigits = SearchValues.Create("0123456789abcdef");
    private static readonly string[] Gaps =
    [
        "predeclared-manifest-and-complete-cohort", "signed-knowledge-timestamp-and-original-time-zones",
        "source-rights-entitlement-and-source-identity", "event-time", "independent-outcome-version-key",
        "observation-checksum-absent", "revision-payload-checksum-not-recomputed-without-payload",
        "probability-regime-equity", "authenticity-of-supplied-snapshots"
    ];

    internal static ProvenanceResult EvaluateSynthetic(IReadOnlyList<JournalPrediction> predictions,
        IReadOnlyList<JournalOutcome> outcomes, IReadOnlyList<RevisionSnapshot> revisions,
        IReadOnlyList<ObservationSnapshot> observations, IReadOnlyList<FeatureRevisionSnapshot> features,
        IReadOnlyList<FeatureValueSnapshot> values, DateTimeOffset asOf)
    {
        var errors = new SortedSet<string>(StringComparer.Ordinal);
        var projected = new List<VerifiedPartialProvenance>();
        void Check(bool condition, string code) { if (!condition) errors.Add(code); }
        ProvenanceResult Result() => new(new(ForwardEvaluation.Contract, "SYNTHETIC_ONLY", "NOT_EVALUABLE",
            "provenance-partial-only", "", "", 0, ForwardMetrics.Unknown, []),
            errors.Count == 0 ? projected.AsReadOnly() : Array.Empty<VerifiedPartialProvenance>(),
            errors.Append("source.rights-entitlement-unknown").ToArray(), Array.AsReadOnly(Gaps));
        bool Valid<T>(IReadOnlyList<T>? rows) => rows is not null && rows.Count <= 100000 && rows.All(r => r is not null);
        if (!Valid(predictions) || !Valid(outcomes) || !Valid(revisions) || !Valid(observations) ||
            !Valid(features) || !Valid(values) || !Utc(asOf)) { errors.Add("invalid-input"); return Result(); }
        void Unique<T, TKey>(IEnumerable<T> rows, Func<T, TKey> key, string table)
        {
            foreach (var group in rows.GroupBy(key).Where(g => g.Count() > 1))
            {
                errors.Add(table + ".duplicate-identity");
                if (group.Distinct().Count() > 1) errors.Add(table + ".conflicting-versions");
            }
        }
        Unique(predictions, p => p.PredictionId, "prediction");
        Unique(outcomes, o => o.OutcomeId, "outcome");
        Unique(outcomes, o => o.PredictionId, "outcome-link");
        Unique(revisions, r => r.RevisionId, "revision");
        Unique(observations, o => (o.Provider, o.Product, o.InstrumentId, o.SessionDate, o.RevisionId), "observation");
        Unique(features, f => f.RevisionId, "feature-revision");
        Unique(values, v => (v.RevisionId, v.InstrumentId, v.DefinitionId, v.SessionDate), "feature-value");
        var revisionIndex = revisions.ToLookup(r => r.RevisionId);
        var featureIndex = features.ToLookup(f => f.RevisionId);
        T? One<T>(IEnumerable<T> rows, string label) where T : class
        {
            var matches = rows.Take(2).ToArray();
            if (matches.Length == 1) return matches[0];
            errors.Add(label + (matches.Length == 0 ? ".missing-row" : ".multiple-matches"));
            return null;
        }
        foreach (var o in outcomes) Check(predictions.Any(p => p.PredictionId == o.PredictionId), "outcome.orphan-prediction");
        foreach (var v in values) Check(featureIndex.Contains(v.RevisionId), "feature-value.orphan-revision");
        foreach (var o in observations) Check(revisionIndex.Contains(o.RevisionId), "observation.orphan-revision");
        foreach (var r in revisions)
        {
            Check(HashShape(r.Checksum), "revision.checksum-format");
            Check(HashShape(r.Checksum) && r.RevisionId == "eodhd-" + r.Checksum[7..23], "revision.checksum-identity");
            Check(Utc(r.CreatedUtc) && r.CreatedUtc <= asOf, "revision.future-or-invalid-time");
            Check(r.ObservationCount > 0 && r.ObservationCount == observations.Count(o => o.RevisionId == r.RevisionId)
                && r.PayloadCount == 1, "revision.incomplete-snapshot");
        }
        JoinedObservation? Join(ObservationSnapshot? o, string label, DateTimeOffset cutoff)
        {
            if (o is null) return null;
            var r = One(revisionIndex[o.RevisionId], label + ".revision");
            if (r is null) return null;
            Check(Utc(o.AcquiredUtc) && o.AcquiredUtc == r.CreatedUtc, label + ".acquisition-revision-time");
            Check(o.AcquiredUtc <= cutoff && r.CreatedUtc <= cutoff, label + ".future-acquired-revision");
            Check(o.SessionDate.Year is >= 2000 and <= 2100 && UsMarketCalendar.Session(o.SessionDate) is { } s
                && s.CloseUtc <= o.AcquiredUtc, label + ".knowledge-before-session-close");
            Check(o.Provider == "EODHD" && o.Product == "Free" && o.Policy == "eodhd-free-personal-v2026-08-11"
                && o.InstrumentId == $"US:{o.Mic}:{o.Symbol}" && o.ProviderSymbol == o.Symbol + ".US", label + ".source-mapping");
            Check(o.Open > 0 && o.Low > 0 && o.High >= o.Open && o.High >= o.Close && o.Low <= o.Open
                && o.Low <= o.Close && o.Close > 0 && o.Close <= 1000000000000m && o.Volume >= 0, label + ".invalid-prices");
            return new(o, r);
        }
        foreach (var p in predictions.OrderBy(p => p.PredictionId, StringComparer.Ordinal))
        {
            var o = One(outcomes.Where(o => o.PredictionId == p.PredictionId), "prediction.outcome");
            var f = One(featureIndex[p.FeatureRevisionId], "prediction.feature-revision");
            if (o is null || f is null) continue;
            Check(p.PredictionId == PredictionId(p), "prediction.version-identity");
            Check(o.OutcomeId == OutcomeId(o), "outcome.version-identity");
            Check(HashShape(p.ParameterFingerprint), "prediction.parameter-checksum-format");
            Check(p.State == "Evaluated" && p.OperatingMode == "RESEARCH" && p.Signal is "TargetLong" or "TargetFlat"
                && p.Horizon == ForwardEvaluation.Horizon, "prediction.contract");
            var source = Join(One(observations.Where(b => b.RevisionId == p.SourceRevisionId && b.Provider == p.Provider
                && b.InstrumentId == p.InstrumentId && b.SessionDate == p.SessionDate), "source.observation"), "source", p.KnowledgeCutoffUtc);
            var target = Join(One(observations.Where(b => b.RevisionId == o.SourceRevisionId && b.Provider == p.Provider
                && b.InstrumentId == p.InstrumentId && b.SessionDate == o.TargetSession), "target.observation"), "target", o.EvaluatedAtUtc);
            if (source is null || target is null) continue;
            Check(source.Observation.Symbol == p.Symbol && target.Observation.Symbol == p.Symbol, "journal.symbol-link");
            Check(source.Observation.AcquiredUtc == p.ObservationKnowledgeUtc && target.Observation.AcquiredUtc == o.KnowledgeUtc,
                "journal.knowledge-link");
            Check(new[] { p.ObservationKnowledgeUtc, p.KnowledgeCutoffUtc, p.CreatedAtUtc, o.KnowledgeUtc, o.EvaluatedAtUtc }.All(Utc)
                && p.ObservationKnowledgeUtc <= p.KnowledgeCutoffUtc && p.KnowledgeCutoffUtc <= p.CreatedAtUtc
                && UsMarketCalendar.Session(o.TargetSession) is { } session && p.CreatedAtUtc < session.OpenUtc
                && p.CreatedAtUtc < o.KnowledgeUtc && o.KnowledgeUtc <= o.EvaluatedAtUtc && o.EvaluatedAtUtc <= asOf,
                "journal.invalid-chronology");
            Check(o.TargetSession > p.SessionDate && o.TargetSession.DayNumber - p.SessionDate.DayNumber <= 7
                && !Enumerable.Range(1, Math.Clamp(o.TargetSession.DayNumber - p.SessionDate.DayNumber - 1, 0, 7))
                    .Any(d => UsMarketCalendar.IsSession(p.SessionDate.AddDays(d))), "journal.not-next-session");
            Check(o.ReferenceClose == source.Observation.Close && o.RealizedClose == target.Observation.Close, "outcome.price-link");
            if (o.ReferenceClose is > 0 and <= 1000000000000m && o.RealizedClose is > 0 and <= 1000000000000m)
                Check(o.RealizedReturn == o.RealizedClose / o.ReferenceClose - 1 && o.DirectionResult ==
                    ((p.Signal == "TargetLong") == (o.RealizedClose > o.ReferenceClose) ? "CORRECT" : "INCORRECT"), "outcome.return-direction");
            else errors.Add("outcome.invalid-prices");
            if (f.SourceRevisions is null || f.SourceRevisions.Count == 0 || f.SourceRevisions.Any(string.IsNullOrWhiteSpace))
            { errors.Add("feature.invalid-source-membership"); continue; }
            Check(f.SourceRevisions.Distinct().Count() == f.SourceRevisions.Count, "feature.duplicate-source");
            Check(f.SourceRevisions.Contains(p.SourceRevisionId), "feature.source-mismatch");
            Check(Utc(f.CreatedUtc) && f.CreatedUtc <= p.KnowledgeCutoffUtc, "feature.knowledge-time");
            Check(HashShape(f.Checksum) && HashShape(f.FeatureSetFingerprint), "feature.checksum-format");
            var inputs = new List<JoinedObservation>();
            foreach (var id in f.SourceRevisions.Order(StringComparer.Ordinal))
            {
                One(revisionIndex[id], "feature.input-revision");
                foreach (var b in observations.Where(b => b.RevisionId == id))
                    if (Join(b, "feature.input", f.CreatedUtc) is { } joined) inputs.Add(joined);
            }
            var fv = values.Where(v => v.RevisionId == f.RevisionId).OrderBy(v => v.InstrumentId, StringComparer.Ordinal)
                .ThenBy(v => v.SessionDate).ThenBy(v => v.DefinitionId, StringComparer.Ordinal).ToArray();
            Check(fv.Any(v => v.InstrumentId == p.InstrumentId && v.SessionDate == p.SessionDate), "feature.missing-prediction-values");
            Check(fv.Where(v => v.InstrumentId == p.InstrumentId && v.SessionDate == p.SessionDate)
                .All(v => v.SourceRevisionId == p.SourceRevisionId), "feature.value-source-mismatch");
            Check(fv.All(v => Utc(v.KnowledgeUtc) && v.KnowledgeUtc <= f.CreatedUtc && v.KnowledgeUtc <= p.KnowledgeCutoffUtc), "feature.value-knowledge-time");
            // No latest-revision selection: overlapping inputs are ambiguous, even if one is newer.
            if (inputs.GroupBy(i => (i.Observation.InstrumentId, i.Observation.SessionDate)).Any(g => g.Count() > 1))
            { errors.Add("feature.multiple-input-versions"); continue; }
            try
            {
                var build = DeterministicDailyFeatureEngine.Build(inputs.Select(i => i.Observation).Select(b =>
                    new DailyFeatureObservation(new(b.InstrumentId), b.SessionDate, b.Open, b.High, b.Low, b.Close,
                        b.Volume, b.AcquiredUtc, new(b.RevisionId))));
                Check(f.FeatureSetId == build.FeatureSetId && f.FeatureSetFingerprint == build.FeatureSetFingerprint
                    && f.EngineVersion == build.EngineVersion, "feature.definition-or-engine-mismatch");
                Check(f.Checksum == build.DeterministicChecksum && f.RevisionId == "feature-" + build.DeterministicChecksum[7..23], "feature.checksum-mismatch");
                Check(f.CoverageFrom == build.CoverageFrom && f.CoverageTo == build.CoverageTo && f.ValueCount == build.Values.Length
                    && f.AvailableCount == build.Values.Count(v => v.State == FeatureValueState.Available)
                    && f.WarmupCount == build.Values.Count(v => v.State == FeatureValueState.Warmup)
                    && f.QualityIssueCount == build.Values.Count(v => v.Quality != FeatureQualityState.Good)
                    && f.ElapsedMs >= 0 && f.PriceBasis == "raw close/OHLC; provider volume classification; no adjusted-price mixing", "feature.metadata-mismatch");
                var rebuilt = build.Values.Select(v => new FeatureValueSnapshot(f.RevisionId, v.InstrumentId.Value,
                    v.DefinitionId, v.DefinitionVersion, v.DefinitionFingerprint, v.SessionDate, v.Value, v.SourceRevisionId.Value,
                    v.SourceFrom, v.SourceTo, v.KnowledgeTimeUtc, v.State.ToString(), v.Quality.ToString(), v.EngineVersion));
                Check(fv.SequenceEqual(rebuilt), "feature.values-or-lineage-mismatch");
            }
            catch (Exception ex) when (ex is ArgumentException or ArithmeticException)
            { errors.Add("feature.invalid-rebuild-input"); }
            projected.Add(new(p with { ReasonCodes = p.ReasonCodes.ToArray() }, o, source, target,
                f with { SourceRevisions = Array.AsReadOnly(f.SourceRevisions.ToArray()) }, Array.AsReadOnly(fv), inputs.AsReadOnly()));
        }
        Check(predictions.Count > 0, "prediction.missing-row");
        return Result();
    }

    internal static string PredictionId(JournalPrediction p) => "shadow-" + Hash(FormattableString.Invariant(
        $"{p.InstrumentId}|{p.SessionDate:yyyy-MM-dd}|{p.SourceRevisionId}|{p.FeatureRevisionId}|{p.StrategyId}|{p.StrategyVersion}|{p.ParameterFingerprint}|{ForwardEvaluation.Horizon}|{p.KnowledgeCutoffUtc:O}"))[7..23];
    internal static string OutcomeId(JournalOutcome o) => "outcome-" + Hash(FormattableString.Invariant(
        $"{o.PredictionId}|{o.SourceRevisionId}|{o.TargetSession:yyyy-MM-dd}"))[7..23];
    private static string Hash(string text) => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    private static bool HashShape(string? value) => value is { Length: 71 } && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).IndexOfAnyExcept(HexDigits) < 0;
    private static bool Utc(DateTimeOffset value) => value != default && value.Offset == TimeSpan.Zero;
}
