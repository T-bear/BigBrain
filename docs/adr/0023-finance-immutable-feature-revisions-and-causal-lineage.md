# ADR 0023: Finance immutable feature revisions and causal lineage

- Status: Accepted
- Date: 2026-08-11
- Owners: Product owner and Finance architecture
- Related: ADR 0017, ADR 0020, ADR 0021, ADR 0022

## Context

BB-078 established immutable real market-data revisions and causal replay. Feature values
used by later research must be reproducible without reading mutable latest state or exposing
future observations. They also inherit the entitlement lifecycle of their source data.

## Decision

Finance derives features only from canonical market observations bound to exact immutable
market revision IDs. A versioned feature set, deterministic definition fingerprints and an
engine/schema version produce an immutable feature revision with coverage, counts and a
content checksum. Repeating the same inputs is idempotent; changed source membership or
semantics creates a new revision and never overwrites the old one.

Each value records instrument, session, feature definition/version, source revision,
source range, knowledge time, quality/warmup state and engine version. A value at time T may
depend only on observations whose market and knowledge boundaries are at or before T.
Readers used by replay are bounded by date/knowledge time and never expose a completed
future table as if it had been known earlier.

`core-daily-v1` uses raw close/OHLC consistently and separately classified provider volume.
It never silently mixes adjusted prices. Warmup and unavailable inputs are explicit; gaps
are never converted into zero-return bars. Calculation changes require a new feature or
feature-set version.

Feature revisions derived from EODHD data are covered artifacts under the same active-
account retention and post-expiry deletion lifecycle. Preview and confirmed deletion must
enumerate and remove dependent values, revisions and indexes. Generic feature definitions
contain no licensed observations and may remain.

Features are measurements only. This decision creates no signal, recommendation, order,
broker, PAPER, LIVE or AUTO authority.

## Consequences

- Future backtests can bind exact market plus feature revisions and reproduce their inputs.
- Storage grows by immutable revisions, but idempotent fingerprints prevent duplicate rows.
- Corporate-action-adjusted research requires a future explicit price-basis version.
- EODHD termination inventory now includes dependent feature artifacts.
