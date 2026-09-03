# BB-129A multi-dataset strategy research campaign

## Metadata

- Baseline: `5e0e1ab1a7294758e0732dcc377b31ddfad54302`
- Implementation: `c46da870798ada8510d3067cb3b345c24cbf16dc`
- Status: implemented, automatically verified, CI verified, deployed, runtime verified and bounded-research verified
- Safety: `RESEARCH / 0 SEK / NONE`

## Status

Completed. Owner review is not claimed.

## Evidence

BB-129A adds a small campaign aggregate above existing BB-127 revisions and BB-123/124/092 contracts. The existing Finance SQLite database stores exact revision/fingerprint/instrument lineage, policy identifiers, fixed knowledge time, predeclared population, bounds, every attempted outcome, limitations, deterministic scorecard and checksum. No alternate backtest, robustness, feature or experiment store was added.

Local evidence: focused Finance 40/40, full API 608/608, Sentinel/architecture 32/32, Release build zero warnings/errors, documentation 217 files/89 IDs, Compose and diff checks passed. GitHub Actions run `33732206649` passed backend, frontend, documentation and full-history secrets.

## Changes

Population v1 is momentum lookback 20 and SMA 10/40 plus 20/80: 3 variants in 2 families. Bounds are 8 instruments, 24 attempts, 5 robustness variants, concurrency 1, no retries and 300 seconds. Each dataset/variant pairing counts; histories remain independent.

API-only deployment moved from image `sha256:08f2db15…` to `sha256:ce133d43…`; API is healthy. The fixed definition used knowledge time `2026-09-03T08:30:00Z` and revisions `research-0e6f681db395214a`, `research-184f99fc38e72935`, `research-21d1aa170ea0a809`, `research-2be8e5ba1eed5177`, `research-2f345e7310ab29d1`, `research-3bd374f3e16ab5ad`, `research-47786d03b307c393`, `research-47dc8d7e58ef1d45`, independently representing GBPSEK, OMXS30, VIX, AAPL, SPY, QQQ, SP500 and EURSEK.

Campaign `campaign-cac9adb016ada14f`, checksum `sha256:cac9adb016ada14f373d49a9260c217a539dd956f9a305beffe9ddffd0dd2f3b`, completed 24 attempts: 0 rejected, 24 inconclusive/not evaluable, 0 survived initial screen and 0 robust candidates. Reasons were 15 `DATASET_INELIGIBLE` and 9 `INSUFFICIENT_DATA`. Exact replay returned the same campaign/checksum and one catalog row.

## Security

`ROBUST CANDIDATE` requires eligibility, schema, integrity, OOS, uncontaminated single-use holdout, conservative cost and existing robustness gates. Current BB-127 rows have no accepted feature revision/BB-124 selection evidence for these strategies, so the honest result is `INCONCLUSIVE / NOT EVALUABLE`. No broker, order, PAPER, LIVE, AUTO, provider activation, acquisition, canonical promotion or capital authority is introduced.

## Remaining work

No implementation blocker remains. Scientific limitation: BB-127 revisions lack BB-124-compatible immutable feature/selection evidence, so no actual OOS/holdout/robustness performance was evaluated and no candidate survived. A future separately approved sprint may close that lineage gap; these results must not be retuned or marketed as performance.

## Resumption

Resume from the published BB-129A commit and verify `origin/main`; do not repeat a campaign with altered rules after seeing its outcomes.

Detta är en sanerad GitHub-version. It contains no credentials, private addresses, raw logs, private identities or sensitive filesystem paths.
