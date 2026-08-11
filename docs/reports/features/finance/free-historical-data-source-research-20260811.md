# BB-072 free historical data source research

## Metadata

- Date reviewed: 2026-08-11
- Scope: zero-cost daily equity/ETF history for private research, local memory and replay
- Research status: complete; public first-party evidence only
- Best free candidate: **NONE authorized**
- Secondary technical candidate: **EODHD Free Starter**, evaluation only
- Swedish/Nordic candidate: **NONE authorized**; Twelve Data Basic is the strongest
  technical lead, but exact retention/non-display rights remain unverified
- Recommendation: **DO NOT INGEST YET**

## Status

Free access is not an entitlement. None of the reviewed zero-cost products has a complete,
verified grant covering durable local raw retention, personal non-display backtesting,
derived artifacts, corporate actions and post-service obligations for BigBrain's intended
US plus Nordic scope. BB-071 therefore remains the hard gate. This research created no
account, key, adapter, download, provider payload, persistence or runtime change.

## Gate and ranking method

Legal/retention eligibility is an absolute gate. Passing technical criteria cannot
compensate for unknown or prohibited rights. Candidates that pass the gate would then be
ranked by zero cost, history, quality, corporate actions, reproducibility, interface
stability, limits, US/Nordic coverage and fit with the canonical model. `UNKNOWN` means
the reviewed first-party material did not grant the required right; it is not permission.

| Rank | Exact source/product | Cost/authentication and limits | Coverage and history | Corporate actions / identity | Retention and permitted use | Result |
| --- | --- | --- | --- | --- | --- | --- |
| 1 conditional | EODHD Free Starter | USD 0; account/API key; 20 calls/day and 20/minute | US/world equities and ETFs; EOD limited to past year | Splits/dividends and adjusted data listed; delisted/symbol-history availability on free tier not verified | Private non-commercial storage/manipulation is stated while subscribed; delete copies within one month after expiry; post-service archive fails | Best technical/legal evaluation lead, **not authorized** |
| 2 conditional | Twelve Data Basic individual EOD | USD 0; account/API key; current credit limits are product-dependent | Global EOD and full history advertised; XSTO and other Nordic venues documented | OHLCV/reference data; exact free-tier actions, delisted and symbol-history scope unverified | Personal/internal non-commercial use stated; durable retention, derived/backtest and post-service rights still not affirmatively complete | Best Nordic technical lead, **not authorized** |
| 3 | Alpha Vantage free API | USD 0; API key; 25 requests/day | Global examples; daily raw endpoint, while 20+ year adjusted daily is premium | Premium adjusted endpoint includes dividends/splits; delisted/symbol history unverified | Personal/non-commercial platform use exists, but permanent local retention, derived-data and deletion terms were not verified | Blocked by rights and tier mismatch |
| 4 | FMP Basic personal | USD 0; API key; 250 calls/day, 500 MB trailing-30-day bandwidth | US EOD, up to five years | Corporate-action, delisted and symbol-history free scope unverified | Terms restrict copying/downloading without prior written approval and derivative works without approval | Ineligible without written grant |
| 5 | Massive Stocks Basic | USD 0; account/API key; product limits apply | US stocks; free flat files/API, history varies by dataset | Raw flat files; split endpoint can support adjustments; strong US reference data | Default market-data terms say display-only and prohibit non-display use, derived investment strategies and derived works without a license | Ineligible for BigBrain backtesting |
| 6 | Stooq public historical download | USD 0 surface; no stable authenticated contract located | Multi-market daily files appear available; exact current Nordic depth unverified | Adjustment, actions, delisted coverage, corrections and symbol history unverified | No sufficiently explicit first-party license/ToS for automated acquisition, durable storage or backtesting was located | Fail-closed `UNKNOWN` |
| 7 | Nasdaq Nordic MiFID delayed files | USD 0; no account for public files; files retained 48 hours | Nordic/Baltic delayed pre/post-trade, not a durable daily historical dataset | Not a canonical EOD/action/symbol-history package | Non-commercial use is stated, but 48-hour availability and product shape do not meet the historical-memory need | Not suitable |
| 8 | Nasdaq Nordic historical products | Paid product/contract; current price is quote/product dependent | Authoritative Nordic historical data | Product-dependent | Policies allow subscriber internal saving/processing, but this is not a free source and non-display fees may apply | Future paid fallback only |
| 9 | Yahoo Finance Gold historical download | Paid subscription; interactive download; no supported public ingestion API | Broad US/global, usually to 1970; instrument-dependent | Display/download includes prices, dividends and splits | Offline download is a Gold feature; automation, durable database retention, derived use and post-service terms are not verified | Not free and not reproducible API ingestion |
| 10 | Nasdaq Data Link free datasets | Dataset-specific; API key raises limits; 50,000 calls/day for authenticated non-premium use | Dataset-specific, not one canonical US/Nordic equity feed | Dataset-specific | License and retention are dataset-specific, so platform-level free access grants nothing for a future selected dataset | Reassess only for an exact dataset |

Tiingo is no longer treated as a free candidate: the official individual API price shown
on 2026-08-11 is USD 30/month. Its prior retention/deletion and derived-use uncertainty
still makes it a BB-071 comparison, not a zero-cost winner. Costs and product limits are
dated observations and must be rechecked before any owner decision.

## Rights matrix

| Source | Download | Local retention | Personal backtesting | Derived data | End-of-service duty | Redistribution / attribution |
| --- | --- | --- | --- | --- | --- | --- |
| EODHD Free Starter | YES within quota | YES while service entitlement is active | PARTIAL: private analysis is permitted, but deterministic backtesting is not named | Private manipulation/analysis stated; artifact scope not explicit | Delete within one month | Redistribution prohibited; attribution requirement not verified |
| Twelve Data Basic | YES within entitlement | PARTIAL/term-bound | NOT VERIFIED for exact free product | NOT VERIFIED | Delete/retention obligations remain product-dependent | Redistribution prohibited without suitable plan/license |
| Alpha Vantage free | YES within quota | NOT VERIFIED | NOT VERIFIED | NOT VERIFIED | NOT VERIFIED | Redistribution/attribution scope not verified |
| FMP Basic | Technically yes, legally requires written approval under public terms | NO verified grant | NOT VERIFIED | Prior approval required | NOT VERIFIED | Display/redistribution requires agreement |
| Massive Basic | Technically yes | NOT VERIFIED | NO under default display-only/non-display restriction | NO without license | Termination obligations apply | No redistribution; exchange terms may add duties |
| Stooq | Public surface | NOT VERIFIED | NOT VERIFIED | NOT VERIFIED | NOT VERIFIED | NOT VERIFIED |
| Nasdaq Nordic delayed | YES, short-lived files | Source available for 48 hours; durable archive right for this exact feed not verified | NOT VERIFIED | NOT VERIFIED | NOT VERIFIED | Non-commercial use free; commercial fees may apply |
| Nasdaq Nordic historical | YES under paid subscription | YES for subscriber internal use | Non-display/product terms and fees require contract review | NOT VERIFIED | Contract-specific | No unauthorized external distribution |
| Yahoo Finance Gold | YES interactively/offline | NOT VERIFIED for a BigBrain database | NOT VERIFIED | NOT VERIFIED | NOT VERIFIED | Do not redistribute; provider restrictions vary |
| Nasdaq Data Link | Dataset-specific | Dataset-specific | Dataset-specific | Dataset-specific | Dataset-specific | Dataset-specific |

No exchange rates are inferred and no legal conclusion is made beyond the cited text.
An exact product, market, user classification and terms version must be captured in the
entitlement evidence before acquisition.

## Evidence

Reviewed on 2026-08-11:

- EODHD [pricing and Free Starter scope](https://eodhd.com/pricing),
  [quick-start limits](https://eodhd.com/financial-apis/quick-start-with-our-financial-data-apis)
  and [terms](https://eodhd.com/financial-apis/terms-conditions)
- Twelve Data [individual pricing](https://twelvedata.com/pricing),
  [EOD product scope](https://support.twelvedata.com/en/articles/12682324-end-of-day-eod-pricing-market-data),
  [personal/commercial use](https://support.twelvedata.com/en/articles/5332349-commercial-and-personal-usage)
  and [terms](https://twelvedata.com/terms)
- Alpha Vantage [documentation](https://www.alphavantage.co/documentation/),
  [free request limit](https://www.alphavantage.co/support/) and
  [terms](https://www.alphavantage.co/terms_of_service/)
- FMP [pricing](https://site.financialmodelingprep.com/pricing-plans),
  [FAQ](https://site.financialmodelingprep.com/faqs) and
  [terms](https://site.financialmodelingprep.com/terms-of-service)
- Massive [Stocks Basic flat-file documentation](https://massive.com/docs/flat-files/stocks/overview)
  and [market-data terms](https://massive.com/legal/market-data-terms-of-service)
- Nasdaq [Nordic delayed-data description](https://www.nasdaq.com/market-regulation/nordic/mifid-ii),
  [European pricing/policies](https://www.nasdaq.com/solutions/data/european-pricing-policies),
  [European Data Policies](https://www.nasdaq.com/docs/nasdaq-european-data-policies-january-2025)
  and [Data Link rate limits](https://docs.data.nasdaq.com/docs/rate-limits-1)
- Yahoo Finance [historical download](https://help.yahoo.com/kb/download-historical-data-yahoo-finance-sln2311.html)
  and [exchange/provider restrictions](https://help.yahoo.com/kb/finance/SLN2310.html)
- Tiingo [current API pricing](https://www.tiingo.com/about/pricing)
- Stooq public download surface; no sufficiently explicit official license document was
  located in this review.

## Changes

BB-072 updates the canonical source-selection decision, backlog/status/roadmap, entitlement
inquiry and report indexes. It changes no executable contract or architecture decision.

### Future acquisition contract — plan only

```text
exact provider product + entitlement evidence
  -> bounded acquisition manifest (request scope, adapter/schema version, checksum)
  -> immutable provider payload envelope (never canonical truth)
  -> fail-closed entitlement gate
  -> canonical identity/normalization
  -> quality + session/gap classification
  -> immutable dataset revision/correction chain
  -> entitlement-aware local historical memory
  -> as-of deterministic replay
  -> later backtesting evidence
```

The acquisition manifest must make pagination, date ranges and response checksums
reproducible. Content-addressed raw envelopes avoid unnecessary reacquisition when the
license permits reuse. Corrections append a revision with event time and availability
time; they never rewrite old knowledge. Policy ID/version and deletion scope follow every
raw and derived artifact. A deletion plan must enumerate raw objects, canonical revisions,
derived lineage and backups without treating derived data as exempt. No persistence
technology is selected until dataset volume, replay queries, retention and deletion are
measured.

## Cost and governance principle

**FREE FIRST / COST-AWARE DATA ACQUISITION:** prefer legally usable free, local,
open-source or self-hosted paths; reuse locally retained evidence when permitted; avoid
duplicate calls; never auto-upgrade; activate no subscription or exchange fee without
explicit product-owner approval. Zero price never relaxes entitlement, quality,
provenance or deletion controls.

## Security

Detta är en sanerad GitHub-version. It contains public product/terms observations only,
not correspondence, identities, credentials, payloads or internal runtime details. No
provider account, key, market-data download, paid service, broker path, trading capability,
runtime mutation or deployment was created.

## Remaining work

BB-072 research is complete, but it does not complete BB-045 or BB-071. The next safe
slice is a **provider entitlement evidence package and synthetic acquisition contract
test plan**: ask EODHD and Twelve Data the ready-to-send BB-071 questions for their exact
free products and intended US/XSTO use, record dated answers, and test only synthetic
acquisition manifests/envelopes. Implement an adapter only after the product owner accepts
an exact entitlement. If neither source grants durable use, retain `DO NOT INGEST` and
evaluate an explicit paid/self-sourced alternative as a separate cost decision.

No source code or tests changed. Documentation gates are recorded at publication. No
external data, persistence, runtime, deployment, broker path or trading authority was
created.

## Resumption

Send the sanitized BB-071 inquiry for EODHD Free Starter and Twelve Data Basic without
creating an account. Record an exact product/market/terms response, retain unknown as denied
and proceed only after explicit product-owner entitlement review. Synthetic acquisition-
contract tests are the only implementation work safe in parallel.
