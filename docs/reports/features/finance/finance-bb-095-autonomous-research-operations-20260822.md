# BB-095 — Finance Autonomous Research Operations & Recovery Hardening v1

> Sanitized repository evidence; no secrets, private addresses, identities or raw logs.

Detta är en sanerad GitHub-version; hemligheter, privata adresser, identiteter och råloggar är uteslutna.

## Metadata

- Date: 2026-08-22
- Scope: unattended Finance RESEARCH operations and recovery
- Related ADR: ADR 0036

## Status

Implemented and locally verified; publication/CI evidence is pending.

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
- BB-095 recovery/operations tests: locally complete.
- Scheduler default: off.
- Authority: `RESEARCH`, `0 SEK`, `NONE`.
- Final code review: pending; CI requires publication.

## Remaining work

After acceptance, Finance may be eligible for an explicit deployment decision enabling continuous unattended RESEARCH. That is not automatic activation and not trading. External notifications and future research methodology remain separate evidence-driven work.

## Resumption

Resume from ADR 0036 and this report. Do not enable scheduling or start PAPER, broker, champion or research-feature expansion without separate authorization.
