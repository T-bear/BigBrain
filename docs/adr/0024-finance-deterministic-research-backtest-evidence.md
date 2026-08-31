# ADR 0024: Finance deterministic research backtest evidence

- Status: Accepted
- Date: 2026-08-12
- Accepted by: Product owner through BB-080
- Related: ADR 0020, ADR 0021, ADR 0023

## Context

Research results must remain reproducible after newer market data, features or simulation assumptions exist. A completed daily close cannot causally fill at that same close.

## Decision

A backtest identity is the deterministic fingerprint of exact market revision IDs, exact feature revision, strategy/version/parameters, simulation and cost model versions, initial capital, universe, date range, whole-share sizing policy and seed. Identical inputs produce the same run ID, journal, fills, curves, metrics and checksum; changed inputs append a new immutable result.

`daily-next-session-open-v1` decides only after a completed bar using features whose knowledge time is no later than the decision boundary. A transition intent may fill only at the next available session open. The first portfolio is cash plus unlevered long whole-share positions. `zero-cost-v1` is diagnostic only; `conservative-cost-v1` applies explicit per-share commission, minimum commission and adverse bps slippage.

All artifacts are offline RESEARCH evidence, never external orders. EODHD-derived runs, events, simulated fills, curves, metrics and indexes inherit source deletion lineage.

## Consequences

Old evidence remains readable beside later datasets. Raw/unadjusted prices, incomplete corporate actions, full-liquidity fills, current-survivor selection and the short window are prominent limitations. This decision creates no PAPER, LIVE, broker or execution authority.

## BB-123 amendment — 2026-08-31

New runs use `daily-next-session-open-v2` plus `next-session-open-full-fill/v2`; v1 rows remain
immutable historical evidence. The next session is now exact under `us-equities-ny-v1`; a missing
instrument bar is rejected rather than silently deferred to a later bar. Versioned assumptions
separate fixed, per-share/minimum and proportional commissions, assumed full-spread and adverse
slippage. Filled and rejected attempts, rejection reasons and commission/spread/slippage totals are
part of immutable result lineage. Spread is an assumption, never a quote observation.

Daily aggregate volume does not establish open liquidity or intraday participation. Partial fills
and volume caps remain unsupported rather than receiving fabricated precision. FX is outside the
current USD-only universe. Equivalent concurrent builders converge through atomic insertion and
checksum verification; different evidence under one run ID still fails closed.
