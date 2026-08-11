# Finance BB-045 – canonical instrument and normalization foundation

## Metadata

- Date: 2026-08-11
- Scope: second provider-neutral BB-045 implementation slice
- Related commit: assigned on publication

## Status

Canonical identity, effective-dated provider symbols, synthetic daily OHLCV, cash
dividends, exact stock splits, basic findings and deterministic normalization are
implemented and automatically verified. All fixtures are invented (`ExampleData`,
`Synthetic-EOD-Personal`, `BB-EQ-TEST-001`, `TEST-A`/`TEST-B`, XSTO). No provider API,
payload, account, credential, HTTP client, persistence, runtime or deployment was added.

## Changes

A `CanonicalInstrument` owns the BigBrain ID, Equity/ETF type, display name, currency,
venue/MIC, lifecycle and validity. The ID is independent of ticker and provider. Mappings
bind provider, product, reference, MIC, inclusive valid-from/valid-to dates and evidence.
Historical lookup includes market date and MIC; the old symbol ends 2024-05-31 and the new
one begins 2024-06-01 without changing `BB-EQ-TEST-001`. Same ticker strings on different
venues are distinct, unknown lookup fails clearly and overlapping histories are rejected.

Daily canonical bars use decimal `Price`, non-negative decimal volume, session date,
explicit Raw/Adjusted classification and an adjustment basis for adjusted observations.
Corporate actions are separate: cash dividends require positive `Money` in canonical
currency; stock splits require an exact positive rational numerator/denominator. Both bar
and action outputs retain immutable dataset revision, policy identity, provider/product,
source/retrieval timestamps and schema/adapter provenance.

The pure pipeline resolves mapping, validates without silent repair and produces canonical
output. Stable findings cover missing/ambiguous mapping, invalid range, negative volume,
currency mismatch, exact duplicate and conflicting duplicate. Within a batch the first
bar is retained; exact duplicates are reported/ignored and conflicts are reported/rejected.
The same input/catalog/policy produces equal output.

## Evidence

- `dotnet restore BigBrain.slnx` — PASS
- `dotnet build BigBrain.slnx -c Release --no-restore` — PASS, zero warnings/errors
- `dotnet test BigBrain.slnx -c Release --no-build --no-restore` — PASS
- API/module: 273 passed; Sentinel: 32 passed; total: 305 passed, 0 failed/skipped
- `node scripts/verify-documentation.mjs` — PASS
- `git diff --check` — PASS
- `docker compose config --quiet` — PASS

Tests are deterministic, network-free, storage-free, clock-independent and synthetic.

## Security

Detta är en sanerad GitHub-version. It contains no credentials, private provider evidence,
real market payload, internal identity, private address, raw log or sensitive path. Finance
keeps zero broker, execution and real-money authority.

## Remaining work

BB-045 remains in progress. A true market-calendar/session system is absent, so expected
no-trading days, unknown missing observations and provider gaps remain explicitly distinct
and are not guessed. Richer gap/quality handling, deterministic adjusted historical replay,
measured persistence, authorized provider adapter, external ingestion and data acceptance
remain. Historical symbol design supports renamed/delisted identity, but no delisted-source
integration exists.

BB-071 remains `Pågår – väntar på leverantörsbekräftelse`; this work authorizes no provider.

## Resumption

The next safe slice is a small versioned market-calendar/session abstraction with richer
gap findings, followed by deterministic replay over immutable synthetic revisions.

Finance remains undeployed RESEARCH with zero real-money authority. Free-first/cost-aware
is unchanged; no paid dependency or SaaS was introduced.
