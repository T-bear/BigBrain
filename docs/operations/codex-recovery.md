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

## Current checkpoint state — resumed 2026-09-08

Status: REVIEW CANDIDATE PUSHED — NOT MERGED TO MAIN
Task: BB-130C CSV optional corporate-action column-zero correction.
Baseline/source-of-truth SHA: 81c80d1c7e361086b5871de512b5ef3855a70f49.
Git status: bb-130c/csv-corporate-action-column-zero-fix directly from baseline main. No blocker ancestry. Two valid local implementation/test changes preserved at resume; unrelated mockups/four ADR proposals preserved and excluded.
Changed files: src/BigBrain.Api/Finance/FinanceDatasetIntake.cs; tests/BigBrain.Api.Tests/FinanceDatasetIntakeTests.cs; docs/reports/features/finance/bb-130c-csv-corporate-action-column-zero-fix-20260907.md; TESTING.md; docs/STATUS.md; docs/BACKLOG.md; docs/modules/finance.md; docs/architecture/bb-130-stabilization.md; docs/reports/REPORT-CATALOG.md; this note.
Completed and valid: five main-derived regression cases; pre-fix two expected failures and three passes; production correction preserves both TryGetValue presence booleans and uses them when reading optional action fields. No extraction, schema/identity change, historical rewrite, production access or deployment.
Remaining: architect review and explicit owner merge approval of the exact remote correction SHA. No main merge, deployment or next checkpoint.
Tests/builds already run and results: pre-fix 3 passed/2 expected failures; post-fix focused intake/protection/identity/research 71/71 passed; Release solution build 0 warnings/errors. These completed results were reused after verifying unchanged source/test content at resume. Remaining full API 649/649 and Sentinel 32/32 passed with zero failures/skips. Documentation verifier 229 Markdown/90 BB IDs and working-tree diff pass. Final staged checks are recorded in the report; no branch CI claim.
Blockers/assumptions: none blocking correction. Production incidence UNKNOWN; no audit or repair authorized. Evidence-only blocker 6ad73c53e2d2e87df99a8ab2840ab20d73106d5b remains NOT MERGEABLE. Finance RESEARCH / 0 SEK / NONE.
Exact next action: review the exact published correction branch SHA and report; the verified remote SHA is supplied in the publication response and available via git ls-remote origin refs/heads/bb-130c/csv-corporate-action-column-zero-fix. Merge requires explicit owner approval of that SHA. Do not deploy or resume CSV extraction.
