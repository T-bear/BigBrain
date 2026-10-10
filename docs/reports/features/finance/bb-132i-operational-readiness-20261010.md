# BB-132I — Finance operational readiness review

## Metadata

- Date/scope: 2026-10-10; read-only Debian host, deployed Finance evidence and accepted BB-132H adoption analysis.
- Accepted baseline: `618f025a3cb72ee4513a9f14b713382e4a673a42`.
- Baseline tree: `72649fe9c5dd9fb7c2bb7fe0ed0fa0dd9ac06547`.
- Branch: `review/finance-bb132i-operational-readiness`.
- Publication identifier: unique subject `review: publish BB-132I Finance operational readiness`.
  Resolve its commit/tree from this branch's Git metadata; no self-referential hash is embedded here.
- Inputs: [H report](bb-132h-versioned-observation-mapping-20261010.md),
  [controlled acquisition runbook](../../../operations/runbooks/finance-controlled-acquisition.md),
  [Finance provenance architecture](../../../architecture/finance/market-data-memory-and-provenance.md),
  STATUS, BACKLOG and accepted implementation. Historical operational handoff is commit
  `2df263a8cc11bd5837ea205244ffacefa184cec9` on `review/finance-live-observation-activation`.

## Status and decision

**REVIEW CHECKPOINT — NOT A MERGE CANDIDATE. STOP / NO-GO for immediate deployment or mapping adoption.**

BB-132H is accepted in Git, but is not deployed on the inspected API. Its additive contract can
preserve legacy evidence; this investigation did not migrate the live database or prove a live
postmigration byte comparison. A separately approved deployment requires the gates below, especially
complete receipt inventory, a verified full recovery copy and quiescence of all Finance writers.
No current versioned mapping assertion was found in effective configuration. No authority for later
AAPL source days was established.

The host is accessible and native read-only receipt validation succeeded. SQL inventory is partial:
the existing read-only helper exposes receipt count and selected receipt integrity, but not every
receipt's metadata or the physical table catalog. Missing facts are not treated as successful checks.

The baseline is the owner-approved H merge of candidate `bd271824bec656d302a289b3f3f053b73013bf57`,
with the exact candidate tree. [Main CI 38041035642](https://github.com/T-bear/BigBrain/actions/runs/38041035642)
is the prior successful acceptance evidence. Dated candidate wording in older reports/status is
historical; it does not supersede the accepted Git identity. This review does not rewrite those reports.

## Evidence — currently observed host facts

Observations are dated 2026-10-10, not an atomic snapshot of an idle host. Existing background work
was left untouched. Sanitized inspection used selected Docker metadata, allowlisted configuration
fields, health/recovery reads, fixed-category log extraction and the existing read-only diagnostic.
No full environments, raw logs or provider payloads are published.

| Surface | Verified observation | Limit |
| --- | --- | --- |
| Deployment | Docker Compose project `bigbrain-sprint1`; Debian 13 | No deployment operation performed |
| API | Running, Docker health `healthy` | Aggregate system health separately reports `Degraded` |
| API image | `sha256:7dd3126240841269096b2027cbc0610be4b9f81fb097f0e0df9fd4097b184f8a` | Exact currently running image, not an H build |
| OCI and compiled revision | Both `a2373f172600c7b45ce378c56aab1d7da2b9b242` | Predates H; compiled identity obtained with early-exit `system-build-identity` |
| Other Compose services | Web, Sentinel, Librarr and FlareSolverr healthy; Audiobookshelf running without healthcheck | No independent application verification or changes to these services |
| Service manager | `bigbrain.service` failed | Observed only; Compose API is running; no repair |
| Recovery endpoint | Healthy, completed; clock synchronized, low-disk false; operating mode RESEARCH | Last reported integrity check `2026-10-04T18:07:24.9869175+00:00`; not a fresh full-database check |
| Clock/disk | Host NTP synchronized; container clock-sync marker present; approximately 425 GiB free | Current prerequisite indicators, not a proof against all possible clock faults |
| Storage | Finance and lifecycle stores present in existing Compose volumes; owner/group 1654, mode 0644 | Private backing paths omitted |
| Finance database | 8,937,451,520 bytes; WAL and SHM companions present | Live file size; not a consistent backup |

Docker health success is compatible with the aggregate `Degraded` result; do not equate HTTP/container
liveness with all Finance prerequisites being healthy.

### Effective operational configuration and scheduling

| Setting | Observed value |
| --- | --- |
| Observation runtime | Enabled |
| Cadence / lookback | 360 minutes / 3 days |
| Alpaca daily transport | Enabled; timeout 15 seconds |
| Universe | Exactly `US:XNAS:AAPL`; legacy flat mapping form |
| EODHD / FRED / Research Scheduler | Disabled / disabled / disabled |
| Alpaca credential presence | Key present, secret present; values never printed; format/authentication not retested |
| Owner data-use acceptance | `bb132e-owner-data-use-risk-v1`; recorded `2026-10-04T14:12:06.714712+00:00` |
| Affected use / deletion state | Enabled / Unknown |

The accepted owner decision is not a provider contractual guarantee. No new provider rights or
entitlement verification was performed. No authenticated provider endpoint was contacted by this review.

Current mapping is Apple Inc., Equity/USD, canonical `US:XNAS:AAPL`, symbol AAPL, listing MIC and venue
code XNAS, venue name The Nasdaq Stock Market LLC, effective `2026-01-29` through `2026-10-02` inclusive.
Its evidence reference is `operator:apple-sec-0000320193-26-000005-faq-20261004-alpaca-aapl`.
IEX remains the feed, not the listing venue. No versioned verification/revalidation assertion is configured.

Filtered logs contained eight `Degraded` observation-cycle results in the inspected 48-hour window;
the latest three were `2026-10-10T00:07:43.328807665Z`, `06:07:43.332414304Z` and
`12:07:43.948469477Z`. This establishes worker activity and approximate six-hour cadence, not the
individual failure category or a complete provider-request audit. No cycle was triggered here.
For New York date 2026-10-10, the three prior calendar days are October 7–9, all outside the configured
mapping. Accepted code rejects such source days before network acquisition. This explains a concrete
configuration limitation; the logs alone do not identify every historical cycle's cause.

EODHD-gated feature/backtest/robustness/shadow workers cannot acquire that authority while EODHD is
false. Nevertheless, disabled acquisition flags do not imply a write-free API: owner-drop scanning
and startup research-operations reconciliation have separate responsibilities. Their complete live
activity was not audited. A future consistent backup must quiesce all writers, not just acquisition.

## Evidence — read-only Finance integrity

Direct host-user access to the Docker volume was denied. No privilege escalation or permission change
was made. The container has no installed SQLite CLI/Python. An existing private diagnostic helper was
available; its source was inspected and its local/deployed binary identities compared successfully
before execution. No helper was built, copied into the container or modified by this review.

The helper was invoked with a historical cutoff and the reported receipt ID. It loads the deployed
assemblies without starting the API or constructing `EodhdMarketMemory`, opens SQLite with
`Mode=ReadOnly`, and calls the deployed static receipt-integrity/preflight readers. Its cutoff
projection is computed in memory. It does not call the write-capable `MarketKnowledgeAt` seal API.
The no-argument whole-database maintenance/quick-check path was not invoked.

| Check | Actual result |
| --- | --- |
| Read-only observation preflight | PASS |
| All retained observation checksums and native lineage validation | PASS |
| Receipt count | **2**, compared with historical handoff count 1 |
| Selected historical receipt ID | `sha256:caa655557878353e9d700ec84c0c696777fcbf52ee08a63b07f72a99e3a5d11c` |
| Selected full receipt checksum | `sha256:13d6426177e0553e1c6ca86c856d0d814695da5030bc56d9c4dea404d8e9637d` |
| Membership one tick before selected receipt's persisted ingestion | False |
| Membership at its persisted ingestion | True |
| Historical cutoff | `2026-10-04T14:29:13.9256374+00:00` |
| Projection checksum at that cutoff | `sha256:5d150d96d66c3c6442868d7b251a3c0b027e43e70a7a4ca9e4ec0198d6f62db6` — matches historical handoff |
| Schema | Version **97**, established by deployed preflight's exact latest-version check |
| Observation watermark | Native preflight passed: not earlier than retained ingestion and not ahead of trusted check time; exact value not emitted |
| H manifest/binding tables | Physical presence/absence **not directly queried**; migration 98 is not recorded as applied |
| Reopen | Independent read-only connection successfully reconstructed and validated retained receipts |

Both retained receipts passed native validation. The second receipt's ID, source date, origin and
New/Revision classification were not emitted by the helper and remain **unverified**. Do not assume
that it is a later-day AAPL acquisition, a duplicate, corruption or a revision. Neither complete
source-day coverage nor duplicate-attempt history can be reconstructed from this limited output.
A full sanitized read-only inventory is required before adopting a manifest that must preserve every
historical snapshot. No production SQL write, persistent PRAGMA, migration or cutoff seal was used.

### Historical facts and strength of comparison

The 2026-10-04 published handoff reported the selected receipt as:

- AAPL, source day 2026-10-02, origin `AcquiredProviderDaily`, result New, no predecessor.
- Alpaca / `iex-historical-1Day-raw`; source contract `alpaca-historical-stocks-1Day-raw-v2`;
  receipt contract `finance-market-observation-daily-v1`.
- Acquired `2026-10-04T17:56:19.98109+00:00`.
- Ingested/knowable `2026-10-04T17:56:20.0069441+00:00`; provider publication time unknown.
- Mapping effective 2026-01-29 through 2026-10-02, with the evidence reference above.

The current native full-checksum comparison corroborates the retained committed content and identity.
The diagnostic did not separately print those persisted timestamps/snapshots or compare original raw
SQLite JSON bytes with a historical byte archive. Therefore this is **canonical integrity verification**,
not a claimed fresh field-by-field/raw-byte export. Source/session date is not acquisition or knowledge
time. The unchanged early projection and before/at-ingestion membership are freshly verified.

## Code-based adoption assessment

Relevant accepted code: `FinanceObservationMappings.cs`, `FinanceObservationRuntime.cs`,
`FinanceMarketObservations.cs`, `FinanceSchemaMigrations.cs`, `FinanceProspectiveDailyShadow.cs`,
`FinanceDataProtection.cs`, and the shared canonical instrument/observation integrity contracts.

H supports at most four instruments and four mapping versions per instrument; the legacy single-entry
form remains compatible. A versioned manifest must retain each receipt's **complete** canonical
instrument and provider mapping snapshot, including effective range and provenance. Stable canonical
ID alone is insufficient. All retained receipts, including the unidentified second one, must match
before network access. Missing, changed, overlapping or ambiguous historical snapshots fail closed.
No receipt checksum, payload or old binding is migrated/reinterpreted.

A future manifest must include the exact historical AAPL snapshot as read from storage, plus separately
justified non-overlapping source-date coverage for a later interval. Dates before January 29 or after
October 2 are unsupported by the inspected mapping. Do not fabricate coverage of an intervening gap.
Material symbol, venue, currency or instrument identity changes require separate authority.

`ResolveHistorical` compares historical provenance without requiring current acquisition authority.
Structural assertions require valid UTC chronology, bounded evidence and a verification window of at
most seven days. Persisted manifests are checked against their **original trusted recording time**;
future verification at recording is invalid and does not become valid by waiting. Expired assertions
remain historical evidence. Only the selected acquisition version must additionally be current:
verification no later than trusted now and now strictly before its revalidation deadline.

The old flat mapping has no invented verification timestamp. A later operator must actually verify and
record a historical assertion with appropriate evidence; receipt acquisition time must not be copied
into `VerifiedAtUtc` as a fictional earlier verification. A later interval requires separately verified
canonical/provider identity, evidence reference, effective-date support and an explicit revalidation
policy. No such current assertion was established here. Null `ValidTo` means no known termination,
not perpetual listing or unlimited unattended authority. Adopted snapshots cannot later be silently
removed or narrowed, so an open-ended snapshot has future transition limitations that need review.

An expired old assertion alone cannot authorize reacquisition of its source day. A separately current,
provenance-compatible authorization is required; `ReobserveDailyAsync` equality remains unchanged.
Exact duplicates preserve receipt and knowledge time; changed provider values follow immutable revision
lineage, while mapping metadata changes cannot masquerade as value revisions. New manifest/binding
persistence is owned by the same Finance SQLite store. Legacy receipts are not retroactively bound.

SHADOW v1 remains fail-closed: `FreezeDailyShadow` requires equal mapping snapshots and
`EvaluateDailyShadow` requires the frozen mapping commitment. Continued collection across a mapping
transition does not grant cross-version scientific compatibility. No SHADOW operation was executed.

## Proposed deployment and rollback gates — NOT EXECUTED

1. **Authorize and complete missing read-only inventory.** Obtain all receipt identities/source dates,
   complete original snapshots and revision links, actual migration rows/table catalog and watermark.
   Preserve canonical and raw stored receipt-byte commitments for before/after comparison. Resolve the
   second receipt's classification and check full store integrity without opening a migrating owner.
2. **Approve quiescence and recovery preparation.** In a separate operational authorization, disable
   observation and provider transport, preserve other acquisition gates as disabled and stop all writers
   during consistent capture. Inventory owner-drop/startup reconciliation and lifecycle writers too.
   Preserve old image/configuration privately. This review changes none of those flags or services.
3. **Create and verify a full recovery copy.** Use an approved SQLite-consistent backup or quiesced
   volume snapshot that correctly accounts for WAL and all associated Finance evidence; never copy only
   a live main database and discard its WAL. Preserve ownership, checksums, receipts, knowledge seals,
   watermark, schema and required lifecycle recovery state. Verify an isolated restore and raw/canonical
   commitments before deployment. `FinanceDataProtection` public-domain dataset backup is insufficient
   for the complete Alpaca observation/manifests store. No backup or restore was performed here.
4. **Build exact newly approved baseline.** Re-fetch/review identities; use the accepted
   `scripts/build-bigbrain-api.sh <exact accepted SHA>` from clean matching HEAD/main. It builds a Git
   archive, excluding ignored secrets. Verify OCI and compiled identities. Recheck approval if main moved.
5. **Deploy under controlled startup.** Keep acquisition disabled, preserve volumes, admit only one
   migrating owner and use the accepted Compose path. `EodhdMarketMemory` construction runs migrations;
   `finance-schema-status` also migrates and is not an inspection-only command. Startup itself is a
   database change even when observation is disabled. Migration 98 transactionally adds manifest and
   receipt-binding tables in the existing store, then records its version. No manual schema patching.
6. **Verify before adoption/network.** Independently check schema 98, expected initially empty H tables,
   exact preserved receipt bytes/IDs/checksums/snapshots/lineage, sealed projections and knowledge
   watermark. Account separately for accepted startup operational writes. Verify recovery, clock, API
   build identity and disabled acquisition. A mismatch requires STOP, not re-ingestion or metadata repair.
7. **Review mapping adoption separately.** Preserve every historical snapshot and provide truthful new
   verification/effective-date evidence. The current seven-day assertion is a bounded authorization,
   not a research grant. Manifest preparation itself writes durable authorization state and must be
   explicitly approved. Keep scientific and provider authority separate; no request merely to test adoption.
8. **Separate owner gate for provider requests.** Only after storage/adoption review may a bounded request
   or unattended-runtime enablement be authorized. No such authority follows from this report.

Migration 98 has no accepted down migration. An old version-97 binary cannot be assumed safe against
version 98; its observation schema check expects 97. Rolling back only the image is insufficient.
Before new evidence, an approved full predeployment restore with all writers stopped may restore the
old state. After new receipts, assertions, seals or other durable writes, a blind old backup restore
would discard history; preserve that state and stop for a reconciliation decision. Do not delete H
tables/migration rows to pretend a downgrade occurred. External requests and trusted knowledge already
acquired cannot be undone or backdated. Restore/deletion operations require separate explicit approval.

## Security

### Changes and verification

Detta är en sanerad GitHub-version. Publication contains no credentials or credential fingerprints,
raw market payload/OHLCV, private filesystem paths, environment dumps or unrelated host configuration.
The receipt commitments are the explicitly authorized non-secret audit identities.

Only this report, `docs/reports/REPORT-CATALOG.md` and `docs/operations/codex-recovery.md` are changed.
Production code, tests, configurations and databases are unchanged by this task. Unrelated local
mockups and ADR drafts are preserved and excluded. No existing review branch is merged or rewritten.

Executed read-only checks: Git identity/tree/ancestry and scope inspection; selected Docker/service/time
metadata; `system-build-identity`; safe health/recovery GETs; sanitized cycle-category extraction;
existing read-only receipt verifier with historical cutoff and receipt ID. One unavailable container
`curl` attempt exited 127; health reads were then performed from the host. Direct host database access
was denied; no permissions were changed. No failed access attempt is reported as a database check.

Documentation validation: `node scripts/verify-documentation.mjs`; whitespace: `git diff --check`;
staged secrets: `gitleaks git --pre-commit --staged --redact --no-banner` using the existing local tool.
Final results are recorded in the publication verification below. API/Finance/Sentinel/frontend suites,
builds and model tests are **not run**: only documentation changed; live read-only checks and accepted
code inspection address this review. Prior H CI is historical implementation evidence, not a new live
migration, rollback, provider or SHADOW test.

Finance authority remains **RESEARCH / 0 SEK / NONE**. No deployment, restart, migration, production
write, configuration/credential modification, provider request/cycle trigger, assertion creation,
mapping extension, Qwen/research/grant change, SHADOW execution, PAPER/LIVE/AUTO, broker, orders,
positions or capital action was performed. Existing unattended runtime was neither disabled nor
triggered; this is not a claim that the entire running host had zero autonomous activity.

## Remaining work and resumption

Independent architect review should decide the next bounded read-only inventory/recovery-preparation
scope, then a separate deployment authorization. Blockers are incomplete second-receipt/table inventory,
no verified full recovery copy/restore evidence, enabled live runtime needing approved quiescence and
absence of a current reviewed versioned assertion for later AAPL dates. Aggregate degraded health needs
category-level diagnosis before operational GO; no causal production fix is asserted here.

No architecture defect requiring an implementation change was demonstrated. The H contract provides
the needed historical/current-authority separation, subject to the live evidence and adoption gates.
## Resumption

Return to the linked canonical runbook and this report; do not infer deployment/provider permission
from publication. STOP for independent architect review. No next implementation checkpoint is started.

### Publication verification

Initial documentation validation rejected two missing schema headings (`Security` and `Resumption`);
headings were corrected without changing findings. Final documentation validation PASS: 267 Markdown
files / 91 unique backlog IDs. Working/staged `git diff --check` PASS. Staged Gitleaks PASS, no leaks.
Scope review PASS: exactly the three documentation files listed above; no source/test/config changes.
Fetched `origin/main` before publication still matches the exact baseline SHA/tree. No application test
suite or build was executed in this documentation-only checkpoint.
