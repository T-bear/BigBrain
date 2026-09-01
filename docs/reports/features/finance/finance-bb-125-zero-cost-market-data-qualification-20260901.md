# BB-125 Zero-Cost Historical Market Data Qualification

Detta är en sanerad GitHub-version. Den innehåller inga hemligheter, privata adresser, råa marknadsrader eller känsliga runtimeuppgifter.

## Metadata

- Date reviewed: 2026-09-01
- Baseline/source of truth: `95490168e113a506bfda200cb2b8603b5ee050d2`
- Scope: SimFin Free, Alpaca Basic/free, Yahoo Finance/yfinance and Tiingo Starter/free
- Finance boundary: `RESEARCH / 0 SEK / NONE`
- Result: **QUALIFIED FAIL-CLOSED / NO STATE A / NO ACQUISITION / NO CODE / NO DEPLOYMENT**

## Status

Research and source-of-truth reconciliation are complete. No candidate reached `STATE A`, so no
production code, data, runtime or deployment changed. Documentation verification and publication
evidence are recorded during finalization.

## Evidence

## Existing capability and decision inventory

BigBrain already has the provider-neutral entitlement/provenance model, effective-dated canonical
instrument mappings and the BB-084 chain `ExternalDatasetCandidate → QuarantineArtifact →
ValidationEvidence → PromotionDecision → CanonicalDatasetRevision`. CSV and ZIP-contained CSV,
checksums, immutable revisions, OHLCV validation, cross-source comparison and all 13
`dataset-promotion-v1` gates already exist. No intake, registry, adapter or quality abstraction is
missing before entitlement is cleared.

Historical conclusions remain dated evidence; this report changes only the current qualification.

| Source/product | Prior dated decision | BB-125 reason to revisit | Current operational state |
|---|---|---|---|
| EODHD Free | BB-077/078 approved bounded active-account personal EOD; about one year; deletion within one month after expiry | Comparison baseline only | Active bounded source; no entitlement change |
| Nasdaq WIKI | BB-084/121 public-domain partial snapshot, immutable and promoted for five mapped equities | Comparison baseline only | 2014–2016 snapshot retained; no acquisition/promotion |
| Stooq | BB-121 human confirmation required and technical access-control boundary | No owner response | Unchanged; no acquisition |
| Twelve Data | Basic is evaluation-only; paid Personal human-cleared | Zero-budget comparison | Inactive paid fallback |
| Alpaca Basic/IEX | BB-075 human confirmation required for durable data lifecycle | Current plan/docs advertise historical data since 2016 | Requalified below; still confirmation required |
| Yahoo Finance/yfinance | BB-075 denied/incompatible for automated ingestion | Owner requested serious source/client separation | Requalified below; automated canonical use remains denied |
| Tiingo | BB-071 found term-bound storage/derived uncertainty; BB-072 later observed paid-only individual API | Starter is now explicitly $0 with materially revised 2026 terms | Requalified below; persistent Starter use expressly denied |
| Massive/Polygon | Default non-display/derived use incompatible | No new exact zero-cost evidence requested | Unchanged/inactive |
| Alpha Vantage | Free scope and research/storage rights insufficient; deeper adjusted history premium | No new exact evidence requested | Unchanged/inactive |
| Nasdaq Data Link | Dataset-specific; no qualifying current free equity/ETF OHLCV product | WIKI handled separately | Unchanged |
| SimFin | No canonical current decision found | New zero-cost candidate | Requalified below |

## Common qualification matrix

`UNKNOWN` is fail-closed. Technical access is not entitlement. Classification concerns BigBrain's
intended automated, persistent, immutable research archive, not whether a human can view a chart.

| Requirement | SimFin Free | Alpaca Basic/free | Yahoo Finance | yfinance | Tiingo Starter |
|---|---|---|---|---|---|
| Price/account | $0, no card; registration required | $0; trading account and API keys required | Consumer service; interactive history/download surface | Apache-2.0 client software; no Yahoo authorization | $0; account/token required |
| Official acquisition | Web API, Python API and bulk CSV are advertised | Official authenticated historical API | Interactive download is documented; no approved BigBrain API | Unofficial endpoints/client | Official authenticated EOD API |
| Daily history | Free page advertises five years and 5,000 US stocks | US stocks/ETFs since 2016 | Instrument-dependent historical prices | Whatever Yahoo endpoints return; not a stable entitlement | 30+ years, 109,769 global securities advertised |
| Quota | 2 web API calls/sec; 500 high-speed credits shown | 200 historical calls/min; latest 15 minutes restricted | Not documented as an automation quota | Endpoint-dependent and fragile | 500 unique symbols/month, 50/hour, 1,000/day, 1 GB/month |
| OHLCV/basis | Daily OHLC plus adjusted close and volume advertised; exact raw/action lineage is incomplete | Bars; raw/split/spin-off adjustment options documented | Prices, dividends and splits available interactively | Adjustment behavior is client-version dependent | Raw and adjusted OHLCV; dividend and split fields |
| Actions/identity | Delisted retention is claimed for SimFin backtests; exact price-source, action, ticker-reuse and stable-ID guarantees are incomplete | Corporate-action endpoint includes splits, dividends, mergers, name/symbol changes and identifiers; exact free entitlement/retention unresolved | Stable identity, venue and delisted completeness not established | Cannot improve Yahoo provenance/identity rights | Exchange code, start/end dates, actions and `permaTicker` concepts exist; delisted/recycled coverage has limits |
| Automated acquisition right | API is offered to registered subscribers | API is offered to account holders | General terms prohibit automated collection without express prior permission | Client license covers code, not Yahoo data or permission | API is offered to registered users |
| Persistent raw/normalized storage | Copies/reprocessing allowed during license; pricing FAQ requires downloaded data and backups deleted after cancellation | **UNKNOWN** for BigBrain raw/normalized archive, backups and accumulated IEX evidence | No explicit grant; automated archive conflicts with Yahoo terms | No independent data right | **DENIED** on Starter: no persistent/durable storage, logs, archives or backups |
| Private backtesting/derived evidence | Personal research/backtesting is named; reprocessed data remains restricted; interpretations may be retained | API docs name backtesting, but exact durable raw/derived lifecycle remains **UNKNOWN** | Personal use does not override automation prohibition; persistent deterministic use is **UNKNOWN** | Research intent does not grant Yahoo rights | Transient calculation allowed; qualifying non-reconstructable derived products may be retained |
| Post-termination retention | Raw/reprocessed data and backups must be deleted; exact retained derived/audit boundary needs confirmation | **UNKNOWN** | **UNKNOWN** | **UNKNOWN** | Raw Tiingo Data must not exist on Starter; qualifying derived products may remain |
| Reproducible immutable archive | Technically possible, but subscription/account lifecycle and provenance details block activation | Technically date-bounded, but rights lifecycle blocks activation | Unsupported for BigBrain automation | Fragile unofficial mechanism and unqualified underlying rights | Express Starter persistence prohibition makes it incompatible |
| BB-125 state | **STATE B — HUMAN CONFIRMATION REQUIRED** | **STATE B — HUMAN CONFIRMATION REQUIRED** | **STATE D — REJECTED for automated canonical intake**; interactive human reference remains narrower only | **STATE D — UNAUTHORIZED/UNSUPPORTED acquisition mechanism** | **STATE D — REJECTED for persistent canonical intake** |

No candidate reaches `STATE A — QUALIFIED FOR BOUNDED PILOT`; consequently no account, key,
request, download, candidate, quarantine artifact, adapter, promotion or canonical revision was
created.

## Provider findings

### SimFin Free — STATE B

The current first-party pricing page says Free is $0 without a billing card, includes 5,000 US
stocks, five years of charts/bulk data, API/bulk download and 500 high-speed backtest credits. The
license covers data obtained through the website, API, CSV and Python API, permits personal
research, copies needed for that use and reprocessing, and treats interpretations separately.
However, it becomes effective through a subscriber/account relationship and the pricing FAQ says
downloaded data and backups must be deleted after cancellation. The exact free market-price
dataset's upstream source/license, split/dividend lineage, raw/adjusted semantics, immutable
correction history, stable identity and post-termination treatment of BigBrain's normalized,
feature, backtest and audit evidence are not explicit enough for activation. SimFin also requires a
new owner-created account, which BB-125 cannot create.

First-party evidence: [pricing and limits](https://www.simfin.com/en/prices/),
[data license](https://www.simfin.com/en/commercial-license/),
[support/API and bulk access](https://www.simfin.com/en/support/), and
[delisted/backtest statement](https://www.simfin.com/en/simfin-screener-backtesting-tutorial/).

### Alpaca Basic/free — STATE B

Current first-party documentation confirms Basic at zero cost, US stocks/ETFs, IEX-only real-time
coverage, historical data since 2016, 200 historical calls/minute and a 15-minute latest-data
restriction. IEX is one exchange and is not SIP/consolidated US market evidence. Historical APIs
are explicitly described for charting/backtesting, and current corporate-action APIs include
splits, dividends, mergers, name/symbol changes, ISIN and currency. Public agreements prohibit
reproduction/distribution/sale/commercial exploitation but do not map BigBrain's persistent raw,
normalized, backup/revision, accumulated IEX, derived-feature, audit and post-account retention
classes. An Alpaca trading account and keys are also required. The previous human-confirmation gate
therefore remains correct.

First-party evidence: [plan comparison](https://docs.alpaca.markets/us/docs/about-market-data-api),
[historical API](https://docs.alpaca.markets/us/docs/historical-api),
[IEX versus SIP](https://docs.alpaca.markets/us/docs/market-data-faq),
[corporate actions](https://docs.alpaca.markets/us/reference/corporateactions-1), and
[current customer agreement](https://files.alpaca.markets/disclosures/library/AcctAppMarginAndCustAgmt.pdf).

### Yahoo Finance and yfinance — STATE D for BigBrain ingestion

Yahoo documents an interactive historical-data download, so the consumer product is evaluated
separately from `yfinance`. Yahoo's current general terms prohibit accessing or collecting service
data using automated means—including robots, scrapers, data-mining or extraction tools—without
express prior permission. They also restrict creating a substitute database/archive. Those terms
do not grant BigBrain automated, persistent, versioned acquisition, even if a human may use Yahoo
personally or download a file through the supported UI. Manual visual/interactively downloaded
information may remain a non-canonical reference for human anomaly investigation only; this report
does not authorize retaining it in BigBrain.

`yfinance` is Apache-2.0 open-source software but explicitly says it is not affiliated, endorsed or
vetted by Yahoo and directs users to Yahoo's terms for data rights. Its code license cannot license
Yahoo data. Unofficial endpoint/cookie/crumb behavior and client adjustment defaults also weaken
stable reproducibility. BigBrain will not use it, reuse browser sessions or bypass controls.

Evidence: Yahoo [general terms](https://legal.yahoo.com/us/en/yahoo/terms/otos/index.html),
[historical download help](https://help.yahoo.com/kb/finance-for-web/download-historical-data-yahoo-finance-sln2311.html),
and yfinance's [upstream README](https://github.com/ranaroussi/yfinance/blob/main/README.md).

### Tiingo Starter/free — STATE D for persistent intake

Tiingo now advertises Starter at $0, 30+ years, 109,769 global securities, raw and adjusted EOD,
splits/dividends, 500 unique symbols/month and explicit API quotas. EOD metadata includes exchange
code and coverage dates; documentation describes correction handling and `permaTicker` for some
delisted/recycled cases. These are strong technical properties.

The current Terms, updated 2026-08-05, are decisive: Starter/Trial users may process Tiingo Data
only transiently and may not write, save, archive, back up or otherwise retain it in any persistent
storage. Qualifying non-reconstructable derived products may be retained, but that does not permit
BigBrain's immutable raw/normalized canonical archive. Terms override the attractive free pricing;
no data may be acquired for this use.

First-party evidence: [Starter pricing and quotas](https://www.tiingo.com/pricing),
[EOD schema/actions](https://www.tiingo.com/documentation/end-of-day),
[symbology](https://www.tiingo.com/documentation/appendix/symbology), and
[Terms §1.6](https://api.tiingo.com/tos/).

## Comparison with current evidence and BB-124 value

Current EODHD contributes about one rolling year for all eight configured symbols. WIKI adds only
2014–2016 evidence for AAPL, MSFT, JPM, XOM and JNJ; SPY, QQQ and IWM are absent. Another source does
not by itself repair survivorship, point-in-time identity, corporate actions or source disagreement.
Provider revisions must remain independent and must never be silently stitched.

Conservative session estimates use approximately 252 US sessions/year and subtract BB-124's two
50-session embargoes before applying its 60/20/20 split. They describe potential scale, not
statistical adequacy or verified retrieved rows.

| Candidate documented depth | Rough common-session scale | Rough usable 60/20/20 after embargo | Material BB-124 value if later cleared |
|---|---:|---:|---|
| SimFin Free, five years | ~1,260 | ~696 / 232 / 232 | More folds/regimes than current history; still limited and identity/provenance uncertain |
| Alpaca since 2016 | ~2,500 | ~1,440 / 480 / 480 | Material depth for all eight, but IEX single-venue semantics and rights remain blockers |
| Yahoo, instrument-dependent maximum | Not responsibly fixed from current first-party product docs | Unknown | Potentially long reference coverage; not authorized/reproducible for automated canonical use |
| Tiingo, 30+ years; common cohort bounded by ETF inception | roughly 2000 onward, ~6,500 | ~3,840 / 1,280 / 1,280 | Largest apparent common-universe gain and strong actions, but Starter persistence is prohibited |

Calendar years do not establish unbiased samples, adequate independent regimes or fresh holdouts.
BB-124 remains correctly `INSUFFICIENT_DATA` on current canonical evidence.

## Human-confirmation pack — drafts only, not sent

### SimFin

> For a private non-commercial individual using the Free plan and investing only their own funds,
> may an automated self-hosted tool download the exact daily share-price dataset, persist raw and
> normalized OHLCV plus checksums/revisions/backups, run deterministic backtests, and retain derived
> features/metrics/audit lineage? Please identify the price source, raw/adjusted and split/dividend
> semantics, correction/version model, delisted/stable-identity coverage, and what raw, normalized,
> derived and audit artifacts must be deleted after account closure. May non-reconstructable results
> remain after closure? No redistribution or public display is intended.

### Alpaca

> For a non-professional individual on Basic using only their own funds, may the IEX historical API
> be accessed programmatically for bounded daily OHLCV research and may raw responses, normalized
> bars, checksums, immutable revisions and backups be retained locally? May they accumulate over
> time and support deterministic private backtests and retained derived features/metrics/audit
> evidence? What must be deleted after account termination? Which historical feed applies to Basic,
> and are corporate actions, delisted securities and symbol/identifier history included? No orders,
> redistribution or public display is intended.

### Yahoo

> Is there an expressly authorized zero-cost API or written permission path for a private
> non-commercial individual to make bounded automated daily-history requests, store raw and
> normalized data/checksums/backups on a private self-hosted server, run deterministic backtests and
> retain non-reconstructable derived features/metrics after access ends, solely for investment of
> their own funds and without redistribution/public display? If not, BigBrain will not automate
> Yahoo Finance access.

### Tiingo

> Do you offer a durable $0 permission for a private non-commercial individual to persist EOD raw
> and normalized data, checksums/versioned backups and deterministic backtest lineage on a private
> self-hosted server? May non-reconstructable derived features/metrics/audit evidence be retained
> after account closure? We understand Starter Terms §1.6 currently prohibit persistent Tiingo Data;
> please confirm whether any written zero-cost exception exists. No redistribution or public display
> is intended.

No inquiry was sent. Account creation, acceptance of new terms and credential issuance require a
separate owner action and a new dated entitlement review.

## Outcome and next action

- SimFin: `STATE B — HUMAN CONFIRMATION REQUIRED`; promising five-year free scope, but account,
  exact price provenance/semantics and lifecycle rights must be resolved.
- Alpaca: `STATE B — HUMAN CONFIRMATION REQUIRED`; strongest documented all-eight depth among
  candidates not expressly storage-denied, but IEX semantics and durable rights remain unresolved.
- Yahoo Finance: `STATE D` for automated canonical intake; human interactive reference remains
  narrower and non-canonical.
- yfinance: `STATE D` as a BigBrain acquisition mechanism; open-source code does not authorize data.
- Tiingo Starter: `STATE D` for persistent canonical intake because Terms expressly prohibit it.

Recommended next action: the owner may send the SimFin and Alpaca drafts, beginning with SimFin
because its free license expressly names personal research/reprocessing and its account needs no
card. A complete written response must be reviewed before account creation or acquisition. If no
written lifecycle grant is obtained, retain current EODHD/WIKI evidence and pursue a different
rights-cleared first-party/public-domain source. No adapter work is justified now.

## Verification

This is research/documentation only. Production source, schema, configuration, credentials,
canonical data and runtime are unchanged. No provider request, autonomous research run, strategy
tuning or deployment occurred. Documentation links/structure, unique BB IDs, Compose syntax, diff
whitespace and secret-pattern gates are the applicable publication checks. The documentation
validator passed for 209 Markdown files and 89 unique BB IDs; Compose validation and
`git diff --check` passed. No production build/test is applicable. The local gitleaks binary is not
installed, so the full-history secrets result is recorded from GitHub CI after publication.

## Changes

Only current Finance status, backlog, module/testing/provider-selection documentation and this
sanitized report/index entry changed. Historical reports, ADR decisions, production source,
configuration, schema and runtime evidence are unchanged.

## Security

No provider account, key, cookie, browser session or credential was used. No request reached a
provider data endpoint. The published evidence contains only public first-party URLs and sanitized
qualification conclusions; it contains no raw payload, market row, private address or sensitive
path.

## Remaining work

- Obtain explicit SimFin Free and/or Alpaca Basic lifecycle answers through owner-sent inquiries.
- Reassess only after a response identifies the exact product, rights, retention and termination
  duties; account creation remains a separate owner action.
- Preserve BB-124's short/biased-data limitation until a source actually passes every gate.

## Resumption

Resume from `NO STATE A / NO ACQUISITION`. The smallest safe next action is owner review of the
SimFin inquiry draft. Do not create an account, adapter or candidate before written entitlement and
separate approval exist.
