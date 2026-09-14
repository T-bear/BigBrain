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

## Backend whitespace cleanup review handoff — 2026-09-14

Status: REVIEW CANDIDATE ONLY — implemented / locally automatically verified; no interrupted implementation.
Task: BB-130D backend whitespace cleanup only.
Accepted main/baseline: `ce31f343851b71a77f3464ef3f2938f9574edbdf`;
[baseline CI 34786279781](https://github.com/T-bear/BigBrain/actions/runs/34786279781) SUCCESS.
Branch: `bb-130d/backend-whitespace-cleanup`.
Candidate: the commit `style: normalize backend whitespace for BB-130D` containing this note.
Resolve exact remote SHA with `git ls-remote origin refs/heads/bb-130d/backend-whitespace-cleanup`
and compare with local HEAD. Main remains accepted source until explicit candidate-SHA approval/merge.

Completed and valid: pre-format exactly matches D1 (59 files / 22,573 WHITESPACE locations).
One formatter write; all 59 source files pass exact token/literal/trivia and recursive-tree
comparison. Post-format exit 0; pre/post Release builds zero warnings/errors; pre/post API
664/664 and Sentinel 32/32 pass. No meaningful non-whitespace change or product blocker found.
Initial sandbox build failure passed on approved host rerun. Roslyn raw equivalence flag on
one existing block was characterized as representation/trivia, with strict tokens/tree and
normalized/reparsed equivalence confirmed; report retains details and reproducible verifier.
Changed files: exactly 59 formatter-scoped C# files listed in the
[cleanup report](../reports/features/platform/bb-130d-backend-whitespace-cleanup-20260914.md),
plus TESTING.md, docs/STATUS.md, docs/BACKLOG.md, docs/architecture/bb-130-stabilization.md,
this note, docs/reports/REPORT-CATALOG.md and that report (66 files total).
Publication checks: documentation, unstaged/staged diff and staged/full-history Gitleaks.
Git state: only checkpoint files belong to the candidate; unrelated untracked mockups and
ADR 0006–0009 remain preserved/excluded. No uncommitted checkpoint work remains after push.
If publication fails, preserve the valid commit/working tree and resume publication, never reset.

Remaining: architect/owner review of exact candidate SHA before merge. A/B COMPLETE;
C COMPLETE / EXIT APPROVED; D IN PROGRESS, D1 ACCEPTED. Cleanup acceptance pending.
Backend formatter CI gate NOT ENABLED; activation is a separate checkpoint. D2 NOT STARTED.
Candidate CI not claimed: unchanged workflow triggers main pushes and pull requests only.
No deployment, runtime/device approval, schema/DDL semantics, provider/broker/PAPER/LIVE/AUTO,
capital or scientific behavior change. Finance RESEARCH / 0 SEK / NONE; NOT EVALUABLE valid.
Exact next action: return to ChatGPT with "Codex är klar" and remote candidate SHA, then stop.
No merge, gate activation, D2, Research Learning or new Finance work is authorized here.
