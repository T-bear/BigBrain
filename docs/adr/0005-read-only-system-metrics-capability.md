# ADR 0005: Read-only System Metrics capability

- Status: Proposed
- Date: 2026-07-29
- Applies to: Sentinel v1 read-only capability set and single-node local transport profile
- Builds on: ADR 0001, ADR 0002, ADR 0003 and ADR 0004

## Context

BigBrain has a System view and the versioned Control Plane endpoint
`GET /api/v1/system/overview`, but the registered provider intentionally returns
`Unavailable`. The API process may not read host resources directly. The current
Sentinel process is only the pre-v1 bootstrap authorized by ADR 0004 and therefore
has neither host access nor Control Plane communication.

The concrete product need is a current, bounded overview of one BigBrain server:

- host uptime;
- aggregate CPU utilization and logical processor count;
- aggregate memory capacity and utilization;
- capacity and utilization for a locally allowlisted set of relevant filesystems.

This is observation only. It does not include processes, files or directory
contents, device details, arbitrary paths, network data, temperature, Docker,
mutation, shell execution or a generic host-information endpoint.

This ADR proposes the capability and a local transport profile. It does not accept
the proposal, authorize implementation, relax ADR 0004 or silently amend the
frozen Sentinel v1 baseline.

## Affected frozen invariants

The proposal preserves and makes concrete these ADR 0003 invariants:

1. Sentinel remains the exclusive boundary for node-local system access.
2. Control Plane and Sentinel remain separate trust domains and independently
   authorize every request.
3. Every observation uses explicit, typed and versioned capabilities.
4. Effective authority is the intersection of Control Plane policy,
   request-bound proof and Sentinel-local safety policy.
5. API, Web, modules, Worker, Brain and automation receive no direct host access or
   alternate path.
6. There is no shell, arbitrary filesystem read, generic proxy or dynamic code.
7. Node and Control Plane identities are mutually authenticated, unique and
   revocable.
8. Results are node-bound, bounded, redacted and carry collection time and
   availability.
9. Sentinel v1 remains read-only.

The proposal crosses the ADR 0004 bootstrap boundary by adding managed-resource
access, capability execution and Control Plane communication. ADR 0004 therefore
remains fully applicable until this ADR and all blockers below are accepted; this
ADR cannot be implemented as a bootstrap exception.

## Decision

If accepted, System Metrics is part of the Sentinel v1 read-only foundation. It
does not require a new protocol major version because the frozen v1 model already
defines typed read capabilities, partial inventory and the relevant CPU, memory
and disk capability names.

The Control Plane obtains one dashboard snapshot through
`Inventory.ReadSnapshot@1`, explicitly requesting:

- `Host.ReadUptime@1`;
- `Host.ReadCpu@1`;
- `Host.ReadMemory@1`;
- `Host.ReadDisk@1`.

`Host.ReadUptime@1` is the only new component capability proposed here. It follows
the existing naming and versioning rules and fills a concrete gap in the initial
taxonomy. The composite capability does not reduce the policy, risk, field,
resource or audit requirements of any component. Each section is authorized and
collected independently.

The capability descriptors are:

| Capability | Effect | Default risk | Data classification | Approval | Hard execution bound |
|---|---|---|---|---|---|
| `Host.ReadUptime@1` | read | low | internal | none | 2 seconds |
| `Host.ReadCpu@1` | read | low | internal | none | 2 seconds |
| `Host.ReadMemory@1` | read | low | internal | none | 2 seconds |
| `Host.ReadDisk@1` | read | medium | sensitive | none | 3 seconds |
| `Inventory.ReadSnapshot@1` | read | medium | sensitive | none | 5 seconds |

The accepted normative schemas may impose stricter time, rate, cardinality and
payload bounds. No field outside the contracts below is implicitly permitted.

## Local transport

For the initial single-node deployment, the proposed transport is HTTP/2 with
UTF-8 JSON Sentinel protocol envelopes over a Unix domain socket owned by the
Sentinel service.

- Sentinel runs as a separately packaged system service, not inside the API
  process and not as a library loaded by it.
- The socket has a fixed configured path, owner and group, restrictive filesystem
  permissions, bounded backlog and no public listener.
- The API receives access only to that socket. It receives no host root mount,
  `/proc`, `/sys`, device, Docker socket or general Sentinel filesystem access.
- Mutual TLS is required over the socket. Sentinel verifies the Control Plane
  workload certificate; the Control Plane verifies the node-bound Sentinel
  certificate. Unix peer credentials and socket ACLs are defense in depth and do
  not replace protocol authentication.
- Every request still uses the v1 envelope, expiry, message ID, node ID, replay
  protection and a short-lived `authorizationProof` bound to the exact capability,
  ordered section/field set and request hash.
- No unauthenticated metrics endpoint, loopback HTTP shortcut or direct module-to-
  Sentinel call is permitted.

The Unix socket is a local deployment profile, not a different application
protocol. A later remote-node transport may carry the same protocol and capability
schemas over mutually authenticated HTTPS/HTTP2 on a private management network.
Selecting and accepting the final transport and PKI lifecycle remains a blocker.

## Typed and versioned contract

### Request

The request is `Inventory.ReadSnapshot@1` with no caller-supplied host path:

```json
{
  "capability": { "name": "Inventory.ReadSnapshot", "version": 1 },
  "arguments": {
    "sections": [
      {
        "capability": "Host.ReadUptime@1",
        "fields": ["uptimeSeconds"]
      },
      {
        "capability": "Host.ReadCpu@1",
        "fields": ["logicalProcessorCount", "usagePercent", "sampleWindowMilliseconds"]
      },
      {
        "capability": "Host.ReadMemory@1",
        "fields": ["totalBytes", "usedBytes", "availableBytes", "usagePercent"]
      },
      {
        "capability": "Host.ReadDisk@1",
        "resourceSelector": { "filesystemSet": "system-dashboard" },
        "fields": ["filesystemId", "displayName", "totalBytes", "usedBytes", "availableBytes", "usagePercent"]
      }
    ]
  }
}
```

`filesystemSet` is an opaque, locally configured allowlist name. The caller cannot
submit absolute paths, mount points, glob patterns, filesystem types or device
names.

### Response

```json
{
  "snapshotId": "snapshot:01K0N96F8TVPNW4YN28EN4CE6A",
  "nodeId": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B",
  "collectedAtUtc": "2026-07-29T12:00:00.000Z",
  "status": "partial",
  "sections": {
    "uptime": {
      "status": "available",
      "data": { "uptimeSeconds": 483920.25 }
    },
    "cpu": {
      "status": "available",
      "data": {
        "logicalProcessorCount": 8,
        "usagePercent": 17.4,
        "sampleWindowMilliseconds": 1000
      }
    },
    "memory": {
      "status": "available",
      "data": {
        "totalBytes": 34359738368,
        "usedBytes": 12884901888,
        "availableBytes": 21474836480,
        "usagePercent": 37.5
      }
    },
    "disks": {
      "status": "partial",
      "items": [
        {
          "filesystemId": "root",
          "displayName": "System",
          "status": "available",
          "totalBytes": 536870912000,
          "usedBytes": 214748364800,
          "availableBytes": 322122547200,
          "usagePercent": 40.0
        },
        {
          "filesystemId": "media",
          "displayName": "Media",
          "status": "unavailable",
          "error": {
            "code": "DEPENDENCY_UNAVAILABLE",
            "message": "The filesystem metric is temporarily unavailable.",
            "retryable": true
          }
        }
      ]
    }
  },
  "warnings": ["One filesystem metric was unavailable."]
}
```

Contract rules:

- Durations are non-negative decimal seconds; byte values are non-negative
  64-bit integers; percentages are decimals in the inclusive range `0..100`.
- `usedBytes` is defined as `totalBytes - availableBytes`; collectors do not mix
  incompatible “free” and “available” semantics.
- CPU utilization is aggregate non-idle time over the declared monotonic sample
  window, not load average and not an instantaneous process value.
- Filesystem IDs and display names are locally assigned, bounded strings. Raw
  device identifiers, volume UUIDs and host paths are never returned.
- Every requested section is present exactly once with `available`, `partial`,
  `unavailable` or `denied` status. Data fields are present only when their value
  is valid.
- Schemas define maximum string lengths, four sections, a maximum of 16 filesystem
  items, a 32 KiB response limit, unknown-field behavior and numeric bounds.
- Capability `@1` semantics are immutable after release. A broadened resource
  selector, new sensitive field or changed measurement meaning requires `@2`.

## Error model and partial availability

Transport/protocol failures use the frozen response envelope and stable categories.
Whole-request codes include `PROTOCOL_UNSUPPORTED`, `MESSAGE_EXPIRED`,
`REPLAY_DETECTED`, `CAPABILITY_UNKNOWN`, `LOCAL_POLICY_DENIED`,
`DEADLINE_EXCEEDED`, `RATE_LIMITED`, `RESULT_TOO_LARGE` and safe internal errors.

Collection failures use section or filesystem-item status and only these initial
safe codes:

- `CAPABILITY_UNAVAILABLE`: collector or platform support is absent;
- `RESOURCE_NOT_ALLOWED`: the requested local filesystem set is not allowed;
- `DEPENDENCY_UNAVAILABLE`: a permitted source cannot currently be read;
- `DEADLINE_EXCEEDED`: collection exceeded its bound;
- `VALUE_INVALID`: source data failed range or consistency validation;
- `LOCAL_POLICY_DENIED`: local policy denies that component.

No exception, raw path, OS error text or source payload crosses the boundary.
Errors include a bounded safe message and `retryable`. Authorization denial is not
reported as dependency failure.

The snapshot is:

- `available` when all requested sections and items are available;
- `partial` when at least one valid metric and at least one section/item is not
  available;
- `unavailable` when no requested metric is valid;
- `denied` when the composite request itself is denied.

One unavailable metric never suppresses valid siblings. The Control Plane maps
valid fields into the existing nullable `SystemOverview` model, carries safe
warnings, marks partial results `Degraded`, and never presents cached data as
current without its collection timestamp and stale state.

## Sentinel-local policy and resources

Default is deny. A local safety baseline must explicitly enable each component
capability version and the named `system-dashboard` filesystem set.

The baseline may allow only:

- monotonic host boot time;
- aggregate CPU counters and logical processor count;
- aggregate physical memory totals and availability;
- capacity statistics for fixed, operator-configured logical filesystems.

It must deny:

- caller-provided paths or filesystem discovery;
- removable, pseudo, virtual, network or secret-bearing filesystems unless each is
  explicitly named and reviewed;
- directory enumeration, file metadata/content, quotas per user, device serials,
  UUIDs, raw mount options and inode details;
- process, cgroup, container, network, sensor and environment information;
- requests exceeding local frequency, concurrency, timeout or result limits.

The operational overlay may narrow fields, filesystem sets, callers, frequency or
availability. It cannot remotely add a filesystem, expose a raw path, enable a
capability version or raise a hard bound beyond the local baseline. Recommended
initial limits are one concurrent snapshot and 12 requests per minute per node and
Control Plane identity.

## Security and information disclosure

Even aggregate metrics reveal operating patterns, machine capacity, restart times
and storage pressure. Filesystem names and relative sizes can reveal workload
purpose. The complete result is therefore internal data, with disk results treated
as sensitive.

Mitigations are:

- mTLS, node binding, short-lived request proof, replay protection and local
  authorization;
- output allowlists and schema/range validation before serialization;
- opaque filesystem IDs and neutral display names instead of paths/devices;
- bounded cardinality, size, sampling frequency and retention;
- metadata-only audit containing capability, normalized selector, policy versions,
  timing and outcome, but not raw metric values;
- structured logs without proof values, paths, source data or full responses;
- Control Plane caching with provenance and explicit freshness;
- no automatic exposure to AI context; a separate reduced-field policy decision is
  required if that use is later proposed.

Compromise of Sentinel can falsify metrics for its own node. The Control Plane must
not treat these observations as authorization or approval evidence.

## Privilege separation

Sentinel Core remains an unprivileged protocol and policy process without broad
filesystem access, shell or Docker access. Collection runs behind a compiled,
typed host-metrics reader boundary.

The preferred Linux packaging is a separate unprivileged reader process or
sandboxed helper with:

- read access only to the documented kernel interfaces needed for aggregate
  uptime, CPU and memory;
- metadata-only filesystem capacity operations for the locally resolved
  allowlist;
- no directory traversal, file-content reads, device access, network access,
  Control Plane credentials or writable host paths;
- a fixed request/response contract, deadline, output bound and no caller-supplied
  path;
- OS sandboxing and service ACLs that permit calls only from Sentinel Core.

Portable .NET runtime APIs are preferred where they provide the required host
semantics. Linux-specific `/proc` or equivalent parsing, if required, is isolated
inside the reader, uses direct bounded file reads rather than shell commands, and
is covered by parser fixtures. No command execution is allowed.

If implementation evidence shows that the reader cannot be meaningfully
constrained without broad host access, implementation stops and a new ADR must
evaluate that reproducible problem. Running the whole Sentinel as root is not an
acceptable fallback.

## Alternatives considered

### Read metrics directly in BigBrain API

Rejected. It violates ADR 0001 and the exclusive Sentinel boundary, expands the
LAN-facing process and gives containers host visibility.

### Expose metrics through Sentinel bootstrap health

Rejected. Protocol-control health cannot read managed resources, and ADR 0004
explicitly bounds bootstrap health to process self-health.

### One broad `Host.ReadSystemMetrics@1` capability

Rejected. It weakens field- and resource-level authorization, diverges from the
frozen capability taxonomy and makes partial policy decisions less precise.

### Call four component capabilities separately from the System view

Not selected. It creates inconsistent collection times and extra protocol/audit
traffic. `Inventory.ReadSnapshot@1` already provides bounded composition while
preserving independent authorization.

### Loopback TCP or unauthenticated Unix socket

Rejected. Network location and local user identity are not sufficient trust, and
loopback is awkward across the existing container/service boundary.

### Shell commands such as `uptime`, `free`, `df` or `top`

Rejected. Shell parsing is locale- and platform-sensitive and would introduce a
generic execution surface. Runtime APIs or narrow direct kernel-interface readers
are more stable and constrainable.

### Prometheus/node_exporter as the Control Plane source

Not selected for this capability. It would create a second node-access path,
different identity/policy semantics and an additional exposed service. A future
adapter decision may reuse an exporter only if Sentinel remains the enforcing and
normalizing boundary.

### Put Sentinel in the API container

Rejected. Co-location would collapse the intended process and credential boundary
and would report container rather than authoritative host semantics.

## Compatibility and migration

1. Existing deployments continue to register
   `UnavailableSystemMetricsProvider`; no fallback to direct host reads is added.
2. After all blockers are accepted, Sentinel may advertise the four component
   capabilities and `Inventory.ReadSnapshot@1`. Absence is a normal unsupported
   state.
3. A new Control Plane provider negotiates protocol/capability versions and uses
   Sentinel only when every required security gate succeeds. During staged rollout,
   unavailable components map to the existing nullable fields and warnings.
4. The existing `/api/v1/system/overview` response shape can remain compatible:
   uptime, CPU, memory and disk values already have nullable fields. Opaque
   filesystem display names may populate the existing presentation label without
   revealing host paths.
5. Old Control Plane/new Sentinel and new Control Plane/old Sentinel combinations
   return bounded unavailable/degraded states, never direct-access fallback.
6. Additive optional fields require schema permission and negotiated protocol
   compatibility. Changed semantics, broader filesystem selection or additional
   disclosed data require a new capability version.
7. A future remote transport changes deployment configuration, not the capability
   result contract. No silent downgrade from mTLS/proof validation to local trust is
   allowed.

## Test strategy

Before release, tests must cover:

- normative JSON Schema validation, bounds, required sections and golden vectors;
- old/new protocol and capability-version compatibility in both directions;
- deterministic mapping from Sentinel partial results to the existing
  `SystemOverview` API and frontend unavailable/degraded states;
- uptime monotonicity and non-negative/range validation;
- CPU sampling math across idle, busy, counter wrap/reset and malformed samples;
- memory arithmetic, overflow, unavailable fields and malformed source data;
- disk allowlist selection, excluded filesystem types, stable opaque identifiers,
  duplicate mounts, disappearing mounts and capacity arithmetic;
- parser fixtures for every supported Linux kernel-interface format, with no shell
  invocation;
- helper sandbox and OS-permission tests proving denial of unconfigured resources,
  directory/file content, device access, writes and network access;
- mutual authentication, wrong-node/audience/certificate rejection, revocation,
  expiry, clock skew, request-hash binding and replay rejection;
- Sentinel-local deny-by-default policy, remote narrowing and prevention of remote
  widening;
- timeouts, cancellation, rate/concurrency limits, payload/cardinality bounds and
  partial availability;
- redaction tests ensuring paths, device IDs, raw OS errors, proofs and source
  payloads never enter API responses, logs or audit;
- deployment tests proving the API has only socket access and no host `/proc`,
  `/sys`, root filesystem, devices or Docker socket;
- an end-to-end single-node test from Control Plane provider through Sentinel and
  the reader to the existing System view.

## Consequences

### Positive

- The existing System view can show real host metrics without moving host access
  into the Control Plane.
- The capability is read-only, bounded, composable and compatible with existing
  nullable UI/API models.
- Individual collection failures remain visible without hiding valid data.
- Filesystem disclosure and OS-specific collection are constrained at the node.

### Negative

- Even a small dashboard requires identity, transport, proof, policy, audit and
  privilege-separation infrastructure.
- CPU utilization requires a sampling interval and cannot be a zero-cost
  instantaneous read.
- A separate reader and mTLS over a local socket add packaging and operational
  complexity.
- Disk metrics remain privacy-sensitive and need local operator configuration.

## Open decisions blocking implementation

Implementation remains prohibited until all of the following are accepted and
available:

1. This ADR, including whether `Host.ReadUptime@1` belongs to Sentinel v1.
2. The transport ADR selecting connection direction and validating HTTP/2 plus
   mTLS over a Unix domain socket for the local profile.
3. Enrollment and PKI decisions covering node and Control Plane identity,
   certificate issuance, storage, rotation and revocation.
4. Normative immutable JSON Schemas and compatibility vectors for protocol v1,
   authorization proof, capability discovery, `Inventory.ReadSnapshot@1` and all
   four component capabilities.
5. The signed request-bound delegation-proof format, issuer lifecycle, replay
   cache, clock-skew and expiry rules.
6. Sentinel-local safety baseline and signed operational-overlay format,
   distribution, activation, rollback protection and emergency deny.
7. A Linux packaging and privilege-separation ADR for Sentinel Core and the host
   metrics reader, including exact OS identity, ACL and sandbox controls.
8. A data-classification and threat-model review for uptime, capacity, filesystem
   labels and usage patterns.
9. Audit record, local spool, integrity, ingestion and outage behavior for
   read-only capability requests.
10. Release signing, dependency provenance, SBOM, update, rollback and supported
    platform policy.
11. Prototype evidence that the chosen APIs expose host rather than container
    namespace semantics and that required filesystem capacity reads work without
    broad privilege.

Until these blockers are resolved, the Sentinel capability registry remains empty,
ADR 0004 remains the only implementation authorization, and the System view
continues to report metrics as unavailable.
