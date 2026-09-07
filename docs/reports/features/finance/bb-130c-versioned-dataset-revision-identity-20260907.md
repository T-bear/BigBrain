# BB-130C — versioned revision identity: cross-candidate lineage blocker

Detta är en sanerad GitHub-version. Evidence uses synthetic fixtures only.

## Metadata

- Date: 2026-09-07.
- Required and verified main baseline: `3bf3bde9c1e2321972e6f31443d740d6b92341f9`.
- Branch: `bb-130c/versioned-dataset-revision-identity`, directly from that baseline.
- Original scope: future versioned invariant canonical identity (Option B), with all legacy IDs retained.
- Publication scope after mandatory stop: one two-case characterization test and sanitized documentation only.
- Related: [accepted compatibility audit](bb-130c-dataset-revision-identity-audit-20260907.md), ADR 0027.

## Status

**BLOCKER HANDOFF — NOT MERGEABLE**.
**BLOCKED — CROSS-CANDIDATE CANONICAL REVISION ROW GROWTH**.
The owner-authorized v2 correction stopped during characterization, before any production
change. No v2 algorithm, extraction, migration, merge, deployment or runtime approval.
The publication commit identifies this handoff's SHA. This is deliberately failing
regression evidence, not a green implementation candidate.

## Evidence

The new `EquivalentCandidatesMustNotAppendRowsToAnExistingCanonicalRevision` theory
runs the existing two-row synthetic CSV through the existing all-pass candidate fixture.
It explicitly sets CurrentCulture and CurrentUICulture to invariant and restores both
in finally. Each case uses its own temporary database. No production data is read.

1. Promote candidate A: status Promoted, two canonical observations.
2. Replay A: same ID and still two observations.
3. Submit the exact same artifact as candidate B; only CandidateId changes.
4. Both artifacts have equal checksums and both candidates reach Promoted.
5. Both candidates bind the same canonical revision ID. There is one revisions row,
   its observation_count remains two, and there are two promoted candidate records.
6. Assert the canonical observations still number two.

| Same source for A and B | Same-candidate replay | Equivalent candidate B | Final assertion |
| --- | --- | --- | --- |
| NASDAQ-WIKI | 2 observations | 2 observations, same revision | PASS |
| SYNTHETIC-SOURCE | 2 observations | 4 observations, same revision | FAIL: expected 2, actual 4 |

The test pins the required immutable row-set contract; it does not change its expectation
to tolerate the defect. No cross-provider equivalence claim is involved: each case uses
one identical source and identical artifact bytes. This failure is independent of culture.

Exact command:

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~EquivalentCandidatesMustNotAppendRowsToAnExistingCanonicalRevision --logger 'console;verbosity=minimal'
```

Result: **1 passed, 1 failed, 0 skipped**. The sole failure is the final row-count assertion
in the generic-source case; all preceding identity/checksum/replay/count assertions passed.
The test project and dependencies compiled in Release. This is not a full solution-build
or full-suite result. An interrupted first invocation had no recoverable result. A subsequent
sandbox attempt failed at MSBuild local IPC permissions; the authorized unsandboxed run
produced the result above. No successful verification was needlessly repeated.

Publication gates passed: `node scripts/verify-documentation.mjs` (227 Markdown files,
90 unique backlog IDs), `git diff --check`, staged diff check and staged Gitleaks v8.28.0
(no leaks). No GitHub CI success is claimed for this deliberately non-mergeable handoff.

## Source explanation and impact

`FinanceDatasetIntakeStore.Promote` computes the legacy content hash without CandidateId
or product. A fixed source therefore gives both candidates the same revision ID.
It separately chooses product `WIKI/PRICES` for NASDAQ-WIKI, but CandidateId for every
other source. Observations use the composite primary key
`(provider, product, instrument_id, session_date, revision_id)`.

Consequently `INSERT OR IGNORE` deduplicates WIKI rows across these candidates but inserts
the generic rows again because product differs. The revisions insertion ignores the
existing revision key, preserving the old checksum/count while its associated row set grows.
`Catalog()` counts observations by revision ID, so the old candidate's displayed promoted
count can also grow. That catalog implication follows from source; the test directly proves
the physical row growth and stale revisions count, not a downstream research result change.

ADR 0027 specifies immutable source-specific canonical revisions. The discrepancy prevents
a safe content-deduplication contract for the requested v2 wiring. Merely making the hash
invariant or adding a version marker would leave this disagreement between identity and
storage keys intact. No production occurrence or affected feature/backtest result is claimed.

## Changes

- `tests/BigBrain.Api.Tests/FinanceDatasetIntakeTests.cs`: one narrowly scoped two-case
  theory using the existing isolated fixture, with a deliberately failing generic-source assertion.
- This report, TESTING, STATUS, BACKLOG, report catalog and canonical recovery record the blocker.
- No file under src changed. No production correction or responsibility extraction.

## Compatibility and unfinished v2 contract

Option B remains the accepted direction: explicit future invariant version, preserve every
legacy ID. Exact existing WIKI compatibility remains UNKNOWN. No original-artifact or
production-database query is needed or performed for this newly reproduced blocker.
No historical rekeying, alias layer or schema change is authorized or implemented.

The v2 serialization contract and its culture/calendar/scale/field-change/restart tests
have not been completed. The previously accepted culture blocker is reused as evidence,
not recharacterized here. Legacy checksums outside the test are neither recomputed nor
rewritten. Do not represent this branch as the v2 correction.

## Security

Finance remains **RESEARCH / 0 SEK / NONE**. Synthetic rights declarations are test fixtures,
not external entitlement decisions. No production-data mutation, acquisition, credentials,
raw market evidence, sensitive paths, provider activation, scientific retuning or deployment.
Rights/provenance gates, quarantine, research eligibility and existing schema are unchanged.
Unrelated local mockups and ADR proposals are excluded and preserved.

## Remaining work

Architect/owner review must settle the relationship between canonical content identity,
source/product scope and independently submitted candidate provenance before separately
authorizing a correction. In particular, decide whether equivalent candidates share one
canonical product/row set or require distinct product-scoped identities. Either solution
needs an explicit contract; this handoff chooses and implements neither.

After that decision, a separately authorized main-derived checkpoint can implement and test
the bounded v2 contract and required promotion wiring. Full API, Sentinel/architecture and
solution Release gates remain unrun because mandatory stop was reached before implementation.
The original culture defect is still unresolved; intake extraction and remaining BB-130C/D
work remain stopped/outstanding. No new BB ID is allocated.

## Resumption

Review the exact remote SHA of this **BLOCKER HANDOFF — NOT MERGEABLE** branch.
Do not merge it. Do not continue the correction or extraction without the separate bounded
authorization required by AGENTS.md. Use the canonical recovery note and this report;
terminal transcripts are unnecessary.
