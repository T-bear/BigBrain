# BB-130C — research-detail loading characterization and extraction boundary

## Metadata

- Date: 2026-09-06.
- Source baseline: `3d923c42d75eaa405ebac9baba3c156430208255`; fetched HEAD/origin/main match.
- Scope: characterize existing frontend detail loading before any production refactor.
  Paired documentation scope: owner-supplied Alpaca evidence and future educational intent.
- Detta är en sanerad GitHub-version. Deterministic synthetic objects only; no private
  correspondence, market payloads, credentials or runtime data were gathered.

## Status

CHARACTERIZATION VERIFIED / EXTRACTION DEFERRED FOR OWNER/ARCHITECT REVIEW.
A/B and the first C observation hook remain published/CI verified. C overall is incomplete;
D has not started. This tests/documentation checkpoint is owner-approved, published and
CI verified: `4cdf1ff2fc3de420f9a7207fbf929dddda5a4c1e`, [Actions 34047595837](https://github.com/T-bear/BigBrain/actions/runs/34047595837)
passed backend restore/Release build/full tests, frontend install/test/build, documentation
and full-history secrets. Recovery is cleared. No production code, API contract, UI behavior, cache or scientific result changed.

## Evidence and responsibility map

FinanceObservation owns `detailsOpen` from native details `onToggle`; it does not unmount
its child JSX on close. Under ordinary production props, nine first-level reads start
on opening, not on cold Finance or merely renderable cached observation. Endpoint paths
below are relative to `/api/v1/modules/finance/`; identifiers are placeholders.

| Domain / endpoint | Trigger/dependencies | State and follow-up |
| --- | --- | --- |
| Features `features` | open, selected instrument, initialFeatures; bypass when initialFeatures matches selected ID | features; no selected-result request |
| Backtests `backtests` | open changes | catalog then first run ID (or null); `backtests/:runId` follows selectedRun |
| Robustness `robustness` | open, initialRobustness; supplied initial catalog bypasses read | catalog then first evaluation ID; `robustness/:evaluationId` follows ID and initialEvaluation |
| Datasets `datasets` | open changes | dataset catalog |
| Backups `backups` | open, initialBackups; supplied value bypasses read | backup inventory |
| Shadow `shadow/scorecard` | open, initialShadow; supplied value bypasses read | shadow catalog |
| Scheduler `research/scheduler/status` | open, initialResearchScheduler; supplied value bypasses read | scheduler status |
| Governor `research/governor/status` | open, initialResearchGovernor; supplied value bypasses read | governor decision |
| Operations `research/operations/status` | open, initialResearchOperations; supplied value bypasses read | operations status |

Observation/cache/refresh is already owned by useFinanceObservation. Overview, risk
status/evaluations and autonomous research are SECONDARY and remain separate effects
waiting for `snapshot !== null`; they are not part of these nine reads. No shared request
scheduler/cache is present or justified by this characterization.

### Open/close, selection and errors

- Closing cleans up/aborts first-level effects but retains their existing state. Reopening
  starts those reads again (unless their existing initial-prop guard bypasses them).
  No one-in-flight ref/deduplication across close/reopen or new retry system exists here.
- Catalog success resets selection to its first entry, even after a user selected another.
  Same selected ID does not rerun its result effect. Empty catalogs set selection null.
- Selected-result effects depend on ID, not `detailsOpen`. Closing does not abort a pending
  selected-result read; changing ID or unmounting does. Reopening/refetching a catalog with
  the same first ID does not refetch that result. Initial robustness with a selected ID
  may request its result while closed unless the matching initialEvaluation bypasses it;
  this is a supplied-prop case, not the ordinary cold production flow.
- Each domain handles its own errors. Most catches set that domain's data null, including
  AbortError; catalog errors do not clear selected ID or result. Features only clear for
  Error not named AbortError. Existing risk Promise.all grouping is outside this boundary.
- There is no independent loading/busy/error state for these detail domains. Null/empty
  data uses existing absence copy; old values can remain while a replacement is pending.
  Observation retry/busy state does not own detail reads.
- Cleanup passes AbortSignal through the existing GET reader/fetch and JSON decoding.
  There is no request-generation or response-ID guard. A reader that resolves after abort
  is not explicitly ignored; an earlier catch can clear newer state. This is a source
  finding, not a claim of a measured production network race.
- Ordinary observation refresh with unchanged selected instrument does not restart any
  first-level detail read. If refresh removes the selected instrument, existing selection
  reconciliation can trigger features; catalog/result IDs otherwise remain independent.
- Nine-read fan-out is preserved and confirmed by deterministic trigger counts. This is
  not a new browser concurrency/latency measurement or performance improvement.

### Verified presentation defect and reason to defer

Reproduction against unchanged production code: open Details, load backtest first and
its two-point equity curve, select second while its result is pending. The selected
summary displays `second / checksum-second` while the first run's curve remains visible,
without a result identity or loading marker. Rejecting the second request removes the
curve but retains the second summary. The new characterization test passes by asserting
this existing behavior; it is evidence of a defect, not an approved UX contract.

This can misattribute scientific presentation even though immutable results themselves
are not mutated. Robustness has analogous separate selected-summary/result ownership
by source inspection; it is a review target, not a separately reproduced runtime defect.
A combined useFinanceResearchDetails would bundle independent domains and conceal this
selection/result issue. A smaller backtest catalog/selection/result boundary is plausible,
but its display identity, pending/error and cancellation policy needs owner/architect
review before extraction or correction. No behavior correction is smuggled into a refactor.

## Tests and verification

Three added component characterizations cover all-nine cold/open/close/reopen/refresh/
unmount triggers, backtest catalog/result dependencies and the pending-result mismatch.
Existing BB-130B request-trigger and BB-130C observation/cache/retry/accessibility tests
remain in the focused command. Production code was unchanged throughout characterization.

```sh
cd src/BigBrain.Web
npm test -- src/finance/FinanceObservation.test.tsx src/finance/financeSnapshotCache.test.ts src/App.test.tsx src/components.test.tsx --reporter=dot
npm test -- --reporter=dot
npm run build
```

Focused result: 59/59 in four files. Full Web 190/190 in 26 files and production build
passed. Initial build caught a union-of-spies Promise inference error in the new test;
explicit Promise<never> fixed the fixture typing, then full Web and build passed again.
Documentation verification passed (224 Markdown / 90 unique BB IDs) after aligning report
headings with the required schema. Diff checks passed. Gitleaks v8.28.0 staged stdin scan passed with no leaks.
Scope check confirms only documentation and one test file changed. Backend replay is not rerun because
no backend, research model, evidence, schema or provider code was touched.

## Changes

The owner-approved publication also adds AGENTS.md's compact OWNER APPROVAL BLOCK.
It does not replace full canonical recovery state or weaken GitHub/Git approval rules.

- Tests: `src/BigBrain.Web/src/finance/FinanceObservation.test.tsx` only; no production extraction.
- Documents: current STATUS/BACKLOG/plan/module/testing/catalog, this report and the
  [Alpaca support record](finance-alpaca-owner-support-evidence-20260906.md) plus explicit
  BB-125/128A amendments and Finance provider/provenance/inquiry/roadmap evidence pointers.
- Investopedia future intent is unnumbered in BACKLOG and defined in the Finance module.
  Source context proposes hypotheses; existing deterministic research/risk retains authority.

## Security

Finance remains **RESEARCH / 0 SEK / NONE**. No Alpaca activation/acquisition/account/key,
  Investopedia integration, provider policy weakening, broker/order/PAPER/LIVE/AUTO,
  scientific retuning, persistence/schema change or deployment. No speedup or device approval.

## Remaining work

Obtain owner/architect decision on selected-result identity and pending/error presentation
before a separately bounded backtest-detail correction/extraction. Keep raw research results
immutable; decide whether to hide the prior curve or clearly identify prior-result content
while loading, and review cancellation/stale completion semantics. These are options for
review, not implemented decisions. Remaining C intake/persistence/composition/schema and D
quality gates are untouched. Do not start another checkpoint automatically.

## Resumption

Follow [STATUS](../../../STATUS.md), [BACKLOG](../../../BACKLOG.md),
[BB-130 plan](../../../architecture/bb-130-stabilization.md) and
[canonical recovery](../../../operations/codex-recovery.md); preserve unrelated mockups/ADRs.
