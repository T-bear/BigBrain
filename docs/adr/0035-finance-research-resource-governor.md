# ADR 0035: Finance research resource governor

- Status: Accepted for BB-094
- Date: 2026-08-22

## Context

BB-093 can schedule optional Finance research after recovery and coherent-data readiness, but it previously had no operational resource gate. Scheduled research must yield to normal BigBrain workloads without coupling system capacity to financial risk or research quality.

## Decision

`finance-research-resource-governor-v1` is an async, read-only prerequisite between BB-093 readiness and opportunity claim. It consumes the existing `ISystemMetricsProvider` snapshot; it does not shell out, poll containers, collect telemetry history, inspect research results, or change the fixed BB-092 budget. Manual BB-092 runs remain intentionally outside this unattended-work gate.

The snapshot-based v1 evaluates CPU utilization, memory utilization and available bytes, and the minimum available bytes across configured disks. Defaults are CPU defer at 80%, memory defer at 85%, minimum free memory 1,024 MiB, disk defer below 10 GiB and block below 2 GiB. Metrics older than five minutes, unavailable provider state, exceptions, missing CPU/memory/disk evidence, or future-dated snapshots defer fail-closed. Temperature and service-activity evidence are not trustworthy in the current metrics contract and are explicitly reported unsupported rather than fabricated.

Decisions are `Allow`, `Defer`, or `Block`, with deterministic precedence `Block > Defer > Allow`. All applicable sorted reason codes and a compact sanitized snapshot are persisted on the one-per-opportunity scheduler journal row. Both `Defer` and `Block` keep the scheduler opportunity recoverable with bounded next eligibility; `Block` records the critical governor decision while avoiding a retry storm. No BB-092 run is created before `Allow`.

The scheduler remains disabled by default. Governor status is exposed read-only at `GET /api/v1/modules/finance/research/governor/status`. Moderate resource deferral does not make global system health unhealthy. Hard Risk Engine, Research Integrity and system recovery retain their separate authorities.

## Consequences

Threshold crossing is snapshot-based; the existing bounded scheduler retry interval provides damping, so v1 adds no telemetry subsystem or hysteresis state. A future operations/recovery slice may add long-running reconciliation, alerts and maintenance-window policy. Adaptive budgets, temperature acquisition and service-activity heuristics are outside BB-094.
