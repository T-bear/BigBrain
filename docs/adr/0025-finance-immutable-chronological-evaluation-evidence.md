# ADR 0025: Finance immutable chronological evaluation evidence

- Status: Accepted
- Date: 2026-08-12

## Context

A deterministic backtest is evidence for one exact period, not evidence that a strategy generalizes. Random time-series splits, selection from the test period, mutable evaluation summaries, or unbounded parameter searches would make later conclusions irreproducible and increase leakage and overfitting risk.

## Decision

Finance evaluation identity binds the exact market revisions, feature revision, strategy/version and reference parameters, simulation/sizing/cost models, universe and dates, chronological split, session embargo, fixed walk-forward plan, bounded diagnostic parameter grid, thresholds, robustness model and seed. Equal inputs produce equal underlying run IDs, evaluation ID, aggregate result and checksum. Results append immutably.

Train precedes embargo and test. Test observations never participate in train evidence. Expanding walk-forward windows use a fixed strategy/parameter set; later windows cannot revise earlier results. Parameter neighborhoods and the cost ladder are diagnostics only: no test-period winner may become a selected strategy parameter. Data-sufficiency thresholds override any numerical score. The transparent score remains decomposable research evidence and grants no PAPER/LIVE authority.

Derived evaluation artifacts inherit their exact provider lineage and deletion obligation. Generic plan definitions without licensed-derived values may remain. API and Web expose bounded reads only; generation is a local maintenance operation.

## Consequences

- Evaluation revisions remain independently auditable as local memory grows.
- Current approximately one-year evidence may correctly remain `INSUFFICIENT_DATA` despite a positive score or return.
- A changed plan, model, threshold, cost assumption or input creates new evidence instead of rewriting old evidence.
- Parameter optimization, automated selection, broker/order and PAPER/LIVE behavior remain outside this decision.
