# BB-131F — Local Reasoner Runtime Isolation & Control Foundation

Detta är en sanerad GitHub-version. Synthetic engineering proof only; no raw prompts/provider
payloads, credentials, private host identifiers or production research data are published.

## Metadata

- Date: 2026-09-29.
- Baseline: `ddcf8011aefae4bd6df708421f02d6ede293fdcc`.
- Branch: `bb-131f/local-reasoner-runtime-isolation`.
- Authority: owner-authorized F plus explicit owned-child lifecycle clarification after the
  [recorded architecture stop](bb-131f-runtime-ownership-assessment-20260929.md).
- [ADR 0040](../../../adr/0040-local-reasoner-owned-worker-lifecycle.md): **Proposed**, not Accepted.
- ADR 0038/0039 remain Accepted; A–E acceptance and BB-130 closure remain historical source of truth.
- Finance: **RESEARCH / 0 SEK / NONE**. AI MAY PROPOSE. DATA MUST PROVE. RISK MAY VETO.

## Status

**IMPLEMENTED / AUTOMATICALLY VERIFIED / REVIEW CANDIDATE ONLY**. Not accepted, merged, deployed or
runtime-authorized. No model selected/installed/downloaded/benchmarked/invoked, provider or SDK.
Final candidate identity is the exact published branch tip; its parent must equal the baseline.
Candidate publication does not authorize model use or the next checkpoint.

## Recovery and ownership

The first interruption left only preflight. This continuation preserved the unrelated mockups and
unpublished Sentinel ADRs, created the one correctly based F branch and recovered 136 passing B/C/E
characterization tests. Five local documentation files recorded a justified architecture stop.
The owner/architect then authorized only an application-owned child lifecycle. Those findings are
preserved as dated history; no branch recreation, reset, clean, stash or rebase occurred.

Brain remains an orchestration library with no API registration or Finance storage access.
`LocalReasonerRuntime` implements the existing `IResearchReasoner`; the internal
`OwnedReasonerWorker` is not a host/process service. Trusted immutable application options fix
executable/arguments and coordination location. Projection/reply data cannot choose them. There is
no arbitrary PID/handle attachment, process enumeration, shell/script executor, process-tree kill,
service locator, public endpoint or Sentinel mutation. The worker executable is in a test-only project.

A retained Linux pidfd is acquired only for the child created by this wrapper. The wrapper checks
that the retained .NET child has not exited after acquisition before using the descriptor. If the
child was reaped before acquisition, that descriptor is discarded rather than trusting a recycled
PID. Later termination uses only the retained descriptor, never a PID lookup. Missing identity
fails closed, preserves reservation if stopping cannot be confirmed, and does not fall back to kill(PID).
Exit/disposal ends authority. Repeated disposal/stop cannot attach to another process.

Implementation references: [.NET Process Unix source](https://github.com/dotnet/runtime/blob/release/10.0/src/libraries/System.Diagnostics.Process/src/System/Diagnostics/Process.Unix.cs),
[Linux pidfd_open](https://man7.org/linux/man-pages/man2/pidfd_open.2.html),
[Linux pidfd_send_signal](https://man7.org/linux/man-pages/man2/pidfd_send_signal.2.html).
These informed identity-bound signaling; .NET Process.Kill is not used by the implementation.
F requires Linux kernel pidfd support and glibc pidfd exports (glibc 2.36+); unavailable primitives
fail before dispatch where probed. No package/runtime installation is performed.

## Transport and limits

The owned child inherits anonymous stdin/stdout pipes created by .NET ProcessStart. There is no
listening socket, TCP/HTTP, socket pathname/ACL or remotely routable endpoint. The pipe endpoints
are created with the child; there is no independent endpoint that a different peer may advertise.
Sentinel's UDS/mTLS pattern was inspected but is unnecessary for an inherited one-child channel.
This does not authenticate a human user or sandbox a malicious same-UID/native process.

One request and one reply use an 8-byte header: ASCII `BRF1` and a signed big-endian positive payload
length. The maximum is 65,536 UTF-8 bytes each way; wrong magic/version, non-positive/excessive length,
truncated header/payload or any byte after the single frame reject. Length is checked before payload
allocation. EOF is required after the sized frame, under the deadline; this is not unbounded EOF parsing.
Invalid UTF-8 rejects. Stderr is never logged/buffered: any first byte fails the proof protocol.

Only Finance's `LearningDevelopmentInput` crosses the input boundary. Variable text members are
bounded before serialization (80 characters per existing identity/contract field); full scope,
ledger snapshots, result evidence and holdout are never serialized. The proof worker validates the
closed canonical projection including fixed limits/history and duplicate properties, depth 8.
Replies use the unchanged Finance `LearningReplyParser`; there is no parallel proposal parser or
repair/default/truncation loop. Parsed replies still require current Finance admission.

Default invocation limit 30 seconds (existing B ceiling), configurable only downward to 100 ms.
This covers sending, receiving and child exit. Grace defaults 250 ms (range 50–1,000 ms), followed
by forced termination and confirmed-exit wait default 2 seconds (range 100–2,000 ms). These are
conservative engineering bounds, not model performance or hardware capacity measurements.

## Single-flight, cancellation and recovery

An instance rejects concurrent calls rather than queueing. Before child start, an exclusive
`CreateNew` reservation is written/flushed in a pre-provisioned private directory (0700; file0600).
All controllers for this application runtime must use that same trusted directory. Existence rejects;
content is never treated as freshness or instructions. This filesystem acquisition, not an in-process
lock alone, arbitrates competing controllers/processes. Configuring another directory is trusted
application reconfiguration, not a reasoner capability or valid scientific reset.

Reservation is released only after confirmed owned-child exit and successful terminal audit delivery.
A controller crash, uncertain termination or failed terminal audit leaves a stale reservation.
Restart never auto-clears it. Operator recovery must establish worker absence and reconcile audit
under separately authorized operations; F supplies no generic recovery kill/PID API. This is a
single bounded operational lease, not a scientific ledger, event store or second Finance database.
Power-loss/filesystem rollback and malicious administrative deletion are not solved by this lease.

Cancellation/timeout closes the invocation path and triggers SIGTERM on the retained child identity;
if it does not exit within grace, SIGKILL targets the same identity. Unconfirmed stop keeps the
reservation and disables further work. Late responses are discarded. No child-tree traversal or
control of API/Finance/Sentinel/unrelated processes. No automatic retry, wider context or cloud fallback.
Operational failures disable that runtime instance; explicit trusted recomposition is required.
The one-way `Disable` switch cancels active work and prevents new dispatch. Dispose awaits cleanup.
Default Enabled is false; no external mode/adapter exists.

Hard CPU/RAM/GPU limits, a distinct low-privilege OS identity, filesystem/network/seccomp/container
sandboxing, descendant containment and host supervision are NOT implemented or certified. F tests
an application process boundary, not arbitrary native-code containment. No host deployment changes.

## Audit and caller boundary

A required typed audit sink receives start intent, response-header observation and terminal metadata:
new invocation UUID, Local/BRF1/controller version, actual serialized development-input SHA-256,
trusted worker-configuration digest, UTC/elapsed time, closed outcome/failure codes and response digest
only for parsed replies. No PID/path, raw exception, arbitrary prompt/response/prose or holdout.
Worker configuration identity is not cryptographic attestation of the entire runtime artifact bundle.

Sink failure before dispatch prevents child creation; failure at completion prevents returning a
reply and retains the slot. The sink is trusted controller composition, never passed to the worker.
It must be bounded/non-reentrant. The typed record can be projected to existing structured logging,
but F does NOT register a logger or invent a new datastore. Tests collect sanitized records only.
Durable intent/terminal persistence, retention, tamper resistance and crash reconciliation of audit
remain explicit first-model gates. The lease is not a durable invocation audit and does not replace it.

CallerKind explicitly says InternalWorkloadUnattested: no authenticated human is invented.
Public invocation, principal/delegation permissions, production audit and local data-use rights remain
separately reviewed prerequisites. The proof uses only self-owned synthetic fixtures; no real market
or provider data, additional entitlement or external export. Core deterministic Finance is independent.

## Finance and scientific invariants

The integration reuses E's test-only orchestration and C's existing owner:
enroll -> reserve invocation -> runtime/proof worker -> existing parser -> C proposal admission /
trial-exposure reservation -> StartLearningExecution -> existing DeterministicRobustnessEvaluator ->
existing immutable persistence/completion. No source in Finance, its schema/ledger, strategy, costs,
fills, OOS/holdout, classification, historic identity/checksum or entitlement policy changed.

One synthetic instrument, momentum/v1 period20, internal 5/10/20 effective trials, one invocation,
no retry and <=64 underlying runs remain. Native results are not relabeled. NoUsefulProposal is a
normal reply with zero engine calls. Invalid transport/reply, operational failure and cancellation
retain C's spent invocation without execution/refund. C timeout/unavailable codes are reused by the
test harness. DENY/HALT/INSUFFICIENT_DATA/missing risk evidence still veto; worker prose cannot grant
risk ALLOW or PAPER/LIVE/AUTO. Existing direct Finance remains usable without a reasoner.

## Evidence

Final gate results below correspond to the completed code tree. No old baseline run substitutes for
final-tree verification. Earlier characterization: B/C/E 136/136 PASS. Development F 54/54 and
combined F/B/C/E 190/190 PASS before the last bounded test/cleanup additions.

New tests use real model-free processes and cover disabled/pre-cancelled start, bounded configuration,
competing controllers/stale lease, cancellation/disable/dispose, timeout/hang/non-reading worker,
SIGTERM-ignore then force/reap, late valid response after cancellation, premature exit, header/length/
truncation/UTF-8/trailing errors, request shape/size, existing JSON duplicate/unknown/depth/null/missing/
strategy/parameter/evidence rules, unknown tool/PID/executable/signal/risk/trading fields, inert attack
prose, sanitized audit failure, retained-handle lifetime and unrelated-child survival. End-to-end
synthetic C/engine/result/risk and no-engine decline/failure tests preserve B/C/E.
Actual OS PID exhaustion/reuse is not forced; retained-kernel-identity behavior and absence of
reattachment are inspected, with repeated/exited/disposed ownership tested.

Final review also added cancellation at the terminal-audit boundary and retained confirmed-exit
state after disposal. Late delivery cannot succeed merely because parsing already completed.
Development-only issues corrected: test type/property names, existing analyzer requirements,
report sanitization wording and a version-tag URL matching the report private-address filter. No pre-existing correctness/security/scientific/lineage defect reproduced.
No full suite rerun after every edit; focused tests preceded final verification.

### Final verification — 2026-09-29

All final commands exited 0; no failed or skipped tests:

| Command / inspection | Actual result |
| --- | --- |
| `dotnet restore BigBrain.slnx` | PASS, complete solution including new proof project/references |
| `dotnet format BigBrain.slnx --verify-no-changes --no-restore` | PASS |
| `dotnet build BigBrain.slnx --configuration Release --no-restore` | PASS, 0 warnings / 0 errors |
| Focused command below | 193/193 PASS: F 57, E 46, B 56, C 34 |
| `dotnet test BigBrain.slnx --configuration Release --no-build --logger 'trx;LogFilePrefix=bb131f-final'` | API 857/857 and Sentinel 32/32 PASS |
| `node scripts/verify-documentation.mjs` | PASS, 257 Markdown files / 91 unique backlog IDs, relative links/indexes |
| `git diff --check` / staged equivalent | PASS |
| Gitleaks `git --log-opts=--all --redact --no-banner` | PASS, 293 pre-candidate commits, no leaks |
| Gitleaks `git --pre-commit --staged --redact --no-banner` | PASS, exact staged candidate content, no leaks |
| Baseline diff over Finance/API/Sentinel/Web/compose/CI | Empty; no production scientific/ledger/schema/provider/runtime-composition change |

```text
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-build --filter 'FullyQualifiedName~LocalReasonerRuntimeTests|FullyQualifiedName~ResearchReasonerContractTests|FullyQualifiedName~ResearchLearningContractTests|FullyQualifiedName~FinanceLearningLedgerTests'
```

Final TRX inspection confirms existing scientific subsets inside the full run: deterministic
backtest10, robustness13 (BB-123/124), research-dataset9 (BB-127), campaigns10/autonomous15 (BB-129),
backtest persistence4 and risk13: 74/74 PASS. Additional dataset identity/intake/revision, migrations,
operations/governor/scheduler and all C governance tests also pass in the full suite. The only E test
changes expose its test harness for reuse, permit added Brain runtime types and map the new timeout
exception to the existing C timeout code. No Finance policy/parser/engine or B/C test is rewritten.
Frontend is proven untouched by the baseline diff; no extra frontend work or model-dependent tests.
Branch push is not main CI, deployment, runtime acceptance or first-model authorization.

## Changes

The candidate's exact Git inventory is authoritative. Production changes are confined to the Brain
library: narrow controller/options/audit types, framed transport, internal owned-worker and updated
port comment. Solution/test references add the non-packable test-only proof executable. E test helpers
are shared explicitly; their port assertion now permits F runtime types, retaining zero API/SQLite/HTTP
references. B/C tests and implementation remain unchanged. Relevant canonical docs, Proposed ADR0040,
indexes, this report and preserved historical stop assessment accompany them.

No Web, API composition, Finance source, Sentinel, package dependency, schema/migration, CI,
Docker/host/provider/deployment configuration change. No production proof-worker registration.

### Exact candidate file inventory

- `ARCHITECTURE.md`
- `BigBrain.slnx`
- `ROADMAP.md`
- `TESTING.md`
- `docs/BACKLOG.md`
- `docs/STATUS.md`
- `docs/adr/0040-local-reasoner-owned-worker-lifecycle.md`
- `docs/architecture/finance/local-first-reasoner-boundary.md`
- `docs/architecture/finance/master-roadmap.md`
- `docs/indexes/adr.md`
- `docs/indexes/documentation.md`
- `docs/modules/finance.md`
- `docs/operations/codex-recovery.md`
- `docs/reports/REPORT-CATALOG.md`
- `docs/reports/features/finance/bb-131f-local-reasoner-runtime-isolation-20260929.md`
- `docs/reports/features/finance/bb-131f-runtime-ownership-assessment-20260929.md`
- `docs/security/finance-threat-model.md`
- `src/BigBrain.Brain/BigBrain.Brain.csproj`
- `src/BigBrain.Brain/IResearchReasoner.cs`
- `src/BigBrain.Brain/LocalReasonerProtocol.cs`
- `src/BigBrain.Brain/LocalReasonerRuntime.cs`
- `src/BigBrain.Brain/LocalReasonerRuntimeOptions.cs`
- `src/BigBrain.Brain/OwnedReasonerWorker.cs`
- `tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj`
- `tests/BigBrain.Api.Tests/LocalReasonerRuntimeTests.cs`
- `tests/BigBrain.Api.Tests/ResearchReasonerContractTests.cs`
- `tests/BigBrain.Reasoner.ProofWorker/BigBrain.Reasoner.ProofWorker.csproj`
- `tests/BigBrain.Reasoner.ProofWorker/Program.cs`

## Security

No models, inference runtimes, paid dependency, external AI call, provider credential, data export,
network listener, broker, order, PAPER/LIVE/AUTO or capital authority. Test fixtures never evaluate
SQL/code/URLs/shell text. Trusted launch configuration is not model input. No external fallback exists.
Finance remains **RESEARCH / 0 SEK / NONE**. No deployment or owner/runtime acceptance claimed.

## Remaining work

Independent review of exact F SHA including Proposed ADR0040. First actual model still needs separate
authorization, provenance/immutable artifact bundle, isolated OS identity and filesystem/network/
descendant controls, measured/enforced hard resource limits, durable audit, authenticated workload/
principal scope, affirmative local-use rights and approved runtime/model choice. External provider,
export/retention/egress/cost gates remain separate optional future scope. Weak model quality cannot
weaken Finance or trigger paid fallback. No next checkpoint is authorized by publication.

## Resumption

The [canonical recovery record](../../../operations/codex-recovery.md) owns unpublished continuity.
After publication: STOP — return the exact BB-131F candidate SHA, including the Proposed owned-worker
lifecycle ADR, to owner/architect for independent review. No merge, deployment, model or next checkpoint.
