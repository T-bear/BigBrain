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

## Current blocker handoff — 2026-09-07

Status: BLOCKER HANDOFF — NOT MERGEABLE
Blocker: BLOCKED — CULTURE-DEPENDENT REVISION IDENTITY
Task: Publish the owner-authorized test/documentation-only identity blocker; intake extraction stays stopped.
Baseline/source-of-truth SHA: 1d7f1f16a8afbfce7768dedc75df7660a9648985; origin/main verified unchanged.
Git status: handoff branch bb-130c/dataset-intake-responsibilities. The commit containing this state is the bounded handoff; exact SHA is available from GitHub branch history. No main merge is authorized. Unrelated untracked mockups and ADR proposals 0006–0009 are excluded.
Changed files: tests/BigBrain.Api.Tests/FinanceDatasetIntakeTests.cs; TESTING.md; docs/STATUS.md; docs/BACKLOG.md; docs/reports/REPORT-CATALOG.md; docs/reports/features/finance/bb-130c-dataset-intake-responsibilities-20260907.md; docs/operations/codex-recovery.md.
Completed and valid: existing failing regression retained; invariant/en-US/sv-SE/th-TH identities pinned, equal artifact bytes/checksum/count and persisted bars verified; numeric/calendar serialization and reference/compatibility analysis documented. No production edits. Owner explicitly permits publication with the reproduced failure as a non-mergeable blocker handoff.
Remaining: architect compatibility decision; any correction needs separate authorization. Intake extraction and complete A–R characterization remain unfinished. Production compatibility audit requires separate read-only evidence authorization; do not access production data to infer it.
Tests/builds already run and results: Release focused FinanceDatasetIntakeTests 21 passed, 1 failed, 0 skipped (22 total). Only IdenticalCsvPromotionKeepsRevisionIdentityAcrossProcessCultures fails at required ID equality. Four new pinned culture cases pass. Original failure has not been weakened or skipped. Documentation verification passed (226 Markdown / 90 IDs), diff checks passed and staged gitleaks found no leaks; no green implementation or CI is claimed.
Blockers/assumptions: decimal separator and DateOnly calendar both influence Promote hash input. Existing production WIKI compatibility UNKNOWN; fixture sv-SE/th-TH IDs differ from invariant. No data migration, scientific/provider/entitlement changes or deployment. Finance RESEARCH / 0 SEK / NONE.
Exact next action: architect reviews the exact published handoff SHA and chooses compatibility/correction scope. Do not merge this branch, implement any option, resume extraction or begin another checkpoint.
