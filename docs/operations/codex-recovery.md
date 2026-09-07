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

## Current checkpoint state — 2026-09-07

Status: BLOCKER HANDOFF — NOT MERGEABLE
Task: BB-130C CSV parsing extraction #2 stopped on optional corporate-action column-zero evidence loss.
Baseline/source-of-truth SHA: 81c80d1c7e361086b5871de512b5ef3855a70f49.
Git status: branch bb-130c/intake-csv-parsing-boundary directly from baseline. Handoff contains only characterization and docs; unrelated mockups/four ADR proposals preserved and excluded. Main remains accepted source of truth.
Changed files: tests/BigBrain.Api.Tests/FinanceDatasetIntakeTests.cs; TESTING.md; docs/STATUS.md; docs/BACKLOG.md; docs/modules/finance.md; docs/architecture/bb-130-stabilization.md; docs/reports/REPORT-CATALOG.md; docs/reports/features/finance/bb-130c-intake-csv-parsing-boundary-20260907.md; this note. Production files unchanged.
Completed and valid: source responsibility map; two isolated cases prove ex-dividend or split_ratio in column zero is silently stored empty. Control values survive, both candidates promote, v2 revision and physical row count match. No extraction or production correction was attempted.
Remaining: architect review of reproduced defect and separate correction authorization; complete parsing characterization/extraction only afterwards. No main merge, deployment or next checkpoint.
Tests/builds already run and results: targeted reproduction two deliberate failures; final focused intake/protection/canonical-identity/research suite 66 passed, 2 deliberate failures, 0 skipped. Test project compiled in Release. Full solution build/API/Sentinel/Web reruns not claimed for this stopped characterization-only handoff. Final documentation/diff/secrets checks are recorded in the report.
Blockers/assumptions: BLOCKED — CSV CORPORATE-ACTION EVIDENCE LOSS AT COLUMN ZERO. Historical production incidence/impact UNKNOWN. Existing identity contract excludes candidate-bound action strings; no identity redesign or historical rewrite follows. Prior blockers and this branch remain NOT MERGEABLE. Finance RESEARCH / 0 SEK / NONE; no schema work or production-data access.
Exact next action: architect reviews the published handoff branch tip and report. Exact verified remote SHA is returned in the publication response and discoverable with git ls-remote origin refs/heads/bb-130c/intake-csv-parsing-boundary. Do not merge or resume extraction; any correction requires a separately authorized checkpoint from verified main.
