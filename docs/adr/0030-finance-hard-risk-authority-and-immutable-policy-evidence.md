# ADR 0030: Finance Hard Risk authority and immutable policy evidence

- Status: Accepted
- Date: 2026-08-16

## Context

Finance has deterministic strategies and prospective RESEARCH evidence but no execution authority.
Safety must be established before a paper or broker path exists.

## Decision

The server-side Hard Risk Engine is the mandatory authority boundary below every Finance proposer.
It evaluates centrally validated versioned policy and fails closed when required evidence is missing,
stale, invalid, inconsistent or temporally unsafe. Strategy signal and risk verdict remain separate
immutable facts. Evaluations bind proposal, instrument, strategy/version, parameters, source/feature
revisions, knowledge cutoff, operating mode and exact policy version.

`ALLOW` means only that hypothetical RESEARCH exposure passed policy; it is not an order or
recommendation. Future execution must reject requests without a current matching immutable risk
evaluation, proposal identity, policy/evidence version, permitted mode and execution-specific gates.
Strategies, Brain, UI and clients cannot set or override verdicts or limits.

## Consequences

BB-089 adds no broker, order, PAPER or LIVE path. Historical evaluations are never rewritten.
Stateful halts survive restart and every transition is audited; recovery is explicit. Provider-
derived evidence inherits provider retention restrictions, while sanitized halt audit may remain.
