# BB-130C — bounded Finance observation lifecycle extraction

## Metadata

- Date: 2026-09-06.
- Baseline: `9f43229961adbbaf45902e919a5ee94e65abc774`, freshly fetched HEAD/origin/main.
- Scope: first C checkpoint, observation/cache/refresh only. No backend/intake/storage/
  composition/schema work, new features, deployment or performance optimization.
- Detta är en sanerad GitHub-version. Evidence uses deterministic synthetic test objects;
  no runtime payloads, credentials, private identities or sensitive logs are included.

## Status

Characterization passed before production edits; the bounded extraction is implemented
and locally automatically verified. Publication approval and CI remain pending. A/B are published and CI verified; C is in progress, not complete;
D has not started. This bounded implementation is owner authorized, but publication
requires separate explicit approval under AGENTS. No new owner/mobile acceptance.

## Evidence

### Characterization of the published source

| Responsibility | Existing behavior and owner | Verification |
| --- | --- | --- |
| Observation acquisition | FinanceObservation calls existing typed GET `/api/v1/modules/finance/observation`; AbortSignal passes through `api.ts` to fetch and JSON decoding | Existing first-read/cache test; App cold Finance contract |
| Initial seed | Explicit `initialSnapshot` bypasses cache read and observation listeners/refresh; it seeds state, not a continuously synchronized prop | Added explicit-seed characterization |
| Display cache | `financeSnapshotCache.ts` owns key/version 1, schema checks and explicit projection. Component reads once per mount; compatible cache renders immediately, marked stale with its fetch timestamp | Cache tests and immediate cached-render tests |
| Cache bounds | Rejects incompatible/malformed data, more than 16 instruments or 400 history points per instrument, and more than 512,000 serialized characters; no truncation/new TTL | Existing helper implementation and cache tests; helper unchanged |
| Cache persistence | Successful observation attempts projection/write and records browser fetch time separately from source time; write rejection does not fail a successful read | Added storage-quota failure characterization |
| Cold loading | No snapshot initially: one shared polite status loader inside aria-busy section | Existing initial-loader test |
| Refresh | Mount, visible-page return, browser online and manual retry share one callback and one promise ref; no interval, timeout or backoff is added | Existing overlap/retry tests; added combined visibility/online/no-poll characterization |
| Visibility | Only `visibilityState === visible` triggers that listener. Online independently refreshes even while hidden | Added visibility/online characterization |
| One-in-flight | Repeated events return the current promise; finally clears only that same promise, so a prior abort cannot clear a replacement request | Added StrictMode effect cleanup/replacement characterization |
| Instrument identity | Successful refresh preserves the selected ID if still present; otherwise selects the first non-null-price instrument or null. User-selected IDs need not themselves have a price | Added selection/reorder/removal test with exact feature-request sequence |
| Fresh refresh | Existing fresh content stays visible without a new stale strip while an ordinary refresh is pending | Added visibility/online characterization |
| Initial error/retry | Non-abort failure without cache shows an alert; retry keeps the failure message and disabled busy button until success, not the initial loader | Existing recovery test; added first-failure pending-retry characterization |
| Cached/degraded | Failure retains observation and fetch timestamp; retry uses only the button loader. Success updates cache/time and clears stale/failure | Existing repeated-failure, single-loader, wrapping-safe timestamp and recovery tests |
| Cancellation | Cleanup removes both listeners, aborts the controller and clears the promise ref. Same-realm Error named AbortError does not enter failure state; finally avoids clearing refreshing on aborted controller | Added mounted/unmounted signal/listener and StrictMode tests |
| Secondary reads | Component's boolean `snapshot !== null` enables overview, risk status/evaluations and autonomous research. Cache enables these before observation refresh completes. Ordinary refresh does not restart them | Existing B cold/refresh test; added cached-secondary/unmount test |
| Details/research | Nine existing first-level detail reads wait for open; selected catalog entries subsequently request results. Initial fixture overrides retain their existing bypasses | Expanded deferral test covers all nine initial detail APIs |
| Error isolation | Overview/autonomous fail independently; risk status and evaluations retain their existing Promise.all success/failure grouping. Detail effects retain their own error handling | Existing component tests and source characterization; these effects stay unchanged |
| Accessibility/motion | UI keeps BBLoadingIndicator's polite status/hidden dots, BBButton busy/disabled semantics and existing classes. Shared components.css disables loader animation under reduced motion; foundation.css also limits animation | Component/Finance tests; unchanged shared JSX/CSS inspected before extraction |
| Safety/status meaning | Read-only RESEARCH, 0 SEK/NONE, no trading controls, unavailable risk never grants approval, synthetic/real EOD/lineage/NOT EVALUABLE labels remain distinct | Existing safety/risk/OOS/autonomous/scheduler tests; no backend mutation |

The component also owns all panel rendering, chart helpers, signal-risk presentation,
features, catalog/result selection, datasets, backups, shadow and scheduler/governor/
operations states/effects. These are separate from the selected observation lifecycle.
No conflict with Finance safety/authority invariants was found in this scoped review.

Existing limits are preserved rather than silently fixed: the observation error guard
uses `instanceof Error` plus name, and cancellation relies on fetch honoring AbortSignal.
A nonconforming reader resolving after abort is not given a new stale-response guard.
An initial StrictMode test used a jsdom DOMException that did not satisfy the existing
Error check; its fixture was corrected to a same-realm Error named AbortError, without
changing production behavior. Cache helper checks do not turn browser data into authority.
Direct localStorage property access and live JSON response typing retain their existing
assumptions; this checkpoint does not introduce a storage framework or runtime API validator.

### Commands and results

Before new characterization: focused FinanceObservation/cache/App/shared component
tests passed 49/49 on the unchanged production source. Expanded characterization
then passed 56/56 (four files) against unchanged production code, before extraction.

```sh
cd src/BigBrain.Web
npm test -- src/finance/FinanceObservation.test.tsx src/finance/financeSnapshotCache.test.ts src/App.test.tsx src/components.test.tsx --reporter=dot
npm test -- --reporter=dot
npm run build
```

After extraction the same 56/56 tests passed. Full Web passed 187/187 in 26 files;
production build (`tsc -b && vite build`) passed. A source comparison confirmed the
refresh callback/effect was copied exactly and all secondary/detail effects, JSX and
presentation helpers remain byte-identical to baseline. No CSS/cache/API/backend edits.
An initial test command from repository root found no package.json; rerunning from the
Web directory passed. Backend tests/scientific checksum replay were not rerun: this
checkpoint moves only frontend ownership and does not execute or modify research.

Root checks: `node scripts/verify-documentation.mjs`, `git diff --check`,
`git diff --cached --check` and staged gitleaks v8.28.0; passed: documentation 222 Markdown / 90 unique BB IDs, clean working/staged diff,
and no leaks in the staged patch. No new browser
benchmark or scientific replay is requested; no performance improvement is claimed.

## Changes

Implemented boundary: `useFinanceObservation` owns the existing seed/cache read,
snapshot, stale/failed/refreshing/fetch time, one-in-flight/controller refs, refresh and
cleanup listeners. The selected instrument ID and its setter accompany that lifecycle
because successful observation replacement already reconciles selection in the same
promise callback. Moving this to a later UI effect would change ordering and could
start a feature request for a removed instrument. Chart/feature rendering and fetching
do not move into the hook.

FinanceObservation consumes that state and callback and retains all JSX,
secondary/detail effects and scientific presentation helpers. Existing cache helper,
API contracts, shared controls and CSS remain authoritative and unchanged. No generic
cache, request scheduler, state machine framework, endpoint or dependency is added.

### Changed files

- Code: `src/BigBrain.Web/src/finance/FinanceObservation.tsx`,
  `src/BigBrain.Web/src/finance/useFinanceObservation.ts`.
- Tests: `src/BigBrain.Web/src/finance/FinanceObservation.test.tsx` (seven added cases
  plus all-nine detail-trigger assertions; existing cache/App/shared tests retained).
- Documentation: STATUS, BACKLOG, BB-130 plan, Finance module, TESTING, this report,
  REPORT-CATALOG and temporary canonical recovery state. README, START-HERE, ROADMAP,
  ARCHITECTURE, ADRs and security/runbooks were assessed; no new architectural or
  operational decision requires changes there.

## Security

Finance remains **RESEARCH / 0 SEK / NONE**. No scientific-result reinterpretation,
provider activation, entitlement change, fail-open gate, broker/order, PAPER/LIVE/AUTO,
capital allocation, schema migration or immutable evidence mutation. Display cache
is not acquisition/execution authority. No host/Sentinel privilege or auth change.

## Remaining work

- Obtain separate publication approval, publish this verified extraction and check CI.
- Remaining FinanceObservation rendering, secondary/detail effects and catalog selections
  are not refactored here. The B nine-read details fan-out, payload sizes, intermittent
  latency and browser-warning evidence remain documented debt, not performance targets here.
- C still includes further cohesive frontend work, intake, provider-neutral persistence,
  explicit composition and structural schema authority; D still owns new quality gates.
- No deployment or new physical-device UX acceptance; earlier BB-128B/C owner evidence
  remains historical and does not become new approval by passing deterministic tests.

## Resumption

Use [START-HERE](../../../START-HERE.md), [STATUS](../../../STATUS.md),
[BACKLOG](../../../BACKLOG.md), [the C plan](../../../architecture/bb-130-stabilization.md)
and [recovery](../../../operations/codex-recovery.md). Preserve unrelated mockups/ADRs.
Do not automatically start another C checkpoint. After this checkpoint is verified,
prepare it for owner publication approval; then recommend a separately scoped
Finance research-detail loading ownership characterization before further extraction.
