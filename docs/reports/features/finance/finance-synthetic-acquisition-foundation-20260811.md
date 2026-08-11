# Finance synthetic acquisition foundation

## Metadata

- Date: 2026-08-11
- Scope: provider-neutral BB-045 acquisition and ingestion preparation
- Related commit: assigned on publication

## Status

The synthetic acquisition contract, fail-closed multi-use entitlement gate, immutable
acquisition journal, synthetic-only fixture adapter and orchestration into the existing
normalization/revision/replay foundation are implemented and automatically verified.
BigBrain is technically prepared to receive a future authorized historical-data adapter,
but no real adapter or data may be accepted until BB-071 passes and the product owner
explicitly approves the exact entitlement.

## Changes

`HistoricalDataAcquisitionRequest` binds source kind, provider/product, canonical and
provider identity, MIC, date range, daily interval, raw/adjusted basis, explicit UTC
acquisition time, source timezone, cursor, policy reference and destination revision.
`HistoricalDataAcquisitionBatch` carries stable request/batch/observation identities,
pagination evidence, completeness, bars, corporate actions, explicit gaps and declared
corrections. It has no authentication or transport-secret surface.

`AcquisitionEntitlementGate` requires the exact policy to allow HistoricalAnalysis,
Backtest, DerivedMetrics and LongTermStorage plus persistence. Missing, mismatched,
unknown, denied or invalid synthetic policy fails closed. Synthetic evidence is accepted
only for provider `SyntheticFixture`, products prefixed `Synthetic-` and `fixture:` policy
evidence, preventing fixture authorization from being confused with a real provider.

`HistoricalDataIngestionPipeline` orders immutable batches and observations,
deduplicates identical repeated batch IDs, rejects conflicting reuse, feeds bars/actions
through the existing `SyntheticMarketDataNormalizer`, converts explicit gap evidence for
existing replay semantics, and sends members/corrections to
`ImmutableDatasetRevisionAssembler`. It does not create a second canonical model.
Corrections create child revisions; parent evidence is never overwritten.

`AcquisitionJournalEntry` records request/source/range/policy evidence, retention/deletion
obligations, batch and accepted/rejected/duplicate counts, quality findings, result revision
and stable outcome reason. No key, credential, header or provider payload is journaled.

## Evidence

- Focused acquisition tests: 12 passed
- `dotnet restore BigBrain.slnx` — PASS
- `dotnet build BigBrain.slnx -c Release --no-restore` — PASS, zero warnings/errors
- `dotnet test BigBrain.slnx -c Release --no-build --no-restore` — PASS
- API/module: 316 passed; Sentinel: 32 passed; total: 348 passed, 0 failed/skipped
- `node scripts/verify-documentation.mjs` — PASS
- `git diff --check` — PASS
- `docker compose config --quiet` — PASS

Tests cover contract repeatability, absence of secret fields, deny-before-adapter behavior,
synthetic acceptance, provider/retention rejection, duplicate batches, overlapping pages,
stable ordering, conflicting batch IDs, correction supersession, dividend/split journaling,
gap/replay integration, repeated replay and future-correction exclusion.

## Security

Detta är en sanerad GitHub-version. All values and prices are invented fixtures. No account,
API key, credential, authentication header, provider HTTP call, scrape, real market data,
paid service, database, broker path, order, PAPER/LIVE/AUTO promotion, runtime change or
deployment was created. Finance retains zero real-money authority.

## Remaining work

BB-045 still lacks measured persistence, an authorized real adapter, real-data acceptance
and richer aggregate quality handling. BB-071 remains `Pågår – väntar på
leverantörsbekräftelse`; neither EODHD nor Twelve Data is selected or authorized. The
synthetic pipeline is an architecture proof, not a provider entitlement.

## Resumption

If BB-071 remains unresolved, the next safe parallel milestone is **SYNTHETIC ACQUISITION
MANIFEST / PERSISTENCE BENCHMARK DESIGN**: measure fixture EOD sizes, append/query shapes,
checksum/idempotency and licensed deletion/backup behavior without selecting storage or
touching provider data. Once exact rights pass owner review, a separately approved
**FIRST AUTHORIZED FREE HISTORICAL DATA ADAPTER** milestone may implement one narrow adapter.
