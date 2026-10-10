# BB-132K Phase A — Protected recovery copy execution preflight

## Metadata

- Date: 2026-10-10. Scope: read-only live inspection, recovery planning and sanitized publication.
- Baseline: `618f025a3cb72ee4513a9f14b713382e4a673a42`;
  tree `72649fe9c5dd9fb7c2bb7fe0ed0fa0dd9ac06547`, verified from fetched Git.
- Branch: `review/finance-bb132k-recovery-execution-preflight`, created from that exact main.
- Publication subject: `review: publish BB-132K recovery execution preflight`.
  Resolve its exact commit/tree from Git metadata on this branch; no self-referential SHA.
- Prior evidence: [BB-132J at ca05b5b](https://github.com/T-bear/BigBrain/blob/ca05b5b1ec50fca377208dd36fc18c7ef390550f/docs/reports/features/finance/bb-132j-evidence-recovery-preflight-20261010.md).
  That review branch was not merged or rewritten.
- Authority: [ADR0028](../../../adr/0028-finance-provider-tagged-backup-restore-and-cleanup.md),
  [backup guidance](../../../operations/backup-restore/README.md),
  [ADR0026](../../../adr/0026-bigbrain-appliance-lifecycle-and-recovery.md),
  [lifecycle runbook](../../../operations/runbooks/bigbrain-appliance-lifecycle.md),
  [EODHD retention](../../../operations/runbooks/finance-eodhd-retention-deletion.md).

Detta är en sanerad GitHub-version. Private destination paths, payloads and credentials are omitted.

## Status and decision

**REVIEW CHECKPOINT — NOT MERGE CANDIDATE. Phase B execution: NO-GO pending the gates below.**
The owner selected existing server storage for planning, not capture or service interruption.
A technically suitable private parent exists, with adequate measured space. It shares the source
physical disk and is not protection against disk failure. No recovery directory, test file, backup,
restore copy or encryption key was created. No service/configuration/permission change occurred.

Required before Phase B can safely be authorized:

1. Complete the private per-artifact rights/retention inventory, including EODHD entitlement evidence,
   macro/intake/quarantine and nested older copies. An active configuration flag is insufficient.
   ADR0028 unknown-classification refusal remains binding; do not silently omit evidence or delete source.
2. Approve the protection policy: encrypted archive plus explicitly bounded private plaintext drill,
   or establish a separately approved encrypted drill destination. No encrypted drill filesystem was verified.
3. Approve the exact service/client/intake quiescence scope, capture/drill writes, retention schedule,
   and optional restart of the old image with acquisition disabled. Read-only Phase A grants none of these.
4. Recheck writer absence, capacity, clock, rights and source commitments at the execution window;
   current inspection is not a quiescent or cross-store consistency proof.

This is a readiness gap, not evidence of corrupt receipts. No H deployment, migration98, mapping
assertion or provider acquisition is included in the proposed Phase B.

## Evidence — destinations and protection

Read-only metadata inspection used `lsblk`, `findmnt`, `stat`, parent/symlink checks, `getfacl`,
filesystem capacity and narrowly filtered Docker mount metadata. No write probe was used.
Private absolute paths are represented by local operator aliases, not published here.

| Candidate | Observed facts | Suitability |
| --- | --- | --- |
| PRIVATE_RECOVERY_PARENT: existing operator private local-data directory | ext4 on source disk; mode0700 through private home/local-data ancestors; UID/GID1000; ACL owner rwx only, no extended/default ACL; traversable/writable by operator; no symlink in inspected chain | Selected proposed parent; outside Git and every inspected Docker bind/volume mount |
| Operator home | Same ext4; mode0700; writable | Technically possible, less distinct separation from other private files |
| Project parent | Same ext4; mode0775 inside private home | Avoid developer/workspace cleanup coupling |
| System backup/service directories | Same ext4; root-owned0755, not operator-writable | Requires additional privilege; not selected |
| Other physical disk | Approximately1TB NTFS partition, currently unmounted | Not available without a separate mount/access decision; untouched |

The selected parent and Finance/lifecycle sources reside on the approximately3TB physical disk's
ext4 partition. The other disk is physically distinct but was not mounted or tested. No dm-crypt
layer was observed. The proposed private child does not exist. No production chmod/chown is needed
for future copying via the existing privileged Docker access and operator-owned destination.
That access is powerful; use only explicitly scoped mounts, never the whole host or Docker socket
inside a helper. Preserve original file modes/ownership in a private manifest; do not widen source access.

No inspected Docker mount overlaps the selected parent. Finance backup/restore cleanup targets its
own configured staging directories, not this parent. Arbitrarily named privileged host jobs were
not exhaustively audited: automatic-cleanup exclusion must be confirmed at the execution window.
Resolve parents again without symlink traversal, create an exclusive new child, reject preexisting
contents or changed ownership, and prohibit archive absolute paths, traversal and symlink escapes.

### Capacity

| Quantity | Observed/proposed bytes |
| --- | ---: |
| Available at selected filesystem inspection | 456,169,156,608 |
| Finance mounted tree, apparent size including nested owner-drop view | 14,279,279,623 |
| Lifecycle mounted tree | 155,872 |
| Combined observed scope S | 14,279,435,495 |
| Two independent full-size copies, no compression assumption | 28,558,870,990 |
| Proposed execution gate: 3 × S plus20GiB free reserve | 64,313,142,965 |

The third S is staging/headroom, not permission to create an untracked third copy. Include every
artifact in retention inventory. Measure allocated/apparent sizes and inodes again after quiescence;
recompute for actual scope/growth. Named-volume copying must include the separate owner-drop bind
exactly once; the mounted-tree size already includes its view. No throughput or downtime was measured.

GnuPG2.4.7, tar, ACL inspection and open-handle tools are installed. Age, cryptsetup, gocryptfs and
rsync were not available. An encrypted archive is feasible with GnuPG, but key/passphrase custody
and independent decryption verification remain Phase B prerequisites. No key material was accessed.
Mode0700/0600 is access control, not encryption. Read-only file modes plus hashes protect against
accidental modification, not a privileged attacker/WORM guarantee. A plaintext drill can leave
filesystem/swap remnants; do not promise secure erasure on this unencrypted filesystem. Co-located
copies do not provide disaster recovery, even when encrypted.

## Evidence — source drift against BB-132J

Fresh read-only inventory completed at `2026-10-10T19:19:16.3933357+00:00`; related lifecycle/file
reference inventory at `2026-10-10T19:19:26.3966632+00:00`. Every compared receipt/recovery field
matched J. No new receipt, revision, mapping snapshot, schema or knowledge commitment was observed.

| Item | Phase A result |
| --- | --- |
| Running API image | `sha256:7dd3126240841269096b2027cbc0610be4b9f81fb097f0e0df9fd4097b184f8a` |
| OCI revision | `a2373f172600c7b45ce378c56aab1d7da2b9b242`, old pre-H deployment |
| Compiled revision evidence | Same immutable image/container as I/J; their compiled-identity verification reused, not rerun here |
| Docker health / aggregate health | healthy / HTTP503 Degraded; do not conflate them |
| Recovery snapshot | HTTP200, healthy/completed, RESEARCH; last integrity check remains October4, not a fresh whole-DB check |
| Clock | NTP synchronized; recovery clock prerequisite reported valid |
| Runtime / Alpaca transport | true / true; cadence360 minutes, lookback3; bounded historical AAPL mapping unchanged |
| Other Finance gates | EODHD=false, FRED=false, ResearchScheduler=false |
| API ownership/restart | Compose; unless-stopped;35-second stop timeout; only API container mounts the two DB volumes |
| Other lifecycle actor | bigbrain.service failed and enabled; not repaired; host boot/deploy must remain frozen during capture |
| Process inspection | API dotnet process; no new maintenance writer started by this review |
| Finance versions | 1,90,91,92,93,94,95,96,97 |
| H manifest/binding tables | Both absent |
| Receipts / daily shadow candidates / results | 2 / 0 / 0 |
| Watermark ticks / UTC | `639267556488789067` / `2026-10-05T00:07:28.8789067+00:00` |
| Native receipt/lineage check | PASS; watermark equals latest ingestion, not future check time |
| Lifecycle |82 sessions,318 events; latest start2026-10-04T18:07:24.3112689+00:00; clean0/no shutdown UTC, normal while running |
| Legacy payload references |304; all present within Finance scope,0 missing/outside; existence check, not complete artifact hashing |

Finance DB8,937,451,520 bytes; WAL32,992 and SHM32,768. Lifecycle DB90,112 bytes with WAL32,992
and SHM32,768. Files UID/GID1654 and mode0644. All match J. External scopes include payloads9,248,915,
intake/quarantine280,486,976, macro2,523,004, existing backup area3,699,908,231, restore staging1,344,151,552,
and separate owner-drop bind5,443,665 apparent bytes. These are overlapping subscopes of the tree,
not numbers to add again. WAL/SHM were only observed, not checkpointed or altered.

### Receipt commitments

Both are AcquiredProviderDaily roots, with no predecessor; New classification follows stored chain
structure. Provider Alpaca, dataset `iex-historical-1Day-raw`, contract `alpaca-historical-stocks-1Day-raw-v2`.
Canonical instrument `US:XNAS:AAPL`, Apple Inc., Equity/Active, USD, XNAS. Instrument and mapping
both retain2026-01-29 through2026-10-02 and canonical venue `THE NASDAQ STOCK MARKET LLC`.
Evidence reference remains `operator:apple-sec-0000320193-26-000005-faq-20261004-alpaca-aapl`.
IEX is feed, not listing venue. No current listing/assertion verification is claimed.

| Commitment | October2 source day | October1 source day |
| --- | --- | --- |
| Receipt ID | `sha256:caa655557878353e9d700ec84c0c696777fcbf52ee08a63b07f72a99e3a5d11c` | `sha256:dfc00b55e068d54947f84c0a15ccb5c4bc9a4bdb3a8fd6aaef86580413ec5677` |
| Full checksum | `sha256:13d6426177e0553e1c6ca86c856d0d814695da5030bc56d9c4dea404d8e9637d` | `sha256:e0d4e47397eb93c8f2b1091ff69d05656f237197d7622f1554230952c30f9260` |
| Stored JSON UTF8 checksum | `sha256:762922a19de0a65dfc311c37a7903ac0bd5885bcd2697a6ec0e179798e5c1b10` | `sha256:9c25dac8187bc80b58350fc61f6627f518894ebbc74d83945fb624698eef793b` |
| Acquisition UTC |2026-10-04T17:56:19.98109+00:00 |2026-10-05T00:07:28.8207632+00:00 |
| Ingestion/knowledge UTC |2026-10-04T17:56:20.0069441+00:00 |2026-10-05T00:07:28.8789067+00:00 |

Both receipt JSON commitments and complete snapshots equal J. No payload/OHLCV is published.
Historical cutoff `2026-10-04T14:29:13.9256374+00:00` still recomputes projection
`sha256:5d150d96d66c3c6442868d7b251a3c0b027e43e70a7a4ca9e4ec0198d6f62db6`.
This is recomputation of a documented cutoff, not an invented persisted seal-log row. No cutoff
was sealed by K. J already explains the second root's later runtime-correlated arrival; no third
receipt appeared here. Duplicate request counts cannot be recovered from idempotent receipt rows.

### Read-only method and limitations

Reused J's existing isolated diagnostic assemblies unchanged, verifying their hashes before execution:
`Inspect.dll`: `7e80d87648d361dff34f9b211b149ca90b15f07874d7c6a1be35d8ef22b6ec42`;
`RecoveryInventory.dll`: `2d25f20fd69ddce93e7d0dfbdd335f1df28c74c6873ad819ee22e1e5f5d9198e`.
Readers use SQLite read-only mode, pooling disabled, deferred read transactions, fixed SELECTs and
pure native receipt validation; no migrating owner or application startup. J documents their scope.
They ran in disposable diagnostic containers from the already present deployed image: no pull/build,
network none, read-only root/source mounts, UID1654, all capabilities dropped, no-new-privileges,
64PID/384MiB/half-CPU bounds, no healthcheck, no production environment or Docker socket.
No helper was deployed into the API. No diagnostic files were generated in Phase A.

This is live read-only evidence, not a coordinated backup or full SQLite integrity check. No
`finance-schema-status`, write-capable `MarketKnowledgeAt`, backup utility, checkpoint, VACUUM,
persistent PRAGMA or whole-store migrating constructor was executed. Native receipt checks do not
prove every historical scientific artifact or all304 referenced files' contents are intact.

## Provider rights and private retention design

This review applies recorded project policy; it does not make a new legal finding or infer a
contractual right from API/configuration availability. No provider endpoint was consulted or called.

| Data class in proposed set | Recorded rule / required execution evidence |
| --- | --- |
| Alpaca daily receipts, commitments and dependent evidence | Existing owner-accepted private/noncommercial risk decision includes research/replay/backups. Not a provider retention guarantee. Preserve termination/deletion uncertainty and stop affected use upon a concrete contradictory duty. Config reports accepted bb132e-owner-data-use-risk-v1, deletion status Unknown. |
| EODHD raw payloads, revisions and derived science | Subscription-only; all copies deletion-controlled, recorded deadline one month after verified termination/expiry. AccountActive=true and empty end-date were observed, not proof of entitlement or unlimited copying. Obtain current owner-held account/terms evidence and track every copy. Never fabricate an end-date. |
| NASDAQ-WIKI | Verified public-domain canonical subset and exact WIKI-only derivations may be retained indefinitely; classify mixed derivations separately. |
| Macro vintages/external packs | Follow exact revision/product rights; FRED disabled does not classify retained files. Per-revision copy/expiry evidence remains required. |
| Intake, quarantine, owner-drop and validation manifests | Untrusted candidates can have unknown rights; quarantine is not permission for an additional recovery copy. Classify payload and derived/validation metadata separately. Unknown copy rights block full capture pending bounded evidence/decision consistent with ADR0028. |
| Lifecycle, recovery and audit state | Private operational records; restrict access and minimize publication. Records/identifiers linked to restricted data require explicit derived-data classification. |
| Existing backups and restore staging | Nested copies inherit their source obligations; do not treat them as harmless metadata or silently omit them from an exact recovery inventory. |

The private inventory must bind recovery-set ID, relative file/DB selection, provider/product/feed,
policy version and evidence reference, source revision/derived lineage, classification, copy purpose,
creation/verification time, retention review date, applicable expiry/deletion trigger/deadline,
archive/drill/staging locations, encryption/key-custody reference and disposal audit. No secret key
or credential belongs in that inventory or Git. Record Unknown explicitly and block capture.

Agree a short purpose-bound review/expiry schedule before creating copies; do not invent a calendar
deletion deadline now. Register archive, drill and any staging before writing them. On a concrete
termination/deletion event, stop affected restore/use and obtain required deletion authorization.
For a mixed encrypted archive, deleting a constituent in place breaks its original commitment:
retire/delete the affected whole copy or create a separately permitted replacement with new manifest
and dispose of the old copy, including keys and residual drill/staging, under an auditable procedure.
Do not mutate a protected original silently, claim cryptographic erasure without proven key control,
or delete production evidence as a backup precondition. Retain only permitted non-payload audit facts.

## Proposed Phase B runbook — NOT EXECUTED

Every step marked **B approval** writes, interrupts service or changes operational authority.
Read-only checks are gates, not automatic authorization for the following step.

1. **Gate:** approve this bounded procedure, complete rights inventory, resolve private destination
   aliases locally, agree archive/drill encryption and retention/key custody. Record expected old
   image identity, volumes/bind boundaries and evidence commitments. Recheck capacity/inodes and
   source/destination symlinks, ACLs, mount identity and cleanup exclusions. Abort on unknown rights.
2. **B approval — change freeze:** pause owner-drop producers, manual maintenance/research/provider
   operations, deployments, host reboots/service starts and API clients. Drain Web/API client work.
   Backup guidance historically stops API/Web; API-only shutdown is proposed because Web is not a
   database writer, but requires explicit approval of client isolation instead of silently waiving
   that guidance. No unrelated appliance/container stop is needed. Verify host jobs/operator sessions.
3. **B approval — configuration:** atomically set observation runtime and Alpaca transport false in
   existing external configuration; preserve all unrelated entries and private0600 mode. Keep EODHD,
   FRED and ResearchScheduler false. This does not change a running process: no capture until shutdown.
   Do not recreate/start merely to apply flags. Preserve private prior config for controlled recovery,
   outside provider archive and without credential publication; never restore enabled flags by default.
4. **B approval — graceful API stop:** stop only the approved Compose API scope with accepted grace,
   allowing cancellation and lifecycle clean shutdown. Do not use broad appliance/systemd restart.
   Verify actual exit/clean lifecycle record, not just a requested stop. If grace expires, forced kill,
   uncertain commit or unhealthy shutdown occurs, STOP for recovery review; do not label a copy clean.
5. **Gate:** prove all Finance/lifecycle/owner-drop writers and open handles have stopped, including
   scanner, startup reconciliation, HTTP/CLI, macro/science/retention and backup constructors.
   Use container inventory plus scoped lsof/fuser/host checks without exposing arguments/secrets.
   Manual stop normally suppresses unless-stopped restart; monitor for restarts and keep failed enabled
   systemd unit untouched. Any restart/new writer invalidates quiescence. Verify stable inventory and
   lifecycle clean state after shutdown. Provider flags alone are insufficient.
6. **B approval — protected capture:** create exclusive private root/subdirectories with owner-only
   access. Capture complete Finance and lifecycle named volumes plus separate owner-drop and all
   referenced external files once. Include remaining WAL/SHM/journals consistently; never copy only
   the main SQLite files or manually checkpoint to make them disappear. Preserve source unchanged.
   Record private file types/relative paths/sizes/ownership/modes/hashes and DB/receipt/JSON commitments.
   Reject traversal, outside references, unresolved links, sockets/special files and scope drift.
   The current logical public-domain backup utility is not a substitute for this full recovery set.
7. **B approval — finalize protection:** use reviewed local archiving/encryption tooling and private
   interactive key entry (no command-line secrets). Hash and verify completeness before marking the
   recovery set COMPLETE. Retain a private manifest bound to ciphertext and verified plaintext entries;
   verify decryption independently. Lock finalized archive against accidental writes; record that this
   is not WORM and shares the source disk. Incomplete copies are never eligible recovery evidence.
8. **B approval — isolated drill:** create a separate destination from the archive, never hardlink it
   to source/protected copy. Permit private plaintext only if explicitly accepted; otherwise require
   approved encrypted scratch storage. No production mounts, network, credentials, Docker socket or
   application/host startup. Use exact old runtime/dependencies with a reviewed isolated validator;
   no current H owner constructor, migration or schema-status. Original archive remains unchanged.
9. **B approval for drill-local writes only; verification gate:** verify manifest/file hashes and all
   external reference commitments; full SQLite integrity and foreign-key checks on isolated copies;
   schema97, native receipts/full JSON, lineage, counts, original knowledge/watermark and historical
   projection equivalence, research grants and lifecycle clean history. No new cutoff sealing.
   Open read-only where possible; if recovery needs isolated-copy writes, authorize only that drill
   scope and preserve pre-open file hashes. Never bypass WAL via immutable-mode assumptions. Report
   raw file equivalence separately from logical equivalence after any drill-local SQLite recovery.
10. **B approval — optional controlled return:** only if explicitly included, recreate API using the
    exact already deployed old image ID, no rebuild/pull/dependencies and all five Finance gates false.
    Do not simply start the previous enabled container. Verify pinned OCI/compiled revision, health,
    schema97, clock/recovery and receipt commitments. Startup legitimately writes lifecycle and may
    reconcile state; this must be included in approval, and evidence must remain unchanged. If a
    source startup could reset grants or alter scientific evidence, stop before it and review first.
    No automatic mapping/provider activation; no H image or migration98. Leave unrelated services alone.
11. **B approval — cleanup/retention:** dispose only specifically inventoried drill/staging artifacts
    under approved expiry and verified containment; never source data. Retain the protected copy for
    its agreed period, monitor obligations, audit disposition without secrets. Publish sanitized
    equivalence evidence, actual interruption duration and remaining risks; STOP for architect review.

All destinations, tools and exact scoped commands must be resolved in a private execution manifest
before service interruption. Shell recipes with guessed production paths are deliberately not published.
No deployment/mapping/acquisition authority follows from successful capture or drill.

### Stop and rollback conditions

Abort before capture for identity/rights/capacity/permission disagreement, unknown writers, unclean
shutdown, unresolved external references or source commitment drift. Abort restore validation for
hash/lineage/schema/watermark/FK mismatch; retain failed artifact privately under the same rights
policy and report, do not repair production. No rollback by overwriting a healthy source is approved.
If returning service is unsafe, keep it stopped under the separately agreed containment decision.

Service stop/config changes are reversible only via an authorized controlled restart; lifecycle
shutdown/start writes are real durable changes. Copy disclosure and deletion can be irreversible;
local plaintext remnants are a limitation. Restoring an old recovery set after later writes would
lose evidence/knowledge and cannot be an automatic rollback. Migration98 and subsequent H bindings
would require separate rollback analysis/approval; Phase B neither applies nor tests that migration.

## Changes and verification

Repository delta is this report, one catalog entry and one short recovery link. Production code,
tests, configuration, schemas and canonical scientific contracts are unchanged. Existing untracked
mockups/ADR0006–0009 and ignored diagnostic/operational files remain untouched and excluded.

Publication checks:

- `node scripts/verify-documentation.mjs`: PASS,267 Markdown files/91 unique backlog IDs.
  Initial invocation reported two missing required section headings; headings corrected, check rerun green.
- `git diff --check` and `git diff --cached --check`: PASS.
- Staged file inventory: exactly the three documentation paths above; no production/test/config delta.
- `gitleaks git --pre-commit --staged --redact --no-banner`: PASS, no leaks.
- Manual publication review: source/receipt hashes checked against J; no private paths, credentials,
  raw payload or unapproved operational command execution included.

Application/Finance/Sentinel/frontend suites and builds are not run: no implementation changed.
No source-backup integrity/restore test is claimed; those require Phase B. The two reused read-only
inventory passes succeeded; full receipt equality against J succeeded, including raw JSON hashes.

## Security

No credentials, auth headers, private absolute paths, raw provider logs/payloads or OHLCV are
published. No backup/restore, service interruption, config/permission change, production data write,
provider request or cycle trigger occurred. No model, research grant, SHADOW, PAPER/LIVE/AUTO,
broker/order/position/capital action. Finance remains **RESEARCH / 0 SEK / NONE**.

## Remaining work

Phase A planning/inspection is complete; Phase B remains blocked by the explicit rights/protection
and interruption approvals above, plus execution-window gates.

## Resumption

Independent architect review of this
exact checkpoint is next. Main remains unchanged; no merge is requested. Read this report and the
linked J inventory before any later action. Owner storage selection is not execution approval.
