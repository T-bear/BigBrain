# Finance module

Status: planned; no Finance runtime, trading mode, broker connection or order capability exists.

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
