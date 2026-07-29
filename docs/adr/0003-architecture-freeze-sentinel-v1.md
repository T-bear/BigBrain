# ADR 0003: Architecture Freeze – Sentinel v1

- Status: Accepted
- Date: 2026-07-23
- Applies to: Sentinel v1 architecture baseline
- Builds on: ADR 0002, Sentinel is the exclusive boundary for node-local system access

## Context

The Sentinel architecture has completed its external and internal architecture reviews. Responsibilities, trust boundaries, protocol semantics, capability and security models, AI and Automation Engine constraints, multi-node direction, evolution rules, and known enterprise risks have been documented and reconciled.

Sentinel v1 now needs a stable architectural baseline so implementation can proceed without repeatedly reopening settled decisions or allowing implementation convenience to erode the security boundary.

## Decision

The Sentinel v1 architecture is approved and frozen.

Further Sentinel v1 design and implementation must follow:

- `docs/architecture/sentinel-architecture.md`
- `docs/architecture/sentinel-protocol.md`
- `docs/architecture/sentinel-capability-model.md`
- `docs/security/sentinel-security-model.md`
- `docs/architecture/sentinel-evolution.md`
- `docs/architecture/enterprise-architecture-review.md`
- ADR 0001 and ADR 0002

The architecture may change only when implementation or validation reveals a concrete, reproducible design problem that cannot be resolved while preserving the approved baseline. Such a change requires an explicit new ADR that identifies the evidence, affected invariants, alternatives, security consequences, compatibility impact, and migration path.

New features, broader capabilities, additional integrations, transports, privilege models, or post-v1 behavior do not modify the Sentinel v1 baseline through routine documentation edits. They are proposed through new ADRs and assigned to a future compatible capability, protocol, or Sentinel version.

Editorial corrections that do not alter semantics are permitted, but must not broaden authority, weaken security controls, change ownership, or reinterpret a released contract.

## Frozen invariants

- Sentinel is the exclusive boundary for node-local system access.
- Control Plane and Sentinel are separate trust domains and independently authorize requests.
- Managed-resource access uses explicit, typed, versioned capabilities.
- Effective authority is constrained by both Control Plane policy and Sentinel-local safety policy.
- AI Brain, Automation Engine, modules, Web, API, and Worker never bypass the Control Plane-to-Sentinel path.
- Sentinel provides no arbitrary shell, Docker API passthrough, arbitrary HTTP proxy, or dynamic in-process plugin execution.
- Node identities are unique, rotatable, and revocable.
- Requests, proofs, inventory, and audit evidence are node-bound, bounded, redacted, and freshness-aware.
- Sentinel v1 is read-only; mutations belong to later, separately approved architecture versions.

## Consequences

### Positive

- Implementation has a stable security and responsibility baseline.
- Scope expansion cannot silently enter Sentinel v1.
- Protocol and capability contracts can be made normative against fixed invariants.
- Design changes require evidence and an auditable decision.

### Negative

- Implementation convenience is not sufficient reason to weaken or bypass an invariant.
- Genuine design defects require a new ADR and may delay delivery.
- New features may require a new version even when a local shortcut appears simpler.

## Governance

Architecture conformance is reviewed alongside implementation. Deviations fail review unless supported by a newer accepted ADR that explicitly supersedes the affected decision.

This freeze authorizes implementation planning and implementation of the approved Sentinel v1 scope only. It does not authorize deferred features or bypass the prerequisite PKI, transport, policy, schema, threat-model, and privilege-separation decisions documented by the architecture.
