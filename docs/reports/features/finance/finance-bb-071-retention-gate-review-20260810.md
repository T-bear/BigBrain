# Finance BB-071 – market-data retention gate and ADR 0021 review

## Metadata

- Date: 2026-08-10
- Scope: BB-071 public-terms review, provider contact package and ADR 0021 owner review
- Related commit: assigned on publication

## Status

ADR 0021 is Accepted after explicit product-owner review of its architectural direction.
BB-071 remains open and waits for provider confirmation. BB-045 remains blocked. No
provider account, credential, adapter, ingestion, runtime change or deployment exists.

## Evidence

Official public terms and product documentation for Twelve Data, Tiingo and Massive were
reviewed on 2026-08-10 against the intended private Swedish EOD archive and deterministic
backtesting use.

| Provider | Confirmed public permission | Material restriction or uncertainty | Gate result |
| --- | --- | --- | --- |
| Twelve Data | Internal access, processing and storage; non-reversible derived data; documented US and Nordic EOD coverage | Retention is limited by subscription/documentation; termination requires deletion within 30 days; exact non-display tier, corporate-action retention and exchange obligations need confirmation | Preferred Nordic candidate, pending written entitlement |
| Tiingo | Individual/internal API consumption and local storage while subscribed; long US EOD history with splits/dividends | Termination requires permanent deletion; derived data requires express written approval; Nordic suitability unestablished | US EOD comparison only, pending agreement |
| Massive | Personal non-business eligibility and extensive US REST/flat-file coverage | Default terms restrict non-display/derived use and require all market data deletion at termination | Defer unless a suitable non-display license is offered |

The public default terms do not permit the intended post-cancellation raw archive. They
also do not establish every required right for durable deterministic evidence. Silence is
not treated as permission, so BB-071 cannot be completed.

Sources: [Twelve Data terms](https://twelvedata.com/terms),
[Twelve Data Nordic EOD coverage](https://support.twelvedata.com/en/articles/12682324-end-of-day-eod-pricing-market-data),
[Tiingo terms](https://api.tiingo.com/tos/),
[Tiingo EOD documentation](https://www.tiingo.com/documentation/end-of-day),
[Massive market-data terms](https://massive.com/legal/market-data-terms-of-service) and
[Massive stock documentation](https://massive.com/docs/rest/stocks).

## Changes

- ADR 0021 records its accepted status and the scope of owner approval.
- The provider-selection record now distinguishes confirmed, likely, unclear and
  prohibited rights and defines entitlement enforcement for BB-045.
- BB-071 is in progress/waiting for external confirmation; BB-045 remains planned and
  blocked.
- A reusable inquiry plus provider-specific variants is ready in
  `docs/architecture/finance/provider-retention-inquiry.md`.

## Security

Detta är en sanerad GitHub-version. No secret, account identity, API key, private quote,
broker connection, market-data request, order capability or user data is present. ADR
acceptance is architecture approval only and cannot activate external access.

## Remaining work

The product owner must send the inquiry, beginning with Twelve Data, and retain the full
dated response privately. BB-071 can close only if an exact plan/addendum explicitly
covers storage duration, non-display deterministic backtesting, corporate actions,
derived evidence, cancellation deletion/retention, selected markets and fees. Otherwise
the provider/product must be rejected and another licensed source evaluated.

## Resumption

Resume from BB-071 in `docs/BACKLOG.md`, the accepted ADR 0021, the provider-selection
matrix and the prepared inquiry. Do not start BB-045, create an account or add credentials
until the sanitized owner-reviewed entitlement decision is published.
