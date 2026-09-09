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

## Published E2 reconciliation / C exit-review handoff — 2026-09-09

Status: E2 ACCEPTED / MERGED TO MAIN / CI VERIFIED; no interrupted implementation remains.

- Accepted candidate: `c59f0200cf73bb936fb6f83ceaf603c0e1cf9d40`.
- Original main / candidate sole parent: `04a7a9c1f5d4d9afb02f313a272b6c4369709f5a`.
- Merge main SHA: `f8f5df5f0c91688c58b2466cb276dda0dfa8c346`.
- [Merge CI 34337618297](https://github.com/T-bear/BigBrain/actions/runs/34337618297):
  completed/success; backend, frontend, documentation and secrets all succeeded.
- Reconciliation/final main SHA: the documentation commit titled
  `docs: reconcile accepted E2 and pending BB-130C exit review` containing this handoff.
  Resolve its full SHA from GitHub history (or `git log -1 --format=%H --grep='^docs: reconcile accepted E2 and pending BB-130C exit review$' origin/main`).
- Final main CI: GitHub Actions CI for that exact reconciliation SHA, event push, branch main.
  Require completed/success and all four jobs successful; do not use the earlier merge run as a substitute.
  The commit cannot embed its own future hash/run ID. This immutable commit identity plus GitHub's
  exact-SHA Actions record reconstructs final publication without terminal history. A pending/failed
  final run means reconciliation publication is not yet verified; stop rather than starting new work.

OUTCOME A: campaign creation/reconstruction/replay preserves one campaign, six unique attempts,
CampaignId/checksum/definition/lineage/scorecard/results and all dataset snapshot rows. NOT EVALUABLE
and null BacktestRunId remain intact. No blocker reproduced; no production source changed.
Local evidence: campaign 10/10, related Finance 129/129, full API 664/664, Sentinel 32/32;
Release zero warnings/errors. Exact commands and limitations remain in
[the E2 report](../reports/features/finance/bb-130c-campaign-sqlite-replay-characterization-20260909.md).
Only canonical documentation changes in reconciliation; tests and production source remain untouched.
Existing unrelated untracked mockups and ADR drafts remain preserved/excluded.

E1 and E2 accepted. No currently-known BB-130C blocking checkpoint remains.
**BB-130C IMPLEMENTATION CHECKPOINTS SATISFIED — PENDING ARCHITECT/OWNER EXIT CONFIRMATION**.
C is not formally exited/closed/completed. BB-130D NOT STARTED. Accepted post-BB-130 debt remains
explicitly deferred, not delivered. Historical blocker branches remain NOT MERGEABLE.

Exact next action after final main CI succeeds: Return to ChatGPT with "Codex är klar" for
BB-130C exit review. Do not start D, another C checkpoint or Alpaca work.
Finance RESEARCH / 0 SEK / NONE. No scientific/schema/data/provider/trading/deployment action;
no production access or runtime/device UX approval is implied.
