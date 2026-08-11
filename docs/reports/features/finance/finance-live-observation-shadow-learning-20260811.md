# Finance live observation and shadow learning foundation

## Metadata

- Date: 2026-08-11
- Scope: BB-073 provider-neutral live/current observation and prospective evidence foundation
- Implementation: synthetic fixture only
- Runtime/deployment: unchanged; not deployed
- Related commit: assigned on publication

## Status

Finance can now model current observations honestly, replay a deterministic synthetic feed,
produce a version-bound non-trading shadow prediction, append a later outcome and calculate
basic prospective metrics. The path is structurally broker-free and cannot create an order.
This is an evidence foundation, not a profitable strategy, live adapter, PAPER executor,
self-modifying system or trading authorization.

## Changes

`LiveMarketObservation` binds canonical/provider/product/symbol/MIC identity, session date,
snapshot/five-minute/daily granularity, decimal OHLCV, raw/adjusted state, dataset/stream and
ingestion-journal identity, policy/provenance, quality/correction and explicit freshness.
Event, provider, received and knowledge timestamps are separate UTC facts and causally
validated. Delayed evidence requires a positive declared delay and cannot be labelled
real-time.

`SyntheticLiveMarketFeed` orders supplied fixture deliveries by knowledge/delivery time,
sequence and stable ID without wall-clock waits. It represents observations, missing
observations, session boundaries and provider outages; late/out-of-order market event time,
duplicates and corrections remain visible evidence rather than silent repair.

`LiveObservationEntitlementGate` requires explicit HistoricalAnalysis, WalkForward,
StrategyTraining, DerivedMetrics and LongTermStorage permission plus persistence. Missing,
unknown, denied or mismatched scope fails closed. Only synthetic policy evidence was tested.

The explicit `TEST-SYNTHETIC-MOMENTUM-NON-TRADING` rule consumes only observations whose
knowledge time is at or before evaluation. `ShadowStrategyVersion` binds strategy/version,
configuration fingerprint, feature-set, risk-policy and build reference. A prediction binds
the exact dataset revision, stream, policy, observations, horizon, hypothetical entry and
reason; it has no order or broker field.

`InMemoryShadowLearningJournal` appends immutable predictions and separately appends one
immutable outcome after the declared horizon. Outcome calculation records gross return,
maximum favorable/adverse excursion, volatility, hypothetical cost and net return without
rewriting the prediction. Metrics are isolated by the full strategy-version key and report
signal count, wins/losses, win rate, average/median/expected net return, excursion and
outcome volatility. Future metrics must add drawdown and grouping by strength, regime,
instrument and horizon before promotion evidence can exist.

## Evidence

- 16 focused tests passed.
- Full solution: 344 API/module tests plus 32 Sentinel tests, 376/376 passed.
- Tests cover four-clock semantics, delay labels, deterministic delivery and out-of-order
  event time, duplicate/correction preservation, session/missing/outage events, no-lookahead,
  versioned prediction evidence, immutable prediction and outcome, horizon enforcement,
  strategy-version metric isolation, catastrophic-loss impact, fail-closed entitlement,
  persistence/journal references, secret-free surfaces and absence of broker/order types.
- All data, timestamps, prices, policies and identifiers are deterministic fixtures.
- `dotnet restore BigBrain.slnx` — pass.
- `dotnet build BigBrain.slnx -c Release --no-restore` — pass, zero warnings/errors.
- `dotnet test BigBrain.slnx -c Release --no-build --no-restore` — pass, 376/376.
- `node scripts/verify-documentation.mjs`, `git diff --check` and
  `docker compose config --quiet` — pass.

## Security

Detta är en sanerad GitHub-version. No real provider observation, account, API key,
credential, HTTP/WebSocket connection, broker, order, PAPER/LIVE/AUTO promotion, runtime
mutation or deployment was created. Finance retains zero real-money authority.

## Remaining work

- BB-071 remains blocked; no external current or historical source is authorized.
- No production persistence or hybrid local-memory implementation exists.
- The fixture strategy is deliberately trivial and cannot qualify as strategy evidence.
- Feature calculation, drawdown/calibration/regime cohorts, strategy research, portfolio
  accounting, transaction-cost models and PAPER execution remain future milestones.
- Adaptive/self-modifying strategy or risk behavior requires a separate architecture and
  product-owner gate; this slice only observes, records, measures, compares and scores.

## Resumption

Resolve the exact Twelve Data Basic entitlement for US current observations and retained
prospective evidence. Until then, continue synthetic-only validation if needed. If complete
written rights are obtained, the next step is product-owner approval—not adapter work in the
same milestone. The first authorized live adapter remains separately gated.
