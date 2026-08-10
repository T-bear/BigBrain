namespace BigBrain.Modules.Finance;

public enum Timeframe
{
    OneMinute,
    FiveMinutes,
    OneHour,
    OneDay
}

public sealed record MarketVenue
{
    public MarketVenue(string code, string name)
    {
        Code = NormalizeRequired(code, nameof(code));
        Name = NormalizeRequired(name, nameof(name));
    }

    public string Code { get; }
    public string Name { get; }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim().ToUpperInvariant();
    }
}

public sealed record Instrument(InstrumentId Id, string DisplayName, MarketVenue Venue, Currency Currency);

public sealed record Quote(Price Bid, Price Ask)
{
    public Quote Validate()
    {
        if (Bid.Currency != Ask.Currency || Ask.Value < Bid.Value)
        {
            throw new ArgumentException("Quote currencies must match and ask cannot be below bid.");
        }

        return this;
    }
}

public sealed record Candle(
    DateTimeOffset OpenedAtUtc,
    Timeframe Timeframe,
    Price Open,
    Price High,
    Price Low,
    Price Close,
    decimal Volume)
{
    public Candle Validate()
    {
        FinanceTime.RequireUtc(OpenedAtUtc, nameof(OpenedAtUtc));
        if (Volume < 0 || High.Value < Low.Value || Open.Value < Low.Value || Open.Value > High.Value ||
            Close.Value < Low.Value || Close.Value > High.Value ||
            new[] { Open.Currency, High.Currency, Low.Currency, Close.Currency }.Distinct().Count() != 1)
        {
            throw new ArgumentException("Candle values are inconsistent.");
        }

        return this;
    }
}

public sealed record MarketDataObservation(
    FinanceId ObservationId,
    Instrument Instrument,
    DateTimeOffset MarketTimestampUtc,
    DateTimeOffset ObservedAtUtc,
    string DataVersion,
    IReadOnlyList<Candle> Candles,
    Quote? Quote)
{
    public MarketDataObservation Validate()
    {
        FinanceTime.RequireUtc(MarketTimestampUtc, nameof(MarketTimestampUtc));
        FinanceTime.RequireUtc(ObservedAtUtc, nameof(ObservedAtUtc));
        ArgumentException.ThrowIfNullOrWhiteSpace(DataVersion);
        if (ObservedAtUtc < MarketTimestampUtc)
        {
            throw new ArgumentException("Observation time cannot precede the market timestamp.");
        }

        foreach (var candle in Candles)
        {
            candle.Validate();
        }

        Quote?.Validate();
        return this;
    }
}

public interface IMarketDataSource
{
    Task<MarketDataObservation> GetObservationAsync(InstrumentId instrumentId, CancellationToken cancellationToken);
}

public static class FinanceTime
{
    public static void RequireUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Finance timestamps must use UTC.", parameterName);
        }
    }
}
