# Finance module

Status: M1 foundation implemented and automatically verified; not deployed. Finance is
read-only RESEARCH and has no broker connection, executor or external order capability.

Finance is a future first-party BigBrain module for research, simulation and eventually
policy-governed trading. It owns Finance domain state and must not read another module's
store. It starts inside the modular monolith; process isolation requires a later measured
security or workload need and an accepted ADR.

## Responsibilities

- Market Data and Market Observer normalize versioned historical/live observations.
- Strategy Engine produces deterministic, testable evidence.
- regime classification may enable, disable or weight strategies.
- Portfolio Engine models cash, positions, exposure and current equity.
- Risk Engine enforces hard policy below every proposer.
- Trading Controller is the only application boundary for order-related capabilities.
- Broker Adapter translates typed requests without exposing vendor contracts upstream.
- Execution Verification and reconciliation compare internal state with broker truth.
- Decision Journal records an append-oriented reconstruction chain.
- Finance UI presents state, previews, evidence, risk and emergency controls.

## Implemented M1 foundation

- `Money`, `Price`, `Quantity` and `Percentage` use `decimal` and reject invalid values.
  Currency is an explicit normalized three-letter value. Money rounds only through an
  explicit midpoint-to-even operation; domain values otherwise retain decimal precision.
- Instruments, venues, quote/OHLCV candles, timeframes and market observations are
  provider-neutral. Persistent/domain timestamps must use `DateTimeOffset` UTC; market,
  observation, evaluation and decision times remain separate for future latency metrics.
- `IFinanceStrategy` accepts verified observation/context and returns only a versioned
  evaluation/signal. It cannot return or submit an order.
- Risk and policy results are separate and default to `Missing`; missing or rejected data
  fails closed. A candidate becomes only a paper intent when both accept and mode is PAPER.
- The in-memory journal records NO TRADE and REJECTED as well as accepted paper intent,
  retaining observation, evaluation, decision and correlation identifiers.
- Paper order/fill, position and result are domain records only. No paper executor,
  persistence or external adapter exists.
- Finance is registered in the existing module registry with status `Research`, no widget
  and only `finance.research.read`.

The deterministic reference pipeline and fixture strategy exist to test architecture,
not as investment logic or evidence of profitability.

## Deliberately deferred

Persistence is deferred until its ownership, retention and journal integrity requirements
are designed. No migration or existing data store was changed. Custom Finance API/UI,
external market data, indicators, production strategies, full risk policy and paper
execution belong to later milestones.

## Market-data research baseline

BB-046 recommends daily raw OHLCV plus separately versioned corporate actions for a small
US/Nordic allowlist. Twelve Data is the primary EOD candidate because documented coverage
includes Nasdaq Stockholm; Tiingo and Massive remain US-specialized alternatives. No
provider is selected or authorized until BB-071 confirms storage and cancellation
retention rights. Public terms currently require deletion after cancellation for all
three shortlisted products, so written clarification or a different license is needed.
ADR 0021's provider-neutral direction is Accepted; that acceptance does not activate a
provider. See [provider selection](../architecture/finance/market-data-provider-selection.md).

Conceptual read capabilities include portfolio, positions, pending orders, market data,
trading/risk state, daily P&L and history. Future mutation capabilities may include an
order preview, exact approved submission, cancellation and position close. Names and
HTTP contracts are intentionally not frozen by M0.

## Decision and trust flow

```text
Strategy or AI proposes
        ↓
Risk Engine validates
        ↓
Policy permits or denies
        ↓
Trading Controller executes
        ↓
Broker Adapter communicates
        ↓
Execution Verification reconciles broker truth
```

Strategy agreement is evidence, never authorization. A successful API response is not
proof of execution. Uncertain results are reconciled and never blindly retried.

The future Autonomic layer uses Finance capabilities through the Trading Controller.
It cannot access broker credentials, the broker adapter or Finance storage directly,
and it cannot override risk, approval, mode or audit policy.

## Product behavior

The UI will expose portfolio, equity curve, positions, pending orders, P&L, signals,
strategy performance, journal, risk and broker health. PAPER and LIVE must be visually,
semantically and accessibly unmistakable. A prominent STOP ALL TRADING control is
planned. Frontend state is never authoritative for mode, risk or execution.

See the [master roadmap](../architecture/finance/master-roadmap.md) and the proposed
[Finance boundary ADR](../adr/0017-finance-policy-governed-trading-boundary.md).
