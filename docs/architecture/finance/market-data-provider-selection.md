# Finance market-data provider selection

Status: BB-046 and BB-072 research complete; BB-071 waiting for provider confirmation as
of 2026-08-11.
Prices and product terms below are time-sensitive observations, not permanent facts.

## Decision

No provider is selected or authorized. BB-072's dated free-source comparison found no
zero-cost product with a complete verified grant for durable retention plus personal
non-display backtesting. The explicit result is **DO NOT INGEST YET**. EODHD Free Starter
is the best conditional free evaluation lead, but it exposes only the past year and its
terms require deletion within one month after expiry. Twelve Data Basic is the best Nordic
technical lead, but its exact durable-retention/derived-use scope remains unresolved.

Within the earlier paid-capable shortlist, Twelve Data remains the primary candidate
for M2 because its official EOD coverage includes Nasdaq Stockholm (`XSTO`) and other
Nordic exchanges, its reference model exposes MIC and optional FIGI/ISIN, and it supports
daily through intraday OHLCV. Tiingo is the preferred specialized comparison for long US
end-of-day history and corporate-action-aware raw/adjusted data. Massive is the US-depth
alternative when consolidated US coverage, delisted identifiers, flat files or later
minute data justify its higher and US-focused scope.

Public terms do not satisfy the intended durable evidence archive. Twelve Data permits
internal processing/storage during the applicable subscription scope but limits retention
to the permitted subscription duration and requires deletion within 30 days after expiry.
Tiingo permits local storage only during an active subscription, requires deletion after
expiry and requires written approval for derived data. Massive requires deletion of all
market data after termination and restricts non-display/derived use without a license.
Activation therefore requires an affirmative written entitlement or a different product.
Until then, fixtures remain the only authorized source.

Selection is free-first and cost-aware: evaluate legally retainable free/self-hosted
sources before paid tiers, then choose the cheapest product that satisfies quality,
coverage and evidence needs. Free tiers and Stooq remain unverified candidates, not an
authorization. Cost cannot relax licensing, provenance, security or live-data quality.

## Dated entitlement matrix

Classification is based only on official public material reviewed 2026-08-10. “Likely”
is an interpretation, not permission.

| Requirement | Twelve Data | Tiingo | Massive |
| --- | --- | --- | --- |
| Personal/private eligibility | **Confirmed**: terms apply to individuals and internal use | **Confirmed**: individual API plan, internal consumption | **Confirmed**: individual, personal non-business use |
| US EOD | **Confirmed** | **Confirmed** | **Confirmed** |
| Swedish/Nordic EOD | **Confirmed** for listed EOD venues including XSTO/XOSL; exact instrument entitlement must be sampled | **Unclear** | **Unclear/not the documented stock focus** |
| Local raw storage while subscribed | **Confirmed**, within tier/documented retention limits | **Confirmed**, only while subscription is active | **Unclear**: API/flat-file access exists, but default terms call data display-only and restrict copying/non-display use |
| Maximum storage duration | **Unclear**: terms defer to subscription and documentation; no product-specific maximum was located | **Confirmed** only as active-subscription duration; any shorter product limit remains unclear | **Unclear** while active |
| Deterministic private backtesting | **Likely/interpretation** as internal non-display processing, but tier permission is not explicit | **Unclear** because derived-data creation requires written approval | **Prohibited without additional license** under the non-display/derived-work restriction |
| Corporate-action storage | **Unclear** | **Unclear** despite EOD split/dividend fields | **Unclear** |
| Derived metrics/report retention | **Likely/interpretation** when non-reversible; explicit post-expiry status is unclear | **Prohibited without written approval** | **Unclear/prohibited without non-display license** |
| Raw retention after cancellation | **Prohibited**: delete within 30 days | **Prohibited**: promptly and permanently delete | **Prohibited**: cease use and delete all market data |
| Redistribution | **Prohibited** absent add-on/agreement | **Prohibited** absent permission | **Prohibited** absent consent/license |
| Exchange-specific restrictions/fees | **Confirmed possible; exact XSTO/product obligations unclear** | **Unclear for intended scope** | **Confirmed possible; US exchange agreements apply by dataset** |

The intended ability to retain a reproducible raw archive after cancellation is not
available under the public default terms of any shortlisted provider. This is not inferred
permission. BB-071 remains open and BB-045 remains blocked.
The provider-neutral policy/provenance subset of BB-045 may proceed with synthetic
fixtures; only external adapter activation and persistence of provider data are blocked.

BB-072 completed its zero-cost review on 2026-08-11 across ten source/product paths. Free
access, public downloads or absence of authentication did not authorize ingestion.
Massive's default terms prohibit the needed non-display/strategy-derived use; FMP requires
prior written approval to download/create derivative works; Alpha Vantage and Stooq do not
provide a verified complete retention/backtesting grant; suitable Nasdaq Nordic history is
paid; Yahoo historical download is a paid interactive feature. The full evidence and rights
matrix are in the
[BB-072 report](../../reports/features/finance/free-historical-data-source-research-20260811.md).
BB-071's fail-closed standard still applies to the exact source/product/market.

The synthetic acquisition foundation does not change this decision. It proves only the
provider-neutral adapter handoff: a real adapter cannot be invoked by the prepared pipeline
unless one exact policy explicitly permits historical analysis, non-display backtesting,
derived metrics, long-term storage and persistence. `SyntheticFixture` policies are
structurally isolated from any external provider identity.

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

Provider/account activation must also bind each dataset to an entitlement record:
product/tier, effective terms version, approved markets, permitted purpose, retention
deadline and deletion obligation. An expired or unknown entitlement fails closed. The
implementation must support quarantining an incomplete import, detecting duplicates and
calendar-aware gaps, and appending provider corrections without rewriting prior evidence.

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

The zero-cost set was rechecked on 2026-08-11 using current first-party pricing, product
and terms material for EODHD, Twelve Data, Alpha Vantage, FMP, Massive, Stooq, Nasdaq
Nordic delayed/historical products, Yahoo Finance, Nasdaq Data Link and Tiingo's current
paid individual tier. The BB-072 report above is canonical for that dated comparison.

The BB-071 evidence relies particularly on Twelve Data Terms sections 2 and 16, Tiingo
Terms section 1.6, and Massive Market Data Terms sections 5 and 8. Provider replies must
identify the applicable product and override/addendum if they differ from these public
defaults. The ready-to-send inquiry is
[provider retention inquiry](provider-retention-inquiry.md).
