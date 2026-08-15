# BB-087 prospective shadow observation foundation

## Metadata

- Date: 2026-08-15
- Baseline: `31156ec8e9780dc6622891cb4ba46050c302a6f6`
- Mode/budget: `RESEARCH`, 0 SEK; no broker, order, PAPER, LIVE or AUTO capability

## Status

Implemented, local regression passed, deployed and bounded-runtime/restart verified. Published CI evidence remains to be recorded after publication.

## Changes

Finance now has a durable SQLite prospective journal for approved EODHD Free `CURRENT EOD / PROSPECTIVE EOD` research. A source-state worker starts after BB-083 recovery, requires clock integrity, scans only canonical EODHD evidence and evaluates the unchanged `buy-and-hold/v1`, `sma-crossover/v1` and `momentum/v1` strategies. It creates no order and has no execution authority.

Prediction identity pins instrument, session, market revision, feature revision, strategy/version, parameter fingerprint, knowledge cutoff and `next-eligible-source-session-close-v1`. Repeated cycles use an idempotent unique insert. Features are selected only when their knowledge time is at or before cutoff. Observations older than the bounded current-session window, or sessions whose next source observation is already knowable, are not backfilled as prospective.

Outcomes are separate one-per-prediction rows. The original ID, cutoff, signal and lineage remain unchanged; only evaluation state advances. EODHD deletion scope and stale-preview fingerprint include the journal, so source-dependent evidence cannot outlive the existing provider policy. It is excluded from WIKI's indefinite public-domain backup class.

Read-only endpoints cover predictions, detail, scorecard and status with bounded filters. The Finance UI separates Shadow research from historical backtests, shows pending versus evaluated, and labels samples below 20 as `BOOTSTRAPPING`.

## Evidence

- Network-free temporal simulation: T0 prediction then T1 outcome.
- Exactly-once retry, clock failure, late-start anti-backfill, malformed ID/filter and immutable prediction assertions.
- Read-only API and UI assertions include no mutation or order controls.
- Runtime: eight EODHD instruments for 2026-08-14, 24 valid pending predictions (three strategy versions), zero evaluated outcomes and exact feature revision `feature-c204a8133abaf8a2` over eight source revisions.
- The first drill exposed 24 rows referencing a WIKI feature revision. They were not deleted or rewritten as successes: the integrity pass classified them `INVALIDATED`, retained the reason, and exact source-revision membership became a hard feature-selection gate.
- Restart retained 48 total audit rows (24 valid pending + 24 invalidated) and created no duplicate valid prediction. Current health, WIKI memory and backup inventory remained available.
- Published CI evidence is recorded in `docs/STATUS.md` after publication verification.

No claim is made that a future EOD outcome exists. A real prediction is created only if the deployed current observation and feature revision pass all causal gates; otherwise the journal remains safely armed.

## Security

Endpoints are read-only, validate bounded filters and malformed opaque IDs, return no secrets or filesystem paths and expose no mutation/order capability. The clock, entitlement, causality and late-start gates fail closed. No provider request, paid service or external database was added.

## Remaining work

The worker currently performs a lightweight source-state scan after startup; it does not add high-frequency polling. The recommended next Finance slice is prospective outcome-evaluator maturation plus explicit daily current-EOD cadence/recovery evidence, while M5 Hard Risk Engine and the security baseline remain prerequisites before any execution eligibility discussion.

## Resumption

Start with ADR 0029, `FinanceShadowResearch.cs`, the shadow API scorecard and the EODHD lifecycle worker. Verify current-source age and clock integrity before treating any new row as prospective.

## Sanitization

Detta är en sanerad GitHub-version. It contains no provider credential, raw market payload, private runtime identity, sensitive path or raw log.
