# Finance read-only observation runtime verification

- Status: Verified for BB-074
- Scope: BigBrain API and Web read-only Finance observation
- Risk: Low when all requests remain read-only

## Preflight

Confirm `HEAD == origin/main`, understand unrelated local changes, record API/Web container
identity and health, and preserve all named application volumes. Run solution restore/build/
tests, full Web tests/build, documentation verification, Compose validation and
`git diff --check`.

## Deployment

Build existing `api` and `web` images. Recreate only API first with `--no-deps`, wait for
healthy and verify the Finance endpoint, then recreate only Web with `--no-deps`. Never use
`down -v`, remove a volume or recreate Sentinel/media services for this slice.

## Read-only verification

- `/api/v1/system/health` and Web root are healthy.
- `/api/v1/modules/finance/observation` reports RESEARCH; no authorized provider;
  `ZERO-COST ENTITLEMENT GATE`; PAPER/LIVE/broker/ingestion/real-storage false; eight configured unpriced
  research instruments; zero observations; no configured persistence.
- POST/PUT/PATCH/DELETE on the observation route return 405.
- Headless mobile and desktop checks show Finance navigation, explicit no-real-money and
  entitlement warnings, empty watchlist/chart/memory states, no trade controls, no overflow,
  no console error and no external request.
- Compare sanitized aggregate Calendar/theme/Media reads before and after. Do not print
  private calendar values, media identities, credentials or raw logs.
- Confirm the historical-memory policy is `zero-cost-provider-unresolved`, with zero
  observations and no silent synthetic fallback.

## Rollback

Rebuild the last known-good API/Web commit and recreate only those services while retaining
all volumes. A rollback must not activate a provider, mutate external media or delete data.
