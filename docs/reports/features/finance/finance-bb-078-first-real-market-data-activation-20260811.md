# BB-078 first real Finance market-data activation

## Metadata

- Title: BB-078 first real Finance market-data activation
- Date: 2026-08-11
- Baseline: `e67a6f04fed05dbeb822224292923ef86348d4bb`
- Provider/product: EODHD / Free
- Scope: read-only daily EOD personal research
- Runtime outcome: activated, persisted and restart-verified
- Related commit: assigned on publication

## Status

The product-owner configured credential was detected as present without reading, printing or
logging its value. Provider enablement and active-account state were true and entitlement end
was unset. EODHD Free then became BigBrain Finance's first verified real market-data source.
The system remains `RESEARCH`; EOD activation grants no broker or trading authority.

The bounded worker made exactly eight external requests against the documented 20-request
daily Free allowance. All eight succeeded on their first attempt:

| Attempted and stored symbols | Failed or unsupported symbols | Retries |
| --- | --- | ---: |
| SPY.US, QQQ.US, IWM.US, AAPL.US, MSFT.US, JPM.US, XOM.US, JNJ.US | none | 0 |

No paid product, exchange add-on or alternative endpoint was selected. The provider output
was treated as delayed daily EOD for the latest completed session, never as live/realtime.

## Evidence

- Canonical instruments: SPY, QQQ, IWM, AAPL, MSFT, JPM, XOM and JNJ with the accepted
  canonical IDs/MIC mappings.
- Interval and classification: daily raw OHLC plus separately recorded adjusted close and
  split-adjusted volume, USD, REAL EOD/delayed.
- Coverage: 2025-08-11 through 2026-08-10.
- Normalized observations: 2,008 (251 per instrument).
- Content-addressed raw payloads: 8.
- Immutable revisions: 8.
- Acquisition failures, corrections and reported gaps: 0 / 0 / 0. The current gap count
  means no stored provider gap evidence; it is not a full exchange-calendar completeness
  certification.
- Active read revision surfaced by the current projection: `eodhd-bfdfa7770fbebed0`.

The eight exact stored revision IDs are:

`eodhd-23b42bae32d6d7de`, `eodhd-2973b33284f6f946`,
`eodhd-4d13774721c95b71`, `eodhd-53854b703d52c45f`,
`eodhd-6a8d44394900aefd`, `eodhd-8a8b419115043082`,
`eodhd-8e7a62bd62a5708b`, `eodhd-bfdfa7770fbebed0`.

## Persistence, replay and restart

The production Finance volume holds the SQLite WAL catalog and SHA-256-addressed payloads.
A sanitized maintenance projection confirmed acquisition journal, payload and revision
counts without reading licensed values into the report. Each exact revision was replayed
twice over the stored coverage and produced the same SHA-256 sequence checksum both times.
Every row's acquisition/knowledge time was at or after its market session time. The existing
historical replay tests additionally enforce chronological ordering and future-knowledge
boundaries; BB-078 is not the full M3 strategy backtest engine.

Only API and Web were recreated with the Finance volume preserved. After recreation:

- API and Web returned healthy;
- observations, payloads, revision IDs and coverage were unchanged;
- raw payload references remained usable for the checksum projection;
- the worker skipped all symbols already completed that UTC day;
- acquisition attempts and actual external requests remained exactly eight;
- all revision replay checks remained deterministic.

## API and UI

`/health` returned healthy. `/api/v1/modules/finance/observation` reported EODHD Free,
authorized owner-accepted personal research, REAL data, delayed/closed session semantics,
2,008 durable observations, active retention and all eight watchlist instruments. It exposed
no credential.

The deployed Web UI was exercised in 390×844 and 1440×1000 viewports. It rendered all eight
real instrument cards, the historical chart, source/evidence, coverage, count, active
revision, persistence and retention. `REAL EOD-MARKET DATA – ... inte live`, `RESEARCH` and
`Ingen handel med riktiga pengar` remained visible. No trading control, browser console
error, layout overflow or non-local browser request was observed.

## Changes

- Added a sanitized, network-free runtime-evidence command for journal/catalog statistics,
  exact-revision replay checksums and causal knowledge-time validation.
- Added focused fixture coverage for that projection.
- Updated current Finance state, roadmap, provider selection, persistence and operations
  documentation from credential-bound to the observed BB-078 runtime state.

## Security

Runtime retention is `active`: account active, entitlement end unset, no deletion deadline
and no deletion action. The covered scope is 2,008 observations, eight revisions and eight
payloads. Verified termination must immediately block new use and follow the existing
preview/explicit-confirmation deletion workflow within one month.

The credential value was never displayed, added to a command result, copied into source,
written to documentation or committed. No resolved environment, sensitive header or raw
credential-bearing URL was captured. The only new operator output is sanitized aggregate
status and replay checksums.

## Verification

- Focused EODHD backend tests: 8/8 passed.
- Full backend: 364 API + 32 Sentinel = 396/396 passed.
- Full Web: 107/107 passed; .NET Release and Web production builds passed.
- Documentation: 134 Markdown files and 78 unique BB IDs; Compose validation passed.
- External runtime: eight bounded successful EODHD requests; no additional request after
  restart.
- Deployment: API/Web healthy after recreation; Finance persistence and UI verified.

## Remaining work

There is no live/near-live source, corporate-action acquisition, full calendar gap audit,
strategy indicator pipeline, M3 portfolio backtest, broker, order or execution capability.
The next safe Finance slice is a small feature/indicator foundation computed from an exact
frozen real EOD revision, with provenance and no-lookahead tests. Zero-cost live-source
entitlement can continue independently and must not block historical research.

## Resumption

Use `docs/operations/runbooks/finance-eodhd-retention-deletion.md` for runtime evidence and
account lifecycle. The smallest safe next step is a versioned feature/indicator foundation
over an exact frozen real revision; do not promote trading mode or infer live authorization.

## Sanitization

Detta är en sanerad GitHub-version. This report contains no token, account identifier,
private endpoint, raw payload, resolved
environment or provider URL containing credentials. Counts, public symbols, revision IDs and
checksums are retained as the minimum durable activation evidence.
