# Codex interrupted-run recovery

Den här filen är den enda kanoniska platsen för en tillfällig, sanerad överlämning när en Codex-körning faktiskt avbryts. Lämna mallen orörd under slutförda uppdrag. Vid återupptagning ska `AGENTS.md` följas först; synka GitHub, jämför repositoryt med noten och stoppa vid konflikt.

## Recovery note template

```text
Status: INTERRUPTED — SAFE TO RESUME | INTERRUPTED — MANUAL REVIEW REQUIRED
Task:
Baseline/source-of-truth SHA:
Git status:
Changed files:
Completed and valid:
Remaining:
Tests/builds already run and results:
Blockers/assumptions:
Exact next action:
```

Noten får inte innehålla hemligheter, credentials, privata adresser, råa känsliga loggar eller förbjudna identifierare/data. Giltiga working-tree-ändringar ska bevaras; ofullständigt arbete får inte committas utan uttryckligt godkännande. Ta bort ifylld avbrottsstatus när originaluppdraget är färdigt och publicerad GitHub-historik åter är fullständig source of truth.

## BB-130 accepted closure — 2026-09-21

Status: COMPLETE / EXIT APPROVED / ACCEPTED / MERGED / CI VERIFIED.
A/B COMPLETE; C/D COMPLETE / EXIT APPROVED; BB-130 COMPLETE / EXIT APPROVED.
No interrupted or pending-review BB-130 implementation remains. Main is source of truth.

### Accepted publication — 2026-09-21

Approved candidate `672d121a14024b485233448ab792870297328514` was exactly one commit ahead,
zero behind baseline `f941f4553fabaffcea8736ccd7f0e5d22233a976`, with only the eight reviewed
documents. Merge `dfce6575d3c05d078b61b30d06a0dd12413099e4` has baseline as first parent
and approved candidate as second parent. Candidate and merge share tree
`84e695cf26b1bece686d0070e2710198964f66f0`; content is identical.
[Merge CI 35607620156](https://github.com/T-bear/BigBrain/actions/runs/35607620156)
completed SUCCESS on that exact merge SHA. Backend/frontend/documentation/secrets all
executed and passed. Backend restore → dotnet-format verify → Release build → tests and
frontend npm ci → npm run format:check → tests → build all have actual SUCCESS step results.
No implementation, package, CI, formatter configuration, test or runtime change; no deployment.
Only pending-candidate wording required this separate documentation reconciliation.

The separate documentation reconciliation changes only the eight reviewed documents.
Resolve its final SHA with `git log -1 --format=%H --grep='^docs: reconcile accepted BB-130 closure$' main`.
Completion requires the Actions run for that exact SHA to show all four jobs SUCCESS,
including actual backend dotnet-format and frontend npm run format:check success.
Do not substitute merge CI for this final exact-main verification.
Finance RESEARCH / 0 SEK / NONE. Deferred debt/security prerequisites remain deferred;
no deployment or subsequent sprint/checkpoint is authorized. Unrelated untracked mockups
and ADR 0006–0009 are preserved and excluded.
Exact next action after final CI: STOP — BB-130 is closed. Return to owner/architect for
selection and authorization of the next BigBrain sprint; do not start it.

## Historical checkpoint/recovery ledger

The completed records below preserve exact evidence and earlier next-action wording;
they do not reopen an interrupted task or authorize work. Current review state is above.

## BB-130D2 frontend formatter CI gate accepted publication — 2026-09-21

Status: ACCEPTED / MERGED / CI VERIFIED. No interrupted CI-gate work remains.

Owner/architect approved exact candidate `fde82cfc65e5e303f814c048d9032d6bd4f27f34`,
merged unchanged as `d1b1dde14071fdcb646d99b383bf0a1819bbd0ca` from baseline
`1e3552c5142ee3360e6a96036e8f7ed86b4f4d57` (one candidate commit).
[Merge CI 35542112456](https://github.com/T-bear/BigBrain/actions/runs/35542112456):
backend/frontend/documentation/secrets SUCCESS. Actual frontend steps npm ci →
**npm run format:check SUCCESS** → npm test -- --run → npm run build all passed.
Backend restore → dotnet format --verify-no-changes --no-restore → Release build → tests
all passed. Both formatter gates are now enabled/enforced on main, check-only.
Post-merge local format:check: exit 0, 83/83; audit: exit 0, zero findings.
Prettier remains 3.9.6 dev-only; package/lock/config/scripts/source/backend/runtime unchanged.
Candidate tests remain 199/199 in 26 files and build PASS; hosted test/build steps also pass.
Negative characterization remains candidate evidence, not a new production mutation.
No write mode, two-pass workaround or auto-fix exists in CI. Historical ThemeControl.test.tsx
convergence remains cleanup history only. No deployment. Finance RESEARCH / 0 SEK / NONE.
BB-130D remains IN PROGRESS. Proposed next action: **BB-130D final reconciliation / exit
assessment**, requiring separate authorization; no lint or agent-neutral workflow work starts.

Reconciliation changes documentation only: STATUS, BACKLOG, TESTING, stabilization plan,
this recovery note, report catalog and the existing gate report. Resolve its exact SHA with
`git log -1 --format=%H --grep='^docs: reconcile accepted BB-130D2 formatter CI gate$' main`.
Final publication requires the Actions run for that exact SHA to show all four jobs SUCCESS,
including actual frontend npm ci/format:check/test/build and backend format step success.
Do not substitute the merge CI run for final reconciliation CI evidence.
Candidate local evidence remains valid: baseline/post-edit npm ci, 83/83 format check,
199/199 tests in 26 files, build PASS, audit zero; negative check exit 1 then exact restoration
and positive exit 0. No temporary source mutation remains. Unrelated untracked mockups and
ADR 0006–0009 are preserved/excluded. Next safe action after final CI: STOP for architect
review. No subsequent checkpoint is authorized; BB-130D exit is not started.

## BB-130D2 frontend formatter cleanup accepted publication — 2026-09-21

Status: ACCEPTED / MERGED / CI VERIFIED. No interrupted cleanup implementation remains.

### Accepted publication — 2026-09-21

Owner/architect approved candidate `b547219473c7ef12cec81b6778bd454a2ce33058`,
exactly one commit above baseline `5cb179d2558fb9bddc8f256d0fcc06251826a1f5`.
Codex merged it unchanged as `8e966404b2b7055acf21ccedabaa883ecaf225ca`; the merge
file tree equals the approved candidate. Claude implemented the cleanup; Codex performed
only this approved merge/publication, without formatter write mode.
[Merge CI 35540892280](https://github.com/T-bear/BigBrain/actions/runs/35540892280):
SUCCESS for backend, frontend, documentation and secrets. Backend restore → actual
`dotnet format BigBrain.slnx --verify-no-changes --no-restore` → Release build → tests
all passed. Frontend ran only `npm ci`, `npm test -- --run`, `npm run build`.

Fresh post-merge local commands: `npm ci`, `npm run format:check`, `npm test -- --run`,
`npm run build`, `npm audit --json` all exit 0: 83/83 conforming, 199/199 tests in 26 files,
production build PASS (70 modules), zero audit findings. Prettier remains exactly 3.9.6,
dev-only. Scope comparison confirms exactly the accepted 80-file debt set; the three
already-clean files, package/lock, formatter config/scripts, CI, backend, CSS, excluded JS
and runtime/deployment configuration are unchanged. The focused Finance 46/46 and semantic/
artifact comparisons above remain evidence from Claude's reviewed candidate, not new runs.
The owner-approved ThemeControl.test.tsx historical two-pass convergence remains documented;
the committed source is already conforming and no write-mode operation was run for publication.

Cleanup is ACCEPTED / MERGED / CI VERIFIED. A/B COMPLETE; C COMPLETE / EXIT APPROVED;
D IN PROGRESS. Frontend formatter CI gate NOT STARTED / NOT ENABLED; lint deferred.
Finance RESEARCH / 0 SEK / NONE. No deployment, runtime, device, UX or scientific behavior
change. Next proposed checkpoint: **BB-130D2 — Frontend Formatter CI Gate**, separately
authorized; neither it nor the agent-neutral workflow/recovery improvement starts here.

Reconciliation changes only STATUS, BACKLOG, TESTING, the stabilization plan, this recovery
note, the report catalog and the existing cleanup report. Its exact SHA is resolved from
`git log -1 --format=%H --grep='^docs: reconcile accepted BB-130D2 formatter cleanup$' main`;
the matching GitHub Actions run must use that exact head SHA and show all four jobs SUCCESS.
Publication completion requires this final CI check, including actual backend format success
and frontend install/test/build only. Do not infer final CI from the earlier merge run.
Unrelated untracked design mockups and ADR 0006–0009 remain preserved and excluded.
Next safe action after final CI: STOP and return to architect review; no next checkpoint
is authorized. The historical handoff below records Claude's implementation and is retained
as evidence, not current merge instructions.

## Historical pre-merge handoff — 2026-09-20

```text
Status: REVIEW CANDIDATE ONLY — NOT MERGED (no interrupted work remains)
Task: BB-130D2 — Frontend Formatter Cleanup, bounded formatter-only cleanup of the accepted
      83-file TS/TSX scope. Agent handoff: Codex → Claude; the Codex session ended because its
      weekly usage limit was exhausted. Claude recovered and completed the checkpoint.
Baseline/source-of-truth SHA: 5cb179d2558fb9bddc8f256d0fcc06251826a1f5
      Verified equal to origin/main by git ls-remote at start, before documentation and again
      immediately before publication.
Branch: bb-130d/frontend-formatter-cleanup. Candidate SHA and remote branch SHA are recorded
      in STATUS, the report and Git history; resolve with
      git log -1 --format=%H --grep='^style: apply BB-130D2 frontend formatter cleanup$'
      bb-130d/frontend-formatter-cleanup, and compare with
      git ls-remote origin refs/heads/bb-130d/frontend-formatter-cleanup.
      The candidate's own SHA is not embedded recursively in itself.
Git status: one bounded candidate commit on the branch; no merge, no push to main, no force
      operation. Unrelated untracked design mockups and unpublished ADR 0006-0009 preserved
      and excluded from the commit.
Changed files: exactly the 80 characterized nonconforming files under
      src/BigBrain.Web/src/**/*.{ts,tsx} (15 .ts, 65 .tsx), 12,873 insertions and 2,498
      deletions, plus seven documentation files: this note, the cleanup report, STATUS,
      BACKLOG, the BB-130 stabilization plan, TESTING and the report catalog.
Completed and valid: recovery reconstruction from Git; formatter contract reproduced;
      baseline; formatter write mode; scope verification; semantic/AST/literal/JSX
      verification; production artifact comparison; Finance verification; regression;
      owner decision applied; documentation; publication gates; candidate commit and push.
Remaining: architect review of the exact candidate SHA, then owner approval before any merge.
Tests/builds already run and results (all reproduced by Claude, not inherited):
      pre  - npm ci 0; tests 199/199 in 26 files; build 0 with 70 modules; audit 0;
             format:check exit 1 with 80 nonconforming of 83 selected.
      post - format:check exit 0 with 83/83; a further npm run format is a no-op; tests
             199/199; focused Finance 46/46; build 0 with 70 modules; audit 0.
      Production artifacts: CSS, icons and manifest byte-identical; index.html differs only in
      the content-hashed JS filename; JS bundle +81 bytes, proven to be adjacent JSX text-child
      splits only (concatenated literals byte-identical at 82,558 bytes; literal-elided code
      skeletons byte-identical at 332,891 bytes after collapsing adjacent-literal runs).
      Gates: documentation verifier 0; git diff --check and --cached --check 0;
      Gitleaks 8.28.0 full history 0 and candidate-file scan 0.
Blockers/assumptions: DISCOVERED DEVIATION, owner-accepted, deliberately preserved.
      src/ThemeControl.test.tsx is not a Prettier 3.9.6 fixed point after one write pass; a
      second pass converges and a third is a no-op. An in-memory sweep shows exactly 82 of 83
      files reach a fixed point on pass 1. The pass-1 to pass-2 change is member-chain line
      breaking inside a test setup block, with no token, argument or literal change. The owner
      approved option (a) on 2026-09-20: accept the converged two-pass result as the cleanup
      candidate baseline with the deviation documented, explicitly NOT establishing "run
      Prettier twice" as the normal workflow. The committed source is at the stable fixed
      point; future formatter CI must verify that committed source is already conforming and
      must NOT depend on a second write pass. This is a presentation-level formatter defect,
      not a correctness, security or scientific defect, so blocker handoff does not apply.
      Verification limitation: semantic equivalence was proven with Prettier 3.9.6's own
      bundled TypeScript parser, because TypeScript 7.0.2 here is the native port and exposes
      no JavaScript compiler API; the proof binds this pinned parser, version and file set.
Exact next action: STOP for architect review and owner acceptance of the exact candidate SHA.
```

Evidence, method and limitations:
[BB-130D2 frontend formatter cleanup](../reports/features/platform/bb-130d2-frontend-formatter-cleanup-20260920.md).

Evidence Claude independently reproduced: every command, count, SHA and comparison above.
Historical evidence reported by Codex but NOT independently reproduced, and not relied upon:
that Codex had read the documentation, verified main, inspected Finance frontend code and
begun parser/AST tooling before its usage limit ended. Git evidence proves Codex created the
branch and made **no** tracked change; formatter write mode had not run before this session.

A/B COMPLETE; C COMPLETE / EXIT APPROVED; D IN PROGRESS. D1, backend cleanup and the backend
format gate remain accepted and enabled; D2 characterization, triage, dependency maintenance
and formatter tooling remain accepted. **Frontend formatter cleanup is IMPLEMENTED / TESTED /
REVIEW CANDIDATE ONLY — not accepted, not merged, not deployed.** No frontend formatter CI
gate exists and lint remains deferred. Finance RESEARCH / 0 SEK / NONE; no provider, broker,
orders, PAPER, LIVE, AUTO, capital, scientific, runtime, deployment or device change occurred.

The next agent is NOT authorized to: merge this branch, enable a frontend formatter CI gate,
deploy, force push, rebase, hand-edit application source, start another checkpoint, begin the
agent-workflow/recovery redesign, or perform Finance feature or Research Learning work.

## BB-130D2 formatter tooling accepted publication — 2026-09-17

Status: ACCEPTED / MERGED / CI VERIFIED; no interrupted implementation.
Task: approved formatter-tooling merge and documentation publication only.
Original baseline: `d7785b4ef2fea271cb78f0019c486b60acdad210`.
Owner/architect approved exact candidate `d71ab8553871d9c5eb89c151ad8cb0392e0de08a`,
merged unchanged as `ba0037ee41f16476e74037f090e83e2e3a78f205`.
[Merge CI 35190196135](https://github.com/T-bear/BigBrain/actions/runs/35190196135): SUCCESS
for backend/frontend/documentation/secrets. Backend restore → actual dotnet format check →
Release build → tests passed. Frontend npm ci/test/build passed; no frontend formatter CI gate.
Post-merge local npm ci/test/build/audit: exit 0, 199/199 tests, production build, audit zero.
Prettier 3.9.6 dev-only, config/scripts unchanged; exact scope 83, 80 nonconforming,
format:check exit 1 expected, output identical to characterization. No write formatting.

Only seven docs reconciled: STATUS, BACKLOG, TESTING, stabilization plan, REPORT-CATALOG,
this note and [tooling report](../reports/features/platform/bb-130d2-frontend-formatter-tooling-20260917.md).
Unrelated mockups/ADR 0006–0009 preserved/excluded. No application/tests/backend/CI/runtime
change, lint tooling, dependency update or deployment during publication.
A/B COMPLETE; C COMPLETE / EXIT APPROVED; D IN PROGRESS. D1/backend cleanup/gate and D2
characterization/triage/maintenance/tooling accepted; backend formatter gate enabled.
Frontend cleanup and frontend formatter gate NOT STARTED / NOT COMPLETE; lint deferred.
Finance RESEARCH / 0 SEK / NONE; no scientific, provider/broker/orders/PAPER/LIVE/AUTO/capital work.

Reconciliation publication checks: documentation verifier exit 0 (242 Markdown files / 90 BB IDs);
working/staged diff checks exit 0; Gitleaks v8.28.0 full-history exit 0 (276 commits) and staged
check exit 0, no leaks. Seven docs only; source/tests/CI/runtime unchanged. Prepublication fetch
confirms exact merge-main and unchanged approved candidate. Final exact-main CI still required.

Final publication resolution: separate main commit `docs: reconcile accepted BB-130D2 formatter tooling`
contains this record. Resolve using
`git log -1 --format=%H --grep='^docs: reconcile accepted BB-130D2 formatter tooling$' origin/main`.
Compare HEAD/origin/main with `git ls-remote origin refs/heads/main`; inspect exact head_sha
Actions for backend/frontend/documentation/secrets SUCCESS. Backend actual format step must
pass after restore/before build/test; frontend remains npm ci/test/build only. Own future SHA/run
is resolved from GitHub history, not recursively embedded. If interrupted before final CI,
preserve valid work/commit and continue first incomplete verification; no repeated merge/commit.
After final exact-main CI STOP and return to ChatGPT with "Codex är klar".
Next proposed checkpoint: BB-130D2 — Frontend Formatter Cleanup, separately authorized,
with semantic/JSX review and strong verification of the large 80-file diff. Do not start here.

## BB-130D2 dependency maintenance accepted publication — 2026-09-16

Status: ACCEPTED / MERGED / CI VERIFIED; no interrupted implementation.
Task: approved dependency-maintenance merge and documentation publication only.
Original baseline: `914d35e596cd066b846e4f6d1c59632b1be84137`.
Owner/architect approved exact candidate `b043bffe21c3cc05f2c57c087d6aac3f26fa0e84`, merged unchanged as
`55b5b5d7e4e982f7d1a194cdab554a4bf419a471`. [Merge CI 35124925562](https://github.com/T-bear/BigBrain/actions/runs/35124925562)
SUCCESS: backend/frontend/documentation/secrets. Backend restore → format → Release build →
tests all executed successfully; frontend npm ci/test/build passed, no formatter/linter gate.
Post-merge npm audit --json exit 0, zero findings; installed graph verifies all target versions
and all eight prior GHSAs remain outside their assessed affected installed ranges.

Vitest + seven companions 4.1.11; PostCSS 8.5.23; nanoid 3.3.18; undici 7.29.0.
Historical audit five packages/3 moderate/2 high; eight GHSAs assessed. Current audit zero is
not blanket security approval. No dependency changes beyond approved candidate; npm edgesOut
resolution limitation remains historical implementation evidence in the
[maintenance report](../reports/features/platform/bb-130d2-frontend-dependency-maintenance-20260916.md).
Only seven docs reconciled: STATUS, BACKLOG, TESTING, stabilization plan, REPORT-CATALOG,
this note and maintenance report. Unrelated untracked mockups/ADR 0006–0009 preserved/excluded.
No source/tests/backend/CI/runtime changes, deployment, audit fix or formatter/linter installation.
A/B COMPLETE; C COMPLETE / EXIT APPROVED; D IN PROGRESS. D1/backend cleanup/gate and D2
characterization/triage/maintenance accepted; backend gate enabled. Frontend formatter
cleanup/tool installation/gate NOT STARTED / NOT COMPLETE. Finance RESEARCH / 0 SEK / NONE.

Reconciliation checks: documentation verifier exit 0 (241 Markdown files / 90 BB IDs);
working/staged diff checks exit 0. Gitleaks v8.28.0 full-history exit 0 (274 commits)
and staged check exit 0, no leaks. Only seven documentation files changed/staged.
Source/tests/workflow/runtime comparison to merge-main exits 0. No local suites repeated
for documentation-only changes; exact-main CI verifies the published tree. README,
ARCHITECTURE/ADRs, ROADMAP, modules, knowledge/index and runbooks reassessed: no updates
required for unchanged architecture/contracts/runtime. Historical triage reports unchanged.

Final publication resolution: separate main commit `docs: reconcile accepted BB-130D2 dependency maintenance`
contains this record. Resolve with
`git log -1 --format=%H --grep='^docs: reconcile accepted BB-130D2 dependency maintenance$' origin/main`.
Compare HEAD/origin/main with `git ls-remote origin refs/heads/main`; inspect Actions on exact
head_sha for backend/frontend/documentation/secrets SUCCESS. Backend actual formatter step
must pass after restore and before build/test; frontend remains npm ci/test/build only.
Own future SHA/run is resolved from GitHub history, not embedded recursively. If interrupted
before final verification, preserve valid work/commit and continue that first incomplete step.
Do not merge again, recreate commits or start a checkpoint. After final exact-main CI verification
STOP and return to ChatGPT with "Codex är klar". Proposed next: BB-130D2 — Frontend Formatter
Tool Installation, requiring separate architect/owner authorization after publication review.

## BB-130D2 dependency triage accepted publication — 2026-09-15

Status: ACCEPTED / MERGED / CI VERIFIED; no interrupted implementation.
Task: approved triage merge and documentation publication only.
Original baseline: `bcf69191b92b9257627cd3c9814c02758b4ae8ae`.
Approved branch: `bb-130d/frontend-dependency-triage`.
Approved candidate: `ed57dba99d82eb47357b2b7d080baa9d72daf3d4`.
Merged unchanged as `73b7bdfe3befe8bb8505897d1d9480184d3e82ba`;
[CI 34993322513](https://github.com/T-bear/BigBrain/actions/runs/34993322513) SUCCESS for all
four jobs. Backend actual formatter step 5 passed after restore 4, before build/test 6/7.
Frontend npm ci/test/build passed; no frontend formatter/linter gate.

Preserved conclusions: dated audit five packages (3 moderate/2 high), eight distinct GHSAs;
six C, Vitest/mocker and PostCSS D. No currently reachable product security defect requiring
blocker handoff established; no findings FIXED. Affected packages absent from inspected bundle,
repository/build evidence only, not blanket security approval. Exact historical evidence in the
[triage report](../reports/features/platform/bb-130d2-frontend-dependency-triage-20260914.md).
No dependency correction, audit fix/update, package/lock/source/CI change, new tooling or formatting.

Reconciliation files: TESTING.md; docs/STATUS.md; docs/BACKLOG.md;
docs/architecture/bb-130-stabilization.md; this note; docs/reports/REPORT-CATALOG.md;
docs/reports/features/platform/bb-130d2-frontend-dependency-triage-20260914.md.
Unrelated mockups and ADR 0006–0009 preserved/excluded. Other historical reports unchanged.

Final publication resolution: separate main commit `docs: reconcile accepted BB-130D2 dependency triage`
contains this record. Resolve with
`git log -1 --format=%H --grep='^docs: reconcile accepted BB-130D2 dependency triage$' origin/main`.
Compare HEAD/origin/main with `git ls-remote origin refs/heads/main`. Inspect Actions on exact
head_sha: backend/frontend/documentation/secrets SUCCESS, including actual successful
`dotnet format BigBrain.slnx --verify-no-changes --no-restore` after restore/before build/test.
Frontend must remain npm ci/test/build only. Own future SHA/run is resolved from GitHub history,
not embedded recursively. If interrupted before final verification, preserve work/commit and
resume first incomplete publication step without merging or recreating commits again.

A/B COMPLETE; C COMPLETE / EXIT APPROVED; D IN PROGRESS. D1/backend cleanup/format gate
ACCEPTED / MERGED / CI VERIFIED; backend gate ENABLED. D2 characterization/triage accepted.
Dependency maintenance and formatter cleanup/tool installation/gate NOT STARTED / NOT COMPLETE.
Next proposed checkpoint: BB-130D2 — Minimal Frontend Dependency Maintenance, separately authorized.
Untested targets: Vitest 4.1.10 → 4.1.11; PostCSS 8.5.22 → 8.5.23;
Nanoid 3.3.16 → 3.3.18; Undici 7.28.0 → 7.29.0. Do not begin automatically.
Finance RESEARCH / 0 SEK / NONE; no deployment/runtime/device/UX/scientific change,
provider/broker/orders/PAPER/LIVE/AUTO/capital, Research Learning or Finance feature work.
Exact next action after final exact-main CI verification: return to ChatGPT with "Codex är klar"
and stop. No subsequent checkpoint starts here.
