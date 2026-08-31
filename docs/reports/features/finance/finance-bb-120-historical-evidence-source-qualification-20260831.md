# BB-120 — Historical Evidence Expansion & Source Qualification

Detta är en sanerad GitHub-version. No secret, credential, private address, account identifier, raw provider payload, raw log or sensitive filesystem path is published.

## Metadata

- Date/runtime snapshot: 2026-08-31 15:56 UTC
- Published baseline: `940afa78932bf22019f2f45e3ef640afc2c9b5f3`
- Scope: existing-capability inventory, first-party source qualification and read-only catalog verification

## Status

Outcome: **SOURCE-QUALIFIED / FAIL-CLOSED / NO PILOT**.

Finance remains `RESEARCH / 0 SEK / NONE`. No paid product, broker, order path, PAPER, LIVE or AUTO capability was added. No provider request, download, database mutation or deployment occurred. The absence of a pilot is intentional: no newly assessed longer-history source cleared all cost, entitlement, provenance and technical gates.

## Evidence

### Existing capability inventory and duplicate work avoided

The implemented BB-084+ chain is authoritative:

`ExternalDatasetCandidate → QuarantineArtifact → ValidationEvidence → PromotionDecision → CanonicalDatasetRevision`

It already supports bounded HTTPS acquisition, CSV and ZIP-contained CSV, maximum size/file-count/extracted-size controls, multi-year artifacts, SHA-256 identity, durable candidate/manifests, restart recovery and idempotency. Canonical readers never parse downloads directly.

`dataset-promotion-v1` has 13 mandatory gates. Generic policy gates are integrity, license, provenance and entitlement/retention. Equity/OHLCV-specific gates cover schema, field and price-basis semantics, date/time and market session, valid OHLCV, duplicates/conflicts, symbol mapping, survivorship coverage, corporate actions and source overlap. `FAIL` rejects; `UNKNOWN` requires manual review; only all-pass evidence promotes.

Existing validation detects malformed rows, non-positive/impossible bars, conflicting keys, bounded invalid-row ratios, calendar/session and instrument mapping, explicit raw/adjusted basis, corporate-action capability, provenance, license/retention rights, checksum changes, coverage and cross-source agreement. Survivorship, historical identity and point-in-time limitations remain explicit rather than silently repaired. The system can ingest multi-year CSV/ZIP without an architectural change. No second importer framework, registry, quarantine, provenance model, entitlement model or comparison mechanism was justified.

### Current runtime evidence (read-only)

`GET /api/v1/modules/finance/datasets` at 2026-08-31 15:56 UTC returned exactly two existing candidates:

- `NASDAQ-WIKI` is `Promoted`, revision `wiki-5713d7dccfa38f56`, with 2,155,310 validated source rows, 3,186 source instruments and 3,722 canonical observations for AAPL/JNJ/JPM/MSFT/XOM. Its session range is 2014-01-02–2016-12-19; limitations include unknown survivorship and 11,295 rejected invalid rows.
- Zenodo record 20192822 remains `ManualReviewRequired`; underlying Yahoo Finance/yfinance redistribution provenance is unresolved. It has zero promoted rows and no canonical revision.

No runtime count or revision changed during BB-120.

### EODHD Free entitlement re-evaluation

First-party evidence reviewed on 2026-08-31:

Evidence references: [EODHD pricing](https://eodhd.com/pricing), [EOD historical API](https://eodhd.com/financial-apis/api-for-historical-data-and-volumes), [Bulk API](https://eodhd.com/financial-apis/bulk-api-eod-splits-dividends) and [EODHD terms](https://eodhd.com/financial-apis/terms-conditions).

- Product: EODHD Free, 0 SEK, personal/non-commercial research.
- Capacity: 20 calls/day and 20 requests/minute.
- Historical EOD depth: past year on Free; deeper/30+ year history requires a paid plan.
- EOD semantics: raw OHLC, adjusted close and split-adjusted volume are documented for the EOD endpoint.
- Bulk EOD: documented for All-In-One/EOD Historical All World paid products, not Free.
- Local use: storage, manipulation and analysis are allowed for private non-commercial use while subscribed.
- Redistribution/display to others: prohibited.
- Termination: copies must be deleted within one month after termination/expiry.
- Backtest/derived private research: allowed under the owner-accepted personal-research policy while active; no execution use is granted.
- Delisted securities, exchange-wide files and separate corporate-action endpoints on the configured Free product: **UNKNOWN / NOT AUTHORIZED**. Technical endpoint reachability would not establish entitlement.

Therefore longer EODHD history cannot be the pilot without a paid upgrade, which BB-120 forbids. No credential, account or entitlement setting was changed.

### Zero-cost source qualification

| Candidate | First-party/right status | Fit for longer OHLCV | Decision |
|---|---|---|---|
| Existing Nasdaq WIKI artifact | Public-domain provenance accepted previously | Useful but already promoted; no new qualified artifact found | Preserve existing revision; no repeat pilot |
| SEC EDGAR bulk/APIs | Public, unauthenticated filings/XBRL/reference evidence; fair-access policy applies | Not daily OHLCV; point-in-time filings need a source-specific semantic path | Qualified future filings/reference candidate; no BB-120 implementation |
| Eurostat bulk/API | EU reuse generally allowed with attribution; third-party exceptions apply | Statistical/macro SDMX/TSV, not daily OHLCV | Qualified future macro candidate; existing macro architecture must be reused |
| FRED/ALFRED | Already implemented; individual series rights remain source-specific | Macro/vintage evidence, not missing OHLCV | No duplicate work |
| Riksbank/ECB | Already implemented first-party macro/FX evidence | Not missing OHLCV | No duplicate work |
| Zenodo/Yahoo-derived | Host CC BY does not resolve upstream market-data rights | Long history, but provenance remains unknown | Manual review; no promotion |
| BB-086 ETF candidates | Previously settled fail-closed research | No rights- and access-qualified artifact | Preserve decision; do not repeat |

Public-source references: [SEC EDGAR APIs and bulk archives](https://www.sec.gov/search-filings/edgar-application-programming-interfaces), [SEC developer/fair-access resources](https://www.sec.gov/about/developer-resources), [Eurostat copyright and reuse notice](https://ec.europa.eu/eurostat/help/copyright-notice) and [FRED API terms](https://fred.stlouisfed.org/docs/api/terms_of_use.html).

No candidate was treated as survivorship-bias-free or point-in-time merely because it is long. No revised value was reclassified as knowledge-time evidence.

## Changes

No production behavior changed. Canonical Status, Backlog, Finance module, intake architecture, testing guidance and report catalog now record the BB-120 outcome.

### Implementation, pilot and verification

Implementation gap: none for an entitlement-cleared multi-year daily-OHLCV CSV/ZIP source. The unresolved gap is source rights and evidence quality, not plumbing.

Implementation: none.

Pilot: none.

Acquisitions/files/requests: 0.

Rows accepted/rejected: 0/0 new.

Promotion decisions: 0 new.

Canonical revisions/rows changed: 0/0.

Deployment: not applicable.

Validation is documentation-focused: repository documentation validator, Compose configuration, `git diff --check`, sanitized secret-pattern review and post-push CI. Existing intake tests remain authoritative because no behavior changed.

## Security

Only public first-party documents, repository code/history and the existing read-only dataset catalog were inspected. No credentials were displayed; no POST, provider acquisition, backfill, manual experiment, database write, deletion, access-control bypass or raw private log was used.

## Remaining work

The useful next step is to locate or obtain first-party clarification for a genuinely free daily-OHLCV artifact whose license permits local research storage and whose evidence explicitly classifies delistings, historical universe membership, symbol/venue changes, corporate actions, price basis, session calendar and knowledge time. If such a source appears, use the existing intake pipeline for one bounded candidate. Do not add a new framework or activate paid EODHD history without explicit owner approval.

## Resumption

Begin from the published BB-120 documentation SHA and re-evaluate dated provider terms before any acquisition. Unknown rights remain fail-closed. Reuse the BB-084+ pipeline and perform at most one separately justified bounded pilot; do not infer entitlement from technical reachability.
