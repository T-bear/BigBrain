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

## BB-130D2 accepted characterization publication — 2026-09-14

Status: characterization ACCEPTED / MERGED / CI VERIFIED; no interrupted implementation.
Task: approved D2 characterization merge and documentation reconciliation only.
Original baseline: `1d824f6d2a9b4e6e2fc679705bd357860042ddbb`.
Approved branch: `bb-130d/frontend-quality-characterization`.
Approved candidate: `01129fc54bdd3871b61eeda47e5cadbadc3ad81c`.
Merged unchanged as `9181f2c75b8ffc12520ce497b7067e60527f459f`;
[merge CI 34877864965](https://github.com/T-bear/BigBrain/actions/runs/34877864965) SUCCESS
for backend/frontend/documentation/secrets. Backend formatter step 5 actually passed after
restore 4 and before build/test 6/7. Frontend npm ci/test/build all passed; no lint/format gate.

Completed and valid: historical characterization of 83 TS/TSX files using pinned ephemeral
Prettier 3.9.6 flags 80/83, check-only exit 1 with repeatable output. No source formatting or
persistent formatter/linter dependency/config/script/CI gate. Historical Web 199/199 tests
and production build pass; current exact merge-main CI supplies publication regression evidence.
No new tool installation or formatter invocation in this publication.
Exact historical commands/results and current amendment are in the
[report](../reports/features/platform/bb-130d2-frontend-quality-characterization-20260914.md).

Reconciliation files: TESTING.md; docs/STATUS.md; docs/BACKLOG.md;
docs/architecture/bb-130-stabilization.md; this note; docs/reports/REPORT-CATALOG.md;
docs/reports/features/platform/bb-130d2-frontend-quality-characterization-20260914.md.
Unrelated untracked mockups and ADR 0006–0009 preserved/excluded. No source/package/CI changes.

Final publication resolution: resolve the separate main commit containing this record with
`git log -1 --format=%H --grep='^docs: reconcile accepted BB-130D2 characterization$' origin/main`.
Compare HEAD/origin/main with `git ls-remote origin refs/heads/main`. Query GitHub Actions for
that exact head_sha: backend/frontend/documentation/secrets must be SUCCESS. Verify actual
`dotnet format BigBrain.slnx --verify-no-changes --no-restore` step success after restore and
before build/test again. The commit's own future SHA/run is resolved from GitHub, not embedded
recursively. If interrupted, preserve this valid work/commit and resume the first incomplete
publication/CI step without merging or recreating commits again.

A/B COMPLETE; C COMPLETE / EXIT APPROVED; D IN PROGRESS. D1/backend cleanup ACCEPTED / MERGED /
CI VERIFIED; backend baseline CLEAN and format gate ACCEPTED / MERGED / CI VERIFIED / ENABLED
ON MAIN. D2 characterization accepted; cleanup/tool installation/gate NOT STARTED / NOT COMPLETE.
Dependency advisories TRIAGE REQUIRED; five registry findings, exploitability not demonstrated.
No audit fix or security acceptance. Any reproduced defect requires the blocker-handoff workflow.
Finance RESEARCH / 0 SEK / NONE; no deployment/runtime/device/UX/scientific behavior change,
provider/broker/orders/PAPER/LIVE/AUTO/capital, Research Learning or Finance feature work.
Exact next action after final exact-main CI: return to ChatGPT with "Codex är klar" and stop.
No cleanup, tool installation or gate activation follows from this publication.
