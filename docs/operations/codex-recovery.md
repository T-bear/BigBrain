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
Task: BB-130C versioned canonical dataset revision identity correction; stopped during required cross-candidate characterization.
Baseline/source-of-truth SHA: 3bf3bde9c1e2321972e6f31443d740d6b92341f9; origin/main reverified unchanged.
Git status: bb-130c/versioned-dataset-revision-identity is main-derived; publication commit identifies handoff SHA. Unrelated mockups and four ADR proposals preserved/excluded.
Changed files: tests/BigBrain.Api.Tests/FinanceDatasetIntakeTests.cs; TESTING.md; docs/STATUS.md; docs/BACKLOG.md; docs/reports/REPORT-CATALOG.md; docs/reports/features/finance/bb-130c-versioned-dataset-revision-identity-20260907.md; docs/operations/codex-recovery.md.
Completed and valid: baseline/pre-read/source inspection; isolated same-candidate and equivalent-candidate test. WIKI control passes. Generic same-source candidates share revision ID/checksum but append rows (2 to 4) while revisions.observation_count remains 2. No production code changed.
Remaining: architect reviews source/product/content identity boundary before separately authorizing correction. V2 exact serialization/tests/implementation and full affected gates are not complete. Do not resume extraction or merge this blocker branch.
Tests/builds already run and results: focused two-case theory compiled in Release; 1 passed, 1 failed, 0 skipped, intended failure expected 2 actual 4. Initial interrupted invocation result unavailable; sandbox rerun failed at MSBuild IPC; authorized unsandboxed run produced verified result. See report for publication gates. No green CI/full-build claim.
Blockers/assumptions: canonical hash omits product/candidate but observation PK contains product, which is CandidateId for non-WIKI. Only synthetic data inspected; production occurrence unknown. Option B remains direction, no alias/rekey/migration; existing WIKI compatibility UNKNOWN. Finance RESEARCH / 0 SEK / NONE.
Exact next action: architect review exact remote handoff SHA and decide source/product/candidate identity contract; any correction requires separate main-derived checkpoint authorization. No merge, deployment or new checkpoint automatically follows.
