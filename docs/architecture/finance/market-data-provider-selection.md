# Finance market-data provider selection

Status: EODHD Free is active for bounded read-only daily EOD personal research after BB-078;
zero-cost live/near-live selection remains open. BB-071 separately has positive human
evidence for a qualifying paid Twelve Data Personal plan.
Prices and product terms below are time-sensitive observations, not permanent facts.

Current product-owner budget constraint: **0 SEK** for external Finance market-data services.

## Decision

EODHD Free is the selected and active zero-cost source for bounded daily EOD historical
research only. BB-078 acquired the complete eight-symbol watchlist in eight successful
requests without retry and activated durable local memory. It exposes the past year and
requires deletion of all covered copies within one month after expiry. No live/near-live
source is selected, and Twelve Data remains an inactive paid fallback.

The earlier live research reviewed Twelve Data Basic, Alpaca Basic/IEX, EODHD Free Starter
and Alpha Vantage Free. Its conditional Basic ranking is superseded by human evidence that
Basic is evaluation/trial only and a paid Personal plan is required.

BB-075 historically rechecked first-party evidence on 2026-08-11. Its result was **FAIL CLOSED**. Alpaca
Basic/free IEX and EODHD Free Starter require exact human clarification; Stooq, Nasdaq Data
Link and Finnhub lack complete product-specific rights evidence; Yahoo/yfinance and Alpha
Vantage are incompatible with the intended automation/research scope; FMP requires prior
written download/derivative approval. No exact free source passed under that earlier policy.

The earlier exact Basic/US pass resolved to **STATE B — HUMAN CONFIRMATION REQUIRED**.
Direct written correspondence from Liam at Twelve Data now confirms that the submitted
private/self-hosted personal use may store and retain data locally, conduct testing and
research, retain data (including derived data and audit metadata) after termination and use
the data for investment of only the owner's own funds on a qualifying **Personal plan**.
Basic is limited to evaluation/trial symbols and is insufficient. This clears entitlement
for that paid personal scope only; it does not select or activate Twelve Data. See the
[human confirmation report](../../reports/features/finance/finance-twelve-data-human-entitlement-confirmation-20260811.md).

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
Twelve Data is therefore an **entitlement-cleared paid fallback / qualified candidate** for
the submitted scope. Commercial/paying-subscriber, redistribution, customer/third-party,
business, unknown-market and materially different use are not covered and require renewed
review. No Twelve Data plan, account, credential, adapter or real-data ingestion exists.
Under the hard current budget Twelve Data is also **INACTIVE DUE TO ZERO-BUDGET CONSTRAINT**.

Selection follows this cost order: **(1) free, (2) local/open-source, (3) existing BigBrain
infrastructure, (4) paid only after verified need**. Twelve Data's positive entitlement does
not bypass that order. Alpaca Basic/free IEX is the next research candidate; its personal
use, storage, raw/normalized retention, replay/backtest, accumulation, backup/revision,
derived/audit, post-termination, personal-funds and IEX/exchange conditions remain unresolved.
No Alpaca inquiry is recorded as sent. Cost cannot relax licensing, provenance, security or
live-data quality.

The dated full zero-cost matrix and first-party source list are in the
[BB-075 report](../../reports/features/finance/finance-zero-cost-real-market-data-gate-20260811.md).

## BB-076 capability-scoped policy update

ADR 0022 permits `OwnerAcceptedPersonalResearch` for a legitimate 0-SEK source and exact
private read-only capability when no identified term prohibits it. It cannot override
payment, prior-approval, automation, retention or access-control restrictions.

Stooq's bounded official daily historical download is owner-accepted at the entitlement
evidence layer. A 2026-08-11 request returned a JavaScript verification challenge rather
than CSV, so the technical gate failed and BigBrain did not bypass or activate it. Alpaca
retained IEX use remains human-confirmation-required. EODHD's personal storage/analysis
grant applies while subscribed and carries a one-month deletion duty after expiry. BB-077
implemented that lifecycle and BB-078 subsequently activated the bounded EOD capability.

BB-082 repeated the bounded check on 2026-08-12. Both the Stooq terms route and the
documented `q/d/l` daily CSV route returned the same JavaScript proof-of-work/browser
verification page. This is an access control under ADR 0022 and the owner rule cannot
override it. pandas-datareader remains only a wrapper around that same route and supplies
no independent entitlement. EODHD Free remained approximately one year, Alpha Vantage
documented full daily output as premium-only, and Nasdaq Data Link classified QuoteMedia US
EOD as premium. No second provider is selected; no source precedence or stitching policy is
activated.

## BB-077 EODHD Free selection

Current first-party material checked 2026-08-11 names the tier `Free` rather than Free
Starter: €0, 20 calls/day, past-year EOD for any ticker after free registration/API key.
Terms expressly permit a non-professional individual to store, manipulate and analyze data
privately/non-commercially during the active subscription and require every copy deleted
within one month after termination/expiry. Redistribution/public access is prohibited.

Under ADR 0022, daily historical acquisition, active-account local storage, normalization,
private analysis and deterministic replay are selected and authorized. Post-expiry use and
retention are denied. Derived artifacts are not exempt from deletion. Corporate actions,
intraday and live are outside this selection. BB-078 crossed the credential boundary on
2026-08-11: exactly eight bounded EOD calls succeeded without retry for all configured
symbols. EODHD is now the **ACTIVE READ-ONLY EOD RESEARCH SOURCE** for these capabilities
only; it is not a live source, broker or global provider authorization.

## Dated entitlement matrix

The Twelve Data column includes direct provider correspondence reviewed 2026-08-11 for the
submitted personal use and a qualifying Personal plan. Other columns remain based on public
material reviewed 2026-08-10. Evidence is product/use specific.

| Requirement | Twelve Data | Tiingo | Massive |
| --- | --- | --- | --- |
| Personal/private eligibility | **Supported by human response** for the submitted use on Personal; Basic insufficient | **Confirmed**: individual API plan, internal consumption | **Confirmed**: individual, personal non-business use |
| US EOD | **Confirmed** | **Confirmed** | **Confirmed** |
| Swedish/Nordic EOD | **Confirmed** for listed EOD venues including XSTO/XOSL; exact instrument entitlement must be sampled | **Unclear** | **Unclear/not the documented stock focus** |
| Local raw storage while subscribed | **Supported by human response** on Personal for submitted scope | **Confirmed**, only while subscription is active | **Unclear**: API/flat-file access exists, but default terms call data display-only and restrict copying/non-display use |
| Maximum storage duration | **Local and post-termination retention supported** by human response for submitted Personal scope | **Confirmed** only as active-subscription duration; any shorter product limit remains unclear | **Unclear** while active |
| Deterministic private backtesting | **Supported within submitted testing/research scope** on Personal | **Unclear** because derived-data creation requires written approval | **Prohibited without additional license** under the non-display/derived-work restriction |
| Corporate-action storage | **Unclear** | **Unclear** despite EOD split/dividend fields | **Unclear** |
| Derived metrics/report retention | **Supported**, including after termination, for submitted Personal scope | **Prohibited without written approval** | **Unclear/prohibited without non-display license** |
| Raw retention after cancellation | **Supported by human response** for submitted Personal scope | **Prohibited**: promptly and permanently delete | **Prohibited**: cease use and delete all market data |
| Redistribution | **Prohibited** absent add-on/agreement | **Prohibited** absent permission | **Prohibited** absent consent/license |
| Exchange-specific restrictions/fees | **Confirmed possible; exact XSTO/product obligations unclear** | **Unclear for intended scope** | **Confirmed possible; US exchange agreements apply by dataset** |

The direct Twelve Data response supersedes the earlier public-text uncertainty only for the
submitted Personal-plan use. It does not authorize Basic or another provider/product.
BB-045 real ingestion remains blocked by cost-first selection and explicit activation approval.
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
