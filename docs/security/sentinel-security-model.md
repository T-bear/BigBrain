# BigBrain Sentinel Security Model

**Status:** Proposed

**Security posture:** Zero trust between Control Plane, network, node, and adapters

## Security objectives

- Prevent unauthorized node access even if an application account or network segment is compromised.
- Contain a compromised Sentinel to one node.
- Prevent a compromised node from impersonating other nodes or the Control Plane.
- Make sensitive actions attributable, bounded, reviewable, and revocable.
- Avoid leaking credentials, file contents, environment variables, logs, paths, or topology.
- Remain safe during partitions, clock drift, retry, upgrade, and partial failure.

## Authentication and certificates

Control Plane and Sentinel use mutual authentication. The recommended target is mTLS with a private BigBrain node CA or a standards-based workload identity system. Transport selection is deferred, but these properties are mandatory:

- Unique private key and certificate per Sentinel node.
- Separate Control Plane service identity.
- SAN-bound stable identities; display names are not identities.
- Short-lived certificates where operationally practical.
- Automated rotation with overlapping validity and explicit revocation.
- Private keys generated locally and non-exportable where TPM or OS keystore support exists.
- No shared fleet token or certificate.
- Certificate chain, purpose, node binding, revocation state, and validity checked on every connection.

Bootstrap uses a single-use, short-lived enrollment token containing no long-term authority. Enrollment requires operator-visible node identity verification and records who approved it. A stolen enrollment token alone must not silently replace an existing node.

If the Control Plane later has multiple replicas, they operate as one logical issuer set governed by a single trust policy and durable signing-key lifecycle. Sentinel trusts explicit issuer keys and audiences, not arbitrary replicas or network addresses. Key rotation supports overlap without accepting rollback to a revoked issuer.

## Token model

mTLS authenticates workloads. Each operation additionally carries a short-lived, audience-restricted, signed delegation token or equivalent proof issued by the Control Plane. The token contains only:

- Issuer and Sentinel-specific audience.
- Unique token ID.
- Issued-at, not-before, and expiry.
- Node ID.
- Capability name/version.
- Normalized resource selector or selector hash.
- Principal and initiator type.
- Policy decision ID.
- Request/arguments hash.
- Approval ID when required.

Tokens are single-purpose and normally valid for seconds, not hours. They are bound to the mTLS client identity and request hash. Refresh tokens and end-user bearer tokens are never sent to Sentinel. Sentinel does not accept a role claim as sufficient authority.

The protocol carries this authority as `authorizationProof`, a typed container for a signed token or equivalent proof. The proof is never logged or echoed. Claims inside the JSON request (`principal`, `delegation`, `approval`) are descriptive and must exactly match signed claims; they have no independent authority. A retry with a new message ID requires a newly issued proof while retaining the same correlation and, where permitted, idempotency key.

## Replay protection

Sentinel enforces:

- TLS channel protection and token-to-client-certificate binding.
- Short validity windows and maximum clock skew.
- Unique message ID and token ID replay caches.
- Nonces during handshake/enrollment.
- Request body hash and audience/node binding.
- Idempotency keys for eligible mutations.
- Monotonic per-connection/session sequencing where the chosen transport supports it.

Replayed or expired messages are rejected and audited. Clock synchronization is monitored; excessive skew fails closed for mutations. Replay-cache capacity is bounded and sized beyond the maximum token lifetime and request rate.

## Authorization and least privilege

Authorization occurs independently in the Control Plane and Sentinel. Both are deny-by-default. Sentinel runs with a dedicated low-privilege identity; adapters receive only the OS permissions they require.

Sentinel evaluates the intersection of:

- A locally installed safety baseline that remote callers cannot silently widen.
- A signed, freshness-bounded operational policy overlay from the Control Plane.
- The request-bound delegation proof.
- Current node state, adapter availability, resource constraints, and hard safety limits.

Remote emergency deny takes effect immediately. Remote widening is staged and requires local activation or a previously approved local policy rule that explicitly permits that class of widening. Policy revisions are signed, monotonically ordered, rollback-protected, auditable, and fail closed after expiry according to capability risk.

Preferred privilege structure:

- Core protocol/policy process: no Docker socket, shell, or broad filesystem access.
- Docker adapter/helper: Docker access only, no Control Plane credential.
- Host metrics reader: read-only access to documented kernel/device interfaces.
- Filesystem adapter: access only to named roots with OS ACL enforcement.
- Power helper: fixed power operation only, isolated from general execution.

If Linux privilege separation cannot practically constrain Docker socket authority, the residual risk is explicit: compromise of that adapter can compromise the node. Additional process/container isolation reduces attack surface but does not make Docker socket access safe.

## Approval model

High- and critical-risk actions use just-in-time approval:

- Bound to principal, node, capability version, resource, arguments hash, and plan revision.
- Short expiry and single use.
- Captures approver identity and authentication strength.
- Cannot be approved by the same automation or AI that proposed it.
- Critical actions may require two-person approval or a recent MFA assertion.

Approval means authorization to attempt an exact action, not assurance of outcome.

## Rate limiting and abuse controls

Limits exist per connection, Control Plane identity, node, principal, capability, resource, and risk class. Controls include:

- Request rate and burst.
- Concurrent requests.
- Maximum queued work.
- Execution time and result size.
- Log bytes, file bytes, and inventory cardinality.
- Mutation cooldown and daily risk budget.
- Circuit breakers for failing dependencies.

Rate-limit state is bounded. Critical safety limits cannot be overridden remotely without a separately audited local configuration change.

## Secrets

- Secrets never appear in capabilities, discovery, logs, metrics, errors, inventory, or audit payloads.
- Sentinel stores only node identity material and adapter credentials strictly required on that node.
- Adapter secrets use OS keystore/TPM or root-owned files with restrictive permissions until a dedicated secret store is justified.
- Secret values are injected at the narrowest adapter boundary and never returned to the Control Plane.
- Redaction is allowlist-based output construction, not regex cleanup after serialization.
- Docker environment variables, registry auth, process environments, Home Assistant tokens, UPS credentials, and private filesystem content are secret by default.
- Memory dumps, crash dumps, diagnostics bundles, and support exports require explicit secure handling.

## Audit security

Audit records are structured separately from logs, contain redacted normalized evidence, and are chained or signed to expose tampering. Sentinel uses a bounded local spool. The Control Plane acknowledges ingestion, preserves original node sequence, and detects gaps or duplicates.

High-risk actions require durable local audit write before execution. Audit access is a separate permission. Retention, encryption, legal/privacy requirements, backup, and deletion policy are documented before production.

## Threat model

| Threat | Impact | Primary countermeasures |
|---|---|---|
| Compromised browser/user account | Unauthorized operations | OIDC/MFA, server-side policy, resource scope, fresh approvals |
| Compromised BigBrain API | Fleet-wide command attempts | Sentinel local policy, short scoped tokens, node isolation, approval binding |
| Compromised policy/signing service | Valid-looking fleet-wide authorization | Locally bounded policy ceiling, separate keys, short proof lifetime, revocation and emergency deny |
| Compromised Sentinel/node | Host takeover and false telemetry | Unique node keys, no fleet secrets, revocation, signed audit, reconciliation |
| Malicious/compromised AI model | Tool abuse or data exfiltration | Filtered capabilities, schema validation, no credentials, budgets, approval |
| Automation loop | Repeated disruptive actions | Cooldowns, idempotency, rate/risk budgets, circuit breakers |
| Network attacker/MITM | Credential theft or command modification | mTLS, private trust anchors, request binding, replay protection |
| Replay/delayed command | Duplicate/stale mutation | Expiry, token/message IDs, replay cache, idempotency, status query |
| Docker API abuse | Root-equivalent node compromise | Sentinel-only adapter, fixed operations, allowlists, isolation, audit |
| Path traversal/symlink race | Unauthorized file access | Named roots, safe resolution, directory handles, no absolute paths |
| Log/process data leakage | Secret disclosure | Field allowlists, redaction, byte/time bounds, elevated capability risk |
| Supply-chain compromise | Sentinel or adapter backdoor | Signed builds, SBOM, pinned dependencies, provenance, staged rollout |
| Rogue node enrollment | Impersonation and false inventory | One-time enrollment, operator verification, unique keys, inventory approval |
| Audit deletion/tampering | Loss of accountability | Durable local pre-write, chaining/signing, remote acknowledgement |
| Resource exhaustion | Sentinel/node outage | Bounded parsers, queues, payloads, timeouts, quotas, backpressure |
| Downgrade attack | Weaker protocol/policy | Minimum versions, signed compatibility policy, no silent fallback |
| Control Plane replica race | Duplicate or inconsistent dispatch | Durable job/idempotency state, one logical issuer policy, Sentinel replay/idempotency enforcement |

## Filesystem-specific controls

Filesystem access is especially difficult to secure. Before it is enabled:

- Only named roots configured locally.
- Relative normalized paths only.
- Symlink, hard-link, mount-boundary, and time-of-check/time-of-use defenses.
- File type, extension, size, count, depth, and total-byte quotas.
- No device files, sockets, procfs, sysfs, secrets roots, or Sentinel state.
- Writes use temporary file plus atomic replacement where supported.
- Deletes are recoverable by default and critical paths are immutable.
- Content scanning and archive expansion limits where relevant.

## Network and Home Assistant controls

`Host.ReadNetwork` exposes normalized state, not packet capture or arbitrary sockets. Sentinel is not a network proxy. Future Home Assistant access uses a dedicated adapter, explicit service account, entity allowlists, typed service calls, and separate capabilities for read and change. It must not become arbitrary HTTP forwarding.

## Incident response

Operators can revoke a node, disable a capability fleet-wide or per node, rotate trust anchors, enter local emergency-stop/read-only mode, quarantine a node, and export redacted evidence. Recovery procedures cover compromised Control Plane, compromised node, lost CA, expired certificates, audit gaps, and unsafe rollout.

## Required security validation before implementation

- Formal data-flow and privilege-boundary review.
- STRIDE-style threat model per adapter.
- Protocol schema fuzzing and parser limits.
- Authorization matrix and negative tests.
- Replay, expiry, clock-skew, and idempotency tests.
- Path traversal/symlink-race tests before filesystem capability.
- Supply-chain scanning, signed artifacts, SBOM, and provenance.
- External penetration test before remote nodes or mutations.
