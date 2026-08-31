# BB-121 — Nasdaq WIKI Historical Recovery & Stooq Requalification

Detta är en sanerad GitHub-version. No secret, credential, private address, account identifier, raw market row, raw provider payload, raw log or sensitive filesystem path is published.

## Metadata

- Date/runtime snapshot: 2026-08-31
- Published baseline: `98c9bdb040ef5f1f7f22e40dac759dfcd62f42b7`
- Scope: read-only WIKI artifact forensics, first-party Nasdaq/Stooq requalification and canonical-scope decision
- Finance boundary: `RESEARCH / 0 SEK / NONE`

## Status

**FORENSICALLY VERIFIED / WIKI STATE A / STOOQ HUMAN CONFIRMATION REQUIRED / NO ACQUISITION / NO PROMOTION**.

BB-120's published outcome was verified from `origin/main` before work. No source, schema, configuration, canonical data or container changed. No account was created, no protected route was probed, no download occurred and no communication was sent.

## Evidence

### Persisted WIKI artifact inventory

| Property | Verified value |
|---|---|
| Candidate | `wiki-eod-mirror-kmfranz-v1` |
| Dataset | `NASDAQ-WIKI / WIKI/PRICES` |
| Existing canonical revision | `wiki-5713d7dccfa38f56` |
| Acquisition source | pinned Git LFS mirror of Nasdaq/Quandl WIKI/PRICES |
| Acquisition date | 2026-08-15 BB-084 intake |
| License/provenance | Nasdaq first-party public-domain statement; mirror bytes pinned by Git/LFS identity |
| Raw format | UTF-8 CSV, 14 WIKI fields: ticker/date, raw OHLCV, dividend/split and adjusted OHLCV |
| Size | 235,562,224 bytes, uncompressed |
| SHA-256 | `dd5127aae478d270150904fcbad6e96a42e461e13c3d48a1587edb9b89cea43e` |
| Raw data rows | 2,166,605 |
| Accepted/rejected | 2,155,310 / 11,295 (0.521% rejected) |
| Ticker strings | 3,186 |
| Session range | 2014-01-02–2016-12-19 |
| Identical duplicates / conflicting duplicate keys | 0 / 0; raw rows equal accepted plus rejected rows and the conflict gate passed |
| Compression | none in the persisted candidate file |

The artifact contains no dates outside 2014–2016 and therefore no decades of history. It is a credible partial 2016 snapshot, not WIKI's later final 2018 release and not proof of complete history for every ticker.

Coverage observations from a read-only aggregate scan:

| Measure | Count |
|---|---:|
| First observed in 2014 / 2015 / 2016 | 3,166 / 13 / 7 |
| Last observed in 2014 / 2015 / 2016 | 100 / 218 / 2,868 |
| Last observation before dataset end | 546 |
| First observation after dataset start | 101 |

These are price-file coverage facts. A ticker disappearing before 2016-12-19 does not prove delisting; appearing later does not prove an IPO. Ticker reuse/change cases are **UNKNOWN** because the artifact has no authoritative historical security master.

### Why only five symbols are canonical

The cause is explicit code policy, not a failed gate. `Promote` filters validated rows against the eight exact `EodhdCatalog.Watchlist` symbols and assigns their configured canonical instrument ID/MIC. The snapshot contains:

| Symbol | Raw snapshot rows | Range | Canonical status |
|---|---:|---|---|
| AAPL | 748 | 2014-01-02–2016-12-19 | all available rows promoted |
| JNJ | 748 | 2014-01-02–2016-12-19 | all available rows promoted |
| JPM | 748 | 2014-01-02–2016-12-19 | all available rows promoted |
| MSFT | 748 | 2014-01-02–2016-12-19 | all available rows promoted |
| XOM | 732 | 2014-01-02–2016-12-19 | all available rows promoted |
| SPY / QQQ / IWM | 0 | not present | no rows available |

Total canonical WIKI observations are therefore 3,722. The remaining 3,181 ticker strings have no approved mapping to a canonical instrument, venue or effective validity interval. This was the intended bounded BB-084 pilot scope.

The 13 gates evaluate candidate integrity, rights/provenance, schema/semantics, dates, OHLCV, conflicts, at least one safe symbol mapping, explicit survivorship classification, corporate-action representation, overlap classification and retention. They passed for the candidate. Promotion then selected only safely mapped rows. `INSUFFICIENT_OVERLAP` was recorded because WIKI 2014–2016 does not overlap EODHD 2025–2026; it was not treated as source agreement.

The same immutable file can be re-read without downloading. A wider promotion is technically possible only as a new candidate/revision with a distinct deterministic scope and explicit mappings; it must not mutate the old revision. Current capability cannot safely promote the unmapped cohort because ticker strings do not establish historical instrument identity or venue validity. No code change is justified until that evidence exists.

### Historical identity and scientific classification

- **A — Price-history evidence:** present, immutable and reproducible for accepted rows.
- **B — Historical security identity:** known only for the five explicitly mapped instruments; unknown for the wider cohort.
- **C — Historical universe membership:** absent; `SurvivorshipUnknown` remains authoritative.
- **D — Delisting evidence:** absent. Early final observations are not silently classified as delistings.
- **E — Corporate-action evidence:** dividend/split and adjusted columns exist, but the old revision was later audited `ADJUSTED_SEMANTICS_INVALID`; raw research remains allowed and adjusted research fails closed.

Venue changes, ticker reuse, listing dates, membership history and knowledge-time availability cannot be derived safely from this price file. WIKI is frozen historical evidence, not point-in-time universe evidence.

### Nasdaq WIKI first-party revalidation

Nasdaq Data Link's current documentation still identifies WIKI database 4922 as free, non-premium, end-of-day prices/dividends/splits released into the public domain. Free data requires registration for an API key. Individual time-series API examples remain documented. The same first-party time-series documentation states that entire-database full/partial ZIP export is restricted to premium products and unavailable for free datasets.

The rights and access conclusions are separate:

- Rights: public-domain statement remains first-party evidence for WIKI data.
- Current individual access: documentation indicates a free-account API-key path, but BB-121 did not create an account or request data.
- Current full-table/bulk access: not established; the documented entire-database route excludes free datasets.
- Dataset status: frozen/deprecated historical evidence; current docs preserve legacy WIKI examples but do not prove that a complete final export remains obtainable today.
- Local artifact completeness: partial 2016 snapshot, proven by its own date range; not a complete final WIKI archive.

Sources reviewed 2026-08-31: [Nasdaq Data Link usage/WIKI metadata and full-download restrictions](https://docs.data.nasdaq.com/v1.0/docs/in-depth-usage), [data organization and WIKI free classification](https://docs.data.nasdaq.com/v1.0/docs/data-organization), and [Tables API authentication](https://docs.data.nasdaq.com/docs/api-and-analysis-tools-for-tables-data).

### WIKI state and pilot decision

**STATE A — the existing persisted artifact already contains useful large history. Do not redownload it.**

No expansion pilot occurred. Additional years for the five mapped instruments do not exist in this snapshot, and every available row is already canonical. Additional instruments would test a missing historical-security-master evidence set rather than the existing intake path. Adding speculative mappings solely to increase row count would weaken identity integrity. Before/after canonical WIKI counts are therefore 3,722 / 3,722, one unchanged revision.

### Stooq 2026 requalification

BB-072/075/076/082/086 were reviewed first. BB-086's one ordinary official CSV request returned browser verification HTML and correctly stopped without challenge bypass.

Current first-party Stooq pages visibly provide historical-data views and describe upstream suppliers for US equities and other asset classes. They do not publish sufficient first-party evidence for an official historical-data API/key enrollment, free automated bulk access, bounded automation rights, private persistent storage, deterministic backtesting, derived-data retention, backups, post-account retention, rate limits, raw/adjusted semantics, corporate actions or historical/delisted coverage. Search results and third-party adapters are discovery leads only and were not treated as entitlement.

Qualification: **HUMAN CONFIRMATION REQUIRED / NO ACQUISITION**. The previous browser/access-control boundary remains. No CAPTCHA, JavaScript challenge, alternate mirror or third-party API was used.

### Sanitized Stooq inquiry draft — not sent

> I am a private individual evaluating Stooq historical daily OHLCV for non-commercial research concerning only investment of my own funds. Could you confirm whether your current terms permit programmatic API access and bounded automated downloads; persistent raw-data storage on a private self-hosted server; normalization; deterministic backtesting; retained derived features/metrics; versioned backups; and continued retention of raw and derived evidence after account termination? Please also specify whether historical/delisted securities are supplied, the raw/adjusted and corporate-action semantics, applicable rate limits, whether human/CAPTCHA registration is required, and whether all these uses are allowed without redistribution or public display. If an API key is required, please identify the official free enrollment route and applicable product/terms.

## Changes

Documentation only: Status, Backlog, Finance module, intake architecture, testing guidance, report catalog and this report. Production source, schema, configuration and runtime data are unchanged.

## Security

Only existing read APIs, aggregate inspection of the retained immutable file and public first-party documentation were used. No raw row, credential, account, private address/path or challenge material is published. No download, promotion, external message, protected-route bypass, deletion or provider stitching occurred.

## Remaining work

For WIKI expansion, obtain authoritative historical security identity, venue and validity mappings for a small cohort before proposing a new candidate scope. Separately, the owner may send the Stooq inquiry and record the first-party response. Unknown Stooq rights remain fail-closed.

## Resumption

Resume from `STATE A`, the immutable artifact checksum and unchanged `wiki-5713d7dccfa38f56`. Do not redownload WIKI. Do not add mappings from ticker text alone. Do not acquire Stooq until a first-party answer clears the exact intended uses.
