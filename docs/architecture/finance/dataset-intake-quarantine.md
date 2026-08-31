# Finance historical dataset intake and quarantine

## Boundary

`ExternalDatasetCandidate → QuarantineArtifact → ValidationEvidence → PromotionDecision → CanonicalDatasetRevision`.
Canonical readers never parse an arbitrary download. Quarantine lives under the Finance data
volume and is excluded from Git. Candidate/manifests are durable SQLite metadata; raw files
are Finance-owned filesystem objects.

## Deterministic policy

`dataset-promotion-v1` evaluates all thirteen mandatory gates. `FAIL` becomes `REJECTED`,
`UNKNOWN` becomes `MANUAL_REVIEW_REQUIRED`, and only all-pass evidence becomes `APPROVED` then
atomically `PROMOTED`. An interrupted inspection returns to `DOWNLOADED`; no partial revision
exists. Repeating an already terminal candidate is idempotent. A changed artifact checksum
requires a new candidate revision.

OHLCV-v1 rejects malformed/non-positive or impossible bars before normalization. A candidate
passes the aggregate OHLCV gate only when rejected rows are at most 1%; all rejected counts
remain limitations. Conflicting duplicate keys fail. `cross-source-comparison-v1` requires at
least 20 overlapping sessions, uses latest EODHD evidence per symbol/session and classifies
relative close differences as consistent, minor, price-basis, material or insufficient.
Insufficient overlap is a recorded limitation, not evidence of equality.

## Current source decisions (updated 2026-08-31)

- WIKI mirror `kmfranz/trading_pairs`: public-domain evidence is first-party Nasdaq Data Link;
  LFS pointer and downloaded SHA-256 agree. The 2016 snapshot passed and only AAPL, MSFT, JPM,
  XOM and JNJ were promoted to `wiki-5713d7dccfa38f56`.
- Zenodo DOI `10.5281/zenodo.20192822`: CC BY 4.0 covers author curation, but the record says
  rows came from Yahoo Finance/yfinance. Underlying redistribution provenance is unresolved,
  so it remains quarantined/manual-review and published zero canonical rows.
- BB-086 verified that the retained WIKI artifact has zero SPY/QQQ/IWM rows. Eight bounded ETF
  candidates failed before acquisition: no new raw artifact entered quarantine. Host-level CC0,
  CC BY or MIT cannot replace missing underlying market-data rights; Stooq's browser verification
  remains a technical stop condition. Candidate research evidence lives in the BB-086 report.
- BB-120 revalidated that EODHD Free exposes approximately one year at 20 calls/day; longer
  history and Bulk EOD are paid capabilities. Delisted and separate corporate-action access on
  the active Free product remains `UNKNOWN`. No request may infer entitlement from endpoint
  reachability.
- SEC EDGAR and Eurostat have first-party zero-cost/public reuse evidence but are filings/reference
  and macro/statistical sources, not longer daily-OHLCV artifacts. They require their existing or
  a separately approved source-specific semantic path; they are not forced through OHLCV-v1.
- No BB-120 candidate cleared every rights, provenance and technical gate. Zero artifacts were
  acquired, zero candidates were added and zero canonical revisions were created.

## Retention and cleanup

Public-domain WIKI quarantine/canonical evidence is indefinite and backup-eligible when the
backup inventory supports source tags. CC BY evidence retains attribution. Manual-review
artifacts remain until an explicit cleanup decision. Rejected large payloads may later be
removed while retaining sanitized manifest/evidence; cleanup never deletes canonical data.
EODHD retention/deletion remains independent and must never match WIKI/Zenodo by shared symbol.

BB-085 implements the cleanup state `Retained → CleanupPending → PayloadDeleted` only for
aged `Rejected` candidates. The raw payload may be removed, but candidate/file metadata,
SHA-256, manifest, provenance, validation and rejection evidence remain. Restart resolves
an interrupted pending state from actual payload presence. `ManualReviewRequired` and
`Promoted` are not automatic cleanup targets; canonical tables are outside this operation.

## Macro candidate evidence (BB-090 closure)

Macro Memory uses the same evidence philosophy without forcing macro rows through equity OHLCV validation. A FRED artifact is copied into Macro quarantine before parsing; the candidate records the first-party source URL, provider, artifact names/hash, acquisition time, rights evidence, expected series IDs, schema fingerprint, validation result and promotion decision. Only `PASS` can reference an immutable Macro revision. Malformed or wrong-series candidates remain `FAIL/REJECTED` and have no canonical revision. Re-ingesting the same artifact is idempotent.

Revised-history CSV and official API vintage evidence coexist as distinct evidence classes. A point-in-time request must select `POINT_IN_TIME_CAUSAL` explicitly and never falls back to `REVISED_HISTORY_EXPLORATORY`. The optional API key is runtime secret configuration only and is never candidate provenance.
