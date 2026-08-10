# ADR 0017: Finance policy-governed trading boundary

- Status: Proposed
- Date: 2026-08-10

## Context

BigBrain may eventually trade automatically, but financial authority requires a narrow,
auditable boundary consistent with the modular monolith and Brain rules.

## Decision

Finance is a first-party BigBrain module. Strategies and AI only propose. The hard Risk
Engine and policy authorize or deny, the Trading Controller is the sole execution
capability, a typed Broker Adapter communicates externally, and execution is independently
verified. Brain/UI cannot access broker credentials, storage or adapters directly.

Automatic trading is an eventual state, not the starting state. Deterministic strategies
precede AI discretion. Zero trades are valid, there is no guaranteed-return target, and
profitability is evaluated net of costs.

## Consequences

Finance fits established module/adapters and can start without a new service. Every
future execution path must prove it cannot bypass risk, mode, authorization, audit or
verification. Additional isolation requires measured need and another ADR.
