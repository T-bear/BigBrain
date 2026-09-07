# BB-130C — v2 canonical product metadata blocker

Detta är en sanerad GitHub-version. Source inspection only; no private or market payloads.

## Metadata

- Date: 2026-09-07.
- Verified main baseline: `3bf3bde9c1e2321972e6f31443d740d6b92341f9`.
- Fresh main-derived branch: `bb-130c/versioned-dataset-revision-identity-v2`.
- Accepted [cross-candidate blocker evidence](https://github.com/T-bear/BigBrain/blob/0dcb15b127652e692bbfa0e531baf557975bd619/docs/reports/features/finance/bb-130c-versioned-dataset-revision-identity-20260907.md).
  That non-mergeable commit is not an ancestor and its failing test is not imported.
- Related: [accepted compatibility audit](bb-130c-dataset-revision-identity-audit-20260907.md), ADR 0027.

## Status

**BLOCKER HANDOFF — NOT MERGEABLE**.
**BLOCKED — STABLE CANONICAL PRODUCT METADATA UNAVAILABLE**.
Stopped at the owner's explicit generic-product evidence gate, before production code or
v2 tests. This is a documentation-only handoff, not a correction candidate. No merge,
deployment, migration or intake extraction. Publication history identifies the exact SHA.

## Accepted architectural decision

Future revision identity must bind algorithm version + canonical source + canonical
product + canonical content. CandidateId is lifecycle/provenance only. Equivalent
candidates sharing all three canonical inputs must share one revision and one observation
row set; differing canonical sources/products must remain separate. The observation keys
must use the same scope. This decision is accepted; its implementation is blocked.

All legacy IDs/observations/fingerprints remain immutable. No rekeying, migration, aliases
or historical recomputation. Option B remains the direction. Legacy WIKI compatibility
remains UNKNOWN and is not needed to determine this metadata gap.

## Evidence — source inspection

| Existing descriptor | Actual contract / limitation |
| --- | --- |
| ExternalDatasetCandidate.CandidateId | Lifecycle/provenance identity; explicitly excluded from canonical product by owner decision |
| SourceName | Source label; no generic within-source product discriminator |
| SourceUrl | Acquisition/provenance URL, not an immutable canonical product key; the WIKI fixture uses a repository URL, and owner drop can use a filename fallback |
| OriginalFilename | Artifact name; no documented stability across renames/submissions or uniqueness across products |
| HostingPlatform | Hosting class; shared by many products |
| Rights, Provenance, owner evidence | Rights declarations/free text; no machine-readable canonical product contract |
| PriceBasis, survivorship, expected bytes | Semantics/limitations/size; cannot distinguish two products sharing these properties |
| OwnerDatasetDropMetadata | Has sourceProvider, originalUrl and claims but no canonical product descriptor; unknown sidecar fields are rejected |
| Workbook dataset_id | Per-sheet research identity, not a canonical CSV promotion product; not on ExternalDatasetCandidate or passed to Promote |

Inspected current sources:

- `src/BigBrain.Modules/Finance/ExternalDatasetIntake.cs`: complete candidate record and rights types.
- `src/BigBrain.Api/Finance/FinanceDatasetIntake.cs`: Discover, Promote, PersistDecision and fixed CLI candidate declarations.
- `src/BigBrain.Api/Finance/FinanceOwnerDatasetDrop.cs`: sidecar type, strict field handling and Candidate construction.
- `src/BigBrain.Api/Finance/FinanceResearchDatasets.cs`: dataset_id parsing and research fingerprint path.
- `docs/operations/runbooks/finance-owner-market-data-drop.md`: owner-sidecar contract.

WIKI has an explicit existing mapping to `WIKI/PRICES`. It can conceptually retain that
product. The fixed Zenodo record URL/label identifies one curated package, but code and
documentation do not define it as a reusable canonical product key. It cannot establish
a generic rule for every source or product. Zenodo's unresolved rights remain unchanged.

Persisted candidate rows/manifest copy these descriptors; they do not add an independent
canonical product. The current observations.product value is WIKI/PRICES or CandidateId,
which is the previously accepted blocker, not a source of safe v2 product identity.

## Conclusion and scope limits

There is no existing generic descriptor with the required canonical product guarantees.
Choosing SourceUrl, filename, a combination, a universal product constant or a content hash
would introduce a new identity interpretation. It could split equivalent submissions or
collapse different products; these are contract risks, not additional measured production
defects. No external lookup or production-data scan can supply the missing input contract.

The owner's instruction is to stop if existing metadata cannot safely supply that key.
Accordingly no fallback or WIKI-only partial correction is implemented. This decision
also avoids expanding scope by adding a product field or changing promotion eligibility
without a reviewed missing-product policy.

## Changes

This report plus STATUS, BACKLOG, TESTING, Finance module/BB-130 plan, report catalog and
canonical recovery note record the accepted source/product/content decision and the blocker.
No production file or test file changed. The earlier deliberately failing characterization
stays only on its own NOT MERGEABLE branch.

## Verification

Baseline, clean tracked tree and fresh branch ancestry were verified. Unrelated untracked
mockups and four Sentinel ADR proposals are preserved and excluded. Source inspection
reused accepted blocker/audit evidence; it did not rerun culture or duplicate-row tests.
No new behavioral test is appropriate for the absence of an approved metadata contract.
No full API/Sentinel/Web suites or Release solution build are claimed for this docs-only
handoff. Correction gates remain required after an authorized implementation.

Publication checks passed: `node scripts/verify-documentation.mjs` (227 Markdown files,
90 unique backlog IDs), `git diff --check`, staged diff check and staged Gitleaks v8.28.0
(no leaks).

## Security

Finance remains **RESEARCH / 0 SEK / NONE**. No production data was read or mutated.
No provider activation, credentials, quarantine/rights/eligibility changes, scientific
calculations, schema changes, historical rewrite, aliases, acquisition or deployment.

## Remaining work

Smallest recommended next decision: explicitly define canonical source/product metadata
for future canonical candidates, its authoritative assignment and validation, the fixed
WIKI mapping, and fail-closed behavior when the product is absent. Decide how new metadata
is retained in existing immutable candidate/manifest structures without a schema migration.
An owner sidecar claim must not silently become verified product or rights authority.
This is a proposal for review, not an implemented or accepted metadata design.

Once authorized, a fresh main-derived correction checkpoint can add that bounded metadata
contract and implement v2 serialization/storage together. It must still test cross-candidate
row-count stability, source/product separation, culture/calendar determinism, scale/null/
field/order/encoding rules, restart and unchanged legacy lineage. Both earlier defects
remain unresolved. Intake responsibility extraction and other BB-130C/D work remain deferred.

## Resumption

Architect reviews this exact remote **BLOCKER HANDOFF — NOT MERGEABLE** SHA and approves
the missing canonical product metadata contract before correction resumes. Do not merge
this branch or either previous blocker branch. No new checkpoint starts automatically.
The canonical recovery note and linked reports suffice; terminal transcripts are unnecessary.
