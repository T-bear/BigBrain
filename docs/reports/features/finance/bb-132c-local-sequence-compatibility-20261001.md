# BB-132C — Local-model finite-session compatibility review

Detta är en sanerad GitHub-version. Model-free integration evidence only; no raw model context,
responses, private runtime paths, production data or credentials are published.

## Metadata

- Date: 2026-10-01. Checkpoint: BB-132C, first real local-model persistent research sequence.
- Goal: real Qwen N -> Finance evaluation/native persistence -> reopen -> permitted history -> real N+1,
  with at most two invocations and one scientific evaluation; no trading or recurring operation.
- Baseline derived from fresh fetch of accepted origin/main, not copied from chat:
  `cab482c49d2187a3f39c13528a5fb88d899f6b8f`.
- Baseline tree: `0b2b3edfdef25b32290754fa9aae364045c66c89`.
- Branch: `bb-132c/local-model-research-sequence`.
- Publication subject: `review: characterize BB-132C local session compatibility`.
- Main contains accepted BB-132A and BB-132B. ADR0038/0039/0040 remain Accepted.

Resolve this exact review SHA/tree after future commits using Git metadata:

```sh
git log -1 --format='%H %T %P' --grep='^review: characterize BB-132C local session compatibility$' origin/bb-132c/local-model-research-sequence
git diff cab482c49d2187a3f39c13528a5fb88d899f6b8f <resolved-review-SHA> --stat
```

## Status

**REVIEW CHECKPOINT / INCOMPLETE / NOT A MERGE CANDIDATE.** Compatibility stop before real inference.
Production code is unchanged. No pre-existing correctness/security/science defect is claimed: A's
initial projection and B's conservative model-free deadline were intentional accepted constraints.
They do not yet provide a consistent history-aware real-model composition under unchanged controls.
This is an ordinary incomplete Review Checkpoint, not BLOCKER HANDOFF for a historical defect.

C is authorized, superseding earlier dated B-publication statements that C was not authorized.
Its real-session allowance remains **UNUSED: session not created, 0 of at most2 real invocations**.
No model artifact loaded, native worker executed/rebuilt, new journal/grant created, or A allowance
reused. No real runtime/resource measurements were made in C. Ordinary tests remain model-free.
Full product acceptance has NOT been demonstrated. Publication does not authorize merge.

## Evidence

### Reuse and model-free composition

`EodhdMarketMemory.RunFiniteResearchIterationAsync` already owns the production finite orchestration.
Its existing narrow delegate accepts `IResearchReasoner.ReasonAsync` without another session runner,
Brain-to-SQL access or API-to-Brain runtime registration. New tests bind `LocalReasonerRuntime`
through that interface directly and use F's existing deterministic proof executable as the child.
This exercises the actual session, controller, owned process, BRF1, strict parser, admission,
scientific evaluator, native persistence, reopen and history policy. It is not a libllama/model test.
No production adapter or second orchestration path is necessary for this binding.

Two success-path test cases evaluate N with the unchanged deterministic evaluator, persist native
references, reopen through existing Finance readers and project N+1's fixed-reference validation
excess return/previous outcome/availability. N+1 traverses the same real local controller/BRF1 path;
its audit input hash equals the exact serialized history-bearing Finance projection. The proof
worker decodes the canonical projection and binds its response to that input checksum.
N+1 either declines (NoUsefulProposal) or proposes again (BudgetExceeded). EngineStarts stays1,
trials3, underlying runs<=64, exposureConsumed and native ledger checksum unchanged. Replay starts
no worker and iteration3 fails closed. No protected result ID/checksum enters the projection.
Existing B noninterference/temporal/history-corruption tests remain the authoritative broader proof.

Three negative runtime cases (malformed reply, premature exit, hung worker with a bounded short
test timeout) retain the spent first session invocation, execute zero scientific evaluations, block
N+1 and replay without retry. Runtime audit preserves only existing sanitized outcome/exit/rejection
metadata; malformed output reports the actual Finance Malformed category.

### Exact compatibility findings

| Boundary | Accepted source behavior | Consequence |
| --- | --- | --- |
| Finance finite session | `FinanceFiniteResearchSession.cs` unconditionally calls `deadline.CancelAfter(TimeSpan.FromSeconds(30))` before awaiting the reasoner; `LearningDevelopmentInput.Limits.ReasonerDeadlineSeconds` is30 | A controlled180s runtime cannot extend this shorter Finance parent deadline. |
| Local runtime | `LocalReasonerRuntimeOptions` permits180s only via internal `ControlledRealModelAcceptance`; ordinary configuration remains<=30s | Passing A's accepted configuration does not solve the session deadline. No existing session override exists. |
| Accepted A latency evidence | [Final A evidence](bb-132a-local-model-preflight-20260929.md#final-bb-132a--2026-10-01) records complete-frame50,408ms and terminal50,458ms under180s | The accepted successful observed duration exceeded B's deadline. This is historical evidence, NOT a prediction that every future call needs>30s. |
| Native system prompt | `worker.cpp::prompt` unconditionally says `Initial history is empty: no previous experiments/results exist in this projection.` and concatenates the supplied JSON | Evaluated N+1 supplies real nonempty `FiniteHistory`, while the immutable prompt asserts the opposite. No history-version branch exists. No claim is made about Qwen's response to the contradiction. |
| Projection/transport/parser | B's versioned history serializes/round-trips through existing BRF1 and the unchanged Finance parser accepts properly bound replies | No evidence justifies changing history selection, accepted vocabulary, reply grammar or parser/admission. |

The new deadline test uses the model-free hung proof child with the real controller configured for
180s. The unchanged session cancels at its30s deadline: FiniteIterationFailure.TimedOut, terminal
controller audit Cancelled, owned cleanup required, observed child exit, zero engine calls, spent
invocation and blocked N+1. Runtime disposal is awaited so session completion is not mistaken for
child termination. The test's29–60s wall assertion tolerates scheduling/cleanup; it is not a new
production limit. The prompt test reads the actual worker source copied by the existing test project,
not a substituted template, and characterizes the unconditional empty-history sentence.

The accepted [local-first resource policy](../../../architecture/finance/local-first-reasoner-boundary.md#resource-policy-failure-and-disable-controls)
requires separate review to change protocol deadlines. C explicitly preserves A isolation/B scientific
boundaries and says to stop rather than weaken them. Therefore no deadline increase, prompt edit,
projection rewrite or speculative real invocation was performed. The mismatch is recorded before
consuming the one bounded session, instead of turning an avoidable integration question into spent
real-model evidence. The next decision is narrow; no general hardening platform is proposed.

### Verification

All checks below passed on the final test source. Seven new model-free cases; no existing test
or production file changed. No model opt-in was enabled.

| Exact command | Result |
| --- | --- |
| `dotnet restore BigBrain.slnx` | PASS, existing dependencies up to date |
| `BB132A_LOCAL_ACCEPTANCE=disabled dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~FiniteLocalReasonerIntegrationTests\|FullyQualifiedName~FiniteResearchSessionTests\|FullyQualifiedName~LocalModelContractCharacterizationTests'` | 48 PASS / 0 FAIL / 0 SKIP (7 new cases, 27 B-session, 14 native-grammar characterization) |
| `dotnet build BigBrain.slnx --configuration Release --no-restore` | PASS, 0 warnings / 0 errors |
| `dotnet format BigBrain.slnx --verify-no-changes --no-restore` | PASS, exit0, no changes |
| `BB132A_LOCAL_ACCEPTANCE=disabled dotnet test BigBrain.slnx --configuration Release --no-build` | API913 PASS / 0 FAIL / 7 intentional real-model skips; Sentinel32 PASS / 0 FAIL |
| `node scripts/verify-documentation.mjs` | PASS,260 Markdown files / 91 unique backlog IDs; links/indexes checked |
| `git diff --check` and staged diff check | PASS |
| `gitleaks git . --redact --no-banner` (8.28.0) | PASS,304 commits / no leaks |
| `gitleaks dir <exact-candidate-export> --redact --no-banner` (8.28.0) | PASS, intended11files / no leaks; final staged content rescanned before commit |

Full suite includes existing Finance science, B/C/E/F/runtime/session/persistence/risk regressions.
Initial sandbox MSBuild IPC permission failures were retried with required escalation. One new-test
xUnit2013 assertion was corrected before green tests; no accepted implementation was repaired.
These are local model-free results, not branch/main CI or real-model acceptance evidence.
No frontend behavior change; Web files/configuration are unchanged. No native build or inference.

### Existing provenance, not a new artifact verification claim

Accepted A pins `unsloth/Qwen3-1.7B-GGUF` revision `d7f544eead698dbd1f15126ef60b45a1e1933222`,
Q4_K_M,1,107,409,472bytes, SHA-256
`b139949c5bd74937ad8ed8c8cf3d9ffb1e99c866c823204dc42c0d91fa181897`, Apache-2.0 provenance.
CPU libllama b11146 commit `7fe450e19305b828c199d602c23a8337aaa1f03b`, MIT;
accepted final native worker SHA-256 `631deb1ccfece198309c0485ada2d5eae62ecd3b146c1efc945078e97dd2eb1b`.
See the linked A report for acquisition, runtime/license manifests, isolation and measured resources.
C neither downloads nor modifies/revalidates these local artifacts because inference was stopped
before provisioning. Their current on-disk integrity/resource readiness must be checked before any
later explicitly resolved invocation. A's journals, consumed grants and scientific evidence were
not opened for mutation or reused. C test databases are isolated temporary synthetic fixtures.

## Changes

Only one new test file and ten Markdown documents:

- `tests/BigBrain.Api.Tests/FiniteLocalReasonerIntegrationTests.cs`
- `ROADMAP.md`
- `TESTING.md`
- `docs/STATUS.md`
- `docs/BACKLOG.md`
- `docs/modules/finance.md`
- `docs/architecture/finance/master-roadmap.md`
- `docs/architecture/finance/local-first-reasoner-boundary.md`
- `docs/operations/codex-recovery.md`
- `docs/reports/REPORT-CATALOG.md`
- `docs/reports/features/finance/bb-132c-local-sequence-compatibility-20261001.md`

README, architecture baseline/ADRs, history contract, security/runbooks and indexes were assessed;
existing contracts remain authoritative and no new decision/runtime surface needs to be recorded
there. Catalog and canonical current-status links index this report. Historical A/B reports remain
unchanged. Unrelated mockups and unpublished ADR0006–0009 are preserved and excluded.

## Security

Finance **RESEARCH / 0 SEK / NONE**. AI MAY PROPOSE. DATA MUST PROVE. RISK MAY VETO.
No parser/admission tolerance, vocabulary, history/no-lookahead/holdout, risk, ledger, schema, native
scientific result or deterministic engine change. No runtime/worker/isolation/resource control change.
Test success does not claim model understanding, OS sandboxing, profitable edge or real inference.
No raw model prompts/replies or sensitive runtime evidence published. Fixed public source prompt
excerpt is configuration characterization, not a captured model request. No Qwen/other inference,
model download/tuning/switch, GPU/CUDA, provider/cloud, credentials, API endpoint, daemon/scheduler,
Web change, Sentinel change, trading/broker/orders/capital, deployment or BB-132D. Main unchanged.

## Remaining work

Owner/architect review must resolve the smallest compatible continuation before real invocation:

1. Decide whether C may use an explicit controlled finite-session deadline matching the accepted
   A180s ceiling while keeping ordinary/model-free defaults30s, count2/evaluation1/no-retry authority
   and spent-state semantics unchanged. No such exception is implemented or inferred here.
2. Decide whether the native prompt may describe existing Finance `FiniteHistory` faithfully instead
   of unconditionally asserting empty history. No new projected fields, inference settings, grammar
   vocabulary, scientific authority or model tuning is proposed. Rebuild/provenance verification
   would be required if that source change is authorized.
3. Complete the approved internal composition/journal, model-free gates and artifact/isolation checks;
   only then consume at most the original two real C invocations, with no retries/refunds/third call.
4. Record actual real N/evaluation/native evidence/reopen/N+1/exhaustion and post-run regressions,
   or publish its actual fail-closed early outcome. Until then C cannot be a Merge Candidate.

No new real inference authorization is requested beyond the user's existing conditional grant;
its prerequisite compatibility decision remains unresolved. No A allowance is renewed.

## Resumption

Read AGENTS/START-HERE, fetch main and this review branch, resolve the publication subject above,
then read this report and the canonical recovery note. Preserve this commit and continue only on
this same branch if the owner/architect resolves the explicit compatibility boundary. Do not
recreate implementation, rewrite history or convert review publication into acceptance.
After this Review Checkpoint push: STOP for independent ChatGPT review. No merge or BB-132D.
