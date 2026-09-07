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

## Current review state — 2026-09-07

Status: REVIEW CANDIDATE ONLY — NOT MERGED TO MAIN
Task: BB-130C explicit canonical product metadata and versioned canonical revision identity v2.
Baseline/source-of-truth SHA: 3bf3bde9c1e2321972e6f31443d740d6b92341f9.
Git status: branch bb-130c/canonical-product-revision-identity-v2 created directly from verified origin/main. Publication commit identifies exact review SHA. Previous blocker commits are not ancestors. Unrelated mockups/four ADR proposals preserved and excluded.
Changed files: production CanonicalDatasetRevisionIdentityV2.cs, FinanceDatasetIntake.cs, FinanceOwnerDatasetDrop.cs, FinanceResearchDatasets.cs (manifest claim only), Modules/Finance/ExternalDatasetIntake.cs; tests CanonicalDatasetRevisionIdentityTests.cs, FinanceDatasetIntakeTests.cs, FinanceDataProtectionTests.cs, FinanceResearchDatasetTests.cs; TESTING.md, STATUS, BACKLOG, Finance module, BB-130 plan, owner-drop runbook, report catalog, implementation report and this recovery note. Exact repository paths are in the report and publication diff.
Completed and valid: explicit product claim frozen in existing manifest storage, strict ASCII/invariant source/product validation, v2 exact byte contract/full SHA-256 identity, consistent normalized observation keys, fail-closed missing/invalid product. Same/separate candidates deduplicate one v2 row set. WIKI uses established source NASDAQ-WIKI plus explicit PRICES. Legacy terminal IDs/rows/manifests remain unchanged. Owner claims cannot bypass gates. No DDL/migration, production evidence read/write, aliasing, deployment or intake extraction.
Remaining: architect reviews exact branch SHA. No merge or deployment authorized. Production legacy WIKI recomputation remains UNKNOWN; equivalent v2 can coexist with legacy without cross-version alias/dedup. Remaining BB-130C/D are separate checkpoints.
Tests/builds already run and results: focused 62/62; full API 640/640 and Sentinel 32/32, zero failed/skipped; solution Release build 0 warnings/errors. Three isolated CLI processes verify identical pinned v2 ID, two rows, unchanged persisted replay revision/observations/manifest. Compose config --quiet passed. Documentation/diff/staged-secrets final gates are recorded in report. No Web rerun (no consumed response-shape or UI changes), no GitHub CI success claim.
Blockers/assumptions: none blocking this bounded candidate. Decimal scale intentionally preserved; identity is canonical representation, not cross-provider or cross-version semantic equivalence. Earlier test failure was solely obsolete new-promotion wiki-prefix counting, updated to explicit dataset-v2 prefix; no legacy expectation relaxed. Claim manifests may exist before validation. Finance RESEARCH / 0 SEK / NONE.
Exact next action: architect review the exact remote candidate SHA and decide explicit merge approval. Do not merge any prior blocker branch; no deployment, provider activation or intake extraction follows automatically.
