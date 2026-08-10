# Finance market-data provider selection

Status: BB-046 research complete 2026-08-10; provider activation is blocked by BB-071.
Prices and product terms below are time-sensitive observations, not permanent facts.

## Decision

No provider is finally selected or authorized yet. Twelve Data is the primary candidate
for M2 because its official EOD coverage includes Nasdaq Stockholm (`XSTO`) and other
Nordic exchanges, its reference model exposes MIC and optional FIGI/ISIN, and it supports
daily through intraday OHLCV. Tiingo is the preferred specialized comparison for long US
end-of-day history and corporate-action-aware raw/adjusted data. Massive is the US-depth
alternative when consolidated US coverage, delisted identifiers, flat files or later
minute data justify its higher and US-focused scope.

Activation requires written/provider-account confirmation under BB-071 that a personal
Swedish installation may cache and retain the exact purchased EOD data and corporate
actions for deterministic local backtests, including after subscription cancellation,
without redistribution. Until then, fixtures remain the only authorized source.

## Initial M2 dataset

- A small owner-approved allowlist of US and Swedish/Nordic equities and ETFs.
- One confirmed end-of-day candle per exchange session, raw/unadjusted OHLCV as source
  truth; corporate actions and any provider-adjusted series are stored separately.
- Daily granularity first. Hourly/minute data is deferred until a strategy demonstrates
  a requirement and licensing/cost/storage impact is reviewed. Tick/order-book data is
  explicitly out of scope.
- No indexes unless the selected dataset explicitly licenses their use; no crypto is
  needed to prove the architecture.

## Provider-neutral boundary

```text
provider HTTP/export
  → provider adapter DTOs
  → validation and canonical instrument mapping
  → immutable import/dataset version
  → canonical raw candle + corporate-action records
  → deterministic replay view
  → strategy/backtest
```

Provider DTOs, symbols, pagination and entitlements stop at the adapter. Canonical
instrument identity must include an internal immutable ID plus venue MIC, currency and
time-bounded provider-symbol mappings; ticker alone is not identity. Direct HTTP is the
default for M2 unless an SDK supplies a reviewed, necessary feature without widening the
dependency or secret boundary.

## Provenance and correction contract for BB-045

Every dataset/import and observation must preserve:

- provider and provider dataset/product identifier;
- canonical instrument ID, provider symbol, venue MIC and currency;
- interval, exchange session/date and canonical UTC timestamps;
- provider market timestamp/timezone and BigBrain ingestion timestamp;
- raw versus adjusted status, adjustment policy/version and corporate-action references;
- import/dataset ID, adapter/schema version, request scope and response checksum;
- provider revision/correction marker when available and BigBrain supersession link;
- validation result, missing/duplicate/gap findings and expected market calendar version.

Corrections append a new immutable dataset version. Published backtest evidence continues
to reference the old version until explicitly rerun. Deduplication uses canonical
instrument + venue + interval + session/open timestamp + source version; identical keys
with different values are corrections/conflicts, never silent overwrites.

## Time, sessions and bias constraints

Provider local timestamps are parsed with the recorded IANA exchange timezone and then
normalized to UTC. A versioned market calendar defines sessions, holidays, early closes
and daylight-saving transitions; absence of a candle is not automatically a gap.
Strategies may only observe data whose availability timestamp is at or before simulated
decision time. Adjusted prices must never leak a future split/dividend into an earlier
decision. Universe membership and delisted instruments must be time-bounded; otherwise
results carry an explicit survivorship-bias limitation and cannot qualify a strategy for
promotion.

## Operational expectations

Adapters use runtime-injected secrets, bounded pagination, rate-limit budgets with jitter,
timeouts and retry only for safe reads. Imports are idempotent. Outage, stale data,
partial pages, duplicate/missing candles and provider corrections are explicit results.
A second provider is a validation source, not silently mixed into one series. Provider
disagreement is journaled and resolved by a declared dataset policy.

## Sources reviewed

Primary provider documentation and terms were reviewed 2026-08-10:

- [Twelve Data API/reference model](https://twelvedata.com/docs/advanced),
  [Nordic EOD coverage](https://support.twelvedata.com/en/articles/12682324-end-of-day-eod-pricing-market-data),
  [pricing](https://twelvedata.com/pricing) and [terms](https://twelvedata.com/terms)
- [Tiingo EOD documentation](https://www.tiingo.com/documentation/end-of-day),
  [pricing](https://www.tiingo.com/about/pricing) and [terms](https://api.tiingo.com/tos/)
- [Massive stock API](https://massive.com/docs/rest/stocks),
  [flat files](https://massive.com/docs/flat-files/stocks/overview),
  [pricing](https://massive.com/pricing?product=stocks) and
  [market-data terms](https://massive.com/legal/market-data-terms-of-service)
- [Alpha Vantage documentation](https://www.alphavantage.co/documentation/) and
  [terms](https://www.alphavantage.co/terms_of_service/)
- [Finnhub pricing/coverage](https://finnhub.io/pricing-stock-api-market-data)
- [Nasdaq Data Link documentation](https://docs.data.nasdaq.com/docs/getting-started)
- [Interactive Brokers market-data documentation](https://www.interactivebrokers.com/campus/ibkr-api-page/twsapi-doc/)
- Stooq public download surface; no sufficiently explicit official API/storage license
  was located, so it is not eligible for automated M2 ingestion without clarification.
