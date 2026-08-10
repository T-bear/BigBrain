# ADR 0019: Broker truth and execution reconciliation

- Status: Proposed
- Date: 2026-08-10

## Context

Transport success, timeout and broker acceptance do not reliably establish an order's
fill state. Blind retry can duplicate financial exposure.

## Decision

The broker is authoritative for orders, fills, positions and cash after execution.
Order intent uses immutable previews and idempotency. Uncertain execution is recorded,
conflicting action is blocked, and reconciliation occurs before retry. Material mismatch
suspends automated trading.

## Consequences

Execution Verification and reconciliation are core components, not observability extras.
Adapters must expose enough stable identity to reconcile while journals and UI avoid
secrets and unnecessary raw broker identifiers.
