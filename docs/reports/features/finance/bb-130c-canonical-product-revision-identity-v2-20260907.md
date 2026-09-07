# BB-130C — canonical product metadata and revision identity v2

Detta är en sanerad GitHub-version. Only source findings and synthetic fixture evidence.

## Metadata

- Date: 2026-09-07.
- Required main baseline: `3bf3bde9c1e2321972e6f31443d740d6b92341f9`.
- Branch: `bb-130c/canonical-product-revision-identity-v2`, created directly from main.
- [Accepted compatibility audit](bb-130c-dataset-revision-identity-audit-20260907.md).
- Accepted [row-growth blocker](https://github.com/T-bear/BigBrain/blob/0dcb15b127652e692bbfa0e531baf557975bd619/docs/reports/features/finance/bb-130c-versioned-dataset-revision-identity-20260907.md)
  and [metadata blocker](https://github.com/T-bear/BigBrain/blob/56f9a546399b82afe521d02b6c5203031e49daf7/docs/reports/features/finance/bb-130c-versioned-dataset-revision-identity-v2-20260907.md)
  are reused evidence, not implementation ancestors. Their branches remain NOT MERGEABLE.

## Status

**REVIEW CANDIDATE ONLY — NOT MERGED TO MAIN**. The owner authorized the bounded
Option B correction and explicit product metadata. Implementation and local verification
are described below; no main acceptance, GitHub CI, deployment or runtime verification
is claimed by branch publication. Intake responsibility extraction has not resumed.

## Decisions and identity metadata

`ExternalDatasetCandidate.CanonicalProduct` is an optional additive identity claim.
The existing SourceName supplies canonical source; both source and product must pass
strict validation before any new canonical promotion. There is no fallback from CandidateId,
filename, path, URL, time or content. Those fields are provenance/lifecycle, not product.

Each identifier trims surrounding whitespace, accepts 1–64 ASCII characters, requires
an ASCII letter/digit first, and permits only letters/digits/dot/underscore/hyphen thereafter.
Validated text is uppercased invariantly. Unicode letters, delimiters, embedded whitespace,
paths and empty/oversized values are rejected, not transliterated. No synonym/alias mapping
is added. Different normalized source/product scopes remain different identities.

The fixed WIKI candidate explicitly supplies product `PRICES`. Its source key remains
`NASDAQ-WIKI`, the existing canonical provider name used by Finance backup/rights readers;
this represents the conceptual WIKI/PRICES source/product without renaming historical
provider storage or changing rights classification. `WIKI` is not introduced as a second
provider alias. Generic program-defined candidates must explicitly supply their known product;
Zenodo remains without an inferred product and independently blocked by existing provenance.

Owner sidecar adds optional `canonicalProduct`. When present, both sourceProvider and product
must satisfy the identifier grammar. The value is an identity claim only; it supplies no
rights, provenance, entitlement, adjusted-price or scientific authority. Unknown sidecar
fields remain rejected. Old sidecars without the field retain review-only intake behavior;
owner files never automatically become canonical. XLSX remains research-only.

## Metadata retention and fail closed

Discovery writes the new product claim into the existing manifest_json column only for newly
inserted candidates. Source is already stored in the candidate row. Before nonterminal CSV
inspection, the stored source/product claim is read back; a caller cannot replace the claim
under an existing CandidateId after restart. Decision manifests retain the product claim and,
for promoted v2 evidence, explicit normalized source/product and algorithm. Workbook manifests
also retain the optional claim without feeding it into research identity/eligibility calculations.
No DDL, migration version or existing-row backfill is added. A newly discovered candidate
now has a claim manifest, so ManifestRetained can be true before validation; it is not a
claim that validation or canonical promotion has completed.

The existing terminal fast path remains ahead of claim processing: after the existing artifact
checksum check it returns the stored candidate. Legacy promoted IDs and terminal decisions are
not reinterpreted or rewritten. A nonterminal legacy candidate without a stored product cannot
be retrofitted by supplying a new caller argument; it requires a new candidate for v2 promotion.

All thirteen existing promotion gates run unchanged. An otherwise all-pass promotion with
missing/invalid canonical source/product becomes Rejected / Fail with limitation
`CanonicalSourceOrProductMissingOrInvalid`, before canonical insertion. Rights/provenance
failures keep their existing result. The metadata prerequisite does not convert Unknown into Pass.
The existing inspection-only Approved state still requires separate explicit promotion review;
this change adds no promotion endpoint or owner-drop promotion mechanism.

## Exact v2 byte contract

`CanonicalDatasetRevisionIdentityV2.Algorithm` is `canonical-dataset-revision-v2`.
Hash input is the following UTF-8 text (no BOM):

```text
canonical-dataset-revision-v2
<NORMALIZED_SOURCE>
<NORMALIZED_PRODUCT>
<symbol>|<date>|<open>|<high>|<low>|<close>|<adjusted-close>|<volume>
<next row, if any>
```

- Every displayed line separator is one literal LF, never Environment.NewLine.
- No terminal LF. No trailing field delimiter. Header and rows belong to the same hash.
- Only the existing safely mapped selected rows are included; validation/mapping is unchanged.
- Rows sort by symbol using StringComparer.Ordinal, then DateOnly ascending. Source row number
  and input row ordering are not hashed. Symbols already pass trim/invariant-upper mapping;
  the serializer also rejects empty symbols or pipe/CR/LF delimiter ambiguity.
- Dates explicitly use `yyyy-MM-dd` with InvariantCulture (Gregorian).
- OHLC and volume are decimal; adjusted close is nullable decimal. Each non-null decimal uses
  default decimal ToString(InvariantCulture). Scale is preserved: `100` and `100.00` are distinct
  representations. No numeric-scale normalization, grouping or scientific reinterpretation.
- Null adjusted close is the empty field between two pipes. Non-null adjusted close retains
  its decimal scale. It is distinct from raw close and remains part of current intended semantics.
- SHA-256 hashes the exact bytes; all 64 digest hex characters are lowercase invariant.
- Checksum: `sha256:<64 hex>`. Revision: `dataset-v2-<64 hex>`. The full digest and explicit
  version prefix distinguish v2 from every historical source-prefixed unversioned identity.

CandidateId, filename/URL, artifact checksum, rights/provenance text, acquisition time,
source row number and corporate-action annotation strings are not canonical hash inputs.
Corporate-action records remain candidate-bound evidence under the existing contract.
This does not claim cross-provider semantic equivalence or normalize decimal scale.

## Storage consistency and legacy compatibility

The exact normalized source/product used in the hash are also observations.provider/product.
Within the existing transaction, identical revision + source + product + instrument + date
hits the same composite primary key. INSERT OR IGNORE therefore leaves one row set for
independently submitted equivalent candidates. Both candidates bind to that same revision;
revisions.observation_count and Catalog promoted counts remain aligned with physical rows.
There is no post-insertion cleanup or alias table. Changed source/product/content creates
new immutable scope. Canonical product never comes from CandidateId.

Historical IDs and row sets coexist with new v2 evidence. An equivalent new candidate can
create v2 evidence beside a legacy revision; no claim of cross-version semantic deduplication
is made. Same-candidate legacy terminal replay remains the original ID. No historical features,
backtests, robustness, campaigns or fingerprints are regenerated or updated by this change.
Exact production WIKI legacy compatibility remains UNKNOWN; no production recomputation was
needed or performed. Existing legacy checksum semantics are not redefined as v2.

## Evidence — synthetic deterministic examples

Pinned two-row fixture, source NASDAQ-WIKI / product PRICES:

```text
AAPL|2024-01-02|100|102|99|101||1000
MSFT|2024-01-02|50|51|49|50.5||2000
```

V2 revision:
`dataset-v2-31c6ed4a006c7514de08409dca1998be6c8e7af9ee2e06a72cb349f69aa15522`.
V2 checksum:
`sha256:31c6ed4a006c7514de08409dca1998be6c8e7af9ee2e06a72cb349f69aa15522`.
The digest was independently calculated from the literal byte vector and pinned in tests.

Synthetic preserved legacy fixture:
`wiki-dde8381ccbe9ccc5`, checksum
`sha256:dde8381ccbe9ccc5c416332ae2d92402f4b90c7e97578e206b36f73d3bd61bfe`.
Its stored observations, candidate binding/manifest and revision checksum/count/time are
snapshot-compared across store restart, terminal replay and a new v2 promotion without changes.
These are synthetic identifiers, not recomputed production evidence.

Tests exercise mixed CurrentCulture/CurrentUICulture and invariant/en-US/sv-SE/th-TH,
including the Thai non-Gregorian process calendar. They pin exact bytes and IDs, same and
separate candidate replay, normalized storage keys/counts, product/source separation,
every identity field, decimal scale/nulls, row ordering, frozen claims, malformed IDs,
rights/provenance separation, sidecar unknown-field rejection and workbook claim retention.
Restart evidence comprises closing/recreating stores over the same isolated SQLite file;
the full test invocation uses a separate test-host process and rechecks the pinned IDs.
No production process restart is claimed.

A separate bounded CLI probe additionally launched three actual processes using the built
Release API maintenance early-return path (`dotnet <built-api-dll> finance-dataset-intake wiki
<synthetic-csv>`). Environment was restricted to PATH, LANG/LC_ALL and explicit temporary
Finance database/payload/quarantine paths; no server was hosted or external data acquired.
Process 1 used LANG/LC_ALL=C; process 2 used sv_SE.UTF-8 against the same isolated database;
process 3 used th_TH.UTF-8 against a fresh isolated database. All returned the pinned v2 ID
and two observations. Entire revision rows, observation rows and candidate manifest/binding/time
were identical before/after process 2. These locale environment settings are process evidence;
explicit .NET culture/calendar assignments are independently asserted by the unit tests above.
Temporary directories were disposed after the probe. No production paths/configuration were used.

## Changes

Production:
- `src/BigBrain.Api/Finance/CanonicalDatasetRevisionIdentityV2.cs`: focused versioned serializer and scope validation.
- `src/BigBrain.Api/Finance/FinanceDatasetIntake.cs`: new-candidate product retention, frozen claim read, fail-closed prerequisite, v2 hash/storage scope, WIKI product declaration.
- `src/BigBrain.Modules/Finance/ExternalDatasetIntake.cs`: additive optional CanonicalProduct.
- `src/BigBrain.Api/Finance/FinanceOwnerDatasetDrop.cs`: bounded optional identity claim; unknown fields and independent gates preserved.
- `src/BigBrain.Api/Finance/FinanceResearchDatasets.cs`: one additive manifest claim only; research fingerprints/eligibility untouched.

Tests: new CanonicalDatasetRevisionIdentityTests; focused additions to FinanceDatasetIntakeTests
and FinanceResearchDatasetTests; future-promotion fixtures in intake/protection tests now explicitly
supply PRICES. The intake count assertion for newly promoted evidence intentionally uses the new
v2 prefix. No legacy expected identity was replaced by a v2 expectation.

## Verification

Focused command:

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~CanonicalDatasetRevisionIdentityTests|FullyQualifiedName~FinanceDatasetIntakeTests|FullyQualifiedName~FinanceDataProtectionTests|FullyQualifiedName~FinanceResearchDatasetTests' --logger 'console;verbosity=minimal'
```

Result: **62/62 passed, zero failed/skipped**. An earlier run had one obsolete future-promotion
prefix assertion (`wiki-%`); it was corrected to the explicitly authorized v2 contract. The new
culture/storage assertions passed; legacy snapshot expectations were not relaxed.

`dotnet build BigBrain.slnx --configuration Release`: passed, **0 warnings / 0 errors**.
Full-suite command: `dotnet test BigBrain.slnx --configuration Release --no-build --logger 'console;verbosity=minimal'`.
Result: **BigBrain.Api.Tests 640/640; BigBrain.Sentinel.Tests 32/32; zero failed/skipped**.
This includes the affected integration, dataset/research/campaign, entitlement, deterministic
engine, schema/restart and architecture tests. No unexpected existing Finance expectation changed.

Web source/API response shapes are unchanged; canonical revision IDs remain opaque strings.
No Web build/test rerun is required for the additive local sidecar/candidate manifest contract.
`docker compose -f compose.yaml config --quiet`: passed; no Compose/runtime configuration changed.

Publication gates passed: `node scripts/verify-documentation.mjs` (227 Markdown files,
90 unique backlog IDs), `git diff --check`, `git diff --cached --check`, and staged
Gitleaks v8.28.0 (no leaks). Main remained at the required baseline; unrelated local
files are excluded. GitHub CI has not been claimed for this review-only publication.

## Security

Finance remains **RESEARCH / 0 SEK / NONE**. No production Finance data access/mutation,
provider calls/activation, credentials, historical rewrite, schema migration, aliases,
scientific calculation change, trading authority or deployment. Tests use isolated temporary
SQLite files. Unrelated local mockups and Sentinel proposals are preserved/excluded.

## Remaining work

Architect reviews this exact branch SHA. Legacy cross-version semantic duplicates are not
aliased/deduplicated, and production WIKI hash compatibility stays UNKNOWN. Product metadata
is identity, not external evidence; independent gates still apply. The v2 implementation is
not active on the server without separate deployment approval. BB-130C intake extraction,
persistence/schema ownership and other remaining responsibilities, plus BB-130D gates,
remain separate checkpoints. No next checkpoint starts automatically.

## Resumption

Use the exact branch publication SHA and canonical recovery note for review. Merge requires
explicit approval of this exact SHA with unchanged main. Do not merge the old blocker branches.
Do not deploy or resume intake extraction as part of this checkpoint.
