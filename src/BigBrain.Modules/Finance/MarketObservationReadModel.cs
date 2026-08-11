namespace BigBrain.Modules.Finance;

public enum FinanceOperatingMode { Unknown = 0, Research }
public enum MarketDataProviderState { Unknown = 0, NoneAuthorized, Candidate, Authorized, Unavailable }
public enum EntitlementState { Unknown = 0, PendingWrittenConfirmation, Authorized, Denied, Expired }
public enum ObservationDataKind { Unknown = 0, None, SyntheticFixture, Real }
public enum ObservationFreshnessState { Unknown = 0, Current, Delayed, Stale, Unavailable }
public enum ObservationSessionState { Unknown = 0, PreMarket, Open, Closed, Gap, Outage }
public enum ObservationQualityState { Unknown = 0, Good, Warning, Gap, Error }
public enum HistoricalPersistenceState { Unknown = 0, NotConfigured, FixtureMemory, Durable }

public sealed record FinanceSafetyState(
    FinanceOperatingMode Mode,
    bool LiveTradingEnabled,
    bool PaperTradingEnabled,
    bool BrokerConnected,
    bool IngestionAllowed,
    bool RealProviderStorageAllowed);

public sealed record FinanceProviderSummary(
    MarketDataProviderState State,
    string DisplayName,
    EntitlementState Entitlement,
    string EntitlementGate,
    string Reason,
    string EvidenceClass = "unknown");

public enum FinanceRetentionState { Unknown = 0, Active, DeletionRequired, ExpiredBlocked, DeletionComplete }

public sealed record FinanceRetentionSummary(
    FinanceRetentionState State,
    DateTimeOffset? EntitlementEndsAtUtc,
    DateTimeOffset? DeletionDeadlineUtc,
    int CoveredObservationCount,
    int CoveredRevisionCount,
    int CoveredPayloadCount,
    string DeletionScope,
    string? LastReceiptId,
    int CoveredFeatureValueCount = 0,
    int CoveredFeatureRevisionCount = 0);

public sealed record FinanceInstrumentObservation(
    string InstrumentId,
    string Symbol,
    string DisplayName,
    decimal? Price,
    string? Currency,
    decimal? DailyChangePercent,
    DateTimeOffset? ObservedAtUtc,
    ObservationFreshnessState Freshness,
    ObservationSessionState Session,
    ObservationQualityState Quality,
    ObservationDataKind DataKind,
    IReadOnlyList<FinanceChartPoint> History);

public sealed record FinanceChartPoint(DateTimeOffset ObservedAtUtc, decimal? Value, bool BeginsAfterGap);

public sealed record FinanceHistoricalMemorySummary(
    long ObservationCount,
    string? ActiveRevisionId,
    string? ParentRevisionId,
    DateOnly? CoverageFrom,
    DateOnly? CoverageTo,
    DateTimeOffset? LastAcquiredAtUtc,
    int GapCount,
    int CorrectionCount,
    HistoricalPersistenceState Persistence,
    string Provider,
    string Product,
    string Policy,
    string Provenance);

public sealed record FinanceObservationSnapshot(
    DateTimeOffset GeneratedAtUtc,
    FinanceSafetyState Safety,
    FinanceProviderSummary Provider,
    DateTimeOffset? LatestMarketDataUpdateUtc,
    ObservationDataKind DataKind,
    IReadOnlyList<FinanceInstrumentObservation> Watchlist,
    FinanceHistoricalMemorySummary HistoricalMemory,
    FinanceRetentionSummary? Retention = null);

public interface IFinanceObservationReader
{
    FinanceObservationSnapshot GetSnapshot();
}

public sealed class SafeDefaultFinanceObservationReader : IFinanceObservationReader
{
    private static readonly (string Id, string Symbol, string Name)[] ResearchWatchlist =
    [
        ("US:ARCX:SPY", "SPY", "SPDR S&P 500 ETF Trust"),
        ("US:XNAS:QQQ", "QQQ", "Invesco QQQ Trust"),
        ("US:ARCX:IWM", "IWM", "iShares Russell 2000 ETF"),
        ("US:XNAS:AAPL", "AAPL", "Apple"),
        ("US:XNAS:MSFT", "MSFT", "Microsoft"),
        ("US:XNYS:JPM", "JPM", "JPMorgan Chase"),
        ("US:XNYS:XOM", "XOM", "Exxon Mobil"),
        ("US:XNYS:JNJ", "JNJ", "Johnson & Johnson")
    ];

    public FinanceObservationSnapshot GetSnapshot() => new(
        DateTimeOffset.UtcNow,
        new(FinanceOperatingMode.Research, false, false, false, false, false),
        new(MarketDataProviderState.NoneAuthorized, "Ingen aktiv provider", EntitlementState.PendingWrittenConfirmation,
            "ZERO-COST ENTITLEMENT GATE",
            "Ingen 0-SEK-källa har komplett verifierad rätt för automation, lokal retention och research/backtesting."),
        null,
        ObservationDataKind.None,
        ResearchWatchlist.Select(item => new FinanceInstrumentObservation(
            item.Id, item.Symbol, item.Name, null, null, null, null,
            ObservationFreshnessState.Unavailable, ObservationSessionState.Unknown,
            ObservationQualityState.Unknown, ObservationDataKind.None, [])).ToArray(),
        new FinanceHistoricalMemorySummary(0, null, null, null, null, null, 0, 0,
            HistoricalPersistenceState.NotConfigured, "none", "none", "zero-cost-provider-unresolved", "none"));
}
