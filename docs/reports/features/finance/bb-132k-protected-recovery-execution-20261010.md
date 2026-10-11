# BB-132K Phase B — Protected recovery execution gate result

## Metadata

Detta är en sanerad GitHub-version. Owner authorization/attestation dated2026-10-10;
remaining rights inspection and final source check performed2026-10-11 UTC. The filename retains
the authorized checkpoint date. This report records an execution precondition failure, not a backup.

- Accepted main: `618f025a3cb72ee4513a9f14b713382e4a673a42`.
- Tree: `72649fe9c5dd9fb7c2bb7fe0ed0fa0dd9ac06547`; fetched and verified unchanged.
- Branch: `review/finance-bb132k-protected-recovery-execution`, directly from that main.
- Publication subject: `review: record BB-132K protected recovery rights gate`.
  Exact publication SHA/tree resolve from this commit's Git metadata, not a self-referential hash.
- Approved [consolidated plan at763ea59](https://github.com/T-bear/BigBrain/blob/763ea59ea18240ee1264095e97f23f806303baea/docs/reports/features/finance/bb-132k-consolidated-readiness-20261010.md).
- Prior [J inventory](https://github.com/T-bear/BigBrain/blob/ca05b5b1ec50fca377208dd36fc18c7ef390550f/docs/reports/features/finance/bb-132j-evidence-recovery-preflight-20261010.md)
  and [K Phase A](https://github.com/T-bear/BigBrain/blob/dcf2c5cc81d602a007173aba9a34a7789a190e51/docs/reports/features/finance/bb-132k-recovery-execution-preflight-20261010.md).
- Applicable [ADR0028](../../../adr/0028-finance-provider-tagged-backup-restore-and-cleanup.md),
  [backup runbook](../../../operations/backup-restore/README.md) and
  [controlled acquisition/build runbook](../../../operations/runbooks/finance-controlled-acquisition.md).

## Status

**REVIEW CHECKPOINT — GATE A BLOCKED / NO SERVICE INTERRUPTION / NOT MERGE CANDIDATE.**

| Gate | Actual outcome |
| --- | --- |
| 0 — source/deployment/evidence identity | PASS; native receipt commitments rechecked before publication |
| A — per-artifact copy/retention rights | BLOCKED: unresolved source copying rights and incomplete artifact-to-policy linkage |
| B — destination/capacity/pinentry/writer readiness | NOT REACHED; prior storage measurements are not claimed as current execution clearance |
| C — quiescence/configuration/encrypted capture | NOT STARTED |
| D — isolated restore/integrity/drill cleanup | NOT STARTED |

No service stop/restart, configuration change, production write, recovery archive, plaintext drill,
GnuPG invocation or passphrase handling occurred. Private metadata inventory and diagnostic code were
created outside tracked publication scope; they contain no copied raw provider payloads.
Actual API/Web interruption: **none**. No deployment/migration98 or production restore is authorized.

## Evidence — owner decisions recorded

Owner attested on2026-10-10 that EODHD Free remains active with no known termination, expiry or
deletion notice. This is **owner attestation**, not independent provider verification or blanket
copy permission. It satisfies the missing account-fact input under the recorded EODHD policy;
it does not classify unrelated intake data. No provider/account API was called to verify it.

GnuPG, owner-only local passphrase, temporary private plaintext during the controlled drill window,
no overnight plaintext, and the proposed archive retention were explicitly authorized. Archive
retention would be the earlier of seven days after verified deployment acceptance or30days from
creation, shortened by provider obligations; disposal requires a separate decision. No archive was
created, so no creation-based expiry date or successful-deployment trigger exists yet. These approved
choices are preserved and need not be requested again. Same-disk storage is not disaster recovery.

## Evidence — Gate 0

Running Compose project is `bigbrain-sprint1`. API image remains
`sha256:7dd3126240841269096b2027cbc0610be4b9f81fb097f0e0df9fd4097b184f8a`;
OCI and compiled revision both `a2373f172600c7b45ce378c56aab1d7da2b9b242`, the expected pre-H image.
The early-exit `system-build-identity` command was used without initializing the application/store.

Finance and lifecycle retain their named-volume mounts; owner-drop is a separate read-only bind.
Observation runtime and Alpaca transport remain true, cadence360/lookback3, exactly the existing
AAPL mapping ending2026-10-02. EODHD/FRED/ResearchScheduler remain false. None of these settings was
changed. Existing runtime authority is not expanded; no manual request or cycle was triggered.

At final check `2026-10-11T02:53:23.9183372+00:00`, both receipts, their complete stored snapshots,
full checksums, raw JSON commitments and historical projection equal J. Native lineage PASS.
Schema versions remain1,90,91,92,93,94,95,96,97. Earlier recovery recheck found82sessions/318events,
and all304 legacy payload references present with zero missing/outside paths; presence is not hashing.

| Preserved evidence | Value |
| --- | --- |
| October2 receipt ID | `sha256:caa655557878353e9d700ec84c0c696777fcbf52ee08a63b07f72a99e3a5d11c` |
| October2 full checksum | `sha256:13d6426177e0553e1c6ca86c856d0d814695da5030bc56d9c4dea404d8e9637d` |
| October2 stored JSON hash | `sha256:762922a19de0a65dfc311c37a7903ac0bd5885bcd2697a6ec0e179798e5c1b10` |
| October1 receipt ID | `sha256:dfc00b55e068d54947f84c0a15ccb5c4bc9a4bdb3a8fd6aaef86580413ec5677` |
| October1 full checksum | `sha256:e0d4e47397eb93c8f2b1091ff69d05656f237197d7622f1554230952c30f9260` |
| October1 stored JSON hash | `sha256:9c25dac8187bc80b58350fc61f6627f518894ebbc74d83945fb624698eef793b` |
| Watermark ticks |639267556488789067 |
| Historical projection | `sha256:5d150d96d66c3c6442868d7b251a3c0b027e43e70a7a4ca9e4ec0198d6f62db6` |

Both remain New chain roots, no predecessors, US:XNAS:AAPL / Alpaca / iex-historical-1Day-raw.
Full original acquisition/ingestion times and effective instrument/mapping snapshots were compared,
not rebuilt from current configuration. No seal, receipt migration or mapping assertion was written.
Final Docker state: API running/healthy since2026-10-04T18:07:21.757927106Z; Web running/healthy
since2026-09-04T20:05:50.729925105Z. Docker health is not a new claim that aggregate Finance health,
rights or current source-date coverage is ready.

## Evidence — Gate A private inventory

A private0700 directory holds mode0600 metadata, a per-file rights/retention inventory and exact
owner-action list. Private paths, filenames and manifest contents are not published. The authoritative
classification is the second inventory pass; the first is explicitly preliminary and preserved.
All365 retained file entries are accounted for, including DB companions, macro/intake, owner-drop,
and nested backup/restore files. Nothing was omitted/deleted to obtain a passing gate.

Bounded read-only queries identified two canonical source policies (EODHD Free and NASDAQ-WIKI),
304 payload references,10 macro candidates,9 macro revisions,4 dataset candidates and4 dataset-file
references. File metadata inventory found13 macro files,28 candidate/quarantine files,4 owner-drop
files, two complete WIKI backup manifests/artifacts, and additional nested SQLite/metadata copies.

| Source/class | Finding and copy classification |
| --- | --- |
| EODHD Free | Recorded eodhd-free-personal-v2026-08-11 plus owner active-account attestation: VERIFIED RESTRICTED WITH CONDITIONS. All copies/derived lineage remain termination/deletion-controlled; no unlimited retention claim |
| NASDAQ-WIKI | Recorded public-domain policy; two complete WIKI-only backup manifest/artifact pairs classified VERIFIED PERMITTED by metadata; byte/hash verification remains unexecuted |
| Macro | All13 retained files match exact candidate directory/artifact references for the recorded FRED/ECB/Riksbank packs.7 FRED files inherit public-domain/citation conditions;6 ECB/Riksbank files inherit recorded LOCAL_RESEARCH attribution conditions. No generic macro-rights assumption |
| GOOGLEFINANCE CSV intake | Stored external rights Unknown; prior ApprovedByOwner intake decision is preserved. No explicit protected-copy/retention permission established |
| Owner GOOGLEFINANCE workbook | Existing owner-research-eligibility-v1 permits purpose-specific private research with external rights Unknown. It is not silently reinterpreted as a recovery-copy permission |
| Zenodo/Yahoo-derived candidate | Host CC BY curation license is recorded; upstream Yahoo/yfinance provenance remains unresolved. Host license alone does not establish required market-data copying rights |
| Additional nested SQLite/metadata and candidate artifacts | Complete per-file association to original rights/derived lineage was not established after the concrete UNKNOWN gate was reached; they remain UNKNOWN, not excluded |

The private per-file classification totals are15 VERIFIED PERMITTED,310 VERIFIED RESTRICTED WITH
CONDITIONS and40 UNKNOWN. These are **metadata linkage classifications**, not completed checksum
verification or40 distinct legal blockers. UNKNOWN includes3 mixed Finance DB/companion entries,
28 candidate-associated files,4 owner-drop files and5 nested-copy entries. Some candidate policy
classes are known, including WIKI, while their exact file associations remain unfinished; do not
interpret that as revoked WIKI rights. Entire mixed-store capture remains blocked regardless.

Fourteen historical macro artifact references were inspected;13 resolve to the inventoried retained
files, one additional reference did not match a current path. That reference requires reconciliation
before claiming full recovery completeness. Its absence alone does not establish corruption or
permission to manufacture/omit a file. Private evidence identifies it precisely.

### Exact unresolved decision/evidence

The private owner-action list identifies the actual retained entries and candidate/lineage references.
For the two GOOGLEFINANCE source families, establish permission/evidence specifically covering this
bounded protected copy, drill and retention, or an explicit separately reviewed treatment of the
known external-rights uncertainty consistent with ADR0028. Do not treat the earlier intake/research
approval as that decision. For Zenodo/Yahoo-derived material, resolve upstream copying provenance;
CC BY on curation is insufficient. No evidence of an explicit provider denial was invented.

Before a resumed full capture, complete the remaining nested/candidate manifest associations and
reconcile the unmatched historical macro reference. These are finite private inventory items, not
another provider survey, download or architecture project. Owner attestation for EODHD is already
recorded and is not requested again. Do not delete source data or omit these entries to bypass Gate A.

Existing documentary bases: [GOOGLEFINANCE intake](finance-googlefinance-goog-owner-intake-20260901.md),
[workbook purpose boundary](finance-bb-127-owner-research-dataset-xlsx-20260901.md),
[Zenodo rights limitation](finance-bb-084-dataset-research-20260815.md),
[macro policy evidence](finance-bb-091-european-macro-fx-20260817.md).

## Checks actually performed

- Git fetch/exact baseline/tree and source scope checks.
- Selected Docker image/OCI/mount/configuration fields, compiled early-exit identity and final API/Web state.
- Existing J reader assembly hashes revalidated; read-only native receipt/recovery inventory rerun.
- Local isolated rights diagnostic compiled using installed SDK compiler/BCL references; compile exit0.
  This is not an application build or test result. No diagnostic was installed in the API container.
- Rights diagnostic: fixed SELECTs, read-only SQLite connection, deferred read transaction, bounded
  rows/files and bounded manifest JSON; no owner constructor, provider code or application startup.
- Disposable existing-image diagnostics used read-only source/root mounts, network none, no credentials,
  no Docker socket, dropped capabilities, no-new-privileges, bounded memory/CPU/PIDs and UID1654.
  Output was captured to private metadata rather than printing raw manifests or data.
- Initial identity comparison incorrectly expected a bare SHA, then attempted JSON; actual command
  contract is `revision=<SHA>`. Correct parsing/source inspection resolved the diagnostic errors;
  no deployed revision mismatch occurred and no mutation depended on the failed comparisons.

Archive completion/decryptability, full SQLite integrity_check/foreign_key_check, file-content hashing,
consistent-copy equivalence and isolated restore are **NOT EXECUTED**, not passing checks. No request
for a passphrase was made because Gate B was not reached. No production backup/restore utility,
write-capable projection, schema-status, PRAGMA repair or migration was invoked.

## Changes and publication verification

Tracked changes only:

- `docs/reports/features/finance/bb-132k-protected-recovery-execution-20261010.md`
- `docs/reports/REPORT-CATALOG.md`
- `docs/operations/codex-recovery.md`

Private metadata and ignored local diagnostic source/binary are excluded from Git. Unrelated mockups,
ADR0006–0009 and prior operational files remain untouched. No production/test code changed.
Publication verification:

- `node scripts/verify-documentation.mjs`: PASS,267 Markdown files/91 unique backlog IDs.
- `git diff --check` and `git diff --cached --check`: PASS.
- `gitleaks git --pre-commit --staged --redact --no-banner`: PASS, no leaks.
- Exact three-document staged scope and reused SHA256 evidence identities verified.
- Manual sanitization: no private paths/manifests, raw data, credential material or operational secrets.

No application or frontend/Sentinel suites were run for this documentation publication.

## Security

No private path, raw rights manifest/provider payload/OHLCV, credentials, key or passphrase is published.
No API/Web interruption, config/permissions change on production, GnuPG operation, backup/restore,
migration98, deployment, systemd repair, provider request/cycle, mapping adoption, Qwen/SHADOW/research
or grant mutation occurred. Existing runtime was left unchanged; this does not claim no independently
scheduled background activity occurred. Finance remains **RESEARCH / 0 SEK / NONE** with no
PAPER/LIVE/AUTO, broker, order, position or capital action.

## Remaining work

Gate A is blocked as specified; gates B–D have not been consumed/executed. There is no recovery archive
or drill to retain/clean up, and no source was deleted. Keep the private classification/action inventory
for owner review; it is not a backup and contains no raw market payloads. Services remain running.

## Resumption

STOP for independent architect review. Next owner input concerns the specifically identified rights
and lineage evidence, not renewed blanket execution/GnuPG/EODHD approval. Continue this review branch
additively only after the decision; revalidate execution-time identity and evidence before any later
interruption. Do not deploy H, migrate98 or restore production under Phase B authority.
