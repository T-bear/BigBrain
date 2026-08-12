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

Current BB-079 runtime is REAL EOD rather than the historical BB-074 empty baseline. Verify
2,008 observations plus `core-daily-v1`, exact market/feature revision labels and compact
SMA20/EMA20/RSI14/ATR14/volatility20/momentum20/volume-ratio20 values. The bounded
`GET /api/v1/modules/finance/features` response must expose warmup/quality and causal lineage.
The page describes measurements only; it must not render BUY/SELL, recommendations or order
controls. EODHD request accounting must remain unchanged during local feature builds.

BB-080 additionally requires the read-only Backtests / Strategiforskning section, exact lineage, zero/conservative cost comparison, equity/drawdown chart, immutable run/checksum and prominent RESEARCH/no-real-money language. `GET /api/v1/modules/finance/backtests` and a bounded exact run detail must work; no HTTP mutation or trade control is permitted. Offline backtest builds must not change provider request accounting.

- `/api/v1/system/health` and Web root are healthy.
- `/api/v1/modules/finance/observation` reports RESEARCH, EODHD Free, REAL EOD/delayed,
  active retention, eight instruments, 2,008 observations and durable persistence;
  PAPER/LIVE/broker remain false. Treat these dated BB-078/079 counts as the current
  2026-08-11 baseline, not an invariant for later scheduled accumulation.
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
