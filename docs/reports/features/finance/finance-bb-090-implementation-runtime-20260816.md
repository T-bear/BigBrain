# BB-090 — FRED macro foundation and Finance correctness stabilization

> Sanitized publication: no secrets, private identities, private addresses, raw logs or sensitive machine paths are included.

Detta är en sanerad GitHub-version.

## Metadata

**Date:** 2026-08-16
**Baseline:** `ae2cb91729795df1fb9356015c8e29159f91acc1`
**Implementation commit:** `6f6cc746ce5d8bea24d19ad48c3596cb9ed0de28`
**Mode/budget:** RESEARCH / 0 SEK

## Status

Implemented, locally regression-tested and published. Not deployed or manually production-verified. GitHub Actions could not be inspected because GitHub CLI is unavailable and the public web fallback returned no status; CI remains unverified.

## Evidence

### Authoritative source and acquisition

FRED API terms, API-key documentation, real-time-period documentation and each selected first-party series page were checked on 2026-08-16. API v1 requires a registered key and does not override series-owner rights. Each selected series is marked `Public Domain: Citation Requested`: `DFF`, `DGS2`, `DGS10`, `CPIAUCSL`, `UNRATE`. The bounded drill used the official no-key `fredgraph.csv` endpoint. It downloaded 932,112 bytes and 58,196 observations, promoted idempotently in an isolated database as revision `fred-a98118273a99adc9`, quality `PASS`. Latest exploratory classification was rates `UNKNOWN`, yield curve `NORMAL`, inflation `HIGH`, labor `IMPROVING`; this is revised-history context, not a point-in-time or trading claim.

This evidence is `REVISED_HISTORY_EXPLORATORY`. Acquisition time is the conservative knowledge time because graph CSV has no release/vintage timestamps. It is not historically point-in-time safe. ALFRED/FRED API supports real-time periods and vintage dates, but its registered-key path was not activated; future point-in-time analysis depends on a bounded key-backed acquisition. Raw drill files are temporary local evidence; normalized observations and hashes are the intended retained form. Rights must be revalidated before redistribution or changed use.

## Changes

### Implemented contracts

- Macro Memory owns series/revision/observation tables and ordered migration 90 in the existing Finance SQLite database.
- `macro-context-v1` implements latest-known rate/yield values, causal 10Y–2Y spread, CPI year-over-year and unemployment/three-month change. Missing history remains missing.
- `market-regime-v1` preserves rate, curve, inflation and labor dimensions. Thresholds are explicit research assumptions; the rate-trend dimension remains `UNKNOWN` until a causal trend definition is added.
- Read-only API: `/macro/status`, `/macro/series`, `/macro/regime`, `/research/regime-analysis`. Analysis groups unchanged backtest runs by composite regime and reports sessions, compounded return, drawdown and insufficient-sample warnings; it does not optimize strategy logic. Absence reports bootstrapping/unavailable and does not affect BigBrain health or EODHD cadence.
- `us-equities-ny-v1` replaces fixed UTC opens/closes and weekday-only risk freshness with New York timezone/DST and bounded US holiday/exception rules.
- Dataset promotion now preserves distinct adjusted close, represents absence without copying raw close, uses candidate provider/product identity and provider-aware revision prefixes. Old WIKI revisions are untouched; their adjusted-price capability remains historically limited and requires an explicit audit before adjusted-price research.
- Hard Risk approval is behind an instrument/strategy policy contract; machine decisions use typed reason categories. Human strings remain presentation only.
- Overview signals carry exact prediction IDs. UI risk matching requires `shadowPredictionId`; old/unmatched signals show `Riskbedömning saknas`.

## Security

No credentials are stored or logged by the macro contract. API keys use configuration/secret conventions if ALFRED is activated later. No trading authority or external write path exists.

## Remaining work

### Verification and limitations

Focused macro/session/dataset/backtest/risk suite: 29 passed. Full API: 428 passed. Sentinel: 32 passed. Frontend: 112 passed; production build passed. Documentation, Compose and diff checks passed. The real drill did not call EODHD and did not create a Sunday session, prediction or outcome. Production deployment/promotion is blocked because a pre-existing long-running API one-off container holds the Finance database; it was preserved rather than stopped without separate authority. The main API was restored healthy after the attempted backup. Complete early-close history and complete audit/remediation of existing adjusted-price revisions remain unfinished; BB-090 must not be called complete until these are resolved.

No PAPER, broker, order, LIVE/AUTO or self-learning path was introduced. Recommended next work is additional Finance correctness stabilization to close the stated BB-090 limitations, while prospective accumulation continues independently—not BB-091 or PAPER.

## Resumption

Resume from ADR 0031, `docs/modules/finance.md`, `docs/STATUS.md` and this report. The smallest safe next step is to complete full regression and the remaining BB-090 correctness work before production backup/deployment.
