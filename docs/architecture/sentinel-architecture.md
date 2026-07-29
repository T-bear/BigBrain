# BigBrain Sentinel Architecture

**Status:** Proposed

**Scope:** Architecture only; no implementation is authorized by this document

**Audience:** Product, architecture, security, development, and operations

## Purpose

BigBrain Sentinel is the only component permitted to communicate directly with node-local resources: Docker, Linux kernel interfaces, processes, filesystems, sensors, network state, UPS and GPU devices, and future local adapters such as Home Assistant. BigBrain API, Web, Worker, Brain, Automation Engine, and modules must never bypass Sentinel.

Sentinel is not a general remote shell, plugin host, or second control plane. It is a small node-side policy enforcement point that exposes explicit, typed capabilities and performs local authorization again before every operation.

```text
Users / Automation / AI Brain
              |
       BigBrain Control Plane
   identity, policy, approvals, jobs, audit
              |
     authenticated Sentinel protocol
              |
    +---------+---------+
    | BigBrain Sentinel |  one instance per managed node
    +---------+---------+
              |
  explicit least-privilege adapters
 Docker  Linux  Files  Network  UPS  GPU  Home Assistant
```

## Responsibilities

Sentinel owns:

- Stable node identity and enrollment state.
- Capability discovery based on installed adapters, platform support, configuration, and local policy.
- Collection of read-only inventory and health data.
- Validation and execution of explicitly defined requests.
- Local deny-by-default authorization, even when the Control Plane has authorized a request.
- Resource selectors, path allowlists, quotas, timeouts, concurrency limits, and result-size limits.
- Idempotency and replay protection for mutating operations.
- Append-only security audit records for accepted and rejected requests.
- Redaction of secrets and sensitive local details before data leaves the node.
- Health, lifecycle, upgrade compatibility, and degraded/offline reporting.

Sentinel does not own:

- End-user identity, roles, UI, global workflow, business policy, or module data.
- AI planning, autonomous goals, or natural-language interpretation.
- Cross-node orchestration or cluster scheduling.
- Arbitrary commands, arbitrary scripts, arbitrary Docker API passthrough, or dynamic in-process plugins.
- Long-term primary audit storage; it keeps a bounded local spool and forwards signed records.
- General reverse proxy, VPN, service discovery, or secrets-vault responsibilities.

## Control Plane and Sentinel responsibility matrix

| Concern | Control Plane | Sentinel |
|---|---|---|
| User, module, AI, and automation identity | Authoritative | Consumes only signed, request-bound delegation claims |
| Global roles, policy, approvals, and workflows | Authoritative | Verifies required evidence and may still deny |
| Node-local safety policy | Proposes a signed policy overlay | Authoritative enforcement point |
| Capability catalog | Understands supported schemas and filters them for callers | Authoritative for installed availability and local limits |
| Request orchestration | Selects node, authorizes, creates jobs, handles aggregate outcomes | Executes one node-scoped typed request |
| Cross-node coordination | Authoritative | Prohibited |
| OS/resource access and redaction | Prohibited | Authoritative |
| Global audit history | Durable system of record | Durable local pre-write and bounded forwarding spool |
| Cached inventory/read models | Owns normalized last-known views and freshness | Owns collection provenance at the node |

The Control Plane is one logical security authority even if it is later deployed as multiple replicas. Replicas share the same governed service identity and durable policy/job state; they are not independent authorities. Sentinel never selects a replica, coordinates replicas, or accepts inconsistent issuers. Control Plane high availability requires a separate leader/idempotency design for dispatch, but duplicate delivery must remain safe at Sentinel.

## Trust boundary

The Control Plane and Sentinel are separate trust domains. Neither trusts network location. Both authenticate the other. A valid Control Plane request is necessary but not sufficient: Sentinel also checks protocol version, freshness, signature/token binding, capability, resource selector, local policy, approval evidence, quota, and current node state.

Effective authorization is the intersection of Control Plane policy and Sentinel-local policy. Sentinel has a locally installed immutable safety baseline plus a signed operational policy overlay. A remote overlay may narrow authority immediately. Widening node authority requires an explicitly defined local administrative activation or an equivalently strong enrollment-time trust decision; compromise of the Control Plane must not silently remove local protected-resource rules, emergency stops, or hard safety limits.

Node-local adapters are lower-trust boundaries. Docker access is equivalent to root-like authority. Files, process data, environment variables, logs, device APIs, and Home Assistant responses are treated as untrusted and potentially secret-bearing. Adapter output is normalized, bounded, and redacted before serialization.

Compromise containment:

- Control Plane compromise is limited by Sentinel local policy and capability allowlists.
- Sentinel compromise is assumed to compromise its node, but must not yield Control Plane credentials or other nodes.
- One node certificate and key are unique per Sentinel and independently revocable.
- A compromised node must not be able to mint capabilities, approvals, or identities.

## Deployment

One Sentinel instance runs per managed node as a separately packaged service. For a Linux node, a system service is preferred over placing the full Sentinel in the same container namespace as BigBrain API. Packaging may use a hardened native service or a narrowly configured container, but the security properties are mandatory:

- No public internet exposure.
- A dedicated management interface or private overlay where available.
- A dedicated unprivileged service account for the core process.
- Privilege-separated adapters or narrowly scoped helpers when an operation requires additional rights.
- Read-only root filesystem where practical, bounded writable state, no inherited interactive shell.
- No Control Plane secrets, user credentials, or database credentials on the node.
- Docker socket access, if later introduced, exists only in the Docker adapter boundary.

Single-node deployment still preserves the process and credential boundary. Co-location is not trust equivalence.

## Internal shape

```text
Transport listener
  -> protocol validation
  -> peer authentication
  -> replay/freshness validation
  -> local policy decision
  -> request coordinator
  -> typed adapter
  -> normalization/redaction
  -> response + audit record
```

The adapter set is compiled, signed first-party code. No arbitrary assembly, script, container image, or downloaded extension is loaded into the Sentinel process. If third-party adapters are ever supported, they require a separate isolation and signing design.

## Lifecycle

1. **Manufactured:** installation creates a node identity key in protected local storage.
2. **Unenrolled:** Sentinel exposes only local bootstrap diagnostics; operational requests are denied.
3. **Enrollment pending:** an operator verifies a short-lived one-time enrollment challenge out of band.
4. **Active:** Sentinel has a node certificate, local safety baseline, signed operational policy overlay, trust anchors, and negotiated protocol.
5. **Degraded:** Sentinel remains reachable but one or more adapters or dependencies are unavailable.
6. **Draining:** new mutations are rejected while in-flight work completes before upgrade or shutdown.
7. **Revoked:** certificate and enrollment are invalid; all Control Plane requests are denied.
8. **Retired:** node identity is destroyed and the Control Plane retains historical audit references.

Enrollment, certificate rotation, policy activation, upgrade, rollback, revocation, and retirement are explicit audited operations. Reinstalling a node produces a new identity unless a documented secure recovery process restores it.

## Versioning and compatibility

Three versions are independent:

- `sentinelVersion`: implementation release using Semantic Versioning.
- `protocolVersion`: wire contract major/minor, for example `1.0`.
- Capability contract version: per capability, for example `Docker.ReadContainers@1`.

Handshake negotiates one common protocol version. Major versions may break compatibility; minor versions are additive. Unknown fields are ignored only where the schema marks them extensible. Unknown enum values never authorize behavior. The Control Plane maintains a compatibility matrix and blocks unsafe upgrades.

Capabilities are never silently reinterpreted. Breaking request, result, selector, or safety semantics create a new capability version. Deprecation includes announcement, usage measurement, minimum support window, and explicit removal.

## Logging and audit

Operational logs are structured, local, bounded, and redacted. They may include timestamp, severity, node ID, component, request ID, correlation ID, capability, duration, outcome class, and error code. They must not contain tokens, private keys, raw environment variables, arbitrary file contents, unbounded logs, or full sensitive paths.

Security audit is distinct from diagnostic logging. Audit records are append-only and record:

- Who or what initiated the action.
- Control Plane decision and policy version.
- Sentinel decision and local policy version.
- Capability and normalized resource selector.
- Approval reference where required.
- Request, idempotency, and correlation identifiers.
- Start/end timestamps and outcome.
- A hash of redacted request/result evidence where appropriate.

Sentinel retains a bounded tamper-evident local spool during Control Plane outages. Backpressure never permits unbounded disk growth. High-risk mutations fail closed if their required audit evidence cannot be durably recorded.

## Telemetry

Sentinel exposes its own bounded operational telemetry, not arbitrary host data:

- Availability and last successful heartbeat.
- Request count, latency, timeout, rejection, retry, and saturation metrics.
- Adapter health and collection age.
- Queue depth and local audit-spool utilization.
- Certificate expiry and policy/configuration version.
- Build version and protocol negotiation outcome.

Telemetry uses OpenTelemetry-compatible concepts in a future implementation. Labels must be low-cardinality; request IDs, file paths, container IDs, and user IDs are not metric labels. Traces propagate correlation context but are sampled and redacted.

## Failure handling

Sentinel fails closed for authorization ambiguity, stale credentials, unknown capabilities, invalid selectors, replay suspicion, unavailable audit durability, and high-risk timeouts. Read-only partial collection may return `degraded` with warnings and explicit field availability.

Errors use stable machine-readable codes and safe human-readable messages. Internal stack traces and adapter payloads never cross the boundary. Requests have deadlines; cancellation is best effort and does not imply rollback. Long-running or non-atomic changes eventually use a job model with observable state and compensating actions.

Offline nodes are normal. The Control Plane retains last-known data with collection timestamps and marks it stale; it never presents cached state as current. Reconnection does not automatically replay mutations.

## Availability model

Sentinel is not initially highly available within one node. Running two active Sentinels against the same local resources would create split-brain and duplicate-action risks. One active instance owns a node identity. Future redundancy requires an explicit lease/fencing design and is not implied by stateless protocol transport.

## Architectural invariants

1. All system access crosses Sentinel.
2. Every operation maps to a declared, versioned capability.
3. Both Control Plane and Sentinel authorize every request.
4. No arbitrary shell, Docker API passthrough, or dynamic in-process code.
5. Mutations are deny-by-default, audited, bounded, and idempotent where possible.
6. Node credentials are unique, rotatable, and revocable.
7. Cached observations carry provenance and freshness.
8. AI and automation never receive a privileged bypass.
9. Protocol-control messages cannot read or mutate managed resources; resource access always uses a capability request.
10. Effective permission is the intersection of Control Plane authorization and Sentinel-local policy.
