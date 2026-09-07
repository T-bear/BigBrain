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

## Current audit review state — 2026-09-07

Status: REVIEW CANDIDATE ONLY — READ-ONLY AUDIT
Task: Canonical dataset revision identity compatibility audit; bounded evidence handoff with explicit UNKNOWN results after interruption.
Baseline/source-of-truth SHA: 1d7f1f16a8afbfce7768dedc75df7660a9648985; origin/main reverified unchanged.
Git status: branch bb-130c/dataset-revision-identity-audit created directly from origin/main. The publication commit containing this state identifies the audit SHA; no blocker commit is an ancestor. Unrelated mockups and ADR proposals preserved/excluded.
Changed files: AGENTS.md; TESTING.md; docs/STATUS.md; docs/BACKLOG.md; docs/operations/codex-recovery.md; docs/reports/REPORT-CATALOG.md; docs/reports/features/finance/bb-130c-dataset-revision-identity-audit-20260907.md.
Completed and valid: actual Finance configuration/volume verified; successful ReadOnly SQLite reads under read-only mount. Inventory 145 canonical revisions (144 EODHD, 1 WIKI), 3,722 WIKI rows; no missing-endpoint orphans in three recorded checks. One WIKI candidate reference, one feature revision, 78,162 feature values reference WIKI. Blocker synthetic evidence reused. Legacy/current field-layout difference inspected in source. Recommendation B; no correction.
Remaining: architect reviews bounded evidence and Option B compatibility strategy. Exact WIKI recomputation and downstream counts beyond features remain UNKNOWN. Owner requested publication of preserved evidence without rediscovery. No fix, merge, intake extraction or deployment authorized.
Tests/builds already run and results: main-derived FinanceDatasetIntakeTests and FinanceDataProtectionTests passed 22/22 in Release, 0 failed/skipped. See report for final repository gates. No green CI claim.
Blockers/assumptions: eager downstream probe was stopped for memory use; streaming replacement compiled (0 warnings/errors) but execution was interrupted with no result file. No completed original-artifact checksum/representation/recomputation evidence exists. Temporary exploratory probe is not a supported application tool or authority; report preserves completed results and missing steps. No production writes or application startup; Finance RESEARCH / 0 SEK / NONE.
Exact next action: architect review the exact pushed audit SHA and decide the next compatibility/correction checkpoint. If additional evidence is needed, inspect only the one original WIKI artifact/legacy algorithm; do not repeat completed inventory or feature-value scan. Do not merge the non-mergeable blocker branch.
