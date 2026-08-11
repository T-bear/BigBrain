# EODHD Free credential-bound activation implementation

## Metadata

- Date: 2026-08-11
- Scope: BB-077 provider adapter, local memory, retention, API/UI and replay
- Runtime outcome: credential-bound; no real provider request or observation
- Related commit: assigned on publication

## Status

The EODHD Free path is implemented, fixture-tested and deployed disabled by default. A valid
free API token was not available, so no provider smoke, real observation, populated
production memory, real replay or real-data UI runtime is claimed.

## Evidence

The direct server-side adapter calls only the EOD daily endpoint for the eight established
US watchlist symbols, requests at most the past year, skips same-day successful symbols,
uses one-second spacing and bounded 429/5xx retry. Unit/integration tests use sanitized
documented-shape fixtures and no network.

SQLite WAL provides the local transactional catalog. Raw responses are immutable and
content-addressed by SHA-256. Normalized rows retain canonical ID, provider symbol, MIC,
session date, raw OHLC, adjusted-close classification, split-adjusted volume, acquisition
time, policy and revision. Reingestion is idempotent; corrections create a new immutable
revision and latest knowledge wins in the read model without overwriting old evidence.

## Changes

- Added disabled EODHD options, secret boundary, adapter, rate/retry behavior and worker.
- Added local SQLite/content-addressed memory, sanitized acquisition journal, checksums,
  deterministic exact-revision replay and restart-safe/idempotent publication.
- Added entitlement/account state, one-month deletion deadline, covered-copy counts,
  preview, exact confirmation, scoped deletion and sanitized receipt.
- Added Finance API/UI EODHD source, REAL/EOD honesty, evidence class, coverage, revision,
  persistence and compact retention state. RESEARCH/no-real-money remains prominent.
- Added a named Docker volume and disabled environment configuration. The token is never
  returned by API/UI and the adapter avoids framework URL logging because authentication is
  an EODHD query parameter.

## Verification

- Focused EODHD/read-model tests: 15/15 passed; Finance Web component: 4/4 passed.
- Full backend: 364 API + 32 Sentinel = 396/396 passed.
- Full Web: 107/107 passed; .NET Release and Web production builds passed.
- Documentation: 133 Markdown files and 77 unique BB IDs; Compose and diff checks passed.
- Disabled deployment: API and Web healthy; EODHD state `candidate`, entitlement
  `authorized`, evidence `ownerAcceptedPersonalResearch`, ingestion/storage false,
  `dataKind=none`, observation count 0 and every trading flag false.
- Deployed deletion preview: 0 observations, 0 revisions and 0 payloads; no deletion executed.
- Initial deployment exposed root ownership on the new empty Finance volume. Ownership was
  scoped to that volume, the API image now provisions `/finance-data` for its unprivileged
  UID, and API/Web recovered healthy. Compose also recreated its Sentinel dependency with
  unchanged configuration; Sentinel returned running.
- No EODHD key existed, no EODHD endpoint was contacted, and no real replay was possible.

## Security

Detta är en sanerad GitHub-version. No credential, account identifier, real payload, real
price, internal runtime identity or raw log is included. No broker, order, PAPER/LIVE
execution, BUY/SELL, LIMITED_AUTO or AUTO capability exists.

## Remaining work

Owner action: create a free EODHD account, set `FINANCE__EODHD__APITOKEN`,
`FINANCE__EODHD__ACCOUNTACTIVE=true` and `FINANCE__EODHD__ENABLED=true`, then deploy and run
the documented smoke. Real counts, coverage, revision and replay evidence remain zero/not
run until that credential boundary is crossed. Independently created Finance-volume copies
must be included in any later deletion; BB-077 does not add this volume to backup automation.

## Resumption

Use `docs/operations/runbooks/finance-eodhd-retention-deletion.md`. After credential setup,
perform the bounded acquisition, restart/idempotence check, exact-revision replay and UI
verification before marking real-data activation complete.
