# BB-132K — Consolidated Finance deployment readiness

## Metadata

Detta är en sanerad GitHub-version. Date2026-10-10; final planning package, no operational execution.

- Accepted main: `618f025a3cb72ee4513a9f14b713382e4a673a42`.
- Accepted tree: `72649fe9c5dd9fb7c2bb7fe0ed0fa0dd9ac06547`; fetched and verified unchanged.
- Branch: `review/finance-bb132k-consolidated-readiness`, directly from that main.
- Publication subject: `review: consolidate BB-132K Finance deployment readiness`.
  Resolve exact publication SHA/tree from Git metadata; no self-referential commit hash.
- [Accepted H implementation evidence](bb-132h-versioned-observation-mapping-20261010.md),
  [I host review](https://github.com/T-bear/BigBrain/blob/4eaf9cef6388af29d9509c71bad93ab351b4f474/docs/reports/features/finance/bb-132i-operational-readiness-20261010.md),
  [J complete inventory](https://github.com/T-bear/BigBrain/blob/ca05b5b1ec50fca377208dd36fc18c7ef390550f/docs/reports/features/finance/bb-132j-evidence-recovery-preflight-20261010.md),
  [K Phase A measurements](https://github.com/T-bear/BigBrain/blob/dcf2c5cc81d602a007173aba9a34a7789a190e51/docs/reports/features/finance/bb-132k-recovery-execution-preflight-20261010.md).
- Governing contracts: [ADR0028](../../../adr/0028-finance-provider-tagged-backup-restore-and-cleanup.md),
  [ADR0026](../../../adr/0026-bigbrain-appliance-lifecycle-and-recovery.md),
  [backup runbook](../../../operations/backup-restore/README.md),
  [controlled build/configuration](../../../operations/runbooks/finance-controlled-acquisition.md),
  [mapping ownership](../../../architecture/finance/market-data-memory-and-provenance.md),
  [EODHD retention](../../../operations/runbooks/finance-eodhd-retention-deletion.md).

## Status and owner decision

**REVIEW CHECKPOINT — NOT MERGE CANDIDATE. Execution remains blocked at gate A below.**
This package consolidates existing evidence; no new host/database survey, build or production action
was performed. GnuPG and existing local storage are already selected by the owner. The passphrase
remains owner-only, entered locally; no further encryption-method decision is requested.

The first decision is a **conditional, bounded B–D authorization**: after gate A is evidenced,
permit API/Web interruption, protected full copy, isolated drill, private temporary plaintext and
specified retention. This does not authorize deployment. Gate E requires a second explicit decision
based on actual successful recovery evidence, before exact-main deployment/migration98 at F–G.
After G, STOP before mapping adoption or any provider/reactivation decision at H.

Dated STATUS/BACKLOG/H-report wording still says candidate/not accepted. The exact owner-approved
merge/main tree and I's recorded acceptance CI establish H's acceptance, not deployment. This
package records that distinction without unrelated status reconciliation or rewriting prior reports.

## Evidence reused and execution-time checks

K Phase A rechecked J at2026-10-10T19:19Z. Those observations are reused, not represented as a fresh
atomic snapshot. Only execution-dependent facts below must be rechecked immediately before actions.

| Reusable evidence | Execution-time gate |
| --- | --- |
| Old image `sha256:7dd3126240841269096b2027cbc0610be4b9f81fb097f0e0df9fd4097b184f8a`, OCI/compiled revision `a2373f172600c7b45ce378c56aab1d7da2b9b242` | Check running image/revision and source main again; drift requires STOP |
| Compose API healthy; aggregate503/Degraded; recovery healthy, last integrity check October4 | Distinguish container health from aggregate state; no fresh full-DB proof claimed |
| Runtime/Alpaca true; cadence360/lookback3; AAPL only, mapping ends2026-10-02; EODHD/FRED/scheduler false | Check allowlisted booleans only, then disable under approval; never repeat a provider call |
| Finance schema97; H tables absent; two Alpaca roots, zero daily shadow candidates/results | Read-only snapshot commitments before capture and compare after migration |
| API sole observed DB-volume consumer; unless-stopped,35-second stop grace; failed enabled bigbrain.service | Freeze host reboot/deploy/manual commands; verify every writer/handle actually stopped |
| Finance tree14,279,279,623 bytes; lifecycle155,872; combined S=14,279,435,495 | Remeasure scope, allocated/apparent sizes, space and inodes after quiescence |
| Selected private operator local-data parent:0700, no extended ACL/symlink; outside Git/Docker mounts | Resolve private aliases again; exclusive new children, no symlink/path escape or cleanup collision |
| ext4 on same approximately3TB disk as source;456,169,156,608 bytes available | Require at least3×actual S plus20GiB reserve; prior requirement64,313,142,965 bytes |
| GnuPG2.4.7 and tar available on host; no encrypted scratch filesystem verified | Owner-local pinentry/decryption capability and permitted source-read privilege before interruption |

This is same-disk recovery against a bad migration, not disk-failure/disaster protection. No downtime
estimate is justified. API/Web remain stopped through D and the separate E review in the primary plan;
owner approval must explicitly accept that waiting interval, not just the file-copy time.

## A — Minimum rights and protection gate

These are classifications under **recorded project evidence**, not new legal findings or provider
warranties. Encryption does not confer copying rights. Existing mixed-provider recovery precedent
in BB-090 does not grant indefinite retention or classify every current file.

| Class | Classification and exact condition |
| --- | --- |
| Verified NASDAQ-WIKI canonical subset and exact WIKI-only derivations | **VERIFIED PERMITTED** for local indefinite copy under ADR0028; preserve source/revision attribution |
| Alpaca receipts/lineage and derived private evidence | **VERIFIED RESTRICTED WITH TRACKABLE CONDITIONS** under existing owner-accepted bb132e-owner-data-use-risk-v1, including backup/replay. Provider lifecycle/deletion uncertainty remains; concrete contrary duty stops affected use. Owner risk acceptance is not an Alpaca guarantee and need not be requested again |
| EODHD Free / eodhd-free-personal-v2026-08-11 and dependent science | **VERIFIED RESTRICTED WITH TRACKABLE CONDITIONS** as a policy class: private active-account storage, all copies deletion-controlled within one month after verified termination/expiry. **Current execution blocked** until owner confirms actual account/entitlement status; AccountActive=true/empty end-date alone is not evidence |
| Selected first-party FRED series DFF/DGS2/DGS10/CPIAUCSL/UNRATE | **VERIFIED PERMITTED** only when each retained artifact matches the recorded public-domain/citation-requested series and source evidence; FRED API access is not blanket rights for other series |
| Selected Riksbank SWEA / ECB first-party packs | **VERIFIED RESTRICTED WITH TRACKABLE CONDITIONS** when exact pack/revision provenance matches recorded LOCAL_RESEARCH use and attribution requirements; ECB third-party data excluded |
| Macro files not matched to those records | **UNKNOWN / EXECUTION BLOCKED** until matched to existing macro candidate/revision rights evidence or owner-held source permission |
| Intake/quarantine/owner-drop payloads and associated metadata | Known WIKI/other verified artifacts inherit their exact policy; rejected validation does not itself remove rights. Unmatched/provider-unknown artifacts remain **UNKNOWN / EXECUTION BLOCKED**; quarantine status alone is not copying permission |
| Existing nested backups/restore staging | Inherit complete original classifications and lineage; **UNKNOWN / EXECUTION BLOCKED** until every nested manifest/data pair or old full-DB copy is matched to its provider inventory. Never infer public-domain from directory name |
| Lifecycle/local audit records | **VERIFIED PERMITTED** as owner-authorized private operational recovery data, with restricted provider-linked content inheriting its restrictions; no public raw records |

Macro policy sources already recorded in [BB-090](finance-bb-090-implementation-runtime-20260816.md)
and [BB-091](finance-bb-091-european-macro-fx-20260817.md). These identify permitted products; they do
not prove that every file presently on disk belongs to them. K did not perform that per-file mapping.

**Missing evidence is specific, not a new investigation sprint:**

1. Owner supplies a local dated account fact confirming whether EODHD Free remains active, with the
   applicable recorded policy/product and whether a termination/expiry/deletion notice exists. An
   account notice/reference is sufficient; no token, login, screenshot containing secrets or provider
   call is requested. If ended, record actual date and applicable copy permission; the deletion grace
   is not assumed to authorize a new recovery copy. Do not invent an end-date or toggle entitlement.
2. Before any stop/copy, reconcile existing `macro_candidates` rights/evidence and revision references,
   `dataset_candidates` license/provenance/manifests, owner sidecars, and `*.manifest.json` backup
   records against the private file list. Use read-only SQL/file reads, never the mutating owners.
   This is the finite gate-A checklist, not repeated receipt/runtime research. For each unmatched
   artifact, owner must identify its source/product and permission for this bounded copy/drill or
   provide a separately reviewed explicit risk decision where lawful. A blanket instruction to copy
   cannot override a known prohibition. If still unknown, STOP without omitting/deleting the artifact.
3. Approve temporary private plaintext for the isolated drill on unencrypted ext4. Proposed limit:
   only the controlled validation window, cleanup after successful evidence collection; no overnight
   plaintext retention without a separate decision. Filesystem/swap remnants cannot be guaranteed erased.

The private inventory records artifact or DB-selection ID, provider/product/policy, evidence reference,
source/derived lineage, copy classification, every archive/drill/staging location, creation time,
retention trigger/deadline, responsible owner and disposal audit. Preserve nested-copy obligations.
Proposed local archive retention: until seven days after verified deployment acceptance, with a
30-day creation-age cap, whichever occurs first, and always shortened by applicable provider duties.
These are proposed operational limits, not invented provider terms; B approval must set them explicitly.
At expiry, stop affected use and obtain/execute only the specifically approved copy disposal; do not
silently extend or erase source. A restriction within a mixed archive may require retiring the whole
archive, not editing its immutable contents. Retain permitted non-payload audit facts.

## B–D — Minimum viable protected recovery package

All steps below are **proposed, require explicit B approval and were NOT executed**. Private aliases
are resolved locally; never commit a path map, environment dump, file list containing private names,
passphrase or source payload. No new production component is required.

### B — Authorization and finite preflight

Approve API/Web interruption and client/intake freeze; atomic private config edits; copy/drill and
owner-local GnuPG use; the stated plaintext/retention limits; containment if checks fail. Preserve
unrelated services and source files. Specify whether services may remain stopped awaiting E; the
primary plan says yes. Returning the old API is a separately scoped contingency described below.

Use Git commands to verify accepted SHA/tree and clean tracked source. Resolve Compose project
`bigbrain-sprint1`, API image ID, both named volumes and owner-drop bind using selected inspect fields
only. Verify no unexpected volume consumer, symlink, outside evidence reference, missing artifact,
clock regression, insufficient capacity or new in-progress research/acquisition before interruption.
Resolve the existing live Compose file/override/env selection before using the command examples below;
keep that same project/volume identity and never fall back to a new project. Use existing J diagnostics
with hash verification, not an API constructor. Persist only sanitized
summary publicly. Reject pending/running research or started acquisitions needing reconciliation;
do not silently reset/refund them on startup. Account for any known staging cleanup before restart.

Prepare private0700 capture metadata/drill directories and0600 files only after approval. All new
paths must be exclusive children of the selected parent, not live volumes. No production chmod/chown.
Verify host tar/GnuPG and the scoped read mechanism work before downtime; no plaintext source archive
is required. Owner uses a local terminal outside Codex capture, with pinentry and no shell tracing.
No passphrase flags, environment variables, stdin pipes from Codex or terminal recordings are permitted.
Symmetric encryption requires no keypair generation. GnuPG's own private operational files belong in
an approved temporary/private home; disable symmetric-key caching and do not publish its contents.

### C — Quiesce and capture

1. Pause client requests and owner-drop producers/manual commands. Freeze host reboots and service
   starts; leave failed bigbrain.service untouched. Stop Web first to drain UI, then API, using the
   existing Compose project/configuration (not broad `down` or appliance shutdown). Proposed commands:

   ```bash
   docker compose stop web
   docker compose stop api
   ```

   Before any subsequent start, atomically set the existing external ObservationRuntime, Alpaca,
   EODHD, FRED and ResearchScheduler Enabled values false; preserve unrelated values/mode0600.
   Set the existing ResearchOperations MaintenancePaused control true for the transition. Neither
   configuration edits nor this pause stop constructors; the API must actually exit cleanly.
2. Confirm no timeout/forced kill, lifecycle clean shutdown committed, all volume/bind handles closed,
   and no other writer/container/operator remains. Check scoped `lsof`/`fuser`, Docker mount consumers
   and restart state without dumping arguments/environments. New writer or unclean exit means STOP.
3. Record a private, deterministic relative-path manifest with file types, sizes, ownership/modes,
   SHA256 and external-reference bindings. Capture the complete Finance named volume, lifecycle named
   volume and separate owner-drop bind exactly once. Include DBs and any surviving WAL/SHM/journals,
   payloads, macro/quarantine, nested backup/staging and recovery metadata. Preserve any named-volume
   content hidden under the owner-drop mount as well as the separately bound tree; measure both. No live-file-only SQLite
   copy, manual checkpoint, VACUUM or deletion. Missing/outside reference or unexplained link means STOP.
4. Stream a bounded archive from read-only source mounts into GnuPG; do not materialize a second
   plaintext archive. Use existing host GNU tar with separately permitted read privilege, or a
   disposable exact-old-image helper only after verifying its tar supports the required metadata.
   No source permission modification. A helper has no network, production credentials or Docker
   socket, and source mounts read-only. Preserve numeric ownership/modes and necessary ACL/xattrs;
   reject absolute/traversing member names, special files and escaping links before extraction.

   Owner-terminal encryption/decryption forms, with locally resolved non-secret path variables:

   ```bash
   # Archive producer writes only to this pipe; pipefail and both exit codes must be checked.
   # Owner supplies the passphrase solely through local pinentry; never through Codex.
   gpg --pinentry-mode ask --no-symkey-cache --symmetric --cipher-algo AES256 --output "$recovery_archive"
   gpg --pinentry-mode ask --no-symkey-cache --decrypt "$recovery_archive"
   ```

   The second command's output must be piped directly to the private validated extractor, **never
   the terminal**. Do not run these fragments standalone. Use an exclusive incomplete filename,
   encrypted embedded file manifest plus private external ciphertext checksum. Check producer,
   encryption and extraction success; only after D mark COMPLETE and protect against accidental writes.
   Read-only modes/hashes are not WORM and do not resist a privileged attacker.

### D — Independent restore drill and equivalence

Decrypt to a separate private drill tree, no hardlinks to original/source and no source mounts in the
validator. Network none, no secrets/socket, no API startup. Preserve archive unchanged. Verify every
manifest entry before opening DBs; confirm both DB companion sets and all304 previously referenced
payloads plus every macro/intake/nested artifact. File presence alone is insufficient.

Use isolated SQLite read-only connections with compatible WAL handling; never `immutable=1` to ignore
live journal state. Full `PRAGMA integrity_check` must return only `ok`, and `PRAGMA foreign_key_check`
zero rows on both drill DBs. These are drill-only checks, not production repair/persistent PRAGMAs.
If SQLite requires drill-local recovery writes, preserve original extracted hashes and permit writes
only in disposable drill scope; compare raw captured bytes separately from recovered logical state.

Reuse J's pure readers/native lineage checks and private manifests; no migrating Finance owner. Compare
all table counts and immutable row commitments, ledger/grant state, lifecycle clean session, external
hash references and the exact expected receipt/projection table below. No cutoff seal or science run.
The existing J readers cover observation and recovery inventory, not full DB integrity/artifact audit:
perform those additional checks with installed host Python SQLite/hash tools against the isolated tree.
They are diagnostic procedures, not a new deployed helper. No application tests substitute for the drill.

After successful drill, owner authorizes/executes only inventoried temporary-copy cleanup under B's
agreed policy. Keep ciphertext and private inventory, verify owner can independently decrypt it.
**STOP at E and publish sanitized copy/drill evidence. No migration is authorized by a successful drill.**

## E–H — Exact deployment package, separate owner approval

**E:** owner approves exact main deployment/migration98 only after complete recovery/drill evidence,
archive identity, clean quiescence and current rights gates are reviewed. Services remain stopped in
the primary plan. If they were restarted meanwhile, re-quiesce and recapture changed durable state;
an old recovery copy cannot silently discard intervening lifecycle/scientific/evidence writes.

**F:** on clean checkout of exact accepted main, fetch and assert the same commit/tree. Use only:

```bash
scripts/build-bigbrain-api.sh 618f025a3cb72ee4513a9f14b713382e4a673a42
```

This script requires HEAD=origin/main, clean tracked files and git-archive build input, excludes local
secrets/untracked files, and does not deploy. Verify resulting image OCI revision; record immutable
image ID in a private Compose override and keep the exact old image available. Do not trust/reuse a
mutable tag. Before startup ensure all five Finance enable flags false, MaintenancePaused=true,
original legacy InstrumentsJson unchanged and no new assertion. Effective values must be checked
through an allowlist, never `docker compose config` or full inspect output.

The owner-drop scanner has **no enable gate** and runs on startup. To prevent new intake without
moving/deleting original ready files, include in E approval a temporary empty private read-only
owner-drop bind via existing `FINANCE_MARKET_DATA_DROP_PATH`. Preserve the original bind and its
configuration reference in the private recovery inventory. Do not restore intake automatically.
This is an explicit temporary external configuration change, not a new scheduler or implementation.

Recreate only API with the normal Compose files plus private pinned-image override:

```text
docker compose <existing approved file/env selection plus private override> up -d --no-deps --no-build --pull never --force-recreate api
```

Startup through the accepted Finance owner applies additive98; do not issue manual production DDL
or `finance-schema-status`. H adds two tables and one migration record, not rewritten receipts.
Startup also owns lifecycle/reconciliation writes independently of provider flags. Preflight must
have excluded unfinished research/acquisitions and identified staging side effects; unexplained
changes are not acceptable merely because startup produced them. Do not call lazy Finance HTTP
inventory endpoints whose constructors can clean backup staging.

**G:** verify image ID/OCI and early-exit compiled identity:

```bash
docker compose exec -T api dotnet BigBrain.Api.dll system-build-identity
```

It must print exact accepted main. Check health/recovery/clock, false flags, no intake/provider/science
work and unchanged historical config. Run read-only SQL/native comparisons below with source mounts
read-only and no owner constructor. Verify schema98 and both mapping tables **empty**; deployment
alone must not adopt a manifest or retrofit receipt bindings. Compare original table/receipt/JSON
commitments, watermark/projection and scientific grants. Lifecycle legitimately gains the new session;
compare preserved old rows and expected clean-stop/new-session events, not whole DB file hash equality.

Reopen independent read-only connections for evidence validation. No actual database restore onto
production is part of deployment. Web may be returned only after G passes and client maintenance
restrictions remain; no Finance mutation/activation is approved. Aggregate historical degradation
must be explained, not hidden or repaired by triggering a provider operation.

**H: STOP.** No mapping adoption, date extension, one-shot, runtime activation, model, SHADOW or
research invocation. Those need separate scientific/operational evidence and authorization.

## Verification procedures and expected evidence

Use J's recorded read-only diagnostic source/assembly identities from Phase A, revalidated locally.
Run it with the corresponding image assemblies without invoking Main/owner constructors. If a
schema98-sensitive helper refuses, inspect its compatibility rather than bypassing validation;
use the documented read-only SQL plus pure native receipt reader, never a migrating status command.
These SELECTs are safe read procedures, not executed in this consolidation:

```sql
SELECT version,name,applied_utc FROM finance_schema_migrations ORDER BY version;
SELECT name FROM sqlite_master WHERE type='table' AND name IN
 ('observation_mapping_manifests','observation_mapping_receipts');
SELECT id,checksum,ingested_ticks FROM market_observation_receipts ORDER BY ingested_ticks,id;
SELECT watermark_ticks FROM market_observation_clock WHERE singleton=1;
```

Hash stored `receipt_json` privately as UTF8 without printing it. After98, fixed count queries must
return zero for both new mapping tables. Full integrity/FK checks run on drill copies; production
inspection uses Mode=ReadOnly, pooling disabled and fixed SELECTs in read transactions. Never invoke
`MarketKnowledgeAt` to verify a historical cutoff: recompute the projection in memory from receipts.

| Preserved fact | Expected value from J/K |
| --- | --- |
| October2 root ID | `sha256:caa655557878353e9d700ec84c0c696777fcbf52ee08a63b07f72a99e3a5d11c` |
| October2 full checksum / JSON hash | `sha256:13d6426177e0553e1c6ca86c856d0d814695da5030bc56d9c4dea404d8e9637d` / `sha256:762922a19de0a65dfc311c37a7903ac0bd5885bcd2697a6ec0e179798e5c1b10` |
| October1 root ID | `sha256:dfc00b55e068d54947f84c0a15ccb5c4bc9a4bdb3a8fd6aaef86580413ec5677` |
| October1 full checksum / JSON hash | `sha256:e0d4e47397eb93c8f2b1091ff69d05656f237197d7622f1554230952c30f9260` / `sha256:9c25dac8187bc80b58350fc61f6627f518894ebbc74d83945fb624698eef793b` |
| Both snapshots | US:XNAS:AAPL, Equity/USD/XNAS, Alpaca/iex-historical-1Day-raw; full original instrument/mapping through2026-10-02 unchanged; no predecessor |
| Knowledge UTC, October2 / October1 |2026-10-04T17:56:20.0069441+00:00 /2026-10-05T00:07:28.8789067+00:00 |
| Acquisition UTC, October2 / October1 |2026-10-04T17:56:19.98109+00:00 /2026-10-05T00:07:28.8207632+00:00 |
| Watermark |639267556488789067 |
| Historical cutoff / projection |2026-10-04T14:29:13.9256374+00:00 / `sha256:5d150d96d66c3c6442868d7b251a3c0b027e43e70a7a4ca9e4ec0198d6f62db6` |
| Before/after knowledge membership | Absent one tick before each persisted ingestion; present at ingestion; source day never substitutes for knowledge |

Public completion evidence contains identities, sanitized counts, result categories, archive checksum,
rights-gate disposition, elapsed interruption actually measured and explicit non-actions. Private file
manifests/path maps, rights/account documents, passphrase, raw payloads and credentials remain local.

## Failure containment and rollback

- Gate-A unknown rights, changed baseline/image/source, insufficient space, unexpected writer,
  unclean stop, missing external artifact, source hash drift, archive/decryption failure or failed
  integrity/FK/native comparison: STOP; do not migrate, delete source or call a provider.
- B approval must cover leaving services stopped on failure. An optional old-image restart requires
  explicit scope: exact old image, disabled five gates, paused research and empty intake bind. It
  creates lifecycle writes, so require new quiescence/recovery evidence before later migration.
- No schema98 down migration exists. Old-image rollback alone is not proven safe against98; never
  drop tables or edit migration rows. After migration failure, stop API under approved containment,
  preserve failed/current data and logs privately, and obtain a separate restore decision.
- A production restore is **not** the isolated drill. Before any separately approved restore, preserve
  all post-copy evidence/assertions/seals/grants/lifecycle writes and reconcile them; do not overwrite
  newer knowledge with a stale copy. Acquisition or trusted historical knowledge cannot be undone.
- Protect the original recovery archive; any repair experiment uses another inventoried isolated copy.
  No source data cleanup, broad appliance restart, systemd repair or automatic retries are in this plan.

## Changes and verification

Changed files:

- `docs/reports/features/finance/bb-132k-consolidated-readiness-20261010.md`
- `docs/reports/REPORT-CATALOG.md`
- `docs/operations/codex-recovery.md`

Only this report, a catalog entry and brief recovery link change. No production/helper/test code,
configuration or runbook behavior changed. Previous review branches and unrelated untracked work
remain preserved. Source inspection confirmed migration98, startup writers, empty-intake requirement,
exact build helper and existing configuration controls. No new host evidence was collected.

Publication verification:

- `node scripts/verify-documentation.mjs`: PASS,267 Markdown files/91 unique backlog IDs.
  Initial check caught an incorrect BB-090 link; corrected to the existing report, then passed.
- `git diff --check` and `git diff --cached --check`: PASS.
- Exact staged scope: three documentation files listed above; no production/test/config files.
- `gitleaks git --pre-commit --staged --redact --no-banner`: PASS, no leaks.
- Manual review: receipt/image commitments match published J/K evidence; command fragments are
  explicitly future-gated, private paths/credentials absent and no operational execution claimed.
 No application build,
backend/frontend/Sentinel suites, GnuPG action, backup or restore performed; existing H verification
is reused for unchanged code, not presented as an operational drill or deployment result.

## Security

No secret values/fingerprints, raw provider payloads/OHLCV, private absolute paths, credentials or
full environments are published. No production action, provider request, runtime trigger, mapping
adoption, Qwen/SHADOW, research grant change, PAPER/LIVE/AUTO, broker/orders/positions/capital occurred.
Finance remains **RESEARCH / 0 SEK / NONE**. Publication grants no execution or merge authority.

## Remaining work

Only concrete gate-A rights matching/account evidence and explicit B–D protection/interruption
approval precede recovery execution. The owner-selected GnuPG method is settled. E deployment approval
is intentionally deferred until actual successful copy/drill evidence; mapping/acquisition remain later.
No additional generic research checkpoint or new backup framework is proposed.

## Resumption

Architect reviews this exact consolidated publication and presents the owner with B–D's conditional
operational authorization. Complete gate A from existing private manifests and owner-held evidence;
if it cannot pass, report the exact unmatched artifact/condition privately and STOP. No production
operation is authorized by this report. After this publication, return control and wait.
