# BigBrain Sentinel Capability Model

**Status:** Proposed

## Principle

A capability is a versioned, typed permission to perform one narrowly defined operation on a constrained resource set. It is simultaneously:

- A discoverable Sentinel contract.
- A Control Plane authorization target.
- A local Sentinel policy target.
- A typed request/response schema.
- A risk, approval, rate-limit, timeout, and audit definition.

A capability is not a UI feature, role, endpoint, shell command, Docker API route, or proof that the caller is authorized.

## Naming and versioning

Names use `Domain.VerbNoun` in PascalCase and are paired with an integer contract version:

```text
Host.ReadCpu@1
Docker.ReadContainers@1
Docker.RestartContainer@1
Filesystem.Read@1
```

Names are stable and semantic. A change that broadens accessible resources, effects, or disclosed data requires a new version. Additive optional response fields may remain within a version only when schema compatibility explicitly permits them.

Avoid broad capabilities such as `Host.Admin`, `Docker.Control`, `Filesystem.All`, or `Execute.Command`.

## Capability descriptor

Each compiled capability declares:

| Field | Meaning |
|---|---|
| Name and version | Stable contract identity |
| Effect | `read`, `mutate`, `delete`, `power`, or `execute` |
| Risk | `low`, `medium`, `high`, or `critical` |
| Request/response schema | Strict machine-readable contracts |
| Resource selector schema | Which resources may be targeted |
| Data classification | Public, internal, sensitive, secret-prohibited |
| AI context policy | Whether and which reduced result fields may enter model context |
| Approval rule | Whether fresh human approval is mandatory |
| Idempotency | None, natural, or key-based |
| Timeout/result limits | Hard execution and payload bounds |
| Rate/concurrency limits | Per node, principal, capability, and resource |
| Audit level | Metadata, evidence hash, or full redacted change record |
| Preconditions | Platform, adapter, health, policy, and dependency requirements |

## Initial taxonomy

### Read-only foundation

| Capability | Scope | Default risk |
|---|---|---|
| `Host.ReadCpu@1` | Aggregate CPU topology and utilization | Low |
| `Host.ReadMemory@1` | Aggregate memory capacity and utilization | Low |
| `Host.ReadDisk@1` | Allowlisted logical storage summaries | Medium |
| `Host.ReadTemperature@1` | Normalized safe sensor values | Low |
| `Host.ReadNetwork@1` | Allowlisted interface state and aggregate counters | Medium |
| `Host.ReadProcesses@1` | Bounded, redacted process inventory | High |
| `Docker.ReadContainers@1` | Redacted container inventory | Medium |
| `Docker.ReadImages@1` | Image metadata without registry credentials | Medium |
| `Docker.ReadLogs@1` | Bounded redacted log window for allowlisted containers | High |
| `Filesystem.Read@1` | Bounded content from named storage roots | High |
| `Inventory.ReadSnapshot@1` | Bounded composite snapshot of explicitly requested read capabilities | Medium |

Process command lines, Docker environment variables, labels known to contain secrets, raw mount paths, registry credentials, and unrestricted logs are excluded by default.

`Inventory.ReadSnapshot@1` is an orchestration capability, not an authorization shortcut. Its request names the exact component capabilities and fields to collect. The Control Plane must be authorized for every requested component, and Sentinel evaluates every component against local policy. A partial result identifies unavailable or denied sections without disclosing protected details. Adding a new inventory section never becomes implicitly authorized by an older snapshot request.

The effective risk, timeout, data classification, AI-context restriction, audit level, and rate limit of a composite request are at least as restrictive as the strictest requested component. Composition can never lower a component's controls.

### Future mutations

| Capability | Required constraints | Default risk |
|---|---|---|
| `Docker.RestartContainer@1` | Allowlisted container, approval, idempotency, cooldown | High |
| `Filesystem.Write@1` | Named root, path normalization, size/type quota, atomic write | High |
| `Filesystem.Delete@1` | Named root, recoverable deletion, approval, audit | Critical |
| `Host.PowerShutdown@1` | Fresh approval, physical-node selector, drain policy | Critical |

There is deliberately no generic `Docker.Exec`, `Host.Shell`, or `AI.Execute` capability. Future execution is a catalog of predefined operations with typed arguments, fixed executables or sandboxed workloads, and separately reviewed capability contracts.

## Resource selectors

Authorization is always capability plus resource, not capability alone.

Examples:

```json
{
  "capability": "Docker.RestartContainer@1",
  "resource": {
    "type": "docker.container",
    "nodeId": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B",
    "id": "a1b2c3d4e5f6"
  }
}
```

```json
{
  "capability": "Filesystem.Read@1",
  "resource": {
    "type": "storage.path",
    "nodeId": "node:01K0MZZVYV8S7Q5M2H7F6C9A2B",
    "storageRoot": "documents",
    "relativePath": "reports/summary.pdf"
  }
}
```

Clients never submit host-absolute paths. Sentinel maps stable named storage roots to local paths, resolves symlinks safely, normalizes paths, and verifies the final object remains beneath the authorized root.

## Authorization decision

BigBrain may send a request only when all gates succeed:

```text
authenticated initiator
  AND initiating module/service declares the capability version when applicable
  AND node currently advertises capability as available
  AND caller policy allows capability + resource + conditions
  AND workflow state and tenant/node boundary permit it
  AND required approval is valid, fresh, scoped, and unused
  AND request conforms to schema and platform limits
  AND rate/concurrency/risk budgets allow dispatch
  => Control Plane may send

Sentinel then independently repeats:
peer authentication + freshness + local policy + resource + approval
+ schema + limits + current state
  => Sentinel may execute
```

Default is deny. Discovery never grants permission. A Control Plane allow decision never forces Sentinel to execute. A Sentinel capability being available never bypasses user authorization.

Protocol-control messages such as handshake, heartbeat, and bounded self-health are authenticated protocol functions, not managed-resource capabilities. They cannot access Docker, host inventory, files, processes, devices, or external integrations. All managed-resource observations, including inventory snapshots, use capability requests.

## Policy inputs

Control Plane policy considers:

- Principal identity, tenant, roles, and explicit grants.
- Origin: UI, API client, module, Worker, Automation Engine, or AI Brain.
- Node and resource ownership/tags.
- Capability version, effect, and risk.
- Time, maintenance window, current incident state, and node health.
- Approval evidence and separation-of-duty requirements.
- Job, workflow, and previous-step state.
- Rate, concurrency, and cumulative risk budgets.

Sentinel local policy considers:

- Authenticated Control Plane identity and policy decision reference.
- Installed/healthy adapter and locally allowed capability versions.
- Node-local resource allowlists and protected resources.
- Request freshness, replay state, approval binding, and idempotency.
- Local maintenance/drain/emergency-stop state.
- Quotas, dependency health, audit durability, and safe preconditions.

Sentinel local policy has two layers:

1. A locally installed safety baseline defining protected resources, hard limits, emergency state, trusted issuers, and maximum capability scope.
2. A signed operational overlay distributed by the Control Plane and constrained by that baseline.

The effective decision is the most restrictive result. Remote policy can reduce permissions immediately but cannot silently widen the local baseline. Policy revisions are monotonic, rollback-protected, expiry-aware, and recorded in health and audit.

## Roles and capabilities

Roles are collections of conditional grants maintained by the Control Plane; they are not sent as authority to Sentinel. Example roles such as Viewer or Operator may simplify administration, but enforcement resolves to explicit capability/resource decisions. Critical capabilities should support separation of duties and just-in-time elevation rather than permanent administrator grants.

## Module use

A BigBrain module declares the exact capability versions it consumes. Installation does not grant them. Deployment validation compares module requirements with policy and Sentinel discovery. Modules call an internal Control Plane application contract; they never address Sentinel directly.

The Control Plane records which module initiated a request and prevents confused-deputy behavior by evaluating both the user and module identity.

## AI Brain use

AI Brain receives a filtered tool catalog derived from:

1. Capabilities required by the current task.
2. Capabilities available on the selected node.
3. Capabilities the authenticated user and Brain policy may propose.
4. Risk and approval constraints.

The model never receives credentials or protocol access. Model output is untrusted data validated against a tool schema. Brain may propose a plan, but a deterministic policy engine authorizes dispatch. High/critical-risk actions require fresh human approval bound to the exact capability, node, resource, arguments hash, expiry, and plan revision. Any changed argument invalidates approval.

AI context should expose opaque resource identifiers and redacted summaries, not secrets or unrestricted filesystem/log content. Token and action budgets limit iterative tool use.

Read capabilities are not automatically safe for AI. Logs, filenames, process metadata, Home Assistant state, and file content can contain prompt injection or private data. Each capability descriptor therefore declares whether its result may enter model context, which fields require further reduction, and the maximum retained provenance. Sentinel performs node-side redaction; the Control Plane performs a second AI-context policy decision.

## Automation Engine use

Automation rules are versioned principals with an owner, enabled state, trigger, conditions, capability allowlist, node/resource scope, schedule, rate budget, and expiry. Rules do not inherit the creator's future permissions silently.

At every run the Automation Engine obtains a new policy decision. Mutations require capability-specific automation eligibility; critical actions normally remain human-approved. Loop detection, cooldown, maximum executions, incident circuit breakers, and idempotency keys prevent event-action storms.

Automation identity and user identity remain distinct in delegation and audit. Disabling a rule, revoking its owner, changing its capability set, or changing policy invalidates outstanding dispatch authority. An event emitted by a Sentinel is untrusted input to rule evaluation and cannot serve as approval.

## Capability lifecycle

`draft -> experimental -> supported -> deprecated -> disabled -> removed`

Experimental capabilities are off by default and cannot be used by AI or unattended automation unless policy explicitly allows the experimental state. Capability changes require threat modeling, schema review, contract tests, audit review, and compatibility documentation.
