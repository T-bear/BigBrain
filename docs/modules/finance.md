# Finance module

## BB-126 owner-controlled dataset drop — 2026-09-01

The host path configured by `FINANCE_MARKET_DATA_DROP_PATH` is mounted read-only at `/finance-data/market-data-drop` in the API. An explicit `<dataset>.ready` marker claims a stable top-level CSV or ZIP-contained CSV; bytes are content-addressed and copied into the existing Finance quarantine before parsing. Same bytes and sidecar are idempotent, while changed evidence produces a distinct candidate.

The existing dataset catalog is the owner-readable result surface. It separates technical quality, historical identity mapping, overlap, provenance, rights and promotion eligibility. Sidecar statements remain unverified claims, `UNKNOWN` remains fail-closed, and all-pass owner evidence stops at `READY_FOR_EXPLICIT_PROMOTION_REVIEW`; no automatic canonical promotion was added. See the [owner runbook](../operations/runbooks/finance-owner-market-data-drop.md).

## BB-125 zero-cost historical-source qualification — 2026-09-01

Current first-party evidence leaves no candidate qualified for a bounded pilot. SimFin Free and
Alpaca Basic/free are `HUMAN CONFIRMATION REQUIRED`; Yahoo Finance/yfinance and Tiingo Starter are
not eligible for BigBrain's automated persistent canonical archive. Tiingo Starter explicitly
forbids durable storage, Yahoo requires prior permission for automated collection, and the
open-source yfinance client grants no Yahoo data rights. No provider request, account, credential,
adapter, candidate, canonical revision, runtime change or deployment occurred.

The existing entitlement/provenance boundary and BB-084 13-gate quarantine/promotion chain remain
authoritative. EODHD Free and WIKI stay independent; no provider histories are stitched. Finance
remains `RESEARCH / 0 SEK / NONE`. See the
[BB-125 report](../reports/features/finance/finance-bb-125-zero-cost-market-data-qualification-20260901.md).

## BB-124 anti-overfitting governance — 2026-09-01

`chronological-oos-walk-forward/v2` reuses BB-081 and reserves immutable chronological train, validation and holdout partitions with a 50-session embargo at both boundaries. Candidate parameters are all retained; selection uses validation evidence only; holdout is evaluated once after the rule is frozen. Existing persisted evaluations make repeated use explicit as `CONTAMINATED` rather than fresh OOS evidence. Insufficient partitions remain `INSUFFICIENT_DATA` with holdout `UNTOUCHED`.

The bounded `family-breadth-fail-closed-v1` rule requires broad positive validation behavior and a positive median before the selected candidate may reach holdout. It emits no p-value and is not DSR/PBO. Deterministic engineering controls cover no-signal noise, future-knowledge leakage, winner selection among noise, regime fragility and a deliberately causal positive series. All use BB-123's versioned conservative execution assumptions. `research-integrity-v2` requires both selection PASS and a fresh-at-selection/evaluated holdout. None of these states authorizes PAPER, LIVE, AUTO or execution.

## BB-123 deterministic execution realism — 2026-08-31

Historical research backtests now use `daily-next-session-open-v2` with an immutable `next-session-open-full-fill/v2` contract. Costs separately record fixed per-fill, per-share/minimum and proportional commissions; assumed full-spread; and adverse slippage. Spread is explicitly assumed because daily OHLCV contains no quotes. Buy fills add half-spread plus slippage; sells subtract them. Cash, whole shares, turnover, gross/net equity and benchmark behavior remain in the existing engine.

An intent may fill only on the exact next US session. Missing/invalid bars, zero quantity, insufficient cash, unavailable sale state and end-of-data produce durable rejection attempts rather than a later favorable fill. Daily aggregate volume is retained as input evidence but does not justify intraday participation or partial-fill claims, so fills remain explicitly full-liquidity assumptions. Old v1 runs remain readable and unchanged. BB-081's existing ladder consumes the same v2 contract; it is not a second simulator. Finance remains `RESEARCH / 0 SEK / NONE`.

## BB-122 historical security identity boundary — 2026-08-31

A deterministic 30-ticker WIKI feasibility cohort produced 3 `VERIFIED`, 17 `PARTIAL`, 5 `AMBIGUOUS` and 5 `UNRESOLVED` identities. SEC/issuer and exchange filings can establish selected issuer, venue, CIK and effective-event intervals, but current exchange directories cannot back-prove 2014–2016 membership. Price appearance/disappearance remains price coverage only—not IPO, listing, delisting, universe membership or corporate-action evidence.

The existing `CanonicalInstrument` and effective-dated `ProviderInstrumentMapping` model can represent a bounded mapping and already fails closed on missing/overlapping resolution. It has no durable structured CIK/source-document/event evidence chain, and WIKI intake remains deliberately bound to approved watchlist identities. BB-122 therefore assigns `STATE B`, adds no second security-master/provenance system and performs no promotion. `wiki-5713d7dccfa38f56` remains immutable at 3,722 rows/five instruments. Any future expansion requires a separately approved evidence-persistence design; Finance remains `RESEARCH / 0 SEK / NONE`.

## BB-121 WIKI recovery boundary — 2026-08-31

The retained WIKI artifact is `STATE A`: large and useful locally, so it must not be downloaded again. It is nevertheless only a 2014-01-02–2016-12-19 snapshot. Its 3,186 ticker strings and 2,155,310 accepted rows are price-history evidence; they do not themselves establish canonical security identity, venue history, listing/delisting, historical universe membership or point-in-time knowledge.

The five-symbol result is intentional. `FinanceDatasetIntakeStore` evaluates the whole candidate but promotes only rows whose ticker has an exact `EodhdCatalog.Watchlist` mapping. AAPL, JNJ, JPM and MSFT each contribute 748 raw snapshot rows and XOM 732; SPY/QQQ/IWM have zero. The resulting 3,722-row revision already covers all locally available WIKI sessions for every safely mapped symbol. Expanding to the other 3,181 tickers would require explicit historical instrument/venue mappings and a new immutable candidate/promotion decision; ticker text is not sufficient identity evidence.

All mandatory candidate gates remain authoritative. WIKI's public-domain/provenance, schema, OHLCV, duplicate-conflict, explicit survivorship limitation, corporate-action columns, retention and insufficient-overlap classification passed. The full artifact's `SurvivorshipUnknown` limitation remains; disappearing/appearing ticker strings are coverage observations only. Adjusted history remains unusable under BB-090's `ADJUSTED_SEMANTICS_INVALID` audit, while raw research remains reproducible.

Stooq remains `HUMAN CONFIRMATION REQUIRED`. Public historical pages do not constitute permission for automated acquisition, persistent private storage, backtesting, derived evidence or retention, and upstream supplier notices do not resolve those rights. No Stooq artifact or adapter is active.

## BB-120 historical evidence qualification — 2026-08-31

The authoritative external-file path remains `ExternalDatasetCandidate → QuarantineArtifact → ValidationEvidence → PromotionDecision → CanonicalDatasetRevision`. It already supports bounded CSV and ZIP-contained CSV, multi-year evidence, SHA-256 revisions, restart/idempotency, OHLCV-v1 and `cross-source-comparison-v1`; all 13 `dataset-promotion-v1` gates must pass. Generic gates cover integrity, license, provenance and entitlement/retention. Equity-specific gates cover field/price-basis semantics, session/date-time, OHLCV, duplicates, symbol mapping, survivorship, corporate actions and source overlap. Unknown rights or semantics remain manual review and cannot become canonical.

The current EODHD product is `Free`: 0 SEK, 20 calls/day and approximately one year of EOD history. Current first-party documentation places deeper history and Bulk EOD in paid products. The existing private non-commercial research/storage grant remains active-subscription scoped; redistribution is prohibited and post-expiry deletion remains mandatory. Free access to delisted instruments or separate splits/dividends capabilities is not proven and is therefore `UNKNOWN`/disabled. No paid capability was activated.

BB-120 found no missing technical capability needed for a qualified longer-OHLCV source and no newly qualified source. WIKI remains the sole promoted external-file revision; the Zenodo/Yahoo-derived artifact remains manual review. SEC EDGAR and Eurostat are credible zero-cost first-party sources for filings/reference and macro/statistical evidence respectively, but neither is a longer daily-OHLCV source compatible with this intake contract. FRED/ALFRED, Riksbank and ECB are already implemented behind source-specific macro adapters. No acquisition, pilot, canonical mutation, code change or deployment occurred.

## BB-119 authoritative readiness semantics — 2026-08-31

Finance readiness is a conjunction, not one vague `READY` flag. The read model separately exposes whether durable historical market/feature evidence exists; whether the pinned scheduler date is an eligible market session; whether all eight configured instruments are exact-session complete; whether an exact compatible feature lineage exists; whether scientific, resource, recovery/operations, maintenance and single-flight gates permit a run. The scheduler may run only when every required gate passes.

On a non-research day, historical evidence remains valid while `currentSessionRequired=false`, `requiredResearchDate=null`, current-session readiness is `NOT_REQUIRED_NON_RESEARCH_DAY`, feature lineage is `NOT_REQUIRED` and current instrument count is not applicable. On an eligible session, `universeIncomplete`, `featuresNotReady` and `featureLineageIncomplete` retain their existing fail-closed meanings. Operations consumes this same live projection rather than reverse-engineering data readiness from the latest opportunity reason.

BB-119 also makes Sentinel restart-safe for its configured Unix socket. Sentinel remains the only source for CPU, memory and configured-disk evidence; Finance invents no metrics and the governor still applies `BLOCK > DEFER > ALLOW`. No research run is triggered by status inspection.

Runtime verification at 2026-08-31 13:00 UTC confirmed historical evidence available, a non-research day with no required current session/lineage/universe count, matching operations semantics, no active run and governor `ALLOW` from a healthy fresh CPU/memory/configured-disk snapshot. Sentinel, API and Web were healthy; opportunities remained 10 and autonomous runs/experiments remained 0.

## BB-118 current deployed baseline — 2026-08-31

Finance is deployed in `RESEARCH` with budget `0 SEK` and execution authority `NONE`. Real trading is impossible: there is no broker, order contract, paper executor, LIVE mode or AUTO trading. Autonomous research is not autonomous trading; signals, candidate evidence, backtests, shadow predictions and BB-089 risk verdicts are research evidence only.

The current implemented chain is provider/entitlement-aware EODHD Free SQLite/WAL memory and immutable replay; `core-daily-v1` features; deterministic buy-and-hold/SMA10-20/momentum20 backtests; chronological OOS/embargo/walk-forward/parameter and cost sensitivity; WIKI plus FRED/Riksbank/ECB macro/vintage evidence; prospective shadow outcomes and research-only risk evaluations; bounded autonomous-research, scheduler, resource-governor and operations/recovery foundations. BB-089 delivers a research-only risk foundation, not the complete execution-grade BB-053 Hard Risk Engine. Paper execution, portfolio/trading controller, broker security adapter, execution verification/reconciliation, manual/live/limited-auto/AUTO and Brain trading integration remain unimplemented.

Read-only appliance inspection at 2026-08-31 12:04 UTC found: recovery healthy/clean; EODHD cadence enabled/healthy with last success and latest canonical session 2026-08-28; maintenance pause false; operations waiting/no attention/no active run; scheduler enabled/not running with 10 opportunities and latest `Skipped / nonResearchDay`; 0 autonomous runs and 0 experiments; governor `Defer / metricsUnavailable`. Persistent evidence counts are 105 market revisions, 29,890 observations, 16 feature revisions, 797 backtests, 25 robustness evaluations, 288 shadow predictions, 240 outcomes and 240 risk evaluations. Latest feature revision has 44,520 values; aggregate feature-value count is not exposed. Schema version is 93 (1/90/91/92/93). Exact current entitlement-end/deletion deadline is not exposed by the responsive status endpoints; the canonical active-account/subscription-only EODHD policy and one-month post-termination deletion duty remain authoritative.

Known current inconsistency: scheduler readiness reports `dataReady=false`, `universeIncomplete` and 0/8 instruments while operations reports `dataReadiness=READY`; governor separately lacks metrics and defers. Resolve this read-model contradiction before relying on unattended eligibility. Do not trigger research to diagnose it. The next owner decision is either A) strengthen scientific evidence/anti-overfitting first (recommended because evidence remains insufficient), or B) continue BB-053 execution-grade Hard Risk foundation while remaining RESEARCH-only.

The sections below preserve the chronological implementation history. Earlier statements such as provider absent, synthetic-only, startup-only cadence or scheduler default-off are historical slice truth and not the current appliance state.

BB-088 replaces the startup-only acquisition/shadow passes with one lightweight prospective cadence.
After BB-083 recovery, it checks local state every 30 minutes; EODHD requests are allowed only on
weekdays after 22:00 UTC and at most one successful provider cycle per UTC day. The existing adapter
caps retries at two with exponential backoff. Every cycle safely rebuilds deterministic features,
evaluates already-genuine outcomes and inserts eligible new predictions exactly once. Weekend,
holiday/no-new-session and restart are normal states; clock failure blocks new temporal evidence.

Read-only `/api/v1/modules/finance/overview` and `/finance/cadence/status` drive a human hierarchy:
actual watched-market breadth, transparent current strategy agreement, prospective results, then
the existing technical evidence under Details & research. `TargetLong` maps to POSITIVE and
`TargetFlat` to NEGATIVE research posture; ties/disagreement map to NEUTRAL and remain visibly
counted. These are not recommendations or orders. Graphs require at least two evaluated source
sessions and use an explicitly labelled equal-weight shadow-decision basket, never actual P/L.

BB-087 adds a persistent prospective shadow journal over approved EODHD current EOD. After BB-083 recovery and clock sanity, an idempotent source-state worker evaluates unchanged strategies against causal `core-daily-v1` features. Predictions pin knowledge cutoff and revision/version lineage; a later eligible source session appends an outcome. Old sessions whose future is already knowable are never labelled prospective.

Read-only `/api/v1/modules/finance/shadow/{predictions,scorecard,status}` surfaces and the Shadow research panel distinguish pending/evaluated prospective evidence from historical backtests and label small samples `BOOTSTRAPPING`. All records are `RESEARCH`; there is no broker, order, PAPER, LIVE/AUTO, recommendation or parameter-learning path. EODHD-derived shadow rows share provider deletion scope and are not placed in indefinite public-domain backup.

BB-086 adds no runtime capability: the bounded SPY/QQQ/IWM search failed closed before acquisition.
WIKI lacks all three ETF tickers; candidate host licenses did not cure underlying Yahoo/PiTrading/
undocumented exchange provenance, and Stooq remained technically blocked. Existing current EOD,
WIKI archive, quarantine, revisions, derived evidence and backups are unchanged. Prospective
shadow observation remains future read-only RESEARCH and immutable predictions may not self-modify.

BB-085 adds `FinanceDataProtectionStore`, maintenance-only backup/verify/restore/corruption
commands and `GET /api/v1/modules/finance/backups`. Backup selection is by provider/product/
policy plus candidate rights/provenance, not by shared database or symbol. COMPLETE manifests
are the only restorable state. Web shows sanitized read-only protection status; no delete,
restore or arbitrary backup control is exposed.

BB-084 adds read-only dataset state and a trusted maintenance-only intake workflow. External
CSV/ZIP artifacts remain quarantined until all `dataset-promotion-v1` gates pass. Web/API users
cannot submit URLs. WIKI historical archive and EODHD current EOD remain separate revisions;
the feature engine defaults to EODHD-only or an explicitly selected exact revision set.

BB-083 gates automatic workers behind recovery. EODHD records request start durably; an
unfinished request becomes interrupted, publishes no partial revision and suppresses same-day
symbol retry. Derived builders consume committed state. Future LIVE must force
`RECONCILIATION_REQUIRED` after unclean startup; no execution authority exists.

Status: M1 plus real EOD/archive memory, derived research evidence and BB-085 provider-tagged
data protection are implemented and runtime-verified. Finance is
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
provider is selected or activated. BB-071 now contains direct human evidence that the
submitted private use has storage and post-termination retention rights on a qualifying
Twelve Data Personal plan. Basic/free is insufficient, so cost-first comparison must
resolve Alpaca Basic/free IEX before a paid choice.
ADR 0021's provider-neutral direction is Accepted; that acceptance does not activate a
provider. See [provider selection](../architecture/finance/market-data-provider-selection.md).

BB-072 re-evaluated ten zero-cost/free-adjacent source products on 2026-08-11. No exact
product passed the complete local-retention, personal non-display backtesting, derived-use
and termination gate. EODHD Free Starter is the best conditional evaluation lead but is
limited to one year and requires deletion after expiry; Twelve Data Basic is the strongest
Nordic technical lead but its exact rights remain incomplete. The current decision is
`DO NOT INGEST YET`; see the
[BB-072 report](../reports/features/finance/free-historical-data-source-research-20260811.md).

The consolidated 2026-08-11 historical/live gate conditionally prefers Twelve Data Basic
as one provider for a tiny US-only bootstrap/current experiment. Its 800 credits/day and
8/minute make rate allowance, not disk, the first bottleneck. This is not authorization:
durable retention, forward-testing/derived-artifact scope, exchange restrictions and
termination deletion require written evidence and explicit owner approval. Nasdaq Nordic
delayed files do not pass the value-added-use gate. See the
[combined evaluation](../reports/features/finance/finance-free-market-data-provider-evaluation-20260811.md).

The former BB-071 State B is superseded for Twelve Data Personal. Human evidence supports
the submitted local storage/retention, research/testing, post-termination raw/derived/audit
retention and owner-personal-funds data use. It does not cover Basic, redistribution,
commercial/paying-subscriber, customer/third-party or materially different use. Twelve Data
is an entitlement-cleared paid fallback only; see the
[human confirmation report](../reports/features/finance/finance-twelve-data-human-entitlement-confirmation-20260811.md).

Finance applies “collect once, reuse when permitted” and free-first/cost-aware selection.
Every raw dataset and derived artifact carries provider, dataset/revision, retrieval,
instrument, quality and entitlement provenance. Allowed purposes are explicit; unknown or
expired rights fail closed. Derived metrics are not presumed exempt from provider terms.
The full model, decision/outcome evidence graph and bounded self-hosted storage direction
are defined in [market-data memory and provenance](../architecture/finance/market-data-memory-and-provenance.md).

The current product-owner constraint is stricter: external market-data budget is exactly
**0 SEK** until explicitly changed. BB-075's fresh sweep did not clear any exact free source.
Alpaca Basic/free IEX and EODHD Free Starter require human clarification; no account, key,
adapter or real data exists. Twelve Data Personal remains an inactive paid fallback. The
production observation reader stays fail-closed and now names the current zero-cost gate
rather than the superseded BB-071 State B.

BB-076 adds a narrower product-owner risk-acceptance class for legitimate zero-cost,
private, read-only personal research. Authorization is capability-specific and records the
evidence class, source/product, rationale and owner-acceptance version. Explicit negative
terms, paid requirements, prior permission and technical access controls remain blocking.
The first Stooq daily-download smoke encountered a JavaScript verification control; it was
not bypassed, so production remains no-provider/no-real-data.

BB-077 adds the first provider implementation behind the existing read-only boundary:
current `EODHD Free`, limited to one year of daily EOD for the eight-symbol US watchlist.
It is disabled without an API token plus explicit active-account/enable flags. Provider JSON
stops at the adapter; SQLite WAL and content-addressed raw payloads preserve canonical IDs,
provider symbols, MIC, raw OHLC, adjusted close, split-adjusted volume, policy, acquisition
time, checksum and revision. The read model/UI labels it REAL EOD/delayed, never live.

Retention is first-class: active-account use is allowed; verified termination blocks new
acquisition/replay and starts a one-month deadline. Preview enumerates raw payloads,
normalized rows, revisions and indexes; exact owner confirmation executes scoped deletion
and retains only a sanitized receipt.

BB-078 activated the configured Free credential without disclosing it. Eight bounded EOD
requests populated the production volume with 2,008 real observations for the complete
watchlist, eight payloads and eight immutable per-source revisions covering 2025-08-11 to
2026-08-10. Restart left journal request count unchanged at eight and preserved the memory;
the API/UI now renders REAL EOD/delayed data. Sanitized maintenance evidence can report
catalog counts and replay checksums without provider payloads or secrets.

BB-079 adds the provider-neutral `core-daily-v1` feature engine above canonical memory.
Exact market revision IDs plus versioned definitions/engine create immutable SQLite feature
revisions; raw close/OHLC is never mixed with adjusted close, warmup is explicit and every
value preserves causal knowledge time. The first real revision
`feature-5d0397a53d094a2f` holds 42,168 values (39,616 available, 2,552 warmup) from all eight
BB-078 source revisions. `GET /api/v1/modules/finance/features` provides bounded read-only
definition/latest/history inspection with an optional UTC `knowledgeAsOfUtc` causal cutoff,
and Web displays a compact indicator subset. These are
measurements, not recommendations. EODHD retention/deletion covers dependent feature rows,
revisions and indexes.

BB-080 adds the first production research-backtest capability. `daily-next-session-open-v1` consumes only exact immutable market and feature revisions, causally visible features and current simulated cash/long position state. Buy-and-hold/v1, SMA crossover/v1 and momentum/v1 return research intents only. Whole-share equal initial allocation, explicit zero/conservative cost models and seed participate in immutable run identity. SQLite retains event journal, simulated fills, equity/drawdown and metrics; `/api/v1/modules/finance/backtests` and run detail are read-only. EODHD deletion covers all dependent backtest evidence. There is no mutation endpoint, broker order, PAPER/LIVE mode or recommendation.

BB-081 adds `chronological-oos-walk-forward/v1`. Exact lineage plus split/embargo, fixed walk-forward, bounded diagnostic grid, cost ladder, sufficiency thresholds and `transparent-robustness-score-v2` create immutable evaluation IDs/checksums. `/api/v1/modules/finance/robustness` and exact detail are bounded read-only surfaces. A 50-session embargo isolates the current maximum feature lookback; test evidence cannot influence train runs or parameter selection. SQLite owns evaluation/window/sensitivity evidence, all of which inherits EODHD deletion scope. Numerical score labels never override `INSUFFICIENT_DATA` and grant no trading authority.

BB-082 rechecked longer zero-cost history on 2026-08-12. No provider passed both the
entitlement and technical-access gates: Stooq presented a JavaScript verification control,
EODHD Free remained bounded to roughly one year, and qualifying Alpha Vantage/Nasdaq Data
Link history required payment. The module therefore has no second provider, cross-provider
merge, overlap state or long-history revision. Existing provider-specific EODHD retention
and all derived lineage remain unchanged.

Conceptual read capabilities include portfolio, positions, pending orders, market data,
trading/risk state, daily P&L and history. Future mutation capabilities may include an
order preview, exact approved submission, cancellation and position close. Names and
HTTP contracts are intentionally not frozen by M0.

## Decision and trust flow

BB-089 makes the first Hard Risk Engine slice operational for new prospective RESEARCH decisions.
Shadow prediction identity is preserved and linked to a separate immutable risk evaluation; a
positive signal denied by risk remains `TargetLong + DENY`, never rewritten to neutral. Policy and
evaluations are read through `/api/v1/modules/finance/risk/status`, `/risk/policy`,
`/risk/evaluations` and `/risk/evaluations/{id}`. All surfaces are GET-only. Existing BB-087
predictions predate this engine and are not retroactively labelled as contemporaneously approved.

ResearchCapital is deterministic hypothetical simulation capital, not account cash, portfolio
value or buying power. v1 caps one instrument at 5% and rejects requests above 10%; `REDUCE` emits
requested/allowed/risk-adjusted research exposure, never broker quantity. Aggregate portfolio,
sector and spread gates await trustworthy evidence. Risk evaluations inherit source retention;
sanitized halt audit may remain independently.

## BB-092 bounded autonomous research

`finance-research-signals-v1` is a code allowlist over existing immutable feature values; it stores no expressions or executable user code. `POST /api/v1/modules/finance/research/autonomous/run` accepts only an idempotency key and a server-clamped experiment limit of 1–3. `GET /api/v1/modules/finance/research/autonomous` returns the high-level snapshot and latest run. Bounded read-only history is available at `/research/autonomous/runs`, `/runs/{runId}`, `/experiments` and `/experiments/{experimentId}`; catalogs use offset/limit (maximum 100), deterministic newest-first ordering and allowlisted filters only.

Run start is a durable SQLite single-flight transaction: one global run may be `Running`, irrespective of idempotency key. A same-key call returns the original running, completed or failed result; a different key receives `409 finance.research.alreadyRunning` while the lease is occupied. On startup, stale work becomes a complete recovered `FAILED` audit record before new work can start. `research_run_experiments` preserves explicit run lineage for partial and reused experiments. Per-experiment parameter attempts are immutable and family attempt totals are their actual sum, distinct from experiment count. Global and per-run verdict counts separately report rejected, inconclusive, not evaluable, promising and challenger outcomes.

Evidence selection consumes only the exact IDs/checksums returned by the current robustness build. It verifies the latest feature revision, its complete normalized source-market lineage, persisted evaluation/result identity, relational completeness, and the approved `momentum/v1` plus `sma-crossover/v1` identities. Both current families must exist before experiment creation. Older evaluations are never substitutes; incomplete or conflicting current evidence returns `409 finance.research.currentEvidenceUnavailable` and remains a durable failed-run audit result.

## BB-093 bounded research scheduling

`Finance:ResearchScheduler` is explicitly disabled by default. When enabled, `finance-research-scheduler-v1` checks hourly for one 02:00 UTC opportunity covering the previous completed US market-session date. The hour follows the normal 22:00 UTC provider window; the scheduler itself performs no acquisition and does not touch prospective shadow cadence. It uses only the latest/current opportunity after downtime, so missed days never create a catch-up storm.

Each `finance-research-scheduler-v1:yyyy-MM-dd` identity has one durable SQLite journal row and is also the BB-092 idempotency key. Recovery/data-not-ready/manual-research-busy are bounded `Deferred` states; non-session dates are `Skipped`; current-evidence or research failure is durable `Failed`. Restart reconciles the linked BB-092 run without duplicating experiments. `GET /api/v1/modules/finance/research/scheduler/status` and `/history` are read-only; history uses offset/limit up to 100. See ADR 0034. Final continuous-operation hardening remains future work.

## BB-094 scheduled-research resource governor

Scheduled work passes `finance-research-resource-governor-v1` after recovery/readiness and before opportunity claim. It reuses the Sentinel-backed system-metrics abstraction and evaluates a single snapshot: CPU ≥80%, memory ≥85% or available memory below 1,024 MiB defers; configured-disk free space below 10 GiB defers and below 2 GiB blocks. Missing, stale (>5 minutes), future-dated or failed critical metrics defer fail-closed. Multiple sorted reasons and compact metrics are journaled with the opportunity; no telemetry stream is retained.

Temperature and service-activity gating are explicitly unsupported because no trusted current contract exists. The scheduler stays disabled by default, manual BB-092 runs remain outside this unattended-work gate, and fixed experiment limits are unchanged. `GET /api/v1/modules/finance/research/governor/status` is read-only. See ADR 0035.

## BB-095 autonomous research operations

`finance-research-operations-v1` derives operational state from cadence, readiness, scheduler journal, governor audit, BB-092 runs and System Recovery. It stores only low-frequency scheduler liveness/failure-streak fields and unique operational incidents. Three consecutive true operational failures require attention; scientific rejection and ordinary data/resource deferrals do not. A completed scheduled cycle resets the active streak without deleting history.

Startup reconciliation preserves every experiment and repairs Started opportunities from their existing deterministic run identity. Enabled scheduler inactivity beyond 180 minutes or a data/resource wait beyond 24 hours becomes degraded. `MaintenancePaused` is deployment configuration, false by default, and prevents new scheduled work without history deletion or catch-up. Status and incidents are read-only at `/api/v1/modules/finance/research/operations/status` and `/incidents`. Existing WAL, transactions and startup `quick_check(1)` remain authoritative; no periodic full integrity check or forced checkpoint was added. See ADR 0036.

The scheduler's readiness gate is universe- and lineage-complete. For the current BB-092 families, the research universe is the eight instruments in `EodhdCatalog.Watchlist`. Each must have canonical evidence exactly through the required US market session. The selected feature generation must cover the same date, contain feature rows for every instrument, and have a normalized source-revision set exactly equal to the canonical observations selected through that date. `universeIncomplete`, `featuresNotReady`, and `featureLineageIncomplete` remain bounded, retryable deferrals. The scheduler never invokes acquisition or feature generation. Older deferred opportunities become explicitly `Skipped/superseded` when a newer opportunity is considered; missed research is not backfilled.

`research-integrity-v1` requires sample, held-out OOS/excess, expanding walk-forward, explicit hypothetical costs, revision lineage, attempt accounting, complexity and the existing robustness result. DSR and PBO/CSCV remain honestly not evaluable. A Challenger has no prospective, champion, risk or execution authority. Macro, background scheduling and negative controls are not used in v1; see ADR 0033.

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

BB-074 implements an earlier read-only observation surface, distinct from the future
trading UI below. Finance owns `FinanceObservationSnapshot` and
`IFinanceObservationReader`; `GET /api/v1/modules/finance/observation` returns sanitized
RESEARCH safety, entitlement, watchlist, freshness/session/quality/data-kind and historical
memory metadata. `SafeDefaultFinanceObservationReader` returns no authorized provider,
BB-071 pending, denied ingestion/storage, no prices and no durable memory. The eight-symbol
US list is configured research scope, not authorization. Web renders explicit empty/error/
synthetic/stale/gap states and a textual, gap-aware SVG chart. It has no provider DTO,
credential, broker, order or mutation, and no synthetic runtime feed starts implicitly.
API and Web were deployed and technically runtime-verified 2026-08-11. Runtime returned the
safe default (RESEARCH, provider none, entitlement pending, all authority flags false,
eight unpriced watchlist entries, zero observations and no configured persistence), and
headless mobile/desktop verification found no external requests or blocking layout errors.
This is not a manual product-owner UI approval.

The UI will expose portfolio, equity curve, positions, pending orders, P&L, signals,
strategy performance, journal, risk and broker health. PAPER and LIVE must be visually,
semantically and accessibly unmistakable. A prominent STOP ALL TRADING control is
planned. Frontend state is never authoritative for mode, risk or execution.

See the [master roadmap](../architecture/finance/master-roadmap.md) and the proposed
[Finance boundary ADR](../adr/0017-finance-policy-governed-trading-boundary.md).
# BB-090 macro research boundary (2026-08-16)

Macro Memory is a separate Finance domain in the existing SQLite store, not an OHLCV extension and not an EODHD persistence responsibility. Pack v1 is `DFF`, `DGS2`, `DGS10`, `CPIAUCSL`, `UNRATE`; spread is derived. Observation/reference time and UTC knowledge time are distinct. Current-history CSV is revised-history exploratory unless an ALFRED vintage proves point-in-time availability. `macro-context-v1`, `market-regime-v1` and `us-equities-ny-v1` are deterministic/versioned. Macro context is explanatory only and cannot select or mutate strategies. Missing macro data degrades macro read models only.

BB-090 closure adds one ordered Finance schema coordinator (`1,90,91,92`), Macro candidate quarantine/evidence, and secure optional `Finance:Fred` configuration. Runtime environment names are `FINANCE__FRED__ENABLED` and secret `FINANCE__FRED__APIKEY`; read models disclose only the booleans. Official vintage acquisition is bounded to first-party `api.stlouisfed.org` and CPIAUCSL/UNRATE. `POINT_IN_TIME_CAUSAL` queries never fall back to revised history.

Finalization runtime evidence uses official `fred/series/observations` JSON `output_type=2`. Vintage columns encode series plus vintage date; Finance validates that identity, derives closed real-time intervals from successive columns and uses the next UTC day at 00:00 as conservative knowledge time because the authority supplies a date rather than a release hour. Production point-in-time revisions `fred-9300bb944c8c319d` (CPIAUCSL) and `fred-5800c2de8f2ef185` (UNRATE) each contain 36 bounded observations for reference periods 2020-01 through 2020-03 and coexist with the unchanged revised-history revision. Repeating the same acquisition returns the same revision IDs and counts.

Adjusted-price research requires an explicit revision capability. Production audit v1 found 24 EODHD revisions with valid raw+adjusted source semantics and one preserved WIKI revision (`wiki-5713d7dccfa38f56`) with invalid historical adjusted semantics; raw research remains valid. Hard Risk universe and strategy approval are Finance policy configuration rather than EODHD or strategy branches. UI risk is matched only by prediction ID and aggregates exact multiple verdicts conservatively without hiding disagreement.

# BB-091 European macro/FX boundary (2026-08-17)

Macro Memory migration 93 adds provider-neutral provider/region/unit/frequency and explicit FX base/quote metadata. The bounded packs are Riksbank `SECBREPOEFF`, `SEKEURPMI`, `SEKUSDPMI` and ECB `EXR.D.USD.EUR.SP00.A`, `EXR.D.SEK.EUR.SP00.A`, `FM.D.U2.EUR.4F.KR.MRR_FR.LEV`. `EUR/SEK` means SEK per EUR. Current-history bootstrap is `REVISED_HISTORY_EXPLORATORY`; only exact evidence-class as-of queries are allowed and causal requests never fall back. Acquisition remains an unscheduled maintenance capability.

## Future account/strategy isolation safeguard (BB-106)

Any future brokerage portfolio must bind an explicit account identity to its strategy objective, risk
budget, portfolio state, decisions, evaluation metrics and execution authority. Conservative active,
speculative and long-horizon child-savings mandates must never share mutable strategy/risk/performance
state merely because Finance presents them in one module. Reusable market observations may be shared
only with compatible provenance and time semantics; research/training evidence must retain account and
mandate identity. This is an architecture/backlog safeguard only: no account, broker, order, PAPER,
LIVE/AUTO or execution capability is introduced, and current `RESEARCH / 0 SEK / NONE` remains binding.
