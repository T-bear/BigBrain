# BB-092 — Finance Autonomous Research v1 foundation

> Sanitized repository evidence; no secrets, private addresses, identities or raw logs.

Detta är en sanerad GitHub-version; hemligheter, privata adresser, identiteter och råloggar är uteslutna.

## Metadata

- Date: 2026-08-22
- Scope: BB-092 bounded research foundation
- Related implementation commit: `8f8358c990b871c986bf0ee30ce434cb68a2394e`

## Status

Implemented, locally verified and published. GitHub Actions run 32587285183 passed all four required jobs. Production deployment and manual production verification were not performed.

## Evidence

Local verification passed: 455 API tests, 32 Sentinel tests and 114 frontend tests, including 6 BB-092 backend tests and the Finance UI cases. Release solution build, frontend production build, API container build, documentation verification, Compose validation, diff whitespace and sanitized secret-pattern scan passed. GitHub Actions run 32587285183 passed backend, frontend, documentation and secrets for the implementation commit.

## Changes

The bounded server-side cycle consumes existing immutable Finance evidence and creates at most three experiments per explicitly triggered run. The v1 signal library formalizes `trend.sma.fast-slow-relation`, `momentum.20.sign`, `volatility.20.level` and `volume.ratio20.level` around existing `core-daily-v1` values. Only the existing SMA and momentum strategy families are enabled in the first run; no indicator calculation is duplicated.

Hypotheses are structured, versioned and fingerprinted from engine, family, features, target horizon and pinned lineage. Experiments bind the hypothesis, robustness evaluation, market revisions, feature revision, knowledge cutoff, explicit `hypothetical-conservative-v1` costs, complexity and integrity evidence. Repeating the same idempotency key returns the same run and cannot inflate experiment evidence. Related public-domain research metadata follows BB-085 backup eligibility; restricted raw/history retention is unchanged.

The integrity gate requires sample, held-out positive net and benchmark-relative evidence, real walk-forward breadth, cost ladder evidence, complete lineage, family attempt accounting, bounded complexity and `MoreRobust`. Positive train return alone cannot pass. A passing candidate becomes only `CHALLENGER`; no champion or shadow-strategy replacement exists. UI presents totals, rejections/challengers and progressively disclosed integrity/lineage without profitability language.

## Security

This report is sanitized. It contains no secrets, credentials, private addresses, raw logs, internal identities or sensitive paths. The implementation adds no arbitrary expression, SQL, path, command, remote-code or provider-secret input. Finance remains `RESEARCH`, budget `0 SEK`, execution authority `NONE`. No PAPER, broker, order, LIVE/AUTO, risk-policy mutation or system-halt override exists.

## Remaining work

- Existing chronological 70/30 train/OOS, 50-session embargo and genuine expanding-window walk-forward are reused.
- Family attempt counts are preserved and raw best-result significance is not treated as independent evidence. Stronger multiplicity statistics remain future maturation.
- DSR: `NOT_EVALUABLE`; required return-series moments and selection-population assumptions are not retained.
- PBO/CSCV: `NOT_EVALUABLE`; required combinatorial partitions are absent.
- Negative control: deferred; prerequisite is a code-reviewed seeded permutation contract isolated from canonical observations.
- Expectancy/profit factor remain null when current immutable metrics cannot calculate them correctly. Win rate is shown only as a descriptive exit ratio, never a promotion rule.
- The supported watchlist is the honest universe; no broad equity or survivorship-free claim is made.
- Macro is not used because current European evidence is revised-history exploratory and causal FRED coverage is insufficient.
- No background scheduler, resource governor, continuous autonomy or production deployment was introduced.

The smallest next operationalization slices are a separately approved bounded scheduler/orchestrator, resource and safety governor, then autonomous-research operations/recovery. These are soft milestones, not promises. Seeded negative controls and statistically correct DSR/PBO remain research-integrity maturation work.

## Resumption

Resume from ADR 0033, `docs/modules/finance.md`, `docs/STATUS.md`, `docs/BACKLOG.md` and this report. Do not begin continuous scheduling, PAPER, broker integration or champion promotion without a new approved slice.
