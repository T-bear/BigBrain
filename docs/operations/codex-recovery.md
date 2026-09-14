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

## Backend format CI gate review handoff — 2026-09-14

Status: IMPLEMENTED / LOCALLY VERIFIED / REVIEW CANDIDATE ONLY; no interrupted implementation.
Task: BB-130D backend format CI gate only.
Accepted main/baseline: `e6fca47d87bc77bce153e77c98251d3b7d5e546b`;
[baseline CI 34806907520](https://github.com/T-bear/BigBrain/actions/runs/34806907520) SUCCESS.
Branch: `bb-130d/backend-format-ci-gate`.
Candidate: the commit `ci: enforce backend format verification` containing this note.
Resolve exact remote SHA with `git ls-remote origin refs/heads/bb-130d/backend-format-ci-gate`
and compare with HEAD. Main remains accepted source until explicit exact-SHA approval/merge.

Completed and valid: one check-only `dotnet format BigBrain.slnx --verify-no-changes --no-restore`
step after backend restore, before unchanged Release build/test. Formatter failure fails the job;
no continue-on-error or second restore. All other workflow bytes are identical to baseline.
Pre/post local restore and format pass; pre/post Release builds zero warnings/errors;
pre/post API 664/664 and Sentinel 32/32 pass, none skipped. Known local pipe permissions used.
No source files were reformatted; no write/fix mode. Frontend, tests, packages, SDK/analyzer
policy, solution/projects, runtime and historical D1/cleanup reports remain unchanged.
Changed files: .github/workflows/ci.yml; TESTING.md; docs/STATUS.md; docs/BACKLOG.md;
docs/architecture/bb-130-stabilization.md; this note; docs/reports/REPORT-CATALOG.md;
docs/reports/features/platform/bb-130d-backend-format-ci-gate-20260914.md.
Exact commands and publication checks are in the
[gate report](../reports/features/platform/bb-130d-backend-format-ci-gate-20260914.md).
Git state: only these eight checkpoint files belong to the candidate. Unrelated untracked
mockups and ADR 0006–0009 remain preserved/excluded. After successful push, no uncommitted
checkpoint work remains. If publication fails, preserve valid work and resume publication.

Remaining: architect/owner review of exact candidate SHA before merge and exact-main CI.
The gate is NOT accepted, merged or enforced on main yet. Candidate CI not claimed: unchanged
workflow triggers main pushes and pull requests only. A/B COMPLETE; C COMPLETE / EXIT APPROVED;
D IN PROGRESS. D1 and cleanup ACCEPTED / MERGED / CI VERIFIED; formatting baseline CLEAN.
D2 NOT STARTED. Finance RESEARCH / 0 SEK / NONE; fail-closed and NOT EVALUABLE unchanged.
No deployment, runtime/device approval, provider/broker/orders/PAPER/LIVE/AUTO/capital or
scientific behavior change. No Research Learning or Finance feature work.
Exact next action: return to ChatGPT with "Codex är klar" and remote candidate SHA, then stop.
No merge, D2 or next checkpoint is authorized by this publication.
