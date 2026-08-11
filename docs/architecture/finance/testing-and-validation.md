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
- Missing, unknown, expired or mismatched entitlement denies persistence and every
  undeclared use.
- Derived artifacts preserve complete input lineage and cannot weaken raw-data policy.
- Provider corrections create new revisions; referenced evidence is never silently
  rewritten.
- Retention deletion covers normalized copies and backups according to explicit policy.
- Outcome analysis includes NO TRADE/REJECTED populations and declared horizons.

Test doubles must be structurally unable to reach live accounts. Sandbox tests require
separate non-live credentials and explicit scope. Live acceptance, when eventually
authorized, uses deliberately bounded capital and never runs as general CI.

## M1 coverage

The M1 regression suite uses only deterministic in-process fixtures. It covers decimal
precision and explicit rounding, currency/invariant failures, UTC/causal timestamps,
OHLCV validation, provider-neutral data, strategy/order separation, safe mode default,
missing/rejected risk and policy, NO TRADE/REJECTED journal entries, paper-only intent and
the complete correlation chain. It contains no network or credential configuration.

## BB-045 policy/provenance coverage

Synthetic domain tests cover exact allowed use, undeclared/unsupported use, explicit
denial, missing/not-current/provider-product-mismatched policy, raw and derived persistence,
post-subscription retention, required derived parents, raw lineage rejection, immutable
dataset revision state and deterministic results/reason codes. Fixtures contain no real
provider DTO or payload and no test opens a network, database or runtime connection.

## BB-045 canonical normalization coverage

Synthetic tests verify canonical identity across an exact ticker-change boundary, MIC
distinction, overlap and unknown-mapping rejection, decimal daily OHLCV ranges and volume,
dataset/policy preservation, explicit raw/adjusted basis, dividends, exact positive split
ratios, duplicate/conflict non-overwrite and repeated-input determinism. Tests are fast,
clock-independent, network-free and storage-free. A future calendar test layer must keep
expected closure, unknown missing observation and provider gap distinct.

## BB-045 session and historical replay coverage

Synthetic fixtures verify explicit Trading/Closed/Unknown session state, IANA timezone
conversion, winter/summer UTC offsets and fail-safe invalid/ambiguous DST handling. Replay
tests distinguish expected closure, generic missing observation, explicit provider gap and
invalid observation; bind one immutable revision; resolve provider symbols at each historical
date; keep raw bars and dividend/split events separate; honor supplied availability/range;
and prove monotonic, stable repeated ordering. No wall clock, host-local timezone, network,
filesystem, database, random input or sleep participates.
