# ADR 0034: Finance bounded research scheduler

- Status: Accepted for BB-093
- Date: 2026-08-22

## Context

BB-092 provides a bounded, idempotent, globally single-flight and recovery-safe RESEARCH cycle. It does not decide when that cycle should run. The first unattended operationalization must schedule one logical opportunity without introducing trading authority, provider polling, research methodology or a resource governor.

## Decision

`finance-research-scheduler-v1` is disabled by default. When explicitly enabled, a thin hosted worker waits for `SystemRecoveryCoordinator`, wakes every 60 minutes and asks a testable orchestrator whether the current opportunity is due. The default opportunity is 02:00 UTC, four hours after the existing EODHD provider window begins at 22:00 UTC. Its research date is the previous UTC date and must be a real `UsMarketCalendar` session. This lets normal acquisition/cadence remain authoritative and does not create a second provider polling path.

The deterministic opportunity/idempotency identity is `finance-research-scheduler-v1:yyyy-MM-dd`, where the date is the intended completed market session. Only the current opportunity is considered; missed days are not replayed. A SQLite journal stores one row per opportunity with `Pending`, `Skipped`, `Deferred`, `Started`, `Completed` or `Failed`, timestamps, reason and linked BB-092 run. Atomic claim and BB-092's existing idempotency/single-flight contracts provide one logical research effect across duplicate ticks and restarts.

Recovery converts a claimed opportunity with no corresponding research run to `Deferred`. If its deterministic BB-092 run exists, the orchestrator reconciles its completed, failed or running state. Recovery/data-not-ready/research-busy conditions defer until the next configured check. Current-evidence failure and unexpected orchestration failure are terminal for that opportunity, preventing retry storms. Graceful cancellation prevents new work and is never recorded as a false failure.

## Consequences

Status and bounded history are read-only. The scheduler never refreshes providers, creates prospective predictions, changes research scoring, bypasses Hard Risk, or creates portfolio/execution authority. Journal backup includes only rows without a research run or rows whose linked run is already backup-eligible. A future Resource & Safety Governor can be inserted as another prerequisite gate; it is not implemented here. Continuous unattended research is not yet approved.
