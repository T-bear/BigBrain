using System.Text.Json;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

public sealed class FinanceHistoricalDataPersistenceTests
{
    private static readonly string[] SensitiveManifestTerms = ["key", "secret", "token", "header", "credential", "payload"];

    private static readonly MarketDataProvider Provider = new("SyntheticFixture");
    private static readonly ProviderDataset Product = new("Synthetic-EOD-Personal");
    private static readonly MarketDataPolicyReference Policy = new(new PolicyId("synthetic-policy"), new PolicyVersion("1"));
    private static readonly InstrumentId Instrument = new("BB-EQ-TEST-001");
    private static readonly MarketVenue Xsto = new("XSTO", "Synthetic Stockholm");
    private static readonly Currency Sek = new("SEK");
    private static readonly DateTimeOffset Available = Utc(2024, 6, 5, 20);

    [Fact]
    public void ManifestAndChecksumAreDeterministic()
    {
        var revision = BaseRevision();
        Assert.Equal(JsonSerializer.Serialize(Manifest(revision)), JsonSerializer.Serialize(Manifest(revision)));
        Assert.Equal(HistoricalDatasetIntegrity.Compute(revision), HistoricalDatasetIntegrity.Compute(revision));
        Assert.True(HistoricalDatasetIntegrity.Verify(Manifest(revision), revision));
    }

    [Fact]
    public void ManifestChecksumDetectsMutation()
    {
        var original = BaseRevision();
        var mutated = RootRevision(11m);
        Assert.False(HistoricalDatasetIntegrity.Verify(Manifest(original), mutated));
    }

    [Fact]
    public void PersistenceRoundtripAndTypedQueriesPreserveEvidence()
    {
        var revision = BaseRevision(includeActionAndGap: true);
        var store = new InMemoryHistoricalDataPersistence();
        store.Append(Manifest(revision), revision);
        Assert.Same(revision, store.GetRevision(revision.Id));
        Assert.Single(store.QueryBars(revision.Id, Instrument, new DateOnly(2024, 6, 3), new DateOnly(2024, 6, 3)));
        Assert.Single(store.QueryCorporateActions(revision.Id, Instrument, new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 10)));
        Assert.Single(store.QueryQualityEvidence(revision.Id, Instrument, new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 10)));
        Assert.True(store.VerifyIntegrity(revision.Id));
    }

    [Fact]
    public void IdenticalAppendIsIdempotentButConflictFails()
    {
        var revision = BaseRevision(); var store = new InMemoryHistoricalDataPersistence(); var manifest = Manifest(revision);
        store.Append(manifest, revision); store.Append(manifest, revision);
        Assert.Single(store.Enumerate(Provider, Product, Policy));
        var conflict = RootRevision(11m);
        Assert.Throws<InvalidDataException>(() => store.Append(Manifest(conflict), conflict));
    }

    [Fact]
    public void IncompleteManifestNeverBecomesVisible()
    {
        var revision = BaseRevision(); var store = new InMemoryHistoricalDataPersistence();
        Assert.Throws<InvalidOperationException>(() => store.Append(Manifest(revision, HistoricalManifestStatus.Staging), revision));
        Assert.Throws<KeyNotFoundException>(() => store.GetRevision(revision.Id));
    }

    [Fact]
    public void CorrectionCreatesImmutableLineageAndOldRevisionRemainsReproducible()
    {
        var first = BaseRevision(); var second = CorrectedRevision(first); var store = new InMemoryHistoricalDataPersistence();
        store.Append(Manifest(first), first); store.Append(Manifest(second), second);
        Assert.Equal([first.Id, second.Id], store.ResolveLineage(second.Id));
        Assert.Equal(10m, Assert.Single(store.QueryBars(first.Id, Instrument, new DateOnly(2024, 6, 3), new DateOnly(2024, 6, 3))).Close.Value);
        Assert.Equal(10.5m, Assert.Single(store.QueryBars(second.Id, Instrument, new DateOnly(2024, 6, 3), new DateOnly(2024, 6, 3))).Close.Value);
    }

    [Fact]
    public void ProviderPolicyEnumerationIsExactlyScoped()
    {
        var first = BaseRevision(); var other = RootRevision(20m, "other-revision", new MarketDataProvider("OtherSynthetic"));
        var store = new InMemoryHistoricalDataPersistence(); store.Append(Manifest(first), first); store.Append(Manifest(other), other);
        Assert.Single(store.Enumerate(Provider, Product, Policy));
        Assert.Equal(first.Id, store.Enumerate(Provider, Product, Policy)[0].RevisionId);
    }

    [Fact]
    public void ScopedDeletionRemovesPayloadButPreservesAuditableReceipt()
    {
        var first = BaseRevision(); var otherProvider = new MarketDataProvider("OtherSynthetic");
        var other = RootRevision(20m, "other-revision", otherProvider); var store = new InMemoryHistoricalDataPersistence();
        store.Append(Manifest(first), first); store.Append(Manifest(other), other);
        var receipt = store.Delete(Provider, Product, Policy, Utc(2024, 7, 1, 0), new EvidenceReference("fixture:license-ended"));
        Assert.Equal([first.Id], receipt.DeletedRevisions); Assert.Single(store.DeletionReceipts);
        Assert.Throws<KeyNotFoundException>(() => store.GetRevision(first.Id));
        Assert.Equal(other.Id, store.GetRevision(other.Id).Id);
        Assert.DoesNotContain("10.0000", JsonSerializer.Serialize(receipt), StringComparison.Ordinal);
    }

    [Fact]
    public void EntitlementAndDeletionMetadataSurviveManifestRoundtrip()
    {
        var manifest = Manifest(BaseRevision());
        Assert.Equal(Policy, manifest.Policy); Assert.Equal("fixture:policy", manifest.PolicyEvidence.Value);
        Assert.Equal(RetentionClassification.SubscriptionOnly, manifest.Retention);
        Assert.Equal(DeletionRequirement.DeleteAtSubscriptionEnd, manifest.Deletion);
    }

    [Fact]
    public void ManifestHasNoSecretOrRawPayloadSurface()
    {
        var names = typeof(HistoricalDatasetManifest).GetProperties().Select(value => value.Name);
        Assert.DoesNotContain(names, value => SensitiveManifestTerms
            .Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase)));
        var json = JsonSerializer.Serialize(Manifest(BaseRevision()));
        Assert.DoesNotContain("Authorization", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PersistedBarsRemainReplayCompatible()
    {
        var revision = BaseRevision(); var store = new InMemoryHistoricalDataPersistence(); store.Append(Manifest(revision), revision);
        var bars = store.QueryBars(revision.Id, Instrument, new DateOnly(2024, 6, 3), new DateOnly(2024, 6, 3));
        var calendar = new SyntheticMarketSessionCalendar(Xsto, "XSTO", "Europe/Stockholm", [Trading(new DateOnly(2024, 6, 3))]);
        var events = new DeterministicHistoricalReplay(calendar, Mappings()).Replay(new HistoricalReplayRequest(revision.Id,
            Provider, Product, "XSTO", Utc(2024, 6, 3, 0), Utc(2024, 6, 6, 0), [Instrument], bars, [], []));
        Assert.Single(events, value => value.Type == HistoricalReplayEventType.ObservationAvailable);
    }

    [Fact]
    public void PersistedRevisionCatalogPreservesNoLookahead()
    {
        var first = BaseRevision(); var second = CorrectedRevision(first); var store = new InMemoryHistoricalDataPersistence();
        store.Append(Manifest(first), first); store.Append(Manifest(second), second);
        var catalog = new ImmutableDatasetRevisionCatalog([store.GetRevision(second.Id), store.GetRevision(first.Id)]);
        Assert.Equal(first.Id, catalog.ResolveAsOf(second.AvailableAtUtc.AddTicks(-1)).Id);
        Assert.Equal(second.Id, catalog.ResolveAsOf(second.AvailableAtUtc).Id);
    }

    private static HistoricalDatasetManifest Manifest(ImmutableDatasetRevision revision,
        HistoricalManifestStatus status = HistoricalManifestStatus.Complete)
    {
        var bars = revision.Members.Where(value => value.Bar is not null).Select(value => value.Bar!).ToArray();
        var dates = revision.Members.Select(value => value.Bar?.SessionDate ?? value.CorporateAction?.EffectiveDate ?? value.QualityEvidence?.TradingDate).Where(value => value is not null).Select(value => value!.Value).ToArray();
        return new HistoricalDatasetManifest(revision.Metadata.DatasetId, revision.Id, revision.ParentRevisionId,
            revision.Metadata.SchemaVersion, revision.Members.Select(value => value.Bar?.InstrumentId ?? value.CorporateAction?.InstrumentId ?? value.QualityEvidence!.InstrumentId).Distinct().ToArray(),
            revision.Metadata.Provider, revision.Metadata.ProviderDataset, "XSTO", new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 30),
            dates.Min(), dates.Max(), MarketDataInterval.Daily, PriceAdjustment.Raw, new AcquisitionRequestId($"request:{revision.Id.Value}"),
            revision.Metadata.RetrievedAtUtc, Policy, new EvidenceReference("fixture:policy"), bars.Length,
            revision.Members.Count(value => value.CorporateAction is not null), revision.Members.Count(value => value.QualityEvidence is not null),
            0, revision.Corrections.Length, HistoricalDatasetIntegrity.Compute(revision), HistoricalStorageFormat.InMemoryFixture,
            new VersionReference("in-memory-v1"), RetentionClassification.SubscriptionOnly, DeletionRequirement.DeleteAtSubscriptionEnd,
            null, [new EvidenceReference("fixture:acquisition"), new EvidenceReference("fixture:policy")], status);
    }

    private static ImmutableDatasetRevision BaseRevision(bool includeActionAndGap = false) => RootRevision(10m, includeActionAndGap: includeActionAndGap);
    private static ImmutableDatasetRevision RootRevision(decimal close, string id = "revision-001", MarketDataProvider? provider = null,
        bool includeActionAndGap = false)
    {
        var actualProvider = provider ?? Provider; var revisionId = new DatasetRevisionId(id);
        var metadata = Metadata(revisionId, null, actualProvider, Available.AddMinutes(1));
        var members = new List<DatasetRevisionMember> { BarMember("bar-v1", revisionId, close, actualProvider, Available) };
        if (includeActionAndGap)
        {
            var action = new CanonicalCorporateAction(new CorporateActionId("split-1"), CorporateActionType.StockSplit, Instrument,
                new DateOnly(2024, 6, 4), new DateOnly(2024, 6, 4), null, new ExactRatio(2, 1), Provenance(revisionId, actualProvider, Available));
            members.Add(DatasetRevisionMember.ForCorporateAction(new DatasetMemberId("split-member"), action, Available));
            var gap = new ReplayQualityEvidence(Instrument, new DateOnly(2024, 6, 5), MarketDataFindingCode.ProviderGap,
                ObservationGapClassification.ProviderGap, Available, revisionId, new EvidenceReference("fixture:gap"));
            members.Add(DatasetRevisionMember.ForQualityEvidence(new DatasetMemberId("gap-member"), gap, Policy));
        }
        return ImmutableDatasetRevisionAssembler.Assemble(new DatasetRevisionAssemblyRequest(metadata, null, members, []));
    }

    private static ImmutableDatasetRevision CorrectedRevision(ImmutableDatasetRevision parent)
    {
        var time = Available.AddDays(2); var id = new DatasetRevisionId("revision-002");
        var replacement = BarMember("bar-v2", id, 10.5m, Provider, time);
        var correction = new DatasetCorrection(new DatasetCorrectionId("correction-1"), new DatasetMemberId("bar-v1"),
            new DatasetMemberId("bar-v2"), time, "provider.correction.synthetic", new EvidenceReference("fixture:correction"));
        return ImmutableDatasetRevisionAssembler.Assemble(new DatasetRevisionAssemblyRequest(Metadata(id, parent.Id, Provider, time), parent, [replacement], [correction]));
    }

    private static DatasetRevision Metadata(DatasetRevisionId id, DatasetRevisionId? parent, MarketDataProvider provider, DateTimeOffset created) =>
        new(id, new DatasetId($"dataset:{provider.Value}"), parent, provider, Product, new VersionReference("adapter-v1"),
            new VersionReference("schema-v1"), new DatasetChecksum($"metadata:{id.Value}"), created, created.AddMinutes(-1), DatasetRevisionStatus.Complete);
    private static DatasetRevisionMember BarMember(string memberId, DatasetRevisionId revision, decimal close, MarketDataProvider provider, DateTimeOffset available) =>
        DatasetRevisionMember.ForBar(new DatasetMemberId(memberId), new CanonicalMarketBar(Instrument, MarketDataInterval.Daily,
            new DateOnly(2024, 6, 3), new Price(close, Sek), new Price(close + 1m, Sek), new Price(close - 1m, Sek),
            new Price(close, Sek), 1000m, PriceAdjustment.Raw, null, Provenance(revision, provider, available)), available);
    private static MarketDataProvenance Provenance(DatasetRevisionId revision, MarketDataProvider provider, DateTimeOffset available) =>
        new(provider, Product, available, Utc(2024, 6, 3, 18), Instrument, Xsto, revision, Policy,
            MarketDataClassification.Raw, [], new VersionReference("adapter-v1"), new VersionReference("schema-v1"), MarketDataQualityStatus.Valid);
    private static MarketSession Trading(DateOnly date) => MarketSession.Trading(new MarketSessionId($"XSTO-{date:yyyyMMdd}"), Xsto,
        "XSTO", date, new TimeOnly(9), new TimeOnly(17, 30), "Europe/Stockholm", MarketSessionKind.Regular, new EvidenceReference("fixture:calendar"));
    private static InstrumentMappingCatalog Mappings() => new([new CanonicalInstrument(Instrument, InstrumentType.Equity,
        "Synthetic Example AB", Sek, Xsto, "XSTO", InstrumentLifecycle.Active, new DateOnly(2020, 1, 1))],
        [new ProviderInstrumentMapping(Instrument, Provider, Product, "TEST-B", Xsto, "XSTO", new DateOnly(2020, 1, 1), null, new EvidenceReference("fixture:mapping"))]);
    private static DateTimeOffset Utc(int year, int month, int day, int hour) => new(year, month, day, hour, 0, 0, TimeSpan.Zero);
}
