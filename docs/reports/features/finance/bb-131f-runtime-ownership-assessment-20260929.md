# BB-131F — Runtime ownership assessment

Detta är en sanerad GitHub-version. Repository evidence only; no raw research/provider payloads, credentials or host identifiers.

## Resolution — 2026-09-29

The owner/architect reviewed this stop and explicitly permits narrowly owned child-worker lifecycle
without generic Brain process control or Sentinel mutation. [Proposed ADR 0040](../../../adr/0040-local-reasoner-owned-worker-lifecycle.md)
records the clarification for F review, not architecture acceptance. Implementation continued on the
same branch, preserving the original five local documentation changes and unrelated files.
[Current F implementation and evidence](bb-131f-local-reasoner-runtime-isolation-20260929.md).
The remaining sections below describe the earlier stop as historical evidence; its next-action and
publication statements no longer describe the current candidate. No defect was retroactively inferred.

## Metadata

- Date: 2026-09-29.
- Authorized baseline / HEAD / fetched origin/main: `ddcf8011aefae4bd6df708421f02d6ede293fdcc`.
- Branch: `bb-131f/local-reasoner-runtime-isolation`, created from that exact baseline.
- Scope: interrupted-run recovery, existing-source characterization and architecture stop assessment.
- Finance: **RESEARCH / 0 SEK / NONE**.

## Status

**STOP FOR ARCHITECTURE REVIEW — NOT IMPLEMENTED / NOT A REVIEW CANDIDATE.**
The F implementation authorization remains valid, but its own architecture stop condition applies
before choosing a process-lifecycle owner. No new ADR or accepted-contract amendment is made here.
This is not a reproduced pre-existing software defect and is not a BLOCKER HANDOFF candidate.
No commit/push: authorization covers a completed F candidate, which does not exist yet.

Recovery established that the interrupted session left preflight only. There was no local or remote
F branch, F source/test/document change or F commit. The tracked tree was clean; HEAD and origin/main
matched the authorized baseline. The recovery document described completed E and made no contrary
claim about F implementation. The current continuation created the requested branch without resetting,
recreating or discarding work. Unrelated mockups and unpublished Sentinel ADR 0006–0009 remain excluded.

## Evidence

The controlling accepted requirements are concrete:

1. [ADR 0039](../../../adr/0039-local-first-research-reasoner-boundary.md) incorporates the accepted
   [detailed local-first contract](../../../architecture/finance/local-first-reasoner-boundary.md).
   Its **Local inference isolation** section says:
   > Provisioning, resource policy, start/stop and any GPU device allocation belong to separately authorized operations through accepted host/Sentinel boundaries. Brain does not inspect/control processes, GPU, containers or host files.
2. [ADR 0003](../../../adr/0003-architecture-freeze-sentinel-v1.md), **Frozen invariants**, assigns
   node-local system access exclusively to Sentinel and says Sentinel v1 is read-only; mutations
   belong to later, separately approved architecture versions. Its evolution rule prohibits routine
   documentation edits that broaden authority or reinterpret a released contract.
3. F requires a runtime controller to create/own a separate worker and terminate only that worker.
   F section 3 explicitly requires a stop if a new architectural decision beyond ADR 0039 is needed.
   It does not expressly amend the accepted allocation quoted above. Whether owned-child supervision
   is an exception to that allocation must be resolved explicitly rather than inferred in code.

Actual source inspection:

| Boundary | Implemented evidence | Consequence |
| --- | --- | --- |
| `src/BigBrain.Brain/IResearchReasoner.cs` and project | Interface-only library; no process owner, runtime adapter or API registration | Adding start/kill authority here needs the ownership clarification above. |
| `src/BigBrain.Api/Sentinel/SentinelClient.cs` | Fixed Unix-domain HTTP/2 transport, TLS 1.3, pinned mutual certificates and request proof | Useful local trust precedent, not a worker lifecycle capability. |
| `src/BigBrain.Sentinel/Program.cs`, capability registry | Read-only metrics/snapshot protocol plus health; no reasoner-worker start/terminate capability | Reusing Sentinel cannot silently add mutating lifecycle scope. |
| `ResearchLearningReply.cs` / admission | Finance-owned strict complete-string parser, 64 KiB/depth 8; separate scientific admission | Receive-time allocation limits are still required in F; parser correctness does not provide them. |
| `FinanceLearningLedger.cs` | Finance-owned durable scientific reservation/history/result references | Not an operational process-audit owner; must remain unchanged. |
| API/Sentinel logging | Existing operational ILogger/JSON console patterns; Finance-specific risk audit | No inspected general durable reasoner audit store. Do not misuse Finance stores or claim console logs meet durable intent/terminal audit. |
| `compose.yaml`, API composition | Existing API/Web/Sentinel deployment; no reasoner runtime or public reasoner endpoint | No deployment or composition change is made. |

B/C/E characterization command:

```text
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --filter 'FullyQualifiedName~ResearchReasonerContractTests|FullyQualifiedName~ResearchLearningContractTests|FullyQualifiedName~FinanceLearningLedgerTests'
```

Result: **PASS, 136/136, zero failed/skipped**, exit 0; Release restore/build dependencies succeeded.
These are 46 E, 56 B and 34 C existing tests; no source/test changes were made.
The first sandbox attempt could not create MSBuild IPC sockets; it was rerun with approved
execution permissions. This is an execution-environment failure, not a reproduced product defect.
No full F/security suite exists, and no claim of F verification follows from B/C/E regression results.

Local handoff validation:

- `node scripts/verify-documentation.mjs`: PASS, 255 Markdown files / 91 unique backlog IDs,
  including relative links/indexes. Initial missing mandated sanitization sentence was corrected.
- `git diff --check`: PASS. Only the five intended Markdown paths changed; nothing staged.
- Gitleaks v8.28.0 `git --log-opts=--all --redact --no-banner`: PASS, 293 commits, no leaks.
- Gitleaks `dir <isolated copies of the five intended documents> --redact --no-banner`: PASS,
  no leaks; unrelated untracked files were not copied, changed or added.
- Full solution tests/build/formatter and F runtime/security tests were not run: implementation
  stopped before source changes. Focused existing characterization is not substituted for F DoD.

README, ROADMAP, TESTING, architecture/ADRs, Finance module/contracts/master roadmap, security and
runbooks were assessed for update scope. Their accepted behavior remains unchanged; the current
stop and pending work belong in STATUS/BACKLOG, this assessment/catalog and the recovery note.

## Changes

Only this report, report catalog, STATUS, BACKLOG and the single canonical recovery note are updated
locally. No production source, test, project/package, schema, scientific engine, CI, Web, runtime or
deployment file changes. Accepted ADRs and historical A–E evidence remain unchanged.

## Security

No model selected, installed, downloaded, benchmarked or invoked. No inference runtime/AI SDK/provider,
external export, credentials, public listener, process-control implementation or host service change.
No PAPER/LIVE/AUTO, broker, orders, capital or risk authority. Finance remains independent and
**RESEARCH / 0 SEK / NONE**. Unrelated local files are preserved, not scanned into a candidate.
No OS sandbox, hard CPU/RAM/GPU limit, receive limit or worker termination guarantee is claimed.

## Remaining work

Architect decision required: name the component allowed to create and terminate the *owned proof
worker*, and state its relationship to ADR 0039's no-process-control rule and ADR 0003's node boundary.
Two bounded alternatives for review, neither selected or implemented:

- Explicitly permit only owned-child lifecycle in a narrowly scoped local runtime controller,
  while keeping host provisioning, arbitrary PID control, services/containers and GPU policy outside
  Brain. State whether this is an accepted clarification or requires a new ADR.
- Keep Brain strictly request-only and identify an accepted separate lifecycle owner and protocol.
  Do not assume read-only Sentinel v1 can be extended for this without a new decision.

A process boundary alone is not native-runtime sandboxing. Hard resource enforcement, durable audit,
workload authentication and first-model rights/isolation acceptance can remain explicit later gates
where F permits; those deferred gates alone are not the reason for this stop.

After ownership is resolved, continue the existing F branch: select bounded local transport, implement
controller/framing/owned termination and deterministic proof worker, add security/C-integration tests,
run all original F final gates, document actual controls and limits, publish one complete candidate.
Do not replace required production control proof with a test-only supervisor and call F complete.

## Resumption

Read [the single recovery note](../../../operations/codex-recovery.md), verify the baseline/working tree,
and preserve these valid findings. No implementation needs recreation. No candidate SHA exists.
Exact next action: STOP — return the lifecycle-ownership question to owner/architect for clarification
before F implementation. Do not merge, deploy, invoke a model or start another checkpoint.
