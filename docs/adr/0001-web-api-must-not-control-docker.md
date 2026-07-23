# ADR 0001: Web API must not directly control the Docker daemon

- Status: Accepted
- Date: 2026-07-23

## Context

Access to the Docker daemon is effectively privileged host access. Mounting the Docker socket in the BigBrain API would place that privilege in a LAN-facing control-plane process and would conflict with the node-access boundary in `ARCHITECTURE.md`.

Sprint 2 needs stable read-only System and Docker inventory contracts before an approved Sentinel integration exists.

## Decision

The BigBrain Web API must not mount or directly access the Docker socket and must not execute shell commands to collect or control Docker state.

The Docker module depends on `IDockerInventoryProvider`. Sprint 2 registers an unavailable provider that returns a structured availability reason and an empty container list. The System module likewise uses `ISystemMetricsProvider` with an unavailable provider; BigBrain API does not collect host metrics directly. A future Sentinel integration may implement these Control Plane contracts through a narrow, authenticated, versioned and read-only interface after its prerequisite architecture decisions are accepted.

No Docker mutation endpoints are introduced. Start, stop, restart, delete and exec remain out of scope.

## Consequences

- The System and Docker dashboard sections work safely but report unavailable until Sentinel integration is implemented.
- System and Docker module status comes from provider results.
- The API remains unprivileged and cannot affect existing containers.
- Later Sentinel communication requires its prerequisite security decisions before implementation.
