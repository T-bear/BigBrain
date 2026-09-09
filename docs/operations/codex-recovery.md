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

## Current E1 blocker handoff

Status: **BLOCKER HANDOFF — NOT MERGEABLE**

Task: BB-130C E1 robustness UI selected-result identity characterization.
Baseline/source-of-truth SHA: `87d53241439b6fcbd97a07cd5a3986b59653eab9`.
Branch: `bb-130c/robustness-selected-result-identity`, directly from verified main.
Git status: bounded handoff consists of the files below; no production changes.
Unrelated `deisgnMockups/` and local ADR 0006–0009 proposals remain excluded and preserved.
The exact published handoff SHA is the remote branch tip containing this note, reported
in delivery; main remains the accepted source of truth. This is not an interrupted
implementation or permission to merge the blocker.

Changed files:
- `src/BigBrain.Web/src/finance/FinanceObservation.test.tsx`
- `TESTING.md`
- `docs/STATUS.md`
- `docs/BACKLOG.md`
- `docs/modules/finance.md`
- `docs/architecture/bb-130-stabilization.md`
- `docs/reports/REPORT-CATALOG.md`
- `docs/reports/features/finance/bb-130c-robustness-selected-result-identity-20260908.md`
- `docs/operations/codex-recovery.md`

Completed and valid: current request/state map; four synthetic tests; DECISION B.
B summary coexists with A diagnostic detail while pending and on reselect after reopen.
Late aborted A completion can overwrite B detail. No production correction/refactor.
Remaining: architect review of exact published blocker SHA; separately authorized
main-derived correction and acceptance before E1 can close. E2 NOT STARTED; C NOT READY;
D NOT STARTED. Preserve accepted post-BB-130 deferrals. No new Alpaca documentation included.
Tests/builds already run and results: new E1 selection 2 pass / 2 intentionally fail
(35 excluded by filter); entire FinanceObservation file 37 pass / 2 intentionally fail,
zero skipped. All 35 existing tests pass. No full Web/build/backend rerun under blocker rule.
Publication documentation/diff/secrets checks passed; see the
[E1 report](../reports/features/finance/bb-130c-robustness-selected-result-identity-20260908.md).
Blockers/assumptions: abort-ignoring synthetic promise proves missing UI stale guard, not
browser fetch behavior. Pending mixed identity needs no abort assumption. Production
incidence and wider races are unverified. Finance RESEARCH / 0 SEK / NONE; no schema,
production access, provider/scientific change or deployment.
Exact next action: architect reviews this non-mergeable branch directly on GitHub;
request separate correction authorization from verified main. Do not merge this branch,
start E2/D, deploy or change production behavior in the evidence checkpoint.
