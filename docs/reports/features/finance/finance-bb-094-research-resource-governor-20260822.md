# BB-094 Finance Resource & Safety Governor v1

> Sanitized repository evidence; no secrets, private addresses, identities or raw logs.

Detta är en sanerad GitHub-version; hemligheter, privata adresser, identiteter och råloggar är uteslutna.

## Metadata

- Date: 2026-08-22
- Scope: scheduled, optional Finance research only
- Authority: `RESEARCH`, `0 SEK`, execution authority `NONE`
- Related ADR: ADR 0035

## Status

Implemented and locally verified; publication and CI evidence remain pending until the final commit is pushed.

## Changes

BB-093 now calls a versioned resource governor after recovery and coherent-data readiness but before claiming an opportunity or invoking BB-092. The governor reuses the Sentinel-backed `ISystemMetricsProvider` snapshot for CPU, memory and configured-disk evidence. Unknown or failed critical measurements defer fail-closed. Critical disk capacity blocks. Multiple findings remain auditable with `Block > Defer > Allow` precedence.

One compact decision snapshot is stored on the existing scheduler-opportunity row; no periodic telemetry stream is retained. Pressure creates no research run or experiment, while the same deterministic opportunity can proceed after metrics recover. The read-only status endpoint and Finance details expose the operational state without implying trading authority.

Temperature and trustworthy foreground-service activity are not present in the current metrics contract, so v1 marks temperature unsupported and does not guess from Docker names, logs or host commands. The decision is snapshot-based and relies on bounded scheduler retry timing rather than adding hysteresis or adaptive experiment budgets.

## Security

Manual BB-092 research remains explicit and is not silently gated. System recovery, BB-093 readiness, BB-092 evidence selection/single-flight, Research Integrity and Hard Risk remain authoritative in their own domains. No PAPER, broker, order, portfolio, LIVE/AUTO or champion capability was introduced.

## Evidence

Deterministic fake-metric tests cover healthy allow, CPU/memory/disk pressure, critical-disk precedence, multiple reasons, unavailable/stale/throwing providers, configuration bounds, scheduled defer/recovery on the same opportunity, persisted/reopened audit evidence and unchanged `RESEARCH / 0 SEK / NONE` authority. Local verification passed 486 API tests, 32 Sentinel tests and 115 frontend tests; Release solution, frontend production and API image builds, documentation (169 Markdown files / 89 unique BB IDs), Compose, diff and sanitized changed-file secret-pattern checks passed. No deployed runtime was changed. Publication and CI evidence are recorded in `docs/STATUS.md` after verification.

## Remaining work

Continuous unattended research is not yet approved. The next bounded slice should harden autonomous research operations and recovery: reconciliation/checkpoints, repeated-failure handling, health/alerts and maintenance windows. It must not widen trading authority.

## Resumption

Resume from ADR 0035, ADR 0034, `docs/modules/finance.md`, `docs/STATUS.md`, `docs/BACKLOG.md` and this report. Do not begin autonomous operations hardening, PAPER, broker or UX work without a separately approved slice.
