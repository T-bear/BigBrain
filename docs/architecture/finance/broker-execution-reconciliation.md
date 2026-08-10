# Finance broker, execution and reconciliation contract

## Evaluation before selection

No broker is selected in M0. Future evaluation covers Swedish availability, API and
sandbox, instruments, fees/spread, market data, order types, fractional support, rate
limits, authentication, account restrictions, reliability, legal/regulatory constraints
and terms for automated trading. Legal, tax, reporting, data-licensing and API-use
questions remain explicit research gates, not conclusions in this document.

The adapter maps a vendor API to stable Finance contracts. Strategy, Risk and UI never
depend on vendor models. Separate paper/live credentials are required where supported.

## Execution contract

An order preview binds instrument, side, type, quantity, limit/stop information, expected
cost/risk, expiry, portfolio/policy versions and identity. Submission accepts only an
authorized, unexpired, unchanged preview in a permitted mode. Idempotency prevents the
same decision from implying duplicate orders.

Request acceptance is not fill evidence. Verification observes broker order/fill state
and updates the append-oriented journal. On timeout or uncertain outcome, do not blindly
retry: mark uncertain, block conflicting actions and reconcile broker truth first.

## Reconciliation and failure modes

Periodic and event-driven reconciliation compares orders, positions, cash and fills.
Missing/unexpected orders, partial fills, stale positions, cash mismatch, rejection or
execution mismatch are explicit states. A material mismatch suspends automation.

Safe-failure design covers market-data outage/staleness, broker outage/timeout, duplicate
requests, rate limits, partial/rejected orders, app/host restart, network loss, clock and
timezone errors, strategy exceptions, storage failure/corruption and abnormal volatility.
Recovery preserves evidence and never assumes a trade did or did not occur.
