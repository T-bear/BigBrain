# BB-132A — local-model acceptance evidence and bounded stops

## Review Checkpoint RC02 — 2026-09-30

**REVIEW CHECKPOINT / INCOMPLETE / NOT A MERGE CANDIDATE.**
Goal remains BB-132A: real local research proposals with deterministic Finance authority.
Accepted main baseline: `739beab55a494068edcf23d3c905aa6601b99dc0`.
Branch: `bb-132a/first-local-language-model`.
Parent RC01: `0788e14a297c238f1eff6d3a622fd96a9978c25d`, tree
`3be7d6c2a12cd7a9fcdde9e637ed54eb08d22ca2`. Owner reports independent RC01 review and authorizes
same-branch continuation; RC01 is not accepted for merge. Existing review history remains intact.
RC02 publication subject: `review: publish BB-132A RC02 sanitized reply diagnostics`.
Exact RC02 SHA/tree are resolved from the containing publication commit, including after later reviews:

```sh
git log -1 --format='%H %T' --grep='^review: publish BB-132A RC02 sanitized reply diagnostics$' origin/bb-132a/first-local-language-model
```

### Minimal diagnostic implementation

Finance already returns a closed `LearningAdmissionReason? Rejection` from `LearningReplyParser`.
The runtime previously replaced every rejected parse with generic InvalidReply. RC02 passes that
existing value through as nullable `ReplyRejection` in the runtime exception and terminal audit.
The test-only acceptance journal also records the enum name when present. No parallel parser,
new verdict taxonomy, model-provided path/value or exception text is logged. No raw output is stored.
Parser acceptance, validation order and all scientific rules are unchanged. A null reason remains
unknown/not-applicable; transport/crash/cancellation failures never manufacture a Finance rejection.
The reason is precisely the existing Finance category, not a guessed field-level diagnosis.

Model-free real-pipe tests assert these existing categories survive unchanged:

| Fixture mutation | Finance reason retained |
| --- | --- |
| Unsupported version | UnsupportedContract |
| Invented evidence checksum | EvidenceMismatch |
| Unsupported strategy | UnsupportedStrategy |
| Unsupported period | UnsupportedParameter |
| Multiple variants | InvalidVariantCount |
| Malformed/duplicate/deep/null/missing/Markdown/trailing/tool/PID/executable/signal/risk/PAPER input | Malformed |

Eighteen invalid-reply fixtures also assert the same runtime InvalidReply failure, no response hash,
no inner exception/raw message and no reason on preterminal audit events. Valid reply and eight
transport-failure fixtures assert no invented parser reason. Existing C-ledger/science/no-retry
regressions remain unchanged. This proves diagnostic propagation, not historical model causality.

### Concrete evidence blocker — no model invocation in RC02

The earlier corrected real run produced a complete BRF1 response, exited0 and was rejected by Finance
as InvalidReply. Only `invocation.jsonl` exists in that attempt directory. It contains the generic
failure and no inner rejection or response hash. The retained test log likewise contains only the
runtime exception. The harness saves response JSON only after successful strict parsing, which did
not occur. No retained response exists to reparse with the new diagnostics.

All 13 previously pinned evidence files remain byte-identical. The exact historical parser reason
cannot be reconstructed from those records. Source comparison of prompt/grammar and Finance rules
does not establish which response the model emitted; synthetic counterexamples are not a substitute.
No reason/field is guessed, no integration correction is claimed and no historical evidence is rewritten.

The owner's last acceptance invocation is conditional on identifying and correcting the cause first.
That condition is not met. No new inference, diagnostic reproduction or acceptance invocation was run;
the conditional allowance remains unused. No model download/selection/change, GPU/CUDA or cloud work.
This is an evidence/authorization dependency, not an asserted pre-existing scientific/security defect
or a request to redesign the architecture. A new bounded owner/architect decision is needed to permit
obtaining diagnostic evidence from a fresh invocation, or to conclude this checkpoint without it.
No such permission is inferred from the conditional final-acceptance grant.

### Verification and exact RC02 scope

Commands/results for the new diagnostic source:

- `dotnet build tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-restore`:
  PASS, zero warnings/errors.
- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-build`
  with filter `FullyQualifiedName~LocalReasonerRuntimeTests|FullyQualifiedName~LocalModelAcceptanceTests|FullyQualifiedName~ResearchReasonerContractTests|FullyQualifiedName~ResearchLearningContractTests|FullyQualifiedName~FinanceLearningLedgerTests`:
  **196 PASS / 0 FAIL / 5 intentional real-model skips**. No model artifact is loaded by those tests.
- `dotnet format BigBrain.slnx --verify-no-changes --no-restore --include` followed by the four
  changed C# paths: PASS. Initial acceptance-harness check found WHITESPACE errors; scoped
  `dotnet format whitespace` corrected only that file. No model/test behavior changed by formatting.
  Full final checkpoint verification is not claimed.
- `node scripts/verify-documentation.mjs`: PASS,258 Markdown /91 unique backlog IDs.
- `git diff --check`: PASS; Gitleaks exact13 intended files: PASS/no leaks. All13 prior local evidence
  hashes verified unchanged; staged inventory/secrets checked again before commit.
- Current workflow triggers pushes only on main or pull_request; no publication-CI result is claimed
  for this branch-only handoff. No full backend/restore matrix or real-model success is claimed.

Four changed implementation/test files relative to RC01:
`src/BigBrain.Brain/LocalReasonerRuntime.cs`, `src/BigBrain.Brain/LocalReasonerRuntimeOptions.cs`,
`tests/BigBrain.Api.Tests/LocalReasonerRuntimeTests.cs`, `tests/BigBrain.Api.Tests/LocalModelAcceptanceTests.cs`.
Nine documentation files: ROADMAP.md, TESTING.md, docs/STATUS.md, docs/BACKLOG.md,
docs/modules/finance.md, docs/architecture/finance/master-roadmap.md, docs/operations/codex-recovery.md,
docs/reports/REPORT-CATALOG.md and this report. Exact RC02 delta: **13 files**.
No changes to Finance parser/admission/ledger/schema/engines/risk, native worker/prompt/grammar,
resource/isolation policy, Sentinel, Web, packages, CI or deployment. Unrelated local material excluded.

Finance **RESEARCH / 0 SEK / NONE**. No scientific engine or trading call in the new diagnostic work;
no budget/exposure reset, raw-data export, model-generated executable behavior or additional authority.
Next: publish RC02 then **STOP** for independent owner/architect review of the missing-evidence
blocker and a bounded next decision. No merge, deployment, PAPER/LIVE/AUTO, broker/orders/capital or BB-132B.
RC01 and earlier evidence below remain historical, unchanged in meaning.

## Review Checkpoint RC01 — 2026-09-30

**REVIEW CHECKPOINT — INCOMPLETE / NOT A MERGE CANDIDATE / NOT ACCEPTED.**
Checkpoint: BB-132A — First Local Language Model.
Goal: a real, bounded local language model proposes research through the accepted owned native
BRF1/libllama path; unchanged Finance parsing/admission retains deterministic authority.
Accepted main baseline: `739beab55a494068edcf23d3c905aa6601b99dc0`.
Branch: `bb-132a/first-local-language-model`. Publication identifier: **BB-132A-RC01**.
Commit subject: `review: publish BB-132A RC01 and checkpoint workflow`.

Exact Review Checkpoint SHA and tree SHA are the containing publication commit's Git metadata.
They are intentionally not embedded recursively in their own tree. Resolve this specific checkpoint
from GitHub history even after later commits (do not substitute a later branch tip):

```sh
git log -1 --format='%H %T' --grep='^review: publish BB-132A RC01 and checkpoint workflow$' origin/bb-132a/first-local-language-model
```

The first value is the Review Checkpoint SHA; the second is its tree. Inspect that exact commit's
parent and `git diff --name-status 739beab55a494068edcf23d3c905aa6601b99dc0 <review-sha>` for inventory.
The same information is available through GitHub commit metadata/diff; no terminal transcript is needed.
This publication contains the preserved implementation unchanged plus the owner-authorized permanent
Review Checkpoint workflow. No new diagnostics, correction or inference was performed in RC01's
publication step. Main remains unchanged. No acceptance, merge or deployment authority follows.

### Current engineering evidence and blocker

| Preserved real run | Result | Runtime terminal time | Finance outcome |
| --- | --- | --- | --- |
| Qwen3-4B, original 30s grant | Timeout | 30,130ms | No reply |
| Qwen3-4B, separate 180s grant | Timeout | 180,120ms | No reply |
| Qwen3-1.7B, original 180s grant | WorkerFailed; old exit unrecorded | 78,988ms | No reply |
| Qwen3-1.7B, diagnostic reproduction | Native exit40:512-token cap without EOG | 72,602ms | No reply |
| Qwen3-1.7B, corrected grammar configuration | Complete BRF1 reply; native exit0 | 30,215ms | InvalidReply |

The last run proves real local generation reached a complete response. It does not prove an admitted
proposal. Existing Finance **reply parsing** rejected it; subsequent admission/engine were not reached.
The inner deterministic parser reason/category and raw response were not retained. Exact rejected field
is UNKNOWN. No inference that 1.7B is too slow/unsuitable follows from these failures.
Detailed measurements, pinned artifact/license/runtime provenance and diagnostics are below.

Retained verification: native compile PASS, Release test-project build PASS with zero warnings/errors;
196 focused B/C/E/F/configuration tests PASS, five real-model tests intentionally skipped in ordinary
execution. Real diagnostic and corrected selections each failed as explicitly recorded below. These
are prior checks of the preserved source, not new publication CI or a full acceptance matrix.
Full final restore/formatter/backend matrix, native reproducibility tooling and completed acceptance
remain unfinished. Model files/binaries/private audit/SQLite are ignored and excluded from Git.
RC01 publication checks: documentation/link/index verifier PASS (258 Markdown files /91 unique
backlog IDs); git diff --check PASS; Gitleaks exact22 intended files PASS/no leaks. Seven source/test
files are byte-identical to the preserved verified tree; no expensive model-free suite was repeated
for this documentation-only delta. Staged inventory/secrets are rechecked before commit. Remote
branch SHA/tree and unchanged main are checked after push. Publication CI is not claimed green.

Thirteen local evidence files have immutable checksums; rechecking them for this publication makes
no database changes. Public sanitized results below are sufficient to review the known failure and gap.

### Changed-file ownership

Preserved runtime and tests (seven files):
- `src/BigBrain.Brain/LocalReasonerRuntime.cs`
- `src/BigBrain.Brain/LocalReasonerRuntimeOptions.cs`
- `src/BigBrain.LocalReasoner.Worker/containment.h`
- `src/BigBrain.LocalReasoner.Worker/containment-probe.cpp`
- `src/BigBrain.LocalReasoner.Worker/worker.cpp`
- `tests/BigBrain.Api.Tests/LocalModelAcceptanceTests.cs`
- `tests/BigBrain.Api.Tests/LocalReasonerRuntimeTests.cs`

Checkpoint documentation (nine files): ROADMAP.md, TESTING.md, docs/STATUS.md, docs/BACKLOG.md,
docs/modules/finance.md, docs/architecture/finance/master-roadmap.md,
docs/operations/codex-recovery.md, docs/reports/REPORT-CATALOG.md and this report.
Workflow documentation (six files): AGENTS.md, docs/START-HERE.md, docs/indexes/documentation.md,
docs/reports/README.md, docs/reports/publication-policy.md and docs/reports/report-schema.md.
Total: **22 intended files**. Unrelated mockups/unpublished Sentinel ADR0006–0009 are excluded.
No changed Finance parser/engine/ledger/schema/risk, Sentinel, Web, project/package, CI or deployment files.

### Publication and continuation boundary

Owner authorizes Review Checkpoints even with failed acceptance; this explicitly supersedes the
older publication stop wording in the historical sections below. Rules live in
[AGENTS](../../../../AGENTS.md#permanent-checkpoint-branch-workflow), not a parallel process document.
A Review Checkpoint becomes a Merge Candidate only after completed acceptance/full verification.
Independent review and explicit owner approval of the exact unchanged SHA still precede merge.

Current next action: publish RC01 and STOP for independent GitHub review. The owner's authorized next
bounded BB-132A work is minimal sanitized Finance rejection diagnostics, identifying the exact
InvalidReply cause and only an in-scope integration/format correction. That work has NOT started in
RC01. At most ONE further Qwen3-1.7B Q4_K_M acceptance invocation, <=180s, is conditional on identifying
and correcting the cause within the accepted boundary. This is not an unconditional retry grant.
No additional diagnostic model invocation is implied. Do not replay any already-consumed opt-in test.
If cause cannot be established within that scope, publish the new blocker state for review instead.
No parser tolerance/vocabulary expansion, lower isolation/resource controls, GPU/CUDA, model swap,
cloud/provider, arbitrary execution, raw sensitive output capture or scientific budget reset is allowed.

Finance **RESEARCH / 0 SEK / NONE**. No PAPER/LIVE/AUTO, broker/orders/capital, deployment, merge,
or BB-132B. Grammar guides generation only; rationale never becomes executable behavior.
Historical sections below retain earlier evidence and earlier authorization/publication states.

## Bounded diagnostic continuation — 2026-09-30

Owner authorizes investigating 1.7B WorkerFailed while retaining all three previous attempts.
No new acceptance is claimed by a diagnostic reproduction. Nine old evidence files are SHA256-pinned;
no ledger writes/refunds, engine execution or parser change. Native source changes only fixed failure
stage exit codes; model, prompt,512-token cap, threads/context/deadline and containment stay unchanged.
The controller records exit status through its existing owned handle after bounded cleanup, plus
whether the child was still running when cleanup began. It never targets a supplied PID or logs
stderr/prompt/output. A signal-like exit code is not alone proof of OOM or a particular signal.

| Native exit | Meaning of caught failure |
| --- | --- |
| 2 | Invalid trusted launch argument count |
| 30 | Isolation/bootstrap (including cgroup policy) |
| 31 | BRF1 request framing/read or bounded prompt construction |
| 32 | CPU backend initialization |
| 33 | Model load |
| 34 | Tokenization/input token bound |
| 35 | Context creation |
| 36 | Prompt evaluation |
| 37 | Sampling |
| 38 | Token-to-piece/output byte bound |
| 39 | Generated-token decode |
| 40 | No end-of-generation token within512 generated-token iterations |
| 41 | Library cleanup |
| 42 | Reply framing/write stage |

Codes describe caught failures, not native crash recovery. Process exit code, audit transport outcome
and cgroup memory.events must be assessed together. No new stderr protocol, process privilege or
filesystem/network allowance is introduced. Old return3 cannot retrospectively identify one stage.
Diagnostic reproduction: **WorkerFailed / native exit40 / cleanup not required**, terminal72,602ms,
caller72,653ms, command79,455ms. It exhausted512 generated-token iterations without EOG. Backend/model
load, context, prompt evaluation and generated-token decode passed; native exception was caught, not
an observed crash/controller kill. Cgroup OOM counters remain0 and group empty. CPU142.985559s;
peak sampled cgroup1,031,168,000bytes (not process RSS). Same input hash, pinned model and pre-correction
configuration as earlier1.7B. This identifies the reproduced failure path; it cannot manufacture an
exit code for the historical uninstrumented run. All nine prior evidence files remain byte-identical.

Bounded local configuration correction: use libllama grammar-constrained sampling for the already
accepted Proposal/NoUsefulProposal wire shapes, compact ASCII question/rationale<=96 characters.
The model generates these fields and context bindings; no model response is edited/repaired or
promoted into eligibility. Finance still parses/re-admits; grammar is generation guidance only.
Unchanged512 output tokens,4096 context,3072 input tokens,65536 transport bytes,180s timeout,
CPU/RAM/cgroup/Landlock/seccomp, no tools or extra authority. Separate binary, no overwrite of old ones.
This corrects unconstrained verbosity within the existing output budget rather than raising controls.
The ONE corrected acceptance was executed: **0 PASS /1 FAIL, InvalidReply**. Complete BRF1 response
started at30,169ms; native process exited0 without controller cleanup; Finance parser rejected it.
Terminal30,215ms, caller30,269ms, whole command36,312ms. CPU58.753418s,145 samples, peak2tasks,
RSS/HWM1,683,792KiB (~1.606GiB), peak sampled cgroup1,077,678,080bytes, OOM0, group empty.
No load/prefill/generation timing or throughput precision claimed. This shows completed generation
within limits, not valid/admitted research. Inner parser rejection enum was discarded by existing
runtime; raw response was not retained. Exact field-level rejection is UNKNOWN, not inferred.
No response hand-edit/repair, admission, engine, risk/trade or further invocation occurred.
All11 previous evidence files are byte-identical. Final ignored13-file evidence manifest pins all
five runs' journals/measurements and original SQLite state; no ledger writes/refunds.

Diagnostic binary SHA256:0a0bb42a2bee0666fe2c570d16e1e979ddefaed913a52042806dd5dabf3a622a.
Corrected binary SHA256:44bbeca4bca8103299fbd8201c2df5fb8d9133411c9544492493ef91b45a1011.
They use the already pinnedlibllama/artifact below; no re-download, model substitution or GPU work.

Verification for this continuation:
- `g++ -std=c++20 -O2 -Wall -Wextra -Werror` with pinned local headers/libraries and existing
  link flags: diagnostic and corrected native binaries build PASS (no model run during compilation).
- `dotnet build tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-restore`:
  PASS, zero warnings/errors.
- `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-build`
  with filter LocalReasonerRuntimeTests, LocalModelAcceptanceTests, ResearchReasonerContractTests,
  ResearchLearningContractTests, FinanceLearningLedgerTests: **196 PASS /0 FAIL /5 intentional model skips**.
- New model-free tests prove retained child exit codes even when response framing is incomplete;
  timeout cases distinguish required cleanup from natural exit. Existing fail-closed behavior preserved.
- Opt-in diagnostic test Qwen17DiagnosticPreservesPriorFailuresWithoutAdmission:0 PASS/1 FAIL exit40.
- Opt-in corrected test Qwen17CorrectedOutputConfigurationRequiresFinanceAdmission:0 PASS/1 FAIL InvalidReply.

**STOP / INCOMPLETE / NOT A REVIEW CANDIDATE.** No more runs/fixes after this failure. Full final
backend/format/restore matrix intentionally not claimed for incomplete A. Required final acceptance,
repeatability and publication remain absent. Handoff-only checks PASS: documentation258Markdown/91uniqueIDs, git diff --check,
Gitleaks exact15 intended files/no leaks. Recovery records exact inventory and preservation. No pre-existing science/security/lineage defect asserted. No merge/deployment/BB-132B.
Historical sections below retain the earlier outcomes and dated authorization state.

Detta är en sanerad GitHub-version. Local, unpublished evidence.
**BLOCKED / INCOMPLETE / NOT A REVIEW CANDIDATE**.
Date: 2026-09-29. Baseline/HEAD `739beab55a494068edcf23d3c905aa6601b99dc0`;
branch `bb-132a/first-local-language-model`. Main, accepted Finance code and Sentinel are unchanged.

## Qwen3-1.7B result — 2026-09-30

**BLOCKED / INCOMPLETE / NOT A REVIEW CANDIDATE**. The separately authorized 1.7B invocation
ended with **WorkerFailed**, not Timeout, after about 79s. No complete BRF1 response/header arrived;
Finance parser/admission and scientific engine were not reached. The180s maximum was not exceeded.
The one-shot grant is consumed. No retry, further model, longer timeout or GPU/CUDA work followed.

The identical native-worker-180 binary and pinned libllama runtime were used. Only the trusted model
artifact and separate test-only journal changed. SHA checks before/after prove all seven previous 4B
evidence files remain unchanged. No C-ledger mutation, budget refund/reset or new Finance database.
No model output was hand-edited, truncated into acceptance, executed or made scientific authority.

### Exact 1.7B provenance

Official Qwen 1.7B GGUF revision `90862c4b9d2787eaed51d12237eafdfe7c5f6077` only offers Q8_0.
Selected standard Q4_K_M artifact (not a dynamic quantization variant):

- [Unsloth quantizer repository](https://huggingface.co/unsloth/Qwen3-1.7B-GGUF/tree/d7f544eead698dbd1f15126ef60b45a1e1933222),
  revision `d7f544eead698dbd1f15126ef60b45a1e1933222`, file `Qwen3-1.7B-Q4_K_M.gguf`.
- Actual size **1,107,409,472 bytes** and full SHA-256 verified after exact-revision download and again
  before inference: `b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897`.
- Quantizer card declares base Qwen/Qwen3-1.7B and Apache-2.0. Exact
  [Qwen LICENSE](https://huggingface.co/Qwen/Qwen3-1.7B/blob/70d244cc86ccca08cf5af4e1e306ecf908b1ad5e/LICENSE)
  inspected/preserved locally. That revision pins the licence inspection, not an independently proven
  converter-input revision; the immutable quantized artifact is pinned separately above.
- Runtime remains llama.cpp b11146, CPU Haswell; native-worker-180 SHA
  `a88c7a31b6e92dcb3b3b5d6c1b456d2f71c79e5c3c516ecd1f1afb11ea8e4b95`, unchanged from 4B 180s.
- Same context 4096, prompt 3072 cap, output 512 cap, greedy, batch 128, two CPU threads, zero GPU,
  4 GiB memory/AS ceiling, CPU quota 2, no network/writes/descendants, 180s controlled deadline.
- Downloaded through four bounded byte ranges, final whole-file verification before rename/load;
  stored in ignored data/bb132a, never Git. No runtime fetching or external inference provider.

### Measurements

| Measurement | 1.7B controlled invocation |
| --- | --- |
| Runtime terminal audit | WorkerFailed at 78,988 ms; caller recorded 79,054 ms |
| Whole command | 87,181 ms including pre-invocation hash/test startup |
| Complete reply / load / generation | No complete reply; load completion and generated token count/rate not instrumented |
| Highest sampled kernel VmHWM / VmRSS | 1,608,304 KiB =1.534 GiB; high-water mark observed while owned process alive |
| Highest sampled VmSize | 1,734,240 KiB; below 4 GiB AS limit |
| Peak sampled cgroup memory | 1,619,570,688 bytes =1.508 GiB; separate accounting from RSS/shared cache |
| Cgroup CPU delta | 144.042 CPU-seconds, approximately1.82 cores over 79s; quota 2 |
| Peak tasks / OOM | 2 / 0 OOM events or kills; dedicated cgroup empty after exit/cleanup |
| Host available RAM before / after | 5,281,228 / 5,256,472 KiB |
| Host swap free before / after | 6,822,556 / 6,794,804 KiB; host-wide, worker swap limit remains0 |
| GPU | CPU-only; no GPU backend/offload or CUDA work |
| Acceptance test | 0 PASS / 1 FAIL, WorkerFailed; zero scientific engine calls |

The monitor collected 348 samples at nominal 250ms intervals. RSS/HWM were read only for the sole
member of the dedicated owned cgroup, with no signal, arbitrary PID targeting or protocol extension.
These are observed high-water values, not a guarantee that sampling caught the final instant.

The current native worker discards raw library logs and maps internal checks to a closed failure exit;
F reports WorkerFailed for this missing/failed response. This evidence does **not** distinguish a
512-token/EOS cap, decode failure or another internal failure. No unsupported root cause or throughput
claim is made. A result within 79s is not proof of successful model generation or acceptable quality.
Memory exhaustion was not observed; this is not a 180s performance-timeout result.

### Verification and disposition

Release test-project build PASS, zero warnings/errors. Model-free opt-in/configuration test PASS;
all three real-model entrypoints correctly skipped without opt-in (1 PASS / 3 intentional skips).
Previously validated B/C/E/F/configuration 194 PASS remains applicable: no production/runtime/native
code changed for the 1.7B swap, only the bounded acceptance harness. Ordinary CI never loads a model.
Real 1.7B test failed as above; prior 30s and 4B 180s failures remain immutable, not greened over.
Handoff checks PASS: documentation/index/link verifier (258 Markdown /91 backlog IDs), git diff --check,
Gitleaks exact13 intended files/no leaks. Full final candidate verification
and publication are not complete, because no genuine proposal has traversed Finance admission.

STOP: return this concrete WorkerFailed result and measurement limits for owner/architect decision.
A further invocation or model/configuration change requires a new explicit bounded authorization;
no extra model experiment or new checkpoint is started. If further work is authorized, distinguish
closed native failure reasons before claiming a performance/model-quality cause. No diagnosis is
obtained by weakening Finance parsing or executing model-generated text.
Finance **RESEARCH / 0 SEK / NONE**. No cloud, provider-data export, PAPER/LIVE/AUTO, broker/orders,
capital, deployment, merge, candidate commit/push or BB-132B. All unrelated local work preserved.

## Separately authorized 180-second probe — 2026-09-30

**BLOCKED / INCOMPLETE / NOT A REVIEW CANDIDATE**. The owner explicitly authorized one new,
separate 180s integration invocation after the 30s failure, with no refund or rewrite of prior history.
That single grant has now been consumed. It also ended in **Timeout**, without a complete BRF1 frame.
Per the owner's explicit stop condition, no further inference, timeout increase or model substitution.
This is a measured failure of this CPU-only configuration to deliver the required complete proposal
within 180s, not proof that all Qwen configurations are impossible or that RAM capacity was exceeded.

The same direct owned native worker/libllama/Qwen artifact/BRF1 path was used. Only an internal
controlled-acceptance options property enables exactly 180s; public/default configuration remains 30s.
The native real-model worker CPU-time cap is 360s (CPU quota 2 ×180s); model-free probe stays 60s.
Finance projection, pure parser/admission and C ledger semantics were not changed. The new test-only
journal records the explicit owner grant and prior ledger SHA, without a new Finance DB or reset.
It was prepared to call existing pure admission against retained scientific counters if a reply arrived;
it never revives the Failed C iteration or grants scientific execution. No reply arrived, so parser/
admission and engine were not reached in this attempt.

| Measurement | Actual 180-second attempt |
| --- | --- |
| Terminal runtime audit | Timeout at 180,120 ms; test observed failure at 180,187 ms |
| Whole test command | 203,620 ms including pre-invocation model hashing and test startup |
| Complete response / load time | No complete response by 180s; load completion, first token and generation rate not separately instrumented |
| Peak sampled cgroup memory | 1,188,593,664 bytes = 1.107 GiB; accounting excludes shared pages charged elsewhere, NOT peak RSS |
| Native address-space ceiling | 4 GiB RLIMIT_AS, unchanged; no measured per-process peak RSS available |
| Cgroup CPU delta | 267.795 CPU-seconds, approximately 1.49 cores averaged over180s; quota 2 |
| Peak sampled tasks | 2; no descendant process authorized |
| Cgroup OOM / kills | 0 / 0; cgroup empty after controller termination |
| Host available RAM before / after | 5,513,536 / 5,728,764 KiB |
| Host swap free before / after | 6,892,164 / 6,751,428 KiB; host observation, worker swap limit remains0 |
| GPU | No offload; no inference VRAM allocation measured |
| Real acceptance | 0 PASS / 1 FAIL (Timeout); zero engine calls |

The operator sampled counters 809 times at a nominal 250ms interval. No claim that a partial internal
model generation did not occur: only complete BRF1 delivery is observable here. Shared-cache accounting
means the lower cgroup peak versus the 30s run is not evidence of lower total model memory demand.
Model remains exact Qwen3-4B Q4_K_M and SHA below. The new native binary is separately preserved as
native-worker-180, SHA-256 `a88c7a31b6e92dcb3b3b5d6c1b456d2f71c79e5c3c516ecd1f1afb11ea8e4b95`;
the original30s binary is retained unchanged.

All four prior evidence files (SQLite database, empty WAL, SHM and audit) were hash-verified unchanged
before/after the test and independently afterwards. Reading used immutable/read-only SQLite with
empty-WAL verification. The prior Failed invocation remains spent; no scientific state was mutated.
New operational journal and measurements retain the 180s failure separately. No automatic retry.

Verification: focused B/C/E/F plus scoped-deadline test **194 PASS**; Release test-project build
**PASS, zero warnings/errors**. Tests prove ordinary 31s/180s settings reject, internal acceptance
requires exactly 180s, 181s rejects, option is non-public and Finance projection still says 30s.
Development syntax error corrected before inference; no invocation was spent on that build failure.
Full final candidate matrix is intentionally not run after the explicit stop: A is not complete.
Handoff documentation/index/link verifier PASS (258 Markdown /91 backlog IDs); diff check PASS;
Gitleaks exact13 intended files PASS/no leaks. No candidate commit/push.

Next: owner/architect evaluates whether to authorize a different measured configuration or a smaller
model. No model is selected or run without that decision. This remains the same BB-132A, not a new
generic hardening checkpoint. Valid structured proposal/admission and repeatability remain unproven.
Finance RESEARCH / 0 SEK / NONE. No deployment, cloud fallback, PAPER/LIVE/AUTO, broker/orders/capital,
merge or BB-132B. Existing scientific calculations, identities, holdout and risk policy unchanged.

## Historical 30-second result — 2026-09-29

The owner/architect resolved the earlier stock-CLI conflict: one directly owned native BRF1 worker
calls libllama, with no CLI child, HTTP server or listener. That implementation shape was used.
The first actual controlled Qwen attempt failed closed at F's unchanged 30-second deadline.
The terminal audit was written at 30,130 ms; the caller observed Timeout at 30,741 ms including
cleanup/ledger handling. No response header, parsed proposal or admitted experiment was obtained.
This is **not evidence that Qwen3-4B is too large**, nor proof that every parameter choice would fail.
It is evidence that this exact CPU-only configuration did not complete within the accepted budget.
No automatic/manual model retry, ledger reset, model substitution or deadline extension followed.

The existing C ledger was reopened read-only: lifecycle Failed, invocations 1, submissions/trials/
reserved runs/engine starts/reused results all 0, no commitment/result. ReasonerTimeout and the
subsequent denied invocation reservation are retained. No protected evaluation occurred.
The incomplete implementation remains local; no candidate commit or push is authorized as completed work.

## Artifact and runtime actually used

- Official Qwen revision `bc640142c66e1fdd12af0bd68f40445458f3869b`, Qwen3-4B Q4_K_M GGUF.
- Downloaded file size 2,497,280,256 bytes; actual SHA-256 verified twice before invocation:
  `7485fe6f11af29433bc51cab58009521f205840f5b4ae3a32fa7f92e8534fdf5`.
- Apache-2.0 LICENSE preserved locally. Weights are ignored local artifacts, never Git content.
- Official llama.cpp b11146, commit `7fe450e19305b828c199d602c23a8337aaa1f03b`, MIT runtime LICENSE
  preserved. CPU archive SHA-256 `c150306eb16b5ab696f76a8bdf810c35fd98a24e82158742e6fa28f420ff8410`.
- Only libllama/libggml/base/Haswell backend extracted; no CLI/server invocation or GPU backend.
  Native worker binary SHA-256 at probe: `589f176be195961f68718b1062b44a87891407ba5313a633f963450a58d76d7b`.
- Acquired by exact-revision HTTPS downloads, resumed byte ranges, final size/full SHA-256 check.
  No runtime download/latest lookup. Libraries and model remain under ignored `data/bb132a`.
- Parameters: CPU threads 2, context 4096, prompt cap 3072 tokens, batch/ubatch 128, greedy decoding,
  maximum 512 generated tokens, GPU layers 0, mmap, no extra repacking buffers. Complete EOS response
  required; no partial output, JSON repair or hand-edited answer.
- Prompt uses the existing synthetic development projection and honestly empty initial history,
  momentum/v1 period20 and existing falsification schema. No protected scope or invented outcomes.

## Containment and measured evidence

Before model access, the direct child verifies and joins only its private, empty, operator-created
cgroup. No existing service/cgroup settings were changed. Controls: memory.max 4 GiB, swap.max 0,
memory.oom.group 1, CPU quota 200000/100000, pids.max 32; RLIMIT_AS 4 GiB additionally bounds mappings
whose page-cache accounting might belong to acquisition. CPU time cap 60 s; F wall deadline 30 s.
Landlock allows only selected artifact reads and no filesystem writes. Seccomp denies networking,
exec/fork/process creation, signals and administration; only computation threads may be created.
Restriction precedes all threads; pidfd lifecycle remains F-owned. No arbitrary process target exists.
Operator preflight required available RAM exceeding 4 GiB plus 768 MiB headroom; this conservative
engineering margin is not a validated general production capacity policy.

| Measurement | Actual observation / limit |
| --- | --- |
| Host | i5-6600, 4 cores/threads; 7,863 MiB RAM; GTX1050Ti 4 GiB, unused by this CPU build |
| Host available RAM before / after | 5,026,208 / 5,209,988 KiB |
| Sampled peak cgroup memory | 2,312,208,384 bytes, about 2.15 GiB; not process RSS or all shared cache |
| Worker cgroup CPU delta | 24.944 CPU-seconds over the bounded invocation; quota throttling observed |
| Cgroup OOM / OOM kills | 0 / 0; owned cgroup empty after termination |
| Host swap free before / after | 6,864,528 / 6,770,228 KiB; host-wide observation, not worker swap (disabled) |
| GPU / throughput / first token | No GPU offload; tokens/sec, model-load completion and first-token latency not established |
| Real acceptance | 0 PASS / 1 FAIL: Timeout; no scientific engine call |

Measurements sampled dedicated cgroup counters every 250 ms. No claim of arbitrary native-code
sandbox certification, host-wide absence of pressure, or completed model-quality/repeatability proof.
Audit is sanitized JSONL with WriteThrough/fsync in a private operator-owned directory; existing
CallerKind remains InternalWorkloadUnattested, not a fabricated authenticated application principal.
No public endpoint or deployment integration was added. Broader production auth/audit remains future work.

## Verification and outstanding work

- Baseline B/C/E/F characterization: 193 PASS, unchanged accepted source.
- Native worker/probe: GCC14 C++20, `-O2 -Wall -Wextra -Werror`, PASS.
- Model-free Landlock/seccomp probe: denied TCP/Unix sockets, foreign read/write, exec/fork and signals;
  permitted restricted reads/threads and pinned libllama CPU bootstrap, PASS.
- Separate 64 MiB controlled OOM probe: kernel killed only owned probe, cgroup empty, PASS.
- `dotnet build tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-restore`:
  PASS, zero warnings/errors. Development-only analyzer/conditional-skip declaration errors corrected
  before the model invocation; earlier harness failure created no ledger/invocation.
- Opt-in `dotnet test ... --configuration Release --no-build --filter FullyQualifiedName~LocalModelAcceptanceTests`:
  FAIL as reported above; not part of ordinary CI. Model hash verification occurs before the F deadline.
- Full final test/format/build matrix and native adversarial suite remain unfinished; no green candidate claim.
- Normal test opt-out: 0 failed / 0 passed / 1 intentional skip; no model invoked by ordinary CI path.
- Handoff documentation verifier PASS (258 Markdown / 91 backlog IDs); diff check PASS;
  Gitleaks exact 12 intended files PASS/no leaks. Full candidate gates remain outstanding.

The immediate review question is the first-model deadline and bounded repeatability policy in this
same checkpoint. Both F options and Finance projection fix 30 seconds; C retains the consumed
invocation. Do not silently change those contracts or rerun against a fresh ledger to manufacture
success. A later explicitly authorized diagnostic/profile must retain this failed attempt.
No new generic hardening checkpoint is proposed. Successful real proposal/admission and repeatability,
complete native tests/reproducible build/run tooling, final verification and candidate publication remain.
Finance **RESEARCH / 0 SEK / NONE**. No cloud, provider data export, PAPER/LIVE/AUTO, broker/orders,
capital, deployment, merge or next checkpoint. ADR0040 and IResearchReasoner/BRF1 remain unchanged.

## Historical preflight — superseded runtime-shape stop

The following is dated evidence from before the architect's native-worker decision and real probe.
Its no-download/no-inference/architecture-stop statements are historical, not current status.

## Metadata

- Date: 2026-09-29.
- Authorized baseline and unchanged HEAD: `739beab55a494068edcf23d3c905aa6601b99dc0`.
- Branch: `bb-132a/first-local-language-model`, created once from verified origin/main.
- Owner model choice: Qwen3-4B, preferred Q4_K_M GGUF and llama.cpp CLI-style integration.
- ADR 0038/0039/0040 remain Accepted. BB-131F remains accepted and unchanged.

## Status

**BLOCKED — RUNTIME INTEGRATION ARCHITECTURE REVIEW REQUIRED**.
Preflight/source characterization completed; no production implementation, model download,
inference, integration acceptance, candidate commit/push or deployment.
This is not a reproduced BigBrain scientific/security defect and not a completed BB-132A candidate.
No claim that Qwen3-4B is too large or that all llama.cpp versions require a server.
The concrete stop applies to the investigated stock `llama-cli` release below.

## Evidence

### Baseline and hardware

`git fetch origin`, `git rev-parse HEAD origin/main` and working-tree inspection confirmed the
exact authorized baseline. Only unrelated mockups and unpublished Sentinel ADR 0006–0009 existed;
they remain untouched and excluded. No reset, clean, rebase or force operation occurred.

Read-only `lscpu`, `free -m`, `uname -r`, OS release, `lspci` and
`nvidia-smi --query-gpu=name,memory.total,memory.used,driver_version --format=csv,noheader` measured:

| Property | Observed, not a capacity guarantee |
| --- | --- |
| CPU | Intel Core i5-6600; 4 cores / 4 threads; AVX2 available |
| RAM | 7,863 MiB total; 5,400 MiB available at the initial observation |
| Swap | 8,106 MiB total; 1,319 MiB used at that observation |
| GPU | NVIDIA GeForce GTX 1050 Ti; 4,096 MiB VRAM, 117 MiB used at observation |
| Driver | NVIDIA 550.163.01; GPU query succeeded outside the development sandbox |
| OS/kernel | Debian 13.6; Linux 6.12.107+deb13-amd64 |
| Available tools | bubblewrap 0.11.0, GCC 14.2.0, unshare/prlimit/setpriv |

The first sandboxed GPU query failed; the host read-only query succeeded. Sandbox permissions
must not be mistaken for hardware incapacity. Cgroup v2 CPU/memory/pids controllers were present;
current session memory and CPU quotas were unlimited, not protective model limits. Outside the
sandbox, the session's delegated cgroup files were writable. No cgroup or limit was changed.
A model-free `unshare --user --map-root-user --net true` probe exited 0. This does not prove an
inference sandbox, filesystem isolation, OOM containment, descendant control or artifact safety.

No model load time, inference RSS/VRAM, CPU utilization, first-token latency, tokens/sec or total
inference time was measured. GPU identity alone does not establish runtime GPU support.

### Artifact provenance, metadata verification only

Official [Qwen GGUF repository](https://huggingface.co/Qwen/Qwen3-4B-GGUF), inspected through its API:

- Repository revision: `bc640142c66e1fdd12af0bd68f40445458f3869b`.
- File: `Qwen3-4B-Q4_K_M.gguf`; quantization Q4_K_M.
- Upstream-declared SHA-256: `7485fe6f11af29433bc51cab58009521f205840f5b4ae3a32fa7f92e8534fdf5`.
- Upstream-declared size: 2,497,280,256 bytes.
- Exact-revision LICENSE retrieved: Apache License 2.0, 11,544 bytes; model metadata also declares
  apache-2.0. This is artifact licence identification, not a grant of market-data export rights.
- Model weights were NOT downloaded; the upstream checksum has NOT been verified against local weights.

Any later acquisition must use the exact revision/file resolve URL, verify actual byte count and
SHA-256 before loading, preserve LICENSE/NOTICE obligations and store weights outside Git.
No runtime fetch-latest, account, API key, cloud model or external market-data export is needed.

Runtime inspected: official llama.cpp stable v0.5.0 metadata points to nightly **b11146**;
that release identifies commit `7fe450e19305b828c199d602c23a8337aaa1f03b`.
The official CPU archive `llama-b11146-bin-ubuntu-x64.tar.gz` was downloaded into temporary local
inspection storage, not installed, extracted into the application or executed:

- Size from release metadata: 16,998,357 bytes.
- Actual archive SHA-256 matched the release digest:
  `c150306eb16b5ab696f76a8bdf810c35fd98a24e82158742e6fa28f420ff8410`.
- [Pinned release](https://github.com/ggml-org/llama.cpp/releases/tag/b11146).
- This CPU archive is not evidence of CUDA/offload support, ABI compatibility or successful execution.
- Runtime bundle licence/third-party notices and build compatibility still require complete review
  before selecting an executable for inference. No selected production runtime exists yet.

### Concrete protocol/listener incompatibility

Pinned upstream source, not a runtime assumption:

- [cli-context.cpp](https://github.com/ggml-org/llama.cpp/blob/7fe450e19305b828c199d602c23a8337aaa1f03b/tools/cli/cli-context.cpp):
  local-model initialization creates `cli_server`, starts it and waits for its health endpoint.
- [cli-server.h](https://github.com/ggml-org/llama.cpp/blob/7fe450e19305b828c199d602c23a8337aaa1f03b/tools/cli/cli-server.h):
  chooses a free port, calls `llama_server` in a thread and communicates through loopback HTTP.
  It is a listener even though the server runs in the same process.
- Stock CLI input/output is not BigBrain's BRF1 frame protocol. F requires an eight-byte
  magic/length header, bounded payload and EOF; any stderr byte is rejected.
- An ordinary wrapper spawning stock CLI would also introduce a descendant outside F's owned
  direct-child lifecycle. ADR 0040 explicitly excludes descendants/process-tree authority.

Therefore the investigated stock CLI cannot be substituted for F's proof-worker executable.
Neither accepting localhost HTTP nor adding descendant supervision was implemented as a workaround.
This does NOT prove a direct libllama-backed BRF1 worker or another pinned non-listening upstream
entry point impossible. Those are alternative integration shapes requiring an explicit decision
and containment proof; no blanket llama.cpp incompatibility or model-performance conclusion is made.

### Other first-model boundaries observed

Current `LearningDevelopmentInput` has initial empty history, one momentum20 variant, three
underlying trials and a fixed 30-second reasoner deadline. The existing scope rejects a different
history checksum. C persistence is not a history-aware reasoner projector. Do not inject prior
outcomes, protected evidence or an invented history in Brain prompts to satisfy a richer scenario.
The first proof may truthfully say no prior experiments; any richer evidence projection needs its
own Finance-owned versioned design. No timeout, budget or admission rule was changed here.

F's audit callback is sanitized but has no durable owner implementation; caller metadata is
explicitly unattested. CPU/RAM/filesystem/network/descendant isolation remains unimplemented.
Hardware/kernel facilities are promising prerequisites, not enforced first-model controls.

### Verification scope

No source/test/project/package/configuration changed. Expensive backend suites and real-model tests
were not run: they cannot establish a nonexistent integration. BB-131F main CI evidence remains
historical evidence for its unchanged baseline, not BB-132A verification.
Documentation/link/scope/diff and secret checks are recorded in the canonical recovery note.

## Changes

Local documentation only: status/backlog, owner product roadmap and Finance principle, master
roadmap pointer, report/catalog and canonical recovery note. No accepted ADR decision is rewritten.
No production/native adapter, HTTP endpoint, process supervisor, schema or scientific change.

## Security

Finance remains **RESEARCH / 0 SEK / NONE**. No model inference, provider activation, credentials,
PAPER/LIVE/AUTO, broker/order/capital or deployment. Existing parser/admission, holdout/no-lookahead,
risk veto, ledger and Sentinel read-only semantics are unchanged by the documentation-only diff.
No real provider data was sent to an AI service. External requests fetched public provenance/source
material only. Existing unrelated local material remains untouched.

## Remaining work

Resolve the concrete runtime integration shape before implementation/inference:
prefer evaluating a single native BRF1 worker using pinned libllama directly, retaining F's pipes
and exact owned PID, rather than permitting a listener or a wrapper/child tree. Its trusted
bootstrap, resource/egress/filesystem/child-creation controls, local authenticated owner scope and
durable bounded audit must be specified and proven before any weights are loaded.
This is a proposed route, not accepted architecture or a claim that containment is already solved.

Then acquire/verify weights, implement bounded adapter and tests, measure capacity and real
repeatability within authorized policy, preserve Finance admission and run all final repository
gates. Do not silently lengthen the accepted deadline, change model or fabricate a passing reply.
No model-size downgrade is recommended without actual measured inference evidence.

## Resumption

Read AGENTS/START-HERE and the single canonical recovery note; verify exact main and preserved
branch/local documentation. Continue this branch after the runtime-shape decision. Do not restart
BB-132A or use an old checkpoint branch. No candidate is complete or published.
Next action: STOP — return the bounded runtime compatibility finding to owner/architect for a
specific integration decision; do not invoke a model or weaken F/Finance to force completion.
