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

## Remediation/finalization — 2026-08-22

Post-implementation review found four foundation defects: stale interrupted rows could retain a unique key without readable result JSON; only the latest run was exposed; different keys could execute concurrently; and family multiplicity inferred historic attempts from the current variant count. The remediation preserves the original verification history above and corrects those stronger-than-implemented claims.

The durable contract is now:

- startup converts stale `Pending`/`Running` rows to complete queryable `FAILED` results with recovery time, interruption reason and linked partial experiments;
- the original idempotency row is immutable, so the same key returns its completed, running or recovered-failed run and a retry uses a new key;
- an atomic SQLite immediate transaction and partial unique running index enforce one global research execution, with safe `409 finance.research.alreadyRunning` behavior for another key;
- a `research_run_experiments` relation preserves every run association while deterministic experiment identity prevents evidence inflation;
- immutable `attempt_count` stores actual variants per experiment, so changing histories such as 3, 5 and 2 yield cumulative family totals 3, 8 and 10;
- next-session expectancy is consistently horizon 1;
- rejected, inconclusive, not-evaluable, promising and challenger totals are separate and reconcile per run and globally;
- bounded run/experiment catalogs and detail endpoints preserve historical audit access without arbitrary filtering or unbounded payloads.

Migration is additive and repository-native. Existing experiment metrics are not recalculated. Attempt counts are backfilled only from the row's own stored complexity metadata; unavailable legacy facts remain null. Public-domain backups include run/link records only when all linked experiments are already eligible, preventing restricted source evidence from being pulled into indefinite backup scope. The Finance UI now distinguishes uncertain verdicts and exposes latest-run recovery and attempt lineage under progressive disclosure.

The remediation adds no new methodology, indicator, scheduler, governor, macro input, negative control, DSR, PBO/CSCV, champion promotion or trading authority. Local finalization passed 461 API tests, 32 Sentinel tests and 114 frontend tests; Release solution and frontend production builds; API container build; documentation verification; Compose validation; diff whitespace and a sanitized private-key/token pattern scan. The recovery, partial-run and cross-key concurrency cases pass within the API total. No deployed production runtime was mutated; storage/reopen and bounded-cycle runtime behavior were verified with isolated persisted SQLite fixtures. Implementation commit `4f5e2b3272c92b3ad072bc13849b374824f50299` is published, and GitHub Actions run 32589143795 passed backend, frontend, documentation and secrets. BB-092 is remediated/finalized; the Scheduler/Orchestrator remains a separate future slice.

## Final evidence-selection remediation — 2026-08-22

Review found that the research cycle built current robustness evidence but then scanned the append-only history in strategy/evaluation-ID order and took the first rows. It now consumes only the build's exact feature revision, market revisions, evaluation IDs and checksums. Each selected row must agree across indexed columns and immutable JSON, have complete child evidence, and match exactly one approved `momentum/v1` and `sma-crossover/v1`. Normalized market sets must exactly equal the feature revision's source lineage. Selection order is the fixed approved-family order, not a timestamp or lexical ID.

The complete current pair is validated before any experiment is created. Missing SMA or momentum evidence, mismatched market lineage, wrong strategy version, missing relational evidence or conflicting immutable identity produces a controlled durable failed run; no older evaluation substitutes for it. Experiments continue to expose feature revision, exact market revisions and robustness evaluation ID through historical APIs. This changes evidence choice only: idempotency, single-flight, attempt accounting and Research Integrity scoring remain intact. Local verification passed 465 API, 32 Sentinel and 114 frontend tests; Release solution, frontend production and API container builds; documentation, Compose, diff and sanitized secret-pattern gates. No deployed runtime was changed. Commit `6796953533fb72cb12e5546964033e2939cf74c0` is published and GitHub Actions run 32590079318 passed backend, frontend, documentation and secrets. BB-092 final remediation is complete; Scheduler/Orchestrator remains separate future work.
