using System.Collections.Immutable;

namespace BigBrain.Modules.Finance;

public enum DatasetMemberType { Unknown = 0, MarketBar, CorporateAction, QualityEvidence }

public readonly record struct DatasetMemberId
{
    public DatasetMemberId(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct DatasetCorrectionId
{
    public DatasetCorrectionId(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public sealed record DatasetRevisionMember
{
    private DatasetRevisionMember(
        DatasetMemberId id,
        DatasetMemberType type,
        string logicalObservationIdentity,
        DateTimeOffset availableAtUtc,
        DatasetRevisionId sourceRevisionId,
        MarketDataPolicyReference policy,
        CanonicalMarketBar? bar,
        CanonicalCorporateAction? corporateAction,
        ReplayQualityEvidence? qualityEvidence)
    {
        Id = id;
        Type = type;
        LogicalObservationIdentity = logicalObservationIdentity;
        AvailableAtUtc = availableAtUtc;
        SourceRevisionId = sourceRevisionId;
        Policy = policy;
        Bar = bar;
        CorporateAction = corporateAction;
        QualityEvidence = qualityEvidence;
    }

    public DatasetMemberId Id { get; }
    public DatasetMemberType Type { get; }
    public string LogicalObservationIdentity { get; }
    public DateTimeOffset AvailableAtUtc { get; }
    public DatasetRevisionId SourceRevisionId { get; }
    public MarketDataPolicyReference Policy { get; }
    public CanonicalMarketBar? Bar { get; }
    public CanonicalCorporateAction? CorporateAction { get; }
    public ReplayQualityEvidence? QualityEvidence { get; }

    public static DatasetRevisionMember ForBar(DatasetMemberId id, CanonicalMarketBar bar, DateTimeOffset availableAtUtc)
    {
        ArgumentNullException.ThrowIfNull(bar);
        return Create(id, DatasetMemberType.MarketBar, bar.Identity, availableAtUtc,
            bar.Provenance.DatasetRevisionId, bar.Provenance.Policy, bar, null, null);
    }

    public static DatasetRevisionMember ForCorporateAction(
        DatasetMemberId id, CanonicalCorporateAction action, DateTimeOffset availableAtUtc)
    {
        ArgumentNullException.ThrowIfNull(action);
        var identity = $"{action.InstrumentId.Value}|{action.Type}|{action.EffectiveDate:yyyy-MM-dd}|{action.Id.Value}";
        return Create(id, DatasetMemberType.CorporateAction, identity, availableAtUtc,
            action.Provenance.DatasetRevisionId, action.Provenance.Policy, null, action, null);
    }

    public static DatasetRevisionMember ForQualityEvidence(
        DatasetMemberId id, ReplayQualityEvidence finding, MarketDataPolicyReference policy)
    {
        ArgumentNullException.ThrowIfNull(finding);
        finding.Validate();
        var identity = $"{finding.InstrumentId.Value}|quality|{finding.TradingDate:yyyy-MM-dd}|{finding.FindingCode}";
        return Create(id, DatasetMemberType.QualityEvidence, identity, finding.ObservedAtUtc,
            finding.DatasetRevisionId, policy, null, null, finding);
    }

    private static DatasetRevisionMember Create(
        DatasetMemberId id,
        DatasetMemberType type,
        string identity,
        DateTimeOffset availableAtUtc,
        DatasetRevisionId sourceRevisionId,
        MarketDataPolicyReference policy,
        CanonicalMarketBar? bar,
        CanonicalCorporateAction? action,
        ReplayQualityEvidence? finding)
    {
        RequiredText.Require(id.Value, nameof(id));
        if (type == DatasetMemberType.Unknown || !Enum.IsDefined(type))
            throw new ArgumentException("Dataset member type is required.", nameof(type));
        var normalizedIdentity = RequiredText.Normalize(identity, nameof(identity));
        FinanceTime.RequireUtc(availableAtUtc, nameof(availableAtUtc));
        RequiredText.Require(sourceRevisionId.Value, nameof(sourceRevisionId));
        policy.Validate();
        return new DatasetRevisionMember(id, type, normalizedIdentity, availableAtUtc,
            sourceRevisionId, policy, bar, action, finding);
    }
}

public sealed record DatasetCorrection
{
    public DatasetCorrection(
        DatasetCorrectionId id,
        DatasetMemberId originalMemberId,
        DatasetMemberId replacementMemberId,
        DateTimeOffset availableAtUtc,
        string reasonCode,
        EvidenceReference evidence)
    {
        RequiredText.Require(id.Value, nameof(id));
        RequiredText.Require(originalMemberId.Value, nameof(originalMemberId));
        RequiredText.Require(replacementMemberId.Value, nameof(replacementMemberId));
        if (originalMemberId == replacementMemberId)
            throw new ArgumentException("A correction must replace a different member.", nameof(replacementMemberId));
        FinanceTime.RequireUtc(availableAtUtc, nameof(availableAtUtc));
        ReasonCode = RequiredText.Normalize(reasonCode, nameof(reasonCode));
        RequiredText.Require(evidence.Value, nameof(evidence));
        Id = id;
        OriginalMemberId = originalMemberId;
        ReplacementMemberId = replacementMemberId;
        AvailableAtUtc = availableAtUtc;
        Evidence = evidence;
    }

    public DatasetCorrectionId Id { get; }
    public DatasetMemberId OriginalMemberId { get; }
    public DatasetMemberId ReplacementMemberId { get; }
    public DateTimeOffset AvailableAtUtc { get; }
    public string ReasonCode { get; }
    public EvidenceReference Evidence { get; }
}

public sealed record DatasetRevisionAssemblyRequest(
    DatasetRevision Revision,
    ImmutableDatasetRevision? Parent,
    IReadOnlyList<DatasetRevisionMember> Additions,
    IReadOnlyList<DatasetCorrection> Corrections);

public sealed record ImmutableDatasetRevision
{
    internal ImmutableDatasetRevision(
        DatasetRevision metadata,
        ImmutableArray<DatasetRevisionMember> members,
        ImmutableArray<DatasetCorrection> corrections,
        ImmutableArray<DatasetRevisionId> ancestry)
    {
        Metadata = metadata;
        Members = members;
        Corrections = corrections;
        Ancestry = ancestry;
    }

    public DatasetRevision Metadata { get; }
    public DatasetRevisionId Id => Metadata.Id;
    public DatasetRevisionId? ParentRevisionId => Metadata.ParentRevisionId;
    public DateTimeOffset AvailableAtUtc => Metadata.CreatedAtUtc;
    public ImmutableArray<DatasetRevisionMember> Members { get; }
    public ImmutableArray<DatasetCorrection> Corrections { get; }
    public ImmutableArray<DatasetRevisionId> Ancestry { get; }

    public DatasetRevisionMember GetMember(DatasetMemberId id) =>
        Members.SingleOrDefault(member => member.Id == id)
        ?? throw new KeyNotFoundException($"Dataset member '{id.Value}' is not present in revision '{Id.Value}'.");
}

public static class ImmutableDatasetRevisionAssembler
{
    public static ImmutableDatasetRevision Assemble(DatasetRevisionAssemblyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Revision);
        ArgumentNullException.ThrowIfNull(request.Additions);
        ArgumentNullException.ThrowIfNull(request.Corrections);
        ValidateParent(request.Revision, request.Parent);

        var additions = request.Additions.OrderBy(member => member.Id.Value, StringComparer.Ordinal).ToArray();
        var corrections = request.Corrections
            .OrderBy(correction => correction.AvailableAtUtc)
            .ThenBy(correction => correction.Id.Value, StringComparer.Ordinal)
            .ToArray();
        EnsureUnique(additions.Select(member => member.Id), "Dataset member IDs must be unique within a revision.");
        EnsureUnique(corrections.Select(correction => correction.Id), "Correction IDs must be unique within a revision.");
        if (additions.Any(member => member.SourceRevisionId != request.Revision.Id))
            throw new ArgumentException("New members must preserve provenance for the revision that introduces them.", nameof(request));
        if (additions.Any(member => member.AvailableAtUtc > request.Revision.CreatedAtUtc))
            throw new ArgumentException("A revision cannot contain members unavailable when the revision was created.", nameof(request));
        if (corrections.Any(correction => correction.AvailableAtUtc > request.Revision.CreatedAtUtc))
            throw new ArgumentException("A revision cannot contain a correction unavailable when the revision was created.", nameof(request));

        var inherited = request.Parent?.Members.ToDictionary(member => member.Id) ?? new Dictionary<DatasetMemberId, DatasetRevisionMember>();
        var additionsById = additions.ToDictionary(member => member.Id);
        if (additionsById.Keys.Any(inherited.ContainsKey))
            throw new ArgumentException("A new revision member ID cannot overwrite an inherited member.", nameof(request));
        var current = new Dictionary<DatasetMemberId, DatasetRevisionMember>(inherited);
        var usedReplacements = new HashSet<DatasetMemberId>();

        foreach (var correction in corrections)
        {
            if (!current.TryGetValue(correction.OriginalMemberId, out var original))
                throw new ArgumentException("Correction original member is not active in the parent/correction chain.", nameof(request));
            if (!additionsById.TryGetValue(correction.ReplacementMemberId, out var replacement))
                throw new ArgumentException("Correction replacement must be introduced by the correcting revision.", nameof(request));
            if (!usedReplacements.Add(replacement.Id))
                throw new ArgumentException("A correction replacement can be applied only once.", nameof(request));
            if (replacement.Type != original.Type || replacement.LogicalObservationIdentity != original.LogicalObservationIdentity)
                throw new ArgumentException("A correction must preserve member type and logical observation identity.", nameof(request));
            if (replacement.AvailableAtUtc != correction.AvailableAtUtc || correction.AvailableAtUtc < original.AvailableAtUtc)
                throw new ArgumentException("Correction and replacement availability must match and cannot precede the original.", nameof(request));
            current.Remove(original.Id);
            current.Add(replacement.Id, replacement);
        }

        foreach (var addition in additions.Where(member => !usedReplacements.Contains(member.Id)))
            current.Add(addition.Id, addition);

        var members = current.Values.OrderBy(member => member.LogicalObservationIdentity, StringComparer.Ordinal)
            .ThenBy(member => member.Id.Value, StringComparer.Ordinal).ToImmutableArray();
        var ancestry = request.Parent is null
            ? ImmutableArray.Create(request.Revision.Id)
            : request.Parent.Ancestry.Add(request.Revision.Id);
        return new ImmutableDatasetRevision(request.Revision, members, corrections.ToImmutableArray(), ancestry);
    }

    private static void ValidateParent(DatasetRevision revision, ImmutableDatasetRevision? parent)
    {
        if (revision.ParentRevisionId is null && parent is not null)
            throw new ArgumentException("A base revision cannot receive a parent snapshot.", nameof(parent));
        if (revision.ParentRevisionId is { } expected && (parent is null || parent.Id != expected))
            throw new ArgumentException("Revision parent metadata must match the supplied parent snapshot.", nameof(parent));
        if (parent is null) return;
        if (parent.Ancestry.Contains(revision.Id))
            throw new ArgumentException("Dataset revision relationships cannot contain cycles.", nameof(revision));
        if (revision.Provider != parent.Metadata.Provider || revision.ProviderDataset != parent.Metadata.ProviderDataset ||
            revision.DatasetId != parent.Metadata.DatasetId)
            throw new ArgumentException("A revision chain cannot change dataset, provider or provider product.", nameof(revision));
        if (revision.CreatedAtUtc < parent.AvailableAtUtc)
            throw new ArgumentException("A child revision cannot be available before its parent.", nameof(revision));
    }

    private static void EnsureUnique<T>(IEnumerable<T> values, string message) where T : notnull
    {
        var array = values.ToArray();
        if (array.Distinct().Count() != array.Length) throw new ArgumentException(message);
    }
}

public sealed class ImmutableDatasetRevisionCatalog
{
    private readonly ImmutableArray<ImmutableDatasetRevision> _revisions;
    private readonly ImmutableDictionary<DatasetRevisionId, ImmutableDatasetRevision> _byId;
    private readonly ImmutableDictionary<DatasetRevisionId, ImmutableDatasetRevision> _successors;

    public ImmutableDatasetRevisionCatalog(IEnumerable<ImmutableDatasetRevision> revisions)
    {
        ArgumentNullException.ThrowIfNull(revisions);
        _revisions = revisions.OrderBy(revision => revision.AvailableAtUtc)
            .ThenBy(revision => revision.Id.Value, StringComparer.Ordinal).ToImmutableArray();
        if (_revisions.IsEmpty) throw new ArgumentException("At least one dataset revision is required.", nameof(revisions));
        if (_revisions.Select(revision => revision.Id).Distinct().Count() != _revisions.Length)
            throw new ArgumentException("Dataset revision IDs must be unique.", nameof(revisions));
        var roots = _revisions.Where(revision => revision.ParentRevisionId is null).ToArray();
        if (roots.Length != 1 || _revisions.Any(revision => revision.Ancestry.IsEmpty || revision.Ancestry[0] != roots[0].Id))
            throw new ArgumentException("A revision catalog must contain exactly one connected revision chain.", nameof(revisions));
        _byId = _revisions.ToImmutableDictionary(revision => revision.Id);
        foreach (var revision in _revisions)
            if (revision.ParentRevisionId is { } parentId && (!_byId.TryGetValue(parentId, out var parent) ||
                !revision.Ancestry.SequenceEqual(parent.Ancestry.Add(revision.Id))))
                throw new ArgumentException("Every revision must reference a valid acyclic parent chain.", nameof(revisions));
        if (_revisions.Where(revision => revision.ParentRevisionId is not null)
            .GroupBy(revision => revision.ParentRevisionId).Any(group => group.Count() > 1))
            throw new ArgumentException("The current provider-neutral catalog requires a linear revision chain.", nameof(revisions));
        _successors = _revisions.Where(revision => revision.ParentRevisionId is not null)
            .ToImmutableDictionary(revision => revision.ParentRevisionId!.Value, revision => revision);
    }

    public ImmutableDatasetRevision GetRevision(DatasetRevisionId id) =>
        _byId.GetValueOrDefault(id) ?? throw new KeyNotFoundException($"Dataset revision '{id.Value}' is unknown.");

    public ImmutableDatasetRevision? GetSupersedingRevision(DatasetRevisionId id)
    {
        if (!_byId.ContainsKey(id)) throw new KeyNotFoundException($"Dataset revision '{id.Value}' is unknown.");
        return _successors.GetValueOrDefault(id);
    }

    public ImmutableDatasetRevision ResolveAsOf(DateTimeOffset availableAtUtc)
    {
        FinanceTime.RequireUtc(availableAtUtc, nameof(availableAtUtc));
        return _revisions.LastOrDefault(revision => revision.AvailableAtUtc <= availableAtUtc)
            ?? throw new KeyNotFoundException("No dataset revision was available at the supplied knowledge boundary.");
    }
}
