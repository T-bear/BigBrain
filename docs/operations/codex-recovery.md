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
Task: BB-130C backtest reader characterization and exit assessment.
Baseline/source-of-truth SHA: 6bfcd011a655a7d9f225bc9c023063738d10db3b
Git status: bb-130c/backtest-reader-exit-assessment, main-derived; intended tests/docs committed for review.
Exact review SHA: remote branch commit carrying this note; main remains the baseline.
Changed files: tests/BigBrain.Api.Tests/FinanceBacktestPersistenceTests.cs; TESTING.md;
docs/STATUS.md; docs/BACKLOG.md; docs/modules/finance.md; docs/architecture/bb-130-stabilization.md;
docs/reports/REPORT-CATALOG.md; this recovery note;
docs/reports/features/finance/bb-130c-backtest-reader-exit-assessment-20260908.md.
Completed and valid: reader map, two new synthetic tests, DO NOT EXTRACT decision;
proposed C exit NOT READY with E1 robustness UI identity and E2 campaign SQL replay characterization.
Explicit broader scope deferrals require owner/architect acceptance. No production changes.
Remaining: architect review/owner acceptance, separately authorized exact-SHA merge and main CI/reconciliation.
Tests/builds already run and results: focused 84/84; full API 663/663; Sentinel 32/32; Release 0 warnings/errors.
Final documentation/diff/staged-secrets evidence is in the report; no candidate CI claim.
Blockers/assumptions: no reproduced defect; no production access/deployment/schema change;
Finance RESEARCH / 0 SEK / NONE. Unrelated mockups and ADR 0006–0009 remain excluded/preserved.
Exact next action: architect reviews the exact remote SHA and report, including explicit C exit deferrals.
Owner approval is required before merging; this note records a review candidate, not accepted main.
Do not merge, deploy or start E1/E2.
