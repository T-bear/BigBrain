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

## Current checkpoint review state — 2026-09-09

Status: REVIEW CANDIDATE ONLY — NOT MERGED TO MAIN
Task: BB-130C E2 campaign SQLite persistence/replay/reload characterization.
Baseline/source-of-truth SHA: `04a7a9c1f5d4d9afb02f313a272b6c4369709f5a`.
Branch: `bb-130c/campaign-sqlite-replay-characterization`, directly from that baseline.
Review identity: the branch commit containing this note/report; verify its exact remote SHA
before review/merge. Main remains the accepted source of truth.

Completed and valid: OUTCOME A. One new isolated SQLite test reuses the BB-127 fixture,
reconstructs store/reader and connections, and compares complete campaign JSON/raw row snapshots
before reload, same-definition replay and final reload. One campaign, six unique attempts,
two research revisions plus 124 research observations stay unchanged. NOT EVALUABLE and null
BacktestRunId remain intact; missing ID returns null. No blocker reproduced or production change.

Changed files: `tests/BigBrain.Api.Tests/FinanceResearchCampaignTests.cs` and fixture visibility
only in `tests/BigBrain.Api.Tests/FinanceResearchDatasetTests.cs`; `TESTING.md`, `docs/STATUS.md`,
`docs/BACKLOG.md`, `docs/modules/finance.md`, `docs/architecture/bb-130-stabilization.md`,
`docs/reports/REPORT-CATALOG.md`, this note and
[the E2 report](../reports/features/finance/bb-130c-campaign-sqlite-replay-characterization-20260909.md).

Tests/builds: campaign 10/10, related Finance 129/129, full API 664/664, Sentinel 32/32;
Release solution build zero warnings/errors. Exact commands and publication gates are in the report.
No Web/shared API change; no unrelated Web rerun. No GitHub CI claim for this review candidate.

Git/unrelated work: only intended test/documentation files belong to this checkpoint. Existing
untracked design mockups and ADR 0006–0009 drafts are unrelated, preserved and excluded.
No incomplete implementation or interrupted run remains in this checkpoint.

Remaining: architect review and exact-SHA owner merge approval; then controlled merge/main CI
and documentation reconciliation. E1 remains accepted. E2 is not accepted; C remains NOT READY
until E2 acceptance/merge/green main CI and exit confirmation. D NOT STARTED. Existing accepted
post-BB-130 debt stays deferred. Sequential replay is characterized; concurrent creators,
corruption and power-loss behavior are unverified scenarios, not reproduced defects.

Exact next action: architect reviews the published E2 branch/report. Do not merge without
exact-SHA owner approval, begin D, deploy, or incorporate outside Alpaca clarification.
Finance RESEARCH / 0 SEK / NONE; no schema, science, production data or provider changes.
