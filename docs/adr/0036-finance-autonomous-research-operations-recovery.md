# ADR 0036: Finance autonomous research operations and recovery

- Status: Accepted for BB-095
- Date: 2026-08-22

## Context

BB-092 through BB-094 provide bounded research, scheduling/readiness and resource gating. Continuous unattended RESEARCH additionally needs one truthful operational view, deterministic reconciliation and visible repeated failures without duplicating those subsystem state machines.

## Decision

`finance-research-operations-v1` derives lifecycle state from cadence, scheduler opportunities, governor audit, research runs and System Recovery. States are `Disabled`, `Maintenance`, `Waiting`, `Ready`, `Running`, `Deferred`, `Degraded` and `AttentionRequired`. They describe operation, never scientific quality.

Persisted additions are deliberately small: a singleton stores the last scheduler evaluation, last successful scheduled research, last operational failure and current consecutive operational-failure streak; a unique opportunity-keyed incident table retains true operational failures. Scheduler evaluations update the singleton at the existing bounded cadence. No high-frequency heartbeat or duplicated pipeline snapshot is stored.

Startup reconciliation runs after System Recovery and is idempotent. A claimed opportunity without a run becomes retryable `Deferred`; a recovered failed run makes its `Started` opportunity `Failed`; a completed run makes its opportunity `Completed`. Existing experiments are never deleted or rewritten. Repeating reconciliation cannot add incidents or evidence because opportunity and incident identities are stable.

Only unexpected orchestration/database/recovered-interruption failures increment the streak. Readiness/resource/busy deferrals and Rejected/Inconclusive/NotEvaluable scientific results do not. Three consecutive operational failures require attention; a later completed scheduled cycle resets the active streak while incidents remain. Enabled scheduler evaluation older than 180 minutes is degraded. One deferred opportunity older than 24 hours surfaces persistent data/resource waiting. Disabled and maintenance-paused states are not failures.

`MaintenancePaused` is explicit deployment configuration, false by default. It prevents new scheduled research but keeps the same opportunity and creates no catch-up storm. Every later attempt evaluates fresh governor metrics; persisted ALLOW is historical audit only. Graceful cancellation retains BB-093 shutdown deferral semantics and creates no operational incident.

## Crash matrix

- Opportunity created or ALLOW persisted before claim: remains Pending/Deferred; next attempt re-evaluates fresh metrics.
- Claimed without run: startup converts it to Deferred with the same identity.
- Run created before experiments, or partially populated: BB-092 recovers it Failed and preserves every linked experiment; operations reconciles scheduler state and records one incident.
- Experiments complete but run marker missing: same recovered Failed semantics; no experiment duplication.
- Run complete but scheduler still Started: scheduler becomes Completed with the original run identity and success resets the active failure streak.

## Consequences

Read-only status and bounded incident APIs expose operational truth. Compact operational metadata participates in BB-085 backup/restore without changing market-data rights. Existing SQLite WAL, transactions and System Recovery `quick_check(1)` remain the corruption/storage foundation; BB-095 adds no per-tick integrity check or manual WAL checkpoint. Scheduler remains disabled by default and Finance remains `RESEARCH / 0 SEK / NONE`.
