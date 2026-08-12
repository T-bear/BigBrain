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
