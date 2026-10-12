# BB-132K Phase B — Protected recovery execution gate result

## Gate D continuation — 2026-10-12

**REVIEW CHECKPOINT / ENCRYPTED RECOVERY AND ISOLATED DRILL VERIFIED / API AND WEB HEALTHY.**
Technical recovery checks passed; the protection-window deviation below remains for explicit review.
Owner explicitly authorized the reviewed stdin correction and continuation from the existing archive.
Previous publication: `b17adc55b5641f1bcb94dff95642ecae4a50f10f`; same branch/main/tree.
No recapture, new backup, production restore, migration98 or new deployment is part of this continuation.

The private owner process and controlling terminal were technically verified. New resumption records
bind the prior run, exact ciphertext, control/helper hashes and stopped-service state. Real extraction
used corrected Docker stdin forwarding, no network and only the isolated drill mount. Owner-local
extraction and subprocess/GnuPG/container cleanup passed at `2026-10-11T19:32:50.791837Z`.
The ciphertext hash remained the previously verified
`0f063a2fcfd3ce6c4b1c43b3fbf8f04a30506eefb6ec63d7db5cc1ad16696a3c`.

### Protection deviation

Execution was interrupted before independent drill validation completed. At resumption on
`2026-10-12T03:37:16Z`, the temporary plaintext drill was still present: it remained overnight,
contrary to the approved controlled-window/no-overnight boundary. This is an actual deviation,
not owner-approved by implication and not erased by a later successful integrity check. Private
access controls remained in place; no exposure is established, and no forensic claim is made.
The previously authorized verification and exact-inventory cleanup were completed on resumption.
Exact inventoried drill plaintext was removed at `2026-10-12T08:49:29.046243Z`. This does not establish
secure erasure of ext4/swap remnants. The deviation remains recorded for independent review.

### Completed verification

- `manifest-readonly.py drill restored-source-manifest.json`:361 files /14,279,312,167 bytes;
  all restored source file hashes, sizes, owners, permissions and timestamps match. Eight captured
  metadata files match independently. Directory set/owners/permissions/timestamps match; one directory's
  filesystem allocation size differs (40,960 versus53,248 bytes). Directory allocation is not archived
  file content; the comparison was corrected to exclude only directory st_size. No data mismatch.
- `InspectQuiesced.dll` on the isolated copy: exact complete receipts/raw JSON, schema97, mapping
  snapshots, watermark, historical projection, PIT membership, lineage and shadow counts match capture.
- `GateBQuiesced.dll` on the isolated copy: lifecycle clean shutdown/counts, research/acquisition,
  finite/v1 grant state and complete file metadata match capture. No owner constructor or app startup.
- `manifest-readonly.py source pre-restart-source-manifest.json`: the production source manifest is
  **byte-identical** to capture, SHA256
  `7f1222ed31081cf65cb30fd43346ddd2db86cacbfa088fa2627664bf017b3baa`.
- `DrillAllIntegrity.dll`: full integrity_check and foreign_key_check against all five isolated SQLite
  files (including nested historical copies), read-only with closed-database immutable mode: all10
  checks PASS. Full isolated verification completed `2026-10-12T04:16:25.307269Z`.
- Original API/Web images/mounts and effective disabled configuration verified. Existing two ready
  owner-drop artifacts match their already retained quarantine identities/content, including embedded
  sidecar semantics. No new intake identified; producers remain frozen during transition.

All diagnostics use the original pre-H image, read-only drill/source mounts as appropriate, no network,
no credentials and no application startup. No SQLite repair/migration or source modification.
The accepted missing rejected ECB content remains outside the restore guarantee.

### Cleanup, protection and safe restart

Before cleanup, the entire drill member set matched the private archive inventory exactly. Only that
isolated tree was mounted writable for deletion; no production volume was present. The directory was
then verified empty and disposable cleanup processes/containers verified absent. No source evidence
was deleted, repaired, reconstructed or overwritten. The unchanged encrypted artifact was finalized
from its incomplete name to the protected final name; ciphertext remains14,287,207,399bytes, mode0400
under a private0700 parent. COMPLETE and final retention records are private; original failure and
resumption records remain intact. No new backup/capture was performed.

A fresh read-only source file-set/metadata comparison before restart matched the full hash manifest;
valid full-hash evidence was reused. NTP remained synchronized. Both existing owner-drop ready
artifacts matched their prior exact quarantine identities, with embedded ZIP sidecar handling taken
from accepted code. No new owner-drop input was introduced.

Existing Compose was used with a private override pinning **original image IDs**, and only
`up -d --no-deps --no-build --pull never api`, then the equivalent `web` command after API readiness.
No image rebuild, H deployment, unrelated restart or systemd repair occurred. External settings
remained exactly the six previously approved booleans: five Finance gates false and
ResearchOperations MaintenancePaused true. Secrets, instrument mappings, mounts and unrelated
configuration were preserved. No provider/model request or automatic scientific work was triggered.

| Service | Original image | Started UTC | Healthy verified UTC |
| --- | --- | --- | --- |
| API | `sha256:7dd3126240841269096b2027cbc0610be4b9f81fb097f0e0df9fd4097b184f8a` |2026-10-12T08:50:44.545636713Z|2026-10-12T08:51:50.045850Z|
| Web | `sha256:1c10a3caf19eddaa8849eec6f5eee527073fc8a2c9ec66ba9054ff74dfa2ddcd` |2026-10-12T08:52:13.338649394Z|2026-10-12T08:52:20.338071Z|

API OCI and compiled identity both equal `a2373f172600c7b45ce378c56aab1d7da2b9b242`.
API recovery is healthy/completed, previous shutdown clean, clock synchronized and low-disk false.
The stop-to-verified-healthy intervals are approximately19h20m24s for API and19h21m7s for Web;
these are observed availability bounds, not a claim of continuous active execution or an estimated
future maintenance duration. The interrupted execution accounts for the long elapsed interval.

The initial manual HTTP status check attempted `curl`, absent from the image (exit127). No application
failure occurred: Docker health was healthy. Recovery JSON was read using the same local Bash TCP
mechanism already used by the accepted image's healthcheck; no tool installation or network expansion.

Post-start read-only checks PASS: schema97, both full receipt/raw-JSON commitments, complete mapping
snapshots, watermark, historical projection, PIT membership and native lineage unchanged. No new
acquisition/research/grant state or shadow candidate/result. Lifecycle has exactly one additional normal
running session following the recorded clean shutdown. No migration98/mapping manifest adoption.

### Final disposition and publication

Gate A rights decisions/ECB exception preserved; Gate B/C passed; Gate D now technically passed,
including complete file manifest, independent decryption, isolated full SQLite/native/lifecycle checks,
source equivalence and approved exact-inventory cleanup. API/Web restored safely with Finance
acquisition/research gates disabled. The no-overnight plaintext requirement was **not met** and is not
reported as compliant merely because integrity succeeded. Review that deviation explicitly.

Same-disk encrypted recovery is not disk-failure disaster recovery. Private retention inventory retains
the earlier deadline rule: seven days after verified deployment acceptance, maximum
`2026-11-10T13:36:45.157317Z`, shortened by applicable provider obligations. No deployment acceptance
date is asserted; separate owner disposal decision required. No automatic source/archive deletion.
The missing rejected ECB raw content remains outside the restore guarantee; all original references,
hash and historical rejection state are preserved. No attempt was made to restore that absent content.

Local operator commands executed: extraction-only `owner-protected-copy.py --resume-drill` by owner;
`manifest-readonly.py` drill/source modes; `verify-resumed-drill.py` using existing read-only assemblies;
`finish-drill-cleanup.py` with exact inventory protection; original-image scoped Compose restart;
post-restart native/state readers. Local scripts/private paths/raw records remain outside Git.
No application code or tests changed. Existing27 harmless helper tests and real Docker stdin proof
are reused unchanged; no redundant product build/full test suite. Documentation verifier, whitespace,
three-document scope and staged redacted secret scan PASS. `node scripts/verify-documentation.mjs`
passed267 Markdown files /91 unique backlog IDs; `git diff --check`, staged whitespace and
`gitleaks git --pre-commit --staged --redact --no-banner` passed with no leaks.

Publication subject: `review: complete BB-132K recovery drill and safe restart`.
Baseline/main/tree unchanged; exact checkpoint SHA/tree resolve from that commit's Git metadata.
Only this report, catalog and recovery handoff change. No merge or next checkpoint. Independent
review must distinguish successful technical recovery from the recorded protection-window deviation.
Finance remains **RESEARCH / 0 SEK / NONE**; acquisition, research, SHADOW, PAPER/LIVE/AUTO,
orders/broker/capital and mapping extension remain unauthorized/disabled.

## Current continuation — Docker stdin fixed; owner-local decryption pending — 2026-10-11

**REVIEW CHECKPOINT / TRANSPORT FIX VERIFIED / RESTORE AND RESTART STILL PENDING.**
Owner reviewed `7c49973b035b1a688178e3f512363e48be0a6715` and explicitly authorized the minimal
Docker stdin correction, harmless real transport test, resumed isolated drill from the retained
ciphertext and conditional original-image restart. No new backup is authorized or required.
This section supersedes the previous instruction to seek approval for that correction.

Exact main/tree and same execution branch verified unchanged. The local extraction command now
includes `-i`. A real disposable Docker test passed tar bytes through stdin, extracted known harmless
text into tmpfs and verified its SHA256; no production mounts, credentials or network were present.
Owned disposable container cleanup passed. `python3 .env.bb132k-execution/test-owner-protected-copy.py`
passed all27 existing tests; Python syntax checks passed. No application build or broad product tests.

The existing ignored owner helper now accepts `--resume-drill`: it requires an owner-controlled
private terminal and exclusive lock, validates original run/control/ciphertext/stopped-image bindings,
requires an empty isolated drill tree, hashes the existing archive before and after extraction, uses
fresh resumption records and preserves all original failure/capture markers. It contains no capture,
symmetric-encryption, production mount, source rewrite or service-restart path. The existing manifest
reader recognizes only a matching successful extraction/cleanup continuation when verifying the drill.
This is bounded recovery continuation, not a replacement backup pipeline.

Local helper SHA256:
- `owner-protected-copy.py`: `fe6b6f9195c572c2b11ab11dde5272c318a3f6bd9757472c67e2da2d1954d0c2`.
- `manifest-readonly.py`: `a580752a26c77364e5b288808267ffbf370fe9dced9eaa04f62ccf883cc22d5c`.

At `2026-10-11T19:23:29Z`, no owner-local resume process/armed/result record exists. The owner was
provided this command, to run from repository root in their **private local terminal**:

```sh
python3 .env.bb132k-execution/owner-protected-copy.py --resume-drill
```

Enter the archive passphrase only into that terminal's GnuPG prompt. Do not send it or terminal output
to Codex/ChatGPT. Do not invoke the helper without `--resume-drill`: the original run is preserved and
must not be repeated. Technical terminal/process identity must be verified after owner startup; a
chat acknowledgement alone does not establish the gate.

API/Web remain stopped on original pre-H images. All five external Finance gates remain false and
MaintenancePaused true; effective Compose parsing verifies those settings. No credentials, source,
production data or external configuration changed in this continuation. No new decryption/capture,
restore, service restart, provider/model/scientific operation was performed. Existing protected archive
and accepted missing-ECB limitation/retention deadlines remain unchanged. Full isolated integrity,
native lineage/projection and safe-restart checks remain unexecuted, not passing.

Next exact step: owner starts the prepared extraction-only command locally; continue the already
approved Gate D file/native/SQLite checks and permitted cleanup, then restart only after all recorded
restart gates pass. No additional scope approval is needed. Do not route pinentry through agent tools
or improvise a passphrase workaround. This is an owner-interaction block, not a new architecture gate.
Publication changes only report/catalog/recovery; local helpers remain ignored, unrelated work preserved.
Resolve exact SHA/tree from publication subject `review: verify BB-132K Docker stdin correction and resume gate`.

## Current outcome — encrypted capture verified; isolated restore failed — 2026-10-11

**REVIEW CHECKPOINT / CAPTURE VERIFIED / RESTORE DRILL FAILED / API AND WEB STOPPED.**
Definition of Done is not met. The owner accepted the focused safety review and started the
updated private execution process. Gates B–C were performed under coordinationVersion3;
Gate D stopped on a concrete extraction failure. No repair, retry or production restart followed.
This section supersedes earlier current-state instructions; subsequent sections preserve history.

Publication subject: `review: record BB-132K verified capture and isolated restore failure`.
Previous checkpoint: `6f4a94d4e9819438ca2099e60b41cb0740839f65`.
Accepted main/tree remain `618f025a3cb72ee4513a9f14b713382e4a673a42` /
`72649fe9c5dd9fb7c2bb7fe0ed0fa0dd9ac06547`. Exact review SHA/tree are Git metadata of this publication.
Only this report, report catalog and recovery handoff are published; private inventories/helpers,
archive, credentials and unrelated local work remain outside Git.

### Actual gates and interruption

| Gate | Execution evidence / result |
| --- | --- |
| 0 / A | Exact Git/deployed identities and prior rights inventory v4 revalidated. Bounded owner risk decisions and missing rejected ECB exception preserved; no blanket provider-rights guarantee |
| B | PASS: current owner process/private controlling terminal, protocol3 run/control/helper bindings, complete privileged host-handle scan, storage/permissions, clock, schema97 and native evidence checks |
| C | PASS: graceful API/Web-only stop, clean lifecycle shutdown, zero remaining source holders, disabled external gates, complete source manifest, encrypted streaming capture and independent decryption/file checks |
| D | FAILED during isolated extraction. No files extracted; full restored SQLite/native checks NOT EXECUTED. Owned subprocess/container cleanup verified; no restart |

Pre-stop source contained365 files /14,279,435,495 bytes. Free space was456,112,529,408 bytes,
above3x scope plus20GiB reserve. Source roots for Finance, lifecycle and owner-drop were physically
separate and mounted flat for capture; the nested production owner-drop mount was not double-counted.
The complete pre-stop host scan found only API source handles. After stop:311 processes inspected,
zero denied, zero source holders. No active research or unfinished recorded acquisition was found.

Web stopped at `2026-10-11T13:31:13.753222598Z`; API at `2026-10-11T13:31:26.200575378Z`.
Both exited0 without OOM. Lifecycle clean shutdown is recorded at `2026-10-11T13:31:22.9576937Z`.
Normal SQLite shutdown removed four WAL/SHM companions; quiesced scope is361 files /
14,279,312,167 bytes. No retained external evidence disappeared. No manual production data write,
checkpoint, repair or migration was performed.

The existing ignored mode0600 external configuration was atomically updated: EODHD, FRED,
ResearchScheduler, ObservationRuntime and AlpacaDailyObservation all false; ResearchOperations
MaintenancePaused true. Unrelated entries were preserved exactly. Effective Compose configuration
was checked privately. These are prepared startup settings; stopped containers retain their former
frozen environment. No credentials were changed or disclosed.

### Captured evidence and protection

The deployed pre-H revision remains `a2373f172600c7b45ce378c56aab1d7da2b9b242`.
API image: `sha256:7dd3126240841269096b2027cbc0610be4b9f81fb097f0e0df9fd4097b184f8a`.
Web image: `sha256:1c10a3caf19eddaa8849eec6f5eee527073fc8a2c9ec66ba9054ff74dfa2ddcd`.
No image was built or replaced. The reviewed local owner helper was unchanged:
`031f388610c5df7eb5b7907b7b2e04f414f1685ac675d0d0c55436addc3ae30e` (SHA256).

The private manifest commits all361 quiesced source files and metadata.304 legacy EODHD payload
commitments,45 retained macro/intake/owner-drop associations and three nested database commitments
were checked against existing lineage. Eight private metadata files accompany the source capture.
The missing rejected `ecb-candidate-cec0507f8bd6b80e` raw content remains excluded from any restore
guarantee; its original reference, hash and REJECTED/FAIL history are preserved, not reconstructed.

Encrypted capture completed `2026-10-11T13:45:04.777089Z`, tar exit0 / GnuPG exit0.
Ciphertext size: **14,287,207,399 bytes**.
Ciphertext SHA256: `0f063a2fcfd3ce6c4b1c43b3fbf8f04a30506eefb6ec63d7db5cc1ad16696a3c`.
Independent owner-local decryption completed `2026-10-11T13:48:17.433327Z`:
**369/369 file members matched** expected names, sizes and SHA256; recorded source UID/GID/mode
matched. Tar path/type/duplicate-member checks passed. Passphrases were handled exclusively by
owner-local GnuPG pinentry, never agent terminal, arguments, environment, logs or Git.

A fresh post-failure ciphertext hash matched. The private encrypted artifact is retained read-only
(mode0400), still explicitly incomplete for end-to-end recovery: no COMPLETE marker and no claim
of a successful restore. Same-disk protection does not provide disk-failure disaster recovery.
Private retention inventory conservatively starts at capture authorization and sets maximum disposal
review deadline `2026-11-10T13:36:45.157317Z`, earlier if deployment acceptance+7days or provider
obligations apply. No deployment acceptance date exists. Disposal requires a separate owner decision;
no automatic archive/source deletion. Private inventory/manifests remain access-restricted.

### Concrete Gate D failure and containment

At `2026-10-11T13:49:39.937588Z`, the helper recorded phase `isolated-extraction`, RuntimeError,
cleanup complete, production restart not authorized. Container tar reported:
`This does not look like a tar archive` and exited unsuccessfully.

Static inspection identifies a concrete transport omission: `Children.docker()` constructs
`docker run` without `--interactive` / `-i`, while extraction pipes decrypted bytes to Docker CLI
stdin. Docker requires that option to keep container stdin open. This explains the discrepancy
between independently verified archive bytes and failed container tar input; a corrected runtime
has NOT been executed, so successful extraction is not yet demonstrated. The prior27 harmless
mocked regression tests did not exercise actual Docker stdin forwarding. No helper correction was
made after this failure and no new capture/decryption/extraction was attempted.

Owned process groups, disposable containers and private GnuPG agent cleanup completed with no
reported error. Independent Docker inventory found no surviving run-owned or read-only diagnostic
container. Owner coordinator exited. The isolated drill directory contains **zero entries**:
no restored source plaintext remains and no plaintext cleanup/secure-erasure claim is needed.

A final optional read-only native recheck timed out after90seconds without a result. Its disposable
diagnostic was subsequently confirmed absent. This is **unverified**, not another successful native
check or proof of corruption. The successful pre-capture/quiesced native checks and independently
verified captured file hashes are the retained evidence. No further diagnostic retry was made.

At `2026-10-11T19:06:28Z`, API and Web were still stopped, exit0, on their exact original images;
interruption remains ongoing, so no completed downtime duration is claimed. External disabled
settings and mode0600 were rechecked. No unrelated service was stopped/restarted or systemd repaired.

### Scientific commitments and verification limits

Before capture, pure read-only native verification confirmed both complete receipts, JSON checksums,
lineage and historical projection unchanged:

| Source date | Receipt ID | Full receipt checksum | Raw stored JSON SHA256 |
| --- | --- | --- | --- |
|2026-10-02|`sha256:caa655557878353e9d700ec84c0c696777fcbf52ee08a63b07f72a99e3a5d11c`|`sha256:13d6426177e0553e1c6ca86c856d0d814695da5030bc56d9c4dea404d8e9637d`|`sha256:762922a19de0a65dfc311c37a7903ac0bd5885bcd2697a6ec0e179798e5c1b10`|
|2026-10-01|`sha256:dfc00b55e068d54947f84c0a15ccb5c4bc9a4bdb3a8fd6aaef86580413ec5677`|`sha256:e0d4e47397eb93c8f2b1091ff69d05656f237197d7622f1554230952c30f9260`|`sha256:9c25dac8187bc80b58350fc61f6627f518894ebbc74d83945fb624698eef793b`|

Watermark remains `639267556488789067`; historical projection remains
`sha256:5d150d96d66c3c6442868d7b251a3c0b027e43e70a7a4ca9e4ec0198d6f62db6`.
Schema97 remains; no migration98/mapping tables/adoption, no grant renewal or new scientific result.
Original acquisition/knowledge times and complete AAPL mapping snapshots remain unchanged as recorded
in J/K. Full isolated `integrity_check`, `foreign_key_check`, lifecycle/science/grant comparison and
restored native receipt/projection checks are **NOT RUN** because extraction failed. Archive file-hash
verification is not a substitute for those checks. No restore to production occurred.

Publication verification: `node scripts/verify-documentation.mjs` PASS (267 Markdown files,
91 unique backlog IDs); `git diff --check` and `git diff --cached --check` PASS; exact three-document
scope/private-path review PASS; staged `gitleaks git --pre-commit --staged --redact --no-banner`
PASS, no leaks. No new application build/full test suite: this publication changes only documentation
and does not establish product implementation correctness anew.

### Next bounded decision

STOP for independent architect review of this failure. Proposed next action is only a minimal local
stdin-forwarding correction with a harmless real Docker transport regression, then resume isolated
owner-local decryption/drill from the existing verified ciphertext. Preserve all current run/cleanup
records; never reuse consumed markers or recapture/reset source evidence to hide this failed attempt.
Full isolated integrity/native checks must pass before the already authorized safe old-image restart
can be considered. No production restore, H deployment/migration98, mapping extension, provider call,
Qwen/research/SHADOW/PAPER/LIVE/trading is authorized by this report. Finance remains
**RESEARCH / 0 SEK / NONE**.

## Prior review outcome — resumption 2026-10-11

**REVIEW CHECKPOINT — RIGHTS CLASSIFIED UNDER BOUNDED OWNER DECISION; SOURCE-COMPLETENESS
STOP BEFORE INTERRUPTION. NOT MERGE CANDIDATE.**

The original attempt below remains historical evidence. The [resumption result](#resumption-result--2026-10-11)
records the new owner decision and the exact remaining blocker. No backup or restore has occurred.

## Metadata

Detta är en sanerad GitHub-version. Owner authorization/attestation dated2026-10-10;
remaining rights inspection and final source check performed2026-10-11 UTC. The filename retains
the authorized checkpoint date. The original gate attempts below are historical; the current outcome above records the actual capture and failed drill.

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

## Status — original attempt, superseded by the resumption below

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

## Resumption result — 2026-10-11

This continuation preserves published parent `1b3244e089e1463118169fe99e01a0d3ec508f91`
(tree `ab52492fc7aee5855c99793ccae8954183f6cce1`) on the same execution review branch.
Accepted main/tree remain the exact identities above. Resolve this additive publication by subject
`review: reconcile BB-132K recovery lineage and record completeness stop` and its Git commit/tree metadata.
No prior checkpoint was rewritten and no new planning sprint/report was created.

### Owner decision and private inventory

The owner explicitly accepts bounded private research handling, protected recovery copying,
bounded retention and an isolated restore drill for existing GOOGLEFINANCE CSV/workbooks,
Zenodo/Yahoo-derived datasets and legacy candidate/database copies. Recorded on2026-10-11 as an
**OWNER-ACCEPTED DATA-USE/RISK DECISION**, not a provider contractual guarantee, legal finding,
redistribution permission, scientific promotion or override of ADR0028/explicit prohibitions.
External rights uncertainty remains visible in the inventory; stored candidate policies were not changed.

Existing EODHD active-account attestation and its all-copies expiry/deletion conditions remain binding.
The recorded WIKI, FRED, ECB, Riksbank and Alpaca policy distinctions are preserved. Bounded inspection
of existing source policies, candidate metadata, nested manifests and accepted source reports found
no recorded explicit prohibition of this narrowly authorized private copy/drill. This is a statement
about the inspected project evidence, not a new independent/current provider-terms verification.
A subsequently applicable explicit restriction must still stop affected use.

The private inventory v3 supersedes, but preserves, v1/v2. All365 retained file entries now have a
source/policy association:16 VERIFIED PERMITTED and349 VERIFIED RESTRICTED WITH CONDITIONS.
For the owner-risk families, the latter means the explicit bounded owner authorization is verified;
it **does not mean previously unknown third-party copying rights have become verified**. Mixed stores,
nested copies and derivatives inherit the relevant source restrictions. No source item was excluded.
No archive creation date, expiry date or deployment-acceptance trigger was fabricated.

- Original WIKI and Zenodo package hashes match their recorded candidate commitments.
- Both GOOGLEFINANCE source ZIPs match their candidate commitments and their owner-drop duplicates;
  the ready markers are associated with those same packages.
- Candidate extraction trees, the generated Zenodo combined CSV and the workbook are associated
  with their exact candidate/source through the accepted intake layout and implementation. This is
  a rights/lineage association, not a new qualification or full semantic revalidation of their contents.
- 45 retained macro/intake/owner-drop files were hashed read-only.13 retained macro files have exact
  source-candidate associations. The additional historical reference is addressed separately below.
- Three nested SQLite copies contain EODHD/WIKI and the recorded WIKI/Zenodo candidates; one also
  contains the earlier FRED candidates. Their inherited restrictions are recorded per file.
  The BB-091 snapshot hash matches its published original; the BB-090 source snapshot hash matches
  its retained checksum file. The drill-copy hash is recorded separately, not claimed identical to
  its pre-migration source. Nested metadata companions are associated with those snapshots.
- These historical DB files have no WAL/SHM/journal companions in the inspected scope and retain
  August modification times. Normal read-only opens returned SQLite14 for a standalone legacy
  copy. A bounded immutable read of these historical files, with size/mtime stability checks and
  file hashes, completed metadata classification. The live Finance DB was **not** opened immutable;
  it used a read-only connection/deferred transaction. No journal recovery, integrity drill, migration,
  copied DB or production file modification was performed.

### Exact unmatched-reference finding

The remaining reference is ECB candidate `ecb-candidate-cec0507f8bd6b80e`, with retained artifact
commitment `sha256:cec0507f8bd6b80e1ce4a8f03959cc7826168233cb6c9fd05377e4785d60977f`.
It is recorded **REJECTED / validation FAIL**, with no canonical revision ID. None of the nine
canonical macro revisions references that artifact hash.

Its expected retained raw file is absent from the inventoried capture roots. A retained file with
that basename belongs to promoted candidate `ecb-candidate-88a79391bac811be` and hashes to
`sha256:88a79391bac811be2cfdc2c501f0f8c2a39e5b3f9bb5198464bc5840bdf1efb3`.
It is different evidence and cannot satisfy the rejected artifact commitment. No matching content
was found in the45 hashed macro/intake/owner-drop files. The private inventory retains the exact
reference and path association; those private paths are not published.

Accepted `FinanceMacroMemory.StageEuropean` writes the candidate artifact before recording rejection.
The BB-091 report describes retained fail-closed attempts. Neither proves where this particular
raw artifact is now. The inspected schema/code provides no matching macro deletion disposition;
the dataset quarantine cleanup state machine does not establish deletion of this macro candidate.
ADR0028 permits appropriately controlled rejected-payload cleanup, but permission is not evidence
that such cleanup actually occurred. Cause/time of absence remain **UNVERIFIED**. No global claim
that the file was deleted or cannot exist outside the approved scope is made.

This is not an AAPL receipt mismatch, a promoted macro revision failure or a renewed copying-rights
question. It is a concrete recovery-completeness discrepancy. The approved consolidated Gate B says
missing artifacts must stop execution. Accordingly, no complete recovery claim, replacement payload,
refetch, deletion, omission or silent acceptance of the absence was introduced.

### Actual gates and service state

| Gate | Resumed outcome |
| --- | --- |
|0 — source/deployment/evidence | PASS: exact main/tree, unchanged pre-H image/revision and native AAPL commitments rechecked |
|A — bounded rights/per-file source classification | Completed under the explicit owner risk decision and inherited restrictions; no new provider guarantee |
|B — execution readiness | BLOCKED by the precisely identified missing rejected ECB artifact; capacity/writer/pinentry clearance for interruption was not issued |
|C — graceful quiescence/configuration/capture | NOT STARTED |
|D — isolated restore/integrity/cleanup | NOT STARTED |

API and Web remain running/healthy with their original start timestamps. ObservationRuntime and
Alpaca transport remain true; EODHD/FRED/ResearchScheduler remain false. No setting, source data,
service, permission, mapping or credential was changed. Existing scheduled authority was left
untouched; no manual provider request, one-shot or cycle was triggered by this task.

The final pure-reader recheck at `2026-10-11T03:21:49.3876465+00:00` confirms both receipts, complete instrument/mapping snapshots, full
checksums, raw JSON commitments, lineage, watermark and historical projection identical to J.
Schema remains1,90–97; no98. This is read-only live evidence verification, not successful capture
or restore evidence. No API/Web interruption occurred. No GnuPG invocation/passphrase entry,
archive, plaintext drill, cleanup or production restore occurred. The archive/drill checks remain
**NOT EXECUTED**. There is nothing newly copied to retain or dispose of.

### Verification and next exact action

Read-only diagnostics ran in disposable exact-old-image containers with no network, credentials,
Docker socket or application startup; source/root mounts read-only, dropped capabilities, UID1654,
bounded CPU/memory/PIDs. An isolated local metadata helper compiled successfully. Two diagnostic
attempts failed on the historical SQLite open (generic SQLite exception, then confirmed code14);
the constrained historical immutable read succeeded. Failed attempts are not passing restore checks.

Publication verification: `node scripts/verify-documentation.mjs` PASS (267 Markdown files,
91 unique backlog IDs); `git diff --check` and `git diff --cached --check` PASS; exact three-document
scope and private-path/secret-pattern checks PASS; staged
`gitleaks git --pre-commit --staged --redact --no-banner` PASS, no leaks.
No application, Finance, frontend or Sentinel suite was run: only sanitized documentation is changed,
and a test suite cannot prove a production recovery artifact exists.

Next: resolve the exact rejected ECB reference by locating/authenticating an already retained original,
or obtain an explicit architect/owner disposition of this pre-existing absence as a known recovery
baseline limitation. Do not claim the absent bytes were preserved/restored and do not refetch or alter
canonical evidence. The bounded rights decision is settled and need not be requested again.
After that disposition, continue the same Phase B with fresh execution-time checks and owner-only
local GnuPG pinentry. No passphrase step is requested while this earlier gate remains blocked.
Services must not be interrupted merely to prepare encryption. No H deployment/migration98,
production restore, mapping adoption or provider reactivation follows from this publication.
Finance remains **RESEARCH / 0 SEK / NONE**. STOP for independent architect review.

## Owner-accepted limitation and local pinentry gate

Owner's subsequent instruction explicitly accepts the pre-existing absence of
`ecb-candidate-cec0507f8bd6b80e`. Preserve its REJECTED/FAIL status, null canonical revision and
`sha256:cec0507f8bd6b80e1ce4a8f03959cc7826168233cb6c9fd05377e4785d60977f` commitment.
**The missing raw content is outside the restoration guarantee.** This acceptance does not reconstruct,
delete, relabel or amend any historical row. The private v4 inventory preserves v1–v3 and records this
exact exception; the previously settled source-risk/retention decisions remain applicable.

The owner also authorizes safe return of API/Web after the copy/drill. This supersedes the earlier
instruction to leave them stopped pending review. Restart remains conditional on clean verification,
the exact existing pre-H image, schema97 and disabled acquisition/research gates; it is not permission
to deploy H, migrate98, reactivate providers or restore production. Since no stop occurred in this
continuation, API/Web already remain in their existing running state.

### Execution preflight actually completed

- Fetched main remains `618f025a3cb72ee4513a9f14b713382e4a673a42`, tree
  `72649fe9c5dd9fb7c2bb7fe0ed0fa0dd9ac06547`. Same review branch, parent
  `b6c9c61c4c9b6716e10e4df66933a3294a0eddd8`; unrelated work preserved.
- Exact existing API image/OCI/compiled revision match the pre-H identity above. API/Web Docker
  health and original start timestamps unchanged. Only API consumes the Finance/lifecycle volumes.
- Private external configuration remains a regular ignored mode0600 file. Actual Compose binds all
  five enable flags and ResearchOperations MaintenancePaused; no configuration was changed.
  ObservationRuntime/Alpaca remain true, legacy EODHD/FRED/Scheduler false, MaintenancePaused false.
- Selected existing private parent is owned, mode0700 with no symlink traversal; recovery destination
  is still absent. Available space454733086720bytes, available inodes181927405. Last inventoried scope
  is14279435495bytes; its3x plus20GiB requirement is64313142965bytes. This is a capacity precheck,
  not a substitute for measuring the final quiesced scope and any hidden mount content.
- Host GNU tar1.35 and GnuPG2.4.7 advertise AES256; pinentry and pinentry-curses are available.
  A disposable networkless/read-only exact-old-image check confirms GNU tar1.35 is also available
  for scoped source reads. No host privilege or production permission was changed.
- At `2026-10-11T07:55:08.4913724+00:00`, the existing isolated pure reader again verifies both AAPL
  receipts, complete snapshots, raw JSON hashes, checksums, lineage, watermark and historical
  projection identical to J. Schema remains1,90–97. No production owner constructor or write-capable
  replay/migration API was invoked.

### Actual stop and safe local next step

Gate A and the specifically identified historical-absence decision are settled. Gate B is incomplete:
this tool session has no owner-exclusive interactive terminal, and availability of the owner's private
terminal has not been confirmed. Installed pinentry alone does not demonstrate safe owner participation.
The approved plan explicitly requires that feasibility **before** service interruption.
No archive, encryption/decryption, passphrase handling, drill, configuration edit or service stop/restart
has occurred. Gates C/D and the Definition of Done remain **NOT COMPLETED**.

The owner can open a separate private terminal on the BigBrain server, disable shell tracing, verify
it is interactive and set its GnuPG terminal locally:

```bash
set +x
test -t 0 && test -t 1
export GPG_TTY="$(tty)"
command -v gpg pinentry-curses
```

If the terminal test fails, do not proceed. Leave the private terminal open and confirm only
“Privat terminal redo”; never send its contents or a passphrase to Codex/ChatGPT. No passphrase
is entered yet and these commands do not stop services or copy data. At the resumed coordinated
capture/drill, GnuPG asks the owner locally; no passphrase argument, environment variable, chat,
agent terminal or captured stdin is allowed. Do not run an unconnected decrypt command that could
print plaintext. No new general execution approval is requested.

Final writer/client/intake freeze, quiesced scope/handle verification and all capture/drill checks
must still run at execution time. No deployment, provider call/cycle trigger, mapping adoption,
Qwen/SHADOW/research/grant/trading action occurred. Finance **RESEARCH / 0 SEK / NONE**.

Publication uses the same three documentation files and resolves by subject
`review: record BB-132K accepted limitation and owner-local encryption gate`.
Publication checks: documentation verifier PASS (267 Markdown files, 91 backlog IDs),
`git diff --check` / staged whitespace PASS, exact three-document scope PASS, staged Gitleaks PASS
(no leaks), manual private-path/credential sanitization PASS. Application suites cannot validate
owner-local passphrase availability and were not rerun. No unexecuted backup/drill check is called PASS.

## Technical pinentry verification prepared — owner-ready continuation

The owner has now confirmed a private terminal is ready. That confirmation settles availability
but does not replace a successful local pinentry test. Same accepted main/tree and review branch;
parent `9a08dd81663a26eb76b66c5e02423dde3bff3702`. No new planning sprint or rights decision.

A local ignored operator check is prepared at `.env.bb132k-execution/owner-gnupg-check.sh`
(SHA256 `02df1a04230d9336ad889974a6e76cf529c3b73694c4bb2046c2a274868ed7b6`).
The owner runs it in the separate private terminal from the repository root:

```bash
bash .env.bb132k-execution/owner-gnupg-check.sh
```

It requires interactive stdin/stdout/stderr, uses a fresh private mode0700 GnuPG home and curses
pinentry, disables external/symmetric caching, encrypts only a known non-sensitive test string,
stops that private GnuPG agent, then independently decrypts and compares the string. The passphrase
is entered solely through owner-local pinentry, never a shell variable, argument, pipe from Codex
or agent terminal. It writes only sanitized success metadata after both operations pass. The
prepared script was syntax-checked; **it has not yet been run by the owner**, and no success file
exists. No source data or production service is involved in this test. Do not repeat it blindly if
an incomplete private test directory exists; inspect the non-secret failure state first.

Remaining read-only Gate B work completed: Finance/lifecycle access succeeds; all365 files are
readable and exactly match the previously inventoried paths, total14279435495bytes. Research runs
are empty,44 scheduling opportunities are Skipped,306 recorded acquisitions are completed
(304success/2historical failure), no finite session exists, and learning v1 remains Uninitialized.
Schema remains1,90–97. Lifecycle has82sessions/318events; current-session clean=0 is expected while
API is running and is not a successful-shutdown claim. NTP synchronized. Available space454723080192
bytes exceeds64313142965bytes required at this scope. No configured user crontab or named
Finance/BigBrain system timer was found; that limited inventory is not an all-writers-stopped claim.
Failed bigbrain.service has Restart=no and was left untouched. Both API/Web images, health and
start identities remain unchanged; restart counts are zero.

Gate B remains incomplete only as a pre-interruption clearance, not a failed backup: owner-local
crypto proof and final coordinated client/intake freeze are still required, then quiesced handle/
source checks. API/Web have not been stopped, configuration is unchanged, and no capture/drill,
provider request, migration98 or production write occurred. The accepted missing ECB content is
still explicitly excluded from the recovery guarantee. Owner has been given the exact local test
command; no new execution approval is requested. Continue on this same branch after technical proof,
not merely another chat assertion. Publication subject:
`review: prepare BB-132K owner-local pinentry verification`.

This continuation's verification: isolated Gate B diagnostic compilation/read-only execution PASS;
`bash -n` operator-check syntax PASS (not execution); documentation verifier PASS (267 Markdown
files/91 backlog IDs), whitespace/scope/sanitization PASS, staged Gitleaks PASS/no leaks. Only the
same three documentation files are published; local helpers/private metadata stay excluded. No
application test suite, GnuPG encryption/decryption or backup/restore integrity check was executed.

## Verified local crypto and execution coordination — 2026-10-11

Same accepted main/tree and same review branch, parent `16b8555c6d212db0781fa2aad6a756bef4bcb912`.
The earlier instruction to run the known-text GnuPG probe is now fulfilled and must not be repeated.
Private regular mode0600 `verified.json` records `2026-10-11T08:08:15.498211+00:00`:
encryption exit0, separate decryption exit0, exact known-text comparison, interactive owner terminal,
and no production data. The metadata was independently read; no passphrase, key material or owner
terminal contents were inspected. This proves the local crypto test, not an encrypted Finance backup.

### Actual Gate B continuation

- Fresh isolated read-only Finance/lifecycle inspection: schema1,90–97;365 retained files,
  14279435495bytes and exactly the previously inventoried file paths. All306 acquisitions are complete
  (304success/2historical failures); no active research runs or finite session;44 scheduling opportunities
  remain Skipped. Learning v1 is unchanged/Uninitialized. Lifecycle82sessions/318events; running-session
  clean=0 remains expected, not a clean-shutdown claim. No owner constructor/migration/production write.
- API and Web remain running/healthy with their prior image identities. API is still pre-H revision
  `a2373f172600c7b45ce378c56aab1d7da2b9b242`, image
  `sha256:7dd3126240841269096b2027cbc0610be4b9f81fb097f0e0df9fd4097b184f8a`.
  No configuration or restart occurred. Observation Runtime/Alpaca remain true as previously deployed;
  EODHD/FRED/Research Scheduler remain false. No cycle or manual acquisition was triggered.
- NTP remains synchronized. Latest available destination space454721728512bytes exceeds the measured
  scope requirement64313142965bytes (3x scope plus20GiB); final quiesced scope still requires validation.
  Same-disk/ext4 limitations and owner-approved temporary-plaintext/retention boundaries are unchanged.
- A scoped disposable host-process reader could see the API handles but was denied access to139 live
  host processes. It therefore **did not prove complete writer coverage**. No broader container privilege,
  AppArmor relaxation or production permission change was used. A fixed read-only host FD/maps check is
  prepared for owner-local administrative execution instead; its result does not yet exist.

### Existing execution, now technically coordinated

Private mode0700 execution, metadata, GnuPG and empty drill directories were prepared. They contain
only control/check metadata, not copied Finance material. Ignored local operator helpers implement
this one bounded capture/drill: read-only host handles, separate quiescence and capture markers,
streaming GNU tar to owner-local GnuPG AES256, independent decrypt-and-file-hash comparison, and isolated
extraction without network, production mounts, credentials or application startup. Fixed source mounts
are read-only. No archive is marked COMPLETE until subsequent isolated integrity/native verification
passes. File-manifest and read-only SQLite/native diagnostic helpers are prepared, not executed against
a backup. Syntax checks and diagnostic compilation succeeded; these are not restore acceptance tests.

The actual owner-local runner must be started in the separate private terminal from the repository root:

```bash
python3 .env.bb132k-execution/owner-protected-copy.py
```

It first performs the scoped read-only host handle check through local sudo, then records a sanitized
armed state and waits. Any sudo password and all later GnuPG passphrase prompts stay in that private
terminal. Do not send terminal output or secrets to Codex. The runner itself does not stop/restart services
or edit configuration. Keep clients, owner-drop producers and unrelated maintenance inactive during
coordination. The operator may proceed only after the armed state/complete host proof is actually present,
then executes the approved quiescence, disabled-gate configuration and manifest checks before releasing
capture. A second host-handle proof must show no holders after shutdown and before capture.

At this publication, the actual runner has **not registered an armed state or host-handle proof**;
no capture marker, encrypted archive, plaintext drill or failure-from-capture record exists.
Do not infer readiness from the successful earlier GnuPG probe or a chat confirmation. Gate B remains
incomplete; Gates C/D are NOT EXECUTED. No interruption duration exists. No additional general owner
approval is needed: the remaining dependency is actual owner-local participation and execution-time
safety checks. The source/reference/hash/status of the accepted missing rejected ECB artifact is unchanged;
its contents are neither captured nor guaranteed restorable.

Publication subject: `review: verify BB-132K local crypto proof and record execution gate`.
Only this existing report, catalog and recovery handoff are published. Private manifests, local helper
code, host paths and raw evidence remain outside Git. No deployment/H/migration98, configuration edit,
provider request, production restore, mapping change, Qwen/research/SHADOW/trading action occurred.
Finance remains **RESEARCH / 0 SEK / NONE**. Resume this same execution when the owner-local process
is armed; finish the already authorized capture/drill rather than start another planning checkpoint.

Publication verification: `node scripts/verify-documentation.mjs` PASS (267 Markdown files/91 backlog
IDs), `git diff --check` and staged whitespace PASS, exact three-document scope/manual sanitization
PASS, `gitleaks git --pre-commit --staged --redact --no-banner` PASS/no leaks. Full application suites
were not rerun: no production implementation changed, and they cannot establish owner participation.
No unexecuted capture, independent archive decryption or restore integrity check is reported as PASS.

## Focused pre-execution helper safety review — 2026-10-11

Owner requested only the four local helper checks, corrections and harmless verification; no service
interruption/capture during this review. Same branch, parent `0b19645cc3bc1874fbfb2607c0286f8e5d5f9db9`;
accepted main `618f025a3cb72ee4513a9f14b713382e4a673a42` and tree
`72649fe9c5dd9fb7c2bb7fe0ed0fa0dd9ac06547` verified unchanged after fetch.

### 1. Finance / owner-drop scope: verified separation, enforced rejection of overlap

Read-only Docker metadata confirms that the three selected mount sources match the private recorded
control exactly when compared by destination, independent of Docker's array order. Finance and lifecycle
are separate named volumes; owner-drop is a separate host bind. Their recorded host source roots are
not equal or nested. The disposable capture/manifest helper mounts them flat as `finance`, `lifecycle`
and `owner-drop`, all read-only. It does **not** reproduce the API's nested owner-drop mount underneath
Finance. Therefore the full Finance volume underlay is preserved separately from the bound owner-drop
content, and the bind is traversed once. The existing read-only inventory has four owner-drop files and
zero files in the hidden Finance drop underlay; this is reused inventory, not a new production scan.

Added rejection of missing/duplicate scopes, wrong mount types, equal/nested sources and current-vs-recorded
mount drift. The owner-local host checker also resolves actual paths and device/inode identities and rejects
symlink aliases/physical root overlap before execution. That privileged host check was **not run** in this
review. Synthetic-directory tests verify alias/overlap rejection; an actual GNU tar fixture preserves both
synthetic hidden-underlay and separately bound input exactly once, with no repeated member paths.

### 2. Interruption and safe API/Web restart: explicit, observable, never automatic

The old failure handler recorded only a generic exception and could leave work active. The helper now
records run identity, last phase, failure category, subprocess cleanup outcome and a sanitized read-only
observation of API/Web state (or UNKNOWN if unavailable). It does not invoke Compose/start/restart, edit
configuration, restore production or mark an archive COMPLETE. Partial ciphertext/drill contents and
failure evidence are preserved; an interrupted run is not silently reused. Existing exclusive run state
and a held file lock prevent concurrent/blind repeat execution.

The bounded recovery procedure after an interruption is:

1. Keep clients/intake/maintenance frozen. Read this run's armed, failure and cleanup records. Distinguish
   a failure before quiescence (leave running services untouched) from one after quiescence (leave services
   stopped). If the process was killed before a record could be written, classify state UNKNOWN.
2. Verify owned process groups and disposable containers have exited. Inspect only container names bound
   to the recorded run, checking exact image and run label before any scoped removal. Docker-unavailable,
   ownership mismatch or incomplete cleanup means STOP; never use broad container/prune/kill operations.
   Do not delete partial evidence or reset markers to make a retry pass.
3. Before considering restart, independently verify no source write/restore occurred, clean lifecycle
   shutdown, preserved schema97/receipt bytes/checksums/watermark/history and source-volume identity.
   Required full backup/drill validation remains the existing Gate D work, not these synthetic tests.
   On failed integrity, unclear state or incomplete verification, keep services stopped for containment
   review. Latest owner authorization permits restart only when verification makes it safe.
4. Use the original pre-H image IDs recorded in private control, not a mutable tag or a new main build.
   Confirm effective external configuration keeps all five Finance gates false and MaintenancePaused=true;
   preserve mappings, secrets, mounts and unrelated settings. Owner-drop has no enable gate: reconcile
   existing ready/intake state and keep producers paused; unknown pending startup work means STOP.
5. Only after those conditions and the applicable reviewed execution clearance, use the existing scoped
   Compose path with pinned old images, `--no-deps --no-build --pull never`, API first and Web after API
   readiness. Verify image/OCI/compiled pre-H revision, effective disabled gates, clock/recovery/storage,
   native evidence and absence of unintended startup work. Record actual downtime and post-start state.
   Do not deploy H/migration98, restore production or restart unrelated services/systemd.

These are verifiable future gates, **not a claim that restart or restore has been tested in production**.
SIGKILL, host power loss and unavailable Docker cannot be cleaned up reliably by Python `finally`; they
require the explicit UNKNOWN/manual-containment path. No automatic restart is attempted on any exception.

### 3. GnuPG/tar/Docker lifecycle: corrected and fault-tested

Every capture/decryption/extraction child is registered immediately in its own process group. A `finally`
cleanup runs for normal completion, exceptions and handled SIGINT/SIGTERM/SIGHUP: close pipes, bounded
TERM/wait followed by KILL/wait for survivors. Each disposable Docker container has a unique run/phase
name and run label; cleanup queries the daemon and validates name/image/label before removing only that
owned container, then checks absence. Killing the Docker CLI alone is no longer treated as completion.
Only the run-specific private GnuPG agent is stopped. Cleanup failures prevent a successful finished
record; arbitrary provider/process exception text and credentials are never copied into status.

The manifest helper now uses the same child/container cleanup with no GnuPG-agent operation. It publishes
its manifest atomically only after its readers have exited and cleanup succeeds. Signals and cleanup
failure tests prove it cannot publish a supposedly successful manifest after failed cleanup.

All GnuPG/Docker behavior in regression tests used doubles. Harmless real child-process tests cover a
live producer when the fake encryptor fails, producer exit failure, failure to spawn the second process,
and a TERM-resistant child requiring KILL. Main-path fault injection covers SIGTERM, capture failure,
bad decrypted archive and extraction failure. No real GnuPG passphrase or Docker workload was used in
these tests. The prior owner-local crypto proof remains separate evidence; this review does not claim a
real encrypted archive was made by the corrected helper.

### 4. Coordination markers: corrected run/state binding

Protocol3 creates a fresh random run ID and binds private mode0600 marker files to it, the frozen
control-file SHA256, helper-file SHA256 and exact cleanly stopped API/Web state commitment (IDs, images,
start/finish epochs). Each phase has a distinct marker, UTC issue time within the current run and at most
five minutes old at consumption. Wrong run/control/helper/version/phase/state, future/stale timestamps,
changed image/mounts, restart, nonzero exit or OOM all fail closed. Existing old markers refuse startup.
Files are fully written/fsynced then exclusively published; a consumed phase has an exclusive retained
consumption record and cannot be replayed. No marker grants new production authority.

The quiesced host proof and source manifest carry the same run/control/state identities. Capture checks
the manifest commitment, repeats the host-handle check and rechecks stopped service identity immediately
before capture. Operator coordination must use the new schema; older coordinationVersion2 instructions
must not be reused. Marker fields are `runId`, `controlSha256`, `helperSha256`, `coordinationVersion:3`,
`atUtc`, `phase` and `stateSha256`; quiescence additionally requires `servicesStopped:true`, capture the
actual `manifestSha256`. No old marker or manually guessed hash should be repurposed.

### Exact local delta and verification

Only existing ignored local operational helpers were corrected; one ignored local harmless regression
script was added. No application source/tests, production configuration, database or permissions changed.
These host-specific helpers remain local, as requested. Exact SHA256 for review/revalidation:

|Local helper basename|SHA256|
|---|---|
|owner-protected-copy.py|031f388610c5df7eb5b7907b7b2e04f414f1685ac675d0d0c55436addc3ae30e|
|host-scope-handles.py|566071be4257fe4476037e01f2cf6b8ed990e9556e76260b7076e85aee3fee18|
|manifest-readonly.py|88d1de2aa5336956fcc54e97b411f8f5b4fd589e1cdcdd0d0ab293e5c4465166|
|test-owner-protected-copy.py|566f203c7694777b58ad03c35433e11422c7bf53e75fb5bff949dd121018ae23|

Commands actually executed:

```text
python3 -W error::ResourceWarning .env.bb132k-execution/test-owner-protected-copy.py
python3 -m py_compile .env.bb132k-execution/owner-protected-copy.py .env.bb132k-execution/manifest-readonly.py .env.bb132k-execution/host-scope-handles.py .env.bb132k-execution/test-owner-protected-copy.py
```

**27 tests PASS,0failed,0skipped; syntax compilation PASS.** An initial harmless test exposed an unclosed
lock on preflight refusal; corrected with a context-managed lock before the final green run. No unchanged
application build/backend/frontend suite was repeated: those do not exercise these local Python helpers.

Final read-only service inspection: API/Web running and healthy, exact original image and start identities
unchanged. No owner armed marker, capture marker or encrypted archive exists. No service stop/restart,
backup, drill, real provider request, migration, configuration edit, mapping change, model/research/SHADOW
or trading action occurred. Finance **RESEARCH / 0 SEK / NONE**. The accepted ECB absence remains excluded.

Publication: same report/catalog/recovery only, subject
`review: harden BB-132K local capture coordination and cleanup`.
STOP for architect review. The four-point code/fixture review is complete; actual Gate B/C/D execution,
full privileged handle proof and real protected backup/restore verification are still outstanding.

Publication checks: documentation verifier PASS (267 Markdown files/91 unique backlog IDs);
working/staged `git diff --check` PASS; exact report/catalog/recovery scope PASS. Gitleaks directory
scan of the local helper area and staged redacted scan both PASS/no leaks. All four documented
helper hashes match the local files. No private source paths, raw evidence, credentials or passphrases
are included in the published delta. Local helper changes themselves remain excluded from Git.
