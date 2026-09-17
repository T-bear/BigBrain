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

## BB-130D2 formatter tooling recovery — 2026-09-17

Status: IMPLEMENTED / TESTED / REVIEW CANDIDATE ONLY; interrupted work completed.
Task: Frontend Formatter Tool Installation only.
Baseline/source-of-truth SHA: `d7785b4ef2fea271cb78f0019c486b60acdad210`.
Current branch: `bb-130d/frontend-formatter-tooling`; HEAD initially baseline; no task commit/remote.
Git status on resume: modified Web package.json/package-lock.json, untracked .prettierrc.json.
Unrelated untracked mockups and ADR 0006–0009 preserved/excluded. No staged work.
Completed and valid: exact Prettier 3.9.6, config, scripts; 83 scope proof/80 format differences;
repeat check output identical, exit 1 expected; source and CI unchanged. npm ci/test/build/audit
completed 2026-09-16: exit 0, 199 tests/26 files, production build pass, audit zero. No relevant
changes since validation; reused on explicit resume authorization. Old note described prior
completed publication only; no formatter interruption note existed. No conflict or remote change.
Changed files now: three tooling/package files and seven docs (STATUS/BACKLOG/TESTING/plan,
REPORT-CATALOG, this note, [tooling report](../reports/features/platform/bb-130d2-frontend-formatter-tooling-20260917.md)).
Documentation verifier exit 0 (242 Markdown files / 90 BB IDs); diff check exit 0; full-history
Gitleaks v8.28.0 exit 0 (275 commits, no leaks). Prepublication fetch: main equals baseline.
Remaining after candidate publication: owner/architect review. No expensive suite rerun
required for documentation-only remainder.
Blockers/assumptions: none; known formatting debt expected, no source fixes authorized.
Staged diff check and staged Gitleaks exit 0, no leaks; exactly ten intended files staged.
Exact next action after publication: owner/architect review of verified remote SHA; stop.
Resolve final SHA using `git log -1 --format=%H --grep='^chore: install BB-130D2 frontend formatter tooling$' origin/bb-130d/frontend-formatter-tooling`
and compare `git ls-remote origin refs/heads/bb-130d/frontend-formatter-tooling`.
No write mode, cleanup, lint, new CI, deployment or Finance change. RESEARCH / 0 SEK / NONE.
After publication STOP for exact-SHA owner/architect review; no merge or next checkpoint.

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
