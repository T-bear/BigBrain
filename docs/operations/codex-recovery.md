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

## Current recovery state

No interrupted run or pending audit review is active. The approved read-only audit
`76a71d28f271b3a9c0e39b9da9b709035aa07aa0` was merged as
`eb21b7c654e5adfaa25065a2264293b8ffbfed6d` and [main CI](https://github.com/T-bear/BigBrain/actions/runs/34112859688) passed all four jobs.
The documentation reconciliation commit records this resolved publication state;
its exact SHA and CI are available in main history and GitHub Actions.

The culture-dependent identity defect remains unresolved, exact WIKI compatibility
remains UNKNOWN, and intake extraction remains stopped. Option B is the approved
architectural direction only: versioned future invariant identity preserving legacy IDs.
No historical rekeying is authorized; no alias layer is justified. No correction or
deployment occurred. Finance remains RESEARCH / 0 SEK / NONE. The next correction
checkpoint requires separate authorization; no new checkpoint has started.
