# Finance testing and validation strategy

BB-072 is a documentation/research-only gate. It added no source code, provider payload or
executable test surface; documentation verification, whitespace validation and Compose
configuration validation are its applicable automated gates. External-data acceptance
tests remain forbidden until an exact provider/product is entitlement-cleared, selected and
explicitly approved for activation.

The later human Twelve Data BB-071 evidence update is also documentation-only. It supersedes
State B for a qualifying paid Personal plan but does not authorize Basic or activation.
Existing synthetic entitlement, duplicate/out-of-order/stale/session/outage/recovery/
correction/no-lookahead/no-order tests must remain green. No provider acceptance/network
test may run until written rights and separate product-owner approval exist.

The subsequent synthetic acquisition slice tests the pre-adapter entitlement boundary and
adapter-to-domain orchestration without network or storage. It covers deterministic request,
batch and pagination order; deny-before-adapter; synthetic-only policy identity; unauthorized
source/retention; identical retry and overlap; conflicting batch identity; correction
supersession; acquisition journal counts/deletion evidence; canonical normalization;
explicit gap/replay; immutable assembly; repeated replay/no-lookahead; and the absence of
credential/header fields. These tests do not constitute external-data acceptance evidence.

The persistence-foundation suite adds deterministic manifest serialization/checksums,
mutation detection, exact immutable roundtrip, idempotent duplicate append, conflict and
partial-manifest rejection, correction lineage, bar/action/gap queries, provider/policy
enumeration, scoped deletion receipts, entitlement preservation, replay compatibility and
revision-catalog no-lookahead. The standalone full benchmark deterministically compares
JSONL and SQLite at 2,520, 126,000 and 1,260,000 rows; timings are architectural samples,
not CI performance thresholds.

The BB-073 fixture suite verifies event/provider/received/knowledge causality, inclusive
knowledge availability, explicit delay labels, deterministic delivery independent of input
order, out-of-order market events, duplicate/correction preservation, session boundaries,
missing observations, provider outage, fail-closed multi-use entitlement, immutable shadow
prediction and separately appended horizon outcome, full strategy-version isolation,
tail-loss-aware metrics, persistence/journal references, no secret surface and no broker or
order property. It uses no wall clock, sleep, network, storage or random input.

Required layers are unit, integration, simulation, backtest regression, property/invariant,
paper integration, broker sandbox, failure injection, reconciliation, security, UI,
accessibility, performance and long-running soak tests.

BB-079 feature promotion specifically requires known-answer formula tests, explicit warmup/
missing/gap cases, immutable revision/idempotency/reopen checks and a future-horizon fixture
proving that values at T do not change when observations after T are appended. Runtime
verification builds only from local memory, keeps provider request count unchanged and
checks bounded API/UI plus source-entitlement deletion lineage. A stable snapshot alone is
not sufficient no-lookahead evidence.

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

## BB-045 immutable revision coverage

Synthetic tests distinguish market event time from UTC knowledge availability and prove the
inclusive `availability <= as-of` boundary. They cover original/corrected views, immutable
old revisions, explicit supersession lookup, multiple ordered corrections, unknown/mismatched
references, correction/revision cycles, ambiguous branches, scope changes, members unavailable
at revision creation, repeated assembly and input-order independence. Corporate actions,
historical ticker resolution and session/gap evidence retain temporal/provenance semantics.
