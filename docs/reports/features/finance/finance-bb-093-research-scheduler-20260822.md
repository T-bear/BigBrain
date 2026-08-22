# BB-093 — Finance Research Scheduler / Orchestrator v1

> Sanitized repository evidence; no secrets, private addresses, identities or raw logs.

Detta är en sanerad GitHub-version; hemligheter, privata adresser, identiteter och råloggar är uteslutna.

## Metadata

- Date: 2026-08-22
- Scope: BB-093 bounded research scheduling
- Related ADR: ADR 0034

## Status

BB-093 adds the first bounded unattended orchestration layer over BB-092. Implementation is verified and published in `0f952f88e3ec630b0a000e3d7565dd6274f63c09`. It does not add research methodology, acquisition behavior, PAPER, broker, order, portfolio, LIVE/AUTO, champion promotion or risk-policy authority.

## Evidence

Deterministic unit/integration coverage exercises the persisted scheduler against isolated SQLite fixtures without provider access or real-time waiting. Local verification passed 475 API tests, 32 Sentinel tests and 115 frontend tests, including nine scheduler orchestration cases and UI/API contracts. Release solution, frontend production and API container builds, documentation verification, Compose validation, diff whitespace and sanitized secret-pattern scan passed. No deployed runtime was changed. GitHub Actions run 32591633284 passed backend, frontend, documentation and secrets. BB-093 is complete.

## Changes

- Configuration `Finance:ResearchScheduler` is versioned and disabled by default.
- Defaults are a 60-minute check interval, one 02:00 UTC opportunity and two experiments, still capped by the BB-092 hard maximum.
- 02:00 UTC follows the existing 22:00 UTC EOD provider window without invoking that provider itself.
- Opportunity identity is `finance-research-scheduler-v1:<research-date>`; the research date is the prior UTC date and must be a US market session.
- Only the current opportunity is considered after startup. Offline days are not backfilled.
- One durable journal row records opportunity state, timestamps, deferral/failure reason and BB-092 run identity.
- Recovery, data readiness and a busy manual research run defer to the next bounded check. Exact-current-evidence failure is durable and terminal for the opportunity.
- The worker waits for system recovery and contains no SQL or research policy. The orchestrator invokes the existing `RunAutonomousResearch` path.

The status and bounded history endpoints are read-only. Finance UI adds a compact scheduler state inside progressive research details and continues to state `RESEARCH`, `0 SEK` and execution authority `NONE`.

## Security

No mutation endpoint, arbitrary expression, SQL, path, command or remote-code input is introduced. Logs contain opportunity identity/state only. Scheduled work invokes BB-092 and cannot modify Hard Risk, clear a halt, create real portfolio state or acquire execution authority.

## Remaining work

Normal Finance acquisition cadence remains the only provider polling path. Prospective shadow cadence is unchanged and receives no backfill. BB-093 has no resource/load decisions: the next recommended bounded slice is Finance Resource & Safety Governor. Final autonomous operations/recovery hardening remains after that. Finance must not yet be described as approved for continuous unattended research.

## Resumption

Resume from ADR 0034, ADR 0033, `docs/modules/finance.md`, `docs/STATUS.md`, `docs/BACKLOG.md` and this report. Do not begin the Resource Governor, final autonomous operations/recovery, PAPER or broker work without a separately approved slice.
