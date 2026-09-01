using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace BigBrain.Modules.Finance;

public enum DatasetCandidateState { Discovered, Downloading, Downloaded, Inspecting, Validating, Rejected, ManualReviewRequired, Approved, Promoted, Superseded }
public enum DatasetLicenseClass { Unknown, PublicDomain, Cc0, CcBy, CompatibleOther, Incompatible }
public enum DatasetEvidenceResult { Pass, Fail, Unknown }
public enum DatasetOwnerRightsDecision { NotProvided, ApprovedByOwner }
public enum DatasetPriceBasis { Unclear, Raw, SplitAdjusted, DividendAdjusted, TotalReturnAdjusted, RawAndAdjusted }
public enum DatasetSurvivorshipBias { SurvivorshipUnknown, CurrentConstituentsOnly, PointInTimeUniverse, Mixed, NotApplicable }
public enum DatasetComparisonClass { Consistent, MinorNumericDifference, PriceBasisDifference, CorporateActionDifference, SessionDifference, MaterialConflict, InsufficientOverlap }
public enum DatasetGate { Integrity, License, Provenance, Schema, FieldSemantics, DateTime, Ohlcv, DuplicateConflict, SymbolMapping, SurvivorshipCoverage, CorporateActions, SourceOverlap, EntitlementRetention }

public sealed record DatasetRightsEvidence(DatasetLicenseClass LicenseClass, string DeclaredLicense, string EvidenceUrl,
    DateOnly RetrievedOn, string Paraphrase, DatasetEvidenceResult UnderlyingProvenance, bool LocalRetentionAllowed,
    string Attribution);

public sealed record ExternalDatasetCandidate(string CandidateId, string SourceName, string SourceUrl,
    string HostingPlatform, string OriginalFilename, DatasetRightsEvidence Rights, string Provenance,
    DatasetPriceBasis PriceBasis, DatasetSurvivorshipBias SurvivorshipBias, long? ExpectedBytes = null,
    DatasetOwnerRightsDecision OwnerRightsDecision = DatasetOwnerRightsDecision.NotProvided,
    string OwnerRightsEvidence = "", string OwnerDeclaredPriceBasis = "UNKNOWN");

public sealed record DatasetGateResult(DatasetGate Gate, DatasetEvidenceResult Result, string Code, string Detail);

public sealed record DatasetValidationSummary(ImmutableArray<DatasetGateResult> Gates, string SchemaFingerprint,
    long ObservationCount, int InstrumentCount, DateOnly? CoverageFrom, DateOnly? CoverageTo, long DuplicateKeys,
    long ConflictingKeys, long InvalidOhlcv, DatasetComparisonClass Comparison, ImmutableArray<string> Limitations,
    long ZeroVolume = 0, long OutOfOrderRows = 0, long MissingSessions = 0,
    long SuspiciousDiscontinuities = 0, long SplitLikeJumps = 0, long MissingValues = 0,
    long InvalidDates = 0, long NonPositivePrices = 0, long InconsistentOhlc = 0, long InvalidVolume = 0)
{
    public DatasetEvidenceResult Overall => Gates.Any(x => x.Result == DatasetEvidenceResult.Fail)
        ? DatasetEvidenceResult.Fail
        : Gates.Any(x => x.Result == DatasetEvidenceResult.Unknown) ? DatasetEvidenceResult.Unknown : DatasetEvidenceResult.Pass;
}

public sealed record DatasetPromotionDecision(string PolicyId, DatasetEvidenceResult Result,
    DatasetCandidateState State, string Reason)
{
    public bool AutomaticallyPromote => Result == DatasetEvidenceResult.Pass;
}

public static class DatasetPromotionPolicyV1
{
    public const string Id = "dataset-promotion-v1";
    private static readonly DatasetGate[] Mandatory = Enum.GetValues<DatasetGate>();

    public static DatasetPromotionDecision Decide(DatasetValidationSummary validation)
    {
        ArgumentNullException.ThrowIfNull(validation);
        foreach (var gate in Mandatory)
            if (validation.Gates.Count(x => x.Gate == gate) != 1)
                return new(Id, DatasetEvidenceResult.Unknown, DatasetCandidateState.ManualReviewRequired,
                    $"Required gate {gate} is unresolved.");
        if (validation.Gates.Any(x => x.Result == DatasetEvidenceResult.Fail))
            return new(Id, DatasetEvidenceResult.Fail, DatasetCandidateState.Rejected, "One or more mandatory gates failed.");
        if (validation.Gates.Any(x => x.Result == DatasetEvidenceResult.Unknown))
            return new(Id, DatasetEvidenceResult.Unknown, DatasetCandidateState.ManualReviewRequired, "One or more mandatory gates require manual review.");
        return new(Id, DatasetEvidenceResult.Pass, DatasetCandidateState.Approved, "All dataset-promotion-v1 gates passed.");
    }
}

public static class DatasetContentIdentity
{
    public static string Sha256(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return "sha256:" + Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
    }

    public static string SchemaFingerprint(IEnumerable<string> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        var canonical = string.Join("\n", fields.Select(x => x.Trim().ToLowerInvariant()));
        return "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}

public static class DatasetCandidateStateMachine
{
    private static readonly Dictionary<DatasetCandidateState, DatasetCandidateState[]> Allowed =
        new Dictionary<DatasetCandidateState, DatasetCandidateState[]>
        {
            [DatasetCandidateState.Discovered] = [DatasetCandidateState.Downloading, DatasetCandidateState.Rejected],
            [DatasetCandidateState.Downloading] = [DatasetCandidateState.Downloaded, DatasetCandidateState.Rejected],
            [DatasetCandidateState.Downloaded] = [DatasetCandidateState.Inspecting, DatasetCandidateState.Rejected],
            [DatasetCandidateState.Inspecting] = [DatasetCandidateState.Validating, DatasetCandidateState.Rejected],
            [DatasetCandidateState.Validating] = [DatasetCandidateState.Approved, DatasetCandidateState.ManualReviewRequired, DatasetCandidateState.Rejected],
            [DatasetCandidateState.Approved] = [DatasetCandidateState.Promoted, DatasetCandidateState.Superseded],
            [DatasetCandidateState.ManualReviewRequired] = [DatasetCandidateState.Approved, DatasetCandidateState.Rejected, DatasetCandidateState.Superseded],
            [DatasetCandidateState.Promoted] = [DatasetCandidateState.Superseded],
            [DatasetCandidateState.Rejected] = [DatasetCandidateState.Superseded],
            [DatasetCandidateState.Superseded] = []
        };

    public static void EnsureTransition(DatasetCandidateState from, DatasetCandidateState to)
    {
        if (!Allowed.TryGetValue(from, out var targets) || !targets.Contains(to))
            throw new InvalidOperationException($"Dataset candidate transition {from} -> {to} is not allowed.");
    }
}

public sealed record DatasetComparableBar(string Symbol, DateOnly Date, decimal Open, decimal High, decimal Low,
    decimal Close, decimal Volume, DatasetPriceBasis PriceBasis);
public sealed record DatasetOverlapMetrics(int OverlapSessions, int MissingOnA, int MissingOnB,
    decimal? MedianAbsoluteRelativeCloseDifference, decimal? MaximumRelativeCloseDifference,
    ImmutableArray<decimal> AbsoluteRelativeVolumeDifferences, DatasetComparisonClass Classification);

public static class DatasetCrossSourceComparerV1
{
    public const string Version = "cross-source-comparison-v1";
    public static DatasetOverlapMetrics Compare(IEnumerable<DatasetComparableBar> a, IEnumerable<DatasetComparableBar> b)
    {
        var left = a.GroupBy(x => $"{x.Symbol}|{x.Date:yyyy-MM-dd}",StringComparer.Ordinal).ToDictionary(x=>x.Key,x=>x.Last(),StringComparer.Ordinal);
        var right = b.GroupBy(x => $"{x.Symbol}|{x.Date:yyyy-MM-dd}",StringComparer.Ordinal).ToDictionary(x=>x.Key,x=>x.Last(),StringComparer.Ordinal);
        var overlap = left.Keys.Intersect(right.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (overlap.Length < 20) return new(overlap.Length, left.Keys.Except(right.Keys).Count(), right.Keys.Except(left.Keys).Count(), null, null, [], DatasetComparisonClass.InsufficientOverlap);
        var price = overlap.Select(k => Math.Abs(left[k].Close - right[k].Close) / Math.Max(Math.Abs(right[k].Close), 0.00000001m)).Order().ToArray();
        var volume = overlap.Where(k => right[k].Volume != 0).Select(k => Math.Abs(left[k].Volume-right[k].Volume)/Math.Abs(right[k].Volume)).Order().ToImmutableArray();
        var basisDiff = overlap.Any(k => left[k].PriceBasis != right[k].PriceBasis);
        var median = price[price.Length / 2]; var maximum = price[^1];
        var classification = basisDiff && median > 0.01m ? DatasetComparisonClass.PriceBasisDifference
            : maximum > 0.10m || median > 0.02m ? DatasetComparisonClass.MaterialConflict
            : maximum > 0.01m || median > 0.001m ? DatasetComparisonClass.MinorNumericDifference
            : DatasetComparisonClass.Consistent;
        return new(overlap.Length, left.Keys.Except(right.Keys).Count(), right.Keys.Except(left.Keys).Count(), median, maximum, volume, classification);
    }
}
