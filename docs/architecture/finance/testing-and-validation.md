# Finance testing and validation strategy

Required layers are unit, integration, simulation, backtest regression, property/invariant,
paper integration, broker sandbox, failure injection, reconciliation, security, UI,
accessibility, performance and long-running soak tests.

Critical invariants receive dedicated automated tests:

- Risk Engine cannot be bypassed by API, strategy, UI or Brain.
- A duplicate decision does not imply a duplicate order.
- Uncertain execution is reconciled and never blindly retried.
- HALTED prevents new exposure while policy-defined safe exits remain possible.
- PAPER cannot invoke any live-order adapter or endpoint.
- Frontend and AI cannot obtain broker credentials.
- Costs are included in net results and reproducible backtests cannot look ahead.
- Mode promotion needs current evidence and explicit product-owner authorization.

Test doubles must be structurally unable to reach live accounts. Sandbox tests require
separate non-live credentials and explicit scope. Live acceptance, when eventually
authorized, uses deliberately bounded capital and never runs as general CI.
