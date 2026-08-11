# EODHD Free entitlement revalidation

## Metadata

- Date: 2026-08-11
- Scope: BB-077 EODHD zero-cost product and entitlement revalidation
- Evidence: current first-party pricing, EOD API documentation and terms
- Related commit: assigned on publication

## Status

EODHD's current product is named **Free**, not Free Starter. It remains €0/month and
€0/year. Capability-scoped private EOD acquisition/storage/research is authorized under ADR
0022 while the account remains active. Post-expiry retention is denied and deletion is due
within one month. No account or credential was available during this slice.

## Evidence

- Pricing checked 2026-08-11: Free = €0, 20 calls/day, 20 requests/minute, past-year range,
  personal use, stocks/ETF/funds and limited EOD access; no card is required for free access.
- EOD API documentation: free registration/API key, any ticker within the past year, daily/
  weekly/monthly JSON or CSV, one call per symbol/range. US exchanges are updated after close.
- Response fields explicitly distinguish raw OHLC/close, split-and-dividend-adjusted close,
  and split-adjusted volume.
- Terms: a non-professional individual may store, manipulate and analyze data privately and
  non-commercially, but may not share, sell, retransmit, redistribute, display or grant access.
- Storage is permitted on the subscriber's premises during the active subscription. Every
  copy must be deleted within one month after termination/expiry; EODHD may request proof.

First-party sources: `https://eodhd.com/pricing`,
`https://eodhd.com/financial-apis/api-for-historical-data-and-volumes`, and
`https://eodhd.com/financial-apis/terms-conditions`.

## Changes

The exact policy is `EODHD` / `Free` / `eodhd-free-personal-v2026-08-11` with owner
acceptance `BB-077/2026-08-11`. Historical acquisition, active-account local storage,
normalization, private analysis and deterministic replay are allowed. Post-expiry use and
retention are denied. Derived artifacts are not assumed exempt and must remain covered by
the same deletion scope. Redistribution and public display are denied.

Corporate actions and live/intraday endpoints are not enabled because their exact Free-tier
availability and lifecycle are outside this bounded capability. The implemented path uses
raw daily OHLC, adjusted close only as separately classified provider metadata, and
split-adjusted volume. It is EOD/delayed, never live.

## Security

Detta är en sanerad GitHub-version. No API key, account identity, private URL, provider
payload, price, internal address or sensitive header is published.

## Remaining work

The owner must create the free account and configure the secret. Current terms can change;
revalidate before materially expanding capability or after an account/product notice.

## Resumption

Resume with the EODHD retention/deletion runbook. The smallest safe step is secret setup,
disabled-to-enabled deployment, bounded eight-symbol smoke and real-data verification.
