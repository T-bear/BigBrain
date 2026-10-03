namespace BigBrain.Modules.Finance;

// A completed source day is not an assertion that the provider will never revise it.
public sealed record DailyObservationSemantics(string Version, DateOnly SourceDate, string TimeZone,
    string SourceContract, string SymbolPolicy);

public static class DailyMarketEvidence
{
    public const string Contract = "finance-market-observation-daily-v1";
    public const string Eligibility = "alpaca-completed-ny-day-v1";
    public const string SourceContract = "alpaca-historical-stocks-1Day-raw-v2";
    public const string Dataset = "iex-historical-1Day-raw";
    public const string SymbolPolicy = "asof-disabled-effective-mapping";
    private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public static DateOnly SourceDate(DateTimeOffset utc) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, NewYork).DateTime);
    public static DateTimeOffset DayStart(DateOnly day) => new(TimeZoneInfo.ConvertTimeToUtc(
        DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), NewYork));

    public static void Validate(ProviderObservation value, ProviderInstrumentMapping mapping, DateTimeOffset requestStarted)
    {
        var daily = value.Daily ?? throw new InvalidDataException("Daily semantics missing.");
        FinanceTime.RequireUtc(requestStarted, nameof(requestStarted));
        if (daily.Version != Eligibility || daily.SourceContract != SourceContract || daily.TimeZone != "America/New_York" ||
            daily.SymbolPolicy != SymbolPolicy || mapping.Provider.Value != "Alpaca" || mapping.ProviderDataset.Value != Dataset ||
            mapping.Mic is not ("XNAS" or "XNYS" or "ARCX") || value.Currency.Code != "USD" ||
            value.Interval != Timeframe.OneDay || value.EventTimeUtc != DayStart(daily.SourceDate) ||
            value.ProviderAvailableAtUtc is not null || daily.SourceDate >= SourceDate(requestStarted))
            throw new InvalidDataException("Daily source/eligibility mismatch or source day still in progress.");
        // Prior New York date + directly returned historical 1Day bar. No missing-minute inference,
        // market-calendar close heuristic, fabricated publication time or consolidated-market claim.
    }
}

// Explicit trusted application policy, not a provider guarantee or automatic live activation.
public static class AlpacaDailyOwnerDecision
{
    public const string Version = "bb132e-owner-data-use-risk-v1";
    public const string Evidence = "owner:bb132e-alpaca-data-use-risk";
    public static MarketDataEntitlementPolicy Create(DateTimeOffset recordedAtUtc,
        DeletionRequirement currentDeletion = DeletionRequirement.Unknown, bool affectedUseEnabled = true) => new(
        new("bb132e-alpaca-private-research"), new(Version), new("Alpaca"), new(DailyMarketEvidence.Dataset), new(Evidence),
        recordedAtUtc, recordedAtUtc, null,
        new Dictionary<MarketDataUse, EntitlementDecision>
        {
            [MarketDataUse.HistoricalAnalysis] = affectedUseEnabled ? EntitlementDecision.Allowed : EntitlementDecision.Denied,
            [MarketDataUse.LongTermStorage] = affectedUseEnabled ? EntitlementDecision.Allowed : EntitlementDecision.Denied,
            [MarketDataUse.DerivedMetrics] = affectedUseEnabled ? EntitlementDecision.Allowed : EntitlementDecision.Denied
        }, EntitlementDecision.Allowed, EntitlementDecision.Unknown, RetentionClassification.LongTerm,
        currentDeletion, currentDeletion == DeletionRequirement.DeleteByDeadline ? recordedAtUtc : null,
        EntitlementEvidenceClass.OwnerAcceptedPersonalResearch, 0, Version,
        "Owner accepts private research retention risk; provider termination/deletion rights remain uncertain.");

    public static void Require(MarketDataEntitlementPolicy policy, DateTimeOffset now)
    {
        FinanceTime.RequireUtc(now, nameof(now));
        if (policy.Provider.Value != "Alpaca" || policy.ProviderDataset.Value != DailyMarketEvidence.Dataset ||
            policy.EvidenceClass != EntitlementEvidenceClass.OwnerAcceptedPersonalResearch ||
            policy.OwnerAcceptanceVersion != Version || policy.Evidence.Value != Evidence ||
            policy.ReviewedAtUtc > now || policy.ValidFromUtc > now || policy.ValidUntilUtc is { } until && now > until ||
            policy.MonetaryCostSek != 0 || policy.Persistence != EntitlementDecision.Allowed ||
            policy.Retention != RetentionClassification.LongTerm ||
            policy.Deletion is not (DeletionRequirement.Unknown or DeletionRequirement.None) ||
            policy.PostSubscriptionRetention == EntitlementDecision.Denied ||
            new[] { MarketDataUse.HistoricalAnalysis, MarketDataUse.LongTermStorage, MarketDataUse.DerivedMetrics }
                .Any(use => policy.DecisionFor(use) != EntitlementDecision.Allowed))
            throw new InvalidDataException("Daily evidence use is disabled or requires retention reconciliation.");
    }
}
