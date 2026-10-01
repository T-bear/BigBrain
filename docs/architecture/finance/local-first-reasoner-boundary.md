# BB-131D — Local-first reasoner boundary and security contract

## BB-132C — local-session compatibility review, 2026-10-01

**REVIEW CHECKPOINT / INCOMPLETE / NOT A MERGE CANDIDATE.** C is now authorized from fetched
main `cab482c49d2187a3f39c13528a5fb88d899f6b8f`; prior dated C-not-authorized entries are history.
[Exact baseline/tree, model-free integration evidence and remaining decision](../../reports/features/finance/bb-132c-local-sequence-compatibility-20261001.md).
Existing finite session can bind IResearchReasoner/LocalReasonerRuntime directly without a second
orchestration path. Model-free proof connects owned BRF1 child, N evaluation/native persistence,
reopen, N+1 bounded history and no second evaluation. Production implementation remains unchanged.

Compatibility stop before inference: B's fixed30s parent deadline overrides A's controlled180s
runtime; A's historical successful reply took50.458s. Native prompt still unconditionally asserts
empty history, contradicting evaluated N+1's existing projection. No performance prediction or
pre-existing science defect claimed. No deadline/prompt/history/parser fix made without review.
Next: review narrowly scoped deadline/prompt compatibility before consuming the conditional real
session. Real session NOT CREATED; real invocations0/2; A grants/journals untouched.
Verification results are maintained in the report. Finance **RESEARCH / 0 SEK / NONE**.
No inference/model/provider/cloud/GPU, trading/broker/orders/capital/deployment or BB-132D.


## BB-131F local runtime control — 2026-09-29

**ACCEPTED / MERGED / CI VERIFIED**.
Owner-authorized from `ddcf8011aefae4bd6df708421f02d6ede293fdcc`; the explicit architecture
clarification permits only the runtime's owned child lifecycle. ADR 0040 is **Accepted**.
[Implementation, exact evidence and limitations](../../reports/features/finance/bb-131f-local-reasoner-runtime-isolation-20260929.md).
The Brain library adds disabled-by-default local control, inherited framed pipes, receive limits,
exclusive reservation, cancellation/deadline and retained-pidfd termination of its own proof child.
No generic process authority, Sentinel change, API registration, Finance engine/ledger/schema change,
model/provider/SDK, external export or deployment. The deterministic executable exists only in tests.
Operational records are sanitized; durable audit integration and hard CPU/RAM/GPU/native sandboxing
remain first-model gates. Finance **RESEARCH / 0 SEK / NONE**. No paid AI dependency or cloud fallback.
A–E remain accepted, BB-130 closed. No model or next-checkpoint authority follows from publication.
Earlier authorization/status statements below retain their dated scope.


## BB-131E local reasoner contract — 2026-09-28

**ACCEPTED / MERGED / CI VERIFIED.**
Owner-authorized from `e2f9cd761d0c8ed7569939b9ada908e6de37cf3f` on
`bb-131e/local-reasoner-contract`. [Implementation and evidence](../../reports/features/finance/bb-131e-local-reasoner-contract-20260928.md).
A minimal Brain contract library references Finance's existing development input and closed reply.
Finance owns the shared B parser and unchanged scientific admission; C still reserves invocation,
trials/exposure and execution. Only test code implements the reasoner and orchestration.
No API registration/reference to Brain, model, inference, external SDK/provider/export or deployment.
Finance **RESEARCH / 0 SEK / NONE**. ADR 0038/0039 remain Accepted; BB-130 closed; A/B/C/D accepted.
E is accepted and merged as a contract foundation, not runtime-authorized. Auth/audit/transport/resource/kill-switch isolation,
model provenance/capacity and first-invocation review remain prerequisites. No paid AI dependency
or cloud fallback. No subsequent checkpoint is authorized. Older D authorization statements below
are dated history and do not override this separately authorized E scope.


- Drafted: 2026-09-23; resumed/reviewed: 2026-09-28.
- Baseline: `269fbd7e786a09ba2bf39fe77492d62d8056a52a`.
- Status: **ACCEPTED / MERGED / CI VERIFIED**.
- Decision: [ADR 0039 — Accepted](../../adr/0039-local-first-research-reasoner-boundary.md).
- Parent authority: [ADR 0038 — Accepted](../../adr/0038-finance-research-learning-authority-contract.md).
- [Assessment, recovery and verification](../../reports/features/finance/bb-131d-local-first-reasoner-boundary-20260923.md).

This is an accepted security design contract, not implemented controls or permission to invoke a model.
BB-130 stays closed; A/B/C stay accepted. BB-131E is NOT STARTED / NOT AUTHORIZED.
**AI MAY PROPOSE. DATA MUST PROVE. RISK MAY VETO.** Finance **RESEARCH / 0 SEK / NONE**.

## Product decision and evidence limits

The primary future path is a BigBrain-hosted local reasoner with no paid API, subscription,
cloud account, licence activation service or external inference dependency. An optional hosted
adapter is a separate future capability. Absence of cloud credentials must not impair the local
contract. Zero SEK means no required model/API purchase; existing hardware, electricity and
operations are not claimed to have zero physical cost. No particular runtime/model is selected.

Model quality affects useful proposals, never safety or scientific validity. A weak model may
return NoUsefulProposal or be rejected. It is not allowed more authority, protected evidence,
relaxed admission or paid fallback to improve its answer. Further iterations require independently
authorized protocol budgets; the current one-invocation protocol cannot be retried until it succeeds.
No promise of useful output or acceptable latency on the current hardware is made.

### Hardware characterization

The owner supplied an approximate i5-6600 CPU, 8 GB RAM, GTX 1050 Ti / 4 GB VRAM class.
A search of tracked repository Markdown found no confirming hardware inventory. Treat these as
**owner-reported planning constraints, not verified current hardware/free capacity**. No host probe,
benchmark, model download, GPU compatibility test or inference was performed. RAM/VRAM nominal
capacity is not capacity available to inference alongside BigBrain and other services.

Future selection must demonstrate offline operation, permissive-enough usage terms, bounded
memory/context/output, cancellation and structured text output on the verified host. Small or
quantized CPU-capable candidates may be assessed later; GPU acceleration is optional and cannot be
assumed compatible. Hardware upgrades can improve capacity, but are not prerequisites for correctness.
If no tested model fits, keep reasoning unavailable rather than silently using cloud or exhausting
host resources. This is a viable provider-independent architectural path, not measured model viability.

## Source-backed ownership and reuse

Paths are repository-relative. This inspection is source analysis, not a penetration test.

| Current boundary | Actual state and consequence for D |
| --- | --- |
| `ARCHITECTURE.md`, `BigBrain.slnx` | Brain owns logical AI orchestration; there is no `src/BigBrain.Brain` project at baseline. D adds no project/service. |
| `src/BigBrain.Api/Program.cs` | General Add/UseAuthentication, Add/UseAuthorization and endpoint RequireAuthorization are not wired. Existing outbound provider authentication and Sentinel transport do not authenticate a Finance user. |
| `src/BigBrain.Modules/Finance/ResearchLearningContracts.cs` | `LearningDevelopmentInput` is an initial empty-history synthetic projection. `SyntheticLearningScope` contains protected full replay inputs; never serialize it to a reasoner. |
| `src/BigBrain.Modules/Finance/ResearchLearningAdmission.cs` | Existing pure admission: exact version/shape, duplicate keys, 64 KiB response/depth 8, momentum/v1 period 20, one variant/criterion, input binding, eligibility, budget and exposure. Reuse, do not replace. |
| `src/BigBrain.Api/Finance/FinanceLearningLedger.cs` | Fixed synthetic protocol/family; durable invocation, proposal, three-trial budget and exposure/start reservation. No authenticated runtime orchestration or generalized history projection. Private snapshot is not model input. |
| `src/BigBrain.Api/Finance/FinanceEndpoints.cs` | Human-facing reads are not a model projection. Full evaluation/selection/holdout output must not cross the reasoner boundary. |
| `src/BigBrain.Modules/Finance/MarketDataEntitlements.cs` | Exact use/product and Unknown/Allowed/Denied evaluation; no local-inference/external-export permission type exists. StrategyTraining is not automatically inference or export permission. |
| `src/BigBrain.Modules/Finance/ResearchDatasetEligibility.cs` | Purpose-specific limited research eligibility does not grant a new use, disclosure or retention right. |
| Existing backtest/robustness/risk/result stores | Remain the sole scientific authorities. Native verdicts and hashes are unchanged; MoreRobust is not promoted to RobustCandidate. |
| `tests/BigBrain.Api.Tests/ResearchLearningFixture.cs`, contract/ledger tests | Test-only reasoner and synthetic engineering/replay proofs. No real-model security, performance or quality claim. |

Finance owns evidence projection, rights/eligibility, exact scientific identity, admission, budget,
negative/duplicate/exposure history, deterministic engines, result persistence/verdicts and risk.
Brain owns only provider-neutral orchestration, deterministic prompt assembly, adapters, resource
controls and AI telemetry. It receives neither Finance database access nor full domain objects.
The model has zero tools. It is not an API client or scheduler. The trusted orchestrator alone
uses a normal authorized Finance application contract; future public routes, if separately approved,
remain versioned with Problem Details. This document defines no route.

## Request sequence and minimal port

```text
Authenticated, scoped initiator
  -> Finance: authorize evidence-read + reasoner-request; validate rights/protocol
  -> Finance: freeze permitted development projection and complete offered experiment scope
  -> Finance: durably reserve the one reasoner invocation; record request binding
  -> Brain: assemble fixed policy + exact permitted projection; acquire local resource slot
  -> isolated local inference: one bounded request -> one untrusted response
  -> trusted parser: Proposal | NoUsefulProposal | operational failure
  -> Finance: reauthorize proposal-submit, recheck current rights/policy, admit + reserve atomically
  -> existing deterministic evaluator -> existing immutable results + C ledger completion
```

Resource readiness may be checked before invocation reservation; a resource race after reservation
fails without refund. No inference without durable reservation and required audit. The model is
not involved between selection and holdout. All engine-owned 5/10/20 trials remain frozen/counted;
one caller variant is not one scientific trial. Maximum underlying runs remains 64, never shrunk.
The exact initial falsification remains `validation.excessReturn <= 0`, in existing fractional-return
units, with the existing sample policy. It is an engineering criterion, not significance/profitability
or a risk decision. No provider or model can change that criterion.

Documentation-only logical port: `ReasonAsync(ReasonerInput, Cancellation) -> ReasonerReply`.
`ReasonerInput` is the Finance projection, not a prompt string supplied by callers.
`ReasonerReply` is a closed Proposal or NoUsefulProposal; transport/resource errors are separate
operational outcomes, never synthesized proposals or scientific verdicts. A local and a future
external adapter implement the same semantics and the same untrusted-output checks. Adapter-specific
wire envelopes must not repair model JSON, fill missing fields or translate unknown aliases.
No arbitrary tool list, endpoint, prompt override, filesystem path or model-selected schema argument.

Trusted dispatch metadata (principal, invocation ID, deadline, adapter mode, model/runtime artifact
identity, policy/template version) stays outside the scientific proposal. Finance receives verified
transport/audit metadata separately; it never branches scientific admission on a vendor or price.
Existing `LearningIdentity`/execution fingerprint semantics are unchanged. Model/provider/request/
time/rationale cannot renew budget/exposure; a genuine plan change is still subject to the same
family history and cannot manufacture a new protocol. Projection checksums bind content, not authority.

## Finance-owned projection contract

First local-model proof should use exactly the existing synthetic `LearningDevelopmentInput`, under
separate authorization, and its one-proposal v1 output. It offers version/projection version,
opaque scope and target, development cutoff/count/checksum, momentum/v1 + period20, three effective
trials, input/history digests and fixed protocol limits. Development here includes the existing
training/validation segment and excludes protected holdout, matching B's source definition.
That v1 proof has **empty initial history only**; opening C history grants no second invocation.
BB-132B's accepted model-free implementation adds an explicit finite session and bounded
versioned history field. It never upgrades an existing enrolled/spent C program. Its N+1 projection
contains only the fixed-reference validation excess return, previous bounded outcome and explicit
availability/knowledge cutoff; no selected/holdout IDs, classifications, raw rows or complete results.
[Finite grant, eligibility, replay and noninterference evidence](../../reports/features/finance/bb-132b-research-loop-boundary-20261001.md).
BB-132B was accepted at exact candidate `8df4541fdcd300b0d334d519c221e4189dd32084`, merged
unchanged and verified by merge CI36918069527; publication evidence is in the linked report.
This does not authorize sending the new projection to a real model. Runtime prompt/grammar/native
worker are unchanged; real-model compatibility/validation requires separate future authorization.

Broader history-aware projection still requires separately reviewed versioned implementation.
Finance selects canonical structured facts, never Brain SQL or arbitrary serialization:

| Data | Proposed rule |
| --- | --- |
| Opaque evidence references | Resolve only inside offered principal/protocol scope; no path/URL. Model cannot enumerate evidence or invent binding handles. |
| Allowed strategy/parameters/criterion | Server-owned exact allowlists, policy versions and engineering-only labels. No model-defined thresholds or executable expressions. |
| Development metrics/context | Only expressly approved phase/metric/unit/sample coverage and causal cutoff. Unknown/missing is not zero. No raw market rows needed initially. |
| Negative/admission history | Complete categorical accounting plus digest; bounded deterministic examples, never success-only selection. Outcome-dependent rejection details are withheld if they reveal protected holdout. |
| Consumed/closed-cohort evidence | Future-only exploratory input after explicit closure/rights/overlap review. Descendants gain no fresh confirmation authority. |
| Holdout and proxies | Never protected rows/features/prices/returns/charts/distributions, selected-run IDs, result classifications, linked risk metrics, full evaluation objects or outcome-dependent prompt selection. |
| Secrets/private data | Never credentials, account details, host paths, SQL, raw provider payloads, C snapshots or unrestricted human API responses. |

Require a noninterference test: varying only protected holdout content must not change the model
projection, its digest, visible error details or selection of supplied summaries. Audit/internal
full-plan fingerprints may depend on holdout but are not model-facing evidence. Even hashes of
protected content can leak membership; use only authorized development digests. No hash proves
statistical independence. Published, human-seen, pretrained or overlapping evidence is not fresh
just because it is hidden from this prompt. Unknown exposure remains fail closed.

## Locality and entitlement are independent decisions

Conceptual policy has TWO axes, not one mutually exclusive enum:

- Local use: `LOCAL_REASONER_ALLOWED`, prohibited or unknown (only Allowed admits).
- External disclosure: `EXTERNAL_EXPORT_ALLOWED`, `EXTERNAL_EXPORT_PROHIBITED`,
  `EXTERNAL_EXPORT_UNKNOWN` (only Allowed exports).

Finance owns a versioned decision bound to exact source/product/use/projection, principal scope,
recipient class, retention conditions and evidence reference. Neither Brain nor the model sets it.
Local Allowed + external Unknown/Prohibited permits only the qualified on-host path. No local grant
is inferred from mere file possession or private-research eligibility. ADR 0022's bounded owner
acceptance is preserved; it cannot override a prohibition or grant third-party redistribution.
Synthetic self-owned fixture rights can be established without external data-export permission.

Every included fact/reference and derived summary must satisfy the intended use. Mixed contexts use
the most restrictive decision; missing/expired provenance stops dispatch. Minimized/redacted/hashed
or derived content is not automatically exportable. External approval names the provider/service,
use, destination/region where required, retention and policy version. Recheck immediately before
send, not just at context creation. Revocation/expiry invalidates queued work. Already-sent external
content cannot be recalled by a local kill switch; this is a distinct external residual risk.

No legal entitlement is newly asserted here. Exact future model/runtime and data-source terms need
separate review. Local inference is not training/fine-tuning; neither training nor model-memory
retention is authorized. Educational material, including Investopedia, stays a future idea source,
not evidence authority, and is not fetched/ingested for Research Learning in D.

## Local inference isolation

Preferred deployment shape is a pre-provisioned isolated request/response runtime on the same host,
not model libraries/plugins inside API/Finance. A permission-controlled local socket with peer
identity is preferred when supported; otherwise a fixed authenticated private service transport
with reviewed binding/network policy. Loopback or home-LAN location alone is not authentication.
Remote self-hosting is not silently treated as on-host locality; it needs a separate locality review.

The inference worker has a dedicated low-privilege identity, read-only pinned model/runtime artifacts,
minimal bounded scratch storage if required, and no Finance DB, backup, general host mounts, secrets,
Docker socket, shell/tool executor, provider/broker credentials or network egress. The model has no
file capability; necessary runtime reads of provisioned weights are not model-directed arbitrary
file access. Disable telemetry, downloads, update checks, prompt persistence and session reuse.
No background cloud lookup or model-triggered URL fetching. Clear request/KV/session buffers between
scopes; no cross-user prompt cache. Validate these properties rather than trusting runtime defaults.

Brain only sends the narrow request to a configured adapter endpoint; no user/model-selected URL,
redirect, dynamic model load, remote code or plugin/tool extension. Provisioning, resource policy,
start/stop and any GPU device allocation belong to separately authorized operations through accepted
host/Sentinel boundaries. Brain does not inspect/control processes, GPU, containers or host files.
A dedicated inference isolation boundary is justified by untrusted runtime and resource risks;
it is not permission for new generic services, brokers or a distributed experiment platform.
Runtime compromise, kernel/driver flaws and privileged host compromise remain residual risks;
application JSON validation alone cannot sandbox native inference code.

## Authentication and three permissions

Existing application auth is insufficient for an exposed production reasoner. Proposed permissions
are independently enforced by Finance (names are sketches, not implemented policy strings):

| Permission | Grants only |
| --- | --- |
| research.evidence.read | The principal's permitted projection for a specific protocol/scope; not all Finance reads |
| research.reasoner.request | One explicitly configured local invocation within that scope and its resource/scientific limits |
| research.proposal.submit | Submission to Finance admission; not admission success, direct computation, risk or execution |

A principal must have all required permissions for an end-to-end flow; none implies another.
Brain authenticates as a workload with an attenuated, short-lived delegation bound to initiator,
audience, scope, projection digest, request and expiry. No tokens go into model context. Finance
rechecks authorization at projection and admission; a stale response after revocation is rejected.
Responses cannot choose a different principal, protocol or input. Default deny and replay protection
are required; no endpoint exposure until application auth/security review is accepted.

A first synthetic, strictly local controlled proof may precede the broad household OIDC rollout
ONLY under a separately accepted design using an authenticated OS/workload principal, fixed owner
scope, explicit launch approval, no public/listening API and no real dataset. Locality alone is not
a permission and does not waive audit/admission. D does not implement or authorize this exception.
Broker/orders/capital/PAPER/LIVE/AUTO and high-authority external actions remain unavailable.

## Untrusted output and prompt injection

Preserve B's exact closed response v1: one Proposal (one variant and criterion) or NoUsefulProposal.
Limit decoded UTF-8 bytes to at most 64 KiB, JSON depth 8, question 1,024 characters and
rationale/explanation 2,048 characters, or stricter selected-runtime limits. Enforce limits while
receiving, before allocating/parsing complete output. Bound transport envelopes and decompression;
reject partial output, trailing content, Markdown fences, duplicate keys at every depth, unknown
properties/versions/enums, numeric strings, null required fields, ambiguous/extra variants and
nonfinite numbers. No silent truncation, defaulting, JSON repair, model self-correction retry or
alias conversion. A grammar/constrained decoder may improve usefulness, never replaces validation.

Only Finance resolves references, normalizes and commits proposals against authoritative state.
Question/rationale are inert plain text. A URL, SQL or code string in prose has no executable meaning;
never fetch, evaluate, render active Markdown/HTML, or promote it to a capability. Unexpected such
content may be discarded from display/audit; legitimate free text is not a script interpreter.
Capability-like fields/tool requests are rejected as unknown shape. Do not claim a keyword filter
can identify every attack or make a semantically bad proposal safe.

Prompt assembly has an immutable versioned system policy, a separate canonical Finance fact object,
and (only in later reviewed projections) a labelled bounded untrusted-text field. External news,
metadata, educational excerpts, imported/user/provider text and previous model prose are data.
The initial synthetic path needs none of this prose. No message-role injection, model-updated policy,
retrieval loop, tool discovery, chain-of-thought retention or recursive invocation. Brain may not add
hidden database/history context. Policy templates contain no secrets; their secrecy is not a control.

[OWASP prompt-injection guidance](https://cheatsheetseries.owasp.org/cheatsheets/LLM_Prompt_Injection_Prevention_Cheat_Sheet.html)
(accessed 2026-09-23) supports instruction/data separation, least privilege and output validation.
Our repository-specific conclusion is that deterministic enforcement and absence of dangerous tools
must remain effective even if prompt instructions are ignored. Prompt separation is mitigation,
not a proof against injection. This applies identically to local and external models.

## Resource policy, failure and disable controls

Scientific and inference budgets are separate. Brain's resource admission cannot replenish C's
scientific counters. Future local default: at most one inference, no automatic retries, no queued
backlog (busy rejects), bounded context/output, deadline/cancellation and an independently enforced
worker memory/CPU/process limit. Optional GPU use needs measured safe headroom and validated limits;
where VRAM isolation cannot be guaranteed, disable GPU inference or use an accepted exclusive budget.
Do not rely only on the model's token setting or an in-process semaphore across multiple callers.

B declares 30-second reasoner / 300-second iteration caps; these are engineering contract ceilings,
not evidence of achievable hardware latency or enforced runtime watchdogs. Preserve them for a first
B-compatible proof, with stricter runtime bounds allowed; changing the protocol needs separate review.
RAM/CPU/VRAM/token numerical allocations require host measurements and acceptance before invocation.
Missing resource policy disables the adapter. Total prompt (including policy/schema) must fit the
chosen context/token and memory budgets; never silently truncate mandatory Finance context.
The Finance projection also retains A's 64 KiB UTF-8 input ceiling; total assembled prompt/token
limits may be stricter and must include policy/schema overhead. These are safety maxima, not a
recommendation to fill the context or a model-performance claim.

Cancellation must interrupt the request and worker computation, not merely close a client await.
If the worker cannot be confirmed stopped, retain the busy slot, block further calls and require
operator recovery. Never start another model to compensate. A lost response, crash or restart does
not refund the invocation or unconsume exposure. No automatic submission of a late response.

Conceptual controls: `REASONER_DISABLED` default true, local enable false until gates pass,
`EXTERNAL_REASONER_DISABLED` default true independently. Configuration is trusted operator policy,
not model input. Disable stops new dispatch/admission, invalidates pending delegations and cancels
in-flight inference; output arriving later is ignored and audited. Already committed scientific work
is handled by Finance's existing lifecycle, not deleted or refunded. Deterministic Finance works
independently. Circuit-open after timeout/resource failure requires explicit operator review; a
restart or alternate model does not clear scientific history. No automatic re-enable.

| Failure | Deterministic operational response |
| --- | --- |
| NoUsefulProposal | Record decline, terminate invocation, no engine call; no retry |
| Model/runtime unavailable, crash, timeout, cancellation, exhausted resources | Record bounded failure; no engine call or replacement model; preserve spent/uncertain reservation |
| Malformed, oversized, unknown-version output | Reject before Finance execution; no repair or wider prompt |
| Authentication/authorization failure | No projection/dispatch; disclose only generic denial, record sanitized audit |
| Duplicate proposal | Finance links existing reservation/result; not a new trial or invocation authority |
| Finance admission/rights/exposure/budget/risk rejection | Preserve native reason/history internally; no engine call, no holdout-derived error fed back |
| Persistence/audit failure before dispatch | No model or engine dispatch without required durable record |
| Persistence failure after dispatch/compute | Mark/retain uncertain state, stop; use C's exact reconciliation, never auto-refund/rerun |
| External disabled, export prohibited/unknown, external budget exhausted | No external request; configuration does not grant export |
| External unavailable/authentication failure/rate limit | Record sanitized failure; no automatic retry, local substitution or another provider |

No model failure widens context, reveals holdout, changes data/thresholds, launches trades, or
switches local to external. Operator-requested fresh work still needs independent Finance authority.

## Optional external adapter and cost boundary

External inference remains future-only and disabled. Reuse the same port/parser/admission with an
explicit dispatch mode chosen before reservation. Local failure is never a paid request. Presence
of credentials or another adapter does not authorize use. No SDK/provider selection in D.
Additional independent gates: explicit owner/provider authorization; recipient-specific export
Allowed for every projected item; terms/retention/privacy/data-residency assessment; approved scoped
credential mechanism outside prompts; fixed egress allowlist, authenticated TLS, no redirects or
arbitrary URL; bounded requests/responses; incident/revocation procedure.

Cost policy separately reserves a conservative maximum cost from configured per-request and
cumulative currency ceilings BEFORE sending, including concurrent requests and uncertain billing.
Unknown price/usage, missing policy or exhausted allowance denies. No purchase/recharge/upgrade by
software; zero configured budget disables paid requests. Requests lost after send remain charged/
reserved conservatively until reconciled. Usage is audit, not scientific budget. Cloud authorization
and cost accounting are not required for the on-host path.

## Sanitized durable audit

Finance retains scientific admission/history/results in its existing owner. Brain's future audit is
operational, joined by opaque invocation/proposal references; it never copies full C snapshots/results.
Minimum durable record: authenticated initiator/workload reference, local/external mode, pinned model/
runtime/adapter identity, protocol/question identifier (not raw question text), projection version/hash,
locality/export policy reference and decision, fixed template version, start/end/deadline semantics,
size/resource counters, outcome/failure code and validated proposal identity where one exists.
No provider secret, raw prompt/response, protected holdout, provider payload or chain-of-thought.
Bounded response hash may aid incident correlation only where retention policy permits it; hashes
are not anonymization or reconstruction. Avoid raw exception messages and identifying endpoint paths.

Require durable intent before dispatch and durable terminal status; missing completion becomes
indeterminate, never inferred success. Access control, retention/deletion, tamper detection and
storage-full behavior need explicit implementation acceptance. This is not a new datastore or audit
schema in D. Existing general identity-bound audit is incomplete; it is a first-inference prerequisite.
Replay of science uses frozen Finance commitments/results, never another model call. It need not
reproduce stochastic model generation. Retention loss must be reported honestly, not hidden by rerun.

## Abuse cases and required acceptance tests

These are design obligations, not tests executed in D. Each must be tested with adversarial fixtures
before enabling a real model; use the same suite for any later external adapter.

| Attempt | Expected containment / proof |
| --- | --- |
| Ask for holdout or richer outcomes | No such field/tool; vary protected data and prove projection/output routing unchanged |
| Ask for raw database / SQL | No DB mount/credential/query capability; SQL/tool fields rejected; prose inert |
| Ask for shell or return code | No executor; closed shape; no eval/plugin/compiler invocation |
| Ask for Internet or return URL | No arbitrary HTTP/redirect capability or egress; inert text, no rendered link fetch |
| Multiple experiments / unsupported strategy or period | Existing exact single momentum20 allowlist rejects before engine |
| Invent evidence reference / input digest | Finance resolves offered scope and stored digest; mismatch denies |
| Claim risk approval or PAPER/LIVE/AUTO | No authority field accepted; native risk/mode controls unchanged |
| Change own instructions / injected research text says ignore policy | No configuration write/tools; output validated independent of obedience |
| Oversized/deep/malicious JSON / duplicate keys | Receiving and parser limits reject before admission; no unbounded buffering |
| Runtime/model crash | No refund/retry; durable failure/uncertainty, Finance independently available |
| Repeated timeouts / poor model quality | Disable/review operationally; no automatic retries, relaxed policy or paid fallback |
| External adapter disabled | No outbound external request even if credentials are present |
| Local failure while external adapter exists | Fail local request; assert external transport call count zero |
| External-export UNKNOWN | No external bytes sent; qualified local use remains possible |
| Rename model/request/rationale/revision | Existing fingerprint/cohort history remains authoritative; no budget/freshness reset |
| Principal revoked / replayed or late response | Scope/expiry/current permission checks deny; no engine call |
| Shared cache / malicious weights / runtime telemetry | Fresh isolated context; provenance/integrity review, deny egress, pinned artifacts and isolation tests |
| Concurrency, audit-full, worker ignores cancel | At most one dispatch; fail closed, retain slot/reservation; independent Finance remains usable |

## First local-model gate — all required, no cloud dependency

1. Owner/architect accepts the exact D design and separately authorizes a bounded implementation
   and first invocation. D alone authorizes neither; initial proof stays synthetic, one protocol.
2. Finance accepts/version-tests the projection and applicable on-host use/retention rights; first
   B-compatible empty-history input, no protected data/proxies or arbitrary human API serialization.
3. Select runtime/model later with documented provenance, hashes, licence/use terms, supported host,
   offline operation and no required paid service/account. Installation is separately approved.
4. Verify actual hardware/free resources and enforce accepted context/token/RAM/CPU/optional-VRAM,
   one-call/deadline/cancellation limits; demonstrate Finance/service health survives resource failure.
5. Accept caller/workload/delegation permissions and audit, including any strictly local synthetic
   harness exception; no public endpoint or general auth bypass.
6. Implement/test isolated request-response transport, zero tools/egress, safe weights/scratch,
   no telemetry/prompt retention and no host/Sentinel bypass. Kill/disable/restart behavior must pass.
7. Implement/test exact structured response, malformed/unknown/duplicate/oversize rejection and
   injection cases. No JSON repair, second pass or automatic retries.
8. Integrate existing Finance admission/C reservation before inference/computation; prove budget,
   exposure, native risk veto, independent deterministic operation and no execution authority.
9. Durable sanitized audit/retention and crash recovery accepted. No stochastic model replay needed.
10. Independent security/architecture review of all abuse-case evidence and explicit owner go/no-go.

No cloud account, credit, subscription, external export or upgraded hardware is required by this
contract. If the chosen runtime requires cloud, choose another; it is not the default local path.
A successful synthetic invocation still does not authorize real-market/adaptive iteration.

## First external-model gate — separate additional authorization

Require equivalent accepted projection, auth, admission, audit, limits, output/injection and
no-execution tests from the local gate, with external transport isolation instead of local GPU/weights
requirements. Additionally require explicit adapter/provider/owner authorization, approved secret
storage, recipient-specific export Allowed, terms/retention/privacy/residency evidence, TLS/egress
controls, cancellation/uncertain-billing behavior and hard cost ceilings. Prove disabled external
means zero calls and local failure never falls back. These requirements cannot block a qualified
local-only path. No external provider/model is chosen here.

## Remaining prerequisites and scope

D completes design only. Projection/auth/audit/resource/runtime implementation and adversarial tests,
model selection/provenance, hardware measurement, operational isolation acceptance and explicit
invocation authorization remain outstanding. General adaptive history, real-data overlap/rights and
C backup rollback limits remain; D does not solve them. There is no current implementation blocker
requiring a scientific fix, no broadened ADR 0038 authority, and no BB-131E implementation authorization.
