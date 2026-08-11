# Finance free-first historical and live provider evaluation

## Metadata

- Date checked: 2026-08-11
- Scope: BB-071/BB-072 data-source gate; research and architecture only
- Evidence: current first-party product, pricing, documentation and legal material
- Decision: **DO NOT INGEST YET**
- Related commit: assigned on publication

## Status

Research is complete for this dated gate. No source is authorized and BB-071 remains open.
The subsequent exact Twelve Data review remains STATE B — HUMAN CONFIRMATION REQUIRED; see
[BB-071 entitlement resolution](finance-bb071-entitlement-resolution-20260811.md).

## Changes

This report consolidates historical and live provider evidence, capacity calculations,
the convergence architecture and an exact-product approval checklist. It adds no code,
account, credential, provider request, payload, persistence or runtime capability.

## Executive result

No reviewed zero-cost product has an affirmative, complete grant for BigBrain's intended
download, normalization, durable local retention, non-display backtesting/forward testing,
derived artifacts and post-termination handling. Technical access is not authorization.

The strongest *conditional* first experiment is **Twelve Data Basic for a very small
US-equity/ETF universe**. Its current free tier combines real-time US equities/ETFs,
historical/EOD access and internal non-display use under one provider, with 8 API credits
per minute, 800 per day and 8 trial WebSocket credits. This is technical, not an entitlement
decision. Terms bind retention to the subscription scope and require deletion within 30
days after termination; exact forward-testing, retained derived-artifact and third-party
venue rights require written confirmation.

There is no free authorized Swedish/Nordic candidate. Nasdaq Nordic publishes free
15-minute-delayed MiFID trade-transparency CSV files, but its policy requires prior approval
to take website data for value-added services. Those files are not a canonical
OHLCV/corporate-action product. Twelve Data Basic has only limited global trial symbols;
current pricing puts global EOD and real-time European coverage on paid tiers.

Recommended architecture: **Option A conditionally for the first US experiment** (Twelve
Data Basic for historical plus current observations), while preserving provider-neutral
boundaries. A later Nordic source may form a hybrid only after its own BB-071 entitlement.

## Evidence classification

- **VERIFIED**: cited first-party product/legal material states the fact.
- **LIKELY**: technical interpretation, never permission.
- **UNKNOWN**: reviewed evidence does not answer; BigBrain denies.
- **UNSUITABLE**: a verified restriction or product mismatch defeats this use.

## Provider scorecard

| Exact source/product | Free limits and technical scope | History/current coverage | Retention / analysis / derived rights | Nordic | Gate result |
| --- | --- | --- | --- | --- | --- |
| Twelve Data Basic | $0; key; 8 API credits/min, 800/day, 8 trial WS credits | US equities/ETFs real-time; historical/EOD; limited global trials | Internal non-display processing/storage and non-reversible derived data subject to tier/third-party rules; subscription-bound; delete within 30 days; exact forward-test/artifact scope **UNKNOWN** | Free production scope **NO/UNKNOWN** | Best conditional US candidate; not authorized |
| Alpaca Basic / IEX | $0; brokerage/Trading API account/key; 200 calls/min; 30 WS symbols | US stocks/ETFs; IEX real-time; history since 2016; recent SIP restricted | Personal/nonprofessional; durable retention, derived/forward-test and termination rules **UNKNOWN** | No | Technical US fallback; rights unresolved |
| EODHD Free Starter | $0; account/key; 20 calls/day | One year EOD; limited live/delayed; true realtime WebSocket paid | Personal/internal subscription use; delete within one month after expiry; retained derived/backtest scope **UNKNOWN** | Exact free scope limited | Conditional small evaluation only |
| Alpha Vantage Free | $0; key; 25 requests/day | Daily history; US realtime and 15-minute delayed are premium-only | Personal non-commercial; durable retention, derived and backtest rights **UNKNOWN** | Not established | Unsuitable for first free current feed |
| Financial Modeling Prep Basic | $0; key; 250 calls/day; 500 MB/30 days | US EOD, up to five years; no free intraday | Terms prohibit copying/downloading and derivative works without prior written approval | No | **UNSUITABLE** absent written grant |
| Massive Stocks Basic | $0 technical US access | US market-data product | Default terms restrict non-display/strategy-derived use and require deletion after termination | No | **UNSUITABLE** without extra license |
| Nasdaq Nordic MiFID delayed files | $0 CSV; new file each minute during market hours | Nordic/Baltic transparency delayed 15 minutes; not OHLCV/actions | Prior approval for value-added website-data use; other retention/derived scope **UNKNOWN** | Yes, technical | Unsuitable without approval |
| Nasdaq Data Link free datasets | Free account/key; platform limit 50,000/day | Dataset-specific; no qualifying canonical equity dataset identified | Dataset-specific rights; platform access grants no general entitlement | Unknown | No exact candidate product |
| Yahoo Finance / Gold historical download | Gold is paid; interactive/offline | Historical charts/download where available | No supported free ingestion API or verified automated-retention grant | Rights unresolved | **UNSUITABLE** for free automation |
| Stooq public download surface | No authenticated product contract located | Useful downloads visible | Official retention/backtest/derived license **UNKNOWN** | Technical coverage cannot authorize | Fail closed |

These ten paths are the carried-forward BB-072 set rechecked for the combined historical/
current decision. Tiingo and paid products remain future alternatives, not free candidates.

## Historical and current convergence

```text
authorized historical source -> acquisition envelope -> entitlement gate -> normalization
                                                              |
authorized current source ----> four-clock live envelope ------+
                                                              v
                           quality/session/gap classification
                                      -> immutable revision/local memory
                                      -> as-of replay/research
```

Bootstrap and current observations retain source/product, raw/adjusted basis, event/
provider/received/knowledge time, journal, policy and immutable revision. Overlap is
deduplicated only when canonical identity and evidence agree. Different values/providers
are explicit conflicts or corrections; no series silently wins. Corporate actions remain
separate. Late data appends a correction whose availability controls replay. A second
provider remains independent evidence until a versioned dataset policy reconciles it.

“Live market learning” initially means authorized observations accumulating into immutable
market memory, then deterministic features/research. It does not mean machine learning,
strategy mutation, PAPER, broker access or orders.

## Conditional operating and security model

Only after approval: use a small allowlist, honest freshness, exchange timezone and explicit
sessions, bounded polling, quota budgeting, retry/backoff for safe reads, idempotent IDs,
outage/gap findings and close reconciliation. Provider timestamps never replace received/
knowledge time. Missed periods use a separately journaled historical request, never repair.

Permitted observations should append locally under the benchmarked provisional shape:
immutable payload files plus a transactional SQLite catalog/index. This is not a final
technology decision or active store. Policy-scoped deletion removes licensed payload and
derived artifacts without unrelated datasets; audit tombstones remain only if permitted.

Future credentials must use backend secret/configuration mechanisms and never enter Git,
frontend, manifests, journals, logged URLs or unredacted provider errors. None was created.

## Capacity and cost estimates

Assumptions: Twelve Data Basic's documented 800 API credits/day and 8/minute; one symbol
request consumes one credit; regular US session 6.5 hours; 252 sessions/year; no batching.
Endpoint costs and WebSocket production eligibility require recheck before implementation.

| Universe | Daily EOD requests | 15-minute polling (26/session) | 5-minute polling (78/session) | Interpretation |
| ---: | ---: | ---: | ---: | --- |
| 10 | 10 | 260 | 780 | 5-minute barely fits daily cap; no recovery/reference margin |
| 50 | 50 | 1,300 | 3,900 | EOD fits; intraday does not |
| 100 | 100 | 2,600 | 7,800 | EOD fits; intraday does not |
| 500 | 500 | 13,000 | 39,000 | EOD fits with limited operational margin |

Eight trial WebSocket credits imply at most eight simultaneous symbols if one costs one
credit; “trial” is not assumed to authorize a durable production feed.

The synthetic benchmark measured about 124 bytes/row JSONL and 283 bytes/row SQLite at
1,260,000 rows. Using a rounded 410 bytes per observation for the provisional hybrid,
before backups/corrections/features:

| Universe | EOD rows/year | EOD growth/year | 5-minute rows/year | 5-minute growth/year |
| ---: | ---: | ---: | ---: | ---: |
| 10 | 2,520 | 1.0 MB | 196,560 | 81 MB |
| 50 | 12,600 | 5.2 MB | 982,800 | 403 MB |
| 100 | 25,200 | 10.3 MB | 1,965,600 | 806 MB |
| 500 | 126,000 | 51.7 MB | 9,828,000 | 4.0 GB |

Free request allowance, not disk, is the first bottleneck. Start at most eight to ten US
instruments with conservative polling or EOD if rights are later confirmed.

## BB-071 checklist — Twelve Data Basic / US equities and ETFs

- [x] Provider identified: Twelve Data
- [x] Exact product/tier: Basic, individual/non-commercial
- [x] Initial markets: US equities/ETFs only
- [ ] Exact automated historical research use permitted
- [ ] Backtesting and prospective forward testing explicitly permitted
- [ ] Derived features/metrics and their retention explicitly permitted
- [ ] Durable local raw/canonical persistence explicitly permitted
- [x] Tier/third-party retention boundary identified
- [x] Product-level live status: real-time US listing with venue/feed caveats
- [x] Public cancellation rule: delete within 30 days
- [ ] Complete deletion scope, including artifacts/backups, confirmed
- [x] Redistribution prohibited absent permission
- [ ] Attribution and exchange/third-party requirements confirmed
- [x] First-party evidence recorded below
- [ ] Written provider confirmation recorded
- [ ] Product-owner approval

Every unchecked item is `UNKNOWN` and denies account, adapter, download and persistence.

## First-party evidence reviewed

Retrieved/rechecked 2026-08-11:

- Twelve Data: [pricing](https://twelvedata.com/pricing), [terms](https://twelvedata.com/terms), [US equities](https://twelvedata.com/markets/stock)
- Alpaca: [market data](https://docs.alpaca.markets/docs/about-market-data-api), [FAQ](https://docs.alpaca.markets/docs/market-data-faq), [agreement](https://files.alpaca.markets/disclosures/library/AcctAppMarginAndCustAgmt.pdf)
- EODHD: [pricing](https://eodhd.com/pricing), [quick start](https://eodhd.com/financial-apis/quick-start-with-our-financial-data-apis), [realtime](https://eodhd.com/financial-apis/new-real-time-data-api-websockets)
- Alpha Vantage: [support](https://www.alphavantage.co/support/), [terms](https://www.alphavantage.co/terms_of_service/)
- FMP: [pricing](https://site.financialmodelingprep.com/pricing-plans), [terms](https://site.financialmodelingprep.com/terms-of-service)
- Massive: [pricing](https://massive.com/pricing?product=stocks), [terms](https://massive.com/legal/market-data-terms-of-service)
- Nasdaq Nordic: [MiFID delayed data](https://www.nasdaq.com/market-regulation/nordic/mifid-ii), [April 2026 policies](https://www.nasdaq.com/docs/Nasdaq_European_Data_Policies_April_2026)
- Nasdaq Data Link: [getting started/limits](https://docs.data.nasdaq.com/v1.0/docs/getting-started)
- Yahoo Finance: [download help](https://help.yahoo.com/kb/finance/SLN2311.html)
- Stooq: public download surface; adequate official license evidence was not located.

## Next decision

Before collection, BigBrain needs affirmative written answers for every unchecked BB-071
item, an exact product/market/use evidence revision, explicit product-owner approval, and
then a separate **FIRST AUTHORIZED FREE MARKET DATA INGESTION** prompt. If Twelve Data
cannot grant the rights, reject it; do not weaken the gate.

## Security

Detta är en sanerad GitHub-version. It contains public product/legal evidence and explicit
assumptions only. No account identity, credential, token, private correspondence, raw
provider payload, real market observation, private address or sensitive path is included.
No account, key, API call, paid service, runtime change, deployment, broker path, PAPER mode
or trading authority was created.

## Remaining work

Obtain written Twelve Data Basic answers for every unchecked BB-071 item or reject the
candidate. Nordic coverage needs its own exact free product and entitlement evidence. A
production persistence choice and adapter remain separately gated.

## Resumption

Next action is **BB-071 WRITTEN ENTITLEMENT + PRODUCT-OWNER APPROVAL**. Only after both may
a separate **FIRST AUTHORIZED FREE MARKET DATA INGESTION** prompt implement an adapter.
