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
Task: owner-supplied Alpaca/Investopedia documentation plus BB-130C research-detail characterization; extraction intentionally deferred for owner/architect review.
Baseline/source-of-truth SHA: 3d923c42d75eaa405ebac9baba3c156430208255; HEAD/origin/main matched after fetch.
Git status: main; intended docs/tests staged for owner-approved publication; unrelated untracked mockups and ADR 0006–0009 proposals preserved/excluded. No production edit or commit/push.
Changed files:
- AGENTS.md
- TESTING.md
- docs/STATUS.md
- docs/BACKLOG.md
- docs/architecture/bb-130-stabilization.md
- docs/architecture/finance/market-data-memory-and-provenance.md
- docs/architecture/finance/market-data-provider-selection.md
- docs/architecture/finance/master-roadmap.md
- docs/architecture/finance/provider-retention-inquiry.md
- docs/modules/finance.md
- docs/reports/REPORT-CATALOG.md
- docs/reports/features/finance/finance-bb-125-zero-cost-market-data-qualification-20260901.md
- docs/reports/features/finance/finance-bb-128a-alpaca-live-activation-readiness-20260902.md
- docs/reports/features/finance/finance-alpaca-owner-support-evidence-20260906.md
- docs/reports/features/finance/bb-130c-research-detail-characterization-20260906.md
- src/BigBrain.Web/src/finance/FinanceObservation.test.tsx
- docs/operations/codex-recovery.md
Completed and valid: six Alpaca storage/research answers recorded as owner-supplied support evidence, remaining gates separate; historical SIP distinct from live IEX. Investopedia future educational/hypothesis role documented with no new ID. Nine detail triggers mapped; three new deterministic tests. Production unchanged. Existing backtest summary/previous-curve mismatch reproduced; no correction/extraction attempted.
Remaining: final fetch/diff/docs/staged secrets checks, commit/push and CI/documentation reconciliation. Separately obtain owner/architect decision on selected-result identity/pending/error policy before further production work. Do not automatically continue C.
Tests/builds already run and results: focused FinanceObservation/cache/App/shared 59/59; full Web 190/190 in 26 files; production Web build passed. Initial test-fixture TypeScript inference error corrected, full Web/build rerun passed. Docs verifier passed 224 Markdown/90 unique BB IDs after report headings corrected. Diff checks passed; final staged scan recorded in characterization report.
Blockers/assumptions: owner has explicitly approved this checkpoint publication and CI reconciliation, plus the compact approval-block workflow rule. Supplied Alpaca summary is evidence, not independently inspected raw correspondence/formal agreement. No activation, provider data, account, credential, Investopedia integration, backend/scientific/schema/persistence change or deployment. Presentation defect requires review; no new product behavior is inferred.
Exact next action: publish the approved docs/tests/workflow checkpoint; fetch and inspect state, rerun final publication checks and publish only intended files, verify CI, reconcile docs and clear this note. Ask owner/architect to decide pending-result presentation separately before any correction/extraction.
