# Finance Sprint 2 – market-data provider and licensing research

## Metadata

- Date: 2026-08-10
- Scope: BB-046 provider, licensing, cost and M2 boundary research
- Related commit: assigned on publication

## Status

BB-046 research is complete. No provider is selected/activated and no account, SDK,
credential, source code, runtime or deployment changed. BB-071 blocks BB-045 until exact
local retention and post-cancellation rights are confirmed.

## Evidence

Public primary documentation, pricing and terms were reviewed for eight candidates on
2026-08-10. Prices are snapshots and require revalidation before purchase.

| Provider | Coverage/data | Indicative personal price | Licensing/operational finding | Assessment |
| --- | --- | --- | --- | --- |
| Twelve Data | Global stocks/ETFs/FX/crypto; XSTO and Nordic EOD; daily–minute, REST/WS, MIC/FIGI tiers | Free; Grow from USD 29/mo, Pro 99, Ultra 329 | Internal use/storage stated; redistribution denied; cache duration and post-cancellation retention unclear | Primary M2 candidate after BB-071 |
| Tiingo | 30+ years EOD, raw/adjusted, splits/dividends; US/China and broad ETF/fund catalog | Free or USD 30/mo individual | Internal consumption only; redistribution by agreement; retained-data/derived-work language needs confirmation | US EOD specialist |
| Massive (Polygon) | Consolidated US equities, active/delisted reference, daily/minute/trade/quote flat files and REST/WS | Free 2y; USD 29/79/199 monthly snapshots | Personal non-business use; strong US provenance; retention after termination not explicit | US depth alternative |
| Alpha Vantage | Global symbol examples; 20+ years daily/intraday, raw and adjusted, splits/dividends | Free plus time-sensitive premium tiers | Useful prototype API but premium endpoints/limits and retention rights are insufficiently explicit | Fixture/prototype only after terms review |
| Finnhub | US/global market products; daily/minute/tick and WebSocket by tier | Free; USD 49.99/129.99/199.99 market tiers shown, broader package much higher | Personal-use tiers; useful coverage but licensing details and cost/benefit weaker for M2 | Not preferred |
| Nasdaq Data Link | Dataset marketplace with free and à-la-carte premium REST/streaming/table data | Dataset-specific | Coverage, price, export and rights vary per dataset; not one stable canonical equity feed | Specialized future datasets |
| Interactive Brokers | Account/subscription-linked historical/live data across broker instruments | Exchange/account specific | Pacing, history/delisted limitations and order-capable account coupling conflict with independent M2 research boundary | Defer to broker milestone |
| Stooq | Public historical downloads for multiple markets | Free surface | No sufficiently explicit official API, storage, correction or redistribution license found | Not eligible without clarification |

Primary links are cataloged in the
[provider-selection document](../../../architecture/finance/market-data-provider-selection.md).

## Changes

The canonical provider-neutral ingestion/provenance boundary and Proposed ADR 0021 were
added. BB-046 is complete; BB-071 is the narrow licensing confirmation gate and BB-045
remains planned.

## Security

Detta är en sanerad GitHub-version. No provider account, credential, secret, private
identifier, SDK, broker connection or order capability was created. Public prices and
terms are dated observations, not legal advice.

## Remaining work

BB-071 must obtain owner-reviewed written confirmation for personal Swedish internal
storage, deterministic backtesting, derived results, corporate actions and retention
after cancellation. Provider/exchange terms may change. Delisted/universe completeness
and exact Nordic symbol coverage must be sampled under the approved entitlement.

## Resumption

Complete BB-071 without creating an integration. If confirmed, BB-045 should implement
direct HTTP ingestion of daily raw OHLCV and separate corporate actions for a small
allowlist, with immutable provenance, calendar-aware gaps and correction versions.
