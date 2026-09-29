# ADR 0040: Local Reasoner Owned-Worker Lifecycle Boundary

- Status: Accepted
- Date: 2026-09-29
- Checkpoint: BB-131F
- Baseline: `ddcf8011aefae4bd6df708421f02d6ede293fdcc`
- Authority: explicit owner approval of reviewed candidate `1e003705c08000f33b97f3d0d5dc7f35546e4d1e`
  includes this bounded clarification; merged unchanged with green main CI on 2026-09-29.

## Context

F exposed an ownership ambiguity: ADR 0039's detailed contract assigns host start/stop outside Brain,
while Sentinel v1 remains read-only. F needs to stop exactly its own deterministic proof worker.
The [assessment](../reports/features/finance/bb-131f-runtime-ownership-assessment-20260929.md)
records the original stop. The owner/architect resolved it without granting generic host control.

## Accepted decision

The local reasoner runtime may own a narrowly scoped child-worker lifecycle. It starts only its
configured executable with fixed trusted arguments, retains the internally created child identity,
observes/waits for exit, requests termination, then force-terminates after bounded graceful waiting,
and reaps/disposes that same child. Neither Finance input nor worker output selects executable,
arguments, PID, process name, signal, working directory, environment or lifecycle operation.

Brain gains no generic process administration. No StartProcess/KillProcess service, process
enumeration, arbitrary PID/handle attachment, shell/script, service locator, Docker/systemd access
or host-control interface is exposed. Authority comes only from the runtime's owned child handle;
exit/disposal ends it. Never reacquire authority by a recycled PID. Linux identity-bound signaling
must use a retained pidfd, with owned-child liveness verified when acquired, not later PID lookup.

Sentinel v1 is unchanged/read-only. Application-owned child supervision is not general node
administration and must not be routed through an invented Sentinel mutation. Host provisioning,
OS credentials/isolation, cgroup/systemd/container policy and GPU control remain separately reviewed
operations. This exception does not permit descendants, process-tree enumeration or arbitrary kills.

One configured application runtime uses inherited anonymous stdin/stdout pipes for one framed
request/response. There is no listening endpoint, pathname to impersonate, TCP/HTTP or new RPC/tool
channel. Kernel-created pipe handles link the exact spawned child; same-UID hostile process and
native-worker compromise require later OS isolation. A private coordination directory holds one
exclusive reservation file before spawn, shared across controllers/processes using that configured
runtime. A crash/uncertain stop leaves it occupied until reviewed operator recovery; no auto-clearing.

Default disabled; one active invocation, bounded data/deadlines, no queue/retry/cloud fallback.
Finance still owns projection/admission/scientific reservation/results/risk. Process controls never
refund C governance or authorize trading. No model/provider/network or deployment authority follows.

## Alternatives and consequences

- Generic Brain process API or Sentinel v1 mutation: rejected.
- Assume a Process object alone protects Linux PID reuse: insufficient for signaling; retain an
  identity-bound kernel handle and fail closed if identity cannot be established.
- Native model in API/Finance: rejected.
- OS sandbox/cgroup hard CPU/RAM/GPU guarantees: future first-model gates, not proven by F tests.
- Durable identity-bound operational audit integration remains a first-model gate if existing
  logging has no suitable durable owner; no second Finance store is authorized.

F contains only a deterministic test/proof worker. No real reasoner/model is selected or invoked.
ADR 0038/0039 scientific/local-first requirements and Sentinel frozen scope otherwise remain intact.
Finance RESEARCH / 0 SEK / NONE. No deployment.
