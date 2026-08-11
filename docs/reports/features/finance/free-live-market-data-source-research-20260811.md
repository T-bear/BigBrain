# Free live/current market-data source research

## Metadata

- Date reviewed: 2026-08-11
- Scope: first zero-cost US/Nordic live/current observation source for personal internal forward research
- Evidence: current first-party pricing, documentation and terms only
- Result: no source authorized; BB-071 remains blocked
- Recommended technical candidate: Twelve Data Basic, conditional only
- Related commit: assigned on publication

## Status

Four realistic zero-cost/free-tier products were reassessed for current observation,
prospective non-display strategy evaluation, derived metrics and entitlement-aware local
memory. Twelve Data Basic is the strongest technical candidate because its current product
page explicitly lists free real-time US equities/ETFs and internal non-display use. Its
terms allow internal processing/storage and non-reversible derived data only within the
applicable tier, but retention remains term/third-party constrained and all data must be
deleted within 30 days after termination. Exact forward-testing and product-specific
retention scope are not sufficiently verified. The decision remains **DO NOT INGEST**.

## Changes

This report updates BB-071/BB-073 planning evidence only. It creates no account, key,
adapter, network call, provider payload, subscription, runtime or trading capability.

## Evidence

Evidence was retrieved 2026-08-11 from official pages:

- Twelve Data [individual pricing](https://twelvedata.com/pricing),
  [US equities coverage/licensing](https://support.twelvedata.com/en/articles/9935903-us-equities-market-data),
  [personal usage](https://support.twelvedata.com/en/articles/5332349-commercial-and-personal-usage)
  and [terms](https://twelvedata.com/terms).
- Alpaca [Market Data API plans](https://docs.alpaca.markets/docs/about-market-data-api),
  [market-data FAQ](https://docs.alpaca.markets/docs/market-data-faq) and
  [customer agreement](https://files.alpaca.markets/disclosures/library/AcctAppMarginAndCustAgmt.pdf).
- EODHD [pricing/product scope](https://eodhd.com/pricing),
  [quick start](https://eodhd.com/financial-apis/quick-start-with-our-financial-data-apis),
  [real-time WebSocket documentation](https://eodhd.com/financial-apis/new-real-time-data-api-websockets)
  and [terms](https://eodhd.com/financial-apis/terms-conditions).
- Alpha Vantage [support/limits](https://www.alphavantage.co/support/),
  [API documentation](https://www.alphavantage.co/documentation/) and
  [terms](https://www.alphavantage.co/terms_of_service/).

| Provider/product | Cost/auth | Current-data reality | Limits/coverage | Internal/derived/retention evidence | Result |
|---|---|---|---|---|---|
| Twelve Data Basic individual | USD 0; account/key | Product page says real-time US equities/ETFs; eight API credits/minute, 800/day and eight trial WS credits; global trial symbols only outside included markets | Three markets on current comparison; Nordic live coverage is not included/verified on Basic | Personal/internal non-commercial and internal non-display are explicit. Terms permit internal receive/process/store and non-reversible derived data subject to tier; non-display is tier-specific. Retention is only for the permitted subscription duration/third-party restrictions; delete all data within 30 days after expiry | **Best conditional technical candidate; not authorized** |
| Alpaca Trading API Basic / IEX | USD 0; trading account plus key/secret | Real-time equities are IEX-only, not consolidated SIP; up to 30 WebSocket symbols; SIP recent history is delayed/restricted | US stocks/ETFs, history since 2016, 200 historical calls/minute | Personal/non-professional use and no redistribution are documented. Durable local database retention, derived-metric ownership, forward-testing scope and cancellation deletion are not affirmatively complete in reviewed public material | Technically strong US fallback; BB-071 rights incomplete and account couples data to brokerage surface |
| EODHD Free Starter | USD 0; account/key | Limited live/delayed data; true sub-50ms WebSocket is explicitly paid. Global delayed stock data may be 15 minutes | 20 calls/day, past-year EOD; exact free live symbols/markets are limited/product-dependent | Personal use and research examples exist; prior terms evidence requires copies deleted within one month after expiry. Exact free live retention/forward-testing/derived scope remains incomplete | Conditional delayed snapshot fallback; not authorized |
| Alpha Vantage free | USD 0; account/key | Official support states real-time and 15-minute-delayed US stock data are premium-only | 25 requests/day for most free datasets; current free endpoint freshness is insufficient for the first forward feed | Personal non-commercial use exists, but durable local retention, derived/forward-testing and termination duties are not sufficiently explicit | Not suitable as first free live feed |

### Hard-gate assessment

| Requirement | Twelve Basic | Alpaca Basic | EODHD Free | Alpha Vantage Free |
|---|---|---|---|---|
| Personal/internal use | YES | YES/PARTIAL | YES | YES |
| Current US observations | YES, limited venue volume/product | YES, IEX only | PARTIAL/delayed and limited | NO on free real-time/delayed tier |
| Nordic current observations | NOT VERIFIED | NO | PARTIAL/market-dependent delayed | NOT VERIFIED |
| Non-display forward research | PARTIAL: tier label supports it, exact forward use not named | NOT VERIFIED | NOT VERIFIED | NOT VERIFIED |
| Derived metrics | PARTIAL: non-reversible derived data in terms, tier/provider restrictions apply | NOT VERIFIED | NOT VERIFIED | NOT VERIFIED |
| Local persistence while active | PARTIAL: terms permit tier-bound internal storage | NOT VERIFIED | PARTIAL | NOT VERIFIED |
| Durable/post-service retention | NO: delete within 30 days | NOT VERIFIED | NO under prior reviewed expiry duty | NOT VERIFIED |
| Product-specific third-party restrictions closed | NO | NO | NO | NO |

Technical accessibility, an API key form or a provider's strategy tutorial is not legal
permission. `PARTIAL` and `NOT VERIFIED` remain deny states in BigBrain.

## Security

Detta är en sanerad GitHub-version. It contains only dated public product and terms
evidence. No account identity, credential, token, private correspondence, payload, market
price or internal address is included. No provider was contacted by API or activated.

## Remaining work

BB-071 requires a written answer for Twelve Data Basic's exact US feed confirming personal
internal forward testing, raw observation retention while the free subscription remains
active, derived outcome/metric retention, third-party venue restrictions and deletion scope
across raw, canonical, derived and backup copies. Nordic current coverage requires a
separate exact product/tier answer. Alpaca needs equivalent storage/derived/termination
evidence and an architectural review of its brokerage-account coupling.

## Resumption

Next milestone: **RESOLVE ENTITLEMENT FOR FIRST FREE LIVE/HISTORICAL PROVIDER**. Send the
existing BB-071 inquiry, extended with forward-observation/outcome questions, without
creating an account. If affirmative written evidence later closes the complete scope,
prepare **PRODUCT OWNER APPROVAL – FIRST FREE MARKET DATA PROVIDER**. Do not implement an
adapter before that separate approval.
