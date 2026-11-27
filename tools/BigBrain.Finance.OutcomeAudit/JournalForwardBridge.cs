using BigBrain.Modules.Finance;

namespace BigBrain.Finance.OutcomeAudit;

// Synthetic in-memory mirrors of FinanceShadowResearch.cs read models. Contract tests pin
// every field/type to that source without referencing or loading the runtime API assembly.
internal sealed record JournalPrediction(string PredictionId, string InstrumentId, string Symbol, DateOnly SessionDate,
    string Provider, string SourceRevisionId, DateTimeOffset ObservationKnowledgeUtc, DateTimeOffset KnowledgeCutoffUtc,
    string FeatureRevisionId, string StrategyId, string StrategyVersion, string ParameterFingerprint, string Signal,
    string Horizon, DateTimeOffset CreatedAtUtc, string State, string OperatingMode, IReadOnlyList<string> ReasonCodes);
internal sealed record JournalOutcome(string OutcomeId, string PredictionId, DateOnly TargetSession, string SourceRevisionId,
    DateTimeOffset KnowledgeUtc, decimal ReferenceClose, decimal RealizedClose, decimal RealizedReturn, string DirectionResult,
    DateTimeOffset EvaluatedAtUtc);

// Deliberately not ForwardEvidence: a provider name is not SourceIdentity, and a revision
// identifier is not a checksum. Retain the source records, including unmapped fields.
internal sealed record PartialForwardEvidence(JournalPrediction Prediction, JournalOutcome Outcome,
    string Strategy, string StrategyVersion, string ParametersHash, string Instrument, string Horizon,
    DateOnly SourceSession, DateOnly TargetSession, string SourceRevision, DateTimeOffset SourceKnownAt,
    string FeatureRevision, DateTimeOffset Cutoff, DateTimeOffset CreatedAt, string TargetRevision,
    DateTimeOffset TargetKnownAt, DateTimeOffset EvaluatedAt, decimal ReferenceClose, decimal RealizedClose,
    bool? PredictsUp);
internal sealed record JournalForwardResult(ForwardReport Report, IReadOnlyList<PartialForwardEvidence> Mapped,
    IReadOnlyList<string> Diagnostics);

internal static class JournalForwardBridge
{
    // These fields are absent from the prediction/outcome read models, not necessarily
    // from other stores. This bridge never opens those stores or accepts enrichments.
    private static readonly string[] Missing =
    [
        "missing:ManifestHash,Cohort (no journal commitment)",
        "missing:PredictionKey.SourceIdentity,FeatureDefinitionHash,FeatureEngineVersion",
        "missing:OutcomeKey (outcome has only PredictionId, no independent version binding)",
        "missing:Source.Checksum,Source.EventAt,Source.AcquiredAt",
        "missing:Target.Checksum,Target.EventAt,Target.AcquiredAt",
        "missing:FeatureChecksum,FeatureInputs,FeatureKnownAt",
        "unverified:Retrospective,Ambiguous,complete-cohort,explicit-original-time-zones",
        "unavailable:ProbabilityUp,Regime,RegimeKnownAt,Equity (metrics remain UNKNOWN)"
    ];

    internal static JournalForwardResult EvaluateSynthetic(ForwardManifest manifest, string expectedManifestHash,
        IReadOnlyList<JournalPrediction> predictions, IReadOnlyList<JournalOutcome> outcomes, DateTimeOffset asOf)
    {
        var diagnostics = new List<string>(Missing);
        var mapped = new List<PartialForwardEvidence>();
        JournalForwardResult Stop() => new(new(ForwardEvaluation.Contract, "SYNTHETIC_ONLY", "NOT_EVALUABLE",
            "journal-evidence-incomplete", expectedManifestHash, "", 0, ForwardMetrics.Unknown,
            Array.Empty<ForwardGroup>()), mapped.AsReadOnly(), diagnostics.AsReadOnly());

        // Reuse the evaluator's manifest/boundary validation; no partial row is ever
        // promoted to ForwardEvidence, nor is an empty evidence hash claimed for it.
        var boundary = ForwardEvaluation.Evaluate(manifest, expectedManifestHash, [],
            ForwardEvaluation.EvidenceHash([]), asOf);
        if (boundary.Reason != "insufficient-or-incomplete-cohort")
        {
            diagnostics.Add("boundary:" + boundary.Reason);
            return Stop();
        }
        if (predictions is null || outcomes is null || predictions.Count > 7680 || outcomes.Count > 7680 ||
            predictions.Any(p => p is null) || outcomes.Any(o => o is null))
        {
            diagnostics.Add("invalid-input");
            return Stop();
        }
        if (predictions.Select(p => p.PredictionId).Distinct(StringComparer.Ordinal).Count() != predictions.Count ||
            outcomes.Select(o => o.OutcomeId).Distinct(StringComparer.Ordinal).Count() != outcomes.Count ||
            outcomes.Select(o => o.PredictionId).Distinct(StringComparer.Ordinal).Count() != outcomes.Count ||
            predictions.Select(p => (p.StrategyId, p.StrategyVersion, p.ParameterFingerprint, p.Provider,
                p.InstrumentId, p.Horizon, p.SessionDate)).Distinct().Count() != predictions.Count)
            diagnostics.Add("duplicate-or-ambiguous-key");

        var joined = outcomes.ToLookup(o => o.PredictionId, StringComparer.Ordinal);
        if (outcomes.Any(o => !predictions.Any(p => p.PredictionId == o.PredictionId))) diagnostics.Add("orphan-outcome");
        foreach (var p in predictions)
        {
            var matches = joined[p.PredictionId].ToArray();
            if (matches.Length != 1) { diagnostics.Add("missing-or-ambiguous-outcome"); continue; }
            var o = matches[0];
            bool? up = p.Signal switch { "TargetLong" => true, "TargetFlat" => false, _ => null };
            mapped.Add(new(p, o, p.StrategyId, p.StrategyVersion, p.ParameterFingerprint, p.InstrumentId, p.Horizon,
                p.SessionDate, o.TargetSession, p.SourceRevisionId, p.ObservationKnowledgeUtc,
                p.FeatureRevisionId, p.KnowledgeCutoffUtc, p.CreatedAtUtc, o.SourceRevisionId, o.KnowledgeUtc,
                o.EvaluatedAtUtc, o.ReferenceClose, o.RealizedClose, up));
            if (p.State != "Evaluated" || p.OperatingMode != "RESEARCH" || up is null)
                diagnostics.Add("invalid-state-mode-or-signal");
            if (!manifest.Series.Any(k => Matches(k, p)) || p.Horizon != ForwardEvaluation.Horizon)
                diagnostics.Add("unmatched-series-or-version");
            if (!Chronological(p, o, manifest.FrozenAt, asOf)) diagnostics.Add("invalid-chronology");
            if (o.ReferenceClose is <= 0 or > 1000000000000m || o.RealizedClose is <= 0 or > 1000000000000m)
                diagnostics.Add("invalid-prices");
            else if (o.RealizedReturn != o.RealizedClose / o.ReferenceClose - 1 ||
                o.DirectionResult != (up is null ? "NOT_APPLICABLE" :
                    up == (o.RealizedClose > o.ReferenceClose) ? "CORRECT" : "INCORRECT"))
                diagnostics.Add("inconsistent-outcome");
        }
        // Check the observable part of the declared panel. Even a matching panel cannot
        // prove completeness or supply missing feature/source version identities.
        if (predictions.Count != manifest.Series.Length * manifest.TargetSessions.Length || mapped.Count != predictions.Count ||
            outcomes.Count != predictions.Count || mapped.Any(r => !manifest.TargetSessions.Contains(r.TargetSession)) ||
            manifest.Series.Any(k => manifest.TargetSessions.Any(d =>
                mapped.Count(r => Matches(k, r.Prediction) && r.TargetSession == d) != 1)))
            diagnostics.Add("insufficient-or-incomplete-cohort");
        return Stop();
    }

    private static bool Matches(ForwardKey key, JournalPrediction p) => key.Strategy == p.StrategyId &&
        key.StrategyVersion == p.StrategyVersion && key.ParametersHash == p.ParameterFingerprint &&
        key.Instrument == p.InstrumentId && key.Horizon == p.Horizon;

    private static bool Chronological(JournalPrediction p, JournalOutcome o, DateTimeOffset frozen, DateTimeOffset asOf)
    {
        if (p.SessionDate.Year is < 2000 or > 2100 || o.TargetSession.Year is < 2000 or > 2100 ||
            UsMarketCalendar.Session(p.SessionDate) is not { } source ||
            UsMarketCalendar.Session(o.TargetSession) is not { } target) return false;
        var next = p.SessionDate.AddDays(1);
        while (!UsMarketCalendar.IsSession(next)) next = next.AddDays(1);
        return next == o.TargetSession &&
            new[] { p.ObservationKnowledgeUtc, p.KnowledgeCutoffUtc, p.CreatedAtUtc, o.KnowledgeUtc, o.EvaluatedAtUtc }
                .All(t => t != default && t.Offset == TimeSpan.Zero) &&
            source.CloseUtc <= p.ObservationKnowledgeUtc && p.ObservationKnowledgeUtc <= p.KnowledgeCutoffUtc &&
            frozen <= p.KnowledgeCutoffUtc && p.KnowledgeCutoffUtc <= p.CreatedAtUtc && p.CreatedAtUtc < target.OpenUtc &&
            target.CloseUtc <= o.KnowledgeUtc && p.CreatedAtUtc < o.KnowledgeUtc &&
            o.KnowledgeUtc <= o.EvaluatedAtUtc && o.EvaluatedAtUtc <= asOf;
    }
}
