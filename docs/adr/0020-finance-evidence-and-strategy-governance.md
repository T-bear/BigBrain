# ADR 0020: Finance evidence and strategy governance

- Status: Proposed
- Date: 2026-08-10

## Context

Historical performance can be distorted by costs, leakage, overfitting, survivorship and
unrealistic execution.

## Decision

Strategies are deterministic and versioned before any AI discretion. Validation separates
in-sample, validation and out-of-sample evidence, adds walk-forward/sensitivity/regime
testing where appropriate, and reports gross and net results after modeled costs. Strategy
lifecycle promotion requires evidence and owner-approved governance, never recent wins
alone. Paper trading precedes every live mode.

## Consequences

Datasets, parameters, policies and reports need durable versions. Historical profitability
never guarantees future results, and a cost-negative strategy fails validation.
