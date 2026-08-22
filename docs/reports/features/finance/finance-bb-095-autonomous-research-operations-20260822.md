# BB-095 — Finance Autonomous Research Operations & Recovery Hardening v1

> Sanitized repository evidence; no secrets, private addresses, identities or raw logs.

Detta är en sanerad GitHub-version; hemligheter, privata adresser, identiteter och råloggar är uteslutna.

## Metadata

- Date: 2026-08-22
- Scope: unattended Finance RESEARCH operations and recovery
- Related ADR: ADR 0036

## Status

Implemented, locally verified and published in `31c11865b430a137b7eb5be77681158d1d6adb1f`. GitHub Actions run 32597319341 completed successfully with backend, frontend, documentation and secrets all green.

## Evidence

Deterministic SQLite fixtures cover the crash boundaries, failure classification/streak recovery, scheduler staleness, persistent data/resource waiting, maintenance pause, idempotent incidents, authority invariants and operations metadata backup/restore. No provider or real clock waiting is used. Local verification passed 495 API, 32 Sentinel and 115 frontend tests; Release solution, frontend production and API image builds, documentation (171 Markdown files / 89 unique BB IDs), Compose, diff and sanitized secret-pattern gates passed.

## Changes

- A derived operations coordinator exposes `Disabled`, `Maintenance`, `Waiting`, `Ready`, `Running`, `Deferred`, `Degraded` and `AttentionRequired`.
- Startup reconciliation repairs Started opportunities from authoritative BB-092 runs without touching immutable experiments.
- One low-write singleton and unique operational incidents retain evidence that cannot be reconstructed reliably.
- Three consecutive operational failures require attention; successful scheduled work resets only the active streak.
- Scheduler staleness after 180 minutes and deferred data/resource waiting after 24 hours become visible.
- Explicit deployment maintenance pause prevents new work and preserves identity/history.
- Read-only status and bounded incident APIs plus a concise Finance UI state were added.
- Operational metadata is included in lawful Finance backup/restore without pulling new provider payloads into scope.

## Security

The operations layer cannot change data, research methodology, resource thresholds, experiment budget, Hard Risk, halt state or execution authority. Scheduler remains disabled by default. No PAPER, broker, order, portfolio, LIVE/AUTO or champion capability exists.

## Operational readiness checklist

- BB-092 bounded research and integrity: complete.
- BB-093 scheduler and coherent readiness: complete.
- BB-094 resource governor: complete.
- BB-095 recovery/operations tests: complete locally and in published CI.
- Scheduler default: off.
- Authority: `RESEARCH`, `0 SEK`, `NONE`.
- Final code review: complete; no unresolved blocker found.
- Published CI: GitHub Actions run 32597319341 passed all four required jobs.

## Remaining work

The complete checklist makes Finance eligible for a separate explicit deployment decision enabling continuous unattended RESEARCH. Scheduler activation remains default-off and was not performed by BB-095. This is not trading. External notifications and future research methodology remain separate evidence-driven work.

## Controlled commissioning — 2026-08-22

The appliance was updated from an older image to repository HEAD and commissioned at 20:55 UTC through the deployment-only `FINANCE__RESEARCHSCHEDULER__ENABLED=true` override. Source defaults remain off. The deployed schedule is one 02:00 UTC opportunity checked every 60 minutes with at most two experiments; maintenance pause is false. Recovery was healthy/clean, Sentinel-backed governor evidence returned `ALLOW`, EODHD remained authorized over the same eight-symbol cadence and Finance remained `RESEARCH / 0 SEK / NONE`.

The first natural opportunity, `finance-research-scheduler-v1:2026-08-21`, safely became `Deferred` because current feature lineage was incomplete. It created zero runs and zero experiments. One controlled API restart retained exactly one opportunity and zero runs/experiments, with no operational incident or attention state. A commissioning review also corrected the aggregate operations readiness label for `featureLineageIncomplete`; the authoritative scheduler gate had already remained fail-closed. Backup policy stayed unchanged: EODHD is restricted/subscription-only and public-domain evidence remains separately eligible. Rollback is the deployment override `FINANCE__RESEARCHSCHEDULER__ENABLED=false` followed by an API recreate; history must not be deleted.

## Resumption

Resume from ADR 0036 and this report. Scheduling is explicitly enabled only on the commissioned appliance; source remains default-off. Do not start PAPER, broker, champion or research-feature expansion without separate authorization.
