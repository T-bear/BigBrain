# Finance module

Status: M1 and provider-neutral BB-045 entitlement/identity/normalization/session/replay/revision/acquisition/persistence-manifest foundations
implemented and automatically verified; not deployed. Finance is
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

## Implemented BB-045 provider-neutral slice

- `MarketDataUse` defines LiveDisplay, HistoricalAnalysis, Backtest, WalkForward,
  PaperTrading, StrategyTraining, DerivedMetrics and LongTermStorage without loose strings.
- Entitlement policies bind ID/version, provider/product, safe evidence reference, UTC
  validity, per-use decisions, persistence, post-subscription retention and deletion.
- The evaluator returns effective allow/deny, original Allowed/Denied/Unknown, stable
  reason code and policy reference. Missing, unsupported, product-mismatched, expired,
  denied or unknown scope fails closed.
- Immutable dataset revisions and provenance envelopes bind checksum, adapter/schema,
  UTC source/retrieval time, instrument/venue, policy, quality and raw/derived lineage.
  Derived provenance requires input revisions and receives no automatic entitlement.
- `CanonicalInstrument` provides a stable BigBrain ID, Equity/ETF type, currency, venue,
  MIC, lifecycle and validity independent of any current ticker. Provider/product/symbol
  mappings use inclusive `valid_from`/`valid_to`; rename boundaries are historical and
  overlapping mappings for one instrument/product/MIC fail configuration.
- Synthetic input contracts normalize without IO into decimal daily OHLCV or separately
  typed cash-dividend/stock-split actions. Raw/adjusted basis, immutable revision and policy
  identity survive normalization; split ratios are exact positive rationals.
- Stable quality codes explain missing/ambiguous mappings, invalid ranges/volume/currency,
  duplicates and conflicts. Within one revision an exact duplicate is ignored with a
  finding; a conflicting duplicate is rejected and never overwrites the first observation.
- `MarketSession` separates local trading date from UTC instants and represents only
  explicit Trading, Closed or Unknown knowledge. Fixture calendars carry venue/MIC,
  timezone and evidence; `TimeZoneInfo` performs conversion and invalid/ambiguous DST
  local times fail rather than being guessed.
- Gap classification keeps ExpectedClosure, MissingObservation, ProviderGap,
  InvalidObservation and UnknownSession distinct. Only explicit provider-gap evidence may
  classify a generic absence as provider-specific; no prices or volumes are fabricated.
- Deterministic historical replay binds exactly one dataset revision and emits ordered
  session, quality, observation and corporate-action events inside a supplied UTC range.
  It uses observation availability timestamps and historical effective-date symbols;
  future observations/actions/mappings do not leak into an earlier replay window.
- Immutable revision assembly inherits members through an explicit parent, adds new facts
  append-only and replaces an active member only through a correction with original and
  replacement IDs, reason/evidence and exact UTC availability. Replacement preserves the
  logical observation identity while its immutable source-revision provenance remains.
- The catalog requires a linear acyclic supersession chain and selects the latest revision
  whose creation/availability is `<=` the supplied knowledge boundary. Exact old revision
  lookup never changes after later corrections. Member/correction output order is ordinal
  and independent of input collection order.
- `HistoricalDataAcquisitionRequest` and immutable acquisition batches bind future adapter
  output to exact provider/product, instrument/reference/MIC, date range, daily/raw-adjusted
  scope, UTC acquisition, timezone, pagination, provenance, policy and destination revision.
- The acquisition gate requires explicit HistoricalAnalysis, Backtest, DerivedMetrics,
  LongTermStorage and persistence permission before calling an adapter. Missing, unknown,
  denied or mismatched scope returns a stable denial. Fixture authorization is restricted
  to unmistakable `SyntheticFixture`/`Synthetic-`/`fixture:` evidence.
- The synthetic ingestion pipeline deterministically deduplicates identical batches and
  overlapping observations, rejects conflicting batch identity, calls the existing
  canonical normalizer, carries explicit gaps into replay evidence and delegates correction
  membership to the existing immutable assembler. Its journal contains policy/deletion,
  counts, findings and result revision, never authentication material.
- `HistoricalDatasetManifest` binds exact revision membership to deterministic SHA-256,
  schema/storage versions, scope and coverage, acquisition and entitlement evidence,
  counts, correction lineage and retention/deletion obligations without credentials or
  provider payloads. Only complete manifests may be appended.
- `IHistoricalDataPersistence` defines exact revision, range bar, corporate-action,
  quality/gap, lineage, integrity, provider/product/policy enumeration and scoped deletion
  operations. Its in-memory implementation is a fixture correctness reference: immutable,
  idempotent for identical input, conflict-rejecting and atomic at its public boundary.
- Scoped deletion removes affected fixture payload/revisions while preserving a sanitized
  receipt with scope, deleted revision IDs, manifest fingerprints and policy evidence. It
  neither deletes unrelated revisions nor retains prices/actions as an audit workaround.
- `LiveMarketObservation` represents snapshot/five-minute/daily current evidence with
  canonical/provider/symbol/MIC identity, decimal OHLCV, explicit freshness/delay, session,
  quality/correction, stream/revision, policy/provenance and acquisition-journal reference.
  Event, provider, received and knowledge time are separate UTC facts; causal ordering and
  delayed-versus-realtime labels fail closed.
- `SyntheticLiveMarketFeed` emits deterministic fixture deliveries without wall-clock wait
  and preserves out-of-order event time, duplicate/correction evidence, missing observations,
  session boundaries and provider outages. Its entitlement gate requires explicit analysis,
  walk-forward, training, derived, storage and persistence rights.
- The only shadow rule is named `TEST-SYNTHETIC-MOMENTUM-NON-TRADING`. It sees only evidence
  known at evaluation time and creates a strategy/config/feature/risk/build-bound immutable
  prediction. A separate outcome later records gross/net return, favorable/adverse excursion,
  volatility and hypothetical costs. Metrics never mix full strategy-version keys.

The domain implementation has no network, production persistence, durable logging or
provider SDK. The benchmark tool performs bounded temporary JSONL/SQLite IO using only the
synthetic `ExampleData`/`Synthetic-EOD-Personal` fixtures and deletes its temporary files.

Production exchange-calendar resolution is deliberately absent. Only supplied fixture
knowledge can establish trading or closure; unknown dates remain unknown.
Renamed, inactive or delisted instruments remain historically addressable by canonical ID;
future backtests must resolve the provider reference at the historical session date.

This replay primitive is not the M3 backtest engine: it has no strategy, portfolio,
position, fill, fee, slippage, P&L, risk expansion, paper executor or order capability.

The shadow-learning fixture is likewise not PAPER or a production strategy. “Learning” in
this phase means observe → record → measure → compare → score → build evidence. It cannot
rewrite strategies/risk, promote versions, deploy code, allocate capital or create orders.

## Measured persistence direction (not activated)

The verified model requires future storage to support append-oriented revision metadata,
immutable member bodies, parent/supersession links, original→replacement correction chains,
UTC event/availability indexes, canonical observation identity, provider/product/policy
provenance, deterministic revision membership queries and entitlement/deletion metadata.
The 2026-08-11 repeatable benchmark compared JSONL and SQLite at 2,520, 126,000 and
1,260,000 deterministic rows. At the largest scale JSONL used 155,992,420 bytes and wrote
in 793.331 ms but required 153.477 ms for a 100-row instrument range scan. SQLite used
356,208,640 bytes and wrote in 9,674.985 ms, while the indexed range query took 0.053 ms
and transactional publication protected completeness. These single-host measurements are
architectural evidence, not precision promises.

The recommended direction is immutable append-oriented payload files with content hashes,
plus SQLite for transactional manifest publication, indexes, lineage and deletion scope.
Confidence is medium: backup/restore, concurrency, cancellation deletion across backups and
the exact payload format still require bounded validation before an ADR or product store.
No Finance migration, production database or retained provider payload is activated.

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

BB-072 re-evaluated ten zero-cost/free-adjacent source products on 2026-08-11. No exact
product passed the complete local-retention, personal non-display backtesting, derived-use
and termination gate. EODHD Free Starter is the best conditional evaluation lead but is
limited to one year and requires deletion after expiry; Twelve Data Basic is the strongest
Nordic technical lead but its exact rights remain incomplete. The current decision is
`DO NOT INGEST YET`; see the
[BB-072 report](../reports/features/finance/free-historical-data-source-research-20260811.md).

Finance applies “collect once, reuse when permitted” and free-first/cost-aware selection.
Every raw dataset and derived artifact carries provider, dataset/revision, retrieval,
instrument, quality and entitlement provenance. Allowed purposes are explicit; unknown or
expired rights fail closed. Derived metrics are not presumed exempt from provider terms.
The full model, decision/outcome evidence graph and bounded self-hosted storage direction
are defined in [market-data memory and provenance](../architecture/finance/market-data-memory-and-provenance.md).

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

Finance does not freely “learn” into production. Evidence flows through collection,
measurement, backtest, validation, PAPER, review and explicit promotion. A new model,
parameter or strategy version has no live authority merely because historical metrics
improve.

## Product behavior

The UI will expose portfolio, equity curve, positions, pending orders, P&L, signals,
strategy performance, journal, risk and broker health. PAPER and LIVE must be visually,
semantically and accessibly unmistakable. A prominent STOP ALL TRADING control is
planned. Frontend state is never authoritative for mode, risk or execution.

See the [master roadmap](../architecture/finance/master-roadmap.md) and the proposed
[Finance boundary ADR](../adr/0017-finance-policy-governed-trading-boundary.md).
