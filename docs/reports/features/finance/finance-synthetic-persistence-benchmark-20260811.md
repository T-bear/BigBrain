# Finance synthetic persistence benchmark and manifest foundation

## Metadata

- Date: 2026-08-11
- Scope: BB-045 provider-neutral M2 fixture foundation
- Related commit: assigned on publication
- Runtime/deployment: source, tests and documentation only; not deployed

## Status

BigBrain now has an immutable historical-dataset manifest, deterministic content integrity,
a provider-neutral persistence contract and an in-memory correctness reference. A standalone
benchmark measured append-oriented JSONL and transactional indexed SQLite using only
deterministic synthetic daily bars. No provider was contacted, no real market data was used,
and no Finance production persistence or runtime was activated.

The evidence supports a **provisional hybrid direction**: immutable content-addressed payload
files plus SQLite for transactional manifest publication, indexes, correction lineage and
entitlement-scoped deletion. Confidence is **medium**. This is not an Accepted technology
decision; backup/restore, concurrent-reader behavior, exact payload format and deletion across
backup copies remain to be validated before a production store or ADR.

## Changes

## Manifest and integrity contract

`HistoricalDatasetManifest` records dataset/revision/parent identity, schema, canonical
instruments, provider/product/MIC, requested and covered dates, daily adjustment basis,
acquisition request/time, policy/evidence, observation/action/gap/rejection/correction counts,
SHA-256 content checksum, storage format/version, provenance and retention/deletion duties.
It deliberately has no credential, header, secret or raw-provider-payload field.

The checksum is derived from stable ordinal/invariant-culture serialization of immutable
revision members and correction evidence. Only `Complete` manifests can be appended. Identical
revision+manifest input is idempotent; a reused revision ID with different content fails.
Parent revisions must already exist, so incomplete children cannot become visible.

## Persistence and deletion semantics

`IHistoricalDataPersistence` supports immutable append, exact manifest/revision lookup,
revision metadata enumeration, instrument/date-range bars, corporate actions, quality/gap
evidence, correction ancestry, checksum verification and provider/product/policy scope.

The fixture reference removes scoped payload and revision objects on explicit deletion while
retaining only a sanitized receipt: deletion request/time/reason, provider/product/policy,
deleted revision IDs, prior manifest fingerprints and evidence reference. Unrelated revisions
remain. The receipt does not retain prices, actions or raw payload and therefore is not a way
to evade a provider deletion obligation. Production backups must eventually participate in
the same deletion scope.

## Evidence

### Benchmark method

Command:

```text
dotnet run --project tools/BigBrain.Finance.PersistenceBenchmarks -c Release --no-build -- --full
```

Environment: local .NET 10 on the repository host. The tool uses fixed inputs, no random
source, no network and process-scoped temporary files that are removed after the run. Scales:

- small: 10 instruments × 252 sessions = 2,520 initial rows;
- medium: 100 × 1,260 = 126,000 initial rows;
- large: 500 × 2,520 = 1,260,000 initial rows.

Each candidate measured initial write, a five-session append per instrument, exact revision
lookup, a 100-row instrument range, full sequential read, integrity verification,
provider-scoped deletion and file size. Timings are one architectural sample, not durable
service-level objectives or CI thresholds. The append publishes a separate child revision;
it never adds facts to the original revision. SQLite stores decimal values as invariant text
rather than binary floating point, and both integrity paths hash deterministic content.

### Measured evidence

| Scale | Candidate | Initial rows | Initial write ms | Append ms | Exact revision ms | 100-row range ms | Sequential ms | Integrity ms | Delete ms | Bytes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Small | JSONL v1 | 2,520 | 10.767 | 1.386 | 3.012 | 0.671 | 0.361 | 7.758 | 0.045 | 312,975 |
| Small | SQLite v1 | 2,520 | 33.682 | 0.710 | 1.715 | 0.090 | 0.220 | 9.340 | 4.207 | 733,184 |
| Medium | JSONL v1 | 126,000 | 165.683 | 0.780 | 0.034 | 28.410 | 33.748 | 37.602 | 1.190 | 15,572,418 |
| Medium | SQLite v1 | 126,000 | 1,098.443 | 3.384 | 0.043 | 0.053 | 8.106 | 324.364 | 334.243 | 35,614,720 |
| Large | JSONL v1 | 1,260,000 | 793.331 | 1.495 | 0.059 | 153.477 | 146.605 | 369.359 | 13.803 | 155,992,420 |
| Large | SQLite v1 | 1,260,000 | 9,674.985 | 18.128 | 0.046 | 0.053 | 74.501 | 2,605.957 | 3,553.960 | 356,208,640 |

JSONL was substantially smaller and faster for bulk initial sequential creation in this
implementation. SQLite was substantially faster for indexed instrument-range access and
provided transactions for atomic revision publication. SQLite deletion was slower because
the benchmark explicitly removed indexed provider-scoped rows; that cost is still preferable
to an untraceable or over-broad deletion. Exact lookup for both forms used a small manifest
catalog and is not evidence that raw JSONL supports efficient arbitrary queries.

### Automated evidence

Twelve new tests cover deterministic manifests/checksums, mutation detection, immutable
roundtrip, idempotent duplicate append, explicit conflict, complete-only publication,
correction lineage and old revision reproducibility, bar/action/gap reads, provider/policy
enumeration, scoped deletion and unrelated-data preservation, entitlement metadata, absence
of secret-bearing manifest surfaces, replay compatibility and revision-catalog no-lookahead.

Verified gates on 2026-08-11:

- `dotnet restore BigBrain.slnx` — pass;
- `dotnet build BigBrain.slnx -c Release --no-restore` — pass, zero warnings/errors;
- `dotnet test BigBrain.slnx -c Release --no-build --no-restore` — pass, 360/360;
- `node scripts/verify-documentation.mjs` — pass;
- `git diff --check` — pass;
- `docker compose config --quiet` — pass.

## Security

Detta är en sanerad GitHub-version. All observations, prices, provider/policy identities
and performance inputs are invented fixtures. No credential, internal address, raw log,
real market payload or private environment detail is published.

## Remaining work

- BB-071 remains open and blocks any real provider ingestion or durable provider storage.
- No account, API key, provider SDK, HTTP call, real payload, broker or trading authority was added.
- No database migration, production repository, runtime configuration or deployment occurred.
- The in-memory store is test infrastructure, not durable historical memory.
- The benchmark reuses the repository's existing SQLite dependency; no paid dependency,
  service or new container was introduced.
- Partial JSONL payload files must remain unreachable until a transactional catalog publishes
  a complete manifest; production crash recovery and garbage collection remain to be proven.

## Resumption

First preference remains obtaining exact BB-071 entitlement evidence. If that remains
blocked, the next safe implementation is **SYNTHETIC LOCAL HISTORICAL MEMORY PROTOTYPE /
BACKUP-RESTORE VALIDATION**: prototype the measured hybrid behind this contract, test
transactional manifest publication, concurrent replay reads, crash recovery and precise
deletion across primary and backup copies. It must still use synthetic data only. A first
authorized free historical-data adapter remains a separate owner-approved milestone after
exact retention/backtesting/derived-use rights are accepted.
