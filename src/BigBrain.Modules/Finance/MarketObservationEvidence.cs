using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace BigBrain.Modules.Finance;

public enum MarketObservationOrigin { Unknown, AcquiredProviderSnapshot, DeterministicFixture, HistoricalImport, AcquiredProviderDaily }

// Snapshot OHLCV is not a finalized bar, a trading signal or permission to run a strategy.
// Provider event time is not publication time. Unknown publication remains null.
public sealed record ProviderObservation(
    string ProviderReference, string Mic, Currency Currency, Timeframe Interval, DateTimeOffset EventTimeUtc,
    DateTimeOffset? ProviderAvailableAtUtc, decimal Open, decimal High, decimal Low, decimal Close,
    decimal Volume, string PayloadSha256, DailyObservationSemantics? Daily = null);

public interface IMarketObservationSource
{
    string ContractVersion => MarketObservationReceipt.Contract;
    MarketDataProvider Provider { get; }
    ProviderDataset Dataset { get; }
    string AdapterVersion { get; }
    MarketObservationOrigin Origin { get; }
    Task<ProviderObservation> ReadAsync(ProviderInstrumentMapping mapping, CancellationToken cancellationToken);
}

public sealed record MarketObservationReceipt(
    string Version, string Id, string LogicalIdentity, string ContentChecksum, string Checksum,
    CanonicalInstrument Instrument, ProviderInstrumentMapping Mapping, MarketDataProvider Provider, ProviderDataset Dataset,
    string AdapterVersion, MarketObservationOrigin Origin, ProviderObservation Value,
    MarketDataPolicyReference Policy, string PolicyEvidence, DateTimeOffset AcquiredAtUtc,
    DateTimeOffset IngestedAtUtc, string? CorrectsId)
{
    public const string Contract = "finance-market-observation-v1";
    public LiveObservationGranularity Granularity => Version == DailyMarketEvidence.Contract ? LiveObservationGranularity.Daily : LiveObservationGranularity.Snapshot;
    public PriceAdjustment Adjustment { get; } = PriceAdjustment.Raw;
    public DateTimeOffset KnowledgeTimeUtc => IngestedAtUtc;
}

public sealed record MarketKnowledgeProjection(string Version, DateTimeOffset CutoffUtc,
    ImmutableArray<MarketObservationReceipt> Observations, string Checksum);

public static class MarketObservationIntegrity
{
    public static void Validate(ProviderObservation value, CanonicalInstrument instrument,
        ProviderInstrumentMapping mapping, DateTimeOffset acquired)
    {
        ArgumentNullException.ThrowIfNull(value);
        FinanceTime.RequireUtc(acquired, nameof(acquired)); FinanceTime.RequireUtc(value.EventTimeUtc, nameof(value));
        var date = value.Daily?.SourceDate ?? DateOnly.FromDateTime(value.EventTimeUtc.UtcDateTime);
        if (value.Daily is not null) DailyMarketEvidence.Validate(value, mapping, acquired);
        if (value.Daily is null && (value.Interval != Timeframe.OneMinute || value.EventTimeUtc.Ticks % TimeSpan.TicksPerMinute != 0) || value.EventTimeUtc == default || value.EventTimeUtc > acquired ||
            value.ProviderAvailableAtUtc is { } available && (available.Offset != TimeSpan.Zero ||
                available < value.EventTimeUtc || available > acquired))
            throw new InvalidDataException("Observation temporal order is invalid.");
        if (instrument.Type is not (InstrumentType.Equity or InstrumentType.Etf) ||
            instrument.Lifecycle != InstrumentLifecycle.Active || date < instrument.ValidFrom || date > instrument.ValidTo ||
            !mapping.IsValidOn(date) || instrument.Id != mapping.InstrumentId || instrument.Mic != mapping.Mic ||
            instrument.Venue != mapping.Venue || value.ProviderReference != mapping.ProviderReference ||
            value.Mic != mapping.Mic || value.Currency != instrument.Currency)
            throw new InvalidDataException("Observation instrument mapping is not eligible.");
        new Candle(value.EventTimeUtc, value.Interval, new(value.Open, value.Currency), new(value.High, value.Currency),
            new(value.Low, value.Currency), new(value.Close, value.Currency), value.Volume).Validate();
        if (value.Open <= 0 || value.High <= 0 || value.Low <= 0 || value.Close <= 0 ||
            value.Open > 1_000_000_000m || value.High > 1_000_000_000m || value.Volume > 1_000_000_000_000m)
            throw new InvalidDataException("Observation numeric bounds are invalid.");
        Token(instrument.Id.Value); Token(instrument.Mic); Token(mapping.ProviderReference);
        Token(mapping.Provider.Value); Token(mapping.ProviderDataset.Value); Token(mapping.Evidence.Value);
        if (instrument.DisplayName.Length > 128 || instrument.Venue.Name.Length > 128) throw new InvalidDataException("Observation metadata exceeds bounds.");
        if (!Digest(value.PayloadSha256)) throw new InvalidDataException("Observation provenance checksum is invalid.");
    }

    public static string LogicalIdentity(CanonicalInstrument instrument, MarketDataProvider provider,
        ProviderDataset dataset, MarketObservationOrigin origin, ProviderObservation value) => Hash(new[]
        { value.Daily is null ? MarketObservationReceipt.Contract : DailyMarketEvidence.Contract, instrument.Id.Value, instrument.Mic, instrument.Currency.Code,
            provider.Value, dataset.Value, origin.ToString(), value.Daily is null ? "Snapshot" : "Daily", "Raw", value.Interval.ToString(), Utc(value.EventTimeUtc) });

    public static string ContentChecksum(ProviderObservation value) => Hash(new[]
    {
        value.ProviderReference, value.Mic, value.Currency.Code, value.Interval.ToString(), Utc(value.EventTimeUtc),
        value.ProviderAvailableAtUtc is { } available ? Utc(available) : "PUBLICATION_UNKNOWN",
        Number(value.Open), Number(value.High), Number(value.Low), Number(value.Close), Number(value.Volume), value.PayloadSha256
    }.Concat(DailyFields(value)));

    public static string Checksum(MarketObservationReceipt receipt) => Hash(new[]
    {
        receipt.Version, receipt.Id, receipt.LogicalIdentity, receipt.ContentChecksum, receipt.Instrument.Id.Value,
        receipt.Instrument.Mic, receipt.Instrument.Venue.Code, receipt.Instrument.Venue.Name, receipt.Instrument.Currency.Code,
        receipt.Instrument.Type.ToString(), receipt.Instrument.DisplayName, receipt.Instrument.Lifecycle.ToString(),
        receipt.Instrument.ValidFrom.ToString("O", CultureInfo.InvariantCulture),
        receipt.Instrument.ValidTo?.ToString("O", CultureInfo.InvariantCulture) ?? "OPEN",
        receipt.Mapping.ProviderReference, receipt.Mapping.Evidence.Value,
        receipt.Mapping.ValidFrom.ToString("O", CultureInfo.InvariantCulture), receipt.Mapping.ValidTo?.ToString("O", CultureInfo.InvariantCulture) ?? "OPEN",
        receipt.Provider.Value, receipt.Dataset.Value, receipt.AdapterVersion, receipt.Origin.ToString(),
        receipt.Value.PayloadSha256, receipt.Policy.Id.Value, receipt.Policy.Version.Value, receipt.PolicyEvidence,
        Utc(receipt.AcquiredAtUtc), Utc(receipt.IngestedAtUtc), receipt.CorrectsId ?? "NONE"
    }.Concat(DailyFields(receipt.Value)));

    private static string[] DailyFields(ProviderObservation value) => value.Daily is { } d
        ? new[] { d.Version, d.SourceDate.ToString("O", CultureInfo.InvariantCulture), d.TimeZone, d.SourceContract, d.SymbolPolicy } : [];

    public static string Hash(IEnumerable<string> values) => "sha256:" + Convert.ToHexStringLower(
        SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(values.ToArray())));
    public static string Utc(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);
    private static string Number(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
    public static bool Digest(string? value) => value is { Length: 71 } && value.StartsWith("sha256:", StringComparison.Ordinal) &&
        value.Skip(7).All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    public static void Token(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128 ||
            value.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is ':' or '-' or '_' or '.' or '@')))
            throw new InvalidDataException("Observation identifier is invalid.");
    }
}
