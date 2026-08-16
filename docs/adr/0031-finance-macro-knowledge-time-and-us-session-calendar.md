# ADR 0031: Finance macro knowledge time and US session calendar

- Status: Accepted
- Date: 2026-08-16

## Decision

Finance keeps Macro Memory as separate domain tables in the existing single-host Finance SQLite database. A macro observation stores reference period, explicit UTC knowledge/acquisition time, real-time/vintage bounds, artifact hash and evidence class. Features may select only values whose knowledge time is at or before the causal cutoff; forward-fill after that instant is allowed and backfill before it is forbidden.

FRED pack v1 is bounded to `DFF`, `DGS2`, `DGS10`, `CPIAUCSL` and `UNRATE`; 10Y–2Y is derived. Current-history FRED CSV without ALFRED vintage evidence is always `REVISED_HISTORY_EXPLORATORY`. It must never be represented as point-in-time causal evidence. ALFRED API real-time periods are the approved future route when a registered key and bounded vintage plan are explicitly activated.

US equity session instants use `America/New_York` 09:30–16:00 converted deterministically to UTC. Calendar `us-equities-ny-v1` implements regular US full-day holidays plus documented bounded exceptional closures. Unsupported exceptional/early-close completeness remains an explicit limitation; no weekday-only freshness calculation is permitted.

Finance schema changes are recorded in ordered `finance_schema_migrations`. Migrations are idempotent, restart-safe and fail startup on error; database deletion/reset is forbidden.

## Consequences

Macro provider failure degrades only Finance macro research. Existing prospective EOD cadence remains independent. Regime policy `market-regime-v1` is deterministic and explanatory; it cannot alter strategies, parameters or risk policy. SQLite remains appropriate, while Macro Memory is not added to the EODHD-specific persistence class.
