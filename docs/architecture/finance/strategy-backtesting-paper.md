# Finance strategy, backtesting and paper methodology

## Strategy contract

Initial research candidates are momentum, trend following, breakout, mean reversion,
volume confirmation and liquidity/spread filters. None is presumed profitable. A common
conceptual `StrategySignal` carries strategy/version, instrument, timestamp, direction,
score, proposed entry, invalidation, stop, target, evidence, regime and data version.

Several signals may contribute to a Candidate Trade, but agreement is evidence only.
The Decision Engine cannot authorize execution; hard Risk policy remains authoritative.
Regimes may include trending, range-bound, high/low volatility, abnormal conditions and
insufficient liquidity, with reduced exposure or no trading when appropriate.

M1 implements the provider-neutral observation/context input and versioned signal output.
The reference strategy used by tests is fixture-only and produces deterministic evidence;
it is not a profitability claim. Paper order/fill records are descriptive domain types,
not an execution engine.

## Backtesting

The engine must replay reproducible datasets deterministically, version parameters and
model fees, spread, slippage, exchange/tax-like fees where applicable, FX, partial fills,
rejections and delay. Reports distinguish gross from net return. A strategy profitable
before costs but unprofitable after costs fails validation.

Metrics include total/annualized return where meaningful, win/loss rate, average win/loss,
expectancy, profit factor, maximum drawdown, volatility, risk-adjusted return, exposure,
trade count, turnover, transaction costs and net return, with benchmark comparison.

Validation must prevent look-ahead and data leakage, document survivorship risk and
unrealistic fills, separate training/validation/out-of-sample periods, use walk-forward
where appropriate, and examine parameter, regime and cost sensitivity. Monte Carlo or
sequence-risk analysis is used when informative. Historical profitability never
guarantees future profitability.

BB-045 must keep raw prices and corporate actions separate from adjusted replay views,
bind every report to an immutable dataset version and use availability timestamps rather
than later corrections. A current-survivor-only universe cannot qualify a strategy unless
the survivorship limitation is explicit and accepted.

The canonical identity foundation keeps renamed, inactive and delisted instruments
historically addressable. Replay must resolve provider symbols using each session date,
never today's ticker. The M2 fixture replay now consumes explicit session knowledge and
availability timestamps, keeps gap classifications distinct and emits corporate actions
without rewriting raw bars. It is not the M3 strategy/portfolio/fill/cost backtest engine.
Production exchange-calendar coverage and cross-revision correction replay remain pending;
adjusted observations remain explicitly classified and are not raw evidence.

The revision catalog now supplies the knowledge boundary required by future backtests:
select the latest immutable revision available at the declared `ReplayAsOf`, then retain
that exact revision ID in evidence. A later correction never changes an older revision or
an earlier as-of selection. M3 still must bind the chosen assembled membership into replay
and report configuration; this slice adds no strategy or simulation behavior.

## Paper trading and Strategy Lab

Paper trading uses live or near-live observations but persistent simulated cash,
positions, orders, fills, P&L, costs, delays, rejections and partial fills. It is
restart-safe, auditable and attributed by strategy. It must run long enough to produce
representative evidence; profitable-day count alone is not acceptance.

Strategy Lab compares versioned evidence and uses lifecycle states EXPERIMENTAL,
BACKTESTED, PAPER, APPROVED, ACTIVE, SUSPENDED and RETIRED. Recent performance alone can
never promote a strategy.

## Evidence and learning boundary

The controlled path is `COLLECT → MEASURE → BACKTEST → VALIDATE → PAPER TRADE → REVIEW
→ PROMOTE → LIVE`. Dataset, feature, parameter and model versions are immutable inputs to
evidence; measuring new outcomes never changes an active strategy automatically. Derived
features/results retain raw dataset provenance and remain subject to the provider policy.

Validation explicitly addresses multiple testing/data mining and selection bias in
addition to overfitting, survivorship, look-ahead, leakage, regime change, costs and
slippage. Promotion requires current lifecycle evidence, Risk policy and explicit owner
approval. See [market-data memory and provenance](market-data-memory-and-provenance.md).
