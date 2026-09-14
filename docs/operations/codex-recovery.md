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

## Accepted backend cleanup publication — 2026-09-14

Status: backend whitespace cleanup ACCEPTED / MERGED / CI VERIFIED; final documentation
publication also requires its own exact-main CI SUCCESS. No interrupted implementation.
Owner/architect approved candidate `8a795b16be87c97319705e9536b90472c83920aa`.
Pre-merge main: `ce31f343851b71a77f3464ef3f2938f9574edbdf`.
Merge/main: `c090a4fbb9450d1e94a0613a2cf8eb0ef5877150`.
[Merge CI 34806580492](https://github.com/T-bear/BigBrain/actions/runs/34806580492) SUCCESS:
backend, frontend, documentation and secrets verified for this exact SHA.
Accepted-main restore and `dotnet format BigBrain.slnx --verify-no-changes --no-restore`
both exit 0; clean tracked tree afterward. No formatter write mode or new source changes.

Final source of truth is the separate main commit `docs: reconcile accepted BB-130D backend cleanup`
containing this record. Resolve its exact full SHA with
`git log -1 --format=%H --grep='^docs: reconcile accepted BB-130D backend cleanup$' origin/main`.
Compare HEAD, origin/main and `git ls-remote origin refs/heads/main`; inspect GitHub Actions
for that exact head_sha and require SUCCESS from the run and all four required jobs.
A commit cannot contain its own future SHA/run ID. GitHub history and exact-SHA CI provide
permanent final evidence without terminal transcripts. If final CI is pending/failed, publication
is unfinished: preserve evidence, stop for review, and do not start unrelated fixes.

Completed: A COMPLETE; B COMPLETE; C COMPLETE / EXIT APPROVED; D IN PROGRESS;
D1 and backend whitespace cleanup ACCEPTED / MERGED / CI VERIFIED.
Backend format baseline CLEAN: D1's historical 59 files / 22,573 WHITESPACE locations
are cleaned on accepted main. Backend formatter CI gate STILL NOT ENABLED; activation
requires the next separately authorized checkpoint. Frontend D2 NOT STARTED.
Source-review and regression evidence remains in the
[cleanup report](../reports/features/platform/bb-130d-backend-whitespace-cleanup-20260914.md);
D1 remains historical pre-cleanup evidence.
Reconciliation changes only TESTING.md, docs/STATUS.md, docs/BACKLOG.md,
docs/architecture/bb-130-stabilization.md, this note, docs/reports/REPORT-CATALOG.md
and the existing cleanup report. Documentation/diff/staged/full-history Gitleaks checks apply;
both exact-main CI runs verify backend/frontend builds and tests.
Unrelated untracked mockups and ADR 0006–0009 remain preserved/excluded. No uncommitted
reconciliation work remains after successful publication. Recovery template stays untouched.
Finance RESEARCH / 0 SEK / NONE. No deployment, runtime/device approval, provider/broker,
PAPER/LIVE/AUTO/capital work or scientific behavior change.
Exact next action after final CI SUCCESS: return to ChatGPT with "Codex är klar" and stop.
No formatter CI gate, D2, deployment, Research Learning or Finance feature work starts here.
