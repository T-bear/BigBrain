using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;
using BigBrain.Api.SystemRecovery;

namespace BigBrain.Api.Finance;

public sealed record EodhdFinanceOptions
{
    public const string Section = "Finance:Eodhd";
    public bool Enabled { get; set; }
    public bool AccountActive { get; set; }
    public string ApiToken { get; set; } = "";
    public string BaseUrl { get; set; } = "https://eodhd.com/api";
    public string DatabasePath { get; set; } = Path.Combine(Path.GetTempPath(), "bigbrain-finance", "eodhd-market-memory.db");
    public string PayloadDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "bigbrain-finance", "eodhd-payloads");
    public int TimeoutSeconds { get; set; } = 15;
    public int MaximumRetries { get; set; } = 2;
    public DateTimeOffset? EntitlementEndsAtUtc { get; set; }
}

internal sealed record EodhdDailyBar(DateOnly Date, decimal Open, decimal High, decimal Low,
    decimal Close, decimal AdjustedClose, long Volume);

internal sealed record EodhdInstrument(string InstrumentId, string Symbol, string Name, string ProviderSymbol, string Mic);

internal static class EodhdCatalog
{
    internal static readonly EodhdInstrument[] Watchlist =
    [
        new("US:ARCX:SPY", "SPY", "SPDR S&P 500 ETF Trust", "SPY.US", "ARCX"),
        new("US:XNAS:QQQ", "QQQ", "Invesco QQQ Trust", "QQQ.US", "XNAS"),
        new("US:ARCX:IWM", "IWM", "iShares Russell 2000 ETF", "IWM.US", "ARCX"),
        new("US:XNAS:AAPL", "AAPL", "Apple", "AAPL.US", "XNAS"),
        new("US:XNAS:MSFT", "MSFT", "Microsoft", "MSFT.US", "XNAS"),
        new("US:XNYS:JPM", "JPM", "JPMorgan Chase", "JPM.US", "XNYS"),
        new("US:XNYS:XOM", "XOM", "Exxon Mobil", "XOM.US", "XNYS"),
        new("US:XNYS:JNJ", "JNJ", "Johnson & Johnson", "JNJ.US", "XNYS")
    ];
}

internal static class EodhdEntitlement
{
    internal static MarketDataEntitlementPolicy Create(EodhdFinanceOptions options) => new(
        new PolicyId(EodhdMarketMemory.Policy), new PolicyVersion("2026-08-11"),
        new MarketDataProvider(EodhdMarketMemory.Provider), new ProviderDataset(EodhdMarketMemory.Product),
        new EvidenceReference("eodhd:first-party-pricing-api-terms:2026-08-11"),
        new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero), options.EntitlementEndsAtUtc,
        new Dictionary<MarketDataUse, EntitlementDecision>
        {
            [MarketDataUse.HistoricalAnalysis] = EntitlementDecision.Allowed,
            [MarketDataUse.Backtest] = EntitlementDecision.Allowed,
            [MarketDataUse.DerivedMetrics] = EntitlementDecision.Allowed,
            [MarketDataUse.LongTermStorage] = EntitlementDecision.Allowed,
            [MarketDataUse.LiveDisplay] = EntitlementDecision.Denied,
            [MarketDataUse.PaperTrading] = EntitlementDecision.Denied,
            [MarketDataUse.WalkForward] = EntitlementDecision.Denied,
            [MarketDataUse.StrategyTraining] = EntitlementDecision.Denied
        }, EntitlementDecision.Allowed, EntitlementDecision.Denied,
        RetentionClassification.SubscriptionOnly, DeletionRequirement.DeleteAtSubscriptionEnd,
        evidenceClass: EntitlementEvidenceClass.OwnerAcceptedPersonalResearch, monetaryCostSek: 0,
        ownerAcceptanceVersion: "BB-077/2026-08-11",
        rationale: "EODHD Free private non-commercial EOD storage, manipulation, analysis and replay while active; all copies deleted within one month after expiry.");

    internal static bool AllowsAcquisition(EodhdFinanceOptions options, DateTimeOffset now)
    {
        var policy = Create(options); var context = new MarketDataEntitlementContext(now, true, true,
            MarketDataClassification.Raw, policy.Provider, policy.ProviderDataset);
        return new[] { MarketDataUse.HistoricalAnalysis, MarketDataUse.Backtest, MarketDataUse.DerivedMetrics, MarketDataUse.LongTermStorage }
            .All(use => MarketDataEntitlementEvaluator.Evaluate(policy, use, context).IsAllowed);
    }
}

internal sealed class EodhdAdapter : IDisposable
{
    private readonly EodhdFinanceOptions _options;
    private readonly HttpClient _client;

    internal EodhdAdapter(EodhdFinanceOptions options, HttpMessageHandler? handler = null)
    {
        _options = options;
        _client = handler is null ? new HttpClient(new SocketsHttpHandler()) : new HttpClient(handler);
        _client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        _client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 3, 60));
    }

    internal async Task<(IReadOnlyList<EodhdDailyBar> Bars, byte[] Payload, int Retries)> FetchAsync(
        string providerSymbol, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiToken)) throw new InvalidOperationException("EODHD API token is not configured.");
        var escapedSymbol = Uri.EscapeDataString(providerSymbol);
        var token = Uri.EscapeDataString(_options.ApiToken);
        var path = $"eod/{escapedSymbol}?api_token={token}&fmt=json&period=d&order=a&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        for (var attempt = 0; ; attempt++)
        {
            using var response = await _client.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (response.IsSuccessStatusCode) return (Parse(payload), payload, attempt);
            if (attempt >= Math.Clamp(_options.MaximumRetries, 0, 3) ||
                response.StatusCode is not (HttpStatusCode.TooManyRequests or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout))
                throw new HttpRequestException($"EODHD request failed with HTTP {(int)response.StatusCode}.", null, response.StatusCode);
            await Task.Delay(TimeSpan.FromMilliseconds(250 * (1 << attempt)), cancellationToken);
        }
    }

    internal static IReadOnlyList<EodhdDailyBar> Parse(byte[] payload)
    {
        using var document = JsonDocument.Parse(payload);
        if (document.RootElement.ValueKind != JsonValueKind.Array) throw new InvalidDataException("EODHD EOD response must be an array.");
        var bars = new List<EodhdDailyBar>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            var date = DateOnly.ParseExact(item.GetProperty("date").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var bar = new EodhdDailyBar(date, Decimal(item, "open"), Decimal(item, "high"), Decimal(item, "low"),
                Decimal(item, "close"), Decimal(item, "adjusted_close"), item.GetProperty("volume").GetInt64());
            if (bar.Open <= 0 || bar.High <= 0 || bar.Low <= 0 || bar.Close <= 0 || bar.AdjustedClose <= 0 ||
                bar.Volume < 0 || bar.High < Math.Max(bar.Open, bar.Close) || bar.Low > Math.Min(bar.Open, bar.Close) || bar.Low > bar.High)
                throw new InvalidDataException($"EODHD returned invalid OHLCV for {bar.Date:yyyy-MM-dd}.");
            bars.Add(bar);
        }
        if (bars.Select(value => value.Date).Distinct().Count() != bars.Count) throw new InvalidDataException("EODHD returned duplicate session dates.");
        return bars.OrderBy(value => value.Date).ToArray();
    }

    private static decimal Decimal(JsonElement item, string property) => item.GetProperty(property).GetDecimal();
    public void Dispose() => _client.Dispose();
}

internal sealed record EodhdDeletionPreview(string PreviewId, int Observations, int Revisions, int Payloads,
    int FeatureValues, int FeatureRevisions, int BacktestRuns, int BacktestEvents, int BacktestFills,
    int BacktestEquityPoints,int RobustnessEvaluations,int RobustnessWindows,int RobustnessParameterPoints,
    int RobustnessCostPoints,int RobustnessRunReferences,DateTimeOffset? DeadlineUtc, string Scope);

internal sealed record EodhdRuntimeEvidence(int ExternalRequests, int AcquisitionAttempts, int SuccessfulAttempts,
    int FailedAttempts, int Retries, int Observations, int Revisions, int Payloads, DateOnly? CoverageFrom,
    DateOnly? CoverageTo, IReadOnlyList<string> SuccessfulSymbols, IReadOnlyList<string> FailedSymbols,
    IReadOnlyList<string> RevisionIds, bool CausalKnowledgeTimes, int MissingPayloadFiles);

internal sealed partial class EodhdMarketMemory
{
    internal const string Provider = "EODHD";
    internal const string Product = "Free";
    internal const string Policy = "eodhd-free-personal-v2026-08-11";
    private readonly EodhdFinanceOptions _options;

    internal EodhdMarketMemory(EodhdFinanceOptions options, FinanceRiskOptions? riskPolicy = null)
    {
        _options = options;
        _riskPolicy = riskPolicy ?? new FinanceRiskOptions();
        _riskPolicy.Validate();
        var directory = Path.GetDirectoryName(options.DatabasePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        Directory.CreateDirectory(options.PayloadDirectory);
        FinanceSchemaMigrator.Migrate(options.DatabasePath);
        Initialize();
    }

    private string ConnectionString => new SqliteConnectionStringBuilder { DataSource = _options.DatabasePath }.ToString();

    private void Initialize()
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        using var command = connection.CreateCommand(); command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS observations(
              provider TEXT NOT NULL, product TEXT NOT NULL, policy TEXT NOT NULL, instrument_id TEXT NOT NULL,
              symbol TEXT NOT NULL, provider_symbol TEXT NOT NULL, mic TEXT NOT NULL, session_date TEXT NOT NULL,
              open TEXT NOT NULL, high TEXT NOT NULL, low TEXT NOT NULL, close TEXT NOT NULL,
              adjusted_close TEXT NOT NULL, volume INTEGER NOT NULL, acquired_utc TEXT NOT NULL,
              revision_id TEXT NOT NULL, PRIMARY KEY(provider, product, instrument_id, session_date, revision_id));
            CREATE TABLE IF NOT EXISTS revisions(
              revision_id TEXT PRIMARY KEY, checksum TEXT NOT NULL, created_utc TEXT NOT NULL,
              observation_count INTEGER NOT NULL, payload_count INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS acquisitions(
              id INTEGER PRIMARY KEY AUTOINCREMENT, provider_symbol TEXT NOT NULL, requested_from TEXT NOT NULL,
              requested_to TEXT NOT NULL, started_utc TEXT NOT NULL, completed_utc TEXT NOT NULL,
              outcome TEXT NOT NULL, rows INTEGER NOT NULL, retries INTEGER NOT NULL,
              checksum TEXT, revision_id TEXT, reason TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS payloads(checksum TEXT PRIMARY KEY, path TEXT NOT NULL, created_utc TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS deletion_receipts(
              receipt_id TEXT PRIMARY KEY, deleted_utc TEXT NOT NULL, observations INTEGER NOT NULL,
              revisions INTEGER NOT NULL, payloads INTEGER NOT NULL, fingerprint TEXT NOT NULL);
            """; command.ExecuteNonQuery();
        InitializeFeatureStorage(connection);
        InitializeBacktestStorage(connection);
        InitializeRobustnessStorage(connection);
        InitializeShadowStorage(connection);
        InitializeCadenceStorage(connection);
        InitializeRiskStorage(connection);
        Execute(connection, null, "UPDATE acquisitions SET outcome='interrupted',reason='marketData.acquisition.interruptedBeforeCommit' WHERE outcome='started'");
    }

    internal long RecordStarted(EodhdInstrument instrument, DateOnly from, DateOnly to, DateTimeOffset startedUtc)
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        Execute(connection, null, """
            INSERT INTO acquisitions(provider_symbol,requested_from,requested_to,started_utc,completed_utc,outcome,rows,retries,reason)
            VALUES($symbol,$from,$to,$started,$started,'started',0,0,'marketData.acquisition.requestStarted')
            """, ("$symbol", instrument.ProviderSymbol), ("$from", Date(from)), ("$to", Date(to)), ("$started", startedUtc.ToString("O")));
        using var command = connection.CreateCommand(); command.CommandText = "SELECT last_insert_rowid()";
        return (long)command.ExecuteScalar()!;
    }

    internal string Store(EodhdInstrument instrument, IReadOnlyList<EodhdDailyBar> bars, byte[] payload,
        DateOnly from, DateOnly to, DateTimeOffset startedUtc, DateTimeOffset completedUtc, int retries, long? acquisitionId = null)
    {
        var payloadHash = Sha(payload); var revisionId = $"eodhd-{payloadHash[7..23]}";
        var payloadPath = Path.Combine(_options.PayloadDirectory, payloadHash[7..] + ".json");
        if (!File.Exists(payloadPath))
        {
            var temporary = payloadPath + ".tmp";
            File.WriteAllBytes(temporary, payload);
            File.Move(temporary, payloadPath, false);
        }
        using var connection = new SqliteConnection(ConnectionString); connection.Open(); using var transaction = connection.BeginTransaction();
        Execute(connection, transaction, "INSERT OR IGNORE INTO payloads VALUES($h,$p,$t)", ("$h", payloadHash), ("$p", payloadPath), ("$t", completedUtc.ToString("O")));
        foreach (var bar in bars)
            Execute(connection, transaction, """
                INSERT OR IGNORE INTO observations VALUES($provider,$product,$policy,$instrument,$symbol,$providerSymbol,$mic,$date,$open,$high,$low,$close,$adjusted,$volume,$acquired,$revision)
                """, ("$provider", Provider), ("$product", Product), ("$policy", Policy), ("$instrument", instrument.InstrumentId),
                ("$symbol", instrument.Symbol), ("$providerSymbol", instrument.ProviderSymbol), ("$mic", instrument.Mic),
                ("$date", Date(bar.Date)), ("$open", Text(bar.Open)), ("$high", Text(bar.High)),
                ("$low", Text(bar.Low)), ("$close", Text(bar.Close)), ("$adjusted", Text(bar.AdjustedClose)),
                ("$volume", bar.Volume), ("$acquired", completedUtc.ToString("O")), ("$revision", revisionId));
        Execute(connection, transaction, "INSERT OR IGNORE INTO revisions VALUES($id,$hash,$created,$count,1)",
            ("$id", revisionId), ("$hash", payloadHash), ("$created", completedUtc.ToString("O")), ("$count", bars.Count));
        Execute(connection, transaction, "INSERT OR IGNORE INTO revision_price_capabilities VALUES($id,'RAW_ONLY_VALID','RawAndAdjustedValid','eodhd-adapter-v1',$evidence,$at)",
            ("$id", revisionId), ("$evidence", "EODHD adapter validated distinct source close and adjusted_close fields; immutable payload hash retained."), ("$at", completedUtc.ToString("O")));
        if (acquisitionId is { } id)
            Execute(connection, transaction, """
                UPDATE acquisitions SET completed_utc=$completed,outcome='success',rows=$rows,retries=$retries,checksum=$checksum,revision_id=$revision,reason='marketData.entitlement.allowed' WHERE id=$id AND outcome='started'
                """, ("$completed", completedUtc.ToString("O")), ("$rows", bars.Count), ("$retries", retries), ("$checksum", payloadHash), ("$revision", revisionId), ("$id", id));
        else Execute(connection, transaction, """
                INSERT INTO acquisitions(provider_symbol,requested_from,requested_to,started_utc,completed_utc,outcome,rows,retries,checksum,revision_id,reason)
                VALUES($symbol,$from,$to,$started,$completed,'success',$rows,$retries,$checksum,$revision,'marketData.entitlement.allowed')
                """, ("$symbol", instrument.ProviderSymbol), ("$from", Date(from)), ("$to", Date(to)),
                ("$started", startedUtc.ToString("O")), ("$completed", completedUtc.ToString("O")), ("$rows", bars.Count),
                ("$retries", retries), ("$checksum", payloadHash), ("$revision", revisionId));
        transaction.Commit(); return revisionId;
    }

    internal void RecordFailure(EodhdInstrument instrument, DateOnly from, DateOnly to, DateTimeOffset startedUtc, string reason, long? acquisitionId = null)
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        if (acquisitionId is { } id) Execute(connection, null, "UPDATE acquisitions SET completed_utc=$completed,outcome='failure',reason=$reason WHERE id=$id AND outcome='started'",
            ("$completed", DateTimeOffset.UtcNow.ToString("O")), ("$reason", reason), ("$id", id));
        else Execute(connection, null, """
            INSERT INTO acquisitions(provider_symbol,requested_from,requested_to,started_utc,completed_utc,outcome,rows,retries,reason)
            VALUES($symbol,$from,$to,$started,$completed,'failure',0,0,$reason)
            """, ("$symbol", instrument.ProviderSymbol), ("$from", Date(from)), ("$to", Date(to)),
            ("$started", startedUtc.ToString("O")), ("$completed", DateTimeOffset.UtcNow.ToString("O")), ("$reason", reason));
    }

    internal bool ShouldAcquire(string providerSymbol, DateOnly utcDate)
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open(); using var command = connection.CreateCommand();
        command.CommandText = "SELECT completed_utc FROM acquisitions WHERE provider_symbol=$symbol AND outcome IN ('success','started','interrupted') ORDER BY started_utc DESC,id DESC LIMIT 1";
        command.Parameters.AddWithValue("$symbol", providerSymbol); var value = command.ExecuteScalar() as string;
        return value is null || DateOnly.FromDateTime(DateTimeOffset.Parse(value, CultureInfo.InvariantCulture).UtcDateTime) < utcDate;
    }

    internal FinanceObservationSnapshot Snapshot(bool enabled, bool configured, bool accountActive)
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        var rows = new List<(string Instrument, string Symbol, DateOnly Date, decimal Close, DateTimeOffset Acquired, string Revision)>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT instrument_id,symbol,session_date,close,acquired_utc,revision_id FROM (
                  SELECT instrument_id,symbol,session_date,close,acquired_utc,revision_id,
                    ROW_NUMBER() OVER(PARTITION BY instrument_id,session_date ORDER BY acquired_utc DESC,revision_id DESC) AS rank
                  FROM observations) WHERE rank=1 ORDER BY instrument_id,session_date
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read()) rows.Add((reader.GetString(0), reader.GetString(1), DateOnly.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture), DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture), reader.GetString(5)));
        }
        var retention = Retention(connection, accountActive);
        var canUse = enabled && configured && accountActive && retention.State == FinanceRetentionState.Active;
        var watchlist = EodhdCatalog.Watchlist.Select(instrument =>
        {
            var values = rows.Where(value => value.Instrument == instrument.InstrumentId).OrderBy(value => value.Date).ToArray();
            var last = values.LastOrDefault(); var previous = values.Length > 1 ? values[^2] : default;
            decimal? change = values.Length > 1 && previous.Close != 0 ? (last.Close - previous.Close) / previous.Close * 100 : null;
            return new FinanceInstrumentObservation(instrument.InstrumentId, instrument.Symbol, instrument.Name,
                values.Length == 0 ? null : last.Close, values.Length == 0 ? null : "USD", change,
                values.Length == 0 ? null : new DateTimeOffset(last.Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                values.Length == 0 ? ObservationFreshnessState.Unavailable : ObservationFreshnessState.Delayed,
                ObservationSessionState.Closed, values.Length == 0 ? ObservationQualityState.Unknown : ObservationQualityState.Good,
                values.Length == 0 ? ObservationDataKind.None : ObservationDataKind.Real,
                values.TakeLast(260).Select(value => new FinanceChartPoint(new DateTimeOffset(value.Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), value.Close, false)).ToArray());
        }).ToArray();
        var hasData = rows.Count > 0; var latest = rows.Count == 0 ? (DateTimeOffset?)null : rows.Max(value => value.Acquired);
        var latestRevision = rows.LastOrDefault().Revision;
        return new FinanceObservationSnapshot(DateTimeOffset.UtcNow,
            new(FinanceOperatingMode.Research, false, false, false, canUse, canUse),
            new(configured ? MarketDataProviderState.Authorized : MarketDataProviderState.Candidate, "EODHD Free",
                _options.EntitlementEndsAtUtc is { } end && end <= DateTimeOffset.UtcNow ? EntitlementState.Expired : EntitlementState.Authorized,
                "EODHD FREE PERSONAL RESEARCH", configured ? "Credential configured; EOD-only read path." : "Free API key missing; provider disabled.",
                "ownerAcceptedPersonalResearch"), latest, hasData ? ObservationDataKind.Real : ObservationDataKind.None, watchlist,
            new(rows.Count, latestRevision, null, rows.Count == 0 ? null : rows.Min(value => value.Date), rows.Count == 0 ? null : rows.Max(value => value.Date), latest,
                0, 0, hasData ? HistoricalPersistenceState.Durable : HistoricalPersistenceState.NotConfigured, Provider, Product, Policy,
                "eodhd:eod-json;raw-ohlc;split-adjusted-volume"), retention);
    }

    internal EodhdDeletionPreview PreviewDeletion()
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        var observations = Scalar(connection, "SELECT COUNT(*) FROM observations");
        var revisions = Scalar(connection, "SELECT COUNT(*) FROM revisions"); var payloads = Scalar(connection, "SELECT COUNT(*) FROM payloads");
        var featureValues = Scalar(connection, "SELECT COUNT(*) FROM feature_values");
        var featureRevisions = Scalar(connection, "SELECT COUNT(*) FROM feature_revisions");
        var backtestRuns=Scalar(connection,"SELECT COUNT(*) FROM backtest_runs");var backtestEvents=Scalar(connection,"SELECT COUNT(*) FROM backtest_events");var backtestFills=Scalar(connection,"SELECT COUNT(*) FROM backtest_fills");var backtestEquity=Scalar(connection,"SELECT COUNT(*) FROM backtest_equity");
        var evaluations=Scalar(connection,"SELECT COUNT(*) FROM robustness_evaluations");var windows=Scalar(connection,"SELECT COUNT(*) FROM robustness_windows");var parameterPoints=Scalar(connection,"SELECT COUNT(*) FROM robustness_parameter_sensitivity");var costPoints=Scalar(connection,"SELECT COUNT(*) FROM robustness_cost_sensitivity");var runReferences=Scalar(connection,"SELECT COUNT(*) FROM robustness_run_references");
        var shadowPredictions=Scalar(connection,"SELECT COUNT(*) FROM shadow_predictions");var shadowOutcomes=Scalar(connection,"SELECT COUNT(*) FROM shadow_outcomes");
        var riskEvaluations=Scalar(connection,"SELECT COUNT(*) FROM risk_evaluations");
        var seed = $"{Provider}|{Product}|{Policy}|{observations}|{revisions}|{payloads}|{featureValues}|{featureRevisions}|{backtestRuns}|{backtestEvents}|{backtestFills}|{backtestEquity}|{evaluations}|{windows}|{parameterPoints}|{costPoints}|{runReferences}|{shadowPredictions}|{shadowOutcomes}|{riskEvaluations}";
        return new($"preview-{Sha(Encoding.UTF8.GetBytes(seed))[7..19]}", observations, revisions, payloads, featureValues, featureRevisions,backtestRuns,backtestEvents,backtestFills,backtestEquity,evaluations,windows,parameterPoints,costPoints,runReferences,
            _options.EntitlementEndsAtUtc?.AddMonths(1), "raw payloads, normalized observations, market/feature revisions, dependent backtests, robustness, prospective shadow and source-dependent risk evaluations; pure halt audit metadata is retained");
    }

    internal string ExecuteDeletion(EodhdDeletionPreview preview, string confirmation, DateTimeOffset deletedAtUtc)
    {
        if (confirmation != $"DELETE {preview.PreviewId}") throw new InvalidOperationException("Exact deletion preview confirmation is required.");
        var current = PreviewDeletion(); if (current != preview) throw new InvalidOperationException("Deletion scope changed; create a new preview.");
        using var connection = new SqliteConnection(ConnectionString); connection.Open(); using var transaction = connection.BeginTransaction();
        var paths = new List<string>(); using (var command = connection.CreateCommand()) { command.Transaction = transaction; command.CommandText = "SELECT path FROM payloads"; using var reader = command.ExecuteReader(); while (reader.Read()) paths.Add(reader.GetString(0)); }
        foreach (var path in paths.Where(File.Exists)) File.Delete(path);
        if (paths.Any(File.Exists)) throw new IOException("One or more covered EODHD payloads could not be deleted.");
        Execute(connection,transaction,"DELETE FROM risk_evaluations");Execute(connection,transaction,"DELETE FROM shadow_outcomes");Execute(connection,transaction,"DELETE FROM shadow_predictions");
        Execute(connection,transaction,"DELETE FROM robustness_run_references");Execute(connection,transaction,"DELETE FROM robustness_windows");Execute(connection,transaction,"DELETE FROM robustness_parameter_sensitivity");Execute(connection,transaction,"DELETE FROM robustness_cost_sensitivity");Execute(connection,transaction,"DELETE FROM robustness_evaluations");
        Execute(connection,transaction,"DELETE FROM backtest_events");Execute(connection,transaction,"DELETE FROM backtest_fills");Execute(connection,transaction,"DELETE FROM backtest_equity");Execute(connection,transaction,"DELETE FROM backtest_runs");
        Execute(connection, transaction, "DELETE FROM feature_values"); Execute(connection, transaction, "DELETE FROM feature_revisions");
        Execute(connection, transaction, "DELETE FROM observations"); Execute(connection, transaction, "DELETE FROM revisions"); Execute(connection, transaction, "DELETE FROM payloads");
        var fingerprint = Sha(Encoding.UTF8.GetBytes($"{preview.PreviewId}|{deletedAtUtc:O}|{preview.Observations}|{preview.Revisions}|{preview.Payloads}|{preview.FeatureValues}|{preview.FeatureRevisions}"));
        var receipt = $"eodhd-delete-{fingerprint[7..23]}";
        Execute(connection, transaction, "INSERT INTO deletion_receipts VALUES($id,$at,$o,$r,$p,$f)", ("$id", receipt), ("$at", deletedAtUtc.ToString("O")),
            ("$o", preview.Observations), ("$r", preview.Revisions), ("$p", preview.Payloads), ("$f", fingerprint));
        Execute(connection, transaction, "INSERT INTO feature_deletion_receipts VALUES($id,$values,$revisions)",
            ("$id", receipt), ("$values", preview.FeatureValues), ("$revisions", preview.FeatureRevisions));
        Execute(connection,transaction,"INSERT INTO backtest_deletion_receipts VALUES($id,$runs,$events,$fills,$equity)",( "$id",receipt),("$runs",preview.BacktestRuns),("$events",preview.BacktestEvents),("$fills",preview.BacktestFills),("$equity",preview.BacktestEquityPoints));
        Execute(connection,transaction,"INSERT INTO robustness_deletion_receipts VALUES($id,$evaluations,$windows,$parameters,$costs,$runs)",( "$id",receipt),("$evaluations",preview.RobustnessEvaluations),("$windows",preview.RobustnessWindows),("$parameters",preview.RobustnessParameterPoints),("$costs",preview.RobustnessCostPoints),("$runs",preview.RobustnessRunReferences));
        transaction.Commit(); return receipt;
    }

    internal string ReplayChecksum(string revisionId, DateOnly from, DateOnly to)
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open(); using var command = connection.CreateCommand();
        command.CommandText = "SELECT instrument_id,session_date,close FROM observations WHERE revision_id=$revision AND session_date BETWEEN $from AND $to ORDER BY instrument_id,session_date";
        command.Parameters.AddWithValue("$revision", revisionId); command.Parameters.AddWithValue("$from", Date(from)); command.Parameters.AddWithValue("$to", Date(to));
        var builder = new StringBuilder(); using var reader = command.ExecuteReader(); while (reader.Read()) builder.Append(reader.GetString(0)).Append('|').Append(reader.GetString(1)).Append('|').Append(reader.GetString(2)).Append('\n');
        return Sha(Encoding.UTF8.GetBytes(builder.ToString()));
    }

    internal EodhdRuntimeEvidence RuntimeEvidence()
    {
        using var connection = new SqliteConnection(ConnectionString); connection.Open();
        var attempts = Scalar(connection, "SELECT COUNT(*) FROM acquisitions");
        var successes = Scalar(connection, "SELECT COUNT(*) FROM acquisitions WHERE outcome='success'");
        var failures = Scalar(connection, "SELECT COUNT(*) FROM acquisitions WHERE outcome='failure'");
        var retries = Scalar(connection, "SELECT COALESCE(SUM(retries),0) FROM acquisitions");
        var observations = Scalar(connection, "SELECT COUNT(*) FROM observations");
        var revisions = Scalar(connection, "SELECT COUNT(*) FROM revisions");
        var payloads = Scalar(connection, "SELECT COUNT(*) FROM payloads");
        var successfulSymbols = Strings(connection, "SELECT DISTINCT provider_symbol FROM acquisitions WHERE outcome='success' ORDER BY provider_symbol");
        var failedSymbols = Strings(connection, "SELECT DISTINCT provider_symbol FROM acquisitions WHERE outcome='failure' ORDER BY provider_symbol");
        var revisionIds = Strings(connection, "SELECT revision_id FROM revisions ORDER BY revision_id");
        var missingPayloadFiles = Strings(connection, "SELECT path FROM payloads ORDER BY path").Count(path => !File.Exists(path));
        var from = OptionalDate(connection, "SELECT MIN(session_date) FROM observations");
        var to = OptionalDate(connection, "SELECT MAX(session_date) FROM observations");
        return new(attempts + retries, attempts, successes, failures, retries, observations, revisions, payloads,
            from, to, successfulSymbols, failedSymbols, revisionIds, HasCausalKnowledgeTimes(connection), missingPayloadFiles);
    }

    private FinanceRetentionSummary Retention(SqliteConnection connection, bool accountActive)
    {
        var observations = Scalar(connection, "SELECT COUNT(*) FROM observations"); var revisions = Scalar(connection, "SELECT COUNT(*) FROM revisions"); var payloads = Scalar(connection, "SELECT COUNT(*) FROM payloads");
        var featureValues = Scalar(connection, "SELECT COUNT(*) FROM feature_values"); var featureRevisions = Scalar(connection, "SELECT COUNT(*) FROM feature_revisions");
        var backtestRuns=Scalar(connection,"SELECT COUNT(*) FROM backtest_runs");var backtestEvents=Scalar(connection,"SELECT COUNT(*) FROM backtest_events");var backtestFills=Scalar(connection,"SELECT COUNT(*) FROM backtest_fills");var backtestEquity=Scalar(connection,"SELECT COUNT(*) FROM backtest_equity");
        var evaluations=Scalar(connection,"SELECT COUNT(*) FROM robustness_evaluations");var windows=Scalar(connection,"SELECT COUNT(*) FROM robustness_windows");var parameterPoints=Scalar(connection,"SELECT COUNT(*) FROM robustness_parameter_sensitivity");var costPoints=Scalar(connection,"SELECT COUNT(*) FROM robustness_cost_sensitivity");var runReferences=Scalar(connection,"SELECT COUNT(*) FROM robustness_run_references");
        string? receipt = null; using (var command = connection.CreateCommand()) { command.CommandText = "SELECT receipt_id FROM deletion_receipts ORDER BY deleted_utc DESC LIMIT 1"; receipt = command.ExecuteScalar() as string; }
        var deadline = _options.EntitlementEndsAtUtc?.AddMonths(1); var state = receipt is not null && observations == 0 ? FinanceRetentionState.DeletionComplete :
            accountActive ? FinanceRetentionState.Active : deadline is null ? FinanceRetentionState.Unknown : DateTimeOffset.UtcNow > deadline ? FinanceRetentionState.ExpiredBlocked : FinanceRetentionState.DeletionRequired;
        return new(state, _options.EntitlementEndsAtUtc, deadline, observations, revisions, payloads,
            "raw payloads, normalized observations, market revisions, derived feature values/revisions and catalog indexes", receipt,
            featureValues, featureRevisions,backtestRuns,backtestEvents,backtestFills,backtestEquity,evaluations,windows,parameterPoints,costPoints,runReferences);
    }

    private static int Scalar(SqliteConnection connection, string sql) { using var command = connection.CreateCommand(); command.CommandText = sql; return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture); }
    private static List<string> Strings(SqliteConnection connection, string sql)
    { using var command = connection.CreateCommand(); command.CommandText = sql; using var reader = command.ExecuteReader(); var values = new List<string>(); while (reader.Read()) values.Add(reader.GetString(0)); return values; }
    private static DateOnly? OptionalDate(SqliteConnection connection, string sql)
    { using var command = connection.CreateCommand(); command.CommandText = sql; return command.ExecuteScalar() is string value ? DateOnly.Parse(value, CultureInfo.InvariantCulture) : null; }
    private static bool HasCausalKnowledgeTimes(SqliteConnection connection)
    {
        using var command = connection.CreateCommand(); command.CommandText = "SELECT session_date,acquired_utc FROM observations";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var marketTime = new DateTimeOffset(DateOnly.Parse(reader.GetString(0), CultureInfo.InvariantCulture).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var knowledgeTime = DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture);
            if (knowledgeTime < marketTime) return false;
        }
        return true;
    }
    private static string Text(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Date(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static string Sha(byte[] value) => $"sha256:{Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant()}";
    private static void Execute(SqliteConnection connection, SqliteTransaction? transaction, string sql, params (string Name, object Value)[] values)
    { using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = sql; foreach (var value in values) command.Parameters.AddWithValue(value.Name, value.Value); command.ExecuteNonQuery(); }
}

internal sealed class EodhdAcquisitionWorker(EodhdFinanceOptions options, EodhdMarketMemory memory, SystemRecoveryCoordinator recovery) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await recovery.WaitUntilRecoveredAsync(stoppingToken);
        if (!recovery.MayStartTimeSensitiveWork) return;
        if (!options.Enabled || !options.AccountActive || string.IsNullOrWhiteSpace(options.ApiToken) ||
            !EodhdEntitlement.AllowsAcquisition(options, DateTimeOffset.UtcNow)) return;
        if (options.EntitlementEndsAtUtc is { } end && end <= DateTimeOffset.UtcNow) return;
        using var adapter = new EodhdAdapter(options); var to = DateOnly.FromDateTime(DateTime.UtcNow); var from = to.AddYears(-1);
        foreach (var instrument in EodhdCatalog.Watchlist)
        {
            if (!memory.ShouldAcquire(instrument.ProviderSymbol, to)) continue;
            var started = DateTimeOffset.UtcNow;
            var acquisitionId = memory.RecordStarted(instrument, from, to, started);
            try { var result = await adapter.FetchAsync(instrument.ProviderSymbol, from, to, stoppingToken); memory.Store(instrument, result.Bars, result.Payload, from, to, started, DateTimeOffset.UtcNow, result.Retries, acquisitionId); }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidDataException or TaskCanceledException)
            { memory.RecordFailure(instrument, from, to, started, exception.GetType().Name, acquisitionId); }
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}

internal sealed class EodhdFinanceObservationReader(EodhdFinanceOptions options, EodhdMarketMemory memory) : IFinanceObservationReader
{
    public FinanceObservationSnapshot GetSnapshot() => memory.Snapshot(options.Enabled, !string.IsNullOrWhiteSpace(options.ApiToken), options.AccountActive);
}

internal static class EodhdMaintenanceCommand
{
    internal static bool TryRun(string[] args, IConfiguration configuration)
    {
        if (args.Length == 0 || !(args[0].StartsWith("finance-eodhd-", StringComparison.Ordinal) ||
            args[0].StartsWith("finance-features-", StringComparison.Ordinal) ||
            args[0].StartsWith("finance-backtests-", StringComparison.Ordinal) ||
            args[0].StartsWith("finance-robustness-", StringComparison.Ordinal))) return false;
        var options = new EodhdFinanceOptions(); configuration.GetSection(EodhdFinanceOptions.Section).Bind(options);
        var memory = new EodhdMarketMemory(options);
        if (args[0] == "finance-features-build")
        {
            var result = memory.BuildFeatures(args.Length>1?args[1..]:null);
            Console.WriteLine($"feature-revision={result.RevisionId} source-revisions={result.SourceMarketRevisions.Count} values={result.ValueCount} available={result.AvailableCount} warmup={result.WarmupCount} quality-issues={result.QualityIssueCount} checksum={result.Checksum} elapsed-ms={result.BuildElapsedMilliseconds} idempotent={result.Idempotent}");
            return true;
        }
        if (args[0] == "finance-backtests-build")
        {
            var result = memory.BuildReferenceBacktests();
            Console.WriteLine($"feature-revision={result.FeatureRevisionId} market-revisions={string.Join(',', result.MarketRevisionIds)} runs={string.Join(',', result.RunIds)} checksums={string.Join(',', result.Checksums)} sessions={result.Sessions} instruments={result.Instruments} feature-reads={result.FeatureReads} fills={result.Fills} events={result.Events} elapsed-ms={result.ElapsedMilliseconds} idempotent={result.Idempotent}");
            return true;
        }
        if(args[0]=="finance-robustness-build")
        {
            var result=memory.BuildRobustnessEvaluations();
            Console.WriteLine($"feature-revision={result.FeatureRevisionId} market-revisions={string.Join(',',result.MarketRevisionIds)} evaluations={string.Join(',',result.EvaluationIds)} checksums={string.Join(',',result.Checksums)} unique-runs={result.UniqueBacktestRuns} windows={result.EvaluationWindows} parameter-variants={result.ParameterVariants} cost-variants={result.CostVariants} elapsed-ms={result.ElapsedMilliseconds} idempotent={result.Idempotent}");return true;
        }
        if (args[0] == "finance-eodhd-runtime-evidence")
        {
            var evidence = memory.RuntimeEvidence();
            Console.WriteLine($"requests={evidence.ExternalRequests} attempts={evidence.AcquisitionAttempts} successes={evidence.SuccessfulAttempts} failures={evidence.FailedAttempts} retries={evidence.Retries}");
            Console.WriteLine($"observations={evidence.Observations} revisions={evidence.Revisions} payloads={evidence.Payloads} missing-payload-files={evidence.MissingPayloadFiles} coverage={evidence.CoverageFrom:yyyy-MM-dd}..{evidence.CoverageTo:yyyy-MM-dd}");
            Console.WriteLine($"successful-symbols={string.Join(',', evidence.SuccessfulSymbols)} failed-symbols={string.Join(',', evidence.FailedSymbols)} causal-knowledge-times={evidence.CausalKnowledgeTimes}");
            foreach (var revision in evidence.RevisionIds)
            {
                var first = memory.ReplayChecksum(revision, evidence.CoverageFrom ?? DateOnly.MinValue, evidence.CoverageTo ?? DateOnly.MaxValue);
                var second = memory.ReplayChecksum(revision, evidence.CoverageFrom ?? DateOnly.MinValue, evidence.CoverageTo ?? DateOnly.MaxValue);
                Console.WriteLine($"replay revision={revision} deterministic={first == second} checksum={first}");
            }
            return true;
        }
        var preview = memory.PreviewDeletion();
        if (args[0] == "finance-eodhd-deletion-preview")
        {
            Console.WriteLine($"preview={preview.PreviewId} observations={preview.Observations} revisions={preview.Revisions} payloads={preview.Payloads} feature-values={preview.FeatureValues} feature-revisions={preview.FeatureRevisions} backtest-runs={preview.BacktestRuns} backtest-events={preview.BacktestEvents} backtest-fills={preview.BacktestFills} backtest-equity={preview.BacktestEquityPoints} evaluations={preview.RobustnessEvaluations} evaluation-windows={preview.RobustnessWindows} parameter-points={preview.RobustnessParameterPoints} cost-points={preview.RobustnessCostPoints} run-references={preview.RobustnessRunReferences} deadline={preview.DeadlineUtc:O}");
            return true;
        }
        if (args[0] == "finance-eodhd-deletion-execute" && args.Length == 2)
        {
            var receipt = memory.ExecuteDeletion(preview, $"DELETE {args[1]}", DateTimeOffset.UtcNow);
            Console.WriteLine($"deletion-complete receipt={receipt} observations={preview.Observations} revisions={preview.Revisions} payloads={preview.Payloads} feature-values={preview.FeatureValues} feature-revisions={preview.FeatureRevisions}");
            return true;
        }
        throw new ArgumentException("Use finance-eodhd-deletion-preview or finance-eodhd-deletion-execute <preview-id>.");
    }
}
