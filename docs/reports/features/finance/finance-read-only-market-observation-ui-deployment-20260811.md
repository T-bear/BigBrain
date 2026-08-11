# Finance read-only market observation UI deployment

## Metadata

- Date: 2026-08-11
- Scope: BB-074 API/Web deployment and technical runtime verification
- Source baseline: `4ddcfcb7e42a75d9009146996bf8eb94d21bdba9`

## Status

BigBrain API and Web are deployed and healthy. BB-074 is technically runtime-verified at
mobile and desktop widths. No manual product-owner UI approval is claimed.

## Evidence

Pre-deploy restore/build passed with zero warnings/errors; 351 API, 32 Sentinel and 106 Web
tests passed. Web production build, documentation verification, Compose validation and
whitespace validation passed.

Runtime API returned RESEARCH; PAPER, LIVE, broker, ingestion and real-provider storage
false; provider `noneAuthorized`; entitlement `pendingWrittenConfirmation` with BB-071 State
B; eight configured watchlist entries with zero prices; zero observations; data kind none;
and persistence not configured. POST/PUT/PATCH/DELETE returned 405.

Headless Chromium at 390×844 and 1440×1000 verified Finance navigation, RESEARCH and
no-real-money warnings, pending entitlement, research watchlist, empty chart/memory, both
storage/ingestion denials and absence of trading controls. There was no overflow, console
error or external browser request.

Read-only smoke preserved two aggregate Calendar import records, six current-week event
records, the configured theme, online Media overview and the Download Control read surface.
API persistent mount destinations remained present; unrelated Sentinel and Flaresolverr
containers retained identity/start time and zero restarts.

## Changes

Only existing API and Web images were rebuilt and their containers recreated in dependency
order. No Compose configuration, volume, database schema, external service or source code
changed. Documentation records deployment evidence and adds the repeatable read-only runbook.

## Security

No provider account, key, SDK, adapter, market-data request, real payload, feed, Finance
database, broker, order or trading mode was created or enabled. No destructive Media or
Download Control request was used. Unknown entitlement remains fail-closed.

Detta är en sanerad GitHub-version. It excludes container identities, internal addresses,
credentials, raw logs, private calendar content and media identities.

## Remaining work

Manual product-owner UI review is optional follow-up evidence and is not inferred here.
BB-071 written entitlement confirmation and explicit owner approval still block real data.

## Resumption

Use the Finance read-only observation verification runbook. The next real-data gate remains:
BB-071 written entitlement confirmation → explicit product-owner approval → first authorized
free market-data ingestion. The prepared inquiry remains in
`docs/architecture/finance/provider-retention-inquiry.md`.
