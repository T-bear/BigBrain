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

## Backend format CI gate accepted publication — 2026-09-14

Status: ACCEPTED / MERGED / CI VERIFIED / ENABLED ON MAIN; no interrupted implementation.
Task: BB-130D backend format CI gate merge and documentation reconciliation only.
Original baseline: `e6fca47d87bc77bce153e77c98251d3b7d5e546b`.
Approved branch: `bb-130d/backend-format-ci-gate`.
Approved candidate: `332bc90975f6479d25622635f13d4b5567f82634`.
Merged unchanged as `47542cc9815b4f95c2dbb1c73dc8538dd29d0240`;
[merge CI 34842088867](https://github.com/T-bear/BigBrain/actions/runs/34842088867) SUCCESS
for backend, frontend, documentation and secrets. Actual backend formatter step 5 passed
after restore step 4, before build/test steps 6/7.

Completed and valid: accepted-main local restore and check-only format exit 0 / CLEAN;
Release build zero warnings/errors; API 664/664 and Sentinel 32/32 passed, none skipped.
Exact commands and gate behavior are in the
[gate report](../reports/features/platform/bb-130d-backend-format-ci-gate-20260914.md).
No source edits, formatter write mode or additional workflow changes.
Reconciliation files: TESTING.md; docs/STATUS.md; docs/BACKLOG.md;
docs/architecture/bb-130-stabilization.md; this note; docs/reports/REPORT-CATALOG.md;
docs/reports/features/platform/bb-130d-backend-format-ci-gate-20260914.md.
Unrelated untracked mockups and ADR 0006–0009 remain preserved/excluded.

Publication resolution: the separate main commit `docs: reconcile accepted BB-130D backend format gate`
contains this record. Resolve with
`git log -1 --format=%H --grep='^docs: reconcile accepted BB-130D backend format gate$' origin/main`.
Compare HEAD, origin/main and `git ls-remote origin refs/heads/main`; query Actions by that
exact head_sha and verify backend/frontend/documentation/secrets SUCCESS. Inspect backend
steps again: the exact `dotnet format BigBrain.slnx --verify-no-changes --no-restore`
command must have executed successfully after restore and before build/test. This commit's
own future SHA/run is resolved from GitHub history, not recursively embedded in itself.
If interrupted before that verification, preserve this valid commit/work and resume the
first incomplete publication/CI step without recreating or merging again.

A/B COMPLETE; C COMPLETE / EXIT APPROVED; D IN PROGRESS. D1 and cleanup ACCEPTED / MERGED /
CI VERIFIED. Historical 59-file / 22,573-location debt cleaned; format baseline CLEAN;
backend gate ENABLED / ENFORCED ON MAIN. D2 NOT STARTED. Finance RESEARCH / 0 SEK / NONE;
fail-closed and NOT EVALUABLE unchanged. No deployment/runtime/device approval,
provider/broker/orders/PAPER/LIVE/AUTO/capital or scientific behavior change.
Exact next action after final exact-main CI verification: return to ChatGPT with
"Codex är klar" and stop. No D2, frontend tooling, Research Learning or Finance feature work.
