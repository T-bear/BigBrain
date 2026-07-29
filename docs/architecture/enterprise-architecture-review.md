# BigBrain Enterprise Architecture Review

**Status:** Critical review of the current baseline and proposed Sentinel direction

**Perspective:** Multi-year operation, many modules, many Sentinel nodes

## Executive assessment

The modular-monolith starting point is sound for current scale, and separating privileged node access is essential. The baseline is not yet an enterprise architecture: identity, persistence, durable jobs, policy, audit, protocol governance, deployment security, observability, recovery, and multi-node semantics remain mostly directional.

The largest risk is not performance. It is concentrating too many responsibilities and too much authority in a future Control Plane while leaving contracts and policy ownership ambiguous. Sentinel reduces blast radius only if it independently enforces local policy; a thin remote-command proxy would merely relocate root access.

## Final revision disposition

The final cross-document review resolved four design inconsistencies:

1. Capability requests now carry the request-bound authorization proof required by the security model; descriptive principal/approval fields are explicitly non-authoritative.
2. Composite inventory is now `Inventory.ReadSnapshot@1`, so inventory no longer bypasses the rule that managed-resource access requires a capability.
3. Sentinel policy now has a locally installed safety baseline and a signed operational overlay; effective permission is their restrictive intersection, and remote widening cannot silently remove local protections.
4. Multiple Control Plane replicas are defined as one logical authority with shared durable dispatch/idempotency state, while every Sentinel request remains bound to exactly one node.

The responsibility matrix now distinguishes global identity/workflow/audit ownership in the Control Plane from node-local execution, redaction, safety policy, and audit pre-write in Sentinel. AI and Automation remain Control Plane principals and never become protocol peers.

## Strengths worth retaining

- Modular monolith avoids premature distributed complexity.
- Explicit module boundaries and adapters reduce product coupling.
- No Docker socket or arbitrary code in API/Web.
- AI Brain is constrained to structured authorized tools.
- PostgreSQL is planned without prematurely adding brokers or caches.
- First-party compiled UI avoids an early untrusted plugin runtime.
- ADR discipline and incremental delivery are appropriate.

## Findings and recommendations

### 1. The Control Plane risks becoming a “distributed monolith”

Current scope includes identity boundary, modules, dashboard, jobs, integrations, audit, notifications, automation, AI coordination, and node control. Even in one process, these need explicit internal application boundaries, ownership, and dependency rules. Otherwise every module will depend on shared services and changes will become coordinated releases.

Recommendation:

- Define a dependency map and enforce it with architecture tests.
- Separate contracts from implementations only where a real boundary exists.
- Establish bounded contexts for Identity/Policy, Node Management, Jobs, Audit, Modules, Automation, and AI Orchestration.
- Keep deployment monolithic until measured operational or team boundaries justify extraction.

### 2. Sentinel naming supersedes Host Agent but the baseline is inconsistent

`ARCHITECTURE.md` uses Host Agent and permits the API to call external product adapters directly. The new requirement says Sentinel is the only component allowed to access future Home Assistant and all local resources. “Local resource” versus “remote product API” must be defined: a cloud service should use a Control Plane integration adapter, while a node-local Home Assistant instance should use Sentinel only when node-local trust/credentials or network locality justify it.

Recommendation:

- Accept the Sentinel ADR, then update the architecture baseline terminology in a separate controlled change.
- Publish a decision matrix for Control Plane adapter versus Sentinel adapter.
- Avoid routing every external SaaS API through Sentinel; that would create an unnecessary data plane bottleneck.

### 3. Authorization is under-specified

RBAC alone will not safely express node, container, file root, risk, time, approval, automation, and AI constraints. Capability discovery is not authorization.

Recommendation:

- Adopt resource- and condition-based policy alongside roles.
- Define policy ownership, evaluation points, versioning, testing, rollout, rollback, and emergency deny.
- Record deterministic policy decisions with explainable reason codes.
- Avoid inventing a custom policy language until requirements are proven; evaluate mature engines later.

### 4. Audit can become a bottleneck and a false assurance

A single synchronous audit table will bottleneck mutations; asynchronous best-effort logs can lose the evidence that matters. Audit also contains sensitive metadata.

Recommendation:

- Define security audit separately from diagnostics and domain events.
- Use append-only records, per-node sequencing, integrity evidence, bounded local spool, retention, partitioning, and restricted access.
- Decide which actions fail closed when durable audit is unavailable.
- Test restoration and evidentiary continuity, not only backup creation.

### 5. Durable jobs are a prerequisite, not an implementation detail

Retries, timeout, unknown outcomes, approvals, and multi-node partial failure cannot safely live only in HTTP request lifetimes or in-memory workers.

Recommendation:

- Design a durable job state machine before enabling mutations.
- Persist idempotency keys, attempts, deadlines, policy/approval evidence, and observed outcomes.
- Use an outbox pattern if database state and emitted work/events must be atomic.
- Do not introduce a broker until worker separation or throughput proves the need.

### 6. Polling will become a scalability bottleneck

Five-second UI/API polling is acceptable for one node. Multiplied by users, widgets, modules, metrics, and nodes, it creates duplicate collection and fan-out.

Recommendation:

- Cache normalized snapshots per node with freshness and provenance.
- Decouple Sentinel collection cadence from UI refresh.
- Add event-driven invalidation or streaming only after sequence/reconciliation semantics exist.
- Establish cardinality and retention budgets before collecting process/container metrics.

### 7. Multi-node consistency is not defined

Fleet operations produce partial success, stale capabilities, policy-version skew, disconnected nodes, and mixed Sentinel versions. A global transaction is unrealistic.

Recommendation:

- Model each node action independently under a parent workflow.
- Use compatibility gates, staged rollout, canaries, maintenance windows, and bounded fan-out.
- Expose partial outcomes rather than claiming atomic fleet success.
- Define desired state only where reconciliation ownership is explicit.

### 8. Sentinel can become an over-privileged monolith

Making Sentinel the only system boundary is correct, but putting Docker, files, power, network, GPU, Home Assistant, and AI execution in one privileged process recreates the API problem on the node.

Recommendation:

- Keep the protocol/policy core unprivileged.
- Use typed, compiled adapters and privilege-separated helpers for high-risk domains.
- Do not load dynamic plugins.
- Apply OS sandboxing, ACLs, seccomp/capabilities where applicable, and separate credentials.
- Treat Docker adapter compromise as node compromise in the threat model.

### 9. Protocol governance and schema compatibility need ownership

JSON examples alone will drift between implementations.

Recommendation:

- Make immutable JSON Schemas and test vectors normative.
- Assign a protocol owner and compatibility support window.
- Test N/N-1 combinations and downgrade refusal.
- Track capability versions independently from transport protocol.
- Generate documentation/models only after schemas stabilize; avoid a shared binary DTO package that couples releases.

### 10. Identity and certificate operations are a major dependency

OIDC solves user authentication, not workload identity, node enrollment, revocation, or certificate recovery. Running a private CA poorly is high risk.

Recommendation:

- Produce a PKI/enrollment ADR before Sentinel implementation.
- Decide trust-anchor storage, renewal, revocation, disaster recovery, time synchronization, and ownership transfer.
- Prefer established libraries/protocols; do not create custom cryptography.
- Test lost-node, cloned-disk, expired-cert, and compromised-CA scenarios.

### 11. Data ownership is stated but not operationalized

PostgreSQL schema-per-module may still allow cross-module coupling via joins, migrations, and shared ORM contexts. JSONB can become an undocumented integration bus.

Recommendation:

- One migration ownership path per module.
- No direct cross-module writes and reviewed cross-module reads.
- Public application contracts or materialized read models for dashboards.
- Database roles and architecture tests when persistence arrives.
- Retention and classification per data set, especially audit and telemetry.

### 12. Availability targets and recovery objectives are absent

“Healthy” is not an SLO. Without RPO/RTO, backup, failover, upgrade, and capacity decisions cannot be evaluated.

Recommendation:

- Define service-level indicators for API availability, Sentinel reachability, inventory freshness, job completion, and audit ingestion.
- Define RPO/RTO for configuration, audit, jobs, and module data.
- Run restore, certificate rotation, node revocation, and rollback drills.
- Document single points of failure explicitly before adding HA machinery.

### 13. Supply-chain and update strategy is incomplete

Sentinel will be highly trusted and broadly deployed. A compromised update is fleet compromise.

Recommendation:

- Reproducible or attestable builds, signed artifacts, SBOM, provenance, pinned dependencies, scanning, and staged rollout.
- Separate release channels and emergency rollback.
- Sentinel verifies update authenticity independently.
- Do not let the Control Plane supply arbitrary executable bytes under an “update” capability.

### 14. Secrets ownership is unclear

Docker registry, Home Assistant, UPS, external APIs, and future modules will introduce credentials. Duplicating them in Control Plane, Sentinel, environment variables, and backups will create leakage.

Recommendation:

- Define whether each secret is control-plane or node-local.
- Store node-local adapter secrets only on the relevant Sentinel node.
- Use envelope encryption/established secret storage when persistence arrives.
- Include rotation, access audit, backup exclusion/recovery, and redacted diagnostics.

### 15. AI increases confused-deputy and data-exfiltration risk

Structured tools help but do not solve prompt injection, malicious file/log content, tool loops, or excessive contextual disclosure.

Recommendation:

- Treat all model output and retrieved content as untrusted.
- Filter tool catalogs by user, node, resource, task, and risk.
- Keep deterministic authorization outside the model.
- Require exact-argument approval, budgets, provenance, and audit.
- Never introduce generic AI execution; use reviewed templates and sandboxing.

### 16. Automation needs a first-class safety model

Automation that inherits its creator's rights indefinitely becomes a latent privileged account. Events can create loops or storms.

Recommendation:

- Versioned automation principals with owners, expiry, explicit capabilities, resource scope, quotas, cooldown, and kill switch.
- Re-authorize every run against current policy.
- Detect causation loops and cap fan-out.
- Separate recommendation, approval, and execution stages for risky actions.

### 17. Module ecosystem governance is future technical debt

Module manifests, capabilities, migrations, UI widgets, events, and compatibility will evolve together. A premature SDK freezes weak contracts; an informal system creates uncontrolled coupling.

Recommendation:

- Stabilize two or three first-party reference modules before external SDK.
- Establish lifecycle, ownership, compatibility, deprecation, signing, and review.
- Keep third-party code out of API/Web/Sentinel processes.
- Prefer declarative extensions and isolated services.

## Potential future separations

Separate deployment is justified only by security, scaling, availability, release cadence, or ownership evidence:

| Candidate | Separation trigger |
|---|---|
| `BigBrain.Worker` | Long jobs affect API latency/restarts or need independent scaling |
| Audit ingestion/store | Write volume, retention, integrity, or access controls diverge |
| Notification delivery | External provider failures/backpressure affect core workflows |
| AI inference/execution | GPU/CPU load, sandbox, credentials, or release cadence differ |
| Automation scheduler | Durable high-volume schedules and leadership become necessary |
| Integration adapter | Untrusted dependency, special network placement, or independent owner |
| Read-model/query service | Dashboard fan-out materially burdens transactional paths |

Identity/policy should remain conceptually separate immediately, but physical extraction is not automatically beneficial. Sentinel is already a required physical security boundary.

## Likely future bottlenecks

1. Repeated node polling and dashboard fan-out.
2. Unpartitioned audit and telemetry tables.
3. Single in-process background queue.
4. Cross-module synchronous calls.
5. Large logs/files passing through API memory.
6. Fleet-wide operations without bounded concurrency.
7. Policy evaluation without caching/version discipline.
8. High-cardinality telemetry.
9. Central certificate issuance/rotation operations.
10. Database connection and migration contention as modules multiply.

## Priority recommendations

### Before Sentinel implementation

1. Accept/revise the Sentinel boundary ADR.
2. Choose enrollment/workload identity and PKI lifecycle.
3. Make capability schemas and policy decision flow normative.
4. Threat-model v1 read-only adapters and define data redaction.
5. Define protocol compatibility, error taxonomy, and test vectors.
6. Accept ADRs for transport/connection direction, policy distribution, and Linux privilege separation.

### Before any mutation

1. Authentication, resource policy, approval, audit, and durable jobs.
2. Idempotency and unknown-outcome reconciliation.
3. Local Sentinel policy and emergency deny.
4. Backup/restore for affected resources where relevant.
5. Security testing and operational rollback.

### Before many nodes

1. Node lifecycle, ownership, grouping, and compatibility inventory.
2. Bounded fan-out and partial-result workflows.
3. SLO/RPO/RTO, capacity model, and stale-state semantics.
4. Certificate automation, revocation, quarantine, and staged upgrades.
5. Telemetry cardinality/retention and audit partitioning.

## Decisions worth reconsidering

- **Direct Control Plane integrations:** Keep for remote SaaS/product APIs, but route node-local privileged integrations through Sentinel. Clarify the distinction.
- **REST everywhere:** Appropriate for public API; Sentinel transport should remain undecided until bidirectional events, NAT, streaming, and certificate operations are understood.
- **PostgreSQL as all persistence:** Good default, but large telemetry/log/file payloads need separate storage and lifecycle.
- **Single compiled UI:** Good initially; a declarative extension boundary may be needed before third-party modules.
- **One Sentinel process:** One node identity is right; one privilege domain is not. Plan adapter/helper isolation.

## Conclusion

BigBrain can remain a modular monolith for years if internal boundaries, durable workflows, policy, audit, and contract governance are made real. Sentinel is the correct security boundary only when it is an independent enforcement point rather than a privileged relay. The enterprise path should prioritize correctness, containment, recovery, and operability before distribution or feature breadth.

The Sentinel boundary architecture is internally coherent after this revision, but Sentinel v1 is **not yet authorized or ready for code implementation**. ADR 0002 remains Proposed, and the required PKI/enrollment, transport, normative schemas, policy-distribution, Linux privilege, and v1 data-classification/threat-model decisions are still open. Once those artifacts are accepted, the read-only v1 scope is suitable for implementation without redesigning the responsibility, capability, AI/automation, or multi-node model.
