using System.Collections.Immutable;
using System.Text.Json;
using BigBrain.Api.SystemRecovery;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BigBrain.Api.Finance;

internal sealed class FinanceObservationRuntimeOptions
{
    internal const string Section = "Finance:ObservationRuntime";
    private static readonly JsonSerializerOptions InstrumentJson = new()
    { UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };
    public bool Enabled { get; set; }
    public int CadenceMinutes { get; set; } = 360;
    public int LookbackDays { get; set; } = 3;
    public string OwnerAcceptanceVersion { get; set; } = "";
    public DateTimeOffset PolicyRecordedAtUtc { get; set; }
    public bool AffectedUseEnabled { get; set; }
    public DeletionRequirement CurrentDeletion { get; set; } = DeletionRequirement.Unknown;
    public ObservationRuntimeInstrument[] Instruments { get; set; } = [];
    public string InstrumentsJson { get; set; } = "";

    internal static FinanceObservationRuntimeOptions FromConfiguration(IConfiguration configuration)
    {
        var options = configuration.GetSection(Section).Get<FinanceObservationRuntimeOptions>() ?? new();
        if (string.IsNullOrEmpty(options.InstrumentsJson)) return options;
        try
        {
            if (options.Instruments.Length != 0 || options.InstrumentsJson.Length > 16384) throw new InvalidDataException();
            using var json = JsonDocument.Parse(options.InstrumentsJson, new JsonDocumentOptions { MaxDepth = 6 });
            if (json.RootElement.ValueKind != JsonValueKind.Array || json.RootElement.GetArrayLength() is < 1 or > 4)
                throw new InvalidDataException();
            RequireUnique(json.RootElement);
            options.Instruments = JsonSerializer.Deserialize<ObservationRuntimeInstrument[]>(options.InstrumentsJson, InstrumentJson)!;
        }
        catch (Exception e) when (e is JsonException or InvalidDataException)
        { options.Instruments = []; } // Invalid deployment input remains fail-closed, never partially binds.
        return options;
    }

    private static void RequireUnique(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException();
                RequireUnique(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) RequireUnique(item);
    }
}

// Reuse canonical instruments/effective mappings; no inferred or default production universe.
internal sealed class ObservationRuntimeInstrument
{
    public string InstrumentId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string ProviderSymbol { get; set; } = "";
    public string Mic { get; set; } = "";
    public string VenueCode { get; set; } = "";
    public string VenueName { get; set; } = "";
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public string MappingEvidence { get; set; } = "";

    public ObservationMappingVersionOptions[] MappingVersions { get; set; } = [];

    internal (CanonicalInstrument Instrument, ProviderInstrumentMapping Mapping) Canonical()
    {
        if (ValidFrom == default) throw new InvalidDataException();
        var venue = new MarketVenue(VenueCode, VenueName);
        var instrument = new CanonicalInstrument(new(InstrumentId), InstrumentType.Equity, DisplayName,
            new("USD"), venue, Mic, InstrumentLifecycle.Active, ValidFrom, ValidTo);
        return (instrument, new(instrument.Id, new("Alpaca"), new(DailyMarketEvidence.Dataset), ProviderSymbol,
            venue, Mic, ValidFrom, ValidTo, new(MappingEvidence)));
    }
}

internal enum ObservationRuntimeState { Disabled, Waiting, Running, Healthy, Degraded, Misconfigured, ClockBlocked, RecoveryBlocked, Cancelled }
internal enum ObservationFailure { None, Timeout, Cancelled, Transport, Rejected, Persistence, Clock, Unexpected }
internal sealed record ObservationAttempt(string Instrument, DateOnly SourceDay, ObservationAcquisitionKind? Outcome,
    ObservationFailure Failure, string? ReceiptId);
internal sealed record ObservationRuntimeSnapshot(bool Enabled, ObservationRuntimeState State,
    DateTimeOffset? LastStartedUtc, DateTimeOffset? LastCompletedUtc, DateTimeOffset? NextCheckUtc,
    ImmutableArray<ObservationAttempt> Attempts, ObservationFailure Failure)
{
    public ImmutableArray<DateOnly> SourceDays { get; init; } = [];
}

// Application singleton. One finite cycle, no queue/cursor, no science or reasoner dependency.
internal sealed class FinanceObservationRuntime : IHealthCheck
{
    private readonly EodhdMarketMemory _memory;
    private readonly Func<bool> _ready;
    private readonly Func<DateOnly, AlpacaDailyMarketObservations> _source;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly ImmutableArray<ObservationInstrumentPlan> _instruments;
    private readonly MarketDataEntitlementPolicy? _policy;
    private readonly int _lookback;
    private int _flight;
    private readonly GuardedClock _trusted;
    private ObservationRuntimeSnapshot _snapshot;
    internal TimeSpan Cadence { get; }
    internal ObservationRuntimeSnapshot Snapshot => Volatile.Read(ref _snapshot);

    internal FinanceObservationRuntime(FinanceObservationRuntimeOptions options, AlpacaDailyObservationOptions transport,
        EodhdMarketMemory memory, TimeProvider clock, Func<bool> ready,
        Func<DateOnly, AlpacaDailyMarketObservations>? fixtureSource = null,
        Func<TimeSpan, CancellationToken, Task>? fixtureDelay = null)
    {
        _memory = memory; _trusted = new(clock); _ready = ready;
        // Freeze trusted transport configuration; no model/provider data controls this factory.
        var copy = new AlpacaDailyObservationOptions
        {
            Enabled = transport.Enabled,
            ApiKey = transport.ApiKey,
            ApiSecret = transport.ApiSecret,
            TimeoutSeconds = transport.TimeoutSeconds
        };
        _source = fixtureSource ?? (day => new(copy, day));
        _delay = fixtureDelay ?? ((duration, token) => Task.Delay(duration, clock, token));
        Cadence = TimeSpan.FromMinutes(options.CadenceMinutes is >= 60 and <= 1440 ? options.CadenceMinutes : 360);
        _snapshot = new(options.Enabled, options.Enabled ? ObservationRuntimeState.Misconfigured : ObservationRuntimeState.Disabled,
            null, null, null, [], ObservationFailure.None);
        if (!options.Enabled) return;
        try
        {
            (_policy, _instruments) = ValidateConfiguration(options, transport, _trusted.GetUtcNow());
            _lookback = options.LookbackDays;
            _snapshot = _snapshot with { State = ObservationRuntimeState.Waiting, NextCheckUtc = _trusted.GetUtcNow() + Cadence };
        }
        catch (Exception e) when (e is ArgumentException or InvalidDataException or ObservationClockException)
        { /* Invalid configuration never leaks values or grants network authority. */ }
    }

    internal static (MarketDataEntitlementPolicy Policy, ImmutableArray<ObservationInstrumentPlan> Instruments)
        ValidateConfiguration(FinanceObservationRuntimeOptions options, AlpacaDailyObservationOptions transport, DateTimeOffset now)
    {
        if (options.CadenceMinutes is < 60 or > 1440 || options.LookbackDays is < 1 or > 7 ||
            options.Instruments is not { Length: >= 1 and <= 4 } || !transport.Enabled || transport.TimeoutSeconds is < 1 or > 30 ||
            !AlpacaDailyMarketObservations.ValidCredential(transport.ApiKey) || !AlpacaDailyMarketObservations.ValidCredential(transport.ApiSecret) ||
            options.OwnerAcceptanceVersion != AlpacaDailyOwnerDecision.Version || options.PolicyRecordedAtUtc == default)
            throw new InvalidDataException();
        var policy = AlpacaDailyOwnerDecision.Create(options.PolicyRecordedAtUtc, options.CurrentDeletion, options.AffectedUseEnabled);
        AlpacaDailyOwnerDecision.Require(policy, now);
        var instruments = options.Instruments.Select(entry => new ObservationInstrumentPlan(entry ?? throw new InvalidDataException(), now))
            .OrderBy(x => x.InstrumentId, StringComparer.Ordinal).ToImmutableArray();
        if (instruments.Select(x => x.InstrumentId).Distinct(StringComparer.Ordinal).Count() != instruments.Length ||
            instruments.Select(x => x.Snapshots[0].Mapping.ProviderReference).Distinct(StringComparer.Ordinal).Count() != instruments.Length)
            throw new InvalidDataException();
        return (policy, instruments);
    }

    internal static void ValidateSnapshot(CanonicalInstrument instrument, ProviderInstrumentMapping mapping)
    {
        if (instrument.Type != InstrumentType.Equity || instrument.Lifecycle != InstrumentLifecycle.Active ||
            instrument.Currency.Code != "USD" || mapping.Provider.Value != "Alpaca" ||
            mapping.ProviderDataset.Value != DailyMarketEvidence.Dataset || mapping.Mic is not ("XNAS" or "XNYS" or "ARCX") ||
            mapping.InstrumentId != instrument.Id || mapping.Mic != instrument.Mic || mapping.Venue != instrument.Venue ||
            mapping.ProviderReference.Length is < 1 or > 16 ||
            mapping.ProviderReference.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '.' or '-')))
            throw new InvalidDataException();
        MarketObservationIntegrity.Token(instrument.Id.Value); MarketObservationIntegrity.Token(mapping.Evidence.Value);
        MarketObservationIntegrity.Token(instrument.Venue.Code);
        if (instrument.DisplayName.Length > 128 || instrument.Venue.Name.Length > 128) throw new InvalidDataException();
    }

    internal async Task<ObservationRuntimeSnapshot> RunCycleAsync(CancellationToken token)
    {
        // No waiting callers/tasks. The in-flight snapshot reports the existing owned cycle.
        if (Interlocked.CompareExchange(ref _flight, 1, 0) != 0) return Snapshot;
        var attempts = ImmutableArray.CreateBuilder<ObservationAttempt>();
        var current = Snapshot;
        try
        {
            if (current.State is ObservationRuntimeState.Disabled or ObservationRuntimeState.Misconfigured or ObservationRuntimeState.ClockBlocked)
                return current;
            token.ThrowIfCancellationRequested();
            var now = _trusted.GetUtcNow();
            if (current.NextCheckUtc is { } next && now < next) return current;
            if (!_ready()) return Set(current with { State = ObservationRuntimeState.RecoveryBlocked, NextCheckUtc = now + Cadence });
            current = Set(current with
            {
                State = ObservationRuntimeState.Running,
                LastStartedUtc = now,
                LastCompletedUtc = null,
                NextCheckUtc = null,
                Attempts = [],
                Failure = ObservationFailure.None
            });
            _memory.RequireObservationRuntimeReady(_policy!, now);
            var today = DailyMarketEvidence.SourceDate(now);
            current = Set(current with { SourceDays = Enumerable.Range(1, _lookback).Reverse().Select(offset => today.AddDays(-offset)).ToImmutableArray() });
            foreach (var day in current.SourceDays)
            {
                // Calendar only reduces requests. Returned evidence must still pass daily eligibility.
                if (!UsMarketCalendar.IsSession(day)) continue;
                foreach (var plan in _instruments)
                {
                    token.ThrowIfCancellationRequested();
                    (CanonicalInstrument Instrument, ProviderInstrumentMapping Mapping) selected;
                    try { selected = plan.Resolve(day, _trusted.GetUtcNow()); }
                    catch (Exception e) when (e is InvalidDataException or MarketDataNormalizationException)
                    {
                        attempts.Add(new(plan.InstrumentId, day, null, ObservationFailure.Rejected, null));
                        continue;
                    }
                    var (instrument, mapping) = selected;
                    _memory.RequireObservationRuntimeReady(_policy!, _trusted.GetUtcNow());
                    if (attempts.Count > 0) await _delay(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        var authorization = _memory.PrepareObservationMapping(plan, day, _trusted.GetUtcNow());
                        using var source = _source(day);
                        var result = await _memory.ReobserveDailyAsync(source, instrument, mapping, _policy!, _trusted, token, authorization).ConfigureAwait(false);
                        attempts.Add(new(instrument.Id.Value, day, result.Kind, ObservationFailure.None, result.Receipt.Id));
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    { attempts.Add(new(instrument.Id.Value, day, null, ObservationFailure.Cancelled, null)); throw; }
                    catch (ObservationSourceException e)
                    {
                        attempts.Add(new(instrument.Id.Value, day, null, e.Failure, null));
                        // Provider failure is isolated to this request; no retry. Later slots are separate acquisitions.
                    }
                    catch (Exception e)
                    { attempts.Add(new(instrument.Id.Value, day, null, Failure(e), null)); throw; }
                }
            }
            var finished = _trusted.GetUtcNow();
            return Set(current with
            {
                State = attempts.Any(x => x.Failure != ObservationFailure.None) ? ObservationRuntimeState.Degraded : ObservationRuntimeState.Healthy,
                LastCompletedUtc = finished,
                NextCheckUtc = finished + Cadence,
                Attempts = attempts.ToImmutable()
            });
        }
        catch (Exception e)
        {
            // Never attach exception text/object, request, credentials or raw provider data to status/logs.
            var failure = e is OperationCanceledException && token.IsCancellationRequested ? ObservationFailure.Cancelled : Failure(e);
            DateTimeOffset? finished = null;
            try { finished = _trusted.GetUtcNow(); } catch (ObservationClockException) { failure = ObservationFailure.Clock; }
            return Set(current with
            {
                State = failure == ObservationFailure.Clock ? ObservationRuntimeState.ClockBlocked :
                    failure == ObservationFailure.Cancelled ? ObservationRuntimeState.Cancelled : ObservationRuntimeState.Degraded,
                Failure = failure,
                LastCompletedUtc = finished,
                NextCheckUtc = finished + Cadence,
                Attempts = attempts.ToImmutable()
            });
        }
        finally { Volatile.Write(ref _flight, 0); }
    }

    private ObservationRuntimeSnapshot Set(ObservationRuntimeSnapshot state) { Volatile.Write(ref _snapshot, state); return state; }
    internal static ObservationFailure Failure(Exception e) => e switch
    {
        ObservationClockException => ObservationFailure.Clock,
        SqliteException => ObservationFailure.Persistence,
        TimeoutException => ObservationFailure.Timeout,
        InvalidDataException or JsonException or ArgumentException or InvalidOperationException => ObservationFailure.Rejected,
        IOException or HttpRequestException => ObservationFailure.Transport,
        _ => ObservationFailure.Unexpected
    };
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var state = Snapshot;
        var healthy = state.State is ObservationRuntimeState.Disabled or ObservationRuntimeState.Waiting or ObservationRuntimeState.Healthy or ObservationRuntimeState.Running;
        return Task.FromResult(new HealthCheckResult(healthy ? HealthStatus.Healthy : HealthStatus.Degraded,
            "Finance observation: " + state.State, data: new Dictionary<string, object> { ["observation"] = state }));
    }
    internal sealed class GuardedClock(TimeProvider inner) : TimeProvider
    {
        private DateTimeOffset _last;
        public override DateTimeOffset GetUtcNow()
        {
            var now = inner.GetUtcNow();
            if (now.Offset != TimeSpan.Zero || now == default || now < _last) throw new ObservationClockException();
            _last = now; return now;
        }
    }
}

internal sealed class FinanceObservationWorker(FinanceObservationRuntime runtime, SystemRecoveryCoordinator recovery,
    TimeProvider clock, ILogger<FinanceObservationWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, string, Exception?> CycleLog = LoggerMessage.Define<string>(LogLevel.Information,
        new EventId(13206, "FinanceObservationCycle"), "Finance observation cycle: {State}.");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!runtime.Snapshot.Enabled) return;
        try
        {
            await recovery.WaitUntilRecoveredAsync(stoppingToken).ConfigureAwait(false);
            while (!stoppingToken.IsCancellationRequested)
            {
                // Also on startup: restart cannot create an immediate retry storm or catch-up burst.
                await Task.Delay(runtime.Cadence, clock, stoppingToken).ConfigureAwait(false);
                var result = await runtime.RunCycleAsync(stoppingToken).ConfigureAwait(false);
                CycleLog(logger, result.State.ToString(), null);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
