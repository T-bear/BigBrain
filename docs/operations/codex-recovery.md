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

## Current review candidate — 2026-09-07

Status: INTERRUPTED — SAFE TO RESUME
Task: BB-130C backtest-result-identity branch candidate.
Baseline/source-of-truth SHA: 904694f3992400d4d58e587afd4832e0bf7f857a (origin/main verified).
Git status: branch `bb-130c/backtest-result-identity`; unrelated untracked mockups and ADR proposals preserved.
Changed files: AGENTS.md; TESTING.md; docs/STATUS.md; docs/BACKLOG.md; docs/architecture/bb-130-stabilization.md; docs/modules/finance.md; docs/reports/REPORT-CATALOG.md; docs/reports/features/finance/bb-130c-backtest-result-identity-20260907.md; src/BigBrain.Web/src/finance/FinanceObservation.tsx; src/BigBrain.Web/src/finance/FinanceObservation.test.tsx; src/BigBrain.Web/src/finance/useFinanceBacktestDetails.ts.
Completed and valid: characterized identity defect; hook enforces selected/result runId, coherent pending/error/retry, abort and stale completion protection. Focused tests 35/35, full Web 191/191 and production build passed. Documentation verification and staged secrets checks passed.
Remaining: run full Web/build/docs/diff/secrets checks, stage only intended files, commit and push review branch, verify remote SHA, then stop for review. Do not merge main or start next checkpoint.
Tests/builds already run and results: focused FinanceObservation 35/35 passed. Production code before this correction failed the intended identity regression test. Previous published full Web 190/190 and build remain historical until rerun.
Blockers/assumptions: review candidate publication is authorized; main merge requires explicit approval of exact branch SHA. Finance remains RESEARCH / 0 SEK / NONE; no backend/provider/scientific/persistence/schema/deployment changes.
Exact next action: run full Web tests and production build, documentation/diff/staged-secrets checks, then commit and push this branch only and report the remote SHA.
