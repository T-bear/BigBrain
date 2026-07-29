# ADR 0002: Sentinel is the exclusive boundary for node-local system access

- Status: Proposed
- Date: 2026-07-23
- Supersedes: The Host Agent naming and narrower scope in the architecture baseline
- Extends: ADR 0001, Web API must not directly control the Docker daemon

## Context

BigBrain needs long-term access to Docker, Linux, processes, filesystems, sensors, network state, UPS, GPU, Home Assistant, and other node-local resources. These interfaces have different privilege requirements, unstable platform-specific behavior, and substantial secret and availability risks.

Placing access in BigBrain API, Worker, Brain, Automation Engine, or individual modules would:

- Expand compromise of an internet/LAN-facing process into node compromise.
- Duplicate privilege, policy, redaction, audit, retry, and platform logic.
- Make module boundaries ineffective.
- Let AI or automation accidentally acquire direct system authority.
- Couple the Control Plane release and deployment model to each operating system.
- Make multi-node identity, compatibility, revocation, and offline behavior inconsistent.

The existing architecture proposed a separate Host Agent and ADR 0001 prohibited direct Docker control from Web API. The new Sentinel design generalizes and strengthens that boundary.

## Decision

BigBrain Sentinel is the only BigBrain component permitted to communicate directly with node-local operating-system resources and locally privileged integrations.

BigBrain API, Web, Worker, Brain, Automation Engine, and modules communicate with system resources only through Control Plane application contracts and the versioned Sentinel protocol. They never receive Docker socket access, general shell access, host filesystem mounts, device credentials, or a direct Sentinel bypass.

Sentinel:

- Is deployed separately on each managed node.
- Mutually authenticates with the Control Plane.
- Exposes only declared, typed, versioned capabilities.
- Independently authorizes every request using local deny-by-default policy.
- Uses least-privilege adapters/helpers rather than a generic command channel.
- Redacts output, applies bounds, and creates security audit evidence.
- Supports unique node identity, rotation, revocation, compatibility, and offline state.

Sentinel effective authorization is the intersection of a locally installed safety baseline, a signed operational policy overlay, a request-bound delegation proof, and current node/resource state. Remote policy may narrow permissions immediately but may not silently widen the local safety baseline or remove hard limits, protected resources, or emergency stops.

Handshake, heartbeat, bounded Sentinel self-health, and capability discovery are authenticated protocol-control messages. They cannot access managed resources. Inventory and every other node-resource observation or mutation use a declared capability; composite inventory is modeled as `Inventory.ReadSnapshot@1` with independently authorized sections.

The Control Plane may later have multiple replicas, but it remains one logical authority with governed issuer keys and shared durable dispatch/idempotency state. Every request and proof targets exactly one Sentinel node. Sentinels never coordinate Control Plane replicas or other Sentinels.

There is no arbitrary shell, Docker API passthrough, arbitrary HTTP proxy, dynamic in-process plugin, or generic AI execution capability.

Node-local Home Assistant access goes through Sentinel when it requires node-local network placement or locally held credentials. Remote SaaS/product APIs may remain behind Control Plane integration adapters; Sentinel is not a mandatory proxy for unrelated external traffic.

The transport, PKI implementation, policy engine technology, and packaging mechanism remain separate decisions. No implementation is authorized by this ADR.

## Consequences

### Positive

- Privileged access is isolated from public-facing and high-complexity processes.
- One capability, policy, redaction, audit, and compatibility model covers all nodes.
- A compromised user, module, AI model, or automation cannot directly address the OS.
- Unique node identity and revocation enable future multi-node management.
- Platform-specific collection and execution remain outside the Control Plane.
- Offline/degraded nodes become an explicit normal state.

### Negative

- Sentinel becomes security-critical and requires a hardened lifecycle, supply chain, and incident response.
- Every system feature pays protocol, schema, policy, and compatibility costs.
- Network partitions and mixed versions introduce distributed-systems complexity.
- The Control Plane cannot provide authoritative current system state when Sentinel is offline.
- Privilege-separated adapters are more complex than a single root process.
- Docker authority remains root-equivalent on the node even when isolated.

### Risks

- Sentinel could grow into an over-privileged monolith.
- A thin relay implementation could falsely appear secure while blindly trusting the Control Plane.
- Protocol and capability version drift could block upgrades or cause unsafe semantics.
- Central Control Plane compromise could target many nodes unless local policy remains effective.

Mitigations are mandatory: unprivileged core, narrow compiled adapters/helpers, dual authorization, scoped short-lived delegation, replay protection, durable audit, compatibility tests, signed releases, staged rollout, and no dynamic code.

## Alternatives considered

### Direct access from BigBrain API

Rejected. It places root-equivalent interfaces in the largest remotely reachable process and violates least privilege.

### Direct access from each module

Rejected. It duplicates privileges and platform logic, defeats central policy/audit, and produces inconsistent security.

### Run all node operations in BigBrain Worker

Rejected. Process separation for workload does not create a node trust boundary; Worker would still hold broad host authority and AI/automation adjacency.

### Use SSH and shell commands

Rejected. A general command channel is difficult to constrain, validate, version, audit, and safely expose to automation.

### Expose Docker Engine or other native APIs over TLS

Rejected. Native APIs are too broad and leak provider-specific semantics into the Control Plane. TLS authenticates but does not provide narrow capabilities or local policy.

### Adopt an existing configuration-management system as the primary boundary

Not selected now. Mature systems may later be integrated for provisioning, but they do not automatically satisfy BigBrain's interactive typed capability, UI, AI, approval, freshness, and audit model. Build-versus-integrate must be reassessed before broad mutation scope.

### Sentinel as a generic local proxy

Rejected. Arbitrary HTTP, filesystem, Docker, or shell forwarding would recreate direct access under a different name.

## Follow-up decisions required

1. Enrollment, PKI, certificate rotation, revocation, and disaster recovery.
2. Wire transport, connection direction, discovery, and remote-node topology.
3. Normative JSON Schema repository and compatibility policy.
4. Policy engine and signed local policy distribution.
5. Linux packaging, service identity, and privilege-separated adapter model.
6. Audit integrity, ingestion, retention, and outage behavior.
7. Sentinel release signing, update, rollback, and supply-chain controls.
8. v1 read-only capability and data-classification review.
9. Control Plane replica identity, durable dispatch, and idempotency behavior.

## Acceptance criteria

This ADR may move to Accepted only after architecture and security owners agree that:

- Sentinel replaces Host Agent terminology in the maintained architecture baseline.
- The exclusive-access invariant is enforceable by deployment and architecture tests.
- Accepted ADRs exist for PKI/enrollment, transport/connection direction, policy distribution, and Linux privilege separation.
- Normative schemas and compatibility vectors exist for protocol control, authorization proof, `Inventory.ReadSnapshot@1`, and every v1 capability.
- No implementation begins before the required PKI, protocol, policy, data-classification, and v1 threat-model decisions exist.
