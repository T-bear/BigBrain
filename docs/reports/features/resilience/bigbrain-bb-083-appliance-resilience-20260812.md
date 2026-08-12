# BB-083 appliance resilience baseline

## Metadata

- Date: 2026-08-12
- Baseline: `461b6ac31fa20aa43929005b77ccf99ef589cee1`
- Scope: host lifecycle, recovery, storage and Finance crash safety
- Related commit: assigned on publication

## Status

Implemented, automatically tested and deployed at Compose/runtime level. Host systemd
installation, Docker-daemon restart and controlled reboot are blocked because this session
cannot satisfy interactive sudo. Physical power-cycle is pending owner verification.

## Evidence

Host: Debian 13.6, systemd, Docker enabled/active, NTP synchronized, local ext4 at 43% used.
Short power-key effective default is `poweroff`; long press is outside software. `/mnt/media`
is a pre-existing failed optional mount, so system state is degraded without blocking local
BigBrain storage. Core volumes use UID 1654 and remained writable.

Compose initially had `unless-stopped` but no host unit, Sentinel healthcheck or explicit
grace. Deployed services now have bounded grace, Sentinel Unix-socket readiness, API health
dependency and lifecycle volume. API/Web/Sentinel/FlareSolverr returned healthy.

API PID-1 crash restarted automatically and the next session reported `previousShutdown=unclean`
then `overall=healthy`. Explicit `docker kill` was correctly treated as operator stop and did
not restart under `unless-stopped`; this distinction is documented. Finance remained 16
requests/16 successes/0 retries, 4,016 raw observations, 16 payloads/revisions, deterministic
replay and no missing payload. Read projection retained 2,016 latest observations and three
`INSUFFICIENT_DATA` evaluations.

## Changes

ADR 0026 defines host lifecycle and recovery. A durable SQLite journal records sanitized
session/events; fast checks cover lifecycle, meal planner, shopping list, calendar, settings,
Finance, clock and disk. Explicit policies govern EODHD catch-up, derived builds and media.
Read-only `/api/v1/system/recovery` and Admin UI expose honest state. Scripts install,
rollback and verify without environment dumps.

## Security

No Docker socket enters API/Web, no root application, host shutdown endpoint, secret log,
broker, order, PAPER or LIVE capability was added. Finance remains `RESEARCH`. Finance backup
remains excluded because provider deletion lineage is not backup-aware.

## Test matrix

Repository lifecycle/Finance interruption fixtures passed. Compose recreate, Sentinel/API/Web
readiness and API PID-1 crash passed. Network/EODHD absence is fail-closed by existing bounded
workers. Docker-daemon restart, host reboot, shutdown timing and physical power cycle are not
verified due the root gate. Low-disk is fixture/config threshold behavior; no real disk was
filled. No real database was corrupted.

## Remaining work

Owner/root must install/start the unit, reboot, run the verifier, then perform the physical
short-press power-cycle. A future slice should add approved module-specific online backup and
restore drills; EODHD backup needs provider-aware deletion inventory. Future UPS is optional.

## Resumption

Use the appliance lifecycle runbook. Smallest next step is the single install/start command,
then verifier and controlled reboot. Do not mark reboot or physical gates passed without output.

## Sanitization

Detta är en sanerad GitHub-version. No secret, environment value, private address, raw market
row, private host path or unbounded log is included.
