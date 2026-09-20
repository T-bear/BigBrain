# Testa BigBrain

## BB-130D2 frontend formatter cleanup — 2026-09-20

**IMPLEMENTED / TESTED / REVIEW CANDIDATE ONLY — not accepted, not merged, not deployed.**
Baseline `5cb179d2558fb9bddc8f256d0fcc06251826a1f5`, verified equal to `origin/main` before
and immediately before publication; branch `bb-130d/frontend-formatter-cleanup`.
Agent handoff **Codex → Claude**: the Codex session was interrupted by an exhausted usage
limit having created only the branch; Git evidence proved no tracked change existed and that
formatter write mode had not run. Claude reproduced the contract and baseline independently.

The accepted formatter write operation rewrote exactly the **80** characterized nonconforming
files of the **83**-file scope (15 `.ts`, 65 `.tsx`; 12,873 insertions, 2,498 deletions). The
changed set equals the characterized set exactly, and the three already-clean files
`src/main.tsx`, `src/test/setup.ts` and `vite.config.ts` remain untouched. No application
source was hand-edited and no package, config, CI, backend, CSS, JS or runtime file changed.

**Discovered deviation, owner-accepted and deliberately preserved.** `format:check` did not
exit 0 after one write pass: `src/ThemeControl.test.tsx` is not a Prettier 3.9.6 fixed point
after one pass. A second pass converges and a third is a no-op; a sweep of all 83 files shows
exactly 82 of 83 reach a fixed point on pass 1. The change is member-chain line breaking in a
test setup block with no token, argument or literal change. Work was halted under the
checkpoint stop rule. The owner approved option (a) on 2026-09-20: accept the converged
two-pass result as the cleanup candidate baseline with the deviation documented. This does
**not** establish running Prettier twice as the normal workflow; the committed source is at
the stable fixed point, and future formatter CI must verify that committed source is already
conforming and must **not** depend on a second write pass.

Verification: `format:check` exit 0 with 83/83 conforming and a further `npm run format` is a
no-op; tests **199/199** in 26 files; focused Finance **46/46**; production build exit 0 with
70 modules transformed; `npm audit --json` exit 0 with zero findings. Semantic verification
used Prettier 3.9.6's own bundled TypeScript parser, because TypeScript 7.0.2 here is the
native port with no JavaScript compiler API. All 83 files parsed without error and every
semantic invariant is identical across all 80 reformatted files: identifiers, imports,
exports, numbers, regexes, templates, JSX element names, JSX props, rendered JSX children,
operators, object keys, executable statements, member paths and comments. Differences are
representation only: JSX text re-wrapping with identical collapsed text, 28 `{' '}` separators
whose only literal delta is the added space, and one ASI-guard `EmptyStatement`. Production
artifacts: CSS, icons and manifest byte-identical; `index.html` differs only in the
content-hashed asset name; the JS bundle differs by 81 bytes, proven to be adjacent JSX
text-child splits only, with concatenated literals byte-identical at 82,558 bytes and
literal-elided code skeletons byte-identical at 332,891 bytes.
[Method, limitations and full evidence](docs/reports/features/platform/bb-130d2-frontend-formatter-cleanup-20260920.md).

Finance: no backend file changed at all, so deterministic identities, checksums, lineage,
dataset/revision, campaign, holdout/OOS, robustness, cost, entitlement/fail-closed and
NOT EVALUABLE semantics cannot have been affected. Four of six Finance frontend files have
byte-identical normalized ASTs, preserving BB-128B/C cache, stale/degraded and single-loader
semantics and BB-130C selected-result identity. Finance remains **RESEARCH / 0 SEK / NONE**.

A/B COMPLETE; C COMPLETE / EXIT APPROVED; D IN PROGRESS. D1, backend cleanup and the backend
format gate remain accepted and enabled; D2 characterization, triage, dependency maintenance
and formatter tooling remain accepted. The **frontend formatter CI gate remains NOT STARTED /
NOT ENABLED** and lint remains deferred. No deployment, runtime, device or owner UX approval
is claimed. Merge requires explicit owner approval of the exact reviewed branch SHA after
verifying that SHA is unchanged and that `origin/main` has not advanced unexpectedly.

## BB-130D2 formatter tooling — 2026-09-17

**ACCEPTED / MERGED / CI VERIFIED.** Baseline
`d7785b4ef2fea271cb78f0019c486b60acdad210`; branch `bb-130d/frontend-formatter-tooling`.
Prettier 3.9.6 pinned dev-only; accepted config and explicit TS/TSX scripts installed.
Exactly 83 selected, 80 nonconforming; repeated check exit 1 expected for historical debt.
No write formatting or source/test changes. npm ci, 199/199 tests, build and audit zero verified;
fresh post-merge verification on 2026-09-17 confirms the same results.
[Scope proof, commands and limits](docs/reports/features/platform/bb-130d2-frontend-formatter-tooling-20260917.md).
A/B complete; C complete/exit approved; D in progress. Prior D1/backend cleanup/gate and D2
characterization/triage/maintenance accepted; backend gate enabled. Tooling accepted;
frontend cleanup/gate NOT STARTED / NOT COMPLETE, lint deferred. No deployment/CI/runtime
or scientific behavior change. Finance RESEARCH / 0 SEK / NONE. Next proposed checkpoint: BB-130D2 — Frontend Formatter Cleanup, separately authorized;
80-file formatting requires semantic/JSX review and strong regression verification. Earlier dated entries retain historical scope.


Owner/architect approved exact candidate `d71ab8553871d9c5eb89c151ad8cb0392e0de08a`,
merged unchanged as `ba0037ee41f16476e74037f090e83e2e3a78f205`.
[Merge CI 35190196135](https://github.com/T-bear/BigBrain/actions/runs/35190196135): SUCCESS
for backend/frontend/documentation/secrets. Backend restore → actual dotnet format check →
Release build → tests passed. Frontend npm ci/test/build passed; no frontend formatter CI gate.
Post-merge local npm ci/test/build/audit: exit 0, 199/199 tests, production build, audit zero.
Prettier 3.9.6 dev-only, config/scripts unchanged; exact scope 83, 80 nonconforming,
format:check exit 1 expected, output identical to characterization. No write formatting.

## BB-130D2 dependency maintenance — 2026-09-16

**ACCEPTED / MERGED / CI VERIFIED.** Baseline
`914d35e596cd066b846e4f6d1c59632b1be84137`; branch `bb-130d/frontend-dependency-maintenance`.
Vitest 4.1.11 (+ seven aligned family packages), PostCSS 8.5.23, nanoid 3.3.18,
undici 7.29.0; no other version upgrades or persistent overrides. Tests 199/199 and
production build pass; audit 5 findings → 0, all eight prior GHSAs remediated by installed
patched dependencies on accepted main. Production inventory: 70 modules, none affected.
[Exact graph, tooling limitations, commands and rollback](docs/reports/features/platform/bb-130d2-frontend-dependency-maintenance-20260916.md).
No source/tests/CI/runtime changes, deployment, formatting or lint tooling. Finance RESEARCH / 0 SEK / NONE.
A/B complete; C complete/exit approved; D in progress. D1/backend cleanup/gate and D2
characterization/triage remain accepted; backend gate enabled. Maintenance accepted; frontend cleanup/tool installation/gate NOT STARTED / NOT COMPLETE.
Owner/architect approved exact candidate `b043bffe21c3cc05f2c57c087d6aac3f26fa0e84`, merged unchanged as
`55b5b5d7e4e982f7d1a194cdab554a4bf419a471`. [Merge CI 35124925562](https://github.com/T-bear/BigBrain/actions/runs/35124925562)
SUCCESS: backend/frontend/documentation/secrets. Backend restore → format → Release build →
tests all executed successfully; frontend npm ci/test/build passed, no formatter/linter gate.
Post-merge npm audit --json exit 0, zero findings; installed graph verifies all target versions
and all eight prior GHSAs remain outside their assessed affected installed ranges.
Earlier dated entries are historical. Next proposed checkpoint: BB-130D2 — Frontend Formatter Tool Installation, separately
authorized after publication review; cleanup and CI activation remain separate bounded decisions.


## BB-130D2 dependency triage — 2026-09-15

**Dependency triage ACCEPTED / MERGED / CI VERIFIED.**
Approved candidate `ed57dba99d82eb47357b2b7d080baa9d72daf3d4` merged unchanged as
`73b7bdfe3befe8bb8505897d1d9480184d3e82ba`;
[CI 34993322513](https://github.com/T-bear/BigBrain/actions/runs/34993322513) SUCCESS for
backend/frontend/documentation/secrets. Actual backend formatter step 5 passed after restore 4,
before build/test 6/7; frontend npm ci/test/build passed with no lint/format gate.
Original baseline `bcf69191b92b9257627cd3c9814c02758b4ae8ae`;
branch `bb-130d/frontend-dependency-triage`. [Advisory evidence](docs/reports/features/platform/bb-130d2-frontend-dependency-triage-20260914.md).
Dated audit reproduces 5 affected packages / 3 moderate / 2 high, eight distinct GHSAs.
Six classified C (affected dependency, vulnerable path not used currently); Vitest/mocker and
PostCSS classified D (dev/test/build boundary, current exploit prerequisites absent).
70-module production inventory includes none of the affected packages and matches baseline
JS/CSS bytes. This is repository/build evidence, not deployed-image or blanket security approval.
No reachable current BigBrain product defect requiring blocker handoff established. No advisory
FIXED: patch maintenance recommended in a separately authorized dependency checkpoint.
Baseline 199/199 Web tests and production build pass; unchanged-source evidence reused on resume.
No dependencies/source/tests/CI changed, no Prettier or formatter execution, no deployment.
A/B COMPLETE; C COMPLETE / EXIT APPROVED; D IN PROGRESS; backend format gate enabled.
D1, backend whitespace cleanup and backend formatter gate ACCEPTED / MERGED / CI VERIFIED;
backend gate ENABLED. D2 characterization and dependency triage ACCEPTED / MERGED / CI VERIFIED.
D2 dependency maintenance and formatter cleanup/tool installation/gate NOT STARTED / NOT COMPLETE.
Finance RESEARCH / 0 SEK / NONE; no runtime/device/UX/scientific behavior or authority change.
Next: **BB-130D2 — Minimal Frontend Dependency Maintenance**, separately reviewed/authorized.
Proposed, untested targets: Vitest 4.1.10 → 4.1.11; PostCSS 8.5.22 → 8.5.23;
Nanoid 3.3.16 → 3.3.18; Undici 7.28.0 → 7.29.0. No correction or npm audit fix occurred.
Earlier sections retain historical states. Final documentation reconciliation requires its own
exact-main CI; resolution is in the canonical recovery note. No next checkpoint starts here.

## BB-130D2 frontend characterization — 2026-09-14

**BB-130D2 characterization ACCEPTED / MERGED / CI VERIFIED.**
Owner/architect approved candidate `01129fc54bdd3871b61eeda47e5cadbadc3ad81c` unchanged.
Merge/main `9181f2c75b8ffc12520ce497b7067e60527f459f` passed
[CI 34877864965](https://github.com/T-bear/BigBrain/actions/runs/34877864965):
backend, frontend, documentation and secrets SUCCESS. Backend formatter step 5 actually
passed after restore and before build/test. Frontend npm ci/test/build all passed;
no frontend formatter/linter gate exists. D2 cleanup/tool installation/gate NOT STARTED / NOT COMPLETE.
Baseline `1d824f6d2a9b4e6e2fc679705bd357860042ddbb`; branch
`bb-130d/frontend-quality-characterization`. [Characterization evidence](docs/reports/features/platform/bb-130d2-frontend-quality-characterization-20260914.md).
83 tracked TS/TSX files: pinned ephemeral Prettier 3.9.6 check flags 80 files, exit 1,
repeatable. Proposed formatter-only scope/config; no source/dependency/config/CI changes.
Existing frontend baseline: npm ci, 199/199 tests (26 files), TypeScript/Vite build pass.
Large wrapping/normalization debt requires separately authorized cleanup batches and later
CI activation. CSS/JS asset-test contracts need separate assessment; lint is deferred.
Registry audit reports five dependency findings (3 moderate, 2 high); applicability/exploitability
is unverified. Dependency advisories: TRIAGE REQUIRED; no audit fix or reproduced product defect.
A/B COMPLETE; C COMPLETE / EXIT APPROVED; D IN PROGRESS. D1/backend cleanup accepted;
backend format baseline CLEAN and formatter gate ENABLED ON MAIN. Previous final-main
[CI 34842836925](https://github.com/T-bear/BigBrain/actions/runs/34842836925) is now verified
SUCCESS for exact baseline, including actual formatter step 5 after restore/before build/test.
Historical sections below retain their checkpoint state; D2 characterization is current here.
Finance RESEARCH / 0 SEK / NONE; no deployment/runtime/device/UX/scientific behavior change.
Next: separately authorize dependency triage and any later cleanup/gate work. No next checkpoint
starts here. The separate documentation reconciliation requires its own exact-main CI;
final publication resolution is recorded in the canonical recovery note.

## BB-130D backend format CI gate — 2026-09-14

**ACCEPTED / MERGED / CI VERIFIED — ENABLED / ENFORCED ON MAIN.**
Owner/architect approved exact candidate `332bc90975f6479d25622635f13d4b5567f82634`
from baseline `e6fca47d87bc77bce153e77c98251d3b7d5e546b`.
Merged unchanged as `47542cc9815b4f95c2dbb1c73dc8538dd29d0240`;
[CI 34842088867](https://github.com/T-bear/BigBrain/actions/runs/34842088867)
passed backend, frontend, documentation and secrets. Backend step 5 actually executed
`dotnet format BigBrain.slnx --verify-no-changes --no-restore` and passed after restore
(step 4), before Release build/test (steps 6/7). [Gate evidence](docs/reports/features/platform/bb-130d-backend-format-ci-gate-20260914.md).
Accepted-main local restore/format/build/test all exit 0; format baseline CLEAN,
zero build warnings/errors, API 664/664 and Sentinel 32/32 passed with none skipped.
The historical 59-file / 22,573-location D1 debt was cleaned by the accepted cleanup;
historical D1/cleanup sections below describe those checkpoints, not current gate status.
A/B COMPLETE; C COMPLETE / EXIT APPROVED; D IN PROGRESS; D1/cleanup ACCEPTED / MERGED / CI VERIFIED.
D2 NOT STARTED. Finance RESEARCH / 0 SEK / NONE; no deployment/runtime/device approval
or scientific behavior change. No formatter write mode or source changes in this publication.
The separate documentation reconciliation commit must pass its own exact-SHA CI,
including the formatter step; final publication resolution is in the recovery note.
Next checkpoint requires separate authorization; stop before D2 or any other work.

## BB-130D backend whitespace cleanup — 2026-09-14

**ACCEPTED / MERGED / CI VERIFIED.**
Owner/architect approved exact candidate `8a795b16be87c97319705e9536b90472c83920aa`.
Merged unchanged as `c090a4fbb9450d1e94a0613a2cf8eb0ef5877150`;
[merge CI 34806580492](https://github.com/T-bear/BigBrain/actions/runs/34806580492)
passed backend, frontend, documentation and secrets on 2026-09-14.
Accepted-main `dotnet restore BigBrain.slnx` and
`dotnet format BigBrain.slnx --verify-no-changes --no-restore` both exit 0.
Backend format baseline: CLEAN; historical D1 debt (59 files / 22,573 locations) is cleaned on main.
Baseline `ce31f343851b71a77f3464ef3f2938f9574edbdf`; branch
`bb-130d/backend-whitespace-cleanup`. [Cleanup evidence](docs/reports/features/platform/bb-130d-backend-whitespace-cleanup-20260914.md).
Pre-check exactly reproduced D1: 59 files / 22,573 WHITESPACE locations. One formatter
write changed only those 59 C# files. Exact token/literal/trivia and full-tree comparison
passed; full-solution format verification now exits 0. Pre/post Release builds have zero
warnings/errors; pre/post API 664/664 and Sentinel 32/32 tests pass. No manual source edits.

A/B COMPLETE; C COMPLETE / EXIT APPROVED; D IN PROGRESS, D1 ACCEPTED. Backend format
CI gate STILL NOT ENABLED; activation requires the next separately authorized checkpoint.
Frontend/D2 NOT STARTED. Historical D1 evidence below remains pre-cleanup evidence;
the accepted main formatting baseline is now clean.
No CI configuration, packages, schema semantics, scientific behavior or runtime changes.
Finance RESEARCH / 0 SEK / NONE. No deployment, runtime/device or owner UX approval.
Next: verify exact-final-main CI for the separate documentation reconciliation, then return
to ChatGPT and stop. Final commit lookup is recorded in the canonical recovery note.
No CI gate, D2, Research Learning or Finance feature work is authorized by this publication.

## BB-130D1 backend quality-gate baseline — 2026-09-14

**ACCEPTED / MERGED / CI VERIFIED — evidence checkpoint; no new CI gate.**
Owner/architect approved exact candidate `ccd7f7906b8d460a5060711e4a5f2b99039492cf`.
Merged unchanged as `81baaf4f5667310e00e83cccb19da3daf9cb790b`;
[merge CI 34786050596](https://github.com/T-bear/BigBrain/actions/runs/34786050596)
passed backend, frontend, documentation and secrets on 2026-09-14.
A/B COMPLETE; C COMPLETE / EXIT APPROVED; D IN PROGRESS / D1 ACCEPTED.
Earlier dated D NOT STARTED entries below describe their historical authorization state.

[Sanitized D1 report](docs/reports/features/platform/bb-130d1-backend-quality-gate-baseline-20260914.md): SDK 10.0.302 restore/Release build pass with zero
warnings/errors; API 664/664 and Sentinel 32/32 pass. Two check-only format runs exit 2
with identical 22,573 WHITESPACE locations across 59 files. No source or CI/configuration
changes; no mass formatting or weaker analyzer policy. Backend format debt remains deferred
to separately authorized cleanup. Frontend tooling was inspected only; D2 needs a fresh
check-only characterization before any gate or cleanup. D2 is not started.

Exact local tool versions: `dotnet --version` = `10.0.302`; `dotnet format --version` =
`10.0.302-servicing.26329.109+35b593bebfcba58f8e78298cef14c2761f5d86c6`.

Reproduce the characterization after restore with `dotnet format BigBrain.slnx --verify-no-changes --no-restore`.
This currently fails on existing debt and is not a required green CI gate.

Backend formatter gate: NOT ENABLED — blocked by characterized pre-existing whitespace
debt, not correctness failure. Cleanup requires separate authorization. D2: NOT STARTED.
Next: verify the separate final reconciliation commit's exact-main CI, then stop for ChatGPT
review; no cleanup, D2 or final D acceptance follows. Finance RESEARCH / 0 SEK / NONE.
No deployment, runtime/device approval, provider/broker/capital or scientific behavior change.
Final publication identity and exact-SHA CI lookup are recorded in the canonical recovery note.

## BB-130C exit approved — 2026-09-09

Owner and architect explicitly approved **BB-130C COMPLETE — ARCHITECT/OWNER EXIT APPROVED**
after review of `00a1fb1ab58a7db8ed1d05ff159f8cebd13b5359` and successful
[CI 34338386364](https://github.com/T-bear/BigBrain/actions/runs/34338386364).
BB-130A COMPLETE; BB-130B COMPLETE; BB-130C COMPLETE / EXIT APPROVED;
BB-130D NOT STARTED and requires separate authorization.
E1 and E2 remain ACCEPTED / MERGED / CI VERIFIED. No currently-known C blocking checkpoint remains.
Broader persistence/naming, composition/DI, sole schema/DDL authority, initialization/recovery,
structural hardening, reader extraction and other accepted post-BB-130 debt remain DEFERRED,
not implemented. C exit does not claim all original aspirational refactors were delivered.
No deployment, physical-device/runtime approval or new Finance authority is implied.
Finance RESEARCH / 0 SEK / NONE. Next: independent verification of this reconciliation in ChatGPT;
do not start D, Research Learning or Live Market Shadow.

## BB-130C E2 campaign SQLite replay/reload — accepted, merged and CI verified, 2026-09-09

[Responsibility map and isolated evidence](docs/reports/features/finance/bb-130c-campaign-sqlite-replay-characterization-20260909.md).
Baseline `04a7a9c1f5d4d9afb02f313a272b6c4369709f5a`; branch
`bb-130c/campaign-sqlite-replay-characterization`. **OUTCOME A / ACCEPTED / MERGED TO MAIN / CI VERIFIED**.
Approved candidate `c59f0200cf73bb936fb6f83ceaf603c0e1cf9d40` merged as
`f8f5df5f0c91688c58b2466cb276dda0dfa8c346`; [merge CI 34337618297](https://github.com/T-bear/BigBrain/actions/runs/34337618297)
passed backend, frontend, documentation and secrets on 2026-09-09.

One new persistence/reconstruction/replay test; existing BB-127 fixture reused. Production unchanged.
One campaign / six unique attempts / two dataset revisions and 124 research observations remain
identical after fresh store/reader reload and replay. Six NOT EVALUABLE outcomes, null BacktestRunId;
no scientific fallback. Campaign 10/10, related Finance 129/129, full API 664/664, Sentinel 32/32;
Release build zero warnings/errors. Publication gates and commands in report.
E1 and E2 are accepted. No currently-known C blocking checkpoint remains.
**BB-130C COMPLETE — ARCHITECT/OWNER EXIT APPROVED**.
C exit was approved by architect/owner on 2026-09-09. BB-130D remains NOT STARTED; accepted post-BB-130 debt
remains deferred, not implemented. Next action: independent publication verification; D requires separate authorization.
No production data, schema change, provider work or deployment. Finance RESEARCH / 0 SEK / NONE.

## BB-130C E1 robustness identity correction — accepted, merged and CI verified, 2026-09-09

[Correction contract and verification](docs/reports/features/finance/bb-130c-robustness-selected-result-identity-fix-20260909.md).
Baseline `87d53241439b6fcbd97a07cd5a3986b59653eab9`; branch
`bb-130c/robustness-selected-result-identity-fix`. **ACCEPTED / MERGED TO MAIN / CI VERIFIED**.
Approved candidate `b34178aa40024cb8e7e953c72706bd88b3dda688` merged as `1204322a8764df33c0182db5b1e98d3cd6912d81`.
[Main CI run 34312516174](https://github.com/T-bear/BigBrain/actions/runs/34312516174)
passed backend, frontend, documentation and secrets on 2026-09-09.
Selected catalog evaluationId/checksum now gates visible detail; effect-lifetime guards exclude
obsolete success/failure. Pending/error shows no stale detail; existing reselection behavior remains.
E1 tests **8/8**, FinanceObservation **43/43**, full Web **199/199**; production Web build passed.
Documentation/diff/secrets publication evidence is recorded in the report.
Blocker `508cec3baceba4452df3431e7e0eab87dce50a3f` remains **NOT MERGEABLE** and is not ancestry.
E1 identity defect resolved and accepted; E2 is now accepted above. C is complete / exit approved on 2026-09-09. D NOT STARTED. Accepted debt deferrals unchanged.
No deployment, runtime or device UX approval is implied.
Only Web presentation/request ownership changed. No backend/schema/scientific change, production
access or deployment. Finance **RESEARCH / 0 SEK / NONE**.


## BB-130C backtest readers and exit assessment — accepted, merged and CI verified, 2026-09-08

[Reader map, evidence and exit plan](docs/reports/features/finance/bb-130c-backtest-reader-exit-assessment-20260908.md).
Baseline `6bfcd011a655a7d9f225bc9c023063738d10db3b`; branch `bb-130c/backtest-reader-exit-assessment`.
**DO NOT EXTRACT**: one shared JSON read implementation; catalog projection and initialization
ownership make another helper unjustified now. Two new isolated ordering/malformed-JSON tests;
existing exact reload/identity/writer evidence reused. Focused **84/84**, full API **663/663**, Sentinel **32/32**, zero failures/skips; Release **0 warnings/errors**.
**ACCEPTED / MERGED TO MAIN / CI VERIFIED**.
Approved candidate `e868b601d511e17554452c24ac7ed9d591bf051f` merged as
`796459be0f6a71fd3855a0cf2bc76f1bb2df68d5`; [main CI run 34275678369](https://github.com/T-bear/BigBrain/actions/runs/34275678369)
passed backend, frontend, documentation and secrets on 2026-09-08.
No production/schema change, deployment or runtime verification.
At the reader checkpoint the accepted C exit plan identified E1 robustness identity and E2
campaign SQLite replay/reload. E1 and E2 are now accepted above; no currently-known C blocking
checkpoint remains. C is complete / exit approved; no E2 blocker was reproduced.
The report's non-blocking debt, including further extraction, sole schema authority and composition,
is explicitly accepted for deferral beyond BB-130, not completed. That acceptance did not authorize E1/E2; the separately approved E1 and E2 merges are recorded above;
BB-130D remains NOT STARTED.
Finance RESEARCH / 0 SEK / NONE. No production data, providers or scientific behavior changed.


## BB-130C shared immutable backtest writer — accepted, merged and CI verified, 2026-09-08

[Boundary and deterministic evidence](docs/reports/features/finance/bb-130c-backtest-persistence-writer-20260908.md).
From `4ec675864d76f6da11d747026d917491f085f9e7` on `bb-130c/backtest-persistence-writer`.
FinanceBacktestPersistence now owns only the immutable four-table write across reference,
robustness and research-dataset callers. Schema, connections, readers and calculations stay put.
Pre-move characterization 4/4; post-move focused 82/82. full API **661/661**, Sentinel **32/32**, zero failures/skips; Release **0 warnings / 0 errors**.
**ACCEPTED / MERGED TO MAIN / CI VERIFIED**.
Approved candidate `6463a407221a0e6c47c5b429766f3398a1231994` merged as
`e28b9850b0ee40f12a68d508f65c9e4c65d57085`; [main CI run 34247876050](https://github.com/T-bear/BigBrain/actions/runs/34247876050)
passed backend, frontend, documentation and secrets on 2026-09-08.
No deployment or production runtime verification.
Finance RESEARCH / 0 SEK / NONE; no schema change, production access or deployment.
Other persistence, intake, composition/schema and BB-130D work remains separately scoped.


## BB-130C Finance persistence characterization — accepted, merged and CI verified, 2026-09-08

[Ownership and test evidence](docs/reports/features/finance/bb-130c-finance-persistence-characterization-20260908.md).
One new isolated regression freezes non-EODHD exact-lineage feature/backtest persistence with
acquisition disabled, restart reload, shared-writer idempotency and checksum-conflict rejection.
Focused **187/187**, API **659/659**, Sentinel **32/32**, zero failures/skips; Release **0 warnings/errors**.
No production/schema change. Remaining persistence coverage gaps are distinguished in the report.
Approved candidate `888ee57fa0c25ee5d3a733c9e2bbff77294fb3aa` merged as
`e16c0c9b62f486955483b0ce782167f53e687714`; [main CI run 34226093232](https://github.com/T-bear/BigBrain/actions/runs/34226093232)
passed backend, frontend, documentation and secrets on 2026-09-08.

## BB-130C CSV syntactic tokenizer — accepted, merged and CI verified, 2026-09-08

[Characterization and commands](docs/reports/features/finance/bb-130c-intake-csv-syntactic-parser-20260908.md).
Nine new syntax/lifecycle cases pass before/after extraction with the existing suites: **80/80**.
Canonical v2/legacy identity, column-zero evidence and artifact checksums remain unchanged.
Full API **658/658**, Sentinel **32/32**, zero failures/skips; Release build **0 warnings/errors**.
Approved candidate `653ee3b0824d6d7edda0d9cde4e2b9884a4f92ef` merged as
`a0f1ff2031016df72744574c5a4e2ee0810b3116`; [main CI run 34192468991](https://github.com/T-bear/BigBrain/actions/runs/34192468991)
passed backend, frontend, documentation and secrets on 2026-09-08.

## BB-130C CSV corporate-action column-zero correction — 2026-09-08

[Regression and command evidence](docs/reports/features/finance/bb-130c-csv-corporate-action-column-zero-fix-20260907.md).
On clean main-derived history: pre-fix five targeted cases produced three passes and the two
expected column-zero failures. Post-fix focused intake/protection/identity/research: **71/71**.
Release build: **zero warnings/errors**. Full API **649/649**, Sentinel **32/32**, zero failures/skips.
Publication gates are recorded in the report. Existing identity vectors and legacy replay remain
unchanged. No production data.
Approved candidate `50769e82e3df033066cd307aae78de2856b27092` merged as
`946eb98f139824e65780bf93d41686ea3724c1a3`; [main CI run 34188274546](https://github.com/T-bear/BigBrain/actions/runs/34188274546)
passed backend, frontend, documentation and secrets on 2026-09-08.

## BB-130C intake quarantine boundary — 2026-09-07

[Commands and evidence](docs/reports/features/finance/bb-130c-intake-safe-artifact-boundary-20260907.md).
Four new tests characterize download size/filename failure timing and cleanup/restart retention.
Focused intake/protection/identity/research suite passes **66/66 before and after extraction**.
Release build passes with **zero warnings/errors**. Full API **644/644** and Sentinel **32/32**
pass with zero failures/skips. Publication gate commands are recorded in the report. Existing v2 vectors and legacy fixtures are unchanged; isolated data only.


## BB-130C canonical product/revision identity v2 — 2026-09-07

[Exact commands, vectors and scope](docs/reports/features/finance/bb-130c-canonical-product-revision-identity-v2-20260907.md).
Focused identity/intake/protection/research-dataset suites: **62/62**, zero failures/skips.
Release solution build: **0 warnings / 0 errors**. Full solution tests: API **640/640**,
Sentinel **32/32**, zero failures/skips. Three isolated CLI processes verified persisted replay
and fresh-process identity. Publication checks and exact scope are recorded in the report. Synthetic legacy ID/checksum remains unchanged; future IDs are explicitly v2.
No production evidence is used. Web/API response shapes are unchanged; no Web rerun required.

## BB-130C revision identity audit — 2026-09-07

The accepted main-derived audit contains documentation only. The deliberately failing
blocker regression stays on its non-mergeable branch. Relevant isolated-fixture command:

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~FinanceDatasetIntakeTests|FullyQualifiedName~FinanceDataProtectionTests' --logger 'console;verbosity=minimal'
```

Production inventory used ReadOnly SQLite and a read-only Finance volume mount;
no application initialization. Exact artifact recomputation did not complete and
is not test evidence. See [audit classifications and limits](docs/reports/features/finance/bb-130c-dataset-revision-identity-audit-20260907.md).
Result: 22/22 passed, 0 failed/skipped locally. Merge `eb21b7c654e5adfaa25065a2264293b8ffbfed6d`
passed [main CI](https://github.com/T-bear/BigBrain/actions/runs/34112859688): backend Release build/full tests,
frontend tests/build, documentation and full-history secrets scan.

## BB-130C backtest identity — merged and CI verified, 2026-09-07

The merged candidate tests A→B pending/success/failure, rapid A→B→C stale completion,
close/reopen, refresh independence and unmount cancellation. The focused Finance suite
passes 35/35; full Web 191/191 and production build pass. Main CI run `34082759615`
passed backend, frontend, documentation and full-history secrets jobs. The prior
summary/curve mismatch is covered by the corrected identity regression rule; robustness
coupling remains outside this checkpoint.

## BB-130C research-detail characterization — 2026-09-06

[Evidence](docs/reports/features/finance/bb-130c-research-detail-characterization-20260906.md)
records 59 focused passing tests on unchanged production code; full Web 190/190 and
production build passed. Coverage includes all-nine detail
close/reopen/refresh/cancellation, selected-result dependencies and a reproduced existing
summary/curve mismatch. The mismatch test characterizes a defect, not an approved invariant;
a future authorized fix must update its expectation. No refactor is claimed.
Published checkpoint `4cdf1ff2fc3de420f9a7207fbf929dddda5a4c1e` passed all CI jobs
(Actions 34047595837); source/test content did not change during publication.


## BB-130C observation lifecycle characterization — 2026-09-06

[Evidence and exact commands](docs/reports/features/finance/bb-130c-observation-lifecycle-20260906.md):
56 focused Finance/cache/App/shared-control tests passed both before and after extraction;
full Web 187/187 and production build passed. Tests cover cached secondary triggers,
visibility/online deduplication, StrictMode cancellation, seed bypass, failed cache writes,
retry transitions and instrument reconciliation. Shared accessibility/motion contracts
remain unchanged. Implementation CI passed all four jobs (Actions 34046148403, implementation
`5d80efbf3f7ee2bbb793c2dbf77c2f0466168d0f`). New device verification is not claimed.

## BB-130B loading verification — 2026-09-06

The [measurement report](docs/reports/features/platform/bb-130b-loading-20260906.md)
contains commands, sanitized browser graphs, priority classification and timing limits.
App tests assert cold request sets, Home slot/cancellation semantics and Admin-only
one-in-flight/visibility polling. Finance tests preserve BB-128B/C and ensure secondaries
wait for an observation without restarting on refresh. Media tests cover closed/open
technical reads and non-overlapping polling. Run `npm test -- --reporter=dot` and
`npm run build` in `src/BigBrain.Web`; run `node scripts/verify-documentation.mjs`
and diff/secrets checks from the root. Publication CI still runs backend/full Web/docs/
full-history secrets; browser preview is not deployment or physical-iPhone approval.
Final checkpoint counts and publication state are authoritative in STATUS.

## BB-130A continuity verification — 2026-09-05

Documentation-only checkpoint: Sentinel suite 32/32 and focused Control Plane
SentinelSystemMetricsProviderTests 2/2 pass in Release. The latter initially hit
sandbox MSBuild named-pipe permission denial; the approved rerun passed.
Documentation verification (220 Markdown / 90 unique BB IDs), Compose config and
diff checks pass. Staged patch scan with gitleaks v8.28.0 found no leaks. Exact
commands, CI and limitations are in the [BB-130 review](docs/reports/documentation/bb-130-architecture-code-review-20260905.md).
No full local application regression or deployment is required for A's documentation
change. CI retains backend/frontend builds/tests, docs and full-history secrets.
Later phases must run the [BB-130 characterization and verification map](docs/architecture/bb-130-stabilization.md).

## BB-128C Finance async/degraded design-system conformance

- Focused tests verify the shared loading indicator, accessible standardized busy button, no overlapping refresh, cached content retention, successful/failed recovery, fetch-time visibility and separator-free wrapping-safe markup.
- Existing BB-128B tests remain authoritative for cache versioning, first-load failure, abort, foreground/online recovery, bounded persistence and Finance research-only safety.
- Result 2026-09-03: focused component/Finance 27/27 and full Web 173/173 passed. The production Web build completed successfully. Web-only deployment/runtime passed. Initial Actions run `33715050244` passed backend, documentation and secrets but failed one frontend assertion because it expected Stockholm-formatted `20:04` under CI's UTC locale; the corrected test checks exact ISO `dateTime` and a locale-independent `HH:MM` shape. Focused/full/build reruns pass; follow-up Actions run `33715603341` passed all jobs. Owner visual review was subsequently completed after the micro-fix; see the BB-128C report and STATUS.
- Micro-fix 2026-09-03: focused Finance 23/23 verifies exactly one loader for automatic refresh, hidden retry until failure, only the button loader during manual retry, retained cached content and successful stale-state clearing. Production Web build and Web-only deployment passed; Web/API and Finance read are healthy.

## BB-128B Finance last-known-good resilience

- `financeSnapshotCache.test.ts` verifies malformed/version-incompatible rejection, explicit display-safe field projection, secret-like unexpected-field exclusion and bounded watchlist persistence.
- `FinanceObservation.test.tsx` verifies fresh persistence, immediate cached render, failed background refresh without page blanking, repeated manual failure, in-place recovery, first-use failure/retry, one in-flight refresh, online/visibility recovery, navigation abort handling and detail-request deferral. Existing Finance panel tests verify local degradation and the absence of trading controls.
- Result 2026-09-02: focused 23/23 and full Web 171/171 passed; `npm run build` produced the production Web bundle. Documentation (215 Markdown files / 89 unique BB IDs), Compose, diff and staged gitleaks gates passed. GitHub Actions run `33657212798` passed backend, frontend, documentation and secrets for implementation `6eabbafed72c5df9a7484167a98acb0249c8a39d`. No local backend test was required because no backend/read-model source changed.

Manual iPhone/PWA owner test after deployment:

1. Open Finance with a healthy API and confirm the normal view loads.
2. Temporarily make only the BigBrain API unreachable using the existing safe appliance procedure; do not delete data or stop unrelated services.
3. Return to or reopen Finance and confirm the last-known-good view remains visible with `Visar senast hämtade data` and a failed-update indication.
4. Press `Försök igen` while unavailable and confirm content remains visible.
5. Restore the API, press `Försök igen` (or foreground/reconnect the PWA), and confirm fresh state replaces stale state without restarting the PWA.
6. Confirm no cached state is presented as LIVE or as authorization for acquisition/trading.

Deployment result 2026-09-02: only `web` was rebuilt and recreated with `--no-deps`. The Web image changed from `sha256:28f348d…` to `sha256:25e88b6…`; Web/API health and Finance via the Web proxy returned HTTP 200. The deployed bundle exposes the expected cache key, stale message and retry action. Finance runtime reported `RESEARCH / 0 SEK / NONE`, eight watchlist instruments, no broker/PAPER/LIVE and no active research run. The API container identity remained unchanged. Physical iPhone/PWA outage/recovery was subsequently explicitly owner verified on 2026-09-03; see BB-128B/C in STATUS.

## BB-128A Alpaca activation readiness

- `FinanceAlpacaActivationReadinessTests` verifies that unresolved entitlement blocks before a
  provider delegate can execute, status contains no configured credential, Basic/IEX cannot be
  represented as consolidated coverage, and stream identity separates instrument, observation
  type, granularity and policy version.
- Existing `LiveMarketObservation` tests remain authoritative for event/provider/receive/knowledge
  causal ordering and for rejecting delayed evidence labelled real-time. The affected regression
  includes the complete live-observation/synthetic-shadow suite and existing EODHD shadow tests.
- No test contacts Alpaca. No account, key, network client or real observation fixture exists.
- Result 2026-09-02: focused Alpaca 4/4 and affected live/shadow 24/24 passed; full API 599/599
  passed. Release build succeeded with zero warnings/errors. GitHub Actions run `33651291772`
  passed backend, frontend, documentation and secrets for implementation
  `646078efe77dfb4c8876afb7c65c4cdcdd421a95`.

## BB-127 owner research capabilities and XLSX

- `FinanceResearchDatasetTests` covers canonical/research independence, owner approval with
  external `Unknown`, explicit external denial, purpose/schema restrictions, current-snapshot
  leakage prevention, bounded workbook intake, manifest mismatch, support-sheet exclusion,
  OHLC anomaly rejection, idempotency/changed bytes, unsafe macro/external features, no canonical
  side effect and existing-engine backtest lineage.
- The supplied workbook was first processed against an isolated Finance DB and then through the
  real owner-drop appliance path:
  expected SHA-256, 18 datasets, 154,345 accepted observations, one MSFT plus three JNJ rejected
  OHLC rows and zero canonical rows. A GOOG buy-and-hold run proves plumbing only.
- Result 2026-09-02: focused 8/8 and full API 595/595 passed; warning-free Release build,
  documentation, Compose, diff and staged-gitleaks gates passed. API image
  `sha256:08f2db15e83051d18a8f8848e911f4ed7ce27839b39465cc40c1c575d4eed0e0`
  is healthy. Runtime intake produced 18 immutable research revisions / 154,345 accepted
  observations and zero promoted rows. GOOG revision `research-a7f1880044c83ede` produced
  deterministic run `backtest-75a9d3296e3f9228`; replay retained its run ID/checksum and did not
  add another run. GitHub Actions run `33586792133` passed backend, frontend, documentation and
  secrets for runtime-evidence commit `ffda7c2dc3a1567708b009741e626586191a8bd5`.

## GOOGLEFINANCE GOOG first owner intake

- Focused intake coverage now includes a matching embedded ZIP sidecar, structured
  `ApprovedByOwner` versus external `Unknown`, owner-declared RAW versus canonical `Unclear`,
  unmapped identity, zero volume, session gaps, discontinuity heuristics and no promotion.
- Actual runtime candidate: 3,126 accepted rows, 2014-03-27–2026-08-31, zero invalid/duplicate/
  conflicting/out-of-order/zero-volume rows, one missing session and zero detected suspicious or
  split-like close jumps. Result is `Rejected / BLOCKED / 0 canonical rows` because identity fails;
  external rights, RAW semantics and corporate actions remain unresolved.
- Result 2026-09-01: focused 17/17 and full API 587/587 passed; Release build passed with zero
  warnings/errors. API-only deployment is healthy and Finance remained `RESEARCH / 0 SEK / NONE`.
  Staged gitleaks found no secret/raw-payload issue. GitHub Actions run `33528970337` passed backend,
  frontend, documentation and secrets for `057c2f07bd10f2ff21d35d981028b262e0d07cd8`.

## BB-126 owner market-data drop

- `FinanceDatasetIntakeTests` covers explicit ready-marker detection, missing/unstable boundaries,
  checksum idempotency, changed evidence, CSV/ZIP inspection, ZIP traversal/expansion rejection,
  suspicious sidecars, unknown schema/provenance/identity, restart and the no-auto-promotion gate.
- Catalog assertions verify explicit technical-quality, rights and promotion states. Existing
  BB-084 tests remain authoritative for all 13 gates, OHLCV/conflicts, cross-source evidence,
  immutable revisions and cleanup safety.
- Required closure: focused and full API tests, Release build, Compose config, documentation/link/
  BB-ID checks, diff/secrets gates and API-only deployment/runtime health. No provider request,
  autonomous research or canonical promotion is a BB-126 verification action.
- Result 2026-09-01: 16 focused and 586 full API tests passed; `BigBrain.slnx` Release build
  succeeded with zero warnings/errors; 211-document/89-unique-BB-ID, Compose and diff gates passed.
  API-only image `sha256:e8b7cf7c03baf560740408bc01917162ba928480489490736a94547e94a37311`
  is healthy. Runtime confirmed a read-only owner-drop bind, unchanged two-candidate catalog and
  `RESEARCH / 0 SEK / NONE`; no inspection candidate or canonical revision was created.
  Staged gitleaks found no secrets; GitHub Actions run `33492668713` passed backend, frontend,
  documentation and full-history secrets for implementation `48755ee0ec27fed8f38132ddca050c2d2654e6ca`.

## BB-125 zero-cost historical market-data qualification

- Documentation/research-only result: no production source, schema, configuration, provider
  request, account/key, quarantine artifact, canonical revision or deployment changed.
- Validate the four-provider matrix and first-party links, unique BB IDs, Compose syntax, diff
  whitespace and secret patterns. Existing entitlement and BB-084 suites remain authoritative;
  no adapter or test is manufactured when no source reaches `STATE A`.
- No provider endpoint, autonomous research run or strategy optimization is part of verification.
  Finance must remain `RESEARCH / 0 SEK / NONE`.
- Result 2026-09-01: documentation verification passed for 209 Markdown files and 89 unique BB
  IDs; Compose validation and `git diff --check` passed. No production build/test or deployment is
  applicable because production behavior did not change. The local gitleaks binary is unavailable;
  GitHub Actions run `33471245304` passed backend, frontend, documentation and full-history secrets
  jobs for qualification commit `16deff048f68da199087de1cccceb8a0111c9a97`.

## BB-124 anti-overfitting and OOS governance

- `FinanceRobustnessEvaluationTests` verifies deterministic 60/20/20 chronology, two embargoes, validation-only selection, complete trial preservation, single-use holdout state, contamination, insufficient-history fail-closed behavior, family-breadth rejection, negative/regime/leakage controls and the positive engineering-only control.
- `FinanceAutonomousResearchTests` plus full API regression verify `research-integrity-v2`, current-evidence selection, immutable legacy readability and no autonomous fallback. DSR/PBO remain `NOT_EVALUABLE`; no p-values are synthesized.
- Any bounded runtime validation uses only existing revisions and `finance-robustness-build`; it must not call providers or trigger autonomous research. Expected short-history output may be `INSUFFICIENT_DATA / UNTOUCHED`.
- Required affected scope: focused robustness/autonomous tests, full API suite, Release build, documentation/Compose/diff/secrets gates and API-only deployment/runtime checks. Media BB-158 is documentation-only and must not trigger Media tests or deployment.

## BB-123 transaction-cost, slippage and fill realism

- `FinanceDeterministicBacktestTests` uses hand-verifiable series for zero, fixed/per-share/minimum/proportional costs, adverse assumed spread and slippage on buy/sell, combined friction, cash/whole shares, invalid open, exact-next-session missing bar, end-of-data, deterministic replay and unchanged strategy intents.
- Legacy v1 cost JSON must deserialize with zero—not invented—spread/fixed/proportional values. New fill/cost assumptions must alter run identity. Old persisted v1 runs must remain readable and unchanged.
- `FinanceEodhdIntegrationTests.ConcurrentEquivalentBacktestBuildsConvergeWithoutImmutableIdentityConflict` protects startup/maintenance concurrency: identical writers converge and immutable checksum conflict behavior remains fail-closed.
- A bounded maintenance comparison may use only existing exact revisions and must not call a provider or autonomous research. Verify zero ≤ low ≤ base ≤ high ≤ stress degradation, retain `INSUFFICIENT_DATA` where evidence is insufficient, and report sanitized aggregates only.
- Required affected scope: focused backtest/robustness/concurrency tests, full API suite, Release build, documentation/Compose/diff/secrets gates, API-only deployment health, old-run read and Finance `RESEARCH / 0 SEK / NONE` safety check.

## BB-122 historical security identity evidence pilot

- Documentation/research-only result. Verify the deterministic 30-ticker cohort and its 3 VERIFIED / 17 PARTIAL / 5 AMBIGUOUS / 5 UNRESOLVED classification without invoking intake, acquisition, promotion or autonomous research.
- Confirm `wiki-5713d7dccfa38f56` remains immutable at 3,722 canonical rows/five instruments, no WIKI redownload or Stooq request occurred, and current exchange directories are never treated as historical membership evidence.
- Existing mapping and BB-084 suites remain authoritative for effective dates/MIC ambiguity, all 13 gates, fail-closed `UNKNOWN`, provenance, checksums, immutable revisions, restart/idempotency, OHLCV/conflicts and cleanup safety. No production test changed because behavior did not change.
- Validate documentation structure/links, BB-ID uniqueness, Compose syntax, diff whitespace and secret patterns. No deployment is required.

## BB-121 WIKI forensics and Stooq requalification

- Documentation/research-only result. Read-only runtime/API and aggregate artifact inspection must not invoke intake, acquisition, promotion, feature generation, backtests or autonomous research. No production build/deployment is required without behavior change.
- Verify the retained WIKI SHA-256/size, 2014-01-02–2016-12-19 range, 3,186 ticker strings, 2,155,310 accepted/11,295 rejected rows, and exact five-symbol canonical lineage. AAPL/JNJ/JPM/MSFT each have 748 snapshot rows, XOM 732; SPY/QQQ/IWM have zero.
- Existing BB-084 tests remain authoritative for all 13 gates, `UNKNOWN` fail-closed behavior, checksum revisioning, immutable promotion, restart/idempotency, OHLCV, conflicts, overlap and cleanup safety. No test or gate changed.
- Validate documentation links/BB-ID uniqueness, Compose syntax, diff whitespace and sanitized secret patterns. No Stooq protected data route or external communication is part of verification.

## BB-120 historical evidence source qualification

- Documentation/research-only result: no production source, schema, configuration, provider request, quarantine artifact, promotion or deployment changed. Full code/build regression is therefore outside affected scope.
- Read-only `/api/v1/modules/finance/datasets` inspection must continue to show WIKI promoted as `wiki-5713d7dccfa38f56` with 3,722 canonical rows/five symbols and Zenodo as `ManualReviewRequired` with zero promoted rows. It must not invoke dataset intake POST operations.
- Validate documentation links/BB-ID uniqueness, Compose syntax, whitespace and sanitized secret patterns. Current EODHD path and the 13-gate fail-closed policy remain covered by the existing BB-077/084/085 suites; no test was weakened or duplicated.

## BB-119 Finance readiness and governor reconciliation

- Pinned-clock scheduler tests cover an eligible complete 8/8 universe, incomplete universe, exact/incompatible feature lineage and a 2026-08-30 non-session date. Historical evidence and current-session eligibility are asserted independently; restart/reconciliation tests remain required.
- Operations tests assert the same live readiness projection and explicitly require `NOT_REQUIRED_NON_RESEARCH_DAY` rather than an inferred generic `READY`. Governor tests preserve missing/stale metrics → `DEFER`, healthy metrics → `ALLOW`, critical disk → `BLOCK`, and `BLOCK > DEFER > ALLOW`.
- Sentinel integration starts over a pre-existing stale socket file and must bind successfully. Deployment verification must inspect scheduler, operations, governor and system overview without triggering research or mutating Finance data.

## BB-118 Finance source-of-truth reconciliation

- Production behavior is unchanged. Runtime verification uses only GET endpoints plus repository-native `finance-evidence-counts` and `finance-schema-status`; it must not invoke autonomous-run POST, provider acquisition, backfill, prediction creation, scheduler/config mutation or deletion.
- Validate canonical current-state consistency, report schema/links, unique BB IDs, Compose syntax, diff whitespace and publication secrets. Historical reports remain immutable evidence; stale phrases are acceptable only when explicitly scoped to their original date/slice.
- Current read-only baseline: `RESEARCH / 0 SEK / NONE`; scheduler enabled/not running; 10 opportunities; 0 autonomous runs/experiments; 105 market revisions; 29,890 observations; 16 feature revisions; 797 backtests; 25 robustness evaluations; 288 shadow predictions; 240 outcomes; 240 research-risk evaluations; schema 93. Latest feature revision has 44,520 values; aggregate values are not exposed.

## BB-117 audiobook final polish and Media handoff

- Focused Web coverage locks the 48 px gold crescent target, 23 px vector glyph, lower-right Continue Listening alignment, inactive/active `aria-pressed`, focus entry/return, dialog open/dismiss, 15/30/45/60, local stop time, off, shared detail deadline, expiry pause/sync, independent status row and unchanged mobile two-column Detail Hero.
- Browser QA targets 390×844, 430×932, tablet and 1440×900. Verify compact mobile bottom sheet/desktop popover, no collision or horizontal overflow, active status separated from progress/time and unchanged cover-left/detail-metadata-right hero. Automated QA never grants physical owner approval.
- BB-117 changes Web presentation only. Run focused Web tests, full Web only if required by a regression signal or publication policy, Vite production build, documentation verification, diff/secret gates, Web-only deployment/runtime smoke and GitHub CI. API, Finance, Family and Sentinel suites are outside the affected scope.
- Result 2026-08-31: 34 focused and 160 full Web tests passed; Vite production build and 201-document/89-unique-BB-ID validation passed. Web-only image `sha256:215921b72acbcf27c5f1fcc2bdc9f0e7c512c70e7fb5892e664b18629bed3749` is healthy and `/` plus `/health` returned HTTP 200. Deployed Firefox at all four viewports verified 48×48/23×23 trigger geometry, gold treatment, active state, 320 px mobile/280 px desktop interaction, focus return, shared deadline/off, non-overlap, unchanged hero and no overflow. Firefox BiDi pointer dispatch was inconsistent at narrow viewports even though hit-testing resolved directly to the button; DOM activation completed state QA, so physical iPhone/PWA remains the authoritative touch gate. GitHub Actions runs `33385591024` and `33386467803` passed all jobs.

## BB-115R physical iPhone sleep-timer remediation

- `Audiobooks.test.tsx` verifies that the pre-session timer is not a dead disabled icon: native details/summary activation opens visible guidance and an explicit playback action; the still-open panel becomes configurable from Continue Listening after session creation. It retains presets, custom local clock, replace/cancel, shared detail status, ordinary expiration pause/sync and unchanged Play/Pause.
- `UXPolishStyles.test.js` requires active timer status to occupy its own in-flow `grid-column:1/-1` row and forbids the former absolute status positioning. The compact panel is viewport-bounded.
- Local result 2026-08-30: 32 focused Web tests and Vite production build passed. Deployed 390×844, 430×932 and 1440×900 QA must cover no-session guidance, start→15 min, hour-boundary/long-duration status, shared detail state, change/cancel, containment and no overflow. Physical iPhone/PWA verification remains authoritative and pending.
- Final result 2026-08-31: 32 focused and 157 full Web tests plus Vite build passed. Deployed Firefox at all three viewports opened the native summary by pointer, showed no-session guidance/start in a 280 px panel, then rendered `60 min kvar` with a 4:33:57 audiobook in a static `1 / -1` row with zero geometric intersection or overflow. Exact status survived detail navigation and cancelled there. Web `/` and `/health` returned HTTP 200; GitHub Actions runs `33337614697` and `33337917824` passed. Physical iPhone/PWA remains pending.

## BB-115 physical iPhone artwork and Continue Listening timer

- `UXPolishStyles.test.js` protects the root cascade correction: the obsolete mobile `display:contents` and broad detail-child grid override are absent, one canonical detail container remains, and both fetched artwork and BigBrain-B receive explicit 2:3, first-column, intrinsic-safe, 40 vw / 180 px constraints.
- `Audiobooks.test.tsx` verifies that the timer affordance is present on Continue Listening, disabled before an active session, enabled after direct playback, keyboard/button semantics through `aria-expanded`/`aria-controls`, presets, custom local clock, cancellation, replacement, ordinary expiration pause and the same deadline/status after navigating to detail. Existing direct Play/Pause behavior remains covered.
- Local result 2026-08-30: 31 focused Web and 156 full Web passed; Vite production build passed. API was not touched and BB-115 does not require an API rerun.
- Browser QA must measure Induction and BigBrain-B at 390×844, 430×932 and 1440×900, verify no overflow, and exercise Continue Listening timer start/change/cancel plus shared detail/navigation state. Browser QA cannot replace physical iPhone/PWA owner verification because BB-114's browser matrix failed to expose the reported regression.
- Deployed result 2026-08-30: Induction measured 156×234, 172×258 and 180×270 px at the three viewports, all 2:3 without overflow; Golden Compass BigBrain-B matched the widths/ratio. Continue Listening exposed the accessible timer, started 15 minutes, retained the same status on detail, changed to 30 minutes and cancelled. Web-only deployment and `/health` returned HTTP 200. GitHub Actions run `33328144299` passed.

## BB-114 audiobook detail polish, sleep timer and floating-player rejection

- `Audiobooks.test.tsx` covers semantic detail metadata, absent redundant label/raw fallback copy, healthy-native suppression of the Audiobookshelf link, truthful unavailable recovery, route-surviving session state, in-detail controls, sleep presets/custom local clock/cancel/replace/expiration and ordinary pause sync.
- `AudiobookTests` traces Audiobookshelf `authorName`, `seriesName`, `narratorName`, `language`, `publishedYear` and `description`; it locks the verified `X3M 4ever!!!` non-synopsis omission while preserving useful Ghostsong metadata. `UXPolishStyles.test.js` locks 2:3 compact responsive detail artwork, shared fallback geometry, no fixed player styling and no AppShell player render.
- Browser QA: 390×844, 430×932 and 1440×900. Verify Narnia, Ghostsong and Golden Compass fallback; no LJUDBOK/raw X3M/reservväg; timer presets/custom/cancel; Home/Family/Finance navigation with continuing audio and no overlay; return-state, dock clearance and zero horizontal overflow. Exact sleep expiry is best-effort while iOS suspends a PWA.
- Result 2026-08-30: 19 focused API, 31 focused Web, 565 full API, 156 full Web and 32 Sentinel passed; Vite/Release, 196-document/89-unique-BB-ID, Compose and diff gates passed. Only API/Web were recreated. Deployed Firefox verified Narnia/Ghostsong/Golden Compass at 390×844, 430×932 and 1440×900 with 156/172/180 px 2:3 artwork, same-size BigBrain-B, no raw/fallback copy and no overflow. Induction playback survived Home without overlay, returned as Pausa, and timer activated at 15 min then cancelled. GitHub Actions run `33326376331` passed all four jobs for `2607d6f804efd6bbc49bdc2c2f9e721655692a68`.

## BB-113 audiobook owner-review remediation

- `Audiobooks.test.tsx` covers separate direct play versus detail navigation, accessible play/pause state, native start/session path, authoritative duration/progress time, healthy native-primary/fallback-secondary hierarchy and truthful unavailable fallback.
- `AudiobookTests` preserves arbitrary valid owner artwork and matches only the exact runtime-verified Audiobookshelf generic media-note placeholder hash. `UXPolishStyles.test.js` locks the hero's `minmax(0,1fr)`, natural word wrapping, 2:3 artwork and narrow stacked fallback.
- Gates: focused audiobook Web/API, full Web/API/Sentinel, Vite/Release build, Compose, documentation, diff and scoped secret scan. Deployed QA: 390×844, 430×932 and 1440×900; overview/direct playback/time/detail/Golden Compass plus a long Swedish title/native player/fallback/mini-player route survival/overflow/dock-safe-area. Theme expansion is required only where the existing automation is cheap.
- Result 2026-08-29: 18 focused API, 23 focused Web, 564 full API, 32 Sentinel and 154 full Web passed; Vite, Compose, 195-document verification, diff and staged gitleaks passed. Deployed Firefox direct-play/mini-player/route-survival passed, as did the 3×3 viewport/theme matrix. GitHub Actions run `33269194506` passed frontend, backend, documentation and secrets for `5d829fdcf64541e49b8cf355004bea90d2a372d3`.
- Publication follow-up: docs-run `33269347381` exposed one asynchronous test race where the collection button still had busy-name `Filtrera pågår`; the assertion now awaits `Filtrera`. Three focused repetitions, full Web and Vite passed locally; GitHub Actions run `33269619672` passed all jobs for testfix `09976f0ab43c825f22434177ca7952f5207b746b`.

## BB-112 native audiobook playback and owner search remediation

- `AudiobookPlaybackTests` covers separate identity/progress verification, credential-free opaque DTOs, item/track binding, invalid progress, single bounded Range and absence of arbitrary upstream URLs.
- Audiobook acquisition/provider tests retain deterministic ranking, unknown-language retention, stable release dedup and partial-provider behavior. `Audiobooks.test.tsx` retains overview/collection/detail, result and confidence presentation.
- Runtime mutation is bounded: move an existing in-progress item one second, sync, restore the original position on close, then require the closed stream to return 404. Firefox verifies `206` audio, playing state, real slider position, route survival and no overflow.

## BB-111 route/detail UX and playback credential gate

- `npm test -- --run src/audiobooks/Audiobooks.test.tsx src/routeFocus.test.ts src/UXPolishStyles.test.js`: 28/28. Täcker semantic Library-link i stället för CTA, pointer-/keyboardklassificerad route focus, forward detail till top, back-restoration, okänt språk som utelämnas, detail artwork-klass/ratio-kontrakt och befintliga bounded audiobookflöden.
- `npm test -- --run`: 151/151 Web.
- `npm run build`: TypeScript + Vite production build passerar.
- `dotnet test BigBrain.slnx -c Release --no-restore`: 558 API + 32 Sentinel passerar. Det första felskrivna försöket mot `BigBrain.sln` kunde inte starta eftersom repositoryt använder `BigBrain.slnx`; det körde inga tester och följdes av korrekt kommando.
- Runtime credential discovery är read-only och sanerad: endast identity/privilege-klass, aktiveringsstatus och aggregerade progress/session-counts får lämna proben. Token, användarnamn, user ID, privata URL:er och råa payloads får aldrig skrivas ut. Resultatet 2026-08-28 är restricted/non-root, 0 progress och 0 sessions; inga playback-write-/sessionanrop kördes mot fel identitet.
- `docker compose config --quiet`, 192-filers documentation verifier, `git diff --check` och scoped staged gitleaks (0 fynd) passerar.
- Deployad Firefox-matris 390×844/430×932/1440×900 × Obsidian Gold/Forest Night/Arctic Wind passerar. Samtliga nio fall har link-semantik, 0 overview-katalograder, pointerfokuserad heading med DOM-fokus men ingen ring, detail på `scrollY=0`, exakt collection-scrollrestoration, 1.5 höjd/bredd (2:3), `object-fit:cover`, inget `Språk okänt` och ingen overflow. Web/API health är HTTP 200; GitHub Actions run `33187236677` passerade implementation `686f621937ae0e376bcc55003077769fff8b4351`.

## BB-110 audiobook owner UX consolidation

- `Audiobooks.test.tsx` verifierar kanonisk secondary-variant för **Bibliotek**, reducerad synlig copy med bevarade accessible names, discovery → library → downloads, semantic whole-row detail navigation, bounded collection och lokal persistent attention-dismiss utan POST eller audit/provider-mutation.
- Full Web-regression, API/Sentinel-regression och Release/Vite-build körs trots att BB-110 endast ändrar Web. Browsermatrisen är 390×844, 430×932 och 1440×900 i Obsidian Gold, Forest Night och Arctic Wind; kontrollera långa titlar, vertikala jobb, dock/safe-area och overflow.
- Native playback får inte testas eller markeras implementerad innan BigBrain-user→Audiobookshelf-playback-identitet och same-origin Range/session/progress-sync har ett godkänt arkitekturkontrakt. Ingen ägartoken får förekomma i Web eller browserlagring.
- Resultat 2026-08-28: 25 fokuserade Web, 147 fulla Web, 558 API och 32 Sentinel; Release build 0 warnings/errors, Vite, Compose och 191 Markdown passerade. Deployad 3×3 browsermatris passerade utan overflow med 20 bounded rows, semantic whole-row, korrekt sektionsordning, jobbwrapping och 112 px mobil dock-clearance. Två separata asynkrona provider-status-assertions stabiliserades efter en CI-race; GitHub Actions run `33180923644` var green för `87d8528791a32a0e4d0e66de400d5ca4d5c392c9`.

## BB-109 audiobook owner UX remediation

- `Audiobooks.test.tsx` verifierar standardiserad **Bibliotek**-affordance utan dominant count, progressbaserad Continue Listening utan falsk tomstatus, separata inputs för ny discovery/lokal filtrering, bounded collection, reducerad-motion scroll-till-top, aktiva/attention/history-sektioner och presentation-only historikrensning. Befintlig edition-confirmation/idempotens körs fortsatt.
- `AudiobookAcquisitionTests` verifierar att provider-absence efter fem minuters grace fail-closed blir `failed`, medan ett nyregistrerat jobb behåller aktiv state under gracen.
- Runtimekontroll ska sanerat verifiera serviceprofilens respektive övrig profils progressantal, provider/download/import-evidens och att inga acquisitioner cancel/start/delete görs. Native playback får inte markeras complete utan beslutad identitet och säker same-origin session/stream-gräns.
- Browser-QA: 390×844, 430×932 och 1440×900 i alla tre teman. Kontrollera overview, Bibliotek, separat discovery/library-search, rubrikhierarki, scroll-till-top/dock/safe-area, aktivitet/history, overflow och lång svensk text.

## BB-108 audiobook navigation experiment

- `Audiobooks.test.tsx` verifierar att Media-overview inte renderar katalog/nyligen tillagt, att Continue Listening kräver verklig progress, collection-routen är bounded, detail har egen deep-link, browser/in-app-back använder History API och query/sort överlever detail-retur. Befintlig sök-, edition-confirmation-, jobb- och completion-regression körs oförändrad.
- `App.test.tsx` verifierar att en direkt `/media/audiobooks`-länk aktiverar Media och renderar endast den dedikerade audiobook-routen, inte hela Media-dashboarden.
- `components.test.tsx` förblir kontraktstest för BigBrain-B vid saknad/misslyckad bild. Runtime artwork-verifiering ska separat skilja HTTP-success från 404: en verklig Audiobookshelf-cover får inte ersättas baserat på motivet.
- Browser-QA: 390×844, 430×932 och 1440×900 i Obsidian Gold, Forest Night och Arctic Wind. Kontrollera overview-density, progress, collection-affordance, bounded catalogue, detail/deep-link/back, state restoration, dock/safe-area, overflow, lång text och `prefers-reduced-motion`. QA får inte skapa acquisition eller ändra media.

## BB-107 owner UX remediation

- `MediaLookupRequestTests` simulerar den verkliga first-click-klassen: Arr registrerar posten men POST-svaret timeoutar. API:t avstämmer exakt registrerad foreign ID, returnerar `created` och gör totalt en POST. Web-testet bevisar att retry av samma dialog använder samma idempotensnyckel och att busy-knappen blockerar dubbeltryck.
- `Audiobooks.test.tsx` bevisar overview → collection: Media-starten visar högst fyra senaste böcker och döljer full katalog/sökfält tills **Visa alla** väljs. Befintliga explicit-utgåve-, aktivitet-, completion- och placeholdertester består.
- Browser-QA kör 390×844, 430×932 och 1440×900 i alla teman. Kontrollera Home-textcellens användbara bredd, horizontal overflow, verklig sista kontroll över dockan, kompakt audiobook overview, gemensam B-placeholder och dimensionsstabil sök-busy state. Den muterande first-click-regressionen körs med säker providerfixture, inte mot användarens bibliotek.

## BB-106 consolidated UX quality

- `components.test.tsx` verifierar dimensionsstabil busy-knapp, tillgänglig status och gemensam media-placeholder. Audiobook-regressionen verifierar explicit utgåvebekräftelse, kompakt historik och pagineringskontrakt; Calendar provar lokal past/today/future-klassificering och Home provar datum+titel+tid.
- Full verifiering: 135 Web-, 555 API- och 32 Sentinel-tester, Vite- och Release-build, Compose, dokumentationsverifierare och diffkontroll.
- Deployad browserkontroll ska vänta tills modulernas read-only data har stabiliserats, scrolla den rumsligt sista kontrollen och prova 390×844 samt 430×932 i alla tre teman. Kräv ingen horisontell overflow, synliga textfält och full dock-clearance. Kontrollera även 1440×900 och reduced-motion-kontraktet.

## BB-105 AudioBookBay parser and literal author search

- Build `bigbrain-librarr:1208254-bb6`. The image applies the sanitized current-markup fixture, proves English rows survive adjacent nested metadata labels, rejects a non-English row, and reruns complete organizer/search/download plus the focused API regression.
- `AudiobookMetadataTests` prove literal author-only input is the first bounded provider seed and only one resolved work supplements it. `AudiobookAcquisitionTests` prove English preference, Swedish preference, All Languages and retained unknown candidates without creating requests.
- Runtime verification is read-only: record source HTML post count, AudioBookBay parsed count, Librarr retained count and BigBrain candidate count; compare acquisition-job totals before/after and never open the final confirmation.
- BB-105 follow-up image `bigbrain-librarr:1208254-bb7` adds a network-free table regression proving AudioBookBay alone sends lowercase queries for `Pirateaba`, `pirateaba`, `PIRATEABA`, `The Wandering Inn` and `the wandering inn`. Runtime QA repeats those five GET-only searches and verifies acquisition-job totals and Ghostsong state are unchanged.

## BB-104 universal metadata-aware audiobook search

- `AudiobookMetadataTests` use network-free Open Library fixtures for ISBN-10/13 classification, malformed identifiers, deterministic metadata parsing, missing metadata, timeout, series/alternate/author planning, narrator capability and the bounded The Wandering Inn fixture.
- `AudiobookAcquisitionTests` prove that at most two metadata seeds are merged, duplicate provider editions collapse, English/unknown remain visible under Swedish preference, partial source failure retains successful results and search creates no acquisition request.
- `Audiobooks.test.tsx` proves the single universal input, canonical book context and unchanged explicit edition-confirmation gate. Runtime acceptance is read-only: compare job count before/after title, author and ISBN searches and never press **Lägg till vald utgåva**.

## BB-103 usable audiobook lifecycle

- Build `bigbrain-librarr:1208254-bb5`; its revision-policy regressions prove the exact pinned native audiobook source set is accepted in Prowlarr-first order while a revision mismatch, missing source, duplicate registration, malformed ID or injected/future source fails closed. The same image reruns the complete organizer/search/download packages, preserving every no-overwrite and lifecycle regression.
- BigBrain provider tests cover all exact pinned source IDs, safe URL/hash/path normalization, unknown-source rejection and rejection-before-request for direct native candidates that cannot enter the hash-backed acquisition lifecycle. Runtime diagnostics use the same bounded title/author variants and expose sanitized per-source counts only.

- Build `bigbrain-librarr:1208254-bb4`; the image gate runs complete upstream organizer/search/download tests. Regressions prove an existing audiobook destination creates durable failure evidence, never deletes the qBittorrent job/source and never changes existing content. Discovery tests cover unchanged title-only behavior, bounded title-plus-author variants reaching sources, normalized deduplication, audiobook-suffix suppression and retained partial-source results; existing scoring/language tests remain in the complete search package. This bb4 evidence is retained historically; bb5 is the current runtime image.
- `LibrarrAudiobookAcquisitionProviderTests` cover single-use opaque candidates, bounded real-state mapping, disappearance without completion, durable import failure, exact local import identity, Audiobookshelf indexing and final completion. Missing evidence remains importing/indexing rather than becoming complete.
- `Audiobooks.test.tsx` proves the actual registered Media view requires explicit release confirmation, submits exactly once only after that confirmation, localizes truthful job states, refreshes the library on completion and never fabricates percentage progress.
- Runtime QA performs read-only real search, confirms zero jobs before the owner gate, and checks 390 × 844 plus 430 × 932 in all themes. Never click **Lägg till vald utgåva** without explicit owner selection and approval.
- First-acquisition regression coverage proves that an exact imported provider hash may reconcile with exactly one canonical Audiobookshelf title even when release tags differ and Librarr reports author `Unknown`; two possible canonical matches remain `indexing`. Web proves transient provider-status failure renders `configuredUnavailable`, not a false not-configured state. Final totals: 540 API and 130 Web tests.

## BB-102 patched Librarr provider

- `docker build -t bigbrain-librarr:1208254-bb4 -f infrastructure/librarr/Dockerfile .` applies all pinned patches, runs the complete upstream organizer/search/download packages and the focused native-source and query regressions. Source-policy tests prove exact allowlisting, Prowlarr-first order and fail-closed empty/duplicate/unknown configuration.
- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --filter 'FullyQualifiedName~LibrarrAudiobookAcquisitionProviderTests|FullyQualifiedName~AudiobookAcquisitionTests' --no-restore` covers authenticated health, dependency degradation, bounded/opaque candidates, request cache, language hints, state mapping and safe cancellation semantics without a network.
- `npm test -- --run src/audiobooks/Audiobooks.test.tsx` covers the real registry-composed Media view and bounded polling of real job states without fabricated percentage progress.
- Runtime search and downstream health are commissioning checks. Aggregate candidate counts/provenance may be recorded, but raw URLs and releases stay private. These checks must not initiate the first download; secrets remain outside Git, test output and chat.

## BB-101 audiobook acquisition foundation

- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --filter FullyQualifiedName~Audiobook --no-restore` covers provider None, bounded search, Swedish/English/unknown ranking, narrator/edition distinction, stable job IDs, state mapping, controlled provider failure, missing-job 404, cancellation and safe import paths including no-overwrite.
- The complete API suite proves the new store and provider boundary do not alter existing modules. Provider fixtures are network-free and never download media.
- `npm test -- --run src/audiobooks/Audiobooks.test.tsx` covers actual Media registry placement, Swedish-default search, explicit provider-unavailable UI, absence of fake progress, edition detail and disabled request controls.
- Runtime verification must preserve BB-100 `configuredHealthy`, show provider `NotConfigured`, return an empty job list, reject a fabricated request without persisting a job and inspect Media/Ljudböcker at 390 × 844 and 430 × 932 in all three themes.
- No real provider is part of ordinary validation. Installing one requires an owner decision plus a separate maintenance/API/security review.

## BB-100 audiobook platform foundation

- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --no-restore` covers network-free ABS mapping/degradation, auth failures, malformed payloads, provider None, language normalization/ranking and edition distinction.
- `npm test -- --run` covers configured and not-configured audiobook UI plus language-filtered local search without a real ABS dependency.
- `npm run build`, Release build, Sentinel tests, Compose validation, documentation verification and `git diff --check` remain publication gates.
- Runtime verification checks the audiobook overview, Media/Ljudböcker in all themes, dock clearance, existing Media flows and Finance `RESEARCH / 0 / NONE`.
- A configured library test requires an owner-created ABS service identity/API key and library ID; secrets never enter fixtures, documentation or frontend.

## BB-099 whole-app UX audit

Render every registered view plus embedded Settings at 390 × 844, scroll long views from top to bottom and exercise their primary controls. Use normal viewport captures, never stitched full-page iOS captures, as reachability evidence. Verify that the last actionable element can scroll entirely above the persistent dock through the shared `--bb-mobile-dock-clearance`, and specifically exercise Media search/request, downloads, Smart Shuffle, its lower select and collapsed technical integrations. Home tests must prove that existing read-only API data appears before contextual navigation. Finance must keep its default narrative and advanced disclosure without API or safety changes. Repeat representative checks at 430 × 932, 768 × 1024 and 1440 × 900 and across all three themes. Family remains the regression canary. Finance must remain `RESEARCH / 0 SEK / NONE` with unchanged scheduler and governor.

## BB-098 global UI migration

Treat BB-097 Family as the visual canary. Frontend regression must cover semantic button variants including busy/disabled state, native label/value behavior for inputs and selects, theme switching and existing route actions. Normal viewport review—not stitched full-page capture—covers Home, Family, Media, Finance, More/Settings, AI and Admin in Obsidian Gold, Forest Night and Arctic Wind at 390 × 844, plus 430 × 932, 768 × 1024 and 1440 × 900 responsive checks. Finance advanced disclosure remains accessible and Finance must stay `RESEARCH / 0 SEK / NONE`. The owner-confirmed iOS Safari full-page screenshot artifact remains a known limitation and is not a BB-098 acceptance surface.

## BB-097 Family reference validation

Family behavioral regression verifies the dedicated reference composition, absence of a normal-mode `DashboardWidget` wrapper, semantic page heading, settings access, meal tabs, shopping mode and calendar access. Existing Meal Planner, Shopping List, Calendar, AppShell, theme and navigation suites remain authoritative for detailed actions and accessibility. Pixel snapshots are deliberately not used. Separate manual browser evidence must render both Obsidian Gold and Forest Night repeatedly at 390 × 844 and then at 430 × 932, using normal viewport captures and additional scroll positions where needed. Compare atmosphere, materiality, tonal and typographic hierarchy, accent restraint, border/radius treatment, lighting, dock placement and theme identity against the original local mockups. Full-page capture is diagnostic only because fixed/composited backgrounds may produce stitching artifacts; it never replaces normal viewport review. Fixture-only visual runs must be identified as such and never represented as deployed evidence.

Real-iPhone owner evidence is authoritative for iOS Safari capture behavior. As of the 2026-08-23 owner-directed pass, the large bright rounded/geometric artifact remains reproducible in stitched full-page iOS Safari screenshots for Obsidian Gold and Forest Night while absent in normal interaction. Desktop/headless non-reproduction must never be reported as an iOS fix. Normal 390 × 844 top and scrolled viewport frames remain the art-direction acceptance surface; 430 × 932 is the responsive smoke surface.

The final BB-097 craftsmanship pass additionally requires the normal mobile Söndag/Idag/Smashed Burgers/Byt-open state in both dark themes. Verify automatic, manual and dismiss actions through component tests; viewport review must not mutate real meal data. The primary action receives focus, Escape dismisses the contextual panel, action targets remain at least 44 px except the intentionally tertiary 40 px dismiss target, long Swedish meal names wrap without colliding with Byt, and `prefers-reduced-motion` removes the new panel/control motion.

BB-092 adds network-free tests for the allowlisted research feature registry, invalid IDs and bounds, deterministic hypothesis fingerprints, explainable complexity, OOS/cost/lineage fail-closed integrity, family attempt visibility, DSR/PBO `NOT_EVALUABLE`, and conservative read-only UI language. Remediation coverage also exercises partial-run evidence retention, persisted restart recovery, cross-key global single-flight, same-key failed-result idempotency, actual 3/5/2 attempt accumulation, bounded/filterable history, count reconciliation and target/horizon consistency. Final evidence-selection cases prove that a second feature/robustness generation wins over lexically earlier stale history, incomplete current families do not fall back, exact market lineage and approved strategy versions are mandatory, and repeated unchanged selection stays deterministic. It reuses BB-081's train/test leakage, cost monotonicity and real expanding-window tests. Research tests never call providers or create PAPER, broker, order, portfolio, LIVE/AUTO or risk-policy state.

BB-093 scheduler tests use injected times/direct orchestrator calls rather than real sleeps. They cover default-disabled startup, due completion, repeated ticks, completed and pre-run restart recovery, no catch-up storm, recovery/data deferral, manual-run busy deferral, current-evidence failure, option bounds, cancellation, bounded APIs, read-only UI wording and unchanged `RESEARCH / 0 SEK / NONE` authority. Readiness remediation additionally covers one-current/rest-stale rejection, full-universe recovery on the same opportunity, stale feature deferral, exact source-lineage mismatch, deterministic readiness, zero experiment evidence on partial acquisition and explicit cross-date supersession of deferred work.

BB-094 resource-governor tests inject deterministic `ISystemMetricsProvider` snapshots; they never depend on workstation load. Coverage includes healthy allow, independent and combined CPU/memory/disk pressure, critical-disk precedence, unavailable/stale/throwing metrics, option bounds, no-run deferral followed by same-opportunity completion, restart-readable compact audit, read-only API/UI state and unchanged `RESEARCH / 0 SEK / NONE` authority. Temperature remains explicitly unsupported and is not faked.

BB-095 operations tests use isolated SQLite stores and injected timestamps. They cover disabled/maintenance semantics, stale enabled scheduling, persistent readiness/resource waits, operational-versus-scientific failure classification, deduplicated incident streaks, success recovery, pre-run interruption, partial experiment preservation, post-run scheduler reconciliation, repeated reconciliation, bounded read APIs, compact metadata backup/restore and unchanged `RESEARCH / 0 SEK / NONE` authority. Hosted-service tests never wait for real scheduled time or contact providers.

BB-096 frontend regression covers the three stable theme IDs, default/fallback and migration aliases, local/server persistence, immediate switching, shared token completeness, five-item primary navigation, secondary AI/Admin access, Family relocation without functional removal, dashboard editing and the existing module interaction suites. Visual review uses temporary, uncommitted browser captures at mobile and desktop sizes; the local design mockup binaries are references and are not test fixtures.

BB-089 adds network-free policy, invariant and adversarial tests for deterministic identity,
version/config validation, ALLOW/REDUCE/DENY/HALT/INSUFFICIENT_DATA, EOD weekend freshness,
clock/lineage/instrument/price/health/volatility/liquidity/exposure failures, client-forged verdicts,
simulated daily loss/drawdown/consecutive losses, immutable idempotence and durable audited halt
recovery. Tests never create an order, broker connection, real portfolio or provider call.

BB-088 adds network-free tests for weekday/provider-window scheduling, healthy weekend/no-provider
cycles, cadence timestamps, bounded read-only status/overview endpoints, repeat outcome evaluation,
actual market breadth, transparent POSITIVE/NEUTRAL/NEGATIVE aggregation, pending/sample honesty,
historical/prospective separation and absence of fake index, portfolio, real-time or order claims.
Runtime provider verification is separate and must not manufacture a weekend session.

BB-087 tests are network-free. They cover deterministic prediction identity, retry idempotence, knowledge cutoff/no-lookahead selection, strategy/parameter/source lineage, explicit horizon, pending-to-evaluated temporal progression, append-only outcomes, clock fail-closed, late-start anti-backfill, bounded/malformed read API, UI pending/sample honesty and absence of mutation/order controls. Full API, Sentinel and frontend regression plus Release/build/documentation/secrets/Compose checks remain required before publication.

BB-086 changes research/planning documentation only because all eight ETF-history candidates
failed closed before acquisition. No runtime contract or fixture changed. Publication verification
therefore runs the complete existing backend/frontend suites and builds, documentation verifier,
secret scan, Compose validation and `git diff --check`; ordinary tests remain network-free.

BB-085 tests are network-free and cover WIKI/EODHD/unknown source classification, deterministic
manifests, atomic/incomplete-state handling, SHA-256 corruption rejection, isolated restore
identity, derived lineage, disk gates, rejected/manual-review cleanup, idempotence and canonical
protection. Runtime drills use only existing local Finance evidence and make no provider calls.

BB-084 tests are network-free and use sanitized CSV/rights fixtures. They cover candidate
transitions, content/schema hashes, promotion PASS/FAIL/UNKNOWN, CSV quoting, ZIP traversal,
OHLCV/duplicate policy, symbol-bounded promotion, cross-source classification and idempotent
re-import. Live WIKI/Zenodo acquisition is a one-time maintenance verification, never an
ordinary test dependency. Long-history robustness also verifies the explicit run-budget cap.

BB-083 tests clean/unclean markers, idempotent recovery, missed-run policies and conservative
interrupted EODHD acquisition without live calls. systemd/reboot remain separate host tests;
CI need not run systemd as PID 1. The verifier prints sanitized states/counts only.

Detta dokument är en kort karta. Auktoritativa procedurer ligger i respektive runbook och modulkontrakt.

## Automatiska tester

Frontend:

```bash
cd src/BigBrain.Web
npm ci
npm test -- --run
npm run build
```

Backend och Sentinel med den lokala .NET 10 SDK:n, från repositoryroten och utan sudo:

```bash
dotnet restore BigBrain.slnx
dotnet build BigBrain.slnx --configuration Release --no-restore
dotnet test BigBrain.slnx --configuration Release --no-build --no-restore
```

Dokumentation och repositoryhygien:

```bash
node scripts/verify-documentation.mjs
git diff --check
docker compose config --quiet
```

## Verifieringskarta

- Dashboard/widgetramverk, persistence, responsiv kontroll, Web-only deployment och rollback: [dashboardrunbook](docs/operations/runbooks/dashboard-widget-framework-verification.md).
- Kalender/Heroma: [modulkontrakt](docs/modules/calendar.md), [import-runbook](docs/operations/runbooks/heroma-schedule-import.md) och [verifieringsrunbook](docs/operations/runbooks/calendar-verification.md). Verkliga Heroma-filer får aldrig användas i automatiska tester; workbooks genereras syntetiskt.
- Media API och read-only providerkontroll: [Media integration verification](docs/operations/runbooks/media-integration-verification.md).
- Smart Shuffle: [Mediamodulen](docs/modules/media.md), [ADR 0011](docs/adr/0011-smart-shuffle-jellyfin-remote-playback-boundary.md) och samma media-runbook.
- Download Control: [säker borttagningsrunbook](docs/operations/runbooks/download-control-safe-removal.md), [ADR 0013](docs/adr/0013-safe-qbittorrent-download-removal-boundary.md) och [ADR 0016](docs/adr/0016-safe-download-control-command-and-partial-batch-boundary.md). Automatiska tester får aldrig mutera riktiga torrents.
- Designsystem och teman: [manuell verifieringsplan](docs/design-system/manual-verification.md), [theme contract](docs/design-system/theme-contract-v1.md) och [Jellyfin-runbook](docs/operations/runbooks/jellyfin-bigbrain-theme.md).
- qBittorrentdiagnostik: [queue/peer-runbook](docs/operations/runbooks/qbittorrent-queue-and-peer-diagnosis.md).
- Aktuell verifieringsstatus: [STATUS](docs/STATUS.md).
- Finance: [testing and validation strategy](docs/architecture/finance/testing-and-validation.md),
  including invariant, simulation, paper, sandbox, failure-injection, reconciliation,
  security, UI/accessibility, performance and soak layers. No Finance test may access a
  live broker or real credentials.
  Market-data tests must prove fail-closed entitlement, immutable provenance, derived
  lineage, correction supersession and retention/deletion scope with synthetic fixtures
  until an exact provider/product is entitlement-cleared, selected and explicitly approved
  for activation; BB-071 evidence alone does not activate a provider.
  BB-075 fail-closed tests additionally assert that the runtime reports the current
  zero-cost entitlement gate rather than superseded State B wording, while every ingestion,
  storage, broker, PAPER and LIVE flag remains false. BB-076 entitlement tests cover
  zero-cost/versioned owner acceptance, capability-specific denial precedence, paid-source
  rejection and fail-closed confirmation/denied evidence.
  BB-077 EODHD tests use documented-shape sanitized JSON fixtures and cover parsing,
  impossible data, 429 retry bounds, symbol mapping, durable SQLite restart/idempotency,
  content-addressed payloads, deterministic exact-revision replay, expiry blocking,
  deletion preview/confirmation/receipt, unrelated-file protection and sanitized API/UI.
  BB-078 adds a network-free runtime-evidence projection test and runs the command against
  the deployed volume before/after restart. It exposes only request/catalog counts, symbols,
  coverage, revision IDs, payload-reference integrity, causal knowledge-time status and replay checksums; never token or
  raw payload content. The single bounded provider acquisition is runtime evidence, not an
  ordinary automated-test dependency.
  BB-079 adds hand-verifiable formula tests for returns, SMA/EMA, momentum, population
  volatility, Wilder RSI/ATR and volume features; edge, warmup/gap, deterministic checksum,
  correction lineage and explicit future-horizon/no-lookahead tests; SQLite reopen/
  idempotency, retention deletion scope, bounded feature API and responsive feature UI.
  Runtime feature builds consume only the existing local memory and must not trigger an
  EODHD request.
  BB-080 adds hand-verifiable next-open/cash/position/whole-share/fee/slippage/exit/final-equity tests; explicit future-bar/feature and same-close no-lookahead proofs; repeated-run identity/checksum/journal/curve determinism; insufficient-cash, warmup, repeated-signal, missing-next-session and retention inventory coverage. Real runs are offline maintenance commands and must not call a provider.
  BB-081 adds chronological 60/40, 70/30 and 80/20 split tests, configurable embargo/no-overlap checks, bounded-grid/isolated-peak tests, higher-cost monotonicity, explicit insufficient train/test/walk-forward evidence, future-feature invisibility, test-mutation isolation, earlier walk-forward stability, evaluation ID/checksum determinism, SQLite retention and read-only UI language coverage. Evaluation commands read local memory only.
  BB-045 policy/provenance tests use only `ExampleData` synthetic fixtures and cover
  exact provider/product scope, missing/unknown/denied/expired policy, persistence,
  post-subscription retention, immutable revision state and raw/derived lineage.
  Canonical-normalization tests additionally cover historical symbol boundaries, MIC venue
  distinction, overlap/unknown mapping rejection, decimal daily OHLCV invariants, raw and
  adjusted classification, dividends, exact split ratios, immutable revision/policy
  references, duplicates/conflicts and repeatable output. No calendar is guessed: future
  expected no-trading days, unknown missing observations and provider gaps remain distinct.
  Session/replay tests use an explicit `Europe/Stockholm` fixture calendar and verify UTC/DST,
  invalid/ambiguous local times, closure/unknown/missing/provider-gap distinctions,
  invalid-observation quarantine, historical ticker resolution, explicit dividends/splits,
  immutable revision binding, no-lookahead, range bounds and deterministic event order.
  Revision-assembly tests verify original/corrected as-of views, inclusive availability,
  immutable old revisions, explicit linear supersession, correction references/cycles,
  deterministic multi-correction order, policy/provenance, corporate-action time,
  inherited session/gap evidence and rejection of future/unavailable membership.
  Acquisition tests require exact multi-use entitlement before adapter invocation and cover
  deterministic requests/batches, synthetic-only identity, unauthorized provider/retention,
  repeated batches, overlapping pagination, correction supersession, journal evidence,
  canonical normalization, explicit provider gaps, immutable revision assembly, repeated
  replay/no-lookahead and absence of secret-bearing contract fields.
  Persistence-foundation tests cover deterministic manifests/checksums, immutable exact
  revision roundtrip, idempotent duplicate append, explicit conflicts, correction lineage,
  gap/action queries, policy-scoped enumeration/deletion receipts, partial-write rejection,
  replay compatibility and no-lookahead. Run the reproducible fixture benchmark with
  `dotnet run --project tools/BigBrain.Finance.PersistenceBenchmarks -c Release --no-build -- --full`;
  it writes only process-scoped temporary files and compares JSONL/SQLite without external IO.
  Live-observation tests use only an injected synthetic feed and explicit UTC evidence.
  They cover event/provider/received/knowledge causality, honest delay classification,
  deterministic and out-of-order delivery, duplicate/correction preservation, missing/
  outage/session events, fail-closed entitlement, immutable versioned prediction/outcome,
  cost-aware prospective metrics, no-lookahead and absence of broker/order/secret surfaces.
  The 2026-08-11 combined historical/live provider gate is documentation-only. It changes
  no domain/runtime code and adds no .NET test delta; source links, scorecard evidence and
  fail-closed language are covered by documentation verification and `git diff --check`.
  The BB-071 resolution is likewise documentation-only: the existing 376-test synthetic
  baseline is rerun, while no provider/network acceptance test is authorized.
  BB-082 follows the legitimate blocked path and changes no executable contract. Provider
  research is verified through dated primary-source links, bounded request accounting,
  documentation link/BB-ID validation and `git diff --check`; no live market-data test,
  fixture parser test or runtime deployment is applicable because no adapter/data exists.
  BB-074 tests prove fail-closed RESEARCH/no-provider/no-order API state and deterministic
  synthetic mapping. Web tests cover navigation, no-real-money and entitlement warnings,
  empty/synthetic/stale/gap/memory/chart states, native keyboard controls and no trade UI.
  Sprint 1 testar decimalprecision, invariants, UTC, provider-neutral fixture-data,
  strategy-/orderseparation, fail-closed risk/policy, NO TRADE/REJECTED-journal,
  korrelationskedja och att endast PAPER kan skapa ett lokalt paper-intent.

## Live-säkerhetsregel

Automatiska tester använder fakes/mocks och får aldrig anropa live write-endpoints. De får inte starta Jellyfin-uppspelning, ta bort eller ändra torrents, mutera Sonarr/Radarr/Prowlarr, ändra media, starta om externa tjänster eller använda riktiga credentials. Verkliga mutationer får endast ske genom dokumenterat UI-flöde efter uttrycklig användaråtgärd och separat scope.

Media har både read- och smala write-kontrakt. Påståendet att Media saknar POST/write-endpoints är historiskt och gäller inte dagens implementation. Läs [Mediamodulen](docs/modules/media.md) för aktuella gränser.
# BB-090 test additions

Network-free fixtures and tests cover macro release/knowledge cutoffs, vintage selection, forward-fill only after knowledge time, migration restart/idempotence, New York DST, regular holidays, weekends and bounded exceptional closures. Dataset/risk regression covers adjusted semantics, provider-aware promotion, typed insufficient/warmup categories and exact prediction-risk lineage. Live FRED acquisition is a bounded maintenance drill and never an automated-test dependency.

BB-090 closure adds empty/legacy/interrupted/concurrent migration coverage, rejected Macro quarantine candidates, strict evidence-class selection, Juneteenth and exact DST transition dates, immutable invalid WIKI adjusted evidence, configurable provider-neutral risk policies and deterministic multi-verdict frontend aggregation. Finalization additionally verifies the official FRED JSON `output_type=2` column schema and rejects non-vintage response shapes. The 2026-08-16 finalization regression passed 440 API, 32 Sentinel and 113 frontend tests. Production migration drills must compare `finance-evidence-counts` before/after; secrets are never test output.

# BB-091 test additions

Network-free sanitized Riksbank JSON and ECB SDMX CSV fixtures cover selected-series identity, policy/FX values, explicit base/quote semantics, malformed artifacts, rights denial, quarantine rejection, exact-artifact idempotence, cross-provider EUR/SEK tolerances and region/evidence-class as-of isolation. Live official acquisition is maintenance evidence only. Current-history bootstrap remains revised-history exploratory.
## BB-129A campaign verification

Focused tests cover deterministic bounded population, categorical fail-closed dispositions, BB-127 eligibility/schema boundaries, BB-123 execution regression and BB-124 selection/holdout regression. Completion additionally requires full API, Release build, architecture/documentation/Compose, diff and full-history secret gates. Runtime must verify both campaign read APIs and `RESEARCH / 0 SEK / NONE`.

Result 2026-09-03: focused Finance 40/40, full API 608/608 and Sentinel/architecture 32/32 passed. Release solution build passed with zero warnings/errors. Documentation verification passed 217 files/89 unique IDs; Compose and diff checks passed. Local gitleaks was unavailable; GitHub Actions run `33732206649` passed backend, frontend, documentation and authoritative full-history secrets. API health and campaign catalog/detail returned successfully after API-only deployment. Fixed-input replay returned campaign/checksum unchanged and catalog count remained one.
