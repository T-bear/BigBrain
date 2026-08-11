using System.Collections.Immutable;

namespace BigBrain.Modules.Finance;

public enum AcquisitionSourceKind { Unknown = 0, SyntheticFixture, ExternalProvider }
public enum AcquisitionCompleteness { Unknown = 0, Partial, Complete }
public enum AcquisitionOutcome { Unknown = 0, Accepted, Rejected }

public readonly record struct AcquisitionRequestId
{
    public AcquisitionRequestId(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct AcquisitionBatchId
{
    public AcquisitionBatchId(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct ProviderObservationId
{
    public ProviderObservationId(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public sealed record HistoricalDataAcquisitionRequest
{
    public HistoricalDataAcquisitionRequest(
        AcquisitionRequestId id, AcquisitionSourceKind sourceKind, MarketDataProvider provider,
        ProviderDataset providerDataset, InstrumentId instrumentId, string providerReference, string mic,
        DateOnly fromDate, DateOnly toDate, MarketDataInterval interval, PriceAdjustment adjustment,
        DateTimeOffset acquiredAtUtc, string sourceTimeZoneId, MarketDataPolicyReference policy,
        DatasetRevision destinationRevision, string? initialCursor = null)
    {
        RequiredText.Require(id.Value, nameof(id));
        if (sourceKind == AcquisitionSourceKind.Unknown || !Enum.IsDefined(sourceKind))
            throw new ArgumentException("Acquisition source kind is required.", nameof(sourceKind));
        RequiredText.Require(provider.Value, nameof(provider));
        RequiredText.Require(providerDataset.Value, nameof(providerDataset));
        RequiredText.Require(instrumentId.Value, nameof(instrumentId));
        ProviderReference = RequiredText.Normalize(providerReference, nameof(providerReference)).ToUpperInvariant();
        Mic = RequiredText.Normalize(mic, nameof(mic)).ToUpperInvariant();
        if (toDate < fromDate) throw new ArgumentException("Acquisition range cannot end before it starts.", nameof(toDate));
        if (interval != MarketDataInterval.Daily)
            throw new ArgumentException("Only daily acquisition is supported by the current M2 boundary.", nameof(interval));
        if (adjustment == PriceAdjustment.Unknown || !Enum.IsDefined(adjustment))
            throw new ArgumentException("Adjustment classification is required.", nameof(adjustment));
        FinanceTime.RequireUtc(acquiredAtUtc, nameof(acquiredAtUtc));
        var zone = TimeZoneInfo.FindSystemTimeZoneById(RequiredText.Normalize(sourceTimeZoneId, nameof(sourceTimeZoneId)));
        policy.Validate();
        ArgumentNullException.ThrowIfNull(destinationRevision);
        if (destinationRevision.Provider != provider || destinationRevision.ProviderDataset != providerDataset)
            throw new ArgumentException("Destination revision must match the acquisition source/product.", nameof(destinationRevision));
        if (destinationRevision.RetrievedAtUtc != acquiredAtUtc)
            throw new ArgumentException("Destination retrieval time must equal the explicit acquisition time.", nameof(destinationRevision));

        Id = id; SourceKind = sourceKind; Provider = provider; ProviderDataset = providerDataset;
        InstrumentId = instrumentId; FromDate = fromDate; ToDate = toDate; Interval = interval;
        Adjustment = adjustment; AcquiredAtUtc = acquiredAtUtc; SourceTimeZoneId = zone.Id;
        Policy = policy; DestinationRevision = destinationRevision;
        InitialCursor = string.IsNullOrWhiteSpace(initialCursor) ? null : initialCursor.Trim();
    }

    public AcquisitionRequestId Id { get; }
    public AcquisitionSourceKind SourceKind { get; }
    public MarketDataProvider Provider { get; }
    public ProviderDataset ProviderDataset { get; }
    public InstrumentId InstrumentId { get; }
    public string ProviderReference { get; }
    public string Mic { get; }
    public DateOnly FromDate { get; }
    public DateOnly ToDate { get; }
    public MarketDataInterval Interval { get; }
    public PriceAdjustment Adjustment { get; }
    public DateTimeOffset AcquiredAtUtc { get; }
    public string SourceTimeZoneId { get; }
    public MarketDataPolicyReference Policy { get; }
    public DatasetRevision DestinationRevision { get; }
    public string? InitialCursor { get; }
}

public sealed record AcquiredRawBar(ProviderObservationId Id, DateTimeOffset AvailableAtUtc, SyntheticRawDailyBar Value);
public sealed record AcquiredRawCorporateAction(ProviderObservationId Id, DateTimeOffset AvailableAtUtc, SyntheticRawCorporateAction Value);

public sealed record AcquiredGapEvidence(
    ProviderObservationId Id, InstrumentId InstrumentId, DateOnly TradingDate,
    MarketDataFindingCode FindingCode, ObservationGapClassification Classification,
    DateTimeOffset ObservedAtUtc, EvidenceReference Evidence);

public sealed record AcquiredCorrection(
    DatasetCorrectionId Id, ProviderObservationId OriginalObservationId,
    ProviderObservationId ReplacementObservationId, DateTimeOffset AvailableAtUtc,
    string ReasonCode, EvidenceReference Evidence);

public sealed record HistoricalDataAcquisitionBatch
{
    public HistoricalDataAcquisitionBatch(
        AcquisitionBatchId id, AcquisitionRequestId requestId, MarketDataProvider provider,
        ProviderDataset providerDataset, DateTimeOffset receivedAtUtc, AcquisitionCompleteness completeness,
        EvidenceReference provenance, IReadOnlyList<AcquiredRawBar> bars,
        IReadOnlyList<AcquiredRawCorporateAction> corporateActions,
        IReadOnlyList<AcquiredGapEvidence> gaps, IReadOnlyList<AcquiredCorrection> corrections,
        string? requestCursor = null, string? nextCursor = null)
    {
        RequiredText.Require(id.Value, nameof(id)); RequiredText.Require(requestId.Value, nameof(requestId));
        RequiredText.Require(provider.Value, nameof(provider)); RequiredText.Require(providerDataset.Value, nameof(providerDataset));
        FinanceTime.RequireUtc(receivedAtUtc, nameof(receivedAtUtc)); RequiredText.Require(provenance.Value, nameof(provenance));
        if (completeness == AcquisitionCompleteness.Unknown || !Enum.IsDefined(completeness))
            throw new ArgumentException("Batch completeness is required.", nameof(completeness));
        ArgumentNullException.ThrowIfNull(bars); ArgumentNullException.ThrowIfNull(corporateActions);
        ArgumentNullException.ThrowIfNull(gaps); ArgumentNullException.ThrowIfNull(corrections);
        Id = id; RequestId = requestId; Provider = provider; ProviderDataset = providerDataset;
        ReceivedAtUtc = receivedAtUtc; Completeness = completeness; Provenance = provenance;
        Bars = bars.ToImmutableArray(); CorporateActions = corporateActions.ToImmutableArray();
        Gaps = gaps.ToImmutableArray(); Corrections = corrections.ToImmutableArray();
        RequestCursor = string.IsNullOrWhiteSpace(requestCursor) ? null : requestCursor.Trim();
        NextCursor = string.IsNullOrWhiteSpace(nextCursor) ? null : nextCursor.Trim();
    }

    public AcquisitionBatchId Id { get; }
    public AcquisitionRequestId RequestId { get; }
    public MarketDataProvider Provider { get; }
    public ProviderDataset ProviderDataset { get; }
    public DateTimeOffset ReceivedAtUtc { get; }
    public AcquisitionCompleteness Completeness { get; }
    public EvidenceReference Provenance { get; }
    public ImmutableArray<AcquiredRawBar> Bars { get; }
    public ImmutableArray<AcquiredRawCorporateAction> CorporateActions { get; }
    public ImmutableArray<AcquiredGapEvidence> Gaps { get; }
    public ImmutableArray<AcquiredCorrection> Corrections { get; }
    public string? RequestCursor { get; }
    public string? NextCursor { get; }
}

public interface IHistoricalDataAcquisitionAdapter
{
    AcquisitionSourceKind SourceKind { get; }
    IReadOnlyList<HistoricalDataAcquisitionBatch> Acquire(HistoricalDataAcquisitionRequest request);
}

public sealed class SyntheticHistoricalDataAdapter : IHistoricalDataAcquisitionAdapter
{
    public const string ProviderName = "SyntheticFixture";
    private readonly ImmutableArray<HistoricalDataAcquisitionBatch> _batches;

    public SyntheticHistoricalDataAdapter(IEnumerable<HistoricalDataAcquisitionBatch> batches)
    {
        ArgumentNullException.ThrowIfNull(batches);
        _batches = batches.ToImmutableArray();
        if (_batches.Any(batch => batch.Provider.Value != ProviderName ||
            !batch.ProviderDataset.Value.StartsWith("Synthetic-", StringComparison.Ordinal)))
            throw new ArgumentException("The synthetic adapter accepts only unmistakably synthetic fixture products.", nameof(batches));
    }

    public AcquisitionSourceKind SourceKind => AcquisitionSourceKind.SyntheticFixture;

    public IReadOnlyList<HistoricalDataAcquisitionBatch> Acquire(HistoricalDataAcquisitionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SourceKind != SourceKind || request.Provider.Value != ProviderName ||
            !request.ProviderDataset.Value.StartsWith("Synthetic-", StringComparison.Ordinal))
            throw new InvalidOperationException("Synthetic adapter cannot represent or authorize a real provider.");
        return _batches.Where(batch => batch.RequestId == request.Id)
            .OrderBy(batch => batch.Id.Value, StringComparer.Ordinal).ToImmutableArray();
    }
}

public sealed record AcquisitionEntitlementResult(
    bool IsAllowed, string ReasonCode, MarketDataPolicyReference? Policy,
    RetentionClassification Retention, DeletionRequirement Deletion, DateTimeOffset? DeletionDeadlineUtc);

public static class AcquisitionEntitlementGate
{
    private static readonly MarketDataUse[] RequiredUses =
        [MarketDataUse.HistoricalAnalysis, MarketDataUse.Backtest, MarketDataUse.DerivedMetrics, MarketDataUse.LongTermStorage];

    public static AcquisitionEntitlementResult Evaluate(
        HistoricalDataAcquisitionRequest request, MarketDataEntitlementPolicy? policy)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (policy is not null && policy.Reference != request.Policy)
            return Denied(MarketDataEntitlementReasons.PolicyScopeMismatch, policy);
        if (request.SourceKind == AcquisitionSourceKind.SyntheticFixture && policy is not null &&
            (policy.Provider.Value != SyntheticHistoricalDataAdapter.ProviderName ||
             !policy.ProviderDataset.Value.StartsWith("Synthetic-", StringComparison.Ordinal) ||
             !policy.Evidence.Value.StartsWith("fixture:", StringComparison.Ordinal)))
            return Denied("marketData.acquisition.syntheticPolicyInvalid", policy);

        var context = new MarketDataEntitlementContext(request.AcquiredAtUtc, true, true,
            MarketDataClassification.Raw, request.Provider, request.ProviderDataset);
        foreach (var use in RequiredUses)
        {
            var evaluation = MarketDataEntitlementEvaluator.Evaluate(policy, use, context);
            if (!evaluation.IsAllowed)
                return Denied(evaluation.ReasonCode, policy, evaluation.Policy);
        }
        return new AcquisitionEntitlementResult(true, MarketDataEntitlementReasons.Allowed,
            policy!.Reference, policy.Retention, policy.Deletion, policy.DeletionDeadlineUtc);
    }

    private static AcquisitionEntitlementResult Denied(string reason, MarketDataEntitlementPolicy? policy,
        MarketDataPolicyReference? reference = null) =>
        new(false, reason, reference ?? policy?.Reference, policy?.Retention ?? RetentionClassification.Unknown,
            policy?.Deletion ?? DeletionRequirement.Unknown, policy?.DeletionDeadlineUtc);
}

public sealed record AcquisitionJournalEntry(
    AcquisitionRequestId RequestId, AcquisitionOutcome Outcome, MarketDataProvider Provider,
    ProviderDataset ProviderDataset, InstrumentId InstrumentId, DateOnly FromDate, DateOnly ToDate,
    DateTimeOffset AcquiredAtUtc, MarketDataPolicyReference Policy, EvidenceReference PolicyEvidence,
    RetentionClassification Retention, DeletionRequirement Deletion, DateTimeOffset? DeletionDeadlineUtc,
    int ReceivedBatchCount, int DuplicateBatchCount, int AcceptedObservationCount,
    int RejectedObservationCount, int DuplicateObservationCount,
    ImmutableArray<MarketDataQualityFinding> QualityFindings, DatasetRevisionId? ResultingRevisionId,
    string ReasonCode);

public sealed record HistoricalDataIngestionResult(
    AcquisitionJournalEntry Journal, ImmutableDatasetRevision? Revision,
    ImmutableArray<CanonicalMarketBar> Bars, ImmutableArray<CanonicalCorporateAction> CorporateActions);

public sealed class HistoricalDataIngestionPipeline
{
    private readonly SyntheticMarketDataNormalizer _normalizer;

    public HistoricalDataIngestionPipeline(InstrumentMappingCatalog mappings) =>
        _normalizer = new SyntheticMarketDataNormalizer(mappings ?? throw new ArgumentNullException(nameof(mappings)));

    public HistoricalDataIngestionResult Prepare(
        HistoricalDataAcquisitionRequest request, MarketDataEntitlementPolicy? policy,
        IHistoricalDataAcquisitionAdapter adapter, ImmutableDatasetRevision? parent = null)
    {
        ArgumentNullException.ThrowIfNull(request); ArgumentNullException.ThrowIfNull(adapter);
        if (adapter.SourceKind != request.SourceKind)
            throw new ArgumentException("Adapter source kind must match the request.", nameof(adapter));
        var entitlement = AcquisitionEntitlementGate.Evaluate(request, policy);
        if (!entitlement.IsAllowed)
            return Rejected(request, policy, entitlement);

        var received = adapter.Acquire(request).ToArray();
        ValidateBatches(request, received);
        var unique = received.GroupBy(batch => batch.Id).Select(group =>
        {
            var values = group.ToArray();
            if (values.Skip(1).Any(value => !BatchEquivalent(values[0], value)))
                throw new ArgumentException("A repeated batch ID must contain identical immutable evidence.", nameof(adapter));
            return values[0];
        }).OrderBy(batch => batch.Id.Value, StringComparer.Ordinal).ToArray();

        var rawBars = unique.SelectMany(batch => batch.Bars).OrderBy(value => value.Value.SessionDate)
            .ThenBy(value => value.Id.Value, StringComparer.Ordinal).ToArray();
        var normalized = _normalizer.NormalizeBatch(rawBars.Select(value => value.Value));
        if (normalized.Accepted.Any(bar => bar.InstrumentId != request.InstrumentId ||
            bar.Adjustment != request.Adjustment))
            throw new ArgumentException("Normalized bars must match the requested canonical instrument and adjustment basis.", nameof(adapter));
        var barIds = rawBars.GroupBy(value => value.Value.SessionDate).ToDictionary(group =>
            group.Key, group => group.First().Id);
        var members = normalized.Accepted.Select(bar => DatasetRevisionMember.ForBar(
            MemberId(barIds[bar.SessionDate]), bar, rawBars.First(value => value.Id == barIds[bar.SessionDate]).AvailableAtUtc)).ToList();

        var actions = unique.SelectMany(batch => batch.CorporateActions)
            .OrderBy(value => value.Value.EffectiveDate).ThenBy(value => value.Id.Value, StringComparer.Ordinal)
            .GroupBy(value => value.Id).Select(group => group.First()).Select(value =>
                (Envelope: value, Canonical: _normalizer.Normalize(value.Value))).ToArray();
        members.AddRange(actions.Select(value => DatasetRevisionMember.ForCorporateAction(
            MemberId(value.Envelope.Id), value.Canonical, value.Envelope.AvailableAtUtc)));

        var gaps = unique.SelectMany(batch => batch.Gaps).OrderBy(value => value.TradingDate)
            .ThenBy(value => value.Id.Value, StringComparer.Ordinal).GroupBy(value => value.Id).Select(group => group.First()).ToArray();
        foreach (var gap in gaps)
        {
            var evidence = new ReplayQualityEvidence(gap.InstrumentId, gap.TradingDate, gap.FindingCode,
                gap.Classification, gap.ObservedAtUtc, request.DestinationRevision.Id, gap.Evidence);
            members.Add(DatasetRevisionMember.ForQualityEvidence(MemberId(gap.Id), evidence, request.Policy));
        }

        var corrections = unique.SelectMany(batch => batch.Corrections).OrderBy(value => value.AvailableAtUtc)
            .ThenBy(value => value.Id.Value, StringComparer.Ordinal).GroupBy(value => value.Id).Select(group => group.First())
            .Select(value => new DatasetCorrection(value.Id, MemberId(value.OriginalObservationId),
                MemberId(value.ReplacementObservationId), value.AvailableAtUtc, value.ReasonCode, value.Evidence)).ToArray();
        var revision = ImmutableDatasetRevisionAssembler.Assemble(
            new DatasetRevisionAssemblyRequest(request.DestinationRevision, parent, members, corrections));
        var duplicateObservations = normalized.Findings.Count(value => value.Code == MarketDataFindingCode.DuplicateObservation);
        var journal = new AcquisitionJournalEntry(request.Id, AcquisitionOutcome.Accepted, request.Provider,
            request.ProviderDataset, request.InstrumentId, request.FromDate, request.ToDate, request.AcquiredAtUtc,
            request.Policy, policy!.Evidence, entitlement.Retention, entitlement.Deletion, entitlement.DeletionDeadlineUtc,
            received.Length, received.Length - unique.Length, normalized.Accepted.Length + actions.Length,
            normalized.Findings.Count(value => value.Code != MarketDataFindingCode.DuplicateObservation), duplicateObservations,
            normalized.Findings, revision.Id, MarketDataEntitlementReasons.Allowed);
        return new HistoricalDataIngestionResult(journal, revision, normalized.Accepted, actions.Select(value => value.Canonical).ToImmutableArray());
    }

    private static HistoricalDataIngestionResult Rejected(HistoricalDataAcquisitionRequest request,
        MarketDataEntitlementPolicy? policy, AcquisitionEntitlementResult entitlement)
    {
        var policyReference = entitlement.Policy ?? request.Policy;
        var journal = new AcquisitionJournalEntry(request.Id, AcquisitionOutcome.Rejected, request.Provider,
            request.ProviderDataset, request.InstrumentId, request.FromDate, request.ToDate, request.AcquiredAtUtc,
            policyReference, policy?.Evidence ?? new EvidenceReference("fixture:missing-entitlement"),
            entitlement.Retention, entitlement.Deletion, entitlement.DeletionDeadlineUtc,
            0, 0, 0, 0, 0, [], null, entitlement.ReasonCode);
        return new HistoricalDataIngestionResult(journal, null, [], []);
    }

    private static void ValidateBatches(HistoricalDataAcquisitionRequest request, IEnumerable<HistoricalDataAcquisitionBatch> batches)
    {
        foreach (var batch in batches)
        {
            if (batch.RequestId != request.Id || batch.Provider != request.Provider || batch.ProviderDataset != request.ProviderDataset)
                throw new ArgumentException("Every acquisition batch must match the exact request/source/product.", nameof(batches));
            if (batch.ReceivedAtUtc > request.DestinationRevision.CreatedAtUtc)
                throw new ArgumentException("A dataset revision cannot precede receipt of an acquisition batch.", nameof(batches));
            if (batch.Bars.Any(value => value.Value.Provider != request.Provider ||
                value.Value.ProviderDataset != request.ProviderDataset || value.Value.DatasetRevisionId != request.DestinationRevision.Id ||
                value.Value.Policy != request.Policy || value.Value.ProviderReference != request.ProviderReference ||
                !string.Equals(value.Value.Mic, request.Mic, StringComparison.OrdinalIgnoreCase) ||
                value.Value.Adjustment != request.Adjustment || value.Value.SessionDate < request.FromDate ||
                value.Value.SessionDate > request.ToDate))
                throw new ArgumentException("Raw bars must remain inside the requested scope and destination revision.", nameof(batches));
            if (batch.CorporateActions.Any(value => value.Value.Provider != request.Provider ||
                value.Value.ProviderDataset != request.ProviderDataset || value.Value.DatasetRevisionId != request.DestinationRevision.Id ||
                value.Value.Policy != request.Policy || value.Value.ProviderReference != request.ProviderReference ||
                !string.Equals(value.Value.Mic, request.Mic, StringComparison.OrdinalIgnoreCase) ||
                value.Value.ExDate < request.FromDate || value.Value.ExDate > request.ToDate))
                throw new ArgumentException("Corporate actions must remain inside the requested scope and destination revision.", nameof(batches));
            if (batch.Gaps.Any(value => value.InstrumentId != request.InstrumentId ||
                value.TradingDate < request.FromDate || value.TradingDate > request.ToDate))
                throw new ArgumentException("Gap evidence must remain inside the requested instrument/range.", nameof(batches));
        }
    }

    private static DatasetMemberId MemberId(ProviderObservationId id) => new($"acquired:{id.Value}");

    private static bool BatchEquivalent(HistoricalDataAcquisitionBatch left, HistoricalDataAcquisitionBatch right) =>
        left.Id == right.Id && left.RequestId == right.RequestId && left.Provider == right.Provider &&
        left.ProviderDataset == right.ProviderDataset && left.ReceivedAtUtc == right.ReceivedAtUtc &&
        left.Completeness == right.Completeness && left.Provenance == right.Provenance &&
        left.RequestCursor == right.RequestCursor && left.NextCursor == right.NextCursor &&
        Ordered(left.Bars, value => value.Id.Value).SequenceEqual(Ordered(right.Bars, value => value.Id.Value)) &&
        Ordered(left.CorporateActions, value => value.Id.Value).SequenceEqual(Ordered(right.CorporateActions, value => value.Id.Value)) &&
        Ordered(left.Gaps, value => value.Id.Value).SequenceEqual(Ordered(right.Gaps, value => value.Id.Value)) &&
        Ordered(left.Corrections, value => value.Id.Value).SequenceEqual(Ordered(right.Corrections, value => value.Id.Value));

    private static IEnumerable<T> Ordered<T>(IEnumerable<T> values, Func<T, string> identity) =>
        values.OrderBy(identity, StringComparer.Ordinal);
}
