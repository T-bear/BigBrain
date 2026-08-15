# Finance strategy, backtesting and paper methodology

BB-084 reruns the unchanged BB-080 strategies only against exact WIKI revision
`wiki-5713d7dccfa38f56` and feature revision `feature-3833eb92bb641e51`. No WIKI/EODHD
continuous series exists. Robustness model v3 caps walk-forward work at the accepted 64-run
budget and versions the semantic change; results remain research evidence, not PAPER authority.

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

The persistence manifest/checksum now gives future M3 reports a verifiable storage-level
revision reference. Range reads require an exact revision ID and preserve canonical dates;
later corrections remain separate child revisions. The benchmark exercises sequential and
instrument-range access shapes only—it does not implement strategy logic or relax the
supplied replay-as-of boundary.

BB-079 supplies the first immutable derived input boundary. `core-daily-v1` binds exact
market revisions, versioned feature definitions and engine version into one deterministic
feature revision. Future M3 runs must cite both market and feature revision and filter
features by causal knowledge time; a fully materialized later table is never universally
visible to an earlier replay horizon. This is feature evidence only, not a signal or fill.

BB-080 implements the first bounded M3 engine. The strategy sees a completed daily bar, features with `knowledgeTime <= decisionTime`, and simulated portfolio state, then returns only `NO_ACTION`, `TARGET_LONG` or `TARGET_FLAT`. An intent after session T may fill only at the next available session open. `zero-cost-v1` is diagnostic; `conservative-cost-v1` explicitly models USD 0.01/share, USD 1 minimum and 5 bps adverse slippage. Initial sizing divides capital equally across the exact universe and floors to whole shares without borrowing. Immutable results report gross/net return, annualization where valid, drawdown, volatility, a zero-risk-free daily/252 Sharpe-like ratio, trade/exits, turnover, costs and SPY-universe benchmark comparison. This one-year raw-OHLC/current-survivor evidence is engineering validation only.

## Prospective shadow evaluation

Forward evidence complements historical replay: at time T an immutable prediction records
only then-known observations, feature/strategy/configuration/risk/build versions, score,
direction, horizon, hypothetical entry, regime and reasons. At or after T+horizon a separate
outcome may append return, maximum favorable/adverse excursion, volatility, hypothetical
fees/spread/slippage and calibration evidence. The original prediction is never rewritten.

Initial fixture metrics cover count, wins/losses, win rate, average/median/expected net
return, excursion and outcome volatility, isolated by full strategy version. Production
evaluation must additionally cover hypothetical-equity drawdown and cohorts by signal
strength, calibration, regime, instrument and horizon. Win rate alone is never sufficient:
tail losses, costs and risk-adjusted expectancy dominate frequency.

Capital compounding is a research objective, not an assumption. Finance may investigate
whether repeated positive net expectancy survives fees, spread, slippage, tax/operational
effects, losses and tail risk. It must never encode “many trades imply profit” or a promised
daily return. Survival and risk-adjusted positive expectancy take precedence over frequency.

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

## BB-081 chronological evaluation governance

`chronological-oos-walk-forward/v1` keeps train earlier than embargo and test, never shuffles time and evaluates fixed parameters in each expanding walk-forward window. The 50-session default embargo matches `core-daily-v1` maximum lookback. SMA and momentum neighborhoods plus the five-point cost ladder are bounded diagnostics, not selection. `transparent-robustness-score-v2` publishes weighted components, while minimum train/test/window requirements have absolute precedence. Evaluation identity, all run references and checksum are immutable; changing test rows cannot change train or an earlier completed window.
