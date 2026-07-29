# BigBrain Sentinel Evolution

**Status:** Proposed directional roadmap, not a delivery commitment

Each phase is gated by evidence, threat modeling, operational readiness, and accepted ADRs. Version numbers describe capability maturity, not a promise that every feature belongs in one implementation release.

## Bootstrap exception — unprivileged Sentinel Core

ADR 0004 permits a pre-v1 implementation increment before the transport, PKI/enrollment, Policy Engine, and Linux privilege-separation ADRs are accepted.

This bootstrap is limited to project structure, `Program.cs`, standard .NET hosting, Dependency Injection, bounded non-secret configuration, structured local logging, deterministic version information, a bounded process health endpoint, an empty capability registry, and tests.

It includes no Control Plane or external communication, protocol transport, authentication, Docker, Linux system access, privileged functionality, capability execution, adapters, or persistent Sentinel security state. Completing bootstrap does not satisfy or bypass any v1 entry criterion below.

## v1 — Read-only node inventory

Entry criteria:

- ADR 0002 is accepted and the maintained architecture baseline uses Sentinel terminology.
- Transport/connection direction, PKI/enrollment, policy distribution, and Linux privilege model have accepted ADRs.
- Normative JSON Schemas and compatibility test vectors exist for all v1 messages and capabilities.
- The v1 data-classification/redaction matrix and adapter threat models are approved.
- Control Plane workload identity, delegation proof issuance, node lifecycle, and last-known-state ownership are designed.

Scope:

- Enrollment, node identity, certificate rotation, handshake, heartbeat, health.
- Capability discovery and local deny-by-default policy.
- Locally installed safety baseline plus signed, narrowing operational policy overlay.
- `Host.ReadCpu`, `Host.ReadMemory`, `Host.ReadDisk`, and safe temperature/network summaries.
- `Inventory.ReadSnapshot@1` with explicit per-section authorization and partial results.
- Docker container/image inventory only if its adapter boundary is approved.
- Structured logs, bounded telemetry, and tamper-evident audit spool.
- Offline/stale-state semantics and protocol compatibility tests.

Explicit exclusions:

- Mutations, filesystem content, process command lines, logs, shell, arbitrary HTTP.
- Event streaming, cluster coordination, AI execution.
- AI or unattended automation dispatch to Sentinel, including read-only calls.

Exit criteria include an external security review, fault-injection tests, certificate lifecycle drill, node revocation drill, and proven non-disclosure of secrets.

## v2 — Controlled Docker actions

Adds narrowly typed start/stop/restart operations, not Docker API passthrough.

Required foundations:

- Control Plane jobs, durable idempotency, approvals, audit, and outcome reconciliation.
- Container allowlists/protection labels and local cooldowns.
- Unknown-outcome handling and safe retry.
- Explicit recognition that Docker authority can compromise the node.

Delete, exec, privileged container creation, volume destruction, and arbitrary Compose deployment remain separate future decisions.

## v3 — Filesystem capabilities

Adds named storage roots, bounded read, atomic write, and recoverable delete in that order.

Required foundations:

- Path and link race defenses.
- Quotas and content/data classification.
- Backup/restore verification before mutation.
- Malware/archive-bomb considerations.
- Fine-grained user/module/automation policy.

This phase may justify a privilege-separated filesystem helper rather than expanding the core process.

## v4 — Home Assistant and local devices

Adds dedicated adapters for Home Assistant, UPS, GPU, and other local resources.

Rules:

- Each external system has its own typed capability namespace and service identity.
- Read and mutate are separate.
- No arbitrary HTTP forwarding or generic device command capability.
- Device/entity allowlists, availability, rate limits, and privacy classification are explicit.

UPS power actions and Home Assistant safety-relevant services are critical-risk operations.

## v5 — Constrained execution for AI and automation

The name “AI execution” must not mean shell access. This phase adds a catalog of predefined, signed execution templates or isolated workloads with typed inputs, immutable executable identity, resource limits, no ambient credentials, controlled network/filesystem access, and captured results.

AI Brain remains a planner and caller through the Control Plane. It never connects to Sentinel, supplies code directly, or bypasses deterministic policy and approval.

This phase requires sandbox escape analysis, workload provenance, egress control, quotas, cancellation, artifact retention, and incident kill switches.

## v6 — Multiple nodes and cluster-aware control

The Control Plane manages many independently identified Sentinels. It adds:

- Node groups, labels, compatibility inventory, and staged policy rollout.
- Fleet health and bounded fan-out.
- Per-node jobs and aggregate workflows with partial failure.
- Maintenance windows, canaries, and concurrency budgets.
- Explicit ownership and tenancy boundaries.

Sentinels do not elect leaders or coordinate each other directly. Cluster scheduling remains a Control Plane concern. Any active/standby Sentinel design for the same node requires fencing and a separate ADR.

The protocol and identity model are multi-node-safe from v1: every request, proof, audit record, and snapshot is bound to exactly one node. v6 adds fleet orchestration and scale, not a new trust model. Multiple Control Plane replicas remain one logical authority with shared durable dispatch/idempotency state.

## v7 — Remote nodes

Remote nodes add hostile-network assumptions, intermittent connectivity, NAT traversal considerations, certificate recovery, regional latency, bandwidth budgets, and physical compromise.

Preferred connection direction and transport require a dedicated ADR. Outbound node-initiated connectivity may reduce inbound exposure but creates command-channel and queue semantics that must be designed explicitly.

Required foundations:

- Strong enrollment and ownership verification.
- Rapid revocation and quarantine.
- Store-and-forward bounds and stale-command prevention.
- Upgrade channels with signed artifacts and rollback.
- External penetration testing and documented disaster recovery.

## Cross-version gates

Every new capability must pass:

1. Concrete product need and named owner.
2. Threat model and data classification.
3. Typed versioned schema and negative test vectors.
4. Control Plane and local Sentinel policy definitions.
5. Least-privilege adapter design.
6. Rate, timeout, payload, concurrency, and audit bounds.
7. Failure, retry, idempotency, and recovery semantics.
8. Compatibility and deprecation plan.
9. Operator documentation and incident controls.
10. Security and architecture approval.

## Decisions required before v1 implementation

- Wire transport and connection direction.
- Certificate authority implementation.
- Policy language/engine technology.
- Native service versus hardened container packaging.

These may remain technology-neutral during architecture review, but implementation beyond the narrow ADR 0004 bootstrap exception may not begin until their security and operational contracts are decided in accepted ADRs.

## Decisions that may remain deferred beyond v1

- Event-stream transport or message broker.
- Third-party adapter model.
- Sentinel self-update mechanism.
- Multi-tenant commercial operating model.

Deferring the beyond-v1 decisions is intentional. Premature selection would harden assumptions before traffic, threat, topology, and operations requirements are measured.
