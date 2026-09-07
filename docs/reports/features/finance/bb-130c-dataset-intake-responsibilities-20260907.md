# BB-130C — dataset intake characterization: lineage blocker

Detta är en sanerad GitHub-version. This handoff contains only source findings and synthetic test evidence.

## Metadata

- Date: 2026-09-07.
- Verified HEAD and origin/main baseline: `1d7f1f16a8afbfce7768dedc75df7660a9648985`.
- Handoff branch: `bb-130c/dataset-intake-responsibilities`, created from that baseline.
- State: **BLOCKED — CULTURE-DEPENDENT REVISION IDENTITY**.
- Scope: characterize existing intake responsibilities before bounded extraction.
- No production refactor, deployment, provider call or production-data mutation.

## Status

**BLOCKER HANDOFF — NOT MERGEABLE**. Test/documentation-only publication is explicitly
owner-authorized despite the retained failing regression. No fix or extraction is approved.
The branch commit history identifies the exact handoff SHA.

## Evidence: pre-existing culture-dependent canonical identity

`FinanceDatasetIntakeStore.Promote` builds its hash input by interpolating decimal
OHLCV values using the current process culture. Its `Text` helper separately formats
persisted decimal values with `InvariantCulture`. Thus identical parsed values can
produce different revision identities when the culture changes.

Added `IdenticalCsvPromotionKeepsRevisionIdentityAcrossProcessCultures` to
`tests/BigBrain.Api.Tests/FinanceDatasetIntakeTests.cs` before changing production code.
It uses the existing synthetic two-row CSV fixture, two independent temporary databases,
and invariant versus `sv-SE` culture. It restores the original culture in `finally`.
Both paths reach `Promoted`; equal artifact SHA-256 and promoted-row-count assertions
pass. The required equal canonical-revision assertion fails:

| Fixture culture | Canonical revision |
| --- | --- |
| Invariant | `wiki-dde8381ccbe9ccc5` |
| sv-SE | `wiki-3c39237eb91a02b8` |

These are synthetic test identities, not production evidence identifiers. This proves
cross-culture identity instability in the existing canonical CSV promotion path. It
does not establish that deployed evidence has been affected, or that workbook research
fingerprints or campaign outputs have this defect. Those scopes have not been tested.

Command:

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --filter FullyQualifiedName~IdenticalCsvPromotionKeepsRevisionIdentityAcrossProcessCultures --logger 'console;verbosity=normal'
```

Result: restore and test-project Release compilation completed; **1 test failed at the
revision-ID equality assertion**, as intended for defect reproduction. The expectation
has not been weakened or skipped. Full API/Sentinel suites and the solution Release
build were not run: the explicit stop condition was reached before extraction.
Documentation verification passed (226 Markdown / 90 IDs), `git diff --check` passed,
and gitleaks v8.28.0 found no leaks in the local diff plus the new report. Final staged gitleaks and diff checks passed; no green implementation/CI is claimed.

## Precise hash input and compatibility analysis (handoff amendment)

The production `Promote` input is ordered by symbol using `StringComparer.Ordinal`,
then by DateOnly ascending. Each row is symbol, date, open, high, low, close,
adjusted close, volume, separated by literal `|`; rows use literal LF with no trailing
LF. SHA-256 hashes UTF-8, lowercase invariant hex supplies the first 16 digits.
WIKI uses `wiki-`; other source prefixes filter letters/digits and lowercase invariantly.
Symbols are trimmed/uppercased invariantly before mapping. These operations do not
depend on culture.

Open/high/low/close/volume are `decimal`; adjusted close is `decimal?` (null becomes
an empty field). Default interpolation has no format provider and uses CurrentCulture.
Dates are DateOnly interpolated with `yyyy-MM-dd`, also without a provider: the
current calendar contributes. Numeric input parsing is invariant `NumberStyles.Float`;
CSV dates parse invariantly. Persisted decimals and dates are explicitly invariant;
stored volume is Int64. Negative signs are not exercised because valid prices are
positive and volume nonnegative. Default decimal formatting retains scale; an invariant
fix must not silently add scale normalization. No timestamp, artifact checksum,
candidate ID, source-row ordinal or corporate-action text enters this content hash.

The additional four-case characterization verifies identical artifact hash/size,
promotion state/count/symbols and persisted symbol/date/OHLC/adjusted-close/volume
rows across cultures (excluding timestamps and revision identifiers):

| Culture | Observed identity | Observed formatting difference |
| --- | --- | --- |
| Invariant | `wiki-dde8381ccbe9ccc5` | Explicit reference |
| en-US | `wiki-dde8381ccbe9ccc5` | Same as reference |
| sv-SE | `wiki-3c39237eb91a02b8` | Decimal `50.5` becomes `50,5` |
| th-TH | `wiki-f0b1622232dc5894` | Gregorian year 2024 formats as Buddhist year 2567 |

Decimal punctuation is therefore not the only observed cause. IDs propagate to
manifest/lineage references; this does not claim those fields or timestamps are equal.
The th-TH observation was first captured, then pinned as a characterization expectation;
the original required-equality regression is retained unchanged and failing.

### Compatibility evidence and limits

A/B: Existing intake tests asserted repeat equality/prefixes, not a pinned WIKI ID or
explicit culture. No global culture assignment was found in inspected application,
test or deployment sources. Ambient default is not a portable serialization contract.
Invariant and en-US explicitly reproduce the earlier observed fixture reference ID;
this does not establish any historical production process culture.

C: Published BB-084/085/120/121 reports name `wiki-5713d7dccfa38f56`, but do not give
the exact ordered parsed decimal representations and historical process culture needed
to recompute it. BB-090 also documents a historical adjusted-price importer difference;
current code must not be assumed equivalent to that original importer. Production
compatibility remains UNKNOWN. No production artifact/database was accessed. A separately
authorized read-only snapshot/original-algorithm audit is needed; no re-promotion,
rewriting or migration. The isolated Swedish/Thai test databases do demonstrate stored
fixture IDs that invariant serialization would not reproduce.

D: `EodhdFinanceMarketData.cs` defines revisions.revision_id as primary key and includes
revision_id in the observations composite primary key. `FinanceDatasetIntake.cs` binds
candidate canonical_revision_id and manifest references. `FinanceFeatureStore.cs`
retains source_revisions_json/source_revision_id; `FinanceBacktestStore.cs` retains
market_revisions_json and result JSON. Backtest fingerprints include exact market IDs.
These inspected definitions use application references, not declared SQL FOREIGN KEY
constraints. `FinanceDataProtection.cs` inventories/verifies IDs in backups, and
`FinanceEndpoints.cs` exposes catalog/result readers with those lineage identifiers.
They are externally visible, not disposable implementation details.
BB-129A campaigns currently pin BB-127 research revision IDs/fingerprints, not canonical
WIKI IDs directly; no direct campaign impact is proven.

E: `INSERT OR IGNORE` deduplicates by keys, not semantic rows across different IDs.
Future invariant hashing could therefore add a semantic duplicate alongside a legacy
non-invariant ID through a different candidate/new promotion. The terminal-candidate
fast path protects same-candidate replay but is not cross-candidate semantic deduplication.
This is a source-based risk, not evidence of production duplicates.

### Options — no implementation authorized

| Option | Benefit | Compatibility cost |
| --- | --- | --- |
| A: invariant future hashing, retain existing IDs | Smallest correction; invariant/en-US fixtures match | Pin calendar and every numeric field while preserving scale/order/nulls; silent algorithm change and duplicate risk remain |
| B: explicitly version future identity algorithm, retain legacy IDs | Auditable legacy versus invariant interpretation | Version/prefix contract needs review; versioning alone does not prevent semantic duplicates |
| C: compatibility lookup/aliasing | Could handle proven legacy duplicates without rekeying | Requires duplicate evidence, conflict rules and persistence design; not justified speculatively |

B appears safest as an architectural direction while production compatibility is unknown:
preserve legacy identities and make algorithm interpretation explicit. This approves no
new prefix/schema. A may be selected after a bounded compatibility audit; C requires
actual duplicate evidence. Historical rekeying is neither necessary nor proposed.

### Handoff verification

`dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~FinanceDatasetIntakeTests --logger 'console;verbosity=minimal'`

Result: **21 passed, 1 failed, 0 skipped (22 total)**. Only the original
cross-culture equality regression fails; all four observation cases pass. This is
deliberately not green implementation evidence. No full-suite/CI success is claimed.

## Partial responsibility map retained from initial characterization

This is a partial source map, not a completed A–R characterization or equivalence claim.

- `FinanceOwnerDatasetDropScanner`: ready markers, direct regular-file and size checks,
  external/embedded sidecar claims, artifact-plus-sidecar candidate identity, stability
  checks, bounded `.partial` copying and checksum verification into quarantine. It
  routes to CSV review or workbook research and records caught inspection failures.
- `FinanceDatasetIntakeStore` in `FinanceDatasetIntake.cs`: HTTP download/retry,
  disk/input bounds, artifact binding, CSV/ZIP preparation, CSV parsing and normalization,
  validation counters, canonical overlap reads, thirteen promotion gates, canonical
  promotion transaction, candidate transitions/catalog/manifests and rejected cleanup.
- The same partial store in `FinanceResearchDatasets.cs`: outer ZIP/workbook preflight,
  cached-cell parsing, per-sheet validation and research identity, eligibility calls,
  research persistence/catalog and bounded backtest lineage. XLSX intake writes research
  tables; it does not perform canonical promotion.
- The same partial store in `FinanceResearchCampaigns.cs`: fixed campaign definition,
  revision selection, categorical outcomes, deterministic aggregate identity and storage.
  This is not a separate intake collaborator merely because it is in a separate file.
- Structural DDL and restart reconciliation are present in the store. They have not
  been moved; schema authority remains a separate checkpoint.
- Inspection-only CSV intake stops all-pass evidence at `Approved`; existing terminal
  candidates are returned after artifact checksum verification. A changed artifact
  checksum under the same candidate ID is rejected. This existing replay guard does
  not prove culture independence when independently promoting the same fixture.
- Canonical promotion is transactional, but lifecycle transitions and manifest writes
  surround it separately. Workbook persistence is transactional with separate lifecycle
  transitions. Failure/rollback characterization is still incomplete.
- HTTP acquisition accepts cancellation; synchronous CSV/workbook/scanner operations
  do not take a cancellation token. No cancellation contract has been changed.

Existing tests cover successful CSV promotion/replay, changed-artifact rejection,
unknown rights, owner quarantine and sidecars, ZIP traversal/expansion, symlinks,
inspection-only promotion prevention and workbook eligibility/replay. No extraction
boundary was approved or implemented before the lineage blocker was reproduced.

## Changes

Only characterization tests and evidence/status/backlog/testing/catalog/recovery
documentation are changed. All existing scientific, provider, entitlement, schema,
persistence and trading implementations remain unchanged. Finance remains
**RESEARCH / 0 SEK / NONE**. No credentials, owner artifacts, raw private correspondence
or production database were used. Unrelated local mockups and ADR proposals are preserved.

## Security

Tests use synthetic data and isolated temporary SQLite databases only. No external
acquisition, credential access, production evidence mutation or deployment occurred.

## Remaining work and resumption

The owner required a stop on any pre-existing correctness/scientific/lineage defect.
Do not fix hash formatting inside this refactor: a correction could affect future
canonical IDs and needs an explicit compatibility decision for existing identities.
Do not rewrite any existing immutable revisions or assume a migration is required.

Architect review must decide how to handle culture-independent future identity and
legacy compatibility, and whether to scope a separate correction before resuming.
Full extraction pre-read and remaining call-graph/test review,
the complete A–R map, extraction decision and all remaining verification are unfinished.
This is a blocker handoff, not a mergeable review candidate.

## Resumption

Review the exact handoff branch SHA and canonical recovery note. Do not merge, fix
or resume extraction before an explicit compatibility decision.
