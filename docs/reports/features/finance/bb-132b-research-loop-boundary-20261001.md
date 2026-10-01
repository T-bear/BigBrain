# BB-132B — Persistent research-loop boundary characterization

Detta är en sanerad GitHub-version. Synthetic engineering characterization only.

## Metadata

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

## Status

This checkpoint is authorized, but the request also requires preserving existing budget/exposure
limits. Inspection found that those concrete limits prohibit N+1 before its reasoner can run.
This is an intentional accepted restriction, not a reproduced correctness/security/science defect.
The AGENTS architecture-conflict stop and ordinary Review Checkpoint path apply; this is not a
pre-existing-defect BLOCKER HANDOFF, nor evidence of a completed autonomous research loop.

## Exact reuse findings

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

## Evidence

### Model-free characterization

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

## History, time and scientific boundaries

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

## Bounded decision needed before implementation resumes

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

## Baseline CI distinction

Owner explicitly designates `2caf2eb898acaafa759c6287d02442a90bb666db` as accepted baseline.
Read-only verification found [Actions 36844601675](https://github.com/T-bear/BigBrain/actions/runs/36844601675),
attempt 1, conclusion failure. The preceding publication handoff identified Gitleaks action's
GitHub user lookup rate limit followed by its missing-license error. No CI rerun/configuration,
secret or license change is made here. This known baseline operational CI issue is separate from
the newly characterized protocol decision; local verification does not imply green baseline CI.

## Verification

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

## Changes

Only a new characterization test and relevant canonical docs/report are changed. Source, parser,
admission, schema, identities, ledger, engines, risk, runtime, native worker/prompt/grammar,
packages, Web, CI, Sentinel and deployment configuration remain unchanged.
## Security

No existing model ledger/audit/artifact is opened or modified; A's inference grants remain consumed.
Tests create only isolated temporary synthetic Finance databases through existing fixtures.
No model selection/download/inference, real data/export/provider, GPU/CUDA, cloud fallback, daemon,
public endpoint, executable model output, trading, deployment, merge or BB-132C implementation.
Finance RESEARCH / 0 SEK / NONE. Sanitized report contains no raw model/provider/host data or secrets.

## Remaining work

The requested production orchestration, history projector, N+1 acceptance, temporal-history checks
and multi-iteration persistence/replay are NOT IMPLEMENTED. These remain BB-132B work, not a new
checkpoint or an accepted feature. The five tests do not pretend to satisfy those missing criteria.
## Resumption

After publication STOP for independent review of this exact Review Checkpoint and the bounded
protocol/projection decision above. Continue on this same branch only after that decision; no merge.

## Exact publication scope

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
