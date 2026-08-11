# Finance market-data memory, provenance and learning foundation

Status: provider-neutral entitlement/provenance code implemented and verified; no external
data, adapter, persistence or runtime is implemented.
Governing decisions: Accepted ADR 0021 for market-data ownership/retention and Proposed
ADR 0020 for evidence and strategy governance.

## Principles

### Collect once, reuse when permitted

Finance should not repeatedly acquire or discard useful history when the exact entitlement
permits local retention. A licensed immutable dataset may be reused for analysis,
backtesting, walk-forward validation, PAPER evaluation, risk research, governed model
training and signal/decision/outcome comparison. Technical possession is never permission:
every use is checked against the dataset's current policy evidence, and unknown rights
fail closed.

### Free first and cost-aware

For external services and data, BigBrain normally evaluates: legally usable free sources,
self-hosted/open-source components, the cheapest sufficient paid product, then more costly
commercial products only for an evidenced need. Cost never outranks security, data
integrity, license compliance or capital/risk controls. Scraping outside terms, unreliable
live inputs and bypassing entitlements are prohibited. Provider-neutral domain contracts
allow a source to be replaced without rewriting strategy, risk or execution logic.

Free candidates such as Stooq or limited developer tiers remain candidates only. Free
access does not establish storage, corporate-action, derived-use or cancellation rights;
BB-071-quality evidence is required before their data can enter durable storage.

## Canonical market-data memory

The logical model is independent of transport, provider and physical database:

```text
EntitlementPolicy ── permits/denies ── DatasetRevision
                                      ├── InstrumentMapping
Provider import → RawObservation      ├── CorporateAction
                                      ├── QualityFinding
                                      └── DerivedArtifact → Evidence/Decision references
```

- **Instrument identity:** immutable BigBrain ID; venue/market MIC, currency and
  time-bounded symbol/provider mappings. A ticker alone is never identity.
- **Raw observation:** instrument, venue, timeframe, session/open/close timestamps,
  decimal OHLCV, currency, provider timestamp, retrieval timestamp and raw/adjusted flag.
- **Corporate action:** typed dividend, split or other action with announcement,
  ex/effective/payment timestamps as available, source and revision. Raw prices/actions
  remain separate; adjusted views are deterministic derived artifacts.
- **Dataset revision:** immutable dataset/import ID, parent/superseded revision, provider
  dataset/product, requested scope, adapter/schema version, checksum and completion state.
- **Quality finding:** valid, incomplete, stale, duplicate, gap, conflict, corrected,
  quarantined or rejected, with calendar/version evidence and non-secret reason codes.

Corrections append a revision and supersession link. They never silently overwrite a
dataset referenced by an evidence report. Replay identifies the exact dataset revision,
adjustment algorithm and calendar version.

## Provenance and entitlement envelope

Every persisted raw record, logical dataset and derived artifact inherits an entitlement
envelope containing at least:

- provider and provider dataset/type/product;
- retrieval time and provider/market observation time;
- canonical market/instrument identity and provider mapping;
- policy ID/version, governing terms/evidence reference and review date;
- retention class and allowed-use set;
- `Raw` or `Derived`, plus parent dataset/revision references;
- whether persistence and post-subscription retention are explicitly permitted;
- maximum retention/deletion deadline and deletion scope when known;
- provider correction/version markers and BigBrain import/schema version.

Allowed uses are explicit values, not free text: `LiveDisplay`, `HistoricalAnalysis`,
`Backtest`, `WalkForward`, `PaperTrading`, `StrategyTraining`, `DerivedMetrics` and
`LongTermStorage`. Policy answers one of `Allowed`, `Denied` or `Unknown` for a requested
use at a point in time. `Unknown`, expired evidence, missing policy, ambiguous product or
an unrecognized use always denies persistence/use. An account or successful API response
does not prove entitlement.

Deletion policy must distinguish raw observations, normalized copies, backups, corporate
actions, derived artifacts and audit metadata. If deletion is required, Finance stops new
uses, records a sanitized deletion event and removes all covered copies through a later
owner-approved procedure. It must not claim reproducibility for evidence whose licensed
inputs can no longer be retained. Non-sensitive entitlement/audit metadata may remain only
when the governing terms and policy allow it.

## Raw provider data and BigBrain-derived knowledge

Provider raw data includes responses, canonicalized observations and corporate actions
that can reproduce the provider facts. BigBrain-derived data includes indicators,
features, volatility measures, signals, scores, decisions, backtest results, statistics
and model outputs. Every derived artifact records its algorithm/model/parameter version,
input dataset revisions, creation time and output checksum.

“Derived” is lineage, not a license exemption. A provider policy may deny creating,
retaining, reverse-engineerable or post-subscription derived artifacts. The entitlement
evaluator checks derived creation and each later use independently.

## Decision and outcome evidence graph

The append-oriented evidence chain is:

```text
market snapshot → signals → strategy evaluation → risk/policy evaluation → decision
  → order intent / paper order / no trade → execution → outcome → post-trade evaluation
```

Stable correlation/causation IDs connect timestamp, instrument, dataset/snapshot revision,
strategy and parameter version, signal/score, risk state, mode, decision/reason codes,
rejected alternatives, immutable order intent, execution/fill, fees/slippage and outcomes
at named horizons. Outcome records distinguish realized and unrealized results and retain
the valuation dataset/version. NO TRADE and REJECTED remain first-class evidence so later
analysis does not select only executed winners.

A query such as “how did strategy X perform after this signal in comparable conditions?”
must resolve declared feature/regime definitions, input entitlements, dataset versions,
decision population (including rejects/no-trades), cost model and outcome horizon. It may
not silently compare differently defined or no-longer-permitted evidence.

## Controlled learning governance

Finance follows `COLLECT → MEASURE → BACKTEST → VALIDATE → PAPER TRADE → REVIEW → PROMOTE
→ LIVE`. Collection may produce evidence; it cannot mutate an active strategy. Models,
features, parameters and strategies are immutable versioned candidates. Promotion requires
the existing lifecycle gates, Risk Engine constraints and explicit owner approval; rollback
suspends a version while preserving permissible evidence.

Validation must address out-of-sample and walk-forward performance, overfitting,
survivorship/look-ahead bias, data leakage, selection and multiple-testing/data-mining
bias, regime change, transaction costs and slippage. Historical success is not proof of
future profitability, and no automatic retraining or recent-performance promotion may
affect real money.

## Storage architecture direction

Finance owns its storage inside the modular monolith. Other modules use versioned Finance
capabilities and never read its tables/files. Start with repository-native, self-hosted
components and no new service: immutable content-addressed import files where justified,
plus a transactional metadata/catalog store following the established modullocal SQLite
pattern for the first bounded EOD allowlist. Schema migrations are explicit, versioned and
backed up; datasets and journals use stable IDs rather than database row identity.

This is a provisional implementation direction, not authorization to create storage now.
Before BB-045 chooses SQLite/blob layout, measure expected daily EOD volume, replay/query
patterns, backup/restore time, concurrent readers and deletion-policy needs. PostgreSQL,
columnar files or another engine requires measured pressure and an architecture review;
no paid service or new container is justified today.

Backups inherit each record's entitlement and deletion deadline. A backup is not a way to
evade retention. Restore tests must prove schema/data version compatibility, provenance,
checksum integrity and licensed deletion across primary copies and backups.

## Implementation gates and next safe slice

Provider-neutral work may proceed before BB-071: typed policy/usage/classification models,
an in-memory fail-closed entitlement evaluator, provenance envelopes, fixture-only dataset
revisions and invariant tests. No real provider payload may be persisted.

Implemented 2026-08-10: these types and evaluator now exist in
`src/BigBrain.Modules/Finance/MarketDataEntitlements.cs`. The evaluator binds requests to
exact provider/product scope and returns the source decision plus a stable reason while
effective use remains denied for missing, unknown, denied, mismatched or expired policy.
Tests are synthetic and deterministic. Persistence and external adapters remain absent.

Implemented 2026-08-11: `CanonicalMarketData.cs` adds stable Equity/ETF identity and
inclusive effective-date provider/product/symbol/MIC mappings. Synthetic daily decimal
OHLCV and separate dividend/split actions normalize deterministically while preserving
raw/adjusted basis, dataset revision, policy identity and source/retrieval provenance.
Overlaps, unknown mappings, invalid ranges, negative volume/currency mismatches and invalid
actions fail explicitly. Exact/conflicting duplicate outcomes are stable and non-overwriting.
Without an exchange-calendar implementation, expected closure, unknown missing observation
and provider gap remain separate concepts and are never guessed.

BB-071 remains the external gate for selecting an entitlement and retaining actual
provider data. BB-045 remains the ingestion/storage implementation and must not activate a
provider until BB-071 passes. The next recommended implementation is a small versioned
market-session/calendar abstraction and richer gap findings, then deterministic historical
replay over immutable synthetic revisions. Persistence selection and external adapter
activation remain separately gated.
