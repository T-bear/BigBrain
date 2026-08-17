# BB-091 — Sweden / Europe macro and FX

> Sanitized repository evidence; no secrets, private addresses, identities or raw logs.

Detta är en sanerad GitHub-version.

## Metadata

**Date:** 2026-08-17

**Starting commit:** `ec605e062841c43b450380584664c28e370657d7`

**Mode/budget:** RESEARCH / 0 SEK
## Status

COMPLETE. Implementation commit `dc42d47f8712023984d653b0c34977145476b2e3` passed GitHub Actions run 31997421033 (backend, frontend, documentation and secrets).

BB-091 adds narrow Riksbank and ECB adapters to BB-090 Macro Memory. It preserves all FRED revisions and uses the same quarantine → hash → rights/provenance → schema/semantic validation → immutable promotion path. Ordered migration 93 adds provider-neutral region, unit, frequency and FX quote metadata without changing old FRED rows.

## Evidence

### Official source and rights evidence

- Riksbank SWEA REST v1: `https://api.riksbank.se/swea/v1/Observations/{series}/{from}/{to}`. The official catalogue endpoint is `/Series`. Anonymous use is limited to 5 calls/minute and 1,000/day; registration is optional for higher limits. The selected first-party pack is policy rate `SECBREPOEFF` (continuous authoritative series, formerly called repo rate before 2022-06-08), EUR/SEK `SEKEURPMI`, and USD/SEK `SEKUSDPMI`. Rights evidence: the official API FAQ permits free automated/adapted use and requires “Source: Sveriges Riksbank” for disseminated unprocessed statistics. FX is indicative and non-transactional. Classification: `LOCAL_RESEARCH`, attribution required, no identified deletion duty.
- ECB SDMX 2.1: `https://data-api.ecb.europa.eu/service/data/{flow}/{key}` with versioned CSV content negotiation. The selected pack is EUR/USD `EXR.D.USD.EUR.SP00.A`, EUR/SEK `EXR.D.SEK.EUR.SP00.A`, and main refinancing operations rate `FM.D.U2.EUR.4F.KR.MRR_FR.LEV`. The API supports bounded periods, `updatedAfter`, conditional requests and `includeHistory`. ESCB public statistics permit free commercial/non-commercial reuse with attribution; modified statistics must be identified and third-party data is excluded. Classification: `LOCAL_RESEARCH`, attribution required, no identified deletion duty.

Neither current-history interface proves the historical instant at which every value first became knowable. Bootstrap observations therefore use acquisition UTC as knowledge time and remain `REVISED_HISTORY_EXPLORATORY`. ECB exposes change history and deltas, but BB-091 does not reinterpret that as a full realtime vintage proof. Riksbank warns that values can change after publication and exposes no vintage/realtime identity in the selected response. No publication hour is invented.

## Changes

### Canonical and validation semantics

`base=EUR, quote=SEK, value=10.xx` means one EUR equals 10.xx SEK; similarly USD/SEK is SEK per USD and EUR/USD is USD per EUR. Actual published dates only are stored—no weekend/holiday fill. Provider, source series, artifact hash, acquisition, knowledge time and revision remain distinct across overlapping EUR/SEK evidence. Exact-artifact retries return the existing promoted revision.

The network-free fixtures cover all six selected series shapes, malformed and wrong identity, rights denial, quarantine rejection, idempotence, quote direction, region, evidence class, bounded FX comparison and multi-region causal as-of selection. `POINT_IN_TIME_CAUSAL` selection is exact and never falls back to revised history.

## Security

The adapters accept only allowlisted series, official HTTPS hosts, bounded ranges and artifacts, and use no credentials. Quarantine and rights checks fail closed. No trading authority or external write path exists.

## Remaining work

### Runtime and closure evidence

The full pre-migration SQLite backup `bb-091-pre-migration-20260817.db` matches the stopped production original at SHA-256 `84ef36437bdcd8b762b0a3243c6af705def029e036749ecce701264c28405986`. An isolated copy migrated to `[1,90,91,92,93]`; all before/after counts were identical. Production migration then passed.

The bounded range is 2006-08-17–2026-08-17 (latest source observation 2026-08-14). Riksbank promoted 15,065 observations: `riksbank-29f3fcf67c80d453`, `riksbank-6c240515b912e8d4`, `riksbank-e886713bdcaad783`. ECB promoted 16,804: 5,145 EUR/USD, 5,145 EUR/SEK and 6,514 MRO rate observations in `ecb-c4b75cd8ac261619`, `ecb-cab72dbb9f97d245`, `ecb-95ff36c020e3c388`. Candidate/artifact hashes are respectively Riksbank `68bf93dd…`, `1504da2a…`, `c1333312…` and ECB `88a79391…`, `d3962e7a…`, `9833ab2c…`; full SHA-256 values remain in canonical candidate evidence.

Two fail-closed ECB attempts exposed and retained a full-schema variant and official empty-value rows. The adapter now requests deterministic `detail=dataonly`; published missing values remain nullable observations and are never fabricated. Exact repeat acquisition returned all six original revision IDs and stable totals: 9 macro revisions, 90,137 macro observations and 10 candidate audit rows.

EUR/SEK comparison respects the Riksbank source-method boundary. From 2023-11-27, 678/678 overlapping observations are `CONSISTENT`, maximum absolute difference 0 and no mismatch. Earlier overlap is `INSUFFICIENT_COMPARABILITY` because Riksbank documents a different source methodology.

Preservation is exact: 25 market revisions, 9,746 market observations, 6 feature revisions, 473 backtests, 13 robustness evaluations, 48 shadow rows, 0 outcomes, 0 risk evaluations and 0 halt audit. The original 58,196 exploratory plus 72 causal FRED observations remain unchanged. API, Web and Sentinel are healthy after recreate and restart; acquisition created no market session, prediction, outcome or risk verdict.

Final local verification: 24 focused tests, 449 API tests, 32 Sentinel tests and 113 frontend tests passed. Release solution build, frontend production build, API container build, documentation verifier, Compose validation, diff whitespace and sanitized diff secret pattern scan passed. Published GitHub Actions run 31997421033 passed all four required jobs.

## Resumption

BB-091 has no remaining closure work. Start the next separately approved Finance slice from the master roadmap, with special consideration for the minimal deterministic `FINANCE AUTONOMOUS RESEARCH v1` orchestration boundary rather than another broad provider pack.
