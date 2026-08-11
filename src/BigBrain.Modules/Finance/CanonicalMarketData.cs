using System.Collections.Immutable;

namespace BigBrain.Modules.Finance;

public enum InstrumentType { Unknown = 0, Equity, Etf }
public enum InstrumentLifecycle { Unknown = 0, Active, Inactive, Delisted }
public enum MarketDataInterval { Unknown = 0, Daily }
public enum PriceAdjustment { Unknown = 0, Raw, Adjusted }
public enum CorporateActionType { Unknown = 0, CashDividend, StockSplit }
public enum MarketDataFindingCode
{
    Unknown = 0,
    MissingMapping,
    AmbiguousMapping,
    DuplicateObservation,
    ConflictingObservation,
    InvalidPriceRange,
    NegativeVolume,
    MissingObservation,
    ProviderGap,
    UnsupportedInterval,
    CurrencyMismatch
}

public readonly record struct CorporateActionId
{
    public CorporateActionId(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct ExactRatio
{
    public ExactRatio(long numerator, long denominator)
    {
        if (numerator <= 0) throw new ArgumentOutOfRangeException(nameof(numerator), "Ratio numerator must be positive.");
        if (denominator <= 0) throw new ArgumentOutOfRangeException(nameof(denominator), "Ratio denominator must be positive.");
        var divisor = GreatestCommonDivisor(numerator, denominator);
        Numerator = numerator / divisor;
        Denominator = denominator / divisor;
    }

    public long Numerator { get; }
    public long Denominator { get; }
    public decimal AsDecimal() => (decimal)Numerator / Denominator;

    private static long GreatestCommonDivisor(long left, long right)
    {
        while (right != 0) (left, right) = (right, left % right);
        return left;
    }
}

public sealed record CanonicalInstrument
{
    public CanonicalInstrument(
        InstrumentId id,
        InstrumentType type,
        string displayName,
        Currency currency,
        MarketVenue venue,
        string mic,
        InstrumentLifecycle lifecycle,
        DateOnly validFrom,
        DateOnly? validTo = null)
    {
        RequiredText.Require(id.Value, nameof(id));
        if (type == InstrumentType.Unknown || !Enum.IsDefined(type)) throw new ArgumentException("Instrument type is required.", nameof(type));
        DisplayName = RequiredText.Normalize(displayName, nameof(displayName));
        if (currency.Code is null) throw new ArgumentException("Currency is required.", nameof(currency));
        ArgumentNullException.ThrowIfNull(venue);
        Mic = RequiredText.Normalize(mic, nameof(mic)).ToUpperInvariant();
        if (lifecycle == InstrumentLifecycle.Unknown || !Enum.IsDefined(lifecycle)) throw new ArgumentException("Instrument lifecycle is required.", nameof(lifecycle));
        if (validTo < validFrom) throw new ArgumentException("Instrument validity cannot end before it starts.", nameof(validTo));
        Id = id; Type = type; Currency = currency; Venue = venue; Lifecycle = lifecycle; ValidFrom = validFrom; ValidTo = validTo;
    }

    public InstrumentId Id { get; }
    public InstrumentType Type { get; }
    public string DisplayName { get; }
    public Currency Currency { get; }
    public MarketVenue Venue { get; }
    public string Mic { get; }
    public InstrumentLifecycle Lifecycle { get; }
    public DateOnly ValidFrom { get; }
    public DateOnly? ValidTo { get; }
}

public sealed record ProviderInstrumentMapping
{
    public ProviderInstrumentMapping(
        InstrumentId instrumentId,
        MarketDataProvider provider,
        ProviderDataset providerDataset,
        string providerReference,
        MarketVenue venue,
        string mic,
        DateOnly validFrom,
        DateOnly? validTo,
        EvidenceReference evidence)
    {
        RequiredText.Require(instrumentId.Value, nameof(instrumentId));
        RequiredText.Require(provider.Value, nameof(provider));
        RequiredText.Require(providerDataset.Value, nameof(providerDataset));
        ProviderReference = RequiredText.Normalize(providerReference, nameof(providerReference)).ToUpperInvariant();
        ArgumentNullException.ThrowIfNull(venue);
        Mic = RequiredText.Normalize(mic, nameof(mic)).ToUpperInvariant();
        if (validTo < validFrom) throw new ArgumentException("Mapping validity cannot end before it starts.", nameof(validTo));
        RequiredText.Require(evidence.Value, nameof(evidence));
        InstrumentId = instrumentId; Provider = provider; ProviderDataset = providerDataset; Venue = venue;
        ValidFrom = validFrom; ValidTo = validTo; Evidence = evidence;
    }

    public InstrumentId InstrumentId { get; }
    public MarketDataProvider Provider { get; }
    public ProviderDataset ProviderDataset { get; }
    public string ProviderReference { get; }
    public MarketVenue Venue { get; }
    public string Mic { get; }
    public DateOnly ValidFrom { get; }
    public DateOnly? ValidTo { get; }
    public EvidenceReference Evidence { get; }
    public bool IsValidOn(DateOnly date) => date >= ValidFrom && (ValidTo is null || date <= ValidTo);
}

public sealed class InstrumentMappingCatalog
{
    private readonly ImmutableDictionary<InstrumentId, CanonicalInstrument> _instruments;
    private readonly ImmutableArray<ProviderInstrumentMapping> _mappings;

    public InstrumentMappingCatalog(IEnumerable<CanonicalInstrument> instruments, IEnumerable<ProviderInstrumentMapping> mappings)
    {
        ArgumentNullException.ThrowIfNull(instruments); ArgumentNullException.ThrowIfNull(mappings);
        var instrumentArray = instruments.ToImmutableArray();
        if (instrumentArray.Select(value => value.Id).Distinct().Count() != instrumentArray.Length)
            throw new ArgumentException("Canonical instrument IDs must be unique.", nameof(instruments));
        _instruments = instrumentArray.ToImmutableDictionary(value => value.Id);
        _mappings = mappings.ToImmutableArray();
        if (_mappings.Any(mapping => !_instruments.ContainsKey(mapping.InstrumentId)))
            throw new ArgumentException("Every mapping must reference a canonical instrument.", nameof(mappings));

        foreach (var group in _mappings.GroupBy(mapping => new
                 { mapping.InstrumentId, mapping.Provider, mapping.ProviderDataset, mapping.Mic }))
        {
            var ordered = group.OrderBy(mapping => mapping.ValidFrom).ToArray();
            for (var index = 1; index < ordered.Length; index++)
                if (ordered[index - 1].ValidTo is null || ordered[index].ValidFrom <= ordered[index - 1].ValidTo)
                    throw new ArgumentException("Provider symbol mappings cannot overlap for an instrument/product/venue.", nameof(mappings));
        }
    }

    public (CanonicalInstrument Instrument, ProviderInstrumentMapping Mapping) Resolve(
        MarketDataProvider provider, ProviderDataset dataset, string providerReference, string mic, DateOnly marketDate)
    {
        var normalizedReference = RequiredText.Normalize(providerReference, nameof(providerReference)).ToUpperInvariant();
        var normalizedMic = RequiredText.Normalize(mic, nameof(mic)).ToUpperInvariant();
        var matches = _mappings.Where(mapping => mapping.Provider == provider && mapping.ProviderDataset == dataset &&
            mapping.ProviderReference == normalizedReference && mapping.Mic == normalizedMic && mapping.IsValidOn(marketDate)).ToArray();
        if (matches.Length == 0) throw new MarketDataNormalizationException(MarketDataFindingCode.MissingMapping, "No provider mapping is valid for the supplied date and venue.");
        if (matches.Length != 1) throw new MarketDataNormalizationException(MarketDataFindingCode.AmbiguousMapping, "More than one provider mapping is valid for the supplied date and venue.");
        return (_instruments[matches[0].InstrumentId], matches[0]);
    }

    public ProviderInstrumentMapping ResolveProviderReference(
        InstrumentId instrumentId, MarketDataProvider provider, ProviderDataset dataset, string mic, DateOnly marketDate)
    {
        var normalizedMic = RequiredText.Normalize(mic, nameof(mic)).ToUpperInvariant();
        var matches = _mappings.Where(mapping => mapping.InstrumentId == instrumentId && mapping.Provider == provider &&
            mapping.ProviderDataset == dataset && mapping.Mic == normalizedMic && mapping.IsValidOn(marketDate)).ToArray();
        if (matches.Length == 0) throw new MarketDataNormalizationException(MarketDataFindingCode.MissingMapping, "No provider reference is valid for the canonical instrument, date and venue.");
        if (matches.Length != 1) throw new MarketDataNormalizationException(MarketDataFindingCode.AmbiguousMapping, "More than one provider reference is valid for the canonical instrument, date and venue.");
        return matches[0];
    }
}

public sealed record SyntheticRawDailyBar(
    MarketDataProvider Provider, ProviderDataset ProviderDataset, string ProviderReference, string Mic,
    DateOnly SessionDate, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume,
    Currency Currency, PriceAdjustment Adjustment, string? AdjustmentBasis, DateTimeOffset SourceTimestampUtc,
    DateTimeOffset RetrievedAtUtc, DatasetRevisionId DatasetRevisionId, MarketDataPolicyReference Policy,
    VersionReference AdapterVersion, VersionReference SchemaVersion);

public sealed record CanonicalMarketBar(
    InstrumentId InstrumentId, MarketDataInterval Interval, DateOnly SessionDate, Price Open, Price High,
    Price Low, Price Close, decimal Volume, PriceAdjustment Adjustment, string? AdjustmentBasis,
    MarketDataProvenance Provenance)
{
    public string Identity => $"{InstrumentId.Value}|{Interval}|{SessionDate:yyyy-MM-dd}";
}

public sealed record MarketDataQualityFinding(MarketDataFindingCode Code, string ObservationIdentity, string Reason);
public sealed record MarketBarNormalizationBatch(ImmutableArray<CanonicalMarketBar> Accepted, ImmutableArray<MarketDataQualityFinding> Findings);

public sealed class MarketDataNormalizationException : ArgumentException
{
    public MarketDataNormalizationException(MarketDataFindingCode findingCode, string message) : base(message) => FindingCode = findingCode;
    public MarketDataFindingCode FindingCode { get; }
}

public sealed record SyntheticRawCorporateAction(
    CorporateActionId Id, CorporateActionType Type, MarketDataProvider Provider, ProviderDataset ProviderDataset,
    string ProviderReference, string Mic, DateOnly ExDate, DateOnly EffectiveDate, Money? CashAmount,
    ExactRatio? SplitRatio, DateTimeOffset SourceTimestampUtc, DateTimeOffset RetrievedAtUtc,
    DatasetRevisionId DatasetRevisionId, MarketDataPolicyReference Policy, VersionReference AdapterVersion,
    VersionReference SchemaVersion);

public sealed record CanonicalCorporateAction(
    CorporateActionId Id, CorporateActionType Type, InstrumentId InstrumentId, DateOnly ExDate,
    DateOnly EffectiveDate, Money? CashAmount, ExactRatio? SplitRatio, MarketDataProvenance Provenance);

public sealed class SyntheticMarketDataNormalizer
{
    private readonly InstrumentMappingCatalog _catalog;
    public SyntheticMarketDataNormalizer(InstrumentMappingCatalog catalog) => _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

    public CanonicalMarketBar Normalize(SyntheticRawDailyBar input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Adjustment == PriceAdjustment.Unknown || !Enum.IsDefined(input.Adjustment))
            throw new MarketDataNormalizationException(MarketDataFindingCode.UnsupportedInterval, "Adjustment classification is required.");
        if (input.Adjustment == PriceAdjustment.Adjusted && string.IsNullOrWhiteSpace(input.AdjustmentBasis))
            throw new ArgumentException("Adjusted observations require an adjustment basis.", nameof(input));
        if (input.Volume < 0) throw new MarketDataNormalizationException(MarketDataFindingCode.NegativeVolume, "Volume cannot be negative.");
        if (input.Open <= 0 || input.High <= 0 || input.Low <= 0 || input.Close <= 0 ||
            input.High < input.Open || input.High < input.Close || input.High < input.Low ||
            input.Low > input.Open || input.Low > input.Close)
            throw new MarketDataNormalizationException(MarketDataFindingCode.InvalidPriceRange, "OHLC price range is invalid.");
        var resolved = _catalog.Resolve(input.Provider, input.ProviderDataset, input.ProviderReference, input.Mic, input.SessionDate);
        if (resolved.Instrument.Currency != input.Currency)
            throw new MarketDataNormalizationException(MarketDataFindingCode.CurrencyMismatch, "Observation currency differs from the canonical instrument.");
        var provenance = CreateProvenance(input.Provider, input.ProviderDataset, input.RetrievedAtUtc, input.SourceTimestampUtc,
            resolved.Instrument, input.DatasetRevisionId, input.Policy, input.AdapterVersion, input.SchemaVersion);
        return new CanonicalMarketBar(resolved.Instrument.Id, MarketDataInterval.Daily, input.SessionDate,
            new Price(input.Open, input.Currency), new Price(input.High, input.Currency), new Price(input.Low, input.Currency),
            new Price(input.Close, input.Currency), input.Volume, input.Adjustment, input.AdjustmentBasis, provenance);
    }

    public CanonicalCorporateAction Normalize(SyntheticRawCorporateAction input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var resolved = _catalog.Resolve(input.Provider, input.ProviderDataset, input.ProviderReference, input.Mic, input.ExDate);
        if (input.EffectiveDate < input.ExDate) throw new ArgumentException("Effective date cannot precede ex-date.", nameof(input));
        if (input.Type == CorporateActionType.CashDividend)
        {
            if (input.CashAmount is not { } cash || cash.Amount <= 0) throw new ArgumentException("Cash dividend must be positive.", nameof(input));
            if (cash.Currency != resolved.Instrument.Currency) throw new MarketDataNormalizationException(MarketDataFindingCode.CurrencyMismatch, "Dividend currency differs from the canonical instrument.");
            if (input.SplitRatio is not null) throw new ArgumentException("Cash dividend cannot contain a split ratio.", nameof(input));
        }
        else if (input.Type == CorporateActionType.StockSplit)
        {
            if (input.SplitRatio is null || input.CashAmount is not null) throw new ArgumentException("Stock split requires only an exact split ratio.", nameof(input));
        }
        else throw new ArgumentException("Supported corporate action type is required.", nameof(input));
        var provenance = CreateProvenance(input.Provider, input.ProviderDataset, input.RetrievedAtUtc, input.SourceTimestampUtc,
            resolved.Instrument, input.DatasetRevisionId, input.Policy, input.AdapterVersion, input.SchemaVersion);
        return new CanonicalCorporateAction(input.Id, input.Type, resolved.Instrument.Id, input.ExDate, input.EffectiveDate,
            input.CashAmount, input.SplitRatio, provenance);
    }

    public MarketBarNormalizationBatch NormalizeBatch(IEnumerable<SyntheticRawDailyBar> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        var accepted = ImmutableArray.CreateBuilder<CanonicalMarketBar>();
        var findings = ImmutableArray.CreateBuilder<MarketDataQualityFinding>();
        var byIdentity = new Dictionary<string, CanonicalMarketBar>(StringComparer.Ordinal);
        foreach (var input in inputs)
        {
            var bar = Normalize(input);
            if (!byIdentity.TryGetValue(bar.Identity, out var existing)) { byIdentity.Add(bar.Identity, bar); accepted.Add(bar); continue; }
            var exact = existing == bar;
            findings.Add(new MarketDataQualityFinding(exact ? MarketDataFindingCode.DuplicateObservation : MarketDataFindingCode.ConflictingObservation,
                bar.Identity, exact ? "Exact duplicate was ignored." : "Conflicting duplicate was rejected; the first observation was retained."));
        }
        return new MarketBarNormalizationBatch(accepted.ToImmutable(), findings.ToImmutable());
    }

    private static MarketDataProvenance CreateProvenance(MarketDataProvider provider, ProviderDataset dataset,
        DateTimeOffset retrievedAtUtc, DateTimeOffset sourceTimestampUtc, CanonicalInstrument instrument,
        DatasetRevisionId revision, MarketDataPolicyReference policy, VersionReference adapter, VersionReference schema) =>
        new(provider, dataset, retrievedAtUtc, sourceTimestampUtc, instrument.Id, instrument.Venue, revision, policy,
            MarketDataClassification.Raw, [], adapter, schema, MarketDataQualityStatus.Valid);
}
