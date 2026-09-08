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

## Current checkpoint review state — 2026-09-08

Status: REVIEW CANDIDATE PUSHED — NOT MERGED TO MAIN
Task: BB-130C intake CSV syntactic tokenizer boundary.
Baseline/source-of-truth SHA: 39af684e8aa8a84b40caff4525b430794838c6eb.
Git status: bb-130c/intake-csv-syntactic-parser directly from verified main. Unrelated mockups and four ADR proposals remain excluded. Exact candidate SHA is the remote branch tip and accompanying publication response.
Changed files: src/BigBrain.Api/Finance/FinanceDatasetCsvTokenizer.cs; src/BigBrain.Api/Finance/FinanceDatasetIntake.cs; tests/BigBrain.Api.Tests/FinanceDatasetIntakeTests.cs; TESTING.md; docs/STATUS.md; docs/BACKLOG.md; docs/modules/finance.md; docs/architecture/bb-130-stabilization.md; docs/reports/REPORT-CATALOG.md; docs/reports/features/finance/bb-130c-intake-csv-syntactic-parser-20260908.md; this note.
Completed and valid: nine characterization cases passed before extraction. Only physical-line field tokenization moved, at five existing call sites. Header/value interpretation, reading, validation, policy and persistence remain in the store. Same focused expectations pass after extraction. Implementation and documentation are committed/pushed for review; no main acceptance or CI claim.
Remaining: architect review and explicit owner merge approval of the exact remote candidate SHA. No next implementation or deployment is authorized.
Tests/builds already run and results: focused 80/80 before and after; full API 658/658; Sentinel 32/32; zero failures/skips. Release solution build 0 warnings/errors. Final documentation/diff/staged secrets gates recorded in the report. No Web/API contract change, so no local Web rerun.
Blockers/assumptions: none discovered. Historical blocker remains NOT MERGEABLE and outside ancestry. Historical column-zero production incidence UNKNOWN. No schema/identity, scientific, rights/provenance, production-data or deployment change. Finance RESEARCH / 0 SEK / NONE.
Exact next action: architect reviews the exact remote bb-130c/intake-csv-syntactic-parser SHA and report. Merge requires explicit owner approval, unchanged reviewed SHA and reconciled main. Do not deploy or start another checkpoint.
