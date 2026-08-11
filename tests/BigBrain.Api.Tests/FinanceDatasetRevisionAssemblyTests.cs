using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceDatasetRevisionAssemblyTests
{
    private static readonly Currency Sek = new("SEK");
    private static readonly MarketDataProvider Provider = new("ExampleData");
    private static readonly ProviderDataset Product = new("Synthetic-EOD-Personal");
    private static readonly DatasetId DatasetId = new("synthetic-history-001");
    private static readonly InstrumentId InstrumentId = new("BB-EQ-TEST-001");
    private static readonly MarketVenue Xsto = new("XSTO", "Synthetic Stockholm");
    private static readonly DateOnly SessionDate = new(2024, 1, 2);
    private static readonly DateTimeOffset OriginalAvailable = Utc(2024, 1, 2, 19);
    private static readonly DateTimeOffset FirstCorrectionAvailable = Utc(2024, 1, 4, 10);
    private static readonly DateTimeOffset SecondCorrectionAvailable = Utc(2024, 1, 6, 12);

    [Fact]
    public void ReplayAsOfBeforeCorrectionSeesOnlyOriginalObservation()
    {
        var chain = Chain();
        var revision = chain.Catalog.ResolveAsOf(FirstCorrectionAvailable.AddTicks(-1));
        Assert.Equal("revision-001", revision.Id.Value);
        Assert.Equal(10m, Assert.Single(revision.Members, member => member.Type == DatasetMemberType.MarketBar).Bar!.Close.Value);
    }

    [Fact]
    public void AvailabilityBoundaryIsInclusiveAndExposesCorrectedRevision()
    {
        var chain = Chain();
        var revision = chain.Catalog.ResolveAsOf(FirstCorrectionAvailable);
        Assert.Equal("revision-002", revision.Id.Value);
        Assert.Equal(10.5m, Assert.Single(revision.Members, member => member.Type == DatasetMemberType.MarketBar).Bar!.Close.Value);
    }

    [Fact]
    public void OldRevisionRemainsExactlyReproducibleAfterSupersession()
    {
        var chain = Chain();
        var before = chain.Catalog.GetRevision(new DatasetRevisionId("revision-001"));
        _ = chain.Catalog.ResolveAsOf(SecondCorrectionAvailable.AddDays(1));
        var after = chain.Catalog.GetRevision(new DatasetRevisionId("revision-001"));
        Assert.Same(before, after);
        Assert.Equal(10m, Assert.Single(after.Members, value => value.Type == DatasetMemberType.MarketBar).Bar!.Close.Value);
        Assert.Equal(new DatasetRevisionId("revision-002"), chain.Catalog.GetSupersedingRevision(before.Id)!.Id);
        Assert.Null(chain.Catalog.GetSupersedingRevision(chain.Third.Id));
    }

    [Fact]
    public void MultipleCorrectionsFormDeterministicLinearSupersession()
    {
        var chain = Chain();
        var latest = chain.Catalog.ResolveAsOf(SecondCorrectionAvailable);
        Assert.Equal(["revision-001", "revision-002", "revision-003"], latest.Ancestry.Select(value => value.Value));
        Assert.Equal("bar-v3", Assert.Single(latest.Members, member => member.Type == DatasetMemberType.MarketBar).Id.Value);
        Assert.Equal(10.75m, Assert.Single(latest.Members, member => member.Type == DatasetMemberType.MarketBar).Bar!.Close.Value);
    }

    [Fact]
    public void FutureCorrectionCannotLeakIntoEarlierKnowledgeBoundary()
    {
        var chain = Chain();
        var dayFive = chain.Catalog.ResolveAsOf(SecondCorrectionAvailable.AddTicks(-1));
        Assert.Equal("bar-v2", Assert.Single(dayFive.Members, value => value.Type == DatasetMemberType.MarketBar).Id.Value);
        Assert.DoesNotContain(dayFive.Members, value => value.Id == new DatasetMemberId("bar-v3"));
    }

    [Fact]
    public void CorrectionWithUnknownOriginalFailsExplicitly()
    {
        var baseRevision = BaseRevision();
        var replacement = BarMember("bar-v2", "revision-002", 10.5m, FirstCorrectionAvailable);
        var correction = Correction("correction-001", "unknown", "bar-v2", FirstCorrectionAvailable);
        Assert.Throws<ArgumentException>(() => Assemble("revision-002", baseRevision, FirstCorrectionAvailable, [replacement], [correction]));
    }

    [Fact]
    public void CorrectionCycleBackToAncestorRevisionIdIsRejected()
    {
        var chain = Chain();
        var metadata = Metadata("revision-001", chain.Third.Id, SecondCorrectionAvailable.AddDays(1));
        Assert.Throws<ArgumentException>(() => ImmutableDatasetRevisionAssembler.Assemble(
            new DatasetRevisionAssemblyRequest(metadata, chain.Third, [], [])));
    }

    [Fact]
    public void CorrectionCycleCannotReintroduceReplacedAncestorMember()
    {
        var parent = BaseRevision();
        var replacement = BarMember("bar-v2", "revision-002", 10.5m, FirstCorrectionAvailable);
        var corrections = new[]
        {
            Correction("correction-001", "bar-v1", "bar-v2", FirstCorrectionAvailable),
            Correction("correction-002", "bar-v2", "bar-v1", FirstCorrectionAvailable)
        };
        Assert.Throws<ArgumentException>(() => Assemble("revision-002", parent, FirstCorrectionAvailable, [replacement], corrections));
    }

    [Fact]
    public void ReplacementMustPreserveLogicalObservationIdentity()
    {
        var baseRevision = BaseRevision();
        var wrongDate = BarMember("bar-v2", "revision-002", 10.5m, FirstCorrectionAvailable, SessionDate.AddDays(1));
        Assert.Throws<ArgumentException>(() => Assemble("revision-002", baseRevision, FirstCorrectionAvailable, [wrongDate],
            [Correction("correction-001", "bar-v1", "bar-v2", FirstCorrectionAvailable)]));
    }

    [Fact]
    public void CorrectionAvailabilityMustMatchReplacementAndNotPrecedeOriginal()
    {
        var baseRevision = BaseRevision();
        var replacement = BarMember("bar-v2", "revision-002", 10.5m, FirstCorrectionAvailable.AddMinutes(1));
        Assert.Throws<ArgumentException>(() => Assemble("revision-002", baseRevision, FirstCorrectionAvailable.AddMinutes(1), [replacement],
            [Correction("correction-001", "bar-v1", "bar-v2", FirstCorrectionAvailable)]));
    }

    [Fact]
    public void InputOrderingDoesNotChangeAssembledMembershipOrCorrectionOrder()
    {
        var parent = BaseRevision();
        var first = BarMember("bar-v2", "revision-002", 10.5m, FirstCorrectionAvailable);
        var extra = BarMember("bar-extra", "revision-002", 20m, FirstCorrectionAvailable, SessionDate.AddDays(1));
        var correction = Correction("correction-001", "bar-v1", "bar-v2", FirstCorrectionAvailable);
        var left = Assemble("revision-002", parent, FirstCorrectionAvailable, [first, extra], [correction]);
        var right = Assemble("revision-002", parent, FirstCorrectionAvailable, [extra, first], [correction]);
        Assert.Equal(left.Members.Select(member => member.Id), right.Members.Select(member => member.Id));
        Assert.Equal(left.Corrections.Select(value => value.Id), right.Corrections.Select(value => value.Id));
    }

    [Fact]
    public void RepeatedAssemblyProducesLogicallyIdenticalImmutableState()
    {
        var parent = BaseRevision();
        var replacement = BarMember("bar-v2", "revision-002", 10.5m, FirstCorrectionAvailable);
        var correction = Correction("correction-001", "bar-v1", "bar-v2", FirstCorrectionAvailable);
        var first = Assemble("revision-002", parent, FirstCorrectionAvailable, [replacement], [correction]);
        var second = Assemble("revision-002", parent, FirstCorrectionAvailable, [replacement], [correction]);
        Assert.Equal(first.Metadata, second.Metadata);
        Assert.Equal(first.Members.Select(MemberFingerprint), second.Members.Select(MemberFingerprint));
        Assert.DoesNotContain(typeof(ImmutableDatasetRevision).GetProperties(), property => property.SetMethod is not null);
    }

    [Fact]
    public void CorporateActionTemporalSemanticsAndPolicySurviveCorrectionChain()
    {
        var chain = Chain();
        var action = Assert.Single(chain.Third.Members, member => member.Type == DatasetMemberType.CorporateAction);
        Assert.Equal(SessionDate.AddDays(3), action.CorporateAction!.EffectiveDate);
        Assert.Equal(new ExactRatio(2, 1), action.CorporateAction.SplitRatio);
        Assert.Equal(new PolicyId("synthetic-policy"), action.Policy.Id);
        Assert.Equal(new DatasetRevisionId("revision-001"), action.SourceRevisionId);
    }

    [Fact]
    public void SessionGapEvidenceRemainsImmutableAcrossSupersession()
    {
        var chain = Chain();
        var finding = Assert.Single(chain.Third.Members, member => member.Type == DatasetMemberType.QualityEvidence);
        Assert.Equal(ObservationGapClassification.ExpectedClosure, finding.QualityEvidence!.Classification);
        Assert.Equal(MarketDataFindingCode.MissingObservation, finding.QualityEvidence.FindingCode);
        Assert.Equal(new DatasetRevisionId("revision-001"), finding.SourceRevisionId);
    }

    [Fact]
    public void HistoricalTickerMappingStillResolvesAtEventDate()
    {
        var catalog = InstrumentCatalog();
        Assert.Equal("TEST-A", catalog.ResolveProviderReference(InstrumentId, Provider, Product, "XSTO", new DateOnly(2024, 5, 31)).ProviderReference);
        Assert.Equal("TEST-B", catalog.ResolveProviderReference(InstrumentId, Provider, Product, "XSTO", new DateOnly(2024, 6, 1)).ProviderReference);
    }

    [Fact]
    public void BranchingSupersessionIsRejectedAsAmbiguous()
    {
        var parent = BaseRevision();
        var left = Assemble("revision-left", parent, FirstCorrectionAvailable, [], []);
        var right = Assemble("revision-right", parent, FirstCorrectionAvailable.AddHours(1), [], []);
        Assert.Throws<ArgumentException>(() => new ImmutableDatasetRevisionCatalog([parent, left, right]));
    }

    [Fact]
    public void UnrelatedRevisionRootsCannotBeSilentlyCombined()
    {
        var first = BaseRevision();
        var other = ImmutableDatasetRevisionAssembler.Assemble(new DatasetRevisionAssemblyRequest(
            Metadata("unrelated-root", null, FirstCorrectionAvailable), null, [], []));
        Assert.Throws<ArgumentException>(() => new ImmutableDatasetRevisionCatalog([first, other]));
    }

    [Fact]
    public void RevisionCannotContainMemberUnavailableAtCreation()
    {
        var member = BarMember("future-bar", "revision-001", 10m, OriginalAvailable.AddHours(2));
        Assert.Throws<ArgumentException>(() => ImmutableDatasetRevisionAssembler.Assemble(
            new DatasetRevisionAssemblyRequest(Metadata("revision-001", null, OriginalAvailable.AddHours(1)), null, [member], [])));
    }

    private static (ImmutableDatasetRevision First, ImmutableDatasetRevision Second, ImmutableDatasetRevision Third,
        ImmutableDatasetRevisionCatalog Catalog) Chain()
    {
        var first = BaseRevision();
        var second = Assemble("revision-002", first, FirstCorrectionAvailable,
            [BarMember("bar-v2", "revision-002", 10.5m, FirstCorrectionAvailable)],
            [Correction("correction-001", "bar-v1", "bar-v2", FirstCorrectionAvailable)]);
        var third = Assemble("revision-003", second, SecondCorrectionAvailable,
            [BarMember("bar-v3", "revision-003", 10.75m, SecondCorrectionAvailable)],
            [Correction("correction-002", "bar-v2", "bar-v3", SecondCorrectionAvailable)]);
        return (first, second, third, new ImmutableDatasetRevisionCatalog([third, first, second]));
    }

    private static ImmutableDatasetRevision BaseRevision()
    {
        var actionDate = SessionDate.AddDays(3);
        var action = new CanonicalCorporateAction(new CorporateActionId("split-001"), CorporateActionType.StockSplit,
            InstrumentId, actionDate, actionDate, null, new ExactRatio(2, 1), Provenance("revision-001", actionDate, OriginalAvailable));
        var finding = new ReplayQualityEvidence(InstrumentId, SessionDate.AddDays(4), MarketDataFindingCode.MissingObservation,
            ObservationGapClassification.ExpectedClosure, OriginalAvailable, new DatasetRevisionId("revision-001"),
            new EvidenceReference("fixture:calendar-closure"));
        return ImmutableDatasetRevisionAssembler.Assemble(new DatasetRevisionAssemblyRequest(
            Metadata("revision-001", null, OriginalAvailable.AddHours(1)), null,
            [BarMember("bar-v1", "revision-001", 10m, OriginalAvailable),
             DatasetRevisionMember.ForCorporateAction(new DatasetMemberId("split-member"), action, OriginalAvailable),
             DatasetRevisionMember.ForQualityEvidence(new DatasetMemberId("closure-member"), finding, Policy())], []));
    }

    private static ImmutableDatasetRevision Assemble(string id, ImmutableDatasetRevision parent, DateTimeOffset created,
        IReadOnlyList<DatasetRevisionMember> additions, IReadOnlyList<DatasetCorrection> corrections) =>
        ImmutableDatasetRevisionAssembler.Assemble(new DatasetRevisionAssemblyRequest(
            Metadata(id, parent.Id, created), parent, additions, corrections));

    private static DatasetRevisionMember BarMember(string memberId, string revisionId, decimal close,
        DateTimeOffset available, DateOnly? sessionDate = null)
    {
        var date = sessionDate ?? SessionDate;
        var bar = new CanonicalMarketBar(InstrumentId, MarketDataInterval.Daily, date,
            new Price(close, Sek), new Price(close + 1m, Sek), new Price(close - 1m, Sek), new Price(close, Sek),
            1000m, PriceAdjustment.Raw, null, Provenance(revisionId, date, available));
        return DatasetRevisionMember.ForBar(new DatasetMemberId(memberId), bar, available);
    }

    private static DatasetCorrection Correction(string id, string original, string replacement, DateTimeOffset available) =>
        new(new DatasetCorrectionId(id), new DatasetMemberId(original), new DatasetMemberId(replacement), available,
            "provider.correction.synthetic", new EvidenceReference($"fixture:{id}"));

    private static DatasetRevision Metadata(string id, DatasetRevisionId? parentId, DateTimeOffset created) => new(
        new DatasetRevisionId(id), DatasetId, parentId, Provider, Product, new VersionReference("fixture-v1"),
        new VersionReference("schema-v1"), new DatasetChecksum($"sha256:{id}"), created, created.AddMinutes(-1),
        DatasetRevisionStatus.Complete);

    private static MarketDataProvenance Provenance(string revisionId, DateOnly date, DateTimeOffset available) => new(
        Provider, Product, available,
        new DateTimeOffset(date.ToDateTime(new TimeOnly(18, 0), DateTimeKind.Utc)) <= available
            ? new DateTimeOffset(date.ToDateTime(new TimeOnly(18, 0), DateTimeKind.Utc))
            : available.AddMinutes(-5),
        InstrumentId, Xsto, new DatasetRevisionId(revisionId), Policy(), MarketDataClassification.Raw, [],
        new VersionReference("fixture-v1"), new VersionReference("schema-v1"), MarketDataQualityStatus.Valid);

    private static InstrumentMappingCatalog InstrumentCatalog() => new([
        new CanonicalInstrument(InstrumentId, InstrumentType.Equity, "Synthetic Example AB", Sek, Xsto, "XSTO",
            InstrumentLifecycle.Active, new DateOnly(2020, 1, 1))], [
        new ProviderInstrumentMapping(InstrumentId, Provider, Product, "TEST-A", Xsto, "XSTO", new DateOnly(2020, 1, 1),
            new DateOnly(2024, 5, 31), new EvidenceReference("fixture:mapping-v1")),
        new ProviderInstrumentMapping(InstrumentId, Provider, Product, "TEST-B", Xsto, "XSTO", new DateOnly(2024, 6, 1),
            null, new EvidenceReference("fixture:mapping-v1"))]);

    private static string MemberFingerprint(DatasetRevisionMember member) =>
        $"{member.Id.Value}|{member.Type}|{member.LogicalObservationIdentity}|{member.AvailableAtUtc:O}|{member.SourceRevisionId.Value}";
    private static MarketDataPolicyReference Policy() => new(new PolicyId("synthetic-policy"), new PolicyVersion("1"));
    private static DateTimeOffset Utc(int year, int month, int day, int hour) => new(year, month, day, hour, 0, 0, TimeSpan.Zero);
}
