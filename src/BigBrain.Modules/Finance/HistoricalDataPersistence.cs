using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BigBrain.Modules.Finance;

public enum HistoricalStorageFormat { Unknown = 0, InMemoryFixture, JsonLinesV1, SqliteV1 }
public enum HistoricalManifestStatus { Unknown = 0, Staging, Complete, Deleted }

public sealed record HistoricalDatasetManifest
{
    public HistoricalDatasetManifest(
        DatasetId datasetId, DatasetRevisionId revisionId, DatasetRevisionId? parentRevisionId,
        VersionReference schemaVersion, IReadOnlyList<InstrumentId> instruments,
        MarketDataProvider provider, ProviderDataset providerDataset, string mic,
        DateOnly requestedFrom, DateOnly requestedTo, DateOnly? coveredFrom, DateOnly? coveredTo,
        MarketDataInterval interval, PriceAdjustment adjustment, AcquisitionRequestId acquisitionRequestId,
        DateTimeOffset acquiredAtUtc, MarketDataPolicyReference policy, EvidenceReference policyEvidence,
        int observationCount, int corporateActionCount, int explicitGapCount, int rejectedObservationCount,
        int correctionCount, DatasetChecksum contentChecksum, HistoricalStorageFormat storageFormat,
        VersionReference storageFormatVersion, RetentionClassification retention,
        DeletionRequirement deletion, DateTimeOffset? deletionDeadlineUtc,
        IReadOnlyList<EvidenceReference> provenance, HistoricalManifestStatus status)
    {
        RequiredText.Require(datasetId.Value, nameof(datasetId)); RequiredText.Require(revisionId.Value, nameof(revisionId));
        if (parentRevisionId is { } parent) RequiredText.Require(parent.Value, nameof(parentRevisionId));
        RequiredText.Require(schemaVersion.Value, nameof(schemaVersion)); ArgumentNullException.ThrowIfNull(instruments);
        var instrumentArray = instruments.OrderBy(value => value.Value, StringComparer.Ordinal).ToImmutableArray();
        if (instrumentArray.IsEmpty || instrumentArray.Any(value => string.IsNullOrWhiteSpace(value.Value)) ||
            instrumentArray.Distinct().Count() != instrumentArray.Length)
            throw new ArgumentException("Manifest instruments must be non-empty and unique.", nameof(instruments));
        RequiredText.Require(provider.Value, nameof(provider)); RequiredText.Require(providerDataset.Value, nameof(providerDataset));
        Mic = RequiredText.Normalize(mic, nameof(mic)).ToUpperInvariant();
        if (requestedTo < requestedFrom) throw new ArgumentException("Requested range is invalid.", nameof(requestedTo));
        if ((coveredFrom is null) != (coveredTo is null) || coveredTo < coveredFrom || coveredFrom < requestedFrom || coveredTo > requestedTo)
            throw new ArgumentException("Covered range must be empty or contained in the requested range.", nameof(coveredFrom));
        if (interval != MarketDataInterval.Daily) throw new ArgumentException("Only daily manifests are supported.", nameof(interval));
        if (adjustment == PriceAdjustment.Unknown || !Enum.IsDefined(adjustment)) throw new ArgumentException("Adjustment is required.", nameof(adjustment));
        RequiredText.Require(acquisitionRequestId.Value, nameof(acquisitionRequestId)); FinanceTime.RequireUtc(acquiredAtUtc, nameof(acquiredAtUtc));
        policy.Validate(); RequiredText.Require(policyEvidence.Value, nameof(policyEvidence));
        if (observationCount < 0 || corporateActionCount < 0 || explicitGapCount < 0 || rejectedObservationCount < 0 || correctionCount < 0)
            throw new ArgumentOutOfRangeException(nameof(observationCount), "Manifest counts cannot be negative.");
        RequiredText.Require(contentChecksum.Value, nameof(contentChecksum));
        if (storageFormat == HistoricalStorageFormat.Unknown || !Enum.IsDefined(storageFormat)) throw new ArgumentException("Storage format is required.", nameof(storageFormat));
        RequiredText.Require(storageFormatVersion.Value, nameof(storageFormatVersion));
        if (retention == RetentionClassification.Unknown || !Enum.IsDefined(retention) || deletion == DeletionRequirement.Unknown || !Enum.IsDefined(deletion))
            throw new ArgumentException("Retention and deletion obligations must be explicit.");
        if (deletion == DeletionRequirement.DeleteByDeadline && deletionDeadlineUtc is null)
            throw new ArgumentException("Deletion deadline is required.", nameof(deletionDeadlineUtc));
        if (deletion != DeletionRequirement.DeleteByDeadline && deletionDeadlineUtc is not null)
            throw new ArgumentException("Deletion deadline is valid only for deadline deletion.", nameof(deletionDeadlineUtc));
        if (deletionDeadlineUtc is { } deadline) FinanceTime.RequireUtc(deadline, nameof(deletionDeadlineUtc));
        ArgumentNullException.ThrowIfNull(provenance);
        var evidence = provenance.OrderBy(value => value.Value, StringComparer.Ordinal).ToImmutableArray();
        if (evidence.IsEmpty || evidence.Any(value => string.IsNullOrWhiteSpace(value.Value)) || evidence.Distinct().Count() != evidence.Length)
            throw new ArgumentException("Manifest provenance must be non-empty and unique.", nameof(provenance));
        if (status == HistoricalManifestStatus.Unknown || !Enum.IsDefined(status)) throw new ArgumentException("Manifest status is required.", nameof(status));

        DatasetId = datasetId; RevisionId = revisionId; ParentRevisionId = parentRevisionId; SchemaVersion = schemaVersion;
        Instruments = instrumentArray; Provider = provider; ProviderDataset = providerDataset; RequestedFrom = requestedFrom;
        RequestedTo = requestedTo; CoveredFrom = coveredFrom; CoveredTo = coveredTo; Interval = interval; Adjustment = adjustment;
        AcquisitionRequestId = acquisitionRequestId; AcquiredAtUtc = acquiredAtUtc; Policy = policy; PolicyEvidence = policyEvidence;
        ObservationCount = observationCount; CorporateActionCount = corporateActionCount; ExplicitGapCount = explicitGapCount;
        RejectedObservationCount = rejectedObservationCount; CorrectionCount = correctionCount; ContentChecksum = contentChecksum;
        StorageFormat = storageFormat; StorageFormatVersion = storageFormatVersion; Retention = retention; Deletion = deletion;
        DeletionDeadlineUtc = deletionDeadlineUtc; Provenance = evidence; Status = status;
    }

    public DatasetId DatasetId { get; }
    public DatasetRevisionId RevisionId { get; }
    public DatasetRevisionId? ParentRevisionId { get; }
    public VersionReference SchemaVersion { get; }
    public ImmutableArray<InstrumentId> Instruments { get; }
    public MarketDataProvider Provider { get; }
    public ProviderDataset ProviderDataset { get; }
    public string Mic { get; }
    public DateOnly RequestedFrom { get; }
    public DateOnly RequestedTo { get; }
    public DateOnly? CoveredFrom { get; }
    public DateOnly? CoveredTo { get; }
    public MarketDataInterval Interval { get; }
    public PriceAdjustment Adjustment { get; }
    public AcquisitionRequestId AcquisitionRequestId { get; }
    public DateTimeOffset AcquiredAtUtc { get; }
    public MarketDataPolicyReference Policy { get; }
    public EvidenceReference PolicyEvidence { get; }
    public int ObservationCount { get; }
    public int CorporateActionCount { get; }
    public int ExplicitGapCount { get; }
    public int RejectedObservationCount { get; }
    public int CorrectionCount { get; }
    public DatasetChecksum ContentChecksum { get; }
    public HistoricalStorageFormat StorageFormat { get; }
    public VersionReference StorageFormatVersion { get; }
    public RetentionClassification Retention { get; }
    public DeletionRequirement Deletion { get; }
    public DateTimeOffset? DeletionDeadlineUtc { get; }
    public ImmutableArray<EvidenceReference> Provenance { get; }
    public HistoricalManifestStatus Status { get; }
}

public static class HistoricalDatasetIntegrity
{
    public static DatasetChecksum Compute(ImmutableDatasetRevision revision)
    {
        ArgumentNullException.ThrowIfNull(revision);
        var builder = new StringBuilder();
        builder.Append(revision.Id.Value).Append('|').Append(revision.ParentRevisionId?.Value ?? "-").Append('\n');
        foreach (var member in revision.Members.OrderBy(value => value.LogicalObservationIdentity, StringComparer.Ordinal).ThenBy(value => value.Id.Value, StringComparer.Ordinal))
        {
            builder.Append(member.Id.Value).Append('|').Append(member.Type).Append('|').Append(member.LogicalObservationIdentity)
                .Append('|').Append(member.AvailableAtUtc.ToString("O", CultureInfo.InvariantCulture)).Append('|')
                .Append(member.Policy.Id.Value).Append('@').Append(member.Policy.Version.Value);
            if (member.Bar is { } bar)
                builder.Append('|').Append(bar.Open.Value.ToString(CultureInfo.InvariantCulture)).Append('|').Append(bar.High.Value.ToString(CultureInfo.InvariantCulture))
                    .Append('|').Append(bar.Low.Value.ToString(CultureInfo.InvariantCulture)).Append('|').Append(bar.Close.Value.ToString(CultureInfo.InvariantCulture))
                    .Append('|').Append(bar.Volume.ToString(CultureInfo.InvariantCulture)).Append('|').Append(bar.Adjustment);
            if (member.CorporateAction is { } action)
                builder.Append('|').Append(action.Id.Value).Append('|').Append(action.Type).Append('|').Append(action.ExDate.ToString("O", CultureInfo.InvariantCulture))
                    .Append('|').Append(action.EffectiveDate.ToString("O", CultureInfo.InvariantCulture)).Append('|').Append(action.CashAmount?.Amount.ToString(CultureInfo.InvariantCulture) ?? "-")
                    .Append('|').Append(action.SplitRatio?.Numerator.ToString(CultureInfo.InvariantCulture) ?? "-").Append('/')
                    .Append(action.SplitRatio?.Denominator.ToString(CultureInfo.InvariantCulture) ?? "-");
            if (member.QualityEvidence is { } quality)
                builder.Append('|').Append(quality.FindingCode).Append('|').Append(quality.Classification).Append('|').Append(quality.Evidence.Value);
            builder.Append('\n');
        }
        foreach (var correction in revision.Corrections.OrderBy(value => value.AvailableAtUtc).ThenBy(value => value.Id.Value, StringComparer.Ordinal))
            builder.Append("correction|").Append(correction.Id.Value).Append('|').Append(correction.OriginalMemberId.Value).Append('|')
                .Append(correction.ReplacementMemberId.Value).Append('|').Append(correction.AvailableAtUtc.ToString("O", CultureInfo.InvariantCulture))
                .Append('|').Append(correction.ReasonCode).Append('|').Append(correction.Evidence.Value).Append('\n');
        return new DatasetChecksum($"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant()}");
    }

    public static bool Verify(HistoricalDatasetManifest manifest, ImmutableDatasetRevision revision) =>
        manifest.RevisionId == revision.Id && manifest.Status == HistoricalManifestStatus.Complete &&
        manifest.ContentChecksum == Compute(revision);
}

public sealed record HistoricalDataDeletionReceipt(
    string ReceiptId, DateTimeOffset DeletedAtUtc, MarketDataProvider Provider, ProviderDataset ProviderDataset,
    MarketDataPolicyReference Policy, ImmutableArray<DatasetRevisionId> DeletedRevisions,
    EvidenceReference ReasonEvidence, DatasetChecksum AuditFingerprint);

public interface IHistoricalDataPersistence
{
    void Append(HistoricalDatasetManifest manifest, ImmutableDatasetRevision revision);
    HistoricalDatasetManifest GetManifest(DatasetRevisionId revisionId);
    ImmutableDatasetRevision GetRevision(DatasetRevisionId revisionId);
    IReadOnlyList<CanonicalMarketBar> QueryBars(DatasetRevisionId revisionId, InstrumentId instrumentId, DateOnly fromDate, DateOnly toDate);
    IReadOnlyList<CanonicalCorporateAction> QueryCorporateActions(DatasetRevisionId revisionId, InstrumentId instrumentId, DateOnly fromDate, DateOnly toDate);
    IReadOnlyList<ReplayQualityEvidence> QueryQualityEvidence(DatasetRevisionId revisionId, InstrumentId instrumentId, DateOnly fromDate, DateOnly toDate);
    IReadOnlyList<DatasetRevisionId> ResolveLineage(DatasetRevisionId revisionId);
    bool VerifyIntegrity(DatasetRevisionId revisionId);
    IReadOnlyList<HistoricalDatasetManifest> Enumerate(MarketDataProvider provider, ProviderDataset providerDataset, MarketDataPolicyReference policy);
    HistoricalDataDeletionReceipt Delete(MarketDataProvider provider, ProviderDataset providerDataset,
        MarketDataPolicyReference policy, DateTimeOffset deletedAtUtc, EvidenceReference reasonEvidence);
    IReadOnlyList<HistoricalDataDeletionReceipt> DeletionReceipts { get; }
}

public sealed class InMemoryHistoricalDataPersistence : IHistoricalDataPersistence
{
    private readonly Dictionary<DatasetRevisionId, (HistoricalDatasetManifest Manifest, ImmutableDatasetRevision Revision)> _entries = [];
    private readonly List<HistoricalDataDeletionReceipt> _receipts = [];

    public IReadOnlyList<HistoricalDataDeletionReceipt> DeletionReceipts => _receipts.ToImmutableArray();

    public void Append(HistoricalDatasetManifest manifest, ImmutableDatasetRevision revision)
    {
        ArgumentNullException.ThrowIfNull(manifest); ArgumentNullException.ThrowIfNull(revision);
        if (manifest.Status != HistoricalManifestStatus.Complete)
            throw new InvalidOperationException("Only complete manifests may become visible.");
        if (manifest.RevisionId != revision.Id || manifest.ParentRevisionId != revision.ParentRevisionId || !HistoricalDatasetIntegrity.Verify(manifest, revision))
            throw new InvalidDataException("Manifest/revision identity or checksum is invalid.");
        if (_entries.TryGetValue(revision.Id, out var existing))
        {
            if (existing.Manifest == manifest && existing.Revision == revision) return;
            throw new InvalidDataException("A revision ID cannot be overwritten with conflicting content.");
        }
        if (revision.ParentRevisionId is { } parent && !_entries.ContainsKey(parent))
            throw new InvalidDataException("A child revision requires its immutable parent.");
        _entries.Add(revision.Id, (manifest, revision));
    }

    public HistoricalDatasetManifest GetManifest(DatasetRevisionId revisionId) => Entry(revisionId).Manifest;
    public ImmutableDatasetRevision GetRevision(DatasetRevisionId revisionId) => Entry(revisionId).Revision;
    public IReadOnlyList<CanonicalMarketBar> QueryBars(DatasetRevisionId revisionId, InstrumentId instrumentId, DateOnly fromDate, DateOnly toDate) =>
        Entry(revisionId).Revision.Members.Where(value => value.Bar is { } bar && bar.InstrumentId == instrumentId && bar.SessionDate >= fromDate && bar.SessionDate <= toDate)
            .Select(value => value.Bar!).OrderBy(value => value.SessionDate).ToImmutableArray();
    public IReadOnlyList<CanonicalCorporateAction> QueryCorporateActions(DatasetRevisionId revisionId, InstrumentId instrumentId, DateOnly fromDate, DateOnly toDate) =>
        Entry(revisionId).Revision.Members.Where(value => value.CorporateAction is { } action && action.InstrumentId == instrumentId && action.EffectiveDate >= fromDate && action.EffectiveDate <= toDate)
            .Select(value => value.CorporateAction!).OrderBy(value => value.EffectiveDate).ThenBy(value => value.Id.Value, StringComparer.Ordinal).ToImmutableArray();
    public IReadOnlyList<ReplayQualityEvidence> QueryQualityEvidence(DatasetRevisionId revisionId, InstrumentId instrumentId, DateOnly fromDate, DateOnly toDate) =>
        Entry(revisionId).Revision.Members.Where(value => value.QualityEvidence is { } finding && finding.InstrumentId == instrumentId && finding.TradingDate >= fromDate && finding.TradingDate <= toDate)
            .Select(value => value.QualityEvidence!).OrderBy(value => value.TradingDate).ThenBy(value => value.FindingCode).ToImmutableArray();
    public IReadOnlyList<DatasetRevisionId> ResolveLineage(DatasetRevisionId revisionId) => Entry(revisionId).Revision.Ancestry;
    public bool VerifyIntegrity(DatasetRevisionId revisionId) { var value = Entry(revisionId); return HistoricalDatasetIntegrity.Verify(value.Manifest, value.Revision); }
    public IReadOnlyList<HistoricalDatasetManifest> Enumerate(MarketDataProvider provider, ProviderDataset providerDataset, MarketDataPolicyReference policy) =>
        _entries.Values.Where(value => value.Manifest.Provider == provider && value.Manifest.ProviderDataset == providerDataset && value.Manifest.Policy == policy)
            .Select(value => value.Manifest).OrderBy(value => value.AcquiredAtUtc).ThenBy(value => value.RevisionId.Value, StringComparer.Ordinal).ToImmutableArray();

    public HistoricalDataDeletionReceipt Delete(MarketDataProvider provider, ProviderDataset providerDataset,
        MarketDataPolicyReference policy, DateTimeOffset deletedAtUtc, EvidenceReference reasonEvidence)
    {
        FinanceTime.RequireUtc(deletedAtUtc, nameof(deletedAtUtc)); RequiredText.Require(reasonEvidence.Value, nameof(reasonEvidence));
        var ids = Enumerate(provider, providerDataset, policy).Select(value => value.RevisionId).OrderBy(value => value.Value, StringComparer.Ordinal).ToImmutableArray();
        foreach (var id in ids) _entries.Remove(id);
        var seed = $"{provider.Value}|{providerDataset.Value}|{policy.Id.Value}|{policy.Version.Value}|{deletedAtUtc:O}|{string.Join(',', ids.Select(value => value.Value))}|{reasonEvidence.Value}";
        var fingerprint = new DatasetChecksum($"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(seed))).ToLowerInvariant()}");
        var receipt = new HistoricalDataDeletionReceipt($"deletion:{fingerprint.Value[7..23]}", deletedAtUtc, provider,
            providerDataset, policy, ids, reasonEvidence, fingerprint);
        _receipts.Add(receipt); return receipt;
    }

    private (HistoricalDatasetManifest Manifest, ImmutableDatasetRevision Revision) Entry(DatasetRevisionId id) =>
        _entries.GetValueOrDefault(id) is { } value && value.Manifest is not null ? value :
            throw new KeyNotFoundException($"Historical revision '{id.Value}' is not available (unknown or deleted).");
}
