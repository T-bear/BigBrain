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
Task: BB-130C Finance persistence responsibility characterization.
Baseline/source-of-truth SHA: b1e84267a9909a55980e472bb6bd6054953d5588.
Git status: bb-130c/finance-persistence-characterization directly from main; unrelated mockups and four ADR proposals excluded. Exact remote candidate SHA is the branch tip and publication response.
Changed files: tests/BigBrain.Api.Tests/CanonicalDatasetRevisionIdentityTests.cs; TESTING.md; docs/STATUS.md; docs/BACKLOG.md; docs/modules/finance.md; docs/architecture/bb-130-stabilization.md; docs/reports/REPORT-CATALOG.md; docs/reports/features/finance/bb-130c-finance-persistence-characterization-20260908.md; this note.
Completed and valid: SQL/caller/initialization/provider ownership map; one isolated non-EODHD shared persistence/restart/conflict regression; recommended shared backtest writer boundary only. No production changes. Tests/docs committed and pushed for review; no main acceptance or CI claim.
Remaining: exact-SHA architect review and owner acceptance/merge approval. Recommended extraction needs a separate authorization/checkpoint and is not started.
Tests/builds already run and results: new test 1/1; focused persistence/intake/identity/research 187/187; full API 659/659; Sentinel 32/32; no failures/skips. Release solution 0 warnings/errors. Final documentation/diff/staged Gitleaks gates in report. No Web-consumed contract changed.
Blockers/assumptions: no reproduced blocker. Distributed DDL/recovery, concurrency/fault-injection and campaign SQL replay coverage gaps remain explicit. No schema/data/identity/scientific/provider change or deployment. No production data access. Finance RESEARCH / 0 SEK / NONE.
Exact next action: architect reviews remote bb-130c/finance-persistence-characterization SHA and report; obtain explicit owner acceptance/merge approval for that SHA. Do not merge, implement extraction or deploy automatically.
