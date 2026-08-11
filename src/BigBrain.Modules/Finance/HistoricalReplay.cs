using System.Collections.Immutable;
using System.Globalization;

namespace BigBrain.Modules.Finance;

public enum MarketSessionState { Unknown = 0, Trading, Closed }
public enum MarketSessionKind { Unknown = 0, Regular, Exceptional }
public enum ObservationGapClassification
{
    Unknown = 0,
    ExpectedClosure,
    MissingObservation,
    ProviderGap,
    InvalidObservation,
    UnknownSession
}
public enum HistoricalReplayEventType
{
    Unknown = 0,
    SessionOpened,
    DividendEffective,
    SplitEffective,
    QualityFindingObserved,
    ObservationAvailable,
    MissingObservationDetected,
    SessionClosed,
    ExpectedClosure,
    UnknownSession
}

public readonly record struct MarketSessionId
{
    public MarketSessionId(string value) => Value = RequiredText.Normalize(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public sealed record MarketSession
{
    private MarketSession(MarketSessionId id, MarketVenue venue, string mic, DateOnly tradingDate,
        string timeZoneId, MarketSessionState state, MarketSessionKind kind, DateTimeOffset? opensAtUtc,
        DateTimeOffset? closesAtUtc, EvidenceReference evidence)
    {
        Id = id; Venue = venue; Mic = mic; TradingDate = tradingDate; TimeZoneId = timeZoneId;
        State = state; Kind = kind; OpensAtUtc = opensAtUtc; ClosesAtUtc = closesAtUtc; Evidence = evidence;
    }

    public MarketSessionId Id { get; }
    public MarketVenue Venue { get; }
    public string Mic { get; }
    public DateOnly TradingDate { get; }
    public string TimeZoneId { get; }
    public MarketSessionState State { get; }
    public MarketSessionKind Kind { get; }
    public DateTimeOffset? OpensAtUtc { get; }
    public DateTimeOffset? ClosesAtUtc { get; }
    public EvidenceReference Evidence { get; }

    public static MarketSession Trading(MarketSessionId id, MarketVenue venue, string mic, DateOnly date,
        TimeOnly localOpen, TimeOnly localClose, string timeZoneId, MarketSessionKind kind, EvidenceReference evidence)
    {
        ValidateCommon(id, venue, mic, timeZoneId, evidence);
        if (kind == MarketSessionKind.Unknown || !Enum.IsDefined(kind)) throw new ArgumentException("Session kind is required.", nameof(kind));
        var zone = ResolveTimeZone(timeZoneId);
        var openLocal = date.ToDateTime(localOpen, DateTimeKind.Unspecified);
        var closeDate = localClose > localOpen ? date : date.AddDays(1);
        var closeLocal = closeDate.ToDateTime(localClose, DateTimeKind.Unspecified);
        var openUtc = ConvertLocalToUtc(openLocal, zone, nameof(localOpen));
        var closeUtc = ConvertLocalToUtc(closeLocal, zone, nameof(localClose));
        if (closeUtc <= openUtc) throw new ArgumentException("Session close must follow session open.", nameof(localClose));
        return new MarketSession(id, venue, mic.Trim().ToUpperInvariant(), date, zone.Id,
            MarketSessionState.Trading, kind, openUtc, closeUtc, evidence);
    }

    public static MarketSession Closed(MarketSessionId id, MarketVenue venue, string mic, DateOnly date,
        string timeZoneId, MarketSessionKind kind, EvidenceReference evidence)
    {
        ValidateCommon(id, venue, mic, timeZoneId, evidence);
        if (kind == MarketSessionKind.Unknown || !Enum.IsDefined(kind)) throw new ArgumentException("Session kind is required.", nameof(kind));
        var zone = ResolveTimeZone(timeZoneId);
        return new MarketSession(id, venue, mic.Trim().ToUpperInvariant(), date, zone.Id,
            MarketSessionState.Closed, kind, null, null, evidence);
    }

    public static MarketSession Unknown(MarketVenue venue, string mic, DateOnly date, string timeZoneId)
    {
        ArgumentNullException.ThrowIfNull(venue);
        var normalizedMic = RequiredText.Normalize(mic, nameof(mic)).ToUpperInvariant();
        var zone = ResolveTimeZone(timeZoneId);
        return new MarketSession(new MarketSessionId($"unknown:{normalizedMic}:{date:yyyy-MM-dd}"), venue,
            normalizedMic, date, zone.Id, MarketSessionState.Unknown, MarketSessionKind.Unknown, null, null,
            new EvidenceReference("calendar:knowledge-unavailable"));
    }

    private static void ValidateCommon(MarketSessionId id, MarketVenue venue, string mic, string timeZoneId, EvidenceReference evidence)
    {
        RequiredText.Require(id.Value, nameof(id)); ArgumentNullException.ThrowIfNull(venue);
        RequiredText.Require(mic, nameof(mic)); RequiredText.Require(timeZoneId, nameof(timeZoneId));
        RequiredText.Require(evidence.Value, nameof(evidence));
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(RequiredText.Normalize(timeZoneId, nameof(timeZoneId))); }
        catch (TimeZoneNotFoundException error) { throw new ArgumentException("Session timezone is unknown.", nameof(timeZoneId), error); }
        catch (InvalidTimeZoneException error) { throw new ArgumentException("Session timezone is invalid.", nameof(timeZoneId), error); }
    }

    private static DateTimeOffset ConvertLocalToUtc(DateTime local, TimeZoneInfo zone, string parameterName)
    {
        if (zone.IsInvalidTime(local)) throw new ArgumentException("Session local time does not exist because of a timezone transition.", parameterName);
        if (zone.IsAmbiguousTime(local)) throw new ArgumentException("Session local time is ambiguous because of a timezone transition.", parameterName);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
    }
}

public interface IMarketSessionCalendar
{
    MarketSession Resolve(DateOnly tradingDate);
}

public sealed class SyntheticMarketSessionCalendar : IMarketSessionCalendar
{
    private readonly ImmutableDictionary<DateOnly, MarketSession> _sessions;
    private readonly MarketVenue _venue;
    private readonly string _mic;
    private readonly string _timeZoneId;

    public SyntheticMarketSessionCalendar(MarketVenue venue, string mic, string timeZoneId, IEnumerable<MarketSession> sessions)
    {
        _venue = venue ?? throw new ArgumentNullException(nameof(venue));
        _mic = RequiredText.Normalize(mic, nameof(mic)).ToUpperInvariant();
        _timeZoneId = RequiredText.Normalize(timeZoneId, nameof(timeZoneId));
        ArgumentNullException.ThrowIfNull(sessions);
        var values = sessions.ToImmutableArray();
        if (values.Any(session => session.Mic != _mic || session.TimeZoneId != _timeZoneId))
            throw new ArgumentException("Fixture sessions must match the calendar MIC and timezone.", nameof(sessions));
        if (values.Select(session => session.TradingDate).Distinct().Count() != values.Length)
            throw new ArgumentException("A fixture calendar can contain only one state per trading date.", nameof(sessions));
        _sessions = values.ToImmutableDictionary(session => session.TradingDate);
    }

    public MarketSession Resolve(DateOnly tradingDate) => _sessions.GetValueOrDefault(tradingDate)
        ?? MarketSession.Unknown(_venue, _mic, tradingDate, _timeZoneId);
}

public sealed record ReplayQualityEvidence(
    InstrumentId InstrumentId, DateOnly TradingDate, MarketDataFindingCode FindingCode,
    ObservationGapClassification Classification, DateTimeOffset ObservedAtUtc,
    DatasetRevisionId DatasetRevisionId, EvidenceReference Evidence)
{
    public ReplayQualityEvidence Validate()
    {
        RequiredText.Require(InstrumentId.Value, nameof(InstrumentId));
        if (FindingCode == MarketDataFindingCode.Unknown || !Enum.IsDefined(FindingCode)) throw new ArgumentException("Finding code is required.", nameof(FindingCode));
        if (Classification == ObservationGapClassification.Unknown || !Enum.IsDefined(Classification)) throw new ArgumentException("Gap classification is required.", nameof(Classification));
        FinanceTime.RequireUtc(ObservedAtUtc, nameof(ObservedAtUtc));
        RequiredText.Require(DatasetRevisionId.Value, nameof(DatasetRevisionId)); RequiredText.Require(Evidence.Value, nameof(Evidence));
        if (Classification == ObservationGapClassification.ProviderGap && FindingCode != MarketDataFindingCode.ProviderGap)
            throw new ArgumentException("Provider gap classification requires explicit provider-gap evidence.", nameof(FindingCode));
        return this;
    }
}

public sealed record HistoricalReplayRequest(
    DatasetRevisionId DatasetRevisionId, MarketDataProvider Provider, ProviderDataset ProviderDataset,
    string Mic, DateTimeOffset FromUtc, DateTimeOffset UntilUtc, IReadOnlyList<InstrumentId> Instruments,
    IReadOnlyList<CanonicalMarketBar> Bars, IReadOnlyList<CanonicalCorporateAction> CorporateActions,
    IReadOnlyList<ReplayQualityEvidence> QualityEvidence)
{
    public HistoricalReplayRequest Validate()
    {
        RequiredText.Require(DatasetRevisionId.Value, nameof(DatasetRevisionId)); RequiredText.Require(Provider.Value, nameof(Provider));
        RequiredText.Require(ProviderDataset.Value, nameof(ProviderDataset)); RequiredText.Require(Mic, nameof(Mic));
        FinanceTime.RequireUtc(FromUtc, nameof(FromUtc)); FinanceTime.RequireUtc(UntilUtc, nameof(UntilUtc));
        if (UntilUtc < FromUtc) throw new ArgumentException("Replay range cannot end before it starts.", nameof(UntilUtc));
        ArgumentNullException.ThrowIfNull(Instruments); ArgumentNullException.ThrowIfNull(Bars);
        ArgumentNullException.ThrowIfNull(CorporateActions); ArgumentNullException.ThrowIfNull(QualityEvidence);
        if (Instruments.Count == 0 || Instruments.Any(id => string.IsNullOrWhiteSpace(id.Value)) || Instruments.Distinct().Count() != Instruments.Count)
            throw new ArgumentException("Replay instruments must be non-empty and unique.", nameof(Instruments));
        if (Bars.Any(bar => bar.Provenance.DatasetRevisionId != DatasetRevisionId) ||
            CorporateActions.Any(action => action.Provenance.DatasetRevisionId != DatasetRevisionId) ||
            QualityEvidence.Any(finding => finding.DatasetRevisionId != DatasetRevisionId))
            throw new ArgumentException("Historical replay cannot mix dataset revisions.", nameof(DatasetRevisionId));
        if (Bars.Any(bar => bar.Provenance.Provider != Provider || bar.Provenance.ProviderDataset != ProviderDataset) ||
            CorporateActions.Any(action => action.Provenance.Provider != Provider || action.Provenance.ProviderDataset != ProviderDataset))
            throw new ArgumentException("Historical replay evidence must match the configured provider and product.");
        if (Bars.Any(bar => !Instruments.Contains(bar.InstrumentId)) || CorporateActions.Any(action => !Instruments.Contains(action.InstrumentId)) ||
            QualityEvidence.Any(finding => !Instruments.Contains(finding.InstrumentId)))
            throw new ArgumentException("Replay evidence must belong to a configured instrument.");
        foreach (var finding in QualityEvidence) finding.Validate();
        return this;
    }
}

public sealed record HistoricalReplayEvent(
    HistoricalReplayEventType Type, InstrumentId InstrumentId, DateOnly TradingDate,
    DateTimeOffset EffectiveAtUtc, DatasetRevisionId DatasetRevisionId, string ProviderReference,
    ObservationGapClassification? GapClassification, CanonicalMarketBar? Bar,
    CanonicalCorporateAction? CorporateAction, ReplayQualityEvidence? QualityEvidence,
    MarketDataPolicyReference? Policy)
{
    public string LogicalIdentity => string.Join('|', Type, InstrumentId.Value, TradingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        EffectiveAtUtc.ToString("O"), DatasetRevisionId.Value, ProviderReference,
        CorporateAction?.Id.Value ?? string.Empty, QualityEvidence?.FindingCode.ToString() ?? string.Empty);
}

public sealed class DeterministicHistoricalReplay
{
    private readonly IMarketSessionCalendar _calendar;
    private readonly InstrumentMappingCatalog _mappings;
    public DeterministicHistoricalReplay(IMarketSessionCalendar calendar, InstrumentMappingCatalog mappings)
    {
        _calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        _mappings = mappings ?? throw new ArgumentNullException(nameof(mappings));
    }

    public ImmutableArray<HistoricalReplayEvent> Replay(HistoricalReplayRequest request)
    {
        request.Validate();
        var events = new List<(HistoricalReplayEvent Event, int Priority, string TieBreaker)>();
        var fromDate = DateOnly.FromDateTime(request.FromUtc.UtcDateTime);
        var untilDate = DateOnly.FromDateTime(request.UntilUtc.UtcDateTime);
        for (var date = fromDate; date <= untilDate; date = date.AddDays(1))
        {
            var session = _calendar.Resolve(date);
            foreach (var instrumentId in request.Instruments.OrderBy(id => id.Value, StringComparer.Ordinal))
            {
                var reference = ResolveReference(request, instrumentId, date);
                if (session.State == MarketSessionState.Unknown)
                {
                    Add(events, CreateGapEvent(HistoricalReplayEventType.UnknownSession, ObservationGapClassification.UnknownSession,
                        instrumentId, date, StartOfDateUtc(date), request.DatasetRevisionId, reference), 90);
                    continue;
                }
                if (session.State == MarketSessionState.Closed)
                {
                    Add(events, CreateGapEvent(HistoricalReplayEventType.ExpectedClosure, ObservationGapClassification.ExpectedClosure,
                        instrumentId, date, StartOfDateUtc(date), request.DatasetRevisionId, reference), 80);
                    continue;
                }

                Add(events, BaseEvent(HistoricalReplayEventType.SessionOpened, instrumentId, date, session.OpensAtUtc!.Value,
                    request.DatasetRevisionId, reference), 10);
                var sessionClose = session.ClosesAtUtc!.Value;
                foreach (var action in request.CorporateActions.Where(action => action.InstrumentId == instrumentId && action.EffectiveDate == date))
                {
                    var type = action.Type == CorporateActionType.CashDividend ? HistoricalReplayEventType.DividendEffective : HistoricalReplayEventType.SplitEffective;
                    var priority = action.Type == CorporateActionType.CashDividend ? 20 : 30;
                    Add(events, BaseEvent(type, instrumentId, date, session.OpensAtUtc.Value, request.DatasetRevisionId, reference) with
                    { CorporateAction = action, Policy = action.Provenance.Policy }, priority, action.Id.Value);
                }

                var findings = request.QualityEvidence.Where(finding => finding.InstrumentId == instrumentId && finding.TradingDate == date).ToArray();
                foreach (var finding in findings)
                    Add(events, BaseEvent(HistoricalReplayEventType.QualityFindingObserved, instrumentId, date, finding.ObservedAtUtc,
                        request.DatasetRevisionId, reference) with { QualityEvidence = finding, GapClassification = finding.Classification }, 40, finding.FindingCode.ToString());

                var bars = request.Bars.Where(bar => bar.InstrumentId == instrumentId && bar.SessionDate == date).ToArray();
                var invalid = findings.Any(finding => finding.Classification == ObservationGapClassification.InvalidObservation &&
                    bars.Any(bar => finding.ObservedAtUtc <= bar.Provenance.SourceTimestampUtc));
                if (!invalid)
                    foreach (var bar in bars)
                        Add(events, BaseEvent(HistoricalReplayEventType.ObservationAvailable, instrumentId, date,
                            bar.Provenance.SourceTimestampUtc, request.DatasetRevisionId, reference) with
                        { Bar = bar, Policy = bar.Provenance.Policy }, 50, bar.Identity);

                if (bars.Length == 0 || invalid)
                {
                    var explicitProviderGap = findings.FirstOrDefault(finding => finding.Classification == ObservationGapClassification.ProviderGap &&
                        finding.ObservedAtUtc <= sessionClose);
                    var classification = invalid ? ObservationGapClassification.InvalidObservation :
                        explicitProviderGap is null ? ObservationGapClassification.MissingObservation : ObservationGapClassification.ProviderGap;
                    Add(events, CreateGapEvent(HistoricalReplayEventType.MissingObservationDetected, classification,
                        instrumentId, date, sessionClose, request.DatasetRevisionId, reference), 60);
                }
                Add(events, BaseEvent(HistoricalReplayEventType.SessionClosed, instrumentId, date, sessionClose,
                    request.DatasetRevisionId, reference), 70);
            }
        }

        return events.Where(value => value.Event.EffectiveAtUtc >= request.FromUtc && value.Event.EffectiveAtUtc <= request.UntilUtc)
            .OrderBy(value => value.Event.EffectiveAtUtc).ThenBy(value => value.Priority)
            .ThenBy(value => value.Event.InstrumentId.Value, StringComparer.Ordinal).ThenBy(value => value.TieBreaker, StringComparer.Ordinal)
            .Select(value => value.Event).ToImmutableArray();
    }

    private string ResolveReference(HistoricalReplayRequest request, InstrumentId instrumentId, DateOnly date) =>
        _mappings.ResolveProviderReference(instrumentId, request.Provider, request.ProviderDataset, request.Mic, date).ProviderReference;

    private static HistoricalReplayEvent BaseEvent(HistoricalReplayEventType type, InstrumentId instrumentId, DateOnly date,
        DateTimeOffset time, DatasetRevisionId revision, string reference) =>
        new(type, instrumentId, date, time, revision, reference, null, null, null, null, null);

    private static HistoricalReplayEvent CreateGapEvent(HistoricalReplayEventType type, ObservationGapClassification classification,
        InstrumentId instrumentId, DateOnly date, DateTimeOffset time, DatasetRevisionId revision, string reference) =>
        BaseEvent(type, instrumentId, date, time, revision, reference) with { GapClassification = classification };

    private static DateTimeOffset StartOfDateUtc(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
    private static void Add(List<(HistoricalReplayEvent Event, int Priority, string TieBreaker)> target,
        HistoricalReplayEvent value, int priority, string tieBreaker = "") => target.Add((value, priority, tieBreaker));
}
