# BB-087 prospective shadow observation foundation

## Metadata

- Date: 2026-08-15
- Baseline: `31156ec8e9780dc6622891cb4ba46050c302a6f6`
- Mode/budget: `RESEARCH`, 0 SEK; no broker, order, PAPER, LIVE or AUTO capability

## Status

Implemented, local regression passed, deployed and bounded-runtime/restart verified. Implementation commit `86e92c431cffc1db37684bcfba5a2a785d7b05ad` passed all four jobs in GitHub Actions run #44 on 2026-08-15.

## Changes

Finance now has a durable SQLite prospective journal for approved EODHD Free `CURRENT EOD / PROSPECTIVE EOD` research. A source-state worker starts after BB-083 recovery, requires clock integrity, scans only canonical EODHD evidence and evaluates the unchanged `buy-and-hold/v1`, `sma-crossover/v1` and `momentum/v1` strategies. It creates no order and has no execution authority.

Prediction identity pins instrument, session, market revision, feature revision, strategy/version, parameter fingerprint, knowledge cutoff and `next-eligible-source-session-close-v1`. Repeated cycles use an idempotent unique insert. Features are selected only when their knowledge time is at or before cutoff. Observations older than the bounded current-session window, or sessions whose next source observation is already knowable, are not backfilled as prospective.

Outcomes are separate one-per-prediction rows. The original ID, cutoff, signal and lineage remain unchanged; only evaluation state advances. EODHD deletion scope and stale-preview fingerprint include the journal, so source-dependent evidence cannot outlive the existing provider policy. It is excluded from WIKI's indefinite public-domain backup class.

Read-only endpoints cover predictions, detail, scorecard and status with bounded filters. The Finance UI separates Shadow research from historical backtests, shows pending versus evaluated, and labels samples below 20 as `BOOTSTRAPPING`.

## Evidence

- Network-free temporal simulation: T0 prediction then T1 outcome.
- Exactly-once retry, clock failure, late-start anti-backfill, malformed ID/filter and immutable prediction assertions.
- Read-only API and UI assertions include no mutation or order controls.
- Runtime: eight EODHD instruments for 2026-08-14, 24 valid pending predictions (three strategy versions), zero evaluated outcomes and exact feature revision `feature-c204a8133abaf8a2` over eight source revisions.
- The first drill exposed 24 rows referencing a WIKI feature revision. They were not deleted or rewritten as successes: the integrity pass classified them `INVALIDATED`, retained the reason, and exact source-revision membership became a hard feature-selection gate.
- Restart retained 48 total audit rows (24 valid pending + 24 invalidated) and created no duplicate valid prediction. Current health, WIKI memory and backup inventory remained available.
- GitHub Actions run #44: documentation, frontend, secrets and backend all `success`.

No claim is made that a future EOD outcome exists. A real prediction is created only if the deployed current observation and feature revision pass all causal gates; otherwise the journal remains safely armed.

## Security

Endpoints are read-only, validate bounded filters and malformed opaque IDs, return no secrets or filesystem paths and expose no mutation/order capability. The clock, entitlement, causality and late-start gates fail closed. No provider request, paid service or external database was added.

## Remaining work

The worker currently performs a lightweight source-state scan after startup; it does not add high-frequency polling. The recommended next Finance slice is prospective outcome-evaluator maturation plus explicit daily current-EOD cadence/recovery evidence, while M5 Hard Risk Engine and the security baseline remain prerequisites before any execution eligibility discussion.

## Resumption

Start with ADR 0029, `FinanceShadowResearch.cs`, the shadow API scorecard and the EODHD lifecycle worker. Verify current-source age and clock integrity before treating any new row as prospective.

## Sanitization

Detta är en sanerad GitHub-version. It contains no provider credential, raw market payload, private runtime identity, sensitive path or raw log.

## Protected outcome audit design checkpoint — 2026-11-20

**REVIEW CHECKPOINT / risk 3 / synthetic only.** This resumes the
[architect's bounded assignment](https://github.com/T-bear/BigBrain/issues/57#issuecomment-6803404086)
and [WAL qualification](https://github.com/T-bear/BigBrain/issues/57#issuecomment-6804821431),
as confirmed by the owner's resumption and
[current handoff](https://github.com/T-bear/BigBrain/issues/57#issuecomment-7036256540).
Accepted baseline is `1e3d203477cc81e8b1e58780dd4a758926469b7d`.
Branch `checkpoint/finance-outcome-audit-20261120` starts there. Its exact publication
SHA/tree and CI are recorded in the review PR; this is not a merge candidate or operational reader.

The separately reviewed PR #81 remains untouched at
`afcf5d79a62be2831ff002014a9dfe972384f01f`, tree
`82528f28a6abba4168a8e8b2133f7b19d7c6761c`. Its 139 focused tests and independent
42-test causal review are reused for that unchanged candidate only. No correction is lost,
cherry-picked, merged or rebased. PR #82 remains untouched at
`ab76e7f2431804e0e8e7800429ed213eb7ec3081`. Both remain drafts.
The audit assignment explicitly requires this separate checkpoint; it is not a competing
implementation of PR #81. The five audited table contracts and exchange calendar are unchanged
between #81 and this accepted baseline. Current main's ingress/policy changes are retained.
Future integration will need to reconcile the overlapping canonical STATUS/BACKLOG/recovery/report
edits with #81; this checkpoint neither overwrites that branch nor resolves an integration merge.

### Implemented boundary and fixed report

`tools/BigBrain.Finance.OutcomeAudit` is an isolated .NET **library**, with internal audit
entry points and no executable, HTTP route, host integration, configuration/credential loading,
provider, broker, order client, migration, cleanup or snapshot creation. The only shared domain
source is the existing `UsMarketCalendar.cs`, linked at build time. It has no API/Modules
assembly dependency. The eight-symbol EODHD mapping is pinned from the accepted catalog.
The separate test project is included in the existing solution/backend CI; it constructs only
synthetic tables using the actual CREATE TABLE definitions, without constructing a storage owner.

The reader accepts only a pre-existing offline rollback-format SQLite image, supplied SHA-256 and explicit UTC snapshot cutoff. It rejects creation/evaluation
after that cutoff; authenticating the receipt remains the blocked operator responsibility.
It rejects links, WAL-mode headers, any WAL/SHM/journal sidecar, missing files, wrong digests and
images outside 100 bytes–256 MiB **before opening SQLite**. It never creates a missing DB.
It opens with `SqliteOpenMode.ReadOnly`, `immutable=1`, pooling disabled, `query_only=ON`,
`trusted_schema=OFF`, in-memory temporary storage and an explicit deferred read transaction.
`quick_check`, foreign-key checks and five fixed SELECTs run inside that transaction.
Only ordinary tables are admitted. More than 100,000 rows in any audited table stops the whole
report, rather than truncating and presenting a partial success. Image digest and absence of
sidecars are checked again after closing SQLite. No raw SQL/filter capability is exposed.

**These checks do not prove that an arbitrary operator-supplied path is stable or complete.**
The prototype has no approved production entry point. Immutable mode is valid only once an
independently protected, complete offline image has been established. Pre/post hashes and a
cooperative file handle are not an OS-level defense against replacement or concurrent writers;
a digest is not evidence of snapshot lineage, rights or authorization. Production use remains
STOP until the separate operator boundary below is approved and technically verified.

Report contract `outcome-ledger-audit-v1` emits fixed-label integer counts and either
`HISTORICAL_AUDIT_ONLY` or `STOP` with a fixed sanitized reason. It emits no symbols, row IDs,
market prices/returns, timestamps, paths, SQL, exceptions, credentials or source payloads.
Counts cover the complete five-table ledger, declared SQLite foreign-key failures, logical
orphans, duplicate identities, invalidated predictions with outcomes and per-outcome findings:

- Prediction mode/provider/horizon/strategy identity; explicit timestamp zones; source close,
  observation knowledge, cutoff, creation, immediately next US session and strict pre-open.
- Exact source and target observation/revision links, instrument/product/policy/MIC mapping,
  acquisition-to-revision time, checksum shape and checksum-derived EODHD revision identity.
- Earliest target knowledge revision, unknown or tied first revisions, later corrections and
  outcome knowledge strictly after prediction/cutoff and no later than evaluation.
- Feature existence, original source membership, all declared input revisions knowable at
  cutoff, feature creation time, checksum shape and the existing raw-price-basis contract.
- Unique outcome identity/link, reference and target price agreement, exact decimal return
  and direction, including negative and zero returns.

Findings overlap and must not be summed as disjoint failed-row counts. The bounded calendar
reuses `us-equities-ny-v1` (normal 16:00 New York close, conservative on early-close days);
unsupported date range is rejected. There is no claim of a new exchange calendar or adjusted
corporate-action proof. `v2-criteria-satisfied` is a diagnostic comparison, not a prospective
success or a claim that v1 admission used v2. The schema has no admission-version field: every
stored outcome remains **historical/unversioned**. Original raw-payload and complete feature
content hashes are **not recomputed**, explicitly counted as unknown; no payload access is
introduced. These limitations prevent a scientific/rights PASS even with zero reported defects.

### Synthetic verification and observed failures

73 focused tests PASS on 2026-11-20. Exact locally observed provider:
**Microsoft.Data.Sqlite assembly major=10, minor=0, build=10, revision=0; SQLite 3.53.3**. Native package versions are pinned alongside
the existing API dependency versions; this is not a statement about a future operator VFS.
Tests use temporary synthetic fixtures only and remove their own fixtures on disposal.

- ReadOnly + query_only + explicit read transaction over synthetic live WAL **changed existing
  SHM bytes**; the absent-SHM control **created SHM**, with unchanged DB/WAL bytes.
- Immutable access directly to uncheckpointed synthetic WAL missed the WAL-only table.
  The real reader rejects these inputs, including a copied WAL-header file with hidden sidecars,
  without creating SHM or changing bytes. Rejection is not successful live-ledger visibility.
- SQLite backup API is used **only in test fixtures** to make a consistent synthetic snapshot,
  followed by rollback journal mode. The reader sees committed WAL-only rows, excludes another
  connection's uncommitted change, and remains stable after the source writer advances.
- Main/sidecar file-name sets and SHA-256 values remain unchanged through accepted/rejected
  reader calls. UPDATE, DELETE, DDL and temporary-table attempts fail under query_only;
  persistent DML also fails independently with SQLite READONLY when query_only is disabled
  in the synthetic negative control. No test grants production access.
- Tests cover known-before-prediction outcomes, late import/arrival, later/ambiguous revisions,
  missing/null/invalid lineage, orphan/duplicate links, original invalidation evidence, time
  zones, pre-open boundary, US holidays/DST, repeat reads and whole-report resource-limit STOP.
- Initial local builds caught analyzer requirements (cached searches/arrays and test naming).
  These were fixed without suppressing rules. The source's actual price-basis string is checked;
  no invented `RawClose` storage enum is used. No production experiment or failed drift action.

Release solution build PASS (zero warnings/errors); documentation and whitespace PASS.
Required final format/secrets/CI results and exact independent-review status
are attached to the publication PR. Prior #81 review does not review this new library.

### Reused evidence and current gates

| Gate | Current assessment and evidence limit |
| --- | --- |
| Offline design and synthetic no-write/causality tests | **PASS** within the fixtures and version above; not an operator privilege-boundary proof. |
| Actual historical outcomes | **BLOCKED / NOT READ**. Reuse [906-prediction metadata audit](https://github.com/T-bear/BigBrain/issues/57#issuecomment-6796635237): dated 858 evaluated / 24 invalidated / 24 pending, 66 logical revision pairs, no full-revision-key collisions. No new production query; these are not today's remeasured counts. |
| Ingress correction | Reuse [#101 owner acceptance](https://github.com/T-bear/BigBrain/issues/101#issuecomment-7002654042): Nov 19 direct API loopback, missing/wrong credentials denied, authorized Web unlock accepted. The old all-interface observation is historical. No new network probe. |
| Overall isolation / Finance level | **Risk 3**. [Nov 19 audit](https://github.com/T-bear/BigBrain/issues/101#issuecomment-7002793301) remains NO-GO for level 2: broker credential surface, incomplete Media/network/Tailscale/backup isolation and RESEARCH negative guarantees. This library does not repair or certify the whole runtime. |
| Rights / BB-132K | **BLOCKED for protected access/copy**. [Owner attestation](https://github.com/T-bear/BigBrain/pull/82#issuecomment-6795991464) is preserved as owner-reported written permission, not independently inspected. Nov 18 proposed re-review date has passed; no later resolution was found in the inspected handoff/#57/#82. Neither automatic extension nor rights expiry is inferred. No reserved decision is needed for synthetic data. |
| Backup / rollback | Reuse scoped Nov 19 STANDARD backup/restartability receipt evidence in #101; do not infer absence of backups. It does not itself grant a restricted EODHD copy or prove this audit's operator boundary. Current daily/full-Finance recovery coverage remains unverified in this task; no backup/restore/restart attempted. |
| Manifest / cadence | **NOT LOCKED / NO NEW COHORT**. [Fixed evidence-lock proposal](https://github.com/T-bear/BigBrain/issues/57#issuecomment-6803578385) is still proposed, including five-session/22:00 cutoff. No operational policy accepted here; no EODHD calls. Cadence was last documented disabled; not remeasured. |
| Runtime revision | No live inspection in this task. Handoff's dated API `873013d2` / Web `1e3d2034` supersede Nov 11 runtime notes; neither is this audit or PR #81. |
| Independent review / merge / production | Exact new-candidate risk-3 review required; request attached to PR. **No merge, deployment, data-copy, key access, provider/model/order or production audit authority.** |

### Minimum future protected operator route — proposal, not executable approval

Live-WAL reading is rejected. If a complete no-write live reader cannot be independently proven,
seek a **separate, rights-approved, operator-generated consistent offline snapshot**. This is
not permission to copy the real DB. Before any such action, bind an exact reviewed tool SHA/tree
and native SQLite/VFS; a fixed source/storage identity; operator scope; restricted provider/data
classification; explicit copy/read grants including applicable BB-132K decision; source snapshot
transaction and creation timestamp; exact byte count/SHA-256; coverage and completeness receipt;
protected local ownership; agreed retention/inventory/cleanup disposition; and backup/rollback
conditions. Do not reuse WIKI public-domain rights for EODHD or make a mixed-rights export by default.

The generating operator must own source access and establish completeness while writers are
handled through an approved consistent-snapshot mechanism. The audit process must receive only
that sealed image via an independently verified read-only file/directory boundary, without
mutable parent paths, symlinks, credentials, source-volume access, network, Docker socket,
root/sudo or an arbitrary path/query channel. Bind a fixed command and sanitized aggregate-only
result; no public/general SQL endpoint. Qualify absent/present SHM, concurrent writers, actual
image completeness and filesystem no-write in that exact approved environment **before real
execution**. Verify before/after image identity and protected receipts. Cleanup/disposal is a
separately authorized operator action, never an automatic reader side effect. Until these
conditions and independent review pass, **STOP: do not read the real 858 outcome rows**.

Next step is ChatGPT's independent review of this exact published checkpoint and the operator
proposal. No automatic implementation of the operator route, manifest policy or activation follows.
Issues #55/#57 remain open; #54/#59/#60 retain their own scope.
