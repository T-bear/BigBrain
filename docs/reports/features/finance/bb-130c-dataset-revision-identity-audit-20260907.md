# BB-130C — canonical dataset revision identity compatibility audit

Detta är en sanerad GitHub-version. Only aggregate existing-data results, public
historical identifiers and source findings are included; no raw market rows or private paths.

## Metadata

- Date: 2026-09-07.
- Baseline main: `1d7f1f16a8afbfce7768dedc75df7660a9648985`, reverified after interruption.
- Audit branch: `bb-130c/dataset-revision-identity-audit`, created directly from main.
- Accepted blocker evidence: [exact blocker report](https://github.com/T-bear/BigBrain/blob/8b597d1f95fe2dff109d378afe0a81433801c79a/docs/reports/features/finance/bb-130c-dataset-intake-responsibilities-20260907.md).
  That branch remains **BLOCKER HANDOFF — NOT MERGEABLE** and is not an audit ancestor.
- Purpose: determine compatibility evidence and recommend a future correction strategy;
  no correction, extraction, migration or deployment is authorized.

## Status

**ACCEPTED / MERGED TO MAIN / CI VERIFIED — READ-ONLY AUDIT**. Bounded evidence handoff, not full
production compatibility certification. The owner requested reuse of completed work
after interruption, explicit UNKNOWN where evidence is missing, and publication without
expensive rediscovery. No new production queries were run during final reconciliation.
The lineage defect remains **BLOCKED — CULTURE-DEPENDENT REVISION IDENTITY**.
Owner/architect approved audit SHA `76a71d28f271b3a9c0e39b9da9b709035aa07aa0`
and Option B as architectural direction. Merge `eb21b7c654e5adfaa25065a2264293b8ffbfed6d` passed
[main CI](https://github.com/T-bear/BigBrain/actions/runs/34112859688) (backend, frontend, documentation, secrets).
Approval does not authorize an identity correction. No historical rekeying is authorized;
no alias layer is justified by current evidence.

## Evidence

### SYNTHETIC BLOCKER EVIDENCE — reused, not rerun

The separate blocker proves identical CSV artifacts and persisted bar values produce
different canonical IDs with decimal-comma and non-Gregorian calendar cultures.
Invariant/en-US matched; sv-SE and th-TH differed. Its deliberately failing equality
regression remains on that non-mergeable branch and is not copied into this audit.

### READ-ONLY EXISTING-DATA EVIDENCE

Before querying, the running BigBrain API's allowlisted Finance database/quarantine
settings were checked against its Finance volume mount and `EodhdFinanceOptions.Section`.
The configured database, rather than a guessed default/temp database, was inspected.
No credentials or full environment dump were needed.

A separate existing .NET SDK image ran a small local probe with network disabled,
read-only root, all capabilities dropped, no-new-privileges and **only the Finance
volume mounted read-only**. Microsoft.Data.Sqlite opened with `Mode=ReadOnly` and
`Pooling=false`; the detailed scan used `BEGIN DEFERRED` for its read snapshot.
Application startup/store constructors were never invoked. No INSERT/UPDATE/DDL,
checkpoint, VACUUM, migration, promotion, acquisition or file-write call targeted Finance.
SQLite successfully opened and returned SELECT results under the read-only mount.

| Completed inventory | Count |
| --- | ---: |
| Canonical revisions represented in observations | 145 |
| EODHD revisions / observations (context only; not recomputed) | 144 / 36,264 |
| WIKI revisions relevant to the CSV promotion defect | 1 |
| WIKI observations | 3,722 |
| Dataset candidates / candidates with canonical references | 4 / 1 |
| Revision records without observations | 0 |
| Observations referencing missing revision records | 0 |
| Candidate canonical references pointing to missing revisions | 0 |
| WIKI revisions with confirmed downstream references | 1 |
| Feature revisions directly referencing WIKI / feature values | 1 / 78,162 |
| Feature revisions / feature values examined | 22 / 998,067 |

The WIKI ID is the already-public `wiki-5713d7dccfa38f56`; the stored revision count
and actual observation count both equal 3,722. No new real identifiers are published.
The feature counts came from literal exact-ID occurrence scans across row fields;
the source definitions confirm the source-revision relationship. Other stages did
not return completed results before the audit process was stopped.

Completed aggregate query definitions (reproduction reference only; do not rerun
without need). The second probe also enumerated table names with `sqlite_master`
and inspected column names with `PRAGMA table_info`; no schema writes occurred.

```sql
SELECT provider, COUNT(DISTINCT revision_id), COUNT(*)
FROM observations GROUP BY provider;
SELECT COUNT(*) FROM revisions r WHERE NOT EXISTS
  (SELECT 1 FROM observations o WHERE o.revision_id=r.revision_id);
SELECT COUNT(*) FROM observations o WHERE NOT EXISTS
  (SELECT 1 FROM revisions r WHERE r.revision_id=o.revision_id);
SELECT COUNT(*) FROM dataset_candidates c WHERE canonical_revision_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM revisions r WHERE r.revision_id=c.canonical_revision_id);
```

The completed lineage stage read dataset_candidates, feature_revisions and feature_values
within the read transaction and emitted only row counts and WIKI-ID occurrence counts.
The uncompleted backtest stage returned no aggregate. These details preserve the
evidence method without making the temporary probe an ongoing operational dependency.

An eager scan reached about 1.79 GiB while reading large downstream rows. Only that
temporary audit container was stopped. A streaming replacement built successfully
with a proposed 512 MiB limit, but its execution was interrupted before any result
file was produced. Its planned recomputation is **not** counted as executed evidence.
Existing application services were not restarted or changed. This operational limit
does not alter or invalidate the aggregate SELECT results already returned.

### Compatibility classification — exact scope

| WIKI classification | Count | Meaning |
| --- | ---: | --- |
| Inspected | 1 | Identity, count and references inventoried |
| Proven invariant-compatible | 0 | No completed exact artifact recomputation |
| Proven non-invariant legacy | 0 | No production culture mismatch established |
| UNKNOWN / not recomputed | 1 | Original parsed representation not yet verified |
| Distinct-ID semantic duplicates within WIKI | 0 | Only one WIKI revision exists; no distinct pair to compare |

Zero proven mismatches does not mean compatibility. Zero WIKI duplicates does not
certify cross-provider equality or overlap, nor all research/feature/backtest histories.
The 144 EODHD revisions use another acquisition path and are outside this hash audit.
The orphan checks concern missing identity endpoints, not unused historical revisions.
The one WIKI revision is demonstrably referenced; unused-revision status for other
providers and full downstream referential integrity remain unassessed.

### SOURCE-CODE INFERENCE — serialization and compatibility

Current `FinanceDatasetIntakeStore.Promote` sorts mapped symbols ordinally then dates,
joins fields with `|`, rows with LF (no terminal LF), encodes UTF-8 and hashes SHA-256.
It uses lowercase invariant digest text and a 16-hex suffix. Decimal and nullable
adjusted-close interpolation uses CurrentCulture; DateOnly `yyyy-MM-dd` uses the
current calendar. Null adjusted close is empty. Symbols/source prefix normalization
is invariant. Acquisition timestamps are not in the content hash. Numeric scale,
ordering, field membership and null handling must not change incidentally in a fix.

Historical source at `822a4ff` hashes symbol/date/OHLC/**volume**, omitting adjusted
close; it stored raw close as adjusted close. Current main additionally hashes nullable
adjusted close. The [BB-090 report](finance-bb-090-implementation-runtime-20260816.md)
already documents this historical adjusted-price limitation without rewriting evidence.
Thus merely applying invariant formatting to today's input must not be claimed to
reproduce the original WIKI algorithm. The algorithm difference is independent of culture.

### UNKNOWN — exact missing evidence

No completed audit has verified original-artifact presence/checksum, selected parsed
row representations or decimal scale against the persisted WIKI revision. No hash
match under legacy/invariant/en-US/sv-SE was returned. Stored integer volume alone
cannot prove the original parsed decimal scale. Artifact availability remains UNKNOWN;
absence is not claimed. Historical public reports alone cannot establish those bytes.

The smallest additional evidence, if required by architectural review, is one bounded
streaming **read-only** recomputation for the one WIKI revision: verify the retained
artifact hash, recover the original selected parsed fields and scale, compare exact
legacy hash plus numeric-invariant and fully Gregorian/invariant variants, and
separately compare today's adjusted-close field layout. Output match booleans/counts
only. Do not repeat inventory or the 998,067-feature-value scan. No write/promotion
is needed. That additional task is not silently executed or claimed complete here.

### Downstream impact classification

| Consumer | Classification | Evidence and limit |
| --- | --- | --- |
| revisions PK / observations composite key | DIRECT | Production WIKI record and 3,722 rows; source keys include revision identity |
| dataset_candidates canonical_revision_id | DIRECT | One existing canonical binding |
| Candidate manifest contents | UNKNOWN existing-data; DIRECT by source | Catalog/manifest serialization writes canonical ID; field-specific manifest check did not finish |
| Feature lineage | DIRECT, then INDIRECT/FINGERPRINT | One feature revision and 78,162 values contain WIKI ID; feature identity depends on source lineage |
| Backtests | INDIRECT/FINGERPRINT and DIRECT by source; current count UNKNOWN | market_revisions_json and run configuration pin IDs; BB-084 public evidence records WIKI runs, but current scan did not complete |
| Robustness | INDIRECT/FINGERPRINT and DIRECT by source; current count UNKNOWN | market_revisions_json, feature revision and run references; historical WIKI evaluations published, not reverified here |
| Risk/shadow/autonomous research | UNKNOWN current links; possible INDIRECT/FINGERPRINT | Evidence/proposal/experiment lineage can depend on upstream revisions; no completed current counts |
| BB-129A campaigns | No direct canonical-WIKI reference in current source selection; runtime UNKNOWN | Selects BB-127 research revisions/fingerprints, not canonical WIKI; do not label current database NONE FOUND without completed scan |
| Backup/API | DIRECT by source; current artifacts UNKNOWN | Revision IDs retained in backup manifests, dataset catalog and result lineage |

Inspected schema definitions use primary keys and application/JSON references; absence
of declared SQL foreign keys does not permit rekeying. A new hash would be a new
revision key and could produce new derived fingerprints. Same-candidate terminal replay
retains its ID; `INSERT OR IGNORE` is not semantic deduplication across different IDs
or candidates. Potential future duplicate lineage is a source inference, not a detected
production duplicate.

## Decision recommendation

**Recommend OPTION B only: explicitly version the future canonical identity algorithm
and preserve every legacy ID.** Legacy compatibility remains materially unknown and
the historical field layout differs from current code. An explicit version makes the
new contract auditable rather than silently reinterpreting old IDs. This recommendation
does not authorize a prefix format, schema change, migration, alias table or production fix.
Versioning alone does not solve semantic duplicates: the correction design must explicitly
retain terminal-candidate replay and define handling of a separately submitted equivalent
artifact before implementation approval. No historical rekeying is proposed.

| Option | Audit conclusion |
| --- | --- |
| A — invariant future formatting, keep IDs | Not selected: compatibility and duplicate risk are not sufficiently established |
| B — versioned future algorithm, keep IDs | Selected recommendation because compatibility is uncertain and field-layout history matters |
| C — lookup/aliasing | Not justified: no distinct WIKI semantic duplicates detected; do not introduce speculative persistence |

## Changes

Documentation/evidence only. AGENTS gains the permanent **BLOCKER HANDOFF — NOT MERGEABLE**
workflow; STATUS/BACKLOG/TESTING/catalog/recovery record this bounded audit and its limits.
No intentionally failing blocker test, production correction or audit probe is imported
into application code. The local exploratory probe is not a supported application tool;
its unfinished recomputation is excluded from verification claims.

## Verification

Relevant main-derived fixture suites: `FinanceDatasetIntakeTests` and
`FinanceDataProtectionTests`, using isolated temporary databases without production mounts.
Result: **22/22 passed, 0 failed/skipped**, using the exact command in TESTING.md.
Repository gates passed: documentation verification (226 Markdown files, 90 unique
backlog IDs), working-tree/staged diff checks and staged Gitleaks v8.28.0 (no leaks).
The probe's final Release build passed with zero warnings/errors after matching the
repository's explicit SQLite native-library pin (2.1.12). Its first temporary project
omitted that pin and restore warned on transitive 2.1.11; no repository dependency
was changed and no audit correctness claim depends on suppressing the warning.
Local verification did not repeat unrelated suites. The subsequent main CI above passed
full backend and frontend verification. No deployment was performed.

## Security

Production data mutated: **NO**. Read-only mount and ReadOnly SQLite enforced the boundary;
no copied data was written back. Existing services may independently update their own
runtime state; this audit does not claim the whole live volume was globally static.
No credentials, raw datasets or sensitive paths enter this report. No provider/network
acquisition, scientific retuning, entitlement change, migration or deployment.
Finance remains **RESEARCH / 0 SEK / NONE**.

## Remaining work

Exact one-revision compatibility and downstream counts beyond features remain UNKNOWN.
The production defect is not corrected; intake extraction remains stopped. No other
BB-130 checkpoint is started. Further read-only evidence or correction requires the
owner/architect's next bounded decision.

## Resumption

The audit is accepted and merged; the blocker branch remains NOT MERGEABLE. Next
recommended checkpoint, after explicit owner/architect approval, is to design the
versioned invariant identity correction with legacy replay/duplicate handling and
the single-revision recomputation above if needed to settle compatibility. Do not
implement that correction or resume intake extraction as part of this audit.
