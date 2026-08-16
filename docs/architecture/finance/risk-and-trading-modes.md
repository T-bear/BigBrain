# Finance risk, exits, compounding and trading modes

## Crash-reconciliation promotion invariant

Future LIVE/external orders must enter `RECONCILIATION_REQUIRED` after unclean restart until
local journal, broker orders, positions and account truth reconcile. Persistent idempotency
and duplicate prevention must be proven. BB-083 grants no execution authority.

## Security promotion gate

A comprehensive automated-security and controlled penetration-testing baseline is mandatory before
real-money LIVE eligibility and is a strong gate before meaningful PAPER/execution authority.
Potentially destructive tests must use isolated environments/test data. This planned gate includes
Web/API/auth boundaries, injection/browser controls, containers/Sentinel privilege and replay,
Finance hostile dataset/provenance/revision/mode-gate inputs, recovery/persistence tampering and
supply-chain risk. Passing it cannot itself promote a mode; product-owner authorization remains.

## Hard Risk Engine

BB-089 implements bounded RESEARCH policy `research-eod-v1`. Verdicts are `ALLOW` (hypothetical
research passes), `REDUCE` (bounded to cap), `DENY` (invalid/unsafe), `HALT` (circuit breaker) and
`INSUFFICIENT_DATA` (required evidence unavailable). Results are deterministic and immutable.
Defaults are hypothetical research capital 100,000 USD, 5% per-instrument cap, 10% maximum request,
15% daily-move cap, 8% maximum population standard deviation of 20 simple daily returns, volume
ratio 0.10 minimum, 3% simulated daily-loss halt, 10% rolling 20-session drawdown halt and three
consecutive losses. These are conservative safety assumptions, not tuned profitability claims.

Friday EOD remains fresh during weekends; freshness counts completed weekdays. Reliable spread,
sector metadata and a coherent hypothetical portfolio are absent, so those rules are explicitly
`NOT_EVALUABLE`. System halt is durable and recovery is audited. Future broker invariant: no
execution request may reach an adapter without a current matching risk evaluation, proposal
identity, policy/evidence version, permitted mode and execution gates. BB-089 implements no broker.

The Risk Engine is authoritative below AI and strategies. Versioned policy must cover
maximum capital and percentage risk per trade, total/instrument/sector exposure,
concurrent positions, daily loss, rolling drawdown, consecutive losses, liquidity,
spread, expected reward/risk, allowed instruments/markets, trading hours, data age,
abnormal volatility, broker/API health, suspension and emergency stop.

If `dailyLoss >= configuredDailyLossLimit`, the conceptual state becomes
`TRADING_DISABLED` until policy-defined recovery and explicit authorization conditions
are satisfied. Denial is safe and zero-trade days are valid.

## Exit invariant

Selling is as important as buying. A position cannot open without a defined, validated
exit model unless an explicitly reviewed strategy and policy safely permit otherwise.
Exit mechanisms may include stop loss, profit target, trailing stop, signal invalidation,
time/volatility exit, Risk Engine forced exit and emergency liquidation policy.

## Modes and transitions

```text
RESEARCH → PAPER → MANUAL_APPROVAL → LIMITED_AUTO → AUTO
    └────────────── any operational mode ──────────────→ HALTED
```

Transitions toward greater authority require satisfied evidence gates and explicit
product-owner action. PAPER can never call a live-order endpoint and can never silently
become live. HALTED prevents new exposure and strategy-originated orders, cancels only
eligible pending orders according to policy, may preserve safe exits, and requires
explicit owner action to resume.

`SHADOW` is an observation behavior inside RESEARCH, not a trading-authority mode and not a
new step in the authority ladder. It may evaluate a versioned test/research strategy and
record a hypothetical signal/outcome, but cannot create an order, paper intent, position or
capital allocation. Prospective evidence does not promote PAPER, change risk limits or
deploy a strategy automatically.

## Compounding

`capital_next = capital_current + realized_net_profit_loss` is the conceptual equity
update. Position sizing may use current risk-adjusted equity: sizes can grow gradually
when equity rises and must shrink when equity falls. Compounding never justifies a
higher percentage risk. All sizing uses net results and current hard limits.

Compounding remains a long-term objective to investigate, never a promised outcome. Signal
frequency, win rate or a nominal daily-return target cannot substitute for positive net
expectancy after costs, drawdown and tail-risk evidence. Survival takes precedence.
