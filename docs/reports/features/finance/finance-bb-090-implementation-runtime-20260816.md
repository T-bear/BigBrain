# BB-090 — FRED macro foundation and Finance correctness stabilization

> Sanitized publication: no secrets, private identities, private addresses, raw logs or sensitive machine paths are included.

Detta är en sanerad GitHub-version.

## Metadata

**Date:** 2026-08-16
**Baseline:** `ae2cb91729795df1fb9356015c8e29159f91acc1`
**Implementation commit:** `6f6cc743b91b9d0c2cb52fb297d0215fd7125b50`
**Closure stabilization commit:** `ef5b090b84b498c68304021e7e57426466b2c4b4`
**Vintage finalization commit:** `0f2ba9c3e7d5e7b3bf088c9094bdd79b3a35db1e`
**Mode/budget:** RESEARCH / 0 SEK

## Status

Implementation, closure stabilization and vintage finalization are regression-tested, deployed and published. GitHub Actions run 55 passed backend, frontend, documentation and secrets for the vintage finalization commit. All hard gates pass; **BB-090 is COMPLETE**.

## Evidence

### Authoritative source and acquisition

FRED API terms, API-key documentation, real-time-period documentation and each selected first-party series page were checked on 2026-08-16. API v1 requires a registered key and does not override series-owner rights. Each selected series is marked `Public Domain: Citation Requested`: `DFF`, `DGS2`, `DGS10`, `CPIAUCSL`, `UNRATE`. The bounded drill used the official no-key `fredgraph.csv` endpoint. It downloaded 932,112 bytes and 58,196 observations, promoted idempotently in an isolated database as revision `fred-a98118273a99adc9`, quality `PASS`. Latest exploratory classification was rates `UNKNOWN`, yield curve `NORMAL`, inflation `HIGH`, labor `IMPROVING`; this is revised-history context, not a point-in-time or trading claim.

This original evidence remains `REVISED_HISTORY_EXPLORATORY`. Acquisition time is its conservative knowledge time because graph CSV has no release/vintage timestamps; it was not mass-upgraded. Finalization activated the registered-key path without disclosing the key and used the official `fred/series/observations` endpoint with JSON `output_type=2`, reference range 2020-01-01–2020-03-01 and real-time range 2020-02-01–2021-02-01. Quarantine validation promoted 36 CPIAUCSL observations as `fred-9300bb944c8c319d` and 36 UNRATE observations as `fred-5800c2de8f2ef185`, all `POINT_IN_TIME_CAUSAL`. Raw drill files are retained only as bounded quarantine evidence; normalized observations and hashes are the canonical retained form. Rights must be revalidated before redistribution or changed use.

## Changes

### Implemented contracts

- Macro Memory owns bounded tables in the existing Finance SQLite database. One Finance schema coordinator applies ordered, transactional, restart-safe versions `1,90,91,92` under SQLite `BEGIN IMMEDIATE`; it safely baselines a legacy database without treating an empty migration journal as an empty database.
- `macro-context-v1` implements latest-known rate/yield values, causal 10Y–2Y spread, CPI year-over-year and unemployment/three-month change. Missing history remains missing.
- `market-regime-v1` preserves rate, curve, inflation and labor dimensions. Thresholds are explicit research assumptions; the rate-trend dimension remains `UNKNOWN` until a causal trend definition is added.
- Read-only API: `/macro/status`, `/macro/series`, `/macro/regime`, `/research/regime-analysis`. Analysis groups unchanged backtest runs by composite regime and reports sessions, compounded return, drawdown and insufficient-sample warnings; it does not optimize strategy logic. Absence reports bootstrapping/unavailable and does not affect BigBrain health or EODHD cadence.
- `us-equities-ny-v1` replaces fixed UTC opens/closes and weekday-only risk freshness with New York timezone/DST and bounded US holiday/exception rules.
- Dataset promotion now preserves distinct adjusted close, represents absence without copying raw close, uses candidate provider/product identity and provider-aware revision prefixes. Old WIKI revisions are untouched; their adjusted-price capability remains historically limited and requires an explicit audit before adjusted-price research.
- Hard Risk approval consumes Finance-level configured research-universe and approved-strategy/version policies, not EODHD catalog types or per-strategy engine branches. Machine decisions use typed reason categories; human strings remain presentation only.
- Overview signals carry exact prediction IDs. UI risk matching requires `shadowPredictionId`; old/unmatched signals show `Riskbedömning saknas`. Multiple exact verdicts aggregate deterministically and visibly: HALT, then DENY, INSUFFICIENT, REDUCE, and only unanimous complete ALLOW becomes allowed; missing or disagreeing lineage is marked `blandad`.
- FRED options use `Finance:Fred`; deployment variables are `FINANCE__FRED__ENABLED` and secret `FINANCE__FRED__APIKEY`. Diagnostics disclose only configured/not configured. The key is never returned, logged, persisted or committed.
- Macro candidates are copied to bounded quarantine before parsing. Candidate evidence stores first-party URL, provider, artifact hashes/names, acquisition time, rights evidence, series identity, schema fingerprint, validation result and promotion decision. Malformed candidates remain rejected evidence and cannot reach canonical tables.

## Security

No credentials are stored or logged by the macro contract. The official client is bounded to `https://api.stlouisfed.org/fred/series/observations`, requires HTTPS first-party host validation, and accepts CPIAUCSL/UNRATE only for the vintage drill. No trading authority or external write path exists.

## Remaining work

### Closure verification and limitations

The pre-existing container `bigbrain-sprint1-api-run-6cc339e7da09` used an older image that did not recognize its requested maintenance command and had run the normal web host for five days. It shared the named Finance volume and held no separate work. After evidence capture it was gracefully stopped and removed; the volume and main services remained intact.

Before migration, a full SQLite copy plus SHA-256 manifest was created under the Finance backup inventory with explicit provider-aware retention: EODHD remains deletion-controlled, WIKI is public-domain, and macro follows revision rights. An isolated copy migrated to versions `1,90,91,92`. Before/after counts were identical: 25 market revisions, 9,746 observations, 6 feature revisions, 473 backtests, 13 robustness evaluations, 48 shadow predictions, 0 outcomes, 0 risk evaluations and 0 halt-audit rows. Production migration, deploy, restart, second restart and a post-promotion restart passed without duplicate migrations or data.

`adjusted-history-audit-v1` classified 24 EODHD revisions `RAW_AND_ADJUSTED_VALID`. `wiki-5713d7dccfa38f56` is `ADJUSTED_SEMANTICS_INVALID` because the pre-BB-090 importer equated adjusted with raw close. The immutable observation was preserved; adjusted research fails closed and raw research remains allowed. New EODHD and generic dataset promotions record explicit capability without fabrication.

Production Macro Memory contains three candidates, three immutable revisions and 58,268 observations. Revision `fred-a98118273a99adc9` retains all 58,196 `REVISED_HISTORY_EXPLORATORY` rows. `fredApiKeyConfigured=true`; the two official vintage revisions add 72 `POINT_IN_TIME_CAUSAL` rows. Repeating both acquisitions returned the same IDs and counts. For UNRATE reference period 2020-01-01, authoritative real-time evidence changed from 3.6 beginning 2020-02-07 through 2021-01-07 to 3.5 beginning 2021-01-08. Finance makes each value visible only on the following day at 00:00 UTC, so a query before 2020-02-08 is unavailable, an as-of query before 2021-01-09 selects 3.6, and later queries select 3.5. Point-in-time feature requests never fall back to revised history, and missing vintage evidence remains unavailable.

The calendar tests cover both DST transitions, weekends, ordinary holidays, Juneteenth beginning with exchange observance in 2022, and documented exceptional full-day closures. Early closes are not modeled in v1. This is non-blocking for the current daily session-identity/EOD research and freshness rules, which do not trade intraday or use close time as execution truth; it must be added before intraday or exact early-close execution semantics.

Focused finalization tests: 11 passed. Full API: 440 passed. Sentinel: 32 passed. Frontend: 113 passed. Release, frontend production and API container builds passed; documentation and Compose verification are green. API, Web and Sentinel remained healthy through restart and recreate. The Sunday deployment made no EODHD acquisition, new prediction or outcome: 24 valid rows remain PENDING and 24 prior rows remain INVALIDATED audit evidence. Market/risk evidence counts remain 25 revisions, 9,746 observations and 0 risk evaluations.

No PAPER, broker, order, LIVE/AUTO or self-learning path was introduced. BB-090 has no remaining closure blocker. The next separately approved Finance slice may be BB-091; it was not started here.

## Resumption

Resume from ADR 0031, `docs/modules/finance.md`, `docs/STATUS.md` and this report. BB-091 requires its own explicit scope.
