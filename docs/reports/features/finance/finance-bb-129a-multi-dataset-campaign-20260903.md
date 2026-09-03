# BB-129A multi-dataset strategy research campaign

## Metadata

- Baseline: `5e0e1ab1a7294758e0732dcc377b31ddfad54302`
- Status: implemented locally; focused verification passed; publication/deployment/runtime pending
- Safety: `RESEARCH / 0 SEK / NONE`

## Status

Implemented locally and focused-test verified; publication, CI, deployment and runtime verification remain pending.

## Evidence

BB-129A adds a small campaign aggregate above existing BB-127 revisions and BB-123/124/092 contracts. The existing Finance SQLite database stores exact revision/fingerprint/instrument lineage, policy identifiers, fixed knowledge time, predeclared population, bounds, every attempted outcome, limitations, deterministic scorecard and checksum. No alternate backtest, robustness, feature or experiment store was added.

## Changes

Population v1 is momentum lookback 20 and SMA 10/40 plus 20/80: 3 variants in 2 families. Bounds are 8 instruments, 24 attempts, 5 robustness variants, concurrency 1, no retries and 300 seconds. Each dataset/variant pairing counts; histories remain independent.

## Security

`ROBUST CANDIDATE` requires eligibility, schema, integrity, OOS, uncontaminated single-use holdout, conservative cost and existing robustness gates. Current BB-127 rows have no accepted feature revision/BB-124 selection evidence for these strategies, so the honest result is `INCONCLUSIVE / NOT EVALUABLE`. No broker, order, PAPER, LIVE, AUTO, provider activation, acquisition, canonical promotion or capital authority is introduced.

## Remaining work

Complete full gates, publish, deploy only API, runtime-check the read endpoints, and execute one fixed-time campaign if the deployed evidence remains safely admissible.

## Resumption

Resume from the published BB-129A commit and verify `origin/main`; do not repeat a campaign with altered rules after seeing its outcomes.

Detta är en sanerad GitHub-version. It contains no credentials, private addresses, raw logs, private identities or sensitive filesystem paths.
