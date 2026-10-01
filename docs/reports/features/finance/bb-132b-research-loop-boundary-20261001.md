# BB-132B — Finite persistent model-free research session

Detta är en sanerad GitHub-version. Synthetic engineering evidence, not market-edge evidence.

## Metadata

- Date: 2026-10-01. Baseline/main: `2caf2eb898acaafa759c6287d02442a90bb666db`.
- Same branch: `bb-132b/persistent-research-loop`.
- Preserved first Review Checkpoint: `3d5cc19903223027de6559d98ebce785b92032bf`,
  tree `6c92e0627044644704f0ddd3180df70e8c6e616d`.
- Publication subject: `review: implement BB-132B finite research session`.
- Finance: **RESEARCH / 0 SEK / NONE**. MODEL-FREE ONLY. No deployment.

Exact publication SHA/tree are Git metadata, not impossible self-referential file contents:

```sh
git log -1 --format='%H %T %P' --grep='^review: implement BB-132B finite research session$' origin/bb-132b/persistent-research-loop
git diff --name-only 2caf2eb898acaafa759c6287d02442a90bb666db origin/bb-132b/persistent-research-loop
```

## Status

**IMPLEMENTED / AUTOMATICALLY VERIFIED / MERGE CANDIDATE — NOT ACCEPTED OR MERGED.**
The owner/architect independently reviewed the first checkpoint and explicitly authorized the
smallest new finite protocol: two invocations, at most one evaluation, three trials/64 underlying
calls, concurrency one, no retries/refunds. This resolves the earlier intentional architectural
stop without reinterpreting old v1 rights. The prior characterization remains valid for v1.
No real-model validation is authorized. Consumed BB-132A allowances remain consumed.

## Changes

### Ownership and explicit grant

`FinanceFiniteResearchSession.cs` is another partial file of the existing `EodhdMarketMemory`
Finance SQLite owner. Migration95, through `FinanceSchemaMigrator`, creates one EMPTY singleton
session table; no seed grant, v1 conversion, result copy or second database. Migration rollback,
idempotence and unchanged legacy bytes are tested. The only change inside the original C ledger
file allows the known additive schema95 alongside94; unknown future schema96 still rejects.

`CreateFiniteResearchSession` is explicit trusted internal Finance administration, not a reasoner
capability or public endpoint. It requires validated synthetic scope, all source knowledge already
available, and **Uninitialized v1 state**. Ready/enrolled/spent/unknown old programs cannot acquire
the grant, even through explicit creation. Opening/migrating a database grants nothing. One fixed
session per database prevents caller-selected session names renewing budget/exposure. Repeated
creation with identical frozen scope is idempotent; scope replacement rejects.

Creation atomically enrolls the unused v1 scientific slot and freezes a separate
`finance-finite-research-session-v1` grant (2/1/3/64/concurrency1/retries0). First invocation atomically
reserves BOTH the new invocation and v1's original one-invocation slot, using the same v1 state
transition and existing state validator. Existing v1 records retain their contract, counters and
one-evaluation semantics. No previously spent record is written by the creation path.

The new version grants N+1 reasoner work, not renewed scientific authority. Evaluation is available
only at N; if N declines/rejects, this conservative proof still does not transfer that opportunity
to N+1. This is a maximum-one-evaluation grant, not a promise of an evaluation. Trial/exposure/
underlying-run accounting remains in C; session invocation/outcome accounting is additive.

### Finite orchestration and native evidence

Finance's internal async method takes bounded invocation identity, expected input checksum/cutoff,
a trusted clock and a narrow delegate bound to `IResearchReasoner.ReasonAsync`. This avoids an
API-to-Brain reference/registration and keeps SQL ownership out of Brain. The fake receives only
`LearningDevelopmentInput` and cancellation. No service locator, process/tool/network capability.

N reserves durably, invokes once, discards late responses, reparses the original reply and uses
unchanged `ReserveLearningProposal`/`LearningAdmissionPolicy`, `StartLearningExecution`,
`DeterministicRobustnessEvaluator.Evaluate`, `PersistLearningResults`, and `CompleteLearningIteration`.
The existing backtester, momentum20/reference and internal5/10/20 grid, cost/fill/OOS/holdout rules,
FinanceBacktestPersistence and immutable robustness/backtest stores are unchanged.

C retains full proposal/plan/execution/result identities privately. The finite envelope binds its
scope checksum, session/iteration number, frozen input checksum, response checksum, exact scientific
ledger checksum and terminal outcome. Native scientific payloads are not copied into the session.
A mismatched/missing native reference or changed authoritative history blocks subsequent projection.

N+1 reparses against its own frozen input through the same strict Finance reply parser. A normal
NoUsefulProposal is retained. Any otherwise allowed proposal receives **BudgetExceeded**, even if
it repeats N with new wording/model/request metadata. There is no second engine path and no
independent duplicate success. Explicit same-iteration replay returns the exact retained outcome;
it never invokes a reasoner or writes duplicate scientific rows.

### Versioned visible history and temporal integrity

`FiniteResearchHistory` is a Finance-owned fixed-size value, with no arbitrary prose, collections,
result identifiers or generic evidence links. It is an optional `LearningDevelopmentInput` member,
omitted from v1 JSON and canonical hashes when absent. No old v1 projection is silently extended.
N uses the unchanged initial projection. N+1 uses `finance-finite-history-v1` and contains:

- the previous iteration number and bounded admission/decline/rejection outcome;
- explicit UTC availability and knowledge-cutoff times;
- the fixed momentum20 reference **validation** excess return in existing fractional units, only
  after verified native completion; missing remains null, never fabricated zero;
- explicit remaining scientific evaluations = zero; bounded counts/limits and canonical history/input digests.

The metric is `PrimarySplit.Test.ExcessReturn`, calculated on the preselection reference validation
window. It is NOT a selected-candidate metric, full robustness verdict, score, holdout return or
success claim. No selection/holdout identity, full result/checksum, run/proposal/execution ID, raw
bars/features, rationale, provider payload or private ledger crosses the projection. Model/provider
identity cannot change the scientific grant. All prior permitted outcomes are included (there is
exactly one prior iteration), without a positive-outcome selection filter.

Session creation checks actual source knowledge <= trusted creation time. Invocation cutoff must
be UTC, >= session creation and previous completion availability, and <= the trusted reservation
clock. N's existing development data cutoff remains distinct from the later operational availability
cutoff. N+1 cannot backdate access to an outcome; ordering/clock/identity mismatches fail before
reasoner invocation. The exact projection is frozen durably and rederived/checked on reopen against
the native source, including its history/input digest. Mere existence of a SQLite result is insufficient.

Noninterference test: hold plan/cohort membership and trusted operational times fixed; change ONLY
protected holdout prices and corresponding features; run actual existing evaluator in two isolated
synthetic databases. Native checksums and holdout returns differ; the entire serialized visible input,
history digest and input digest are identical. Internal scope/session/ledger/native identities may
differ and are never projected. This proves the bounded fixed-cohort projection, not arbitrary
cross-dataset overlap or covert timing-channel elimination. Invalid native state fails closed and
cannot be used as a request for wider evidence.

### Failures, concurrency and recovery

Each invocation is Reserved -> Completed / Failed / Indeterminate. Its ordinal is the spent
invocation count. SQLite immediate transactions arbitrate independent connections, not an in-process
lock. At most one pending invocation exists; N+1 requires completed N and the same authoritative
history. A concurrent/uncertain reservation rejects with no queue or hidden retry.

Reasoner calls have the existing conservative 30-second model-free deadline plus caller cancellation.
A late typed reply is discarded. No default is relaxed for models or ordinary CI. Operational or
parser exceptions from the closed reasoner port are retained as sanitized ReasonerUnavailable;
exact raw exceptions/prompts/replies are not persisted. A typed foreign-input reply is reparsed and
retained as EvidenceMismatch. Failure ends this finite session's progression; failures/uncertainty
stay visible in governance, but do not trigger another model invocation merely to discuss failure.
Ordinary parsed decline/rejection history is eligible for N+1; it cannot grant evaluation.

A crash before transaction commit leaves no invocation; after commit it remains spent/Reserved.
Explicit recovery may classify it Indeterminate, never retry/refund. Result writers retain their
existing separate immutable transactions. A crash after native writes and before ledger binding
leaves existing results and an uncertain spent evaluation, never reruns to repair history. This
checkpoint conservatively stops that session; broader reconciliation policy remains future work.
Completed replay verifies IDs/checksums and emits no model/engine call. No scientific result is
inferred from audit or session state. No production authentication or unattended orchestration claim.

## Evidence

Focused tests: **227 PASS / 0 FAIL / 0 SKIP** (27 new session cases plus unchanged B/C/E/F and
first-review characterization). The first24 new cases also passed before adding three extra
cancellation/integrity/v1-wire tests. Development analyzer errors were confined to new code/test
properties and cancellation plumbing, corrected before the passing focused run.

```sh
BB132A_LOCAL_ACCEPTANCE=disabled dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~FiniteResearchSessionTests|FullyQualifiedName~ResearchLearning|FullyQualifiedName~ResearchReasonerContractTests|FullyQualifiedName~FinanceLearningLedgerTests|FullyQualifiedName~LocalReasonerRuntimeTests'
```

Acceptance matrix is covered by `FiniteResearchSessionTests`: explicit grant; N actual fake invocation;
unchanged admission; single real deterministic evaluation; native persistence/checksums; reopen;
N+1 actual history-aware invocation and decline/second-proposal exhaustion; old-v1 incompatibility;
malformed/unsupported/operational/cancellation failures; replay without reasoner; missing/mutated
native/history identity; future/unavailable cutoff; independent-connection contention for both slots;
holdout noninterference; migration and crash commit boundaries. Ordinary CI requires no model.

Final verification, against the completed implementation (no subsequent code changes):

| Command / check | Observed result |
| --- | --- |
| `dotnet restore BigBrain.slnx` | PASS, projects up-to-date |
| `dotnet build BigBrain.slnx --configuration Release --no-restore` | PASS, 0 warnings / 0 errors |
| `dotnet format BigBrain.slnx --verify-no-changes --no-restore` | PASS, no changes |
| `BB132A_LOCAL_ACCEPTANCE=disabled dotnet test BigBrain.slnx --configuration Release --no-build` | API **906 PASS / 0 FAIL / 7 intentional real-model skips**; Sentinel **32 PASS / 0 FAIL** |
| `node scripts/verify-documentation.mjs` | PASS, **259 Markdown files / 91 unique backlog IDs**, links/indexes included |
| `git diff --check` / staged equivalent | PASS |
| Gitleaks8.28.0 `git --redact --no-banner` | PASS, **302 commits** scanned, no leaks |
| Gitleaks8.28.0 `dir` on exact cumulative20 intended file contents | PASS, no leaks; staged content rechecked before commit |
| `git diff --exit-code <baseline> -- <protected paths>` | PASS: Brain/runtime/native tooling, Web, Sentinel/contracts, proof worker, workflows, Finance parser/admission/evaluator/native persistence files unchanged |

Full solution tests include Finance science, cost/fill/OOS/holdout, historical campaign/autonomous,
persistence/migrations/risk and local reasoner regressions. No frontend change or new frontend test.
Formatter write was restricted to the seven intended C# paths before final build/tests/verification.
Documentation verifier's Git child required the approved non-sandbox execution after EPERM.
Development sandbox prevented
one MSBuild execution without compiler diagnostics; unchanged commands run with the approved
execution permission. No sandbox failure is reported as a scientific test failure.

## Exact file inventory

This continuation changes **19 files**: five production C# files, two test files and twelve
Markdown documents. The cumulative diff from main has **20 files**, adding the first review's
unchanged `ResearchLearningIterationBoundaryTests.cs`. No other file is included.

- `ARCHITECTURE.md`
- `ROADMAP.md`
- `TESTING.md`
- `docs/BACKLOG.md`
- `docs/STATUS.md`
- `docs/architecture/finance/local-first-reasoner-boundary.md`
- `docs/architecture/finance/master-roadmap.md`
- `docs/architecture/finance/research-learning-contract.md`
- `docs/modules/finance.md`
- `docs/operations/codex-recovery.md`
- `docs/reports/REPORT-CATALOG.md`
- `docs/reports/features/finance/bb-132b-research-loop-boundary-20261001.md`
- `src/BigBrain.Api/Finance/FinanceFiniteResearchSession.cs`
- `src/BigBrain.Api/Finance/FinanceLearningLedger.cs`
- `src/BigBrain.Api/Finance/FinanceSchemaMigrations.cs`
- `src/BigBrain.Modules/Finance/FiniteResearchHistory.cs`
- `src/BigBrain.Modules/Finance/ResearchLearningContracts.cs`
- `tests/BigBrain.Api.Tests/FinanceLearningLedgerTests.cs`
- `tests/BigBrain.Api.Tests/FiniteResearchSessionTests.cs`
- `tests/BigBrain.Api.Tests/ResearchLearningIterationBoundaryTests.cs`

README, ADRs/indexes, security/operations runbooks and other module docs were assessed: no new
ADR/deployment/runtime boundary, document path or operational procedure needs a parallel update.
The existing report/catalog and canonical Finance architecture carry the durable result. Old A
reports/evidence and the initial B characterization remain historical, not rewritten as acceptance.

## Security

Scientific admission/parser/strategy vocabulary, historical identity algorithms, cost/fill/OOS,
risk/entitlements and result persistence semantics are unchanged. The new grant is explicit and
finite. No result promotion, statistical/profitability claim, scientific refund or holdout renewal.
No model-generated behavior executes. The native runtime, prompt/grammar, model/artifact, provider,
Sentinel, Web, packages, CI and deployment configuration remain untouched.

No Qwen/other inference/download/tuning, GPU/CUDA/cloud fallback, provider access, external export,
PAPER/LIVE/AUTO, broker/orders/capital, scheduler/daemon/public endpoint/deployment or BB-132C.
Unrelated mockups and unpublished ADR0006–0009 remain untouched/excluded. Prior A evidence and
consumed invocation journals remain untouched. Finance **RESEARCH / 0 SEK / NONE**.

Baseline CI's prior documented Gitleaks-action lookup/license failure (run36844601675 attempt1)
is separate from these local checks. The owner explicitly chose this accepted baseline. No CI
repair/rerun/license change is undertaken and no green branch/main CI is inferred from local tests.

## Remaining work

Independent exact-SHA review and explicit owner acceptance/merge remain required. This candidate
is only the finite synthetic/model-free proof. No existing spent v1 database conversion, arbitrary
new sessions, second evaluated experiment, general adaptive cohort/overlap accounting, evolving
strategy vocabulary, arbitrary history pagination, real-market eligibility or profit evidence.
Real-model testing of the new history projection, authenticated invocation/audit integration,
recurring operation and deployment each need separate authorization. No old inference allowance
may be reused. An uncooperative test reasoner can ignore cancellation internally; the controller
stops awaiting and blocks further session progression. It grants no OS sandbox/termination claim;
F's runtime owns such worker lifecycle enforcement when separately authorized.

## Resumption

Do not recreate the branch or rewrite the first review. This report's publication subject resolves
the new exact SHA/tree; canonical recovery records final verification/publication state. If interrupted,
inspect Git/working tree before completing remaining gates/publication. No model invocation.
After publication STOP for independent ChatGPT review. Do not merge or start BB-132C.

## Historical first Review Checkpoint

The following dated characterization and its original remaining-work statements describe
`3d5cc19903223027de6559d98ebce785b92032bf`, not the subsequent authorized implementation above.

### Metadata

- Date: 2026-10-01.
- Goal: retained eligible history -> Finance projection -> IResearchReasoner -> existing admission,
  deterministic evaluation and persistence -> a subsequent bounded history-aware iteration.
- Baseline: `2caf2eb898acaafa759c6287d02442a90bb666db`, verified equal to origin/main after fetch.
- Branch: `bb-132b/persistent-research-loop`.
- **REVIEW CHECKPOINT / INCOMPLETE / NOT A MERGE CANDIDATE — protocol decision required.**
- Production orchestration: NOT IMPLEMENTED. Model-free characterization: implemented.
- Finance: **RESEARCH / 0 SEK / NONE**. No model inference or deployment.

Resolve the exact review SHA/tree from GitHub history; no self-referential commit hash:

```sh
git log -1 --format='%H %T' --grep='^review: characterize BB-132B iteration authority boundary$' origin/bb-132b/persistent-research-loop
git diff --name-only 2caf2eb898acaafa759c6287d02442a90bb666db origin/bb-132b/persistent-research-loop
```

### Status

This checkpoint is authorized, but the request also requires preserving existing budget/exposure
limits. Inspection found that those concrete limits prohibit N+1 before its reasoner can run.
This is an intentional accepted restriction, not a reproduced correctness/security/science defect.
The AGENTS architecture-conflict stop and ordinary Review Checkpoint path apply; this is not a
pre-existing-defect BLOCKER HANDOFF, nor evidence of a completed autonomous research loop.

### Exact reuse findings

| Existing owner / code | Verified responsibility and constraint |
| --- | --- |
| `ResearchLearningContracts.cs`: `LearningDevelopmentInput` | `History` is a getter fixed to zero counters/Unexposed; `Limits` fixes one invocation, one submitted proposal, one evaluation, three trials, 64 underlying runs, concurrency one and zero retries. No prior-outcome facts are represented. |
| `SyntheticLearningScope` | Accepts only the empty-history checksum; any nonempty digest yields `UnsupportedScope`. Restoring a frozen scope reconstructs the original initial projection. It is not a history selector. |
| `ResearchLearningAdmission.cs` | Nonzero counters reject new admission. NoUsefulProposal after prior submitted/trial/evaluation counters also returns BudgetExceeded. Duplicate fingerprint links old evidence; it cannot create another opportunity. |
| `FinanceLearningLedger.cs` | One singleton fixed synthetic program/family, one frozen scope/commitment/result. ReserveLearningInvocation grants only Ready + Invocations=0 + Unexposed. State validation rejects Invocations>1 and EngineStarts>1. Completion has no next-iteration transition. |
| `LocalReasonerProtocol.cs` | Exact closed canonical input comparison rejects mutated fixed history or limits. Keeping the port method signature does not make a new projection version automatically compatible. |
| `FinanceBacktestPersistence` / `FinanceRobustnessStore` | Reusable immutable native results/readers; each writer has its existing transaction. These stores need not become a second experiment store. |
| `DeterministicRobustnessEvaluator` | Existing combined selection/holdout calculation, momentum20 caller variant and engine-owned 5/10/20 grid. No development-only adaptive-evaluation mode is assumed. |
| `IResearchReasoner` / E test harness | Existing typed input/cancellation and closed reply work for the initial iteration. API has no Brain reference; E orchestration is test-only. Brain does not read Finance SQLite. |

Source paths are under `src/BigBrain.Modules/Finance`, `src/BigBrain.Api/Finance` and
`src/BigBrain.Brain`. Existing tests consulted: ResearchLearningContractTests,
FinanceLearningLedgerTests, ResearchReasonerContractTests and LocalReasonerRuntimeTests.
Accepted A–F/A reports, ADR 0038/0039/0040, ARCHITECTURE, the research-learning and local-first
contracts, module/roadmap, STATUS/BACKLOG, TESTING and report/publication rules were reconciled.

The [accepted contract](../../../architecture/finance/research-learning-contract.md) explicitly
requires separate review of cumulative budgets/cohort dependence for later multi-iteration work.
The [local-first contract](../../../architecture/finance/local-first-reasoner-boundary.md#finance-owned-projection-contract)
explicitly documents empty initial history, no supported completed-C-history -> next-invocation path,
and a separately reviewed versioned history projector. C's accepted report documents deliberate
absence of caller-selected protocol IDs, parent chains and arbitrary additional iterations.

### Evidence

#### Model-free characterization

New `ResearchLearningIterationBoundaryTests.cs` adds five cases without production changes:

1. Reuse E's test-only invocation with a deterministic IResearchReasoner fake and C's isolated SQLite.
   N admits a valid momentum proposal, executes the existing evaluator exactly once, persists native
   results, completes C, then reopens and checks evaluation ID/checksum against the existing reader.
   Invocation=1, trials=3, EngineStarts=1, exposure=Consumed; result references survive.
   Restored input still has zero history counters and the original empty history digest.
   A would-be N+1 fake returning NoUsefulProposal is never called: reservation rejects BudgetExceeded.
   Existing commitment/result and full prior history prefix survive; a rejection is appended.
   Direct pure NoUsefulProposal admission with retained counters likewise rejects BudgetExceeded.
2. A nonempty synthetic history digest fails scope validation and actual pure admission with
   UnsupportedScope, even with the matching input checksum.
3. Real .NET protocol decoding rejects injected history.evaluations=1.
4. Real .NET protocol decoding rejects injected limits.reasonerInvocations=2.
5. After a reasoner-unavailable failure, enrollment of a different synthetic scope throws without
   modifying state; reopening retains the failure and spent invocation. No execution is granted.

The fake has only input/cancellation and a fixed response. No tools, network, model runtime or
SQLite dependency is passed to it. N's scientific computation is synthetic engineering evidence,
not a real retained-data operation or market-performance claim. N+1 is deliberately NOT executed.
These passing tests prove the current boundary, not completion of the requested acceptance proof.
Existing B/C/E tests supply negative/rejected/no-useful, duplicate, replay, concurrent reservation,
corrupt identity, recovery and protected-holdout regression coverage; no new loop is claimed.

### History, time and scientific boundaries

Current visible model input remains the initial development session count/checksum/cutoff, allowed
strategy/period and fixed empty history. C's private snapshot holds full frozen bars/features,
commitment, sanitized attempts and checked native result references. It is NOT model input.
No retained outcome is currently projected to a subsequent reasoner invocation.

A future B implementation needs a Finance-owned versioned projection, with deterministic selection,
complete negative accounting, explicit history availability times and knowledge cutoff checks.
A prior synthetic data session date alone cannot establish when a later research result became
known. C records UTC attempt times, but has no history-projection availability/selection contract.
Do not backdate completed results, substitute wall-clock audit time into experiment identities,
or infer safe historical visibility merely because a result can be deserialized.

Protected holdout rows/features/outcomes and proxies, full robustness objects/classifications,
selection/holdout result IDs and full-scope fingerprints must remain hidden. A consumed cohort is
not automatically a reviewed closed cohort whose outcomes may be exposed. A future projection must
prove noninterference when only protected data changes, including its digest and selected summaries.
Development-only outcome facts may be a narrower path, but their exact schema/selection is not
implemented or accepted here. No scientific vocabulary/falsification rule is changed.

Existing immutable IDs/checksums, immediate SQLite reservation-before-computation, no refund after
uncertainty, duplicate links and persistent negative history are reusable. None grants an unused
second invocation. Using a new DB, protocol label or fabricated fresh state to demonstrate N+1
would defeat the very persistence/budget constraints this checkpoint must preserve; none is done.

### Bounded decision needed before implementation resumes

Clarify whether B may introduce a **new versioned finite protocol** whose inference budget permits
N and N+1, while preserving every spent v1 record and its original ceilings. The smallest proposed
scope to review is two total model-free reasoner invocations, at most one scientific evaluation /
three trials / 64 underlying calls, concurrency one, zero retries and no renewed holdout. N+1 may
observe only approved development/operational history and decline; any duplicate links prior work,
and any new scientific request still fails exhausted scientific/exposure authority.

This is a proposal for review, not an implemented or already accepted policy. Even that narrow
path requires durable second-invocation accounting and a history-aware input/admission binding.
The grant must be frozen explicitly and must not appear automatically when a spent v1 DB reopens.
Owner/architect must decide compatibility/migration/grant semantics and the precise safe projection
before implementation changes those accepted boundaries. No new ADR is silently accepted.
If the intended product instead requires two different evaluated experiments, independently qualified
new evidence and cumulative scientific/cohort budget policy must also be specified; this report
neither grants that expansion nor claims the current single-variant vocabulary can deliver it.

### Baseline CI distinction

Owner explicitly designates `2caf2eb898acaafa759c6287d02442a90bb666db` as accepted baseline.
Read-only verification found [Actions 36844601675](https://github.com/T-bear/BigBrain/actions/runs/36844601675),
attempt 1, conclusion failure. The preceding publication handoff identified Gitleaks action's
GitHub user lookup rate limit followed by its missing-license error. No CI rerun/configuration,
secret or license change is made here. This known baseline operational CI issue is separate from
the newly characterized protocol decision; local verification does not imply green baseline CI.

### Verification

| Command | Actual result |
| --- | --- |
| `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~ResearchLearningContractTests\|FullyQualifiedName~FinanceLearningLedgerTests\|FullyQualifiedName~ResearchReasonerContractTests'` | Existing B/C/E baseline: 136 PASS / 0 FAIL / 0 SKIP |
| `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~ResearchLearningIterationBoundaryTests` | New characterization: 5 PASS / 0 FAIL / 0 SKIP |
| `dotnet restore BigBrain.slnx` | PASS |
| `dotnet format BigBrain.slnx --verify-no-changes --no-restore` | PASS, no changes |
| `dotnet build BigBrain.slnx --configuration Release --no-restore` | PASS, zero warnings/errors |
| `BB132A_LOCAL_ACCEPTANCE=disabled dotnet test BigBrain.slnx --configuration Release --no-build` | API 879 PASS / 0 FAIL / 7 intentional real-model skips; Sentinel 32 PASS / 0 FAIL / 0 SKIP |
| `node scripts/verify-documentation.mjs` | PASS, 259 Markdown files / 91 unique backlog IDs |
| `git diff --check` | PASS |

Full backend tests include existing Finance science/persistence/risk, B/C/E/F, concurrency/recovery
and A model-free regressions. No new frontend run is needed: Web and package/CI/deployment surfaces
are unchanged. Production source diff is empty. No real-model result is claimed.
Initial documentation check correctly rejected missing report-template headings/sanitization notice;
only the new report was conformed, then verification passed. Final staged scope/secrets are checked
before publication. Gitleaks 8.28.0 `git . --redact --no-banner`: PASS, 301 commits, no leaks.
Gitleaks `dir` on all eleven intended files: PASS, no leaks. Final staged content is rechecked.
The first new-test build encountered CA1861 on a constant array argument; corrected only in the
new test with a static readonly fixture array, then all five cases passed. No production fix.

### Changes

Only a new characterization test and relevant canonical docs/report are changed. Source, parser,
admission, schema, identities, ledger, engines, risk, runtime, native worker/prompt/grammar,
packages, Web, CI, Sentinel and deployment configuration remain unchanged.
### Security

No existing model ledger/audit/artifact is opened or modified; A's inference grants remain consumed.
Tests create only isolated temporary synthetic Finance databases through existing fixtures.
No model selection/download/inference, real data/export/provider, GPU/CUDA, cloud fallback, daemon,
public endpoint, executable model output, trading, deployment, merge or BB-132C implementation.
Finance RESEARCH / 0 SEK / NONE. Sanitized report contains no raw model/provider/host data or secrets.

### Remaining work

The requested production orchestration, history projector, N+1 acceptance, temporal-history checks
and multi-iteration persistence/replay are NOT IMPLEMENTED. These remain BB-132B work, not a new
checkpoint or an accepted feature. The five tests do not pretend to satisfy those missing criteria.
### Resumption

After publication STOP for independent review of this exact Review Checkpoint and the bounded
protocol/projection decision above. Continue on this same branch only after that decision; no merge.

### Exact publication scope

Eleven intended files: one new characterization test and ten Markdown documents:

- tests/BigBrain.Api.Tests/ResearchLearningIterationBoundaryTests.cs
- ROADMAP.md
- TESTING.md
- docs/STATUS.md
- docs/BACKLOG.md
- docs/modules/finance.md
- docs/architecture/finance/master-roadmap.md
- docs/architecture/finance/research-learning-contract.md
- docs/operations/codex-recovery.md
- docs/reports/REPORT-CATALOG.md
- docs/reports/features/finance/bb-132b-research-loop-boundary-20261001.md

README, ARCHITECTURE, ADRs, security/runbooks and documentation index were assessed; no decision,
implementation or runtime change requires an edit there. The existing report catalog discovers this
report. Prior A–F/A reports and their experiment evidence remain unchanged. Unrelated untracked
mockups and unpublished ADR 0006–0009 are preserved and excluded. No main push is authorized here.
