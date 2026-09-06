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

## Current interrupted run — 2026-09-06

```text
Status: INTERRUPTED — SAFE TO RESUME
Task: BB-130B Loading & Request Architecture; publication/CI checkpoint pending.
Baseline/source-of-truth SHA: fddf3394591ac6c716090444276f60c0c564e873
Git status: HEAD and fetched origin/main match baseline. BB-130B code/tests/docs/evidence
  prepared for the owner-approved publication checkpoint.
  No commit, push, deployment, reset or changes to pre-existing untracked files.
Changed files:
  src/BigBrain.Web/src/App.tsx
  src/BigBrain.Web/src/App.test.tsx
  src/BigBrain.Web/src/MediaDashboard.tsx
  src/BigBrain.Web/src/MediaDashboard.test.tsx
  src/BigBrain.Web/src/api.ts
  src/BigBrain.Web/src/dashboard/HomeOverview.tsx
  src/BigBrain.Web/src/finance/FinanceObservation.tsx
  src/BigBrain.Web/src/finance/FinanceObservation.test.tsx
  src/BigBrain.Web/src/shopping-list/shoppingListApi.ts
  scripts/measure-browser-loading.mjs
  TESTING.md
  docs/STATUS.md
  docs/BACKLOG.md
  docs/architecture/dashboard-widget-framework.md
  docs/modules/finance.md
  docs/reports/REPORT-CATALOG.md
  docs/reports/features/platform/bb-130b-loading-20260906.md
  docs/reports/features/platform/bb-130b-before-20260906.json
  docs/reports/features/platform/bb-130b-after-20260906.json
  docs/reports/features/platform/bb-130b-initial-20260906.json
  docs/operations/codex-recovery.md
Completed and valid:
  Required pre-read and source verification. Baseline build matches deployed asset names.
  Sanitized before/after Home, Finance, cached Finance, Media and open-details graphs.
  View-owned global reads, Home three-slot hydration/cancellation, Finance secondary
  reads after renderable observation, lazy Media technical reads and polling guards.
  Cold peak through headers: Home 10 to 5; Finance 10 to 4; Media 14 to 8.
  Cached Finance still renders before refresh; its peak is 6 rather than 10.
  No Finance backend/scientific/safety/cache-version or CSS changes.
  Report/index/status/backlog/testing/module/architecture documentation updated locally.
Remaining:
  Owner approved BB-130B commit/push and CI reconciliation on 2026-09-06.
  Remaining delivery: coherent publication and CI reconciliation.
  Remove this temporary note after completed publication; record final SHA/CI in docs.
  No B deployment or owner/mobile UX approval. C and D have not started.
  Preserve documented latency, browser-warning, payload and nine-way detail-load debt.
Tests/builds already run and results:
  Baseline npm test -- --reporter=dot: 26 files, 174 tests passed; npm run build passed.
  Final candidate npm test -- --reporter=dot: 26 files, 180 tests passed.
  Final npm run build: TypeScript/Vite passed; CSS unchanged from baseline.
  Focused request tests initially caught test-authoring errors, corrected before full pass.
  node --check scripts/measure-browser-loading.mjs passed.
  node scripts/verify-documentation.mjs: 221 Markdown, 90 unique BB IDs passed.
  git diff --check and git diff --cached --check passed.
  Gitleaks v8.28.0: 225-commit full history and staged patch passed, no leaks.
  No local backend tests required by B frontend-only DoD; publication CI remains required.
Blockers/assumptions:
  Explicit owner approval now covers B implementation and final documentation publication.
  Earlier diagnostic Finance observation/overview exceeded the approximately 45-second
  sample; warm repeats completed. Cause unresolved; no causal speedup claim.
  Firefox script-timeout warnings were seen in output retrieved at shutdown, without
  attributed event time/cause. No complete runtime UX verification is claimed.
  Finance details remain explicitly on demand with nine initial reads; not globally bounded.
  Pre-existing untracked deisgnMockups/ and ADR 0006–0009 are unrelated and untouched.
  Isolated profile and local intermediate evidence are ignored under TestResults/bb130/;
  never publish the profile. Measurement Firefox and preview processes were stopped.
Exact next action:
  Read AGENTS/START-HERE and this note; fetch origin and verify unchanged baseline/state.
  Publication is approved. Review the staged B diff and report, then restage only
  intended final B files, run final doc/diff/staged-secrets checks, commit/push coherent B
  checkpoint(s), verify origin/main and CI, reconcile docs and clear this note after
  publication. Do not start C before that coherent B checkpoint or infer deployment approval.
```

Noten får inte innehålla hemligheter, credentials, privata adresser, råa känsliga loggar eller förbjudna identifierare/data. Giltiga working-tree-ändringar ska bevaras; ofullständigt arbete får inte committas utan uttryckligt godkännande. Ta bort ifylld avbrottsstatus när originaluppdraget är färdigt och publicerad GitHub-historik åter är fullständig source of truth.
