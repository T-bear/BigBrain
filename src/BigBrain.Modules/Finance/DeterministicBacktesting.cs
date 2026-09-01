using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BigBrain.Modules.Finance;

public enum ResearchIntentKind { NoAction, TargetLong, TargetFlat }
public sealed record ResearchStrategyIntent(ResearchIntentKind Kind, IReadOnlyList<string> ReasonCodes);
public sealed record BacktestMarketBar(InstrumentId InstrumentId, string MarketRevisionId, DateOnly SessionDate,
    decimal Open, decimal Close, DateTimeOffset KnowledgeTimeUtc, long? Volume = null);
public sealed record BacktestFeatureValue(InstrumentId InstrumentId, DateOnly SessionDate, string DefinitionId,
    decimal? Value, DateTimeOffset KnowledgeTimeUtc, string FeatureRevisionId);
public sealed record ResearchPortfolioView(decimal Cash, int Quantity, decimal MarkPrice, decimal Equity);
public sealed record ResearchStrategyContext(InstrumentId InstrumentId, DateOnly SessionDate,
    DateTimeOffset DecisionTimeUtc, decimal Close, IReadOnlyDictionary<string, decimal> Features,
    ResearchPortfolioView Portfolio);

public interface IResearchBacktestStrategy
{
    StrategyIdentity Identity { get; }
    IReadOnlyDictionary<string, decimal> Parameters { get; }
    ResearchStrategyIntent Evaluate(ResearchStrategyContext context);
}

public sealed class BuyAndHoldResearchStrategy : IResearchBacktestStrategy
{
    public StrategyIdentity Identity => new("buy-and-hold", "v1");
    public IReadOnlyDictionary<string, decimal> Parameters { get; } = new Dictionary<string, decimal>();
    public ResearchStrategyIntent Evaluate(ResearchStrategyContext context) => context.Portfolio.Quantity == 0
        ? new(ResearchIntentKind.TargetLong, ["benchmark.enter-on-first-known-bar"])
        : new(ResearchIntentKind.NoAction, ["benchmark.hold"]);
}

public sealed class SmaCrossoverResearchStrategy(int fast = 10, int slow = 20) : IResearchBacktestStrategy
{
    public StrategyIdentity Identity => new("sma-crossover", "v1");
    public IReadOnlyDictionary<string, decimal> Parameters { get; } = new Dictionary<string, decimal> { ["fastPeriod"] = fast, ["slowPeriod"] = slow };
    public ResearchStrategyIntent Evaluate(ResearchStrategyContext context)
    {
        if (!context.Features.TryGetValue($"sma.{fast}", out var fastValue) || !context.Features.TryGetValue($"sma.{slow}", out var slowValue))
            return new(ResearchIntentKind.NoAction, ["feature.warmup-or-unavailable"]);
        var desired = fastValue > slowValue ? ResearchIntentKind.TargetLong : ResearchIntentKind.TargetFlat;
        return new(desired, [fastValue > slowValue ? "sma.fast-above-slow" : "sma.fast-not-above-slow"]);
    }
}

public sealed class MomentumResearchStrategy(int period = 20) : IResearchBacktestStrategy
{
    public StrategyIdentity Identity => new("momentum", "v1");
    public IReadOnlyDictionary<string, decimal> Parameters { get; } = new Dictionary<string, decimal> { ["period"] = period };
    public ResearchStrategyIntent Evaluate(ResearchStrategyContext context)
    {
        if (!context.Features.TryGetValue($"momentum.{period}", out var value))
            return new(ResearchIntentKind.NoAction, ["feature.warmup-or-unavailable"]);
        return new(value > 0 ? ResearchIntentKind.TargetLong : ResearchIntentKind.TargetFlat,
            [value > 0 ? "momentum.positive" : "momentum.non-positive"]);
    }
}

public sealed record BacktestCostModel(string Id, string Version, decimal CommissionPerShare,
    decimal MinimumCommission, decimal SlippageBasisPoints, decimal FixedCommissionPerFill = 0,
    decimal ProportionalCommissionBasisPoints = 0, decimal AssumedFullSpreadBasisPoints = 0)
{
    public static BacktestCostModel Zero => new("zero-cost", "v1", 0, 0, 0);
    public static BacktestCostModel Conservative => new("conservative-cost", "v2", 0.01m, 1m, 5m,
        FixedCommissionPerFill: 0, ProportionalCommissionBasisPoints: 0, AssumedFullSpreadBasisPoints: 2m);
}

public sealed record BacktestFillModel(string Id, string Version, bool WholeShares, bool FullFillOnly,
    int MaximumExecutionDelaySessions)
{
    public static BacktestFillModel NextSessionOpen => new("next-session-open-full-fill", "v2", true, true, 1);
}

public sealed record BacktestRunConfiguration(IReadOnlyList<string> MarketRevisionIds, string FeatureRevisionId,
    StrategyIdentity Strategy, IReadOnlyDictionary<string, decimal> StrategyParameters, string SimulationModel,
    BacktestCostModel CostModel, decimal InitialCapital, IReadOnlyList<string> Universe, DateOnly From, DateOnly To,
    string SizingPolicy, int Seed,string? EvaluationContext=null, BacktestFillModel? FillModel=null,
    ResearchDatasetLineage? ResearchDatasetLineage=null);
public sealed record BacktestFill(string FillId, string InstrumentId, DateOnly IntentSession, DateOnly FillSession,
    string Side, int Quantity, decimal ReferenceOpen, decimal FillPrice, decimal Commission, decimal EstimatedSlippage,
    decimal CashBefore, decimal CashAfter, int PositionBefore, int PositionAfter, decimal EstimatedSpreadCost = 0);
public sealed record BacktestExecutionAttempt(string AttemptId, string InstrumentId, DateOnly IntentSession,
    DateOnly ExpectedFillSession, string Side, int RequestedQuantity, int FilledQuantity, string Status, string Reason);
public sealed record BacktestEvent(int Sequence, DateOnly Session, string InstrumentId, string Type,
    DateTimeOffset EventTimeUtc, DateTimeOffset KnowledgeTimeUtc, string? FeatureRevisionId,
    string? MarketRevisionId, ResearchIntentKind? Intent, string Detail, decimal Cash, int PositionQuantity,
    decimal MarkPrice, decimal Equity);
public sealed record BacktestEquityPoint(DateOnly Session, decimal Cash, decimal HoldingsValue,
    decimal TotalEquity, decimal Drawdown);
public sealed record BacktestMetrics(decimal InitialEquity, decimal FinalEquity, decimal GrossReturn,
    decimal NetReturn, decimal? AnnualizedReturn, decimal MaxDrawdown, DateOnly? MaxDrawdownDate,
    decimal Volatility, decimal? SharpeLikeRatio, int Trades, int WinningExits, int LosingExits,
    decimal Turnover, decimal TotalCommissions, decimal TotalEstimatedSlippage,
    decimal? BenchmarkReturn, decimal? ExcessReturn, decimal TotalAssumedSpreadCost = 0,
    int RejectedOrUnfilled = 0);
public sealed record BacktestResult(string RunId, string Checksum, BacktestRunConfiguration Configuration,
    BacktestMetrics Metrics, IReadOnlyList<BacktestFill> Fills, IReadOnlyList<BacktestEvent> Events,
    IReadOnlyList<BacktestEquityPoint> EquityCurve, string Status, IReadOnlyList<string> Limitations,
    IReadOnlyList<BacktestExecutionAttempt>? ExecutionAttempts = null);

public static class DeterministicBacktestEngine
{
    public const string SimulationModel = "daily-next-session-open-v2";
    public const string LegacySimulationModel = "daily-next-session-open-v1";
    public const string SizingPolicy = "equal-initial-capital-whole-shares-v1";

    public static BacktestResult Run(BacktestRunConfiguration configuration, IResearchBacktestStrategy strategy,
        IEnumerable<BacktestMarketBar> market, IEnumerable<BacktestFeatureValue> features,
        decimal? benchmarkReturn = null)
    {
        Validate(configuration, strategy);
        var bars = market.Where(x => configuration.Universe.Contains(x.InstrumentId.Value, StringComparer.Ordinal) &&
            x.SessionDate >= configuration.From && x.SessionDate <= configuration.To &&
            configuration.MarketRevisionIds.Contains(x.MarketRevisionId, StringComparer.Ordinal) && UsMarketCalendar.IsSession(x.SessionDate))
            .OrderBy(x => x.SessionDate).ThenBy(x => x.InstrumentId.Value, StringComparer.Ordinal).ToArray();
        if (bars.Length == 0) throw new ArgumentException("The exact pinned market revisions contain no bars in scope.", nameof(market));
        var featureRows = features.Where(x => x.FeatureRevisionId == configuration.FeatureRevisionId)
            .GroupBy(x => (x.InstrumentId, x.SessionDate)).ToDictionary(x => x.Key, x => x.ToArray());
        var positions = configuration.Universe.ToDictionary(x => x, _ => 0, StringComparer.Ordinal);
        var averageCosts = configuration.Universe.ToDictionary(x => x, _ => 0m, StringComparer.Ordinal);
        var lastMarks = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var pending = new Dictionary<string, (ResearchIntentKind Kind, DateOnly Session, DateOnly ExpectedFill, DateTimeOffset Knowledge)>(StringComparer.Ordinal);
        var fills = new List<BacktestFill>(); var attempts = new List<BacktestExecutionAttempt>(); var events = new List<BacktestEvent>(); var curve = new List<BacktestEquityPoint>();
        decimal cash = configuration.InitialCapital, commissions = 0, slippage = 0, spread = 0, turnover = 0, grossCash = cash;
        var grossPositions = positions.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        var realizedWins = 0; var realizedLosses = 0; decimal peak = configuration.InitialCapital; var sequence = 0;
        foreach (var sessionGroup in bars.GroupBy(x => x.SessionDate).OrderBy(x => x.Key))
        {
            foreach (var stale in pending.Where(x => x.Value.ExpectedFill < sessionGroup.Key).ToArray())
                Reject(stale.Key, stale.Value, "MISSING_NEXT_SESSION_BAR", sessionGroup.Key);
            var processed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var bar in sessionGroup)
            {
                var id = bar.InstrumentId.Value;
                if (pending.TryGetValue(id, out var request) && request.ExpectedFill == bar.SessionDate)
                {
                    pending.Remove(id); processed.Add(id);
                    var before = positions[id]; var buy = request.Kind == ResearchIntentKind.TargetLong && before == 0;
                    var sell = request.Kind == ResearchIntentKind.TargetFlat && before > 0;
                    var side = buy ? "ENTER_LONG" : "EXIT_LONG";
                    if (bar.Open <= 0)
                        RejectAttempt(id, request, side, 0, "INVALID_OPEN");
                    else if (!buy && !sell)
                        RejectAttempt(id, request, request.Kind == ResearchIntentKind.TargetLong ? "ENTER_LONG" : "EXIT_LONG", 0,
                            request.Kind == ResearchIntentKind.TargetFlat ? "POSITION_UNAVAILABLE" : "CONTRACT_STATE_CHANGED");
                    else
                    {
                        var sign = buy ? 1m : -1m;
                        var halfSpreadBps = configuration.CostModel.AssumedFullSpreadBasisPoints / 2m;
                        var spreadPrice = Round(bar.Open * sign * halfSpreadBps / 10_000m, 6);
                        var slippagePrice = Round(bar.Open * sign * configuration.CostModel.SlippageBasisPoints / 10_000m, 6);
                        var fillPrice = Round(bar.Open + spreadPrice + slippagePrice, 6);
                        var budget = configuration.InitialCapital / configuration.Universe.Count;
                        var requested = buy ? Math.Max(0, (int)Math.Floor(budget / fillPrice)) : before;
                        var quantity = requested;
                        if (buy)
                        {
                            while (quantity > 0 && quantity * fillPrice + Fee(quantity, fillPrice, configuration.CostModel) > cash) quantity--;
                        }
                        if (quantity > 0)
                        {
                            var fee = Fee(quantity, fillPrice, configuration.CostModel); var cashBefore = cash;
                            var estimatedSlippage = Round(Math.Abs(slippagePrice) * quantity, 6);
                            var estimatedSpread = Round(Math.Abs(spreadPrice) * quantity, 6);
                            cash += buy ? -(quantity * fillPrice + fee) : quantity * fillPrice - fee;
                            grossCash += buy ? -(quantity * bar.Open) : quantity * bar.Open;
                            grossPositions[id] = buy ? quantity : 0;
                            positions[id] = buy ? quantity : 0;
                            if (buy) averageCosts[id] = fillPrice + fee / quantity;
                            else { var pnl = (fillPrice - averageCosts[id]) * quantity - fee; if (pnl >= 0) realizedWins++; else realizedLosses++; averageCosts[id] = 0; }
                            commissions += fee; slippage += estimatedSlippage; spread += estimatedSpread; turnover += quantity * fillPrice;
                            var fillId = Hash($"{id}|{request.Session:yyyy-MM-dd}|{bar.SessionDate:yyyy-MM-dd}|{(buy ? "BUY" : "SELL")}|{quantity}|{fillPrice}|{fee}")[7..23];
                            fills.Add(new($"fill-{fillId}", id, request.Session, bar.SessionDate, buy ? "ENTER_LONG" : "EXIT_LONG", quantity,
                                bar.Open, fillPrice, fee, estimatedSlippage, cashBefore, cash, before, positions[id], estimatedSpread));
                            attempts.Add(new(AttemptId(id, request.Session, bar.SessionDate, side), id, request.Session,
                                bar.SessionDate, side, requested, quantity, "FILLED", "FULL_FILL"));
                            events.Add(new(++sequence, bar.SessionDate, id, "SIMULATED_FILL", AtOpen(bar.SessionDate), request.Knowledge,
                                configuration.FeatureRevisionId, bar.MarketRevisionId, request.Kind, fills[^1].FillId, cash, positions[id], bar.Open, 0));
                        }
                        else RejectAttempt(id, request, side, requested, requested == 0 ? "ZERO_QUANTITY" : "INSUFFICIENT_CASH");
                    }
                }
                lastMarks[id] = bar.Close;
            }
            foreach (var missing in pending.Where(x => x.Value.ExpectedFill == sessionGroup.Key && !processed.Contains(x.Key)).ToArray())
                Reject(missing.Key, missing.Value, "MISSING_NEXT_SESSION_BAR", sessionGroup.Key);
            foreach (var bar in sessionGroup)
            {
                var id = bar.InstrumentId.Value;
                var visible = featureRows.TryGetValue((bar.InstrumentId, bar.SessionDate), out var candidate) ? candidate
                    .Where(x => x.KnowledgeTimeUtc <= bar.KnowledgeTimeUtc).ToArray() : [];
                var featureMap = visible.Where(x => x.Value.HasValue).ToDictionary(x => x.DefinitionId, x => x.Value!.Value, StringComparer.Ordinal);
                var equityNow = cash + positions.Sum(x => x.Value * lastMarks.GetValueOrDefault(x.Key));
                var intent = strategy.Evaluate(new(bar.InstrumentId, bar.SessionDate, bar.KnowledgeTimeUtc, bar.Close, featureMap,
                    new(cash, positions[id], bar.Close, equityNow)));
                var transition = intent.Kind == ResearchIntentKind.TargetLong && positions[id] == 0 || intent.Kind == ResearchIntentKind.TargetFlat && positions[id] > 0;
                if (transition) pending[id] = (intent.Kind, bar.SessionDate, NextSession(bar.SessionDate), bar.KnowledgeTimeUtc);
                events.Add(new(++sequence, bar.SessionDate, id, "STRATEGY_INTENT", bar.KnowledgeTimeUtc, bar.KnowledgeTimeUtc,
                    configuration.FeatureRevisionId, bar.MarketRevisionId, intent.Kind, string.Join(',', intent.ReasonCodes), cash,
                    positions[id], bar.Close, equityNow));
            }
            var holdings = positions.Sum(x => x.Value * lastMarks.GetValueOrDefault(x.Key)); var equity = cash + holdings;
            peak = Math.Max(peak, equity); var drawdown = peak == 0 ? 0 : equity / peak - 1m;
            curve.Add(new(sessionGroup.Key, Round(cash), Round(holdings), Round(equity), Round(drawdown, 12)));
            events.Add(new(++sequence, sessionGroup.Key, "PORTFOLIO", "MARK_TO_MARKET", AtClose(sessionGroup.Key), AtClose(sessionGroup.Key),
                configuration.FeatureRevisionId, null, null, "daily equity", Round(cash), positions.Values.Sum(), 0, Round(equity)));
        }
        foreach (var remaining in pending.ToArray()) Reject(remaining.Key, remaining.Value, "END_OF_DATA", remaining.Value.ExpectedFill);
        var grossFinal = grossCash + grossPositions.Sum(x => x.Value * lastMarks.GetValueOrDefault(x.Key));
        var final = curve[^1].TotalEquity; var netReturn = final / configuration.InitialCapital - 1m;
        var grossReturn = grossFinal / configuration.InitialCapital - 1m; var daily = Returns(curve);
        var years = Math.Max(0, curve.Count - 1) / 252d; decimal? annualized = curve.Count >= 30 && years > 0 ? (decimal)(Math.Pow((double)(final / configuration.InitialCapital), 1d / years) - 1d) : null;
        var volatility = daily.Length == 0 ? 0 : (decimal)(Std(daily.Select(x => (double)x)) * Math.Sqrt(252));
        decimal? sharpe = daily.Length < 2 || volatility == 0 ? null : daily.Average() * 252m / volatility;
        var metrics = new BacktestMetrics(configuration.InitialCapital, final, Round(grossReturn, 12), Round(netReturn, 12), annualized is null ? null : Round(annualized.Value, 12),
            curve.Min(x => x.Drawdown), curve.OrderBy(x => x.Drawdown).First().Session, Round(volatility, 12), sharpe is null ? null : Round(sharpe.Value, 12),
            fills.Count, realizedWins, realizedLosses, Round(turnover), Round(commissions), Round(slippage), benchmarkReturn,
            benchmarkReturn is null ? null : Round(netReturn - benchmarkReturn.Value, 12), Round(spread),
            attempts.Count(x => x.Status != "FILLED"));
        var runId = "backtest-" + Hash(Canonical(configuration))[7..23];
        var provisional = new BacktestResult(runId, "", configuration, metrics, fills, events, curve, "RESEARCH",
            ["Raw OHLC is not corporate-action adjusted; results are engineering/research evidence only.", "One-year current-survivor universe is not robust validation.", "Spread and slippage are explicit simulation assumptions, not observed quotes or intraday execution evidence.", "Next-session-open fills assume full liquidity; daily volume is not used as proof of intraday capacity and partial fills are not modeled."], attempts);
        return provisional with { Checksum = Hash(JsonSerializer.Serialize(provisional, JsonOptions)) };

        void Reject(string instrumentId, (ResearchIntentKind Kind, DateOnly Session, DateOnly ExpectedFill, DateTimeOffset Knowledge) request,
            string reason, DateOnly eventSession)
        {
            pending.Remove(instrumentId);
            RejectAttempt(instrumentId, request, request.Kind == ResearchIntentKind.TargetLong ? "ENTER_LONG" : "EXIT_LONG", 0, reason, eventSession);
        }
        void RejectAttempt(string instrumentId, (ResearchIntentKind Kind, DateOnly Session, DateOnly ExpectedFill, DateTimeOffset Knowledge) request,
            string side, int requestedQuantity, string reason, DateOnly? eventSession = null)
        {
            attempts.Add(new(AttemptId(instrumentId, request.Session, request.ExpectedFill, side), instrumentId, request.Session,
                request.ExpectedFill, side, requestedQuantity, 0, "REJECTED", reason));
            var session = eventSession ?? request.ExpectedFill;
            events.Add(new(++sequence, session, instrumentId, "SIMULATED_REJECTION", AtOpen(session), request.Knowledge,
                configuration.FeatureRevisionId, null, request.Kind, reason, cash, positions[instrumentId], lastMarks.GetValueOrDefault(instrumentId),
                cash + positions.Sum(x => x.Value * lastMarks.GetValueOrDefault(x.Key))));
        }
    }

    private static void Validate(BacktestRunConfiguration c, IResearchBacktestStrategy strategy)
    {
        if (c.Strategy != strategy.Identity || c.InitialCapital <= 0 || c.Universe.Count == 0 || c.MarketRevisionIds.Count == 0 || c.From > c.To)
            throw new ArgumentException("Backtest configuration is invalid or does not match the strategy.");
        if (c.SimulationModel != SimulationModel || c.SizingPolicy != SizingPolicy || c.FillModel != BacktestFillModel.NextSessionOpen)
            throw new ArgumentException("Unsupported versioned simulation, fill or sizing model.");
        var cost=c.CostModel;
        if(cost.CommissionPerShare<0||cost.MinimumCommission<0||cost.SlippageBasisPoints<0||cost.FixedCommissionPerFill<0||
            cost.ProportionalCommissionBasisPoints<0||cost.AssumedFullSpreadBasisPoints<0||cost.SlippageBasisPoints+cost.AssumedFullSpreadBasisPoints/2m>=10_000m)
            throw new ArgumentException("Execution friction assumptions must be non-negative and preserve a positive sell fill price.");
        if (!c.StrategyParameters.OrderBy(x => x.Key).SequenceEqual(strategy.Parameters.OrderBy(x => x.Key))) throw new ArgumentException("Pinned strategy parameters do not match strategy instance.");
    }
    private static decimal Fee(int quantity, decimal fillPrice, BacktestCostModel model)
    {
        var variable = quantity * model.CommissionPerShare;
        var perShare = variable == 0 && model.MinimumCommission == 0 ? 0 : Math.Max(model.MinimumCommission, variable);
        var proportional = quantity * fillPrice * model.ProportionalCommissionBasisPoints / 10_000m;
        return Round(model.FixedCommissionPerFill + perShare + proportional, 6);
    }
    private static decimal[] Returns(IReadOnlyList<BacktestEquityPoint> curve) => curve.Zip(curve.Skip(1), (a, b) => a.TotalEquity == 0 ? 0 : b.TotalEquity / a.TotalEquity - 1m).ToArray();
    private static double Std(IEnumerable<double> source) { var x = source.ToArray(); if (x.Length == 0) return 0; var mean = x.Average(); return Math.Sqrt(x.Average(v => (v - mean) * (v - mean))); }
    private static decimal Round(decimal value, int places = 2) => Math.Round(value, places, MidpointRounding.AwayFromZero);
    private static DateTimeOffset AtOpen(DateOnly date) => UsMarketCalendar.Session(date)?.OpenUtc ?? throw new InvalidOperationException($"{date} is not a {UsMarketCalendar.Version} session.");
    private static DateTimeOffset AtClose(DateOnly date) => UsMarketCalendar.Session(date)?.CloseUtc ?? throw new InvalidOperationException($"{date} is not a {UsMarketCalendar.Version} session.");
    private static DateOnly NextSession(DateOnly date){do{date=date.AddDays(1);}while(!UsMarketCalendar.IsSession(date));return date;}
    private static string AttemptId(string instrumentId,DateOnly intent,DateOnly expected,string side)=>"attempt-"+Hash($"{instrumentId}|{intent:yyyy-MM-dd}|{expected:yyyy-MM-dd}|{side}")[7..23];
    private static string Canonical(BacktestRunConfiguration c) => JsonSerializer.Serialize(c with { MarketRevisionIds = c.MarketRevisionIds.Order(StringComparer.Ordinal).ToArray(), Universe = c.Universe.Order(StringComparer.Ordinal).ToArray(), StrategyParameters = c.StrategyParameters.OrderBy(x => x.Key).ToDictionary() }, JsonOptions);
    private static string Hash(string value) => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };
}
