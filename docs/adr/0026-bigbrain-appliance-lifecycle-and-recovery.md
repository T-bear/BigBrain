# ADR 0026: BigBrain appliance lifecycle and recovery

- Status: Accepted by product-owner sprint authorization 2026-08-12
- Date: 2026-08-12

## Context

Docker restart policies alone do not guarantee host-boot startup, safe stop, clean/unclean
detection or storage-gated writers. Finance has quota-sensitive immutable evidence.

## Decision

The host owns boot/shutdown through repository-managed `bigbrain.service`: require Docker
and local filesystems, start Compose with bounded readiness, stop with bounded grace. API
stays unprivileged and receives no Docker socket.

API owns a durable lifecycle SQLite journal. Every process session begins unclean; only a
successful bounded hosted-service shutdown commits clean last. Startup runs fast writable-
path, SQLite, clock and disk checks before Finance workers are released. Component
degradation need not block unrelated modules.

Finance journals `started` before external acquisition. An unfinished attempt becomes
`interrupted`, publishes no partial evidence and suppresses another symbol request that UTC
day. Derived builders consume only committed source state.

No remote shutdown endpoint is added. Normal input remains systemd-logind short power-key
`poweroff`; forced long hold is outside software control.

## Consequences

Docker and `bigbrain.service` must be enabled. Host installation needs local root and has an
explicit rollback. Future Finance execution must force `RECONCILIATION_REQUIRED` after any
unclean restart before external orders.
