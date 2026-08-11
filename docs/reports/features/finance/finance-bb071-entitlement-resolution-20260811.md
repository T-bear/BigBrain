# Finance BB-071 Twelve Data Basic entitlement resolution

## Metadata

- Date checked: 2026-08-11
- Candidate: Twelve Data Basic (Free), individual/personal, US listed equities and ETFs
- Evidence: current Twelve Data first-party pricing, documentation, support/licensing and Terms of Use only
- Result: **STATE B — HUMAN CONFIRMATION REQUIRED**
- Runtime: unchanged; no account, key, API call, payload or real observation
- Related commit: assigned on publication

## Status

Public evidence resolves technical access and some license concepts but does not resolve
the complete BB-071 artifact lifecycle. Unknown uses remain denied. BB-071 stays open.

## Changes

This report maps seventeen intended uses, specifies the first real-data experiment and its
zero-cost quota budget, and narrows the provider inquiry to the minimum unanswered matters.
No adapter or provider-specific code was warranted while the entitlement remains unknown.

## First-party findings

Twelve Data Basic is free and currently lists 8 API credits per minute, 800 per day,
internal non-display usage and real-time US equities/ETFs. The US-equities guide states that
the default current feed covers roughly 5% of total US trade volume and that next-day US
historical/EOD data on Basic covers the consolidated market. Basic requires an account/API
key. Full WebSocket access requires a paid Pro plan; Basic's eight WS credits are explicitly
trial access, so the proposed experiment does not depend on WebSocket.

Terms section 2 permits internal access, receipt, processing and storage; compliant Derived
Data may be created when the original cannot be reverse-engineered. Basic explicitly permits
non-display usage and free-tier commercial use is prohibited. Sections 2.3 and 3 make
documentation timeframes and third-party/exchange restrictions controlling. Section 16
permits retention only for the subscription-permitted duration and requires all Data to be
deleted within 30 days after termination/expiry; compliance audit trails may remain.

The terms define `Data` and `Derived Data` separately and state that the customer retains
rights to compliant Derived Data. They do not explicitly say whether non-reversible features,
shadow predictions, outcomes and aggregate metrics may survive termination, nor whether an
ordinary reproducibility/provenance record qualifies for the audit exception. Those points
cannot be inferred.

## Intended-use entitlement matrix

Confidence describes the reading of the cited public text, not a substitute for permission.

| ID | Intended use | Classification | Public evidence and interpretation | Confidence |
| --- | --- | --- | --- | --- |
| A | Download US historical observations | **VERIFIED ALLOWED** | US guide: historical/EOD available from Basic; Terms 2.2 permits access/receive | High |
| B | Receive US live/current observations | **VERIFIED ALLOWED** | Basic pricing lists real-time US equities/ETFs; feed is partial-volume, not consolidated | High |
| C | Normalize observations | **VERIFIED ALLOWED** | Terms 2.2(a) permits internal processing | High |
| D | Store observations locally while Basic remains active | **UNKNOWN** | Terms permits internal storage but defers timeframe and third-party limits; “local” and exact Basic duration are not stated | Medium |
| E | Retain observations long-term | **UNKNOWN** | Retention lasts only as subscription/documentation permits; no Basic maximum found | High |
| F | Deterministic historical replay | **UNKNOWN** | Internal non-display analytics is supported, but repeated versioned replay is not named | Medium |
| G | Non-display backtesting | **UNKNOWN** | Research/automation examples exist; exact stored deterministic backtesting grant is absent | Medium |
| H | Forward testing | **UNKNOWN** | Not expressly described | High |
| I | Create immutable shadow predictions | **UNKNOWN** | Could be derived evidence, but this artifact class is not described | High |
| J | Compare predictions with later outcomes | **UNKNOWN** | Prospective outcome attachment is not described | High |
| K | Compute non-reversible indicators/features | **VERIFIED ALLOWED** | Terms 2.2(c), 6.2 and Basic technical-indicator feature | High |
| L | Retain compliant derived metrics while active | **VERIFIED ALLOWED** | Customer retains rights to compliant non-reversible Derived Data; active-term use remains tier-bound | Medium |
| M | Train/evaluate BigBrain strategy logic | **UNKNOWN** | Internal analytics/automation is supported, but strategy training/evaluation scope is not explicit | Medium |
| N | Internal use without redistribution | **VERIFIED ALLOWED** | Basic internal non-display; individual plans personal/internal; redistribution prohibited | High |
| O | Retain audit/provenance metadata | **UNKNOWN** | Compliance audit trail may survive, but ordinary BigBrain provenance/reproducibility scope is unstated | High |
| P | Retain non-reversible derived artifacts after termination | **UNKNOWN** | Derived ownership exists, but post-termination retention is not explicit alongside section 16 deletion | High |
| Q | Delete provider Data after termination | **VERIFIED PROHIBITED to retain** | All Data must be deleted within 30 days; certification may be required | High |

Items D–J, M, O and P deny ingestion. Public documentation is therefore insufficient to
close BB-071 even though A–C, K–L, N and Q are materially clearer.

## Prepared first real-data experiment

Working name: **FIRST AUTHORIZED FREE MARKET DATA INGESTION**. It remains inactive.

- Provider/product: Twelve Data Basic, only if written entitlement and owner approval pass.
- Market: US listed equities/ETFs; regular session only; no OTC, pre/post or Nordic data.
- Granularity: 15-minute REST snapshots/candles plus daily close reconciliation; no tick
  assumption and no WebSocket dependency.
- Universe: `SPY`, `QQQ`, `IWM`, `AAPL`, `MSFT`, `JPM`, `XOM`, `JNJ`.
- Rationale: three liquid broad/style/size ETFs plus technology, financials, energy and
  healthcare; eight symbols fit one 8-credit batch. This is a systems-validation universe,
  not an investment recommendation or portfolio.
- Bootstrap: daily raw/unadjusted OHLCV, target five years where the endpoint returns it;
  corporate actions remain separate and must be confirmed available on the exact tier.
- Boundary: observation/research/shadow evidence only; no strategy promotion, PAPER,
  broker, order, allocation or AUTO.

The default real-time feed represents only about 5% of US trading volume. Every observation
must therefore identify the exact feed and freshness; it must not be called a consolidated
exchange view. Next-day EOD is the reconciliation evidence, not a silent overwrite.

## Zero-cost request budget

Assumptions use first-party documented weights: `/time_series` is 1 credit per symbol,
batching does not reduce credits, Basic permits 8/minute and 800/day, and a regular US
session contains 26 fifteen-minute boundaries.

| Purpose | Planned credits |
| --- | ---: |
| One-time five-year daily bootstrap, eight symbols (up to 5,000 points/request) | 8 |
| Normal session: 26 batches × 8 symbols | 208/day |
| Daily close reconciliation: 8 symbols | 8/day |
| Quota/health checks | 4/day |
| Restart recovery reserve: up to 10 missed batches | 80/day reserved |
| Retry/correction reserve | 40/day reserved |
| Reference/metadata reserve | 16/day reserved |
| Planned maximum including reserves | **356/day** |
| Unallocated safety capacity | **444/day (55.5%)** |

Each scheduled batch uses exactly the 8/minute ceiling, so implementation must not issue a
concurrent credit-bearing request in that minute. Health/reference work is scheduled away
from observation batches. A local hard budget stops requests before 356 credits/day; 429
means provider-rate-limit outage evidence, never “price unchanged.” WebSocket is excluded
because Basic access is trial-only. If endpoint weights or terms change, the adapter fails
closed until its versioned budget/policy is reviewed.

Restart recovery requests only the missing bounded range and deduplicates through existing
canonical observation and immutable revision identity. Closed sessions, unknown sessions,
provider outage, missing data, late delivery and corrections remain distinct. Historical
bootstrap and current evidence converge only after entitlement, normalization and explicit
provenance; conflicts append rather than overwrite.

## Human confirmation required

Twelve Data support/licensing must answer the exact Basic/US scope. A free account is
technically required for any later API use, but it is **not required to send the inquiry**
because official support/legal contact addresses are public. Do not create the account yet.

The canonical ready-to-send message is in
`docs/architecture/finance/provider-retention-inquiry.md`. A complete response must identify
the governing Basic tier/terms date and answer each artifact/deletion question. Marketing
language, silence or successful API access does not close the gate.

## Evidence

First-party material retrieved/rechecked 2026-08-11:

- [Individual pricing](https://twelvedata.com/pricing)
- [Terms of Use, last updated 2026-01-01](https://twelvedata.com/terms)
- [Commercial and personal usage](https://support.twelvedata.com/en/articles/5332349-commercial-and-personal-usage)
- [US equities market data](https://support.twelvedata.com/en/articles/9935903-us-equities-market-data)
- [Trial / Basic limits](https://support.twelvedata.com/en/articles/5335783-trial)
- [Credits](https://support.twelvedata.com/en/articles/5615854-credits)
- [Historical prices](https://support.twelvedata.com/en/articles/5656039-how-to-get-historical-prices)
- [Batch requests](https://support.twelvedata.com/en/articles/5203360-batch-api-requests)
- [Attribution](https://support.twelvedata.com/en/articles/12647398-attribution-guidelines-for-using-twelve-data)
- [Account requirement](https://support.twelvedata.com/en/articles/5544963-how-do-i-create-a-twelve-data-account)

No legal conclusion is inferred beyond these texts. Product/terms are time-sensitive and
must be snapshotted as evidence when any later account is approved.

## Security

Detta är en sanerad GitHub-version. No account, identity, credential, token, private
correspondence, provider payload, real price, network integration, paid dependency, broker,
order or runtime state is included. Finance retains zero real-money authority.

## Verification

- `dotnet restore BigBrain.slnx` — pass.
- `dotnet build BigBrain.slnx -c Release --no-restore` — pass, zero warnings/errors.
- `dotnet test BigBrain.slnx -c Release --no-build --no-restore` — pass, 344 API plus
  32 Sentinel tests, 376/376 total.
- `node scripts/verify-documentation.mjs` — pass, 123 Markdown files and 73 unique BB IDs.
- `git diff --check` — pass.
- `docker compose config --quiet` — pass.

## Remaining work

- Obtain and privately retain a dated provider response; publish only a sanitized decision.
- Map the response to every `UNKNOWN`; reject or narrow the experiment if any remains.
- Obtain explicit product-owner activation approval in a new prompt.
- Only then implement the isolated adapter, credential injection and local persistence.

## Resumption

Next action: send the exact Twelve Data inquiry. BB-071 remains open. Do not implement the
adapter or create a free account before affirmative evidence and owner approval.
