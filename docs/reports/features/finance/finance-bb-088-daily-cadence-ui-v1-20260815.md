# BB-088 prospective daily cadence and Finance UI v1.0 hierarchy

## Metadata

- Date: 2026-08-15
- Baseline: `0ab8c7470a1b16e399728ea564cfeb34962d3069`
- Provider/observation: EODHD Free, `CURRENT EOD / PROSPECTIVE EOD`
- Mode/budget: `RESEARCH`, 0 SEK; no broker, order, PAPER, LIVE/AUTO or self-learning
- Implementation commit: `8653c44fd05fc76460e460ec5c0fef8d4cc9e7ed`

## Status

Implemented, full-regression verified, deployed, bounded runtime/restart verified and published.
GitHub Actions run #46 passed documentation, frontend, secrets and backend. No ADR was added: the
cadence applies ADR 0026 recovery and ADR 0029 immutable knowledge-time evidence; the approved UI
hierarchy is a product/roadmap decision rather than a new cross-system boundary.

## Changes

`FinanceProspectiveCadenceWorker` replaces startup-only acquisition/shadow workers with one
recovery-gated loop. Local state is checked every 30 minutes. External acquisition is permitted
only on weekdays after 22:00 UTC, remains limited by the existing once-per-symbol/day entitlement
gate and uses the adapter's maximum two retries with 250/500 ms exponential delays. Provider
failures fail closed; a holiday/no-new-session result is healthy. Feature build and shadow cycle
are idempotent on every internal cycle.

Existing genuine predictions can append one outcome after a later source session. Repeated cycles
do not duplicate prediction or outcome records. The BB-087 current-session age, source-feature
membership, clock and later-observation anti-backfill gates remain unchanged. Multiple missed
sessions may mature predictions that already existed, while missing predictions are never invented.

Read-only `GET /api/v1/modules/finance/overview` and
`GET /api/v1/modules/finance/cadence/status` expose sanitized backend truth. The overview derives
actual watched-universe breadth and strategy agreement; it never fabricates Nasdaq/S&P index data,
positions, P/L or real-time freshness. `TargetLong` is displayed as POSITIVE research posture,
`TargetFlat` as NEGATIVE, and ties as NEUTRAL with counts visible. The UI order is Market today,
BigBrain now, Prospective result, then Details & research. Historical results remain separate.

Prospective charts stay empty until at least two evaluated sessions. If available, the defined
series is cumulative hypothetical return of an equal-weight shadow-decision basket per session,
explicitly not actual portfolio return. Layout collapses from three columns to one below 760 px;
signal state uses text/symbol plus color and technical details remain keyboard-accessible through
native `details`/`summary`.

## Evidence

- Focused backend: 15 tests passed.
- Focused frontend: 8 Finance tests passed.
- Full backend: 412 API tests and 32 Sentinel tests passed; Release build passed with zero warnings.
- Full frontend: 111 tests passed; production build transformed 57 modules.
- Documentation verifier passed 155 Markdown files/88 unique backlog IDs; Compose config and
  `git diff --check` passed.
- MSBuild `MSB1025`/named-pipe permission failure reproduced once in the restricted environment;
  `dotnet build-server shutdown` plus `--disable-build-servers -m:1` resumed deterministic testing.
- Deployed API/Web were healthy before and after a second restart. The Saturday worker logged a
  healthy `no-provider-check`: latest canonical session 2026-08-14; 24 valid pending predictions,
  zero evaluated outcomes, 24 invalidated audit rows; no new rows or rewritten IDs.
- Runtime overview reported eight tracked instruments, 3 up/5 down/0 unchanged, eight signal rows,
  `BOOTSTRAPPING` and no prospective graph. Clock integrity was healthy.
- Existing backup inventory remained one COMPLETE set: WIKI `Indefinite/eligible`; EODHD
  `SubscriptionOnly/restricted`.
- Whole-worktree gitleaks found only pre-existing ignored local `.env`/Sentinel identity material
  plus one tracked documentation false positive; no BB-088 file or Git-tracked new secret was found.
  The authoritative Git-history gitleaks publication gate subsequently passed.
- GitHub Actions run #46: documentation, frontend, secrets and backend all `success`.
- Multi-real-session proof: not available in a same-day Saturday implementation; cadence must
  accumulate this evidence naturally.

## Retention and backup

Cadence persistence contains operational timestamps/status only. Overview data is derived on read.
Predictions and outcomes retain EODHD source-dependent deletion scope and remain excluded from the
indefinite public-domain backup class. No provider market-data copy or new backup class was added.

## Security

The new endpoints are GET-only, return no credential or filesystem path and expose no mutation,
mode promotion or execution surface. Provider payloads are not logged. Ordinary tests are
network-free. This report contains no secret, raw market payload, private runtime identity or
sensitive path.

## Remaining work

- Saturday 2026-08-15 supplied no legitimate new US market session; correctly, no provider check,
  session, prediction or outcome was fabricated.
- Holiday detection relies on the provider returning no new canonical session after a bounded
  weekday check; weekends are skipped deterministically. No full exchange-calendar dependency was
  added.
- Strategy quality cannot be inferred from the bootstrapping sample.

## Future improvement proposals — NOT approved for implementation

- Observe the deployed cadence across multiple genuine sessions and add dated evidence without
  changing strategy parameters.
- Consider richer named index summaries only after a legitimate approved index source exists.
- Refine the Finance UI from actual owner feedback after prospective outcomes accumulate.
- Build M5 Hard Risk Engine and complete the planned security/pentest baseline before any future
  PAPER eligibility discussion.

## Resumption

Start with ADR 0026, ADR 0029, `FinanceProspectiveCadence.cs`, `FinanceOverview.cs`, the BB-088
status/backlog entries and this report. The exact next slice is continued prospective
multi-real-session evidence observation; do not infer PAPER eligibility.

## Sanitization

Detta är en sanerad GitHub-version. It contains no provider credential, raw market payload,
private runtime identity, sensitive path or raw log.
