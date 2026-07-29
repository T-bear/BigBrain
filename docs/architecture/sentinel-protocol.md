# BigBrain Sentinel Protocol

**Status:** Proposed protocol v1 design

**Encoding:** UTF-8 JSON

**Transport:** Deliberately undecided in this iteration

This document defines transport-independent message semantics. A later ADR must select transport after threat modeling and prototyping. JSON over HTTPS, HTTP/2, a private overlay, or another authenticated channel may carry the messages, but transport choice must not weaken mutual authentication, deadlines, replay protection, or auditability.

## Envelope

Every message uses the same envelope:

```json
{
  "protocolVersion": "1.0",
  "messageType": "request",
  "messageId": "01K0N8Y2M8ZJ4Y3M4M6H6F2T1A",
  "correlationId": "01K0N8Y1ZDMX8KWHBGK7M4EJRF",
  "causationId": "01K0N8Y1R61YZKX4YXRQ9MBH32",
  "sentAtUtc": "2026-07-23T14:15:30.123Z",
  "expiresAtUtc": "2026-07-23T14:15:40.123Z",
  "sender": {
    "type": "control-plane",
    "id": "control-plane:primary"
  },
  "nodeId": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B",
  "body": {}
}
```

- `messageId` is globally unique and used for replay detection.
- `correlationId` ties UI/API/job/Brain/automation/Sentinel/audit activity together.
- `causationId` points to the immediately preceding message or job.
- UTC timestamps use RFC 3339 with milliseconds.
- `expiresAtUtc` is mandatory for requests and ignored as authorization evidence after expiry.
- The authenticated peer identity must match `sender`; the JSON claim alone has no trust value.

IDs are opaque. Examples use ULID-shaped strings for readability, but the implementation may choose UUIDv7 if one scheme is standardized across BigBrain.

## Message classes and authorization

Messages are divided into two classes:

- **Protocol control:** handshake, heartbeat, bounded Sentinel self-health, capability discovery, draining, and revocation state. These messages are mutually authenticated and rate-limited but cannot read or mutate managed node resources.
- **Capability execution:** every Docker, host, process, filesystem, network, device, integration, or composite inventory operation. These use the generic `request`/`response` body and require request-bound authorization proof.

The inventory result below is the typed `result` inside a generic capability response for `Inventory.ReadSnapshot@1`; it is not an unauthenticated side channel. Heartbeat and self-health may report adapter status and bounded counters, but not the adapter's managed data.

The authenticated mTLS peer establishes workload identity. A capability request also carries `authorizationProof`, whose signed claims bind issuer, audience, mTLS client identity, node, capability version, normalized resource/arguments hash, principal, policy decision, approval reference, token ID, and validity window. Descriptive JSON fields never override the proof.

## Handshake

Handshake negotiates compatibility and reports identity, not authorization.

Request:

```json
{
  "protocolVersion": "1.0",
  "messageType": "handshake.request",
  "messageId": "01K0N900A5D37JHJE5V6AR0Q4K",
  "correlationId": "01K0N900A5D37JHJE5V6AR0Q4K",
  "causationId": null,
  "sentAtUtc": "2026-07-23T14:16:00.000Z",
  "expiresAtUtc": "2026-07-23T14:16:10.000Z",
  "sender": { "type": "control-plane", "id": "control-plane:primary" },
  "nodeId": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B",
  "body": {
    "supportedProtocolVersions": ["1.1", "1.0"],
    "controlPlaneVersion": "0.4.0",
    "requestedFeatures": ["capability-discovery", "inventory-snapshots"],
    "nonce": "base64url-256-bit-random-value"
  }
}
```

Response:

```json
{
  "protocolVersion": "1.0",
  "messageType": "handshake.response",
  "messageId": "01K0N900BSWRQXFGRZQPGM0TCS",
  "correlationId": "01K0N900A5D37JHJE5V6AR0Q4K",
  "causationId": "01K0N900A5D37JHJE5V6AR0Q4K",
  "sentAtUtc": "2026-07-23T14:16:00.050Z",
  "expiresAtUtc": null,
  "sender": { "type": "sentinel", "id": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B" },
  "nodeId": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B",
  "body": {
    "accepted": true,
    "selectedProtocolVersion": "1.0",
    "sentinelVersion": "1.0.0",
    "nodeIdentityThumbprint": "sha256:4f1d...9a2c",
    "features": ["capability-discovery", "inventory-snapshots"],
    "serverNonce": "base64url-256-bit-random-value",
    "timeUtc": "2026-07-23T14:16:00.049Z",
    "maximumClockSkewSeconds": 30
  }
}
```

Handshake failure returns supported versions and a stable reason, but no sensitive configuration.

## Heartbeat

Heartbeat is a liveness signal, not a full inventory poll:

```json
{
  "protocolVersion": "1.0",
  "messageType": "heartbeat",
  "messageId": "01K0N91VQ8E7MCHZZTQKX42PRM",
  "correlationId": "01K0N91VQ8E7MCHZZTQKX42PRM",
  "causationId": null,
  "sentAtUtc": "2026-07-23T14:17:00.000Z",
  "expiresAtUtc": null,
  "sender": { "type": "sentinel", "id": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B" },
  "nodeId": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B",
  "body": {
    "sequence": 1842,
    "status": "degraded",
    "policyVersion": "policy:42",
    "capabilityRevision": "sha256:9c81...42bd",
    "auditSpoolPercent": 2.1,
    "activeRequests": 1
  }
}
```

The Control Plane records last receipt time. Missed heartbeats transition `online -> suspect -> offline` using configurable thresholds; one missed heartbeat is not an incident.

## Health

Health provides bounded component status:

```json
{
  "protocolVersion": "1.0",
  "messageType": "health.response",
  "messageId": "01K0N934AMY8CFK2B47V6Q6A9Q",
  "correlationId": "01K0N9346ZYPJFD9CZGSEYZX5Q",
  "causationId": "01K0N9346ZYPJFD9CZGSEYZX5Q",
  "sentAtUtc": "2026-07-23T14:17:45.100Z",
  "expiresAtUtc": null,
  "sender": { "type": "sentinel", "id": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B" },
  "nodeId": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B",
  "body": {
    "status": "degraded",
    "checks": [
      { "name": "policy", "status": "healthy", "observedAtUtc": "2026-07-23T14:17:45.080Z" },
      { "name": "docker-adapter", "status": "unavailable", "code": "DEPENDENCY_UNAVAILABLE", "message": "Docker adapter is not enabled." },
      { "name": "audit-spool", "status": "healthy", "utilizationPercent": 2.1 }
    ]
  }
}
```

Valid aggregate states are `healthy`, `degraded`, `unhealthy`, `draining`, and `revoked`.

## Capability discovery

Discovery is node-specific and policy-filtered:

```json
{
  "protocolVersion": "1.0",
  "messageType": "capabilities.response",
  "messageId": "01K0N94GX4YYG1GKMSSKBHK59N",
  "correlationId": "01K0N94GTBA6MHNV5WBTG7RXPR",
  "causationId": "01K0N94GTBA6MHNV5WBTG7RXPR",
  "sentAtUtc": "2026-07-23T14:18:30.020Z",
  "expiresAtUtc": null,
  "sender": { "type": "sentinel", "id": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B" },
  "nodeId": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B",
  "body": {
    "revision": "sha256:9c81...42bd",
    "capabilities": [
      {
        "name": "Host.ReadCpu",
        "version": 1,
        "effect": "read",
        "risk": "low",
        "available": true,
        "requestSchema": "urn:bigbrain:sentinel:capability:Host.ReadCpu:request:1",
        "responseSchema": "urn:bigbrain:sentinel:capability:Host.ReadCpu:response:1",
        "limits": { "requestsPerMinute": 60, "timeoutMilliseconds": 2000 }
      },
      {
        "name": "Docker.RestartContainer",
        "version": 1,
        "effect": "mutate",
        "risk": "high",
        "available": false,
        "unavailableReason": "Disabled by local policy.",
        "requestSchema": "urn:bigbrain:sentinel:capability:Docker.RestartContainer:request:1",
        "responseSchema": "urn:bigbrain:sentinel:capability:Docker.RestartContainer:response:1",
        "approval": { "required": true, "maximumAgeSeconds": 60 }
      }
    ]
  }
}
```

Discovery does not grant a caller permission; it only describes what the node could accept under current local policy.

## Inventory

Inventory is a snapshot with provenance and freshness:

```json
{
  "protocolVersion": "1.0",
  "messageType": "response",
  "messageId": "01K0N96F97BM68BWG9QMADQT01",
  "correlationId": "01K0N96F5AT91T0T2TJE9G2PBM",
  "causationId": "01K0N96F5AT91T0T2TJE9G2PBM",
  "sentAtUtc": "2026-07-23T14:19:35.140Z",
  "expiresAtUtc": null,
  "sender": { "type": "sentinel", "id": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B" },
  "nodeId": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B",
  "body": {
    "requestId": "request:01K0N96F4VHQP63S5X2P8HTM77",
    "outcome": "succeeded",
    "startedAtUtc": "2026-07-23T14:19:35.020Z",
    "completedAtUtc": "2026-07-23T14:19:35.135Z",
    "result": {
      "snapshotId": "snapshot:01K0N96F8TVPNW4YN28EN4CE6A",
      "collectedAtUtc": "2026-07-23T14:19:35.100Z",
      "status": "partial",
      "sections": {
        "host": {
          "status": "available",
          "data": {
            "logicalProcessorCount": 8,
            "memoryTotalBytes": 34359738368
          }
        },
        "docker": {
          "status": "unavailable",
          "code": "CAPABILITY_UNAVAILABLE",
          "message": "Docker inventory is disabled."
        }
      },
      "warnings": ["One inventory section was unavailable."]
    },
    "warnings": [],
    "auditRecordId": "audit:01K0N96F9C6MT3H96CMJQ5V8PF"
  }
}
```

Inventory responses never include environment variables, secret material, raw device credentials, unrestricted mounts, or unbounded process command lines.

## Capability request

```json
{
  "protocolVersion": "1.0",
  "messageType": "request",
  "messageId": "01K0N98B5XCGFA61YQAQXPA4J4",
  "correlationId": "01K0N98AZCF50TFZQJ1Z61SRW7",
  "causationId": "job:01K0N989ZBC76YQBGDXAF9H00A",
  "sentAtUtc": "2026-07-23T14:20:38.000Z",
  "expiresAtUtc": "2026-07-23T14:20:48.000Z",
  "sender": { "type": "control-plane", "id": "control-plane:primary" },
  "nodeId": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B",
  "body": {
    "capability": { "name": "Docker.ReadContainers", "version": 1 },
    "requestId": "request:01K0N98B54YJ1JFMXF7V6M5CJQ",
    "idempotencyKey": null,
    "deadlineMilliseconds": 5000,
    "authorizationProof": {
      "format": "signed-delegation-token",
      "value": "<detached-or-compact-signed-proof>"
    },
    "principal": {
      "type": "user",
      "subjectId": "user:8a84f4c0",
      "tenantId": "tenant:home"
    },
    "delegation": {
      "source": "web",
      "workflowId": null,
      "policyDecisionId": "decision:01K0N98AFY4MKN9CPEJ3X3BPNR"
    },
    "approval": null,
    "arguments": {
      "includeStopped": true,
      "fields": ["id", "name", "image", "state", "health", "ports"]
    }
  }
}
```

The proof value is illustrative and must never be logged, returned, placed in audit evidence, or exposed to UI/AI. Sentinel treats `principal`, `delegation`, and `approval` as untrusted duplicates until they exactly match verified proof claims.

## Composite inventory request

Inventory uses a capability request and names each requested component explicitly:

```json
{
  "protocolVersion": "1.0",
  "messageType": "request",
  "messageId": "01K0N97KQ9B7GQ6YAHQXPQFJ1C",
  "correlationId": "01K0N97KMDW9DVPSNGR4QG6H5V",
  "causationId": null,
  "sentAtUtc": "2026-07-23T14:20:05.000Z",
  "expiresAtUtc": "2026-07-23T14:20:15.000Z",
  "sender": { "type": "control-plane", "id": "control-plane:primary" },
  "nodeId": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B",
  "body": {
    "capability": { "name": "Inventory.ReadSnapshot", "version": 1 },
    "requestId": "request:01K0N97KQ1Z7E3FG13EX0F3GCJ",
    "idempotencyKey": null,
    "deadlineMilliseconds": 5000,
    "authorizationProof": {
      "format": "signed-delegation-token",
      "value": "<proof-bound-to-the-exact-section-and-field-set>"
    },
    "principal": {
      "type": "service",
      "subjectId": "service:node-inventory",
      "tenantId": "tenant:home"
    },
    "delegation": {
      "source": "worker",
      "workflowId": "workflow:01K0N97KJ5PY9QVS3H3J49AXZP",
      "policyDecisionId": "decision:01K0N97KJX7W8M2W5F26VM70TG"
    },
    "approval": null,
    "arguments": {
      "sections": [
        { "capability": "Host.ReadCpu@1", "fields": ["logicalProcessorCount", "usagePercent"] },
        { "capability": "Host.ReadMemory@1", "fields": ["totalBytes", "availableBytes"] }
      ]
    }
  }
}
```

Authorization proof binds the complete ordered section/field set. Sentinel authorizes each component independently and returns explicit partial status. A new section or field requires a new request and proof.

## Success response

```json
{
  "protocolVersion": "1.0",
  "messageType": "response",
  "messageId": "01K0N98BC9WT92C6JEKDB8YEV9",
  "correlationId": "01K0N98AZCF50TFZQJ1Z61SRW7",
  "causationId": "01K0N98B5XCGFA61YQAQXPA4J4",
  "sentAtUtc": "2026-07-23T14:20:38.180Z",
  "expiresAtUtc": null,
  "sender": { "type": "sentinel", "id": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B" },
  "nodeId": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B",
  "body": {
    "requestId": "request:01K0N98B54YJ1JFMXF7V6M5CJQ",
    "outcome": "succeeded",
    "startedAtUtc": "2026-07-23T14:20:38.030Z",
    "completedAtUtc": "2026-07-23T14:20:38.175Z",
    "result": {
      "collectedAtUtc": "2026-07-23T14:20:38.160Z",
      "containers": [
        {
          "id": "a1b2c3d4e5f6",
          "name": "media-server",
          "image": "vendor/media:1.2.3",
          "state": "running",
          "health": "healthy",
          "ports": [{ "privatePort": 8096, "publicPort": 8096, "protocol": "tcp" }]
        }
      ]
    },
    "warnings": [],
    "auditRecordId": "audit:01K0N98BC3YQ63SE4K4B0E4V7P"
  }
}
```

## Error response

```json
{
  "protocolVersion": "1.0",
  "messageType": "response",
  "messageId": "01K0N99QEN3BGGKXFQDHCB09XP",
  "correlationId": "01K0N99Q7ERD7MXB5T47P51WV5",
  "causationId": "01K0N99Q8A97DBM7CB48EVBWDF",
  "sentAtUtc": "2026-07-23T14:21:23.100Z",
  "expiresAtUtc": null,
  "sender": { "type": "sentinel", "id": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B" },
  "nodeId": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B",
  "body": {
    "requestId": "request:01K0N99Q83R3NW6AKPV4PFAE2E",
    "outcome": "rejected",
    "error": {
      "code": "LOCAL_POLICY_DENIED",
      "category": "authorization",
      "message": "The node policy does not permit this capability.",
      "retryable": false,
      "details": {}
    },
    "auditRecordId": "audit:01K0N99QEGM7B6Z1QGP5NJVWXN"
  }
}
```

Stable categories are `validation`, `authentication`, `authorization`, `conflict`, `dependency`, `timeout`, `rate-limit`, and `internal`. Safe codes include `PROTOCOL_UNSUPPORTED`, `MESSAGE_EXPIRED`, `REPLAY_DETECTED`, `CAPABILITY_UNKNOWN`, `CAPABILITY_UNAVAILABLE`, `LOCAL_POLICY_DENIED`, `APPROVAL_REQUIRED`, `RESOURCE_NOT_ALLOWED`, `DEPENDENCY_UNAVAILABLE`, `DEADLINE_EXCEEDED`, `RATE_LIMITED`, and `RESULT_TOO_LARGE`.

## Timeouts, cancellation, and retries

- Every request has an absolute envelope expiry and a shorter execution deadline.
- Sentinel rejects requests that cannot start before expiry.
- The Control Plane may send a typed cancellation request; cancellation is best effort.
- Read operations may be retried with exponential backoff, jitter, a retry budget, and the same correlation ID but a new message ID.
- Every retry receives a new authorization proof bound to the new message ID and current expiry.
- Mutations are retried only with a stable idempotency key and capability-declared idempotency semantics.
- `timeout` means outcome may be unknown. The Control Plane queries request/job status before retrying a mutation.
- `rate-limit` responses may include safe `retryAfterMilliseconds`.
- Reconnection never causes blanket replay of queued mutations.

## Future event streaming

Protocol v1 reserves event semantics but does not choose or implement streaming transport.

```json
{
  "protocolVersion": "1.1",
  "messageType": "event",
  "messageId": "01K0N9BMTD0H5T0B9KKT1DRD3D",
  "correlationId": "01K0N9BMTD0H5T0B9KKT1DRD3D",
  "causationId": null,
  "sentAtUtc": "2026-07-23T14:22:25.000Z",
  "expiresAtUtc": null,
  "sender": { "type": "sentinel", "id": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B" },
  "nodeId": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B",
  "body": {
    "eventType": "docker.container.state-changed.v1",
    "sequence": 9843,
    "observedAtUtc": "2026-07-23T14:22:24.950Z",
    "resource": { "type": "docker.container", "id": "a1b2c3d4e5f6" },
    "data": { "previousState": "running", "state": "exited", "exitCode": 0 }
  }
}
```

Future streaming requires ordered per-node sequence numbers, resumable checkpoints, bounded retention, gap detection, duplicate tolerance, snapshot reconciliation, and backpressure. Events are observations, never commands. Delivery is at-least-once; consumers must be idempotent.

## Schema governance

JSON Schemas become the normative machine-readable contracts when implementation begins. Schemas are immutable after release, stored with test vectors, and validated by both sides. Payload size, nesting, string length, collection count, numeric range, and unknown-field behavior are explicitly bounded. Contract tests cover old Control Plane/new Sentinel and new Control Plane/old Sentinel combinations.

Protocol v1 implementation is blocked until normative schemas define the envelope, authorization proof container, every protocol-control message, `Inventory.ReadSnapshot@1`, and each v1 component capability. JSON examples are explanatory, not sufficient contracts.
