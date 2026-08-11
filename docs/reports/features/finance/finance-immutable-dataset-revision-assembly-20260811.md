# Finance BB-045 – immutable dataset revision assembly

## Metadata

- Date: 2026-08-11
- Scope: fourth provider-neutral BB-045 implementation slice
- Related commit: assigned on publication

## Status

Immutable in-memory revision assembly, correction/supersession relationships and inclusive
availability-as-of selection are implemented and automatically verified with synthetic data.
BB-045 remains in progress. BB-071 remains open and blocks real provider ingestion/storage.
No persistence, provider, runtime, deployment or trading capability was activated.

## Changes

`DatasetRevisionMember` provides immutable IDs for bars, corporate actions and session/gap
quality evidence while preserving logical observation identity, source revision, policy and
UTC availability. Event/effective time remains in each canonical fact and is not confused
with the knowledge time when that member could be known.

`DatasetCorrection` explicitly relates original and replacement member IDs with exact UTC
availability, stable reason and evidence. A correcting child revision must introduce the
replacement, preserve type/logical identity and cannot apply it before the original or after
revision creation. Assembly inherits parent membership, applies corrections in stable
availability/ordinal order and never mutates parent state.

`ImmutableDatasetRevisionCatalog` validates a linear acyclic parent/supersession chain,
provides exact historical revision lookup and selects the latest revision satisfying
`revision.AvailableAtUtc <= ReplayAsOf`. The boundary is inclusive. Future corrections,
members and revisions cannot leak backwards; old IDs remain exactly reproducible. Invalid
parents, cycles, correction references, duplicate IDs, branches, scope changes and unavailable
members fail explicitly.

Corporate actions retain their effective dates and exact ratios. Inherited session/gap
evidence retains its classification/source revision. Existing historical symbol resolution
and deterministic session replay tests remain green. Assembly uses no wall clock, host
timezone, dictionary enumeration, filesystem, random, network or timing behavior.

## Evidence

- `dotnet restore BigBrain.slnx` — PASS
- `dotnet build BigBrain.slnx -c Release --no-restore` — PASS, zero warnings/errors
- `dotnet test BigBrain.slnx -c Release --no-build --no-restore` — PASS
- API/module: 304 passed; Sentinel: 32 passed; total: 336 passed, 0 failed/skipped
- `node scripts/verify-documentation.mjs` — PASS
- `git diff --check` — PASS
- `docker compose config --quiet` — PASS

The 18 new tests cover original/corrected as-of views, inclusive availability, immutable old
revisions, explicit supersession, multiple corrections, future-information exclusion,
unknown references, correction/revision cycles, logical identity, availability consistency,
deterministic input/replay order, corporate actions, ticker history, session/gap inheritance,
ambiguous branches, unrelated roots and members unavailable at revision creation.

## Security

Detta är en sanerad GitHub-version. Fixtures use only invented `ExampleData` metadata and
synthetic decimal values. No account, API key, credential, real payload, external request,
broker, order, PAPER/LIVE/AUTO promotion, paid dependency/service, database, migration,
runtime change or deployment exists. Finance retains zero real-money authority.

## Remaining work

Persistence remains deliberately unimplemented. The model now establishes requirements for
append-only revision metadata, immutable member bodies, parent/supersession and correction
links, UTC event/availability indexes, logical identities, provenance/policy/deletion data,
deterministic as-of/range queries and licensed backup/deletion. Measure bounded EOD volume,
correction rates, replay latency, readers and backup/restore/deletion before technology choice.

BB-045 also retains richer quality aggregation, authorized adapter, actual ingestion and
external acceptance scope. BB-071 must provide exact entitlement before any real provider
data is downloaded or stored.

## Resumption

Proceed with BB-072 **FREE HISTORICAL DATA INGESTION preparation/research**: compare zero-cost
sources using dated first-party license/ToS, retention and personal backtesting rights before
coverage, delisted/survivorship, corporate actions, raw/adjusted state, symbol history, rate
limits, reproducibility and quality. Research activates no provider. Only an explicitly
owner-reviewed, BB-071-compliant source may later proceed to an adapter/acceptance slice.
