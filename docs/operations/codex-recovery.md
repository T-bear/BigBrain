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

## Current unpublished checkpoint — 2026-09-06

Status: INTERRUPTED — SAFE TO RESUME
Task: BB-130C first bounded Finance observation/cache/refresh extraction; owner approved publication; final checks and CI pending.
Baseline/source-of-truth SHA: 9f43229961adbbaf45902e919a5ee94e65abc774 (HEAD/origin/main verified).
Git status: main; intended checkpoint staged after verification; no C commit/push. Unrelated untracked mockups and ADR proposals preserved.
Changed files:
- src/BigBrain.Web/src/finance/FinanceObservation.tsx
- src/BigBrain.Web/src/finance/useFinanceObservation.ts
- src/BigBrain.Web/src/finance/FinanceObservation.test.tsx
- TESTING.md
- docs/STATUS.md
- docs/BACKLOG.md
- docs/architecture/bb-130-stabilization.md
- docs/modules/finance.md
- docs/reports/REPORT-CATALOG.md
- docs/reports/features/finance/bb-130c-observation-lifecycle-20260906.md
- docs/operations/codex-recovery.md
Completed and valid: baseline characterization then identical lifecycle moved into focused hook, including selected-ID reconciliation. Secondary/detail effects, JSX and presentation helpers unchanged. RESEARCH / 0 SEK / NONE; no backend/scientific/provider/entitlement/data change.
Remaining: final fetch/diff/docs/staged-secrets check before publication; commit/push, CI verification and publication reconciliation. Clear this temporary note once resolved and published. C overall remains partial; D not started.
Tests/builds already run and results: focused FinanceObservation/cache/App/shared tests 49/49 before added tests; expanded 56/56 before and after production extraction; full Web 187/187 in 26 files; npm run build passed. Documentation verifier passed (222 Markdown, 90 unique BB IDs); diff check passed. See report for final staged scan result. Initial StrictMode fixture failed due Error realm and was corrected before extraction. Initial root npm invocation had no package.json; correct Web invocation passed. Documentation sandbox git EPERM resolved by authorized rerun.
Blockers/assumptions: owner explicitly approved this C publication and final documentation reconciliation. No deployment or next C checkpoint authorization. Existing AbortError/fetch-cancellation assumptions remain documented, not silently changed. Unrelated deisgnMockups/ and docs/adr/0006 through 0009 proposals must remain excluded.
Exact next action: publish this owner-approved checkpoint after final checks; fetch origin and compare against baseline; inspect staged/unstaged state without resetting; rerun final documentation/diff/staged-secrets checks, then publish and verify CI. Do not start another C scope automatically.
