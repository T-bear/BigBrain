using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using BigBrain.Modules.Finance;

namespace BigBrain.Finance.OutcomeAudit;

// In-memory synthetic protocol only. No clock, persistence, adapters or runtime registration.
internal sealed record ForwardKey(string Strategy, string StrategyVersion, string ParametersHash,
    string SourceIdentity, string FeatureDefinitionHash, string FeatureEngineVersion,
    string Instrument, string Horizon);

internal sealed record ForwardManifest(string Cohort, DateTimeOffset FrozenAt, int MinimumSessions,
    string Calendar, ForwardKey[] Series, DateOnly[] TargetSessions, string[] Regimes,
    string RegimeDefinition, string EquityDefinition);

internal sealed record ForwardInput(string Revision, string Checksum, DateTimeOffset EventAt,
    DateTimeOffset AcquiredAt, DateTimeOffset KnownAt);

// Equity is a supplied, net-of-cost, unitized synthetic series sampled at target open/close.
// It is never inferred from a direction prediction or close-to-close price change.
internal sealed record ForwardEquity(decimal AtOpen, decimal AtClose, decimal? CostFraction);

internal sealed record ForwardEvidence(string PredictionId, string OutcomeId, string ManifestHash,
    string Cohort, ForwardKey PredictionKey, ForwardKey OutcomeKey, DateOnly SourceSession,
    DateOnly TargetSession, ForwardInput Source, string FeatureRevision, string FeatureChecksum,
    ForwardInput[] FeatureInputs, DateTimeOffset FeatureKnownAt, DateTimeOffset Cutoff,
    DateTimeOffset CreatedAt, ForwardInput Target, DateTimeOffset EvaluatedAt,
    decimal ReferenceClose, decimal RealizedClose, bool PredictsUp, decimal? ProbabilityUp,
    string? Regime, DateTimeOffset? RegimeKnownAt, ForwardEquity? Equity,
    bool Invalidated = false, bool Retrospective = false, bool Ambiguous = false);

internal sealed record ForwardMetric(string Status, decimal? Value)
{
    internal static ForwardMetric Unknown { get; } = new("UNKNOWN", null);
    internal static ForwardMetric Known(decimal value) => new("KNOWN", value);
}

internal sealed record ForwardMetrics(ForwardMetric Accuracy, ForwardMetric BrierScore,
    ForwardMetric MeanCostFraction, ForwardMetric SampledNetEquityDrawdown,
    ForwardMetric WorstNetLoss, ForwardMetric EmpiricalNetLossTail5Percent)
{
    internal static ForwardMetrics Unknown { get; } = new(ForwardMetric.Unknown, ForwardMetric.Unknown,
        ForwardMetric.Unknown, ForwardMetric.Unknown, ForwardMetric.Unknown, ForwardMetric.Unknown);
}

internal sealed record ForwardGroup(ForwardKey Key, string? Regime, int Sessions, ForwardMetrics Metrics);
internal sealed record ForwardReport(string Contract, string EvidenceKind, string Status, string? Reason,
    string ManifestHash, string EvidenceHash, int GenuineFutureSessions,
    ForwardMetrics Metrics, IReadOnlyList<ForwardGroup> Groups);

internal static class ForwardEvaluation
{
    internal const string Contract = "synthetic-forward-outcome-v1";
    internal const string Horizon = "next-eligible-source-session-close-v1";
    internal const int MinimumFutureSessions = 120;
    private const int MaximumSessions = 240;
    private const int MaximumSeries = 32;
    private static readonly SearchValues<char> HashCharacters = SearchValues.Create("0123456789abcdef");

    // Commitments must be retained separately by the caller before evaluation. These are
    // content hashes, not signatures, clock attestations, source completeness or authority.
    internal static string ManifestHash(ForwardManifest manifest) => Hash(new { Contract, Manifest = manifest });
    internal static string EvidenceHash(IEnumerable<ForwardEvidence> evidence) =>
        Hash(evidence.OrderBy(row => row.PredictionId, StringComparer.Ordinal).ToArray());

    internal static ForwardReport Evaluate(ForwardManifest manifest, string expectedManifestHash,
        IReadOnlyList<ForwardEvidence> evidence, string expectedEvidenceHash, DateTimeOffset asOf)
    {
        ForwardReport Stop(string reason) => new(Contract, "SYNTHETIC_ONLY", "NOT_EVALUABLE", reason,
            expectedManifestHash, expectedEvidenceHash, 0, ForwardMetrics.Unknown, Array.Empty<ForwardGroup>());

        if (manifest is null || evidence is null || !ValidManifest(manifest) || !Utc(asOf) || asOf < manifest.FrozenAt)
            return Stop("invalid-manifest-or-boundary");
        if (ManifestHash(manifest) != expectedManifestHash) return Stop("manifest-tampered");
        if (evidence.Count > MaximumSessions * MaximumSeries) return Stop("evidence-limit");
        var rows = evidence.ToArray();
        if (rows.Any(row => row is null)) return Stop("invalid-row");
        if (EvidenceHash(rows) != expectedEvidenceHash) return Stop("evidence-tampered");
        if (rows.Select(row => row.PredictionId).Distinct(StringComparer.Ordinal).Count() != rows.Length ||
            rows.Select(row => row.OutcomeId).Distinct(StringComparer.Ordinal).Count() != rows.Length ||
            rows.Select(row => (row.PredictionKey, row.TargetSession)).Distinct().Count() != rows.Length)
            return Stop("duplicate-or-ambiguous-key");

        var revisions = new Dictionary<string, ForwardInput>(StringComparer.Ordinal);
        var features = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (!ValidRow(manifest, expectedManifestHash, row, asOf)) return Stop("invalid-lineage-or-chronology");
            foreach (var input in row.FeatureInputs.Append(row.Source).Append(row.Target))
            {
                if (revisions.TryGetValue(input.Revision, out var prior) && prior != input)
                    return Stop("conflicting-revision");
                revisions[input.Revision] = input;
            }
            var featureIdentity = Hash(new { row.FeatureChecksum, row.FeatureKnownAt,
                row.PredictionKey.FeatureDefinitionHash, row.PredictionKey.FeatureEngineVersion,
                Inputs = row.FeatureInputs.OrderBy(input => input.Revision, StringComparer.Ordinal).ToArray() });
            if (features.TryGetValue(row.FeatureRevision, out var previous) && previous != featureIdentity)
                return Stop("conflicting-feature-revision");
            features[row.FeatureRevision] = featureIdentity;
        }

        // Complete predeclared population only: no dropping losses, choosing later windows,
        // substituting revisions or counting several instruments as several sessions.
        if (rows.Length != manifest.Series.Length * manifest.TargetSessions.Length)
            return Stop("insufficient-or-incomplete-cohort");
        var count = rows.Select(row => row.TargetSession).Distinct().Count();
        if (count < manifest.MinimumSessions) return Stop("insufficient-future-sessions");

        var groups = new List<ForwardGroup>();
        foreach (var key in manifest.Series)
        {
            var series = rows.Where(row => row.PredictionKey == key).OrderBy(row => row.TargetSession).ToArray();
            groups.Add(new(key, null, series.Length, Metrics(series, manifest.MinimumSessions, true)));
            foreach (var regime in manifest.Regimes)
            {
                var subset = series.Where(row => row.Regime == regime).ToArray();
                // Regime slices do not establish a continuous equity curve.
                groups.Add(new(key, regime, subset.Length, Metrics(subset, manifest.MinimumSessions, false)));
            }
        }
        // Accuracy, costs and Brier score use the complete equal-count panel. Never pool
        // unrelated equity paths into portfolio drawdown or a portfolio tail statistic.
        return new(Contract, "SYNTHETIC_ONLY", "EVALUABLE", null, expectedManifestHash, expectedEvidenceHash,
            count, Metrics(rows, manifest.MinimumSessions, manifest.Series.Length == 1), groups.AsReadOnly());
    }

    private static bool ValidManifest(ForwardManifest m)
    {
        if (!Text(m.Cohort) || !Utc(m.FrozenAt) || m.Calendar != UsMarketCalendar.Version ||
            m.MinimumSessions is < MinimumFutureSessions or > MaximumSessions ||
            m.Series is null || m.Series.Length is < 1 or > MaximumSeries ||
            m.Series.Any(key => !ValidKey(key)) || m.Series.Distinct().Count() != m.Series.Length ||
            m.TargetSessions is null || m.TargetSessions.Length < m.MinimumSessions || m.TargetSessions.Length > MaximumSessions ||
            m.Regimes is null || m.Regimes.Length > 16 || m.Regimes.Any(regime => !Text(regime)) ||
            m.Regimes.Distinct(StringComparer.Ordinal).Count() != m.Regimes.Length ||
            !Text(m.RegimeDefinition) || m.EquityDefinition != "synthetic-unitized-net-equity-v1") return false;
        DateOnly? previous = null;
        foreach (var date in m.TargetSessions)
        {
            if (date.Year is < 2000 or > 2100 || UsMarketCalendar.Session(date) is not { } session ||
                m.FrozenAt >= session.OpenUtc || (previous.HasValue && Next(previous.Value) != date)) return false;
            previous = date;
        }
        return true;
    }

    private static bool ValidRow(ForwardManifest m, string hash, ForwardEvidence r, DateTimeOffset asOf)
    {
        if (!Text(r.PredictionId) || !Text(r.OutcomeId) || r.ManifestHash != hash || r.Cohort != m.Cohort ||
            !m.Series.Contains(r.PredictionKey) || r.OutcomeKey != r.PredictionKey ||
            !m.TargetSessions.Contains(r.TargetSession) || r.SourceSession.Year is < 2000 or > 2100 ||
            Next(r.SourceSession) != r.TargetSession || r.Invalidated || r.Retrospective || r.Ambiguous ||
            !ValidInput(r.Source) || !ValidInput(r.Target) || r.Source.Revision == r.Target.Revision ||
            !Text(r.FeatureRevision) || !Digest(r.FeatureChecksum) || r.FeatureInputs is null ||
            r.FeatureInputs.Length is < 1 or > 256 || r.FeatureInputs.Any(input => !ValidInput(input)) ||
            r.FeatureInputs.Select(input => input.Revision).Distinct(StringComparer.Ordinal).Count() != r.FeatureInputs.Length ||
            !r.FeatureInputs.Contains(r.Source) || !Utc(r.FeatureKnownAt) || !Utc(r.Cutoff) || !Utc(r.CreatedAt) || !Utc(r.EvaluatedAt))
            return false;
        var source = UsMarketCalendar.Session(r.SourceSession);
        var target = UsMarketCalendar.Session(r.TargetSession);
        if (source is null || target is null || r.Source.EventAt != source.CloseUtc || r.Target.EventAt != target.CloseUtc ||
            r.Source.KnownAt > r.FeatureKnownAt || r.FeatureKnownAt > r.Cutoff ||
            r.FeatureInputs.Any(input => input.EventAt > source.CloseUtc || input.KnownAt > r.FeatureKnownAt) ||
            r.Cutoff > r.CreatedAt || m.FrozenAt > r.Cutoff || r.CreatedAt >= target.OpenUtc ||
            r.Target.KnownAt <= r.CreatedAt || r.Target.KnownAt > r.EvaluatedAt || r.EvaluatedAt > asOf ||
            r.ReferenceClose is <= 0 or > 1000000000000m || r.RealizedClose is <= 0 or > 1000000000000m ||
            r.ProbabilityUp is < 0 or > 1) return false;
        if (r.Regime is null ? r.RegimeKnownAt.HasValue :
            !m.Regimes.Contains(r.Regime, StringComparer.Ordinal) || !r.RegimeKnownAt.HasValue ||
            !Utc(r.RegimeKnownAt.Value) || r.RegimeKnownAt.Value > r.Cutoff) return false;
        return r.Equity is null || (r.Equity.AtOpen is >= 0.00000001m and <= 1000000000000m &&
            r.Equity.AtClose is >= 0.00000001m and <= 1000000000000m && r.Equity.CostFraction is not (< 0 or > 1));
    }

    private static ForwardMetrics Metrics(ForwardEvidence[] rows, int minimum, bool allowPath)
    {
        if (rows.Select(row => row.TargetSession).Distinct().Count() < minimum) return ForwardMetrics.Unknown;
        var accuracy = ForwardMetric.Known(rows.Count(row => row.PredictsUp == (row.RealizedClose > row.ReferenceClose)) / (decimal)rows.Length);
        var brier = rows.All(row => row.ProbabilityUp.HasValue)
            ? ForwardMetric.Known(rows.Average(row => Square(row.ProbabilityUp!.Value - (row.RealizedClose > row.ReferenceClose ? 1m : 0m))))
            : ForwardMetric.Unknown;
        var cost = rows.All(row => row.Equity?.CostFraction is not null)
            ? ForwardMetric.Known(rows.Average(row => row.Equity!.CostFraction!.Value)) : ForwardMetric.Unknown;
        var drawdown = ForwardMetric.Unknown;
        var worst = ForwardMetric.Unknown;
        var tail = ForwardMetric.Unknown;
        if (rows.All(row => row.Equity is not null) && rows.Select(row => row.PredictionKey).Distinct().Count() == 1)
        {
            var ordered = rows.OrderBy(row => row.TargetSession).ToArray();
            var losses = ordered.Select(row => 1m - row.Equity!.AtClose / row.Equity.AtOpen).OrderDescending().ToArray();
            worst = ForwardMetric.Known(Math.Max(0, losses[0]));
            // Fixed empirical upper 5% loss mean; signed (a wholly profitable tail can be negative).
            tail = ForwardMetric.Known(losses.Take((int)Math.Ceiling(rows.Length * 0.05m)).Average());
            if (allowPath && ordered.Skip(1).Select((row, i) =>
                Next(ordered[i].TargetSession) == row.TargetSession && ordered[i].Equity!.AtClose == row.Equity!.AtOpen).All(value => value))
            {
                var peak = ordered[0].Equity!.AtOpen;
                var maximum = 0m;
                foreach (var row in ordered)
                {
                    peak = Math.Max(peak, row.Equity!.AtClose);
                    maximum = Math.Max(maximum, 1m - row.Equity.AtClose / peak);
                }
                drawdown = ForwardMetric.Known(maximum);
            }
        }
        return new(accuracy, brier, cost, drawdown, worst, tail);
    }

    private static bool ValidKey(ForwardKey key) => key is not null && Text(key.Strategy) && Text(key.StrategyVersion) &&
        Digest(key.ParametersHash) && Text(key.SourceIdentity) && Digest(key.FeatureDefinitionHash) &&
        Text(key.FeatureEngineVersion) && Text(key.Instrument) && key.Horizon == Horizon;
    private static bool ValidInput(ForwardInput input) => input is not null && Text(input.Revision) && Digest(input.Checksum) &&
        Utc(input.EventAt) && Utc(input.AcquiredAt) && Utc(input.KnownAt) && input.EventAt <= input.AcquiredAt && input.AcquiredAt <= input.KnownAt;
    private static bool Utc(DateTimeOffset time) => time != default && time.Offset == TimeSpan.Zero;
    private static bool Text(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 256 && !value.Any(char.IsControl);
    private static bool Digest(string value) => value is not null && value.Length == 71 && value.StartsWith("sha256:", StringComparison.Ordinal) &&
        value.AsSpan(7).IndexOfAnyExcept(HashCharacters) < 0;
    private static decimal Square(decimal value) => value * value;
    private static string Hash<T>(T value) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static DateOnly Next(DateOnly date)
    {
        do { date = date.AddDays(1); } while (!UsMarketCalendar.IsSession(date));
        return date;
    }
}
