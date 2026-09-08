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

## Current checkpoint review state

Status: REVIEW CANDIDATE PUSHED — NOT MERGED TO MAIN
Task: BB-130C shared immutable PersistBacktest writer extraction.
Baseline/source-of-truth SHA: 4ec675864d76f6da11d747026d917491f085f9e7
Git status: bb-130c/backtest-persistence-writer; intended code/tests/docs committed for review.
The exact review SHA is the remote branch commit carrying this note; main remains the baseline.
Unrelated untracked mockups and ADR 0006–0009 proposals remain excluded and preserved.
Changed files: FinanceBacktestPersistence.cs (new), FinanceBacktestStore.cs, FinanceRobustnessStore.cs,
FinanceResearchDatasets.cs; FinanceBacktestPersistenceTests.cs (new), CanonicalDatasetRevisionIdentityTests.cs;
TESTING.md, docs/STATUS.md, docs/BACKLOG.md, docs/modules/finance.md,
docs/architecture/bb-130-stabilization.md, docs/reports/REPORT-CATALOG.md, this recovery note,
docs/reports/features/finance/bb-130c-backtest-persistence-writer-20260908.md.
Completed and valid: exact shared writer moved; four calls/three families updated; pre-move rollback,
replay/conflict/restart/convergence characterization passed; pinned stored JSON digest/identity preserved.
Remaining: architect review and owner acceptance of exact remote SHA, then separately authorized merge
and main CI/reconciliation. No further implementation or deployment authorized.
Tests/builds already run and results: pre-move 4/4; post-move focused 82/82; API 661/661;
Sentinel 32/32; Release 0 warnings/errors. Documentation/diff/staged-secrets checks recorded in report.
Blockers/assumptions: no reproduced blocker; no schema/production-data/runtime change. Finance
RESEARCH / 0 SEK / NONE. Other persistence/schema/intake/composition and BB-130D debt remains separate.
Exact next action: architect reviews exact origin/bb-130c/backtest-persistence-writer SHA and report;
owner approval is required before merge. Do not deploy or start another checkpoint.
