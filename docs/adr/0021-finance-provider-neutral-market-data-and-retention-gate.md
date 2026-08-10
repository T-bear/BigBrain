# ADR 0021: Provider-neutral market data and retention gate

- Status: Proposed
- Date: 2026-08-10

## Context

Reproducible Finance backtests require locally retained, versioned data, but provider and
exchange terms vary by product, market, use, caching period and subscription state. A
broker-coupled feed would also combine observation with a future order-capable boundary.

## Decision

Finance uses provider adapters feeding an immutable canonical dataset boundary. Domain
and strategy contracts never consume provider DTOs or ticker-only identity. M2 starts
with daily raw OHLCV and separately versioned corporate actions/adjustments for a small
allowlist; tick/order-book data is excluded.

Twelve Data is the primary candidate for Nordic/global EOD coverage, with Tiingo as a US
EOD specialist and Massive as a later US-depth alternative. No provider is activated
until BB-071 confirms in writing the exact personal/internal storage, deterministic
backtest, derived-result, cancellation-retention and non-redistribution rights.

Broker-provided market data is not the M2 default because it requires account-coupled
authentication, exchange subscriptions and an order-capable integration surface.

## Alternatives

- Select Twelve Data immediately: rejected until retention/cancellation terms are clear.
- Use Tiingo only: good US EOD economics, but Nordic suitability is not established.
- Use Massive only: strong consolidated US history and flat files, but US-focused.
- Use free Stooq/Alpha Vantage as canonical data: rejected because licensing/retention,
  limits, corporate actions or coverage are insufficiently clear for the required archive.
- Use IBKR: deferred to broker evaluation; historical limitations and coupling are poor
  foundations for independent reproducible research.

## Consequences

BB-045 depends on BB-071. Storage must preserve provenance, corrections, adjustment state,
market calendars and immutable versions. Multiple adapters remain possible, but data is
never silently blended. This ADR authorizes no account, credential, SDK or ingestion.
