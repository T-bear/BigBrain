# BB-130C E1 — robustness selected-result identity correction

## Metadata

- Date: 2026-09-09. Detta är en sanerad GitHub-version.
- Accepted main baseline: `87d53241439b6fcbd97a07cd5a3986b59653eab9`.
- Correction branch: `bb-130c/robustness-selected-result-identity-fix`, directly from main.
- Historical blocker SHA: `508cec3baceba4452df3431e7e0eab87dce50a3f` on
  `bb-130c/robustness-selected-result-identity`.
- [Pinned blocker report](https://github.com/T-bear/BigBrain/blob/508cec3baceba4452df3431e7e0eab87dce50a3f/docs/reports/features/finance/bb-130c-robustness-selected-result-identity-20260908.md).
  That evidence remains **BLOCKER HANDOFF — NOT MERGEABLE**. It is not correction ancestry;
  its report is not rewritten or imported as a successful implementation report.

## Status

**ACCEPTED / MERGED TO MAIN / CI VERIFIED**.
Approved candidate `b34178aa40024cb8e7e953c72706bd88b3dda688` merged as `1204322a8764df33c0182db5b1e98d3cd6912d81`.
[Main CI run 34312516174](https://github.com/T-bear/BigBrain/actions/runs/34312516174)
passed backend, frontend, documentation and secrets on 2026-09-09.
The selected-summary/detail defect is resolved in accepted source. Implementation is limited
to Web presentation/request ownership. E2 **NOT STARTED** remains the known exit requirement;
BB-130C **NOT READY** until E2 acceptance, BB-130D **NOT STARTED**.
Accepted non-blocking debt deferrals remain in force. No deployment or runtime/owner UX claim.

## Root cause and identity contract

The prior component selected a catalog summary independently from its retained detail state.
Selection B immediately changed the summary while loaded A detail remained eligible to render.
The detail promise had no stale-completion guard; abort alone could not enforce association.

The API catalog and detail both expose immutable `evaluationId` and `checksum`. The existing
backend projects them from the same stored evaluation. This correction compares both **exactly**;
it does not compute, normalize or redefine either value. The UI does not cryptographically
revalidate the payload or recalculate scientific evidence.

**Invariant:** visible detail must match the selected evaluation ID and its selected catalog
summary's ID/checksum. One selected-summary derivation feeds both display and association.
No matching summary or detail means no rendered diagnostic detail.

## Changes

Only production file: `src/BigBrain.Web/src/finance/FinanceObservation.tsx`.

- Derive the selected robustness catalog summary once. Derive `visibleEvaluation` by matching
  selected ID and catalog checksum, so a selection render excludes A immediately, before effects.
- On selected ID/checksum change, invalidate loaded evaluation and request the selected detail.
  Matching initialEvaluation may be restored only for its own ID/checksum; reselecting it cannot
  leave the previously selected detail visible.
- An effect-local `current` flag is invalidated by cleanup **before** abort. Both successful and
  rejected obsolete promises are ignored after cleanup. Abort remains resource cancellation;
  render-time matching independently protects the selection transition before effect cleanup.
- A returned ID/checksum mismatch yields no detail, using the existing empty failure behavior.
- Only `visibleEvaluation` reaches RobustnessDetails. No new hook, framework, cache, DI or endpoint.

Pending/error behavior: B summary stays; A diagnostic cards disappear. B detail appears on
matching success. Failure or identity mismatch leaves no detail; no scientific fallback,
new status framework or retry button is introduced. The existing retry route is reselection
away and back; clicking the current selection still does not reissue its read.

Catalog request behavior is retained: runtime Details reopen refetches catalog and selects its
first entry; initialRobustness avoids that catalog read. Closing Details hides mounted content
without cancelling its selected detail request. Pending completion while closed may be retained,
but render-time association prevents mixed evidence on reopen or subsequent selection. Unmount
and selection cleanup still abort requests. Observation refresh does not restart these reads.

Ordinary backtests remain in their existing hook, untouched. Detail diagnostic formatting,
summary values, catalog selection policy, scientific calculations and stored evidence are unchanged.
This is a bounded identity correction, not a research-detail refactor or performance optimization.

## Evidence

`src/BigBrain.Web/src/finance/FinanceObservation.test.tsx` reproduces the four blocker scenarios
on clean main-derived history. Their assertions are retained (test labels no longer say BLOCKER).
Before production change: **2 passed / 2 failed**, for the same five identity assertions as the
published blocker. No test was skipped, marked expected-failure or weakened to permit the fix.

After correction: **8 E1 tests pass**, including four additional cases (two parameterized):

| Case | Verified result |
| --- | --- |
| A loaded → B pending | B summary visible; no A parameter/cost detail, including reselect after close/reopen. |
| Pending A → B success → late A success | B detail remains; aborted A completion cannot replace it. |
| A loaded → B success | Evaluation/checksum, parameter/cost, train/test and walk-forward display consistently belong to B. |
| B failure / reselect | Detail stays empty on failure; no old fallback; reselect issues current B request and shows B on success. |
| Close/reopen | Runtime catalog resets to A as before; subsequent B selection excludes A. Initial catalog retains B, then reselecting initial A restores only A. |
| Unmount | Selected request signal is aborted. |
| Obsolete rejection | Late A AbortError cannot erase resolved B detail or resurrect A. |
| Wrong returned evaluationId or checksum | No detail is displayed beside selected B summary. |

Deferred promises make ordering deterministic without sleeps. API functions are mocked and
unrelated fetch rejects locally; all identities and numerical markers are synthetic. The
abort-ignoring completion is a UI guard test, not a claim about normal browser fetch behavior.

## Verification

Commands from repository root unless shown otherwise:

```sh
npm --prefix src/BigBrain.Web test -- --run src/finance/FinanceObservation.test.tsx -t 'BB-130C E1' --reporter=dot
npm --prefix src/BigBrain.Web test -- --run src/finance/FinanceObservation.test.tsx --reporter=dot
npm --prefix src/BigBrain.Web test -- --reporter=dot
npm --prefix src/BigBrain.Web run build
node scripts/verify-documentation.mjs
git diff --check
git diff --cached --check
git diff --cached | docker run --rm -i --network none zricethezav/gitleaks:v8.28.0 stdin --redact --no-banner
```

- E1 **8/8**; entire FinanceObservation **43/43**, including all 35 existing observation,
  research-detail, ordinary backtest identity and interaction tests.
- Full Web suite: **199/199 tests in 26 files**, zero failures/skips.
- Production Web build: **passed**, TypeScript and Vite. Vite emitted a CSS plugin timing advisory;
  no performance improvement or zero-warning claim is made.
- Documentation verifier: **passed, 234 Markdown files / 90 unique backlog IDs**.
- Working/staged diff checks: **passed**. Staged Gitleaks v8.28.0: **no leaks**.
- Bounded publication: one production file, one test file, eight documentation files.
  Unrelated mockups and ADR proposals excluded. Local evidence above predates acceptance;
  actual main CI and exact accepted candidate/merge identities are recorded in Status.
- Backend/API/Sentinel/backend Release/Compose not rerun: no backend, shared/API contract,
  schema or Compose changes. No production Finance data used.

## Correction review

| Question | Result |
| --- | --- |
| Can B render A detail? | No: render requires selected ID plus matching catalog ID/checksum before child receives detail. |
| Can an obsolete response supersede current evidence? | Inactive effect completions are ignored on both paths. Render association also excludes mismatches during transition before cleanup. |
| Is abort the only guard? | No: independent lifetime flag plus render association. |
| Can error/reselect revive old evidence? | Current failure clears detail; old failure is ignored. Reselect requests/restores only selected identity. |
| Close/reopen safe? | Identity gate remains active while state is retained; both runtime and initial-catalog paths tested. |
| Ordinary backtests/scientific behavior preserved? | No changes in their production code or data contracts; existing Web tests pass. No scientific engine or persistence touched. |

## Security

Finance remains **RESEARCH / 0 SEK / NONE**. No scientific, verdict/scoring/eligibility,
NOT EVALUABLE, lineage/checksum, canonical v2/legacy, BB-123/124/127/129A or immutable writer
semantics changed. No backend, schema/DDL/migration, historical rewrite, production-data access,
provider activation, broker/orders/PAPER/LIVE/AUTO/capital, deployment or new infrastructure.
No new Alpaca support evidence or integration is included. Sanitized synthetic evidence only.

## Remaining work

E1 correction is accepted, merged and CI verified. E2 remains separately scoped and NOT STARTED;
C remains NOT READY until E2 acceptance. Existing broader debt stays deferred. E2 and D were
not authorized to start by this merge. No deployment/runtime/device UX approval is implied.
No production incidence audit, device-specific manual UX approval, adversarial JSON integrity
validation or performance claim. This correction does not certify every unrelated catalog race.

## Resumption

The checkpoint review note is cleared in [canonical recovery](../../../operations/codex-recovery.md).
Main contains the accepted correction and publication evidence. The historical blocker was
excluded from ancestry, never merged and remains NOT MERGEABLE. The next recommendation is
separate authorization for E2; do not start E2, BB-130D, provider work or deployment automatically.
