# BB-130C E1 — robustness selected-result identity blocker

## Metadata

- Evidence date: 2026-09-09; requested checkpoint filename retains 20260908.
- Verified main baseline: `87d53241439b6fcbd97a07cd5a3986b59653eab9`.
- Branch: `bb-130c/robustness-selected-result-identity`, directly from main.
- Scope: synthetic Web characterization only. Detta är en sanerad GitHub-version.
- Prior authority: [accepted reader/exit assessment](bb-130c-backtest-reader-exit-assessment-20260908.md)
  and [ordinary backtest identity](bb-130c-backtest-result-identity-20260907.md).

## Status

**DECISION B — PRE-EXISTING IDENTITY DEFECT REPRODUCED.**
**BLOCKER HANDOFF — NOT MERGEABLE**. This branch contains characterization/evidence,
not a correction or an identity-safe review candidate. Production code is unchanged.
The exact remote handoff SHA is available from the published branch and delivery response;
no merge, green implementation CI, deployment or runtime verification is claimed.

E1 evidence is ready for architect review but E1 is **BLOCKED / NOT ACCEPTED**.
E2 campaign SQLite replay/reload remains **NOT STARTED**. BB-130C remains **NOT READY**;
BB-130D is **NOT STARTED**. The accepted post-BB-130 debt deferrals remain unchanged.

## Current request and identity map

Source: `src/BigBrain.Web/src/finance/FinanceObservation.tsx` (selection/effects and
RobustnessDetails), `src/BigBrain.Web/src/api.ts`, `src/BigBrain.Web/src/types.ts`,
and the read-only robustness routes in `src/BigBrain.Api/Finance/FinanceEndpoints.cs`.

| Responsibility | Current contract / identity |
| --- | --- |
| Catalog ownership | `robustness` holds FinanceRobustnessCatalog: plans plus evaluation summaries. Each summary has evaluationId/checksum, strategy/plan, score/verdict, diagnostic counts, market/feature lineage and limitations. |
| Initial selection | `selectedEvaluation` starts at initialRobustness.evaluations[0], or null. Runtime catalog success also selects the first evaluation. |
| Catalog trigger | Details open requests GET `/api/v1/modules/finance/robustness`, unless initialRobustness exists. Cold runtime Finance makes no robustness read. Catalog restarts on reopen and resets selection to its first entry on success. |
| Selection request | A changed selectedEvaluation requests GET `/api/v1/modules/finance/robustness/{evaluationId}`. Only this detail request is triggered by a robustness button; IDs are encodeURIComponent-encoded. The effect is not dependent on detailsOpen or observation refresh. |
| Detail ownership | `evaluation` holds FinanceRobustnessEvaluation, initialized independently from initialEvaluation or null. `.then(setEvaluation)` replaces it without checking selectedEvaluation, returned evaluationId/checksum or abort state. No clear occurs when selection changes. |
| Optional initial props | Matching initialEvaluation.evaluationId skips acquisition. No render-time identity check binds initialEvaluation to the current catalog selection. This path is source inspection only; no new fixture-prop mismatch test was needed for the runtime blocker. |
| Rendered summary | Derived from catalog matching selectedEvaluation, falling back to first entry. Its score, plan, lineage, checksum and limitations switch immediately with selection. |
| Rendered detail | RobustnessDetails receives only evaluation. It renders train/test returns, degradation, parameter verdict/median/range, cost points and walk-forward percentages/window count without any selected identity prop/check. |
| Other result-derived state | No separate robustness curve, selected-result loading, error or retry state exists. The numerical score is catalog-derived, diagnostic cards are detail-derived. No cached robustness detail framework exists. |
| Failure | Any rejection, including AbortError, sets evaluation to null. Catalog/selected summary stays. No robustness-specific error/retry control is rendered. Clicking the already-selected button does not retry; selecting away and back starts a new read. |
| Cancellation | Selection dependency cleanup and unmount abort detail's controller. Catalog aborts on close/unmount. Closing Details does not unmount or cancel the selected detail effect. No stale-response guard supplements abort. |
| Close/reopen | Children remain mounted but hidden by native details. Detail state is retained; pending detail may complete while closed. Reopening catalog chooses its first entry again. Re-selecting B can again expose retained A detail. |
| Observation refresh | Observation online/visibility/retry lifecycle is separate; it does not retrigger robustness catalog/detail unless their own dependencies change. No observation retry doubles as robustness retry. |
| Ordinary backtests | Separate useFinanceBacktestDetails selection/result state and endpoints. Robustness selection does not select an ordinary backtest or its curve. No ordinary backtest code changed. |
| API boundary | Catalog returns JSON; detail returns JSON by requested ID or 404. getJson passes AbortSignal to fetch, parses JSON, throws ApiError for non-success. TypeScript assertion is not runtime response-identity validation. No new backend finding or change. |

The catalog/evaluation data are provider-neutral immutable research evidence under ADR 0025.
The defect is **presentation association**, not proof that stored evaluations, calculations,
checksums or eligibility are wrong. NOT EVALUABLE / insufficient-data meaning is unchanged.

## Evidence

Four tests were added to the existing FinanceObservation test file, reusing its snapshot,
Details toggle and cleanup helpers. All IO is synthetic: catalog/detail API functions are mocked;
unrelated fetch calls reject locally. No timing sleeps, server, database or production data.
A/B have distinct strategy, evaluation/checksum, feature, parameter/cost and numeric markers.
The tests render the real FinanceObservation and RobustnessDetails components.

| Scenario | Exact unchanged-production result |
| --- | --- |
| A loaded, then B success | PASS control: A summary plus A parameter/cost/train-test/walk-forward markers; after B success all checked markers belong to B and A detail is absent. |
| A loaded → B pending | FAIL: visible `evaluation-B / checksum-B` appears while `parameters-A` and `cost-A` remain rendered. This defect needs no failed cancellation or unusual transport. |
| Close/reopen while B pending | Same failing test: closing does not abort B detail; reopening catalog resets selection to A, aborting B. Selecting B again leaves A detail with B summary until B resolves. Final B success is coherent. |
| Pending A → B; B completes before late A | FAIL: A signal is aborted, B first renders coherently, then deferred A completion replaces B detail. Summary still says B; parameter A returns and parameter B disappears. |
| B error / reselect retry | PASS control: B rejection removes A detail and retains B summary; no error alert or retry button. Same B click makes no request. Selecting A then B issues a new B request and returns coherent B evidence. This proves that tested retry route only; it does not establish universal race safety. |
| Unmount | PASS control: B detail signal is aborted on unmount. Abort issuance does not itself prove stale-response exclusion. |

The late-response mock deliberately completes after abort. This isolates the component's
missing guard; it is not a claim that native fetch normally ignores abort. The primary
A-loaded/B-pending defect is independently reproduced without that assumption. Late rejected
obsolete requests can clear evaluation according to source code, but a separate deterministic
rejection-race test was not added after the stop gate. No browser/production incidence audit.

Commands (run from `src/BigBrain.Web` on 2026-09-09):

```sh
npm test -- --run src/finance/FinanceObservation.test.tsx -t 'BB-130C E1' --reporter=dot
npm test -- --run src/finance/FinanceObservation.test.tsx --reporter=dot
```

- New E1 tests: **2 passed / 2 intentionally failed**, 35 existing tests excluded by filter.
- Entire focused file: **37 passed / 2 intentionally failed / 0 skipped**. All 35 existing
  observation/research-detail/backtest identity tests still pass. Both runs exit 1 for the
  same two blocker tests, comprising five soft assertion failures, not five failing tests.
- An initial npm invocation from repository root found no package.json (exit 254); it ran
  no tests. The commands above ran from the correct Web directory.
- Full Web suite and production build are **not run** under the defect-case stop rule.
  This is not green implementation. Backend/API/Sentinel/Release/Compose are not applicable:
  no production/backend/API/schema/Compose contract changed.
- Documentation verifier: **passed, 234 Markdown files / 90 unique backlog IDs**.
  The sandbox initially blocked its Git subprocess with EPERM; the approved rerun passed.
- `git diff --check` and `git diff --cached --check`: passed.
- Staged Gitleaks v8.28.0 (`git diff --cached | docker run --rm -i --network none
  zricethezav/gitleaks:v8.28.0 stdin --redact --no-banner`): **no leaks**.
- No CI success is inferred from branch publication.

## Changes

Production files changed: **none**. Only
`src/BigBrain.Web/src/finance/FinanceObservation.test.tsx` and the eight documentation
files listed in canonical recovery are part of this handoff. No assertions are skipped,
marked expected-failure, or weakened to permit the bug. No ordinary backtest implementation,
loading/cache contract, scientific result, immutable ID or stored evidence changed.

## Security

Finance **RESEARCH / 0 SEK / NONE**. No broker/orders, PAPER/LIVE/AUTO, capital, providers,
Alpaca integration/evidence reconciliation, production Finance access, historical rewrite,
schema/DDL/migration, deployment or new infrastructure. All example identities are synthetic;
no secrets, private payloads, raw production logs or sensitive filesystem paths are published.

## Remaining work

Architect review of this exact **non-mergeable** evidence branch. A separate explicitly
authorized main-derived correction must preserve one selected robustness summary/detail
identity during pending/success/error/retry and stale completion. It must decide pending/error
presentation from existing Finance UX, and verify cancellation and close/reopen without
changing calculations or ordinary backtests. No particular hook/extraction is prescribed here.

E1 cannot close on this evidence alone: correction and architect/owner acceptance remain.
E2 is still unstarted and separately authorized; BB-130D waits for accepted E1/E2 resolution.
Broader race permutations and real-browser/owner UX verification remain unverified, not
additional proven defects. No performance or historical production-damage claim.

## Resumption

Do not merge this blocker branch or use it as correction ancestry. Follow AGENTS.md and
[canonical recovery](../../../operations/codex-recovery.md); review the published branch SHA.
The next safe action is architect review followed by a separately authorized correction from
verified main. Do not start E2, BB-130D, provider work or deployment automatically.
