# ADR 0004: Sentinel Bootstrap Exception

- Status: Accepted
- Date: 2026-07-23
- Applies to: Sentinel pre-v1 bootstrap only
- Clarifies: ADR 0003, Architecture Freeze – Sentinel v1

## Context

The frozen Sentinel v1 architecture requires accepted decisions for transport, PKI and enrollment, policy distribution and enforcement, and Linux privilege separation before implementation of those security-sensitive areas begins.

This created an unintended blanket prohibition against establishing the unprivileged Sentinel process and project foundation. That foundation does not require, exercise, or pre-empt the deferred security decisions when it contains no node-resource access, protocol transport, authentication, authorization, or capability execution.

Without an explicit exception, beginning even a non-privileged project skeleton would conflict with the v1 entry criteria in `docs/architecture/sentinel-evolution.md`.

## Decision

An unprivileged Sentinel Core bootstrap may be implemented before the following ADRs are finalized:

- Transport and connection direction.
- PKI and enrollment.
- Policy Engine and policy distribution.
- Linux privilege separation.

This is a narrow sequencing exception, not a change to the frozen Sentinel v1 architecture or its security invariants.

The bootstrap phase may contain only:

- Project and solution structure.
- `Program.cs`.
- Generic HostBuilder or equivalent standard .NET hosting bootstrap.
- Dependency Injection registration.
- A bounded, validated configuration model containing no secrets or remote credentials.
- Structured local application logging using framework facilities.
- Build and version information.
- A local health endpoint exposing only bounded Sentinel self-health.
- An empty, immutable-by-consumer capability registry abstraction and implementation.
- Tests for the preceding bootstrap behavior and architectural absence rules.

The bootstrap phase must not contain:

- Network communication with BigBrain Control Plane or any external/local service.
- Sentinel protocol transport, listeners intended for remote management, clients, handshake, heartbeat, or event streaming.
- Authentication, certificates, enrollment, delegation tokens, authorization proofs, approvals, or policy evaluation.
- Docker dependencies, Docker socket access, Docker API calls, or container inventory.
- Linux host metrics, `/proc`, `/sys`, device, process, network, filesystem, shell, or other system-resource access.
- Privileged mode, Linux capabilities, elevated helpers, host mounts, or privilege-separated adapters.
- Capability execution, adapters, commands, handlers, dispatch, or dynamic plugins.
- Persistent job, audit-spool, identity, secret, or node-enrollment state.

The health endpoint is bootstrap process self-health only. It is not the future Sentinel protocol health message and must not expose host identity, host metrics, installed software, environment variables, paths, credentials, network topology, or capability data beyond an empty registry count.

The empty capability registry establishes only the DI and read-only discovery shape needed by the process. It does not define normative capability schemas, advertise executable authority, or authorize adding placeholder capabilities.

## Guardrails

- The bootstrap process runs as an ordinary unprivileged process in development and tests.
- Dependencies are limited to the .NET hosting/web framework and test infrastructure already justified by the repository.
- Configuration fails fast for invalid bootstrap-owned values and never logs raw configuration.
- Logging and errors remain bounded and do not include secret-bearing payloads.
- Architecture tests or equivalent repository checks verify the absence of forbidden dependencies and routes where practical.
- No deferred design is guessed, stubbed, mocked as if authoritative, or hidden behind a premature abstraction.
- A later sprint may extend the bootstrap only after the relevant accepted ADR authorizes that responsibility.

## Consequences

### Positive

- Sentinel project conventions, hosting, DI, configuration, health, versioning, and tests can be validated independently.
- Security-sensitive architecture decisions remain open and are not pre-empted by implementation.
- The first implementation increment has no node authority and minimal attack surface.
- Subsequent work has a tested host process without creating placeholder protocol or adapter debt.

### Negative

- The bootstrap is intentionally not a useful Sentinel agent and communicates with nothing.
- Some bootstrap abstractions may need compatible refinement after normative schemas and transport decisions exist.
- Strict scope enforcement is required to prevent “temporary” security-sensitive stubs from entering the core.

## Exit criteria

The bootstrap exception is complete when:

- The project builds in Release configuration.
- Bootstrap tests pass.
- The health endpoint reports only bounded process self-health.
- Version information is deterministic and testable.
- The capability registry is empty.
- Repository inspection confirms no forbidden system, Docker, authentication, protocol, or privileged functionality.

Completion of this exception does not satisfy the Sentinel v1 entry criteria for transport, enrollment, policy, adapters, resource inventory, or Control Plane communication.
