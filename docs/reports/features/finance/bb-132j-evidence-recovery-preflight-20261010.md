# BB-132J — Finance evidence and recovery preflight

## Metadata

- Date: 2026-10-10. Scope: read-only deployed evidence, writer inventory and future recovery design.
- Accepted main: `618f025a3cb72ee4513a9f14b713382e4a673a42`.
- Accepted tree: `72649fe9c5dd9fb7c2bb7fe0ed0fa0dd9ac06547`.
- Review branch: `review/finance-bb132j-evidence-recovery-preflight`.
- Publication subject: `review: publish BB-132J Finance evidence and recovery preflight`.
  Exact publication commit/tree resolve from that unique subject on the branch; no self-referential SHA.
- [BB-132I reviewed evidence at exact commit 4eaf9ce](https://github.com/T-bear/BigBrain/blob/4eaf9cef6388af29d9509c71bad93ab351b4f474/docs/reports/features/finance/bb-132i-operational-readiness-20261010.md).
  Neither I nor another review branch is merged into this branch.
- Authority inspected: AGENTS, START-HERE, STATUS/BACKLOG, [H report](bb-132h-versioned-observation-mapping-20261010.md),
  [F1 runbook](../../../operations/runbooks/finance-controlled-acquisition.md),
  [backup/restore guidance](../../../operations/backup-restore/README.md),
  [ADR0028](../../../adr/0028-finance-provider-tagged-backup-restore-and-cleanup.md) and relevant code.

## Status and decision

**REVIEW CHECKPOINT — NOT A MERGE CANDIDATE. READY FOR CONTROLLED BACKUP PREPARATION.**

This means the architect can now scope a separately authorized, protected recovery-copy procedure.
It does **not** mean a backup exists, restoration has passed, all-provider retention has been approved,
or that backup execution, stopping services, H deployment, migration98, mapping adoption or acquisition
is authorized. Prerequisites and separate approval gates are listed below.

The unidentified second receipt is resolved: a distinct AAPL source day, October 1, acquired after the
historical one-receipt handoff. Both receipts have identical complete historical mapping provenance;
neither is a revision or corruption. H must preserve **both**, not just the manually acquired October 2
receipt. Physical schema inventory confirms version97 and absent H mapping tables.

No production change was made. The accepted H implementation remains undeployed. Receipt integrity is
freshly verified; full 8.94-GB database integrity and isolated restore are **not tested** in this review.
Older STATUS/BACKLOG candidate labels are dated implementation evidence; Git establishes H acceptance.

## Evidence — safe read-only method

Host-user access to the backing Docker volume was unavailable in I. J used the explicitly permitted
small isolated diagnostic alternative: locally compiled BCL-only C# readers, executed as disposable
diagnostics from the **already present exact deployed image**, not inside the production API container.
No image was built/pulled; no service was deployed, stopped, recreated or restarted.

The diagnostic container had `--network=none`, `--read-only`, `--cap-drop=ALL`,
`--security-opt=no-new-privileges`, UID/GID1654, no healthcheck, 64-process/384-MiB/0.5-CPU limits.
Only required evidence volumes and diagnostic binaries were mounted **read-only**. It inherited no
production environment or credentials and had no Docker socket. Existing production files/permissions
were not modified. The local ignored diagnostics and sanitized working evidence are not publication input.

The entrypoint was the diagnostic DLL, never `BigBrain.Api.dll` Main. SQLite connections used
`Mode=ReadOnly;Pooling=False` and deferred read transactions. SQL was fixed `SELECT` only. The receipt
reader loaded the deployed assemblies to call the existing static `ReadObservationReceipts` validator;
it never constructed `EodhdMarketMemory`, recovery, a backup store, or a migrating owner. This validates
all receipt envelopes, content/full hashes, identifiers, timestamps, instrument/source eligibility and
ordered predecessor lineage. Stored JSON was held in memory only; only approved metadata and its UTF-8
byte commitment were emitted. No raw OHLCV or provider payload was exported.

Representative executed queries:

```sql
SELECT version,name,applied_utc FROM finance_schema_migrations ORDER BY version;
SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;
SELECT id,receipt_json FROM market_observation_receipts ORDER BY ingested_ticks,id;
SELECT watermark_ticks FROM market_observation_clock WHERE singleton=1;
SELECT id,candidate_id,cutoff_ticks,checksum FROM prospective_daily_results ORDER BY cutoff_ticks,id;
```

Additional bounded counts/existence queries covered H tables, daily candidates/results, payload file
references, historical provider presence and lifecycle tables. Native receipt validation and read
transactions used the same deployed schema97 code; no H code was run against production data.
No backup API, snapshot, checkpoint, VACUUM, persistent PRAGMA, quick-check/repair command,
`finance-schema-status`, or write-capable `MarketKnowledgeAt` was executed.

## Complete retained receipt inventory

Two successful independent opens at `2026-10-10T14:31:25.4255237+00:00` and
`2026-10-10T14:34:50.9389297+00:00` returned identical complete sanitized receipt records,
raw stored JSON commitments and watermark. This verifies reopen, not a production restart or restore.

### Receipt 1 — source day 2026-10-02

| Field | Persisted / verified fact |
| --- | --- |
| Receipt ID | `sha256:caa655557878353e9d700ec84c0c696777fcbf52ee08a63b07f72a99e3a5d11c` |
| Full checksum | `sha256:13d6426177e0553e1c6ca86c856d0d814695da5030bc56d9c4dea404d8e9637d` |
| Logical identity | `sha256:64d7c52f7c4de0fabaee526f3363e30ddc5b02397330b5a812bd7dbf072d12ed` |
| Stored receipt JSON UTF-8 SHA-256 | `sha256:762922a19de0a65dfc311c37a7903ac0bd5885bcd2697a6ec0e179798e5c1b10` |
| Instrument | `US:XNAS:AAPL` |
| Provider / dataset | `Alpaca` / `iex-historical-1Day-raw` |
| Origin / classification | `AcquiredProviderDaily` / New chain root |
| Predecessor | None |
| Event/source timestamp | `2026-10-02T04:00:00+00:00` |
| Provider publication/availability | Unknown (`null`) |
| Acquired | `2026-10-04T17:56:19.98109+00:00` |
| Ingested / knowledge time | `2026-10-04T17:56:20.0069441+00:00` |
| Native envelope, checksum and lineage | PASS individually and as the complete ordered set |
| Cutoff one tick before ingestion / at ingestion | Excluded / included |

### Receipt 2 — source day 2026-10-01

| Field | Persisted / verified fact |
| --- | --- |
| Receipt ID | `sha256:dfc00b55e068d54947f84c0a15ccb5c4bc9a4bdb3a8fd6aaef86580413ec5677` |
| Full checksum | `sha256:e0d4e47397eb93c8f2b1091ff69d05656f237197d7622f1554230952c30f9260` |
| Logical identity | `sha256:946fa56fbacc5683637efa242330bea9326b4c9655e7d2e44c50e4d198305dc7` |
| Stored receipt JSON UTF-8 SHA-256 | `sha256:9c25dac8187bc80b58350fc61f6627f518894ebbc74d83945fb624698eef793b` |
| Instrument | `US:XNAS:AAPL` |
| Provider / dataset | `Alpaca` / `iex-historical-1Day-raw` |
| Origin / classification | `AcquiredProviderDaily` / New chain root |
| Predecessor | None |
| Event/source timestamp | `2026-10-01T04:00:00+00:00` |
| Provider publication/availability | Unknown (`null`) |
| Acquired | `2026-10-05T00:07:28.8207632+00:00` |
| Ingested / knowledge time | `2026-10-05T00:07:28.8789067+00:00` |
| Native envelope, checksum and lineage | PASS individually and as the complete ordered set |
| Cutoff one tick before ingestion / at ingestion | Excluded / included |

“New chain root” is established from distinct logical identity and null predecessor, not a replay of an
acquisition command. Duplicate attempts do not add receipt rows; this inventory cannot count all such
attempts. There are zero retained revision edges and no missing/branching predecessors in this set.

### Exact shared instrument and mapping snapshot

Both receipts carry the same complete snapshots; comparison of all stored fields passed:

| Canonical instrument field | Value |
| --- | --- |
| ID / display name | `US:XNAS:AAPL` / `Apple Inc.` |
| Type / lifecycle | Equity / Active (persisted enum values 1 / 1) |
| Currency | USD |
| MIC / venue code | XNAS / XNAS |
| Canonical venue name | `THE NASDAQ STOCK MARKET LLC` |
| Effective start / inclusive end | 2026-01-29 / 2026-10-02 |

| Provider mapping field | Value |
| --- | --- |
| Instrument ID | `US:XNAS:AAPL` |
| Provider / dataset / symbol | Alpaca / `iex-historical-1Day-raw` / AAPL |
| MIC / venue code / venue name | XNAS / XNAS / `THE NASDAQ STOCK MARKET LLC` |
| Effective start / inclusive end | 2026-01-29 / 2026-10-02 |
| Evidence reference | `operator:apple-sec-0000320193-26-000005-faq-20261004-alpaca-aapl` |

The uppercase venue name is the actual canonical stored value, not a new mapping change. IEX is the
feed, not the listing venue. These snapshots match the historical receipt commitment from the accepted
operational evidence. No extra canonical instrument or alternative mapping snapshot is present among
retained observation receipts.

Both envelopes use `finance-market-observation-daily-v1`, adapter `alpaca-daily-observation-v1`, daily
eligibility `alpaca-completed-ny-day-v1`, timezone `America/New_York`, source contract
`alpaca-historical-stocks-1Day-raw-v2`, and symbol policy `asof-disabled-effective-mapping`.
Each passed actual daily source-date and canonical mapping validation at its persisted acquisition time.
This is stored provenance/integrity evidence, not a fresh Alpaca authentication or payload refetch.

### Why one became two

The historical handoff on October 4 correctly reported the first manual October 2 receipt. The new
October 1 receipt records later ingestion at `2026-10-05T00:07:28.8789067+00:00`. It does not modify the
first receipt and cannot be visible at the earlier handoff's cutoff.

A sanitized existing log records `Finance observation cycle: Healthy` at
`2026-10-05T00:07:35.016405825Z`. The deployed runtime waits six hours after startup, uses a three-day
lookback and scans prior source days oldest first. At this UTC time the New York date was October 4;
October 1 and 2 were eligible sessions within the historical mapping, October 3 was not a session.
The recorded ingestion shortly before that cycle completion therefore supports attribution to the
already authorized unattended runtime. The receipt itself does not encode caller/process identity;
this attribution is a correlation with code/configuration/log timing, not a fabricated invocation ID
or complete request audit. Exact duplicates/reacquisition counts cannot be reconstructed from receipts.
No new request was made or cycle triggered by J.

## Database and temporal inventory

| Item | Fresh read-only result |
| --- | --- |
| SQLite engine in diagnostic/deployed image | 3.53.3 |
| Applied Finance versions | 1,90,91,92,93,94,95,96,97; maximum97 |
| Migration97 applied UTC | `2026-10-04T13:28:19.8229631+00:00` |
| SQLite table catalog | 52 tables |
| `market_observation_receipts` | 2 |
| `observation_mapping_manifests` / `observation_mapping_receipts` | Both physically ABSENT |
| `prospective_daily_candidates` / `prospective_daily_results` | 0 / 0 |
| Observation watermark ticks | `639267556488789067` |
| Watermark UTC | `2026-10-05T00:07:28.8789067+00:00` |
| Watermark integrity | Equal to maximum retained ingestion; not ahead of current trusted check time |
| Revisions / predecessor referential integrity | No revision edges; both roots validated; no missing predecessor |
| Historical cutoff recomputed | `2026-10-04T14:29:13.9256374+00:00` |
| Historical projection commitment | `sha256:5d150d96d66c3c6442868d7b251a3c0b027e43e70a7a4ca9e4ec0198d6f62db6` — unchanged from I/earlier handoff |

Important persistence limit: `MarketKnowledgeAt` advances a maximum watermark and returns a projection;
it does **not** persist a separate log of every cutoff/projection pair. J did not seal a cutoff. The
specific historical cutoff above is a published historical reference, now recomputed from persisted
receipts. The watermark alone cannot prove every past seal call. Daily shadow candidate/result tables
would retain their respective commitments, but are empty. No missing audit rows are invented.

Future H adoption must retain both receipts byte-for-byte and their single identical historical snapshot.
Neither today's mapping authority nor a new manifest may backdate their knowledge. A separately current
assertion is needed for later acquisition, even if historical assertions remain readable after expiry.
SHADOW v1 mixed-provenance refusal remains unchanged; no scientific compatibility grant follows.

## Live storage, deployment and writer evidence

API image remains `sha256:7dd3126240841269096b2027cbc0610be4b9f81fb097f0e0df9fd4097b184f8a`,
OCI revision `a2373f172600c7b45ce378c56aab1d7da2b9b242`, predating H. The running container was not
replaced. Docker health is healthy; aggregate health returns **HTTP503 / Degraded**. Recovery snapshot
is healthy/completed/RESEARCH with its last integrity check still dated October 4. Host NTP is synchronized.
This is not new proof of whole-database integrity or healthy acquisition for unsupported dates.

Observation runtime and Alpaca transport remain enabled; cadence360 minutes/lookback3; EODHD, FRED and
Research Scheduler remain disabled. Only the API container currently mounts the Finance/lifecycle
volumes, both writable in production. Its process listing showed one dotnet process. No visible
BigBrain/Finance-named system timer was found. Arbitrarily named privileged host jobs and external
operator sessions were not exhaustively audited; absence of such writers must be verified at quiescence.
No full environments or process command arguments were printed.

| Storage item | Observed size / state |
| --- | --- |
| Finance main SQLite | 8,937,451,520 bytes |
| Finance WAL / SHM | 32,992 / 32,768 bytes; both present |
| Lifecycle SQLite | 90,112 bytes |
| Lifecycle WAL / SHM | 32,992 / 32,768 bytes; both present |
| Database/companion ownership | UID/GID1654; mode0644 |
| Finance mounted tree, apparent bytes | 14,279,279,623, including nested owner-drop view |
| Lifecycle mounted tree, apparent bytes | 155,872 |
| Separate owner-drop bind, apparent bytes | 5,443,665; must be inventoried separately rather than assumed part of named-volume copy |
| Host filesystem available bytes | 456,171,458,560 at inspection; destination-specific capacity remains a gate |
| Legacy payload references | 304; all304 referenced files present within Finance scope, zero missing/outside paths |
| Historical provider presence | Both EODHD and NASDAQ-WIKI confirmed, separately from the two Alpaca receipts |

Presence is not a checksum audit of those304 payloads. Candidate/quarantine and macro file completeness
also need a future complete file manifest and reference check. No raw payload was read or copied for J.
At `2026-10-10T14:35:56.3777981+00:00`, lifecycle held82 sessions/318 events; its latest session started
`2026-10-04T18:07:24.3112689+00:00`, `clean=0`, `clean_utc=null`. This is expected for a running process,
not evidence of a crash. Shutdown later writes its clean marker/events, so capture lifecycle **after**
confirmed graceful quiescence. Separate Finance/lifecycle read transactions here are not a cross-store backup.

### Writer inventory and required quiescence

All paths below are repository-relative. Runtime enablement is distinguished from implemented capability.

| Writer family | Activation and durable scope | Quiescence requirement |
| --- | --- | --- |
| Observation worker / daily adapter | `FinanceObservationRuntime.cs`, `FinanceMarketObservations.cs`; enabled runtime, recovery/currentness/transport gates; receipts and clock, H manifest/bindings after deployment | Disable under separate approval, drain/cancel owned cycle, stop API; no maintenance replacement |
| EODHD cadence and derived workers | `FinanceProspectiveCadence.cs`, feature/backtest/robustness stores; EODHD enabled/account/recovery gates; acquisitions, payloads, revisions, derived science, legacy shadow/risk/cadence | Keep EODHD false, drain and stop API; disabled currently |
| Startup owner initialization | `EodhdFinanceMarketData.cs`, `FinanceSchemaMigrations.cs`; DI construction runs migrations, DDL/WAL setup and interrupted acquisition/research/schedule reconciliation | Do not start any API/maintenance host while capture occurs; disabled provider flags are insufficient |
| Owner-drop scanner / intake / promotion | `FinanceOwnerDatasetDrop.cs`, dataset intake/quarantine/research-dataset/campaign stores; scans ready files periodically without observation enablement; files and shared DB | Stop API scanner; stop operator drop-file changes or capture/reconcile separate intake area |
| Scheduled research / operations | `FinanceResearchScheduler.cs`, `FinanceResearchOperations.cs`, autonomous research store; scheduler gate false, but startup reconciliation is independent | Stop API and forbid explicit research calls; do not reset grants or pending state |
| Scientific/ledger/risk methods | Finite session, learning ledger, backtest persistence, daily shadow, legacy shadow and risk stores; explicit in-process callers/maintenance and accepted governance | No invocation; stop API/other consumers; include these tables in recovery, not just observations |
| Macro owner | `FinanceMacroMemory.cs`; owner construction migrates; explicit pack/vintage/promotion and research paths can write database/quarantine | Keep FRED false, forbid maintenance/import paths, stop API; flag alone does not disable constructors |
| HTTP routes / lazy readers | `FinanceEndpoints.cs`: explicit research POST plus GET services that resolve owners or backup constructors; some status paths can reconcile/initialize indirectly | Drain/block clients then stop API; HTTP GET is not a universal no-write guarantee |
| Maintenance processes | One-shot Alpaca; schema-status/adjusted-audit; intake/drop/promote/research; feature/backtest/robustness; macro; retention deletion; backup/restore/cleanup commands | No command may run during capture; inspect processes/open files, do not rely only on hosted flags |
| Backup/protection constructor | `FinanceDataProtection.cs`: creates directories/removes incomplete staging even before some inventory operations | Do not instantiate during inspection/capture; existing public-domain utility is not a full recovery copier |
| Lifecycle/recovery | `SystemRecovery.cs`: constructor/session, startup journal/probes, shutdown clean marker/events; lifecycle DB and recovery probe files | Graceful API shutdown must finish, then verify no file handles; don't create a second lifecycle coordinator |
| Host/container operators and intake producers | Compose/systemd/manual commands or external file producers; only API currently observed with DB mounts | Explicit change freeze; prevent auto-restart/redeployment and concurrent CLI work; verify all writers, not just named timers |

The current failed `bigbrain.service` was not repaired. It represents another potential lifecycle actor;
using its broad appliance stop/start as a shortcut is inappropriate. The preferred future change is
API-scoped quiescence with UI/API client access drained; existing backup guidance also calls for Web
quiescence. Decide that bounded service scope explicitly, without stopping unrelated media services.

## Proposed recovery-copy design — no execution authorization

### Method comparison

| Method | Capability and limitation | Assessment |
| --- | --- | --- |
| SQLite Online Backup API | Consistent SQLite database copy while source may be live; separate database destinations and external files are not one atomic recovery set | Feasible later with reviewed tool and controlled writers; does not by itself solve Finance/lifecycle/file coordination |
| Fully quiesced filesystem copy | Captures DB plus whatever WAL/SHM/journals remain, external evidence and lifecycle after all writers close | Preferred first recovery procedure here; no established filesystem snapshot facility was verified |
| Atomic filesystem snapshot | Useful only if actual storage supports it and all required mounts share a coordinated point | Not established; no claim that Docker volumes supply atomic snapshots |
| Existing `finance-backup-*` | Provider-scoped logical public-domain subset and staging verification; constructor also mutates staging | Insufficient for exact full Alpaca/Finance/lifecycle recovery; do not substitute its success for full restoration |

SQLite's [Online Backup API](https://www.sqlite.org/backup.html) describes a consistent database copy;
it is not a multi-database/filesystem transaction. SQLite's [WAL documentation](https://www.sqlite.org/wal.html)
explains persistent WAL state and read-only opening with existing companions. Its
[corruption guidance](https://www.sqlite.org/howtocorrupt.html) warns against copying during a live
transaction or separating a database from required journal state. Official pages consulted2026-10-10;
no provider API used. No journal-mode change or checkpoint is proposed as an unapproved shortcut.

### Preconditions, capture and isolated verification

1. Owner authorizes **only** protected recovery preparation/capture/drill, explicit service downtime and
   private destination. Resolve rights for every retained provider class. EODHD is actually present;
   deployed account-active is true with no configured termination date, which is operator configuration,
   not fresh provider-account verification. ADR0028 and the existing pre-migration exception require
   restricted EODHD copies to join deletion/expiry inventory. Alpaca owner acceptance does not grant
   blanket indefinite retention of all Finance data. Unknown rights fail closed; never delete source
   evidence to make a backup eligible. No retention/deletion was performed here.
2. Verify destination ownership/access, encryption/access policy and failure-domain suitability; verify
   capacity on that destination. Current mounted scopes total approximately14.28GB before manifests and
   growth. An uncompressed capture plus a separate drill copy needs at least twice the finalized source
   size, plus temporary/WAL/growth reserve. Roughly28.56GB is only an observed-size lower bound, not a
   space reservation. Do not promise compression savings or duration. Current host free space is ample
   relative to that bound but does not establish off-host durability or destination access.
3. Under that approval, freeze operator jobs/clients/drop producers, disable acquisition gates without
   broadening authority, and gracefully stop only the approved API/Web scope. Wait for cancellation,
   completed shutdown journaling and process exit; verify open handles and other mounts/writers are gone.
   A forced termination or automatic restart changes the plan: preserve crash state and STOP for review.
   Services have not been stopped by J.
4. Capture the full Finance named-volume content, required separately mounted intake artifacts and
   lifecycle scope at this quiescent point. Inventory symlinks/paths safely, numeric ownership/modes,
   sizes and hashes; preserve DB/WAL/SHM/journal files exactly as found. Do not delete companions or copy
   only the main live SQLite file. If graceful close already checkpoints naturally, capture that resulting
   state and record it; do not manufacture a clean marker. Include private runtime/build configuration
   provenance separately with restricted access, never in Git or ordinary evidence output.
5. Privileges: Docker inspection/read-only diagnostics worked without changing permissions. Future
   stop/copy/restore requires separately approved Docker/service and destination-writing authority,
   plus ability to preserve UID/GID1654 and protected ownership. Do not chmod production files to make
   a tool work. Record every recovery copy in applicable retention inventory before calling it complete.
6. Verify source-to-copy file hashes and sizes while quiescent. Preserve both receipts' exact stored JSON
   bytes and native commitments; schema rows, watermark, all scientific/ledger/grant state, lineage,
   lifecycle journal and referenced external files must be covered. File checksums alone are not proof
   of database consistency. Keep the original recovery copy immutable/access-protected; do not open it
   through application startup.
7. Restore a **second** copy into isolated disposable storage with no production mounts, no credentials,
   no network and no application host/worker startup. Use direct SQLite readers, matching engine/code,
   whole-database `integrity_check`/foreign-key verification on this isolated copy, domain integrity and
   all reference/file commitments. These checks are future work, not executed now. Validate schema97,
   identical receipt bytes/checksums, predecessor chains, mapping snapshots, watermark, historical and
   receipt-boundary projections, all native result/ledger identities and lifecycle content. Prevent
   regeneration, grant reset, provider calls or silent migration from masking a mismatch.
8. Record measured capture/verification time, complete manifest and restore results privately/sanitized
   before accepting the recovery set. Downtime cannot be estimated from free space; it depends on measured
   read/copy/hash/verification throughput and agreed point for resuming the old approved revision. A new
   predeployment quiescent capture is needed if writes resume before the later H migration.

The finalized copy must retain external artifacts, not just the two observation envelopes. All304
legacy payload paths resolved to existing Finance files, but their hashes and quarantine/macro references
remain part of the future complete verification. The current read-only diagnostics are not a backup.

### Migration and rollback limits

H startup may apply migration98 before any provider request even with runtime disabled. It adds manifest
and binding tables transactionally; legacy receipts must remain byte-identical. Owner initialization and
lifecycle startup also write independently, so compare allowed operational changes separately from
immutable evidence. Deployment requires a new explicit approval, exact SHA/tree build and verified recovery
set; J does not build or deploy.

There is no accepted schema98 down migration. The old binary expects schema97 for observation work;
image rollback alone is not a proven rollback. Do not drop tables or edit migration rows. A full approved
predeployment restore with all writers stopped may recover the old state, but any subsequent receipts,
assertions, seals, grants or lifecycle/scientific changes must first be preserved and reconciled. Blind
restore would erase knowledge/history. Requests and past trusted knowledge cannot be undone or backdated.
After new durable evidence, STOP for a separate recovery decision instead of silently restoring stale data.

## Deployment-readiness dependency gates

| Gate | State / next evidence or authorization |
| --- | --- |
| A — Facts verified in J | Exact baseline/tree; every observation identified/validated; two identical snapshots; schema97; H tables absent; watermark/projection stable; writer and mounted storage inventory |
| B — Evidence still missing | Full DB/FK integrity; complete artifact checksums/references; private destination/retention plan and capacity; exhaustive privileged writer/open-handle check at quiescence; actual restorable copy and measured drill |
| C — Owner approval before backup | Approve protected mixed-provider copy/retention inventory, explicit API/Web quiescence, destination privileges, isolated restore drill and restart boundaries. No authority yet |
| D — Owner approval before H deployment | Verified recovery set, exact accepted build identity, disabled acquisition, approved startup/migration98 and postmigration byte/schema/watermark checks. Separate from C |
| E — Mapping assertion/adoption | Actual reviewed historical/current verification evidence and dates; preserve both old receipts/snapshot; selected current assertion <=7days; no invented validity or cross-version SHADOW grant |
| F — Provider acquisition | Separate explicit bounded request or runtime activation approval only after D/E pass; no authority from backup or migration |

Current degraded observation health and expired source-date coverage remain known limitations. No new
mapping or current listing verification was performed. No corruption or need to change H implementation
was established by this investigation.

## Changes and verification

Only this report, report catalog and the short recovery handoff change. Local isolated diagnostic
source/binaries remain ignored and are not production/test implementation. Prior review branches and
unrelated local mockups/ADR drafts remain unchanged and excluded.

Executed: Git fetch/identity checks; local diagnostic compilation with installed .NET SDK (major10, minor0, patch302) csc
(BCL references only); isolated read-only SELECT/native receipt validation; selected Docker metadata,
mount/process and fixed-category historical log inspection; file metadata/size/presence checks; safe
health/recovery GETs; NTP and visible named-timer inspection; accepted code/runbook analysis.

Results: complete receipt inventory/lineage/reopen PASS (2/2); matching original receipt commitment PASS;
physical schema/table/watermark inventory PASS; historical projection and membership PASS; payload
presence304/304; lifecycle inventory PASS. Supplemental grouped historical-provider queries failed with
sanitized SqliteException (one recorded SQLITE_IOERR code10); those broad counts are **not verified**.
Bounded provider-existence queries subsequently succeeded without changing permissions, mounts or data.
No cause of that broad-query failure or whole-database corruption is inferred. A process-name-only
Docker query initially failed because Docker required PID; corrected PID/name listing succeeded.
Aggregate health503/Degraded is reported as observed, not as PASS.

No API/Finance/frontend/Sentinel suites or solution build were run: repository delta is documentation,
not implementation. Compiling a diagnostic is not application test evidence. No model/provider/network
dependency was introduced into CI. Publication checks: documentation verifier, diff/scope and staged
Gitleaks; exact results recorded below after execution.

## Security

Detta är en sanerad GitHub-version. No credentials or credential fingerprints, raw OHLCV/provider payload,
private filesystem paths, full environments, unrelated host secrets or production configuration contents
are published. Receipt/file commitments shown are explicitly authorized non-secret evidence identities.
Only approved metadata was retained in diagnostic output.

No merge, production deployment/image rebuild, service stop/restart, production filesystem/database
write, backup/restore/snapshot/checkpoint/VACUUM/migration, persistent PRAGMA, permission/configuration
change, provider request/cycle trigger, assertion/validity extension, SHADOW/Qwen/research/grant action,
PAPER/LIVE/AUTO, broker, orders, positions or capital action occurred. Disposable diagnostic containers
were removed on exit; the live API and its enabled bounded runtime were untouched. This is not an assertion
that no pre-existing background process ran. Finance remains **RESEARCH / 0 SEK / NONE**.

## Remaining work

Gate B/C recovery prerequisites and separate D/E/F approvals remain. Technical receipt-inventory gaps
from I are resolved; a complete, restorable Finance recovery copy has deliberately not been created.
The provider-specific retention boundary and live writer freeze cannot be waived for convenience.

## Resumption

Independent architect review of this exact REVIEW CHECKPOINT comes next. Recommended next owner decision is
a bounded protected recovery-copy and isolated restore drill, with destination/retention and service scope
reviewed first, no H deployment or provider request. Follow the gate order above; do not reuse J's date
or source inventory as a future point-in-time capture without revalidation. STOP after publication.

### Publication checks

Initial documentation verification rejected the dotted SDK version as a private-address pattern.
The version is now expressed as major/minor/patch without changing its identity.

- `node scripts/verify-documentation.mjs`: PASS, 267 Markdown files / 91 unique backlog IDs.
- `git diff --check` and `git diff --cached --check`: PASS.
- Staged `gitleaks git --pre-commit --staged --redact --no-banner`: PASS, no leaks.
- Scope: exactly this report, report catalog and recovery handoff; no production/test/config files.
- Fetched main SHA/tree still equal the exact accepted baseline.
- No application test/build or live backup/restore evidence is claimed.
