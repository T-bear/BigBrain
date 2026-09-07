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

## Current blocker handoff state — 2026-09-07

Status: BLOCKER HANDOFF — NOT MERGEABLE
Task: BB-130C v2 canonical source/product/content revision correction; stopped at explicit metadata sufficiency gate.
Baseline/source-of-truth SHA: 3bf3bde9c1e2321972e6f31443d740d6b92341f9; origin/main reverified unchanged.
Git status: fresh branch bb-130c/versioned-dataset-revision-identity-v2 directly from main; publication commit identifies handoff SHA. No previous blocker ancestry. Unrelated mockups/four ADR proposals preserved and excluded.
Changed files: TESTING.md; docs/STATUS.md; docs/BACKLOG.md; docs/modules/finance.md; docs/architecture/bb-130-stabilization.md; docs/reports/REPORT-CATALOG.md; docs/reports/features/finance/bb-130c-versioned-dataset-revision-identity-v2-20260907.md; docs/operations/codex-recovery.md.
Completed and valid: verified baseline/branch; inspected full candidate and owner-sidecar descriptors, constructors, promotion product/storage use, manifest persistence and workbook research dataset_id. Existing WIKI mapping explicit; no safe generic canonical product contract. Prior accepted blocker evidence reused without re-running it. No production/test code changed.
Remaining: architect decides explicit canonical product metadata, authoritative assignment, validation/missing-product behavior and immutable manifest retention without schema migration. V2 implementation/test/full gates not started. Both earlier defects unresolved; no intake extraction.
Tests/builds already run and results: no behavioral suites/build rerun for docs-only source analysis; no CI claim. Documentation/diff/secrets publication gates recorded in report.
Blockers/assumptions: CandidateId prohibited as product; source/URL/filename/free-text cannot safely infer generic product guarantees. Workbook dataset_id belongs to separate research path. No new metadata field/fallback or eligibility rule invented. Legacy WIKI compatibility remains UNKNOWN; no production-data access/mutation, migration, aliases, rekey or deployment. Finance RESEARCH / 0 SEK / NONE.
Exact next action: architect reviews exact remote SHA and authorizes the canonical product metadata contract in a separately bounded main-derived correction checkpoint. Do not merge this or previous blocker branches. Do not resume correction/extraction automatically.
