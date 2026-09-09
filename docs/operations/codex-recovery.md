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

## Current E1 correction review state

Status: **E1 CORRECTION — REVIEW CANDIDATE ONLY — NOT MERGED TO MAIN**

Task: BB-130C E1 robustness selected-result identity correction.
Baseline/source-of-truth SHA: `87d53241439b6fcbd97a07cd5a3986b59653eab9`.
Branch: `bb-130c/robustness-selected-result-identity-fix`, directly from accepted main.
Git status: bounded correction, test and documentation listed below; unrelated mockups
and ADR 0006–0009 proposals preserved and excluded. Published review identity is the
remote branch tip containing this note and the exact SHA reported in delivery.
Historical blocker `508cec3baceba4452df3431e7e0eab87dce50a3f` remains NOT MERGEABLE;
its history is not included. This review state is not an interrupted implementation.

Changed files:
- `src/BigBrain.Web/src/finance/FinanceObservation.tsx`
- `src/BigBrain.Web/src/finance/FinanceObservation.test.tsx`
- `TESTING.md`
- `docs/STATUS.md`
- `docs/BACKLOG.md`
- `docs/modules/finance.md`
- `docs/architecture/bb-130-stabilization.md`
- `docs/reports/REPORT-CATALOG.md`
- `docs/reports/features/finance/bb-130c-robustness-selected-result-identity-fix-20260909.md`
- `docs/operations/codex-recovery.md`

Completed and valid: selected evaluationId/catalog checksum render association;
current-effect success/failure guard independent of abort; immediate exclusion of old detail;
matching initial evidence restoration; eight deterministic synthetic regression cases.
Remaining: architect review and explicit owner approval of exact correction SHA before merge.
E1 is NOT ACCEPTED; E2 NOT STARTED; C NOT READY; D NOT STARTED. No next checkpoint authorized.
Tests/builds already run and results: reproduced original four cases before correction,
2 pass / 2 fail (same identity failures). After correction E1 8/8, FinanceObservation 43/43,
full Web 199/199 in 26 files, zero failures/skips in full runs. Production Web build passed;
CSS plugin timing advisory only. Backend/API/Sentinel/backend Release not applicable: untouched.
Final documentation/diff/secrets checks are recorded in the
[correction report](../reports/features/finance/bb-130c-robustness-selected-result-identity-fix-20260909.md).
Blockers/assumptions: no new separate defect found. Matching is presentation association,
not payload cryptographic validation. Pending/error retains the selected summary with no detail;
reselection remains the retry route. No device-specific UX/runtime approval or production audit.
Finance RESEARCH / 0 SEK / NONE; no scientific, schema, production-data, provider, deployment,
trading or new Alpaca-evidence work. Accepted post-BB-130 debt remains deferred.
Exact next action: architect review this exact GitHub correction SHA and obtain owner merge
approval. Do not merge the blocker branch, deploy, start E2 or BB-130D automatically.
