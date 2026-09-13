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

## D1 accepted publication reconciliation — 2026-09-14

Status: BB-130D1 ACCEPTED / MERGED / CI VERIFIED; final documentation publication must
also pass its own exact-main CI before this task is complete. No interrupted implementation.
Owner/architect explicitly approved candidate `ccd7f7906b8d460a5060711e4a5f2b99039492cf`.
Pre-merge accepted main: `df848b422022d7a257eff34d27d2572503ff807c`.
Merge/main SHA: `81baaf4f5667310e00e83cccb19da3daf9cb790b`.
[Merge CI 34786050596](https://github.com/T-bear/BigBrain/actions/runs/34786050596) SUCCESS:
backend, frontend, documentation and secrets verified for that exact merge SHA.

Final source of truth: the separate main commit `docs: reconcile accepted BB-130D1 publication`
containing this record. Resolve its full SHA using
`git log -1 --format=%H --grep='^docs: reconcile accepted BB-130D1 publication$' origin/main`.
Compare local HEAD, origin/main and `git ls-remote origin refs/heads/main`; query GitHub Actions
for that exact head_sha and verify all four jobs and the CI run conclude SUCCESS.
A commit cannot contain its own future SHA/run ID; GitHub history and exact-SHA CI reconstruct
final publication without terminal history. If final CI is pending/failed, publication is
unfinished: preserve evidence and stop for review; no fixes outside this approved scope.

Completed and valid: A COMPLETE; B COMPLETE; C COMPLETE / EXIT APPROVED;
D IN PROGRESS / D1 ACCEPTED. No source/configuration changes. Backend formatter gate
NOT ENABLED: 59 files / 22,573 characterized WHITESPACE diagnostics, not correctness failure.
Backend cleanup requires separate authorization. Frontend D2 NOT STARTED.
Original local Release/test/format evidence remains in the
[D1 report](../reports/features/platform/bb-130d1-backend-quality-gate-baseline-20260914.md).
Reconciliation files: TESTING.md; docs/STATUS.md; docs/BACKLOG.md;
docs/architecture/bb-130-stabilization.md; this note; docs/reports/REPORT-CATALOG.md;
docs/reports/features/platform/bb-130d1-backend-quality-gate-baseline-20260914.md.
Publication checks: documentation verifier, unstaged/staged diff checks and staged/full-history
Gitleaks; backend/frontend builds/tests are verified through exact-main CI.
Unrelated untracked mockups and ADR 0006–0009 remain preserved/excluded. After successful
publication, no uncommitted reconciliation work remains; retain the untouched recovery template.
Finance RESEARCH / 0 SEK / NONE. No deployment, runtime/device approval, provider/broker,
PAPER/LIVE/AUTO/capital or scientific behavior change. Existing post-BB-130 debt stays deferred.
Exact next action after final CI SUCCESS: return to ChatGPT with "Codex är klar" and stop.
No backend cleanup, format CI gate, D2, Research Learning or Finance feature work is authorized.
